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
import type { TestQuery, TestType, TrainingTest } from '@/domains/test'
import type { TableColumn } from '@/types/ui'
import { compactListQuery } from '@/utils/list-route-state'

const router = useRouter()
const filters = reactive({
  employeeKeyword: '',
  keyword: '',
  testType: '' as TestType | '',
  dateRange: [] as string[],
  page: 1,
  pageSize: 20,
})
const rows = ref<TrainingTest[]>([])
const total = ref(0)
const loading = ref(false)
const hasLoaded = ref(false)
const error = ref<UiError | null>(null)
const columns: TableColumn[] = [
  { key: 'employeeName', label: '员工', width: 130 },
  { key: 'courseName', label: '课程名称', minWidth: 240 },
  { key: 'testTypeLabel', label: '测试类型', width: 120, align: 'center' },
  { key: 'scoreLabel', label: '成绩', width: 110, align: 'center' },
  { key: 'testDateLabel', label: '测试时间', width: 170 },
]
const state = computed(() =>
  loading.value && !hasLoaded.value
    ? 'loading'
    : !loading.value && rows.value.length === 0
      ? filters.employeeKeyword || filters.keyword || filters.testType || filters.dateRange.length
        ? 'no-result'
        : 'empty'
      : 'default',
)
const tableRows = computed(() =>
  rows.value.map((item) => ({
    ...item,
    testTypeLabel: testTypeLabel(item.testType),
    scoreLabel: `${item.score} 分`,
    testDateLabel: formatDate(item.testDate),
  })),
)
function formatDate(value: string) {
  const date = new Date(value)
  return Number.isNaN(date.getTime())
    ? '—'
    : new Intl.DateTimeFormat('zh-CN', { dateStyle: 'short', timeStyle: 'short' }).format(date)
}
function testTypeLabel(type: TestType) {
  return { PRE: '训前测试', POST: '训后测试', UNKNOWN: '未知' }[type] ?? '未知'
}
function testTypeSemantic(type: TestType): 'success' | 'warning' | 'error' | 'info' | 'neutral' {
  const values: Record<TestType, 'success' | 'warning' | 'error' | 'info' | 'neutral'> = {
    PRE: 'info',
    POST: 'success',
    UNKNOWN: 'neutral',
  }
  return values[type] ?? 'neutral'
}
function query(): TestQuery {
  return {
    employeeKeyword: filters.employeeKeyword || undefined,
    keyword: filters.keyword || undefined,
    testType: filters.testType || undefined,
    startDateFrom: filters.dateRange[0],
    startDateTo: filters.dateRange[1],
    page: filters.page,
    pageSize: filters.pageSize,
    sortBy: 'testDate',
    sortDirection: 'desc',
  }
}
async function load() {
  loading.value = true
  error.value = null
  try {
    const result = await (await getTestService()).listManage(query())
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
          message: '测试成绩列表加载失败。',
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
      testType: filters.testType,
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
    <PageHeader title="测试成绩" description="查询员工训前、训后的测试成绩记录。" /><SearchPanel
      :expanded="true"
      :searching="loading"
      @search="sync({}, true)"
      @reset="
        sync(
          { employeeKeyword: '', keyword: '', testType: '', dateRange: [], page: 1, pageSize: 20 },
          true,
        )
      "
      ><el-input
        v-model="filters.employeeKeyword"
        clearable
        aria-label="员工"
        placeholder="员工姓名或工号" /><el-input
        v-model="filters.keyword"
        clearable
        aria-label="课程关键词"
        placeholder="课程名称" /><el-select
        v-model="filters.testType"
        clearable
        aria-label="测试类型"
        placeholder="测试类型"
        ><el-option label="训前测试" value="PRE" /><el-option
          label="训后测试"
          value="POST" /></el-select
      ><template #expanded
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
      title="暂无测试成绩"
      description="当前没有已录入的测试成绩记录。"
      compact
    /><PageState
      v-else-if="state === 'no-result'"
      state="no-result"
      secondary-label="清空筛选"
      compact
      @secondary="
        sync({ employeeKeyword: '', keyword: '', testType: '', dateRange: [], page: 1 }, true)
      "
    /><DataTable v-else :columns="columns" :rows="tableRows"
      ><template #cell="{ column, row }"
        ><StatusTag
          v-if="column.key === 'testTypeLabel'"
          :label="String(row.testTypeLabel)"
          :semantic="testTypeSemantic(row.testType as TestType)"
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
