<script setup lang="ts">
import { computed, onMounted, reactive, ref } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import AppButton from '@/components/common/AppButton.vue'
import AppPagination from '@/components/common/AppPagination.vue'
import ConfirmDialog from '@/components/common/ConfirmDialog.vue'
import DataTable from '@/components/common/DataTable.vue'
import PageHeader from '@/components/common/PageHeader.vue'
import PageState from '@/components/common/PageState.vue'
import SearchPanel from '@/components/common/SearchPanel.vue'
import StatusTag from '@/components/common/StatusTag.vue'
import { getTrainingRequestService } from '@/services/training-request'
import { isServiceError, type UiError } from '@/types/api'
import type {
  TrainingRequest,
  TrainingRequestQuery,
  TrainingRequestStatus,
} from '@/domains/training-request'
import type { TableColumn } from '@/types/ui'
import {
  compactListQuery,
  readPositiveQueryInteger,
  readQueryString,
} from '@/utils/list-route-state'

const router = useRouter()
const route = useRoute()
const filters = reactive({
  employeeKeyword: '',
  keyword: '',
  status: 'PENDING' as TrainingRequestStatus | '',
  dateRange: [] as string[],
  page: 1,
  pageSize: 20,
})
const rows = ref<TrainingRequest[]>([])
const total = ref(0)
const loading = ref(false)
const hasLoaded = ref(false)
const error = ref<UiError | null>(null)
const dialog = ref<'approve' | 'reject' | null>(null)
const selected = ref<TrainingRequest | null>(null)
const opinion = ref('')
const saving = ref(false)
const columns: TableColumn[] = [
  { key: 'employeeName', label: '申请人', width: 130 },
  { key: 'courseName', label: '课程名称', minWidth: 230 },
  { key: 'departmentName', label: '部门', width: 140 },
  { key: 'submittedAtLabel', label: '申请时间', width: 170 },
  { key: 'statusLabel', label: '状态', width: 120, align: 'center' },
]
const state = computed(() =>
  loading.value && !hasLoaded.value
    ? 'loading'
    : !loading.value && rows.value.length === 0
      ? filters.employeeKeyword ||
        filters.keyword ||
        filters.status !== 'PENDING' ||
        filters.dateRange.length
        ? 'no-result'
        : 'empty'
      : 'default',
)
const tableRows = computed(() =>
  rows.value.map((item) => ({
    ...item,
    submittedAtLabel: formatDate(item.submittedAt),
    statusLabel: statusLabel(item.status),
  })),
)
const dialogTitle = computed(() => (dialog.value === 'approve' ? '通过培训申请' : '驳回培训申请'))
const dialogVisible = computed({
  get: () => dialog.value !== null,
  set: (value: boolean) => {
    if (!value && !saving.value) dialog.value = null
  },
})

function formatDate(value: string | null) {
  if (!value) return '—'
  const date = new Date(value)
  return Number.isNaN(date.getTime())
    ? '—'
    : new Intl.DateTimeFormat('zh-CN', { dateStyle: 'short', timeStyle: 'short' }).format(date)
}
function statusLabel(status: TrainingRequestStatus) {
  return (
    {
      PENDING: '待审批',
      DEPT_APPROVED: '部门已通过',
      DEPT_REJECTED: '部门已驳回',
      HR_FILED: '已备案',
      UNKNOWN: '未知状态',
    }[status] ?? '未知状态'
  )
}
function statusSemantic(status: unknown): 'success' | 'warning' | 'error' | 'info' | 'neutral' {
  return ({
    PENDING: 'warning',
    DEPT_APPROVED: 'info',
    DEPT_REJECTED: 'error',
    HR_FILED: 'success',
    UNKNOWN: 'neutral',
  }[String(status) as TrainingRequestStatus] ?? 'neutral') as
    'success' | 'warning' | 'error' | 'info' | 'neutral'
}
function query(): TrainingRequestQuery {
  return {
    employeeKeyword: filters.employeeKeyword || undefined,
    keyword: filters.keyword || undefined,
    status: filters.status || undefined,
    startDateFrom: filters.dateRange[0],
    startDateTo: filters.dateRange[1],
    page: filters.page,
    pageSize: filters.pageSize,
    sortBy: 'submittedAt',
    sortDirection: 'desc',
  }
}
async function load() {
  loading.value = true
  error.value = null
  try {
    const result = await (await getTrainingRequestService()).listDepartment(query())
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
          message: '审批列表加载失败。',
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
      employeeKeyword: filters.employeeKeyword,
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
function open(row: TrainingRequest, action: 'approve' | 'reject') {
  selected.value = row
  opinion.value = ''
  dialog.value = action
}
async function submit() {
  if (!selected.value || !dialog.value || (dialog.value === 'reject' && !opinion.value.trim()))
    return
  saving.value = true
  try {
    const service = await getTrainingRequestService()
    if (dialog.value === 'approve') await service.approve(selected.value.id, opinion.value)
    else await service.reject(selected.value.id, opinion.value)
    dialog.value = null
    await load()
  } catch (caught) {
    if (isServiceError(caught) && caught.ui.resultUnknown) {
      dialog.value = null
      await load()
    } else if (isServiceError(caught)) error.value = caught.ui
  } finally {
    saving.value = false
  }
}
function applyRouteQuery() {
  const { query } = route
  filters.employeeKeyword = readQueryString(query.employeeKeyword)
  filters.keyword = readQueryString(query.keyword)
  const status = readQueryString(query.status)
  filters.status =
    status === 'PENDING' ||
    status === 'DEPT_APPROVED' ||
    status === 'DEPT_REJECTED' ||
    status === 'HR_FILED'
      ? (status as TrainingRequestStatus)
      : 'PENDING'
  const from = readQueryString(query.startDateFrom)
  const to = readQueryString(query.startDateTo)
  filters.dateRange = from && to ? [from, to] : []
  filters.page = readPositiveQueryInteger(query.page, 1)
  filters.pageSize = readPositiveQueryInteger(query.pageSize, 20)
}
onMounted(() => {
  applyRouteQuery()
  load()
})
</script>

<template>
  <section class="business-list">
    <PageHeader title="主管审批" description="处理本部门员工提交的培训申请。" />
    <SearchPanel
      :expanded="true"
      :searching="loading"
      @search="sync({}, true)"
      @reset="
        sync(
          {
            employeeKeyword: '',
            keyword: '',
            status: 'PENDING',
            dateRange: [],
            page: 1,
            pageSize: 20,
          },
          true,
        )
      "
    >
      <el-input
        v-model="filters.employeeKeyword"
        clearable
        aria-label="申请人"
        placeholder="申请人或工号"
      />
      <el-input
        v-model="filters.keyword"
        clearable
        aria-label="课程关键词"
        placeholder="课程名称"
      />
      <el-select v-model="filters.status" clearable aria-label="申请状态" placeholder="状态"
        ><el-option label="待审批" value="PENDING" /><el-option
          label="已处理"
          value="DEPT_APPROVED" /><el-option label="已驳回" value="DEPT_REJECTED"
      /></el-select>
      <template #expanded
        ><el-date-picker
          v-model="filters.dateRange"
          type="daterange"
          value-format="YYYY-MM-DD"
          range-separator="至"
          start-placeholder="申请开始日期"
          end-placeholder="申请结束日期"
          aria-label="申请日期"
      /></template>
    </SearchPanel>
    <PageState v-if="state === 'loading'" state="loading" />
    <PageState
      v-else-if="error?.kind === 'forbidden'"
      state="forbidden"
      :description="error.message"
    />
    <PageState
      v-else-if="error"
      state="error"
      :description="error.message"
      primary-label="重新加载"
      @primary="load"
    />
    <PageState
      v-else-if="state === 'empty'"
      state="empty"
      title="暂无待处理申请"
      description="当前部门没有可审批的培训申请。"
      compact
    />
    <PageState
      v-else-if="state === 'no-result'"
      state="no-result"
      compact
      @primary="
        sync({ employeeKeyword: '', keyword: '', status: 'PENDING', dateRange: [], page: 1 }, true)
      "
    />
    <DataTable v-else :columns="columns" :rows="tableRows" has-actions>
      <template #cell="{ column, row }"
        ><StatusTag
          v-if="column.key === 'statusLabel'"
          :label="String(row.statusLabel)"
          :semantic="statusSemantic(row.status as TrainingRequestStatus)"
        /><span v-else>{{ row[column.key] || '—' }}</span></template
      >
      <template #actions="{ row }"
        ><AppButton
          label="查看"
          variant="text"
          @click="router.push({ name: 'request-detail', params: { id: row.id } })" /><AppButton
          v-if="row.status === 'PENDING'"
          label="通过"
          variant="text"
          @click="open(row as unknown as TrainingRequest, 'approve')" /><AppButton
          v-if="row.status === 'PENDING'"
          label="驳回"
          variant="text"
          @click="open(row as unknown as TrainingRequest, 'reject')"
      /></template>
    </DataTable>
    <AppPagination
      v-if="state === 'default' && total"
      :page="filters.page"
      :page-size="filters.pageSize"
      :total="total"
      :disabled="loading"
      @update:page="sync({ page: $event })"
      @update:page-size="sync({ pageSize: $event }, true)"
    />
    <ConfirmDialog
      v-model="dialogVisible"
      :title="dialogTitle"
      :description="
        dialog === 'approve'
          ? '通过后申请将进入 HR 备案阶段。'
          : '驳回后申请将结束，员工可在详情中看到处理意见。'
      "
      :confirm-label="dialog === 'approve' ? '确认通过' : '确认驳回'"
      :type="dialog === 'reject' ? 'danger' : 'confirm'"
      :loading="saving"
      @confirm="submit"
      ><el-input
        v-model="opinion"
        type="textarea"
        :rows="3"
        :placeholder="dialog === 'reject' ? '请填写驳回意见（必填）' : '可填写审批意见（选填）'"
    /></ConfirmDialog>
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
