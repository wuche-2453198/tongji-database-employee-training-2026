import {
  createRouter,
  createWebHistory,
  type RouteLocationRaw,
  type RouteMeta,
  type RouteRecordRaw,
  type RouterScrollBehavior,
} from 'vue-router'

import type { AppRole } from '@/types/navigation'
import { hasAnyRole } from '@/config/permissions'
import { pinia } from '@/stores'
import { useAuthStore } from '@/stores/auth'
import type { AppUser } from '@/types/auth'

const allRoles: AppRole[] = ['EMPLOYEE', 'DEPT_MANAGER', 'HR', 'ADMIN']

const protectedPage = (
  path: string,
  name: string,
  title: string,
  roles: AppRole[],
  menuKey: string,
  breadcrumb: string[],
): RouteRecordRaw => ({
  path,
  name,
  component:
    name === 'course-list'
      ? () => import('@/views/courses/CourseListView.vue')
      : name === 'course-detail'
        ? () => import('@/views/courses/CourseDetailView.vue')
        : name === 'request-create'
          ? () => import('@/views/requests/RequestCreateView.vue')
          : name === 'my-request-list'
            ? () => import('@/views/requests/MyRequestListView.vue')
            : name === 'request-detail'
              ? () => import('@/views/requests/RequestDetailView.vue')
              : name === 'department-approval'
                ? () => import('@/views/approvals/DepartmentApprovalView.vue')
                : name === 'hr-filing'
                  ? () => import('@/views/filings/HrFilingView.vue')
                  : name === 'my-registration-list'
                    ? () => import('@/views/registrations/MyRegistrationListView.vue')
                    : name === 'registration-detail'
                      ? () => import('@/views/registrations/RegistrationDetailView.vue')
                      : name === 'attendance-management'
                        ? () => import('@/views/attendance/AttendanceManagementView.vue')
                        : name === 'my-certificate-list'
                          ? () => import('@/views/certificates/MyCertificateListView.vue')
                          : name === 'certificate-management'
                            ? () => import('@/views/certificates/CertificateManagementView.vue')
                            : name === 'certificate-detail'
                              ? () => import('@/views/certificates/CertificateDetailView.vue')
                              : name === 'my-rating-list'
                                ? () => import('@/views/ratings/MyRatingListView.vue')
                                : name === 'test-management'
                                  ? () => import('@/views/tests/TestManagementView.vue')
                                  : name === 'blacklist-management'
                                    ? () => import('@/views/blacklist/BlacklistManagementView.vue')
                                    : name === 'employee-management'
                                      ? () => import('@/views/employees/EmployeeManagementView.vue')
                                      : name === 'rating-management'
                                        ? () => import('@/views/ratings/RatingManagementView.vue')
                                        : name === 'department-budget-management'
                                          ? () => import('@/views/budgets/DepartmentBudgetManagementView.vue')
                                          : name === 'trainer-management'
                                            ? () => import('@/views/trainers/TrainerManagementView.vue')
                                    : () => import('@/views/errors/NotFoundView.vue'),
  meta: {
    title,
    requiresAuth: true,
    roles,
    menuKey,
    breadcrumb,
    keepAlive: false,
    priority: 'P0',
  },
})

const routes: RouteRecordRaw[] = [
  {
    path: '/',
    redirect: '/dashboard',
  },
  {
    path: '/login',
    component: () => import('@/layouts/AuthLayout.vue'),
    children: [
      {
        path: '',
        name: 'login',
        component: () => import('@/views/auth/LoginView.vue'),
        meta: { title: '登录', priority: 'P0' },
      },
    ],
  },
  {
    path: '/',
    component: () => import('@/layouts/MainLayout.vue'),
    children: [
      {
        path: 'dashboard',
        name: 'dashboard',
        component: () => import('@/views/dashboard/DashboardView.vue'),
        meta: {
          title: '控制台',
          requiresAuth: true,
          roles: allRoles,
          menuKey: 'dashboard',
          breadcrumb: ['首页', '控制台'],
          keepAlive: false,
          priority: 'P0',
        },
      },
      protectedPage('courses', 'course-list', '课程中心', allRoles, 'courses', [
        '培训中心',
        '课程中心',
      ]),
      protectedPage('courses/:id', 'course-detail', '课程详情', allRoles, 'courses', [
        '培训中心',
        '课程中心',
        '课程详情',
      ]),
      protectedPage(
        'my/requests/new',
        'request-create',
        '发起培训申请',
        ['EMPLOYEE'],
        'my-requests',
        ['我的培训', '我的申请', '发起申请'],
      ),
      protectedPage('my/requests', 'my-request-list', '我的申请', ['EMPLOYEE'], 'my-requests', [
        '我的培训',
        '我的申请',
      ]),
      protectedPage(
        'requests/:id',
        'request-detail',
        '申请详情',
        ['EMPLOYEE', 'DEPT_MANAGER', 'HR', 'ADMIN'],
        'my-requests',
        ['申请管理', '申请详情'],
      ),
      protectedPage(
        'approvals/department',
        'department-approval',
        '主管审批',
        ['DEPT_MANAGER', 'ADMIN'],
        'department-approval',
        ['审批管理', '主管审批'],
      ),
      protectedPage('filings/hr', 'hr-filing', 'HR 备案', ['HR', 'ADMIN'], 'hr-filing', [
        '审批管理',
        'HR 备案',
      ]),
      protectedPage(
        'my/registrations',
        'my-registration-list',
        '我的报名',
        ['EMPLOYEE'],
        'my-registrations',
        ['我的培训', '我的报名'],
      ),
      protectedPage(
        'registrations/:id',
        'registration-detail',
        '报名详情',
        ['EMPLOYEE', 'HR', 'ADMIN'],
        'my-registrations',
        ['报名管理', '报名详情'],
      ),
      protectedPage(
        'operations/attendance',
        'attendance-management',
        '报名与签到',
        ['HR', 'ADMIN'],
        'attendance-management',
        ['培训运营', '报名与签到'],
      ),
      protectedPage(
        'my/certificates',
        'my-certificate-list',
        '我的证书',
        ['EMPLOYEE'],
        'my-certificates',
        ['我的培训', '我的证书'],
      ),
      protectedPage(
        'operations/certificates',
        'certificate-management',
        '证书管理',
        ['HR', 'ADMIN'],
        'certificate-management',
        ['培训运营', '证书管理'],
      ),
      protectedPage(
        'certificates/:id',
        'certificate-detail',
        '证书详情',
        ['EMPLOYEE', 'HR', 'ADMIN'],
        'my-certificates',
        ['证书管理', '证书详情'],
      ),
      protectedPage('my/ratings', 'my-rating-list', '我的评分', ['EMPLOYEE'], 'my-ratings', [
        '我的培训',
        '我的评分',
      ]),
      protectedPage(
        'operations/tests',
        'test-management',
        '测试成绩',
        ['HR', 'ADMIN'],
        'test-management',
        ['培训运营', '测试成绩'],
      ),
      protectedPage(
        'organization/blacklist',
        'blacklist-management',
        '黑名单管理',
        ['DEPT_MANAGER', 'HR', 'ADMIN'],
        'blacklist-management',
        ['组织管理', '黑名单管理'],
      ),
      protectedPage(
        'organization/employees',
        'employee-management',
        '员工管理',
        ['ADMIN'],
        'employee-management',
        ['组织管理', '员工管理'],
      ),
      protectedPage(
        'organization/budgets',
        'department-budget-management',
        '部门预算管理',
        ['HR', 'ADMIN'],
        'department-budget-management',
        ['组织管理', '部门预算管理'],
      ),
      protectedPage(
        'operations/ratings',
        'rating-management',
        '讲师评分管理',
        ['HR', 'ADMIN'],
        'rating-management',
        ['培训运营', '讲师评分管理'],
      ),
      protectedPage(
        'operations/trainers',
        'trainer-management',
        '讲师管理',
        ['HR', 'ADMIN'],
        'trainer-management',
        ['培训运营', '讲师管理'],
      ),
    ],
  },
  {
    path: '/403',
    component: () => import('@/layouts/PublicLayout.vue'),
    children: [
      {
        path: '',
        name: 'forbidden',
        component: () => import('@/views/errors/ForbiddenView.vue'),
        meta: { title: '无权访问', requiresAuth: true, roles: allRoles, priority: 'P0' },
      },
    ],
  },
  {
    path: '/components',
    name: 'component-gallery',
    component: () => import('@/views/dev/ComponentGalleryView.vue'),
    meta: { title: '公共组件展示', developmentOnly: true },
  },
  {
    path: '/api-boundary',
    name: 'api-boundary',
    component: () => import('@/views/dev/ApiBoundaryView.vue'),
    meta: { title: 'API 与 Mock 边界验收', developmentOnly: true },
  },
  {
    path: '/:pathMatch(.*)*',
    component: () => import('@/layouts/PublicLayout.vue'),
    children: [
      {
        path: '',
        name: 'not-found',
        component: () => import('@/views/errors/NotFoundView.vue'),
        meta: { title: '页面不存在', priority: 'P0' },
      },
    ],
  },
]

export const resolveScrollPosition: RouterScrollBehavior = (_to, _from, savedPosition) =>
  savedPosition ?? { top: 0 }

const router = createRouter({
  history: createWebHistory(import.meta.env.BASE_URL),
  routes,
  scrollBehavior: resolveScrollPosition,
})

export const resolveRouteAccess = (
  to: {
    name: unknown
    fullPath: string
    meta: RouteMeta
  },
  user: AppUser | null,
  sessionExpired = false,
): RouteLocationRaw | true => {
  const allowedRoles = to.meta.roles as AppRole[] | undefined

  if (to.name === 'login' && user) return { name: 'dashboard' }
  if (to.meta.requiresAuth && !user) {
    return {
      name: 'login',
      query: {
        redirect: to.fullPath,
        ...(sessionExpired ? { reason: 'session-expired' } : {}),
      },
    }
  }
  if (user && allowedRoles?.length && !hasAnyRole(user, allowedRoles)) {
    return { name: 'forbidden' }
  }

  return true
}

router.beforeEach(async (to) => {
  const authStore = useAuthStore(pinia)
  await authStore.initialize()
  return resolveRouteAccess(to, authStore.currentUser, authStore.lastSessionEvent === 'expired')
})

router.afterEach((to) => {
  document.title = to.meta.title ? `${to.meta.title}｜企业内部培训管理系统` : '企业内部培训管理系统'
})

export default router
