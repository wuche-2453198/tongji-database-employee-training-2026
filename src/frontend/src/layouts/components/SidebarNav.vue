<script setup lang="ts">
import { computed } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import {
  Collection,
  Document,
  HomeFilled,
  Operation,
  Stamp,
  Tickets,
  TrendCharts,
} from '@element-plus/icons-vue'

import { getActiveMenuKey, getNavigationForRoles } from '@/config/navigation'
import type { AppRole } from '@/types/navigation'

const props = defineProps<{
  roles: AppRole[]
  collapsed: boolean
}>()

const route = useRoute()
const router = useRouter()
const groups = computed(() => getNavigationForRoles(props.roles))
const activeMenuKey = computed(() =>
  getActiveMenuKey(
    route.name,
    props.roles,
    typeof route.meta.menuKey === 'string' ? route.meta.menuKey : '',
  ),
)

const groupIcons = {
  workspace: HomeFilled,
  'my-training': Tickets,
  'approval-management': Stamp,
  'training-operations': Operation,
}

const itemIcons = {
  dashboard: HomeFilled,
  courses: Collection,
  'my-requests': Document,
  'my-registrations': Tickets,
  'my-certificates': Stamp,
  'my-test-scores': TrendCharts,
  'department-approval': Stamp,
  'hr-filing': Document,
  'attendance-management': Operation,
  'certificate-management': Stamp,
}

const navigate = (menuKey: string) => {
  const item = groups.value
    .flatMap((group) => group.items)
    .find((entry) => entry.menuKey === menuKey)
  if (item) void router.push(item.path)
}
</script>

<template>
  <aside class="sidebar-nav" :class="{ 'sidebar-nav--collapsed': collapsed }">
    <el-menu
      :default-active="String(activeMenuKey)"
      :collapse="collapsed"
      :collapse-transition="false"
      unique-opened
      @select="navigate"
    >
      <template v-for="group in groups" :key="group.key">
        <template v-if="group.key === 'workspace'">
          <el-menu-item v-for="item in group.items" :key="item.menuKey" :index="item.menuKey">
            <el-icon><component :is="itemIcons[item.menuKey as keyof typeof itemIcons]" /></el-icon>
            <template #title>{{ item.label }}</template>
          </el-menu-item>
        </template>

        <el-sub-menu v-else :index="group.key">
          <template #title>
            <el-icon><component :is="groupIcons[group.key as keyof typeof groupIcons]" /></el-icon>
            <span>{{ group.label }}</span>
          </template>
          <el-menu-item v-for="item in group.items" :key="item.menuKey" :index="item.menuKey">
            <el-icon><component :is="itemIcons[item.menuKey as keyof typeof itemIcons]" /></el-icon>
            <template #title>{{ item.label }}</template>
          </el-menu-item>
        </el-sub-menu>
      </template>
    </el-menu>
  </aside>
</template>

<style scoped>
.sidebar-nav {
  width: var(--layout-sidebar-width);
  min-height: calc(100vh - var(--layout-topbar-height));
  border-right: var(--border-default);
  background: var(--color-surface);
  transition: width 160ms ease;
}

.sidebar-nav--collapsed {
  width: var(--layout-sidebar-collapsed-width);
}

.sidebar-nav :deep(.el-menu) {
  border-right: 0;
}

.sidebar-nav :deep(.el-menu-item),
.sidebar-nav :deep(.el-sub-menu__title) {
  height: 48px;
}

.sidebar-nav :deep(.el-menu-item.is-active) {
  border-right: 3px solid var(--color-primary);
  background: var(--color-primary-soft);
  font-weight: 600;
}
</style>
