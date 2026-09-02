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

export interface TestQuery extends DomainPageQuery {
  keyword?: string
  employeeKeyword?: string
  testType?: TestType
  startDateFrom?: string
  startDateTo?: string
}

export interface TestService {
  listManage(query: TestQuery, options?: ServiceRequestOptions): Promise<DomainPage<TrainingTest>>
}
