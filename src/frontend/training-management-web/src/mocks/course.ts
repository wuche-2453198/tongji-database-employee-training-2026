import { mapCoursePage, mapRegistrationReceipt } from '@/api/mappers/course'
import type {
  ApiEnvelopeDto,
  CourseSummaryDto,
  PageDto,
  RegistrationReceiptDto,
} from '@/api/transport'
import { ServiceError, type UiError, type UiErrorKind } from '@/types/api'
import type { CourseService } from '@/services/course'

export type CourseMockScenario =
  'normal' | 'empty' | 'failure' | 'forbidden' | 'not-found' | 'conflict' | 'result-unknown'

const courses: CourseSummaryDto[] = [
  {
    courseId: 'COURSE-2026-001',
    courseName: '数据库性能优化实战',
    courseType: 'SKILL',
    trainerName: '陈老师',
    startTime: '2026-09-02T09:00:00+08:00',
    endTime: '2026-09-02T17:00:00+08:00',
    location: '培训中心 A201',
    status: 'PUBLISHED',
    maxStudents: 30,
    registeredCount: 18,
    remainingSeats: 12,
  },
  {
    courseId: 'COURSE-2026-002',
    courseName: '新任主管沟通与反馈',
    courseType: 'MANAGEMENT',
    trainerName: '周老师',
    startTime: '2026-09-08T13:30:00+08:00',
    endTime: '2026-09-08T17:30:00+08:00',
    location: '行政楼 3F 多功能厅',
    status: 'PUBLISHED',
    maxStudents: 24,
    registeredCount: 24,
    remainingSeats: 0,
  },
]

const messages: Record<CourseMockScenario, string> = {
  normal: '请求成功',
  empty: '请求成功',
  failure: '服务暂时不可用，请稍后重试。',
  forbidden: '当前账号无权查看该课程范围。',
  'not-found': '课程不存在或已被移除。',
  conflict: '课程名额已满，请刷新课程状态。',
  'result-unknown': '报名结果未知，请先查询我的报名。',
}

const scenarioKind: Partial<Record<CourseMockScenario, UiErrorKind>> = {
  failure: 'server',
  forbidden: 'forbidden',
  'not-found': 'not-found',
  conflict: 'conflict',
  'result-unknown': 'result-unknown',
}

function currentScenario(): CourseMockScenario {
  if (typeof window === 'undefined') return 'normal'
  const value = new URLSearchParams(window.location.search).get('scenario')
  const scenarios: CourseMockScenario[] = [
    'normal',
    'empty',
    'failure',
    'forbidden',
    'not-found',
    'conflict',
    'result-unknown',
  ]
  return scenarios.includes(value as CourseMockScenario) ? (value as CourseMockScenario) : 'normal'
}

function mockError(scenario: CourseMockScenario): ServiceError {
  const kind = scenarioKind[scenario] || 'unknown'
  const error: UiError = {
    kind,
    code: `MOCK_${scenario.toUpperCase().replace('-', '_')}`,
    message: messages[scenario],
    traceId: `trace-mock-${scenario}`,
    fieldErrors:
      scenario === 'conflict'
        ? [{ field: 'courseId', message: '该课程当前没有剩余名额', code: 'COURSE_FULL' }]
        : [],
    retryable: scenario === 'failure',
    resultUnknown: scenario === 'result-unknown',
  }
  return new ServiceError(error)
}

const wait = (signal?: AbortSignal): Promise<void> =>
  new Promise((resolve, reject) => {
    const canceled = () =>
      new ServiceError({
        kind: 'canceled',
        code: 'REQUEST_CANCELED',
        message: '请求已取消。',
        fieldErrors: [],
        retryable: false,
        resultUnknown: false,
      })

    if (signal?.aborted) {
      reject(canceled())
      return
    }

    const timer = window.setTimeout(resolve, 180)
    signal?.addEventListener(
      'abort',
      () => {
        window.clearTimeout(timer)
        reject(canceled())
      },
      { once: true },
    )
  })

export function createMockCourseService(
  getScenario: () => CourseMockScenario = currentScenario,
): CourseService {
  return {
    async listCourses(query, options) {
      await wait(options?.signal)
      const scenario = getScenario()
      if (['failure', 'forbidden', 'not-found'].includes(scenario)) throw mockError(scenario)

      const matching =
        scenario === 'empty'
          ? []
          : courses.filter((course) =>
              course.courseName.toLowerCase().includes((query.keyword || '').toLowerCase()),
            )
      const envelope: ApiEnvelopeDto<PageDto<CourseSummaryDto>> = {
        success: true,
        message: messages[scenario],
        data: {
          items: matching,
          page: query.page,
          pageSize: query.pageSize,
          total: matching.length,
        },
        errors: [],
        traceId: `trace-mock-${scenario}`,
      }
      return mapCoursePage(envelope)
    },

    async registerForCourse(courseId, options) {
      await wait(options?.signal)
      const scenario = getScenario()
      if (scenario === 'conflict' || scenario === 'result-unknown') throw mockError(scenario)

      const envelope: ApiEnvelopeDto<RegistrationReceiptDto> = {
        success: true,
        message: '报名成功',
        data: {
          regId: 'REG-MOCK-001',
          courseId,
          status: 'REGISTERED',
          regDate: '2026-08-17T10:00:00+08:00',
        },
        errors: [],
        traceId: 'trace-mock-register-success',
      }
      return mapRegistrationReceipt(envelope)
    },
  }
}

export const mockCourseService = createMockCourseService()
