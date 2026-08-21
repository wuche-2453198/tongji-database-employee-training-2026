import type { PagedResult } from '@/types/api'
import type { CourseListItem, CourseDetail, CourseQuery, Trainer } from '@/types/course'
import { mockCourses, mockCourseDetails, mockTrainers } from '../data/courses'

export function handleMockGetCourseList(query: CourseQuery): PagedResult<CourseListItem> {
  let list = [...mockCourses]

  if (query.keyword) {
    const kw = query.keyword.toLowerCase()
    list = list.filter(
      (c) =>
        c.courseName.toLowerCase().includes(kw) ||
        (c.trainerName && c.trainerName.toLowerCase().includes(kw)),
    )
  }

  if (query.courseType) {
    list = list.filter((c) => c.courseType === query.courseType)
  }

  if (query.status) {
    list = list.filter((c) => c.courseStatus === query.status)
  }

  if (query.startDate) {
    list = list.filter((c) => c.startAt && c.startAt.slice(0, 10) >= query.startDate!)
  }
  if (query.endDate) {
    list = list.filter((c) => c.startAt && c.startAt.slice(0, 10) <= query.endDate!)
  }

  const page = query.page ?? 1
  const pageSize = query.pageSize ?? 12
  const total = list.length
  const start = (page - 1) * pageSize
  const items = list.slice(start, start + pageSize)

  return { items, page, pageSize, total }
}

export function handleMockGetTrainerList(): Trainer[] {
  return mockTrainers
}

export function handleMockGetCourseDetail(id: number): CourseDetail {
  const detail = mockCourseDetails[id]
  if (!detail) {
    throw { status: 404, message: '课程不存在' }
  }
  return detail
}

/** 同步更新列表与详情两份 Mock 数据，保证发布/关闭后两处状态一致 */
function syncMockCourseStatus(id: number, status: CourseListItem['courseStatus']): void {
  const course = mockCourses.find((c) => c.courseId === id)
  if (course) course.courseStatus = status
  const detail = mockCourseDetails[id]
  if (detail) detail.courseStatus = status
}

export function handleMockPublishCourse(id: number): { published: boolean } {
  const course = mockCourses.find((c) => c.courseId === id)
  if (!course) throw { status: 404, message: '课程不存在' }
  if (course.courseStatus === 'PUBLISHED') throw { status: 409, message: '课程已发布，不能重复发布' }
  syncMockCourseStatus(id, 'PUBLISHED')
  return { published: true }
}

export function handleMockCloseCourse(id: number): { closed: boolean } {
  const course = mockCourses.find((c) => c.courseId === id)
  if (!course) throw { status: 404, message: '课程不存在' }
  if (course.courseStatus !== 'PUBLISHED') throw { status: 409, message: '只有已发布的课程可以关闭' }
  syncMockCourseStatus(id, 'CLOSED')
  return { closed: true }
}
