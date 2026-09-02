<script setup lang="ts">
import { computed, onMounted, reactive, ref } from 'vue'
import { useRouter } from 'vue-router'
import AppButton from '@/components/common/AppButton.vue'
import AppDescriptions from '@/components/common/AppDescriptions.vue'
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
import { compactListQuery } from '@/utils/list-route-state'

const router = useRouter()
const filters = reactive({
  employeeKeyword: '',
  keyword: '',
  departmentName: '',
  status: 'DEPT_APPROVED' as TrainingRequestStatus | '',
  dateRange: [] as string[],
  page: 1,
  pageSize: 20,
})
const rows = ref<TrainingRequest[]>([])
const total = ref(0)
const loading = ref(false)
const hasLoaded = ref(false)
const error = ref<UiError | null>(null)
const selected = ref<TrainingRequest | null>(null)
const dialogVisible = ref(false)
const saving = ref(false)
const columns: TableColumn[] = [
  { key: 'employeeName', label: '申请人', width: 130 },
  { key: 'departmentName', label: '部门', width: 140 },
  { key: 'courseName', label: '课程名称', minWidth: 230 },
  { key: 'submittedAtLabel', label: '申请时间', width: 170 },
  { key: 'statusLabel', label: '状态', width: 120, align: 'center' },
]
const state = computed(() =>
  loading.value && !hasLoaded.value
    ? 'loading'
    : !loading.value && rows.value.length === 0
      ? filters.employeeKeyword ||
        filters.keyword ||
        filters.departmentName ||
        filters.status !== 'DEPT_APPROVED' ||
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
      DEPT_REJECTED: '已驳回',
      HR_FILED: '已备案',
      UNKNOWN: '未知状态',
    }[status] ?? '未知状态'
  )
}
function semantic(status: unknown): 'success' | 'warning' | 'error' | 'info' | 'neutral' {
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
    departmentName: filters.departmentName || undefined,
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
    const result = await (await getTrainingRequestService()).listForHr(query())
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
          message: 'HR 备案列表加载失败。',
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
      departmentName: filters.departmentName,
      status: filters.status,
      startDateFrom: filters.dateRange[0],
      startDateTo: filters.dateRange[1],
      page: filters.page === 1 ? '' : filters.page,
      pageSize: filters.pageSize === 20 ? '' : filters.pageSize,
    }),
  })
  await load()
}
function open(row: TrainingRequest) {
  selected.value = row
  dialogVisible.value = true
}
async function submit() {
  if (!selected.value) return
  saving.value = true
  try {
    await (await getTrainingRequestService()).file(selected.value.id, '')
    dialogVisible.value = false
    await load()
  } catch (caught) {
    if (isServiceError(caught) && caught.ui.resultUnknown) {
      dialogVisible.value = false
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
    <PageHeader title="HR 备案" description="对已通过部门审批的培训申请进行备案。" /><SearchPanel
      :expanded="true"
      :searching="loading"
      @search="sync({}, true)"
      @reset="
        sync(
          {
            employeeKeyword: '',
            keyword: '',
            departmentName: '',
            status: 'DEPT_APPROVED',
            dateRange: [],
            page: 1,
            pageSize: 20,
          },
          true,
        )
      "
      ><el-input
        v-model="filters.employeeKeyword"
        clearable
        aria-label="申请人"
        placeholder="申请人或工号" /><el-input
        v-model="filters.keyword"
        clearable
        aria-label="课程关键词"
        placeholder="课程名称" /><el-input
        v-model="filters.departmentName"
        clearable
        aria-label="部门"
        placeholder="部门名称" /><template #expanded
        ><el-date-picker
          v-model="filters.dateRange"
          type="daterange"
          value-format="YYYY-MM-DD"
          range-separator="至"
          start-placeholder="申请开始日期"
          end-placeholder="申请结束日期"
          aria-label="申请日期" /></template></SearchPanel
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
      title="暂无待备案申请"
      description="当前没有等待 HR 备案的申请。"
      compact
    /><PageState
      v-else-if="state === 'no-result'"
      state="no-result"
      secondary-label="清空筛选"
      compact
      @secondary="
        sync(
          {
            employeeKeyword: '',
            keyword: '',
            departmentName: '',
            status: 'DEPT_APPROVED',
            dateRange: [],
            page: 1,
          },
          true,
        )
      "
    /><DataTable v-else :columns="columns" :rows="tableRows" has-actions
      ><template #cell="{ column, row }"
        ><StatusTag
          v-if="column.key === 'statusLabel'"
          :label="String(row.statusLabel)"
          :semantic="semantic(row.status as TrainingRequestStatus)"
        /><span v-else>{{ row[column.key] || '—' }}</span></template
      ><template #actions="{ row }"
        ><AppButton
          label="查看"
          variant="text"
          @click="router.push({ name: 'request-detail', params: { id: row.id } })" /><AppButton
          v-if="row.status === 'DEPT_APPROVED'"
          label="备案"
          variant="text"
          @click="open(row as unknown as TrainingRequest)" /></template></DataTable
    ><AppPagination
      v-if="state === 'default' && total"
      :page="filters.page"
      :page-size="filters.pageSize"
      :total="total"
      :disabled="loading"
      @update:page="sync({ page: $event })"
      @update:page-size="sync({ pageSize: $event }, true)"
    /><ConfirmDialog
      v-model="dialogVisible"
      title="确认 HR 备案"
      description="备案后员工可以进入课程报名环节。"
      confirm-label="确认备案"
      :loading="saving"
      @confirm="submit"
      ><AppDescriptions
        :columns="1"
        :items="[
          { label: '申请人', value: selected?.employeeName ?? '—' },
          { label: '所属部门', value: selected?.departmentName ?? '—' },
          { label: '课程名称', value: selected?.courseName ?? '—' },
          { label: '申请时间', value: selected ? formatDate(selected.submittedAt) : '—' },
          { label: '主管意见', value: selected?.departmentOpinion || '—' },
        ]"
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
