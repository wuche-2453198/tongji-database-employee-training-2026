<script setup lang="ts">
import { computed, onMounted, reactive, ref } from 'vue'
import { useRouter } from 'vue-router'
import AppPagination from '@/components/common/AppPagination.vue'
import DataTable from '@/components/common/DataTable.vue'
import PageHeader from '@/components/common/PageHeader.vue'
import PageState from '@/components/common/PageState.vue'
import SearchPanel from '@/components/common/SearchPanel.vue'
import StatusTag from '@/components/common/StatusTag.vue'
import { getTestService } from '@/services/test'
import { isServiceError, type UiError } from '@/types/api'
import type { TestQuerySummary, TestScoreSummary } from '@/domains/test'
import type { StatusSemantic, TableColumn } from '@/types/ui'
import { compactListQuery } from '@/utils/list-route-state'

const router = useRouter()
const filters = reactive({
  keyword: '',
  dateRange: [] as string[],
  page: 1,
  pageSize: 20,
})
const rows = ref<TestScoreSummary[]>([])
const total = ref(0)
const loading = ref(false)
const hasLoaded = ref(false)
const error = ref<UiError | null>(null)
const columns: TableColumn[] = [
  { key: 'courseName', label: '课程名称', minWidth: 240 },
  { key: 'preScoreLabel', label: '训前成绩', width: 120, align: 'center' },
  { key: 'postScoreLabel', label: '训后成绩', width: 120, align: 'center' },
  { key: 'changeLabel', label: '分数变化', width: 120, align: 'center' },
  { key: 'improvementRateLabel', label: '提升率', width: 110, align: 'center' },
  { key: 'updatedAtLabel', label: '更新时间', width: 170 },
]
const state = computed(() =>
  loading.value && !hasLoaded.value
    ? 'loading'
    : !loading.value && rows.value.length === 0
      ? filters.keyword || filters.dateRange.length
        ? 'no-result'
        : 'empty'
      : 'default',
)
const tableRows = computed(() =>
  rows.value.map((item) => ({
    ...item,
    preScoreLabel: scoreText(item.preScore),
    postScoreLabel: scoreText(item.postScore),
    changeLabel: changeText(item.change),
    improvementRateLabel: rateText(item.improvementRate),
    updatedAtLabel: formatDate(item.updatedAt),
  })),
)

const scoreSemantic = (score: number | null): StatusSemantic =>
  score === null ? 'neutral' : score >= 60 ? 'success' : 'error'
function scoreText(value: number | null) {
  return value === null ? '未录入' : `${value} 分`
}
function changeText(value: number | null) {
  if (value === null) return '—'
  return value > 0 ? `+${value}` : String(value)
}
function rateText(value: number | null) {
  if (value === null) return '—'
  return `${value > 0 ? '+' : ''}${value}%`
}
function formatDate(value: string | null) {
  if (!value) return '—'
  const date = new Date(value)
  return Number.isNaN(date.getTime())
    ? '—'
    : new Intl.DateTimeFormat('zh-CN', { dateStyle: 'short', timeStyle: 'short' }).format(date)
}
function query(): TestQuerySummary {
  return {
    keyword: filters.keyword || undefined,
    startDateFrom: filters.dateRange[0],
    startDateTo: filters.dateRange[1],
    page: filters.page,
    pageSize: filters.pageSize,
    sortBy: 'updatedAt',
    sortDirection: 'desc',
  }
}
async function load() {
  loading.value = true
  error.value = null
  try {
    const result = await (await getTestService()).listSummaries(query())
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
          message: '成绩列表加载失败。',
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
      startDateFrom: filters.dateRange[0],
      startDateTo: filters.dateRange[1],
      page: filters.page === 1 ? '' : filters.page,
      pageSize: filters.pageSize === 20 ? '' : filters.pageSize,
    }),
  })
  await load()
}
onMounted(load)
</script>

<template>
  <section class="business-list">
    <PageHeader
      title="我的成绩"
      description="查看本人各培训课程的训前、训后成绩与提升率。"
    /><SearchPanel
      :expanded="false"
      :searching="loading"
      @search="sync({}, true)"
      @reset="sync({ keyword: '', dateRange: [], page: 1, pageSize: 20 }, true)"
      ><el-input
        v-model="filters.keyword"
        clearable
        aria-label="课程关键词"
        placeholder="课程名称" /><template #expanded
        ><el-date-picker
          v-model="filters.dateRange"
          type="daterange"
          value-format="YYYY-MM-DD"
          range-separator="至"
          start-placeholder="测试开始日期"
          end-placeholder="测试结束日期"
          aria-label="测试日期" /></template></SearchPanel
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
      title="暂无成绩记录"
      description="完成培训并录入测试成绩后，会显示在这里。"
      compact
    /><PageState
      v-else-if="state === 'no-result'"
      state="no-result"
      secondary-label="清空筛选"
      compact
      @secondary="sync({ keyword: '', dateRange: [], page: 1 }, true)"
    /><DataTable v-else :columns="columns" :rows="tableRows"
      ><template #cell="{ column, row }"
        ><StatusTag
          v-if="column.key === 'preScoreLabel'"
          :label="String(row.preScoreLabel)"
          :semantic="scoreSemantic(row.preScore as number | null)"
        /><StatusTag
          v-else-if="column.key === 'postScoreLabel'"
          :label="String(row.postScoreLabel)"
          :semantic="scoreSemantic(row.postScore as number | null)"
        /><span v-else>{{ row[column.key] || '—' }}</span></template
      ></DataTable
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
.business-list :deep(.el-date-editor) {
  width: 100%;
}
</style>
