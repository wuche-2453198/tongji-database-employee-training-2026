<script setup lang="ts">
import { computed, onBeforeUnmount, onMounted, reactive, ref, watch } from 'vue'
import { useRoute, useRouter } from 'vue-router'

import { LatestRequestController } from '@/api/request-control'
import AppButton from '@/components/common/AppButton.vue'
import AppPagination from '@/components/common/AppPagination.vue'
import ConfirmDialog from '@/components/common/ConfirmDialog.vue'
import CourseCard from '@/components/business/CourseCard.vue'
import CourseEditorDialog from '@/components/business/CourseEditorDialog.vue'
import PageHeader from '@/components/common/PageHeader.vue'
import PageState from '@/components/common/PageState.vue'
import SearchPanel from '@/components/common/SearchPanel.vue'
import { hasAnyRole } from '@/config/permissions'
import { getCourseService } from '@/services/course'
import { useAuthStore } from '@/stores/auth'
import { isServiceError, type UiError } from '@/types/api'
import type { CourseQuery, CourseStatus, CourseSummary, CourseType } from '@/domains/course'
import {
  compactListQuery,
  readPositiveQueryInteger,
  readQueryString,
} from '@/utils/list-route-state'

type CourseAction = 'publish' | 'close'

const route = useRoute()
const router = useRouter()
const authStore = useAuthStore()
const latestQuery = new LatestRequestController()

const canManageCourses = computed(() => hasAnyRole(authStore.currentUser, ['HR', 'ADMIN']))

const statusOptions = computed(() =>
  canManageCourses.value
    ? [
        { label: '全部', value: '' },
        { label: '草稿', value: 'DRAFT' },
        { label: '已发布', value: 'PUBLISHED' },
        { label: '已关闭', value: 'CLOSED' },
      ]
    : [{ label: '已发布', value: 'PUBLISHED' }],
)

const defaultStatus = computed<CourseStatus | ''>(() => (canManageCourses.value ? '' : 'PUBLISHED'))

function sanitizeStatus(value: string): CourseStatus | '' {
  if (canManageCourses.value) {
    return value === 'DRAFT' || value === 'PUBLISHED' || value === 'CLOSED'
      ? (value as CourseStatus)
      : ''
  }
  return 'PUBLISHED'
}

const filters = reactive<{
  keyword: string
  type: CourseType | ''
  status: CourseStatus | ''
  dateRange: string[]
  page: number
  pageSize: number
}>({
  keyword: '',
  type: '',
  status: defaultStatus.value,
  dateRange: [],
  page: 1,
  pageSize: 20,
})

const loading = ref(false)
const searchExpanded = ref(true)
const rows = ref<CourseSummary[]>([])
const total = ref(0)
const error = ref<UiError | null>(null)
const hasLoaded = ref(false)

const actionDialog = ref<{ action: CourseAction; course: CourseSummary } | null>(null)
const actionSubmitting = ref(false)
const actionError = ref<UiError | null>(null)
const editorVisible = ref(false)

const hasActiveFilter = computed(() =>
  Boolean(
    filters.keyword ||
    filters.type ||
    filters.status !== defaultStatus.value ||
    filters.dateRange.length,
  ),
)
const state = computed<'default' | 'loading' | 'empty' | 'error'>(() => {
  if (loading.value && !hasLoaded.value) return 'loading'
  if (error.value) return 'error'
  if (!loading.value && rows.value.length === 0) return 'empty'
  return loading.value ? 'loading' : 'default'
})

const actionDialogVisible = computed({
  get: () => actionDialog.value !== null,
  set: (visible: boolean) => {
    if (!visible) actionDialog.value = null
  },
})
const actionDialogTitle = computed(() =>
  actionDialog.value?.action === 'publish' ? '发布课程' : '关闭课程',
)
const actionDialogDescription = computed(() => {
  const target = actionDialog.value
  if (!target) return ''
  return target.action === 'publish'
    ? `确定发布课程《${target.course.name}》吗？发布后员工将可以查看并申请。`
    : `确定关闭课程《${target.course.name}》吗？关闭后员工将无法继续报名。`
})
const actionDialogConfirmLabel = computed(() =>
  actionDialog.value?.action === 'publish' ? '确认发布' : '确认关闭',
)

function applyRouteQuery(): void {
  filters.keyword = readQueryString(route.query.keyword)
  filters.type = readQueryString(route.query.type) as CourseType | ''
  filters.status = sanitizeStatus(readQueryString(route.query.status))
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
      status: filters.status === defaultStatus.value ? '' : filters.status,
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
    { keyword: '', type: '', status: defaultStatus.value, dateRange: [], page: 1, pageSize: 20 },
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

function openActionDialog(action: CourseAction, course: CourseSummary): void {
  actionError.value = null
  actionDialog.value = { action, course }
}

async function confirmAction(): Promise<void> {
  const target = actionDialog.value
  if (!target) return
  actionSubmitting.value = true
  actionError.value = null
  try {
    const service = await getCourseService()
    if (target.action === 'publish') await service.publishCourse(target.course.id)
    else await service.closeCourse(target.course.id)
    actionDialog.value = null
    await loadCourses()
  } catch (caught) {
    if (isServiceError(caught)) {
      if (caught.ui.kind === 'conflict') {
        actionDialog.value = null
        await loadCourses()
      } else {
        actionError.value = caught.ui
      }
    } else {
      actionError.value = {
        kind: 'unknown',
        code: 'UNKNOWN',
        message: '操作失败，请稍后重试。',
        fieldErrors: [],
        retryable: false,
        resultUnknown: false,
      }
    }
  } finally {
    actionSubmitting.value = false
  }
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
    <PageHeader title="课程中心" description="浏览已发布课程，查看培训安排与报名资格。">
      <template #action><AppButton v-if="canManageCourses" label="新建课程" variant="primary" @click="editorVisible = true" /></template>
    </PageHeader>

    <el-alert
      v-if="actionError"
      :title="actionError.message"
      type="error"
      show-icon
      :closable="false"
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
        <el-option label="技术培训" value="技术培训" />
        <el-option label="管理培训" value="管理培训" />
        <el-option label="产品培训" value="产品培训" />
        <el-option label="营销培训" value="营销培训" />
      </el-select>
      <el-select
        v-model="filters.status"
        :clearable="canManageCourses"
        aria-label="课程状态"
        placeholder="课程状态"
      >
        <el-option
          v-for="option in statusOptions"
          :key="option.value"
          :label="option.label"
          :value="option.value"
        />
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
      :title="canManageCourses ? '暂无课程' : '暂无已发布课程'"
      :description="canManageCourses ? '当前没有可浏览的课程。' : '当前没有可浏览的已发布课程。'"
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
    <div v-else class="course-list-view__grid">
      <div v-for="course in rows" :key="course.id" class="course-list-view__item">
        <CourseCard :course="course" @open="openDetail" />
        <div
          v-if="canManageCourses && (course.status === 'DRAFT' || course.status === 'PUBLISHED')"
          class="course-list-view__item-actions"
        >
          <AppButton
            v-if="course.status === 'DRAFT'"
            label="发布课程"
            variant="primary"
            @click="openActionDialog('publish', course)"
          />
          <AppButton
            v-if="course.status === 'PUBLISHED'"
            label="关闭课程"
            @click="openActionDialog('close', course)"
          />
        </div>
      </div>
    </div>

    <AppPagination
      v-if="state === 'default' && total > 0"
      :page="filters.page"
      :page-size="filters.pageSize"
      :total="total"
      :disabled="loading"
      @update:page="changePage"
      @update:page-size="changePageSize"
    />

    <ConfirmDialog
      v-model="actionDialogVisible"
      :title="actionDialogTitle"
      :description="actionDialogDescription"
      :confirm-label="actionDialogConfirmLabel"
      :type="actionDialog?.action === 'close' ? 'danger' : 'confirm'"
      :loading="actionSubmitting"
      :loading-text="actionDialog?.action === 'publish' ? '发布中…' : '关闭中…'"
      @confirm="confirmAction"
    />
    <CourseEditorDialog v-model="editorVisible" @saved="loadCourses" />
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
.course-list-view__grid {
  display: grid;
  grid-template-columns: repeat(auto-fill, minmax(280px, 1fr));
  gap: var(--space-6);
}
.course-list-view__item {
  display: grid;
  gap: var(--space-2);
}
.course-list-view__item-actions {
  display: flex;
  justify-content: flex-end;
  gap: var(--space-2);
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
