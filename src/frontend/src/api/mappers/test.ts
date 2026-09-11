import type { TestScoreSummaryDto, TrainingTestDto } from '@/api/transport'
import type { TestScoreSummary, TestType, TrainingTest } from '@/domains/test'

function mapType(value: string): TestType {
  if (value === 'PRE') return 'PRE'
  if (value === 'POST') return 'POST'
  return 'UNKNOWN'
}

/** 保留 null 语义：缺失成绩区别于 0 分。 */
function mapScore(value: number | null | undefined): number | null {
  return value === null || value === undefined ? null : Number(value)
}

export function mapTrainingTest(dto: TrainingTestDto): TrainingTest {
  return {
    id: String(dto.testId),
    employeeId: dto.empId,
    employeeName: dto.employeeName || '—',
    courseId: String(dto.courseId),
    courseName: dto.courseName || '未命名课程',
    testType: mapType(dto.testType),
    score: Number(dto.score),
    testDate: dto.testedAt,
  }
}

export function mapTestScoreSummary(dto: TestScoreSummaryDto): TestScoreSummary {
  return {
    employeeId: dto.empId,
    employeeName: dto.employeeName || '—',
    courseId: String(dto.courseId),
    courseName: dto.courseName || '未命名课程',
    preScore: mapScore(dto.preScore),
    postScore: mapScore(dto.postScore),
    change: mapScore(dto.change),
    improvementRate: mapScore(dto.improvementRate),
    updatedAt: dto.updatedAt ?? null,
  }
}
