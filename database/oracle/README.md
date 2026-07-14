# Oracle Database README

This directory manages the deployment, lifecycle initialization, and seed data for the 13 core business tables.

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
Use the following designated test environment credentials to connect:

| Account Name | Password | Role / Intent |
| :--- | :--- | :--- |
| **`TRAINING_OWNER`** | `Owner_Password_2026#` | Schema Owner. Used exclusively by migrations to manage structures, indexes, and grants. |
| **`TRAINING_APP`** | `App_Runtime_2026#` | Application User. Used securely by the C# Web API for runtime DML operations. |

Example command to run:
```
sqlplus TRAINING_OWNER/Owner_Password_2026#@//localhost:1539/FREEPDB1.localdomain
```



> ⚠️ **Crucial Runtime Rule:** Upon opening a connection instance, the C# application data access layer must immediately execute the following session context command:
> ```sql
> ALTER SESSION SET CURRENT_SCHEMA = TRAINING_OWNER;
> ```
> This maps all un-prefixed database queries cleanly to the business tables without requiring individual schema prefixes.

---

## 3. Repository Directory Tree
The database engine workspace is strictly structured as follows:

```text
database/oracle/
├── README.md               # This connection and environment guide
├── migrations/             # Version-controlled structural DDL and scripts
│   ├── V001__init_schema.sql
│   ├── V002__add_indexes.sql
│   └── V003__grant_app_privileges.sql
├── seed/                   # Data initialization environments
│   ├── S001__roles_and_accounts.sql
│   ├── S002__basic_data.sql
│   └── S003__demo_flow.sql
└── samples/                # Sanity checking and validation operations
    └── S001__verification.sql
```


## 4. Dev Logs

### Jue 14 

> Migrations (V001 - V003) loaded, ECS & oracle fundamentals configured.

Proposed Task Board Changes:

P0-08 : Change status to DONE.
* Evidence: Oracle Free 23ai instance running on AliCloud Linux 4 (47.100.108.150:1539/FREEPDB1), with separate TRAINING_OWNER and TRAINING_APP schemas successfully initialized. 

P0-11 : Change status to DONE.
* Evidence: 13 core database tables, query indexes, and minimal application DML permission rules successfully compiled and verified through the terminal. 

Proposed Risk Ledger changes:

R-001 : Change current status from OPEN to CLOSED. 

* Verification Evidence: Remote connectivity achieved. Hostname loopback resolution and 4GB swap space configured on host. Port 1539 listener verified active and restricted via AliCloud Security Group inbound whitelisting rules.  

Proposed Decision Logs changes:

* D-004 : Change status from PROPOSED to ACCEPTED.  
* D-005: Change status from PROPOSED to ACCEPTED.  
* D-006 (迁移账号 TRAINING_OWNER / 运行账号 TRAINING_APP): Change status from PROPOSED to ACCEPTED.  

