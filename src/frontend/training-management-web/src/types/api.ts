export type ServiceOperation = 'query' | 'write'

export type UiErrorKind =
  | 'validation'
  | 'unauthorized'
  | 'forbidden'
  | 'not-found'
  | 'conflict'
  | 'network'
  | 'timeout'
  | 'canceled'
  | 'server'
  | 'result-unknown'
  | 'unknown'

export interface FieldError {
  field: string
  message: string
  code?: string
}

export interface UiError {
  kind: UiErrorKind
  code: string
  message: string
  traceId?: string
  fieldErrors: FieldError[]
  retryable: boolean
  resultUnknown: boolean
}

export interface PageResult<T> {
  items: T[]
  page: number
  pageSize: number
  total: number
}

export interface ServiceRequestOptions {
  signal?: AbortSignal
}

export class ServiceError extends Error {
  readonly ui: UiError

  constructor(ui: UiError) {
    super(ui.message)
    this.name = 'ServiceError'
    this.ui = ui
  }
}

export const isServiceError = (error: unknown): error is ServiceError =>
  error instanceof ServiceError
