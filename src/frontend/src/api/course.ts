import { apiRequest } from './client'
import type { PagedResult } from '@/types/api'
import type { CourseListItem, CourseDetail, CourseQuery, Trainer } from '@/types/course'

/** 获取课程列表 */
export function getCourseListApi(query: CourseQuery) {
  return apiRequest<PagedResult<CourseListItem>>('GET', '/api/courses', query)
}

/** 获取课程详情 */
export function getCourseDetailApi(courseId: number) {
  return apiRequest<CourseDetail>('GET', `/api/courses/${courseId}`)
}

/** 获取讲师列表（待联调） */
export function getTrainerListApi() {
  return apiRequest<Trainer[]>('GET', '/api/trainers')
}
