import type { CourseType } from '@/domains/course'

const COVER_BY_TYPE: Partial<Record<CourseType, string>> = {
  技术培训: '/images/course-technology.webp',
  管理培训: '/images/course-management.webp',
  产品培训: '/images/course-product.webp',
  营销培训: '/images/course-marketing.webp',
}

export function resolveCourseCover(type: CourseType): string {
  return COVER_BY_TYPE[type] ?? ''
}
