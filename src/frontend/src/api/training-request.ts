import { apiRequest } from './client'
import { useMock } from '@/config/mock'
import type { ApiResponse, PagedResult } from '@/types/api'
import type {
  CreateTrainingRequest,
  CourseRequestStatus,
  TrainingRequestApprovalItem,
  TrainingRequestApprovalQuery,
  TrainingRequestDetail,
  TrainingRequestItem,
  TrainingRequestQuery,
} from '@/types/training-request'

/**
 * 培训申请统一 Service。
 * 后端 feature/module-request 目前存在严重缺陷（写死 employeeId=56、
 * 无统一 [Authorize]、角色用 "Manager"、响应信封不一致、无撤回接口），
 * 故该模块默认保持 Mock。后端修复后仅需将 useMock('trainingRequest') 切为 false。
 *
 * 真实后端接口契约（待后端修复后启用）：
 *   POST  /api/training-requests
 *   GET   /api/training-requests/my
 *   GET   /api/training-requests
 *   GET   /api/training-requests/{id}
 *   PATCH /api/training-requests/{id}/dept-approve
 *   PATCH /api/training-requests/{id}/dept-reject
 *   PATCH /api/training-requests/{id}/hr-file
 */

const MOCK = () => useMock('trainingRequest')

/** 后端 TrainingRequestQueryDto 仅支持 Status/EmployeeId/CourseId/Page/PageSize，无关键词与日期筛选 */
function buildBackendRequestQuery(query: TrainingRequestQuery) {
  return {
    Status: query.status,
    Page: query.page ?? 1,
    PageSize: query.pageSize ?? 10,
  }
}

function buildBackendApprovalQuery(query: TrainingRequestApprovalQuery) {
  return {
    Status: query.status,
    Page: query.page ?? 1,
    PageSize: query.pageSize ?? 10,
  }
}

/** 提交培训申请 */
export function createTrainingRequestApi(data: CreateTrainingRequest) {
  const mock = MOCK()
  return apiRequest<{ requestId: number }>(
    'POST',
    '/api/training-requests',
    mock ? data : { CourseId: data.courseId, RequestReason: data.reason },
    { mock },
  )
}

/** 获取我的培训申请列表 */
export function getMyTrainingRequestsApi(query: TrainingRequestQuery) {
  const mock = MOCK()
  const url = mock ? '/api/training-requests/mine' : '/api/training-requests/my'
  return apiRequest<PagedResult<TrainingRequestItem>>(
    'GET',
    url,
    mock ? query : buildBackendRequestQuery(query),
    { mock },
  )
}

/** 获取培训申请详情 */
export function getTrainingRequestDetailApi(requestId: number) {
  return apiRequest<TrainingRequestDetail>('GET', `/api/training-requests/${requestId}`, undefined, {
    mock: MOCK(),
  })
}

/** 查询当前用户对某课程的申请状态（真实后端无独立接口，可改为 GET /my?courseId=x 派生） */
export function getMyCourseRequestStatusApi(courseId: number) {
  return apiRequest<CourseRequestStatus>(
    'GET',
    `/api/training-requests/mine/status/${courseId}`,
    undefined,
    { mock: MOCK() },
  )
}

/** 撤回培训申请（真实后端当前未提供该接口，标记为 BLOCKED_BY_BACKEND） */
export async function withdrawTrainingRequestApi(
  requestId: number,
): Promise<ApiResponse<{ requestId: number }>> {
  if (!MOCK()) {
    return { success: false, message: '撤回接口尚未提供', data: null as never, traceId: '' }
  }
  return apiRequest<{ requestId: number }>('DELETE', `/api/training-requests/${requestId}`, undefined, {
    mock: true,
  })
}

/** 撤回接口是否可用（后端未提供，仅 Mock 可用） */
export function isWithdrawSupported(): boolean {
  return MOCK()
}

/** 获取全部申请（主管/HR，待后端补齐部门隔离与角色） */
export function getAllTrainingRequestsApi(query: TrainingRequestApprovalQuery) {
  const mock = MOCK()
  return apiRequest<PagedResult<TrainingRequestApprovalItem>>(
    'GET',
    '/api/training-requests',
    mock ? query : buildBackendApprovalQuery(query),
    { mock },
  )
}

/** 主管审批通过 */
export function approveTrainingRequestApi(requestId: number, comment: string | null) {
  const mock = MOCK()
  return apiRequest<{ requestId: number }>(
    'PATCH',
    `/api/training-requests/${requestId}/dept-approve`,
    mock ? { comment } : { Comment: comment },
    { mock },
  )
}

/** 主管驳回 */
export function rejectTrainingRequestApi(requestId: number, comment: string) {
  const mock = MOCK()
  return apiRequest<{ requestId: number }>(
    'PATCH',
    `/api/training-requests/${requestId}/dept-reject`,
    mock ? { comment } : { Comment: comment },
    { mock },
  )
}

/** HR 备案（后端当前不支持备案意见字段，故不发送） */
export function fileTrainingRequestApi(requestId: number) {
  return apiRequest<{ requestId: number }>(
    'PATCH',
    `/api/training-requests/${requestId}/hr-file`,
    {},
    { mock: MOCK() },
  )
}
