# 后端模块测试

课程、讲师、成果评估和认证权限的模块级回归测试，位于 `tests/backend/`。`.http` 手工用例在 `tests/api/`，不要放回本目录。

## 运行方式

在仓库根目录执行（需要 .NET 8 运行时及兼容 SDK）：

```powershell
dotnet run --project tests/backend/TrainingManagement.Api.ModuleTests.csproj
dotnet run --project tests/backend/TrainingManagement.Api.ModuleTests.csproj -- --auth-http
```

`--auth-http` 会额外启动真实 API 子进程做登录与权限矩阵检查；依赖已还原时可在末尾加 `--no-restore`。

## 测试清单（2026-09-09）

| 文件 | 项数 | 覆盖内容 |
| --- | --- | --- |
| `CourseServiceTests.cs` | 18 | 课程创建默认 `DRAFT`、部门存在性、发布后冻结字段、状态机与并发、容量统计、发布必填项 |
| `TrainerServiceTests.cs` | 9 | 讲师 CRUD、过滤、分页、星级 1~5、内部讲师 `Y/N`、缺失记录 |
| `ResultModuleTests.cs` | 12 | 评分资格与重复、PRE/POST 时点与分数、证书资格（`COMPLETED`、`POST >= 60`）、重复发证、越权查询 |
| `RequestValidationTests.cs` | 7 | `Trainer`/`Course` DTO 的电话长度、学时与人数精度边界，对照 `V001` |
| `RepositoryContractTests.cs` | 5 | Dapper 命令绑定、分页参数、条件 `UPDATE`、数字/空值映射、输出 ID |
| `HttpAuthorizationTests.cs` | 1（仅 `--auth-http`） | 四角色登录、JWT claim、`/api/auth/me`，以及 10 个课程/讲师路由 × 6 种鉴权组合共 60 次请求 |
| 合计 | **51**（加 `--auth-http` 为 **52**） | |

## 最近验证记录（2026-09-09）

- `dotnet build src/backend/TrainingManagement.Api/TrainingManagement.Api.csproj --no-incremental`：**成功，0 错误，1 警告**（`Controllers/RatingsController.cs:62` CS8604，历史遗留，待成果评估模块清理）。
- `dotnet run --project tests/backend/TrainingManagement.Api.ModuleTests.csproj`：**51/51 通过**。
- `dotnet run --project tests/backend/TrainingManagement.Api.ModuleTests.csproj -- --auth-http`：**52/52 通过**，含 60 次 HTTP 权限请求。
- 讲师列表与课程列表统一返回 `PagedResult<T>`，支持 `Page`（默认 1）和 `PageSize`（默认 20，最大 100）；前端从 `data.items`、`data.page`、`data.pageSize`、`data.total` 读取。
- **未覆盖**：真实 Oracle 上的 SQL 执行、事务回滚、行锁与并发场景。发布路径 `PublishAsync` 目前固定返回 409（等待组织模块提供同一事务内的预算占用能力），用例 `Publish remains blocked without budget transaction` 就是这条临时行为的回归保护。

## Oracle 字段约束回归

创建及更新 DTO 的电话长度限制为 20，课程学时为 0.1～999.9，最大人数为 1～999999，与 `V001` 中对应字段容量对齐。Service 同时拒绝越界学时/人数及已发布课程的空白地点；草稿空地点及未开始课程的合法地点修改仍允许。课程分页偏移采用 long 计算，避免大页码发生 int 溢出。

## HTTP 登录与权限检查

`--auth-http` 启动真实 API 子进程，使用随机本地端口。仅为该进程通过运行参数清空 Oracle 连接串、启用开发演示账号、设置随机测试密码和 JWT 密钥；不会修改配置文件或写入共享数据库。测试结束（包括失败）会关闭子进程。

账号映射使用最新 `develop` 的 `appsettings.Development.json`，测试不会覆盖账号 ID 或角色：

| 账号 | 员工 ID | 角色 |
| --- | --- | --- |
| admin@example.com | 54 | ADMIN |
| employee@example.com | 55 | EMPLOYEE |
| manager@example.com | 56 | DEPT_MANAGER |
| hr@example.com | 57 | HR |

检查登录、JWT 的 `emp_id` / `NameIdentifier` / 角色、`GET /api/auth/me`，以及从两种 claim 单独读取员工 ID。

课程/讲师共 10 个路由分别检查无 token、无效 token 和四种角色，共 60 次权限请求：

- 无 token / 无效 token：401。
- 四种角色查询课程：通过鉴权。
- EMPLOYEE / DEPT_MANAGER 访问课程写接口或讲师管理：403。
- ADMIN / HR：通过管理权限检查；POST / PUT 使用无效请求体，预期进入参数校验返回 400；Publish / Close 使用不存在的课程 ID，预期 404。

这里的空列表和 404 来自未配置数据库时现有 Repository 的返回行为，只用于验证 HTTP 权限链路，不代表真实 Oracle CRUD 测试成功。

## 仍需联调

- 连接 Oracle 后确认实际表结构、SQL 执行、PUT / Close 更新及并发冲突；使用专用测试数据，参考 `tests/api/courses.http`。
- **课程发布仍被阻塞**：`PublishAsync` 返回 409，等待组织模块提供能接受同一连接与事务的预算占用能力；预算占用和课程状态更新必须一起提交或回滚。
- 容量摘要内部查询已实现，公开接口契约仍待团队确认。
- 更新演示账号配置后需重新登录获取 token；原 token 携带的旧 ID 不会自动变化。
- 组织、报名、前端三个模块合入后，本套测试需再跑一次并补跨模块用例。
