import { beforeEach, describe, expect, it } from 'vitest'

import { mapTrainingRequest, mapTrainingRequestPage } from '@/api/mappers/training-request'
import type { ApiEnvelopeDto, PageDto, TrainingRequestDto } from '@/api/transport'
import { domainError } from '@/domains/errors'
import {
  getMockBusinessState,
  loadMockBusinessSnapshot,
  resetMockBusinessSnapshot,
} from '@/mocks/repositories/business-repository'
import { mockTrainingRequestService } from '@/mocks/training-request'

function setActor(account: string): void {
  window.sessionStorage.setItem(
    'training-management.mock-auth-session',
    JSON.stringify({ account }),
  )
}

function setScenario(value = ''): void {
  window.history.replaceState({}, '', value ? `/?scenario=${value}` : '/')
}

describe('B2 培训申请领域与 Mock 流程', () => {
  beforeEach(() => {
    setActor('employee.demo')
    setScenario()
    resetMockBusinessSnapshot('FLOW-SNAPSHOT-01')
  })

  it('校验理由、拒绝重复待处理申请，并显式推进共享状态', async () => {
    await expect(
      mockTrainingRequestService.create({ courseId: '4001', reason: '   ' }),
    ).rejects.toMatchObject({ ui: { code: 'REQUEST_REASON_REQUIRED' } })
    await expect(
      mockTrainingRequestService.create({ courseId: '4001', reason: 'x'.repeat(501) }),
    ).rejects.toMatchObject({ ui: { code: 'REQUEST_REASON_TOO_LONG' } })
    const created = await mockTrainingRequestService.create({
      courseId: '4001',
      reason: '用于验证申请流转',
    })
    expect(created.status).toBe('PENDING')
    expect(getMockBusinessState().requests).toHaveLength(1)

    await expect(
      mockTrainingRequestService.create({ courseId: '4001', reason: '再次提交' }),
    ).rejects.toMatchObject({ ui: { code: 'REQUEST_EXISTS' } })
    loadMockBusinessSnapshot('FLOW-SNAPSHOT-03')
    await expect(
      mockTrainingRequestService.create({ courseId: '4001', reason: '已审批后不可重复提交' }),
    ).rejects.toMatchObject({ ui: { code: 'REQUEST_EXISTS' } })
  })

  it('离职员工不能发起申请', async () => {
    setActor('employee.resigned.demo')
    await expect(
      mockTrainingRequestService.create({ courseId: '4001', reason: '离职账号验证' }),
    ).rejects.toMatchObject({ ui: { code: 'REQUEST_EMPLOYEE_INACTIVE' } })
  })

  it('列表查询使用员工本人范围、服务端分页和筛选条件', async () => {
    loadMockBusinessSnapshot('FLOW-SNAPSHOT-02')
    const result = await mockTrainingRequestService.listMine({
      page: 1,
      pageSize: 20,
      status: 'PENDING',
    })
    expect(result).toMatchObject({ page: 1, pageSize: 20, total: 1 })
    expect(result.items[0]?.id).toBe('5001')
    const noResult = await mockTrainingRequestService.listMine({
      page: 1,
      pageSize: 20,
      keyword: '不存在',
    })
    expect(noResult.items).toHaveLength(0)
  })

  it('主管只能读取本部门申请，HR可读取申请详情，员工不能越权', async () => {
    loadMockBusinessSnapshot('FLOW-SNAPSHOT-02')
    setActor('manager.demo')
    await expect(mockTrainingRequestService.getById('5001')).resolves.toMatchObject({ id: '5001' })
    setActor('hr.demo')
    await expect(mockTrainingRequestService.getById('5001')).resolves.toMatchObject({ id: '5001' })
    setActor('admin.demo')
    await expect(mockTrainingRequestService.getById('5001')).rejects.toMatchObject({
      ui: { code: 'REQUEST_FORBIDDEN' },
    })
  })

  it('未知结果先落库，后续查询得到最终申请状态', async () => {
    setScenario('result-unknown')
    await expect(
      mockTrainingRequestService.create({ courseId: '4001', reason: '未知结果查询验证' }),
    ).rejects.toMatchObject({ ui: { resultUnknown: true } })
    setScenario()
    const result = await mockTrainingRequestService.listMine({ page: 1, pageSize: 20 })
    expect(result.items[0]).toMatchObject({ courseId: '4001', status: 'PENDING' })
  })

  it('DTO 映射将未知状态安全降级并保留分页口径', () => {
    const dto: TrainingRequestDto = {
      id: '5009',
      courseId: '4001',
      courseName: '',
      employeeId: 1001,
      employeeName: '',
      deptId: 10,
      requestReason: '',
      status: 'FUTURE_STATUS',
      createTime: '2026-08-23T10:00:00+08:00',
    }
    expect(mapTrainingRequest(dto)).toMatchObject({
      id: '5009',
      courseName: '未命名课程',
      status: 'UNKNOWN',
      submittedAt: '2026-08-23T10:00:00+08:00',
    })
    const envelope: ApiEnvelopeDto<PageDto<TrainingRequestDto>> = {
      success: true,
      message: 'ok',
      data: { items: [dto], page: 1, pageSize: 20, total: 1 },
    }
    expect(mapTrainingRequestPage(envelope)).toMatchObject({ page: 1, pageSize: 20, total: 1 })
    expect(() => domainError('REQUEST_NOT_FOUND')).not.toThrow()
  })

  it('历史别名字段仍可映射，后端 id 优先于 requestId', () => {
    const legacy = mapTrainingRequest({
      requestId: '5010',
      courseId: '4001',
      status: 'PENDING',
      createdAt: '2026-08-23T10:00:00+08:00',
      hrFilingComment: '旧字段意见',
    })
    expect(legacy).toMatchObject({
      id: '5010',
      submittedAt: '2026-08-23T10:00:00+08:00',
      hrOpinion: '旧字段意见',
    })

    const both = mapTrainingRequest({
      id: 5011,
      requestId: '5010',
      courseId: '4001',
      status: 'PENDING',
      hrFileComment: '后端意见',
      hrFilingComment: '旧字段意见',
    })
    expect(both.id).toBe('5011')
    expect(both.hrOpinion).toBe('后端意见')
  })

  it('缺少 id 与 requestId 时抛出契约异常，不生成 undefined 路由参数', () => {
    let thrown: unknown
    try {
      mapTrainingRequest({ courseId: '4001', status: 'PENDING' })
    } catch (error) {
      thrown = error
    }
    expect(thrown).toMatchObject({ ui: { code: 'HTTP_CONTRACT_NOT_FROZEN' } })
  })
})
