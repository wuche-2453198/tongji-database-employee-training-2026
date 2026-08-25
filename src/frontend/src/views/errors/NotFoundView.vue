<script setup lang="ts">
import { useRouter } from 'vue-router'

import PageState from '@/components/common/PageState.vue'

const router = useRouter()
</script>

<template>
  <section class="error-view">
    <div class="error-view__code">404</div>
    <PageState
      state="not-found"
      title="页面不存在"
      description="你访问的页面不存在或已被调整，请检查地址后重试。"
      primary-label="返回首页"
      secondary-label="返回上一页"
      @primary="router.replace('/')"
      @secondary="router.back()"
    />
  </section>
</template>

<style scoped>
.error-view {
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
</style>
