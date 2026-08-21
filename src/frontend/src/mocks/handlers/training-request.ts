import type { PagedResult } from '@/types/api'
import type {
  CourseRequestStatus,
  CreateTrainingRequest,
  TrainingRequestApprovalItem,
  TrainingRequestApprovalQuery,
  TrainingRequestDetail,
  TrainingRequestItem,
  TrainingRequestQuery,
  TrainingRequestTimelineEntry,
} from '@/types/training-request'
import { mockCourses } from '../data/courses'
import {
  addMockRequest,
  approveMockRequest,
  fileMockRequest,
  findMockRequest,
  findMockRequestById,
  mockEmployeeDirectory,
  mockTrainingRequests,
  rejectMockRequest,
  removeMockRequest,
  type MockTrainingRequest,
} from '../data/training-requests'

function getEmpIdFromToken(token?: string): number | null {
  if (token && token.startsWith('mock-jwt-')) {
    try {
      const payload = JSON.parse(atob(token.replace('mock-jwt-', '')))
      const sub = payload.sub as number | undefined
      if (sub) return sub
    } catch {
      // 解析失败则回退到 user_info
    }
  }
  // 真实 JWT 无法被 Mock 解析时，回退到登录时缓存到 localStorage 的用户信息
  try {
    const info = JSON.parse(localStorage.getItem('user_info') || 'null')
    return (info?.empId as number) ?? null
  } catch {
    return null
  }
}

function getApproverName(token?: string): string {
  const empId = getEmpIdFromToken(token)
  if (empId) {
    const dir = mockEmployeeDirectory[empId]
    if (dir) return dir.name
  }
  return '审批人'
}

function getCourse(courseId: number) {
  return mockCourses.find((c) => c.courseId === courseId)
}

function toItem(req: MockTrainingRequest): TrainingRequestItem {
  const course = getCourse(req.courseId)
  return {
    requestId: req.requestId,
    courseId: req.courseId,
    courseName: course?.courseName ?? `课程 #${req.courseId}`,
    courseType: course?.courseType ?? null,
    trainerName: course?.trainerName ?? null,
    reason: req.reason,
    expectedGain: req.expectedGain,
    status: req.status,
    createdAt: req.createdAt,
    updatedAt: req.updatedAt,
  }
}

function buildTimeline(req: MockTrainingRequest): TrainingRequestTimelineEntry[] {
  const courseName = getCourse(req.courseId)?.courseName ?? `课程 #${req.courseId}`
  const timeline: TrainingRequestTimelineEntry[] = [
    { title: '提交申请', description: `申请参加「${courseName}」`, time: req.createdAt, type: 'info' },
  ]

  if (req.reviewedAt) {
    if (req.status === 'DEPT_REJECTED') {
      timeline.push({
        title: '主管驳回',
        description: req.reviewComment,
        time: req.reviewedAt,
        type: 'danger',
      })
    } else {
      timeline.push({
        title: '主管审批通过',
        description: req.reviewComment,
        time: req.reviewedAt,
        type: 'primary',
      })
    }
  }

  if (req.filedAt) {
    timeline.push({
      title: 'HR 备案',
      description: req.filingComment,
      time: req.filedAt,
      type: 'success',
    })
  }

  return timeline
}

export function handleMockCreateRequest(
  data: CreateTrainingRequest,
  token?: string,
): { requestId: number } {
  const empId = getEmpIdFromToken(token)
  if (!empId) {
    throw { status: 401, message: '未登录' }
  }

  if (!data.reason || !data.reason.trim()) {
    throw { status: 400, message: '申请理由不能为空' }
  }

  const req = addMockRequest(empId, data.courseId, data.reason, data.expectedGain)
  return { requestId: req.requestId }
}

export function handleMockGetMyRequests(
  query: TrainingRequestQuery,
  token?: string,
): PagedResult<TrainingRequestItem> {
  const empId = getEmpIdFromToken(token)
  if (!empId) {
    throw { status: 401, message: '未登录' }
  }

  let list = mockTrainingRequests.filter((r) => r.empId === empId)

  if (query.status) {
    list = list.filter((r) => r.status === query.status)
  }

  if (query.keyword) {
    const kw = query.keyword.toLowerCase()
    list = list.filter((r) => {
      const course = getCourse(r.courseId)
      return course?.courseName.toLowerCase().includes(kw)
    })
  }

  if (query.startDate) {
    list = list.filter((r) => r.createdAt.slice(0, 10) >= query.startDate!)
  }
  if (query.endDate) {
    list = list.filter((r) => r.createdAt.slice(0, 10) <= query.endDate!)
  }

  // 按申请时间倒序
  list = [...list].sort((a, b) => b.createdAt.localeCompare(a.createdAt))

  const page = query.page ?? 1
  const pageSize = query.pageSize ?? 10
  const total = list.length
  const start = (page - 1) * pageSize
  const items = list.slice(start, start + pageSize).map(toItem)

  return { items, page, pageSize, total }
}

export function handleMockGetRequestDetail(
  requestId: number,
  token?: string,
): TrainingRequestDetail {
  const empId = getEmpIdFromToken(token)
  if (!empId) {
    throw { status: 401, message: '未登录' }
  }

  const req = findMockRequestById(requestId)
  if (!req || req.empId !== empId) {
    throw { status: 404, message: '申请记录不存在' }
  }

  return {
    ...toItem(req),
    reviewerName: req.reviewerName,
    reviewComment: req.reviewComment,
    filedByName: req.filedByName,
    filingComment: req.filingComment,
    timeline: buildTimeline(req),
  }
}

export function handleMockGetMyCourseRequestStatus(
  courseId: number,
  token?: string,
): CourseRequestStatus {
  const empId = getEmpIdFromToken(token)
  if (!empId) {
    throw { status: 401, message: '未登录' }
  }

  const req = findMockRequest(empId, courseId)
  if (!req) {
    return { applied: false, requestId: null, status: null }
  }
  return { applied: true, requestId: req.requestId, status: req.status }
}

export function handleMockWithdrawRequest(requestId: number, token?: string): { requestId: number } {
  const empId = getEmpIdFromToken(token)
  if (!empId) {
    throw { status: 401, message: '未登录' }
  }

  const req = findMockRequestById(requestId)
  if (!req || req.empId !== empId) {
    throw { status: 404, message: '申请记录不存在' }
  }
  if (req.status !== 'PENDING') {
    throw { status: 409, message: '只有待审批的申请可以撤回' }
  }

  removeMockRequest(requestId)
  return { requestId }
}

function toApprovalItem(req: MockTrainingRequest): TrainingRequestApprovalItem {
  const course = getCourse(req.courseId)
  const emp = mockEmployeeDirectory[req.empId]
  return {
    requestId: req.requestId,
    employeeId: req.empId,
    employeeName: emp?.name ?? `员工 #${req.empId}`,
    deptName: emp?.deptName ?? null,
    courseId: req.courseId,
    courseName: course?.courseName ?? `课程 #${req.courseId}`,
    courseType: course?.courseType ?? null,
    reason: req.reason,
    status: req.status,
    createdAt: req.createdAt,
    budgetAmount: course?.budgetAmount ?? null,
    maxStudents: course?.maxStudents ?? null,
    deptApproveComment: req.reviewComment,
    deptApproveTime: req.reviewedAt,
    hrFileTime: req.filedAt,
  }
}

export function handleMockGetAllRequests(
  query: TrainingRequestApprovalQuery,
  token?: string,
): PagedResult<TrainingRequestApprovalItem> {
  if (!getEmpIdFromToken(token)) {
    throw { status: 401, message: '未登录' }
  }

  let list = [...mockTrainingRequests]
  if (query.status) {
    list = list.filter((r) => r.status === query.status)
  }
  list = list.sort((a, b) => b.createdAt.localeCompare(a.createdAt))

  const page = query.page ?? 1
  const pageSize = query.pageSize ?? 10
  const total = list.length
  const start = (page - 1) * pageSize
  const items = list.slice(start, start + pageSize).map(toApprovalItem)
  return { items, page, pageSize, total }
}

export function handleMockDeptApprove(
  requestId: number,
  comment: string | null,
  token?: string,
): { requestId: number } {
  if (!getEmpIdFromToken(token)) {
    throw { status: 401, message: '未登录' }
  }
  const ok = approveMockRequest(requestId, getApproverName(token), comment)
  if (!ok) {
    throw { status: 409, message: '只有待审批的申请可以审批' }
  }
  return { requestId }
}

export function handleMockDeptReject(
  requestId: number,
  comment: string,
  token?: string,
): { requestId: number } {
  if (!getEmpIdFromToken(token)) {
    throw { status: 401, message: '未登录' }
  }
  if (!comment || !comment.trim()) {
    throw { status: 400, message: '驳回时必须填写理由' }
  }
  const ok = rejectMockRequest(requestId, getApproverName(token), comment)
  if (!ok) {
    throw { status: 409, message: '只有待审批的申请可以驳回' }
  }
  return { requestId }
}

export function handleMockHrFile(requestId: number, token?: string): { requestId: number } {
  if (!getEmpIdFromToken(token)) {
    throw { status: 401, message: '未登录' }
  }
  const ok = fileMockRequest(requestId, getApproverName(token))
  if (!ok) {
    throw { status: 409, message: '只有主管已通过的申请可以备案' }
  }
  return { requestId }
}
