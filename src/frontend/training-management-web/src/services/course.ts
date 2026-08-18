import type { PageResult, ServiceRequestOptions } from '@/types/api'
import type { CourseListQuery, CourseSummary, RegistrationReceipt } from '@/types/course'

export interface CourseService {
  listCourses(
    query: CourseListQuery,
    options?: ServiceRequestOptions,
  ): Promise<PageResult<CourseSummary>>
  registerForCourse(courseId: string, options?: ServiceRequestOptions): Promise<RegistrationReceipt>
}

let servicePromise: Promise<CourseService> | undefined

export const getCourseService = (): Promise<CourseService> => {
  servicePromise ??=
    import.meta.env.VITE_USE_MOCK === 'true'
      ? import('@/mocks/course').then(({ mockCourseService }) => mockCourseService)
      : import('@/services/http/course-http').then(({ httpCourseService }) => httpCourseService)

  return servicePromise
}
