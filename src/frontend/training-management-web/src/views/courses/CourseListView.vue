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
import { getCourseService } from '@/services/course'
import { isServiceError, type UiError } from '@/types/api'
import type { CourseQuery, CourseStatus, CourseSummary, CourseType } from '@/domains/course'
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
  type: CourseType | ''
  status: CourseStatus | ''
  dateRange: string[]
  page: number
  pageSize: number
}>({ keyword: '', type: '', status: 'PUBLISHED', dateRange: [], page: 1, pageSize: 20 })

const loading = ref(false)
const searchExpanded = ref(true)
const rows = ref<CourseSummary[]>([])
const total = ref(0)
const error = ref<UiError | null>(null)
const hasLoaded = ref(false)

const columns: TableColumn[] = [
  { key: 'name', label: '课程名称', minWidth: 240 },
  { key: 'typeLabel', label: '课程类型', width: 120 },
  { key: 'trainerName', label: '讲师', width: 100 },
  { key: 'startTimeLabel', label: '开课时间', width: 170 },
  { key: 'location', label: '地点', minWidth: 170 },
  { key: 'remainingSeatsLabel', label: '剩余名额', width: 110, align: 'right' },
  { key: 'statusLabel', label: '状态', width: 100, align: 'center' },
]

const tableRows = computed<Record<string, unknown>[]>(() =>
  rows.value.map((course) => ({
    ...course,
    startTimeLabel: formatDateTime(course.startTime),
    remainingSeatsLabel: `${course.remainingSeats} / ${course.maxStudents}`,
  })),
)

const hasActiveFilter = computed(() =>
  Boolean(
    filters.keyword || filters.type || filters.status !== 'PUBLISHED' || filters.dateRange.length,
  ),
)
const state = computed<'default' | 'loading' | 'empty' | 'error'>(() => {
  if (loading.value && !hasLoaded.value) return 'loading'
  if (error.value) return 'error'
  if (!loading.value && rows.value.length === 0) return 'empty'
  return loading.value ? 'loading' : 'default'
})

function formatDateTime(value: string): string {
  const date = new Date(value)
  if (Number.isNaN(date.getTime())) return '—'
  return new Intl.DateTimeFormat('zh-CN', { dateStyle: 'short', timeStyle: 'short' }).format(date)
}

function statusSemantic(status: CourseStatus): 'success' | 'warning' | 'error' | 'neutral' {
  if (status === 'PUBLISHED') return 'success'
  if (status === 'CLOSED') return 'neutral'
  if (status === 'DRAFT') return 'warning'
  return 'error'
}

function applyRouteQuery(): void {
  filters.keyword = readQueryString(route.query.keyword)
  filters.type = readQueryString(route.query.type) as CourseType | ''
  filters.status = (readQueryString(route.query.status, 'PUBLISHED') || 'PUBLISHED') as CourseStatus
  filters.dateRange = [
    readQueryString(route.query.startDateFrom),
    readQueryString(route.query.startDateTo),
  ].filter(Boolean)
  filters.page = readPositiveQueryInteger(route.query.page, 1)
  filters.pageSize = [10, 20, 50].includes(readPositiveQueryInteger(route.query.pageSize, 20))
    ? readPositiveQueryInteger(route.query.pageSize, 20)
    : 20
}

function buildQuery(): CourseQuery {
  return {
    keyword: filters.keyword || undefined,
    type: filters.type || undefined,
    status: filters.status || undefined,
    startDateFrom: filters.dateRange[0] || undefined,
    startDateTo: filters.dateRange[1] || undefined,
    page: filters.page,
    pageSize: filters.pageSize,
    sortBy: 'startTime',
    sortDirection: 'desc',
  }
}

async function loadCourses(): Promise<void> {
  loading.value = true
  error.value = null
  try {
    const service = await getCourseService()
    const result = await latestQuery.run((signal) => service.listCourses(buildQuery(), { signal }))
    rows.value = result.items
    total.value = result.total
    hasLoaded.value = true
  } catch (caught) {
    if (isServiceError(caught) && caught.ui.kind === 'canceled') return
    rows.value = []
    total.value = 0
    hasLoaded.value = true
    error.value = isServiceError(caught) ? caught.ui : null
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
      type: filters.type,
      status: filters.status === 'PUBLISHED' ? '' : filters.status,
      startDateFrom: filters.dateRange[0],
      startDateTo: filters.dateRange[1],
      page: filters.page === 1 ? '' : filters.page,
      pageSize: filters.pageSize === 20 ? '' : filters.pageSize,
      sort: 'startTime.desc',
    }),
  })
  await loadCourses()
}

function resetFilters(): void {
  void updateUrlAndLoad(
    { keyword: '', type: '', status: 'PUBLISHED', dateRange: [], page: 1, pageSize: 20 },
    true,
  )
}

function search(): void {
  void updateUrlAndLoad({}, true)
}
function changePage(page: number): void {
  void updateUrlAndLoad({ page })
}
function changePageSize(pageSize: number): void {
  void updateUrlAndLoad({ pageSize }, true)
}
function openDetail(course: CourseSummary): void {
  void router.push({ name: 'course-detail', params: { id: course.id } })
}

watch(
  () => route.query,
  async () => {
    applyRouteQuery()
    await loadCourses()
  },
  { deep: true },
)

onMounted(() => {
  applyRouteQuery()
  void loadCourses()
})
onBeforeUnmount(() => latestQuery.cancel())
</script>

<template>
  <section class="course-list-view">
    <PageHeader
      title="课程中心"
      description="浏览已发布课程，查看培训安排与报名资格。"
      :breadcrumbs="['培训中心', '课程中心']"
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
        aria-label="课程关键词"
        placeholder="请输入课程名称"
      />
      <el-select v-model="filters.type" clearable aria-label="课程类型" placeholder="课程类型">
        <el-option label="技能培训" value="SKILL" />
        <el-option label="管理培训" value="MANAGEMENT" />
        <el-option label="安全培训" value="SAFETY" />
      </el-select>
      <el-select v-model="filters.status" clearable aria-label="课程状态" placeholder="课程状态">
        <el-option label="已发布" value="PUBLISHED" />
        <el-option label="草稿" value="DRAFT" />
        <el-option label="已关闭" value="CLOSED" />
      </el-select>
      <template #expanded>
        <el-date-picker
          v-model="filters.dateRange"
          type="daterange"
          value-format="YYYY-MM-DD"
          range-separator="至"
          start-placeholder="开始日期"
          end-placeholder="结束日期"
          aria-label="开课日期范围"
        />
      </template>
    </SearchPanel>

    <div class="course-list-view__summary" aria-live="polite">
      <strong>课程列表</strong>
      <span v-if="state === 'default' || state === 'loading'">共 {{ total }} 门课程</span>
      <span>按开课时间降序</span>
    </div>

    <PageState v-if="state === 'loading'" state="loading" />
    <PageState
      v-else-if="error?.kind === 'forbidden'"
      state="forbidden"
      :description="error.message"
    />
    <PageState
      v-else-if="error?.kind === 'not-found'"
      state="not-found"
      :description="error.message"
    />
    <PageState
      v-else-if="error"
      state="error"
      :description="error.message"
      :trace-id="error.traceId"
      @primary="loadCourses"
    />
    <PageState
      v-else-if="state === 'empty' && !hasActiveFilter"
      state="empty"
      title="暂无已发布课程"
      description="当前没有可浏览的已发布课程。"
      compact
    />
    <PageState
      v-else-if="state === 'empty' && hasActiveFilter"
      state="no-result"
      title="未找到匹配课程"
      description="请调整关键词、类型、状态或日期范围。"
      secondary-label="清空筛选"
      compact
      @secondary="resetFilters"
    />
    <DataTable
      v-else
      :columns="columns"
      :rows="tableRows"
      state="default"
      has-actions
      empty-title="暂无已发布课程"
      empty-description="当前没有可报名的课程。"
      @retry="loadCourses"
    >
      <template #cell="{ column, row }">
        <StatusTag
          v-if="column.key === 'statusLabel'"
          :label="String(row.statusLabel)"
          :semantic="statusSemantic(row.status as CourseStatus)"
        />
        <button
          v-else-if="column.key === 'name'"
          type="button"
          class="course-list-view__link"
          @click="openDetail(row as unknown as CourseSummary)"
        >
          {{ row.name }}
        </button>
        <span v-else>{{ row[column.key] || '—' }}</span>
      </template>
      <template #actions="{ row }">
        <AppButton
          label="查看详情"
          variant="text"
          @click="openDetail(row as unknown as CourseSummary)"
        />
      </template>
    </DataTable>

    <AppPagination
      v-if="state === 'default' && total > 0"
      :page="filters.page"
      :page-size="filters.pageSize"
      :total="total"
      :disabled="loading"
      @update:page="changePage"
      @update:page-size="changePageSize"
    />
  </section>
</template>

<style scoped>
.course-list-view {
  display: grid;
  gap: var(--space-6);
}
.course-list-view__summary {
  display: flex;
  flex-wrap: wrap;
  gap: var(--space-4);
  align-items: center;
  color: var(--text-secondary);
}
.course-list-view__summary strong {
  color: var(--text-primary);
  font-size: var(--font-size-title-component);
}
.course-list-view__link {
  padding: 0;
  border: 0;
  color: var(--text-link);
  background: transparent;
  cursor: pointer;
  text-align: left;
}
.course-list-view__link:hover {
  text-decoration: underline;
}
.course-list-view :deep(.el-date-editor) {
  width: 100%;
}
@media (width <= 1100px) {
  .course-list-view :deep(.search-panel__fields) {
    grid-template-columns: repeat(2, minmax(0, 1fr));
  }
}
</style>
