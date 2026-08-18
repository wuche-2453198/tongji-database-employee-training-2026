import { AxiosError, type AxiosResponse, type InternalAxiosRequestConfig } from 'axios'
import { describe, expect, it, vi } from 'vitest'

import { toUiError } from '@/api/error'
import { requestApi } from '@/api/client'
import { mapCoursePage } from '@/api/mappers/course'
import { LatestRequestController, SingleFlightController } from '@/api/request-control'
import type { ApiEnvelopeDto, CourseSummaryDto, PageDto } from '@/api/transport'
import { createMockCourseService, type CourseMockScenario } from '@/mocks/course'
import { isServiceError } from '@/types/api'

const query = { page: 1, pageSize: 20 }

function axiosResponse(status: number, data: unknown): AxiosResponse {
  return {
    status,
    statusText: String(status),
    headers: {},
    config: { headers: {} } as InternalAxiosRequestConfig,
    data,
  }
}

describe('M5 API 与 Mock 边界', () => {
  it('传输 DTO 映射为领域模型并对未知枚举兜底', () => {
    const dto: CourseSummaryDto = {
      courseId: 'C-1',
      courseName: '领域映射测试',
      courseType: 'NEW_TYPE',
      trainerName: '测试讲师',
      startTime: '2026-09-01T09:00:00+08:00',
      endTime: '2026-09-01T10:00:00+08:00',
      location: 'A101',
      status: 'NEW_STATUS',
      maxStudents: 20,
      registeredCount: 3,
      remainingSeats: 17,
    }
    const envelope: ApiEnvelopeDto<PageDto<CourseSummaryDto>> = {
      success: true,
      message: 'ok',
      data: { items: [dto], page: 1, pageSize: 20, total: 1 },
    }

    const result = mapCoursePage(envelope)
    expect(result.items[0]).toMatchObject({
      id: 'C-1',
      name: '领域映射测试',
      type: 'UNKNOWN',
      typeLabel: '未知类型',
      status: 'UNKNOWN',
      statusLabel: '未知状态',
    })
  })

  it.each([
    ['empty', 0],
    ['normal', 2],
  ] as const)('Mock 查询场景 %s 返回领域分页', async (scenario, expectedTotal) => {
    const service = createMockCourseService(() => scenario)
    const result = await service.listCourses(query)
    expect(result.total).toBe(expectedTotal)
    expect(result.items).toHaveLength(expectedTotal)
  })

  it.each([
    ['failure', 'server'],
    ['forbidden', 'forbidden'],
    ['not-found', 'not-found'],
  ] as const)('Mock 查询场景 %s 映射为 %s UI 错误', async (scenario, kind) => {
    const service = createMockCourseService(() => scenario)
    await expect(service.listCourses(query)).rejects.toSatisfy(
      (error: unknown) => isServiceError(error) && error.ui.kind === kind,
    )
  })

  it.each([
    ['conflict', 'conflict', false],
    ['result-unknown', 'result-unknown', true],
  ] as const)('Mock 写场景 %s 映射为 %s', async (scenario, kind, resultUnknown) => {
    const service = createMockCourseService(() => scenario as CourseMockScenario)
    try {
      await service.registerForCourse('COURSE-2026-001')
      throw new Error('预期操作失败')
    } catch (error) {
      expect(isServiceError(error)).toBe(true)
      if (isServiceError(error)) {
        expect(error.ui.kind).toBe(kind)
        expect(error.ui.resultUnknown).toBe(resultUnknown)
      }
    }
  })

  it('HTTP 状态与字段错误统一映射为 UI 错误', () => {
    const error = new AxiosError(
      'conflict',
      'ERR_BAD_REQUEST',
      undefined,
      undefined,
      axiosResponse(409, {
        message: '课程名额已满',
        code: 'COURSE_FULL',
        traceId: 'trace-409',
        errors: [{ field: 'courseId', message: '没有剩余名额', code: 'FULL' }],
      }),
    )
    const ui = toUiError(error, 'write')

    expect(ui).toMatchObject({
      kind: 'conflict',
      code: 'COURSE_FULL',
      message: '课程名额已满',
      traceId: 'trace-409',
      resultUnknown: false,
    })
    expect(ui.fieldErrors).toEqual([{ field: 'courseId', message: '没有剩余名额', code: 'FULL' }])
  })

  it('写操作超时与查询超时采用不同恢复语义', () => {
    const timeout = new AxiosError('timeout', 'ECONNABORTED')
    expect(toUiError(timeout, 'query')).toMatchObject({ kind: 'timeout', retryable: true })
    expect(toUiError(timeout, 'write')).toMatchObject({
      kind: 'result-unknown',
      retryable: false,
      resultUnknown: true,
    })
  })

  it.each([
    [401, 'unauthorized'],
    [403, 'forbidden'],
    [404, 'not-found'],
    [409, 'conflict'],
    [500, 'server'],
  ] as const)('HTTP %s 映射为 %s', (status, kind) => {
    const error = new AxiosError(
      String(status),
      'ERR_BAD_RESPONSE',
      undefined,
      undefined,
      axiosResponse(status, { message: `HTTP ${status}`, traceId: `trace-${status}` }),
    )
    expect(toUiError(error, 'query')).toMatchObject({ kind, traceId: `trace-${status}` })
  })

  it('查询故障最多重试一次，写操作不自动重试', async () => {
    const failedResponse = axiosResponse(503, { message: '服务暂不可用' })
    const queryAdapter = vi
      .fn()
      .mockRejectedValueOnce(
        new AxiosError('unavailable', 'ERR_BAD_RESPONSE', undefined, undefined, failedResponse),
      )
      .mockResolvedValueOnce(axiosResponse(200, { ok: true }))

    await expect(
      requestApi<{ ok: boolean }>({
        method: 'GET',
        url: '/query',
        operation: 'query',
        adapter: queryAdapter,
      }),
    ).resolves.toEqual({ ok: true })
    expect(queryAdapter).toHaveBeenCalledTimes(2)

    const writeAdapter = vi
      .fn()
      .mockRejectedValue(
        new AxiosError('unavailable', 'ERR_BAD_RESPONSE', undefined, undefined, failedResponse),
      )
    await expect(
      requestApi({
        method: 'POST',
        url: '/write',
        operation: 'write',
        adapter: writeAdapter,
      }),
    ).rejects.toSatisfy((error: unknown) => isServiceError(error) && error.ui.kind === 'server')
    expect(writeAdapter).toHaveBeenCalledOnce()
  })

  it('新的查询会取消旧查询', async () => {
    const controller = new LatestRequestController()
    const aborted = vi.fn()
    const first = controller.run(
      (signal) =>
        new Promise<string>((_resolve, reject) => {
          signal.addEventListener('abort', () => {
            aborted()
            reject(new DOMException('aborted', 'AbortError'))
          })
        }),
    )
    const second = controller.run(async () => 'latest')

    await expect(first).rejects.toMatchObject({ name: 'AbortError' })
    await expect(second).resolves.toBe('latest')
    expect(aborted).toHaveBeenCalledOnce()
  })

  it('相同写操作只执行一个在途请求', async () => {
    const controller = new SingleFlightController()
    const task = vi.fn(async () => 'receipt')
    const first = controller.run('register:C-1', task)
    const second = controller.run('register:C-1', task)

    expect(first).toBe(second)
    await expect(first).resolves.toBe('receipt')
    expect(task).toHaveBeenCalledOnce()
    expect(controller.isRunning('register:C-1')).toBe(false)
  })
})
