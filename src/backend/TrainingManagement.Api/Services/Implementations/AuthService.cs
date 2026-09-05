using System.Security.Claims;
using Microsoft.Extensions.Options;
using TrainingManagement.Api.Common.Exceptions;
using TrainingManagement.Api.Common.Extensions;
using TrainingManagement.Api.Common.Options;
using TrainingManagement.Api.Common.Security;
using TrainingManagement.Api.Dtos.Auth;
using TrainingManagement.Api.Entities;
using TrainingManagement.Api.Repositories.Interfaces;
using TrainingManagement.Api.Services.Interfaces;

namespace TrainingManagement.Api.Services.Implementations;

public sealed class AuthService : IAuthService
{
    private readonly IAuthRepository _authRepository;
    private readonly ITokenService _tokenService;
    private readonly AuthOptions _authOptions;
    private readonly ILogger<AuthService> _logger;

    public AuthService(
        IAuthRepository authRepository,
        ITokenService tokenService,
        IOptions<AuthOptions> authOptions,
        ILogger<AuthService> logger)
    {
        _authRepository = authRepository;
        _tokenService = tokenService;
        _authOptions = authOptions.Value;
        _logger = logger;
    }

    public async Task<LoginResponse> LoginAsync(
        LoginRequest request,
        CancellationToken cancellationToken)
    {
        var identifier = request.Identifier.Trim();
        var password = request.Password.Trim();

        var user = await FindDatabaseUserAsync(identifier, password, cancellationToken)
            ?? FindLocalDemoUser(identifier, password);

        if (user is null)
        {
            throw new UnauthorizedApiException("Invalid identifier or password.");
        }

        var (token, expiresAt) = _tokenService.CreateToken(user);

        return new LoginResponse
        {
            AccessToken = token,
            ExpiresAt = expiresAt,
            User = user
        };
    }

    public async Task<AuthUserResponse> GetCurrentUserAsync(
        ClaimsPrincipal principal,
        CancellationToken cancellationToken)
    {
        var empId = principal.GetEmployeeId();

        if (empId is null)
        {
            throw new UnauthorizedApiException("Invalid token.");
        }

        try
        {
            var employee = await _authRepository.GetEmployeeByIdAsync(empId.Value, cancellationToken);

            if (employee is not null)
            {
                var roles = await _authRepository.GetRolesByEmployeeIdAsync(empId.Value, cancellationToken);
                return BuildUser(employee, roles);
            }
        }
        catch (Exception exception) when (CanFallbackToLocalUsers())
        {
            _logger.LogWarning(
                exception,
                "Falling back to local demo user for /api/auth/me. EmpId: {EmpId}",
                empId.Value);
        }

        if (_authOptions.EnableLocalDemoUsers)
        {
            var localUser = _authOptions.LocalDemoUsers.FirstOrDefault(item => item.EmpId == empId.Value);
            if (localUser is not null)
            {
                return BuildLocalDemoUser(localUser);
            }
        }

        return BuildUserFromClaims(principal, empId.Value);
    }

    private async Task<AuthUserResponse?> FindDatabaseUserAsync(
        string identifier,
        string password,
        CancellationToken cancellationToken)
    {
        try
        {
            var employee = await _authRepository.FindEmployeeForLoginAsync(identifier, cancellationToken);

            if (employee is null)
            {
                return null;
            }

            if (!VerifyPassword(password, employee.PasswordHash))
            {
                throw new UnauthorizedApiException("Invalid identifier or password.");
            }

            var roles = await _authRepository.GetRolesByEmployeeIdAsync(employee.EmpId, cancellationToken);
            return BuildUser(employee, roles);
        }
        catch (UnauthorizedApiException)
        {
            throw;
        }
        catch (Exception exception) when (CanFallbackToLocalUsers())
        {
            _logger.LogWarning(
                exception,
                "Database authentication failed, local demo users may be used. Identifier: {Identifier}",
                identifier);

            return null;
        }
    }

    private bool CanFallbackToLocalUsers()
    {
        return _authOptions.EnableLocalDemoUsers
            && _authOptions.FallbackToLocalUsersOnDatabaseFailure;
    }

    private static bool VerifyPassword(string password, string passwordHash)
    {
        if (string.IsNullOrWhiteSpace(passwordHash))
        {
            return false;
        }

        try
        {
            return BCrypt.Net.BCrypt.Verify(password, passwordHash);
        }
        catch (BCrypt.Net.SaltParseException)
        {
            return false;
        }
    }

    private AuthUserResponse? FindLocalDemoUser(string identifier, string password)
    {
        if (!_authOptions.EnableLocalDemoUsers
            || !string.Equals(password, _authOptions.LocalDemoPassword, StringComparison.Ordinal))
        {
            return null;
        }

        var user = _authOptions.LocalDemoUsers.FirstOrDefault(item =>
            string.Equals(item.EmpId.ToString(), identifier, StringComparison.OrdinalIgnoreCase)
            || string.Equals(item.Email, identifier, StringComparison.OrdinalIgnoreCase)
            || string.Equals(item.Phone, identifier, StringComparison.OrdinalIgnoreCase)
            || string.Equals(item.EmpName, identifier, StringComparison.OrdinalIgnoreCase));

        return user is null ? null : BuildLocalDemoUser(user);
    }

    private static AuthUserResponse BuildUser(
        EmployeeAuthRecord employee,
        IReadOnlyCollection<RoleRecord> roleRecords)
    {
        if (!EmployeeStatusValues.IsActive(employee.Status))
        {
            throw new ForbiddenApiException("Employee is not active.");
        }

        var roles = roleRecords.Count == 0
            ? new[]
            {
                new AuthRoleResponse
                {
                    RoleId = 0,
                    RoleCode = RoleCodes.Employee,
                    RoleName = RoleCodes.Employee,
                    Permissions = PermissionCodes.GetDefaultPermissions(RoleCodes.Employee)
                }
            }
            : roleRecords.Select(ToAuthRoleResponse).ToArray();

        return new AuthUserResponse
        {
            EmpId = employee.EmpId,
            EmpName = employee.EmpName,
            DeptName = employee.DeptName,
            Position = employee.Position,
            Email = employee.Email,
            Phone = employee.Phone,
            Status = string.IsNullOrWhiteSpace(employee.Status)
                ? EmployeeStatusValues.Active
                : employee.Status,
            Roles = roles,
            Permissions = roles
                .SelectMany(role => role.Permissions)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(item => item, StringComparer.OrdinalIgnoreCase)
                .ToArray()
        };
    }

    private static AuthRoleResponse ToAuthRoleResponse(RoleRecord role)
    {
        var roleCode = RoleCodes.Normalize(string.IsNullOrWhiteSpace(role.RoleCode)
            ? role.RoleName
            : role.RoleCode);

        return new AuthRoleResponse
        {
            RoleId = role.RoleId,
            RoleCode = roleCode,
            RoleName = role.RoleName,
            Permissions = RolePermissionParser.Parse(role.Permissions, roleCode)
        };
    }

    private static AuthUserResponse BuildLocalDemoUser(LocalDemoUserOptions user)
    {
        var roles = user.Roles.Count == 0
            ? new[] { RoleCodes.Employee }
            : user.Roles;

        var authRoles = roles
            .Select((role, index) =>
            {
                var roleCode = RoleCodes.Normalize(role);
                return new AuthRoleResponse
                {
                    RoleId = index + 1,
                    RoleCode = roleCode,
                    RoleName = role,
                    Permissions = PermissionCodes.GetDefaultPermissions(roleCode)
                };
            })
            .ToArray();

        return new AuthUserResponse
        {
            EmpId = user.EmpId,
            EmpName = user.EmpName,
            DeptName = user.DeptName,
            Position = user.Position,
            Email = user.Email,
            Phone = user.Phone,
            Status = user.Status,
            Roles = authRoles,
            Permissions = authRoles
                .SelectMany(role => role.Permissions)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(item => item, StringComparer.OrdinalIgnoreCase)
                .ToArray()
        };
    }

    private static AuthUserResponse BuildUserFromClaims(ClaimsPrincipal principal, long empId)
    {
        var roles = principal
            .FindAll(ClaimTypes.Role)
            .Select((claim, index) =>
            {
                var roleCode = RoleCodes.Normalize(claim.Value);
                return new AuthRoleResponse
                {
                    RoleId = index + 1,
                    RoleCode = roleCode,
                    RoleName = claim.Value,
                    Permissions = PermissionCodes.GetDefaultPermissions(roleCode)
                };
            })
            .ToArray();

        var permissions = principal
            .FindAll(TrainingClaimTypes.Permission)
            .Select(claim => claim.Value)
            .Concat(roles.SelectMany(role => role.Permissions))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(item => item, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        return new AuthUserResponse
        {
            EmpId = empId,
            EmpName = principal.FindFirstValue(ClaimTypes.Name) ?? empId.ToString(),
            DeptName = principal.FindFirstValue(TrainingClaimTypes.Department),
            Position = principal.FindFirstValue(TrainingClaimTypes.Position),
            Email = principal.FindFirstValue(ClaimTypes.Email),
            Status = EmployeeStatusValues.Active,
            Roles = roles,
            Permissions = permissions
        };
    }
}
