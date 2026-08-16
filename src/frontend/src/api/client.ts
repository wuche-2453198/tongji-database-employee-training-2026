import axios, { type AxiosInstance, type AxiosError } from 'axios'
import { mockRequest } from '@/mocks'
import { notifyUnauthorized } from './unauthorized'
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

// 响应拦截器：处理 401，交给外部监听器统一登出与跳转
httpClient.interceptors.response.use(
  (response) => response,
  (error: AxiosError) => {
    if (error.response?.status === 401) {
      notifyUnauthorized()
    }
    return Promise.reject(error)
  },
)

/**
 * 统一请求方法。
 * 根据 VITE_USE_MOCK 在 Mock 和真实 HTTP 之间切换。
 * 页面组件不感知此切换。
 */
export async function apiRequest<T>(
  method: 'GET' | 'POST' | 'PUT' | 'PATCH' | 'DELETE',
  url: string,
  data?: unknown,
): Promise<ApiResponse<T>> {
  if (USE_MOCK) {
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
    return response.data
  } catch (error) {
    const axiosError = error as AxiosError<ApiResponse<T>>
    if (axiosError.response?.data) {
      return axiosError.response.data
    }
    return {
      success: false,
      message: axiosError.message || '网络请求失败',
      data: null as never,
      traceId: '',
    }
  }
}

export { httpClient, USE_MOCK }
