import type {
  ActionEligibility,
  DomainPage,
  DomainPageQuery,
  ServiceRequestOptions,
  StatusSemantic,
} from '@/domains/shared'
import type { RegistrationStatus } from '@/domains/registration'

export type CertificateDisplayStatus = 'VALID' | 'EXPIRING' | 'EXPIRED' | 'UNKNOWN'

const statusLabels: Record<CertificateDisplayStatus, string> = {
  VALID: '有效',
  EXPIRING: '即将到期',
  EXPIRED: '已过期',
  UNKNOWN: '未知状态',
}

const statusSemantics: Record<CertificateDisplayStatus, StatusSemantic> = {
  VALID: 'success',
  EXPIRING: 'warning',
  EXPIRED: 'error',
  UNKNOWN: 'neutral',
}

export function certificateStatusLabel(status: CertificateDisplayStatus): string {
  return statusLabels[status] ?? '未知状态'
}

export function certificateStatusSemantic(status: CertificateDisplayStatus): StatusSemantic {
  return statusSemantics[status] ?? 'neutral'
}

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
  registrationStatus: RegistrationStatus
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
