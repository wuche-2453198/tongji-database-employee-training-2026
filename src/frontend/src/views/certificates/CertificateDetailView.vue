<script setup lang="ts">
import { onMounted, ref } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import AppButton from '@/components/common/AppButton.vue'
import AppDescriptions from '@/components/common/AppDescriptions.vue'
import PageHeader from '@/components/common/PageHeader.vue'
import PageState from '@/components/common/PageState.vue'
import StatusTag from '@/components/common/StatusTag.vue'
import { getCertificateService } from '@/services/certificate'
import { isServiceError, type UiError } from '@/types/api'
import {
  certificateStatusLabel as label,
  certificateStatusSemantic as semantic,
  type Certificate,
} from '@/domains/certificate'
const route = useRoute()
const router = useRouter()
const certificate = ref<Certificate | null>(null)
const loading = ref(false)
const error = ref<UiError | null>(null)
function format(value: string | null) {
  if (!value) return '—'
  const date = new Date(value)
  return Number.isNaN(date.getTime())
    ? '—'
    : new Intl.DateTimeFormat('zh-CN', { dateStyle: 'medium', timeStyle: 'short' }).format(date)
}
function formatExpiry(value: string | null) {
  return value ? format(value) : '长期有效'
}
async function load() {
  loading.value = true
  error.value = null
  try {
    certificate.value = await (await getCertificateService()).getById(String(route.params.id ?? ''))
  } catch (caught) {
    certificate.value = null
    error.value = isServiceError(caught)
      ? caught.ui
      : {
          kind: 'unknown',
          code: 'UNKNOWN',
          message: '证书详情加载失败。',
          fieldErrors: [],
          retryable: false,
          resultUnknown: false,
        }
  } finally {
    loading.value = false
  }
}
onMounted(load)
</script>
<template>
  <section class="certificate-detail">
    <PageHeader title="证书详情" context="detail" @back="router.back"
      ><template #action
        ><AppButton label="刷新状态" :loading="loading" @click="load" /></template></PageHeader
    ><PageState v-if="loading" state="loading" /><PageState
      v-else-if="error?.kind === 'forbidden'"
      state="forbidden"
      :description="error.message"
    /><PageState
      v-else-if="error?.kind === 'not-found'"
      state="not-found"
      :description="error.message"
    /><PageState
      v-else-if="error"
      state="error"
      :description="error.message"
      primary-label="重新加载"
      @primary="load"
    /><template v-else-if="certificate"
      ><section class="certificate-detail__hero">
        <div>
          <p>培训证书</p>
          <h2>{{ certificate.courseName }}</h2>
          <span>{{ certificate.certificateNo }}</span>
        </div>
        <StatusTag
          :label="label(certificate.displayStatus)"
          :semantic="semantic(certificate.displayStatus)"
        />
      </section>
      <section class="certificate-detail__panel">
        <h2>证书信息</h2>
        <AppDescriptions
          :items="[
            { label: '证书编号', value: certificate.certificateNo },
            { label: '课程名称', value: certificate.courseName },
            { label: '持证人', value: certificate.employeeName },
            { label: '发证时间', value: format(certificate.issuedAt) },
            { label: '有效期至', value: formatExpiry(certificate.expiresAt) },
            { label: '当前状态', value: label(certificate.displayStatus) },
          ]"
        />
      </section>
      <p class="certificate-detail__note">证书信息由培训管理服务生成。</p></template
    >
  </section>
</template>
<style scoped>
.certificate-detail {
  display: grid;
  gap: var(--space-6);
}
.certificate-detail__hero,
.certificate-detail__panel {
  padding: var(--space-6);
  border: var(--border-default);
  border-radius: var(--radius-lg);
  background: var(--color-surface);
}
.certificate-detail__hero {
  display: flex;
  justify-content: space-between;
  gap: var(--space-4);
}
.certificate-detail__hero p {
  margin: 0 0 var(--space-1);
  color: var(--text-secondary);
}
.certificate-detail__hero h2 {
  margin: 0 0 var(--space-2);
}
.certificate-detail__hero span,
.certificate-detail__note {
  color: var(--text-secondary);
}
.certificate-detail__panel {
  display: grid;
  gap: var(--space-5);
}
.certificate-detail__note {
  margin: 0;
  padding: var(--space-4);
  background: var(--color-surface-subtle);
  border-radius: var(--radius-md);
}
</style>
