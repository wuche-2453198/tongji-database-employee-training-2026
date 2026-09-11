# 后端入口

维护协调人：王天宇。

交付内容：`.NET 8` Web API 工程、Swagger、Dapper Oracle 连接、统一响应/错误/分页、JWT 认证、角色策略、`/api/health`、`/api/health/db` 和一个可复用的模块模板。

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

## 架构总览

后端采用分层结构。一条典型请求按下面的方向流动：

```text
HTTP 请求
  -> 中间件（异常处理、跨域、JWT 认证、角色授权）
  -> Controller（接收和校验输入，选择 HTTP 状态码）
  -> Service（业务规则、状态流转、数据范围权限）
  -> Repository（Dapper 参数化 SQL）
  -> OracleConnectionFactory（打开连接并设置 CURRENT_SCHEMA）
  -> Oracle Database

Oracle 查询结果
  -> Entity（承接数据库记录）
  -> DTO（对外传输的数据）
  -> ApiResponse<T>（统一响应和 TraceId）
  -> HTTP 响应
```

各目录的职责如下：

| 目录 | 主要职责 | 不应该承担的职责 |
| --- | --- | --- |
| `Controllers` | 路由、授权特性、参数接收、调用 Service、返回响应 | SQL 和复杂业务判断 |
| `Services` | 业务校验、状态机、数据范围和流程编排 | 拼接 HTTP 响应、直接依赖页面 |
| `Repositories` | Oracle 查询和持久化、分页、事务相关数据操作 | 决定用户是否有业务权限 |
| `Dtos` | 定义请求和响应结构、基础输入校验 | 数据库连接和业务流程 |
| `Entities` | 映射数据库查询结果和领域数据 | 直接作为公共接口响应 |
| `Common` | 统一响应、异常、配置、角色权限和公共扩展 | 某个业务模块独有的流程 |
| `Middlewares` | 处理跨控制器的 HTTP 请求行为 | 某个接口专属的业务逻辑 |

依赖方向保持为 `Controller -> Service -> Repository -> Oracle`。每层依赖接口，具体实现集中在 `Program.cs` 注册，控制器不直接创建服务或数据库连接。

## 一条请求怎么看

以登录 `POST /api/auth/login` 为例：

1. `AuthController.Login` 接收 `LoginRequest`，ASP.NET Core 自动执行 DTO 校验。
2. `AuthService.LoginAsync` 查找用户、用 BCrypt 校验密码、加载角色与权限。
3. `OracleAuthRepository` 使用 Dapper 参数化 SQL 查询员工和角色。
4. `OracleConnectionFactory` 使用 `TRAINING_APP` 打开连接，并切换到 `TRAINING_OWNER` Schema。
5. `JwtTokenService` 为认证成功的用户签发 JWT。
6. `ApiControllerBase.OkResponse` 将结果包装为 `ApiResponse<LoginResponse>`。
7. 过程中抛出的业务异常由 `ExceptionHandlingMiddleware` 统一转换成 4xx 响应；未知异常转换成 500，并通过 TraceId 关联日志。

其他业务接口也沿用同一条主链路。阅读一个模块时，可以从 Controller 的路由方法开始，依次使用“转到定义”进入 Service 接口、Service 实现、Repository 接口和 Oracle Repository。

## 推荐阅读顺序

1. `Program.cs`：了解配置来源、JWT、授权策略、依赖注入和中间件顺序。
2. `Controllers/ApiControllerBase.cs` 与 `Common/Responses/ApiResponse.cs`：了解统一成功响应。
3. `Middlewares/ExceptionHandlingMiddleware.cs` 与 `Common/Exceptions/`：了解统一错误响应。
4. `Repositories/Implementations/OracleConnectionFactory.cs`：了解运行账号、连接生命周期和 Schema 切换。
5. `AuthController -> AuthService -> OracleAuthRepository -> JwtTokenService`：完整阅读认证链路。
6. `TrainingRequestsController -> TrainingRequestService -> OracleTrainingRequestRepository`：阅读包含状态机、数据范围和并发保护的业务参考模块。
7. `Templates/BackendModuleTemplate.md`：检查其他模块是否遵守相同分层规则。

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

## 核心枚举

| 业务 | 后端枚举 | 取值 |
| --- | --- | --- |
| 员工状态 | `EmployeeStatus` | `ACTIVE`、`RESIGNED` |
| 课程状态 | `CourseStatus` | `DRAFT`、`PUBLISHED`、`CLOSED` |
| 申请状态 | `TrainingRequestStatus` | `PENDING`、`DEPT_APPROVED`、`DEPT_REJECTED`、`HR_FILED` |
| 报名状态 | `RegistrationStatus` | `REGISTERED`、`SIGNED_IN`、`ABSENT`、`COMPLETED`、`CANCELED` |
| 测试类型 | `TestType` | `PRE`、`POST` |
| 是否值 | `YesNo` | `Y`、`N` |
| 黑名单状态 | `BlacklistStatus` | `ACTIVE`、`RELEASED` |

数据库和接口统一存英文枚举，中文展示由前端集中映射。数据库中的角色代码和中文角色名由后端归一化为稳定代码：`员工` -> `EMPLOYEE`，`MANAGER`/`部门主管` -> `DEPT_MANAGER`，`HR` -> `HR`，`管理员` -> `ADMIN`。

## 角色与权限

| 角色代码 | 说明 | 主要权限 |
| --- | --- | --- |
| `EMPLOYEE` | 员工 | 查看课程、提交申请、报名、评分、查看证书 |
| `DEPT_MANAGER` | 部门主管 | 员工只读、课程只读、本部门申请审批 |
| `HR` | HR | HR 备案、签到管理、评分复核、测试成绩、证书管理 |
| `ADMIN` | 管理员 | 员工、部门、黑名单、课程等后台维护 |

权限最终由后端的策略和授权特性裁决，前端菜单只做展示控制。
