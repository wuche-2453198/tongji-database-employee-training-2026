import type { AxiosResponse, InternalAxiosRequestConfig } from 'axios'
import { beforeEach, describe, expect, it } from 'vitest'

import { httpTransport } from '@/api/client'
import type { RegistrationDto } from '@/api/transport'
import { httpRegistrationService } from '@/services/http/registration-http'

const response = <T>(
  config: InternalAxiosRequestConfig,
  data: T,
  status = 200,
): AxiosResponse<T> => ({
  config,
  data,
  headers: {},
  status,
  statusText: String(status),
})

const baseRegistration = (overrides: Partial<RegistrationDto> = {}): RegistrationDto => ({
  regId: 6001,
  requestId: 5001,
  empId: 55,
  empName: '张三',
  deptId: 101,
  deptName: '技术部',
  courseId: 4001,
  courseName: '数据库实践',
  courseType: 'SKILL',
  durationHours: 8,
  trainerName: '陈老师',
  startAt: '2026-09-02T09:00:00+08:00',
  endAt: '2026-09-02T17:00:00+08:00',
  location: 'A201',
  courseStatus: 'PUBLISHED',
  maxStudents: 20,
  status: 'REGISTERED',
  registeredAt: '2026-08-21T10:00:00+08:00',
  completedAt: null,
  actualHours: null,
  canceledAt: null,
  cancelReason: null,
  attendance: null,
  actions: {
    cancel: { allowed: true, reason: null },
    signIn: { allowed: true, reason: null },
    complete: { allowed: false, reason: '需先签到。' },
    markAbsent: { allowed: true, reason: null },
  },
  ...overrides,
})

describe('报名签到真实 HTTP 适配器', () => {
  beforeEach(() => {
    window.localStorage.clear()
    window.sessionStorage.clear()
  })

  it('listMine 使用报名查询契约并映射 SCAN 签到为本人签到', async () => {
    httpTransport.defaults.adapter = async (config) => {
      expect(config.url).toBe('/api/registrations/my')
      expect(config.params).toMatchObject({
        CourseName: '数据库',
        Status: 'SIGNED_IN',
        SortBy: 'regDate',
        SortDirection: 'desc',
        Page: 1,
        PageSize: 20,
      })
      return response(config, {
        success: true,
        message: 'ok',
        data: {
          items: [
            baseRegistration({
              status: 'SIGNED_IN',
              attendance: {
                attendId: 1,
                regId: 6001,
                signinType: 'SCAN',
                signedInAt: '2026-09-02T09:00:00+08:00',
                latenessMinutes: 0,
                deductHours: 0,
                remark: null,
                createdAt: '2026-09-02T09:00:00+08:00',
              },
            }),
          ],
          page: 1,
          pageSize: 20,
          total: 1,
        },
      })
    }

    const result = await httpRegistrationService.listMine({
      keyword: '数据库',
      status: 'SIGNED_IN',
      sortBy: 'regDate',
      sortDirection: 'desc',
      page: 1,
      pageSize: 20,
    })
    expect(result.total).toBe(1)
    expect(result.items[0]).toMatchObject({
      id: '6001',
      employeeId: 55,
      courseName: '数据库实践',
      status: 'SIGNED_IN',
      signinMethod: 'SELF',
    })
  })

  it('create 以数字课程 ID 提交并映射报名响应', async () => {
    httpTransport.defaults.adapter = async (config) => {
      expect(config.url).toBe('/api/registrations')
      expect(JSON.parse(String(config.data))).toEqual({ CourseId: 4001 })
      return response(config, { success: true, message: 'ok', data: baseRegistration() })
    }

    const result = await httpRegistrationService.create({ courseId: '4001' })
    expect(result).toMatchObject({ id: '6001', status: 'REGISTERED', employeeId: 55 })
  })

  it('signIn 命中报名签到端点并映射考勤响应', async () => {
    httpTransport.defaults.adapter = async (config) => {
      expect(config.url).toBe('/api/registrations/6001/signin')
      expect(config.method?.toLowerCase()).toBe('post')
      return response(config, {
        success: true,
        message: 'ok',
        data: {
          attendId: 1,
          regId: 6001,
          signinType: 'MANUAL',
          signedInAt: '2026-09-02T09:06:00+08:00',
          latenessMinutes: 6,
          deductHours: 0.5,
          remark: null,
          createdAt: '2026-09-02T09:06:00+08:00',
        },
      })
    }

    const result = await httpRegistrationService.signIn('6001')
    expect(result).toMatchObject({
      attendId: 1,
      regId: '6001',
      signinType: 'MANUAL',
      deductHours: 0.5,
    })
  })

  it('manualSignIn 提交补签请求并映射考勤响应', async () => {
    httpTransport.defaults.adapter = async (config) => {
      expect(config.url).toBe('/api/attendance/manual')
      expect(config.method?.toLowerCase()).toBe('post')
      expect(JSON.parse(String(config.data))).toEqual({
        RegId: 6001,
        SigninTime: '2026-09-02T09:10:00+08:00',
        Remark: '交通延误',
      })
      return response(config, {
        success: true,
        message: 'ok',
        data: {
          attendId: 2,
          regId: 6001,
          signinType: 'MANUAL',
          signedInAt: '2026-09-02T09:10:00+08:00',
          latenessMinutes: 10,
          deductHours: 0.5,
          remark: '交通延误',
          createdAt: '2026-09-02T09:10:00+08:00',
        },
      })
    }

    const result = await httpRegistrationService.manualSignIn({
      regId: '6001',
      signinTime: '2026-09-02T09:10:00+08:00',
      remark: '交通延误',
    })
    expect(result).toMatchObject({ attendId: 2, remark: '交通延误' })
  })

  it('取消、缺席、完成命中各自动作端点', async () => {
    const calls: Array<{ url?: string; method?: string }> = []
    httpTransport.defaults.adapter = async (config) => {
      calls.push({ url: config.url, method: config.method })
      return response(config, {
        success: true,
        message: 'ok',
        data: baseRegistration({ status: 'CANCELED' }),
      })
    }

    await httpRegistrationService.cancel('6001')
    await httpRegistrationService.markAbsent('6001')
    await httpRegistrationService.complete('6001')

    expect(calls.map((call) => call.url)).toEqual([
      '/api/registrations/6001/cancel',
      '/api/registrations/6001/absent',
      '/api/registrations/6001/complete',
    ])
    expect(calls.every((call) => call.method?.toLowerCase() === 'patch')).toBe(true)
  })
})
