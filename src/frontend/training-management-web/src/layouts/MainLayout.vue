<script setup lang="ts">
import { computed, ref } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { storeToRefs } from 'pinia'

import { useAuthStore } from '@/stores/auth'

import AppTopbar from './components/AppTopbar.vue'
import BreadcrumbBar from './components/BreadcrumbBar.vue'
import SidebarNav from './components/SidebarNav.vue'

const route = useRoute()
const router = useRouter()
const collapsed = ref(false)
const authStore = useAuthStore()
const { currentUser } = storeToRefs(authStore)
const breadcrumbs = computed<string[]>(() =>
  Array.isArray(route.meta.breadcrumb) ? (route.meta.breadcrumb as string[]) : [],
)

const logout = async () => {
  await authStore.logout()
  await router.replace({ name: 'login' })
}
</script>

<template>
  <div v-if="currentUser" class="main-layout">
    <AppTopbar
      :user="currentUser"
      :collapsed="collapsed"
      show-navigation-toggle
      @toggle-navigation="collapsed = !collapsed"
      @logout="logout"
    />
    <div class="main-layout__body">
      <SidebarNav :roles="currentUser.roles" :collapsed="collapsed" />
      <div class="main-layout__workspace">
        <BreadcrumbBar :items="breadcrumbs" />
        <main class="main-layout__content">
          <RouterView />
        </main>
      </div>
    </div>
  </div>
</template>

<style scoped>
.main-layout {
  min-height: 100vh;
}

.main-layout__body {
  display: flex;
}

.main-layout__workspace {
  min-width: 0;
  flex: 1;
}

.main-layout__content {
  width: 100%;
  max-width: calc(var(--layout-content-max-width) + var(--layout-page-padding) * 2);
  min-height: calc(100vh - var(--layout-topbar-height) - 40px);
  padding: var(--layout-page-padding);
  margin: 0 auto;
}

@media (width <= 1100px) {
  .main-layout__content {
    padding: var(--layout-page-padding-compact);
  }
}
</style>
