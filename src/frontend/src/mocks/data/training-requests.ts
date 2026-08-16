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
]

let nextId = 5

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
