import { fromEnvelopeFailure } from '@/api/error'
import type { ApiEnvelopeDto, PageDto, TrainingRequestDto } from '@/api/transport'
import type { DomainPage } from '@/domains/shared'
import type { TrainingRequest, TrainingRequestStatus } from '@/domains/training-request'

const knownStatuses = new Set<TrainingRequestStatus>([
  'PENDING',
  'DEPT_APPROVED',
  'DEPT_REJECTED',
  'HR_FILED',
])

const mapStatus = (value: string): TrainingRequestStatus =>
  knownStatuses.has(value as TrainingRequestStatus) ? (value as TrainingRequestStatus) : 'UNKNOWN'

export function mapTrainingRequest(dto: TrainingRequestDto): TrainingRequest {
  return {
    id: String(dto.requestId),
    courseId: String(dto.courseId),
    courseName: dto.courseName || '未命名课程',
    employeeId: dto.employeeId ?? dto.empId ?? 0,
    employeeName: dto.employeeName || dto.empName || '—',
    departmentName: dto.departmentName || dto.deptName || '—',
    reason: dto.reason || dto.requestReason || '',
    status: mapStatus(dto.status),
    submittedAt: dto.submittedAt || dto.createdAt || '',
    updatedAt: dto.updatedAt ?? null,
    departmentOpinion: dto.deptApproveComment ?? null,
    hrOpinion: dto.hrFilingComment ?? null,
  }
}

export function mapTrainingRequestPage(
  envelope: ApiEnvelopeDto<PageDto<TrainingRequestDto>>,
): DomainPage<TrainingRequest> {
  if (!envelope.success || !envelope.data) throw fromEnvelopeFailure(envelope)
  return { ...envelope.data, items: envelope.data.items.map(mapTrainingRequest) }
}
