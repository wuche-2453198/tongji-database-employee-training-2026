<script setup lang="ts">
import { computed } from 'vue'
import { storeToRefs } from 'pinia'
import { useRoute, useRouter } from 'vue-router'

import PageHeader from '@/components/common/PageHeader.vue'
import { useAuthStore } from '@/stores/auth'

const route = useRoute()
const router = useRouter()
const authStore = useAuthStore()
const { currentUser } = storeToRefs(authStore)
const title = computed(() => route.meta.title ?? '业务页面')
const resourceId = computed(() => (typeof route.params.id === 'string' ? route.params.id : ''))
const isDetail = computed(() => Boolean(resourceId.value))

const goBack = () => {
  const name = typeof route.name === 'string' ? route.name : ''
  const roles = currentUser.value?.roles ?? []
  const defaultBackRoutes: Record<string, string> = {
    'course-detail': '/courses',
    'request-detail': roles.includes('EMPLOYEE')
      ? '/my/requests'
      : roles.includes('MANAGER')
        ? '/approvals/department'
        : '/filings/hr',
    'registration-detail': roles.includes('EMPLOYEE')
      ? '/my/registrations'
      : '/operations/attendance',
    'certificate-detail': roles.includes('EMPLOYEE')
      ? '/my/certificates'
      : '/operations/certificates',
  }
  void router.push(defaultBackRoutes[name] ?? '/dashboard')
}
</script>

<template>
  <section class="business-placeholder">
    <PageHeader
      :title="String(title)"
      :description="
        isDetail ? `已从当前 URL 恢复资源编号 ${resourceId}。` : '路由、布局与访问边界已经就绪。'
      "
      :context="isDetail ? 'detail' : 'list'"
      @back="goBack"
    />
    <div class="business-placeholder__panel">
      <span class="business-placeholder__phase">M3 ROUTE READY</span>
      <h2>{{ title }}尚未实现业务功能</h2>
      <p>
        当前阶段只交付页面入口、角色守卫、刷新恢复和明确的占位状态；查询、表单与业务操作将在对应业务里程碑实现。
      </p>
      <dl>
        <div>
          <dt>当前路由</dt>
          <dd>{{ route.path }}</dd>
        </div>
        <div v-if="resourceId">
          <dt>资源编号</dt>
          <dd>{{ resourceId }}</dd>
        </div>
        <div>
          <dt>优先级</dt>
          <dd>{{ route.meta.priority ?? '待确认' }}</dd>
        </div>
      </dl>
    </div>
  </section>
</template>

<style scoped>
.business-placeholder {
  display: grid;
  gap: var(--space-6);
}

.business-placeholder__panel {
  min-height: 360px;
  padding: var(--space-10);
  border: 1px dashed var(--color-border-strong);
  border-radius: var(--radius-lg);
  background: var(--color-surface);
}

.business-placeholder__phase {
  color: var(--color-primary);
  font-size: var(--font-size-caption);
  font-weight: 700;
  letter-spacing: 0.12em;
}

.business-placeholder h2 {
  margin: var(--space-3) 0 var(--space-2);
  font-size: var(--font-size-title-section);
}

.business-placeholder p {
  max-width: 680px;
  margin: 0;
  color: var(--text-secondary);
  line-height: var(--line-height-body);
}

.business-placeholder dl {
  display: grid;
  max-width: 680px;
  gap: 0;
  margin: var(--space-8) 0 0;
  border-top: var(--border-default);
}

.business-placeholder dl div {
  display: grid;
  grid-template-columns: 120px 1fr;
  padding: var(--space-3) 0;
  border-bottom: var(--border-default);
}

.business-placeholder dt {
  color: var(--text-secondary);
}

.business-placeholder dd {
  margin: 0;
  font-family: ui-monospace, SFMono-Regular, Consolas, monospace;
  overflow-wrap: anywhere;
}
</style>
