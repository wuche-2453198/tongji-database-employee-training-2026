import { requestApi } from '@/api/client'
import { fromEnvelopeFailure } from '@/api/error'
import { mapTrainingTest } from '@/api/mappers/test'
import type { ApiEnvelopeDto, PageDto, TrainingTestDto } from '@/api/transport'
import type { TestService } from '@/domains/test'

const queryParams = (query: Parameters<TestService['listManage']>[0]) => ({
  EmployeeName: query.employeeKeyword || undefined,
  CourseName: query.keyword || undefined,
  TestType: query.testType === 'UNKNOWN' ? undefined : query.testType || undefined,
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
}
