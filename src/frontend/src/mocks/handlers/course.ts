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

  if (query.trainerId) {
    list = list.filter((c) => c.trainerId === query.trainerId)
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
