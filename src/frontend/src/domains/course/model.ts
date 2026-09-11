import type {
  ActionEligibility,
  DomainPage,
  DomainPageQuery,
  ServiceRequestOptions,
} from '@/domains/shared'

export type CourseType = '技术培训' | '管理培训' | '产品培训' | '营销培训' | 'UNKNOWN'
export type CourseStatus = 'DRAFT' | 'PUBLISHED' | 'CLOSED' | 'UNKNOWN'
export type CourseAction = 'apply' | 'register'

export const COURSE_TYPES: readonly CourseType[] = [
  '技术培训',
  '管理培训',
  '产品培训',
  '营销培训',
] as const

export const COURSE_TYPE_LABELS: Record<CourseType, string> = {
  技术培训: '技术培训',
  管理培训: '管理培训',
  产品培训: '产品培训',
  营销培训: '营销培训',
  UNKNOWN: '未知类型',
}

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
  registeredCount: number | null
  remainingSeats: number | null
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
  trainerId?: string
  deptId?: string
  budgetAmount?: number
  preTestUrl?: string | null
  postTestUrl?: string | null
  materialUrl?: string | null
}

export interface CourseEditorInput {
  name: string
  type: Exclude<CourseType, 'UNKNOWN'>
  durationHours: number
  trainerId: string
  maxStudents: number
  startAt: string
  endAt: string
  location: string
  budgetAmount: number
  deptId: string
  preTestUrl?: string
  postTestUrl?: string
  materialUrl?: string
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
  publishCourse(courseId: string, options?: ServiceRequestOptions): Promise<void>
  closeCourse(courseId: string, options?: ServiceRequestOptions): Promise<void>
  createCourse(input: CourseEditorInput, options?: ServiceRequestOptions): Promise<CourseDetail>
  updateCourse(
    courseId: string,
    input: CourseEditorInput,
    options?: ServiceRequestOptions,
  ): Promise<CourseDetail>
}
