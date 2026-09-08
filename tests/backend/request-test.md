# 培训申请审批模块测试说明

## 准备

1. 启动后端:`dotnet run --project src/backend/TrainingManagement.Api`(默认 http://localhost:5156)。
2. 在 `tests/api/auth.http` 中分别用 `employee`、`manager`、`rdmgr`、`hr`、`admin` 登录
   (种子密码见 `database/oracle/README.md`),把五个 token 粘贴进 `training-requests.http` 顶部变量。
3. 确认课程 ID:执行
   `SELECT COURSE_ID, COURSE_NAME, COURSE_STATUS, START_AT FROM TRAINING_COURSES ORDER BY COURSE_ID;`
   按注释更新 `@courseDraftId` / `@courseStartedId` / `@courseFutureId`。
4. 先执行 1.4 创建申请,把返回的 `id` 填入 `@requestId`;`@otherDeptRequestId` 取 dev01 的
   HR_FILED 申请(S003 种子生成,可用 hr 登录后查询列表获得)。

## 用例与预期

| 分组 | 覆盖点 |
| --- | --- |
| 1.x 提交校验 | DRAFT 拒绝(400)、课程不存在(404)、已开课拒绝(400)、正常提交(201)、重复提交 409(含 HR_FILED 占用)、缺字段 400 |
| 2.x 查询范围 | 员工仅本人、员工跨人详情 403、员工访问列表 403、主管仅本部门列表、跨部门详情 403、HR/管理员跨部门、非法状态筛选 400 |
| 3.x 主管审批 | 跨部门审批 403、正常审批 200、重复审批 400、状态机禁止驳回已批准、不存在 404、管理员代办 200 |
| 4.x HR 备案 | 跳步备案 400、备案含意见 200(HR_FILE_REMARK 落库)、重复备案 400、备案后重复申请 409 |
| 5.x 认证 | 无 Token 401 |

## 验收对照(REQ-01~09)

- REQ-01/02/03:`2.x` 数据范围用例。
- REQ-04:`1.x` 提交校验(在职/已发布/未开始/无活跃申请)。
- REQ-05/06:`3.x` 审批与越权(角色 + 部门范围,管理员代办)。
- REQ-07:`4.2` 备案记录 HR 员工 ID 与备案意见。
- REQ-08:`3.3`/`3.4`/`4.1` 状态机禁止跳步与重复审批。
- REQ-09(按状态统计接口)与对报名模块的 HR_FILED 资格校验 Service 契约:挂账,待后续提交。
