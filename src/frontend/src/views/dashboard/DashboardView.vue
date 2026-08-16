<script setup lang="ts">
import { ref, onMounted } from 'vue'
import { useRouter } from 'vue-router'
import { useAuthStore } from '@/stores/auth'
import { getDashboardStatsApi, type DashboardStats } from '@/api/dashboard'

const auth = useAuthStore()
const router = useRouter()

const stats = ref<DashboardStats | null>(null)
const loading = ref(true)
const error = ref(false)
const errorMessage = ref('')

async function fetchStats() {
  loading.value = true
  error.value = false

  const res = await getDashboardStatsApi()
  loading.value = false

  if (res.success && res.data) {
    stats.value = res.data
  } else {
    error.value = true
    errorMessage.value = res.message || '加载失败'
  }
}

function formatCardValue(card: { key: string; value: number }): string {
  // 预算类数值格式化
  if (card.key === 'budget' && card.value > 1000) {
    return `¥${card.value.toLocaleString('zh-CN')}`
  }
  return String(card.value)
}

onMounted(() => {
  fetchStats()
})
</script>

<template>
  <div class="page-container page-container--wide dashboard">
    <!-- 页头 -->
    <div class="page-header">
      <div>
        <h2>工作台</h2>
        <p class="page-desc">欢迎回来，{{ auth.user?.empName }}</p>
      </div>
    </div>

    <!-- 加载 / 错误 / 空 -->
    <div v-if="loading" v-loading="true" element-loading-text="加载中..." style="min-height: 240px" />

    <el-alert
      v-else-if="error"
      :title="errorMessage"
      type="error"
      show-icon
      :closable="false"
      style="margin-bottom: 16px"
    >
      <template #default>
        <el-button size="small" @click="fetchStats">重试</el-button>
      </template>
    </el-alert>

    <template v-else-if="stats">
      <!-- 统计卡片 -->
      <el-row :gutter="16" class="stats-row">
        <el-col
          v-for="card in stats.cards"
          :key="card.key"
          :xs="12"
          :sm="12"
          :md="6"
        >
          <el-card
            shadow="never"
            class="stat-card"
            :class="{ 'is-clickable': card.path }"
            @click="card.path && router.push(card.path)"
          >
            <div class="stat-card__inner">
              <div
                class="stat-card__icon"
                :style="{ background: (card.color || '#1a56db') + '18', color: card.color || '#1a56db' }"
              >
                <el-icon :size="22">
                  <component :is="card.icon" />
                </el-icon>
              </div>
              <div class="stat-card__body">
                <div class="stat-card__value">{{ formatCardValue(card) }}</div>
                <div class="stat-card__label">{{ card.label }}</div>
              </div>
            </div>
          </el-card>
        </el-col>
      </el-row>

      <!-- 快捷入口 -->
      <el-card shadow="never" class="shortcuts-card">
        <template #header>
          <span class="card-title">快捷入口</span>
        </template>
        <el-row :gutter="12">
          <el-col
            v-for="item in stats.shortcuts"
            :key="item.path"
            :xs="12"
            :sm="6"
            style="margin-bottom: 8px"
          >
            <el-button
              @click="router.push(item.path)"
              style="width: 100%"
            >
              <el-icon style="margin-right: 4px">
                <component :is="item.icon" />
              </el-icon>
              {{ item.label }}
            </el-button>
          </el-col>
        </el-row>
      </el-card>
    </template>

    <!-- 无业务数据 -->
    <el-empty v-else description="暂无数据" />
  </div>
</template>

<style lang="scss" scoped>
.dashboard {
  // page-header styles
  .page-header {
    margin-bottom: var(--space-lg);

    h2 {
      font-size: 20px;
      font-weight: 600;
      color: var(--color-text-primary);
    }

    .page-desc {
      margin-top: 4px;
      font-size: 13px;
      color: var(--color-text-secondary);
    }
  }
}

.stats-row {
  margin-bottom: var(--space-lg);
}

.stat-card {
  margin-bottom: var(--space-md);

  &.is-clickable {
    cursor: pointer;
    transition: box-shadow 0.15s, border-color 0.15s;

    &:hover {
      border-color: var(--color-primary-light);
      box-shadow: var(--shadow-sm);
    }
  }

  :deep(.el-card__body) {
    padding: 20px;
  }

  &__inner {
    display: flex;
    align-items: center;
    gap: 14px;
  }

  &__icon {
    width: 44px;
    height: 44px;
    border-radius: 10px;
    display: flex;
    align-items: center;
    justify-content: center;
    flex-shrink: 0;
  }

  &__body {
    min-width: 0;
  }

  &__value {
    font-size: 24px;
    font-weight: 700;
    color: var(--color-text-primary);
    line-height: 1.2;
  }

  &__label {
    font-size: 13px;
    color: var(--color-text-secondary);
    margin-top: 2px;
  }
}

.shortcuts-card {
  .card-title {
    font-weight: 600;
    font-size: 14px;
  }
}
</style>
