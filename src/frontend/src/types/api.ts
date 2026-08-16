/** 后端统一响应结构，对应 ApiResponse<T> */
export interface ApiResponse<T = unknown> {
  success: boolean
  message: string
  data: T
  traceId: string
  errors?: ApiError[]
}

export interface ApiError {
  field: string
  message: string
}

/** 后端分页结构，对应 PagedResult<T> */
export interface PagedResult<T> {
  items: T[]
  page: number
  pageSize: number
  total: number
}
