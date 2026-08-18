<script setup lang="ts">
import { useRouter } from 'vue-router'
import { storeToRefs } from 'pinia'

import PageState from '@/components/common/PageState.vue'
import { useAuthStore } from '@/stores/auth'

const router = useRouter()
const authStore = useAuthStore()
const { currentUser } = storeToRefs(authStore)

const roleLabels = {
  EMPLOYEE: '员工',
  MANAGER: '部门主管',
  HR: 'HR',
  ADMIN: '管理员',
}
</script>

<template>
  <section class="error-view">
    <div class="error-view__code">403</div>
    <PageState
      state="forbidden"
      title="当前角色无权访问此页面"
      description="权限范围以当前登录角色为准。如需访问，请联系管理员确认账号权限。"
      primary-label="返回控制台"
      secondary-label="返回上一页"
      @primary="router.replace('/dashboard')"
      @secondary="router.back()"
    />
    <p v-if="currentUser" class="error-view__context">
      当前账号：{{ currentUser.displayName }} · 当前角色：{{ roleLabels[currentUser.primaryRole] }}
    </p>
  </section>
</template>

<style scoped>
.error-view {
  position: relative;
  width: min(100%, 720px);
  text-align: center;
}

.error-view__code {
  margin-bottom: -28px;
  color: var(--color-primary-soft);
  font-size: 112px;
  font-weight: 800;
  line-height: 1;
}

.error-view :deep(.page-state) {
  position: relative;
  border: 0;
  box-shadow: var(--shadow-md);
}

.error-view__context {
  margin: var(--space-4) 0 0;
  color: var(--text-secondary);
  font-size: var(--font-size-helper);
}
</style>
