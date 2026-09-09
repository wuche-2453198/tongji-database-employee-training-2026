<script setup lang="ts">
import { computed, onMounted, reactive, ref } from 'vue'
import { useRouter } from 'vue-router'
import AppButton from '@/components/common/AppButton.vue'
import AppPagination from '@/components/common/AppPagination.vue'
import DataTable from '@/components/common/DataTable.vue'
import PageHeader from '@/components/common/PageHeader.vue'
import PageState from '@/components/common/PageState.vue'
import SearchPanel from '@/components/common/SearchPanel.vue'
import StatusTag from '@/components/common/StatusTag.vue'
import { getCertificateService } from '@/services/certificate'
import { isServiceError, type UiError } from '@/types/api'
import type { Certificate, CertificateDisplayStatus, CertificateQuery } from '@/domains/certificate'
import type { TableColumn } from '@/types/ui'
const router = useRouter()
const filters = reactive({
  keyword: '',
  status: '' as CertificateDisplayStatus | '',
  page: 1,
  pageSize: 20,
})
const rows = ref<Certificate[]>([])
const total = ref(0)
const loading = ref(false)
const hasLoaded = ref(false)
const error = ref<UiError | null>(null)
const columns: TableColumn[] = [
  { key: 'certificateNo', label: '证书编号', width: 190 },
  { key: 'courseName', label: '课程名称', minWidth: 260 },
  { key: 'issuedAtLabel', label: '生成时间', width: 170 },
  { key: 'expiresAtLabel', label: '有效期至', width: 170 },
  { key: 'statusLabel', label: '状态', width: 120, align: 'center' },
]
const tableRows = computed(() =>
  rows.value.map((item) => ({
    ...item,
    issuedAtLabel: format(item.issuedAt),
    expiresAtLabel: format(item.expiresAt),
    statusLabel: label(item.displayStatus),
  })),
)
const state = computed(() =>
  loading.value && !hasLoaded.value
    ? 'loading'
    : !loading.value && rows.value.length === 0
      ? filters.keyword || filters.status
        ? 'no-result'
        : 'empty'
      : 'default',
)
function format(value: string | null) {
  if (!value) return '—'
  const date = new Date(value)
  return Number.isNaN(date.getTime())
    ? '—'
    : new Intl.DateTimeFormat('zh-CN', { dateStyle: 'short' }).format(date)
}
function label(status: CertificateDisplayStatus) {
  return (
    { VALID: '有效', EXPIRING: '即将到期', EXPIRED: '已过期', UNKNOWN: '未知状态' }[status] ??
    '未知状态'
  )
}
function semantic(status: unknown): 'success' | 'warning' | 'error' | 'info' | 'neutral' {
  return ({ VALID: 'success', EXPIRING: 'warning', EXPIRED: 'error', UNKNOWN: 'neutral' }[
    String(status) as CertificateDisplayStatus
  ] ?? 'neutral') as 'success' | 'warning' | 'error' | 'info' | 'neutral'
}
function query(): CertificateQuery {
  return {
    keyword: filters.keyword || undefined,
    status: filters.status || undefined,
    page: filters.page,
    pageSize: filters.pageSize,
    sortBy: 'issuedAt',
    sortDirection: 'desc',
  }
}
async function load() {
  loading.value = true
  error.value = null
  try {
    const result = await (await getCertificateService()).listMine(query())
    rows.value = result.items
    total.value = result.total
    hasLoaded.value = true
  } catch (caught) {
    rows.value = []
    total.value = 0
    hasLoaded.value = true
    error.value = isServiceError(caught)
      ? caught.ui
      : {
          kind: 'unknown',
          code: 'UNKNOWN',
          message: '证书列表加载失败。',
          fieldErrors: [],
          retryable: false,
          resultUnknown: false,
        }
  } finally {
    loading.value = false
  }
}
async function sync(next: Partial<typeof filters>, reset = false) {
  Object.assign(filters, next)
  if (reset) filters.page = 1
  await load()
}
onMounted(load)
</script>
<template>
  <section class="business-list">
    <PageHeader title="我的证书" description="查看本人已生成的培训证书。" /><SearchPanel
      :expanded="false"
      :searching="loading"
      @search="sync({}, true)"
      @reset="sync({ keyword: '', status: '', page: 1, pageSize: 20 }, true)"
      ><el-input
        v-model="filters.keyword"
        clearable
        aria-label="课程关键词"
        placeholder="课程名称" /><el-select
        v-model="filters.status"
        clearable
        aria-label="证书状态"
        placeholder="证书状态"
        ><el-option label="有效" value="VALID" /><el-option
          label="即将到期"
          value="EXPIRING" /><el-option label="已过期" value="EXPIRED" /></el-select></SearchPanel
    ><PageState v-if="state === 'loading'" state="loading" /><PageState
      v-else-if="error?.kind === 'forbidden'"
      state="forbidden"
      :description="error.message"
    /><PageState
      v-else-if="error"
      state="error"
      :description="error.message"
      primary-label="重新加载"
      @primary="load"
    /><PageState
      v-else-if="state === 'empty'"
      state="empty"
      title="暂无证书"
      description="完成培训并满足发证条件后，证书会显示在这里。"
      compact
    /><PageState
      v-else-if="state === 'no-result'"
      state="no-result"
      secondary-label="清空筛选"
      compact
      @secondary="sync({ keyword: '', status: '', page: 1 }, true)"
    /><DataTable v-else :columns="columns" :rows="tableRows" has-actions
      ><template #cell="{ column, row }"
        ><StatusTag
          v-if="column.key === 'statusLabel'"
          :label="String(row.statusLabel)"
          :semantic="semantic(row.displayStatus)"
        /><span v-else>{{ row[column.key] || '—' }}</span></template
      ><template #actions="{ row }"
        ><AppButton
          label="查看详情"
          variant="text"
          @click="
            router.push({ name: 'certificate-detail', params: { id: row.id } })
          " /></template></DataTable
    ><AppPagination
      v-if="state === 'default' && total"
      :page="filters.page"
      :page-size="filters.pageSize"
      :total="total"
      :disabled="loading"
      @update:page="sync({ page: $event })"
      @update:page-size="sync({ pageSize: $event }, true)"
    />
  </section>
</template>
<style scoped>
.business-list {
  display: grid;
  gap: var(--space-6);
}
</style>
