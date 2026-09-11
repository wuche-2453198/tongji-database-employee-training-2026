import { requestApi } from '@/api/client'
import { fromEnvelopeFailure } from '@/api/error'
import { mapTestScoreSummary, mapTrainingTest } from '@/api/mappers/test'
import type { ApiEnvelopeDto, PageDto, TestScoreSummaryDto, TrainingTestDto } from '@/api/transport'
import type { CreateTestCommand, TestService } from '@/domains/test'

const queryParams = (query: Parameters<TestService['listManage']>[0]) => ({
  EmployeeName: query.employeeKeyword || undefined,
  CourseName: query.keyword || undefined,
  TestType: query.testType === 'UNKNOWN' ? undefined : query.testType || undefined,
  StartDateFrom: query.startDateFrom || undefined,
  StartDateTo: query.startDateTo || undefined,
  Page: query.page,
  PageSize: query.pageSize,
})

const summaryQueryParams = (query: Parameters<TestService['listSummaries']>[0]) => ({
  EmployeeName: query.employeeKeyword || undefined,
  CourseName: query.keyword || undefined,
  StartDateFrom: query.startDateFrom || undefined,
  StartDateTo: query.startDateTo || undefined,
  Page: query.page,
  PageSize: query.pageSize,
})

export const httpTestService: TestService = {
  async listManage(query, options) {
    const envelope = await requestApi<ApiEnvelopeDto<PageDto<TrainingTestDto>>>({
      method: 'GET',
      url: '/api/tests',
      params: queryParams(query),
      signal: options?.signal,
      operation: 'query',
    })
    if (!envelope.success || !envelope.data) throw fromEnvelopeFailure(envelope)
    return { ...envelope.data, items: envelope.data.items.map(mapTrainingTest) }
  },
  async listSummaries(query, options) {
    const envelope = await requestApi<ApiEnvelopeDto<PageDto<TestScoreSummaryDto>>>({
      method: 'GET',
      url: '/api/tests/summary',
      params: summaryQueryParams(query),
      signal: options?.signal,
      operation: 'query',
    })
    if (!envelope.success || !envelope.data) throw fromEnvelopeFailure(envelope)
    return { ...envelope.data, items: envelope.data.items.map(mapTestScoreSummary) }
  },
  async create(command: CreateTestCommand, options) {
    const envelope = await requestApi<ApiEnvelopeDto<boolean>>({
      method: 'POST',
      url: '/api/tests',
      data: {
        EmpId: command.employeeId,
        CourseId: Number(command.courseId),
        TestType: command.testType === 'UNKNOWN' ? undefined : command.testType,
        Score: command.score,
        TestedAt: command.testedAt || undefined,
      },
      signal: options?.signal,
      operation: 'write',
    })
    if (!envelope.success) throw fromEnvelopeFailure(envelope)
  },
}
