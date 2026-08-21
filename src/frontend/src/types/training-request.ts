import type { TrainingRequestStatus } from './enums'

/** 提交培训申请 */
export interface CreateTrainingRequest {
  courseId: number
  reason: string
  expectedGain?: string
}

/** 我的申请列表项 */
export interface TrainingRequestItem {
  requestId: number
  courseId: number
  courseName: string
  courseType: string | null
  trainerName: string | null
  reason: string
  expectedGain: string | null
  status: TrainingRequestStatus
  createdAt: string
  updatedAt: string
}

/** 申请处理时间线节点 */
export interface TrainingRequestTimelineEntry {
  title: string
  description: string | null
  time: string
  type: 'primary' | 'success' | 'danger' | 'warning' | 'info'
}

/** 申请详情（含审批/备案信息与时间线） */
export interface TrainingRequestDetail extends TrainingRequestItem {
  reviewerName: string | null
  reviewComment: string | null
  filedByName: string | null
  filingComment: string | null
  timeline: TrainingRequestTimelineEntry[]
}

/** 申请列表查询参数 */
export interface TrainingRequestQuery {
  status?: TrainingRequestStatus
  keyword?: string
  startDate?: string
  endDate?: string
  page?: number
  pageSize?: number
}

/** 当前用户对某课程的申请状态 */
export interface CourseRequestStatus {
  applied: boolean
  requestId: number | null
  status: TrainingRequestStatus | null
}

/** 审批/备案列表项（面向 DEPT_MANAGER 与 HR） */
export interface TrainingRequestApprovalItem {
  requestId: number
  employeeId: number
  employeeName: string
  deptName: string | null
  courseId: number
  courseName: string
  courseType: string | null
  reason: string
  status: TrainingRequestStatus
  createdAt: string
  budgetAmount: number | null
  maxStudents: number | null
  deptApproveComment: string | null
  deptApproveTime: string | null
  hrFileTime: string | null
}

/** 审批/备案列表查询参数 */
export interface TrainingRequestApprovalQuery {
  status?: TrainingRequestStatus
  page?: number
  pageSize?: number
}
