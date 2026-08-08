<script setup>
import { ref, computed, watch } from 'vue'
import { useRoute } from 'vue-router'
import SearchBar from '@/components/common/SearchBar.vue'

const route = useRoute()

/* =========================
   🔹 STATE
========================= */
const search = ref('')
const selectedItem = ref(null)
const eventList = ref([])
const searchResults = ref([])
const myAmounts = ref({}) // { benefitId: resolvedAmount }

/* =========================
   🔹 FORMAT NOMINAL
   - Kalau bisa di-parse angka -> format ribuan (Rp)
   - Kalau tidak (teks bebas seperti "1 kali gaji") -> tampilkan apa adanya
========================= */
const formatAmount = (val) => {
  if (val === null || val === undefined || val === '') return null
  const trimmed = val.toString().trim()
  const num = Number(trimmed)
  if (trimmed !== '' && !isNaN(num)) {
    return new Intl.NumberFormat('id-ID', {
      style: 'currency',
      currency: 'IDR',
      minimumFractionDigits: 0
    }).format(num)
  }
  return trimmed
}

const fetchMyAmounts = async () => {
  try {
    const res = await fetch('http://localhost:5117/api/benefits/my-amounts', {
      credentials: 'include'
    })
    const data = await res.json()
    const map = {}
    data.forEach(item => { map[item.benefitId] = item.resolvedAmount })
    myAmounts.value = map
  } catch (err) {
    console.error('Gagal fetch nominal:', err)
  }
}
fetchMyAmounts()

const selectedAmount = computed(() => {
  if (!selectedItem.value) return null
  const raw = myAmounts.value[selectedItem.value.id]
  return formatAmount(raw)
})

// =========================
// CLEAN SELECTED ITEM
// =========================
const selectedItemClean = computed(() => {
  if (!selectedItem.value) return null

  const item = JSON.parse(JSON.stringify(selectedItem.value)) // deep clone

  if (item.sections && Array.isArray(item.sections)) {
    item.sections.forEach(section => {
      if (section.details && Array.isArray(section.details)) {
        section.details = section.details
          .map(d => d.content)
          .filter((v, i, a) => a.indexOf(v) === i) // remove duplicates
          .map(c => ({ content: c })) // kembalikan ke objek
      }
    })
  }

  return item
})

/* =========================
   🔹 NAVIGATION
========================= */
const navigation = [
  { name: 'Penghargaan', label: 'Penghargaan', categoryId: 1 },
  { name: 'Bantuan', label: 'Bantuan', categoryId: 2 },
  { name: 'Tunjangan', label: 'Tunjangan', categoryId: 3 },
  { name: 'BenefitLainnya', label: 'Benefit Lainnya', categoryId: 4 }
]

/* =========================
   🔹 FETCH DATA (category 1)
========================= */
const fetchData = async (categoryId) => {
  try {
    console.log("FETCH CATEGORY:", categoryId)

    const res = await fetch(`http://localhost:5117/api/benefits/my-benefits?categoryId=${categoryId}`, {
      credentials: 'include'
    })
    const data = await res.json()

    eventList.value = data
  } catch (err) {
    console.error('Gagal fetch:', err)
  }
}


/* =========================
   🔹 AUTO FETCH
========================= */
watch(
  () => route.name,
  (newRoute) => {
    console.log("ROUTE:", newRoute)

    const selectedNav = navigation.find(n => n.name === newRoute)
    console.log("FOUND NAV:", selectedNav)
    if (selectedNav) {
      fetchData(selectedNav.categoryId)
    }
  },
  { immediate: true }
)
let timeout = null

watch(search, (val) => {
  clearTimeout(timeout)

  if (!val) {
    searchResults.value = []
    return
  }

  timeout = setTimeout(async () => {
    try {
      console.log("SEARCH INPUT:", val)

      const res = await fetch(`http://localhost:5117/api/search?q=${val}`)
      const data = await res.json()

      console.log("SEARCH RESULT:", data)

      searchResults.value = data
    } catch (err) {
      console.error('Search error:', err)
    }
  }, 300) // delay 300ms
})
/* =========================
   🔹 FILTER
========================= */
const filteredEvent = computed(() => {
  if (!search.value) return eventList.value

  const keyword = search.value.toLowerCase()

  // 1. Exact match di title
  const exactMatch = eventList.value.filter(item =>
    item.title.toLowerCase() === keyword
  )

  if (exactMatch.length > 0) return exactMatch

  // 2. Partial / fuzzy match
  return eventList.value.filter(item =>
    item.title.toLowerCase().includes(keyword) ||
    (item.description && item.description.toLowerCase().includes(keyword))
  )
})
/* =========================
   🔹 HELPER CATEGORY
========================= */
const getCategoryName = (id) => {
  const map = {
    1: 'Penghargaan',
    2: 'Bantuan',
    3: 'Tunjangan',
    4: 'Benefit Lainnya'
  }
  return map[id] || '-'
}

/* =========================
   🔹 ACTION
========================= */
const selectItem = (item) => {
  selectedItem.value = item
}
</script>

<template>
  <div class="container bantuan-container">
    
    <!-- 🔍 SEARCH -->
    <div class="mb-4">
      <SearchBar 
        v-model="search" 
        placeholder="Cari penghargaan..."
      />
    </div>

    <div class="row">

      <!-- 🔹 SIDEBAR -->
      <div class="col-md-3 mb-4">
        <div class="card p-3 shadow-sm border-0 sticky-column">
          <h6 class="fw-bold mb-3 text-dark">Kategori</h6>
          
          <div class="nav flex-column nav-pills custom-nav-sidebar">
            <router-link
              v-for="nav in navigation"
              :key="nav.name"
              :to="{ name: nav.name }"
              class="nav-link mb-2 d-flex align-items-center justify-content-between"
              :class="{ 'active': route.name === nav.name }"
            >
              {{ nav.label }}
              <span v-if="route.name === nav.name" class="active-dot"></span>
            </router-link>
          </div>
        </div>
      </div>

      <!-- 🔹 LIST -->
      <div class="col-md-4 mb-4">
        <div
          v-for="item in filteredEvent"
          :key="item.id"
          :class="[
            'card mb-3 bantuan-card shadow-sm border-0', 
            { 'active-card': selectedItem?.id === item.id }
          ]"
          @click="selectItem(item)"
        >
          <div class="card-body">
            <h6 class="fw-bold mb-1">{{ item.title }}</h6>

            <p class="text-muted mb-2 small line-clamp-2">
              {{ item.description || '-' }}
            </p>
            
            <div class="d-flex justify-content-between align-items-center mt-3">
              <span class="badge bg-light text-secondary border fw-normal">
                {{ getCategoryName(item.category) }}
              </span>

              <div v-if="selectedItem?.id === item.id">
                ✔
              </div>
            </div>
          </div>
        </div>

        <!-- 🔹 EMPTY -->
        <div v-if="filteredEvent.length === 0" class="text-center py-5 bg-white rounded shadow-sm border">
          <p class="text-muted m-0">Data penghargaan tidak ditemukan.</p>
        </div>
      </div>

      <!-- 🔹 DETAIL -->
      <div class="col-md-5 mb-4">
        <div class="card p-4 shadow-sm border-0 sticky-column" style="min-height: 400px;">

          <div v-if="selectedItem">
            <h4 class="fw-bold text-primary-dark mb-1">{{ selectedItem.title }}</h4>
            <span class="text-muted small">
              Kategori: {{ getCategoryName(selectedItem.category) }}
            </span>

            <!-- 🔹 NOMINAL BENEFIT (hasil precompute sesuai atribut karyawan) -->
            <div v-if="selectedAmount" class="mt-3 p-3 rounded" style="background:#f0f4fa;">
              <span class="text-muted small d-block">Nominal</span>
              <span class="fw-bold fs-5 text-primary-dark">{{ selectedAmount }}</span>
            </div>

            <p class="text-secondary mt-4 leading-relaxed">
              {{ selectedItem.description || '-' }}
            </p>

            <!-- 🔹 DETAIL SECTIONS -->
          <div class="mt-4" v-if="selectedItemClean">
            <div v-for="section in selectedItemClean.sections" :key="section.sectionId" class="mb-3">
            <h6 class="fw-bold small text-uppercase text-muted mb-1">{{ section.sectionTitle }}</h6>
              <ul class="list-unstyled small text-dark mb-0">
                <li v-for="detail in section.details" :key="detail.content">
                 {{ detail.content }}
                </li>
              </ul>
            </div>
          </div>

          </div>

          <!-- 🔹 EMPTY -->
          <div v-else class="h-100 d-flex flex-column align-items-center justify-content-center text-muted text-center p-4">
            <h6>Pilih Penghargaan</h6>
            <p class="small">Pilih salah satu penghargaan untuk melihat detail.</p>
          </div>

        </div>
      </div>

    </div>
  </div>
</template>