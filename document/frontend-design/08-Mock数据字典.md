# Mock数据字典与场景设计

## 1. 文档信息

| 项目 | 内容 |
| --- | --- |
| 项目名称 | 企业内部培训管理系统 |
| 当前阶段 | 第三阶段：规范与Mock（步骤9） |
| 文档版本 | V1.0（Mock数据评审通过） |
| 评审状态 | 已通过；依据 2026-08-07 用户确认进入步骤10 |
| 适用范围 | P0页面、角色演示、页面状态和核心主流程 |
| 数据性质 | 全部为虚构Mock数据，不包含真实员工、密码、联系方式或业务数据 |
| 当前边界 | 只定义数据和场景，不创建Mock服务、JSON文件、接口代码或Vue工程 |

## 2. 使用原则与确认状态

### 2.1 使用原则

1. Mock数据只用于页面设计、交互验证和演示，不等于最终API字段。
2. 字段名采用前端暂定的`camelCase`，接口冻结后逐项映射。
3. 枚举传输暂用英文值，页面展示统一中文。
4. 所有日期使用固定值，避免每次演示因“今天”变化而产生不确定结果。
5. 关联ID必须真实对应，禁止为了单页展示创建无法串联的孤立数据。
6. 正常、空数据、失败、权限、登录过期和业务冲突场景相互隔离，避免一个场景污染另一个场景。
7. Mock不得绕过角色权限和业务状态，不能让前端原型演示出后端不允许的状态跳转。

### 2.2 确认状态

| 标记 | 含义 |
| --- | --- |
| 已有依据（API未冻结） | 数据库或项目文档已有同类字段，但API名称、类型和结构仍需确认 |
| 暂定 | 前端为页面设计和演示提出的临时字段或结构 |
| 待确认 | 文档存在冲突或缺少规则，不能视为最终字段 |

本文件不使用“后端已确认”标记，因为当前真实API尚未冻结。

## 3. 通用数据约定

### 3.1 类型约定

| 类型 | 说明 | 示例 |
| --- | --- | --- |
| `number` | 数字ID、数量、金额、分数、学时 | `1001`、`8.0` |
| `string` | 名称、说明、枚举、编号、URL | `"张三"`、`"PUBLISHED"` |
| `boolean` | 仅用于前端派生或临时开关 | `true` |
| `date` | 仅日期，格式`YYYY-MM-DD` | `"2026-08-20"` |
| `datetime` | 日期时间，格式`YYYY-MM-DDTHH:mm:ss+08:00` | `"2026-08-20T09:00:00+08:00"` |
| `array<T>` | 同类对象数组 | `[{...}]` |
| `object` | 嵌套摘要或结构化数据 | `{ "empId": 1001 }` |
| `null` | 业务上尚未产生或明确无值 | `null` |

### 3.2 空值约定

- 未发生的审批、签到、测试或过期日期使用`null`，不使用空字符串。
- 列表展示空值时统一转换为`—`。
- 空数组使用`[]`，不使用`null`。
- 可选对象无值时使用`null`，避免返回只有空字段的伪对象。
- 后端最终是否返回缺失字段或`null`待接口冻结确认，Mock统一使用`null`便于状态验证。

### 3.3 ID区间约定

| 实体 | Mock ID区间 |
| --- | --- |
| 部门 | 100～199 |
| 角色 | 200～299 |
| 员工 | 1000～1999 |
| 用户角色 | 2000～2999 |
| 讲师 | 3000～3999 |
| 课程 | 4000～4999 |
| 申请 | 5000～5999 |
| 报名 | 6000～6999 |
| 签到 | 7000～7999 |
| 评分 | 8000～8999 |
| 测试 | 9000～9999 |
| 证书 | 10000～10999 |
| 黑名单 | 11000～11999 |

## 4. 通用响应与分页字典

### 4.1 `ApiResponse<T>`

| 字段 | 类型 | 必填 | 示例 | 说明 | 状态 |
| --- | --- | --- | --- | --- | --- |
| `success` | boolean | 是 | `true` | 请求是否按预期成功 | 已有依据（API未冻结） |
| `message` | string | 是 | `"ok"` | 用户提示基础文案 | 已有依据（API未冻结） |
| `data` | T/null | 是 | `{...}` | 成功数据；失败时可为`null` | 已有依据（API未冻结） |
| `errors` | array<object> | 否 | `[]` | 结构化字段错误 | 已有依据（API未冻结） |
| `traceId` | string | 是 | `"trace-mock-0001"` | 联调和异常定位编号 | 已有依据（API未冻结） |

成功示例：

```json
{
  "success": true,
  "message": "ok",
  "data": {},
  "traceId": "trace-mock-0001"
}
```

失败示例：

```json
{
  "success": false,
  "message": "课程名额已满",
  "data": null,
  "errors": [],
  "traceId": "trace-mock-409-full"
}
```

### 4.2 `FieldError`

| 字段 | 类型 | 必填 | 示例 | 说明 | 状态 |
| --- | --- | --- | --- | --- | --- |
| `field` | string | 是 | `"reason"` | 前端暂定字段名 | 已有依据（API未冻结） |
| `code` | string | 否 | `"MAX_LENGTH"` | 稳定错误码 | 待确认 |
| `message` | string | 是 | `"申请理由不能超过500字"` | 中文错误提示 | 已有依据（API未冻结） |

### 4.3 `PagedResult<T>`

| 字段 | 类型 | 必填 | 示例 | 说明 | 状态 |
| --- | --- | --- | --- | --- | --- |
| `items` | array<T> | 是 | `[]` | 当前页数据 | 已有依据（API未冻结） |
| `page` | number | 是 | `1` | 当前页，从1开始 | 已有依据（API未冻结） |
| `pageSize` | number | 是 | `20` | 每页数量 | 已有依据（API未冻结） |
| `total` | number | 是 | `48` | 符合条件的总数 | 已有依据（API未冻结） |

## 5. 认证、当前用户与权限字典

### 5.1 `LoginRequest`

| 字段 | 类型 | 必填 | 示例 | 说明 | 状态 |
| --- | --- | --- | --- | --- | --- |
| `account` | string | 是 | `"employee.demo"` | 员工账号/登录名 | 待确认，数据库表未明确账号字段 |
| `password` | string | 是 | `"Demo@123"` | 纯Mock演示密码 | 暂定，禁止用于真实环境 |

### 5.2 `CurrentUser`

| 字段 | 类型 | 必填 | 示例 | 说明 | 状态 |
| --- | --- | --- | --- | --- | --- |
| `empId` | number | 是 | `1001` | 员工ID | 已有依据（API未冻结） |
| `empName` | string | 是 | `"张三"` | 员工姓名 | 已有依据（API未冻结） |
| `deptId` | number/null | 是 | `101` | 部门ID | 暂定，员工表原稿主要使用部门名称 |
| `deptName` | string/null | 是 | `"技术部"` | 部门名称 | 已有依据（API未冻结） |
| `position` | string/null | 是 | `"前端工程师"` | 职位 | 已有依据（API未冻结） |
| `status` | string | 是 | `"ACTIVE"` | 员工状态 | 已有依据（API未冻结） |
| `roles` | array<RoleSummary> | 是 | `[{...}]` | 当前用户角色 | 已有依据（API未冻结） |
| `permissions` | array<string> | 是 | `["course:view"]` | 精细权限码 | 待确认 |
| `primaryRole` | string/null | 否 | `"EMPLOYEE"` | 主角色或当前身份 | 待确认，多角色方案未冻结 |

### 5.3 纯Mock角色账号

| 角色 | 账号 | 密码 | 对应员工 | 用途 |
| --- | --- | --- | --- | --- |
| 员工 | `employee.demo` | `Demo@123` | 张三（1001） | 申请、报名、查看证书 |
| 部门主管 | `manager.demo` | `Demo@123` | 李主管（1002） | 本部门审批 |
| HR | `hr.demo` | `Demo@123` | 王HR（1003） | 备案、签到、证书 |
| 管理员 | `admin.demo` | `Demo@123` | 赵管理员（1004） | 基础数据、课程发布、签到 |

这些账号仅为设计说明，不应进入生产数据库或真实登录说明。

## 6. 枚举字典

| 枚举 | 接口暂定值 | 中文展示 | 状态 |
| --- | --- | --- | --- |
| `RoleCode` | `EMPLOYEE` | 员工 | 暂定英文值 |
| `RoleCode` | `DEPT_MANAGER` | 部门主管 | 暂定英文值 |
| `RoleCode` | `HR` | HR | 暂定英文值 |
| `RoleCode` | `ADMIN` | 管理员 | 暂定英文值 |
| `EmployeeStatus` | `ACTIVE` | 在职 | 已有依据（API未冻结） |
| `EmployeeStatus` | `RESIGNED` | 离职 | 已有依据（API未冻结） |
| `CourseType` | `SKILL` | 技能类 | 待确认英文值 |
| `CourseType` | `MANAGEMENT` | 管理类 | 待确认英文值 |
| `CourseType` | `SAFETY` | 安全类 | 待确认英文值 |
| `CourseStatus` | `DRAFT` | 草稿 | 已有依据（API未冻结） |
| `CourseStatus` | `PUBLISHED` | 已发布 | 已有依据（API未冻结） |
| `CourseStatus` | `CLOSED` | 已关闭 | 已有依据（API未冻结） |
| `RequestStatus` | `PENDING` | 待审批 | 已有依据（API未冻结） |
| `RequestStatus` | `DEPT_APPROVED` | 主管通过 | 已有依据（API未冻结） |
| `RequestStatus` | `DEPT_REJECTED` | 主管驳回 | 已有依据（API未冻结） |
| `RequestStatus` | `HR_FILED` | HR已备案 | 已有依据（API未冻结） |
| `RegistrationStatus` | `REGISTERED` | 已报名 | 已有依据（API未冻结） |
| `RegistrationStatus` | `SIGNED_IN` | 已签到 | 已有依据（API未冻结） |
| `RegistrationStatus` | `ABSENT` | 缺勤 | 已有依据（API未冻结） |
| `RegistrationStatus` | `COMPLETED` | 已完成 | 已有依据（API未冻结） |
| `RegistrationStatus` | `CANCELED` | 已取消 | 项目规范有依据，数据库原稿缺失 |
| `SigninType` | `QR` | 扫码签到 | 待确认英文值与实现范围 |
| `SigninType` | `MANUAL` | 手动签到 | 待确认英文值 |
| `TestType` | `PRE` | 训前测试 | 已有依据（API未冻结） |
| `TestType` | `POST` | 训后测试 | 已有依据（API未冻结） |
| `YesNo` | `Y` | 是 | 已有依据（API未冻结） |
| `YesNo` | `N` | 否 | 已有依据（API未冻结） |
| `BlacklistStatus` | `ACTIVE` | 生效中 | 已有依据（API未冻结） |
| `BlacklistStatus` | `RELEASED` | 已解除 | 已有依据（API未冻结） |
| `CertificateDisplayStatus` | `VALID` | 有效 | 待确认是否由接口返回 |
| `CertificateDisplayStatus` | `EXPIRING` | 即将过期 | 待确认时间窗口 |
| `CertificateDisplayStatus` | `EXPIRED` | 已过期 | 待确认是否为派生状态 |

## 7. 核心实体总览

| 实体 | 前端暂定名称 | 主要页面 | 关联实体 |
| --- | --- | --- | --- |
| 员工 | `Employee` | 登录、审批、签到、证书 | 部门、角色、申请、报名 |
| 部门预算 | `DepartmentTraining` | 控制台、员工/申请摘要、P1预算页 | 员工、课程 |
| 角色 | `Role` | 当前用户、权限菜单 | 用户角色 |
| 用户角色 | `UserRole` | 当前用户权限 | 员工、角色 |
| 讲师 | `Trainer` | 课程详情 | 课程、评分 |
| 课程 | `Course` | 课程列表/详情、申请、报名 | 讲师、部门 |
| 培训申请 | `TrainingRequest` | 我的申请、主管审批、HR备案 | 员工、课程 |
| 培训报名 | `Registration` | 我的报名、签到 | 员工、课程、签到 |
| 培训签到 | `Attendance` | 报名详情、签到管理 | 报名 |
| 讲师评分 | `TrainerRating` | P2评分页面 | 员工、课程、讲师 |
| 培训测试 | `TrainingTest` | P2测试页面、证书资格 | 员工、课程 |
| 培训证书 | `Certificate` | 我的证书、证书管理/详情 | 员工、课程 |
| 黑名单 | `BlacklistEntry` | 报名资格、P2黑名单页 | 员工 |

## 8. 实体字段字典

### 8.1 `Employee`

| 字段 | 类型 | 必填 | 示例 | 枚举/说明 | 确认状态 |
| --- | --- | --- | --- | --- | --- |
| `empId` | number | 是 | `1001` | 员工ID | 已有依据（API未冻结） |
| `empName` | string | 是 | `"张三"` | 员工姓名 | 已有依据（API未冻结） |
| `deptId` | number/null | 是 | `101` | 部门ID | 暂定 |
| `deptName` | string/null | 是 | `"技术部"` | 部门名称 | 已有依据（API未冻结） |
| `position` | string/null | 是 | `"前端工程师"` | 职位 | 已有依据（API未冻结） |
| `email` | string/null | 是 | `"zhangsan@example.test"` | 纯Mock邮箱 | 已有依据（API未冻结） |
| `phone` | string/null | 是 | `"13800001001"` | 纯Mock电话 | 已有依据（API未冻结） |
| `hireDate` | date/null | 是 | `"2024-07-01"` | 入职日期 | 已有依据（API未冻结） |
| `status` | string | 是 | `"ACTIVE"` | `EmployeeStatus` | 已有依据（API未冻结） |
| `createdAt` | datetime | 是 | `"2024-07-01T09:00:00+08:00"` | 创建时间 | 暂定命名 |

### 8.2 `DepartmentTraining`

| 字段 | 类型 | 必填 | 示例 | 枚举/说明 | 确认状态 |
| --- | --- | --- | --- | --- | --- |
| `deptId` | number | 是 | `101` | 部门ID | 已有依据（API未冻结） |
| `deptName` | string | 是 | `"技术部"` | 部门名称 | 已有依据（API未冻结） |
| `year` | number | 是 | `2026` | 预算年度 | 待确认，原表业务描述按年度但字段缺失 |
| `annualBudget` | number | 是 | `200000.00` | 年度预算 | 已有依据（API未冻结） |
| `usedBudget` | number | 是 | `60000.00` | 已用预算 | 已有依据（API未冻结） |
| `remainBudget` | number | 是 | `140000.00` | 剩余预算，由后端返回 | 已有依据（API未冻结） |

### 8.3 `Role`

| 字段 | 类型 | 必填 | 示例 | 枚举/说明 | 确认状态 |
| --- | --- | --- | --- | --- | --- |
| `roleId` | number | 是 | `201` | 角色ID | 已有依据（API未冻结） |
| `roleCode` | string | 是 | `"EMPLOYEE"` | `RoleCode` | 暂定 |
| `roleName` | string | 是 | `"员工"` | 中文角色名 | 已有依据（API未冻结） |
| `permissions` | array<string> | 是 | `["course:view"]` | 权限码列表 | 待确认，数据库原稿为JSON字符串 |
| `description` | string/null | 是 | `"普通员工角色"` | 角色说明 | 暂定 |

### 8.4 `UserRole`

| 字段 | 类型 | 必填 | 示例 | 枚举/说明 | 确认状态 |
| --- | --- | --- | --- | --- | --- |
| `userRoleId` | number | 是 | `2001` | 关联ID | 已有依据（API未冻结） |
| `empId` | number | 是 | `1001` | 员工ID | 已有依据（API未冻结） |
| `roleId` | number | 是 | `201` | 角色ID | 已有依据（API未冻结） |
| `isPrimary` | boolean | 否 | `true` | 是否主角色 | 待确认，多角色方案未冻结 |

### 8.5 `Trainer`

| 字段 | 类型 | 必填 | 示例 | 枚举/说明 | 确认状态 |
| --- | --- | --- | --- | --- | --- |
| `trainerId` | number | 是 | `3001` | 讲师ID | 已有依据（API未冻结） |
| `trainerName` | string | 是 | `"王老师"` | 讲师姓名 | 已有依据（API未冻结） |
| `title` | string/null | 是 | `"高级讲师"` | 职称 | 已有依据（API未冻结） |
| `company` | string/null | 是 | `"同济培训中心"` | 所属单位 | 已有依据（API未冻结） |
| `phone` | string/null | 是 | `"13800003001"` | 纯Mock电话 | 已有依据（API未冻结） |
| `email` | string/null | 是 | `"trainer1@example.test"` | 纯Mock邮箱 | 已有依据（API未冻结） |
| `starLevel` | number | 是 | `4` | 1～5，默认3 | 已有依据（API未冻结） |
| `isInternal` | string | 是 | `"N"` | `YesNo` | 暂定按英文值传输 |
| `createdAt` | datetime | 是 | `"2026-01-10T09:00:00+08:00"` | 创建时间 | 暂定命名 |

### 8.6 `Course`

| 字段 | 类型 | 必填 | 示例 | 枚举/说明 | 确认状态 |
| --- | --- | --- | --- | --- | --- |
| `courseId` | number | 是 | `4001` | 课程ID | 已有依据（API未冻结） |
| `courseName` | string | 是 | `"数据库基础训练"` | 课程名称 | 已有依据（API未冻结） |
| `courseType` | string | 是 | `"SKILL"` | `CourseType` | 待确认英文值 |
| `duration` | number | 是 | `8.0` | 课程学时 | 已有依据（API未冻结） |
| `trainerId` | number | 是 | `3001` | 当前按单讲师 | 已有依据；N:M设计冲突待确认 |
| `trainerName` | string | 是 | `"王老师"` | 列表/详情摘要 | 暂定冗余展示字段 |
| `maxStudents` | number | 是 | `30` | 最大人数，大于0 | 已有依据（API未冻结） |
| `registeredCount` | number | 是 | `18` | 已报名人数 | 暂定，由后端统计 |
| `remainingSeats` | number | 是 | `12` | 剩余名额 | 暂定，由后端返回 |
| `startTime` | datetime | 是 | `"2026-08-20T09:00:00+08:00"` | 开始时间 | 已有依据（API未冻结） |
| `endTime` | datetime | 是 | `"2026-08-20T17:00:00+08:00"` | 结束时间 | 已有依据（API未冻结） |
| `location` | string | 是 | `"A101培训室"` | 培训地点 | 已有依据（API未冻结） |
| `status` | string | 是 | `"PUBLISHED"` | `CourseStatus` | 已有依据（API未冻结） |
| `budgetAmount` | number | 是 | `12000.00` | 预算金额 | 已有依据（API未冻结） |
| `deptId` | number | 是 | `101` | 主办部门ID | 已有依据（API未冻结） |
| `deptName` | string | 是 | `"技术部"` | 主办部门名称 | 暂定冗余展示字段 |
| `preTestUrl` | string/null | 是 | `"https://example.test/pre/4001"` | 纯Mock链接 | 已有依据（API未冻结） |
| `postTestUrl` | string/null | 是 | `"https://example.test/post/4001"` | 纯Mock链接 | 已有依据（API未冻结） |
| `materialUrl` | string/null | 是 | `"https://example.test/material/4001"` | 纯Mock链接 | 已有依据（API未冻结） |
| `createdAt` | datetime | 是 | `"2026-08-01T10:00:00+08:00"` | 创建时间 | 暂定命名 |

### 8.7 `TrainingRequest`

| 字段 | 类型 | 必填 | 示例 | 枚举/说明 | 确认状态 |
| --- | --- | --- | --- | --- | --- |
| `requestId` | number | 是 | `5001` | 申请ID | 已有依据（API未冻结） |
| `empId` | number | 是 | `1001` | 申请员工ID | 已有依据（API未冻结） |
| `empName` | string | 是 | `"张三"` | 列表摘要 | 暂定冗余展示字段 |
| `deptId` | number/null | 是 | `101` | 申请员工部门ID | 暂定 |
| `deptName` | string/null | 是 | `"技术部"` | 列表摘要 | 暂定冗余展示字段 |
| `courseId` | number | 是 | `4001` | 课程ID | 已有依据（API未冻结） |
| `courseName` | string | 是 | `"数据库基础训练"` | 列表摘要 | 暂定冗余展示字段 |
| `reason` | string/null | 是 | `"提升数据库设计与查询能力"` | 最大500字暂定 | 已有依据（API未冻结） |
| `status` | string | 是 | `"PENDING"` | `RequestStatus` | 已有依据（API未冻结） |
| `deptApproverId` | number/null | 是 | `1002` | 主管员工ID | 暂定，数据库原稿主要记录姓名 |
| `deptApproverName` | string/null | 是 | `"李主管"` | 主管姓名 | 已有依据（API未冻结） |
| `deptApproveTime` | datetime/null | 是 | `null` | 主管审批时间 | 已有依据（API未冻结） |
| `deptApproveRemark` | string/null | 是 | `null` | 主管意见 | 已有依据（API未冻结） |
| `hrApproverId` | number/null | 是 | `1003` | HR员工ID | 暂定 |
| `hrApproverName` | string/null | 是 | `"王HR"` | HR姓名 | 已有依据（API未冻结） |
| `hrApproveTime` | datetime/null | 是 | `null` | HR备案时间 | 已有依据（API未冻结） |
| `requestDate` | datetime | 是 | `"2026-08-06T10:00:00+08:00"` | 申请时间 | 已有依据（API未冻结） |

### 8.8 `Registration`

| 字段 | 类型 | 必填 | 示例 | 枚举/说明 | 确认状态 |
| --- | --- | --- | --- | --- | --- |
| `regId` | number | 是 | `6001` | 报名ID | 已有依据（API未冻结） |
| `empId` | number | 是 | `1001` | 员工ID | 已有依据（API未冻结） |
| `empName` | string | 是 | `"张三"` | 列表摘要 | 暂定冗余展示字段 |
| `deptName` | string/null | 是 | `"技术部"` | 列表摘要 | 暂定冗余展示字段 |
| `courseId` | number | 是 | `4001` | 课程ID | 已有依据（API未冻结） |
| `courseName` | string | 是 | `"数据库基础训练"` | 列表摘要 | 暂定冗余展示字段 |
| `regDate` | datetime | 是 | `"2026-08-08T10:00:00+08:00"` | 报名时间 | 已有依据（API未冻结） |
| `status` | string | 是 | `"REGISTERED"` | `RegistrationStatus` | 已有依据（API未冻结） |
| `signinTime` | datetime/null | 是 | `null` | 最近/主签到时间 | 已有依据（API未冻结） |
| `actualHours` | number/null | 是 | `null` | 实际学时 | 已有依据（API未冻结） |
| `isOvertime` | string | 是 | `"N"` | `YesNo` | 暂定按英文值传输 |
| `attendance` | object/null | 否 | `null` | 报名详情可嵌套签到摘要 | 暂定响应结构 |

### 8.9 `Attendance`

| 字段 | 类型 | 必填 | 示例 | 枚举/说明 | 确认状态 |
| --- | --- | --- | --- | --- | --- |
| `attendId` | number | 是 | `7001` | 签到ID | 已有依据（API未冻结） |
| `regId` | number | 是 | `6001` | 报名ID | 已有依据（API未冻结） |
| `signinType` | string | 是 | `"MANUAL"` | `SigninType` | 待确认英文值 |
| `signinTime` | datetime | 是 | `"2026-08-20T09:06:00+08:00"` | 签到时间 | 已有依据（API未冻结） |
| `latenessMinutes` | number | 是 | `6` | 迟到分钟 | 已有依据（API未冻结） |
| `deductHours` | number | 是 | `0.5` | 扣减学时 | 已有依据（API未冻结） |
| `remark` | string/null | 是 | `"交通延误，HR补签到"` | 备注 | 已有依据（API未冻结） |
| `operatorId` | number/null | 否 | `1003` | 操作人ID | 暂定，审计需要待确认 |

### 8.10 `TrainerRating`

| 字段 | 类型 | 必填 | 示例 | 枚举/说明 | 确认状态 |
| --- | --- | --- | --- | --- | --- |
| `ratingId` | number | 是 | `8001` | 评分ID | 已有依据（API未冻结） |
| `courseId` | number | 是 | `4001` | 课程ID | 已有依据（API未冻结） |
| `courseName` | string | 是 | `"数据库基础训练"` | 列表摘要 | 暂定 |
| `trainerId` | number | 是 | `3001` | 讲师ID | 已有依据（API未冻结） |
| `trainerName` | string | 是 | `"王老师"` | 列表摘要 | 暂定 |
| `empId` | number | 是 | `1001` | 员工ID | 已有依据；匿名可见范围待确认 |
| `score` | number | 是 | `4.5` | 1～5 | 已有依据（API未冻结） |
| `comment` | string/null | 是 | `"讲解清晰，案例实用"` | 评价内容 | 已有依据（API未冻结） |
| `hrVerified` | string | 是 | `"N"` | `YesNo` | 暂定按英文值传输 |
| `hrComment` | string/null | 是 | `null` | HR复核意见 | 已有依据（API未冻结） |
| `ratingDate` | datetime | 是 | `"2026-08-20T18:00:00+08:00"` | 评分时间 | 已有依据（API未冻结） |

### 8.11 `TrainingTest`

| 字段 | 类型 | 必填 | 示例 | 枚举/说明 | 确认状态 |
| --- | --- | --- | --- | --- | --- |
| `testId` | number | 是 | `9001` | 测试ID | 已有依据（API未冻结） |
| `empId` | number | 是 | `1001` | 员工ID | 已有依据（API未冻结） |
| `empName` | string | 是 | `"张三"` | 列表摘要 | 暂定 |
| `courseId` | number | 是 | `4001` | 课程ID | 已有依据（API未冻结） |
| `courseName` | string | 是 | `"数据库基础训练"` | 列表摘要 | 暂定 |
| `testType` | string | 是 | `"PRE"` | `TestType` | 已有依据（API未冻结） |
| `score` | number | 是 | `78.0` | 0～100 | 已有依据（API未冻结） |
| `testDate` | datetime | 是 | `"2026-08-19T18:00:00+08:00"` | 测试时间 | 已有依据（API未冻结） |

### 8.12 `Certificate`

| 字段 | 类型 | 必填 | 示例 | 枚举/说明 | 确认状态 |
| --- | --- | --- | --- | --- | --- |
| `certId` | number | 是 | `10001` | 证书ID | 已有依据（API未冻结） |
| `empId` | number | 是 | `1001` | 员工ID | 已有依据（API未冻结） |
| `empName` | string | 是 | `"张三"` | 展示摘要 | 暂定 |
| `deptName` | string/null | 是 | `"技术部"` | HR列表摘要 | 暂定 |
| `courseId` | number | 是 | `4001` | 课程ID | 已有依据（API未冻结） |
| `courseName` | string | 是 | `"数据库基础训练"` | 展示摘要 | 暂定 |
| `certCode` | string | 是 | `"CERT-20260820-4001-1001"` | 唯一编号 | 已有依据，格式暂定 |
| `issueDate` | date | 是 | `"2026-08-20"` | 发证日期 | 已有依据（API未冻结） |
| `expireDate` | date/null | 是 | `"2028-08-20"` | 过期日期 | 已有依据（API未冻结） |
| `notified` | string | 是 | `"N"` | `YesNo` | 暂定按英文值传输 |
| `displayStatus` | string | 否 | `"VALID"` | `CertificateDisplayStatus` | 待确认是否由接口返回 |

### 8.13 `BlacklistEntry`

| 字段 | 类型 | 必填 | 示例 | 枚举/说明 | 确认状态 |
| --- | --- | --- | --- | --- | --- |
| `blackId` | number | 是 | `11001` | 黑名单ID | 已有依据（API未冻结） |
| `empId` | number | 是 | `1006` | 员工ID | 已有依据（API未冻结） |
| `empName` | string | 是 | `"孙员工"` | 列表/冲突摘要 | 暂定 |
| `reason` | string/null | 是 | `"连续缺勤"` | 黑名单原因 | 已有依据（API未冻结） |
| `startDate` | date | 是 | `"2026-08-01"` | 开始日期 | 已有依据（API未冻结） |
| `endDate` | date/null | 是 | `"2026-09-01"` | 结束日期 | 已有依据（API未冻结） |
| `status` | string | 是 | `"ACTIVE"` | `BlacklistStatus` | 已有依据（API未冻结） |
| `sourceRegId` | number/null | 否 | `6006` | 触发黑名单的报名 | 待确认，不自动联动前不使用 |

## 9. 列表与详情摘要结构

为避免每个列表复制完整实体，Mock暂定以下摘要：

### 9.1 `EmployeeSummary`

```json
{
  "empId": 1001,
  "empName": "张三",
  "deptId": 101,
  "deptName": "技术部",
  "position": "前端工程师",
  "status": "ACTIVE"
}
```

### 9.2 `CourseSummary`

```json
{
  "courseId": 4001,
  "courseName": "数据库基础训练",
  "courseType": "SKILL",
  "trainerName": "王老师",
  "startTime": "2026-08-20T09:00:00+08:00",
  "endTime": "2026-08-20T17:00:00+08:00",
  "location": "A101培训室",
  "status": "PUBLISHED",
  "maxStudents": 30,
  "registeredCount": 18,
  "remainingSeats": 12
}
```

### 9.3 `ActionEligibility`

```json
{
  "canApply": true,
  "applyReason": null,
  "canRegister": false,
  "registerReason": "申请尚未完成HR备案",
  "existingRequestId": 5001,
  "existingRegistrationId": null
}
```

`ActionEligibility`用于原型展示明确的禁用原因，但是否由课程详情接口返回、单独资格接口返回或由权限码组合，待后端确认。

## 10. 基础示例数据集

### 10.1 部门

| deptId | deptName | year | annualBudget | usedBudget | remainBudget |
| --- | --- | --- | --- | --- | --- |
| 101 | 技术部 | 2026 | 200000.00 | 60000.00 | 140000.00 |
| 102 | 人力资源部 | 2026 | 150000.00 | 45000.00 | 105000.00 |
| 103 | 财务部 | 2026 | 120000.00 | 30000.00 | 90000.00 |

### 10.2 角色与员工

| empId | empName | dept | position | status | roles | 场景用途 |
| --- | --- | --- | --- | --- | --- | --- |
| 1001 | 张三 | 技术部 | 前端工程师 | ACTIVE | EMPLOYEE | 正常员工主流程 |
| 1002 | 李主管 | 技术部 | 部门主管 | ACTIVE | DEPT_MANAGER | 审批主流程 |
| 1003 | 王HR | 人力资源部 | HR专员 | ACTIVE | HR | 备案、签到、证书 |
| 1004 | 赵管理员 | 人力资源部 | 系统管理员 | ACTIVE | ADMIN | 基础数据、课程、签到 |
| 1005 | 陈员工 | 财务部 | 会计 | RESIGNED | EMPLOYEE | 离职冲突 |
| 1006 | 孙员工 | 技术部 | 测试工程师 | ACTIVE | EMPLOYEE | 黑名单冲突 |
| 1007 | 周员工 | 技术部 | 后端工程师 | ACTIVE | EMPLOYEE | 满员/重复场景 |

### 10.3 讲师与课程

| courseId | courseName | trainer | status | startTime | capacity | registered | 场景用途 |
| --- | --- | --- | --- | --- | --- | --- | --- |
| 4001 | 数据库基础训练 | 王老师（3001） | PUBLISHED | 2026-08-20 09:00 | 30 | 18 | 正常主流程 |
| 4002 | 安全生产培训 | 李老师（3002） | PUBLISHED | 2026-08-15 09:00 | 20 | 20 | 课程满员 |
| 4003 | 管理能力提升 | 王老师（3001） | CLOSED | 2026-07-20 09:00 | 25 | 20 | 已关闭/已过期 |
| 4004 | Oracle性能优化 | 李老师（3002） | DRAFT | 2026-09-01 09:00 | 20 | 0 | 草稿不可见/不可报名 |

## 11. 主流程快照设计

同一业务链路使用固定ID，在不同快照中改变状态。每次原型演示只加载一个快照，避免同时出现互相矛盾的数据。

| 快照编号 | 申请5001 | 报名6001 | 签到7001 | 测试 | 证书10001 | 演示节点 |
| --- | --- | --- | --- | --- | --- | --- |
| FLOW-SNAPSHOT-01 | 不存在 | 不存在 | 不存在 | 无 | 不存在 | 员工准备提交申请 |
| FLOW-SNAPSHOT-02 | PENDING | 不存在 | 不存在 | 无 | 不存在 | 等待主管审批 |
| FLOW-SNAPSHOT-03 | DEPT_APPROVED | 不存在 | 不存在 | 无 | 不存在 | 等待HR备案 |
| FLOW-SNAPSHOT-04 | HR_FILED | 不存在 | 不存在 | 无 | 不存在 | 员工准备报名 |
| FLOW-SNAPSHOT-05 | HR_FILED | REGISTERED | 不存在 | PRE可选 | 不存在 | 等待签到 |
| FLOW-SNAPSHOT-06 | HR_FILED | SIGNED_IN | 已存在 | PRE可选 | 不存在 | 等待完成培训 |
| FLOW-SNAPSHOT-07 | HR_FILED | COMPLETED | 已存在 | PRE/POST可选 | 不存在 | 等待生成证书 |
| FLOW-SNAPSHOT-08 | HR_FILED | COMPLETED | 已存在 | PRE/POST | 已存在 | 员工查看证书 |

快照切换只是设计概念，不规定未来Mock工具或服务的实现方式。

## 12. 页面场景设计

### 12.1 公共通用场景

| 场景编号 | 对应状态 | 数据/响应 | 预期界面 |
| --- | --- | --- | --- |
| SCN-GLOBAL-01 | MOCK-STATE-01 | 请求延迟，暂不返回 | 首次加载骨架 |
| SCN-GLOBAL-02 | MOCK-STATE-02 | 正常响应和有效数据 | PAGE-READY |
| SCN-GLOBAL-03 | MOCK-STATE-03 | `items=[]`,`total=0`，无额外筛选 | 业务空数据 |
| SCN-GLOBAL-04 | MOCK-STATE-04 | `items=[]`,`total=0`，存在筛选 | 搜索无结果 |
| SCN-GLOBAL-05 | MOCK-STATE-05 | 500 + `traceId` | 页面加载失败 |
| SCN-GLOBAL-06 | MOCK-STATE-06 | 主体成功、局部区块500 | 局部失败 |
| SCN-GLOBAL-07 | MOCK-STATE-07 | 写请求保持Pending | 按钮/当前行提交中 |
| SCN-GLOBAL-08 | MOCK-STATE-08 | 200/201成功 | 成功提示并刷新 |
| SCN-GLOBAL-09 | MOCK-STATE-09 | 400 + `errors[]` | 字段校验失败 |
| SCN-GLOBAL-10 | MOCK-STATE-10 | 403 | 403页面/操作无权 |
| SCN-GLOBAL-11 | MOCK-STATE-11 | 401 | 登录过期恢复 |
| SCN-GLOBAL-12 | MOCK-STATE-12 | 409 + 业务冲突 | 冲突提示和刷新 |
| SCN-GLOBAL-13 | MOCK-STATE-13 | 404 | 页面或资源不存在 |
| SCN-GLOBAL-14 | MOCK-STATE-14 | 超时且结果未知 | 查询最新状态 |

### 12.2 17个P0及辅助页面场景矩阵

| 页面 | 正常场景 | 空/无结果 | 失败/权限 | 关键冲突场景 |
| --- | --- | --- | --- | --- |
| 登录 | SCN-AUTH-01四角色登录成功 | 不适用 | SCN-AUTH-02认证失败、03账号不可用、04服务失败 | 登录过期回跳 |
| 控制台 | SCN-DASH-01角色化待办 | SCN-DASH-02无待办 | SCN-DASH-03局部统计失败、04无权限入口 | 待办状态变化 |
| 课程列表 | SCN-COURSE-01正常分页 | SCN-COURSE-02业务空、03搜索无结果 | SCN-COURSE-04加载失败 | 状态和余量刷新 |
| 课程详情 | SCN-COURSE-05可申请、06可报名 | 不适用 | SCN-COURSE-07资源不存在、08无权限 | 关闭、满员、黑名单、重复 |
| 提交申请 | SCN-REQ-01正常表单 | 不适用 | SCN-REQ-02课程加载失败、03字段错误 | 重复、课程变化、离职 |
| 我的申请 | SCN-REQ-04多状态列表 | SCN-REQ-05空、06无结果 | SCN-REQ-07加载失败 | 状态刷新 |
| 申请详情 | SCN-REQ-08四状态详情 | 不适用 | SCN-REQ-09无权限、10不存在 | 已被处理、重复备案 |
| 主管审批 | SCN-APPROVAL-01待审批列表 | SCN-APPROVAL-02无待办、03无结果 | SCN-APPROVAL-04越权 | 重复审批、状态变化 |
| HR备案 | SCN-FILING-01待备案列表 | SCN-FILING-02无待办、03无结果 | SCN-FILING-04无权限 | 跳过审批、重复备案 |
| 我的报名 | SCN-REG-01多状态列表 | SCN-REG-02空、03无结果 | SCN-REG-04加载失败 | 取消时间、已签到 |
| 报名详情 | SCN-REG-05各状态详情 | 不适用 | SCN-REG-06非本人、07不存在 | 状态变化 |
| 报名与签到 | SCN-ATT-01正常列表和统计 | SCN-ATT-02未选课程、03无报名、04无结果 | SCN-ATT-05加载失败、06无权限 | 重复签到、缺勤条件、学时不足 |
| 我的证书 | SCN-CERT-01有效/过期列表 | SCN-CERT-02空、03无结果 | SCN-CERT-04加载失败 | 状态变化 |
| 证书管理 | SCN-CERT-05候选和已生成 | SCN-CERT-06无候选、07无结果 | SCN-CERT-08无权限 | 资格不足、重复生成、结果未知 |
| 证书详情 | SCN-CERT-09员工/HR视角 | 不适用 | SCN-CERT-10非本人、11不存在 | 重复提醒 |
| 403 | SCN-ERROR-01 | 不适用 | 固定403数据 | 不适用 |
| 404 | SCN-ERROR-02 | 不适用 | 固定404数据 | 不适用 |

## 13. 业务冲突Mock场景

| 冲突编号 | 场景数据设置 | 推荐HTTP | 关键返回信息 | 预期恢复 |
| --- | --- | --- | --- | --- |
| CONFLICT-001 | 员工1001对课程4001已有PENDING申请5001 | 409 | `DUPLICATE_PENDING_REQUEST`（暂定） | 查看申请5001 |
| CONFLICT-002 | 课程4003为CLOSED或4004为DRAFT | 400/409待确认 | `COURSE_NOT_AVAILABLE` | 刷新课程详情 |
| CONFLICT-003 | 员工1005为RESIGNED | 400/409待确认 | `EMPLOYEE_RESIGNED` | 联系管理员 |
| CONFLICT-004 | 审批提交前申请5001已从PENDING变化 | 409 | `REQUEST_ALREADY_PROCESSED` | 刷新申请 |
| CONFLICT-005 | 对PENDING申请直接HR备案 | 409 | `REQUEST_NOT_DEPT_APPROVED` | 返回备案列表 |
| CONFLICT-006 | 申请5001已为HR_FILED再次备案 | 409 | `REQUEST_ALREADY_FILED` | 查看详情 |
| CONFLICT-007 | 申请仍为PENDING时尝试报名（暂定前置） | 409 | `REQUEST_NOT_FILED` | 查看申请进度 |
| CONFLICT-008 | 员工1006有ACTIVE黑名单11001 | 409 | `EMPLOYEE_BLACKLISTED` | 联系管理员 |
| CONFLICT-009 | 员工1001已有有效报名6001 | 409 | `DUPLICATE_REGISTRATION` | 查看报名6001 |
| CONFLICT-010 | 课程4002剩余名额0 | 409 | `COURSE_FULL` | 返回课程列表 |
| CONFLICT-011 | 课程已开始或过期后报名/取消 | 409 | `COURSE_ALREADY_STARTED` | 查看报名详情 |
| CONFLICT-012 | 报名6001已经SIGNED_IN再次签到 | 409 | `ALREADY_SIGNED_IN` | 查看签到记录 |
| CONFLICT-013 | 员工/课程组合不存在有效报名 | 404/409待确认 | `VALID_REGISTRATION_NOT_FOUND` | 返回报名列表 |
| CONFLICT-014 | 课程未结束或员工已签到时标记缺勤 | 409 | `ABSENT_NOT_ALLOWED` | 刷新状态 |
| CONFLICT-015 | 报名实际学时不足完成条件 | 409 | `COMPLETION_REQUIREMENT_NOT_MET` | 查看学时 |
| CONFLICT-016 | 员工1001已有评分8001 | 409 | `DUPLICATE_RATING` | 查看已有结果（若开放） |
| CONFLICT-017 | 已存在相同员工/课程/类型测试9001 | 409 | `DUPLICATE_TEST_TYPE` | 查看已有测试 |
| CONFLICT-018 | 报名不是COMPLETED或测试条件不足 | 409 | `CERTIFICATE_NOT_ELIGIBLE` | 查看资格原因 |
| CONFLICT-019 | 已存在证书10001 | 409 | `DUPLICATE_CERTIFICATE` | 查看证书10001 |
| CONFLICT-020 | 预算、讲师、部门或课程在提交前被修改 | 409 | `RELATED_DATA_CHANGED` | 刷新页面 |

业务错误码均为前端暂定建议，不得直接作为后端最终约定。

## 14. 权限场景设计

| 场景编号 | 当前用户 | 访问/操作 | Mock结果 | 预期界面 |
| --- | --- | --- | --- | --- |
| SCN-PERM-01 | 员工1001 | 访问主管审批 | 403 | 403页面 |
| SCN-PERM-02 | 主管1002 | 审批其他部门申请 | 403或404待确认 | 不暴露申请内容 |
| SCN-PERM-03 | HR1003 | 访问员工管理 | 403 | 403页面 |
| SCN-PERM-04 | 管理员1004 | 访问HR证书管理 | 按当前矩阵403 | 管理员是否拥有HR权限待确认 |
| SCN-PERM-05 | 员工1001 | 查看其他员工证书 | 403或404待确认 | 返回我的证书 |
| SCN-PERM-06 | 登录过期用户 | 任意受保护请求 | 401 | 进入登录恢复流程 |
| SCN-PERM-07 | 多角色用户（暂定） | 菜单与页面权限 | 权限并集 | 多角色方案评审 |

## 15. 数据量与分页场景

| 场景 | 数据量 | 目的 |
| --- | --- | --- |
| 小列表 | 3～5条 | 低保真和高保真视觉展示 |
| 标准列表 | 20条 | 默认页大小 |
| 多页列表 | 48条 | 验证3页、翻页和总数 |
| 空列表 | 0条 | PAGE-EMPTY |
| 搜索无结果 | 基础数据存在，但筛选后0条 | PAGE-NO-RESULT |
| 长文本 | 课程名80字、申请理由500字 | 省略、Tooltip和详情换行 |
| 长姓名/部门 | 10～20个中文字符 | 表格列宽和换行 |
| 边界数值 | 容量1、容量满、学时0/0.5/8、分数0/100 | 校验和显示精度 |

## 16. 日期与边界场景

| 场景编号 | 数据 | 验证目标 |
| --- | --- | --- |
| SCN-DATE-01 | 课程尚未开始 | 可报名/可取消 |
| SCN-DATE-02 | 当前时间等于课程开始时间 | 报名和取消边界待后端确认 |
| SCN-DATE-03 | 课程进行中 | 禁止新报名/取消，允许签到策略待确认 |
| SCN-DATE-04 | 课程已结束未签到 | 允许标记缺勤 |
| SCN-DATE-05 | 证书距过期日30天 | 即将过期窗口待确认 |
| SCN-DATE-06 | 证书已过期 | 显示已过期 |
| SCN-DATE-07 | 跨月/跨年课程 | 日期格式和筛选边界 |

## 17. Mock响应延迟建议

仅用于原型体验设计，不是性能指标：

| 场景 | 建议模拟延迟 | 目的 |
| --- | --- | --- |
| 普通查询 | 300～600ms | 看清表格局部Loading |
| 首次详情 | 500～800ms | 验证骨架结构 |
| 写操作 | 600～1000ms | 验证按钮Loading和防重复 |
| 慢请求 | 3000ms | 验证持续Loading和用户耐心 |
| 超时/结果未知 | 主动不返回或断开 | 验证ACTION-UNKNOWN |

正式性能目标由项目负责人和后端确定，不能从Mock延迟反推。

## 18. 待后端确认字段清单

| 编号 | 实体/结构 | 待确认内容 | 前端当前处理 |
| --- | --- | --- | --- |
| MOCK-Q-001 | 登录 | 登录账号字段、Token或Session、刷新机制 | 使用`account/password`占位 |
| MOCK-Q-002 | 当前用户 | 角色结构、权限码、主角色/角色切换 | 使用角色数组和权限数组 |
| MOCK-Q-003 | 员工/部门 | 员工使用`deptId`还是`deptName`关联 | 同时保留ID和名称 |
| MOCK-Q-004 | 部门预算 | 预算年度字段和剩余预算计算位置 | 增加`year`，结果由后端返回 |
| MOCK-Q-005 | 课程 | 单讲师还是多讲师 | 当前按单`trainerId` |
| MOCK-Q-006 | 课程 | 已报名数、剩余名额是否直接返回 | Mock直接返回两个统计字段 |
| MOCK-Q-007 | 课程 | 申请/报名资格结构 | 使用`ActionEligibility`占位 |
| MOCK-Q-008 | 申请 | 主管/HR使用员工ID还是姓名记录 | 同时保留ID和姓名 |
| MOCK-Q-009 | 申请 | HR备案是否为报名前置、备案后是否自动报名 | 暂按备案后员工手动报名 |
| MOCK-Q-010 | 报名 | `CANCELED`是否进入最终枚举 | 暂按项目规范保留 |
| MOCK-Q-011 | 签到 | 迟到和实际学时由哪个接口返回 | Mock由签到/报名详情返回 |
| MOCK-Q-012 | 签到 | 是否记录操作人和审计字段 | 使用可选`operatorId` |
| MOCK-Q-013 | 评分 | 匿名可见范围和重复评分规则 | 保留`empId`但不决定展示 |
| MOCK-Q-014 | 测试 | PRE/POST录入主体和证书门槛 | 使用HR录入场景占位 |
| MOCK-Q-015 | 证书 | 编号生成规则、有效期、测试及格要求 | 使用暂定编号和资格状态 |
| MOCK-Q-016 | 证书 | 有效/即将过期/过期由接口还是前端派生 | 使用可选`displayStatus` |
| MOCK-Q-017 | 黑名单 | 缺勤是否自动创建黑名单 | 当前不自动联动 |
| MOCK-Q-018 | 错误响应 | 稳定业务错误码与字段错误格式 | 使用暂定`code` |
| MOCK-Q-019 | 数据权限 | 越权资源返回403还是404 | Mock同时准备两类场景 |
| MOCK-Q-020 | 幂等性 | 写操作是否支持幂等键 | 使用禁用按钮+结果查询设计 |

## 19. Mock数据验收清单

### 19.1 字段与枚举

- [ ] 13个核心实体均有字段、类型、必填、示例、枚举和确认状态。
- [ ] 前端字段使用统一`camelCase`，页面不直接使用数据库大写字段。
- [ ] 所有英文枚举在页面有唯一中文展示。
- [ ] `null`、空数组、空字符串和空字段使用规则一致。
- [ ] 未由后端确认的字段明确标为暂定或待确认。

### 19.2 关联与流程

- [ ] 员工、部门、角色、课程、申请、报名、签到和证书ID可以串联。
- [ ] 主流程快照覆盖申请、审批、备案、报名、签到、完成和证书。
- [ ] 状态跳转符合核心业务流程和状态机。
- [ ] 无需手工修改数据库概念即可切换演示快照。

### 19.3 页面状态

- [ ] 17个P0及辅助页面均有正常场景。
- [ ] 所有列表页有空数据和搜索无结果场景。
- [ ] 写操作有提交中、成功、校验失败、冲突和结果未知场景。
- [ ] 权限、登录过期、403、404、500和`traceId`场景完整。
- [ ] 20类业务冲突均有数据设置和恢复动作。

### 19.4 安全

- [ ] Mock不包含真实姓名、邮箱、手机号、密码或服务器地址。
- [ ] `example.test`等保留域名不会误请求真实第三方服务。
- [ ] Mock密码仅用于设计说明，不复用到真实账号。
- [ ] 错误响应不包含SQL、连接串或内部堆栈。

## 20. 步骤9完成结论

- 已定义统一响应、字段错误、分页、认证用户和权限Mock结构。
- 已定义角色、员工、部门、讲师、课程、申请、报名、签到、评分、测试、证书和黑名单等13个核心实体。
- 已统一主要业务枚举、空值、日期、ID和关联规则。
- 已设计4类角色账号、基础示例数据和8个完整主流程快照。
- 已覆盖14个通用页面状态、17个P0页面场景、20类业务冲突和7类权限场景。
- 已整理20项待后端确认字段和结构问题。
- 本文已通过评审，步骤10：P0高保真页面与可点击交互原型设计已经启动。
