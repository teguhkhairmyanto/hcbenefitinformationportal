<script setup>
import { ref } from 'vue'
import { useRouter } from 'vue-router'

const router = useRouter()

const nrp = ref('')
const tanggalLahir = ref('')
const isLoading = ref(false)
const errorMessage = ref('')

const handleLogin = async () => {
  errorMessage.value = ''

  if (!nrp.value || !tanggalLahir.value) {
    errorMessage.value = 'NRP dan tanggal lahir wajib diisi.'
    return
  }

  isLoading.value = true

  try {
    const res = await fetch('http://localhost:5117/api/auth/login', {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      credentials: 'include', // WAJIB: supaya browser menyimpan httpOnly cookie dari response
      body: JSON.stringify({
        nrp: nrp.value,
        tanggalLahir: tanggalLahir.value
      })
    })

    if (res.status === 429) {
      errorMessage.value = 'Terlalu banyak percobaan gagal. Coba lagi dalam 15 menit.'
      return
    }

    if (res.status === 401) {
      errorMessage.value = 'NRP atau tanggal lahir tidak sesuai.'
      return
    }

    if (!res.ok) {
      errorMessage.value = 'Terjadi kesalahan. Silakan coba lagi.'
      return
    }

    const data = await res.json()

    // Simpan info dasar untuk ditampilkan di UI (nama, role) - bukan untuk otentikasi,
    // karena otentikasi sesungguhnya ada di httpOnly cookie yang tidak bisa diakses JS
    sessionStorage.setItem('hc_user', JSON.stringify(data))

    router.push({ name: 'Home' })
  } catch (err) {
    console.error('Login error:', err)
    errorMessage.value = 'Tidak bisa terhubung ke server. Pastikan server berjalan.'
  } finally {
    isLoading.value = false
  }
}
</script>

<template>
  <div class="login-page">
    <div class="login-card">
      <div class="login-logo">HC Portal</div>
      <h1 class="login-title">Masuk ke Portal Benefit</h1>
      <p class="login-subtitle">Gunakan NRP dan tanggal lahir Anda untuk melihat benefit yang berlaku.</p>

      <form @submit.prevent="handleLogin">
        <div class="form-group">
          <label for="nrp">NRP</label>
          <input
            id="nrp"
            v-model="nrp"
            type="text"
            placeholder="Masukkan NRP Anda"
            autocomplete="username"
          />
        </div>

        <div class="form-group">
          <label for="tanggalLahir">Tanggal Lahir</label>
          <input
            id="tanggalLahir"
            v-model="tanggalLahir"
            type="date"
            autocomplete="bday"
          />
        </div>

        <p v-if="errorMessage" class="login-error">{{ errorMessage }}</p>

        <button type="submit" class="login-button" :disabled="isLoading">
          {{ isLoading ? 'Memproses...' : 'Masuk' }}
        </button>
      </form>
    </div>
  </div>
</template>

<style scoped>
.login-page {
  min-height: 100vh;
  width: 100%;
  display: flex;
  align-items: center;
  justify-content: center;
  background: linear-gradient(135deg, #1a2b4c 0%, #2d4a7c 100%);
  padding: 20px;
}

.login-card {
  background: #fff;
  border-radius: 12px;
  padding: 40px;
  width: 100%;
  max-width: 400px;
  box-shadow: 0 10px 40px rgba(0, 0, 0, 0.2);
}

.login-logo {
  font-weight: bold;
  font-size: 1.1rem;
  color: #2d4a7c;
  margin-bottom: 24px;
}

.login-title {
  font-size: 1.4rem;
  font-weight: 600;
  color: #1a1a1a;
  margin: 0 0 8px 0;
}

.login-subtitle {
  font-size: 0.9rem;
  color: #666;
  margin: 0 0 28px 0;
}

.form-group {
  margin-bottom: 18px;
}

.form-group label {
  display: block;
  font-size: 0.85rem;
  font-weight: 500;
  color: #333;
  margin-bottom: 6px;
}

.form-group input {
  width: 100%;
  padding: 10px 12px;
  border: 1px solid #d0d0d0;
  border-radius: 8px;
  font-size: 0.95rem;
  box-sizing: border-box;
}

.form-group input:focus {
  outline: none;
  border-color: #2d4a7c;
  box-shadow: 0 0 0 3px rgba(45, 74, 124, 0.12);
}

.login-error {
  color: #c0392b;
  font-size: 0.85rem;
  margin: -8px 0 16px 0;
}

.login-button {
  width: 100%;
  padding: 12px;
  background: #2d4a7c;
  color: #fff;
  border: none;
  border-radius: 8px;
  font-size: 0.95rem;
  font-weight: 600;
  cursor: pointer;
  transition: background 0.2s;
}

.login-button:hover:not(:disabled) {
  background: #1a2b4c;
}

.login-button:disabled {
  opacity: 0.6;
  cursor: not-allowed;
}
</style>