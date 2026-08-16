<script setup lang="ts">
import { ref, reactive, computed, onMounted } from 'vue'
import { useRouter } from 'vue-router'
import {
  getMyTrainingRequestsApi,
  getTrainingRequestDetailApi,
  withdrawTrainingRequestApi,
} from '@/api/training-request'
import type {
  TrainingRequestItem,
  TrainingRequestDetail,
  TrainingRequestQuery,
} from '@/types/training-request'
import { TRAINING_REQUEST_STATUS_LABELS, type TrainingRequestStatus } from '@/types/enums'
import { formatDateTime } from '@/utils/format'
import { ElMessage } from 'element-plus'
import StatusTag from '@/components/common/StatusTag.vue'
import PageHeader from '@/components/common/PageHeader.vue'
import FilterCard from '@/components/common/FilterCard.vue'
import AsyncState from '@/components/common/AsyncState.vue'

const router = useRouter()

const tabs: { label: string; value: string }[] = [
  { label: '全部', value: '' },
  { label: '待主管审批', value: 'PENDING' },
  { label: '待 HR 备案', value: 'DEPT_APPROVED' },
  { label: '已备案', value: 'HR_FILED' },
  { label: '已驳回', value: 'DEPT_REJECTED' },
]

const activeTab = ref<string | number>('')

const query = reactive<TrainingRequestQuery>({
  status: undefined,
  keyword: '',
  startDate: undefined,
  endDate: undefined,
  page: 1,
  pageSize: 10,
})

const dateRange = ref<[string, string] | null>(null)

const list = ref<TrainingRequestItem[]>([])
const total = ref(0)
const loading = ref(false)
const error = ref(false)
const errorMessage = ref('')

// 展开区已生效的筛选条件数量（用于折叠时提示）
const activeFilterCount = computed(() => (dateRange.value && dateRange.value.length === 2 ? 1 : 0))

function buildQuery(): TrainingRequestQuery {
  return {
    status: query.status,
    keyword: query.keyword || undefined,
    startDate: dateRange.value?.[0],
    endDate: dateRange.value?.[1],
    page: query.page,
    pageSize: query.pageSize,
  }
}

async function fetchList() {
  loading.value = true
  error.value = false

  const res = await getMyTrainingRequestsApi(buildQuery())
  loading.value = false

  if (res.success && res.data) {
    list.value = res.data.items
    total.value = res.data.total
  } else {
    error.value = true
    errorMessage.value = res.message || '加载失败'
  }
}

function handleTabChange(name: string | number) {
  query.status = typeof name === 'string' && name ? (name as TrainingRequestStatus) : undefined
  query.page = 1
  fetchList()
}

function handleSearch() {
  query.page = 1
  fetchList()
}

function handleReset() {
  query.keyword = ''
  dateRange.value = null
  query.page = 1
  fetchList()
}

function handlePageChange(page: number) {
  query.page = page
  fetchList()
}

function goToCourse(courseId: number) {
  router.push(`/courses/${courseId}`)
}

// 申请详情
const detailVisible = ref(false)
const detail = ref<TrainingRequestDetail | null>(null)
const detailLoading = ref(false)

async function openDetail(item: TrainingRequestItem) {
  detailVisible.value = true
  detailLoading.value = true
  detail.value = null

  const res = await getTrainingRequestDetailApi(item.requestId)
  detailLoading.value = false

  if (res.success && res.data) {
    detail.value = res.data
  } else {
    ElMessage.error(res.message || '加载详情失败')
    detailVisible.value = false
  }
}

// 撤回申请
const withdrawingId = ref<number | null>(null)

async function handleWithdraw(item: TrainingRequestItem) {
  if (withdrawingId.value !== null) return

  withdrawingId.value = item.requestId
  const res = await withdrawTrainingRequestApi(item.requestId)
  withdrawingId.value = null

  if (res.success) {
    ElMessage.success('撤回成功')
    fetchList()
  } else {
    ElMessage.error(res.message || '撤回失败')
  }
}

onMounted(fetchList)
</script>

<template>
  <div class="page-container my-requests-page">
    <PageHeader title="我的申请" description="查看培训申请和审批进度" />

    <!-- 状态页签 -->
    <el-tabs v-model="activeTab" class="status-tabs" @tab-change="handleTabChange">
      <el-tab-pane
        v-for="tab in tabs"
        :key="tab.value"
        :label="tab.label"
        :name="tab.value"
      />
    </el-tabs>

    <!-- 筛选区 -->
    <FilterCard :active-count="activeFilterCount" @search="handleSearch" @reset="handleReset">
      <el-form-item label="课程名称">
        <el-input
          v-model="query.keyword"
          placeholder="搜索课程名称"
          clearable
          style="width: 220px"
          @keyup.enter="handleSearch"
        />
      </el-form-item>
      <el-form-item label="申请时间">
        <el-date-picker
          v-model="dateRange"
          type="daterange"
          range-separator="至"
          start-placeholder="开始日期"
          end-placeholder="结束日期"
          value-format="YYYY-MM-DD"
          style="width: 260px"
        />
      </el-form-item>
    </FilterCard>

    <!-- 状态区 -->
    <AsyncState
      :loading="loading"
      :error="error"
      :error-message="errorMessage"
      :empty="list.length === 0"
      empty-description="暂无申请记录"
      @retry="fetchList"
    >
      <template #empty-actions>
        <el-button type="primary" @click="router.push('/courses')">浏览课程</el-button>
      </template>

      <el-table :data="list" row-key="requestId" style="width: 100%">
        <el-table-column label="课程" min-width="220">
          <template #default="{ row }">
            <el-link type="primary" :underline="false" @click="goToCourse(row.courseId)">
              {{ row.courseName }}
            </el-link>
          </template>
        </el-table-column>
        <el-table-column label="申请时间" width="170">
          <template #default="{ row }">{{ formatDateTime(row.createdAt) }}</template>
        </el-table-column>
        <el-table-column label="状态" width="130">
          <template #default="{ row }">
            <StatusTag :type="row.status" :label-map="TRAINING_REQUEST_STATUS_LABELS" />
          </template>
        </el-table-column>
        <el-table-column label="最近处理时间" width="170">
          <template #default="{ row }">{{ formatDateTime(row.updatedAt) }}</template>
        </el-table-column>
        <el-table-column label="操作" width="150" fixed="right">
          <template #default="{ row }">
            <el-button text type="primary" size="small" @click="openDetail(row)">查看进度</el-button>
            <el-popconfirm
              v-if="row.status === 'PENDING'"
              title="确定撤回该申请吗？"
              confirm-button-text="撤回"
              cancel-button-text="取消"
              @confirm="handleWithdraw(row)"
            >
              <template #reference>
                <el-button text type="danger" size="small" :disabled="withdrawingId !== null">撤回</el-button>
              </template>
            </el-popconfirm>
          </template>
        </el-table-column>
      </el-table>

      <!-- 分页 -->
      <div v-if="total > (query.pageSize ?? 10)" class="pagination-wrapper">
        <el-pagination
          :current-page="query.page"
          :page-size="query.pageSize"
          :total="total"
          layout="total, prev, pager, next"
          @current-change="handlePageChange"
        />
      </div>
    </AsyncState>

    <!-- 申请详情 Drawer -->
    <el-drawer v-model="detailVisible" title="申请详情" size="480px">
      <div v-loading="detailLoading" style="min-height: 200px">
        <template v-if="detail">
          <!-- 驳回原因 -->
          <el-alert
            v-if="detail.status === 'DEPT_REJECTED' && detail.reviewComment"
            :title="`驳回原因：${detail.reviewComment}`"
            type="error"
            show-icon
            :closable="false"
            class="reject-alert"
          />

          <el-descriptions :column="1" border size="small" class="detail-desc">
            <el-descriptions-item label="课程">
              {{ detail.courseName }}
            </el-descriptions-item>
            <el-descriptions-item label="申请时间">
              {{ formatDateTime(detail.createdAt) }}
            </el-descriptions-item>
            <el-descriptions-item label="当前状态">
              <StatusTag :type="detail.status" :label-map="TRAINING_REQUEST_STATUS_LABELS" />
            </el-descriptions-item>
            <el-descriptions-item label="申请理由">
              {{ detail.reason }}
            </el-descriptions-item>
            <el-descriptions-item v-if="detail.expectedGain" label="预期收益">
              {{ detail.expectedGain }}
            </el-descriptions-item>
            <el-descriptions-item v-if="detail.reviewerName" label="主管审批人">
              {{ detail.reviewerName }}
            </el-descriptions-item>
            <el-descriptions-item v-if="detail.reviewComment" label="主管意见">
              {{ detail.reviewComment }}
            </el-descriptions-item>
            <el-descriptions-item v-if="detail.filedByName" label="HR 备案人">
              {{ detail.filedByName }}
            </el-descriptions-item>
            <el-descriptions-item v-if="detail.filingComment" label="HR 意见">
              {{ detail.filingComment }}
            </el-descriptions-item>
          </el-descriptions>

          <h4 class="detail-section-title">处理时间线</h4>
          <el-timeline>
            <el-timeline-item
              v-for="(entry, idx) in detail.timeline"
              :key="idx"
              :timestamp="formatDateTime(entry.time)"
              :type="entry.type"
            >
              <div class="timeline-title">{{ entry.title }}</div>
              <div v-if="entry.description" class="timeline-desc">{{ entry.description }}</div>
            </el-timeline-item>
          </el-timeline>
        </template>
      </div>
    </el-drawer>
  </div>
</template>

<style lang="scss" scoped>
.status-tabs {
  margin-bottom: var(--space-md);
}

.pagination-wrapper {
  display: flex;
  justify-content: center;
  margin-top: var(--space-lg);
}

.reject-alert {
  margin-bottom: var(--space-md);
}

.detail-desc {
  margin-bottom: var(--space-lg);
}

.detail-section-title {
  font-size: 14px;
  font-weight: 600;
  color: var(--color-text-primary);
  margin-bottom: 12px;
}

.timeline-title {
  font-size: 13px;
  font-weight: 500;
  color: var(--color-text-primary);
}

.timeline-desc {
  margin-top: 4px;
  font-size: 12px;
  color: var(--color-text-secondary);
}
</style>
