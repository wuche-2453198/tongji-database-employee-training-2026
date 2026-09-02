import type { TestQuery, TestService } from '@/domains/test'
import { getMockActor, mockBusinessRepository } from '@/mocks/repositories/business-repository'
import { createMockError, currentMockScenario, mockWait } from '@/mocks/scenarios'

const PAGE_SIZES = [10, 20, 50]

function pageOf<T>(items: T[], query: TestQuery) {
  const page = Number.isInteger(query.page) && query.page > 0 ? query.page : 1
  const pageSize = PAGE_SIZES.includes(query.pageSize) ? query.pageSize : 20
  const start = (page - 1) * pageSize
  return { items: items.slice(start, start + pageSize), page, pageSize, total: items.length }
}

export const mockTestService: TestService = {
  async listManage(query, options) {
    await mockWait(options?.signal, 50)
    if (!['HR', 'ADMIN'].includes(getMockActor().role)) throw createMockError('forbidden')
    if (currentMockScenario() === 'failure') throw createMockError('failure')
    let items = mockBusinessRepository.getState().tests
    const keyword = query.keyword?.trim().toLowerCase()
    const employeeKeyword = query.employeeKeyword?.trim().toLowerCase()
    if (keyword) items = items.filter((item) => item.courseName.toLowerCase().includes(keyword))
    if (employeeKeyword)
      items = items.filter(
        (item) =>
          item.employeeName.toLowerCase().includes(employeeKeyword) ||
          String(item.employeeId).includes(employeeKeyword),
      )
    if (query.testType) items = items.filter((item) => item.testType === query.testType)
    if (query.startDateFrom)
      items = items.filter((item) => item.testDate.slice(0, 10) >= query.startDateFrom!)
    if (query.startDateTo)
      items = items.filter((item) => item.testDate.slice(0, 10) <= query.startDateTo!)
    return pageOf(
      items.sort((left, right) => right.testDate.localeCompare(left.testDate)),
      query,
    )
  },
}
