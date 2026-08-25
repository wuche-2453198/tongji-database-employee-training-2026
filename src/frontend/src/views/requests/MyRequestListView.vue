<script setup lang="ts">
import { computed, onBeforeUnmount, onMounted, reactive, ref, watch } from 'vue'
import { useRoute, useRouter } from 'vue-router'

import { LatestRequestController } from '@/api/request-control'
import AppButton from '@/components/common/AppButton.vue'
import AppPagination from '@/components/common/AppPagination.vue'
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

const route = useRoute()
const router = useRouter()
const latestQuery = new LatestRequestController()
const filters = reactive<{
  keyword: string
  status: TrainingRequestStatus | ''
  dateRange: string[]
  page: number
  pageSize: number
}>({
  keyword: '',
  status: '',
  dateRange: [],
  page: 1,
  pageSize: 20,
})
const rows = ref<TrainingRequest[]>([])
const total = ref(0)
const loading = ref(false)
const hasLoaded = ref(false)
const error = ref<UiError | null>(null)
const searchExpanded = ref(true)
const submittedMessage = ref('')
const columns: TableColumn[] = [
  { key: 'courseName', label: '课程名称', minWidth: 240 },
  { key: 'submittedAtLabel', label: '申请时间', width: 170 },
  { key: 'statusLabel', label: '申请状态', width: 120, align: 'center' },
  { key: 'stageLabel', label: '当前阶段', width: 150 },
]
const hasActiveFilter = computed(() =>
  Boolean(filters.keyword || filters.status || filters.dateRange.length),
)
const state = computed<'loading' | 'empty' | 'no-result' | 'default'>(() => {
  if (loading.value && !hasLoaded.value) return 'loading'
  if (!loading.value && rows.value.length === 0)
    return hasActiveFilter.value ? 'no-result' : 'empty'
  return 'default'
})
const tableRows = computed<Record<string, unknown>[]>(() =>
  rows.value.map((item) => ({
    ...item,
    submittedAtLabel: formatDateTime(item.submittedAt),
    statusLabel: statusMeta(item.status).label,
    stageLabel: stageLabel(item.status),
  })),
)

function formatDateTime(value: string): string {
  const date = new Date(value)
  if (Number.isNaN(date.getTime())) return '—'
  return new Intl.DateTimeFormat('zh-CN', { dateStyle: 'short', timeStyle: 'short' }).format(date)
}

function statusMeta(status: TrainingRequestStatus): {
  label: string
  semantic: 'success' | 'warning' | 'error' | 'info' | 'neutral'
} {
  const values: Record<
    TrainingRequestStatus,
    { label: string; semantic: 'success' | 'warning' | 'error' | 'info' | 'neutral' }
  > = {
    PENDING: { label: '待审批', semantic: 'warning' },
    DEPT_APPROVED: { label: '部门已通过', semantic: 'info' },
    DEPT_REJECTED: { label: '部门已驳回', semantic: 'error' },
    HR_FILED: { label: '已备案', semantic: 'success' },
    UNKNOWN: { label: '未知状态', semantic: 'neutral' },
  }
  return values[status] ?? values.UNKNOWN
}

function stageLabel(status: TrainingRequestStatus): string {
  return {
    PENDING: '部门主管审批',
    DEPT_APPROVED: '等待 HR 备案',
    DEPT_REJECTED: '部门审批结束',
    HR_FILED: '申请流程完成',
    UNKNOWN: '状态待确认',
  }[status]
}

function applyRouteQuery(): void {
  filters.keyword = readQueryString(route.query.keyword)
  filters.status = readQueryString(route.query.status) as TrainingRequestStatus | ''
  filters.dateRange = [
    readQueryString(route.query.startDateFrom),
    readQueryString(route.query.startDateTo),
  ].filter(Boolean)
  filters.page = readPositiveQueryInteger(route.query.page, 1)
  filters.pageSize = [10, 20, 50].includes(readPositiveQueryInteger(route.query.pageSize, 20))
    ? readPositiveQueryInteger(route.query.pageSize, 20)
    : 20
  if (readQueryString(route.query.submitted))
    submittedMessage.value = '培训申请已提交，当前状态为待审批。'
}

function buildQuery(): TrainingRequestQuery {
  return {
    keyword: filters.keyword || undefined,
    status: filters.status || undefined,
    startDateFrom: filters.dateRange[0] || undefined,
    startDateTo: filters.dateRange[1] || undefined,
    page: filters.page,
    pageSize: filters.pageSize,
    sortBy: 'submittedAt',
    sortDirection: 'desc',
  }
}

async function loadRequests(): Promise<void> {
  loading.value = true
  error.value = null
  try {
    const service = await getTrainingRequestService()
    const result = await latestQuery.run((signal) => service.listMine(buildQuery(), { signal }))
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
          message: '申请列表加载失败。',
          fieldErrors: [],
          retryable: false,
          resultUnknown: false,
        }
  } finally {
    loading.value = false
  }
}

async function updateUrlAndLoad(next: Partial<typeof filters>, resetPage = false): Promise<void> {
  Object.assign(filters, next)
  if (resetPage) filters.page = 1
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
  await loadRequests()
}

function resetFilters(): void {
  void updateUrlAndLoad({ keyword: '', status: '', dateRange: [], page: 1, pageSize: 20 }, true)
}
function search(): void {
  void updateUrlAndLoad({}, true)
}
function openDetail(row: TrainingRequest): void {
  void router.push({ name: 'request-detail', params: { id: row.id } })
}

watch(
  () => route.query,
  async () => {
    applyRouteQuery()
    await loadRequests()
  },
  { deep: true },
)
onMounted(() => {
  applyRouteQuery()
  void loadRequests()
})
onBeforeUnmount(() => latestQuery.cancel())
</script>

<template>
  <section class="my-request-list-view">
    <PageHeader
      title="我的申请"
      description="查看本人培训申请的审批与备案进度。"
      :breadcrumbs="['我的培训', '我的申请']"
    >
      <template #action
        ><AppButton
          label="浏览课程并申请"
          variant="primary"
          @click="router.push({ name: 'course-list' })"
      /></template>
    </PageHeader>
    <el-alert
      v-if="submittedMessage"
      :title="submittedMessage"
      type="success"
      show-icon
      :closable="true"
      @close="submittedMessage = ''"
    />
    <SearchPanel
      :expanded="searchExpanded"
      :searching="loading"
      @update:expanded="searchExpanded = $event"
      @search="search"
      @reset="resetFilters"
    >
      <el-input
        v-model="filters.keyword"
        clearable
        aria-label="申请课程关键词"
        placeholder="请输入课程名称"
      />
      <el-select v-model="filters.status" clearable aria-label="申请状态" placeholder="申请状态">
        <el-option label="待审批" value="PENDING" /><el-option
          label="部门已通过"
          value="DEPT_APPROVED"
        /><el-option label="部门已驳回" value="DEPT_REJECTED" /><el-option
          label="已备案"
          value="HR_FILED"
        />
      </el-select>
      <template #expanded
        ><el-date-picker
          v-model="filters.dateRange"
          type="daterange"
          value-format="YYYY-MM-DD"
          range-separator="至"
          start-placeholder="申请开始日期"
          end-placeholder="申请结束日期"
          aria-label="申请日期范围"
      /></template>
    </SearchPanel>
    <div class="my-request-list-view__summary" aria-live="polite">
      <strong>申请记录</strong
      ><span v-if="state === 'default' || state === 'loading'">共 {{ total }} 条</span>
    </div>
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
      :trace-id="error.traceId"
      primary-label="重新加载"
      @primary="loadRequests"
    />
    <PageState
      v-else-if="state === 'empty'"
      state="empty"
      title="暂无培训申请"
      description="你还没有提交培训申请，可以先浏览课程。"
      secondary-label="浏览课程"
      compact
      @secondary="router.push({ name: 'course-list' })"
    />
    <PageState
      v-else-if="state === 'no-result'"
      state="no-result"
      title="未找到匹配申请"
      description="请调整课程名称、申请状态或日期范围。"
      secondary-label="清空筛选"
      compact
      @secondary="resetFilters"
    />
    <DataTable v-else :columns="columns" :rows="tableRows" has-actions @retry="loadRequests">
      <template #cell="{ column, row }">
        <button
          v-if="column.key === 'courseName'"
          type="button"
          class="my-request-list-view__link"
          @click="openDetail(row as unknown as TrainingRequest)"
        >
          {{ row.courseName }}
        </button>
        <StatusTag
          v-else-if="column.key === 'statusLabel'"
          :label="String(row.statusLabel)"
          :semantic="statusMeta(row.status as TrainingRequestStatus).semantic"
        />
        <span v-else>{{ row[column.key] || '—' }}</span>
      </template>
      <template #actions="{ row }"
        ><AppButton
          label="查看详情"
          variant="text"
          @click="openDetail(row as unknown as TrainingRequest)"
      /></template>
    </DataTable>
    <AppPagination
      v-if="state === 'default' && total > 0"
      :page="filters.page"
      :page-size="filters.pageSize"
      :total="total"
      :disabled="loading"
      @update:page="updateUrlAndLoad({ page: $event })"
      @update:page-size="updateUrlAndLoad({ pageSize: $event }, true)"
    />
  </section>
</template>

<style scoped>
.my-request-list-view {
  display: grid;
  gap: var(--space-6);
}
.my-request-list-view__summary {
  display: flex;
  gap: var(--space-4);
  align-items: center;
  color: var(--text-secondary);
}
.my-request-list-view__summary strong {
  color: var(--text-primary);
  font-size: var(--font-size-title-component);
}
.my-request-list-view__link {
  padding: 0;
  border: 0;
  color: var(--text-link);
  background: transparent;
  cursor: pointer;
  text-align: left;
}
.my-request-list-view__link:hover {
  text-decoration: underline;
}
.my-request-list-view :deep(.el-date-editor) {
  width: 100%;
}
@media (width <= 1100px) {
  .my-request-list-view :deep(.search-panel__fields) {
    grid-template-columns: repeat(2, minmax(0, 1fr));
  }
}
</style>
