import axios, { type AxiosRequestConfig } from 'axios'

import { asServiceError, isRetryableQueryError } from '@/api/error'
import { appConfig } from '@/config/env'
import type { ServiceOperation } from '@/types/api'

const transport = axios.create({
  baseURL: appConfig.apiBaseUrl || undefined,
  timeout: appConfig.requestTimeoutMs,
})

export interface ApiRequestConfig<TBody = unknown> extends Omit<
  AxiosRequestConfig<TBody>,
  'baseURL' | 'timeout'
> {
  operation: ServiceOperation
}

async function requestOnce<TResponse, TBody>(config: ApiRequestConfig<TBody>): Promise<TResponse> {
  try {
    const response = await transport.request<TResponse, { data: TResponse }, TBody>(config)
    return response.data
  } catch (error) {
    throw asServiceError(error, config.operation)
  }
}

export async function requestApi<TResponse, TBody = unknown>(
  config: ApiRequestConfig<TBody>,
): Promise<TResponse> {
  const maxAttempts = config.operation === 'query' ? 2 : 1

  for (let attempt = 1; attempt <= maxAttempts; attempt += 1) {
    try {
      return await requestOnce<TResponse, TBody>(config)
    } catch (error) {
      if (attempt === maxAttempts || !isRetryableQueryError(error) || config.signal?.aborted) {
        throw error
      }
    }
  }

  throw new Error('Unreachable request state')
}

/** 后端认证方案未冻结，Cookie、Token 或自定义请求头只允许在此替换点接入。 */
export const httpTransport = transport
