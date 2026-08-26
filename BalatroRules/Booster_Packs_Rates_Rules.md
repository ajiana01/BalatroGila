# Spesifikasi Peluang Kemunculan Booster Pack (Drop Rates)

Dokumen ini berisi aturan probabilitas dan bobot (*weight*) untuk menentukan jenis dan ukuran Booster Pack yang ditawarkan kepada pemain di dalam toko (Shop). Format ini disusun untuk memudahkan implementasi logika probabilitas ke dalam kode atau sebagai instruksi bagi model AI.

## Aturan Dasar Probabilitas
Booster Pack yang ditawarkan kepada pemain ditentukan menggunakan probabilitas berdasarkan Tipe (*Type*) dan Ukuran (*Size*). 
Setiap jenis Booster Pack memiliki **bobot (weight)**. Bobot ini menentukan seberapa besar kemungkinan paket tersebut ditarik (*drawn*). 
*Contoh:* Jumbo Arcana Pack memiliki kemungkinan 4x lebih besar untuk ditawarkan daripada Mega Arcana Pack, tetapi hanya setengah (0.5x) kemungkinannya dibandingkan Normal Arcana Pack.

---

## 1. Tabel Spesifikasi Probabilitas

| Nama Pack | Normal Pack (Bobot \| Peluang) | Jumbo Pack (Bobot \| Peluang) | Mega Pack (Bobot \| Peluang) |
| :--- | :--- | :--- | :--- |
| **Standard Pack** | 4 \| 17.84% | 2 \| 8.92% | 0.5 \| 2.23% |
| **Arcana Pack** | 4 \| 17.84% | 2 \| 8.92% | 0.5 \| 2.23% |
| **Celestial Pack** | 4 \| 17.84% | 2 \| 8.92% | 0.5 \| 2.23% |
| **Buffoon Pack** | 1.2 \| 5.35% | 0.6 \| 2.68% | 0.15 \| 0.67% |
| **Spectral Pack** | 0.6 \| 2.68% | 0.3 \| 1.34% | 0.07 \| 0.31% |

---

## 2. Format JSON (Untuk Implementasi Kode / RNG Generation)

Struktur data JSON di bawah ini dapat langsung digunakan ke dalam sistem *Random Number Generator* (RNG) atau konfigurasi *weighted draw* di dalam memori game.

```json
{
  "booster_pack_rates": {
    "mechanics": "Packs are offered based on weighted probabilities. Higher weight means higher chance to appear.",
    "packs": {
      "Standard Pack": {
        "Normal": { "weight": 4.0, "chance_percent": 17.84 },
        "Jumbo": { "weight": 2.0, "chance_percent": 8.92 },
        "Mega": { "weight": 0.5, "chance_percent": 2.23 }
      },
      "Arcana Pack": {
        "Normal": { "weight": 4.0, "chance_percent": 17.84 },
        "Jumbo": { "weight": 2.0, "chance_percent": 8.92 },
        "Mega": { "weight": 0.5, "chance_percent": 2.23 }
      },
      "Celestial Pack": {
        "Normal": { "weight": 4.0, "chance_percent": 17.84 },
        "Jumbo": { "weight": 2.0, "chance_percent": 8.92 },
        "Mega": { "weight": 0.5, "chance_percent": 2.23 }
      },
      "Buffoon Pack": {
        "Normal": { "weight": 1.2, "chance_percent": 5.35 },
        "Jumbo": { "weight": 0.6, "chance_percent": 2.68 },
        "Mega": { "weight": 0.15, "chance_percent": 0.67 }
      },
      "Spectral Pack": {
        "Normal": { "weight": 0.6, "chance_percent": 2.68 },
        "Jumbo": { "weight": 0.3, "chance_percent": 1.34 },
        "Mega": { "weight": 0.07, "chance_percent": 0.31 }
      }
    }
  }
}