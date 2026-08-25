import { beforeEach, describe, expect, it } from 'vitest'

import { mockTrainingRequestService } from '@/mocks/training-request'
import { createMockRegistrationService } from '@/mocks/registration'
import { mockCertificateService } from '@/mocks/certificate'
import {
  getMockBusinessState,
  resetMockBusinessSnapshot,
} from '@/mocks/repositories/business-repository'
import { isServiceError } from '@/types/api'

function setActor(account: string) {
  window.sessionStorage.setItem(
    'training-management.mock-auth-session',
    JSON.stringify({ account }),
  )
}

function setScenario(scenario = 'normal') {
  window.history.replaceState({}, '', `/?scenario=${scenario}`)
}

describe('B3-B5 Mock 业务流程', () => {
  beforeEach(() => {
    setScenario()
    setActor('employee.demo')
    resetMockBusinessSnapshot('FLOW-SNAPSHOT-01')
  })

  it('主管审批与 HR 备案共享同一申请状态', async () => {
    resetMockBusinessSnapshot('FLOW-SNAPSHOT-02')
    setActor('manager.demo')
    await mockTrainingRequestService.approve('5001', '同意参加')
    expect(getMockBusinessState().requests[0]?.status).toBe('DEPT_APPROVED')

    resetMockBusinessSnapshot('FLOW-SNAPSHOT-03')
    setActor('hr.demo')
    await mockTrainingRequestService.file('5001', '已完成备案')
    expect(getMockBusinessState().requests[0]?.status).toBe('HR_FILED')
  })

  it('报名运营支持签到到完成培训的状态流转', async () => {
    resetMockBusinessSnapshot('FLOW-SNAPSHOT-05')
    setActor('hr.demo')
    const service = createMockRegistrationService()
    await service.signIn('6001')
    expect(getMockBusinessState().registrations[0]?.status).toBe('SIGNED_IN')
    await service.complete('6001')
    expect(getMockBusinessState().registrations[0]?.status).toBe('COMPLETED')
  })

  it('证书候选生成后从候选列表移入已生成列表', async () => {
    resetMockBusinessSnapshot('FLOW-SNAPSHOT-07')
    setActor('hr.demo')
    const candidates = await mockCertificateService.listCandidates({ page: 1, pageSize: 20 })
    expect(candidates.total).toBe(1)
    await mockCertificateService.generate({ registrationId: '6001' })
    expect((await mockCertificateService.listCandidates({ page: 1, pageSize: 20 })).total).toBe(0)
    expect((await mockCertificateService.listManage({ page: 1, pageSize: 20 })).total).toBe(1)
  })

  it('冲突与结果未知都不伪造成功状态', async () => {
    resetMockBusinessSnapshot('FLOW-SNAPSHOT-05')
    setActor('hr.demo')
    const service = createMockRegistrationService(() => 'conflict')
    await expect(service.signIn('6001')).rejects.toSatisfy(
      (error: unknown) => isServiceError(error) && error.ui.kind === 'conflict',
    )
    expect(getMockBusinessState().registrations[0]?.status).toBe('REGISTERED')

    const unknownService = createMockRegistrationService(() => 'result-unknown')
    await expect(unknownService.signIn('6001')).rejects.toSatisfy(
      (error: unknown) => isServiceError(error) && error.ui.resultUnknown,
    )
    expect(getMockBusinessState().registrations[0]?.status).toBe('SIGNED_IN')
  })
})
