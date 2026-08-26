# Spesifikasi Efek Kartu (Card Enhancements)

Dokumen ini berisi spesifikasi aturan untuk modifikasi kartu (Enhancement Cards). Format ini disusun agar mudah dibaca oleh programmer (sebagai referensi logika) maupun oleh model AI lain (sebagai *system prompt* atau konteks).

## 1. Tabel Spesifikasi (Untuk Pembacaan Cepat)

| Tipe Kartu | Kondisi Pemicu (Trigger) | Efek Utama (Effect) | Aturan Tambahan / Probabilitas (Notes) |
| :--- | :--- | :--- | :--- |
| **Bonus** | Saat dinilai (*When scored*) | Tambahan **+30 Chips** | - |
| **Mult** | Saat dinilai (*When scored*) | Tambahan **+4 Mult** | - |
| **Wild** | Selalu Aktif (*Passive*) | - | Kartu dianggap memiliki **semua *suit*** secara bersamaan. |
| **Glass** | Saat dinilai (*When scored*) | Dikali **X2 Mult** | Terdapat peluang **1 banding 4 (25%)** kartu hancur setelah semua perhitungan skor selesai. |
| **Steel** | Saat ditahan di tangan (*Held in hand*) | Dikali **X1.5 Mult** | - |
| **Stone** | Selalu dinilai (*Always scores*) | Nilai kartu menjadi **+50 Chips** | Kartu kehilangan *rank* (angka/wajah) dan *suit* (simbol). Selalu dihitung ke dalam skor apapun kombinasi (hand) yang dimainkan. |
| **Gold** | Ditahan di tangan pada akhir ronde | Tambahan uang **$3** | Hanya memicu jika kartu masih ada di tangan saat ronde (round) berakhir. |
| **Lucky** | Saat dinilai (*When scored*) | Peluang **1:5 (20%)** untuk **+20 Mult** <br> Peluang **1:15 (~6.67%)** untuk **+$20** | Kedua efek diproses (*rolled*) secara terpisah. Keduanya bisa terpicu pada giliran yang sama. |

---

## 2. Format JSON (Untuk Implementasi Kode / Parsing AI)

Jika Anda perlu memasukkan data ini ke dalam *state management* atau mem-parsing-nya secara terprogram, Anda dapat menggunakan struktur JSON berikut:

```json
{
  "enhancements": {
    "Bonus": {
      "trigger": "when_scored",
      "effect": { "chips": 30 }
    },
    "Mult": {
      "trigger": "when_scored",
      "effect": { "mult": 4 }
    },
    "Wild": {
      "trigger": "passive",
      "effect": { "is_all_suits": true }
    },
    "Glass": {
      "trigger": "when_scored",
      "effect": { "x_mult": 2 },
      "after_scoring": {
        "destroy_chance": "1/4"
      }
    },
    "Steel": {
      "trigger": "held_in_hand",
      "effect": { "x_mult": 1.5 }
    },
    "Stone": {
      "trigger": "always_scores",
      "properties": {
        "has_rank": false,
        "has_suit": false
      },
      "effect": { "set_chips": 50 }
    },
    "Gold": {
      "trigger": "held_in_hand_end_of_round",
      "effect": { "money": 3 }
    },
    "Lucky": {
      "trigger": "when_scored",
      "effects": [
        { "chance": "1/5", "mult": 20 },
        { "chance": "1/15", "money": 20 }
      ],
      "notes": "Effects are rolled independently and can both trigger."
    }
  }
}
```
