import { ServiceError } from '@/types/api'
import type { UiErrorKind } from '@/types/api'

export type MockScenario =
  'normal' | 'empty' | 'failure' | 'forbidden' | 'not-found' | 'conflict' | 'result-unknown'

const allScenarios: MockScenario[] = [
  'normal',
  'empty',
  'failure',
  'forbidden',
  'not-found',
  'conflict',
  'result-unknown',
]

export function currentMockScenario(): MockScenario {
  if (typeof window === 'undefined') return 'normal'
  const value = new URLSearchParams(window.location.search).get('scenario') as MockScenario | null
  return value && allScenarios.includes(value) ? value : 'normal'
}

export function createMockError(scenario: MockScenario): ServiceError {
  const kinds: Partial<Record<MockScenario, UiErrorKind>> = {
    failure: 'server',
    forbidden: 'forbidden',
    'not-found': 'not-found',
    conflict: 'conflict',
    'result-unknown': 'result-unknown',
  }
  const messages: Record<MockScenario, string> = {
    normal: '请求成功',
    empty: '请求成功',
    failure: '服务暂时不可用，请稍后重试。',
    forbidden: '当前账号无权查看该课程范围。',
    'not-found': '课程不存在或已被移除。',
    conflict: '课程名额已满，请刷新课程状态。',
    'result-unknown': '报名结果未知，请先查询最终状态。',
  }
  const kind = kinds[scenario] ?? 'unknown'
  return new ServiceError({
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
  })
}

export function mockWait(signal?: AbortSignal, duration = 80): Promise<void> {
  return new Promise((resolve, reject) => {
    if (signal?.aborted) {
      reject(
        new ServiceError({
          kind: 'canceled',
          code: 'REQUEST_CANCELED',
          message: '请求已取消。',
          fieldErrors: [],
          retryable: false,
          resultUnknown: false,
        }),
      )
      return
    }
    const timer = setTimeout(resolve, duration)
    const cancel = () => {
      clearTimeout(timer)
      reject(
        new ServiceError({
          kind: 'canceled',
          code: 'REQUEST_CANCELED',
          message: '请求已取消。',
          fieldErrors: [],
          retryable: false,
          resultUnknown: false,
        }),
      )
    }
    signal?.addEventListener('abort', cancel, { once: true })
  })
}
