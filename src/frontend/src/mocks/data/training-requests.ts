import type { TrainingRequestStatus } from '@/types/enums'

/** 内存中的申请记录（不含课程冗余字段，展示时与 mockCourses 联查） */
export interface MockTrainingRequest {
  requestId: number
  empId: number
  courseId: number
  reason: string
  expectedGain: string | null
  status: TrainingRequestStatus
  createdAt: string
  updatedAt: string
  reviewedAt: string | null
  reviewerName: string | null
  reviewComment: string | null
  filedAt: string | null
  filedByName: string | null
  filingComment: string | null
}

export const mockTrainingRequests: MockTrainingRequest[] = [
  {
    requestId: 1,
    empId: 2,
    courseId: 2,
    reason: '想深入学习 .NET Web API 开发',
    expectedGain: '掌握企业内部 API 开发标准',
    status: 'PENDING',
    createdAt: '2026-08-01T10:00:00',
    updatedAt: '2026-08-01T10:00:00',
    reviewedAt: null,
    reviewerName: null,
    reviewComment: null,
    filedAt: null,
    filedByName: null,
    filingComment: null,
  },
  {
    requestId: 2,
    empId: 2,
    courseId: 1,
    reason: '了解微服务架构演进方向，为团队技术选型做准备',
    expectedGain: '形成微服务落地路线图',
    status: 'DEPT_APPROVED',
    createdAt: '2026-07-25T09:00:00',
    updatedAt: '2026-07-28T15:30:00',
    reviewedAt: '2026-07-28T15:30:00',
    reviewerName: '王主管',
    reviewComment: '方向契合团队规划，同意推荐',
    filedAt: null,
    filedByName: null,
    filingComment: null,
  },
  {
    requestId: 3,
    empId: 2,
    courseId: 4,
    reason: '提升产品设计能力',
    expectedGain: null,
    status: 'DEPT_REJECTED',
    createdAt: '2026-07-20T14:00:00',
    updatedAt: '2026-07-22T11:00:00',
    reviewedAt: '2026-07-22T11:00:00',
    reviewerName: '王主管',
    reviewComment: '本季度部门预算已用尽，建议下季度再申请',
    filedAt: null,
    filedByName: null,
    filingComment: null,
  },
  {
    requestId: 4,
    empId: 2,
    courseId: 3,
    reason: '学习敏捷项目管理方法，提升项目交付效率',
    expectedGain: '考取敏捷相关认证',
    status: 'HR_FILED',
    createdAt: '2026-07-10T08:30:00',
    updatedAt: '2026-07-15T16:00:00',
    reviewedAt: '2026-07-12T10:00:00',
    reviewerName: '王主管',
    reviewComment: '同意，作为部门敏捷推广种子',
    filedAt: '2026-07-15T16:00:00',
    filedByName: '李HR',
    filingComment: '已登记培训档案，可正常参训',
  },
  {
    requestId: 5,
    empId: 5,
    courseId: 5,
    reason: '提升 B2B 销售技巧，冲刺下半年业绩',
    expectedGain: '掌握大客户销售方法论',
    status: 'PENDING',
    createdAt: '2026-08-05T09:30:00',
    updatedAt: '2026-08-05T09:30:00',
    reviewedAt: null,
    reviewerName: null,
    reviewComment: null,
    filedAt: null,
    filedByName: null,
    filingComment: null,
  },
  {
    requestId: 6,
    empId: 6,
    courseId: 4,
    reason: '学习产品设计方法，优化用户体验',
    expectedGain: null,
    status: 'DEPT_APPROVED',
    createdAt: '2026-08-03T14:00:00',
    updatedAt: '2026-08-06T10:00:00',
    reviewedAt: '2026-08-06T10:00:00',
    reviewerName: '王主管',
    reviewComment: '产品线需要，同意',
    filedAt: null,
    filedByName: null,
    filingComment: null,
  },
]

/** 员工目录（Mock 联查，真实后端应从 EMPLOYEE 表关联查询） */
export const mockEmployeeDirectory: Record<number, { name: string; deptName: string }> = {
  2: { name: '测试普通员工', deptName: '研发部' },
  3: { name: '测试部门主管', deptName: '研发部' },
  4: { name: '测试HR专员', deptName: '人力资源部' },
  5: { name: '张三', deptName: '研发部' },
  6: { name: '李四', deptName: '产品部' },
}

let nextId = 7

export function addMockRequest(
  empId: number,
  courseId: number,
  reason: string,
  expectedGain?: string,
): MockTrainingRequest {
  const now = new Date().toISOString()
  const req: MockTrainingRequest = {
    requestId: nextId++,
    empId,
    courseId,
    reason,
    expectedGain: expectedGain || null,
    status: 'PENDING',
    createdAt: now,
    updatedAt: now,
    reviewedAt: null,
    reviewerName: null,
    reviewComment: null,
    filedAt: null,
    filedByName: null,
    filingComment: null,
  }
  mockTrainingRequests.push(req)
  return req
}

export function findMockRequest(empId: number, courseId: number): MockTrainingRequest | undefined {
  return mockTrainingRequests.find((r) => r.empId === empId && r.courseId === courseId)
}

export function findMockRequestById(requestId: number): MockTrainingRequest | undefined {
  return mockTrainingRequests.find((r) => r.requestId === requestId)
}

export function removeMockRequest(requestId: number): boolean {
  const idx = mockTrainingRequests.findIndex((r) => r.requestId === requestId)
  if (idx === -1) return false
  mockTrainingRequests.splice(idx, 1)
  return true
}

function touch(req: MockTrainingRequest): void {
  const now = new Date().toISOString()
  req.updatedAt = now
}

/** 主管审批通过 */
export function approveMockRequest(
  requestId: number,
  approverName: string,
  comment: string | null,
): boolean {
  const req = findMockRequestById(requestId)
  if (!req || req.status !== 'PENDING') return false
  req.status = 'DEPT_APPROVED'
  req.reviewedAt = new Date().toISOString()
  req.reviewerName = approverName
  req.reviewComment = comment || null
  touch(req)
  return true
}

/** 主管驳回 */
export function rejectMockRequest(
  requestId: number,
  approverName: string,
  comment: string,
): boolean {
  const req = findMockRequestById(requestId)
  if (!req || req.status !== 'PENDING') return false
  req.status = 'DEPT_REJECTED'
  req.reviewedAt = new Date().toISOString()
  req.reviewerName = approverName
  req.reviewComment = comment || null
  touch(req)
  return true
}

/** HR 备案 */
export function fileMockRequest(requestId: number, hrName: string): boolean {
  const req = findMockRequestById(requestId)
  if (!req || req.status !== 'DEPT_APPROVED') return false
  req.status = 'HR_FILED'
  req.filedAt = new Date().toISOString()
  req.filedByName = hrName
  touch(req)
  return true
}
