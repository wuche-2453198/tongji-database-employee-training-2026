import type {
  ActionEligibility,
  DomainPage,
  DomainPageQuery,
  ServiceRequestOptions,
} from '@/domains/shared'

export type CourseType = 'SKILL' | 'MANAGEMENT' | 'SAFETY' | 'UNKNOWN'
export type CourseStatus = 'DRAFT' | 'PUBLISHED' | 'CLOSED' | 'UNKNOWN'
export type CourseAction = 'apply' | 'register'

export interface CourseQuery extends DomainPageQuery {
  keyword?: string
  type?: CourseType
  status?: CourseStatus
  startDateFrom?: string
  startDateTo?: string
}

export interface CourseSummary {
  id: string
  name: string
  type: CourseType
  typeLabel: string
  trainerName: string
  startTime: string
  endTime: string
  location: string
  status: CourseStatus
  statusLabel: string
  maxStudents: number
  registeredCount: number
  remainingSeats: number
}

export interface CourseTrainer {
  id: string
  name: string
  title: string
  department: string
  expertise: string
  rating?: number
}

export interface CourseMaterial {
  id: string
  name: string
  kind: 'DOCUMENT' | 'LINK' | 'UNKNOWN'
  available: boolean
}

export interface CourseActionEligibility {
  apply: ActionEligibility
  register: ActionEligibility
}

export interface CourseDetail extends CourseSummary {
  description: string
  objectives: string[]
  hours: number | null
  organizer: string
  trainer: CourseTrainer
  materials: CourseMaterial[]
  eligibility: CourseActionEligibility
}

export interface CourseService {
  listCourses(
    query: CourseQuery,
    options?: ServiceRequestOptions,
  ): Promise<DomainPage<CourseSummary>>
  getCourse(courseId: string, options?: ServiceRequestOptions): Promise<CourseDetail>
  getActionEligibility(
    courseId: string,
    options?: ServiceRequestOptions,
  ): Promise<CourseActionEligibility>
}
