<script setup lang="ts">
import { computed, onMounted, reactive, ref } from 'vue'
import { useRouter } from 'vue-router'
import AppButton from '@/components/common/AppButton.vue'
import AppPagination from '@/components/common/AppPagination.vue'
import ConfirmDialog from '@/components/common/ConfirmDialog.vue'
import DataTable from '@/components/common/DataTable.vue'
import PageHeader from '@/components/common/PageHeader.vue'
import PageState from '@/components/common/PageState.vue'
import SearchPanel from '@/components/common/SearchPanel.vue'
import StatusTag from '@/components/common/StatusTag.vue'
import { getRegistrationService } from '@/services/registration'
import { isServiceError, type UiError } from '@/types/api'
import type { Registration, RegistrationQuery, RegistrationStatus } from '@/domains/registration'
import type { TableColumn } from '@/types/ui'
import { compactListQuery } from '@/utils/list-route-state'
const router = useRouter()
const filters = reactive({
  keyword: '',
  status: '' as RegistrationStatus | '',
  dateRange: [] as string[],
  page: 1,
  pageSize: 20,
})
const rows = ref<Registration[]>([])
const total = ref(0)
const loading = ref(false)
const hasLoaded = ref(false)
const error = ref<UiError | null>(null)
const selected = ref<Registration | null>(null)
const cancelVisible = ref(false)
const saving = ref(false)
const columns: TableColumn[] = [
  { key: 'courseName', label: '课程名称', minWidth: 260 },
  { key: 'registeredAtLabel', label: '报名时间', width: 170 },
  { key: 'statusLabel', label: '报名状态', width: 130, align: 'center' },
  { key: 'signedInAtLabel', label: '签到时间', width: 170 },
]
const state = computed(() =>
  loading.value && !hasLoaded.value
    ? 'loading'
    : !loading.value && rows.value.length === 0
      ? filters.keyword || filters.status || filters.dateRange.length
        ? 'no-result'
        : 'empty'
      : 'default',
)
const tableRows = computed(() =>
  rows.value.map((item) => ({
    ...item,
    registeredAtLabel: format(item.registeredAt),
    signedInAtLabel: format(item.signedInAt),
    statusLabel: label(item.status),
  })),
)
function format(value: string | null) {
  if (!value) return '—'
  const date = new Date(value)
  return Number.isNaN(date.getTime())
    ? '—'
    : new Intl.DateTimeFormat('zh-CN', { dateStyle: 'short', timeStyle: 'short' }).format(date)
}
function label(status: RegistrationStatus) {
  return (
    {
      REGISTERED: '已报名',
      SIGNED_IN: '已签到',
      ABSENT: '缺席',
      COMPLETED: '已完成',
      CANCELED: '已取消',
      UNKNOWN: '未知状态',
    }[status] ?? '未知状态'
  )
}
function semantic(status: unknown): 'success' | 'warning' | 'error' | 'info' | 'neutral' {
  return ({
    REGISTERED: 'info',
    SIGNED_IN: 'warning',
    ABSENT: 'error',
    COMPLETED: 'success',
    CANCELED: 'neutral',
    UNKNOWN: 'neutral',
  }[String(status) as RegistrationStatus] ?? 'neutral') as
    'success' | 'warning' | 'error' | 'info' | 'neutral'
}
function query(): RegistrationQuery {
  return {
    keyword: filters.keyword || undefined,
    status: filters.status || undefined,
    startDateFrom: filters.dateRange[0],
    startDateTo: filters.dateRange[1],
    page: filters.page,
    pageSize: filters.pageSize,
    sortBy: 'registeredAt',
    sortDirection: 'desc',
  }
}
async function load() {
  loading.value = true
  error.value = null
  try {
    const result = await (await getRegistrationService()).listMine(query())
    rows.value = result.items
    total.value = result.total
    hasLoaded.value = true
  } catch (caught) {
    if (isServiceError(caught) && caught.ui.kind === 'canceled') return
    rows.value = []
    total.value = 0
    hasLoaded.value = true
    error.value = isServiceError(caught)
      ? caught.ui
      : {
          kind: 'unknown',
          code: 'UNKNOWN',
          message: '报名列表加载失败。',
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
  await router.replace({
    query: compactListQuery({
      keyword: filters.keyword,
      status: filters.status,
      startDateFrom: filters.dateRange[0],
      startDateTo: filters.dateRange[1],
      page: filters.page === 1 ? '' : filters.page,
      pageSize: filters.pageSize === 20 ? '' : filters.pageSize,
    }),
  })
  await load()
}
function openCancel(row: Registration) {
  selected.value = row
  cancelVisible.value = true
}
async function cancel() {
  if (!selected.value) return
  saving.value = true
  try {
    await (await getRegistrationService()).cancel(selected.value.id)
    cancelVisible.value = false
    await load()
  } catch (caught) {
    if (isServiceError(caught) && caught.ui.resultUnknown) {
      cancelVisible.value = false
      await load()
    } else if (isServiceError(caught)) error.value = caught.ui
  } finally {
    saving.value = false
  }
}
onMounted(load)
</script>
<template>
  <section class="business-list">
    <PageHeader
      title="我的报名"
      description="查看本人报名、签到与培训完成状态。"
      :breadcrumbs="['我的培训', '我的报名']"
      ><template #action
        ><AppButton
          label="浏览课程"
          variant="primary"
          @click="router.push({ name: 'course-list' })" /></template></PageHeader
    ><SearchPanel
      :expanded="true"
      :searching="loading"
      @search="sync({}, true)"
      @reset="sync({ keyword: '', status: '', dateRange: [], page: 1, pageSize: 20 }, true)"
      ><el-input
        v-model="filters.keyword"
        clearable
        aria-label="课程关键词"
        placeholder="课程名称" /><el-select
        v-model="filters.status"
        clearable
        aria-label="报名状态"
        placeholder="报名状态"
        ><el-option label="已报名" value="REGISTERED" /><el-option
          label="已签到"
          value="SIGNED_IN" /><el-option label="缺席" value="ABSENT" /><el-option
          label="已完成"
          value="COMPLETED" /><el-option label="已取消" value="CANCELED" /></el-select
      ><template #expanded
        ><el-date-picker
          v-model="filters.dateRange"
          type="daterange"
          value-format="YYYY-MM-DD"
          range-separator="至"
          start-placeholder="报名开始日期"
          end-placeholder="报名结束日期"
          aria-label="报名日期" /></template></SearchPanel
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
      title="暂无报名记录"
      description="报名课程后，签到和完成状态会显示在这里。"
      secondary-label="浏览课程"
      compact
      @secondary="router.push({ name: 'course-list' })"
    /><PageState
      v-else-if="state === 'no-result'"
      state="no-result"
      secondary-label="清空筛选"
      compact
      @secondary="sync({ keyword: '', status: '', dateRange: [], page: 1 }, true)"
    /><DataTable v-else :columns="columns" :rows="tableRows" has-actions
      ><template #cell="{ column, row }"
        ><StatusTag
          v-if="column.key === 'statusLabel'"
          :label="String(row.statusLabel)"
          :semantic="semantic(row.status)"
        /><span v-else>{{ row[column.key] || '—' }}</span></template
      ><template #actions="{ row }"
        ><AppButton
          label="查看详情"
          variant="text"
          @click="router.push({ name: 'registration-detail', params: { id: row.id } })" /><AppButton
          v-if="row.status === 'REGISTERED'"
          label="取消报名"
          variant="text"
          @click="openCancel(row as unknown as Registration)" /></template></DataTable
    ><AppPagination
      v-if="state === 'default' && total"
      :page="filters.page"
      :page-size="filters.pageSize"
      :total="total"
      :disabled="loading"
      @update:page="sync({ page: $event })"
      @update:page-size="sync({ pageSize: $event }, true)"
    /><ConfirmDialog
      v-model="cancelVisible"
      title="取消报名"
      description="取消后将释放课程名额，且需要重新报名才能参加培训。"
      confirm-label="确认取消"
      type="danger"
      :loading="saving"
      @confirm="cancel"
    />
  </section>
</template>
<style scoped>
.business-list {
  display: grid;
  gap: var(--space-6);
}
.business-list :deep(.el-date-editor) {
  width: 100%;
}
</style>
