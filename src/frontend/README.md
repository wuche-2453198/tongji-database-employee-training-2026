# 前端入口

维护协调人：李司翰；业务页面负责人：彭浩。

交付内容：Vue 3 + TypeScript + Vite 工程，覆盖登录、仪表盘、课程、培训申请与审批、HR 备案、报名签到、评分、测试、证书等 P0 业务页面，以及公共组件（数据表格、分页、状态标签、确认弹窗、页面状态等）、Axios API 客户端、路由守卫与权限菜单。工程代码已合入 `develop`。

## 运行方式

```powershell
corepack pnpm install --frozen-lockfile
corepack pnpm dev              # Mock 模式（默认，无需后端）
corepack pnpm dev:local        # 对接本地后端（先复制 .env.local.example 为 .env.local）
corepack pnpm dev:integration  # 对接集成环境
```

前端只调用 HTTP API，不直接访问 Oracle。业务页面必须使用公共 API 客户端、类型和状态枚举映射；HTTP 适配器与 Mock 实现遵循同一套领域 Service 接口，通过 `VITE_USE_MOCK` / `VITE_APP_ENV` 切换。进入联调后必须与后端当前实现保持一致。详细规范见 [技术架构与开发规范](../../document/02-技术设计/技术架构与开发规范.md)。
