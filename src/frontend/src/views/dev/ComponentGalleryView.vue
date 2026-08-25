<script setup lang="ts">
import { computed, ref } from 'vue'
import { Plus } from '@element-plus/icons-vue'

import AppButton from '@/components/common/AppButton.vue'
import AppDescriptions from '@/components/common/AppDescriptions.vue'
import AppPagination from '@/components/common/AppPagination.vue'
import AppSelect from '@/components/common/AppSelect.vue'
import ConfirmDialog from '@/components/common/ConfirmDialog.vue'
import DataTable from '@/components/common/DataTable.vue'
import FormField from '@/components/common/FormField.vue'
import PageHeader from '@/components/common/PageHeader.vue'
import PageState from '@/components/common/PageState.vue'
import ProcessTimeline from '@/components/common/ProcessTimeline.vue'
import SearchPanel from '@/components/common/SearchPanel.vue'
import StatCard from '@/components/common/StatCard.vue'
import StatusTag from '@/components/common/StatusTag.vue'
import CourseSummary from '@/components/business/CourseSummary.vue'
import FileEntry from '@/components/business/FileEntry.vue'
import type {
  CourseInfo,
  DescriptionItem,
  SelectOption,
  TableColumn,
  TimelineNode,
} from '@/types/ui'

const searchExpanded = ref(false)
const searching = ref(false)
const keyword = ref('')
const courseType = ref<string | number | null>(null)
const status = ref<string | number | null>('PUBLISHED')
const page = ref(1)
const pageSize = ref(20)
const tableState = ref<'default' | 'loading' | 'empty' | 'error'>('default')
const pageState = ref<'loading' | 'empty' | 'no-result' | 'error' | 'forbidden' | 'not-found'>(
  'empty',
)
const courseState = ref<'published' | 'closed' | 'full' | 'restricted'>('published')
const dialogOpen = ref(false)
const dialogLoading = ref(false)

const typeOptions: SelectOption[] = [
  { label: '全部类型', value: '' },
  { label: '专业技能', value: 'PROFESSIONAL' },
  { label: '管理能力', value: 'MANAGEMENT' },
]
const statusOptions: SelectOption[] = [
  { label: '全部状态', value: '' },
  { label: '已发布', value: 'PUBLISHED' },
  { label: '已关闭', value: 'CLOSED' },
]
const tableColumns: TableColumn[] = [
  { key: 'code', label: '课程编号', width: 120 },
  { key: 'name', label: '课程名称', minWidth: 220 },
  { key: 'trainer', label: '培训讲师', width: 120 },
  { key: 'date', label: '培训日期', width: 130 },
  { key: 'remaining', label: '剩余名额', width: 100, align: 'right' },
  { key: 'status', label: '状态', width: 110, align: 'center' },
]
const tableRows: Record<string, unknown>[] = [
  {
    id: 4001,
    code: 'COURSE-4001',
    name: '数据库基础训练',
    trainer: '李老师',
    date: '2026-09-12',
    remaining: 18,
    status: 'PUBLISHED',
  },
  {
    id: 4002,
    code: 'COURSE-4002',
    name: '团队管理工作坊',
    trainer: '王老师',
    date: '2026-09-20',
    remaining: 0,
    status: 'FULL',
  },
  {
    id: 4003,
    code: 'COURSE-4003',
    name: '信息安全意识',
    trainer: null,
    date: '2026-10-08',
    remaining: 24,
    status: 'CLOSED',
  },
]
const descriptionItems: DescriptionItem[] = [
  { label: '申请编号', value: 'REQ-5001' },
  { label: '申请员工', value: '张三' },
  { label: '所属部门', value: '研发一部' },
  { label: '申请日期', value: '2026-08-15' },
  { label: '审批意见', value: null, span: 2 },
]
const requestTimeline: TimelineNode[] = [
  {
    title: '员工提交',
    meta: '张三 · 2026-08-15 09:30',
    summary: '申请参加数据库基础训练',
    state: 'complete',
  },
  { title: '主管审批', meta: '等待处理', summary: '当前节点', state: 'current' },
  { title: 'HR 备案', meta: '尚未开始', state: 'future' },
]
const course: CourseInfo = {
  name: '数据库基础训练',
  type: '专业技能',
  trainer: '李老师',
  schedule: '2026-09-12 09:00',
  location: '培训室 A301',
  hours: 8,
  remainingSeats: 0,
}

const statusMeta = computed(() => ({
  PUBLISHED: { label: '已发布', semantic: 'success' as const },
  FULL: { label: '名额已满', semantic: 'warning' as const },
  CLOSED: { label: '已关闭', semantic: 'neutral' as const },
}))

function runSearch() {
  searching.value = true
  window.setTimeout(() => {
    searching.value = false
    tableState.value = 'default'
  }, 500)
}

function resetSearch() {
  keyword.value = ''
  courseType.value = null
  status.value = 'PUBLISHED'
  page.value = 1
}

function confirmDialog() {
  dialogLoading.value = true
  window.setTimeout(() => {
    dialogLoading.value = false
    dialogOpen.value = false
  }, 700)
}
</script>

<template>
  <main class="component-gallery">
    <PageHeader
      title="公共组件展示"
      description="M2 设计令牌、Element Plus 主题和公共组件状态验收入口。"
      :breadcrumbs="['前端工程', '开发工具', '公共组件']"
      action-label="新建示例"
    />

    <nav class="gallery-nav" aria-label="组件分类">
      <a href="#foundations">设计基础</a>
      <a href="#actions">操作与状态</a>
      <a href="#data">搜索与数据</a>
      <a href="#forms">表单与弹窗</a>
      <a href="#business">业务摘要</a>
    </nav>

    <section id="foundations" class="gallery-section">
      <div class="gallery-section__heading">
        <span>01</span>
        <div>
          <h2>设计基础</h2>
          <p>颜色、间距、圆角、阴影与文字层级均来自统一 CSS 令牌。</p>
        </div>
      </div>
      <div class="token-grid">
        <div
          v-for="token in ['primary', 'success', 'warning', 'error', 'neutral']"
          :key="token"
          class="token-swatch"
        >
          <span :class="`token-swatch__color token-swatch__color--${token}`" />
          <strong>{{ token }}</strong>
        </div>
      </div>
    </section>

    <section id="actions" class="gallery-section">
      <div class="gallery-section__heading">
        <span>02</span>
        <div>
          <h2>操作与状态</h2>
          <p>主要、默认、文字、危险、禁用与提交中状态。</p>
        </div>
      </div>
      <div class="demo-card">
        <h3>Button</h3>
        <div class="demo-row">
          <AppButton label="提交申请" variant="primary"
            ><template #icon
              ><el-icon><Plus /></el-icon></template
          ></AppButton>
          <AppButton label="返回列表" />
          <AppButton label="查看详情" variant="text" />
          <AppButton label="标记缺勤" variant="danger" />
          <AppButton label="提交申请" variant="primary" loading loading-text="提交中…" />
          <AppButton label="课程已满" disabled disabled-reason="当前没有剩余名额" />
        </div>
      </div>
      <div class="demo-card">
        <h3>StatusTag</h3>
        <div class="demo-row">
          <StatusTag label="已完成" semantic="success" />
          <StatusTag label="待审批" semantic="warning" />
          <StatusTag label="主管驳回" semantic="error" />
          <StatusTag label="已报名" semantic="info" />
          <StatusTag label="已取消" semantic="neutral" />
        </div>
      </div>
      <div class="demo-card">
        <h3>PageState</h3>
        <el-radio-group v-model="pageState" aria-label="页面状态示例">
          <el-radio-button value="loading">加载</el-radio-button>
          <el-radio-button value="empty">空数据</el-radio-button>
          <el-radio-button value="no-result">无结果</el-radio-button>
          <el-radio-button value="error">失败</el-radio-button>
          <el-radio-button value="forbidden">403</el-radio-button>
          <el-radio-button value="not-found">404</el-radio-button>
        </el-radio-group>
        <PageState
          :state="pageState"
          compact
          trace-id="TRACE-M2-DEMO"
          secondary-label="返回上一页"
        />
      </div>
    </section>

    <section id="data" class="gallery-section">
      <div class="gallery-section__heading">
        <span>03</span>
        <div>
          <h2>搜索与数据</h2>
          <p>支持 Enter 查询、展开条件、局部加载、空态、失败与服务端分页。</p>
        </div>
      </div>
      <SearchPanel
        v-model:expanded="searchExpanded"
        :searching="searching"
        @search="runSearch"
        @reset="resetSearch"
      >
        <label class="demo-field"
          ><span>课程名称</span><el-input v-model="keyword" placeholder="请输入课程名称" clearable
        /></label>
        <label class="demo-field"
          ><span>课程类型</span
          ><AppSelect v-model="courseType" accessible-label="课程类型" :options="typeOptions"
        /></label>
        <label class="demo-field"
          ><span>课程状态</span
          ><AppSelect v-model="status" accessible-label="课程状态" :options="statusOptions"
        /></label>
        <template #expanded>
          <label class="demo-field"
            ><span>开始日期</span><el-date-picker aria-label="开始日期" placeholder="选择开始日期"
          /></label>
          <label class="demo-field"
            ><span>结束日期</span><el-date-picker aria-label="结束日期" placeholder="选择结束日期"
          /></label>
        </template>
      </SearchPanel>
      <div class="demo-card">
        <div class="demo-card__toolbar">
          <h3>DataTable / Pagination</h3>
          <el-radio-group v-model="tableState" aria-label="表格状态示例">
            <el-radio-button value="default">正常</el-radio-button>
            <el-radio-button value="loading">加载</el-radio-button>
            <el-radio-button value="empty">空数据</el-radio-button>
            <el-radio-button value="error">失败</el-radio-button>
          </el-radio-group>
        </div>
        <DataTable
          :columns="tableColumns"
          :rows="tableRows"
          :state="tableState"
          has-actions
          trace-id="TRACE-TABLE-001"
        >
          <template #cell="{ column, value }">
            <StatusTag
              v-if="column.key === 'status'"
              :label="statusMeta[value as keyof typeof statusMeta]?.label || '未知状态'"
              :semantic="statusMeta[value as keyof typeof statusMeta]?.semantic || 'neutral'"
            />
            <template v-else>{{ value ?? '—' }}</template>
          </template>
          <template #actions
            ><AppButton label="详情" variant="text" /><AppButton label="申请" variant="text"
          /></template>
        </DataTable>
        <AppPagination
          v-model:page="page"
          v-model:page-size="pageSize"
          :total="48"
          :disabled="tableState === 'loading'"
        />
      </div>
    </section>

    <section id="forms" class="gallery-section">
      <div class="gallery-section__heading">
        <span>04</span>
        <div>
          <h2>表单、详情与弹窗</h2>
          <p>字段标签、错误关联、只读信息、流程和明确确认动作。</p>
        </div>
      </div>
      <div class="demo-grid demo-grid--two">
        <div class="demo-card">
          <h3>FormField / Select</h3>
          <el-form label-position="top">
            <FormField label="培训课程" required helper="请选择本次申请的培训课程。"
              ><AppSelect v-model="courseType" accessible-label="培训课程" :options="typeOptions"
            /></FormField>
            <FormField label="申请理由" required error="申请理由不能少于 10 个字"
              ><el-input type="textarea" :rows="3" model-value="需要提升"
            /></FormField>
            <FormField label="所属部门" readonly display-value="研发一部" />
          </el-form>
          <AppButton label="打开确认弹窗" variant="primary" @click="dialogOpen = true" />
        </div>
        <div class="demo-card">
          <h3>Descriptions</h3>
          <AppDescriptions title="申请信息" :items="descriptionItems" :columns="2" />
        </div>
      </div>
      <div class="demo-card">
        <h3>Timeline</h3>
        <ProcessTimeline accessible-label="培训申请流程" :nodes="requestTimeline" />
      </div>
      <ConfirmDialog
        v-model="dialogOpen"
        title="完成 HR 备案"
        description="确认将张三的“数据库基础训练”申请标记为 HR 已备案？"
        confirm-label="完成备案"
        :loading="dialogLoading"
        loading-text="备案中…"
        @confirm="confirmDialog"
      />
    </section>

    <section id="business" class="gallery-section">
      <div class="gallery-section__heading">
        <span>05</span>
        <div>
          <h2>业务摘要</h2>
          <p>统计、课程资格和资源入口仅展示服务端结果，不在组件内推导业务状态。</p>
        </div>
      </div>
      <div class="stat-grid">
        <StatCard label="待审批申请" :value="12" helper="较昨日新增 2 条" />
        <StatCard label="今日待办" state="empty" helper="当前没有待处理事项" />
        <StatCard
          label="已完成培训"
          :value="8"
          state="link"
          to="/components"
          helper="查看培训记录"
        />
      </div>
      <div class="demo-card">
        <div class="demo-card__toolbar">
          <h3>CourseSummary</h3>
          <el-radio-group v-model="courseState" aria-label="课程摘要状态"
            ><el-radio-button value="published">已发布</el-radio-button
            ><el-radio-button value="closed">已关闭</el-radio-button
            ><el-radio-button value="full">已满员</el-radio-button
            ><el-radio-button value="restricted">资格受限</el-radio-button></el-radio-group
          >
        </div>
        <CourseSummary
          :course="course"
          :state="courseState"
          restriction-reason="当前员工处于培训黑名单有效期内。"
          ><template #action="{ disabled }"
            ><AppButton
              label="提交申请"
              variant="primary"
              :disabled="disabled"
              :disabled-reason="disabled ? '当前状态不可申请' : ''" /></template
        ></CourseSummary>
      </div>
      <div class="demo-grid demo-grid--three">
        <FileEntry title="课程讲义.pdf" state="available" />
        <FileEntry title="训前测试" state="expired" />
        <FileEntry title="保密培训资料" state="forbidden" />
      </div>
    </section>
  </main>
</template>

<style scoped>
.component-gallery {
  width: min(var(--layout-content-max-width), calc(100% - 48px));
  margin: 0 auto;
  padding: var(--space-8) 0 var(--space-12);
}

.gallery-nav {
  position: sticky;
  z-index: 10;
  top: 0;
  display: flex;
  gap: var(--space-2);
  padding: var(--space-3) 0;
  margin: var(--space-5) 0 var(--space-8);
  border-bottom: var(--border-default);
  background: color-mix(in srgb, var(--color-page-bg) 94%, transparent);
}

.gallery-nav a {
  padding: var(--space-2) var(--space-3);
  border-radius: var(--radius-md);
  color: var(--text-secondary);
  font-weight: 500;
  text-decoration: none;
}

.gallery-nav a:hover {
  color: var(--text-link);
  background: var(--color-primary-soft);
}

.gallery-section {
  display: grid;
  gap: var(--space-5);
  padding: var(--space-8) 0;
  scroll-margin-top: 64px;
}

.gallery-section__heading {
  display: grid;
  grid-template-columns: 40px 1fr;
  gap: var(--space-4);
}

.gallery-section__heading > span {
  color: var(--color-primary);
  font-size: var(--font-size-title-section);
  font-weight: 600;
}

.gallery-section h2,
.gallery-section h3,
.gallery-section__heading p {
  margin: 0;
}

.gallery-section h2 {
  font-size: var(--font-size-title-section);
  line-height: var(--line-height-title-section);
}

.gallery-section__heading p {
  margin-top: var(--space-1);
  color: var(--text-secondary);
}

.demo-card {
  display: grid;
  gap: var(--space-5);
  padding: var(--space-6);
  border: var(--border-default);
  border-radius: var(--radius-lg);
  background: var(--color-surface);
}

.demo-card h3 {
  font-size: var(--font-size-title-component);
  line-height: var(--line-height-title-component);
}

.demo-card__toolbar,
.demo-row {
  display: flex;
  flex-wrap: wrap;
  align-items: flex-start;
  justify-content: space-between;
  gap: var(--space-3);
}

.demo-row {
  justify-content: flex-start;
}

.demo-grid,
.stat-grid,
.token-grid {
  display: grid;
  gap: var(--space-4);
}

.demo-grid--two {
  grid-template-columns: repeat(2, minmax(0, 1fr));
}

.demo-grid--three,
.stat-grid {
  grid-template-columns: repeat(3, minmax(0, 1fr));
}

.token-grid {
  grid-template-columns: repeat(5, minmax(0, 1fr));
}

.token-swatch {
  display: grid;
  gap: var(--space-2);
  padding: var(--space-3);
  border: var(--border-default);
  border-radius: var(--radius-lg);
  background: var(--color-surface);
}

.token-swatch__color {
  height: 56px;
  border-radius: var(--radius-md);
  background: var(--color-primary);
}

.token-swatch__color--success {
  background: var(--color-success);
}
.token-swatch__color--warning {
  background: var(--color-warning);
}
.token-swatch__color--error {
  background: var(--color-error);
}
.token-swatch__color--neutral {
  background: var(--color-neutral);
}

.demo-field {
  display: grid;
  gap: var(--space-2);
  color: var(--text-secondary);
  font-size: var(--font-size-helper);
}
</style>
