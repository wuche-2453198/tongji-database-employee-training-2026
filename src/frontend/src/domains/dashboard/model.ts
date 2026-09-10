import type { ServiceRequestOptions } from '@/domains/shared'

export interface DashboardStats {
  upcomingCourses: number
  activeRequests: number
  validCertificates: number
  pendingApprovals: number
  handledThisWeek: number
  overdueRequests: number
  pendingFilings: number
  pendingSignIn: number
  pendingCertificates: number
  totalCourses: number
}

export interface DashboardService {
  getStats(options?: ServiceRequestOptions): Promise<DashboardStats>
}
