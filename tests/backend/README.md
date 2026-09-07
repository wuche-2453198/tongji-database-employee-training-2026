# 课程 / 讲师模块测试

在仓库根目录运行（需要 .NET 8 运行时及兼容 SDK）：

```powershell
dotnet run --project tests/backend/TrainingManagement.Api.ModuleTests.csproj -- --auth-http
```

依赖已还原时可在 `-- --auth-http` 前加 `--no-restore`。省略 `--auth-http` 运行 20 项 Service 测试和 6 项 DTO 边界测试；包含 HTTP 综合测试时共 27 项。

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
