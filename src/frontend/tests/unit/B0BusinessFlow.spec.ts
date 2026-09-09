import { describe, expect, it } from 'vitest'

import { mapCourseDetail } from '@/api/mappers/course'
import type { ApiEnvelopeDto, CourseDetailDto } from '@/api/transport'
import {
  loadMockBusinessSnapshot,
  getMockBusinessState,
  resetMockBusinessSnapshot,
} from '@/mocks/repositories/business-repository'
import { mockCourseService } from '@/mocks/course'
import { mockRegistrationService } from '@/mocks/registration'
import { domainError } from '@/domains/errors'

describe('B0 业务基础与 Mock 流程', () => {
  it('每个快照可显式复位且主状态按设计推进', () => {
    resetMockBusinessSnapshot('FLOW-SNAPSHOT-01')
    expect(getMockBusinessState()).toMatchObject({
      requests: [],
      registrations: [],
      certificates: [],
    })

    loadMockBusinessSnapshot('FLOW-SNAPSHOT-04')
    expect(getMockBusinessState().requests[0]?.status).toBe('HR_FILED')
    expect(getMockBusinessState().registrations).toHaveLength(0)

    loadMockBusinessSnapshot('FLOW-SNAPSHOT-06')
    expect(getMockBusinessState().registrations[0]?.status).toBe('SIGNED_IN')

    loadMockBusinessSnapshot('FLOW-SNAPSHOT-08')
    expect(getMockBusinessState().registrations[0]?.status).toBe('COMPLETED')
    expect(getMockBusinessState().certificates[0]?.certificateNo).toBe('CERT-2026-0001')
  })

  it('课程服务从仓储读取资格，不在页面拼装剩余名额', async () => {
    resetMockBusinessSnapshot('FLOW-SNAPSHOT-01')
    const first = await mockCourseService.getCourse('4001')
    expect(first.remainingSeats).toBe(12)
    expect(first.eligibility.apply.allowed).toBe(true)
    expect(first.eligibility.register.allowed).toBe(false)

    loadMockBusinessSnapshot('FLOW-SNAPSHOT-04')
    const filed = await mockCourseService.getCourse('4001')
    expect(filed.eligibility.register.allowed).toBe(true)
  })

  it('报名成功改变共享仓储；结果未知不会伪造失败', async () => {
    loadMockBusinessSnapshot('FLOW-SNAPSHOT-04')
    const result = await mockRegistrationService.create({ courseId: '4001' })
    expect(result.status).toBe('REGISTERED')
    expect(getMockBusinessState().registrations).toHaveLength(1)
    expect(getMockBusinessState().courses[0]?.remainingSeats).toBe(11)

    resetMockBusinessSnapshot('FLOW-SNAPSHOT-04')
    expect(getMockBusinessState().registrations).toHaveLength(0)
  })

  it('DTO 映射将未知枚举安全降级，并从详情承接资格', () => {
    const dto: CourseDetailDto = {
      courseId: '4001',
      courseName: '未知枚举课程',
      courseType: 'FUTURE_TYPE',
      trainerName: '—',
      startTime: '2026-09-01T09:00:00+08:00',
      endTime: '2026-09-01T10:00:00+08:00',
      location: '',
      status: 'FUTURE_STATUS',
      maxStudents: 10,
      registeredCount: 0,
      remainingSeats: 10,
      eligibility: { apply: { allowed: false, reasonCode: 'UNKNOWN', reason: '暂不可操作' } },
    }
    const envelope: ApiEnvelopeDto<CourseDetailDto> = { success: true, message: 'ok', data: dto }
    const result = mapCourseDetail(envelope)
    expect(result.type).toBe('UNKNOWN')
    expect(result.status).toBe('UNKNOWN')
    expect(result.eligibility.apply.reason).toBe('暂不可操作')
    expect(result.trainer.name).toBe('—')
  })

  it('稳定业务错误码不依赖中文提示', () => {
    const error = domainError('CONCURRENT_UPDATE')
    expect(error.ui.code).toBe('CONCURRENT_UPDATE')
    expect(error.ui.kind).toBe('conflict')
  })
})
