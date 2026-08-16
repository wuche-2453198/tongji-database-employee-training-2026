/**
 * 课程封面统一映射。按课程类型使用固定封面，不在页面中根据课程名散落判断。
 * 图片为装饰性内容，加载失败由 CourseCover 组件降级为渐变占位。
 */
export const COURSE_COVER_MAP: Record<string, string> = {
  技术培训: '/images/course-technology.webp',
  管理培训: '/images/course-management.webp',
  产品培训: '/images/course-product.webp',
  营销培训: '/images/course-marketing.webp',
}

/** 个别课程的封面覆盖（按 courseId），优先级高于按类型映射 */
export const COURSE_COVER_OVERRIDE: Record<number, string> = {}

/**
 * 封面解析顺序：课程级覆盖 → 类型级映射 → undefined（降级为占位）。
 * courseId 为 0 或 NaN 时按未提供处理，走类型映射。
 */
export function getCourseCover(
  courseId: number | null | undefined,
  courseType: string | null | undefined,
): string | undefined {
  if (courseId) {
    const override = COURSE_COVER_OVERRIDE[courseId]
    if (override) return override
  }
  if (!courseType) return undefined
  return COURSE_COVER_MAP[courseType]
}
