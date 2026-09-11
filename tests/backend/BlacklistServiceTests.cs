using System.Security.Claims;
using TrainingManagement.Api.Common.Exceptions;
using TrainingManagement.Api.Common.Responses;
using TrainingManagement.Api.Common.Security;
using TrainingManagement.Api.Dtos.Organization;
using TrainingManagement.Api.Entities;
using TrainingManagement.Api.Repositories.Interfaces;
using TrainingManagement.Api.Services.Implementations;

namespace TrainingManagement.Api.ModuleTests;

internal static class BlacklistServiceTests
{
    public static IEnumerable<(string Name, Func<Task> Run)> GetTests()
    {
        yield return ("Manager blacklists an employee in the same department", ManagerOwnDepartmentAsync);
        yield return ("Manager cannot blacklist another department", ManagerCrossDepartmentAsync);
        yield return ("Manager cannot blacklist themselves", ManagerSelfAsync);
        yield return ("Duplicate active blacklist conflicts", DuplicateAsync);
        yield return ("Missing employee returns 404", MissingEmployeeAsync);
        yield return ("Employee role is forbidden", EmployeeForbiddenAsync);
        yield return ("Missing identity returns 401", MissingIdentityAsync);
        yield return ("Empty reason returns 400", EmptyReasonAsync);
        yield return ("Manager list is scoped to own department", ManagerListScopedAsync);
        yield return ("Admin can blacklist across departments", AdminCrossDepartmentAsync);
        yield return ("Manager candidates are scoped to own department", ManagerCandidatesScopedAsync);
        yield return ("Admin candidates are not department scoped", AdminCandidatesUnscopedAsync);
        yield return ("Manager cannot blacklist a manager account", ManagerCannotBlacklistPrivilegedAsync);
    }

    private const long ManagerId = 98;
    private const long TargetId = 55;
    private const long OtherDeptTargetId = 77;
    private const string ManagerDept = "技术部";
    private const string OtherDept = "市场部";

    private static async Task ManagerOwnDepartmentAsync()
    {
        var employees = new FakeEmployeeRepository();
        employees.Employees[ManagerId] = Employee(ManagerId, ManagerDept);
        employees.Employees[TargetId] = Employee(TargetId, ManagerDept);
        var repository = new FakeBlacklistRepository();
        var service = new BlacklistService(repository, employees);

        var response = await service.CreateAsync(
            new CreateBlacklistRequest { EmpId = TargetId, Reason = "违规缺勤" },
            TestPrincipals.Manager(ManagerId),
            CancellationToken.None);

        TestAssert.Equal(TargetId, response.EmpId, "Created record must target the employee.");
        TestAssert.Equal(ManagerDept, response.DeptName, "Created record must carry the employee department.");
        TestAssert.True(response.BlackId > 0, "Created record must have a generated ID.");
    }

    private static async Task ManagerCrossDepartmentAsync()
    {
        var employees = new FakeEmployeeRepository();
        employees.Employees[ManagerId] = Employee(ManagerId, ManagerDept);
        employees.Employees[OtherDeptTargetId] = Employee(OtherDeptTargetId, OtherDept);
        var service = new BlacklistService(new FakeBlacklistRepository(), employees);

        await TestAssert.ThrowsAsync<ForbiddenApiException>(
            () => service.CreateAsync(
                new CreateBlacklistRequest { EmpId = OtherDeptTargetId, Reason = "违规" },
                TestPrincipals.Manager(ManagerId),
                CancellationToken.None),
            "A manager must not blacklist an employee outside their department.");
    }

    private static async Task ManagerSelfAsync()
    {
        var employees = new FakeEmployeeRepository();
        employees.Employees[ManagerId] = Employee(ManagerId, ManagerDept);
        var service = new BlacklistService(new FakeBlacklistRepository(), employees);

        await TestAssert.ThrowsAsync<BusinessException>(
            () => service.CreateAsync(
                new CreateBlacklistRequest { EmpId = ManagerId, Reason = "违规" },
                TestPrincipals.Manager(ManagerId),
                CancellationToken.None),
            "A manager must not blacklist themselves.");
    }

    private static async Task DuplicateAsync()
    {
        var employees = new FakeEmployeeRepository();
        employees.Employees[ManagerId] = Employee(ManagerId, ManagerDept);
        employees.Employees[TargetId] = Employee(TargetId, ManagerDept);
        var repository = new FakeBlacklistRepository();
        repository.Records.Add(new Blacklist
        {
            BlackId = 1001,
            EmpId = TargetId,
            Reason = "已有记录",
            Status = "ACTIVE",
            DeptName = ManagerDept
        });
        var service = new BlacklistService(repository, employees);

        await TestAssert.ThrowsAsync<ConflictApiException>(
            () => service.CreateAsync(
                new CreateBlacklistRequest { EmpId = TargetId, Reason = "重复" },
                TestPrincipals.Manager(ManagerId),
                CancellationToken.None),
            "An already blacklisted employee must conflict.");
    }

    private static async Task MissingEmployeeAsync()
    {
        var employees = new FakeEmployeeRepository();
        employees.Employees[ManagerId] = Employee(ManagerId, ManagerDept);
        var service = new BlacklistService(new FakeBlacklistRepository(), employees);

        await TestAssert.ThrowsAsync<NotFoundApiException>(
            () => service.CreateAsync(
                new CreateBlacklistRequest { EmpId = 9999, Reason = "违规" },
                TestPrincipals.Manager(ManagerId),
                CancellationToken.None),
            "A missing target employee must be 404.");
    }

    private static async Task EmployeeForbiddenAsync()
    {
        var employees = new FakeEmployeeRepository();
        employees.Employees[TargetId] = Employee(TargetId, ManagerDept);
        var service = new BlacklistService(new FakeBlacklistRepository(), employees);

        await TestAssert.ThrowsAsync<ForbiddenApiException>(
            () => service.CreateAsync(
                new CreateBlacklistRequest { EmpId = TargetId, Reason = "违规" },
                TestPrincipals.Employee(55),
                CancellationToken.None),
            "An employee must not add to the blacklist.");
    }

    private static async Task MissingIdentityAsync()
    {
        var employees = new FakeEmployeeRepository();
        employees.Employees[TargetId] = Employee(TargetId, ManagerDept);
        var service = new BlacklistService(new FakeBlacklistRepository(), employees);

        var principal = new ClaimsPrincipal(
            new ClaimsIdentity(new[] { new Claim(ClaimTypes.Role, RoleCodes.DepartmentManager) }, "test"));

        await TestAssert.ThrowsAsync<UnauthorizedApiException>(
            () => service.CreateAsync(
                new CreateBlacklistRequest { EmpId = TargetId, Reason = "违规" },
                principal,
                CancellationToken.None),
            "A missing identity must be 401.");
    }

    private static async Task EmptyReasonAsync()
    {
        var employees = new FakeEmployeeRepository();
        employees.Employees[ManagerId] = Employee(ManagerId, ManagerDept);
        employees.Employees[TargetId] = Employee(TargetId, ManagerDept);
        var service = new BlacklistService(new FakeBlacklistRepository(), employees);

        await TestAssert.ThrowsAsync<BusinessException>(
            () => service.CreateAsync(
                new CreateBlacklistRequest { EmpId = TargetId, Reason = "   " },
                TestPrincipals.Manager(ManagerId),
                CancellationToken.None),
            "An empty reason must be rejected.");
    }

    private static async Task ManagerListScopedAsync()
    {
        var employees = new FakeEmployeeRepository();
        employees.Employees[ManagerId] = Employee(ManagerId, ManagerDept);
        var repository = new FakeBlacklistRepository();
        repository.Records.Add(new Blacklist
        {
            BlackId = 1001,
            EmpId = TargetId,
            EmpName = "张三",
            Reason = "违规缺勤",
            Status = "ACTIVE",
            DeptName = ManagerDept,
            CreatedAt = DateTime.Now
        });
        repository.Records.Add(new Blacklist
        {
            BlackId = 1002,
            EmpId = OtherDeptTargetId,
            EmpName = "李四",
            Reason = "其他部门",
            Status = "ACTIVE",
            DeptName = OtherDept
        });
        var service = new BlacklistService(repository, employees);

        var result = await service.GetPagedAsync(
            new BlacklistQuery { Page = 1, PageSize = 20 },
            TestPrincipals.Manager(ManagerId),
            CancellationToken.None);

        TestAssert.Equal(1, result.Items.Count, "A manager must only see their own department records.");
        TestAssert.Equal(1001, result.Items.First().BlackId, "The scoped record must be returned.");
        TestAssert.Equal("张三", result.Items.First().EmpName, "The record must carry the employee name.");
    }

    private static async Task AdminCrossDepartmentAsync()
    {
        var employees = new FakeEmployeeRepository();
        employees.Employees[OtherDeptTargetId] = Employee(OtherDeptTargetId, OtherDept);
        var repository = new FakeBlacklistRepository();
        var service = new BlacklistService(repository, employees);

        var response = await service.CreateAsync(
            new CreateBlacklistRequest { EmpId = OtherDeptTargetId, Reason = "跨部门违规" },
            TestPrincipals.Admin(),
            CancellationToken.None);

        TestAssert.Equal(OtherDeptTargetId, response.EmpId, "Admin must be able to blacklist any department.");
    }

    private static async Task ManagerCandidatesScopedAsync()
    {
        var employees = new FakeEmployeeRepository();
        employees.Employees[ManagerId] = Employee(ManagerId, ManagerDept);
        employees.Roles[ManagerId] = new[] { RoleCodes.DepartmentManager };
        employees.Employees[TargetId] = Employee(TargetId, ManagerDept);
        employees.Employees[OtherDeptTargetId] = Employee(OtherDeptTargetId, OtherDept);
        // 同部门内的管理员账号必须从候选名单中排除
        employees.Employees[900] = Employee(900, ManagerDept);
        employees.Roles[900] = new[] { RoleCodes.Admin };
        var service = new BlacklistService(new FakeBlacklistRepository(), employees);

        var result = await service.GetCandidatesAsync(
            new BlacklistCandidateQuery { Page = 1, PageSize = 50 },
            TestPrincipals.Manager(ManagerId),
            CancellationToken.None);

        TestAssert.Equal(1, result.Items.Count, "Manager candidates must exclude privileged accounts.");
        TestAssert.Equal(TargetId, result.Items.First().EmpId, "Manager candidates must be scoped to own department.");
    }

    private static async Task AdminCandidatesUnscopedAsync()
    {
        var employees = new FakeEmployeeRepository();
        employees.Employees[TargetId] = Employee(TargetId, ManagerDept);
        employees.Employees[OtherDeptTargetId] = Employee(OtherDeptTargetId, OtherDept);
        var service = new BlacklistService(new FakeBlacklistRepository(), employees);

        var result = await service.GetCandidatesAsync(
            new BlacklistCandidateQuery { Page = 1, PageSize = 50 },
            TestPrincipals.Admin(),
            CancellationToken.None);

        TestAssert.Equal(2, result.Items.Count, "Admin candidates must span departments.");
    }

    private static async Task ManagerCannotBlacklistPrivilegedAsync()
    {
        var employees = new FakeEmployeeRepository();
        employees.Employees[ManagerId] = Employee(ManagerId, ManagerDept);
        employees.Employees[TargetId] = Employee(TargetId, ManagerDept);
        employees.Roles[TargetId] = new[] { RoleCodes.DepartmentManager };
        var service = new BlacklistService(new FakeBlacklistRepository(), employees);

        await TestAssert.ThrowsAsync<ForbiddenApiException>(
            () => service.CreateAsync(
                new CreateBlacklistRequest { EmpId = TargetId, Reason = "违规" },
                TestPrincipals.Manager(ManagerId),
                CancellationToken.None),
            "A manager must not blacklist another manager/admin/HR account.");
    }

    private static Employee Employee(long empId, string deptName)
    {
        return new Employee
        {
            EmpId = empId,
            EmpName = empId == ManagerId ? "主管" : "员工",
            DeptName = deptName,
            Status = "ACTIVE"
        };
    }

    private sealed class FakeBlacklistRepository : IBlacklistRepository
    {
        public List<Blacklist> Records { get; } = new();

        private long _nextId = 1001;

        public Task<PagedResult<Blacklist>> GetPagedAsync(
            BlacklistQuery query,
            CancellationToken cancellationToken = default)
        {
            var items = Records
                .Where(r => query.EmpId is null || r.EmpId == query.EmpId)
                .Where(r => string.IsNullOrWhiteSpace(query.Status) || r.Status == query.Status)
                .Where(r => string.IsNullOrWhiteSpace(query.DeptName) || r.DeptName == query.DeptName)
                .ToArray();

            return Task.FromResult(
                new PagedResult<Blacklist>(items, query.Page, query.PageSize, items.Length));
        }

        public Task<Blacklist?> GetByIdAsync(long blackId, CancellationToken cancellationToken = default)
        {
            return Task.FromResult<Blacklist?>(Records.FirstOrDefault(r => r.BlackId == blackId));
        }

        public Task<Blacklist?> GetActiveByEmployeeIdAsync(long empId, CancellationToken cancellationToken = default)
        {
            return Task.FromResult<Blacklist?>(Records
                .FirstOrDefault(r => r.EmpId == empId && r.Status == "ACTIVE"));
        }

        public Task<bool> IsEmployeeBlacklistedAsync(long empId, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(Records.Any(r => r.EmpId == empId && r.Status == "ACTIVE"));
        }

        public Task<bool> ExistsAsync(long blackId, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(Records.Any(r => r.BlackId == blackId));
        }

        public Task<Blacklist> CreateAsync(Blacklist entity, CancellationToken cancellationToken = default)
        {
            entity.BlackId = _nextId++;
            Records.Add(entity);
            return Task.FromResult(entity);
        }

        public Task<bool> UpdateAsync(Blacklist entity, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(true);
        }

        public Task<bool> DeleteAsync(long blackId, CancellationToken cancellationToken = default)
        {
            var count = Records.RemoveAll(r => r.BlackId == blackId);
            return Task.FromResult(count > 0);
        }
    }

    private sealed class FakeEmployeeRepository : IEmployeeRepository
    {
        public Dictionary<long, Employee> Employees { get; } = new();

        public Dictionary<long, string[]> Roles { get; } = new();

        public Task<PagedResult<Employee>> GetPagedAsync(
            EmployeeQuery query,
            CancellationToken cancellationToken = default)
        {
            var items = Employees.Values
                .Where(e => string.IsNullOrWhiteSpace(query.DeptName) || e.DeptName == query.DeptName)
                .ToArray();
            return Task.FromResult(
                new PagedResult<Employee>(items, query.Page, query.PageSize, items.Length));
        }

        public Task<PagedResult<Employee>> GetBlacklistCandidatesAsync(
            BlacklistCandidateQuery query,
            CancellationToken cancellationToken = default)
        {
            var items = Employees.Values
                .Where(e => string.IsNullOrWhiteSpace(query.DeptName) || e.DeptName == query.DeptName)
                .Where(e => !Roles.TryGetValue(e.EmpId, out var roles) || !roles.Any(IsPrivileged))
                .ToArray();
            return Task.FromResult(
                new PagedResult<Employee>(items, query.Page, query.PageSize, items.Length));
        }

        public Task<IReadOnlyCollection<string>> GetRoleCodesAsync(
            long empId,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyCollection<string>>(
                Roles.TryGetValue(empId, out var roles) ? roles : Array.Empty<string>());
        }

        private static bool IsPrivileged(string role)
        {
            var normalized = RoleCodes.Normalize(role);
            return normalized == RoleCodes.Admin
                || normalized == RoleCodes.Hr
                || normalized == RoleCodes.DepartmentManager;
        }

        public Task<Employee?> GetByIdAsync(long empId, CancellationToken cancellationToken = default)
        {
            return Task.FromResult<Employee?>(Employees.TryGetValue(empId, out var e) ? e : null);
        }

        public Task<bool> ExistsAsync(long empId, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(Employees.ContainsKey(empId));
        }

        public Task<bool> IsLoginNameExistsAsync(
            string loginName,
            long? excludeEmpId = null,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(false);
        }

        public Task<Employee> CreateAsync(Employee employee, CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }

        public Task<bool> UpdateAsync(Employee employee, CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }

        public Task<bool> DeleteAsync(long empId, CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }
    }
}
