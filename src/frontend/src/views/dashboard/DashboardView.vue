<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'
import type { Component } from 'vue'
import { storeToRefs } from 'pinia'
import { useRouter } from 'vue-router'
import {
  AlarmClock,
  ArrowRight,
  Calendar,
  CircleCheck,
  Clock,
  DocumentChecked,
  Medal,
  Reading,
} from '@element-plus/icons-vue'

import PageHeader from '@/components/common/PageHeader.vue'
import StatCard from '@/components/common/StatCard.vue'
import { getNavigationForRoles } from '@/config/navigation'
import type { DashboardStats } from '@/domains/dashboard'
import { getDashboardService } from '@/services/dashboard'
import { useAuthStore } from '@/stores/auth'
import type { AppRole } from '@/types/navigation'

const router = useRouter()
const authStore = useAuthStore()
const { currentUser } = storeToRefs(authStore)

const roleLabels: Record<AppRole, string> = {
  EMPLOYEE: '员工',
  DEPT_MANAGER: '部门主管',
  HR: 'HR',
  ADMIN: '管理员',
}

interface RoleStat {
  label: string
  key: keyof DashboardStats
  icon: Component
  to: string
}

const roleStats: Record<AppRole, RoleStat[]> = {
  EMPLOYEE: [
    { label: '待开始课程', key: 'upcomingCourses', icon: Calendar, to: '/courses' },
    { label: '处理中申请', key: 'activeRequests', icon: DocumentChecked, to: '/my/requests' },
    { label: '有效证书', key: 'validCertificates', icon: Medal, to: '/my/certificates' },
  ],
  DEPT_MANAGER: [
    {
      label: '待审批申请',
      key: 'pendingApprovals',
      icon: DocumentChecked,
      to: '/approvals/department?status=PENDING',
    },
    {
      label: '本周已处理',
      key: 'handledThisWeek',
      icon: CircleCheck,
      to: '/approvals/department?status=DEPT_APPROVED',
    },
    {
      label: '逾期申请',
      key: 'overdueRequests',
      icon: AlarmClock,
      to: '/approvals/department?status=PENDING',
    },
  ],
  HR: [
    { label: '待备案申请', key: 'pendingFilings', icon: DocumentChecked, to: '/filings/hr' },
    { label: '待签到报名', key: 'pendingSignIn', icon: Clock, to: '/operations/attendance' },
    {
      label: '待生成证书',
      key: 'pendingCertificates',
      icon: Medal,
      to: '/operations/certificates',
    },
  ],
  ADMIN: [
    { label: '课程总数', key: 'totalCourses', icon: Reading, to: '/courses' },
    {
      label: '待审批申请',
      key: 'pendingApprovals',
      icon: DocumentChecked,
      to: '/approvals/department?status=PENDING',
    },
    {
      label: '待生成证书',
      key: 'pendingCertificates',
      icon: Medal,
      to: '/operations/certificates',
    },
  ],
}

const statsData = ref<DashboardStats | null>(null)

const stats = computed(() => {
  const template = currentUser.value ? roleStats[currentUser.value.primaryRole] : []
  return template.map((item) => ({
    ...item,
    value: statsData.value ? statsData.value[item.key] : 0,
  }))
})

onMounted(async () => {
  try {
    statsData.value = await (await getDashboardService()).getStats()
  } catch {
    statsData.value = null
  }
})
const quickLinks = computed(() => {
  if (!currentUser.value) return []
  return getNavigationForRoles(currentUser.value.roles)
    .flatMap((group) => group.items)
    .filter((item) => item.path !== '/dashboard')
    .slice(0, 4)
})
</script>

<template>
  <section v-if="currentUser" class="dashboard-view">
    <PageHeader title="控制台" description="查看与你当前角色相关的培训工作入口。" />

    <article class="dashboard-view__welcome">
      <div>
        <span class="dashboard-view__eyebrow">{{ roleLabels[currentUser.primaryRole] }}工作台</span>
        <h2>欢迎回来，{{ currentUser.displayName }}</h2>
        <p>祝你今天工作顺利，从这里快速进入你的培训任务。</p>
      </div>
      <span class="dashboard-view__identity">{{ currentUser.account }}</span>
    </article>

    <div class="dashboard-view__stats" aria-label="业务概览">
      <StatCard
        v-for="item in stats"
        :key="item.label"
        :label="item.label"
        :value="item.value"
        :icon="item.icon"
        state="link"
        :to="item.to"
      />
    </div>

    <section class="dashboard-view__section">
      <div class="dashboard-view__section-heading">
        <div>
          <h2>快捷入口</h2>
          <p>从这里进入你的常用功能。</p>
        </div>
      </div>
      <div class="dashboard-view__links">
        <button
          v-for="item in quickLinks"
          :key="item.menuKey"
          type="button"
          @click="router.push(item.path)"
        >
          <span>{{ item.label }}</span>
          <el-icon aria-hidden="true"><ArrowRight /></el-icon>
        </button>
      </div>
    </section>

    <section class="dashboard-view__section dashboard-view__section--muted">
      <div class="dashboard-view__section-heading">
        <div>
          <h2>最近动态</h2>
          <p>这里会展示与你相关的最近动态。</p>
        </div>
      </div>
      <el-empty description="暂无可展示的动态" :image-size="72" />
    </section>
  </section>
</template>

<style scoped>
.dashboard-view {
  display: grid;
  gap: var(--space-6);
}

.dashboard-view__welcome {
  display: flex;
  min-height: 96px;
  align-items: center;
  justify-content: space-between;
  gap: var(--space-8);
  padding: var(--space-5) var(--space-6);
  border-radius: var(--radius-xl);
  color: var(--text-on-primary);
  background: linear-gradient(110deg, #1e3a8a, #2563eb);
  box-shadow: var(--shadow-md);
}

.dashboard-view__eyebrow {
  font-size: var(--font-size-caption);
  letter-spacing: 0.08em;
  opacity: 0.75;
}

.dashboard-view__welcome h2 {
  margin: var(--space-1) 0 0;
  font-size: var(--font-size-title-section);
}

.dashboard-view__welcome p {
  margin: var(--space-2) 0 0;
  opacity: 0.78;
}

.dashboard-view__identity {
  padding: var(--space-2) var(--space-3);
  border: 1px solid rgb(255 255 255 / 24%);
  border-radius: 999px;
  white-space: nowrap;
}

.dashboard-view__stats {
  display: grid;
  grid-template-columns: repeat(3, minmax(0, 1fr));
  gap: var(--space-4);
}

.dashboard-view__section {
  padding: var(--space-6);
  border: var(--border-default);
  border-radius: var(--radius-lg);
  background: var(--color-surface);
  box-shadow: var(--shadow-sm);
}

.dashboard-view__section--muted {
  display: grid;
  min-height: 240px;
  align-content: start;
  box-shadow: none;
}

.dashboard-view__section-heading h2 {
  margin: 0;
  font-size: var(--font-size-title-component);
}

.dashboard-view__section-heading p {
  margin: var(--space-1) 0 0;
  color: var(--text-secondary);
}

.dashboard-view__links {
  display: grid;
  grid-template-columns: repeat(2, minmax(0, 1fr));
  gap: var(--space-3);
  margin-top: var(--space-5);
}

.dashboard-view__links button {
  display: flex;
  min-height: 48px;
  align-items: center;
  justify-content: space-between;
  padding: 0 var(--space-4);
  border: var(--border-default);
  border-radius: var(--radius-md);
  color: var(--text-primary);
  background: var(--color-surface);
  cursor: pointer;
}

.dashboard-view__links button:hover {
  border-color: var(--color-primary);
  color: var(--color-primary);
  background: var(--color-primary-soft);
}

@media (width <= 700px) {
  .dashboard-view__stats {
    grid-template-columns: 1fr;
  }
  .dashboard-view__links {
    grid-template-columns: 1fr;
  }
}
</style>
