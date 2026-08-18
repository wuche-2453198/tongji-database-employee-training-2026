<script setup lang="ts">
import { ArrowDown, Fold, Expand } from '@element-plus/icons-vue'

import type { AppUser } from '@/types/navigation'

defineProps<{
  user?: AppUser | null
  collapsed?: boolean
  showNavigationToggle?: boolean
}>()

const emit = defineEmits<{
  toggleNavigation: []
  logout: []
}>()

const roleLabels = {
  EMPLOYEE: '员工',
  MANAGER: '部门主管',
  HR: 'HR',
  ADMIN: '管理员',
}
</script>

<template>
  <header class="app-topbar">
    <div class="app-topbar__brand">
      <button
        v-if="showNavigationToggle"
        class="app-topbar__toggle"
        type="button"
        :aria-label="collapsed ? '展开侧边导航' : '收起侧边导航'"
        @click="emit('toggleNavigation')"
      >
        <el-icon :size="20" aria-hidden="true">
          <Expand v-if="collapsed" />
          <Fold v-else />
        </el-icon>
      </button>
      <RouterLink class="app-topbar__logo" to="/dashboard" aria-label="企业内部培训管理系统首页">
        <span class="app-topbar__mark">T</span>
        <span>企业内部培训管理系统</span>
      </RouterLink>
    </div>

    <el-dropdown v-if="user" trigger="click" @command="emit('logout')">
      <button class="app-topbar__account" type="button">
        <span class="app-topbar__role">
          {{ roleLabels[user.primaryRole]
          }}<template v-if="user.roles.length > 1">等{{ user.roles.length }}角色</template>
        </span>
        <span>{{ user.displayName }}</span>
        <el-icon aria-hidden="true"><ArrowDown /></el-icon>
      </button>
      <template #dropdown>
        <el-dropdown-menu>
          <el-dropdown-item command="logout">退出登录</el-dropdown-item>
        </el-dropdown-menu>
      </template>
    </el-dropdown>
  </header>
</template>

<style scoped>
.app-topbar {
  position: relative;
  z-index: 20;
  display: flex;
  height: var(--layout-topbar-height);
  align-items: center;
  justify-content: space-between;
  padding: 0 var(--space-6);
  border-bottom: var(--border-default);
  background: var(--color-surface);
  box-shadow: var(--shadow-sm);
}

.app-topbar__brand,
.app-topbar__logo,
.app-topbar__account {
  display: flex;
  align-items: center;
}

.app-topbar__brand {
  gap: var(--space-3);
}

.app-topbar__logo {
  gap: var(--space-3);
  color: var(--text-primary);
  font-size: var(--font-size-title-component);
  font-weight: 600;
  text-decoration: none;
}

.app-topbar__mark {
  display: grid;
  width: 30px;
  height: 30px;
  place-items: center;
  border-radius: var(--radius-md);
  color: var(--text-on-primary);
  background: var(--color-primary);
  font-weight: 700;
}

.app-topbar__toggle,
.app-topbar__account {
  border: 0;
  color: var(--text-secondary);
  background: transparent;
  cursor: pointer;
}

.app-topbar__toggle {
  display: grid;
  width: 32px;
  height: 32px;
  place-items: center;
  border-radius: var(--radius-md);
}

.app-topbar__toggle:hover,
.app-topbar__account:hover {
  color: var(--text-primary);
  background: var(--color-surface-subtle);
}

.app-topbar__account {
  gap: var(--space-2);
  min-height: 36px;
  padding: 0 var(--space-3);
  border-radius: var(--radius-md);
}

.app-topbar__role {
  padding: 2px var(--space-2);
  border-radius: 999px;
  color: var(--color-info-text);
  background: var(--color-info-soft);
  font-size: var(--font-size-caption);
}
</style>
