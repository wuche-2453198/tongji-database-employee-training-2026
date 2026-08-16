<script setup lang="ts">
import { ref, computed } from 'vue'
import { useRouter, useRoute } from 'vue-router'
import { useAuthStore } from '@/stores/auth'
import { MENU_ITEMS, type MenuItem } from '@/utils/constants'

const router = useRouter()
const route = useRoute()
const auth = useAuthStore()

const isCollapsed = defineModel<boolean>('collapsed', { default: false })

/** 根据角色过滤可见菜单，提取 children 为 _children */
interface VisibleMenuItem extends MenuItem {
  _children: MenuItem[]
}
const visibleMenus = computed<VisibleMenuItem[]>(() => {
  const result: VisibleMenuItem[] = []
  for (const item of MENU_ITEMS) {
    // 父菜单角色检查
    if (item.roles && item.roles.length > 0 && !auth.hasRole(item.roles)) continue
    if (!item.children) {
      result.push({ ...item, _children: [] })
      continue
    }
    const visibleChildren = item.children.filter(
      (child) => !child.roles || child.roles.length === 0 || auth.hasRole(child.roles),
    )
    if (visibleChildren.length === 0) continue
    result.push({ ...item, _children: visibleChildren })
  }
  return result
})

// 当前路由所属的父级菜单路径（用于自动展开），无父级时返回 null
function parentPathOf(current: string): string | null {
  for (const item of MENU_ITEMS) {
    if (item.children?.some((child) => child.path === current)) {
      return item.path
    }
  }
  return null
}

// 用户手动展开的菜单（会话内保持）；当前路由父级始终并入，确保自动展开
const manualOpeneds = ref<string[]>([])

const defaultOpeneds = computed<string[]>(() => {
  const parent = parentPathOf(route.path)
  const set = new Set(manualOpeneds.value)
  if (parent) set.add(parent)
  return [...set]
})

// 路由变化时重新应用 default-openeds（仅重置展开态，激活态由 default-active 响应式处理）
const menuKey = computed(() => route.path)

function onMenuOpen(index: string) {
  if (!manualOpeneds.value.includes(index)) {
    manualOpeneds.value.push(index)
  }
}

function onMenuClose(index: string) {
  manualOpeneds.value = manualOpeneds.value.filter((p) => p !== index)
}
</script>

<template>
  <aside
    class="app-sidebar"
    :class="{ 'is-collapsed': isCollapsed }"
  >
    <div class="sidebar-logo" @click="router.push('/dashboard')">
      <el-icon :size="24"><component :is="'Reading'" /></el-icon>
      <transition name="fade">
        <span v-show="!isCollapsed" class="logo-text">培训管理系统</span>
      </transition>
    </div>

    <el-menu
      :key="menuKey"
      :default-active="route.path"
      :default-openeds="defaultOpeneds"
      :collapse="isCollapsed"
      :unique-opened="true"
      background-color="transparent"
      router
      @open="onMenuOpen"
      @close="onMenuClose"
    >
      <template v-for="menu in visibleMenus" :key="menu.path">
        <el-sub-menu v-if="menu._children.length > 0" :index="menu.path">
          <template #title>
            <el-icon v-if="menu.icon" :size="18">
              <component :is="menu.icon" />
            </el-icon>
            <span>{{ menu.title }}</span>
          </template>
          <el-menu-item
            v-for="child in menu._children"
            :key="child.path"
            :index="child.path"
          >
            <el-icon v-if="child.icon" :size="16">
              <component :is="child.icon" />
            </el-icon>
            <span>{{ child.title }}</span>
          </el-menu-item>
        </el-sub-menu>

        <el-menu-item v-else :index="menu.path">
          <el-icon v-if="menu.icon" :size="18">
            <component :is="menu.icon" />
          </el-icon>
          <template #title>{{ menu.title }}</template>
        </el-menu-item>
      </template>
    </el-menu>
  </aside>
</template>

<style lang="scss" scoped>
.app-sidebar {
  position: fixed;
  left: 0;
  top: 0;
  bottom: 0;
  width: var(--sidebar-width);
  background: var(--color-bg-card);
  border-right: 1px solid var(--color-border);
  display: flex;
  flex-direction: column;
  transition: width 0.2s ease;
  z-index: 100;
  overflow: hidden;

  &.is-collapsed {
    width: var(--sidebar-collapsed-width);
  }
}

.sidebar-logo {
  display: flex;
  align-items: center;
  gap: 10px;
  height: var(--header-height);
  padding: 0 20px;
  color: var(--color-primary);
  cursor: pointer;
  flex-shrink: 0;
  border-bottom: 1px solid var(--color-border);

  .logo-text {
    font-size: 15px;
    font-weight: 600;
    white-space: nowrap;
  }
}

:deep(.el-menu) {
  border-right: none;
  flex: 1;
  overflow-y: auto;
  overflow-x: hidden;
  padding: 8px 0;

  // 一级菜单项高度
  .el-menu-item,
  .el-sub-menu__title {
    height: 48px;
    line-height: 48px;
  }

  // 一级菜单项
  .el-menu-item {
    position: relative;
    margin: 2px 8px;
    border-radius: var(--radius-md);
    font-size: 13px;
    color: var(--color-text-regular);

    &:hover {
      background: var(--color-surface-subtle);
      color: var(--color-text-primary);
    }

    &.is-active {
      background: var(--color-primary-bg);
      color: var(--color-primary);
      font-weight: 500;

      // 左侧 3px 主色指示条
      &::before {
        content: '';
        position: absolute;
        left: 0;
        top: 9px;
        bottom: 9px;
        width: 3px;
        border-radius: 0 2px 2px 0;
        background: var(--color-primary);
      }
    }
  }

  // 子菜单标题
  .el-sub-menu__title {
    margin: 2px 8px;
    border-radius: var(--radius-md);
    font-size: 13px;
    color: var(--color-text-regular);

    &:hover {
      background: var(--color-surface-subtle);
      color: var(--color-text-primary);
    }

    .el-icon {
      color: inherit;
    }
  }

  // 子菜单展开状态
  .el-sub-menu.is-opened > .el-sub-menu__title {
    font-weight: 500;
  }

  // 子菜单内的菜单项缩进（二级菜单项高度 44px）
  .el-menu--inline {
    .el-menu-item {
      height: 44px;
      line-height: 44px;
      padding-left: 56px !important;
      font-size: 13px;
    }
  }
}

// 折叠状态下的弹出菜单
:deep(.el-menu--collapse) {
  .el-sub-menu.is-opened > .el-sub-menu__title {
    background: var(--color-primary-bg);
  }
}

.fade-enter-active,
.fade-leave-active {
  transition: opacity 0.15s ease;
}
.fade-enter-from,
.fade-leave-to {
  opacity: 0;
}
</style>
