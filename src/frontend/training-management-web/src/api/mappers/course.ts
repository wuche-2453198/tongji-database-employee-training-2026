import { fromEnvelopeFailure } from '@/api/error'
import type {
  ApiEnvelopeDto,
  CourseSummaryDto,
  PageDto,
  RegistrationReceiptDto,
} from '@/api/transport'
import type { PageResult } from '@/types/api'
import type { CourseStatus, CourseSummary, CourseType, RegistrationReceipt } from '@/types/course'

const courseTypeLabels: Record<CourseType, string> = {
  SKILL: '技能培训',
  MANAGEMENT: '管理培训',
  SAFETY: '安全培训',
  UNKNOWN: '未知类型',
}

const courseStatusLabels: Record<CourseStatus, string> = {
  DRAFT: '草稿',
  PUBLISHED: '已发布',
  CLOSED: '已关闭',
  UNKNOWN: '未知状态',
}

const knownTypes = new Set<CourseType>(['SKILL', 'MANAGEMENT', 'SAFETY'])
const knownStatuses = new Set<CourseStatus>(['DRAFT', 'PUBLISHED', 'CLOSED'])

const mapCourseType = (value: string): CourseType =>
  knownTypes.has(value as CourseType) ? (value as CourseType) : 'UNKNOWN'

const mapCourseStatus = (value: string): CourseStatus =>
  knownStatuses.has(value as CourseStatus) ? (value as CourseStatus) : 'UNKNOWN'

export function mapCourseSummary(dto: CourseSummaryDto): CourseSummary {
  const type = mapCourseType(dto.courseType)
  const status = mapCourseStatus(dto.status)

  return {
    id: dto.courseId,
    name: dto.courseName,
    type,
    typeLabel: courseTypeLabels[type],
    trainerName: dto.trainerName,
    startTime: dto.startTime,
    endTime: dto.endTime,
    location: dto.location,
    status,
    statusLabel: courseStatusLabels[status],
    maxStudents: dto.maxStudents,
    registeredCount: dto.registeredCount,
    remainingSeats: dto.remainingSeats,
  }
}

export function mapCoursePage(
  envelope: ApiEnvelopeDto<PageDto<CourseSummaryDto>>,
): PageResult<CourseSummary> {
  if (!envelope.success || !envelope.data) throw fromEnvelopeFailure(envelope)
  return { ...envelope.data, items: envelope.data.items.map(mapCourseSummary) }
}

export function mapRegistrationReceipt(
  envelope: ApiEnvelopeDto<RegistrationReceiptDto>,
): RegistrationReceipt {
  if (!envelope.success || !envelope.data) throw fromEnvelopeFailure(envelope)
  return {
    registrationId: envelope.data.regId,
    courseId: envelope.data.courseId,
    status: 'REGISTERED',
    registeredAt: envelope.data.regDate,
  }
}
