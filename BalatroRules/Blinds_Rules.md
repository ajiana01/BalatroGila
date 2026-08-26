# Spesifikasi Aturan Blinds (Small, Big, Boss, & Showdown)

Dokumen ini berisi spesifikasi lengkap mengenai aturan, modifikasi, dan efek dari setiap tipe **Blind**. Format ini dirancang untuk memudahkan parsing logika ke dalam kode permainan atau sebagai referensi instruksi (*system prompt*) untuk AI.

## 1. Tabel Spesifikasi Blinds

| Kategori | Nama Blind | Efek / Deskripsi | Minimum Ante | Target Skor | Hadiah | Matador-compatible? |
| :--- | :--- | :--- | :--- | :--- | :--- | :--- |
| **Basic** | **Small Blind** | Tidak ada efek khusus - bisa di-*skip* untuk mendapat Tag. | Any | 1x base | $3 | ✗ No |
| **Basic** | **Big Blind** | Tidak ada efek khusus - bisa di-*skip* untuk mendapat Tag. | Any | 1.5x base | $4 | ✗ No |
| **Boss** | **The Hook** | Membuang (discard) 2 kartu acak di tangan setelah setiap *hand* dimainkan. | Any | 2x base | $5 | ✗ No |
| **Boss** | **The Ox** | Memainkan *poker hand* yang paling sering dimainkan pada *run* ini akan membuat uang menjadi $0. | 6 | 2x base | $5 | ✓ Yes |
| **Boss** | **The House** | *Hand* pertama yang ditarik akan menghadap ke bawah (*face down*). | 2 | 2x base | $5 | ✗ No |
| **Boss** | **The Wall** | *Blind* ekstra besar (kebutuhan skor sangat tinggi). | 2 | 4x base | $5 | ✗ No |
| **Boss** | **The Wheel** | 1 dari 7 kartu akan ditarik menghadap ke bawah (*face down*) selama ronde. | 2 | 2x base | $5 | ✗ No |
| **Boss** | **The Arm** | Menurunkan level *poker hand* yang dimainkan secara permanen sebanyak 1 (minimal Level 1, dikurangi sebelum *scoring*). | 2 | 2x base | $5 | ✓ Yes |
| **Boss** | **The Club** | Semua kartu *Clubs* (Keriting) terkena *debuff*. | Any | 2x base | $5 | ✓ Yes |
| **Boss** | **The Fish** | Kartu ditarik menghadap ke bawah (*face down*) setelah setiap *hand* dimainkan. | 2 | 2x base | $5 | ✗ No |
| **Boss** | **The Psychic** | Harus memainkan tepat 5 kartu (tidak semua kartu harus menghasilkan skor). | Any | 2x base | $5 | ✓ Yes |
| **Boss** | **The Goad** | Semua kartu *Spades* (Sekop) terkena *debuff*. | Any | 2x base | $5 | ✓ Yes |
| **Boss** | **The Water** | Memulai ronde dengan 0 *discard*. | 2 | 2x base | $5 | ✗ No |
| **Boss** | **The Window** | Semua kartu *Diamonds* (Wajik) terkena *debuff*. | Any | 2x base | $5 | ✓ Yes |
| **Boss** | **The Manacle** | -1 *Hand Size* (ukuran tangan berkurang 1). | Any | 2x base | $5 | ✗ No |
| **Boss** | **The Eye** | Tidak boleh mengulang tipe *hand* (setiap *hand* yang dimainkan harus berbeda jenisnya dari yang sebelumnya di ronde ini). | 3 | 2x base | $5 | ✓ Yes |
| **Boss** | **The Mouth** | Hanya satu tipe *hand* yang bisa dimainkan pada ronde ini. | 2 | 2x base | $5 | ✓ Yes |
| **Boss** | **The Plant** | Semua kartu wajah (*face cards*) terkena *debuff*. | 4 | 2x base | $5 | ✓ Yes |
| **Boss** | **The Serpent** | Setelah melakukan *Play* atau *Discard*, selalu menarik 3 kartu (mengabaikan batas ukuran tangan). | 5 | 2x base | $5 | ✗ No |
| **Boss** | **The Pillar** | Kartu yang sebelumnya dimainkan pada *Ante* ini (saat Small dan Big Blinds) terkena *debuff*. | Any | 2x base | $5 | ✓ Yes |
| **Boss** | **The Needle** | Hanya boleh memainkan 1 *hand* (langsung *game over* jika skor tidak tercapai). | 2 | 1x base | $5 | ✗ No |
| **Boss** | **The Head** | Semua kartu *Hearts* (Hati) terkena *debuff*. | Any | 2x base | $5 | ✓ Yes |
| **Boss** | **The Tooth** | Kehilangan $1 untuk setiap kartu yang dimainkan. | 3 | 2x base | $5 | ✗ No |
| **Boss** | **The Flint** | *Base Chips* dan *Mult* dari *poker hand* yang dimainkan dikurangi setengah (dibagi 2) selama ronde. | 2 | 2x base | $5 | ✓ Yes |
| **Boss** | **The Mark** | Semua kartu wajah (*face cards*) ditarik menghadap ke bawah (*face down*). | 2 | 2x base | $5 | ✗ No |
| **Showdown** | **Amber Acorn** | Membalik dan mengacak posisi semua kartu Joker. | 8 | 2x base | $8 | ✗ No |
| **Showdown** | **Verdant Leaf** | Semua kartu terkena *debuff* sampai 1 Joker dijual. | 8 | 2x base | $8 | ✓ Yes |
| **Showdown** | **Violet Vessel** | *Blind* berukuran sangat besar. | 8 | 6x base | $8 | ✗ No |
| **Showdown** | **Crimson Heart** | Satu Joker acak dinonaktifkan setiap *hand* (target Joker berubah tiap *hand*). | 8 | 2x base | $8 | ✗ No |
| **Showdown** | **Cerulean Bell** | Memaksa 1 kartu untuk selalu dipilih (*selected*). | 8 | 2x base | $8 | ✗ No |

---

## 2. Format JSON (Untuk Implementasi Kode / State Management)

Struktur data berikut mengelompokkan atribut dari setiap *blind* dan memetakan logika efeknya agar mudah digunakan oleh sistem atau programmer.

```json
{
  "blinds": {
    "Small Blind": {
      "type": "Basic",
      "effect": "none",
      "can_skip": true,
      "min_ante": 1,
      "score_multiplier": 1.0,
      "reward": 3,
      "matador_compatible": false
    },
    "Big Blind": {
      "type": "Basic",
      "effect": "none",
      "can_skip": true,
      "min_ante": 1,
      "score_multiplier": 1.5,
      "reward": 4,
      "matador_compatible": false
    },
    "Boss Blinds": {
      "The Hook": { "min_ante": 1, "score_multiplier": 2.0, "reward": 5, "matador_compatible": false, "effect": "discard_after_play", "amount": 2 },
      "The Ox": { "min_ante": 6, "score_multiplier": 2.0, "reward": 5, "matador_compatible": true, "effect": "lose_all_money_on_most_played_hand" },
      "The House": { "min_ante": 2, "score_multiplier": 2.0, "reward": 5, "matador_compatible": false, "effect": "first_hand_face_down" },
      "The Wall": { "min_ante": 2, "score_multiplier": 4.0, "reward": 5, "matador_compatible": false, "effect": "extra_large_blind" },
      "The Wheel": { "min_ante": 2, "score_multiplier": 2.0, "reward": 5, "matador_compatible": false, "effect": "chance_face_down", "chance": "1/7" },
      "The Arm": { "min_ante": 2, "score_multiplier": 2.0, "reward": 5, "matador_compatible": true, "effect": "decrease_played_hand_level" },
      "The Club": { "min_ante": 1, "score_multiplier": 2.0, "reward": 5, "matador_compatible": true, "effect": "debuff_suit", "suit": "Clubs" },
      "The Fish": { "min_ante": 2, "score_multiplier": 2.0, "reward": 5, "matador_compatible": false, "effect": "draw_face_down_after_play" },
      "The Psychic": { "min_ante": 1, "score_multiplier": 2.0, "reward": 5, "matador_compatible": true, "effect": "require_five_cards_played" },
      "The Goad": { "min_ante": 1, "score_multiplier": 2.0, "reward": 5, "matador_compatible": true, "effect": "debuff_suit", "suit": "Spades" },
      "The Water": { "min_ante": 2, "score_multiplier": 2.0, "reward": 5, "matador_compatible": false, "effect": "start_with_zero_discards" },
      "The Window": { "min_ante": 1, "score_multiplier": 2.0, "reward": 5, "matador_compatible": true, "effect": "debuff_suit", "suit": "Diamonds" },
      "The Manacle": { "min_ante": 1, "score_multiplier": 2.0, "reward": 5, "matador_compatible": false, "effect": "decrease_hand_size", "amount": 1 },
      "The Eye": { "min_ante": 3, "score_multiplier": 2.0, "reward": 5, "matador_compatible": true, "effect": "no_repeat_hand_types" },
      "The Mouth": { "min_ante": 2, "score_multiplier": 2.0, "reward": 5, "matador_compatible": true, "effect": "only_one_hand_type_allowed" },
      "The Plant": { "min_ante": 4, "score_multiplier": 2.0, "reward": 5, "matador_compatible": true, "effect": "debuff_face_cards" },
      "The Serpent": { "min_ante": 5, "score_multiplier": 2.0, "reward": 5, "matador_compatible": false, "effect": "draw_three_after_play_or_discard" },
      "The Pillar": { "min_ante": 1, "score_multiplier": 2.0, "reward": 5, "matador_compatible": true, "effect": "debuff_previously_played_cards_in_ante" },
      "The Needle": { "min_ante": 2, "score_multiplier": 1.0, "reward": 5, "matador_compatible": false, "effect": "only_one_hand_allowed" },
      "The Head": { "min_ante": 1, "score_multiplier": 2.0, "reward": 5, "matador_compatible": true, "effect": "debuff_suit", "suit": "Hearts" },
      "The Tooth": { "min_ante": 3, "score_multiplier": 2.0, "reward": 5, "matador_compatible": false, "effect": "lose_money_per_card_played", "amount": 1 },
      "The Flint": { "min_ante": 2, "score_multiplier": 2.0, "reward": 5, "matador_compatible": true, "effect": "halve_base_chips_and_mult" },
      "The Mark": { "min_ante": 2, "score_multiplier": 2.0, "reward": 5, "matador_compatible": false, "effect": "face_cards_drawn_face_down" }
    },
    "Showdown Boss Blinds": {
      "Amber Acorn": { "min_ante": 8, "score_multiplier": 2.0, "reward": 8, "matador_compatible": false, "effect": "flip_and_shuffle_jokers" },
      "Verdant Leaf": { "min_ante": 8, "score_multiplier": 2.0, "reward": 8, "matador_compatible": true, "effect": "debuff_all_cards_until_joker_sold" },
      "Violet Vessel": { "min_ante": 8, "score_multiplier": 6.0, "reward": 8, "matador_compatible": false, "effect": "extra_large_blind_showdown" },
      "Crimson Heart": { "min_ante": 8, "score_multiplier": 2.0, "reward": 8, "matador_compatible": false, "effect": "disable_random_joker_per_hand" },
      "Cerulean Bell": { "min_ante": 8, "score_multiplier": 2.0, "reward": 8, "matador_compatible": false, "effect": "force_one_card_selected" }
    }
  }
}
```
