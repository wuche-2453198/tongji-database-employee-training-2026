# 后端入口

维护协调人：王天宇。

第 0 阶段交付：`.NET 8` Web API 工程、Swagger、Dapper Oracle 连接、统一响应/错误/分页、JWT 认证、角色策略、`/api/health`、`/api/health/db` 和一个可复用的模块模板。

实现规范见 [技术架构与开发规范](../../document/02-技术设计/技术架构与开发规范.md)。数据库连接、Schema 和迁移顺序见 [Oracle 数据库设计与 DDL](../../document/02-技术设计/Oracle数据库设计与DDL.md)。

业务模块不得在此目录外散落 Controller、Service、Repository 或 DTO。真实连接串和 JWT 密钥必须从环境变量或不入库的本地配置读取。

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

## 配置

仓库不提交真实数据库密码和生产 JWT 签名密钥。开发时可以复制 `appsettings.Local.example.json` 为未提交的 `appsettings.Local.json`，或使用环境变量配置：

```json
{
  "ConnectionStrings": {
    "OracleDb": "User Id=TRAINING_APP;Password=***;Data Source=47.100.108.150:1539/FREEPDB1.localdomain"
  },
  "Oracle": {
    "CurrentSchema": "TRAINING_OWNER"
  },
  "Jwt": {
    "SigningKey": "replace-with-at-least-32-byte-local-signing-key"
  }
}
```

环境变量示例：

```powershell
$env:ConnectionStrings__OracleDb="User Id=TRAINING_APP;Password=***;Data Source=47.100.108.150:1539/FREEPDB1.localdomain"
$env:Oracle__CurrentSchema="TRAINING_OWNER"
$env:Jwt__SigningKey="replace-with-at-least-32-byte-runtime-signing-key"
```

非开发环境中，如果 `Jwt:SigningKey` 为空或仍为开发默认值，应用会启动失败。开发环境的本地演示账号只在 `appsettings.Development.json` 中开启；主配置默认关闭。

## Oracle 连接配置

数据库连接信息以 `database/oracle/README.md` 为准。当前测试库要点：

- Oracle 端口：`1539`，不是默认的 `1521`
- PDB 服务名：`FREEPDB1.localdomain`
- 后端运行账号：`TRAINING_APP`
- 业务表 Owner：`TRAINING_OWNER`
- 云服务器安全组只放行数据库负责人允许的后端 IP

后端打开 Oracle 连接后会自动执行：

```sql
ALTER SESSION SET CURRENT_SCHEMA = TRAINING_OWNER;
```

数据库健康检查接口会真实连接 Oracle 并执行 `SELECT 1 FROM DUAL`：

```text
GET /api/health/db
```

如果本地或部署服务器没有被数据库负责人加入白名单，`/api/health/db` 会失败，这是网络权限问题，不是后端代码问题。

## 认证接口

| 方法 | 路径 | 说明 |
| --- | --- | --- |
| `POST` | `/api/auth/login` | 登录，返回用户信息和 JWT |
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

数据库登录字段为 `EMPLOYEES.LOGIN_NAME` 和 `EMPLOYEES.PASSWORD_HASH`，密码使用 BCrypt 哈希保存。

| 登录名 | 密码 | 数据库角色代码 | 后端规范角色 |
| --- | --- | --- | --- |
| `admin` | `Password2026!` | `ADMIN` | `ADMIN` |
| `hr` | `Password2026!` | `HR` | `HR` |
| `manager` | `Password2026!` | `MANAGER` | `DEPT_MANAGER` |
| `employee` | `Password2026!` | `EMPLOYEE` | `EMPLOYEE` |

数据库中部门主管角色代码为 `MANAGER`，后端会统一归一化为 `DEPT_MANAGER` 写入 JWT 和接口响应。

## 本地演示账号

本地演示账号仅用于开发环境或显式开启配置时的联调。主配置默认关闭，避免数据库正常但查不到用户时误登录演示账号。

| 账号 | 密码 | 角色 |
| --- | --- | --- |
| `admin@example.com` | `123456` | `ADMIN` |
| `hr@example.com` | `123456` | `HR` |
| `manager@example.com` | `123456` | `DEPT_MANAGER` |
| `employee@example.com` | `123456` | `EMPLOYEE` |

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
