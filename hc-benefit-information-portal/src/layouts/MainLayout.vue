<script setup>
import { computed } from 'vue'
import { useRoute } from 'vue-router'
import AppIcon from '@/components/common/AppIcon.vue'

const route = useRoute()

const isHome = computed(() => {
  return route.name === 'Home'
})
</script>

<template>
  <!-- Header -->
  <header :class="['header', { 'header-home': isHome }]">
    <div class="logo">HC Portal</div>

    <div class="menu">
      <router-link :to="{ name: 'Home' }" :class="{ active: $route.name === 'Home' }" class="menu-item">
        <AppIcon name="cilHome" class="icon" />
        Home
      </router-link>

      <router-link :to="{ name: 'KalenderKerjaPerusahaan' }" :class="{ active: $route.name === 'KalenderKerjaPerusahaan' }" class="menu-item">
        <AppIcon name="cilCalendar" class="icon" />
        Kalender Kerja Perusahaan
      </router-link>

      <router-link :to="{ name: 'FormulirPengajuan' }" :class="{ active: $route.name === 'FormulirPengajuan' }" class="menu-item">
        <AppIcon name="cilSpreadsheet" class="icon" />
        Formulir & Pengajuan
      </router-link>

      <router-link :to="{ name: 'HelpDesk' }" :class="{ active: $route.name === 'HelpDesk' }"  class="menu-item">
        <AppIcon name="cilHeadphones" class="icon" />
        Helpdesk
      </router-link>
    </div>
  </header>

  <!-- Main Content -->
  <main class="main">
      <router-view />
  </main>

  <!-- Footer -->
  <footer class="footer">
  <div class="footer-content">
    
    <div class="footer-left">
      <div class="footer-logo">HC Portal</div>
      <p class="footer-desc">
        Akses informasi benefit dan layanan Human Capital dalam satu platform.
      </p>
    </div>

    <div class="footer-menu">
      <router-link to="/home" class="footer-link">Home</router-link>
      <router-link to="/kalenderkerjaperusahaan" class="footer-link">Kalender Kerja Perusahaan</router-link>
      <router-link to="/formulirpengajuan" class="footer-link">Formulir & Pengajuan</router-link>
      <router-link to="/helpdesk" class="footer-link">Helpdesk</router-link>
    </div>

    <div class="footer-menu">
      <router-link to="/" class="footer-link">Penghargaan</router-link>
      <router-link to="/" class="footer-link">Bantuan</router-link>
      <router-link to="/" class="footer-link">Tunjangan</router-link>
      <router-link to="/" class="footer-link">Benefit Lainnya</router-link>
    </div>

  </div>

  <div class="footer-bottom">
    © 2026 HC Portal
  </div>
</footer>

</template>