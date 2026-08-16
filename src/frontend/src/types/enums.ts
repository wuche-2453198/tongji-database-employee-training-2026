/** 角色代码，与后端 RoleCodes.cs 保持一致 */
export type RoleCode = 'ADMIN' | 'HR' | 'DEPT_MANAGER' | 'EMPLOYEE'

/** 角色显示名称映射 */
export const ROLE_LABELS: Record<RoleCode, string> = {
  ADMIN: '系统管理员',
  HR: 'HR',
  DEPT_MANAGER: '部门主管',
  EMPLOYEE: '员工',
}

/** 课程状态，与后端 CourseStatus.cs 一致 */
export type CourseStatus = 'DRAFT' | 'PUBLISHED' | 'CLOSED'

export const COURSE_STATUS_LABELS: Record<CourseStatus, string> = {
  DRAFT: '草稿',
  PUBLISHED: '已发布',
  CLOSED: '已关闭',
}

/** 申请状态，与后端 TrainingRequestStatus.cs 一致 */
export type TrainingRequestStatus =
  | 'PENDING'
  | 'DEPT_APPROVED'
  | 'DEPT_REJECTED'
  | 'HR_FILED'

export const TRAINING_REQUEST_STATUS_LABELS: Record<TrainingRequestStatus, string> = {
  PENDING: '待主管审批',
  DEPT_APPROVED: '待 HR 备案',
  DEPT_REJECTED: '已驳回',
  HR_FILED: '已备案',
}

/** 报名状态，与后端 RegistrationStatus.cs 一致 */
export type RegistrationStatus =
  | 'REGISTERED'
  | 'SIGNED_IN'
  | 'ABSENT'
  | 'COMPLETED'
  | 'CANCELED'

export const REGISTRATION_STATUS_LABELS: Record<RegistrationStatus, string> = {
  REGISTERED: '已报名',
  SIGNED_IN: '已签到',
  ABSENT: '缺勤',
  COMPLETED: '已完成',
  CANCELED: '已取消',
}

/** 员工状态，与后端 EmployeeStatus.cs 一致 */
export type EmployeeStatus = 'ACTIVE' | 'RESIGNED'

export const EMPLOYEE_STATUS_LABELS: Record<EmployeeStatus, string> = {
  ACTIVE: '在职',
  RESIGNED: '已离职',
}

/** 黑名单状态，与后端 BlacklistStatus.cs 一致 */
export type BlacklistStatus = 'ACTIVE' | 'RELEASED'

export const BLACKLIST_STATUS_LABELS: Record<BlacklistStatus, string> = {
  ACTIVE: '生效中',
  RELEASED: '已解除',
}

/** 测试类型 */
export type TestType = 'PRE' | 'POST'

export const TEST_TYPE_LABELS: Record<TestType, string> = {
  PRE: '训前测试',
  POST: '训后测试',
}
