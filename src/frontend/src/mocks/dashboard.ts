import type { DashboardService, DashboardStats } from '@/domains/dashboard'
import { mockWait } from '@/mocks/scenarios'

const stats: DashboardStats = {
  upcomingCourses: 3,
  activeRequests: 1,
  validCertificates: 2,
  pendingApprovals: 5,
  handledThisWeek: 12,
  overdueRequests: 2,
  pendingFilings: 4,
  pendingSignIn: 3,
  pendingCertificates: 2,
  totalCourses: 24,
}

export const mockDashboardService: DashboardService = {
  async getStats(options) {
    await mockWait(options?.signal, 50)
    return stats
  },
}
