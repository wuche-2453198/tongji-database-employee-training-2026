import { apiRequest } from './client'

export interface DashboardStats {
  /** 卡片统计项 */
  cards: DashboardCard[]
  /** 快捷入口 */
  shortcuts: DashboardShortcut[]
}

export interface DashboardCard {
  key: string
  label: string
  value: number
  icon: string
  color?: string
  /** 可选跳转地址，指向已实现的页面 */
  path?: string
}

export interface DashboardShortcut {
  label: string
  path: string
  icon: string
}

/** 获取当前角色的 Dashboard 统计数据 */
export function getDashboardStatsApi() {
  return apiRequest<DashboardStats>('GET', '/api/dashboard/stats')
}
