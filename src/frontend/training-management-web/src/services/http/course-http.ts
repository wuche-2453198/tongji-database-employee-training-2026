import { requestApi } from '@/api/client'
import { mapCourseDetail, mapCoursePage } from '@/api/mappers/course'
import type { ApiEnvelopeDto, CourseSummaryDto, PageDto } from '@/api/transport'
import type { CourseService } from '@/services/course'

export const httpCourseService: CourseService = {
  async listCourses(query, options) {
    const response = await requestApi<ApiEnvelopeDto<PageDto<CourseSummaryDto>>>({
      method: 'GET',
      url: '/api/courses',
      params: {
        courseName: query.keyword || undefined,
        courseType: query.type || undefined,
        status: query.status || undefined,
        startDateFrom: query.startDateFrom || undefined,
        startDateTo: query.startDateTo || undefined,
        page: query.page,
        pageSize: query.pageSize,
        sortBy: query.sortBy || 'startTime',
        sortDirection: query.sortDirection || 'desc',
      },
      signal: options?.signal,
      operation: 'query',
    })
    return mapCoursePage(response)
  },

  async getCourse(courseId, options) {
    // TODO(API-Q-009): 最终详情路径、资格字段和 DTO 包裹结构待 OpenAPI 冻结。
    const response = await requestApi<ApiEnvelopeDto<import('@/api/transport').CourseDetailDto>>({
      method: 'GET',
      url: `/api/courses/${encodeURIComponent(courseId)}`,
      signal: options?.signal,
      operation: 'query',
    })
    return mapCourseDetail(response)
  },

  async getActionEligibility(courseId, options) {
    // TODO(API-Q-009): 若资格独立接口冻结，应在此适配；当前先从详情响应承接。
    return (await this.getCourse(courseId, options)).eligibility
  },
}
