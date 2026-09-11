import type {
  CreateTestCommand,
  TestQuery,
  TestQuerySummary,
  TestScoreSummary,
  TestService,
  TestType,
  TrainingTest,
} from '@/domains/test'
import { domainError } from '@/domains/errors'
import { getMockActor, mockBusinessRepository } from '@/mocks/repositories/business-repository'
import { createMockError, currentMockScenario, mockWait } from '@/mocks/scenarios'

const PAGE_SIZES = [10, 20, 50]
const VALID_REGISTRATION_STATUSES = new Set(['REGISTERED', 'SIGNED_IN', 'COMPLETED'])

function canManage(): boolean {
  const role = getMockActor().role
  return role === 'HR' || role === 'ADMIN'
}

/** 越权防护:HR/管理员可查任意员工,其余角色只能看到本人成绩(与后端一致)。 */
function visibleTests(): TrainingTest[] {
  const tests = mockBusinessRepository.getState().tests
  if (canManage()) return tests
  return tests.filter((item) => item.employeeId === getMockActor().employeeId)
}

function pageOf<T>(items: T[], query: { page: number; pageSize: number }) {
  const page = Number.isInteger(query.page) && query.page > 0 ? query.page : 1
  const pageSize = PAGE_SIZES.includes(query.pageSize) ? query.pageSize : 20
  const start = (page - 1) * pageSize
  return { items: items.slice(start, start + pageSize), page, pageSize, total: items.length }
}

export const mockTestService: TestService = {
  async listManage(query, options) {
    await mockWait(options?.signal, 50)
    if (!canManage()) throw createMockError('forbidden')
    if (currentMockScenario() === 'failure') throw createMockError('failure')
    const items = filterTests(visibleTests(), query)
    return pageOf(
      items.sort((left, right) => right.testDate.localeCompare(left.testDate)),
      query,
    )
  },

  async listSummaries(query, options) {
    await mockWait(options?.signal, 50)
    if (currentMockScenario() === 'failure') throw createMockError('failure')

    const summaries = summarize(filterTests(visibleTests(), query))
    return pageOf(summaries, query)
  },

  async create(command: CreateTestCommand, options) {
    await mockWait(options?.signal, 50)
    if (!canManage()) throw domainError('TEST_FORBIDDEN')
    if (currentMockScenario() === 'failure') throw createMockError('failure')

    if (command.testType !== 'PRE' && command.testType !== 'POST') {
      throw domainError('TEST_TYPE_INVALID')
    }
    if (!Number.isFinite(command.score) || command.score < 0 || command.score > 100) {
      throw domainError('TEST_SCORE_INVALID')
    }
    if (command.testedAt && new Date(command.testedAt).getTime() > Date.now() + 60_000) {
      throw domainError('TEST_TESTED_AT_INVALID')
    }

    const state = mockBusinessRepository.getState()
    const employee = state.employees.find((item) => item.empId === command.employeeId)
    if (!employee) throw domainError('TEST_EMPLOYEE_NOT_FOUND')
    const course = state.courses.find((item) => item.id === command.courseId)
    if (!course) throw domainError('TEST_COURSE_NOT_FOUND')

    const registration = state.registrations.find(
      (item) => item.employeeId === command.employeeId && item.courseId === command.courseId,
    )
    if (command.testType === 'PRE') {
      if (!registration || !VALID_REGISTRATION_STATUSES.has(registration.status)) {
        throw domainError('TEST_REGISTRATION_REQUIRED')
      }
    } else if (registration?.status !== 'COMPLETED') {
      throw domainError('TEST_POST_NOT_COMPLETED')
    }

    const duplicate = state.tests.some(
      (item) =>
        item.employeeId === command.employeeId &&
        item.courseId === command.courseId &&
        item.testType === command.testType,
    )
    if (duplicate) throw domainError('TEST_DUPLICATE')

    const record: TrainingTest = {
      id: String(Date.now()),
      employeeId: command.employeeId,
      employeeName: employee.empName,
      courseId: command.courseId,
      courseName: course.name,
      testType: command.testType as TestType,
      score: command.score,
      testDate: command.testedAt ?? new Date().toISOString(),
    }
    mockBusinessRepository.update((next) => {
      next.tests.unshift(record)
    })
  },
}

function filterTests(tests: TrainingTest[], query: TestQuery | TestQuerySummary): TrainingTest[] {
  let items = tests
  const keyword = query.keyword?.trim().toLowerCase()
  const employeeKeyword = query.employeeKeyword?.trim().toLowerCase()
  if (keyword) items = items.filter((item) => item.courseName.toLowerCase().includes(keyword))
  if (employeeKeyword)
    items = items.filter(
      (item) =>
        item.employeeName.toLowerCase().includes(employeeKeyword) ||
        String(item.employeeId).includes(employeeKeyword),
    )
  if ('testType' in query && query.testType) {
    items = items.filter((item) => item.testType === query.testType)
  }
  if (query.startDateFrom) {
    items = items.filter((item) => item.testDate.slice(0, 10) >= query.startDateFrom!)
  }
  if (query.startDateTo) {
    items = items.filter((item) => item.testDate.slice(0, 10) <= query.startDateTo!)
  }
  return items
}

/** 按员工+课程聚合 PRE/POST，分数变化与提升率缺失时保持 null（区别于 0 分）。 */
function summarize(tests: TrainingTest[]): TestScoreSummary[] {
  const groups = new Map<string, TestScoreSummary>()
  for (const test of tests) {
    const key = `${test.employeeId}:${test.courseId}`
    const group = groups.get(key) ?? {
      employeeId: test.employeeId,
      employeeName: test.employeeName,
      courseId: test.courseId,
      courseName: test.courseName,
      preScore: null,
      postScore: null,
      change: null,
      improvementRate: null,
      updatedAt: null,
    }
    if (test.testType === 'PRE') group.preScore = test.score
    if (test.testType === 'POST') group.postScore = test.score
    if (!group.updatedAt || test.testDate > group.updatedAt) group.updatedAt = test.testDate
    groups.set(key, group)
  }

  return [...groups.values()]
    .map((group) => {
      const change =
        group.preScore !== null && group.postScore !== null
          ? group.postScore - group.preScore
          : null
      const improvementRate =
        group.preScore !== null && group.postScore !== null && group.preScore !== 0
          ? Math.round(((group.postScore - group.preScore) / group.preScore) * 1000) / 10
          : null
      return { ...group, change, improvementRate }
    })
    .sort((left, right) => (right.updatedAt ?? '').localeCompare(left.updatedAt ?? ''))
}
