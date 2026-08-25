import { createApp } from 'vue'

import App from './App.vue'
import { setUnauthorizedHandler } from './api/auth-session'
import router from './router'
import { pinia } from './stores'
import { useAuthStore } from './stores/auth'
import './styles/index.css'

const app = createApp(App)

app.use(pinia)
app.use(router)

setUnauthorizedHandler(() => {
  const authStore = useAuthStore(pinia)
  authStore.expireSession()
  const current = router.currentRoute.value
  if (current.name !== 'login') {
    void router.replace({
      name: 'login',
      query: { redirect: current.fullPath, reason: 'session-expired' },
    })
  }
})

app.mount('#app')
