# Spesifikasi Edisi Kartu (Card Editions)

Dokumen ini berisi spesifikasi aturan untuk berbagai jenis **Editions** (Edisi) yang dapat diterapkan pada kartu di dalam game. Format ini disusun agar mudah dibaca oleh programmer sebagai referensi logika (seperti *scoring* dan limit slot) maupun oleh model AI sebagai *system prompt*.

## 1. Tabel Spesifikasi Edisi

Terdapat 5 jenis edisi dengan efek yang berbeda tergantung pada tipe kartu yang menerimanya (Playing Cards, Jokers, atau Consumables).

| Edisi | Efek pada *Playing Cards* | Efek pada *Jokers* | Efek pada *Consumables* |
| :--- | :--- | :--- | :--- |
| **Base** | Tidak ada efek tambahan (*No extra effects*) | Tidak ada efek tambahan (*No extra effects*) | Tidak ada efek tambahan (*No extra effects*) |
| **Foil** | **+50 Chips** saat dinilai (*when scored*) | **+50 Chips** tepat *sebelum* Joker ini dieksekusi saat perhitungan skor. | N/A |
| **Holographic** | **+10 Mult** saat dinilai (*when scored*) | **+10 Mult** tepat *sebelum* Joker ini dieksekusi saat perhitungan skor. | N/A |
| **Polychrome** | **X1.5 Mult** saat dinilai (*when scored*) | **X1.5 Mult** tepat *setelah* Joker ini dieksekusi saat perhitungan skor. | N/A |
| **Negative** | N/A | **+1 slot Joker** (kapasitas slot Joker bertambah 1). | **+1 slot Consumable** |

---

## 2. Format JSON (Untuk Implementasi Kode / State Management)

Struktur data JSON di bawah ini memetakan setiap edisi beserta *trigger timing* (waktu pemicu) efeknya. Struktur ini sangat ideal untuk diintegrasikan ke dalam fungsi kalkulasi *scoring* utama permainan.

```json
{
  "card_editions": {
    "Base": {
      "playing_card": { "effect": "none" },
      "joker": { "effect": "none" },
      "consumable": { "effect": "none" }
    },
    "Foil": {
      "playing_card": { 
        "effect": "add_chips", 
        "amount": 50, 
        "trigger": "when_scored" 
      },
      "joker": { 
        "effect": "add_chips", 
        "amount": 50, 
        "trigger": "directly_before_joker_scored" 
      },
      "consumable": null
    },
    "Holographic": {
      "playing_card": { 
        "effect": "add_mult", 
        "amount": 10, 
        "trigger": "when_scored" 
      },
      "joker": { 
        "effect": "add_mult", 
        "amount": 10, 
        "trigger": "directly_before_joker_scored" 
      },
      "consumable": null
    },
    "Polychrome": {
      "playing_card": { 
        "effect": "multiply_mult", 
        "amount": 1.5, 
        "trigger": "when_scored" 
      },
      "joker": { 
        "effect": "multiply_mult", 
        "amount": 1.5, 
        "trigger": "directly_after_joker_scored" 
      },
      "consumable": null
    },
    "Negative": {
      "playing_card": null,
      "joker": { 
        "effect": "increase_joker_slots", 
        "amount": 1 
      },
      "consumable": { 
        "effect": "increase_consumable_slots", 
        "amount": 1
      }
    }
  }
}
```
