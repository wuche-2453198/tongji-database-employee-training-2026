# API 测试入口

维护协调人：彭浩。所有主路径和关键失败路径使用 `.http` 文件或导出的 Postman/Apifox 集合保存，保证他人可复现。

## 当前文件状态（2026-09-09 盘点）

| 文件 | 覆盖模块 | 状态 |
| --- | --- | --- |
| `auth.http` | 登录、当前用户、角色列表 | 已有 |
| `health.http` | 存活检查、数据库检查 | 已有 |
| `courses.http` | 课程 CRUD、发布、关闭、容量 | 已有 |
| `trainers.http` | 讲师 CRUD | 已有 |
| `training-requests.http` | 申请提交、查询范围、主管审批、HR 备案 | 已有（2026-09-09 由 `tests/backend/` 迁入） |
| `training-requests.md` | 上述用例的准备步骤、预期结果与 REQ 对照 | 已有（同上迁入） |
| `employees.http` | 员工、部门预算、黑名单 | **待补**（组织模块 PR #11 未合并） |
| `registrations.http`、`attendance.http` | 报名、签到、缺勤、完成 | **待合并**（在 `feature/module-registration` 分支，未提 PR） |
| `results.http` | 评分、测试、证书 | **待补** |
| `demo-flow.http` | 四角色主链路端到端 | **待补** |

`tests/api/` 只放可复现的 HTTP 用例及其说明；C# 单元/契约测试一律放在 `tests/backend/`，不要把 `.http` 或测试说明写进 `tests/backend/`。

每个用例注明所需角色、前置数据、请求、预期状态码和关键响应字段。主流程和最小失败矩阵见 [测试联调与发布验收](../../document/03-质量交付/测试联调与发布验收.md)。
