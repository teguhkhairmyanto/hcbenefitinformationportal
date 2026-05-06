import { createApp } from 'vue'
import App from './App.vue'
import router from './router'

import '@coreui/coreui/dist/css/coreui.min.css'
import './styles/layout.css'

import CoreuiVue from '@coreui/vue'
import AppIcon from '@/components/common/AppIcon.vue'

const app = createApp(App)

app.use(router)
app.use(CoreuiVue)

app.component('AppIcon', AppIcon)

app.mount('#app')