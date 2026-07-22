# 后端业务模块模板

新增业务模块建议保持以下文件边界：

```text
Controllers/EmployeesController.cs
Services/Interfaces/IEmployeeService.cs
Services/Implementations/EmployeeService.cs
Repositories/Interfaces/IEmployeeRepository.cs
Repositories/Implementations/OracleEmployeeRepository.cs
Dtos/Employees/CreateEmployeeRequest.cs
Dtos/Employees/UpdateEmployeeRequest.cs
Dtos/Employees/EmployeeQuery.cs
Dtos/Employees/EmployeeResponse.cs
Entities/EmployeeRecord.cs
```

Controller 只做三件事：

- 接收路由、查询参数和请求体。
- 调用 Service。
- 使用 `OkResponse(...)` 返回统一响应。

Service 负责业务规则和状态流转，例如员工状态、课程发布状态、报名容量、审批流程。

Repository 只负责 Oracle 数据访问，SQL 必须参数化，禁止拼接用户输入。

示例 Controller 形态：

```csharp
[Authorize]
[Route("api/employees")]
public sealed class EmployeesController : ApiControllerBase
{
    private readonly IEmployeeService _employeeService;

    public EmployeesController(IEmployeeService employeeService)
    {
        _employeeService = employeeService;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<PagedResult<EmployeeResponse>>>> GetAll(
        [FromQuery] EmployeeQuery query,
        CancellationToken cancellationToken)
    {
        var employees = await _employeeService.GetAllAsync(query, cancellationToken);
        return OkResponse(employees);
    }
}
```

示例 Repository 查询形态：

```csharp
const string sql = """
    SELECT
        e.EMP_ID AS "EmpId",
        e.EMP_NAME AS "EmpName"
    FROM EMPLOYEES e
    WHERE (:Keyword IS NULL OR e.EMP_NAME LIKE '%' || :Keyword || '%')
    """;

await using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
var rows = await connection.QueryAsync<EmployeeRecord>(
    new CommandDefinition(sql, new { query.Keyword }, cancellationToken: cancellationToken));
```
