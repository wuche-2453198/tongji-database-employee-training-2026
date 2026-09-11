import { fromEnvelopeFailure } from '@/api/error'
import type { ApiEnvelopeDto, CourseDetailDto, CourseSummaryDto, PageDto } from '@/api/transport'
import type { PageResult } from '@/types/api'
import type {
  CourseActionEligibility,
  CourseDetail,
  CourseStatus,
  CourseSummary,
  CourseType,
} from '@/types/course'

const courseTypeLabels: Record<CourseType, string> = {
  技术培训: '技术培训',
  管理培训: '管理培训',
  产品培训: '产品培训',
  营销培训: '营销培训',
  UNKNOWN: '未知类型',
}

const courseStatusLabels: Record<CourseStatus, string> = {
  DRAFT: '草稿',
  PUBLISHED: '已发布',
  CLOSED: '已关闭',
  UNKNOWN: '未知状态',
}

const knownTypes = new Set<CourseType>(['技术培训', '管理培训', '产品培训', '营销培训'])
const knownStatuses = new Set<CourseStatus>(['DRAFT', 'PUBLISHED', 'CLOSED'])

/** 兼容历史英文枚举，避免后端过渡期返回旧值时被判为 UNKNOWN。 */
const legacyTypeAliases: Record<string, CourseType> = {
  SKILL: '技术培训',
  MANAGEMENT: '管理培训',
  PRODUCT: '产品培训',
  MARKETING: '营销培训',
}

const mapCourseType = (value: string): CourseType =>
  knownTypes.has(value as CourseType)
    ? (value as CourseType)
    : (legacyTypeAliases[value] ?? 'UNKNOWN')

const mapCourseStatus = (value: string): CourseStatus =>
  knownStatuses.has(value as CourseStatus) ? (value as CourseStatus) : 'UNKNOWN'

const toNullableNumber = (value: unknown): number | null =>
  typeof value === 'number' && Number.isFinite(value) ? value : null

export function mapCourseSummary(dto: CourseSummaryDto): CourseSummary {
  const type = mapCourseType(dto.courseType)
  const status = mapCourseStatus(dto.status || dto.courseStatus || '')
  const registeredCount = toNullableNumber(dto.registeredCount ?? dto.enrolledCount)
  const maxStudents = toNullableNumber(dto.maxStudents)
  const remainingSeats =
    toNullableNumber(dto.remainingSeats) ??
    (registeredCount !== null && maxStudents !== null ? maxStudents - registeredCount : null)

  return {
    id: String(dto.courseId),
    name: dto.courseName,
    type,
    typeLabel: courseTypeLabels[type],
    trainerName: dto.trainerName || '—',
    startTime: dto.startTime || dto.startAt || '',
    endTime: dto.endTime || dto.endAt || '',
    location: dto.location || '—',
    status,
    statusLabel: courseStatusLabels[status],
    maxStudents: maxStudents ?? 0,
    registeredCount,
    remainingSeats,
  }
}

/**
 * 后端当前只返回课程状态与剩余名额，没有 eligibility 字段。
 * 在既有字段上派生最小可用资格，最终裁决仍由写接口（申请/报名）负责。
 */
const deriveEligibility = (
  summary: CourseSummary,
  dto: CourseDetailDto,
): CourseActionEligibility => {
  const published = summary.status === 'PUBLISHED'
  const startAt = Date.parse(summary.startTime || dto.startAt || '')
  const notStarted = Number.isFinite(startAt) ? startAt > Date.now() : false
  // 名额未知（后端未返回容量）时不阻止操作，由写接口做最终校验。
  const hasSeats = summary.remainingSeats === null || summary.remainingSeats > 0

  if (!published) {
    const reason = summary.status === 'CLOSED' ? '课程已关闭，不能申请或报名。' : '课程尚未发布。'
    return { apply: { allowed: false, reason }, register: { allowed: false, reason } }
  }

  if (!notStarted) {
    return {
      apply: { allowed: false, reason: '课程已开始，不能申请或报名。' },
      register: { allowed: false, reason: '课程已开始，不能申请或报名。' },
    }
  }

  return {
    apply: { allowed: true },
    register: hasSeats
      ? { allowed: true }
      : { allowed: false, reason: '课程名额已满，请等待名额释放。' },
  }
}

export function mapCourseDetail(envelope: ApiEnvelopeDto<CourseDetailDto>): CourseDetail {
  if (!envelope.success || !envelope.data) throw fromEnvelopeFailure(envelope)
  const summary = mapCourseSummary(envelope.data)
  const backendEligibility = envelope.data.eligibility
  const derived = deriveEligibility(summary, envelope.data)
  const eligibility: CourseActionEligibility = backendEligibility
    ? {
        apply: {
          allowed: backendEligibility.apply?.allowed === true,
          reasonCode: backendEligibility.apply?.reasonCode,
          reason: backendEligibility.apply?.reason,
        },
        register: {
          allowed: backendEligibility.register?.allowed === true,
          reasonCode: backendEligibility.register?.reasonCode,
          reason: backendEligibility.register?.reason,
        },
      }
    : derived
  return {
    ...summary,
    description: envelope.data.description || '暂无课程介绍。',
    objectives: envelope.data.objectives || [],
    hours: envelope.data.hours ?? envelope.data.durationHours ?? null,
    organizer: envelope.data.organizer || envelope.data.deptName || '—',
    trainer: {
      id: envelope.data.trainer?.id || String(envelope.data.trainerId ?? 'UNKNOWN'),
      name: envelope.data.trainer?.name || summary.trainerName,
      title: envelope.data.trainer?.title || '—',
      department: envelope.data.trainer?.department || '—',
      expertise: envelope.data.trainer?.expertise || '—',
      rating: envelope.data.trainer?.rating,
    },
    materials: (envelope.data.materials || []).map((material, index) => ({
      id: material.id || `material-${index}`,
      name: material.name || '未命名资料',
      kind: material.kind === 'DOCUMENT' || material.kind === 'LINK' ? material.kind : 'UNKNOWN',
      available: material.available === true,
    })),
    eligibility,
    trainerId: envelope.data.trainerId ? String(envelope.data.trainerId) : undefined,
    deptId: envelope.data.deptId ? String(envelope.data.deptId) : undefined,
    budgetAmount: envelope.data.budgetAmount ?? undefined,
    preTestUrl: envelope.data.preTestUrl ?? null,
    postTestUrl: envelope.data.postTestUrl ?? null,
    materialUrl: envelope.data.materialUrl ?? null,
  }
}

export function mapCoursePage(
  envelope: ApiEnvelopeDto<PageDto<CourseSummaryDto>>,
): PageResult<CourseSummary> {
  if (!envelope.success || !envelope.data) throw fromEnvelopeFailure(envelope)
  return { ...envelope.data, items: envelope.data.items.map(mapCourseSummary) }
}
