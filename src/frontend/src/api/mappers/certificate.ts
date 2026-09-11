import { mapRegistrationStatus } from '@/api/mappers/registration'
import type { CertificateCandidateDto, CertificateDto } from '@/api/transport'
import type {
  Certificate,
  CertificateCandidate,
  CertificateDisplayStatus,
} from '@/domains/certificate'

const EXPIRING_SOON_MS = 30 * 24 * 60 * 60 * 1000

const knownStatuses = new Set<CertificateDisplayStatus>(['VALID', 'EXPIRING', 'EXPIRED'])

function fromExpireDate(expiresAt: string | null): CertificateDisplayStatus {
  // 未设置到期日视为长期有效，而不是未知状态。
  if (!expiresAt) return 'VALID'
  const expire = new Date(expiresAt).getTime()
  if (Number.isNaN(expire)) return 'UNKNOWN'
  if (expire < Date.now()) return 'EXPIRED'
  return expire <= Date.now() + EXPIRING_SOON_MS ? 'EXPIRING' : 'VALID'
}

/** 优先使用后端下发的状态值，缺失或不可识别时按到期日回退计算。 */
function mapDisplayStatus(
  status: string | null | undefined,
  expiresAt: string | null,
): CertificateDisplayStatus {
  if (status && knownStatuses.has(status as CertificateDisplayStatus)) {
    return status as CertificateDisplayStatus
  }
  return fromExpireDate(expiresAt)
}

export function mapCertificate(dto: CertificateDto): Certificate {
  return {
    id: String(dto.certId),
    certificateNo: dto.certCode,
    courseId: String(dto.courseId),
    courseName: dto.courseName || '未命名课程',
    employeeId: dto.empId,
    employeeName: dto.employeeName || '—',
    issuedAt: dto.issueDate,
    expiresAt: dto.expireDate ?? null,
    displayStatus: mapDisplayStatus(dto.status, dto.expireDate ?? null),
  }
}

export function mapCertificateCandidate(dto: CertificateCandidateDto): CertificateCandidate {
  const allowed = (dto.qualified ?? 'N') === 'Y'
  const reason = dto.qualificationReason?.trim()
  return {
    registrationId: String(dto.regId),
    courseId: String(dto.courseId),
    courseName: dto.courseName || '未命名课程',
    employeeId: dto.empId,
    employeeName: dto.employeeName || '—',
    departmentName: dto.departmentName || '—',
    registrationStatus: mapRegistrationStatus(dto.status ?? ''),
    actualHours: dto.actualHours ?? null,
    qualification: { allowed, reason: reason ? reason : undefined },
  }
}
