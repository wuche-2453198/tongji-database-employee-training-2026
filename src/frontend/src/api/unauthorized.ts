/**
 * 401 未授权事件桥接。
 * API 客户端不直接依赖 router / store，避免循环依赖；
 * 由 main.ts 注册处理器，统一执行登出与跳转。
 */
type UnauthorizedListener = () => void

let listener: UnauthorizedListener | null = null

export function setUnauthorizedListener(fn: UnauthorizedListener | null): void {
  listener = fn
}

export function notifyUnauthorized(): void {
  listener?.()
}
