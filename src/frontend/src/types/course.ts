export type {
  CourseActionEligibility,
  CourseDetail,
  CourseMaterial,
  CourseQuery,
  CourseService,
  CourseStatus,
  CourseSummary,
  CourseTrainer,
  CourseType,
} from '@/domains/course'

export type CourseListQuery = import('@/domains/course').CourseQuery

export interface RegistrationReceipt {
  registrationId: string
  courseId: string
  status: 'REGISTERED'
  registeredAt: string
}
