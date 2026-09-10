import type { TrainerRatingDto } from '@/api/transport'
import type { CourseRating, RatingVerifiedStatus } from '@/domains/rating'

function mapVerified(value: string): RatingVerifiedStatus {
  if (value === 'Y') return 'Y'
  if (value === 'N') return 'N'
  return 'UNKNOWN'
}

export function mapCourseRating(dto: TrainerRatingDto): CourseRating {
  return {
    id: String(dto.ratingId),
    courseId: String(dto.courseId),
    courseName: dto.courseName || '未命名课程',
    trainerId: String(dto.trainerId),
    trainerName: dto.trainerName || '—',
    employeeId: dto.empId,
    employeeName: dto.employeeName || '—',
    score: Number(dto.score),
    comment: dto.ratingComment ?? null,
    hrVerified: mapVerified(dto.hrVerified),
    hrComment: dto.hrComment ?? null,
    ratingDate: dto.ratedAt,
  }
}
