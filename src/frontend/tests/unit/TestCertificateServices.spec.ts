import { beforeEach, describe, expect, it } from 'vitest'

import { mapCertificate, mapCertificateCandidate } from '@/api/mappers/certificate'
import type { CertificateCandidateDto, CertificateDto } from '@/api/transport'
import { mockTestService } from '@/mocks/test'
import {
  mockBusinessRepository,
  resetMockBusinessSnapshot,
} from '@/mocks/repositories/business-repository'
import { isServiceError } from '@/types/api'

function setActor(account: string) {
  window.sessionStorage.setItem(
    'training-management.mock-auth-session',
    JSON.stringify({ account }),
  )
}

function certificateDto(overrides: Partial<CertificateDto> = {}): CertificateDto {
  return {
    certId: 1,
    empId: 55,
    courseId: 4001,
    certCode: 'CERT-2026-0001',
    issueDate: '2026-09-03T10:00:00+08:00',
    expireDate: null,
    notified: 'N',
    notifiedAt: null,
    issuedByEmpId: 57,
    createdAt: '2026-09-03T10:00:00+08:00',
    ...overrides,
  }
}

const isConflict = (error: unknown) => isServiceError(error) && error.ui.kind === 'conflict'

describe('证书状态映射', () => {
  it('EXPIRE_DATE 为空映射为有效而不是未知状态', () => {
    const certificate = mapCertificate(certificateDto({ expireDate: null, status: null }))
    expect(certificate.displayStatus).toBe('VALID')
    expect(certificate.expiresAt).toBeNull()
  })

  it('优先采用后端下发的状态值', () => {
    expect(
      mapCertificate(certificateDto({ expireDate: null, status: 'EXPIRED' })).displayStatus,
    ).toBe('EXPIRED')
    expect(
      mapCertificate(certificateDto({ expireDate: null, status: 'EXPIRING' })).displayStatus,
    ).toBe('EXPIRING')
  })

  it('后端状态缺失时按到期日回退计算', () => {
    const past = new Date(Date.now() - 86_400_000).toISOString()
    const soon = new Date(Date.now() + 10 * 86_400_000).toISOString()
    const far = new Date(Date.now() + 200 * 86_400_000).toISOString()
    expect(mapCertificate(certificateDto({ expireDate: past })).displayStatus).toBe('EXPIRED')
    expect(mapCertificate(certificateDto({ expireDate: soon })).displayStatus).toBe('EXPIRING')
    expect(mapCertificate(certificateDto({ expireDate: far })).displayStatus).toBe('VALID')
  })

  it('证书候选映射报名状态与发证资格', () => {
    const dto: CertificateCandidateDto = {
      regId: 6001,
      empId: 55,
      employeeName: '张三',
      departmentName: '技术部',
      courseId: 4001,
      courseName: '数据库性能优化实战',
      actualHours: null,
      status: 'COMPLETED',
      qualified: 'N',
      qualificationReason: '尚未录入训后测试成绩',
    }
    const candidate = mapCertificateCandidate(dto)
    expect(candidate.registrationStatus).toBe('COMPLETED')
    expect(candidate.qualification).toEqual({ allowed: false, reason: '尚未录入训后测试成绩' })

    const allowed = mapCertificateCandidate({ ...dto, qualified: 'Y', qualificationReason: null })
    expect(allowed.qualification.allowed).toBe(true)
  })

  it('未知报名状态回退为 UNKNOWN 而不是抛错', () => {
    const candidate = mapCertificateCandidate({
      regId: 6001,
      empId: 55,
      employeeName: null,
      departmentName: null,
      courseId: 4001,
      courseName: null,
      actualHours: null,
      status: null,
      qualified: 'N',
      qualificationReason: null,
    })
    expect(candidate.registrationStatus).toBe('UNKNOWN')
  })
})

describe('测试成绩 Mock 服务', () => {
  beforeEach(() => {
    resetMockBusinessSnapshot('FLOW-SNAPSHOT-01')
  })

  it('汇总列表按员工+课程聚合 PRE/POST 并计算变化与提升率', async () => {
    setActor('hr.demo')
    const result = await mockTestService.listSummaries({ page: 1, pageSize: 20 })
    expect(result.total).toBe(1)
    expect(result.items[0]).toMatchObject({
      employeeId: 55,
      courseId: '4001',
      preScore: 78,
      postScore: 85,
      change: 7,
    })
    // (85 - 78) / 78 * 100 = 8.97... 四舍五入到 1 位小数
    expect(result.items[0]?.improvementRate).toBe(9)
  })

  it('缺失 POST 时变化与提升率保持 null 而不是 0', async () => {
    setActor('hr.demo')
    mockBusinessRepository.update((next) => {
      next.tests = next.tests.filter((item) => item.testType === 'PRE')
    })
    const result = await mockTestService.listSummaries({ page: 1, pageSize: 20 })
    expect(result.items[0]?.preScore).toBe(78)
    expect(result.items[0]?.postScore).toBeNull()
    expect(result.items[0]?.change).toBeNull()
    expect(result.items[0]?.improvementRate).toBeNull()
  })

  it('PRE 为 0 分时提升率不可计算', async () => {
    setActor('hr.demo')
    mockBusinessRepository.update((next) => {
      next.tests = [
        { ...next.tests[0]!, testType: 'PRE', score: 0 },
        { ...next.tests[1]!, testType: 'POST', score: 50 },
      ]
    })
    const result = await mockTestService.listSummaries({ page: 1, pageSize: 20 })
    expect(result.items[0]?.change).toBe(50)
    expect(result.items[0]?.improvementRate).toBeNull()
  })

  it('重复录入同一员工+课程+类型返回 409 冲突', async () => {
    setActor('hr.demo')
    resetMockBusinessSnapshot('FLOW-SNAPSHOT-05')
    await expect(
      mockTestService.create({
        employeeId: 55,
        courseId: '4001',
        testType: 'PRE',
        score: 90,
      }),
    ).rejects.toSatisfy(isConflict)
  })

  it('分数越界与非法类型被拒绝', async () => {
    setActor('hr.demo')
    const isValidation = (error: unknown) => isServiceError(error) && error.ui.kind === 'validation'

    await expect(
      mockTestService.create({ employeeId: 55, courseId: '4001', testType: 'PRE', score: 120 }),
    ).rejects.toSatisfy(isValidation)
    await expect(
      mockTestService.create({
        employeeId: 55,
        courseId: '4001',
        testType: 'MID' as never,
        score: 80,
      }),
    ).rejects.toSatisfy(isValidation)
  })

  it('测试时间晚于当前时间被拒绝', async () => {
    setActor('hr.demo')
    await expect(
      mockTestService.create({
        employeeId: 55,
        courseId: '4001',
        testType: 'PRE',
        score: 80,
        testedAt: new Date(Date.now() + 3_600_000).toISOString(),
      }),
    ).rejects.toSatisfy((error: unknown) => isServiceError(error) && error.ui.kind === 'validation')
  })

  it('无有效报名不能录入 PRE,未完成培训不能录入 POST', async () => {
    setActor('hr.demo')
    resetMockBusinessSnapshot('FLOW-SNAPSHOT-02')
    mockBusinessRepository.update((next) => {
      next.tests = []
    })
    await expect(
      mockTestService.create({ employeeId: 55, courseId: '4001', testType: 'PRE', score: 80 }),
    ).rejects.toSatisfy((error: unknown) => isServiceError(error) && error.ui.kind === 'validation')

    resetMockBusinessSnapshot('FLOW-SNAPSHOT-05')
    mockBusinessRepository.update((next) => {
      next.tests = []
    })
    await expect(
      mockTestService.create({ employeeId: 55, courseId: '4001', testType: 'POST', score: 80 }),
    ).rejects.toSatisfy((error: unknown) => isServiceError(error) && error.ui.kind === 'validation')
  })

  it('有效报名可录入 PRE 并反映到汇总列表', async () => {
    setActor('hr.demo')
    resetMockBusinessSnapshot('FLOW-SNAPSHOT-05')
    mockBusinessRepository.update((next) => {
      next.tests = []
    })
    await mockTestService.create({
      employeeId: 55,
      courseId: '4001',
      testType: 'PRE',
      score: 80,
      testedAt: '2026-08-19T09:00:00+08:00',
    })
    const result = await mockTestService.listSummaries({ page: 1, pageSize: 20 })
    expect(result.items[0]?.preScore).toBe(80)
  })

  it('普通员工与主管无权录入成绩', async () => {
    resetMockBusinessSnapshot('FLOW-SNAPSHOT-05')
    const isForbidden = (error: unknown) => isServiceError(error) && error.ui.kind === 'forbidden'

    setActor('employee.demo')
    await expect(
      mockTestService.create({ employeeId: 55, courseId: '4001', testType: 'PRE', score: 80 }),
    ).rejects.toSatisfy(isForbidden)

    setActor('manager.demo')
    await expect(
      mockTestService.create({ employeeId: 55, courseId: '4001', testType: 'PRE', score: 80 }),
    ).rejects.toSatisfy(isForbidden)
  })
})

describe('我的成绩（员工自助范围）', () => {
  beforeEach(() => {
    resetMockBusinessSnapshot('FLOW-SNAPSHOT-05')
    mockBusinessRepository.update((next) => {
      next.tests.push({
        id: '9100',
        employeeId: 58,
        employeeName: '张后端',
        courseId: '4001',
        courseName: '数据库性能优化实战',
        testType: 'PRE',
        score: 60,
        testDate: '2026-08-19T09:00:00+08:00',
      })
    })
  })

  it('普通员工只能看到本人成绩，HR 可看到全部', async () => {
    setActor('employee.demo')
    const mine = await mockTestService.listSummaries({ page: 1, pageSize: 20 })
    expect(mine.total).toBe(1)
    expect(mine.items[0]?.employeeId).toBe(55)
    expect(mine.items[0]?.preScore).toBe(78)

    setActor('hr.demo')
    const all = await mockTestService.listSummaries({ page: 1, pageSize: 20 })
    expect(all.total).toBe(2)
  })

  it('主管同样只看到本人成绩而不是全部', async () => {
    setActor('manager.demo')
    const result = await mockTestService.listSummaries({ page: 1, pageSize: 20 })
    expect(result.total).toBe(0)
  })

  it('员工自助视图仍然不允许录入成绩', async () => {
    setActor('employee.demo')
    await expect(
      mockTestService.create({ employeeId: 55, courseId: '4001', testType: 'PRE', score: 80 }),
    ).rejects.toSatisfy((error: unknown) => isServiceError(error) && error.ui.kind === 'forbidden')
  })
})
