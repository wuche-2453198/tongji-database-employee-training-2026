# 后端业务模块模板

> 开始写代码前先通读本文,并对照两份权威文档:
> 数据库列名以 `database/oracle/migrations/V001__init_schema.sql` 为准;
> 架构与分层规则见 `document/02-技术设计/技术架构与开发规范.md`。
> 参考实现:培训申请模块(`OracleTrainingRequestRepository` / `TrainingRequestService` / `TrainingRequestsController`)。

## 1. 文件边界

```text
Controllers/TrainingRequestsController.cs        # 路由用小写连字符: api/training-requests
Services/Interfaces/ITrainingRequestService.cs
Services/Implementations/TrainingRequestService.cs
Repositories/Interfaces/ITrainingRequestRepository.cs
Repositories/Implementations/OracleTrainingRequestRepository.cs   # 命名: Oracle + 模块名 + Repository
Dtos/TrainingRequest/CreateTrainingRequestDto.cs                  # Dto 按模块建子目录
Entities/TrainingRequest.cs
```

- Controller 只做三件事:接收路由/查询/请求体 → 调用 Service → 用 `OkResponse`/`CreatedResponse` 返回。
- Service 负责业务规则、状态机、**数据范围权限**和事务边界。
- Repository 只做 Oracle 数据访问,SQL 必须参数化,禁止拼接用户输入。
- 不要自建响应类、异常类、连接管理——Common 里都有(见 §5、§6)。

## 2. Controller 形态

```csharp
[ApiController]
[Route("api/training-requests")]                     // 小写连字符,不用 [controller](会生成 PascalCase)
public sealed class TrainingRequestsController : ApiControllerBase   // 继承基类才有 OkResponse/CreatedResponse
{
    [HttpPost]
    [Authorize]                                      // 所有接口必须有;鉴权三选一,不要叠加
    [Authorize(Policy = AuthorizationPolicies.ManagerHrOrAdmin)]     // 方式A:已有策略 AdminOnly / HrOrAdmin / ManagerHrOrAdmin
    // [Authorize(Roles = RoleCodes.DepartmentManager + "," + RoleCodes.Admin)]  // 方式B:无现成策略时用 RoleCodes 拼接
    public async Task<ActionResult<ApiResponse<TrainingRequestResponseDto>>> Create(
        [FromBody] CreateTrainingRequestDto dto, CancellationToken cancellationToken)
    {
        var actor = GetActor();                      // 见 §4
        var created = await _service.SubmitRequestAsync(actor.EmployeeId, dto, cancellationToken);
        return CreatedResponse(nameof(GetById), new { id = created.Id }, created, "申请提交成功");   // 创建用 201
        // 查询/更新用: return OkResponse(data);
    }
}
```

- 请求 DTO 必须带校验特性:`[Required]`、`[Range(1, int.MaxValue)]`、`[StringLength(500)]`(长度对照 DDL 列宽)。
- 返回 DTO,不返回 Entity;列表返回 `PagedResult<T>`。
- `CancellationToken` 一路透传到 Repository。

## 3. Repository 形态(Dapper)

```csharp
public sealed class OracleTrainingRequestRepository : ITrainingRequestRepository
{
    private readonly IDbConnectionFactory _connectionFactory;   // 不要注册共享 IDbConnection

    public async Task<TrainingRequest?> GetByIdAsync(int id, CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT r.REQUEST_ID AS "Id", e.EMP_NAME AS "EmployeeName"
            FROM TRAINING_REQUESTS r
                LEFT JOIN EMPLOYEES e ON r.EMP_ID = e.EMP_ID
            WHERE r.REQUEST_ID = :Id
            """;

        await using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        return await connection.QueryFirstOrDefaultAsync<TrainingRequest>(
            new CommandDefinition(sql, new { Id = id }, cancellationToken: cancellationToken));
    }
}
```

硬性规则:

- 列名**逐列对照 V001 DDL**;别名加双引号并精确匹配 C# 属性名(如 `AS "EmployeeName"`)。
- 分页:`OFFSET :Offset ROWS FETCH NEXT :PageSize ROWS ONLY` + 单独的 `COUNT(*)`;不要"收了 page 参数却不用"。
- 绑定变量命名避开 Oracle 保留字:`:Comment` 会报 ORA-01745,用 `:Remark` 等替代(保留字还有 COMMENT、LEVEL、SIZE、TYPE 等)。
- 查询结果里"由代码决定的标志位"(如 `Exists`)不在 SELECT 列里,Dapper 不会赋值——查询后显式赋值。
- 实体用普通 `get; set;` 类;不要用 positional record 接收查询结果(Dapper 构造函数匹配会失败)。
- 状态机更新必须带前置条件并检查影响行数,天然防并发跳步:

```csharp
UPDATE TRAINING_REQUESTS SET STATUS = :NewStatus, ...
WHERE REQUEST_ID = :Id AND STATUS = :ExpectedStatus
-- rows == 0 说明状态已被并发修改,Service 层应抛 ConflictApiException
```

## 4. 当前用户与数据范围

- 取当前员工 ID:用现成扩展 `User.GetEmployeeId()`(`Common/Extensions/ClaimsPrincipalExtensions.cs`),不要手写 FindFirst。
- 需要角色集合时,在 Controller 构建 Actor 记录传给 Service(参考 `Dtos/TrainingRequest/ActorContext.cs`),`IsAdmin`/`IsHr`/`IsManager` 判定放记录上。
- **数据范围是强制规则**:主管只看/审本部门,HR 与管理员跨部门,普通员工只看本人。实现方式:Service 里查询审批人所属部门(`SELECT DEPT_ID FROM EMPLOYEES WHERE EMP_ID = :id`,只读,等组织模块 Service 就绪后替换),把 `deptId` 作为过滤条件下推到 SQL(`EXISTS` 子查询)。
- 员工查详情必须校验属主;主管校验部门;判断失败抛 `ForbiddenApiException`。

## 5. 异常与统一响应

| 场景 | 抛法 | HTTP |
| --- | --- | --- |
| 数据不存在 | `NotFoundApiException("申请不存在。")` | 404 |
| 角色/数据范围不允许 | `ForbiddenApiException("您只能审批本部门员工的申请。")` | 403 |
| 重复提交/并发冲突/满员 | `ConflictApiException("您已提交过该课程的申请,请勿重复提交。")` | 409 |
| 状态流转不合法/参数业务校验失败 | `BusinessException("课程尚未发布,无法申请。")` | 400 |

- 全部由 `Middlewares/ExceptionHandlingMiddleware` 统一转成带 traceId 的 ApiResponse,**Controller 里不要写 try/catch**。
- 禁止抛 `ArgumentException`/`InvalidOperationException`(会变成 500);禁止返回手写匿名对象。

## 6. 枚举与 DI 注册

- 状态值统一放 `Common/Enums/`:枚举 + 与 DDL CHECK 对齐的文本常量(参考 `TrainingRequestStatusText`),不要在 Service/Repository 里散落魔法字符串。
- DI 注册集中在 `Program.cs` 既有区块,每类一行,顺序:Repository 在前、Service 在后:

```csharp
builder.Services.AddScoped<ITrainingRequestRepository, OracleTrainingRequestRepository>();
builder.Services.AddScoped<ITrainingRequestService, TrainingRequestService>();
```

## 7. 提交前自查

- [ ] `dotnet build` 0 错误(在本地跑过,贴输出);
- [ ] 所有接口连真实云库手测过至少一条成功路径和一条失败路径;
- [ ] SQL 列名与 V001 逐列核对;绑定变量无保留字;
- [ ] 数据范围用三类账号(本部门主管/HR 或管理员/普通员工)各测一遍越权;
- [ ] Swagger/OpenAPI、`tests/` 用例、涉及文档已同步。
