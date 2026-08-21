<script setup lang="ts">
import { ref, reactive, onMounted, computed, onBeforeUnmount } from 'vue'
import { useRouter, useRoute } from 'vue-router'
import { useAuthStore } from '@/stores/auth'
import { getCourseListApi } from '@/api/course'
import type { CourseListItem, CourseQuery } from '@/types/course'
import type { CourseStatus } from '@/types/enums'
import { COURSE_STATUS_LABELS } from '@/types/enums'
import { formatDateTime, formatCurrency, formatDuration } from '@/utils/format'
import StatusTag from '@/components/common/StatusTag.vue'
import PageHeader from '@/components/common/PageHeader.vue'
import FilterCard from '@/components/common/FilterCard.vue'
import AsyncState from '@/components/common/AsyncState.vue'
import CourseCover from '@/components/common/CourseCover.vue'

const router = useRouter()
const route = useRoute()
const auth = useAuthStore()

// 普通员工：只看到已发布课程；其余角色（主管/HR/管理员）可查看全部状态
const isEmployee = computed(() => auth.hasRole(['EMPLOYEE']) && !auth.hasRole(['ADMIN', 'HR', 'DEPT_MANAGER']))
const canManageStatus = computed(() => !isEmployee.value)

const query = reactive<CourseQuery>({
  keyword: '',
  courseType: undefined,
  status: undefined,
  startDate: undefined,
  endDate: undefined,
  page: 1,
  pageSize: 12,
})

const dateRange = ref<[string, string] | null>(null)

const courseList = ref<CourseListItem[]>([])
const total = ref(0)
const loading = ref(false)
const error = ref(false)
const errorMessage = ref('')

const statusOptions: { label: string; value: CourseStatus }[] = [
  { label: '已发布', value: 'PUBLISHED' },
  { label: '草稿', value: 'DRAFT' },
  { label: '已关闭', value: 'CLOSED' },
]

const courseTypeOptions = [
  { label: '技术培训', value: '技术培训' },
  { label: '管理培训', value: '管理培训' },
  { label: '产品培训', value: '产品培训' },
  { label: '营销培训', value: '营销培训' },
]

// 展开区域中已生效的筛选条件数量（用于折叠时提示）
const activeFilterCount = computed(() => (dateRange.value && dateRange.value.length === 2 ? 1 : 0))

function buildQuery(): CourseQuery {
  return {
    keyword: query.keyword || undefined,
    courseType: query.courseType,
    // 普通员工强制只看已发布（前端防御，最终以后端权限校验为准）
    status: isEmployee.value ? 'PUBLISHED' : query.status,
    startDate: dateRange.value?.[0],
    endDate: dateRange.value?.[1],
    page: query.page,
    pageSize: query.pageSize,
  }
}

// 请求序号：防止快速筛选时旧请求覆盖新结果
let requestSeq = 0

async function fetchCourses() {
  const seq = ++requestSeq
  loading.value = true
  error.value = false

  const res = await getCourseListApi(buildQuery())
  if (seq !== requestSeq) return

  loading.value = false

  if (res.success && res.data) {
    courseList.value = res.data.items
    total.value = res.data.total
  } else {
    error.value = true
    errorMessage.value = res.message || '加载失败'
  }
}

// 关键词输入 300ms 防抖
let keywordTimer: number | undefined

function onKeywordInput() {
  if (keywordTimer) window.clearTimeout(keywordTimer)
  keywordTimer = window.setTimeout(() => handleSearch(), 300)
}

function syncUrl() {
  const params: Record<string, string> = {}
  if (query.keyword) params.keyword = query.keyword
  if (query.courseType) params.courseType = query.courseType
  if (canManageStatus.value && query.status) params.status = query.status
  if (dateRange.value?.[0]) params.startDate = dateRange.value[0]
  if (dateRange.value?.[1]) params.endDate = dateRange.value[1]
  if (query.page && query.page !== 1) params.page = String(query.page)
  router.replace({ query: params })
}

function handleSearch() {
  if (keywordTimer) {
    window.clearTimeout(keywordTimer)
    keywordTimer = undefined
  }
  query.page = 1
  syncUrl()
  fetchCourses()
}

function handleReset() {
  if (keywordTimer) {
    window.clearTimeout(keywordTimer)
    keywordTimer = undefined
  }
  query.keyword = ''
  query.courseType = undefined
  query.status = undefined
  dateRange.value = null
  query.page = 1
  syncUrl()
  fetchCourses()
}

function handlePageChange(page: number) {
  query.page = page
  syncUrl()
  fetchCourses()
}

function goToDetail(courseId: number) {
  router.push(`/courses/${courseId}`)
}

/** 后端暂未返回 enrolledCount，缺失时返回 null，前端不伪造为 0 */
function remainingSlots(course: CourseListItem): number | null {
  if (course.enrolledCount === undefined || course.enrolledCount === null) return null
  return course.maxStudents - course.enrolledCount
}

/** 从 URL 恢复筛选状态（刷新页面后保留） */
function readInitialQuery() {
  const q = route.query
  query.keyword = typeof q.keyword === 'string' ? q.keyword : ''
  query.courseType = typeof q.courseType === 'string' ? q.courseType : undefined
  if (typeof q.status === 'string') {
    query.status = q.status as CourseStatus
  }
  query.page = q.page ? Math.max(1, Number(q.page) || 1) : 1
  if (typeof q.startDate === 'string' && typeof q.endDate === 'string') {
    dateRange.value = [q.startDate, q.endDate]
  }
}

onMounted(() => {
  readInitialQuery()
  fetchCourses()
})

onBeforeUnmount(() => {
  if (keywordTimer) window.clearTimeout(keywordTimer)
})
</script>

<template>
  <div class="page-container page-container--wide course-list-page">
    <PageHeader title="课程大厅" description="浏览培训课程，申请参加心仪课程" />

    <!-- 筛选区 -->
    <FilterCard :active-count="activeFilterCount" @search="handleSearch" @reset="handleReset">
      <el-form-item label="课程名称">
        <el-input
          v-model="query.keyword"
          placeholder="搜索课程或讲师"
          clearable
          style="width: 260px"
          @input="onKeywordInput"
          @keyup.enter="handleSearch"
        />
      </el-form-item>
      <el-form-item label="课程分类">
        <el-select
          v-model="query.courseType"
          placeholder="全部"
          clearable
          style="width: 140px"
          @change="handleSearch"
        >
          <el-option
            v-for="opt in courseTypeOptions"
            :key="opt.value"
            :label="opt.label"
            :value="opt.value"
          />
        </el-select>
      </el-form-item>
      <el-form-item v-if="canManageStatus" label="课程状态">
        <el-select
          v-model="query.status"
          placeholder="全部"
          clearable
          style="width: 140px"
          @change="handleSearch"
        >
          <el-option
            v-for="opt in statusOptions"
            :key="opt.value"
            :label="opt.label"
            :value="opt.value"
          />
        </el-select>
      </el-form-item>

      <template #extra>
        <el-form inline>
          <el-form-item label="开课日期">
            <el-date-picker
              v-model="dateRange"
              type="daterange"
              range-separator="至"
              start-placeholder="开始日期"
              end-placeholder="结束日期"
              value-format="YYYY-MM-DD"
              style="width: 260px"
              @change="handleSearch"
            />
          </el-form-item>
        </el-form>
      </template>
    </FilterCard>

    <!-- 状态区 -->
    <AsyncState
      :loading="loading"
      :error="error"
      :error-message="errorMessage"
      :empty="courseList.length === 0"
      empty-description="暂无课程数据"
      @retry="fetchCourses"
    >
      <!-- 结果计数 -->
      <div class="course-result-count">
        共找到 <span class="course-result-count__num">{{ total }}</span> 门课程
      </div>

      <!-- 课程卡片网格 -->
      <div class="course-grid">
        <div
          v-for="course in courseList"
          :key="course.courseId"
          class="course-card"
          @click="goToDetail(course.courseId)"
        >
          <!-- 封面 -->
          <CourseCover :course-id="course.courseId" :course-type="course.courseType" alt="" />

          <div class="course-card__body">
            <!-- 顶部标签行 -->
            <div class="card-top">
              <StatusTag :type="course.courseStatus" :label-map="COURSE_STATUS_LABELS" />
              <span class="course-type">{{ course.courseType }}</span>
              <!-- 剩余名额提示（仅当后端返回已报名人数时展示） -->
              <span
                v-if="course.courseStatus === 'PUBLISHED' && remainingSlots(course) !== null"
                class="slots-badge"
                :class="{
                  'slots-warning': remainingSlots(course)! > 0 && remainingSlots(course)! <= 3,
                  'slots-full': remainingSlots(course)! <= 0,
                }"
              >
                {{ remainingSlots(course)! <= 0 ? '名额已满' : `剩余 ${remainingSlots(course)} 人` }}
              </span>
            </div>

            <!-- 课程名称 -->
            <h3 class="course-name" :title="course.courseName">{{ course.courseName }}</h3>

            <!-- 信息行 -->
            <div class="card-meta">
              <div class="meta-item" v-if="course.trainerName">
                <span class="meta-label">讲师</span>
                <span class="meta-value">{{ course.trainerName }}</span>
              </div>
              <div class="meta-item" v-if="course.startAt">
                <span class="meta-label">时间</span>
                <span class="meta-value">
                  <el-tooltip :content="formatDateTime(course.startAt)" placement="top">
                    <span>{{ formatDateTime(course.startAt) }}</span>
                  </el-tooltip>
                </span>
              </div>
              <div class="meta-item" v-if="course.location">
                <span class="meta-label">地点</span>
                <span class="meta-value">
                  <el-tooltip :content="course.location" placement="top">
                    <span class="text-ellipsis">{{ course.location }}</span>
                  </el-tooltip>
                </span>
              </div>
              <div class="meta-item">
                <span class="meta-label">时长</span>
                <span class="meta-value">{{ formatDuration(course.durationHours) }}</span>
              </div>
              <div v-if="canManageStatus" class="meta-item">
                <span class="meta-label">预算</span>
                <span class="meta-value">{{ formatCurrency(course.budgetAmount) }}</span>
              </div>
            </div>

            <!-- 底部 -->
            <div class="card-footer">
              <div class="capacity-info">
                <template v-if="course.enrolledCount !== undefined && course.enrolledCount !== null">
                  已报名 {{ course.enrolledCount }} / {{ course.maxStudents }}
                </template>
                <template v-else>
                  名额上限 {{ course.maxStudents }} 人
                </template>
              </div>
              <el-button
                v-if="course.courseStatus === 'PUBLISHED' && (remainingSlots(course) === null || remainingSlots(course)! > 0)"
                type="primary"
                size="small"
                @click.stop="goToDetail(course.courseId)"
              >
                查看详情
              </el-button>
              <el-button
                v-else-if="course.courseStatus === 'PUBLISHED' && remainingSlots(course)! <= 0"
                size="small"
                disabled
              >
                名额已满
              </el-button>
              <el-button
                v-else
                size="small"
                disabled
              >
                {{ course.courseStatus === 'DRAFT' ? '未发布' : '已结束' }}
              </el-button>
            </div>
          </div>
        </div>
      </div>

      <!-- 分页 -->
      <div v-if="total > (query.pageSize ?? 12)" class="pagination-wrapper">
        <el-pagination
          :current-page="query.page"
          :page-size="query.pageSize"
          :total="total"
          layout="total, prev, pager, next"
          @current-change="handlePageChange"
        />
      </div>
    </AsyncState>
  </div>
</template>

<style lang="scss" scoped>
.course-result-count {
  font-size: 13px;
  color: var(--color-text-secondary);
  margin-bottom: var(--space-md);

  &__num {
    font-weight: 600;
    color: var(--color-text-primary);
  }
}

.course-grid {
  display: grid;
  grid-template-columns: repeat(auto-fill, minmax(360px, 1fr));
  gap: 16px;
}

.course-card {
  background: var(--color-bg-card);
  border: 1px solid var(--color-border);
  border-radius: var(--radius-md);
  overflow: hidden;
  cursor: pointer;
  transition: box-shadow 0.15s, border-color 0.15s, transform 0.15s;
  display: flex;
  flex-direction: column;

  &:hover {
    border-color: var(--color-primary-light);
    box-shadow: var(--shadow-md);
    transform: translateY(-2px);
  }

  &__body {
    padding: 18px 20px 20px;
    display: flex;
    flex-direction: column;
    flex: 1;
  }
}

.card-top {
  display: flex;
  align-items: center;
  gap: 8px;
  margin-bottom: 12px;
}

.course-type {
  font-size: 12px;
  color: var(--color-text-secondary);
  background: var(--color-bg);
  padding: 2px 8px;
  border-radius: var(--radius-sm);
}

.slots-badge {
  margin-left: auto;
  font-size: 12px;
  padding: 2px 8px;
  border-radius: var(--radius-sm);
  background: var(--color-surface-subtle);
  color: var(--color-text-secondary);

  &.slots-warning {
    background: var(--color-warning-bg);
    color: var(--color-warning);
  }

  &.slots-full {
    background: var(--color-danger-bg);
    color: var(--color-danger);
  }
}

.course-name {
  font-size: 17px;
  font-weight: 600;
  color: var(--color-text-primary);
  margin-bottom: 12px;
  line-height: 1.4;
  display: -webkit-box;
  -webkit-line-clamp: 2;
  -webkit-box-orient: vertical;
  overflow: hidden;
}

.card-meta {
  display: flex;
  flex-direction: column;
  gap: 6px;
  flex: 1;
}

.meta-item {
  display: flex;
  gap: 8px;
  font-size: 13px;
  line-height: 1.4;

  .meta-label {
    color: var(--color-text-placeholder);
    flex-shrink: 0;
    min-width: 32px;
  }

  .meta-value {
    color: var(--color-text-regular);
    min-width: 0;

    .text-ellipsis {
      display: block;
      max-width: 240px;
      overflow: hidden;
      text-overflow: ellipsis;
      white-space: nowrap;
    }
  }
}

.card-footer {
  display: flex;
  align-items: center;
  justify-content: space-between;
  margin-top: 14px;
  padding-top: 14px;
  border-top: 1px solid var(--color-divider);
}

.capacity-info {
  font-size: 12px;
  color: var(--color-text-secondary);
}

.pagination-wrapper {
  display: flex;
  justify-content: center;
  margin-top: var(--space-lg);
}
</style>
