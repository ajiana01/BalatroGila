# Spesifikasi Booster Packs

Dokumen ini berisi spesifikasi aturan untuk setiap jenis dan ukuran Booster Pack. Format ini disusun agar mudah dibaca oleh programmer sebagai referensi logika maupun oleh model AI sebagai *system prompt* atau konteks.

## 1. Tabel Spesifikasi Booster Pack

| Jenis Pack | Ukuran | Harga | Efek & Aturan |
| :--- | :--- | :--- | :--- |
| **Arcana** | Normal | $4 | Pilih 1 dari maksimal 3 kartu **Tarot** untuk digunakan secara langsung (*used immediately*). |
| | Jumbo | $6 | Pilih 1 dari maksimal 5 kartu **Tarot** untuk digunakan secara langsung. |
| | Mega | $8 | Pilih 2 dari maksimal 5 kartu **Tarot** untuk digunakan secara langsung. |
| **Celestial** | Normal | $4 | Pilih 1 dari maksimal 3 kartu **Planet** untuk digunakan secara langsung. |
| | Jumbo | $6 | Pilih 1 dari maksimal 5 kartu **Planet** untuk digunakan secara langsung. |
| | Mega | $8 | Pilih 2 dari maksimal 5 kartu **Planet** untuk digunakan secara langsung. |
| **Standard** | Normal | $4 | Pilih 1 dari maksimal 3 kartu **Playing Cards** (kartu remi) untuk ditambahkan ke dalam *deck*. |
| | Jumbo | $6 | Pilih 1 dari maksimal 5 kartu **Playing Cards** untuk ditambahkan ke dalam *deck*. |
| | Mega | $8 | Pilih 2 dari maksimal 5 kartu **Playing Cards** untuk ditambahkan ke dalam *deck*. |
| **Buffoon** | Normal | $4 | Pilih 1 dari maksimal 2 kartu **Joker**. |
| | Jumbo | $6 | Pilih 1 dari maksimal 4 kartu **Joker**. |
| | Mega | $8 | Pilih 2 dari maksimal 4 kartu **Joker**. |
| **Spectral** | Normal | $4 | Pilih 1 dari maksimal 2 kartu **Spectral** untuk digunakan secara langsung. |
| | Jumbo | $6 | Pilih 1 dari maksimal 4 kartu **Spectral** untuk digunakan secara langsung. |
| | Mega | $8 | Pilih 2 dari maksimal 4 kartu **Spectral** untuk digunakan secara langsung. |

---

## 2. Format JSON (Untuk Implementasi Kode / Parsing AI)

Struktur data JSON di bawah ini mengelompokkan setiap paket berdasarkan tipe dan ukuran, yang sangat ideal untuk diintegrasikan ke dalam sistem toko (Shop) atau logika *RNG generation* dalam game.

```json
{
  "booster_packs": {
    "Arcana": {
      "card_type": "Tarot",
      "action": "use_immediately",
      "sizes": {
        "Normal": { "cost": 4, "options_generated": 3, "picks_allowed": 1 },
        "Jumbo": { "cost": 6, "options_generated": 5, "picks_allowed": 1 },
        "Mega": { "cost": 8, "options_generated": 5, "picks_allowed": 2 }
      }
    },
    "Celestial": {
      "card_type": "Planet",
      "action": "use_immediately",
      "sizes": {
        "Normal": { "cost": 4, "options_generated": 3, "picks_allowed": 1 },
        "Jumbo": { "cost": 6, "options_generated": 5, "picks_allowed": 1 },
        "Mega": { "cost": 8, "options_generated": 5, "picks_allowed": 2 }
      }
    },
    "Standard": {
      "card_type": "PlayingCard",
      "action": "add_to_deck",
      "sizes": {
        "Normal": { "cost": 4, "options_generated": 3, "picks_allowed": 1 },
        "Jumbo": { "cost": 6, "options_generated": 5, "picks_allowed": 1 },
        "Mega": { "cost": 8, "options_generated": 5, "picks_allowed": 2 }
      }
    },
    "Buffoon": {
      "card_type": "Joker",
      "action": "add_to_joker_slots",
      "sizes": {
        "Normal": { "cost": 4, "options_generated": 2, "picks_allowed": 1 },
        "Jumbo": { "cost": 6, "options_generated": 4, "picks_allowed": 1 },
        "Mega": { "cost": 8, "options_generated": 4, "picks_allowed": 2 }
      }
    },
    "Spectral": {
      "card_type": "Spectral",
      "action": "use_immediately",
      "sizes": {
        "Normal": { "cost": 4, "options_generated": 2, "picks_allowed": 1 },
        "Jumbo": { "cost": 6, "options_generated": 4, "picks_allowed": 1 },
        "Mega": { "cost": 8, "options_generated": 4, "picks_allowed": 2 }
      }
    }
  }
}