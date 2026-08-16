import { apiRequest } from './client'
import type { PagedResult } from '@/types/api'
import type {
  CreateTrainingRequest,
  CourseRequestStatus,
  TrainingRequestDetail,
  TrainingRequestItem,
  TrainingRequestQuery,
} from '@/types/training-request'

/**
 * 培训申请相关接口。
 * 注意：除「提交申请」外，其余接口路径均为前端约定，待后端确认后联调。
 */

/** 提交培训申请 */
export function createTrainingRequestApi(data: CreateTrainingRequest) {
  return apiRequest<{ requestId: number }>('POST', '/api/training-requests', data)
}

/** 获取我的培训申请列表（待联调） */
export function getMyTrainingRequestsApi(query: TrainingRequestQuery) {
  return apiRequest<PagedResult<TrainingRequestItem>>('GET', '/api/training-requests/mine', query)
}

/** 获取培训申请详情（待联调） */
export function getTrainingRequestDetailApi(requestId: number) {
  return apiRequest<TrainingRequestDetail>('GET', `/api/training-requests/${requestId}`)
}

/** 查询当前用户对某课程的申请状态（待联调） */
export function getMyCourseRequestStatusApi(courseId: number) {
  return apiRequest<CourseRequestStatus>('GET', `/api/training-requests/mine/status/${courseId}`)
}

/** 撤回培训申请（待联调） */
export function withdrawTrainingRequestApi(requestId: number) {
  return apiRequest<{ requestId: number }>('DELETE', `/api/training-requests/${requestId}`)
}
