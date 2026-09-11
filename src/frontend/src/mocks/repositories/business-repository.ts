import type { CourseDetail } from '@/domains/course'
import type { Certificate } from '@/domains/certificate'
import type { TrainingRequest } from '@/domains/training-request'
import type { Registration } from '@/domains/registration'
import type { CourseRating } from '@/domains/rating'
import type { TrainingTest } from '@/domains/test'
import type { BlacklistRecord } from '@/domains/blacklist'
import type { EmployeeSummary } from '@/domains/employee'

export type FlowSnapshotId =
  | 'FLOW-SNAPSHOT-01'
  | 'FLOW-SNAPSHOT-02'
  | 'FLOW-SNAPSHOT-03'
  | 'FLOW-SNAPSHOT-04'
  | 'FLOW-SNAPSHOT-05'
  | 'FLOW-SNAPSHOT-06'
  | 'FLOW-SNAPSHOT-07'
  | 'FLOW-SNAPSHOT-08'

export interface MockBusinessState {
  snapshotId: FlowSnapshotId
  version: number
  courses: CourseDetail[]
  requests: TrainingRequest[]
  registrations: Registration[]
  certificates: Certificate[]
  ratings: CourseRating[]
  tests: TrainingTest[]
  blacklist: BlacklistRecord[]
  employees: EmployeeSummary[]
}

const STORAGE_KEY = 'training-management.mock-business-state'
const clone = <T>(value: T): T => JSON.parse(JSON.stringify(value)) as T

const createCourse = (): CourseDetail[] => [
  {
    id: '4001',
    name: '数据库性能优化实战',
    type: '技术培训',
    typeLabel: '技术培训',
    trainerName: '陈老师',
    startTime: '2026-09-02T09:00:00+08:00',
    endTime: '2026-09-02T17:00:00+08:00',
    location: '培训中心 A201',
    status: 'PUBLISHED',
    statusLabel: '已发布',
    maxStudents: 30,
    registeredCount: 18,
    remainingSeats: 12,
    description: '围绕索引设计、执行计划和慢查询治理，完成一次可落地的性能优化演练。',
    objectives: ['理解慢查询定位方法', '掌握索引与执行计划分析', '完成优化方案复盘'],
    hours: 8,
    organizer: '培训发展部',
    trainer: {
      id: '3001',
      name: '陈老师',
      title: '高级数据库工程师',
      department: '技术平台部',
      expertise: '数据库架构、性能优化',
      rating: 4.8,
    },
    materials: [
      { id: 'M-4001-1', name: '课程讲义（报名后开放）', kind: 'DOCUMENT', available: false },
      { id: 'M-4001-2', name: '课前阅读：执行计划', kind: 'LINK', available: false },
      { id: 'M-4001-3', name: '课后练习说明', kind: 'DOCUMENT', available: false },
    ],
    eligibility: {
      apply: { allowed: true },
      register: {
        allowed: false,
        reasonCode: 'REQUEST_NOT_FILED',
        reason: '申请尚未完成 HR 备案。',
      },
    },
  },
  {
    id: '4002',
    name: '新任主管沟通与反馈',
    type: '管理培训',
    typeLabel: '管理培训',
    trainerName: '周老师',
    startTime: '2026-09-08T13:30:00+08:00',
    endTime: '2026-09-08T17:30:00+08:00',
    location: '行政楼 3F 多功能厅',
    status: 'PUBLISHED',
    statusLabel: '已发布',
    maxStudents: 24,
    registeredCount: 24,
    remainingSeats: 0,
    description: '通过情境演练提升主管在目标沟通、反馈和团队协作中的表达能力。',
    objectives: ['掌握结构化反馈方法', '识别沟通中的关键障碍'],
    hours: 4,
    organizer: '培训发展部',
    trainer: {
      id: '3002',
      name: '周老师',
      title: '组织发展顾问',
      department: '人力资源部',
      expertise: '领导力、团队沟通',
      rating: 4.6,
    },
    materials: [],
    eligibility: {
      apply: { allowed: true },
      register: { allowed: false, reasonCode: 'COURSE_FULL', reason: '课程暂无剩余名额。' },
    },
  },
  {
    id: '4003',
    name: '产品需求分析与设计',
    type: '产品培训',
    typeLabel: '产品培训',
    trainerName: '林老师',
    startTime: '2026-09-15T09:30:00+08:00',
    endTime: '2026-09-15T16:30:00+08:00',
    location: '创新中心 B305',
    status: 'PUBLISHED',
    statusLabel: '已发布',
    maxStudents: 20,
    registeredCount: 8,
    remainingSeats: 12,
    description: '从用户洞察到需求文档，系统掌握产品需求分析与原型设计方法。',
    objectives: ['掌握用户调研方法', '学会撰写需求文档', '完成一次原型设计'],
    hours: 7,
    organizer: '培训发展部',
    trainer: {
      id: '3003',
      name: '林老师',
      title: '高级产品经理',
      department: '产品部',
      expertise: '需求分析、原型设计',
      rating: 4.7,
    },
    materials: [],
    eligibility: {
      apply: { allowed: true },
      register: {
        allowed: false,
        reasonCode: 'REQUEST_NOT_FILED',
        reason: '申请尚未完成 HR 备案。',
      },
    },
  },
  {
    id: '4004',
    name: '数字营销与客户增长',
    type: '营销培训',
    typeLabel: '营销培训',
    trainerName: '黄老师',
    startTime: '2026-09-22T14:00:00+08:00',
    endTime: '2026-09-22T18:00:00+08:00',
    location: '线上直播',
    status: 'PUBLISHED',
    statusLabel: '已发布',
    maxStudents: 40,
    registeredCount: 15,
    remainingSeats: 25,
    description: '拆解主流数字营销渠道与增长漏斗，掌握可复用的获客方法。',
    objectives: ['理解增长漏斗模型', '掌握渠道投放要点'],
    hours: 4,
    organizer: '培训发展部',
    trainer: {
      id: '3004',
      name: '黄老师',
      title: '市场增长顾问',
      department: '市场部',
      expertise: '数字营销、用户增长',
      rating: 4.5,
    },
    materials: [],
    eligibility: {
      apply: { allowed: true },
      register: {
        allowed: false,
        reasonCode: 'REQUEST_NOT_FILED',
        reason: '申请尚未完成 HR 备案。',
      },
    },
  },
  {
    id: '4005',
    name: '数据分析入门实战',
    type: '技术培训',
    typeLabel: '技术培训',
    trainerName: '赵老师',
    startTime: '2026-10-10T09:00:00+08:00',
    endTime: '2026-10-10T17:00:00+08:00',
    location: '培训中心 A202',
    status: 'DRAFT',
    statusLabel: '草稿',
    maxStudents: 25,
    registeredCount: 0,
    remainingSeats: 25,
    description: '面向零基础员工的数据分析入门课程，目前仍为草稿。',
    objectives: ['理解数据分析基本流程', '掌握常用分析工具'],
    hours: 8,
    organizer: '培训发展部',
    trainer: {
      id: '3005',
      name: '赵老师',
      title: '数据分析讲师',
      department: '技术平台部',
      expertise: '数据分析、可视化',
      rating: 4.7,
    },
    materials: [],
    eligibility: {
      apply: { allowed: false, reasonCode: 'COURSE_CLOSED', reason: '课程尚未发布。' },
      register: { allowed: false, reasonCode: 'COURSE_CLOSED', reason: '课程尚未发布。' },
    },
  },
]

const baseRequest = (): TrainingRequest => ({
  id: '5001',
  courseId: '4001',
  courseName: '数据库性能优化实战',
  employeeId: 55,
  employeeName: '张三',
  departmentName: '技术部',
  reason: '提升数据库设计与查询能力',
  status: 'PENDING',
  submittedAt: '2026-08-20T10:00:00+08:00',
  updatedAt: null,
  departmentOpinion: null,
  hrOpinion: null,
})

const baseRegistration = (): Registration => ({
  id: '6001',
  courseId: '4001',
  courseName: '数据库性能优化实战',
  employeeId: 55,
  employeeName: '张三',
  status: 'REGISTERED',
  registeredAt: '2026-08-21T10:00:00+08:00',
  signedInAt: null,
  completedAt: null,
  departmentName: '技术部',
  signinMethod: 'UNKNOWN',
  attendanceNote: null,
  actualHours: null,
})

const baseCertificate = (): Certificate => ({
  id: '10001',
  certificateNo: 'CERT-2026-0001',
  courseId: '4001',
  courseName: '数据库性能优化实战',
  employeeId: 55,
  employeeName: '张三',
  issuedAt: '2026-09-03T10:00:00+08:00',
  expiresAt: '2028-09-03T23:59:59+08:00',
  displayStatus: 'VALID',
})

const baseRating = (): CourseRating => ({
  id: '8001',
  courseId: '4001',
  courseName: '数据库性能优化实战',
  trainerId: '3001',
  trainerName: '陈老师',
  employeeId: 55,
  employeeName: '张三',
  score: 4.5,
  comment: '讲解清晰，案例实用。',
  hrVerified: 'N',
  hrComment: null,
  ratingDate: '2026-08-20T18:00:00+08:00',
})

const baseTests = (): TrainingTest[] => [
  {
    id: '9001',
    employeeId: 55,
    employeeName: '张三',
    courseId: '4001',
    courseName: '数据库性能优化实战',
    testType: 'PRE',
    score: 78,
    testDate: '2026-08-19T09:00:00+08:00',
  },
  {
    id: '9002',
    employeeId: 55,
    employeeName: '张三',
    courseId: '4001',
    courseName: '数据库性能优化实战',
    testType: 'POST',
    score: 85,
    testDate: '2026-08-21T17:00:00+08:00',
  },
]

const baseEmployees = (): EmployeeSummary[] => [
  {
    empId: 55,
    empName: '张三',
    deptName: '技术部',
    position: '前端工程师',
    email: 'zhangsan@example.com',
    phone: null,
    hireDate: '2024-01-10',
    status: 'ACTIVE',
    createdAt: '2024-01-10T09:00:00+08:00',
  },
  {
    empId: 56,
    empName: '李主管',
    deptName: '技术部',
    position: '部门主管',
    email: 'manager@example.com',
    phone: null,
    hireDate: '2020-04-12',
    status: 'ACTIVE',
    createdAt: '2020-04-12T09:00:00+08:00',
  },
  {
    empId: 58,
    empName: '王五',
    deptName: '技术部',
    position: '后端工程师',
    email: null,
    phone: null,
    hireDate: '2023-08-01',
    status: 'ACTIVE',
    createdAt: '2023-08-01T09:00:00+08:00',
  },
  {
    empId: 59,
    empName: '赵六',
    deptName: '技术部',
    position: '测试工程师',
    email: null,
    phone: null,
    hireDate: '2023-06-01',
    status: 'ACTIVE',
    createdAt: '2023-06-01T09:00:00+08:00',
  },
  {
    empId: 57,
    empName: '王 HR',
    deptName: '人力资源部',
    position: '培训专员',
    email: null,
    phone: null,
    hireDate: '2021-05-12',
    status: 'ACTIVE',
    createdAt: '2021-05-12T09:00:00+08:00',
  },
  {
    empId: 54,
    empName: '赵管理员',
    deptName: '信息管理部',
    position: '系统管理员',
    email: null,
    phone: null,
    hireDate: '2019-09-12',
    status: 'ACTIVE',
    createdAt: '2019-09-12T09:00:00+08:00',
  },
  {
    empId: 60,
    empName: '钱七',
    deptName: '市场部',
    position: '市场专员',
    email: null,
    phone: null,
    hireDate: '2022-07-12',
    status: 'ACTIVE',
    createdAt: '2022-07-12T09:00:00+08:00',
  },
]

const baseBlacklist = (): BlacklistRecord[] => [
  {
    id: '9001',
    empId: 55,
    empName: '张三',
    deptName: '技术部',
    reason: '违规缺勤',
    startDate: '2026-08-01',
    endDate: '2026-08-31',
    status: 'ACTIVE',
    statusLabel: '生效中',
    createdAt: '2026-08-01T09:00:00+08:00',
  },
  {
    id: '9002',
    empId: 60,
    empName: '钱七',
    deptName: '市场部',
    reason: '泄密事件',
    startDate: '2026-07-15',
    endDate: '2026-08-15',
    status: 'RELEASED',
    statusLabel: '已解除',
    createdAt: '2026-07-15T10:00:00+08:00',
  },
]

function buildSnapshot(snapshotId: FlowSnapshotId): MockBusinessState {
  const requests = snapshotId === 'FLOW-SNAPSHOT-01' ? [] : [baseRequest()]
  const registrations = [
    'FLOW-SNAPSHOT-05',
    'FLOW-SNAPSHOT-06',
    'FLOW-SNAPSHOT-07',
    'FLOW-SNAPSHOT-08',
  ].includes(snapshotId)
    ? [baseRegistration()]
    : []
  const certificates = snapshotId === 'FLOW-SNAPSHOT-08' ? [baseCertificate()] : []
  const ratings = [baseRating()]
  const tests = baseTests()

  if (snapshotId === 'FLOW-SNAPSHOT-03' && requests[0]) requests[0].status = 'DEPT_APPROVED'
  if (snapshotId === 'FLOW-SNAPSHOT-04' && requests[0]) requests[0].status = 'HR_FILED'
  if (snapshotId === 'FLOW-SNAPSHOT-06' && registrations[0]) {
    registrations[0].status = 'SIGNED_IN'
    registrations[0].signedInAt = '2026-08-20T09:06:00+08:00'
    registrations[0].signinMethod = 'MANUAL'
    registrations[0].attendanceNote = '交通延误，HR补签到'
  }
  if (
    (snapshotId === 'FLOW-SNAPSHOT-07' || snapshotId === 'FLOW-SNAPSHOT-08') &&
    registrations[0]
  ) {
    registrations[0].status = 'COMPLETED'
    registrations[0].signedInAt = '2026-08-20T09:06:00+08:00'
    registrations[0].completedAt = '2026-08-20T17:00:00+08:00'
    registrations[0].signinMethod = 'MANUAL'
    registrations[0].attendanceNote = '交通延误，HR补签到'
  }

  const courses = createCourse()
  if (courses[0]) {
    courses[0].registeredCount = (courses[0].registeredCount ?? 0) + registrations.length
    courses[0].remainingSeats = (courses[0].remainingSeats ?? 0) - registrations.length
  }
  return {
    snapshotId,
    version: 1,
    courses,
    requests,
    registrations,
    certificates,
    ratings,
    tests,
    blacklist: baseBlacklist(),
    employees: baseEmployees(),
  }
}

export const FLOW_SNAPSHOTS: Record<FlowSnapshotId, MockBusinessState> = {
  'FLOW-SNAPSHOT-01': buildSnapshot('FLOW-SNAPSHOT-01'),
  'FLOW-SNAPSHOT-02': buildSnapshot('FLOW-SNAPSHOT-02'),
  'FLOW-SNAPSHOT-03': buildSnapshot('FLOW-SNAPSHOT-03'),
  'FLOW-SNAPSHOT-04': buildSnapshot('FLOW-SNAPSHOT-04'),
  'FLOW-SNAPSHOT-05': buildSnapshot('FLOW-SNAPSHOT-05'),
  'FLOW-SNAPSHOT-06': buildSnapshot('FLOW-SNAPSHOT-06'),
  'FLOW-SNAPSHOT-07': buildSnapshot('FLOW-SNAPSHOT-07'),
  'FLOW-SNAPSHOT-08': buildSnapshot('FLOW-SNAPSHOT-08'),
}

export class MockBusinessRepository {
  private state: MockBusinessState

  constructor() {
    const requested =
      typeof window !== 'undefined'
        ? (new URLSearchParams(window.location.search).get('mockSnapshot') as FlowSnapshotId | null)
        : null
    if (requested && FLOW_SNAPSHOTS[requested]) {
      this.state = clone(FLOW_SNAPSHOTS[requested])
      this.persist()
    } else {
      this.state = this.readPersisted() ?? clone(FLOW_SNAPSHOTS['FLOW-SNAPSHOT-01'])
    }
  }

  getState(): MockBusinessState {
    return clone(this.state)
  }

  reset(snapshotId: FlowSnapshotId = 'FLOW-SNAPSHOT-01'): MockBusinessState {
    this.state = clone(FLOW_SNAPSHOTS[snapshotId])
    this.persist()
    return this.getState()
  }

  load(snapshotId: FlowSnapshotId): MockBusinessState {
    return this.reset(snapshotId)
  }

  update(mutator: (state: MockBusinessState) => void): MockBusinessState {
    const next = this.getState()
    mutator(next)
    next.version += 1
    this.state = next
    this.persist()
    return this.getState()
  }

  private persist(): void {
    if (typeof window !== 'undefined')
      window.sessionStorage.setItem(STORAGE_KEY, JSON.stringify(this.state))
  }

  private readPersisted(): MockBusinessState | null {
    if (typeof window === 'undefined') return null
    const raw = window.sessionStorage.getItem(STORAGE_KEY)
    if (!raw) return null
    try {
      const parsed = JSON.parse(raw) as MockBusinessState
      return parsed?.snapshotId && Array.isArray(parsed.courses) ? parsed : null
    } catch {
      window.sessionStorage.removeItem(STORAGE_KEY)
      return null
    }
  }
}

export const mockBusinessRepository = new MockBusinessRepository()

export function resetMockBusinessSnapshot(snapshotId: FlowSnapshotId = 'FLOW-SNAPSHOT-01') {
  return mockBusinessRepository.reset(snapshotId)
}

export function loadMockBusinessSnapshot(snapshotId: FlowSnapshotId) {
  return mockBusinessRepository.load(snapshotId)
}

export function getMockBusinessState() {
  return mockBusinessRepository.getState()
}

type MockActor = {
  employeeId: number
  role: 'EMPLOYEE' | 'DEPT_MANAGER' | 'HR' | 'ADMIN'
  departmentId: number
  departmentName: string
  status: 'ACTIVE' | 'RESIGNED'
}

const mockActorDirectory: Record<string, MockActor> = {
  'employee.demo': {
    employeeId: 55,
    role: 'EMPLOYEE',
    departmentId: 101,
    departmentName: '技术部',
    status: 'ACTIVE',
  },
  'employee.resigned.demo': {
    employeeId: 1005,
    role: 'EMPLOYEE',
    departmentId: 101,
    departmentName: '技术部',
    status: 'RESIGNED',
  },
  'manager.demo': {
    employeeId: 56,
    role: 'DEPT_MANAGER',
    departmentId: 101,
    departmentName: '技术部',
    status: 'ACTIVE',
  },
  'hr.demo': {
    employeeId: 57,
    role: 'HR',
    departmentId: 102,
    departmentName: '人力资源部',
    status: 'ACTIVE',
  },
  'admin.demo': {
    employeeId: 54,
    role: 'ADMIN',
    departmentId: 103,
    departmentName: '管理部',
    status: 'ACTIVE',
  },
}

/** 具备管理权限(管理员/HR/部门主管)的演示账号员工编号，用于从黑名单候选中排除。 */
export function getMockPrivilegedEmployeeIds(): Set<number> {
  return new Set(
    Object.values(mockActorDirectory)
      .filter(
        (actor) => actor.role === 'ADMIN' || actor.role === 'HR' || actor.role === 'DEPT_MANAGER',
      )
      .map((actor) => actor.employeeId),
  )
}

export function getMockActor(): MockActor {
  const account =
    typeof window === 'undefined'
      ? 'employee.demo'
      : (() => {
          try {
            const raw = window.sessionStorage.getItem('training-management.mock-auth-session')
            return raw
              ? ((JSON.parse(raw) as { account?: string }).account ?? 'employee.demo')
              : 'employee.demo'
          } catch {
            return 'employee.demo'
          }
        })()
  return mockActorDirectory[account] ?? mockActorDirectory['employee.demo']!
}

if (typeof window !== 'undefined') {
  Object.assign(window, {
    __trainingMockReset: resetMockBusinessSnapshot,
    __trainingMockLoad: loadMockBusinessSnapshot,
  })
}
