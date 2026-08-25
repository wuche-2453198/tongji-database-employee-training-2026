import 'vue-router'

import type { AppRole } from './navigation'

export {}

declare module 'vue-router' {
  interface RouteMeta {
    title?: string
    requiresAuth?: boolean
    roles?: AppRole[]
    menuKey?: string
    breadcrumb?: string[]
    keepAlive?: boolean
    priority?: 'P0' | 'P1' | 'P2'
    developmentOnly?: boolean
  }
}
