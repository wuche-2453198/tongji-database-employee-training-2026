# 后端入口

维护协调人：王天宇。

第 0 阶段交付：`.NET 8` Web API 工程、Swagger、Dapper Oracle 连接、统一响应/错误/分页、JWT 认证、角色策略、`/api/health`、`/api/health/db` 和一个可复用的模块模板。

实现规范见 [技术架构与开发规范](../../document/02-技术设计/技术架构与开发规范.md)。数据库连接、Schema 和迁移顺序见 [Oracle 数据库设计与 DDL](../../document/02-技术设计/Oracle数据库设计与DDL.md)。

业务模块不得在此目录外散落 Controller、Service、Repository 或 DTO。真实连接串和 JWT 密钥必须从环境变量或不入库的本地配置读取。
