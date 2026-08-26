# Spesifikasi Efek Kartu Spectral (Spectral Cards)

Dokumen ini berisi spesifikasi aturan dan efek untuk setiap kartu Spectral. Format ini dirancang untuk memudahkan parsing logika ke dalam kode permainan atau sebagai konteks instruksi (*system prompt*) untuk AI.

## 1. Tabel Spesifikasi Kartu Spectral

| Nama Kartu | Efek Utama |
| :--- | :--- |
| **Familiar** | Menghancurkan 1 kartu acak di tangan, lalu menambahkan 3 kartu wajah (*face cards*) Enhanced acak ke tangan. |
| **Grim** | Menghancurkan 1 kartu acak di tangan, lalu menambahkan 2 kartu As (*Aces*) Enhanced acak ke tangan. |
| **Incantation** | Menghancurkan 1 kartu acak di tangan, lalu menambahkan 4 kartu angka (*numbered cards*) Enhanced acak ke tangan. |
| **Wraith** | Menciptakan 1 kartu **Rare Joker** acak, dan mengubah jumlah uang (*money*) pemain menjadi **$0**. |
| **Sigil** | Mengubah semua kartu yang ada di tangan menjadi satu jenis *suit* acak yang sama. |

---

## 2. Format JSON (Untuk Implementasi Kode / State Management)

Struktur data berikut dapat digunakan langsung ke dalam *dictionary*, *database*, atau memori AI untuk memvalidasi aksi pemain saat menggunakan kartu Spectral.

```json
{
  "spectral_cards": {
    "Familiar": {
      "effect": "destroy_and_add",
      "destroy": {
        "amount": 1,
        "target_pool": "random_in_hand"
      },
      "add": {
        "amount": 3,
        "card_type": "face_card",
        "is_enhanced": true,
        "destination": "hand"
      }
    },
    "Grim": {
      "effect": "destroy_and_add",
      "destroy": {
        "amount": 1,
        "target_pool": "random_in_hand"
      },
      "add": {
        "amount": 2,
        "card_type": "ace",
        "is_enhanced": true,
        "destination": "hand"
      }
    },
    "Incantation": {
      "effect": "destroy_and_add",
      "destroy": {
        "amount": 1,
        "target_pool": "random_in_hand"
      },
      "add": {
        "amount": 4,
        "card_type": "numbered_card",
        "is_enhanced": true,
        "destination": "hand"
      }
    },
    "Wraith": {
      "effect": "spawn_joker_and_lose_money",
      "spawn": {
        "amount": 1,
        "joker_rarity": "Rare",
        "requires_room": true
      },
      "economy_penalty": {
        "set_money_to": 0
      }
    },
    "Sigil": {
      "effect": "convert_suit",
      "target_pool": "all_cards_in_hand",
      "new_suit": "single_random_suit"
    }
  }
}