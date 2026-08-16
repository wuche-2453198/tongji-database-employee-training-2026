<script setup lang="ts">
import { computed, ref } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { useAuthStore } from '@/stores/auth'
import { ROLE_LABELS } from '@/types/enums'
import type { RoleCode } from '@/types/enums'

const auth = useAuthStore()
const router = useRouter()
const route = useRoute()

const isCollapsed = defineModel<boolean>('collapsed', { default: false })

defineEmits<{
  'toggle-collapse': []
}>()

const primaryRole = computed<RoleCode | null>(() => {
  if (auth.roles.length === 0) return null
  return auth.roles[0]
})

const roleLabel = computed(() => {
  if (!primaryRole.value) return ''
  return ROLE_LABELS[primaryRole.value] ?? primaryRole.value
})

const showLogoutConfirm = ref(false)

interface Crumb {
  title: string
  path?: string
}

/** 面包屑：课程相关页面补齐「培训中心 / 课程大厅」层级 */
const breadcrumbs = computed<Crumb[]>(() => {
  const crumbs: Crumb[] = [{ title: '首页', path: '/dashboard' }]
  if (route.path.startsWith('/courses')) {
    crumbs.push({ title: '培训中心' })
    crumbs.push({ title: '课程大厅', path: '/courses' })
    if (route.name === 'CourseDetail') {
      crumbs.push({ title: '课程详情' })
    }
  } else if (route.meta.title && route.meta.title !== '工作台') {
    crumbs.push({ title: route.meta.title as string })
  }
  return crumbs
})

function handleLogout() {
  showLogoutConfirm.value = true
}

function confirmLogout() {
  showLogoutConfirm.value = false
  auth.logout()
  router.push('/login')
}
</script>

<template>
  <header class="app-header">
    <div class="header-left">
      <el-button
        text
        @click="$emit('toggle-collapse')"
        class="collapse-btn"
        :aria-label="isCollapsed ? '展开侧边栏' : '折叠侧边栏'"
      >
        <el-icon :size="20">
          <component :is="isCollapsed ? 'Expand' : 'Fold'" />
        </el-icon>
      </el-button>

      <el-breadcrumb separator="/" class="header-breadcrumb">
        <el-breadcrumb-item
          v-for="(crumb, idx) in breadcrumbs"
          :key="idx"
          :to="crumb.path ? { path: crumb.path } : undefined"
        >
          {{ crumb.title }}
        </el-breadcrumb-item>
      </el-breadcrumb>
    </div>

    <div class="header-right">
      <el-tag v-if="roleLabel" size="small" effect="plain" type="info" class="role-tag">
        {{ roleLabel }}
      </el-tag>

      <el-dropdown trigger="click" placement="bottom-end">
        <div class="user-trigger">
          <el-avatar :size="32" :icon="'UserFilled'" class="user-avatar" />
          <span class="user-name">{{ auth.user?.empName || '用户' }}</span>
          <el-icon :size="12" class="user-arrow"><component :is="'ArrowDown'" /></el-icon>
        </div>

        <template #dropdown>
          <el-dropdown-menu>
            <el-dropdown-item disabled>
              <div class="dropdown-user-info">
                <div>{{ auth.user?.empName }}</div>
                <div class="dropdown-user-role">{{ roleLabel }}</div>
              </div>
            </el-dropdown-item>
            <el-dropdown-item disabled>
              <span class="dropdown-dept">{{ auth.user?.deptName || '-' }}</span>
            </el-dropdown-item>
            <el-dropdown-item divided>
              <span @click="handleLogout" class="dropdown-logout">退出登录</span>
            </el-dropdown-item>
          </el-dropdown-menu>
        </template>
      </el-dropdown>
    </div>

    <!-- 退出确认 -->
    <el-dialog
      v-model="showLogoutConfirm"
      title="退出登录"
      width="360px"
      :close-on-click-modal="false"
    >
      <p style="text-align: center; color: var(--color-text-secondary);">确定要退出登录吗？</p>
      <template #footer>
        <el-button @click="showLogoutConfirm = false">取消</el-button>
        <el-button type="primary" @click="confirmLogout">确定退出</el-button>
      </template>
    </el-dialog>
  </header>
</template>

<style lang="scss" scoped>
.app-header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  height: var(--header-height);
  padding: 0 24px;
  background: var(--color-bg-card);
  border-bottom: 1px solid var(--color-border);
  flex-shrink: 0;
  position: fixed;
  top: 0;
  right: 0;
  left: 0;
  z-index: 99;
}

.header-left {
  display: flex;
  align-items: center;
  gap: 16px;
  min-width: 0;
}

.collapse-btn {
  padding: 4px;
  color: var(--color-text-secondary);
  flex-shrink: 0;

  &:hover {
    color: var(--color-primary);
  }
}

.header-breadcrumb {
  // 窄屏隐藏面包屑
  @media (max-width: 768px) {
    display: none;
  }
}

.header-right {
  display: flex;
  align-items: center;
  gap: 12px;
  flex-shrink: 0;
}

.role-tag {
  @media (max-width: 768px) {
    display: none;
  }
}

.user-trigger {
  display: flex;
  align-items: center;
  gap: 8px;
  cursor: pointer;
  padding: 4px 8px;
  border-radius: var(--radius-md);
  transition: background 0.15s;

  &:hover {
    background: var(--color-surface-subtle);
  }
}

.user-avatar {
  background: var(--color-primary-bg);
  color: var(--color-primary);
  flex-shrink: 0;
}

.user-name {
  font-size: 13px;
  color: var(--color-text-regular);
  max-width: 120px;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;

  @media (max-width: 640px) {
    display: none;
  }
}

.user-arrow {
  color: var(--color-text-placeholder);
  flex-shrink: 0;
}

.dropdown-user-info {
  line-height: 1.5;

  .dropdown-user-role {
    font-size: 12px;
    color: var(--color-text-placeholder);
  }
}

.dropdown-dept {
  font-size: 12px;
  color: var(--color-text-secondary);
}

.dropdown-logout {
  color: var(--color-text-secondary);
  cursor: pointer;
}
</style>
