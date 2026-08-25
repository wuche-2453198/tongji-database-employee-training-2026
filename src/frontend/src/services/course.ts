import type { CourseService } from '@/domains/course'

export type { CourseService } from '@/domains/course'

let servicePromise: Promise<CourseService> | undefined

export const getCourseService = (): Promise<CourseService> => {
  servicePromise ??=
    import.meta.env.VITE_USE_MOCK === 'true'
      ? import('@/mocks/course').then(({ mockCourseService }) => mockCourseService)
      : import('@/services/http/course-http').then(({ httpCourseService }) => httpCourseService)

  return servicePromise
}
