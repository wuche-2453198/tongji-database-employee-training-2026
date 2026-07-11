# 企业内部培训管理系统

同济大学数据库课程设计项目，目标是在 15 个工作日内交付一个可部署、可演示、可恢复数据的企业内部培训管理系统。

当前状态：第 0 阶段准备中。此阶段只冻结技术、数据、接口和协作边界，不将未验证的业务功能合入集成线。

## 文档入口

- [文档中心](document/README.md)：按项目管理、技术设计、质量交付分类的唯一入口。
- [项目执行与人员分工](document/01-项目管理/项目执行与人员分工.md)：十人职责、依赖、排程和第 0 阶段清单。
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
document/                    项目执行资料
```

## 协作规则

- `main` 只保留可演示版本，`develop` 作为日常集成线，个人工作使用 `feature/{module}-{topic}`。
- 业务开发必须在 DDL、状态枚举、角色权限和 OpenAPI v0.1 评审通过后开始。
- 所有数据库变更必须提交迁移脚本；前端不得直连 Oracle。
- 不提交真实密码、私钥、Token、连接串、构建产物或个人本地配置。
- PR 合并前执行 `./scripts/verify-project-structure.sh`，并满足对应模块的测试和审查要求。

具体启动顺序以 [第 0 阶段任务看板](document/01-项目管理/第0阶段任务看板.md) 为准。
