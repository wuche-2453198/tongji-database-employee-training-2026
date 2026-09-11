import type { DomainPage, DomainPageQuery, ServiceRequestOptions } from '@/domains/shared'

export type RatingVerifiedStatus = 'N' | 'Y' | 'UNKNOWN'

export interface CourseRating {
  id: string
  courseId: string
  courseName: string
  trainerId: string
  trainerName: string
  employeeId: number
  employeeName: string
  score: number
  comment: string | null
  hrVerified: RatingVerifiedStatus
  hrComment: string | null
  ratingDate: string
}

export interface RatingQuery extends DomainPageQuery {
  keyword?: string
  courseId?: string
  trainerId?: string
  startDateFrom?: string
  startDateTo?: string
}

export interface CreateRatingInput {
  courseId: string
  trainerId: string
  score: number
  comment?: string
}

export interface RatingService {
  listMine(query: RatingQuery, options?: ServiceRequestOptions): Promise<DomainPage<CourseRating>>
  listAll(query: RatingQuery, options?: ServiceRequestOptions): Promise<DomainPage<CourseRating>>
  create(input: CreateRatingInput, options?: ServiceRequestOptions): Promise<void>
  verify(id: string, comment?: string, options?: ServiceRequestOptions): Promise<void>
}
