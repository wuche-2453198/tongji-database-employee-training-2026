import { fromEnvelopeFailure } from '@/api/error'
import type {
  ApiEnvelopeDto,
  AttendanceDto,
  PageDto,
  RegistrationDto,
  RegistrationSummaryDto,
} from '@/api/transport'
import type { PageResult } from '@/types/api'
import type {
  Attendance,
  Registration,
  RegistrationActions,
  RegistrationStatus,
  RegistrationSummary,
  SigninMethod,
} from '@/domains/registration'

const knownStatuses = new Set<RegistrationStatus>([
  'REGISTERED',
  'SIGNED_IN',
  'ABSENT',
  'COMPLETED',
  'CANCELED',
])

/** 报名状态映射：后端状态值与前端枚举保持一致，未知值回落到 UNKNOWN。 */
export const mapRegistrationStatus = (value: string): RegistrationStatus =>
  knownStatuses.has(value as RegistrationStatus) ? (value as RegistrationStatus) : 'UNKNOWN'

const mapStatus = mapRegistrationStatus

const mapSigninMethod = (value: string | null | undefined): SigninMethod => {
  if (value === 'SCAN') return 'SELF'
  if (value === 'MANUAL') return 'MANUAL'
  return 'UNKNOWN'
}

const mapActions = (actions: RegistrationDto['actions']): RegistrationActions => ({
  cancel: { allowed: actions.cancel.allowed, reason: actions.cancel.reason ?? undefined },
  signIn: { allowed: actions.signIn.allowed, reason: actions.signIn.reason ?? undefined },
  complete: { allowed: actions.complete.allowed, reason: actions.complete.reason ?? undefined },
  markAbsent: {
    allowed: actions.markAbsent.allowed,
    reason: actions.markAbsent.reason ?? undefined,
  },
})

export function mapRegistration(dto: RegistrationDto): Registration {
  const attendance = dto.attendance ?? null
  return {
    id: String(dto.regId),
    requestId: String(dto.requestId),
    courseId: String(dto.courseId),
    courseName: dto.courseName,
    courseType: dto.courseType,
    durationHours: dto.durationHours,
    trainerName: dto.trainerName ?? null,
    startAt: dto.startAt ?? null,
    endAt: dto.endAt ?? null,
    location: dto.location ?? null,
    courseStatus: dto.courseStatus,
    maxStudents: dto.maxStudents,
    employeeId: dto.empId,
    employeeName: dto.empName,
    departmentId: dto.deptId,
    departmentName: dto.deptName || null,
    status: mapStatus(dto.status),
    registeredAt: dto.registeredAt,
    completedAt: dto.completedAt ?? null,
    actualHours: dto.actualHours ?? null,
    canceledAt: dto.canceledAt ?? null,
    cancelReason: dto.cancelReason ?? null,
    signedInAt: attendance?.signedInAt ?? null,
    signinMethod: mapSigninMethod(attendance?.signinType),
    attendanceNote: attendance?.remark ?? null,
    latenessMinutes: attendance?.latenessMinutes ?? null,
    deductHours: attendance?.deductHours ?? null,
    actions: mapActions(dto.actions),
  }
}

export function mapRegistrationPage(
  envelope: ApiEnvelopeDto<PageDto<RegistrationDto>>,
): PageResult<Registration> {
  if (!envelope.success || !envelope.data) throw fromEnvelopeFailure(envelope)
  return { ...envelope.data, items: envelope.data.items.map(mapRegistration) }
}

export function mapRegistrationSummary(
  envelope: ApiEnvelopeDto<RegistrationSummaryDto>,
): RegistrationSummary {
  if (!envelope.success || !envelope.data) throw fromEnvelopeFailure(envelope)
  return {
    total: envelope.data.total,
    registered: envelope.data.registered,
    signedIn: envelope.data.signedIn,
    absent: envelope.data.absent,
    completed: envelope.data.completed,
    canceled: envelope.data.canceled,
    remainingSeats: envelope.data.remainingSeats ?? null,
  }
}

export function mapAttendance(dto: AttendanceDto): Attendance {
  return {
    attendId: dto.attendId,
    regId: String(dto.regId),
    signinType: dto.signinType,
    signedInAt: dto.signedInAt,
    latenessMinutes: dto.latenessMinutes,
    deductHours: dto.deductHours,
    remark: dto.remark ?? null,
    createdAt: dto.createdAt,
  }
}
