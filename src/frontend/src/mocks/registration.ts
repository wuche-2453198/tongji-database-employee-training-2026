import type { Registration, RegistrationQuery, RegistrationService } from '@/domains/registration'
import { domainError } from '@/domains/errors'
import { getMockActor, mockBusinessRepository } from '@/mocks/repositories/business-repository'
import { createMockError, currentMockScenario, mockWait } from '@/mocks/scenarios'
import type { MockScenario } from '@/mocks/scenarios'

const PAGE_SIZES = [10, 20, 50]

function canManage(): boolean {
  const role = getMockActor().role
  return role === 'HR' || role === 'ADMIN'
}

function canRead(registration: Registration): boolean {
  const actor = getMockActor()
  if (actor.role === 'HR' || actor.role === 'ADMIN') return true
  return actor.role === 'EMPLOYEE' && registration.employeeId === actor.employeeId
}

function findRegistration(id: string): Registration {
  const item = mockBusinessRepository.getState().registrations.find((entry) => entry.id === id)
  if (!item) throw domainError('REGISTRATION_NOT_FOUND')
  if (!canRead(item)) throw domainError('REGISTRATION_FORBIDDEN')
  return item
}

function throwQueryScenario(scenario: MockScenario): void {
  if (scenario === 'failure' || scenario === 'forbidden' || scenario === 'not-found')
    throw createMockError(scenario)
}

function pageOf<T>(items: T[], query: RegistrationQuery) {
  const page = Number.isInteger(query.page) && query.page > 0 ? query.page : 1
  const pageSize = PAGE_SIZES.includes(query.pageSize) ? query.pageSize : 20
  const start = (page - 1) * pageSize
  return { items: items.slice(start, start + pageSize), page, pageSize, total: items.length }
}

function filterItems(query: RegistrationQuery, manage: boolean): Registration[] {
  const actor = getMockActor()
  let items = mockBusinessRepository
    .getState()
    .registrations.filter((item) => (manage ? true : item.employeeId === actor.employeeId))
  const keyword = query.keyword?.trim().toLowerCase()
  const employeeKeyword = query.employeeKeyword?.trim().toLowerCase()
  if (keyword) items = items.filter((item) => item.courseName.toLowerCase().includes(keyword))
  if (employeeKeyword)
    items = items.filter(
      (item) =>
        item.employeeName.toLowerCase().includes(employeeKeyword) ||
        String(item.employeeId).includes(employeeKeyword),
    )
  if (query.departmentName)
    items = items.filter((item) => item.departmentName === query.departmentName)
  if (query.courseId) items = items.filter((item) => item.courseId === query.courseId)
  if (query.status) items = items.filter((item) => item.status === query.status)
  if (query.startDateFrom)
    items = items.filter((item) => item.registeredAt.slice(0, 10) >= query.startDateFrom!)
  if (query.startDateTo)
    items = items.filter((item) => item.registeredAt.slice(0, 10) <= query.startDateTo!)
  return items.sort((left, right) => right.registeredAt.localeCompare(left.registeredAt))
}

export function createMockRegistrationService(
  getScenario: () => MockScenario = currentMockScenario,
): RegistrationService {
  const update = async (
    id: string,
    transition: (registration: Registration) => void,
    options?: { signal?: AbortSignal },
  ): Promise<Registration> => {
    await mockWait(options?.signal)
    const scenario = getScenario()
    const current = findRegistration(id)
    const next = { ...current }
    transition(next)
    if (scenario === 'conflict') throw createMockError('conflict')
    mockBusinessRepository.update((state) => {
      const index = state.registrations.findIndex((entry) => entry.id === id)
      if (index >= 0) state.registrations[index] = next
    })
    if (scenario === 'result-unknown') throw createMockError('result-unknown')
    return next
  }

  return {
    async create(command, options) {
      await mockWait(options?.signal)
      const scenario = getScenario()
      const actor = getMockActor()
      if (scenario === 'conflict') throw createMockError('conflict')
      if (actor.role !== 'EMPLOYEE')
        throw domainError('REGISTRATION_NOT_ELIGIBLE', { message: '当前角色仅支持浏览课程。' })
      const state = mockBusinessRepository.getState()
      const course = state.courses.find((item) => item.id === command.courseId)
      if (!course) throw domainError('COURSE_NOT_FOUND')
      const request = state.requests.find(
        (item) => item.courseId === command.courseId && item.employeeId === actor.employeeId,
      )
      if (scenario === 'result-unknown' && request?.status !== 'HR_FILED')
        throw createMockError('result-unknown')
      const existing = state.registrations.find(
        (item) =>
          item.courseId === command.courseId &&
          item.employeeId === actor.employeeId &&
          item.status !== 'CANCELED',
      )
      if (existing) throw domainError('REGISTRATION_EXISTS')
      if (request?.status !== 'HR_FILED')
        throw domainError('REGISTRATION_NOT_ELIGIBLE', { message: '申请尚未完成 HR 备案。' })
      if ((course.remainingSeats ?? 0) <= 0) throw domainError('COURSE_FULL')
      const registration: Registration = {
        id: `600${state.registrations.length + 1}`,
        courseId: course.id,
        courseName: course.name,
        employeeId: actor.employeeId,
        employeeName: '张三',
        status: 'REGISTERED',
        registeredAt: '2026-08-22T10:00:00+08:00',
        signedInAt: null,
        completedAt: null,
        departmentName: actor.departmentName,
        signinMethod: 'UNKNOWN',
        attendanceNote: null,
        actualHours: null,
      }
      if (scenario === 'result-unknown') throw createMockError('result-unknown')
      mockBusinessRepository.update((next) => {
        next.registrations.push(registration)
        const nextCourse = next.courses.find((item) => item.id === course.id)
        if (nextCourse) {
          nextCourse.registeredCount = (nextCourse.registeredCount ?? 0) + 1
          nextCourse.remainingSeats = (nextCourse.remainingSeats ?? 0) - 1
        }
      })
      return registration
    },

    async getMineByCourse(courseId, options) {
      await mockWait(options?.signal, 50)
      const actor = getMockActor()
      return (
        mockBusinessRepository
          .getState()
          .registrations.find(
            (item) =>
              item.courseId === courseId &&
              item.employeeId === actor.employeeId &&
              item.status !== 'CANCELED',
          ) ?? null
      )
    },

    async listMine(query, options) {
      await mockWait(options?.signal, 50)
      throwQueryScenario(getScenario())
      return pageOf(filterItems(query, false), query)
    },

    async getById(id, options) {
      await mockWait(options?.signal, 50)
      throwQueryScenario(getScenario())
      return findRegistration(id)
    },

    async getActionEligibility(id, options) {
      await mockWait(options?.signal, 30)
      const item = findRegistration(id)
      const actor = getMockActor()
      return [
        {
          action: 'cancel' as const,
          allowed: actor.role === 'EMPLOYEE' && item.status === 'REGISTERED',
          reason: item.status === 'REGISTERED' ? undefined : '当前状态不能取消报名。',
        },
        {
          action: 'signin' as const,
          allowed: canManage() && item.status === 'REGISTERED',
          reason: item.status === 'REGISTERED' ? undefined : '当前状态不能签到。',
        },
        {
          action: 'complete' as const,
          allowed: canManage() && item.status === 'SIGNED_IN',
          reason: item.status === 'SIGNED_IN' ? undefined : '需签到后才能完成培训。',
        },
      ]
    },

    async listManage(query, options) {
      await mockWait(options?.signal, 50)
      throwQueryScenario(getScenario())
      if (!canManage()) throw domainError('REGISTRATION_FORBIDDEN')
      return pageOf(filterItems(query, true), query)
    },

    async cancel(id, options) {
      const actor = getMockActor()
      if (actor.role !== 'EMPLOYEE') throw domainError('REGISTRATION_FORBIDDEN')
      const item = findRegistration(id)
      if (item.status !== 'REGISTERED') throw domainError('REGISTRATION_STATE_CONFLICT')
      return update(
        id,
        (next) => {
          next.status = 'CANCELED'
        },
        options,
      )
    },

    async signIn(id, options) {
      if (!canManage()) throw domainError('REGISTRATION_FORBIDDEN')
      const item = findRegistration(id)
      if (item.status !== 'REGISTERED') throw domainError('ATTENDANCE_NOT_ALLOWED')
      return update(
        id,
        (next) => {
          next.status = 'SIGNED_IN'
          next.signedInAt = '2026-08-23T09:06:00+08:00'
          next.signinMethod = 'MANUAL'
          next.attendanceNote = 'Mock人工签到'
        },
        options,
      )
    },

    async markAbsent(id, options) {
      if (!canManage()) throw domainError('REGISTRATION_FORBIDDEN')
      const item = findRegistration(id)
      if (item.status !== 'REGISTERED') throw domainError('ATTENDANCE_NOT_ALLOWED')
      return update(
        id,
        (next) => {
          next.status = 'ABSENT'
        },
        options,
      )
    },

    async complete(id, options) {
      if (!canManage()) throw domainError('REGISTRATION_FORBIDDEN')
      const item = findRegistration(id)
      if (item.status !== 'SIGNED_IN') throw domainError('ATTENDANCE_NOT_ALLOWED')
      return update(
        id,
        (next) => {
          next.status = 'COMPLETED'
          next.completedAt = '2026-08-23T17:00:00+08:00'
        },
        options,
      )
    },
  }
}

export const mockRegistrationService = createMockRegistrationService()
