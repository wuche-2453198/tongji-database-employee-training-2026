import { beforeEach, describe, expect, it } from 'vitest'

import { mockRatingService } from '@/mocks/rating'
import { mockTestService } from '@/mocks/test'
import { resetMockBusinessSnapshot } from '@/mocks/repositories/business-repository'
import { isServiceError } from '@/types/api'

function setActor(account: string) {
  window.sessionStorage.setItem(
    'training-management.mock-auth-session',
    JSON.stringify({ account }),
  )
}

describe('评分与测试 Mock 服务', () => {
  beforeEach(() => {
    resetMockBusinessSnapshot('FLOW-SNAPSHOT-01')
  })

  it('我的评分只返回当前员工的评分', async () => {
    setActor('employee.demo')
    const result = await mockRatingService.listMine({ page: 1, pageSize: 20 })
    expect(result.total).toBe(1)
    expect(result.items[0]).toMatchObject({
      courseName: '数据库性能优化实战',
      employeeId: 1001,
      score: 4.5,
    })

    setActor('manager.demo')
    const empty = await mockRatingService.listMine({ page: 1, pageSize: 20 })
    expect(empty.total).toBe(0)
  })

  it('测试成绩仅 HR 与 ADMIN 可查询，并支持类型筛选', async () => {
    setActor('hr.demo')
    const result = await mockTestService.listManage({ page: 1, pageSize: 20 })
    expect(result.total).toBe(2)
    expect(result.items.map((item) => item.testType)).toEqual(['POST', 'PRE'])

    const preOnly = await mockTestService.listManage({
      page: 1,
      pageSize: 20,
      testType: 'PRE',
    })
    expect(preOnly.total).toBe(1)
    expect(preOnly.items[0]?.score).toBe(78)

    setActor('employee.demo')
    await expect(mockTestService.listManage({ page: 1, pageSize: 20 })).rejects.toSatisfy(
      (error: unknown) => isServiceError(error) && error.ui.kind === 'forbidden',
    )
  })
})
