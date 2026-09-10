/** 暂定传输结构：只允许在 api、mocks 与 HTTP 适配器内使用。 */
export interface FieldErrorDto {
  field?: string
  message?: string
  code?: string
}

/** 最终结构等待 OpenAPI 冻结，页面不得依赖本类型。 */
export interface ApiEnvelopeDto<T> {
  success: boolean
  message: string
  data: T | null
  errors?: FieldErrorDto[]
  traceId?: string
  code?: string
}

export interface PageDto<T> {
  items: T[]
  page: number
  pageSize: number
  total: number
}

export interface CourseSummaryDto {
  courseId: string | number
  courseName: string
  courseType: string
  trainerName: string | null
  startTime?: string | null
  startAt?: string | null
  endTime?: string | null
  endAt?: string | null
  location: string | null
  status?: string
  courseStatus?: string
  maxStudents: number
  registeredCount?: number
  enrolledCount?: number
  remainingSeats?: number
  durationHours?: number | null
  deptName?: string | null
}

/** TODO(API-Q-009): 课程详情、讲师和 ActionEligibility 字段等待 OpenAPI 冻结。 */
export interface CourseDetailDto extends CourseSummaryDto {
  description?: string
  objectives?: string[]
  hours?: number | null
  organizer?: string
  trainer?: {
    id?: string
    name?: string
    title?: string
    department?: string
    expertise?: string
    rating?: number
  }
  materials?: Array<{ id?: string; name?: string; kind?: string; available?: boolean }>
  eligibility?: {
    apply?: { allowed?: boolean; reasonCode?: string; reason?: string }
    register?: { allowed?: boolean; reasonCode?: string; reason?: string }
  }
}

export interface RegistrationReceiptDto {
  regId: string
  courseId: string
  status: string
  regDate: string
}

/** 培训申请后端 DTO；仅 transport、mapper 与 HTTP 适配器可使用。 */
export interface TrainingRequestDto {
  id: string | number
  employeeId: number
  employeeName?: string | null
  deptId?: number | null
  courseId: string | number
  courseName?: string | null
  requestReason?: string | null
  status?: string | null
  createTime?: string | null
  deptApproveComment?: string | null
  hrFileComment?: string | null
}
