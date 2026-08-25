import type {
  ActionEligibility,
  DomainPage,
  DomainPageQuery,
  ServiceRequestOptions,
} from '@/domains/shared'

export type CertificateDisplayStatus = 'VALID' | 'EXPIRING' | 'EXPIRED' | 'UNKNOWN'

export interface Certificate {
  id: string
  certificateNo: string
  courseId: string
  courseName: string
  employeeId: number
  employeeName: string
  issuedAt: string
  expiresAt: string | null
  displayStatus: CertificateDisplayStatus
}

export interface CertificateCandidate {
  registrationId: string
  courseId: string
  courseName: string
  employeeId: number
  employeeName: string
  departmentName: string
  registrationStatus: 'COMPLETED'
  actualHours: number | null
  qualification: ActionEligibility
}

export interface CertificateQuery extends DomainPageQuery {
  keyword?: string
  employeeKeyword?: string
  departmentName?: string
  status?: CertificateDisplayStatus
  startDateFrom?: string
  startDateTo?: string
}

export interface GenerateCertificateCommand {
  registrationId: string
}

export interface CertificateActionEligibility extends ActionEligibility {
  action: 'generate'
}

export interface CertificateService {
  listMine(
    query: CertificateQuery,
    options?: ServiceRequestOptions,
  ): Promise<DomainPage<Certificate>>
  getById(id: string, options?: ServiceRequestOptions): Promise<Certificate>
  generate(
    command: GenerateCertificateCommand,
    options?: ServiceRequestOptions,
  ): Promise<Certificate>
  getActionEligibility(
    id: string,
    options?: ServiceRequestOptions,
  ): Promise<CertificateActionEligibility>
  listManage(
    query: CertificateQuery,
    options?: ServiceRequestOptions,
  ): Promise<DomainPage<Certificate>>
  listCandidates(
    query: CertificateQuery,
    options?: ServiceRequestOptions,
  ): Promise<DomainPage<CertificateCandidate>>
}
