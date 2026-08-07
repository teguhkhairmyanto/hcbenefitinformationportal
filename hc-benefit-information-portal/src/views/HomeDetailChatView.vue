<script setup>
// 🔹 PERBAIKAN: Pastikan nextTick diimport
import { ref, onMounted, nextTick, watch } from 'vue' 
import { useRoute } from 'vue-router'
import SearchBar from '@/components/common/SearchBar.vue'

// =========================
// 🔹 STATE
// =========================
const route = useRoute()
const chatHistory = ref([])
const inputText = ref('')
const isLoading = ref(false)
const chatContainer = ref(null)
const suggestions = ref([])


// =========================
// 🔹 HELPER: SCROLL TO BOTTOM
// =========================
const scrollToBottom = async () => {
  await nextTick()
  if (chatContainer.value) {
    chatContainer.value.scrollTo({
      top: chatContainer.value.scrollHeight,
      behavior: 'smooth'
    })
  }
}

const fetchSuggestions = async (query) => {
  if (!query || query.trim().length < 2) {
    suggestions.value = []
    return
  }
  try {
    const res = await fetch(`http://localhost:5117/api/home-search/suggestions?keyword=${encodeURIComponent(query)}`)
    const data = await res.json()
    suggestions.value = (data.suggestions || []).map(item => ({
      id: item.id,
      type: item.type,
      title: item.title,
      displayTitle: item.highlight || item.title,
      subtitle: item.type === 'faq' ? item.subtitle : item.description
    })).slice(0, 5) // Limit 5 saja agar tidak menutupi chat
  } catch (err) {
    console.error(err)
  }
}

// 🔹 Watcher untuk input text
watch(inputText, (newVal) => {
  fetchSuggestions(newVal)
})

const fetchAnswer = async (question) => {
  if (!question) return;
  
  suggestions.value = [] // Tutup suggestion saat mulai bertanya
  isLoading.value = true;
  chatHistory.value.push({ role: 'user', sender: 'Anda', text: question });
  
  await scrollToBottom();

  try {
    const response = await fetch(`http://localhost:5117/api/home-search/suggestions?keyword=${encodeURIComponent(question)}`);
    if (!response.ok) throw new Error(`Server error: ${response.status}`);
    const data = await response.json();
    
    let answer = 'Maaf, saya tidak menemukan informasi tersebut. Pertanyaan Anda sudah kami teruskan ke Admin, Mohon menunggu email dari Admin';
    let status = 'unanswered'; // Default jika tidak ketemu

    if (data.bestAnswer?.answer) {
      answer = data.bestAnswer.answer;
      status = 'answered';
    } else if (data.suggestions?.length > 0) {
      const first = data.suggestions[0];
      answer = first.type === 'faq' ? first.answer : first.description;
      status = 'answered'; // Ketemu di suggestion pertama
    }

    chatHistory.value.push({ role: 'bot', sender: 'HC Portal', text: answer });
    saveChatToLog(question, answer, status);

  } catch (err) {
    chatHistory.value.push({ role: 'bot', sender: 'HC Portal', text: 'Koneksi terputus.' });
  } finally {
    isLoading.value = false;
    await scrollToBottom();
  }
};

// 🔹 Pilih suggestion
const selectSuggestion = (item) => {
  inputText.value = ''
  suggestions.value = []
  fetchAnswer(item.title)
}

const saveChatToLog = async (question, answer, status) => {
  try {
    await fetch('http://localhost:5117/api/chat-log', {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      credentials: 'include',
      body: JSON.stringify({
        question: question,
        answerPreview: answer,
        status: status
      })
    });
  } catch (err) {
    console.error("Gagal mencatat log ke database", err);
  }
}

onMounted(() => {
  const question = route.query.question
  if (question) fetchAnswer(question)
})


const sendMessage = () => {
  const q = inputText.value.trim()
  if (!q) return
  fetchAnswer(q)
  inputText.value = ''
}
</script>

<template>
  <div class="chat-page-container">
    <div class="chat-window shadow-lg">
      
      <div class="chat-header">
        <div class="d-flex align-items-center">
          <div class="bot-logo-circle me-3">HC</div>
          <div>
            <h6 class="mb-0 fw-bold">HC Benefit Information Portal</h6>
            <small class="text-success">● <span class="text-muted">Online Assistant</span></small>
          </div>
        </div>
      </div>

      <div class="chat-messages" ref="chatContainer">
        <div v-for="(msg, index) in chatHistory" :key="index" 
             :class="['message-row', msg.role === 'user' ? 'user-row' : 'bot-row']">
          
          <div v-if="msg.role === 'bot'" class="avatar bot-avatar">HC</div>

          <div class="message-content">
            <div class="message-bubble shadow-sm">
              <div v-html="msg.text"></div>
            </div>
            <span class="message-time">{{ msg.sender }}</span>
          </div>

          <div v-if="msg.role === 'user'" class="avatar user-avatar">U</div>
        </div>

        <div v-if="isLoading" class="message-row bot-row">
          <div class="avatar bot-avatar">HC</div>
          <div class="message-content">
            <div class="message-bubble bot typing-bubble shadow-sm">
              <span class="dot"></span>
              <span class="dot"></span>
              <span class="dot"></span>
            </div>
          </div>
        </div>
      </div>

      

      <div class="chat-input-area">
        <div v-if="suggestions.length > 0" class="chat-suggestion-box shadow-lg">
          <div 
            v-for="item in suggestions" 
            :key="`${item.type}-${item.id}`"
            class="chat-suggestion-item"
            @click="selectSuggestion(item)"
          >
            <div class="d-flex align-items-center">
              <span class="suggestion-text" v-html="item.displayTitle"></span>
            </div>
          </div>
        </div>

        <SearchBar
          v-model="inputText"
          placeholder="Apa yang mau kamu tanyakan?"
          @search="sendMessage"
          @keyup.enter="sendMessage"
        />
      </div>

    </div>
  </div>
</template>