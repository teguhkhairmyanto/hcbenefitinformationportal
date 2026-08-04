<script setup>
import { ref, onMounted } from 'vue'
import AppIcon from '@/components/common/AppIcon.vue'

// State
const benefits = ref([])
const isLoading = ref(false)
const isEdit = ref(false)
const currentId = ref(null)

// MODAL STATE
const showModal = ref(false)
// Update formData untuk detail sections
const newTag = ref('')
const formData = ref({
  title: '',
  category: 1,
  details: {  // ✅ 6 SECTIONS
    1: '', // Syarat
    2: '', // Cara Klaim
    3: '', // Dokumen
    4: '', // Limit
    5: '', // Proses Approval
    6: ''  // Catatan
  },
  tags: [],           // ✅ Tags array
  faqs: [             // ✅ FAQ array
    { question: '', answer: '' }
  ]
})
const isSubmitting = ref(false)

// 🔥 TAGS FUNCTIONS
const addTag = () => {
  const tag = newTag.value.trim()
  if (tag && !formData.value.tags.includes(tag)) {
    formData.value.tags.push(tag)
    newTag.value = ''
  }
}

const removeTag = (index) => {
  formData.value.tags.splice(index, 1)
}

// 🔥 FAQ FUNCTIONS
const addFaq = () => {
  formData.value.faqs.push({ question: '', answer: '' })
}

const removeFaq = (index) => {
  formData.value.faqs.splice(index, 1)
}

// Detail fields config
const detailFields = [
  { id: 1, label: 'Syarat', placeholder: 'Syarat untuk mengikuti benefit ini...' },
  { id: 2, label: 'Cara Klaim', placeholder: 'Langkah-langkah klaim benefit...' },
  { id: 3, label: 'Dokumen', placeholder: 'Dokumen yang diperlukan...' },
  { id: 4, label: 'Limit', placeholder: 'Batasan kuota/anggaran...' },
  { id: 5, label: 'Proses Approval', placeholder: 'Alur persetujuan...' },
  { id: 6, label: 'Catatan', placeholder: 'Informasi tambahan...' }
]

// Master kategori
const categories = [
  { category: 1, label: 'Penghargaan', value: 1 },
  { category: 2, label: 'Bantuan', value: 2 },
  { category: 3, label: 'Tunjangan', value: 3 },
  { category: 4, label: 'Benefit Lainnya', value: 4 }
]

// Fetch
const fetchBenefits = async () => {
  isLoading.value = true
  try {
    const response = await fetch('http://localhost:5117/api/benefits')
    if (response.ok) {
      benefits.value = await response.json()
    }
  } catch (error) {
    console.error('Error fetching benefits:', error)
  } finally {
    isLoading.value = false
  }
}

// MODAL ACTIONS
const openModal = () => {
  isEdit.value = false // Mode Tambah
  currentId.value = null
  formData.value = {
    title: '',
    description: '',
    category: 1,
    details: {1:'',2:'',3:'',4:'',5:'',6:''},
    tags: [],
    faqs: [{question:'', answer:''}]
  }
  newTag.value = ''
  showModal.value = true
}


const closeModal = () => {
  showModal.value = false
}

const resetForm = () => {
  formData.value = { title: '', description: '', category: 1 }
}

// Utils
const getCategoryLabel = (id) => {
  const cat = categories.find(c => c.category === id)
  return cat ? cat.label : 'N/A'
}

const handleEdit = async (item) => {
  isEdit.value = true;
  currentId.value = item.id;

  // 1. Ambil FAQ terbaru dari API (karena GetAllBenefits mungkin tidak bawa FAQ lengkap)
  const faqRes = await fetch(`http://localhost:5117/api/benefitfaq?benefitId=${item.id}`);
  let faqData = [];
  if (faqRes.ok) {
    faqData = await faqRes.json();
  }

  // 2. Map FAQ agar hanya menyisakan properti question & answer (Buang ID sampah)
  const cleanFaqs = faqData.length > 0 
    ? faqData.map(f => ({ question: f.question, answer: f.answer }))
    : [{ question: '', answer: '' }];

  // 3. Map Details (Pastikan kuncinya Integer 1-6)
  const mappedDetails = { 1: '', 2: '', 3: '', 4: '', 5: '', 6: '' };
  if (item.sections) {
    item.sections.forEach(sec => {
      if (sec.details && sec.details.length > 0) {
        mappedDetails[sec.sectionId] = sec.details[0].content;
      }
    });
  }

  // 4. Set ke formData
  formData.value = {
    title: item.title || '',
    description: item.description || '',
    category: item.category || 1,
    details: mappedDetails,
    tags: Array.isArray(item.tags) ? [...item.tags] : [],
    faqs: cleanFaqs
  };

  showModal.value = true;
}

const handleSubmit = async () => {
  // 1. Validasi Sederhana
  if (!formData.value.title) {
    alert("Judul wajib diisi");
    return;
  }

  isSubmitting.value = true;

  // 2. Siapkan Payload (Struktur ini sama untuk Baru maupun Edit)
  const benefitPayload = {
    Title: formData.value.title,
    Description: formData.value.description,
    Category: parseInt(formData.value.category),
    Details: formData.value.details,
    Tags: formData.value.tags,
    Faqs: formData.value.faqs
      .filter(f => f.question.trim() !== '') // Buang FAQ kosong
      .map(f => ({
        question: f.question,
        answer: f.answer
      }))
  };

  // 3. Tentukan URL dan Method berdasarkan status isEdit
  const url = isEdit.value 
    ? `http://localhost:5117/api/benefits/${currentId.value}` // URL Edit
    : `http://localhost:5117/api/benefits`;                  // URL Baru

  const method = isEdit.value ? 'PUT' : 'POST';

  try {
    const res = await fetch(url, {
      method: method,
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(benefitPayload)
    });

    if (res.ok) {
      alert(isEdit.value ? 'Update Berhasil!' : 'Data Berhasil Disimpan!');
      closeModal();
      fetchBenefits(); // Refresh tabel
    } else {
      const errorData = await res.json();
      alert("Gagal: " + (errorData.message || "Terjadi kesalahan server"));
    }
  } catch (error) {
    console.error("Error:", error);
    alert("Gagal menghubungi server.");
  } finally {
    isSubmitting.value = false;
  }
}

const handleDelete = async (item) => {
  if (!confirm(`Apakah Anda yakin ingin menghapus benefit "${item.title}"?`)) return

  try {
    // Kita kirim partial update untuk menonaktifkan
    const response = await fetch(`http://localhost:5117/api/benefits/${item.id}`, {
      method: 'DELETE', // Atau PUT tergantung konfigurasi Backend Anda untuk soft delete
      headers: { 'Content-Type': 'application/json' }
    })

    if (response.ok) {
      await fetchBenefits() // Refresh tabel
      console.log('✅ Benefit dinonaktifkan')
    }
  } catch (error) {
    console.error('❌ Gagal menghapus:', error)
  }
}

const submitForm = async () => {
  console.log("Data yang akan dikirim:", JSON.stringify(formData.value, null, 2));
  isSubmitting.value = true
  try {
    // KONVERSI details (objek) ke format sections (array) agar Backend tidak error
    const mappedSections = Object.keys(formData.value.details).map(key => ({
      sectionId: parseInt(key),
      details: [{ content: formData.value.details[key] }]
    }))

    const benefitPayload = {
      title: formData.value.title,
      description: formData.value.description,
      category: formData.value.category,
      sections: mappedSections, // Gunakan nama 'sections' sesuai format API Anda
      tags: formData.value.tags,
      faqs: formData.value.faqs
    }

    const url = isEdit.value 
      ? `http://localhost:5117/api/benefits/${currentId.value}` 
      : 'http://localhost:5117/api/benefits'
    
    // Pastikan method sesuai
    const method = isEdit.value ? 'PUT' : 'POST'

    const res = await fetch(url, {
      method: method,
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(benefitPayload)
    })

    if (res.ok) {
      await fetchBenefits()
      closeModal()
      alert('Data berhasil disimpan!')
    } else {
      const errorData = await res.json()
      alert('Gagal menyimpan: ' + (errorData.message || res.statusText))
    }
  } catch (error) {
    console.error('Error submit:', error)
    alert('Terjadi kesalahan koneksi ke server.')
  } finally {
    isSubmitting.value = false
  }
}

onMounted(() => {
  fetchBenefits()
})



</script>

<template>
  <div class="benefit-page">
    <!-- HEADER + TAMBAH BUTTON -->
    <div class="d-flex justify-content-between align-items-center mb-5">
      <div>
        <h2 class="mb-1 fw-bold text-dark">Kelola Benefit</h2>
        <p class="text-muted mb-0">Daftar semua benefit karyawan</p>
      </div>
      
      <button class="btn btn-primary rounded-pill px-4 py-2 d-flex align-items-center gap-2 shadow-sm"
              @click="openModal">
        <AppIcon name="cilPlus" />
        <span>Tambah Benefit</span>
      </button>
    </div>

    <!-- TABLE -->
    <div class="card border-0 shadow-sm rounded-4 overflow-hidden mb-5">
      <div class="card-body p-0">
        <div class="table-responsive">
          <table class="table table-hover align-middle mb-0">
            <thead>
              <tr class="bg-light">
                <th class="ps-4 py-3 text-muted small text-uppercase">ID</th>
                <th class="py-3 text-muted small text-uppercase">Nama Benefit</th>
                <th class="py-3 text-muted small text-uppercase text-center">Kategori</th>
                <th class="py-3 text-muted small text-uppercase">Deskripsi</th>
                <th class="text-end pe-4 py-3 text-muted small text-uppercase">Aksi</th>
              </tr>
            </thead>

            <tbody>
              <!-- LOADING -->
              <tr v-if="isLoading">
                <td colspan="5" class="text-center py-5">
                  <div class="spinner-border text-primary me-2" role="status"></div>
                  Memuat data...
                </td>
              </tr>

              <!-- EMPTY -->
              <tr v-else-if="benefits.length === 0">
                <td colspan="5" class="text-center py-5 text-muted">
                  <AppIcon name="cilInbox" class="mb-2 fs-1 text-muted" />
                  <div>Belum ada data benefit</div>
                </td>
              </tr>

              <!-- DATA -->
              <tr v-for="item in benefits" :key="item.id">
                <td class="ps-4 small">#{{ item.id }}</td>
                <td>
                  <div class="fw-semibold">{{ item.title }}</div>
                </td>
                <td class="text-center">
                  <span class="badge bg-primary-subtle text-primary px-3 py-1 rounded-pill fw-semibold small">
                    {{ getCategoryLabel(item.categoryId || item.category) }}
                  </span>
                </td>
                <td class="small">
                  <div class="text-muted text-truncate" style="max-width: 300px;">
                    {{ item.description }}
                  </div>
                </td>
                <td class="text-end pe-4">
                  <div class="btn-group btn-group-sm" role="group">
                    <button class="btn btn-outline-primary rounded-3 px-3"
                            @click="handleEdit(item)"
                            title="Edit">
                      <AppIcon name="cilPencil" />
                    </button>
                    <button class="btn btn-outline-danger rounded-3 px-3"
                            @click="handleDelete(item)"
                            title="Hapus">
                      <AppIcon name="cilTrash" />
                    </button>
                  </div>
                </td>
              </tr>
            </tbody>
          </table>
        </div>
      </div>
    </div>

    <!-- ✅ MODAL FORM TAMBAH BENEFIT -->
    <Teleport to="body">
    <div 
    class="modal fade" 
    :class="{ show: showModal, 'd-block': showModal }" 
    tabindex="-1" 
    style="z-index: 1085 !important; background: rgba(0,0,0,0.7);"
    @click.self="closeModal">
    <div class="modal-dialog modal-xl modal-dialog-centered modal-dialog-scrollable" @click.stop>
      <div class="modal-content border-0 shadow-lg rounded-4 overflow-hidden" style="z-index: 1086 !important;">
        
        <!-- HEADER -->
        <div class="modal-header border-0 pb-3 bg-white">
          <div class="d-flex align-items-center gap-3">
            <div class="bg-primary bg-opacity-10 p-3 rounded-3">
              <AppIcon name="cilPlus" class="text-primary fs-3" />
            </div>
            <div>
              <h4 class="modal-title fw-bold mb-0">{{ isEdit ? 'Edit Benefit' : 'Tambah Benefit Baru' }}</h4>
              <small class="text-muted">{{ isEdit ? 'Perbarui informasi benefit ID: #' + currentId : 'Lengkapi semua informasi benefit' }}</small>
            </div>
          </div>
          <button type="button" class="btn-close btn-close-white" @click="closeModal"></button>
        </div>

        <!-- BODY -->
        <div class="modal-body p-4 pt-0 max-h-75vh">
          <form @submit.prevent="handleSubmit">
            
            <!-- 1. TITLE + CATEGORY (SAMA) -->
            <div class="row mb-5">
              <div class="col-md-8">
                <label class="form-label fw-semibold mb-2">Nama Benefit <span class="text-danger">*</span></label>
                <input v-model="formData.title" type="text" class="form-control rounded-3 shadow-sm lh-lg" placeholder="Contoh: Family Gathering 2024" required>
              </div>
              <div class="col-md-4">
                <label class="form-label fw-semibold mb-2">Kategori <span class="text-danger">*</span></label>
                <select v-model="formData.category" class="form-select rounded-3 shadow-sm lh-lg" required>
                  <option v-for="cat in categories" :key="cat.value" :value="cat.value">{{ cat.label }}</option>
                </select>
              </div>
            </div>
            <!-- 2. DESKRIPSI) -->
            <div class="row mb-5">
            <div class="col-12">
              <label class="form-label fw-semibold mb-2">Deskripsi Singkat</label>
              <textarea 
                v-model="formData.description" 
                class="form-control rounded-3 shadow-sm" 
                rows="2" 
                placeholder="Berikan ringkasan singkat mengenai benefit ini..."
              ></textarea>
              <small class="text-muted">Deskripsi ini akan muncul pada kartu benefit di halaman depan.</small>
            </div>
            </div>
            <!-- 2. DETAIL SECTIONS 6 FIELDS (SAMA) -->
            <div class="mb-5">
              <h6 class="fw-bold mb-4 pb-2 border-bottom">
                <AppIcon name="cilNotes" class="me-2 text-primary" />
                Detail Benefit
              </h6>
              <div class="row g-4">
                <div v-for="field in detailFields" :key="field.id" class="col-lg-6 col-md-6">
                  <div class="h-100">
                    <label class="form-label fw-semibold mb-2 d-flex align-items-center gap-2">
                      <span class="badge bg-light text-dark rounded-pill px-2 py-1 fs-6">{{ field.id }}</span>
                      {{ field.label }}
                    </label>
                    <textarea 
                      v-model="formData.details[field.id]"
                      :placeholder="field.placeholder"
                      rows="3"
                      class="form-control rounded-3 shadow-sm resize-vertical"
                    ></textarea>
                  </div>
                </div>
              </div>
            </div>

            <!-- 🔥 3. TAGS INPUT -->
            <div class="mb-5">
              <label class="h6 fw-bold mb-3 d-flex align-items-center gap-2">
                <AppIcon name="cilTag" class="text-primary fs-5" />
                Tags
              </label>
              
              <!-- TAG INPUT -->
              <div class="input-group mb-3">
                <input 
                  v-model="newTag"
                  @keyup.enter="addTag"
                  type="text" 
                  class="form-control rounded-3 shadow-sm" 
                  placeholder="Ketik tag (tekan Enter untuk tambah)"
                >
                <button class="btn btn-outline-primary rounded-3" type="button" @click="addTag">
                  <AppIcon name="cilPlus" />
                </button>
              </div>

              <!-- TAG LIST -->
              <div v-if="formData.tags.length" class="d-flex flex-wrap gap-2 p-3 bg-light rounded-3">
                <span 
                  v-for="(tag, index) in formData.tags" 
                  :key="index"
                  class="badge bg-primary text-white px-3 py-2 rounded-pill fw-semibold position-relative"
                >
                  {{ tag }}
                  <button 
                    class="btn-close btn-close-white ms-1 p-1 position-absolute" 
                    style="top: 0; right: 0;"
                    @click="removeTag(index)"
                    type="button"
                  ></button>
                </span>
              </div>
              <small v-else class="text-muted">Belum ada tags (opsional)</small>
            </div>

            <!-- 🔥 4. FAQ INPUT -->
            <div class="mb-5">
              <label class="h6 fw-bold mb-4 d-flex align-items-center gap-2">
                <AppIcon name="cilSpeech" class="text-primary fs-5" />
                FAQ (Pertanyaan Umum)
              </label>
              
              <!-- FAQ ITEMS -->
              <div v-for="(faq, index) in formData.faqs" :key="index" class="faq-item mb-4 p-4 border rounded-3 shadow-sm">
                <div class="row g-3 align-items-end">
                  <div class="col-md-5">
                    <label class="form-label fw-semibold mb-2">Pertanyaan</label>
                    <input 
                      v-model="faq.question"
                      type="text"
                      class="form-control rounded-3"
                      placeholder="Contoh: Apa syarat mengikuti event ini?"
                    >
                  </div>
                  <div class="col-md-5">
                    <label class="form-label fw-semibold mb-2">Jawaban</label>
                    <textarea 
                      v-model="faq.answer"
                      rows="2"
                      class="form-control rounded-3 resize-vertical"
                      placeholder="Jawaban lengkap..."
                    ></textarea>
                  </div>
                  <div class="col-md-2">
                    <button 
                      class="btn btn-outline-danger w-100 rounded-3 h-100"
                      @click="removeFaq(index)"
                    >
                      <AppIcon name="cilTrash" />
                    </button>
                  </div>
                </div>
              </div>

              <!-- ADD FAQ BUTTON -->
              <button 
                type="button"
                class="btn btn-outline-primary rounded-3 px-4 py-2"
                @click="addFaq"
              >
                <AppIcon name="cilPlus" class="me-2" />
                Tambah FAQ
              </button>
            </div>

            <!-- ACTIONS -->
            <div class="d-flex gap-3 pt-4 border-top">
              <button 
                type="button" 
                class="btn btn-outline-secondary flex-fill rounded-3 py-2"
                @click="closeModal"
                :disabled="isSubmitting"
              >
                <AppIcon name="cilXCircle" class="me-2" />
                Batal
              </button>
              <button 
  type="submit"
  class="btn btn-primary flex-fill rounded-3 py-2"
  :disabled="isSubmitting || !formData.title"
>
  <span v-if="isSubmitting" class="spinner-border spinner-border-sm me-2"></span>
  <AppIcon v-else :name="isEdit ? 'cilSave' : 'cilCheckCircle'" class="me-2" />
  {{ isSubmitting ? 'Menyimpan...' : (isEdit ? 'Simpan Perubahan' : 'Simpan Benefit') }}
</button>
            </div>

          </form>
        </div>
      </div>
      </div>
    </div>
  </Teleport>

    <!-- SPACER UNTUK FOOTER -->
    <div style="height: 80px;"></div>
  </div>
</template>