<script setup lang="ts">
import { ref, reactive, computed, onMounted, nextTick } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { useAuthStore } from '@/stores/auth'
import { getCourseDetailApi, publishCourseApi, closeCourseApi } from '@/api/course'
import { createTrainingRequestApi, getMyCourseRequestStatusApi } from '@/api/training-request'
import type { CourseDetail } from '@/types/course'
import { COURSE_STATUS_LABELS } from '@/types/enums'
import { formatDateTime, formatDate, formatCurrency, formatDuration } from '@/utils/format'
import StatusTag from '@/components/common/StatusTag.vue'
import CourseCover from '@/components/common/CourseCover.vue'
import { ElMessage, type FormInstance, type FormRules } from 'element-plus'

const route = useRoute()
const router = useRouter()
const auth = useAuthStore()

const courseId = Number(route.params.id)

const isEmployee = computed(() => auth.hasRole(['EMPLOYEE']) && !auth.hasRole(['ADMIN', 'HR', 'DEPT_MANAGER']))
// 普通员工仅可查看已发布课程（前端防御，最终以后端权限校验为准）
const forbidden = computed(
  () => isEmployee.value && course.value !== null && course.value.courseStatus !== 'PUBLISHED',
)

// 课程发布/关闭仅 HR/Admin 可操作（对应后端 AuthorizationPolicies.HrOrAdmin）
const canManageCourse = computed(() => auth.hasRole(['HR', 'ADMIN']))
const publishLoading = ref(false)
const closeLoading = ref(false)

const course = ref<CourseDetail | null>(null)
const loading = ref(true)
const error = ref(false)
const errorMessage = ref('')

// 申请状态
const hasApplied = ref(false)
const showApplyDialog = ref(false)
const applyFormRef = ref<FormInstance>()
const applyForm = reactive({
  reason: '',
  expectedGain: '',
})
const applyLoading = ref(false)
const applyResult = ref<{ success: boolean; message: string } | null>(null)

const applyRules: FormRules = {
  reason: [
    { required: true, message: '请填写申请理由', trigger: 'blur' },
    { min: 2, message: '申请理由至少 2 个字符', trigger: 'blur' },
  ],
}

function remainingSlots(): number | null {
  if (!course.value) return null
  if (course.value.enrolledCount === undefined || course.value.enrolledCount === null) return null
  return course.value.maxStudents - course.value.enrolledCount
}

function canApply(): boolean {
  if (!course.value) return false
  if (course.value.courseStatus !== 'PUBLISHED') return false
  // 名额数据未知时不阻止申请（以后端最终校验为准），仅明确满员时禁止
  if (remainingSlots() !== null && remainingSlots()! <= 0) return false
  return !hasApplied.value
}

async function handlePublish() {
  publishLoading.value = true
  const res = await publishCourseApi(courseId)
  publishLoading.value = false
  if (res.success) {
    ElMessage.success('课程已发布')
    await fetchDetail()
  } else {
    // 后端 PublishAsync 当前可能固定返回 409，需如实展示后端原因，不得提示“发布成功”
    ElMessage.error(res.message || '发布失败')
  }
}

async function handleClose() {
  closeLoading.value = true
  const res = await closeCourseApi(courseId)
  closeLoading.value = false
  if (res.success) {
    ElMessage.success('课程已关闭')
    await fetchDetail()
  } else {
    ElMessage.error(res.message || '关闭失败')
  }
}

async function fetchDetail() {
  loading.value = true
  error.value = false

  const res = await getCourseDetailApi(courseId)
  if (!res.success || !res.data) {
    loading.value = false
    error.value = true
    errorMessage.value = res.message || '加载失败'
    return
  }

  course.value = res.data
  loading.value = false
  await refreshRequestStatus()
}

/** 查询当前用户对该课程的申请状态（页面不感知 Mock/真实接口） */
async function refreshRequestStatus() {
  const res = await getMyCourseRequestStatusApi(courseId)
  if (res.success && res.data) {
    hasApplied.value = res.data.applied
  }
}

function goBack() {
  if (window.history.state?.back) {
    router.back()
  } else {
    router.push('/courses')
  }
}

function openApplyDialog() {
  if (hasApplied.value) {
    router.push('/my-requests')
    return
  }
  showApplyDialog.value = true
}

function closeApplyDialog() {
  showApplyDialog.value = false
  applyResult.value = null
  applyForm.reason = ''
  applyForm.expectedGain = ''
  nextTick(() => {
    applyFormRef.value?.resetFields()
  })
}

async function submitApply() {
  const valid = await applyFormRef.value?.validate().catch(() => false)
  if (!valid) return

  applyLoading.value = true
  applyResult.value = null

  const res = await createTrainingRequestApi({
    courseId,
    reason: applyForm.reason,
    expectedGain: applyForm.expectedGain || undefined,
  })

  applyLoading.value = false

  if (res.success) {
    applyResult.value = { success: true, message: '申请已提交，请等待主管审批' }
    // 提交成功后刷新申请状态
    await refreshRequestStatus()
    // 2 秒后自动关闭
    setTimeout(() => {
      closeApplyDialog()
    }, 2000)
  } else {
    applyResult.value = { success: false, message: res.message || '提交失败' }
  }
}

onMounted(() => {
  fetchDetail()
})
</script>

<template>
  <div class="page-container page-container--narrow course-detail-page">
    <!-- 返回 -->
    <el-button text @click="goBack" style="margin-bottom: 16px">
      <el-icon><component :is="'ArrowLeft'" /></el-icon> 返回课程列表
    </el-button>

    <!-- 加载中 -->
    <div v-if="loading" v-loading="true" element-loading-text="加载中..." style="min-height: 300px" />

    <!-- 错误 -->
    <template v-else-if="error">
      <el-result icon="error" :title="errorMessage">
        <template #extra>
          <el-button type="primary" @click="fetchDetail">重试</el-button>
          <el-button @click="goBack">返回列表</el-button>
        </template>
      </el-result>
    </template>

    <!-- 空数据 -->
    <el-empty v-else-if="!course" description="课程不存在" />

    <!-- 普通员工无权查看非已发布课程 -->
    <el-result v-else-if="forbidden" icon="warning" title="无权访问" sub-title="普通员工仅可查看已发布的课程">
      <template #extra>
        <el-button type="primary" @click="goBack">返回课程列表</el-button>
      </template>
    </el-result>

    <!-- 课程信息 -->
    <template v-else>
      <!-- 顶部摘要卡 -->
      <el-card shadow="never" class="summary-card">
        <div class="summary-top">
          <div class="summary-cover">
            <CourseCover :course-id="courseId" :course-type="course.courseType" alt="" />
          </div>

          <div class="summary-info">
            <div class="summary-tags">
              <StatusTag :type="course.courseStatus" :label-map="COURSE_STATUS_LABELS" />
              <el-tag v-if="course.courseType" size="small" effect="plain" type="info">
                {{ course.courseType }}
              </el-tag>
            </div>
            <h2 class="summary-title">{{ course.courseName }}</h2>
            <div class="summary-meta">
              <span v-if="course.trainerName">
                <el-icon :size="14"><component :is="'User'" /></el-icon>
                {{ course.trainerName }}
              </span>
              <span v-if="course.startAt">
                <el-icon :size="14"><component :is="'Clock'" /></el-icon>
                {{ formatDateTime(course.startAt) }}
              </span>
              <span v-if="course.location">
                <el-icon :size="14"><component :is="'Location'" /></el-icon>
                {{ course.location }}
              </span>
            </div>

            <div class="summary-stats">
              <div class="stat-item">
                <div class="stat-label">剩余名额</div>
                <div
                  v-if="remainingSlots() !== null"
                  class="stat-value"
                  :class="{ 'text-warning': remainingSlots()! <= 3 && remainingSlots()! > 0, 'text-danger': remainingSlots()! <= 0 }"
                >
                  {{ remainingSlots() }} / {{ course.maxStudents }}
                </div>
                <div v-else class="stat-value stat-value--muted">名额数据暂不可用</div>
              </div>
              <div class="stat-item">
                <div class="stat-label">培训费用</div>
                <div class="stat-value">{{ formatCurrency(course.budgetAmount) }}</div>
              </div>
              <div class="stat-item">
                <div class="stat-label">培训时长</div>
                <div class="stat-value">{{ formatDuration(course.durationHours) }}</div>
              </div>
              <div class="stat-item">
                <div class="stat-label">主办部门</div>
                <div class="stat-value">{{ course.deptName || '-' }}</div>
              </div>
            </div>

            <div class="summary-action">
              <!-- 课程管理（仅 HR/Admin） -->
              <template v-if="canManageCourse">
                <el-button
                  v-if="course.courseStatus === 'DRAFT'"
                  type="primary"
                  size="large"
                  :loading="publishLoading"
                  @click="handlePublish"
                >
                  发布课程
                </el-button>
                <el-popconfirm
                  v-else-if="course.courseStatus === 'PUBLISHED'"
                  title="确定关闭该课程吗？关闭后员工将无法申请"
                  confirm-button-text="关闭"
                  cancel-button-text="取消"
                  confirm-button-type="danger"
                  @confirm="handleClose"
                >
                  <template #reference>
                    <el-button type="danger" size="large" :loading="closeLoading">关闭课程</el-button>
                  </template>
                </el-popconfirm>
                <el-tag v-else type="info" size="large">已关闭</el-tag>
              </template>

              <!-- 申请相关操作 -->
              <template v-if="!canManageCourse || course.courseStatus === 'PUBLISHED'">
                <el-button
                  v-if="canApply()"
                  type="primary"
                  size="large"
                  @click="openApplyDialog"
                >
                  申请培训
                </el-button>
                <el-button
                  v-else-if="hasApplied"
                  type="primary"
                  size="large"
                  @click="openApplyDialog"
                >
                  查看申请进度
                </el-button>
                <el-tag v-else-if="course.courseStatus === 'DRAFT'" type="info" size="large">
                  课程未发布
                </el-tag>
                <el-tag v-else-if="course.courseStatus === 'CLOSED'" type="info" size="large">
                  课程已关闭
                </el-tag>
                <el-tag v-else-if="remainingSlots() !== null && remainingSlots()! <= 0" type="warning" size="large">
                  名额已满
                </el-tag>
              </template>
            </div>
          </div>
        </div>
      </el-card>

      <!-- 详细信息 -->
      <el-card shadow="never" class="section-card">
        <template #header>
          <span class="card-title">详细信息</span>
        </template>
        <div class="detail-two-col">
          <div class="detail-col">
            <div class="detail-col__title">讲师信息</div>
            <div class="trainer-detail">
              <div class="trainer-main">
                <div class="trainer-name">{{ course.trainerName || '-' }}</div>
                <div class="trainer-sub">
                  <span v-if="course.trainerTitle">{{ course.trainerTitle }}</span>
                  <span v-if="course.trainerCompany" class="trainer-org">{{ course.trainerCompany }}</span>
                </div>
              </div>
              <div v-if="course.trainerStarLevel" class="trainer-stars">
                <el-rate
                  :model-value="course.trainerStarLevel"
                  disabled
                  show-score
                  text-color="#ff9900"
                  score-template="{value}"
                />
              </div>
            </div>
            <el-descriptions v-if="course.trainerEmail || course.trainerPhone" :column="2" size="small" border style="margin-top: 16px">
              <el-descriptions-item v-if="course.trainerEmail" label="邮箱">
                {{ course.trainerEmail }}
              </el-descriptions-item>
              <el-descriptions-item v-if="course.trainerPhone" label="电话">
                {{ course.trainerPhone }}
              </el-descriptions-item>
            </el-descriptions>
          </div>

          <div class="detail-col">
            <div class="detail-col__title">课程信息</div>
            <el-descriptions :column="1" border size="small">
              <el-descriptions-item label="创建时间">
                {{ formatDate(course.createdAt) }}
              </el-descriptions-item>
              <el-descriptions-item label="最后更新">
                {{ formatDateTime(course.updatedAt) }}
              </el-descriptions-item>
            </el-descriptions>
          </div>
        </div>
      </el-card>
    </template>

    <!-- 申请培训 Dialog -->
    <el-dialog
      v-model="showApplyDialog"
      title="申请培训"
      width="520px"
      :close-on-click-modal="false"
      @close="closeApplyDialog"
    >
      <el-alert
        v-if="applyResult"
        :title="applyResult.message"
        :type="applyResult.success ? 'success' : 'error'"
        show-icon
        :closable="false"
        style="margin-bottom: 16px"
      />

      <el-form
        v-if="!applyResult"
        ref="applyFormRef"
        :model="applyForm"
        :rules="applyRules"
        label-position="top"
      >
        <el-form-item label="培训课程" required>
          <el-input :model-value="course?.courseName" disabled />
        </el-form-item>
        <el-form-item label="申请理由" prop="reason" required>
          <el-input
            v-model="applyForm.reason"
            type="textarea"
            :rows="3"
            placeholder="请说明申请参加本次培训的原因"
            maxlength="500"
            show-word-limit
          />
        </el-form-item>
        <el-form-item label="预期收益">
          <el-input
            v-model="applyForm.expectedGain"
            type="textarea"
            :rows="2"
            placeholder="期望通过本次培训获得哪些提升（选填）"
            maxlength="500"
            show-word-limit
          />
        </el-form-item>
      </el-form>

      <template #footer>
        <el-button
          @click="closeApplyDialog"
          :disabled="applyLoading"
        >
          {{ applyResult ? '关闭' : '取消' }}
        </el-button>
        <el-button
          v-if="!applyResult"
          type="primary"
          :loading="applyLoading"
          @click="submitApply"
        >
          提交申请
        </el-button>
      </template>
    </el-dialog>
  </div>
</template>

<style lang="scss" scoped>
.summary-card {
  :deep(.el-card__body) {
    padding: 24px;
  }
}

.summary-top {
  display: flex;
  align-items: stretch;
  gap: 24px;
}

.summary-cover {
  flex: 0 0 320px;
  max-width: 320px;
  border-radius: var(--radius-md);
  overflow: hidden;
}

.summary-info {
  flex: 1;
  min-width: 0;
  display: flex;
  flex-direction: column;
}

.summary-tags {
  display: flex;
  gap: 8px;
  margin-bottom: 10px;
}

.summary-title {
  font-size: 24px;
  font-weight: 600;
  color: var(--color-text-primary);
  margin-bottom: 10px;
  line-height: 1.4;
}

.summary-meta {
  display: flex;
  flex-wrap: wrap;
  gap: 6px 20px;
  font-size: 13px;
  color: var(--color-text-secondary);

  span {
    display: inline-flex;
    align-items: center;
    gap: 4px;
  }
}

.summary-stats {
  display: flex;
  flex-wrap: wrap;
  gap: 8px 24px;
  margin-top: 16px;
}

.summary-action {
  margin-top: auto;
  padding-top: 20px;
  display: flex;
  justify-content: flex-end;
}

.stat-item {
  & + .stat-item {
    padding-left: 24px;
    border-left: 1px solid var(--color-divider);
  }

  .stat-label {
    font-size: 13px;
    color: var(--color-text-placeholder);
    margin-bottom: 4px;
  }

  .stat-value {
    font-size: 17px;
    font-weight: 600;
    color: var(--color-text-primary);

    &.text-warning {
      color: var(--color-warning);
    }

    &.text-danger {
      color: var(--color-danger);
    }

    &--muted {
      font-size: 14px;
      font-weight: 400;
      color: var(--color-text-placeholder);
    }
  }
}

/* 小屏：上图下文 */
@media (max-width: 720px) {
  .summary-top {
    flex-direction: column;
  }

  .summary-cover {
    flex: none;
    max-width: 100%;
    width: 100%;
  }
}

.section-card {
  margin-top: 16px;

  .card-title {
    font-weight: 600;
    font-size: 14px;
  }
}

.detail-two-col {
  display: grid;
  grid-template-columns: 1fr 1fr;

  @media (max-width: 720px) {
    grid-template-columns: 1fr;
    gap: 24px 0;
  }
}

.detail-col {
  min-width: 0;

  & + .detail-col {
    padding-left: 32px;
    border-left: 1px solid var(--color-divider);

    @media (max-width: 720px) {
      padding-left: 0;
      border-left: none;
      padding-top: 24px;
      border-top: 1px solid var(--color-divider);
    }
  }

  &__title {
    font-size: 14px;
    font-weight: 600;
    color: var(--color-text-primary);
    margin-bottom: 12px;
  }
}

.trainer-detail {
  display: flex;
  justify-content: space-between;
  align-items: flex-start;

  .trainer-main {
    .trainer-name {
      font-size: 16px;
      font-weight: 600;
      color: var(--color-text-primary);
    }

    .trainer-sub {
      font-size: 13px;
      color: var(--color-text-secondary);
      margin-top: 4px;
    }

    .trainer-org {
      margin-left: 12px;
      padding-left: 12px;
      border-left: 1px solid var(--color-divider);
    }
  }
}
</style>
