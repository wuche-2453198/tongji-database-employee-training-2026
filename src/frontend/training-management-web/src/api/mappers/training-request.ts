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
    id: dto.requestId,
    courseId: dto.courseId,
    courseName: dto.courseName || '未命名课程',
    employeeId: dto.employeeId,
    employeeName: dto.employeeName || '—',
    departmentName: dto.departmentName || '—',
    reason: dto.reason || '',
    status: mapStatus(dto.status),
    submittedAt: dto.submittedAt,
    updatedAt: dto.updatedAt ?? null,
  }
}

export function mapTrainingRequestPage(
  envelope: ApiEnvelopeDto<PageDto<TrainingRequestDto>>,
): DomainPage<TrainingRequest> {
  if (!envelope.success || !envelope.data) throw fromEnvelopeFailure(envelope)
  return { ...envelope.data, items: envelope.data.items.map(mapTrainingRequest) }
}
