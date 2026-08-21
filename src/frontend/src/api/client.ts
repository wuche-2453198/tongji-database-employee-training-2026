import axios, { type AxiosInstance, type AxiosError } from 'axios'
import { mockRequest } from '@/mocks'
import { notifyUnauthorized } from './unauthorized'
import { toCamelCase } from '@/utils/case'
import type { ApiResponse } from '@/types/api'

const USE_MOCK = import.meta.env.VITE_USE_MOCK === 'true'

const httpClient: AxiosInstance = axios.create({
  baseURL: USE_MOCK ? '' : import.meta.env.VITE_API_BASE_URL,
  timeout: 15000,
  headers: { 'Content-Type': 'application/json' },
})

// 请求拦截器：添加 Authorization
httpClient.interceptors.request.use((config) => {
  const token = localStorage.getItem('access_token')
  if (token) {
    config.headers.Authorization = `Bearer ${token}`
  }
  return config
})

// 401 并发去重：多个请求同时返回 401 时，只触发一次统一登出/跳转
let unauthorizedNotified = false

function handleUnauthorized(): void {
  if (unauthorizedNotified) return
  unauthorizedNotified = true
  notifyUnauthorized()
  // 跳转登录页后重新放行，允许登录流程中的下一次 401 正常处理
  setTimeout(() => {
    unauthorizedNotified = false
  }, 1200)
}

// 响应拦截器：仅负责 401 桥接，具体错误文案在 apiRequest 中统一归一化
httpClient.interceptors.response.use(
  (response) => response,
  (error: AxiosError) => {
    if (error.response?.status === 401) {
      handleUnauthorized()
    }
    return Promise.reject(error)
  },
)

/** 根据 HTTP 状态码提供可理解的中文兜底文案 */
function fallbackMessage(status: number | undefined): string {
  switch (status) {
    case 400:
      return '请求参数有误'
    case 401:
      return '登录已过期，请重新登录'
    case 403:
      return '没有权限执行此操作'
    case 404:
      return '请求的资源不存在'
    case 409:
      return '操作冲突，请刷新后重试'
    case 422:
      return '数据校验失败'
    case 500:
      return '服务器内部错误，请稍后重试'
    default:
      return '网络请求失败'
  }
}

interface ApiRequestOptions {
  /** 该次请求是否走 Mock（默认跟随全局 VITE_USE_MOCK） */
  mock?: boolean
}

/**
 * 统一请求方法。
 * - 根据 mock 开关在 Mock 与真实 HTTP 之间切换，页面组件不感知。
 * - 真实响应统一将后端 PascalCase 数据转为 camelCase 领域模型。
 * - 失败时统一返回 ApiResponse（success=false + 中文 message），不抛异常。
 */
export async function apiRequest<T>(
  method: 'GET' | 'POST' | 'PUT' | 'PATCH' | 'DELETE',
  url: string,
  data?: unknown,
  options: ApiRequestOptions = {},
): Promise<ApiResponse<T>> {
  const useMock = options.mock ?? USE_MOCK

  if (useMock) {
    const token = localStorage.getItem('access_token') ?? undefined
    return mockRequest<T>(method, url, data, token)
  }

  try {
    const response = await httpClient.request<ApiResponse<T>>({
      method,
      url,
      data: method !== 'GET' ? data : undefined,
      params: method === 'GET' ? data : undefined,
    })
    const body = response.data
    return { ...body, data: toCamelCase(body.data) }
  } catch (error) {
    const axiosError = error as AxiosError<ApiResponse<T>>
    const status = axiosError.response?.status
    const serverBody = axiosError.response?.data

    if (serverBody && typeof serverBody === 'object') {
      return {
        ...serverBody,
        success: false,
        message: (serverBody as ApiResponse<T>).message || fallbackMessage(status),
        data: null as never,
      }
    }

    return {
      success: false,
      message: fallbackMessage(status),
      data: null as never,
      traceId: '',
    }
  }
}

export { httpClient, USE_MOCK }
