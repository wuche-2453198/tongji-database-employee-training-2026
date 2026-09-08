# Oracle 数据库入口

> Note: The original README.md file, preserved; and renamed for clarity.

维护人：钮培源。物理模型、约束、账号策略和 SQL 内容以 [Oracle 数据库设计与 DDL](../../document/02-技术设计/Oracle数据库设计与DDL.md) 为准。

预期目录：

```text
migrations/
  V001__init_schema.sql
  V002__add_indexes.sql
  V003__grant_app_privileges.sql
seed/
  S001__roles_and_accounts.sql
  S002__basic_data.sql
  S003__demo_flow.sql
samples/
  S001__verification.sql
```

执行顺序严格为 `V001 -> V002 -> V003 -> S001 -> S002 -> S003 -> verification`。每次强制联调前建立 baseline，演示前建立 demo-ready；任何云端改动都必须先有对应脚本并保存执行日志。

上述脚本已提交到 `migrations/`、`seed/` 和 `samples/`，并在云库空 Schema 按顺序执行验证通过。
