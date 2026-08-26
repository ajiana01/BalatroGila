# Spesifikasi Efek Kartu Planet (Planet Cards)

Dokumen ini berisi spesifikasi aturan dan efek peningkatan (upgrade) untuk setiap kartu Planet. Format ini disusun agar mudah dibaca oleh programmer sebagai referensi logika maupun oleh model AI sebagai *system prompt* atau konteks.

## 1. Tabel Spesifikasi Kartu Planet

| Nama Kartu | Target *Poker Hand* | Peningkatan *Mult* | Peningkatan *Chips* |
| :--- | :--- | :--- | :--- |
| **Pluto** | High Card | +1 Mult | +10 Chips |
| **Mercury** | Pair | +1 Mult | +15 Chips |
| **Uranus** | Two Pair | +1 Mult | +20 Chips |
| **Venus** | Three of a Kind | +2 Mult | +20 Chips |
| **Saturn** | Straight | +3 Mult | +30 Chips |
| **Jupiter** | Flush | +2 Mult | +15 Chips |
| **Earth** | Full House | +2 Mult | +25 Chips |
| **Mars** | Four of a Kind | +3 Mult | +30 Chips |
| **Neptune** | Straight Flush | +4 Mult | +40 Chips |
---

## 2. Format JSON (Untuk Implementasi Kode / State Management)

Struktur data JSON di bawah ini dapat digunakan untuk mengelola *level up* setiap *poker hand* di dalam *state* game Anda saat pemain menggunakan kartu Planet.

```json
{
  "planet_cards": {
    "Pluto": {
      "target_hand": "High Card",
      "upgrade": { "mult_increase": 1, "chips_increase": 10 }
    },
    "Mercury": {
      "target_hand": "Pair",
      "upgrade": { "mult_increase": 1, "chips_increase": 15 }
    },
    "Uranus": {
      "target_hand": "Two Pair",
      "upgrade": { "mult_increase": 1, "chips_increase": 20 }
    },
    "Venus": {
      "target_hand": "Three of a Kind",
      "upgrade": { "mult_increase": 2, "chips_increase": 20 }
    },
    "Saturn": {
      "target_hand": "Straight",
      "upgrade": { "mult_increase": 3, "chips_increase": 30 }
    },
    "Jupiter": {
      "target_hand": "Flush",
      "upgrade": { "mult_increase": 2, "chips_increase": 15 }
    },
    "Earth": {
      "target_hand": "Full House",
      "upgrade": { "mult_increase": 2, "chips_increase": 25 }
    },
    "Mars": {
      "target_hand": "Four of a Kind",
      "upgrade": { "mult_increase": 3, "chips_increase": 30 }
    },
    "Neptune": {
      "target_hand": "Straight Flush",
      "upgrade": { "mult_increase": 4, "chips_increase": 40 }
    }
  }
}
```
