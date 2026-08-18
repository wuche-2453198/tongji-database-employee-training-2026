import type { LocationQueryRaw, LocationQueryValue } from 'vue-router'

type QueryValue = LocationQueryValue | LocationQueryValue[]

const firstValue = (value: QueryValue): LocationQueryValue =>
  Array.isArray(value) ? (value[0] ?? null) : value

export function readQueryString(value: QueryValue, fallback = ''): string {
  const resolved = firstValue(value)
  return typeof resolved === 'string' ? resolved.trim() : fallback
}

export function readPositiveQueryInteger(value: QueryValue, fallback: number): number {
  const parsed = Number(readQueryString(value))
  return Number.isInteger(parsed) && parsed > 0 ? parsed : fallback
}

export function compactListQuery(
  values: Record<string, string | number | null | undefined>,
): LocationQueryRaw {
  return Object.fromEntries(
    Object.entries(values)
      .filter(([, value]) => value !== '' && value !== null && value !== undefined)
      .map(([key, value]) => [key, String(value)]),
  )
}
