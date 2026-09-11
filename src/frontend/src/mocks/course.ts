import type {
  CourseActionEligibility,
  CourseDetail,
  CourseQuery,
  CourseService,
  CourseSummary,
} from '@/domains/course'
import type { DomainPage } from '@/domains/shared'
import { domainError } from '@/domains/errors'
import { mockBusinessRepository, getMockActor } from '@/mocks/repositories/business-repository'
import { createMockError, currentMockScenario, mockWait } from '@/mocks/scenarios'
import type { MockScenario } from '@/mocks/scenarios'

export type CourseMockScenario = MockScenario

const normalize = (value: string | undefined) => value?.trim().toLowerCase() ?? ''

function actorEligibility(course: CourseDetail): CourseActionEligibility {
  const actor = getMockActor()
  if (actor.role !== 'EMPLOYEE') {
    return {
      apply: { allowed: false, reasonCode: 'ROLE_NOT_ALLOWED', reason: '当前角色仅支持浏览课程。' },
      register: {
        allowed: false,
        reasonCode: 'ROLE_NOT_ALLOWED',
        reason: '当前角色仅支持浏览课程。',
      },
    }
  }

  const state = mockBusinessRepository.getState()
  const request = state.requests.find(
    (item) => item.courseId === course.id && item.employeeId === actor.employeeId,
  )
  const registration = state.registrations.find(
    (item) => item.courseId === course.id && item.employeeId === actor.employeeId,
  )
  if (registration && registration.status !== 'CANCELED') {
    return {
      apply: { allowed: false, reasonCode: 'REGISTRATION_EXISTS', reason: '你已经报名该课程。' },
      register: { allowed: false, reasonCode: 'REGISTRATION_EXISTS', reason: '你已经报名该课程。' },
    }
  }
  if (course.status !== 'PUBLISHED') {
    return {
      apply: { allowed: false, reasonCode: 'COURSE_CLOSED', reason: '课程已关闭，暂时不能申请。' },
      register: {
        allowed: false,
        reasonCode: 'COURSE_CLOSED',
        reason: '课程已关闭，暂时不能报名。',
      },
    }
  }
  if (request?.status === 'HR_FILED') {
    return {
      apply: { allowed: false, reasonCode: 'REQUEST_FILED', reason: '该课程申请已备案。' },
      register:
        (course.remainingSeats ?? 0) > 0
          ? { allowed: true }
          : { allowed: false, reasonCode: 'COURSE_FULL', reason: '课程暂无剩余名额。' },
    }
  }
  if (request) {
    return {
      apply: { allowed: false, reasonCode: 'REQUEST_EXISTS', reason: '你已经有该课程的申请记录。' },
      register: {
        allowed: false,
        reasonCode: 'REQUEST_NOT_FILED',
        reason: '申请尚未完成 HR 备案。',
      },
    }
  }
  return {
    apply: { allowed: true },
    register: {
      allowed: false,
      reasonCode: 'REQUEST_NOT_FILED',
      reason: '请先提交培训申请并完成 HR 备案。',
    },
  }
}

const pageOf = (items: CourseSummary[], query: CourseQuery): DomainPage<CourseSummary> => {
  const start = (query.page - 1) * query.pageSize
  return {
    items: items.slice(start, start + query.pageSize),
    page: query.page,
    pageSize: query.pageSize,
    total: items.length,
  }
}

export function createMockCourseService(
  getScenario: () => CourseMockScenario = currentMockScenario,
): CourseService {
  return {
    async listCourses(query, options) {
      await mockWait(options?.signal)
      const scenario = getScenario()
      if (['failure', 'forbidden', 'not-found'].includes(scenario)) throw createMockError(scenario)
      const state = mockBusinessRepository.getState()
      const keyword = normalize(query.keyword)
      // 与后端一致：非 HR/管理员仅能查看已发布课程。
      const actor = getMockActor()
      const effectiveStatus =
        actor.role === 'HR' || actor.role === 'ADMIN' ? query.status : 'PUBLISHED'
      const matching =
        scenario === 'empty'
          ? []
          : state.courses
              .filter((course) => {
                if (keyword && !course.name.toLowerCase().includes(keyword)) return false
                if (query.type && query.type !== 'UNKNOWN' && course.type !== query.type)
                  return false
                if (
                  effectiveStatus &&
                  effectiveStatus !== 'UNKNOWN' &&
                  course.status !== effectiveStatus
                )
                  return false
                if (query.startDateFrom && course.startTime.slice(0, 10) < query.startDateFrom)
                  return false
                if (query.startDateTo && course.startTime.slice(0, 10) > query.startDateTo)
                  return false
                return true
              })
              .sort((a, b) => {
                const direction = query.sortDirection === 'asc' ? 1 : -1
                return a.startTime.localeCompare(b.startTime) * direction
              })
      return pageOf(matching, query)
    },

    async getCourse(courseId, options) {
      await mockWait(options?.signal)
      const scenario = getScenario()
      if (scenario === 'failure' || scenario === 'forbidden' || scenario === 'not-found')
        throw createMockError(scenario)
      const course = mockBusinessRepository.getState().courses.find((item) => item.id === courseId)
      if (!course) throw domainError('COURSE_NOT_FOUND')
      const actor = getMockActor()
      if (actor.role !== 'HR' && actor.role !== 'ADMIN' && course.status === 'DRAFT') {
        throw domainError('COURSE_FORBIDDEN', { message: '课程尚未发布。' })
      }
      return { ...course, eligibility: actorEligibility(course) }
    },

    async getActionEligibility(courseId, options) {
      await mockWait(options?.signal)
      const course = mockBusinessRepository.getState().courses.find((item) => item.id === courseId)
      if (!course) throw domainError('COURSE_NOT_FOUND')
      return actorEligibility(course)
    },

    async publishCourse(courseId, options) {
      await mockWait(options?.signal)
      const scenario = getScenario()
      if (['failure', 'forbidden', 'not-found'].includes(scenario)) throw createMockError(scenario)
      const actor = getMockActor()
      if (actor.role !== 'HR' && actor.role !== 'ADMIN') {
        throw domainError('COURSE_FORBIDDEN', { message: '只有 HR 或管理员可以发布课程。' })
      }
      const state = mockBusinessRepository.getState()
      const course = state.courses.find((item) => item.id === courseId)
      if (!course) throw domainError('COURSE_NOT_FOUND')
      if (course.status !== 'DRAFT') {
        throw domainError('COURSE_CLOSED', { message: '只有草稿课程可以发布。' })
      }
      mockBusinessRepository.update((next) => {
        const target = next.courses.find((item) => item.id === courseId)
        if (target) {
          target.status = 'PUBLISHED'
          target.statusLabel = '已发布'
        }
      })
    },

    async closeCourse(courseId, options) {
      await mockWait(options?.signal)
      const scenario = getScenario()
      if (['failure', 'forbidden', 'not-found'].includes(scenario)) throw createMockError(scenario)
      const actor = getMockActor()
      if (actor.role !== 'HR' && actor.role !== 'ADMIN') {
        throw domainError('COURSE_FORBIDDEN', { message: '只有 HR 或管理员可以关闭课程。' })
      }
      const state = mockBusinessRepository.getState()
      const course = state.courses.find((item) => item.id === courseId)
      if (!course) throw domainError('COURSE_NOT_FOUND')
      if (course.status !== 'PUBLISHED') {
        throw domainError('COURSE_CLOSED', { message: '只有已发布课程可以关闭。' })
      }
      mockBusinessRepository.update((next) => {
        const target = next.courses.find((item) => item.id === courseId)
        if (target) {
          target.status = 'CLOSED'
          target.statusLabel = '已关闭'
        }
      })
    },
  }
}

export const mockCourseService = createMockCourseService()
