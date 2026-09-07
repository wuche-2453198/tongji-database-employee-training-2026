# 课程 / 讲师模块测试

在仓库根目录运行（需要 .NET 8 运行时及兼容 SDK）：

```powershell
dotnet run --project tests/backend/TrainingManagement.Api.ModuleTests.csproj -- --auth-http
```

依赖已还原时可在 `-- --auth-http` 前加 `--no-restore`。当前测试清单包含 27 项 Service、7 项 DTO 边界、5 项 Repository 命令/映射测试；加上 HTTP 综合测试共 40 项。清单数量不表示这些测试已全部通过，当前验证状态见下节。

讲师列表与课程列表统一返回 `PagedResult<T>`，支持 `Page`（默认 1）和 `PageSize`（默认 20，最大 100）；分页边界和总数元数据包含在 Service 回归测试中。

前端需要从统一响应的 `data.items` 读取讲师数组，同时读取 `data.page`、`data.pageSize` 和 `data.total`；旧的直接把 `data` 当数组的调用需要适配。

## 当前复核状态（尚未提交）

- 历史 29 项测试通过不覆盖本轮后续修改。
- 本轮新增用例先运行 34 项：33 通过，1 失败，复现课程创建未拒绝不存在部门的问题。
- 随后已添加 Create/PUT 共用的部门存在性校验；仓储只读 `DEPARTMENTS_TRAINING`，不维护预算。预检不代替数据库外键或并发删除场景的真库测试。
- 模拟讲师仓储已补过滤、排序和分页；增加冻结字段、终态、缺失记录、容量及 HTTP 分页/Swagger 检查。
- 新增 RepositoryContractTests 使用真实 Repository 和 Dapper，搭配记录命令及合成结果行，检查分页参数、条件 UPDATE、数字/空值映射和输出 ID。它不执行 Oracle SQL，不证明服务器端语法、锁或回滚行为。
- 修改后的完整复跑被自动审批用量限制阻止，尚无本轮最终 build / 40 项通过结果，不能作为验收通过记录。
- 本轮 GitHub fetch 失败（连接重置 / TLS EOF），本地仍未同步缓存中较新的 develop；提交前需恢复验证并完成集成检查。

待运行：

```powershell
dotnet build src/backend/TrainingManagement.Api/TrainingManagement.Api.csproj -warnaserror
dotnet run --project tests/backend/TrainingManagement.Api.ModuleTests.csproj -- --auth-http
git diff --check
```

## Oracle 字段约束回归

创建及更新 DTO 的电话长度限制为 20，课程学时为 0.1～999.9，最大人数为 1～999999，与 V001 中对应字段容量对齐。Service 同时拒绝越界学时/人数及已发布课程的空白地点；草稿空地点及未开始课程的合法地点修改仍允许。课程分页偏移采用 long 计算，避免大页码发生 int 溢出。

2026-09-07 验证：修改前新增用例出现 8 项预期失败，修复后 27 项全部通过，build 0 警告、0 错误。HTTP 测试含 60 次权限请求。真库端口探测仍超时，以上结果不包含 Oracle 实库成功/失败路径。

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
- Publish 等待组织模块提供能接受同一连接与事务的预算占用能力；预算占用和课程状态更新必须一起提交或回滚。当前仍明确返回 409，不执行发布。
- 容量摘要内部查询已实现，公开接口契约仍待团队确认。
- 更新演示账号配置后需重新登录获取 token；原 token 携带的旧 ID 不会自动变化。

本次同步依据：`develop` 提交 `e382beb`（演示账号 ID 修复）。
