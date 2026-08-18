export type CourseType = 'SKILL' | 'MANAGEMENT' | 'SAFETY' | 'UNKNOWN'
export type CourseStatus = 'DRAFT' | 'PUBLISHED' | 'CLOSED' | 'UNKNOWN'

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

export interface CourseListQuery {
  keyword?: string
  page: number
  pageSize: number
}

export interface RegistrationReceipt {
  registrationId: string
  courseId: string
  status: 'REGISTERED'
  registeredAt: string
}
