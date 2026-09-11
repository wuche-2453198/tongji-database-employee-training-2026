import type { DomainPage, DomainPageQuery, ServiceRequestOptions } from '@/domains/shared'

export type TestType = 'PRE' | 'POST' | 'UNKNOWN'

export interface TrainingTest {
  id: string
  employeeId: number
  employeeName: string
  courseId: string
  courseName: string
  testType: TestType
  score: number
  testDate: string
}

/** 按员工+课程聚合的成绩汇总：PRE/POST/分数变化/提升率。 */
export interface TestScoreSummary {
  employeeId: number
  employeeName: string
  courseId: string
  courseName: string
  preScore: number | null
  postScore: number | null
  change: number | null
  improvementRate: number | null
  updatedAt: string | null
}

export interface TestQuery extends DomainPageQuery {
  keyword?: string
  employeeKeyword?: string
  testType?: TestType
  startDateFrom?: string
  startDateTo?: string
}

export type TestQuerySummary = Omit<TestQuery, 'testType'>

export interface CreateTestCommand {
  employeeId: number
  courseId: string
  testType: TestType
  score: number
  testedAt?: string
}

export interface TestService {
  listManage(query: TestQuery, options?: ServiceRequestOptions): Promise<DomainPage<TrainingTest>>
  listSummaries(
    query: TestQuerySummary,
    options?: ServiceRequestOptions,
  ): Promise<DomainPage<TestScoreSummary>>
  create(command: CreateTestCommand, options?: ServiceRequestOptions): Promise<void>
}
