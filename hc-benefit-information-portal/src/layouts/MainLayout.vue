<script setup>
import { computed } from 'vue'
import { useRoute } from 'vue-router'
import AppIcon from '@/components/common/AppIcon.vue'
import { ref, onMounted } from 'vue'

const route = useRoute()
const userEmail = ref('')
const showEmailModal = ref(false)

const isHome = computed(() => {
  return route.name === 'Home'
})

onMounted(() => {
  const savedEmail = sessionStorage.getItem('user_email')
  if (!savedEmail) {
    showEmailModal.value = true
  } else {
    // Jika sudah ada, pastikan modal tertutup
    showEmailModal.value = false
    userEmail.value = savedEmail
  }
})

const saveEmail = () => {
  if (userEmail.value && userEmail.value.includes('@')) {
    sessionStorage.setItem('user_email', userEmail.value)
    showEmailModal.value = false
  } else {
    alert('Mohon masukkan email yang valid')
  }
}


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
<div v-if="showEmailModal" class="email-overlay">
    <div class="email-modal-card shadow-lg">
      <div class="modal-body p-4 text-center">
        <div class="mb-3">
          <AppIcon name="cilUser" style="width: 48px; height: 48px; color: #0056b3;" />
        </div>
        <h5 class="fw-bold">Verifikasi Email</h5>
        <p class="text-muted small">Silakan masukkan email perusahaan Anda untuk mengakses informasi benefit.</p>
        
        <input 
          v-model="userEmail" 
          type="email" 
          class="form-control mb-3 text-center" 
          placeholder="nama@perusahaan.com"
          @keyup.enter="saveEmail"
        >
        
        <button class="btn btn-primary w-100 fw-bold" @click="saveEmail">
          Lanjutkan
        </button>
      </div>
    </div>
  </div>

</template>