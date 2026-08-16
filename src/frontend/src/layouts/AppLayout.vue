<script setup lang="ts">
import { ref } from 'vue'
import { RouterView } from 'vue-router'
import AppSidebar from '@/components/common/AppSidebar.vue'
import AppHeader from '@/components/common/AppHeader.vue'

const sidebarCollapsed = ref(false)

function toggleSidebar() {
  sidebarCollapsed.value = !sidebarCollapsed.value
}
</script>

<template>
  <div class="app-layout">
    <AppSidebar v-model:collapsed="sidebarCollapsed" />
    <AppHeader
      v-model:collapsed="sidebarCollapsed"
      @toggle-collapse="toggleSidebar"
    />
    <main
      class="main-content"
      :style="{ marginLeft: sidebarCollapsed ? 'var(--sidebar-collapsed-width)' : 'var(--sidebar-width)' }"
    >
      <RouterView />
    </main>
  </div>
</template>

<style lang="scss" scoped>
.app-layout {
  min-height: 100vh;
  display: flex;
  flex-direction: column;
}

.main-content {
  margin-top: var(--header-height);
  margin-left: var(--sidebar-width);
  padding: 24px;
  flex: 1;
  transition: margin-left 0.2s ease;
  min-width: 0;
}
</style>
