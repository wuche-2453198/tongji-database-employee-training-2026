# 后端基础架构与认证第 0 阶段交付说明

维护人：后端基础架构与认证负责人

## 1. 阶段结论

第 0 阶段后端侧目标是固定技术路线、工程骨架、Oracle 连接方式、统一响应、异常处理、认证授权、核心枚举和测试账号规划。

当前结论：

- 后端使用 C# + ASP.NET Core Web API。
- 目标框架使用 .NET 8，项目路径为 `src/backend/TrainingManagement.Api`。
- Oracle 访问使用 `Oracle.ManagedDataAccess.Core` + Dapper。
- API 文档使用 Swagger / OpenAPI。
- 认证方式使用 JWT Bearer，数据库密码使用 BCrypt 哈希校验。
- 统一响应通过 `ApiResponse<T>` 和 `PagedResult<T>` 承载。
- 全局异常由 `ExceptionHandlingMiddleware` 转换为统一错误响应。

## 2. 第 0 阶段任务对应表

| 编号 | 任务 | 当前状态 | 交付物 |
| --- | --- | --- | --- |
| P0-02 | 决定数据访问方式 | DONE | Dapper + `Oracle.ManagedDataAccess.Core`，Repository 只负责参数化 SQL |
| P0-11 | 固定后端项目骨架方案 | DONE | `Controllers`、`Services`、`Repositories`、`Dtos`、`Entities`、`Common`、`Middlewares` |
| P0-12 | 固定 .NET SDK 与依赖包 | DONE | `TrainingManagement.Api.csproj`、`NuGet.config`、后端 README、BCrypt 依赖 |
| P0-13 | 设计统一响应和分页格式 | DONE | `ApiResponse<T>`、`ApiError`、`PagedResult<T>`、统一 401/403/500 响应 |
| P0-14 | 设计核心状态枚举 | DONE | `Common/Enums` 下的核心枚举 |
| P0-15 | 明确角色权限初版 | DONE | `RoleCodes`、`PermissionCodes`、README 权限矩阵 |
| P0-16 | 准备第一批测试账号规划 | DONE | 本地演示账号和远程数据库账号对接说明 |
| T04 | 创建 C# Web API 项目 | DONE | `src/backend/TrainingManagement.Api` |
| T05 | 配置 Oracle 连接和健康检查 | DONE | `/api/health/db` 已真实连接远程 Oracle 并返回成功；`/api/roles` 已验证运行账号可读业务表 |
| T06 | 登录接口和当前用户接口 | DONE | `/api/auth/login`、`/api/auth/me` |

## 3. Oracle 对接约定

数据库环境以 `origin/npy/oracle` 分支的 `database/oracle/README.md` 为准。

当前后端使用的连接信息：

```text
Host: 47.100.108.150
Port: 1539
Service: FREEPDB1.localdomain
Runtime user: TRAINING_APP
Current schema: TRAINING_OWNER
```

本地不要提交真实密码。复制示例文件后填写本地配置：

```powershell
cd src/backend/TrainingManagement.Api
Copy-Item appsettings.Local.example.json appsettings.Local.json
```

`appsettings.Local.json` 形态：

```json
{
  "ConnectionStrings": {
    "OracleDb": "User Id=TRAINING_APP;Password=YOUR_PASSWORD;Data Source=47.100.108.150:1539/FREEPDB1.localdomain"
  },
  "Oracle": {
    "CurrentSchema": "TRAINING_OWNER"
  }
}
```

后端每次打开 Oracle 连接后会自动执行：

```sql
ALTER SESSION SET CURRENT_SCHEMA = TRAINING_OWNER;
```

如果 `/api/health/db` 失败，优先检查：

- 当前后端出口 IP 是否已经加入阿里云安全组白名单。
- 端口是否使用 `1539`，不是默认 `1521`。
- 连接串服务名是否为 `FREEPDB1.localdomain`。
- `TRAINING_APP` 密码是否和数据库 README 一致。

## 4. 已实现接口

| 方法 | 路径 | 说明 | 登录要求 |
| --- | --- | --- | --- |
| GET | `/api/health` | 后端存活检查 | 不需要 |
| GET | `/api/health/db` | Oracle 连接检查 | 不需要 |
| POST | `/api/auth/login` | 登录并返回 JWT | 不需要 |
| POST | `/api/auth/logout` | 退出登录 | 需要 |
| GET | `/api/auth/me` | 当前用户、角色、权限 | 需要 |
| GET | `/api/roles` | 角色列表 | 管理员 |

## 5. 统一响应格式

成功响应：

```json
{
  "success": true,
  "message": "ok",
  "data": {},
  "traceId": "string"
}
```

失败响应：

```json
{
  "success": false,
  "message": "Permission denied.",
  "errors": [],
  "traceId": "string"
}
```

分页响应：

```json
{
  "success": true,
  "message": "ok",
  "data": {
    "items": [],
    "page": 1,
    "pageSize": 20,
    "total": 0
  },
  "traceId": "string"
}
```

## 6. 核心枚举

| 业务 | 后端枚举 | 接口建议值 |
| --- | --- | --- |
| 员工状态 | `EmployeeStatus` | `ACTIVE`, `RESIGNED` |
| 课程状态 | `CourseStatus` | `DRAFT`, `PUBLISHED`, `CLOSED` |
| 申请状态 | `TrainingRequestStatus` | `PENDING`, `DEPT_APPROVED`, `DEPT_REJECTED`, `HR_FILED` |
| 报名状态 | `RegistrationStatus` | `REGISTERED`, `SIGNED_IN`, `ABSENT`, `COMPLETED`, `CANCELED` |
| 测试类型 | `TestType` | `PRE`, `POST` |
| 是否值 | `YesNo` | `Y`, `N` |
| 黑名单状态 | `BlacklistStatus` | `ACTIVE`, `RELEASED` |

## 7. 角色权限初版

| 角色代码 | 说明 | 主要权限 |
| --- | --- | --- |
| `EMPLOYEE` | 员工 | 查看课程、提交申请、报名、评分、查看证书 |
| `DEPT_MANAGER` | 部门主管 | 员工只读、课程只读、申请审批 |
| `HR` | HR | HR 备案、签到管理、评分复核、测试成绩、证书管理 |
| `ADMIN` | 管理员 | 员工、部门、黑名单、课程、角色等后台维护 |

数据库里的角色代码或中文角色名会被后端统一映射为稳定代码：

- `员工` -> `EMPLOYEE`
- `MANAGER` -> `DEPT_MANAGER`
- `部门主管` -> `DEPT_MANAGER`
- `HR` -> `HR`
- `管理员` -> `ADMIN`

## 8. 测试账号规划

远程 Oracle 种子账号由 `origin/npy/oracle` 分支的 `database/oracle/seed/S001__roles_and_accounts.sql` 提供，登录字段为 `EMPLOYEES.LOGIN_NAME`，密码字段为 `EMPLOYEES.PASSWORD_HASH`。

| 登录名 | 密码 | 数据库角色代码 | 后端规范角色 |
| --- | --- | --- | --- |
| `admin` | `Password2026!` | `ADMIN` | `ADMIN` |
| `hr` | `Password2026!` | `HR` | `HR` |
| `manager` | `Password2026!` | `MANAGER` | `DEPT_MANAGER` |
| `employee` | `Password2026!` | `EMPLOYEE` | `EMPLOYEE` |

未配置 Oracle 或数据库连接失败时，后端提供本地演示账号用于前端联调。

| 账号 | 密码 | 角色 |
| --- | --- | --- |
| `admin@example.com` | `123456` | `ADMIN` |
| `hr@example.com` | `123456` | `HR` |
| `manager@example.com` | `123456` | `DEPT_MANAGER` |
| `employee@example.com` | `123456` | `EMPLOYEE` |

连接远程 Oracle 后，后端会优先读取 `EMPLOYEES`、`USER_ROLES`、`ROLES`。

## 9. 验收命令

本机需要可用的 .NET 8 SDK：

```powershell
dotnet --version
dotnet build src/backend/TrainingManagement.Api/TrainingManagement.Api.csproj
```

启动：

```powershell
cd src/backend/TrainingManagement.Api
dotnet run
```

Swagger：

```text
http://localhost:5156/swagger
```

HTTP 调用样例：

- `tests/api/health.http`
- `tests/api/auth.http`

## 10. 后续阻塞项

`/api/health/db` 要达到成功，需要数据库负责人放行当前后端出口 IP。本机开发时可用下面命令查询 IPv4：

```powershell
curl.exe -4 ifconfig.me
```

如果后端部署到云服务器，需要提供云服务器出口 IP，而不是本机 IP。
