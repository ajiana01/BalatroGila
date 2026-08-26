# Spesifikasi Efek Kartu Tarot (Tarot Cards)

Dokumen ini berisi spesifikasi aturan dan efek untuk setiap kartu Tarot. Format ini dirancang untuk memudahkan parsing logika ke dalam kode permainan atau sebagai konteks instruksi (*system prompt*) untuk AI.

## 1. Tabel Spesifikasi Tarot

| Nama Kartu | Efek Utama | Kondisi / Batasan Tambahan |
| :--- | :--- | :--- |
| **The Fool** | Menciptakan (*creates*) kartu Tarot atau Planet terakhir yang digunakan pada *run* ini. | Kartu *The Fool* tidak termasuk (tidak bisa menduplikasi dirinya sendiri). |
| **The Magician** | Mengubah 2 kartu yang dipilih menjadi **Lucky Cards**. | - |
| **The High Priestess** | Menciptakan hingga 2 kartu **Planet** acak. | Harus memiliki ruang (slot) yang cukup. |
| **The Empress** | Mengubah 2 kartu yang dipilih menjadi **Mult Cards**. | - |
| **The Emperor** | Menciptakan hingga 2 kartu **Tarot** acak. | Harus memiliki ruang (slot) yang cukup. |
| **The Hierophant** | Mengubah 2 kartu yang dipilih menjadi **Bonus Cards**. | - |
| **The Lovers** | Mengubah 1 kartu yang dipilih menjadi **Wild Card**. | - |
| **The Chariot** | Mengubah 1 kartu yang dipilih menjadi **Steel Card**. | - |
| **Justice** | Mengubah 1 kartu yang dipilih menjadi **Glass Card**. | - |
| **The Hermit** | Menggandakan uang saat ini. | Maksimal uang yang diberikan adalah **$20**. |
| **The Wheel of Fortune**| Peluang 1 banding 4 (25%) untuk menambahkan edisi *Foil*, *Holographic*, atau *Polychrome* ke Joker acak. | - |
| **Strength** | Meningkatkan *rank* (angka/nilai) hingga 2 kartu yang dipilih sebanyak 1 tingkat. | - |
| **The Hanged Man** | Menghancurkan (*destroys*) hingga 2 kartu yang dipilih. | - |
| **Death** | Pilih 2 kartu; mengubah kartu sebelah kiri menjadi kartu sebelah kanan. | Pemain dapat menggeser (drag) untuk mengatur posisi. |
| **Temperance** | Memberikan uang sejumlah total nilai jual (*sell value*) semua Joker yang dimiliki saat ini. | Maksimal uang yang diberikan adalah **$50**. |
| **The Devil** | Mengubah 1 kartu yang dipilih menjadi **Gold Card**. | - |
| **The Tower** | Mengubah 1 kartu yang dipilih menjadi **Stone Card**. | - |
| **The Star** | Mengubah hingga 3 kartu yang dipilih menjadi *suit* **Diamonds** (Wajik). | - |
| **The Moon** | Mengubah hingga 3 kartu yang dipilih menjadi *suit* **Clubs** (Keriting). | - |
| **The Sun** | Mengubah hingga 3 kartu yang dipilih menjadi *suit* **Hearts** (Hati). | - |
| **Judgement** | Menciptakan 1 kartu **Joker** acak. | Harus memiliki ruang (slot) yang cukup. |
| **The World** | Mengubah hingga 3 kartu yang dipilih menjadi *suit* **Spades** (Sekop). | - |

---

## 2. Format JSON (Untuk Implementasi Kode / State Management)

Struktur data berikut dapat digunakan langsung ke dalam *dictionary*, *database*, atau memori AI untuk memvalidasi aksi pemain saat menggunakan kartu Tarot.

```json
{
  "tarot_cards": {
    "The Fool": {
      "effect": "copy_last_consumable",
      "valid_targets": ["Tarot", "Planet"],
      "exceptions": ["The Fool"]
    },
    "The Magician": {
      "effect": "enhance_card",
      "enhancement_type": "Lucky",
      "max_targets": 2
    },
    "The High Priestess": {
      "effect": "spawn_card",
      "card_type": "Planet",
      "amount": 2,
      "requires_room": true
    },
    "The Empress": {
      "effect": "enhance_card",
      "enhancement_type": "Mult",
      "max_targets": 2
    },
    "The Emperor": {
      "effect": "spawn_card",
      "card_type": "Tarot",
      "amount": 2,
      "requires_room": true
    },
    "The Hierophant": {
      "effect": "enhance_card",
      "enhancement_type": "Bonus",
      "max_targets": 2
    },
    "The Lovers": {
      "effect": "enhance_card",
      "enhancement_type": "Wild",
      "max_targets": 1
    },
    "The Chariot": {
      "effect": "enhance_card",
      "enhancement_type": "Steel",
      "max_targets": 1
    },
    "Justice": {
      "effect": "enhance_card",
      "enhancement_type": "Glass",
      "max_targets": 1
    },
    "The Hermit": {
      "effect": "multiply_money",
      "multiplier": 2,
      "max_payout": 20
    },
    "The Wheel of Fortune": {
      "effect": "add_joker_edition",
      "editions": ["Foil", "Holographic", "Polychrome"],
      "chance": "1/4"
    },
    "Strength": {
      "effect": "increase_rank",
      "amount": 1,
      "max_targets": 2
    },
    "The Hanged Man": {
      "effect": "destroy_card",
      "max_targets": 2
    },
    "Death": {
      "effect": "convert_card",
      "mechanism": "left_becomes_right",
      "exact_targets_required": 2,
      "ui_notes": "Drag to rearrange"
    },
    "Temperance": {
      "effect": "gain_money",
      "calculation": "sum_of_joker_sell_values",
      "max_payout": 50
    },
    "The Devil": {
      "effect": "enhance_card",
      "enhancement_type": "Gold",
      "max_targets": 1
    },
    "The Tower": {
      "effect": "enhance_card",
      "enhancement_type": "Stone",
      "max_targets": 1
    },
    "The Star": {
      "effect": "change_suit",
      "new_suit": "Diamonds",
      "max_targets": 3
    },
    "The Moon": {
      "effect": "change_suit",
      "new_suit": "Clubs",
      "max_targets": 3
    },
    "The Sun": {
      "effect": "change_suit",
      "new_suit": "Hearts",
      "max_targets": 3
    },
    "Judgement": {
      "effect": "spawn_card",
      "card_type": "Joker",
      "amount": 1,
      "requires_room": true
    },
    "The World": {
      "effect": "change_suit",
      "new_suit": "Spades",
      "max_targets": 3
    }
  }
}