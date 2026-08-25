# 企业内部培训管理系统

同济大学数据库课程设计项目，目标是交付一个可部署、可演示、可恢复数据的企业内部培训管理系统。系统覆盖员工组织、课程培训、申请审批、报名签到、成果评估和证书记录等内部培训流程。

当前状态：后端认证、讲师与课程、培训申请审批、成果评估、组织基础、报名签到六个模块已合入 `develop`，可通过 Swagger 查看；前端工程在独立分支开发，尚未合入。技术、数据、状态、权限和核心业务决策已通过；接口路径、DTO 和页面映射为推荐基线，最终以运行后端后的 Swagger/OpenAPI 为准。

## 文档入口

- [文档中心](document/README.md)：按项目管理、技术设计、质量交付分类的唯一入口。
- [项目执行与人员分工](document/01-项目管理/项目执行与人员分工.md)：十人职责、模块边界、依赖顺序和开发排程。
- [决策日志](document/01-项目管理/决策日志.md)：已通过的技术、数据、流程与权限决策。
- [技术架构与开发规范](document/02-技术设计/技术架构与开发规范.md)：技术栈、API、代码、前端、Git 和安全规则。
- [Oracle 数据库设计与 DDL](document/02-技术设计/Oracle数据库设计与DDL.md)：13 张表、迁移顺序、约束和种子规范。
- [测试联调与发布验收](document/03-质量交付/测试联调与发布验收.md)：模块 DoD、联调、缺陷和发布门禁。

## 仓库结构

```text
src/backend/                 ASP.NET Core API（王天宇协调）
src/frontend/                Vue 3 前端（李司翰协调）
database/oracle/             Oracle 迁移、种子、验证（钮培源）
tests/backend/               后端测试
tests/api/                   HTTP 主流程和失败用例（彭浩协调）
scripts/dev/                 本地开发辅助脚本
scripts/deploy/              部署和验证辅助脚本
tools/postman/               Postman/Apifox 导出文件
document/                    项目说明、规范与验收文档
```

## 技术栈

| 层级 | 技术 |
| --- | --- |
| 后端 | C#、ASP.NET Core Web API、.NET 8 |
| 数据库 | Oracle Database Free 23ai |
| 数据访问 | Dapper、Oracle.ManagedDataAccess.Core |
| 接口文档 | Swagger / OpenAPI |
| 认证 | JWT Bearer、BCrypt 密码哈希 |
| 前端 | Vue 3，统一通过 HTTP API 调用后端 |

## 后端快速启动

本机需要安装 .NET 8 SDK。后端项目位于 `src/backend/TrainingManagement.Api`。

```powershell
cd src/backend/TrainingManagement.Api
dotnet restore
dotnet run
```

默认访问地址：

- API: `http://localhost:5156`
- Swagger: `http://localhost:5156/swagger`
- 存活检查：`GET http://localhost:5156/api/health`
- 数据库检查：`GET http://localhost:5156/api/health/db`

## 前端快速启动

仓库只保留一套前端应用，根目录为 `src/frontend/`，使用 Node.js 24.10、pnpm 11、Vue 3、TypeScript、Vite、Pinia、Element Plus、Vitest 和 Playwright。

```powershell
cd src/frontend
corepack pnpm install --frozen-lockfile
corepack pnpm dev
```

默认启动 Mock 模式，可验收全部17个P0页面。连接本地后端时，复制 `.env.local.example` 为未跟踪的 `.env.local`，再执行 `corepack pnpm dev:local`。

如果 `dotnet --version` 找不到 .NET 8 SDK，请先安装 .NET 8 SDK，或按本机实际 SDK 路径执行 `dotnet.exe`。

## Oracle 连接说明

数据库连接信息以 [database/oracle/README.md](database/oracle/README.md) 为准。当前测试库要点：

- Oracle 端口：`1539`，不是默认的 `1521`
- PDB 服务名：`FREEPDB1.localdomain`
- 后端运行账号：`TRAINING_APP`
- 业务表 Owner：`TRAINING_OWNER`

仓库不提交真实数据库密码。开发时复制 `src/backend/TrainingManagement.Api/appsettings.Local.example.json` 为未提交的 `appsettings.Local.json`，或使用环境变量配置连接串和 JWT 签名密钥。

后端每次打开 Oracle 连接后会自动执行：

```sql
ALTER SESSION SET CURRENT_SCHEMA = TRAINING_OWNER;
```

## 认证接口

| 方法 | 路径 | 说明 |
| --- | --- | --- |
| `POST` | `/api/auth/login` | 登录并返回 JWT |
| `GET` | `/api/auth/me` | 获取当前登录用户、角色和权限 |
| `GET` | `/api/roles` | 获取角色列表，仅管理员可访问 |
| `GET` | `/api/health` | 后端存活检查 |
| `GET` | `/api/health/db` | Oracle 连接检查 |

## 报名签到接口

| 方法 | 路径 | 角色 | 说明 |
| --- | --- | --- | --- |
| `GET` | `/api/registrations/my` | 员工 | 当前员工的报名分页列表 |
| `GET` | `/api/registrations` | HR/管理员 | 全量报名查询（员工、部门、课程、状态、签到状态、培训日期筛选） |
| `GET` | `/api/registrations/{id}` | 本人或 HR/管理员 | 报名详情、签到摘要和可执行操作资格 |
| `GET` | `/api/registrations/summary` | HR/管理员 | 按状态统计报名数量，传 `courseId` 时返回剩余名额 |
| `POST` | `/api/registrations` | 员工 | 报名已发布课程（校验 HR 备案、员工、黑名单、课程状态/时间、容量和防重） |
| `PATCH` | `/api/registrations/{id}/cancel` | 本人或 HR/管理员 | 课程开始前取消报名 |
| `PATCH` | `/api/registrations/{id}/absent` | HR/管理员 | 课程结束后将未签到报名标记缺勤 |
| `PATCH` | `/api/registrations/{id}/complete` | HR/管理员 | 课程结束后完成已签到培训 |
| `POST` | `/api/registrations/{id}/signin` | HR/管理员 | 正常签到（SCAN，时间取服务端） |
| `POST` | `/api/attendance/manual` | HR/管理员 | 补签（MANUAL，备注必填，可指定签到时间） |

报名状态枚举：`REGISTERED`、`SIGNED_IN`、`ABSENT`、`COMPLETED`、`CANCELED`；签到类型：`SCAN`、`MANUAL`。迟到扣减规则：实际学时 = 课程学时 - 按迟到分钟等比例折算并四舍五入到 0.5 小时的扣减（上限为课程学时）。


Oracle 种子测试账号：

| 登录名 | 密码 | 角色 |
| --- | --- | --- |
| `admin` | `Password2026!` | `ADMIN` |
| `hr` | `Password2026!` | `HR` |
| `manager` | `Password2026!` | `DEPT_MANAGER` |
| `employee` | `Password2026!` | `EMPLOYEE` |

数据库中部门主管角色代码为 `MANAGER`，后端统一归一化为 `DEPT_MANAGER` 写入 JWT 和接口响应。

开发环境可显式开启本地演示账号，用于 Oracle 不可用时的登录页联调；默认生产配置关闭本地演示账号。

## 业务接口与实现状态

已合入 `develop`：

| 模块 | 接口前缀 | 说明 |
| --- | --- | --- |
| 认证与基础 | `/api/auth`、`/api/roles`、`/api/health` | 登录、当前用户、角色列表、健康检查 |
| 讲师与课程 | `/api/trainers`、`/api/courses` | 讲师与课程 CRUD、发布、关闭、容量摘要；发布需与部门预算占用在同一事务内完成 |
| 培训申请审批 | `/api/training-requests` | 提交、部门审批、HR 备案、状态查询 |
| 成果评估 | `/api/ratings`、`/api/tests`、`/api/certificates` | 评分、训前/训后测试、证书生成与查询 |
| 组织基础 | `/api/employees`、`/api/department-trainings`、`/api/blacklists` | 员工、部门培训预算、黑名单；读取限 HR/管理员，写操作限管理员 |
| 报名签到 | `/api/registrations`、`/api/attendance` | 报名、签到、缺勤、完成；员工仅本人，HR/管理员全量 |

尚未合入 `develop`：

| 模块 | 接口前缀 | 说明 |
| --- | --- | --- |
| 前端 | — | Vue 3 单页应用、公共组件与业务页面 |

字段、枚举和状态码以运行后端后的 Swagger/OpenAPI 为准；测试入口见 `tests/backend/`（C# 回归）和 `tests/api/`（HTTP 用例）。

## 协作规则

- `main` 只保留可演示版本，`develop` 作为日常集成线，个人工作使用 `feature/{module}-{topic}`。
- 各模块可根据已通过的 DDL、状态枚举、角色权限和业务决策立即开发；规划中的 API/DTO 为建议，最终以验收时的 Swagger/OpenAPI、测试和文档为准。
- 所有数据库变更必须提交迁移脚本；前端不得直连 Oracle。
- 不提交真实密码、私钥、Token、连接串、构建产物或个人本地配置。
- PR 合并前执行 `./scripts/verify-project-structure.sh`，并满足对应模块的测试要求。

模块边界与开发顺序见 [项目执行与人员分工](document/01-项目管理/项目执行与人员分工.md)，联调与验收顺序见 [测试联调与发布验收](document/03-质量交付/测试联调与发布验收.md)。
