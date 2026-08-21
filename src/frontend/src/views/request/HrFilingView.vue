<script setup lang="ts">
import { ref, reactive, onMounted } from 'vue'
import { getAllTrainingRequestsApi, fileTrainingRequestApi } from '@/api/training-request'
import type {
  TrainingRequestApprovalItem,
  TrainingRequestApprovalQuery,
} from '@/types/training-request'
import { TRAINING_REQUEST_STATUS_LABELS, type TrainingRequestStatus } from '@/types/enums'
import { formatDateTime, formatCurrency, formatEmpty } from '@/utils/format'
import { ElMessage, ElMessageBox } from 'element-plus'
import StatusTag from '@/components/common/StatusTag.vue'
import PageHeader from '@/components/common/PageHeader.vue'
import AsyncState from '@/components/common/AsyncState.vue'

const tabs: { label: string; value: string }[] = [
  { label: '待备案', value: 'DEPT_APPROVED' },
  { label: '已备案', value: 'HR_FILED' },
  { label: '全部', value: '' },
]

const activeTab = ref<string>('DEPT_APPROVED')

const query = reactive<TrainingRequestApprovalQuery>({
  status: 'DEPT_APPROVED',
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

// 备案（双重确认，不发送备案意见）
const filingId = ref<number | null>(null)

async function handleFile(row: TrainingRequestApprovalItem) {
  if (filingId.value !== null) return

  try {
    await ElMessageBox.confirm(
      `确认对「${row.employeeName}」的「${row.courseName}」申请进行备案吗？备案后该员工可正常参训。`,
      '备案确认',
      { confirmButtonText: '确认备案', cancelButtonText: '取消', type: 'warning' },
    )
  } catch {
    return
  }

  filingId.value = row.requestId
  const res = await fileTrainingRequestApi(row.requestId)
  filingId.value = null

  if (res.success) {
    ElMessage.success('备案完成')
    fetchList()
  } else {
    ElMessage.error(res.message || '备案失败')
  }
}

onMounted(fetchList)
</script>

<template>
  <div class="page-container hr-filing-page">
    <PageHeader title="HR 备案" description="对主管已通过的培训申请进行备案登记" />

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
      empty-description="暂无待备案申请"
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
        <el-table-column label="预算" width="120" align="right">
          <template #default="{ row }">{{ formatCurrency(row.budgetAmount) }}</template>
        </el-table-column>
        <el-table-column label="名额" width="90" align="center">
          <template #default="{ row }">{{ formatEmpty(row.maxStudents) }}</template>
        </el-table-column>
        <el-table-column label="主管意见" min-width="180" show-overflow-tooltip>
          <template #default="{ row }">{{ row.deptApproveComment || '—' }}</template>
        </el-table-column>
        <el-table-column label="主管审批时间" width="160">
          <template #default="{ row }">{{ formatDateTime(row.deptApproveTime) }}</template>
        </el-table-column>
        <el-table-column label="状态" width="130">
          <template #default="{ row }">
            <StatusTag :type="row.status" :label-map="TRAINING_REQUEST_STATUS_LABELS" />
          </template>
        </el-table-column>
        <el-table-column label="操作" width="100" fixed="right">
          <template #default="{ row }">
            <el-button
              v-if="row.status === 'DEPT_APPROVED'"
              text
              type="primary"
              size="small"
              :disabled="filingId !== null"
              @click="handleFile(row)"
            >
              备案
            </el-button>
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
</style>
