import axios from 'axios'

import type { ApiEnvelopeDto } from '@/api/transport'
import type { FieldError, ServiceOperation, UiError, UiErrorKind } from '@/types/api'
import { ServiceError, isServiceError } from '@/types/api'

const statusKinds: Record<number, UiErrorKind> = {
  400: 'validation',
  401: 'unauthorized',
  403: 'forbidden',
  404: 'not-found',
  409: 'conflict',
}

const defaultMessages: Record<UiErrorKind, string> = {
  validation: '提交内容有误，请检查后重试。',
  unauthorized: '登录状态已失效，请重新登录。',
  forbidden: '当前账号无权执行此操作。',
  'not-found': '请求的资源不存在或已被移除。',
  conflict: '数据状态已发生变化，请刷新后重试。',
  network: '网络连接失败，请检查网络后重试。',
  timeout: '请求超时，请稍后重试。',
  canceled: '请求已取消。',
  server: '服务暂时不可用，请稍后重试。',
  'result-unknown': '操作结果未知，请先查询最新状态，避免重复提交。',
  unknown: '发生未知错误，请稍后重试。',
}

const isRecord = (value: unknown): value is Record<string, unknown> =>
  typeof value === 'object' && value !== null

function readEnvelope(value: unknown): Partial<ApiEnvelopeDto<unknown>> | undefined {
  if (!isRecord(value)) return undefined
  return value
}

function mapFieldErrors(value: unknown): FieldError[] {
  if (!Array.isArray(value)) return []

  return value.flatMap((item) => {
    if (!isRecord(item) || typeof item.field !== 'string' || typeof item.message !== 'string') {
      return []
    }
    return [
      { field: item.field, message: item.message, code: String(item.code || '') || undefined },
    ]
  })
}

function createUiError(kind: UiErrorKind, overrides: Partial<Omit<UiError, 'kind'>> = {}): UiError {
  return {
    kind,
    code: kind.toUpperCase().replace('-', '_'),
    message: defaultMessages[kind],
    fieldErrors: [],
    retryable: ['network', 'timeout', 'server'].includes(kind),
    resultUnknown: kind === 'result-unknown',
    ...overrides,
  }
}

export function fromEnvelopeFailure(envelope: ApiEnvelopeDto<unknown>, status = 400): ServiceError {
  const kind = statusKinds[status] ?? (status >= 500 ? 'server' : 'unknown')
  return new ServiceError(
    createUiError(kind, {
      code: envelope.code || `HTTP_${status}`,
      message: envelope.message || defaultMessages[kind],
      traceId: envelope.traceId,
      fieldErrors: mapFieldErrors(envelope.errors),
    }),
  )
}

export function toUiError(error: unknown, operation: ServiceOperation): UiError {
  if (isServiceError(error)) return error.ui

  if (axios.isCancel(error)) return createUiError('canceled', { code: 'REQUEST_CANCELED' })

  if (axios.isAxiosError(error)) {
    const response = error.response
    const isTimeout = ['ECONNABORTED', 'ETIMEDOUT'].includes(error.code || '')

    if (!response && operation === 'write' && (isTimeout || error.code === 'ERR_NETWORK')) {
      return createUiError('result-unknown', {
        code: isTimeout ? 'WRITE_TIMEOUT_RESULT_UNKNOWN' : 'WRITE_NETWORK_RESULT_UNKNOWN',
      })
    }

    if (isTimeout) return createUiError('timeout', { code: 'REQUEST_TIMEOUT' })
    if (!response) return createUiError('network', { code: error.code || 'NETWORK_ERROR' })

    const envelope = readEnvelope(response.data)
    const kind = statusKinds[response.status] ?? (response.status >= 500 ? 'server' : 'unknown')
    const message = typeof envelope?.message === 'string' ? envelope.message : defaultMessages[kind]
    const traceId = typeof envelope?.traceId === 'string' ? envelope.traceId : undefined
    const code = typeof envelope?.code === 'string' ? envelope.code : `HTTP_${response.status}`

    return createUiError(kind, {
      code,
      message,
      traceId,
      fieldErrors: mapFieldErrors(envelope?.errors),
    })
  }

  return createUiError('unknown')
}

export function asServiceError(error: unknown, operation: ServiceOperation): ServiceError {
  return isServiceError(error) ? error : new ServiceError(toUiError(error, operation))
}

export function isRetryableQueryError(error: unknown): boolean {
  const ui = toUiError(error, 'query')
  return ui.retryable && !ui.resultUnknown
}
