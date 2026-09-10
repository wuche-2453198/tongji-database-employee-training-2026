import type { CertificateCandidateDto, CertificateDto } from '@/api/transport'
import type {
  Certificate,
  CertificateCandidate,
  CertificateDisplayStatus,
} from '@/domains/certificate'

const EXPIRING_SOON_MS = 30 * 24 * 60 * 60 * 1000

function mapDisplayStatus(expiresAt: string | null): CertificateDisplayStatus {
  if (!expiresAt) return 'UNKNOWN'
  const expire = new Date(expiresAt).getTime()
  if (Number.isNaN(expire)) return 'UNKNOWN'
  if (expire < Date.now()) return 'EXPIRED'
  return expire <= Date.now() + EXPIRING_SOON_MS ? 'EXPIRING' : 'VALID'
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
    displayStatus: mapDisplayStatus(dto.expireDate),
  }
}

export function mapCertificateCandidate(dto: CertificateCandidateDto): CertificateCandidate {
  return {
    registrationId: String(dto.regId),
    courseId: String(dto.courseId),
    courseName: dto.courseName || '未命名课程',
    employeeId: dto.empId,
    employeeName: dto.employeeName || '—',
    departmentName: dto.departmentName || '—',
    registrationStatus: 'COMPLETED',
    actualHours: dto.actualHours ?? null,
    qualification: { allowed: true },
  }
}
