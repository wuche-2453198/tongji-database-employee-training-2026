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
import { getRatingService } from '@/services/rating'
import { isServiceError, type UiError } from '@/types/api'
import type { CourseRating, RatingQuery, RatingVerifiedStatus } from '@/domains/rating'
import type { TableColumn } from '@/types/ui'
import { compactListQuery } from '@/utils/list-route-state'

const router = useRouter()
const filters = reactive({
  keyword: '',
  dateRange: [] as string[],
  page: 1,
  pageSize: 20,
})
const rows = ref<CourseRating[]>([])
const total = ref(0)
const loading = ref(false)
const hasLoaded = ref(false)
const error = ref<UiError | null>(null)
const columns: TableColumn[] = [
  { key: 'courseName', label: '课程名称', minWidth: 220 },
  { key: 'trainerName', label: '讲师', width: 140 },
  { key: 'scoreLabel', label: '评分', width: 190 },
  { key: 'comment', label: '评价内容', minWidth: 240 },
  { key: 'hrVerifiedLabel', label: '复核状态', width: 120, align: 'center' },
  { key: 'ratingDateLabel', label: '评分时间', width: 170 },
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
    comment: item.comment || '—',
    hrVerifiedLabel: verifiedLabel(item.hrVerified),
    ratingDateLabel: formatDate(item.ratingDate),
  })),
)
function formatDate(value: string) {
  const date = new Date(value)
  return Number.isNaN(date.getTime())
    ? '—'
    : new Intl.DateTimeFormat('zh-CN', { dateStyle: 'short', timeStyle: 'short' }).format(date)
}
function verifiedLabel(status: RatingVerifiedStatus) {
  return { N: '未复核', Y: '已复核', UNKNOWN: '未知' }[status] ?? '未知'
}
function verifiedSemantic(
  status: RatingVerifiedStatus,
): 'success' | 'warning' | 'error' | 'info' | 'neutral' {
  const values: Record<RatingVerifiedStatus, 'success' | 'warning' | 'error' | 'info' | 'neutral'> =
    {
      N: 'neutral',
      Y: 'success',
      UNKNOWN: 'neutral',
    }
  return values[status] ?? 'neutral'
}
function query(): RatingQuery {
  return {
    keyword: filters.keyword || undefined,
    startDateFrom: filters.dateRange[0],
    startDateTo: filters.dateRange[1],
    page: filters.page,
    pageSize: filters.pageSize,
    sortBy: 'ratingDate',
    sortDirection: 'desc',
  }
}
async function load() {
  loading.value = true
  error.value = null
  try {
    const result = await (await getRatingService()).listMine(query())
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
          message: '评分列表加载失败。',
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
    <PageHeader title="我的评分" description="查看本人对已完成课程讲师的评分与评价。"
      ><template #action
        ><AppButton
          label="浏览课程"
          variant="primary"
          @click="router.push({ name: 'course-list' })" /></template></PageHeader
    ><SearchPanel
      :expanded="false"
      :searching="loading"
      @search="sync({}, true)"
      @reset="sync({ keyword: '', dateRange: [], page: 1, pageSize: 20 }, true)"
      ><el-input
        v-model="filters.keyword"
        clearable
        aria-label="课程或讲师"
        placeholder="课程名称或讲师" /><template #expanded
        ><el-date-picker
          v-model="filters.dateRange"
          type="daterange"
          value-format="YYYY-MM-DD"
          range-separator="至"
          start-placeholder="评分开始日期"
          end-placeholder="评分结束日期"
          aria-label="评分日期" /></template></SearchPanel
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
      title="暂无评分记录"
      description="完成课程后，可以在这里查看你提交的评分与评价。"
      secondary-label="浏览课程"
      compact
      @secondary="router.push({ name: 'course-list' })"
    /><PageState
      v-else-if="state === 'no-result'"
      state="no-result"
      secondary-label="清空筛选"
      compact
      @secondary="sync({ keyword: '', dateRange: [], page: 1 }, true)"
    /><DataTable v-else :columns="columns" :rows="tableRows"
      ><template #cell="{ column, row }"
        ><span v-if="column.key === 'scoreLabel'" class="business-list__score"
          ><el-rate :model-value="Number(row.score)" disabled allow-half /><span
            >{{ Number(row.score).toFixed(1) }} / 5</span
          ></span
        ><StatusTag
          v-else-if="column.key === 'hrVerifiedLabel'"
          :label="String(row.hrVerifiedLabel)"
          :semantic="verifiedSemantic(row.hrVerified as RatingVerifiedStatus)"
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
.business-list__score {
  display: flex;
  align-items: center;
  gap: var(--space-2);
  color: var(--text-secondary);
  font-size: var(--font-size-caption);
}
</style>
