<script setup lang="ts">
import { useAuthStore } from '@/stores/auth'

const auth = useAuthStore()
</script>

<template>
  <router-view v-if="auth.initialized" />
  <div v-else class="app-loading">
    <el-icon :size="32" class="loading-icon"><component :is="'Loading'" /></el-icon>
    <p>加载中...</p>
  </div>
</template>

<style>
.app-loading {
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  height: 100vh;
  gap: 12px;
  color: var(--color-text-secondary);
}
</style>
