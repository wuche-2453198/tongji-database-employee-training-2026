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

    /// <summary>通过依赖注入保存本类所需协作对象，供后续方法使用。</summary>
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

    /// <summary>验证登录信息；优先查数据库，显式开启演示账号后才尝试本地账号，成功时签发令牌。</summary>
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

    /// <summary>从已认证身份提取员工编号，优先读取数据库；未找到记录时按配置尝试演示用户，最后使用令牌声明构造用户。</summary>
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

    /// <summary>查询员工、校验密码并加载角色；凭据错误直接拒绝，数据库异常仅在允许回退时转为未找到用户。</summary>
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

    /// <summary>仅当演示账号和数据库失败回退两个开关同时开启时，允许异常回退。</summary>
    private bool CanFallbackToLocalUsers()
    {
        return _authOptions.EnableLocalDemoUsers
            && _authOptions.FallbackToLocalUsersOnDatabaseFailure;
    }

    /// <summary>使用 BCrypt 验证密码哈希；空哈希或盐格式错误均视为校验失败。</summary>
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

    /// <summary>按演示配置匹配账号和共用密码；开关关闭时不参与认证。</summary>
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

    /// <summary>检查在职状态，组装用户资料、角色和去重权限；没有角色记录时使用员工默认角色。</summary>
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

    /// <summary>统一角色代码并解析权限，兼容数据库中的 MANAGER 角色名称。</summary>
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

    /// <summary>将本地演示配置转换为登录响应用户，不查询数据库。</summary>
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

    /// <summary>从令牌声明重建用户资料；此路径不重新核验数据库中的员工状态。</summary>
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
