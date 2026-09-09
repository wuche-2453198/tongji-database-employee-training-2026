import type { RatingQuery, RatingService } from '@/domains/rating'
import { getMockActor, mockBusinessRepository } from '@/mocks/repositories/business-repository'
import { createMockError, currentMockScenario, mockWait } from '@/mocks/scenarios'

const PAGE_SIZES = [10, 20, 50]

function pageOf<T>(items: T[], query: RatingQuery) {
  const page = Number.isInteger(query.page) && query.page > 0 ? query.page : 1
  const pageSize = PAGE_SIZES.includes(query.pageSize) ? query.pageSize : 20
  const start = (page - 1) * pageSize
  return { items: items.slice(start, start + pageSize), page, pageSize, total: items.length }
}

export const mockRatingService: RatingService = {
  async listMine(query, options) {
    await mockWait(options?.signal, 50)
    if (currentMockScenario() === 'failure') throw createMockError('failure')
    const actor = getMockActor()
    let items = mockBusinessRepository
      .getState()
      .ratings.filter((item) => item.employeeId === actor.employeeId)
    const keyword = query.keyword?.trim().toLowerCase()
    if (keyword)
      items = items.filter(
        (item) =>
          item.courseName.toLowerCase().includes(keyword) ||
          item.trainerName.toLowerCase().includes(keyword),
      )
    if (query.startDateFrom)
      items = items.filter((item) => item.ratingDate.slice(0, 10) >= query.startDateFrom!)
    if (query.startDateTo)
      items = items.filter((item) => item.ratingDate.slice(0, 10) <= query.startDateTo!)
    return pageOf(
      items.sort((left, right) => right.ratingDate.localeCompare(left.ratingDate)),
      query,
    )
  },
}
