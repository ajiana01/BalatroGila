# Spesifikasi Aturan Toko (The Shop)

Dokumen ini berisi spesifikasi lengkap mengenai aturan sistem Toko (*The Shop*), termasuk probabilitas (*weights*), mekanika *reroll*, serta kalkulasi harga. Format ini dirancang agar mudah diimplementasikan oleh programmer ke dalam kode atau dibaca oleh model AI sebagai *system prompt*.

## 1. Aturan Dasar & Inventaris Default
Toko hanya dapat diakses setelah mengalahkan *Small Blind*, *Big Blind*, atau *Boss Blind*. Secara *default*, toko menjual:
* **2 Kartu Acak:** Biasanya Joker, tetapi bisa berupa Tarot, Planet, dan *Playing Cards* jika telah membeli *Voucher* tertentu (*Magic Trick* & *Illusion*), atau *Spectral cards* jika menggunakan *Ghost Deck*.
* **2 Booster Packs Acak:** Pada kunjungan pertama di setiap *run*, salah satu *pack* dipastikan berupa *Normal Buffoon Pack*.
* **1 Voucher.**

---

## 2. Bobot Kemunculan (Spawn Weights)

Bobot menentukan seberapa besar kemungkinan sebuah kartu muncul di slot kartu acak.

| Tipe Kartu | Bobot (*Weight*) | Peluang (*Chance*) |
| :--- | :--- | :--- |
| **Joker** | 20 | ~71.4% |
| **Tarot** | 4 | ~14.3% |
| **Planet** | 4 | ~14.3% |

**Bobot Kelangkaan Joker (Joker Rarities):**
Saat sistem memutuskan untuk memunculkan Joker, kelangkaannya diundi dengan peluang berikut:
* **Common:** 70%
* **Uncommon:** 25%
* **Rare:** 5%

---

## 3. Mekanisme Reroll
Pemain dapat membayar sejumlah uang untuk melakukan *reroll* (mengganti kartu acak di toko dengan 2 kartu acak baru) tanpa batas. Kartu yang sudah muncul sebelumnya bisa muncul kembali.
* **Biaya Awal:** $5 (Biaya ini akan di-reset kembali ke $5 setiap kali memasuki toko baru).
* **Kenaikan Biaya:** +$1 untuk setiap *reroll* berturut-turut di toko yang sama.
* **Modifikasi Item:**
  * **Reroll Surplus:** Mengubah biaya awal menjadi **$3**.
  * **Reroll Glut:** Mengubah biaya awal menjadi **$1**.
  * **Chaos the Clown:** Membuat *reroll* pertama menjadi **Gratis ($0)**.

---

## 4. Sistem Harga (Pricing)

Semua item memiliki harga beli (*buy cost*) dan nilai jual (*sell value*). Harga ini dihitung ulang setiap kali pemain berinteraksi dengan item tersebut.

**Formula Kalkulasi Dasar:**
* `buy_cost = (base_cost + edition_cost) * discount_percent`
* `sell_value = floor(buy_cost / 2)`

**Tabel Base Cost (Harga Dasar):**

| Tipe Item | Kelangkaan / Ukuran | Harga Dasar (*Base Cost*) |
| :--- | :--- | :--- |
| **Joker** | Common | $1 - $6 |
| | Uncommon | $4 - $8 |
| | Rare | $7 - $10 |
| | Legendary | $20 |
| **Playing cards** | Standar | $1 |
| **Tarot cards** | Standar | $3 |
| **Planet cards**| Standar | $3 |
| **Spectral cards**| Standar | $4 |
| **Booster Packs**| Normal | $4 |
| | Jumbo | $6 |
| | Mega | $8 |
| **Vouchers** | Standar | $10 |

---

## 5. Format JSON (Untuk Implementasi Kode / State Management)

Struktur data berikut memetakan seluruh aturan toko di atas ke dalam format JSON yang ideal untuk variabel konfigurasi, validasi *shop generator*, dan manajemen ekonomi game.

```json
{
  "shop_system": {
    "access_conditions": ["Small Blind", "Big Blind", "Boss Blind"],
    "default_inventory": {
      "random_cards": {
        "slots": 2,
        "base_pool": ["Joker"],
        "conditional_pool": ["Tarot", "Planet", "PlayingCards", "SpectralCards"]
      },
      "booster_packs": {
        "slots": 2,
        "first_visit_guarantee": "Normal Buffoon Pack"
      },
      "vouchers": {
        "slots": 1
      }
    },
    "spawn_weights": {
      "card_types": {
        "Joker": { "weight": 20, "chance_percent": 71.4 },
        "Tarot": { "weight": 4, "chance_percent": 14.3 },
        "Planet": { "weight": 4, "chance_percent": 14.3 }
      },
      "joker_rarity_percent": {
        "Common": 70.0,
        "Uncommon": 25.0,
        "Rare": 5.0
      }
    },
    "reroll_mechanics": {
      "base_cost": 5,
      "cost_increase_per_roll": 1,
      "resets_on_new_shop": true,
      "modifiers": {
        "Reroll Surplus": { "new_base_cost": 3 },
        "Reroll Glut": { "new_base_cost": 1 },
        "Chaos the Clown": { "first_reroll_free": true }
      }
    },
    "pricing": {
      "formulas": {
        "buy_cost": "(base_cost + edition_cost) * discount_percent",
        "sell_value": "floor(buy_cost / 2)"
      },
      "base_costs": {
        "Jokers": {
          "Common": [1, 6],
          "Uncommon": [4, 8],
          "Rare": [7, 10],
          "Legendary": 20
        },
        "Consumables": {
          "Playing_cards": 1,
          "Tarot_cards": 3,
          "Planet_cards": 3,
          "Spectral_cards": 4
        },
        "Booster_Packs": {
          "Normal": 4,
          "Jumbo": 6,
          "Mega": 8
        },
        "Vouchers": 10
      }
    }
  }
}
```
