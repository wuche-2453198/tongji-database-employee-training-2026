import { requestApi } from '@/api/client'
import { mapCourseDetail, mapCoursePage } from '@/api/mappers/course'
import type { ApiEnvelopeDto, CourseSummaryDto, PageDto } from '@/api/transport'
import type { CourseEditorInput, CourseService } from '@/domains/course'

const toPayload = (input: CourseEditorInput) => ({
  CourseName: input.name, CourseType: input.type, DurationHours: input.durationHours,
  TrainerId: Number(input.trainerId), MaxStudents: input.maxStudents, StartAt: input.startAt,
  EndAt: input.endAt, Location: input.location, BudgetAmount: input.budgetAmount, DeptId: Number(input.deptId),
  PreTestUrl: input.preTestUrl || undefined, PostTestUrl: input.postTestUrl || undefined,
  MaterialUrl: input.materialUrl || undefined,
})

export const httpCourseService: CourseService = {
  async listCourses(query, options) {
    const response = await requestApi<ApiEnvelopeDto<PageDto<CourseSummaryDto>>>({
      method: 'GET',
      url: '/api/courses',
      params: {
        CourseName: query.keyword || undefined,
        CourseType: query.type === 'UNKNOWN' ? undefined : query.type || undefined,
        CourseStatus: query.status === 'UNKNOWN' ? undefined : query.status || undefined,
        StartAtFrom: query.startDateFrom || undefined,
        StartAtTo: query.startDateTo || undefined,
        Page: query.page,
        PageSize: query.pageSize,
      },
      signal: options?.signal,
      operation: 'query',
    })
    return mapCoursePage(response)
  },

  async getCourse(courseId, options) {
    const response = await requestApi<ApiEnvelopeDto<import('@/api/transport').CourseDetailDto>>({
      method: 'GET',
      url: `/api/courses/${encodeURIComponent(courseId)}`,
      signal: options?.signal,
      operation: 'query',
    })
    return mapCourseDetail(response)
  },

  async getActionEligibility(courseId, options) {
    // 当前后端没有独立资格接口；资格缺失时映射为禁止操作，由服务端写接口最终校验。
    return (await this.getCourse(courseId, options)).eligibility
  },

  async publishCourse(courseId, options) {
    await requestApi<ApiEnvelopeDto<unknown>>({
      method: 'PATCH',
      url: `/api/courses/${encodeURIComponent(courseId)}/publish`,
      signal: options?.signal,
      operation: 'write',
    })
  },

  async closeCourse(courseId, options) {
    await requestApi<ApiEnvelopeDto<unknown>>({
      method: 'PATCH',
      url: `/api/courses/${encodeURIComponent(courseId)}/close`,
      signal: options?.signal,
      operation: 'write',
    })
  },

  async createCourse(input, options) {
    const response = await requestApi<ApiEnvelopeDto<import('@/api/transport').CourseDetailDto>>({
      method: 'POST', url: '/api/courses', data: toPayload(input), signal: options?.signal, operation: 'write',
    })
    return mapCourseDetail(response)
  },

  async updateCourse(courseId, input, options) {
    const response = await requestApi<ApiEnvelopeDto<import('@/api/transport').CourseDetailDto>>({
      method: 'PUT', url: `/api/courses/${encodeURIComponent(courseId)}`, data: toPayload(input),
      signal: options?.signal, operation: 'write',
    })
    return mapCourseDetail(response)
  },
}
