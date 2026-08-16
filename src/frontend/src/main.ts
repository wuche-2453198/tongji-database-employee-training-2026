import { createApp } from 'vue'
import { createPinia } from 'pinia'
import ElementPlus from 'element-plus'
import 'element-plus/dist/index.css'
import App from './App.vue'
import router from './router'
import { registerIcons } from './plugins/icons'
import { setUnauthorizedListener } from '@/api/unauthorized'
import { useAuthStore } from '@/stores/auth'
import './styles/global.scss'

const app = createApp(App)
const pinia = createPinia()

registerIcons(app)

app.use(pinia)
app.use(router)
app.use(ElementPlus, { size: 'default' })

// 401 统一处理：清理认证状态并跳转登录页，保留原始目标地址
setUnauthorizedListener(() => {
  const auth = useAuthStore(pinia)
  auth.logout()
  const current = router.currentRoute.value
  if (current.name !== 'Login') {
    router.replace({ path: '/login', query: { redirect: current.fullPath } })
  }
})

app.mount('#app')
