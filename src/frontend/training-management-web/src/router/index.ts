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

const allRoles: AppRole[] = ['EMPLOYEE', 'MANAGER', 'HR', 'ADMIN']

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
  component: () => import('@/views/placeholders/BusinessPlaceholderView.vue'),
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
        ['EMPLOYEE', 'MANAGER', 'HR'],
        'my-requests',
        ['申请管理', '申请详情'],
      ),
      protectedPage(
        'approvals/department',
        'department-approval',
        '主管审批',
        ['MANAGER'],
        'department-approval',
        ['审批管理', '主管审批'],
      ),
      protectedPage('filings/hr', 'hr-filing', 'HR 备案', ['HR'], 'hr-filing', [
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
        ['HR'],
        'certificate-management',
        ['培训运营', '证书管理'],
      ),
      protectedPage(
        'certificates/:id',
        'certificate-detail',
        '证书详情',
        ['EMPLOYEE', 'HR'],
        'my-certificates',
        ['证书管理', '证书详情'],
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
