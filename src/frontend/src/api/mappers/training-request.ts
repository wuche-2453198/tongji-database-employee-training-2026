import { fromEnvelopeFailure } from '@/api/error'
import type { ApiEnvelopeDto, PageDto, TrainingRequestDto } from '@/api/transport'
import { domainError } from '@/domains/errors'
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

/** 后端字段 `id` 优先，`requestId` 仅为历史别名；两者都缺失说明契约已变化。 */
const resolveId = (dto: TrainingRequestDto): string => {
  const rawId = dto.id ?? dto.requestId

  if (rawId === undefined || rawId === null || rawId === '') {
    throw domainError('HTTP_CONTRACT_NOT_FROZEN', {
      message: '培训申请响应缺少 id 字段，无法定位申请记录。',
    })
  }

  return String(rawId)
}

export function mapTrainingRequest(dto: TrainingRequestDto): TrainingRequest {
  return {
    id: resolveId(dto),
    courseId: String(dto.courseId),
    courseName: dto.courseName || '未命名课程',
    employeeId: dto.employeeId ?? dto.empId ?? 0,
    employeeName: dto.employeeName || dto.empName || '—',
    departmentName: dto.departmentName || dto.deptName || '—',
    reason: dto.reason || dto.requestReason || '',
    status: mapStatus(dto.status),
    // 后端字段是 createTime；createdAt 只作历史别名兼容。
    submittedAt: dto.submittedAt || dto.createTime || dto.createdAt || '',
    updatedAt: dto.updatedAt ?? null,
    departmentOpinion: dto.deptApproveComment ?? null,
    hrOpinion: dto.hrFileComment ?? dto.hrFilingComment ?? null,
  }
}

export function mapTrainingRequestPage(
  envelope: ApiEnvelopeDto<PageDto<TrainingRequestDto>>,
): DomainPage<TrainingRequest> {
  if (!envelope.success || !envelope.data) throw fromEnvelopeFailure(envelope)
  return { ...envelope.data, items: envelope.data.items.map(mapTrainingRequest) }
}
