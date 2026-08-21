import { apiRequest } from './client'
import { useMock } from '@/config/mock'
import type { PagedResult } from '@/types/api'
import type { CourseListItem, CourseDetail, CourseQuery, Trainer } from '@/types/course'

/**
 * 将前端 camelCase 查询映射为后端 PascalCase 查询参数。
 * 注意：后端 CourseQuery 不支持按讲师筛选，故 trainerId 在此处不映射。
 */
interface BackendCourseQuery {
  CourseName?: string
  CourseType?: string
  CourseStatus?: string
  StartAtFrom?: string
  StartAtTo?: string
  Page: number
  PageSize: number
}

function buildCourseQuery(query: CourseQuery): BackendCourseQuery {
  return {
    CourseName: query.keyword || undefined,
    CourseType: query.courseType,
    CourseStatus: query.status,
    StartAtFrom: query.startDate,
    StartAtTo: query.endDate,
    Page: query.page ?? 1,
    PageSize: query.pageSize ?? 12,
  }
}

/** 获取课程列表 */
export function getCourseListApi(query: CourseQuery) {
  const mock = useMock('course')
  return apiRequest<PagedResult<CourseListItem>>(
    'GET',
    '/api/courses',
    mock ? query : buildCourseQuery(query),
    { mock },
  )
}

/** 获取课程详情 */
export function getCourseDetailApi(courseId: number) {
  return apiRequest<CourseDetail>('GET', `/api/courses/${courseId}`, undefined, {
    mock: useMock('course'),
  })
}

/** 获取讲师列表（仅 HR/Admin 可访问，普通员工调用会 403） */
export function getTrainerListApi() {
  return apiRequest<Trainer[]>('GET', '/api/trainers', undefined, {
    mock: useMock('course'),
  })
}

/** 发布课程（HR/Admin） */
export function publishCourseApi(courseId: number) {
  return apiRequest<{ published: boolean }>(
    'PATCH',
    `/api/courses/${courseId}/publish`,
    {},
    { mock: useMock('course') },
  )
}

/** 关闭课程（HR/Admin） */
export function closeCourseApi(courseId: number) {
  return apiRequest<{ closed: boolean }>(
    'PATCH',
    `/api/courses/${courseId}/close`,
    {},
    { mock: useMock('course') },
  )
}
