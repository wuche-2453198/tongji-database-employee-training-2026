# 后端基础架构与认证模块

第 0 阶段交付详情见 `src/backend/PHASE0_DELIVERABLE.md`。

## 技术栈

- ASP.NET Core Web API，目标框架 `net8.0`
- Swagger / OpenAPI
- JWT Bearer 认证
- BCrypt 密码哈希校验
- Dapper + `Oracle.ManagedDataAccess.Core`

## 目录结构

```text
TrainingManagement.Api/
├── Controllers/
├── Services/
│   ├── Interfaces/
│   └── Implementations/
├── Repositories/
│   ├── Interfaces/
│   └── Implementations/
├── Dtos/
├── Entities/
├── Common/
├── Middlewares/
└── Templates/
```

## 本地启动

本机需要安装 .NET 8 SDK。

```powershell
cd src/backend/TrainingManagement.Api
dotnet restore
dotnet run
```

默认地址：

- API: `http://localhost:5156`
- Swagger: `http://localhost:5156/swagger`

## Oracle 连接配置

数据库连接信息以 `origin/npy/oracle` 分支的 `database/oracle/README.md` 为准。当前测试库要点：

- 公网地址：`47.100.108.150`
- Oracle 端口：`1539`，不是默认的 `1521`
- PDB 服务名：`FREEPDB1.localdomain`
- 后端运行账号：`TRAINING_APP`
- 业务表 Owner：`TRAINING_OWNER`
- 云服务器安全组只放行数据库负责人允许的后端 IP

仓库不提交真实数据库密码。开发时可以复制 `appsettings.Local.example.json` 为未提交的 `appsettings.Local.json`，或使用环境变量配置：

```json
{
  "ConnectionStrings": {
    "OracleDb": "User Id=TRAINING_APP;Password=***;Data Source=47.100.108.150:1539/FREEPDB1.localdomain"
  },
  "Oracle": {
    "CurrentSchema": "TRAINING_OWNER"
  }
}
```

也可以使用环境变量：

```powershell
$env:ConnectionStrings__OracleDb="User Id=TRAINING_APP;Password=***;Data Source=47.100.108.150:1539/FREEPDB1.localdomain"
$env:Oracle__CurrentSchema="TRAINING_OWNER"
```

后端打开 Oracle 连接后会自动执行：

```sql
ALTER SESSION SET CURRENT_SCHEMA = TRAINING_OWNER;
```

数据库健康检查接口会真实连接 Oracle 并执行 `SELECT 1 FROM DUAL`：

```text
GET /api/health/db
```

如果本地或部署服务器没有被数据库负责人加入白名单，`/api/health/db` 会失败，这是网络权限问题，不是后端代码问题。开发后端前需要向数据库负责人提供当前后端出口 IP。

如果 `/api/health/db` 成功但 Oracle 测试账号登录返回 401，优先核对种子脚本中的 `PASSWORD_HASH` 是否由对应测试密码生成。

## 认证接口

| 方法 | 路径 | 说明 |
| --- | --- | --- |
| `POST` | `/api/auth/login` | 登录，返回用户信息和 JWT |
| `POST` | `/api/auth/logout` | 退出登录，JWT 模式下返回成功即可 |
| `GET` | `/api/auth/me` | 获取当前登录用户、角色和权限 |
| `GET` | `/api/roles` | 查询角色列表，仅管理员可访问 |
| `GET` | `/api/health` | 后端存活检查 |
| `GET` | `/api/health/db` | Oracle 连接检查 |

登录参数：

```json
{
  "identifier": "admin",
  "password": "Password2026!"
}
```

`identifier` 支持员工编号、登录名、邮箱、手机号或姓名。

## Oracle 种子测试账号

`origin/npy/oracle` 分支已经将登录字段固定在 `EMPLOYEES.LOGIN_NAME` 和 `EMPLOYEES.PASSWORD_HASH`，密码使用 BCrypt 哈希保存。远程 Oracle 种子脚本提供四类测试账号：

| 登录名 | 密码 | 数据库角色代码 | 后端规范角色 |
| --- | --- | --- | --- |
| `admin` | `Password2026!` | `ADMIN` | `ADMIN` |
| `hr` | `Password2026!` | `HR` | `HR` |
| `manager` | `Password2026!` | `MANAGER` | `DEPT_MANAGER` |
| `employee` | `Password2026!` | `EMPLOYEE` | `EMPLOYEE` |

数据库中部门主管角色代码暂为 `MANAGER`，后端会统一归一化为 `DEPT_MANAGER` 写入 JWT 和接口响应。

## 本地演示账号

未配置 Oracle 或数据库连接失败时，开发环境提供本地演示账号，便于前端先联调登录态和权限菜单。

| 账号 | 密码 | 角色 |
| --- | --- | --- |
| `admin@example.com` | `123456` | `ADMIN` |
| `hr@example.com` | `123456` | `HR` |
| `manager@example.com` | `123456` | `DEPT_MANAGER` |
| `employee@example.com` | `123456` | `EMPLOYEE` |

连接 Oracle 后，登录会优先读取 `EMPLOYEES`、`USER_ROLES`、`ROLES`，并使用 BCrypt 校验 `PASSWORD_HASH`。

## 角色权限初版

| 角色代码 | 说明 | 主要权限 |
| --- | --- | --- |
| `EMPLOYEE` | 员工 | 查看课程、提交申请、报名、评分、查看证书 |
| `DEPT_MANAGER` | 部门主管 | 员工只读、课程只读、申请审批 |
| `HR` | HR | HR 备案、签到管理、评分复核、测试成绩、证书管理 |
| `ADMIN` | 管理员 | 员工、部门、黑名单、课程、角色等后台维护 |

数据库里角色代码或角色名可以使用 `EMPLOYEE`、`MANAGER`、`HR`、`ADMIN` 或中文名。后端会统一映射成稳定角色代码写入 JWT，其中 `MANAGER` 会归一化为 `DEPT_MANAGER`。

## 统一响应

成功：

```json
{
  "success": true,
  "message": "ok",
  "data": {},
  "traceId": "string"
}
```

失败：

```json
{
  "success": false,
  "message": "Invalid identifier or password.",
  "errors": [],
  "traceId": "string"
}
```

## 后续对接事项

- 若数据库负责人后续决定把 `MANAGER` 改名为 `DEPT_MANAGER`，需要同步更新种子脚本和前端权限判断。
- 生产环境应关闭本地演示账号，并使用强 JWT 签名密钥。
