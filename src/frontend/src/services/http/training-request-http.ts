import { requestApi } from '@/api/client'
import { fromEnvelopeFailure } from '@/api/error'
import { mapTrainingRequest, mapTrainingRequestPage } from '@/api/mappers/training-request'
import type { ApiEnvelopeDto, PageDto, TrainingRequestDto } from '@/api/transport'
import { domainError } from '@/domains/errors'
import type { TrainingRequestService } from '@/domains/training-request'

/** 创建接口正常返回完整申请 DTO；仅当契约退回引用对象时才需要二次查询。 */
interface RequestReferenceDto {
  id?: string | number
  requestId?: string | number
}

const unwrap = <T>(envelope: ApiEnvelopeDto<T>): T => {
  if (!envelope.success || !envelope.data) throw fromEnvelopeFailure(envelope)
  return envelope.data
}

const queryParams = (query: Parameters<TrainingRequestService['listMine']>[0]) => ({
  Status: query.status === 'UNKNOWN' ? undefined : query.status || undefined,
  // 主管审批页与 HR 备案页的部门筛选由后端 TrainingRequestQueryDto.DepartmentName 支持。
  DepartmentName: query.departmentName || undefined,
  Page: query.page,
  PageSize: query.pageSize,
})

const getRequest = async (id: string, signal?: AbortSignal) => {
  const envelope = await requestApi<ApiEnvelopeDto<TrainingRequestDto>>({
    method: 'GET',
    url: `/api/training-requests/${encodeURIComponent(id)}`,
    signal,
    operation: 'query',
  })
  return mapTrainingRequest(unwrap(envelope))
}

const performAction = async (
  id: string,
  action: 'dept-approve' | 'dept-reject' | 'hr-file',
  opinion: string,
  signal?: AbortSignal,
) => {
  await requestApi<ApiEnvelopeDto<RequestReferenceDto>>({
    method: 'PATCH',
    url: `/api/training-requests/${encodeURIComponent(id)}/${action}`,
    // 三个动作的请求体都是后端 ApproveRequestDto/HrFileRequestDto 的 Comment。
    data: { Comment: opinion || null },
    signal,
    operation: 'write',
  })
  return getRequest(id, signal)
}

export const httpTrainingRequestService: TrainingRequestService = {
  async create(command, options) {
    const envelope = await requestApi<ApiEnvelopeDto<RequestReferenceDto | TrainingRequestDto>>({
      method: 'POST',
      url: '/api/training-requests',
      data: { CourseId: Number(command.courseId), RequestReason: command.reason },
      signal: options?.signal,
      operation: 'write',
    })
    const created = unwrap(envelope)
    if ('courseId' in created) return mapTrainingRequest(created)

    const rawId = created.id ?? created.requestId
    if (rawId === undefined || rawId === null || rawId === '') {
      throw domainError('HTTP_CONTRACT_NOT_FROZEN', {
        message: '申请创建响应缺少 id 字段，无法查询申请详情。',
      })
    }

    return getRequest(String(rawId), options?.signal)
  },
  async listMine(query, options) {
    const envelope = await requestApi<ApiEnvelopeDto<PageDto<TrainingRequestDto>>>({
      method: 'GET',
      url: '/api/training-requests/my',
      params: queryParams(query),
      signal: options?.signal,
      operation: 'query',
    })
    return mapTrainingRequestPage(envelope)
  },
  async getById(id, options) {
    return getRequest(id, options?.signal)
  },
  async getActionEligibility(id, options) {
    const request = await getRequest(id, options?.signal)
    return [
      {
        action: 'approve',
        allowed: request.status === 'PENDING',
        reason: request.status === 'PENDING' ? undefined : '当前状态不可审批。',
      },
      {
        action: 'reject',
        allowed: request.status === 'PENDING',
        reason: request.status === 'PENDING' ? undefined : '当前状态不可驳回。',
      },
      {
        action: 'file',
        allowed: request.status === 'DEPT_APPROVED',
        reason: request.status === 'DEPT_APPROVED' ? undefined : '仅主管已通过的申请可备案。',
      },
    ]
  },
  async listDepartment(query, options) {
    const envelope = await requestApi<ApiEnvelopeDto<PageDto<TrainingRequestDto>>>({
      method: 'GET',
      url: '/api/training-requests',
      params: queryParams({ ...query, status: query.status || 'PENDING' }),
      signal: options?.signal,
      operation: 'query',
    })
    return mapTrainingRequestPage(envelope)
  },
  async listForHr(query, options) {
    const envelope = await requestApi<ApiEnvelopeDto<PageDto<TrainingRequestDto>>>({
      method: 'GET',
      url: '/api/training-requests',
      params: queryParams({ ...query, status: query.status || 'DEPT_APPROVED' }),
      signal: options?.signal,
      operation: 'query',
    })
    return mapTrainingRequestPage(envelope)
  },
  async approve(id, opinion, options) {
    return performAction(id, 'dept-approve', opinion, options?.signal)
  },
  async reject(id, opinion, options) {
    return performAction(id, 'dept-reject', opinion, options?.signal)
  },
  async file(id, opinion, options) {
    return performAction(id, 'hr-file', opinion, options?.signal)
  },
}
