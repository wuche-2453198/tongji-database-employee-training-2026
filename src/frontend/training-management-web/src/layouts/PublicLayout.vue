<script setup lang="ts">
import { computed } from 'vue'
import { storeToRefs } from 'pinia'
import { useRouter } from 'vue-router'
import { useRoute } from 'vue-router'

import { useAuthStore } from '@/stores/auth'

import AppTopbar from './components/AppTopbar.vue'

const router = useRouter()
const route = useRoute()
const authStore = useAuthStore()
const { currentUser } = storeToRefs(authStore)
const visibleUser = computed(() => (route.name === 'forbidden' ? currentUser.value : null))

const logout = async () => {
  await authStore.logout()
  await router.replace({ name: 'login' })
}
</script>

<template>
  <div class="public-layout">
    <AppTopbar :user="visibleUser" @logout="logout" />
    <main class="public-layout__content">
      <RouterView />
    </main>
  </div>
</template>

<style scoped>
.public-layout {
  min-height: 100vh;
}

.public-layout__content {
  display: grid;
  min-height: calc(100vh - var(--layout-topbar-height));
  place-items: center;
  padding: var(--space-8);
}
</style>
