<script setup>
import SearchBar from '@/components/common/SearchBar.vue'
import CategoryCard from '@/components/benefit/CategoryCard.vue'
import coverImg from '@/assets/images/cover.jpg'
import { ref, watch } from 'vue'
import { useRouter } from 'vue-router'

// =========================
// 🔹 ROUTER & STATE
// =========================
const router = useRouter()
const search = ref('')
const suggestions = ref([])
const isLoading = ref(false)

// =========================
// 🔹 CATEGORY DATA
// =========================
const categories = [
  { title: 'Penghargaan', icon: 'cilUser', routeName: 'Penghargaan' },
  { title: 'Bantuan', icon: 'cilMoney', routeName: 'Bantuan' },
  { title: 'Tunjangan', icon: 'cilWallet', routeName: 'Tunjangan' },
  { title: 'Benefit Lainnya', icon: 'cilHappy', routeName: 'BenefitLainnya' }
]

// =========================
// 🔹 FETCH SUGGESTIONS
// =========================
const fetchSuggestions = async (query) => {
  if (!query || query.trim().length < 2) {
    suggestions.value = []
    return
  }

  isLoading.value = true
  try {
    // Pastikan port (5117) sesuai dengan running project .NET kamu
    const res = await fetch(`http://localhost:5117/api/home-search/suggestions?keyword=${encodeURIComponent(query)}`)
    const data = await res.json()
    
    let rawItems = data.suggestions || []
    const qLower = query.toLowerCase().trim()

    // 🔥 FRONTEND SORTING (Safety Net): 
    // Paksa item yang judulnya sama persis dengan input user ke urutan paling atas.
    rawItems.sort((a, b) => {
      const aTitle = (a.title || "").toLowerCase()
      const bTitle = (b.title || "").toLowerCase()
      
      if (aTitle === qLower) return -1
      if (bTitle === qLower) return 1
      return 0
    })

    // Mapping hasil untuk kebutuhan display di UI
    suggestions.value = rawItems.map(item => ({
      id: item.id,
      type: item.type,
      title: item.title, // Digunakan untuk navigasi/pertanyaan ke chat
      displayTitle: item.highlight || item.title, // Mengandung tag <em> dari Meilisearch
      subtitle: item.type === 'faq' ? item.subtitle : item.description
    })).slice(0, 8) // Limit tampilan maksimal 8 item agar tetap rapi

  } catch (err) {
    console.error('Search error:', err)
    suggestions.value = []
  } finally {
    isLoading.value = false
  }
}

// =========================
// 🔹 WATCHER & HANDLERS
// =========================
watch(search, (val) => {
  fetchSuggestions(val)
})

const goToChat = () => {
  if (!search.value.trim()) return
  router.push({
    name: 'HomeDetailChatView',
    query: { question: search.value }
  })
}

const goToDetail = (item) => {
  // Mengirim judul asli (bukan yang ada tag HTML highlight) ke halaman Chat
  router.push({
    name: 'HomeDetailChatView',
    query: { question: item.title }
  })
}

</script>

<template>
  <div class="container-fluid p-0">
    
    <!-- ========================= COVER HERO ========================= -->
    <div class="cover-hero d-flex align-items-center" :style="{ backgroundImage: `url(${coverImg})` }">
      <div class="container">
        <div class="hero-content text-start">

          <h1 class="fw-bold hero-title mb-3">
            Selamat Datang Di<br>HC Benefit Information Portal.
          </h1>

          <p class="text-muted fs-5 mb-4 d-none d-md-block">
            Akses informasi benefit dan layanan HC dalam satu pintu.
          </p>

          <div class="hero-search col-12 col-md-8 col-lg-6">

            <!-- 🔹 SEARCH BAR -->
            <SearchBar 
              v-model="search" 
              placeholder="Apa yang mau kamu tanyakan?"
              @search="goToChat"
              @keyup.enter="goToChat"
            />

            <!-- 🔹 SUGGESTION BOX -->
            <div v-if="suggestions.length > 0" class="suggestion-box">
              <div
                v-for="item in suggestions"
                :key="item.id"
                class="suggestion-item"
                @click="goToDetail(item)"
              >
                {{ item.title }}
              </div>
            </div>

          </div>

        </div>
      </div>
    </div>

    <!-- ========================= CATEGORIES ========================= -->
    <div class="container floating-categories mt-4">
      <div class="row g-0 rounded-3 shadow-lg overflow-hidden bg-white">
        <div class="col-6 col-md-3" v-for="item in categories" :key="item.title">
          <router-link :to="{ name: item.routeName }" class="text-decoration-none">
            <CategoryCard :title="item.title" :icon="item.icon" />
          </router-link>
        </div>
      </div>
    </div>

  </div>
</template>