import type { CourseStatus } from './enums'

/** 课程列表项（对应 TRAINING_COURSES + TRAINERS 联查） */
export interface CourseListItem {
  courseId: number
  courseName: string
  courseType: string
  durationHours: number
  trainerId: number | null
  trainerName: string | null
  maxStudents: number
  enrolledCount: number
  startAt: string | null
  endAt: string | null
  location: string | null
  courseStatus: CourseStatus
  budgetAmount: number
  deptId: number | null
  deptName: string | null
}

/** 课程详情 */
export interface CourseDetail extends CourseListItem {
  preTestUrl: string | null
  postTestUrl: string | null
  materialUrl: string | null
  createdAt: string
  updatedAt: string
  trainerTitle?: string | null
  trainerCompany?: string | null
  trainerStarLevel?: number | null
  trainerEmail?: string | null
  trainerPhone?: string | null
}

/** 课程查询参数 */
export interface CourseQuery {
  keyword?: string
  courseType?: string
  status?: CourseStatus
  trainerId?: number
  startDate?: string
  endDate?: string
  page?: number
  pageSize?: number
}

/** 讲师 */
export interface Trainer {
  trainerId: number
  trainerName: string
  title: string | null
  company: string | null
  phone: string | null
  email: string | null
  starLevel: number
  isInternal: string
}
