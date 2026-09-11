# Oracle Database README

This directory manages the deployment, lifecycle initialization, and seed data for the 13 core business tables.
Maintained by 2350439 钮培源

## 1. Environment Configurations
* **Cloud Platform:** AliCloud ECS
* **Operating System:** Alibaba Cloud Linux 4
* **Database Engine:** Oracle Database Free 23ai (Character Set: `AL32UTF8`)
* **Timezone Standard:** `Asia/Shanghai`
* **Dynamic Connection Port:** **`1539`** (Whitelisted via AliCloud Security Groups strictly for backend app IPs)
> Note: Not the default 1521. Dynamically assigned by the setup assistant.
* **Global Container Database (CDB):** `FREE`
* **Pluggable Database (PDB):** **`FREEPDB1`** (Target for all application data)
* **CPU & Memory**: `2 vCPUs 4 GiB`
* **Public IP （公网IP）**: `47.100.108.150`
* **Primary Private IP Address （主私网IP）** : `172.28.175.23`

---

## 2. Database Accounts & Test Credentials

| Account Name | Password | Role / Intent |
| :--- | :--- | :--- |
| **`TRAINING_OWNER`** | 见部署环境配置（不写入仓库） | Schema Owner. Used exclusively by migrations to manage structures, indexes, and grants. |
| **`TRAINING_APP`** | 见部署环境配置（不写入仓库） | Application User. Used securely by the C# Web API for runtime DML operations. |

> ⚠️ **凭据不得写入仓库。** 按项目规则（`document/README.md` 使用规则第 5 条、`document/02-技术设计/技术架构与开发规范.md` §9），
> 真实密码只通过环境变量或未跟踪的本地配置提供：后端使用 `appsettings.Local.json`（已 gitignore）或
> `ConnectionStrings__OracleDb` 环境变量，数据库账号密码向数据库负责人索取。仓库中只保留占位符示例。

Example command to run（口令由环境变量传入，避免进入 shell 历史）：
```
sqlplus TRAINING_OWNER/"$TRAINING_OWNER_PASSWORD"@//<host>:1539/FREEPDB1.localdomain
```



> ⚠️ **Crucial Runtime Rule:** Upon opening a connection instance, the C# application data access layer must immediately execute the following session context command:
> ```sql
> ALTER SESSION SET CURRENT_SCHEMA = TRAINING_OWNER;
> ```
> This maps all un-prefixed database queries cleanly to the business tables without requiring individual schema prefixes.

---

## 3. Repository Directory Tree
The database engine workspace is strictly structured as follows:

```
database/oracle/
├── README.md               # This connection and environment guide
├── migrations/             # Version-controlled structural DDL and scripts
│   ├── V001__init_schema.sql
│   ├── V002__add_indexes.sql
│   ├── V003__grant_app_privileges.sql
│   └── V004__blacklist_operator.sql
├── seed/                   # Data initialization environments
│   ├── S001__roles_and_accounts.sql
│   ├── S002__basic_data.sql
│   └── S003__demo_flow.sql
└── samples/                # Sanity checking and validation operations
│   └── S001__verification.sql
└── utility                 # Utility purposes
    └── R001__reset_seed_data.sql
```


## 4. Dev Logs

### Jul 14 

> Migrations (V001 - V003) loaded, ECS & oracle fundamentals configured.

#### Outcome

* `V001`~`V003` executed successfully on the cloud instance; database setup task (P0-08, P0-11) completed.
* Decisions D-004, D-005 and D-006 accepted (Oracle version/charset, identity primary keys, `TRAINING_OWNER` / `TRAINING_APP` split).


### Jul 17

> Seeds (S001 - S003) planted, correctness verified with test commands and the verification query.

#### Outcome

* Seed data (`S001`~`S003`) executed and verified with test commands and the verification query; database baseline completed.


### Jul 22

> Applied fixes as per instructed. 

> Added comments to enhance readability. 

> Added utility module to store utility files. 

#### Critical Decision Gaps:

* Role code left as `MANAGER`. If the backend uses `DEPT_MANAGER`, S001 and S002's binding would need to be changed together.

* `rdmgr` added as a new account rather than repurposing an existing one.

* `EMPLOYEES.MANAGER_EMP_ID` left `NULL` : Approval routes by role + department. It should be populated iff the backend enforces a direct-manager link.
