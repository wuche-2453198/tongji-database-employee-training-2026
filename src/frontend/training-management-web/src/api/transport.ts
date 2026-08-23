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
  courseId: string
  courseName: string
  courseType: string
  trainerName: string
  startTime: string
  endTime: string
  location: string
  status: string
  maxStudents: number
  registeredCount: number
  remainingSeats: number
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

/** TODO(API-Q-005/006/013): 申请字段和分页包络等待后端 OpenAPI 冻结。 */
export interface TrainingRequestDto {
  requestId: string
  courseId: string
  courseName: string
  employeeId: number
  employeeName: string
  departmentName: string
  reason: string
  status: string
  submittedAt: string
  updatedAt?: string | null
}
