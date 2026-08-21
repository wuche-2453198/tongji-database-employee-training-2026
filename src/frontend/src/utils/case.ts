/**
 * 字段命名风格转换工具。
 * 后端 DTO 统一使用 PascalCase（如 CourseName / PreTestUrl），
 * 前端领域模型统一使用 camelCase（如 courseName / preTestUrl）。
 * 仅在真实 API 响应路径上做转换，Mock 已直接返回 camelCase，不经过此层。
 */

function upperFirst(key: string): string {
  return key.charAt(0).toUpperCase() + key.slice(1)
}

function lowerFirst(key: string): string {
  return key.charAt(0).toLowerCase() + key.slice(1)
}

function isPlainObject(value: unknown): value is Record<string, unknown> {
  return typeof value === 'object' && value !== null && !Array.isArray(value)
}

/** 递归将对象 key 转为 camelCase（仅处理普通对象与数组，原始值/Date 保持原样） */
export function toCamelCase<T>(value: T): T {
  if (Array.isArray(value)) {
    return value.map((item) => toCamelCase(item)) as unknown as T
  }

  if (!isPlainObject(value)) {
    return value
  }

  const result: Record<string, unknown> = {}
  for (const [key, val] of Object.entries(value)) {
    result[lowerFirst(key)] = toCamelCase(val)
  }
  return result as unknown as T
}

/** 递归将对象 key 转为 PascalCase（用于请求体；查询参数请在 service 层做显式映射，含字段改名） */
export function toPascalCase<T>(value: T): T {
  if (Array.isArray(value)) {
    return value.map((item) => toPascalCase(item)) as unknown as T
  }

  if (!isPlainObject(value)) {
    return value
  }

  const result: Record<string, unknown> = {}
  for (const [key, val] of Object.entries(value)) {
    result[upperFirst(key)] = toPascalCase(val)
  }
  return result as unknown as T
}
