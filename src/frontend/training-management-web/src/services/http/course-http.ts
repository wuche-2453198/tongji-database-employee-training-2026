import { requestApi } from '@/api/client'
import { mapCoursePage, mapRegistrationReceipt } from '@/api/mappers/course'
import type {
  ApiEnvelopeDto,
  CourseSummaryDto,
  PageDto,
  RegistrationReceiptDto,
} from '@/api/transport'
import type { CourseService } from '@/services/course'

export const httpCourseService: CourseService = {
  async listCourses(query, options) {
    const response = await requestApi<ApiEnvelopeDto<PageDto<CourseSummaryDto>>>({
      method: 'GET',
      url: '/api/courses',
      params: {
        courseName: query.keyword || undefined,
        page: query.page,
        pageSize: query.pageSize,
      },
      signal: options?.signal,
      operation: 'query',
    })
    return mapCoursePage(response)
  },

  async registerForCourse(courseId, options) {
    const response = await requestApi<ApiEnvelopeDto<RegistrationReceiptDto>, { courseId: string }>(
      {
        method: 'POST',
        url: '/api/registrations',
        data: { courseId },
        signal: options?.signal,
        operation: 'write',
      },
    )
    return mapRegistrationReceipt(response)
  },
}
