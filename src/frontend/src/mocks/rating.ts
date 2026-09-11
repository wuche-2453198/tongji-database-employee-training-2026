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

  async listAll(query, options) {
    await mockWait(options?.signal, 50)
    const actor = getMockActor()
    if (actor.role !== 'HR' && actor.role !== 'ADMIN') throw createMockError('forbidden')
    const keyword = query.keyword?.trim().toLowerCase()
    const items = mockBusinessRepository.getState().ratings.filter(
      (item) =>
        (!keyword || `${item.courseName}${item.trainerName}${item.employeeName}`.toLowerCase().includes(keyword)) &&
        (!query.courseId || item.courseId === query.courseId) &&
        (!query.trainerId || item.trainerId === query.trainerId),
    )
    return pageOf(items.sort((left, right) => right.ratingDate.localeCompare(left.ratingDate)), query)
  },

  async create(input, options) {
    await mockWait(options?.signal, 50)
    const actor = getMockActor()
    if (actor.role !== 'EMPLOYEE') throw createMockError('forbidden')
    const course = mockBusinessRepository.getState().courses.find((item) => item.id === input.courseId)
    if (!course || String(course.trainer.id) !== input.trainerId) throw createMockError('not-found')
    if (!Number.isInteger(input.score) || input.score < 1 || input.score > 5) throw createMockError('failure')
    const employeeName = mockBusinessRepository.getState().employees.find(
      (employee) => employee.empId === actor.employeeId,
    )?.empName ?? '当前员工'
    mockBusinessRepository.update((next) => {
      next.ratings.unshift({ id: String(Date.now()), courseId: input.courseId, courseName: course.name,
        trainerId: input.trainerId, trainerName: course.trainer.name, employeeId: actor.employeeId,
        employeeName, score: input.score, comment: input.comment?.trim() || null,
        hrVerified: 'N', hrComment: null, ratingDate: new Date().toISOString() })
    })
  },

  async verify(id, comment, options) {
    await mockWait(options?.signal, 50)
    const actor = getMockActor()
    if (actor.role !== 'HR' && actor.role !== 'ADMIN') throw createMockError('forbidden')
    let found = false
    mockBusinessRepository.update((next) => {
      const rating = next.ratings.find((item) => item.id === id)
      if (!rating) return
      rating.hrVerified = 'Y'
      rating.hrComment = comment?.trim() || null
      found = true
    })
    if (!found) throw createMockError('not-found')
  },
}
