import { createRouter, createWebHistory, type RouteRecordRaw } from 'vue-router'
import { useAuthStore } from '@/stores/auth'
import type { RoleCode } from '@/types/enums'

const routes: RouteRecordRaw[] = [
  {
    path: '/login',
    name: 'Login',
    component: () => import('@/views/auth/LoginView.vue'),
    meta: { title: '登录', requiresAuth: false },
  },
  {
    path: '/',
    component: () => import('@/layouts/AppLayout.vue'),
    meta: { requiresAuth: true },
    redirect: '/dashboard',
    children: [
      {
        path: 'dashboard',
        name: 'Dashboard',
        component: () => import('@/views/dashboard/DashboardView.vue'),
        meta: { title: '工作台', icon: 'HomeFilled' },
      },
      // 培训中心
      {
        path: 'courses',
        name: 'CourseList',
        component: () => import('@/views/course/CourseListView.vue'),
        meta: { title: '课程大厅', icon: 'Collection' },
      },
      {
        path: 'courses/:id',
        name: 'CourseDetail',
        component: () => import('@/views/course/CourseDetailView.vue'),
        meta: { title: '课程详情', hidden: true },
      },
      {
        path: 'my-requests',
        name: 'MyRequests',
        meta: { title: '我的申请', icon: 'Document' },
        component: () => import('@/views/request/MyRequestsView.vue'),
      },
      {
        path: 'my-registrations',
        name: 'MyRegistrations',
        meta: { title: '我的报名', icon: 'List' },
        component: () => import('@/views/common/PlaceholderView.vue'),
      },
      {
        path: 'my-certificates',
        name: 'MyCertificates',
        meta: { title: '我的证书', icon: 'Medal' },
        component: () => import('@/views/common/PlaceholderView.vue'),
      },
      // 审批管理
      {
        path: 'dept-approval',
        name: 'DeptApproval',
        meta: { title: '主管审批', icon: 'Select', roles: ['DEPT_MANAGER', 'ADMIN'] },
        component: () => import('@/views/request/DeptApprovalView.vue'),
      },
      {
        path: 'hr-filing',
        name: 'HrFiling',
        meta: { title: 'HR 备案', icon: 'Stamp', roles: ['HR', 'ADMIN'] },
        component: () => import('@/views/request/HrFilingView.vue'),
      },
      // 培训运营
      {
        path: 'attendance',
        name: 'Attendance',
        meta: { title: '签到管理', icon: 'Calendar', roles: ['HR', 'ADMIN'] },
        component: () => import('@/views/common/PlaceholderView.vue'),
      },
      {
        path: 'evaluations',
        name: 'Evaluations',
        meta: { title: '评分与测试', icon: 'Star', roles: ['HR', 'ADMIN'] },
        component: () => import('@/views/common/PlaceholderView.vue'),
      },
      // P1 占位页
      {
        path: 'organization',
        name: 'Organization',
        meta: { title: '组织管理', icon: 'OfficeBuilding', roles: ['ADMIN'] },
        component: () => import('@/views/common/PlaceholderView.vue'),
      },
      {
        path: 'course-management',
        name: 'CourseManagement',
        meta: { title: '课程管理', icon: 'Edit', roles: ['ADMIN'] },
        component: () => import('@/views/common/PlaceholderView.vue'),
      },
      {
        path: 'risk-control',
        name: 'RiskControl',
        meta: { title: '风险控制', icon: 'Warning', roles: ['ADMIN'] },
        component: () => import('@/views/common/PlaceholderView.vue'),
      },
      {
        path: 'statistics',
        name: 'Statistics',
        meta: { title: '数据分析', icon: 'DataAnalysis', roles: ['ADMIN'] },
        component: () => import('@/views/common/PlaceholderView.vue'),
      },
      // 异常页面
      {
        path: '403',
        name: 'Forbidden',
        component: () => import('@/views/exception/ForbiddenView.vue'),
        meta: { title: '无权限', hidden: true },
      },
    ],
  },
  {
    path: '/:pathMatch(.*)*',
    name: 'NotFound',
    component: () => import('@/views/exception/NotFoundView.vue'),
    meta: { title: '页面不存在', requiresAuth: false },
  },
]

const router = createRouter({
  history: createWebHistory(),
  routes,
  scrollBehavior: () => ({ top: 0 }),
})

/**
 * 全局前置守卫：在首次导航前生效。
 * 首次进入会先恢复认证状态，再判断是否放行，避免重复调用 /api/auth/me。
 */
router.beforeEach(async (to) => {
  const auth = useAuthStore()

  // 仅恢复一次会话（幂等）
  if (!auth.initialized) {
    await auth.restoreSession()
  }

  // 公开页面（登录页 / 404）
  if (to.meta.requiresAuth === false) {
    // 已登录访问登录页时重定向到工作台
    if (auth.isAuthenticated && to.name === 'Login') {
      return { path: '/dashboard', replace: true }
    }
    return true
  }

  // 需要认证但未登录
  if (!auth.isAuthenticated) {
    return { path: '/login', query: { redirect: to.fullPath }, replace: true }
  }

  // 角色检查（与侧边栏菜单使用同一套 RoleCode 定义）
  const requiredRoles = (to.meta.roles as RoleCode[] | undefined) ?? []
  if (requiredRoles.length > 0 && !auth.hasRole(requiredRoles)) {
    return { path: '/403', replace: true }
  }

  return true
})

export default router
