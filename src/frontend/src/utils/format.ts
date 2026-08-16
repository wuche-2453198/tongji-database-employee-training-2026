/**
 * 格式化金额（元）
 */
export function formatCurrency(amount: number): string {
  return `¥${amount.toLocaleString('zh-CN', { minimumFractionDigits: 0, maximumFractionDigits: 2 })}`
}

/**
 * 格式化日期时间
 */
export function formatDateTime(value: string | null | undefined): string {
  if (!value) return '-'
  const date = new Date(value)
  if (isNaN(date.getTime())) return '-'
  return date.toLocaleString('zh-CN', {
    year: 'numeric',
    month: '2-digit',
    day: '2-digit',
    hour: '2-digit',
    minute: '2-digit',
  })
}

/**
 * 格式化日期
 */
export function formatDate(value: string | null | undefined): string {
  if (!value) return '-'
  const date = new Date(value)
  if (isNaN(date.getTime())) return '-'
  return date.toLocaleDateString('zh-CN', {
    year: 'numeric',
    month: '2-digit',
    day: '2-digit',
  })
}

/**
 * 格式化日期范围
 */
export function formatDateRange(
  start: string | null | undefined,
  end: string | null | undefined,
): string {
  if (!start && !end) return '-'
  const startDate = start ? formatDate(start) : '?'
  const endDate = end ? formatDate(end) : '?'
  return `${startDate} ~ ${endDate}`
}

/**
 * 格式化培训学时
 */
export function formatDuration(hours: number): string {
  if (hours >= 8) {
    const days = Math.floor(hours / 8)
    const remain = hours % 8
    return remain > 0 ? `${days} 天 ${remain} 小时` : `${days} 天`
  }
  return `${hours} 小时`
}
