export type StatusSemantic = 'success' | 'warning' | 'error' | 'info' | 'neutral'

export interface SelectOption {
  label: string
  value: string | number
  disabled?: boolean
}

export interface TableColumn {
  key: string
  label: string
  width?: number | string
  minWidth?: number | string
  align?: 'left' | 'center' | 'right'
}

export interface DescriptionItem {
  label: string
  value: string | number | null | undefined
  span?: number
}

export type TimelineNodeState = 'complete' | 'current' | 'future' | 'error'

export interface TimelineNode {
  title: string
  meta?: string
  summary?: string
  state: TimelineNodeState
}

export interface CourseInfo {
  name: string
  type: string
  trainer: string
  schedule: string
  location: string
  hours: number
  remainingSeats?: number | null
  cover?: string
}
