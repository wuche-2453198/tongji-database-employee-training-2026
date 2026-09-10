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

export interface AttendanceDto {
  attendId: number
  regId: number
  signinType: string
  signedInAt: string
  latenessMinutes: number
  deductHours: number
  remark?: string | null
  createdAt: string
}

export interface RegistrationActionDto {
  allowed: boolean
  reason?: string | null
}

export interface RegistrationActionsDto {
  cancel: RegistrationActionDto
  signIn: RegistrationActionDto
  complete: RegistrationActionDto
  markAbsent: RegistrationActionDto
}

export interface RegistrationDto {
  regId: number
  requestId: number
  empId: number
  empName: string
  deptId: number
  deptName: string
  courseId: number
  courseName: string
  courseType: string
  durationHours: number
  trainerName?: string | null
  startAt?: string | null
  endAt?: string | null
  location?: string | null
  courseStatus: string
  maxStudents: number
  status: string
  registeredAt: string
  completedAt?: string | null
  actualHours?: number | null
  canceledAt?: string | null
  cancelReason?: string | null
  attendance?: AttendanceDto | null
  actions: RegistrationActionsDto
}

export interface RegistrationSummaryDto {
  total: number
  registered: number
  signedIn: number
  absent: number
  completed: number
  canceled: number
  remainingSeats?: number | null
}

/** TODO(API-Q-005/006/013): 申请字段和分页包络等待后端 OpenAPI 冻结。 */
export interface TrainingRequestDto {
  id?: string | number
  requestId?: string | number
  courseId: string | number
  courseName?: string | null
  employeeId?: number
  empId?: number
  employeeName?: string | null
  empName?: string | null
  departmentName?: string | null
  deptName?: string | null
  reason?: string | null
  requestReason?: string | null
  status: string
  submittedAt?: string
  createdAt?: string
  createTime?: string
  updatedAt?: string | null
  deptApproveComment?: string | null
  hrFileComment?: string | null
  hrFilingComment?: string | null
}

/** 后端 TrainingCertificate 实体(camelCase 序列化)。 */
export interface CertificateDto {
  certId: number
  empId: number
  courseId: number
  certCode: string
  issueDate: string
  expireDate: string | null
  notified: string
  notifiedAt: string | null
  issuedByEmpId: number
  createdAt: string
  courseName?: string | null
  employeeName?: string | null
}

/** 后端 CertificateCandidate 实体(camelCase 序列化)。 */
export interface CertificateCandidateDto {
  regId: number
  empId: number
  employeeName: string | null
  departmentName: string | null
  courseId: number
  courseName: string | null
  actualHours: number | null
}

/** 后端 TrainerRating 实体(camelCase 序列化)。 */
export interface TrainerRatingDto {
  ratingId: number
  courseId: number
  trainerId: number
  empId: number
  score: number
  ratingComment: string | null
  hrVerified: string
  hrVerifierEmpId: number | null
  verifiedAt: string | null
  hrComment: string | null
  ratedAt: string
  courseName?: string | null
  trainerName?: string | null
  employeeName?: string | null
}

/** 后端 TrainingTest 实体(camelCase 序列化)。 */
export interface TrainingTestDto {
  testId: number
  empId: number
  courseId: number
  testType: string
  score: number
  recordedByEmpId: number
  testedAt: string
  employeeName?: string | null
  courseName?: string | null
}
