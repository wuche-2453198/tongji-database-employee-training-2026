# 企业内部培训管理系统前端

本目录是企业内部培训管理系统唯一的 Vue 3 单页应用。整合分支以 Figma 对齐的19个P0页面、公共组件、布局和交互为界面基线，并接入当前后端的 JWT 认证契约及课程、培训申请 HTTP 适配边界。

## 环境要求

- Node.js `24.10.0`（以 `.nvmrc` 和 `.node-version` 为准）
- pnpm `11.19.0`（以 `package.json#packageManager` 为准）

启用 Corepack 后可准备指定 pnpm 版本：

```sh
corepack enable
corepack prepare pnpm@11.19.0 --activate
```

Windows普通终端若执行`corepack enable`出现`EPERM`，说明无权向`C:\Program Files\nodejs`写入命令入口。可以用管理员终端执行一次，或不创建全局入口，直接把后续命令中的`pnpm`替换为`corepack pnpm`，例如`corepack pnpm dev`。

## 安装与启动

```sh
pnpm install --frozen-lockfile
pnpm dev
```

开发服务器默认使用 `.env.mock`，地址为 `http://localhost:5173`。

M2 公共组件展示页位于 `http://localhost:5173/components`，组件使用边界见 [`src/components/README.md`](src/components/README.md)。

M5 API 边界验收页位于 `http://localhost:5173/api-boundary`，可切换正常、空、500、403、404、409 和写操作结果未知场景。

## API 与服务边界

页面和 Store 只允许调用 `src/services/` 中的领域服务接口：

- `src/types/` 保存页面可消费的领域模型与统一 `UiError`，不暴露后端字段名。
- `src/api/transport.ts` 保存等待 OpenAPI 冻结的暂定传输类型。
- `src/api/mappers/` 负责 DTO 到领域模型的映射及未知枚举兜底。
- `src/api/client.ts` 集中 Base URL、10 秒超时、取消和查询最多一次有限重试。
- `src/mocks/` 与 `src/services/http/` 实现相同服务接口，由 `VITE_USE_MOCK` 切换。

写操作不自动重试；网络中断或超时映射为 `result-unknown`，页面必须引导用户查询最终状态。相同写操作可使用 `SingleFlightController` 共享在途 Promise，列表查询可使用 `LatestRequestController` 取消旧请求。

真实模式已按当前后端源码接入 `POST /api/auth/login`、`GET /api/auth/me`、本地 JWT 恢复、`Authorization: Bearer` 请求头以及并发401统一清理和登录回跳。角色使用后端 `ADMIN`、`HR`、`DEPT_MANAGER`、`EMPLOYEE`，权限码使用 `PermissionCodes.cs` 的点号格式。

课程和培训申请已建立真实 HTTP 适配器；当前整合基线尚无这两个模块的后端 Controller，因此只能视为“按业务分支记录完成适配、等待 Swagger/真实环境验证”。报名、签到、证书、评分和测试继续以 Mock 模式验收；其 HTTP 适配器对未冻结能力明确返回阻塞错误，不伪造联调结果。

## Mock 登录

Mock 模式提供四类固定演示账号，密码均为 `Demo@123`：

| 角色     | 账号            |
| -------- | --------------- |
| 员工     | `employee.demo` |
| 部门主管 | `manager.demo`  |
| HR       | `hr.demo`       |
| 管理员   | `admin.demo`    |

账号与密码仅由 `src/mocks/`中的开发态认证服务加载，不在登录页面展示。Pinia Store 通过`src/services/auth.ts`消费领域化当前用户；非 Mock 构建按后端 `identifier/password` 契约使用真实 JWT 认证适配器，构建产物不得包含上述 Mock 账号或密码。

## 常用命令

| 命令                     | 用途                                     |
| ------------------------ | ---------------------------------------- |
| `pnpm dev`               | 使用 Mock 环境启动开发服务器             |
| `pnpm dev:local`         | 使用本地后端环境启动开发服务器           |
| `pnpm dev:integration`   | 使用联调环境启动开发服务器               |
| `pnpm type-check`        | 执行 Vue 与 TypeScript 严格类型检查      |
| `pnpm lint`              | 执行 ESLint 检查，不自动改写文件         |
| `pnpm format`            | 使用 Prettier 格式化工程文件             |
| `pnpm format:check`      | 检查格式但不改写文件                     |
| `pnpm test`              | 运行 Vitest 单元/组件测试                |
| `pnpm test:e2e`          | 运行 Playwright E2E 测试                 |
| `pnpm build`             | 类型检查并生成生产构建                   |
| `pnpm build:integration` | 使用联调配置生成构建                     |
| `pnpm check`             | 顺序执行类型、Lint、格式、单测和构建检查 |

本地 E2E 默认复用已安装的 Chrome。CI 或没有 Chrome 的环境需先安装 Playwright Chromium：

```sh
pnpm exec playwright install chromium
pnpm test:e2e --project=chromium
```

M6总验收建议先生成Mock构建，再使用独立端口预览生产产物，避免复用开发服务器的热更新缓存：

```powershell
pnpm build
$env:PLAYWRIGHT_PORT = '4174'
$env:PLAYWRIGHT_USE_PREVIEW = 'true'
pnpm test:e2e --project=chromium
Remove-Item Env:PLAYWRIGHT_PORT
Remove-Item Env:PLAYWRIGHT_USE_PREVIEW
```

## 环境变量

Mock配置由`.env.mock`提供。本地或联调时，分别复制`.env.local.example`为`.env.local`、复制`.env.integration.example`为`.env.integration.local`，再填写实际地址；这两个目标文件由Git忽略，不得提交真实地址或凭证。

```powershell
Copy-Item .env.local.example .env.local
Copy-Item .env.integration.example .env.integration.local
```

环境变量如下：

| 变量                      | 含义                                |
| ------------------------- | ----------------------------------- |
| `VITE_APP_ENV`            | `mock`、`local` 或 `integration`    |
| `VITE_API_BASE_URL`       | 后端 API 基础地址；非 Mock 环境必填 |
| `VITE_USE_MOCK`           | 是否使用 Mock 服务                  |
| `VITE_REQUEST_TIMEOUT_MS` | 请求超时毫秒数，必须为正整数        |

配置统一由 `src/config/env.ts` 解析。页面不得拼接 API Base URL，也不得直接读取原始 Mock 对象。真实密钥、Cookie、Token 和真实测试账号不写入仓库。

## 目录边界

- `src/api/`：HTTP 客户端、传输类型和 DTO 映射。
- `src/components/`：通用组件与业务摘要组件。
- `src/config/`：环境配置解析与校验。
- `src/layouts/`：认证布局与主布局。
- `src/mocks/`：仅开发环境使用的 Mock 服务与场景。
- `src/router/`：路由表、元信息和守卫。
- `src/services/`：页面消费的前端服务接口。
- `src/stores/`：Pinia 全局状态。
- `src/styles/`：设计令牌、Element Plus 主题与全局样式。
- `src/types/`：前端领域模型和自动导入类型。
- `src/views/`：页面级组件。
- `tests/unit/`：Vitest 单元和组件测试。
- `tests/e2e/`：Playwright 端到端测试。

技术决策见 [`document/frontend-development/02-M0技术决策记录.md`](../../../document/frontend-development/02-M0技术决策记录.md)。

M6验收结果、业务页面接入规范与后端阻塞项见 [`document/frontend-development/03-M6公共基础能力验收与交接.md`](../../../document/frontend-development/03-M6公共基础能力验收与交接.md)。

本次分支整合的文件取舍、页面来源和验证边界见 [`document/frontend-development/05-前端分支整合说明.md`](../../../document/frontend-development/05-前端分支整合说明.md)；页面到真实/Mock接口的映射见 [`document/frontend-development/06-页面接口映射与联调阻塞.md`](../../../document/frontend-development/06-页面接口映射与联调阻塞.md)。
