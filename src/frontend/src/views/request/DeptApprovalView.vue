<script setup lang="ts">
import { ref, reactive, onMounted } from 'vue'
import {
  getAllTrainingRequestsApi,
  approveTrainingRequestApi,
  rejectTrainingRequestApi,
} from '@/api/training-request'
import type {
  TrainingRequestApprovalItem,
  TrainingRequestApprovalQuery,
} from '@/types/training-request'
import { TRAINING_REQUEST_STATUS_LABELS, type TrainingRequestStatus } from '@/types/enums'
import { formatDateTime } from '@/utils/format'
import { ElMessage } from 'element-plus'
import StatusTag from '@/components/common/StatusTag.vue'
import PageHeader from '@/components/common/PageHeader.vue'
import AsyncState from '@/components/common/AsyncState.vue'

const tabs: { label: string; value: string }[] = [
  { label: '待我审批', value: 'PENDING' },
  { label: '全部', value: '' },
  { label: '待 HR 备案', value: 'DEPT_APPROVED' },
  { label: '已备案', value: 'HR_FILED' },
  { label: '已驳回', value: 'DEPT_REJECTED' },
]

const activeTab = ref<string>('PENDING')

const query = reactive<TrainingRequestApprovalQuery>({
  status: 'PENDING',
  page: 1,
  pageSize: 10,
})

const list = ref<TrainingRequestApprovalItem[]>([])
const total = ref(0)
const loading = ref(false)
const error = ref(false)
const errorMessage = ref('')

async function fetchList() {
  loading.value = true
  error.value = false

  const res = await getAllTrainingRequestsApi({
    status: query.status,
    page: query.page,
    pageSize: query.pageSize,
  })
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

function handlePageChange(page: number) {
  query.page = page
  fetchList()
}

// 审批/驳回弹窗
const dialogVisible = ref(false)
const dialogMode = ref<'approve' | 'reject'>('approve')
const current = ref<TrainingRequestApprovalItem | null>(null)
const comment = ref('')
const submitting = ref(false)

function openApprove(row: TrainingRequestApprovalItem) {
  dialogMode.value = 'approve'
  current.value = row
  comment.value = ''
  dialogVisible.value = true
}

function openReject(row: TrainingRequestApprovalItem) {
  dialogMode.value = 'reject'
  current.value = row
  comment.value = ''
  dialogVisible.value = true
}

async function handleSubmit() {
  if (!current.value || submitting.value) return
  if (dialogMode.value === 'reject' && !comment.value.trim()) {
    ElMessage.warning('驳回时必须填写理由')
    return
  }

  submitting.value = true
  const res =
    dialogMode.value === 'approve'
      ? await approveTrainingRequestApi(current.value.requestId, comment.value.trim() || null)
      : await rejectTrainingRequestApi(current.value.requestId, comment.value.trim())
  submitting.value = false

  if (res.success) {
    ElMessage.success(dialogMode.value === 'approve' ? '审批通过' : '已驳回')
    dialogVisible.value = false
    fetchList()
  } else {
    ElMessage.error(res.message || '操作失败')
  }
}

onMounted(fetchList)
</script>

<template>
  <div class="page-container dept-approval-page">
    <PageHeader title="主管审批" description="审批本部门员工的培训申请" />

    <el-tabs v-model="activeTab" class="status-tabs" @tab-change="handleTabChange">
      <el-tab-pane
        v-for="tab in tabs"
        :key="tab.value"
        :label="tab.label"
        :name="tab.value"
      />
    </el-tabs>

    <AsyncState
      :loading="loading"
      :error="error"
      :error-message="errorMessage"
      :empty="list.length === 0"
      empty-description="暂无待审批申请"
      @retry="fetchList"
    >
      <el-table :data="list" row-key="requestId" style="width: 100%">
        <el-table-column label="员工" min-width="140">
          <template #default="{ row }">
            <div>{{ row.employeeName }}</div>
            <div class="sub-text">{{ row.deptName || '—' }}</div>
          </template>
        </el-table-column>
        <el-table-column label="课程" min-width="200">
          <template #default="{ row }">
            <div>{{ row.courseName }}</div>
            <div class="sub-text">{{ row.courseType || '—' }}</div>
          </template>
        </el-table-column>
        <el-table-column label="申请理由" min-width="220" show-overflow-tooltip>
          <template #default="{ row }">{{ row.reason }}</template>
        </el-table-column>
        <el-table-column label="申请时间" width="160">
          <template #default="{ row }">{{ formatDateTime(row.createdAt) }}</template>
        </el-table-column>
        <el-table-column label="状态" width="130">
          <template #default="{ row }">
            <StatusTag :type="row.status" :label-map="TRAINING_REQUEST_STATUS_LABELS" />
          </template>
        </el-table-column>
        <el-table-column label="操作" width="140" fixed="right">
          <template #default="{ row }">
            <template v-if="row.status === 'PENDING'">
              <el-button text type="primary" size="small" @click="openApprove(row)">通过</el-button>
              <el-button text type="danger" size="small" @click="openReject(row)">驳回</el-button>
            </template>
            <span v-else class="sub-text">—</span>
          </template>
        </el-table-column>
      </el-table>

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

    <!-- 审批/驳回弹窗 -->
    <el-dialog
      v-model="dialogVisible"
      :title="dialogMode === 'approve' ? '审批通过' : '驳回申请'"
      width="480px"
      :close-on-click-modal="false"
    >
      <template v-if="current">
        <div class="dialog-summary">
          <div><span class="label">员工：</span>{{ current.employeeName }}（{{ current.deptName || '—' }}）</div>
          <div><span class="label">课程：</span>{{ current.courseName }}</div>
        </div>
        <el-form label-position="top">
          <el-form-item :label="dialogMode === 'approve' ? '审批意见（选填）' : '驳回理由（必填）'">
            <el-input
              v-model="comment"
              type="textarea"
              :rows="3"
              :placeholder="dialogMode === 'approve' ? '填写审批意见' : '请说明驳回原因'"
              maxlength="200"
              show-word-limit
            />
          </el-form-item>
        </el-form>
      </template>
      <template #footer>
        <el-button @click="dialogVisible = false">取消</el-button>
        <el-button
          :type="dialogMode === 'approve' ? 'primary' : 'danger'"
          :loading="submitting"
          @click="handleSubmit"
        >
          {{ dialogMode === 'approve' ? '确认通过' : '确认驳回' }}
        </el-button>
      </template>
    </el-dialog>
  </div>
</template>

<style lang="scss" scoped>
.status-tabs {
  margin-bottom: var(--space-md);
}

.sub-text {
  font-size: 12px;
  color: var(--color-text-secondary);
}

.pagination-wrapper {
  display: flex;
  justify-content: center;
  margin-top: var(--space-lg);
}

.dialog-summary {
  margin-bottom: var(--space-md);
  padding: var(--space-sm) var(--space-md);
  background: var(--color-bg-page);
  border-radius: var(--radius-sm);
  font-size: 13px;
  line-height: 1.8;
}

.dialog-summary .label {
  color: var(--color-text-secondary);
}
</style>
