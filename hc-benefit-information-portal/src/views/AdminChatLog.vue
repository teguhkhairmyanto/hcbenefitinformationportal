<script setup>
import { ref, onMounted } from 'vue'

const logs = ref([])
const isLoading = ref(false)

// 1. Ambil data unanswered dari Backend
const fetchUnanswered = async () => {
  isLoading.value = true
  try {
    const res = await fetch('http://localhost:5117/api/chat-log/unanswered')
    if (res.ok) {
      logs.value = await res.json()
    }
  } catch (err) {
    console.error("Gagal load data admin:", err)
  } finally {
    isLoading.value = false
  }
}

const showConfirmModal = ref(false)
const pendingLogId = ref(null)

const triggerConfirm = (id) => {
  pendingLogId.value = id
  showConfirmModal.value = true // Munculkan modal custom Anda
}

// 2. Fungsi EKSEKUSI (dipanggil dari tombol "Ya, Selesai" di dalam modal)
const markAsDone = async () => {
  // Ambil ID dari ref yang sudah diset saat triggerConfirm
  const id = pendingLogId.value
  
  // HAPUS BARIS confirm(...) BAWAAN BROWSER DI SINI
  // if (!confirm('...')) return <--- Hapus/Komentari baris ini
  
  try {
    const res = await fetch(`http://localhost:5117/api/chat-log/mark-answered/${id}`, {
      method: 'PUT'
    })
    
    if (res.ok) {
      // Update tampilan local
      logs.value = logs.value.filter(log => log.id !== id)
      // Tutup modal custom setelah berhasil
      showConfirmModal.value = false 
    }
  } catch (err) {
    alert('Gagal memperbarui status')
  }
}

onMounted(() => {
  fetchUnanswered()
})

const showModal = ref(false)
const selectedLog = ref(null)
const adminResponse = ref('')
const isSending = ref(false)

const openReplyModal = (log) => {
  selectedLog.value = log
  adminResponse.value = ''
  showModal.value = true
}

const sendResponse = async () => {
  if (!adminResponse.value.trim()) return alert('Isi jawaban dulu ya!')
  
  isSending.value = true
  try {
    const res = await fetch('http://localhost:5117/api/chat-log/send-response', {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({
        id: selectedLog.value.id,
        email: selectedLog.value.email,
        question: selectedLog.value.question,
        answer: adminResponse.value
      })
    })

    if (res.ok) {
      alert('Jawaban berhasil terkirim ke email karyawan!')
      showModal.value = false
      logs.value = logs.value.filter(l => l.id !== selectedLog.value.id)
    }
  } catch (err) {
    alert('Gagal mengirim jawaban')
  } finally {
    isSending.value = false
  }
}
</script>

<template>
  <div class="chatlog-page">
    <div class="d-flex justify-content-between align-items-end mb-4">
      <div>
        <h2 class="h2 fw-bold text-dark mb-1">Follow Up Pertanyaan</h2>
        <p class="text-muted small mb-0">Kelola pertanyaan karyawan yang memerlukan jawaban manual.</p>
      </div>
      <div class="text-end">
        <span class="badge bg-primary bg-opacity-10 text-primary px-3 py-2 rounded-pill fw-bold">
          {{ logs.length }} Pertanyaan Menunggu
        </span>
      </div>
    </div>

    <div class="card border-0 shadow-sm rounded-4 overflow-hidden mb-5">
      <div class="table-responsive">
        <table class="table table-hover align-middle mb-0">
          <thead class="bg-light border-bottom">
            <tr>
              <th class="ps-4 py-3 text-uppercase small fw-bold text-secondary">Created Date</th>
              <th class="py-3 text-uppercase small fw-bold text-secondary">Email User</th>
              <th class="py-3 text-uppercase small fw-bold text-secondary">Detail Pertanyaan</th>
              <th class="pe-4 py-3 text-center text-uppercase small fw-bold text-secondary">Aksi</th>
            </tr>
          </thead>
          <tbody>
            <tr v-if="logs.length === 0 && !isLoading">
              <td colspan="4" class="text-center py-5">
                <div class="py-4">
                  <h2 class="mb-3">🎉</h2>
                  <h6 class="fw-bold text-dark">Luar Biasa!</h6>
                  <p class="text-muted small">Semua pertanyaan sudah terjawab dan bersih.</p>
                </div>
              </td>
            </tr>

            <tr v-for="log in logs" :key="log.id">
              <td class="ps-4">
                <span class="text-dark small">{{ log.CreatedAt }}</span>
              </td>
              <td>
                <span class="text-primary fw-medium small">{{ log.email }}</span>
              </td>
              <td class="text-wrap" style="max-width: 400px;">
                <p class="mb-0 text-dark small leading-relaxed">"{{ log.question }}"</p>
              </td>
              <td class="pe-4 text-center">
                <div class="d-flex gap-2 justify-content-center">
                  <button @click="openReplyModal(log)" class="btn btn-primary btn-sm px-3 rounded-3">
                    Balas Chat
                  </button>
                 <button @click="triggerConfirm(log.id)" class="btn btn-outline-success btn-sm px-3 rounded-pill d-flex align-items-center gap-1">
  <AppIcon name="cilCheckCircle" size="sm" />
  <span>Selesaikan</span>
</button>
                </div>
              </td>
            </tr>
          </tbody>
        </table>
      </div>
    </div>

    <Teleport to="body">
      <div 
        class="modal fade" 
        :class="{ show: showModal, 'd-block': showModal }" 
        tabindex="-1" 
        style="z-index: 1085 !important; background: rgba(0,0,0,0.7);"
        @click.self="showModal = false"
      >
        <div class="modal-dialog modal-lg modal-dialog-centered" @click.stop>
          <div class="modal-content border-0 shadow-lg rounded-4 overflow-hidden" style="z-index: 1086 !important;">
            
            <div class="modal-header border-0 pb-3 bg-white p-4">
              <div class="d-flex align-items-center gap-3">
                <div class="bg-primary bg-opacity-10 p-3 rounded-3">
                  <i class="bi bi-chat-left-dots text-primary fs-3"></i>
                </div>
                <div>
                  <h4 class="modal-title fw-bold mb-0">Balas Pertanyaan</h4>
                  <small class="text-muted">Kirim jawaban langsung ke email karyawan</small>
                </div>
              </div>
              <button type="button" class="btn-close" @click="showModal = false"></button>
            </div>

            <div class="modal-body p-4 pt-0">
          <div class="rounded-4 overflow-hidden border mb-4" style="border-color: #dee2e6 !important;">
            
            <div class="p-3 bg-light border-bottom" style="border-color: #dee2e6 !important;">
              <div class="row align-items-center">
                <div class="col-md-3">
                  <small class="text-uppercase fw-bold text-secondary" style="font-size: 0.7rem; letter-spacing: 0.5px;">From</small>
                </div>
                <div class="col-md-9 text-end text-md-start">
                  <span class="text-primary fw-bold small">{{ selectedLog?.email }}</span>
                </div>
              </div>
            </div>
        
            <div class="p-3 bg-white border-bottom" style="border-color: #dee2e6 !important;">
              <div class="row">
                <div class="col-md-3">
                  <small class="text-uppercase fw-bold text-secondary" style="font-size: 0.7rem; letter-spacing: 0.5px;">Pertanyaan</small>
                </div>
                <div class="col-md-9">
                  <p class="mb-0 text-dark small leading-relaxed italic text-muted">
                    "{{ selectedLog?.question }}"
                  </p>
                </div>
              </div>
            </div>
        
            <div class="p-3 bg-white">
              <div class="row">
                <div class="col-md-3">
                  <label class="text-uppercase fw-bold text-secondary mb-2 mb-md-0 d-block" style="font-size: 0.7rem; letter-spacing: 0.5px;">Jawaban</label>
                </div>
                <div class="col-md-9">
                  <textarea 
                    v-model="adminResponse" 
                    class="form-control border-0 shadow-none p-0 small" 
                    rows="6" 
                    placeholder="Ketik jawaban Anda di sini..."
                    style="resize: none; background: transparent;"
                    required
                  ></textarea>
                </div>
              </div>
            </div>
        
          </div>
        
          <div class="d-flex align-items-center gap-2 mb-4 px-2">
            <AppIcon name="cilInfo" class="text-muted" size="sm" />
            <small class="text-muted" style="font-size: 0.75rem;">
              Jawaban ini akan langsung dikirimkan ke email karyawan yang bersangkutan.
            </small>
          </div>
        
          <div class="d-flex gap-3 pt-4 border-top" style="border-color: #dee2e6 !important;">
            <button 
              type="button" 
              class="btn btn-outline-secondary flex-fill rounded-3 py-2"
              @click="showModal = false"
              :disabled="isSending"
            >
              <AppIcon name="cilX" class="me-1" /> Batal
            </button>
            <button 
              @click="sendResponse"
              type="button"
              class="btn btn-primary flex-fill rounded-3 py-2"
              :disabled="isSending || !adminResponse"
            >
              <span v-if="isSending" class="spinner-border spinner-border-sm me-2"></span>
              <AppIcon v-else name="cilCursor" class="me-1" />
              {{ isSending ? 'Mengirim...' : 'Kirim Jawaban' }}
            </button>
          </div>
        </div>
        </div>
        </div>
      </div>
    </Teleport>
    <Teleport to="body">
  <div 
    class="modal fade" 
    :class="{ show: showConfirmModal, 'd-block': showConfirmModal }" 
    tabindex="-1" 
    style="z-index: 1090 !important; background: rgba(0,0,0,0.5);"
  >
    <div class="modal-dialog modal-sm modal-dialog-centered">
      <div class="modal-content border-0 shadow rounded-4 overflow-hidden">
        <div class="modal-body p-4 text-center">
          <div class="bg-light d-inline-block p-3 rounded-circle mb-3">
             <AppIcon name="cilCheckCircle" class="text-success fs-2" />
          </div>
          
          <h5 class="fw-bold text-dark">Konfirmasi Selesai</h5>
          <p class="text-muted small">Tandai pertanyaan ini sudah dijawab tanpa mengirim email?</p>

          <div class="d-flex gap-2 mt-4">
            <button 
              @click="showConfirmModal = false" 
              class="btn btn-light flex-fill rounded-3 small"
              style="border: 1px solid #dee2e6;"
            >
              Batal
            </button>
            <button 
              @click="markAsDone" 
              class="btn btn-success flex-fill rounded-3 small"
            >
              Ya, Selesai
            </button>
          </div>
        </div>
      </div>
    </div>
  </div>
</Teleport>
  </div>
  
</template>