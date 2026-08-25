import { AxiosError, type AxiosResponse, type InternalAxiosRequestConfig } from 'axios'
import { beforeEach, describe, expect, it, vi } from 'vitest'

import {
  clearAccessToken,
  getAccessToken,
  saveAccessToken,
  setUnauthorizedHandler,
} from '@/api/auth-session'
import { httpTransport } from '@/api/client'
import { httpAuthService } from '@/services/http/auth-http'
import { httpCourseService } from '@/services/http/course-http'
import { httpTrainingRequestService } from '@/services/http/training-request-http'
import { isAuthServiceError } from '@/types/auth'

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

describe('整合分支真实 HTTP 适配器', () => {
  beforeEach(() => {
    window.localStorage.clear()
    window.sessionStorage.clear()
  })

  it('按后端 identifier 契约登录并保存 Bearer Token、归一化主管角色', async () => {
    const adapter = vi.fn(async (config: InternalAxiosRequestConfig) => {
      expect(config.url).toBe('/api/auth/login')
      expect(JSON.parse(String(config.data))).toEqual({ identifier: 'manager', password: 'secret' })
      return response(config, {
        success: true,
        message: 'ok',
        traceId: 'auth-login',
        data: {
          accessToken: 'jwt-token',
          tokenType: 'Bearer',
          expiresAt: '2026-08-26T00:00:00+08:00',
          user: {
            empId: 2,
            empName: '主管',
            deptName: '技术部',
            position: '部门主管',
            email: null,
            phone: null,
            status: 'ACTIVE',
            roles: [
              {
                roleId: 2,
                roleCode: 'DEPT_MANAGER',
                roleName: '部门主管',
                permissions: ['request.approve'],
              },
            ],
            permissions: ['auth.me', 'course.read', 'request.approve'],
          },
        },
      })
    })
    httpTransport.defaults.adapter = adapter

    const user = await httpAuthService.login({ account: 'manager', password: 'secret' })

    expect(getAccessToken()).toBe('jwt-token')
    expect(user.roles).toEqual(['DEPT_MANAGER'])
    expect(user.permissions).toContain('request.approve')
  })

  it('/api/auth/me 自动携带 Authorization，401 时清理会话并发出统一失效事件', async () => {
    const unauthorized = vi.fn()
    setUnauthorizedHandler(unauthorized)
    saveAccessToken('expired-token')
    httpTransport.defaults.adapter = async (config) => {
      expect(config.headers.Authorization).toBe('Bearer expired-token')
      throw new AxiosError(
        'unauthorized',
        'ERR_BAD_REQUEST',
        config,
        undefined,
        response(config, { success: false, message: 'Unauthorized', data: null }, 401),
      )
    }

    await expect(httpAuthService.getCurrentUser()).rejects.toSatisfy(
      (error: unknown) => isAuthServiceError(error) && error.code === 'SESSION_EXPIRED',
    )
    expect(getAccessToken()).toBeNull()
    expect(unauthorized).toHaveBeenCalledOnce()
  })

  it('课程列表使用业务分支记录的真实查询字段并兼容后端课程 DTO', async () => {
    httpTransport.defaults.adapter = async (config) => {
      expect(config.url).toBe('/api/courses')
      expect(config.params).toMatchObject({
        CourseName: '数据库',
        CourseStatus: 'PUBLISHED',
        Page: 1,
        PageSize: 20,
      })
      return response(config, {
        success: true,
        message: 'ok',
        data: {
          items: [
            {
              courseId: 4001,
              courseName: '数据库实践',
              courseType: 'SKILL',
              trainerName: '讲师',
              startAt: '2026-09-01T09:00:00+08:00',
              endAt: '2026-09-01T17:00:00+08:00',
              location: 'A101',
              courseStatus: 'PUBLISHED',
              maxStudents: 20,
              enrolledCount: 3,
            },
          ],
          page: 1,
          pageSize: 20,
          total: 1,
        },
      })
    }

    const result = await httpCourseService.listCourses({
      keyword: '数据库',
      status: 'PUBLISHED',
      page: 1,
      pageSize: 20,
    })
    expect(result.items[0]).toMatchObject({ id: '4001', registeredCount: 3, remainingSeats: 17 })
  })

  it('申请列表使用 /my 路径，主管审批使用 dept-approve 动作路径', async () => {
    const adapter = vi.fn(async (config: InternalAxiosRequestConfig) => {
      if (config.url === '/api/training-requests/my') {
        return response(config, {
          success: true,
          message: 'ok',
          data: { items: [], page: 1, pageSize: 20, total: 0 },
        })
      }
      if (config.url === '/api/training-requests/5001/dept-approve') {
        expect(JSON.parse(String(config.data))).toEqual({ Comment: '同意' })
        return response(config, {
          success: true,
          message: 'ok',
          data: { requestId: 5001 },
        })
      }
      if (config.url === '/api/training-requests/5001') {
        return response(config, {
          success: true,
          message: 'ok',
          data: {
            requestId: 5001,
            courseId: 4001,
            courseName: '数据库实践',
            employeeId: 1,
            employeeName: '员工',
            departmentName: '技术部',
            requestReason: '提升能力',
            status: 'DEPT_APPROVED',
            createdAt: '2026-08-25T09:00:00+08:00',
          },
        })
      }
      throw new Error(`unexpected request: ${config.url}`)
    })
    httpTransport.defaults.adapter = adapter

    await expect(
      httpTrainingRequestService.listMine({ page: 1, pageSize: 20 }),
    ).resolves.toMatchObject({ total: 0 })
    await expect(httpTrainingRequestService.approve('5001', '同意')).resolves.toMatchObject({
      id: '5001',
      status: 'DEPT_APPROVED',
    })
    expect(adapter.mock.calls.map(([config]) => config.url)).toEqual([
      '/api/training-requests/my',
      '/api/training-requests/5001/dept-approve',
      '/api/training-requests/5001',
    ])
    clearAccessToken()
  })
})
