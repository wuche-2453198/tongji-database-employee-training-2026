import type { TrainingRequest, TrainingRequestService } from '@/domains/training-request'
import { domainError } from '@/domains/errors'
import { getMockActor, mockBusinessRepository } from '@/mocks/repositories/business-repository'
import { createMockError, currentMockScenario, mockWait } from '@/mocks/scenarios'

const REQUEST_PAGE_SIZES = [10, 20, 50]

function canReadRequest(request: TrainingRequest): boolean {
  const actor = getMockActor()
  if (actor.role === 'HR') return true
  if (actor.role === 'EMPLOYEE') return request.employeeId === actor.employeeId
  return actor.role === 'MANAGER' && request.departmentName === actor.departmentName
}

function ensureRequestReadAccess(request: TrainingRequest): void {
  if (!canReadRequest(request)) throw domainError('REQUEST_FORBIDDEN')
}

function ensureReason(reason: string): string {
  const normalized = reason.trim()
  if (!normalized) throw domainError('REQUEST_REASON_REQUIRED')
  if (normalized.length > 500) throw domainError('REQUEST_REASON_TOO_LONG')
  return normalized
}

function throwScenarioForQuery(): void {
  const scenario = currentMockScenario()
  if (scenario === 'failure' || scenario === 'forbidden' || scenario === 'not-found') {
    throw createMockError(scenario)
  }
}

function isPending(request: TrainingRequest): boolean {
  return request.status === 'PENDING' || request.status === 'DEPT_APPROVED'
}

function pageRequests(
  query: Parameters<TrainingRequestService['listMine']>[0],
  mode: 'department' | 'hr',
) {
  const actor = getMockActor()
  let items = mockBusinessRepository
    .getState()
    .requests.filter((item) =>
      mode === 'department' ? item.departmentName === actor.departmentName : true,
    )
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
  if (query.status) items = items.filter((item) => item.status === query.status)
  if (query.startDateFrom)
    items = items.filter((item) => item.submittedAt.slice(0, 10) >= query.startDateFrom!)
  if (query.startDateTo)
    items = items.filter((item) => item.submittedAt.slice(0, 10) <= query.startDateTo!)
  items = items.sort((left, right) => left.submittedAt.localeCompare(right.submittedAt))
  const page = Number.isInteger(query.page) && query.page > 0 ? query.page : 1
  const pageSize = REQUEST_PAGE_SIZES.includes(query.pageSize) ? query.pageSize : 20
  const start = (page - 1) * pageSize
  return { items: items.slice(start, start + pageSize), page, pageSize, total: items.length }
}

async function transitionRequest(
  id: string,
  action: 'approve' | 'reject' | 'file',
  opinion: string,
  options?: { signal?: AbortSignal },
): Promise<TrainingRequest> {
  await mockWait(options?.signal)
  const actor = getMockActor()
  const current = mockBusinessRepository.getState().requests.find((item) => item.id === id)
  if (!current) throw domainError('REQUEST_NOT_FOUND')
  if (action !== 'file' && actor.role !== 'MANAGER') throw domainError('REQUEST_FORBIDDEN')
  if (action === 'file' && actor.role !== 'HR') throw domainError('REQUEST_FORBIDDEN')
  if (action === 'approve' || action === 'reject') {
    if (current.status !== 'PENDING' || current.departmentName !== actor.departmentName)
      throw domainError('REQUEST_STATE_CONFLICT')
  } else if (current.status !== 'DEPT_APPROVED') {
    throw domainError('REQUEST_STATE_CONFLICT')
  }
  if (action === 'reject' && !opinion.trim())
    throw domainError('REQUEST_REASON_REQUIRED', { message: '请填写驳回意见。' })
  if (currentMockScenario() === 'conflict') throw createMockError('conflict')
  const next: TrainingRequest = {
    ...current,
    status:
      action === 'approve' ? 'DEPT_APPROVED' : action === 'reject' ? 'DEPT_REJECTED' : 'HR_FILED',
    updatedAt: '2026-08-23T11:00:00+08:00',
    departmentOpinion:
      action === 'approve' || action === 'reject'
        ? opinion.trim() || null
        : current.departmentOpinion,
    hrOpinion: action === 'file' ? opinion.trim() || null : current.hrOpinion,
  }
  mockBusinessRepository.update((state) => {
    const index = state.requests.findIndex((item) => item.id === id)
    if (index >= 0) state.requests[index] = next
  })
  if (currentMockScenario() === 'result-unknown') throw createMockError('result-unknown')
  return next
}

export const mockTrainingRequestService: TrainingRequestService = {
  async create(command, options) {
    await mockWait(options?.signal)
    const scenario = currentMockScenario()
    const actor = getMockActor()
    if (actor.role !== 'EMPLOYEE')
      throw domainError('REQUEST_FORBIDDEN', { message: '当前角色不能发起培训申请。' })
    if (actor.status !== 'ACTIVE') throw domainError('REQUEST_EMPLOYEE_INACTIVE')
    if (scenario === 'failure' || scenario === 'conflict' || scenario === 'forbidden') {
      throw createMockError(scenario)
    }
    if (scenario === 'not-found') throw domainError('COURSE_NOT_FOUND')

    const reason = ensureReason(command.reason)
    const state = mockBusinessRepository.getState()
    const course = state.courses.find((item) => item.id === command.courseId)
    if (!course) throw domainError('COURSE_NOT_FOUND')
    if (course.status !== 'PUBLISHED') throw domainError('COURSE_CLOSED')

    if (
      state.requests.some(
        (item) =>
          item.courseId === command.courseId &&
          item.employeeId === actor.employeeId &&
          isPending(item),
      )
    )
      throw domainError('REQUEST_EXISTS')

    const request: TrainingRequest = {
      id: `500${state.requests.length + 1}`,
      courseId: course.id,
      courseName: course.name,
      employeeId: actor.employeeId,
      employeeName: '张三',
      departmentName: actor.departmentName,
      reason,
      status: 'PENDING',
      submittedAt: '2026-08-23T10:00:00+08:00',
      updatedAt: null,
    }
    mockBusinessRepository.update((next) => next.requests.push(request))

    // Persist before reporting an unknown result. The page must query final state instead of retrying.
    if (scenario === 'result-unknown') throw createMockError(scenario)
    return request
  },

  async listMine(query, options) {
    await mockWait(options?.signal, 50)
    throwScenarioForQuery()
    const actor = getMockActor()
    if (actor.role !== 'EMPLOYEE') throw domainError('REQUEST_FORBIDDEN')
    const normalizedPage = Number.isInteger(query.page) && query.page > 0 ? query.page : 1
    const normalizedPageSize = REQUEST_PAGE_SIZES.includes(query.pageSize) ? query.pageSize : 20
    const state = mockBusinessRepository.getState()
    let items =
      currentMockScenario() === 'empty'
        ? []
        : state.requests.filter((item) => item.employeeId === actor.employeeId)
    const keyword = query.keyword?.trim().toLowerCase()
    if (keyword) items = items.filter((item) => item.courseName.toLowerCase().includes(keyword))
    if (query.status) items = items.filter((item) => item.status === query.status)
    if (query.startDateFrom)
      items = items.filter((item) => item.submittedAt.slice(0, 10) >= query.startDateFrom!)
    if (query.startDateTo)
      items = items.filter((item) => item.submittedAt.slice(0, 10) <= query.startDateTo!)
    items = [...items].sort((left, right) => right.submittedAt.localeCompare(left.submittedAt))
    const start = (normalizedPage - 1) * normalizedPageSize
    return {
      items: items.slice(start, start + normalizedPageSize),
      page: normalizedPage,
      pageSize: normalizedPageSize,
      total: items.length,
    }
  },

  async getById(id, options) {
    await mockWait(options?.signal, 50)
    throwScenarioForQuery()
    const item = mockBusinessRepository.getState().requests.find((request) => request.id === id)
    if (!item) throw domainError('REQUEST_NOT_FOUND')
    ensureRequestReadAccess(item)
    return item
  },

  async getActionEligibility(id, options) {
    await mockWait(options?.signal, 30)
    const item = mockBusinessRepository.getState().requests.find((request) => request.id === id)
    if (!item) throw domainError('REQUEST_NOT_FOUND')
    ensureRequestReadAccess(item)
    const actor = getMockActor()
    if (actor.role === 'MANAGER') {
      return [
        {
          action: 'approve',
          allowed: item.status === 'PENDING',
          reasonCode: item.status === 'PENDING' ? undefined : 'REQUEST_STATE_CONFLICT',
          reason: item.status === 'PENDING' ? undefined : '申请已处理，不能重复审批。',
        },
        {
          action: 'reject',
          allowed: item.status === 'PENDING',
          reasonCode: item.status === 'PENDING' ? undefined : 'REQUEST_STATE_CONFLICT',
          reason: item.status === 'PENDING' ? undefined : '申请已处理，不能重复审批。',
        },
      ]
    }
    if (actor.role === 'HR') {
      return [
        {
          action: 'file',
          allowed: item.status === 'DEPT_APPROVED',
          reasonCode: item.status === 'DEPT_APPROVED' ? undefined : 'REQUEST_STATE_CONFLICT',
          reason: item.status === 'DEPT_APPROVED' ? undefined : '申请尚未进入 HR 备案阶段。',
        },
      ]
    }
    return []
  },

  async listDepartment(query, options) {
    await mockWait(options?.signal, 50)
    if (getMockActor().role !== 'MANAGER') throw domainError('REQUEST_FORBIDDEN')
    throwScenarioForQuery()
    return pageRequests(query, 'department')
  },

  async listForHr(query, options) {
    await mockWait(options?.signal, 50)
    if (getMockActor().role !== 'HR') throw domainError('REQUEST_FORBIDDEN')
    throwScenarioForQuery()
    return pageRequests(query, 'hr')
  },

  async approve(id, opinion, options) {
    return transitionRequest(id, 'approve', opinion, options)
  },

  async reject(id, opinion, options) {
    return transitionRequest(id, 'reject', opinion, options)
  },

  async file(id, opinion, options) {
    return transitionRequest(id, 'file', opinion, options)
  },
}
