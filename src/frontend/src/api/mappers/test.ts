import type { TrainingTestDto } from '@/api/transport'
import type { TestType, TrainingTest } from '@/domains/test'

function mapType(value: string): TestType {
  if (value === 'PRE') return 'PRE'
  if (value === 'POST') return 'POST'
  return 'UNKNOWN'
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
