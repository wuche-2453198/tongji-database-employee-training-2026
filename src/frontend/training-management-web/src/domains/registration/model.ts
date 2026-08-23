import type {
  ActionEligibility,
  DomainPage,
  DomainPageQuery,
  ServiceRequestOptions,
} from '@/domains/shared'

export type RegistrationStatus =
  'REGISTERED' | 'SIGNED_IN' | 'ABSENT' | 'COMPLETED' | 'CANCELED' | 'UNKNOWN'

export interface Registration {
  id: string
  courseId: string
  courseName: string
  employeeId: number
  employeeName: string
  status: RegistrationStatus
  registeredAt: string
  signedInAt: string | null
  completedAt: string | null
  departmentName?: string
  signinMethod?: 'SELF' | 'MANUAL' | 'UNKNOWN'
  attendanceNote?: string | null
  actualHours?: number | null
}

export interface RegistrationQuery extends DomainPageQuery {
  keyword?: string
  employeeKeyword?: string
  departmentName?: string
  courseId?: string
  status?: RegistrationStatus
  startDateFrom?: string
  startDateTo?: string
}

export interface CreateRegistrationCommand {
  courseId: string
}

export interface RegistrationActionEligibility extends ActionEligibility {
  action: 'create' | 'cancel' | 'signin' | 'complete'
}

export interface RegistrationService {
  create(command: CreateRegistrationCommand, options?: ServiceRequestOptions): Promise<Registration>
  getMineByCourse(courseId: string, options?: ServiceRequestOptions): Promise<Registration | null>
  listMine(
    query: RegistrationQuery,
    options?: ServiceRequestOptions,
  ): Promise<DomainPage<Registration>>
  getById(id: string, options?: ServiceRequestOptions): Promise<Registration>
  getActionEligibility(
    id: string,
    options?: ServiceRequestOptions,
  ): Promise<RegistrationActionEligibility[]>
  listManage(
    query: RegistrationQuery,
    options?: ServiceRequestOptions,
  ): Promise<DomainPage<Registration>>
  cancel(id: string, options?: ServiceRequestOptions): Promise<Registration>
  signIn(id: string, options?: ServiceRequestOptions): Promise<Registration>
  markAbsent(id: string, options?: ServiceRequestOptions): Promise<Registration>
  complete(id: string, options?: ServiceRequestOptions): Promise<Registration>
}
