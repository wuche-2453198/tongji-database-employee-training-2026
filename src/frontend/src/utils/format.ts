/**
 * 统一格式化工具。空值一律显示「—」。
 */

const EMPTY = '—'

function pad(n: number): string {
  return String(n).padStart(2, '0')
}

function toDate(value: string | number | null | undefined): Date | null {
  if (value === null || value === undefined || value === '') return null
  const d = new Date(value)
  return isNaN(d.getTime()) ? null : d
}

/**
 * 格式化金额（人民币）
 */
export function formatCurrency(amount: number | null | undefined): string {
  if (amount === null || amount === undefined || isNaN(amount)) return EMPTY
  const hasDecimal = amount % 1 !== 0
  return `¥${amount.toLocaleString('zh-CN', {
    minimumFractionDigits: hasDecimal ? 2 : 0,
    maximumFractionDigits: 2,
  })}`
}

/**
 * 格式化日期时间：YYYY-MM-DD HH:mm
 */
export function formatDateTime(value: string | number | null | undefined): string {
  const d = toDate(value)
  if (!d) return EMPTY
  return `${d.getFullYear()}-${pad(d.getMonth() + 1)}-${pad(d.getDate())} ${pad(d.getHours())}:${pad(d.getMinutes())}`
}

/**
 * 格式化日期：YYYY-MM-DD
 */
export function formatDate(value: string | number | null | undefined): string {
  const d = toDate(value)
  if (!d) return EMPTY
  return `${d.getFullYear()}-${pad(d.getMonth() + 1)}-${pad(d.getDate())}`
}

/**
 * 格式化日期范围
 */
export function formatDateRange(
  start: string | null | undefined,
  end: string | null | undefined,
): string {
  if (!start && !end) return EMPTY
  const startDate = start ? formatDate(start) : '?'
  const endDate = end ? formatDate(end) : '?'
  return `${startDate} ~ ${endDate}`
}

/**
 * 格式化培训学时
 */
export function formatDuration(hours: number | null | undefined): string {
  if (hours === null || hours === undefined || isNaN(hours)) return EMPTY
  if (hours >= 8) {
    const days = Math.floor(hours / 8)
    const remain = hours % 8
    return remain > 0 ? `${days} 天 ${remain} 小时` : `${days} 天`
  }
  return `${hours} 小时`
}

/**
 * 空值统一显示「—」
 */
export function formatEmpty(value: string | number | null | undefined): string {
  if (value === null || value === undefined || value === '') return EMPTY
  return String(value)
}
