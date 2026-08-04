import { createRouter, createWebHistory } from 'vue-router'

import MainLayout from '../layouts/MainLayout.vue'
import HomeView from '../views/HomeView.vue'
import KalenderKerjaPerusahaan from '../views/KalenderKerjaPerusahaan.vue'
import FormulirPengajuan from '../views/FormulirPengajuan.vue'
import HelpDesk from '../views/HelpDesk.vue'
import Penghargaan from '../views/Penghargaan.vue'
import Bantuan from '../views/Bantuan.vue'
import Tunjangan from '../views/Tunjangan.vue'
import BenefitLainnya from '../views/BenefitLainnya.vue'
import HomeDetailChatView from '@/views/HomeDetailChatView.vue'
import AdminLayout from '@/layouts/AdminLayout.vue'
import AdminDashboard from '@/views/AdminDashboard.vue'
import AdminBenefit from '@/views/AdminBenefit.vue'
import AdminChatLog from '@/views/AdminChatLog.vue'


const routes = [
  {
    path: '/',
    component: MainLayout,
    children: [
      {
        path: '', // Biarkan kosong agar mendeteksi path root dari parent
        redirect: 'home' // Arahkan ke path 'home'
      },
      {
        path: 'home',
        name: 'Home',
        component: HomeView
      },
      {
        path: 'kalenderkerjaperusahaan',
        name: 'KalenderKerjaPerusahaan',
        component: KalenderKerjaPerusahaan
      },
      {
        path: 'formulirpengajuan',
        name: 'FormulirPengajuan',
        component: FormulirPengajuan,
        props: true
      },
      {
        path: 'helpdesk',
        name: 'HelpDesk',
        component: HelpDesk,
        props: true
      },
      {
        path: 'penghargaan',
        name: 'Penghargaan',
        component: Penghargaan,
        props: true
      },
      {
        path: 'bantuan',
        name: 'Bantuan',
        component: Bantuan,
        props: true
      },
      {
        path: 'tunjangan',
        name: 'Tunjangan',
        component: Tunjangan,
        props: true
      },
      {
        path: 'benefitlainnya',
        name: 'BenefitLainnya',
        component: BenefitLainnya,
        props: true
      },
      {
        path: 'homedetailchatview',
        name: 'HomeDetailChatView',
        component: HomeDetailChatView,
        props: true
      }
    ]
  },
  {
    path: '/admin',
    component: AdminLayout,
    redirect: { name: 'AdminDashboard' }, 
    children: [
      {
        // Ubah path-nya jadi sederhana saja karena sudah di bawah /admin
        path: '/admindashboard', 
        name: 'AdminDashboard',
        component: AdminDashboard
      },
      {
        // Ubah path-nya jadi sederhana saja karena sudah di bawah /admin
        path: '/adminbenefit', 
        name: 'AdminBenefit',
        component: AdminBenefit
      },
      {
        path: '/adminchatlog',
        name: 'AdminChatLog',
        component: AdminChatLog
      }
    ]
  }
]

const router = createRouter({
  history: createWebHistory(),
  routes
})

export default router