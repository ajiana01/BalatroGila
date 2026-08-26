# Spesifikasi Efek Voucher (Base Vouchers)

Dokumen ini berisi spesifikasi aturan dan efek untuk setiap tipe **Base Voucher**. Format ini disusun agar mudah dibaca oleh programmer sebagai referensi logika maupun oleh model AI sebagai *system prompt* atau konteks (*context*).

## 1. Tabel Spesifikasi Voucher

| Nama Voucher | Efek Utama |
| :--- | :--- |
| **Overstock** | **+1** slot kartu yang tersedia di toko (*shop*) (menjadi maksimal 3 slot). |
| **Clearance Sale** | Semua kartu dan *pack* di toko mendapat diskon **25% off**. |
| **Hone** | Kartu edisi *Foil*, *Holographic*, dan *Polychrome* muncul **2X** lebih sering. |
| **Reroll Surplus** | Biaya *Reroll* berkurang sebesar **$2**. |
| **Crystal Ball** | **+1** slot *consumable* (slot untuk kartu Tarot/Planet/Spectral). |
| **Telescope** | *Celestial Packs* selalu berisi kartu Planet untuk *poker hand* yang paling sering kamu mainkan (*most played poker hand*). |
| **Grabber** | Secara permanen mendapatkan **+1** *hand* (kesempatan main) setiap ronde. |
| **Wasteful** | Secara permanen mendapatkan **+1** *discard* (kesempatan buang kartu) setiap ronde. |
| **Tarot Merchant** | Kartu Tarot muncul **2X** lebih sering di toko. |
| **Planet Merchant** | Kartu Planet muncul **2X** lebih sering di toko. |
| **Seed Money** | Meningkatkan batas maksimal bunga (*interest cap*) yang didapat setiap ronde menjadi **$10**. |
| **Blank** | Tidak melakukan apa-apa? (*Does nothing?* - Biasanya menjadi syarat untuk meng-unlock voucher tingkat lanjut). |
| **Magic Trick** | *Playing cards* (kartu remi standar) sekarang dapat dibeli langsung dari toko. |
| **Hieroglyph** | **-1** *Ante* (mundur 1 tingkat kesulitan), namun juga **-1** *hand* setiap ronde. |
| **Director's Cut** | Mengizinkan *Reroll Boss Blind* sebanyak 1 kali per *Ante*, dengan biaya **$10** per *roll*. |
| **Paint Brush** | **+1** *hand size* (jumlah maksimal kartu yang bisa dipegang di tangan bertambah 1). |

---

## 2. Format JSON (Untuk Implementasi Kode / State Management)

Struktur data JSON di bawah ini dapat langsung diintegrasikan ke dalam sistem manajemen *state* game, variabel global pemain, atau sebagai basis data validasi logika toko.

```json
{
  "vouchers": {
    "Overstock": {
      "effect": "increase_shop_slots",
      "amount": 1,
      "max_slots": 3
    },
    "Clearance Sale": {
      "effect": "discount_shop_items",
      "discount_percentage": 25
    },
    "Hone": {
      "effect": "increase_edition_chance",
      "multiplier": 2,
      "editions": ["Foil", "Holographic", "Polychrome"]
    },
    "Reroll Surplus": {
      "effect": "reduce_reroll_cost",
      "amount": 2
    },
    "Crystal Ball": {
      "effect": "increase_consumable_slots",
      "amount": 1
    },
    "Telescope": {
      "effect": "guarantee_planet_card",
      "condition": "most_played_poker_hand"
    },
    "Grabber": {
      "effect": "increase_hands_per_round",
      "amount": 1,
      "duration": "permanent"
    },
    "Wasteful": {
      "effect": "increase_discards_per_round",
      "amount": 1,
      "duration": "permanent"
    },
    "Tarot Merchant": {
      "effect": "increase_shop_appearance",
      "card_type": "Tarot",
      "multiplier": 2
    },
    "Planet Merchant": {
      "effect": "increase_shop_appearance",
      "card_type": "Planet",
      "multiplier": 2
    },
    "Seed Money": {
      "effect": "raise_interest_cap",
      "new_cap": 10
    },
    "Blank": {
      "effect": "none",
      "notes": "Does nothing? (Often used to unlock Antimatter)"
    },
    "Magic Trick": {
      "effect": "enable_playing_cards_in_shop"
    },
    "Hieroglyph": {
      "effect": "modify_stats",
      "ante_change": -1,
      "hands_per_round_change": -1
    },
    "Director's Cut": {
      "effect": "enable_boss_blind_reroll",
      "limit_per_ante": 1,
      "cost_per_roll": 10
    },
    "Paint Brush": {
      "effect": "increase_hand_size",
      "amount": 1
    }
  }
}
```
