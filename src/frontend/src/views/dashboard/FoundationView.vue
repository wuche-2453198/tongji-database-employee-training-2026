<script setup lang="ts">
import { appConfig } from '@/config/env'
</script>

<template>
  <main class="foundation-page">
    <section class="foundation-card" aria-labelledby="foundation-title">
      <p class="foundation-eyebrow">FE-BASE · M1</p>
      <h1 id="foundation-title">企业内部培训管理系统</h1>
      <p class="foundation-summary">前端基础工程已就绪，业务页面将在后续里程碑中实现。</p>

      <dl class="foundation-status">
        <div>
          <dt>运行环境</dt>
          <dd>{{ appConfig.environment }}</dd>
        </div>
        <div>
          <dt>数据模式</dt>
          <dd>{{ appConfig.useMock ? 'Mock' : 'HTTP' }}</dd>
        </div>
        <div>
          <dt>请求超时</dt>
          <dd>{{ appConfig.requestTimeoutMs }} ms</dd>
        </div>
      </dl>
      <RouterLink class="foundation-link" to="/components">查看 M2 公共组件展示</RouterLink>
    </section>
  </main>
</template>

<style scoped>
.foundation-page {
  display: grid;
  min-height: 100vh;
  place-items: center;
  padding: var(--space-6);
}

.foundation-card {
  width: min(680px, 100%);
  padding: var(--space-10);
  border: var(--border-default);
  border-radius: var(--radius-xl);
  background: var(--color-surface);
  box-shadow: var(--shadow-md);
}

.foundation-eyebrow {
  margin: 0 0 var(--space-3);
  color: var(--color-primary);
  font-weight: 600;
  letter-spacing: 0.08em;
}

.foundation-card h1 {
  margin: 0;
  font-size: var(--font-size-title-page);
  line-height: var(--line-height-title-page);
}

.foundation-summary {
  margin: var(--space-4) 0 var(--space-8);
  color: var(--text-secondary);
  line-height: var(--line-height-body);
}

.foundation-status {
  display: grid;
  grid-template-columns: repeat(3, minmax(0, 1fr));
  gap: var(--space-3);
  margin: 0;
}

.foundation-status div {
  padding: var(--space-4);
  border-radius: var(--radius-lg);
  background: var(--color-surface-subtle);
}

.foundation-status dt {
  color: var(--text-secondary);
  font-size: var(--font-size-helper);
}

.foundation-status dd {
  margin: var(--space-2) 0 0;
  font-weight: 600;
}

.foundation-link {
  display: inline-block;
  margin-top: var(--space-6);
  font-weight: 500;
}
</style>
