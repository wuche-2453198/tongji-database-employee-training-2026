# 员工培训管理系统

同济数据库课程设计项目：基于 B/S 架构的员工培训管理系统。系统面向企业内部培训流程，覆盖员工与部门基础信息、培训课程、培训申请审批、报名签到、测试评分、证书与培训记录等业务。

## 项目状态

当前处于第 0 阶段，重点是统一技术路线、目录结构、数据库连接方式、后端工程骨架、认证模块和接口规范。

已完成的后端基础内容：

- ASP.NET Core Web API 工程骨架
- Swagger / OpenAPI 文档入口
- JWT Bearer 认证基础设施
- 统一响应、分页、异常处理和核心状态枚举
- Oracle 连接工厂与数据库健康检查
- 登录、当前用户、角色查询接口
- 后端 README、阶段交付说明和 HTTP 测试脚本

后端第 0 阶段详细交付见 [src/backend/PHASE0_DELIVERABLE.md](src/backend/PHASE0_DELIVERABLE.md)。

## 技术栈

| 层级 | 技术 |
| --- | --- |
| 后端 | C#、ASP.NET Core Web API、.NET 8 |
| 数据库 | Oracle Database Free 23ai |
| 数据访问 | Dapper、Oracle.ManagedDataAccess.Core |
| 接口文档 | Swagger / OpenAPI |
| 认证 | JWT Bearer |
| 前端 | 待定，统一通过 HTTP API 调用后端 |

## 目录结构

```text
tongji-database-employee-training-2026/
├── document/                  # 项目分工、开发规范、阶段目标、目录结构说明
├── src/
│   └── backend/               # 后端源码与后端模块说明
├── tests/
│   └── api/                   # HTTP 接口测试脚本
├── NuGet.config               # .NET 包源配置
└── README.md                  # 项目总入口
```

完整目录规划见 [document/仓库目录结构.md](document/仓库目录结构.md)。

## 后端快速启动

本机需要安装 .NET 8 SDK。仓库中已提供后端项目：

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

如果使用仓库内置 SDK，可用本机路径执行：

```powershell
D:\code\.dotnet-sdk-8\dotnet.exe run --project src/backend/TrainingManagement.Api/TrainingManagement.Api.csproj
```

## Oracle 连接说明

数据库连接信息以数据库负责人维护的 `database/oracle/README.md` 为准。后端当前使用 Oracle PDB `FREEPDB1.localdomain`，端口是 `1539`，不是 Oracle 默认端口 `1521`。

仓库不提交真实数据库密码。开发时复制示例配置：

```powershell
cd src/backend/TrainingManagement.Api
copy appsettings.Local.example.json appsettings.Local.json
```

然后在 `appsettings.Local.json` 中填写本地数据库连接串。该文件已被 `.gitignore` 忽略，不应提交。

后端每次打开 Oracle 连接后会自动执行：

```sql
ALTER SESSION SET CURRENT_SCHEMA = TRAINING_OWNER;
```

如果 `/api/health/db` 返回连接失败，优先检查：

- 当前机器或后端服务器出口 IP 是否已加入数据库白名单
- Oracle 端口是否使用 `1539`
- PDB 服务名是否为 `FREEPDB1.localdomain`
- 本地配置中是否填写了正确密码

## 认证接口

| 方法 | 路径 | 说明 |
| --- | --- | --- |
| `POST` | `/api/auth/login` | 登录并返回 JWT |
| `POST` | `/api/auth/logout` | 退出登录 |
| `GET` | `/api/auth/me` | 获取当前登录用户、角色和权限 |
| `GET` | `/api/roles` | 获取角色列表，仅管理员可访问 |
| `GET` | `/api/health` | 后端存活检查 |
| `GET` | `/api/health/db` | Oracle 连接检查 |

开发环境演示账号：

| 账号 | 密码 | 角色 |
| --- | --- | --- |
| `admin@example.com` | `123456` | 管理员 |
| `hr@example.com` | `123456` | HR |
| `manager@example.com` | `123456` | 部门主管 |
| `employee@example.com` | `123456` | 员工 |

更多后端接口和响应格式见 [src/backend/README.md](src/backend/README.md)。

## 接口测试

HTTP 测试脚本位于 [tests/api](tests/api)：

- [tests/api/health.http](tests/api/health.http)
- [tests/api/auth.http](tests/api/auth.http)

可以使用 JetBrains Rider、Visual Studio Code REST Client 插件或其他 HTTP 客户端执行。

## 协作规范入口

| 文档 | 内容 |
| --- | --- |
| [document/项目分工计划.md](document/项目分工计划.md) | 成员职责与模块分工 |
| [document/项目开发规范.md](document/项目开发规范.md) | 分支、提交、接口、命名和安全规范 |
| [document/第0阶段详细目标.md](document/第0阶段详细目标.md) | 第 0 阶段任务与验收标准 |
| [document/仓库目录结构.md](document/仓库目录结构.md) | 仓库目录边界和 README 维护方式 |

## 提交注意事项

不要提交以下内容：

- 真实数据库密码、云服务器账号、SSH 私钥
- `appsettings.Local.json`
- `bin/`、`obj/`、`dist/`、`node_modules/`
- IDE 缓存、临时日志和个人数据库客户端配置

提交前建议至少执行：

```powershell
dotnet build src/backend/TrainingManagement.Api/TrainingManagement.Api.csproj
git status --short --ignored
```
