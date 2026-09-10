import type {
  ActionEligibility,
  DomainPage,
  DomainPageQuery,
  ServiceRequestOptions,
} from '@/domains/shared'

export type RegistrationStatus =
  'REGISTERED' | 'SIGNED_IN' | 'ABSENT' | 'COMPLETED' | 'CANCELED' | 'UNKNOWN'

export type SigninMethod = 'SELF' | 'MANUAL' | 'UNKNOWN'

export interface RegistrationActions {
  cancel: ActionEligibility
  signIn: ActionEligibility
  complete: ActionEligibility
  markAbsent: ActionEligibility
}

export interface Registration {
  id: string
  requestId?: string
  courseId: string
  courseName: string
  courseType?: string
  durationHours?: number
  trainerName?: string | null
  startAt?: string | null
  endAt?: string | null
  location?: string | null
  courseStatus?: string
  maxStudents?: number
  employeeId: number
  employeeName: string
  departmentId?: number
  departmentName?: string | null
  status: RegistrationStatus
  registeredAt: string
  signedInAt: string | null
  completedAt: string | null
  actualHours?: number | null
  canceledAt?: string | null
  cancelReason?: string | null
  signinMethod?: SigninMethod
  attendanceNote?: string | null
  latenessMinutes?: number | null
  deductHours?: number | null
  actions?: RegistrationActions
}

export interface Attendance {
  attendId: number
  regId: string
  signinType: string
  signedInAt: string
  latenessMinutes: number
  deductHours: number
  remark: string | null
  createdAt: string
}

export interface RegistrationSummary {
  total: number
  registered: number
  signedIn: number
  absent: number
  completed: number
  canceled: number
  remainingSeats: number | null
}

export interface RegistrationQuery extends DomainPageQuery {
  keyword?: string
  employeeKeyword?: string
  courseId?: string
  status?: RegistrationStatus
  startDateFrom?: string
  startDateTo?: string
}

export interface CreateRegistrationCommand {
  courseId: string
}

export interface ManualSignInCommand {
  regId: string
  signinTime?: string
  remark: string
}

export interface RegistrationService {
  create(command: CreateRegistrationCommand, options?: ServiceRequestOptions): Promise<Registration>
  getMineByCourse(courseId: string, options?: ServiceRequestOptions): Promise<Registration | null>
  listMine(
    query: RegistrationQuery,
    options?: ServiceRequestOptions,
  ): Promise<DomainPage<Registration>>
  getById(id: string, options?: ServiceRequestOptions): Promise<Registration>
  listManage(
    query: RegistrationQuery,
    options?: ServiceRequestOptions,
  ): Promise<DomainPage<Registration>>
  summary(courseId?: string, options?: ServiceRequestOptions): Promise<RegistrationSummary>
  cancel(id: string, options?: ServiceRequestOptions): Promise<Registration>
  signIn(id: string, options?: ServiceRequestOptions): Promise<Attendance>
  manualSignIn(command: ManualSignInCommand, options?: ServiceRequestOptions): Promise<Attendance>
  markAbsent(id: string, options?: ServiceRequestOptions): Promise<Registration>
  complete(id: string, options?: ServiceRequestOptions): Promise<Registration>
}
