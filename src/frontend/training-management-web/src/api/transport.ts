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

export interface RegistrationReceiptDto {
  regId: string
  courseId: string
  status: string
  regDate: string
}
