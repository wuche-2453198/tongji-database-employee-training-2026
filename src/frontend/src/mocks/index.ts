import type { ApiResponse } from '@/types/api'
import type { LoginRequest } from '@/types/auth'
import type { CourseQuery } from '@/types/course'
import type {
  CreateTrainingRequest,
  TrainingRequestApprovalQuery,
  TrainingRequestQuery,
} from '@/types/training-request'
import { handleMockLogin, handleMockGetMe } from './handlers/auth'
import {
  handleMockGetCourseList,
  handleMockGetCourseDetail,
  handleMockGetTrainerList,
  handleMockPublishCourse,
  handleMockCloseCourse,
} from './handlers/course'
import { handleMockGetDashboard } from './handlers/dashboard'
import {
  handleMockCreateRequest,
  handleMockGetAllRequests,
  handleMockGetMyCourseRequestStatus,
  handleMockGetMyRequests,
  handleMockGetRequestDetail,
  handleMockWithdrawRequest,
  handleMockDeptApprove,
  handleMockDeptReject,
  handleMockHrFile,
} from './handlers/training-request'

/**
 * Mock 请求分发器。
 * 页面组件不直接调用此模块，统一通过 API service 层调用。
 */

// 约定参数顺序：路径参数 -> 请求体/查询 -> token（存在时作为最后一个参数）
type MockHandler = (...args: unknown[]) => unknown

const routes: Record<string, MockHandler> = {
  'POST /api/auth/login': (data) => {
    try {
      return { success: true, message: 'ok', data: handleMockLogin(data as LoginRequest), traceId: 'mock-trace-001' }
    } catch (e: unknown) {
      const err = e as { message: string }
      return { success: false, message: err.message, data: null, traceId: 'mock-trace-001' }
    }
  },
  'GET /api/auth/me': (_data, token) => {
    try {
      return { success: true, message: 'ok', data: handleMockGetMe((token as string) || ''), traceId: 'mock-trace-002' }
    } catch (e: unknown) {
      const err = e as { message: string }
      return { success: false, message: err.message, data: null, traceId: 'mock-trace-002' }
    }
  },
  'GET /api/courses': (query) => {
    return { success: true, message: 'ok', data: handleMockGetCourseList(query as CourseQuery), traceId: 'mock-trace-003' }
  },
  'GET /api/courses/:id': (id) => {
    try {
      return { success: true, message: 'ok', data: handleMockGetCourseDetail(id as number), traceId: 'mock-trace-004' }
    } catch (e: unknown) {
      const err = e as { message: string }
      return { success: false, message: err.message, data: null, traceId: 'mock-trace-004' }
    }
  },
  'PATCH /api/courses/:id/publish': (id) => {
    try {
      return { success: true, message: 'ok', data: handleMockPublishCourse(id as number), traceId: 'mock-trace-012' }
    } catch (e: unknown) {
      const err = e as { message: string }
      return { success: false, message: err.message, data: null, traceId: 'mock-trace-012' }
    }
  },
  'PATCH /api/courses/:id/close': (id) => {
    try {
      return { success: true, message: 'ok', data: handleMockCloseCourse(id as number), traceId: 'mock-trace-013' }
    } catch (e: unknown) {
      const err = e as { message: string }
      return { success: false, message: err.message, data: null, traceId: 'mock-trace-013' }
    }
  },
  'GET /api/trainers': () => {
    return { success: true, message: 'ok', data: handleMockGetTrainerList(), traceId: 'mock-trace-007' }
  },
  'GET /api/dashboard/stats': (_query, token) => {
    return { success: true, message: 'ok', data: handleMockGetDashboard(undefined, token as string | undefined), traceId: 'mock-trace-005' }
  },
  'POST /api/training-requests': (data, token) => {
    try {
      return { success: true, message: '申请已提交', data: handleMockCreateRequest(data as CreateTrainingRequest, token as string | undefined), traceId: 'mock-trace-006' }
    } catch (e: unknown) {
      const err = e as { message: string }
      return { success: false, message: err.message, data: null, traceId: 'mock-trace-006' }
    }
  },
  'GET /api/training-requests/mine': (query, token) => {
    try {
      return { success: true, message: 'ok', data: handleMockGetMyRequests(query as TrainingRequestQuery, token as string | undefined), traceId: 'mock-trace-008' }
    } catch (e: unknown) {
      const err = e as { message: string }
      return { success: false, message: err.message, data: null, traceId: 'mock-trace-008' }
    }
  },
  'GET /api/training-requests/mine/status/:courseId': (courseId, token) => {
    try {
      return { success: true, message: 'ok', data: handleMockGetMyCourseRequestStatus(courseId as number, token as string | undefined), traceId: 'mock-trace-009' }
    } catch (e: unknown) {
      const err = e as { message: string }
      return { success: false, message: err.message, data: null, traceId: 'mock-trace-009' }
    }
  },
  'GET /api/training-requests/:id': (id, token) => {
    try {
      return { success: true, message: 'ok', data: handleMockGetRequestDetail(id as number, token as string | undefined), traceId: 'mock-trace-010' }
    } catch (e: unknown) {
      const err = e as { message: string }
      return { success: false, message: err.message, data: null, traceId: 'mock-trace-010' }
    }
  },
  'DELETE /api/training-requests/:id': (id, token) => {
    try {
      return { success: true, message: '撤回成功', data: handleMockWithdrawRequest(id as number, token as string | undefined), traceId: 'mock-trace-011' }
    } catch (e: unknown) {
      const err = e as { message: string }
      return { success: false, message: err.message, data: null, traceId: 'mock-trace-011' }
    }
  },
  'GET /api/training-requests': (query, token) => {
    try {
      return { success: true, message: 'ok', data: handleMockGetAllRequests(query as TrainingRequestApprovalQuery, token as string | undefined), traceId: 'mock-trace-014' }
    } catch (e: unknown) {
      const err = e as { message: string }
      return { success: false, message: err.message, data: null, traceId: 'mock-trace-014' }
    }
  },
  'PATCH /api/training-requests/:id/dept-approve': (id, data, token) => {
    try {
      const body = data as { comment?: string | null }
      return { success: true, message: '审批通过', data: handleMockDeptApprove(id as number, body?.comment ?? null, token as string | undefined), traceId: 'mock-trace-015' }
    } catch (e: unknown) {
      const err = e as { message: string }
      return { success: false, message: err.message, data: null, traceId: 'mock-trace-015' }
    }
  },
  'PATCH /api/training-requests/:id/dept-reject': (id, data, token) => {
    try {
      const body = data as { comment?: string }
      return { success: true, message: '已驳回', data: handleMockDeptReject(id as number, body?.comment ?? '', token as string | undefined), traceId: 'mock-trace-016' }
    } catch (e: unknown) {
      const err = e as { message: string }
      return { success: false, message: err.message, data: null, traceId: 'mock-trace-016' }
    }
  },
  'PATCH /api/training-requests/:id/hr-file': (id, _data, token) => {
    try {
      return { success: true, message: '备案完成', data: handleMockHrFile(id as number, token as string | undefined), traceId: 'mock-trace-017' }
    } catch (e: unknown) {
      const err = e as { message: string }
      return { success: false, message: err.message, data: null, traceId: 'mock-trace-017' }
    }
  },
}

function matchRoute(method: string, url: string): { handler: MockHandler; params: unknown[] } | null {
  // 精确匹配
  const exactKey = `${method} ${url}`
  if (routes[exactKey]) {
    return { handler: routes[exactKey], params: [] }
  }

  // 路径参数匹配 (如 /api/courses/:id)
  for (const [key, handler] of Object.entries(routes)) {
    const [routeMethod, routePath] = key.split(' ')
    if (routeMethod !== method) continue

    const routeParts = routePath.split('/')
    const urlParts = url.split('/')
    if (routeParts.length !== urlParts.length) continue

    const params: unknown[] = []
    let matched = true
    for (let i = 0; i < routeParts.length; i++) {
      if (routeParts[i].startsWith(':')) {
        const p = urlParts[i]
        params.push(isNaN(Number(p)) ? p : Number(p))
      } else if (routeParts[i] !== urlParts[i]) {
        matched = false
        break
      }
    }

    if (matched) {
      return { handler, params }
    }
  }

  return null
}

export async function mockRequest<T>(
  method: string,
  url: string,
  data?: unknown,
  token?: string,
): Promise<ApiResponse<T>> {
  // 模拟网络延迟（仅 mock 层，页面不感知）
  await new Promise((r) => setTimeout(r, 200 + Math.random() * 300))

  const matched = matchRoute(method, url)
  if (!matched) {
    return {
      success: false,
      message: 'Mock 接口未定义: ' + method + ' ' + url,
      data: null as never,
      traceId: 'mock-trace-999',
    }
  }

  const { handler, params } = matched

  // 统一参数顺序：路径参数 -> 请求体/查询 -> token
  const args: unknown[] = [...params]
  if (method === 'GET') {
    if (params.length === 0) args.push(data)
  } else if (method === 'POST' || method === 'PUT' || method === 'PATCH') {
    args.push(data)
  }
  if (token) args.push(token)

  return handler(...args) as ApiResponse<T>
}
