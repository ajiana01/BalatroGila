# Test Cases Specification: GameController

Dokumen ini berisi spesifikasi lengkap **Unit Test Cases** untuk [GameController.cs](../BackendBalatro/Services/Core/GameController.cs) menggunakan **NUnit**, **Moq**, dan **Serilog Structured Logging**.

---

## 1. Standar & Konvensi

### 1.1. Test Naming Convention
Setiap test case mengikuti standar penamaan:
```text
MethodName_StateUnderTest_ExpectedBehavior
```
- **MethodName**: Nama metode pada `GameController` yang diuji (e.g., `PlayHand`, `SelectBlind`, `BuyCardFromShop`).
- **StateUnderTest**: Kondisi input, state controller, atau modifier saat pengujian dijalankan (e.g., `ValidCardsInHand`, `InsufficientMoney`, `ThePsychicBossActive`).
- **ExpectedBehavior**: Hasil akhir yang diekspektasikan (e.g., `ReturnsSuccessAndUpdatesScore`, `ThrowsException`, `ReturnsFailureResult`).

### 1.2. Structured Logging dengan Serilog
Penggunaan logging di seluruh alur `GameController` harus menggunakan **Serilog Message Templates** (bukan *string interpolation* `$""`) agar properti log terindeks secara individual:

```csharp
// Standar Benar (Structured Logging):
_logger.LogInformation("Player played hand {HandType} with {CardCount} cards scoring {FinalScore} for Session {SessionId}",
    result.HandType, playedCards.Count, result.FinalScore, SessionId);

// Standar Salah (Plain Text String Interpolation):
_logger.LogInformation($"Player played hand {result.HandType} with score {result.FinalScore}");
```

---

## 2. Matriks Test Cases

### Modul 1: Game Lifecycle & State Initialization

| ID | Kategori | Nama Test Case | Skenario Pengujian | Ekspektasi Hasil |
| :--- | :--- | :--- | :--- | :--- |
| **TC-1.1** | Good Case | `StartGame_NewGame_InitializesDefaultStateAnd52CardDeck` | Memulai permainan baru dari state awal. | `CurrentAnte=1`, `CurrentRound=1`, `Money=4`, `MaxHands=4`, `MaxDiscards=4`, `MaxHand=8`, `DrawPile=52 kartu`, `Phase=SelectingBlind`. Return `true`. |
| **TC-1.2** | Good Case | `StartGame_PokerHands_InitializesAllLevelsToOneAndPlayedCountToZero` | Cek level poker hand awal. | Semua tipe poker hand (`HighCard` s.d. `FlushFive`) memiliki `Level = 1` dan `PlayedCount = 0`. |
| **TC-1.3** | Good Case | `StartGame_ExistingState_CleansUpHandDiscardAndVouchers` | Game di-restart saat ada sisa kartu/voucher dari sesi sebelumnya. | `Hand`, `DiscardPile`, `UsableCards`, `JokerCards`, dan `PurchasedVouchers` dibersihkan total. |
| **TC-1.4** | Good Case | `Win_WhenInvoked_SetsPhaseToVictoryAndFiresOnWinGameEvent` | Fungsi `Win()` dipanggil. | `Phase` menjadi `GameStatePhase.Victory` dan event `OnWinGame` terpicu 1x. |
| **TC-1.5** | Good Case | `GameOver_WhenInvoked_SetsPhaseToGameOverAndFiresOnGameOverEvent` | Fungsi `GameOver()` dipanggil saat kehabisan Hands. | `Phase` menjadi `GameStatePhase.GameOver` dan event `OnGameOver` terpicu 1x. |
| **TC-1.6** | Good Case | `AdvanceAnte_NextAnte_IncrementsAnteResetsDebuffTrackerAndGeneratesNewBlinds` | Pindah ke Ante berikutnya. | `CurrentAnte` naik 1, voucher ante baru di-generate dari `IShopService`, 3 blind baru terisi di `BlindEnemies`. |
| **TC-1.7** | Good Case | `GetGameState_WhenInShopPhase_ReturnsCompleteShopDtoAndGameState` | Memanggil `GetGameState()` saat fase Shop. | DTO berisi data session, uang, kartu di tangan, sisa deck, level kartu poker, dan seluruh penawaran shop aktif. |
| **TC-1.8** | Bad / Edge Case | `StartGame_WhenShopServiceReturnsNull_HandlesNullVoucherGracefully` | `_shopService.GenerateVoucherForAnte` mengembalikan `null` (misal voucher habis/gagal). | `CurrentAnteVoucher` bernilai `null` tanpa crash (`NullReferenceException`), game tetap berjalan. |
| **TC-1.9** | Bad / Edge Case | `AdvanceAnte_BeyondMaxAnte_CalculatesExponentialBaseScoreCorrectly` | Ante melampaui Ante 8 (Endless mode Ante 9+). | Target skor blind terhitung dengan formula eksponensial `50000 * 1.5^(Ante-8)` tanpa *integer overflow*. |
| **TC-1.10** | Bad / Edge Case | `GetGameState_WhenShopCollectionsAreEmpty_DoesNotThrowNullReference` | Memanggil `GetGameState()` saat koleksi shop kosong/null. | Return DTO yang valid dengan list kosong, bukan melempar *NullReferenceException*. |
| **TC-1.11** | Bad / Edge Case | `Win_WhenInvokedDuringGameOverPhase_DoesNotProduceInconsistentState` | Memanggil `Win()` saat kondisi permainan sudah `GameOver`. | Memastikan transisi state terdokumentasi dan konsisten sesuai kebijakan lifecycle engine. |

---

### Modul 2: Blind Selection & Boss Generation

| ID | Kategori | Nama Test Case | Skenario Pengujian | Ekspektasi Hasil |
| :--- | :--- | :--- | :--- | :--- |
| **TC-2.1** | Good Case | `SelectBlind_ValidSmallBlind_TransitionsToPlayingPhaseAndDrawsInitialHand` | Memilih Small Blind (Id: 1) saat `Phase == SelectingBlind`. | `CurrentBlind` terisi, kartu dibagikan ke `Hand` sejumlah `MaxHand` (8), `Phase=Playing`, event `OnBlindSelected` terpicu. |
| **TC-2.2** | Good Case | `SelectBlind_CardsFromPreviousRound_RecyclesAndClearsDebuffs` | Memilih blind baru setelah ronde sebelumnya selesai. | Seluruh kartu dari Hand dan DiscardPile dikembalikan ke DrawPile, status `IsDebuffed` semua kartu di-reset jadi `false`. |
| **TC-2.3** | Bad Case | `SelectBlind_InvalidBlindId_ReturnsFalseAndPhaseUnchanged` | `blindId` tidak ditemukan (misal: ID `999` atau `-1`). | Return `false`, `CurrentBlind` tetap null, `Phase` tetap `SelectingBlind`. |
| **TC-2.4** | Bad Case | `SelectBlind_AlreadyDefeatedBlind_ReturnsFalse` | Memilih blind yang sudah berstatus `IsDefeated = true`. | Return `false`, ronde tidak dimulai ulang. |
| **TC-2.5** | Bad Case | `SelectBlind_WhenNotInSelectingBlindPhase_ReturnsFalse` | Memanggil `SelectBlind` saat game sedang `Playing` atau `InShop`. | Return `false`, state permainan tidak terganggu. |
| **TC-2.6** | Good Case | `RerollBossBlind_WithDirectorsCutVoucherAndSufficientMoney_RerollsBossBlind` | Pemain memiliki voucher `DirectorsCut` dan uang >= $10. | Uang berkurang $10, `IsBossBlindRerolledThisAnte = true`, boss blind digantikan dengan boss blind baru dari pool. |
| **TC-2.7** | Bad Case | `RerollBossBlind_WithoutDirectorsCutVoucher_ReturnsFailure` | Pemain mencoba reroll boss tanpa memiliki voucher `DirectorsCut`. | Return `OperationResult.Fail("Director's Cut voucher required to reroll Boss Blind.")`. |
| **TC-2.8** | Bad Case | `RerollBossBlind_AlreadyRerolledInSameAnte_ReturnsFailure` | Pemain mencoba reroll boss kedua kalinya dalam Ante yang sama. | Return `OperationResult.Fail("Boss Blind can only be rerolled once per Ante.")`. |
| **TC-2.9** | Bad Case | `RerollBossBlind_InsufficientMoney_ReturnsFailure` | Pemain memiliki voucher `DirectorsCut` tetapi uang < $10. | Return `OperationResult.Fail("Not enough money to reroll Boss Blind (Costs $10).")`. |

---

### Modul 3: Play Hand Mechanics & Scoring

| ID | Kategori | Nama Test Case | Skenario Pengujian | Ekspektasi Hasil |
| :--- | :--- | :--- | :--- | :--- |
| **TC-3.1** | Good Case | `PlayHand_ValidCardsSelected_CalculatesScoreReducesHandsAndDrawsCards` | Memainkan 1–5 kartu valid dari tangan saat `Phase == Playing`. | `_scoringService.CalculateScore` dipanggil, `HandsRemaining` berkurang 1, score bertambah, kartu pengganti di-draw. |
| **TC-3.2** | Good Case | `PlayHand_ScoreReachesTarget_DefeatsBlindAndOpensShop` | Skor yang diperoleh membuat `RoundScore >= CurrentBlind.ScoreToDefeat`. | Memanggil `DefeatBlind()`, memicu cashout, status blind `IsDefeated = true`, `Phase` berpindah ke `InShop`. |
| **TC-3.3** | Good Case | `PlayHand_WithLuckyCardMoneyWon_AddsBonusMoneyToPlayer` | Evaluasi skor menghasilkan `LuckyMoneyWon > 0`. | `Money` pemain bertambah sesuai nilai bonus yang dimenangkan. |
| **TC-3.4** | Good Case | `PlayHand_WithGlassCard_DestroysShatteredCardsAndDiscardsSurviving` | Memainkan kartu Glass dan terpicu shatter (hancur). | Kartu yang hancur dihapus permanen (tidak masuk `DiscardPile`), kartu yang selamat masuk `DiscardPile`. |
| **TC-3.5** | Bad Case | `PlayHand_WhenNotInPlayingPhase_ReturnsFailureResult` | Memanggil `PlayHand` saat `Phase` adalah `InShop` atau `SelectingBlind`. | Return `OperationResult.Fail("Cannot play hand while in {Phase} phase.")`. |
| **TC-3.6** | Bad Case | `PlayHand_EmptyCardList_ReturnsFailureResult` | Parameter `cardIds` bernilai null atau kosong (`Count == 0`). | Return `OperationResult.Fail("Must play between 1 and 5 cards.")`. |
| **TC-3.7** | Bad Case | `PlayHand_ExceedsFiveCards_ReturnsFailureResult` | Parameter `cardIds` berisi 6 kartu atau lebih. | Return `OperationResult.Fail("Must play between 1 and 5 cards.")`. |
| **TC-3.8** | Bad Case | `PlayHand_CardNotInHand_ReturnsFailureResult` | ID kartu yang dikirim tidak terdapat di list `Hand` pemain. | Return `OperationResult.Fail("One or more selected cards are not in hand.")`. |
| **TC-3.9** | Bad Case | `PlayHand_LastHandExhaustedWithoutMeetingTarget_TriggersGameOver` | `HandsRemaining` habis (menjadi 0) dan target skor belum tercapai. | Memanggil `GameOver()`, `Phase = GameStatePhase.GameOver`, return message kekalahan. |

---

### Modul 4: Boss Blind Specific Rules & Restrictions

| ID | Boss Blind | Nama Test Case | Aturan / Skenario Pengujian | Ekspektasi Hasil |
| :--- | :--- | :--- | :--- | :--- |
| **TC-4.1** | **The Psychic** | `PlayHand_ThePsychicBoss_FailsWhenPlayingLessThanFiveCards` | Memainkan kurang dari 5 kartu saat melawan `The Psychic`. | Return `Fail("The Psychic forces you to play exactly 5 cards!")`. Jika 5 kartu -> Lolos. |
| **TC-4.2** | **The Eye** | `PlayHand_TheEyeBoss_FailsWhenPlayingRepeatedPokerHandType` | Memainkan tipe poker hand yang sudah pernah dimainkan di ronde yang sama. | Return `Fail("The Eye does not allow repeating {HandName} in this round!")`. |
| **TC-4.3** | **The Mouth** | `PlayHand_TheMouthBoss_FailsWhenPlayingDifferentPokerHandType` | Memainkan tipe poker hand yang berbeda dari tipe hand pertama di ronde tersebut. | Return `Fail("The Mouth only allows playing {AllowedHandType} this round!")`. |
| **TC-4.4** | **The Needle** | `SelectBlind_TheNeedleBoss_SetsInitialHandsToOne` | Memilih blind `The Needle`. | `_currentHand` (Hands Remaining) di-set ke 1 (hanya diberi 1 kesempatan main). |
| **TC-4.5** | **The Water** | `SelectBlind_TheWaterBoss_SetsInitialDiscardsToZero` | Memilih blind `The Water`. | `_currentDiscard` (Discards Remaining) di-set ke 0. |
| **TC-4.6** | **The Manacle** | `SelectBlind_TheManacleBoss_ReducesEffectiveHandSizeByOne` | Memilih blind `The Manacle`. | Ukuran tangan efektif saat draw berkurang 1 (e.g. 7 kartu dari base 8). |
| **TC-4.7** | **The Arm** | `PlayHand_TheArmBoss_DecreasesPlayedPokerHandLevelByOne` | Memainkan poker hand dengan level > 1 saat melawan `The Arm`. | Level poker hand yang dimainkan berkurang 1 level (`PokerHandLevels[handType] - 1`). |
| **TC-4.8** | **The Tooth** | `PlayHand_TheToothBoss_DeductsOneDollarPerPlayedCard` | Memainkan kartu saat melawan `The Tooth`. | `Money` berkurang $1 untuk setiap kartu yang dimainkan (e.g. main 5 kartu = -$5, min $0). |
| **TC-4.9** | **The Ox** | `PlayHand_TheOxBoss_ResetsMoneyToZeroWhenPlayingMostPlayedHand` | Memainkan tipe poker hand yang paling sering dimainkan sepanjang sesi. | `Money` pemain langsung di-set ke `$0`. |
| **TC-4.10** | **The Hook** | `PlayHand_TheHookBoss_DiscardsTwoRandomCardsFromHand` | Selesai memainkan hand saat melawan `The Hook`. | 2 kartu acak dari sisa di tangan otomatis terbuang ke `DiscardPile`. |
| **TC-4.11** | **Suit Debuffs** | `SelectBlind_SuitDebuffBosses_DebuffsMatchingCards` | Melawan `The Club`, `The Goad`, `The Window`, `The Head`, atau `The Plant`. | Kartu dengan Suit/Face yang bersesuaian mendapatkan properti `IsDebuffed = true`. |
| **TC-4.12** | **Verdant Leaf** | `SellCard_VerdantLeafBoss_LiftsAllCardDebuffsWhenJokerSold` | Melawan showdown boss `Verdant Leaf` lalu menjual 1 Joker. | Seluruh debuff kartu pada Hand, DrawPile, dan DiscardPile dicabut (`IsDebuffed = false`). |

---

### Modul 5: Discard & Preview Mechanics

| ID | Kategori | Nama Test Case | Skenario Pengujian | Ekspektasi Hasil |
| :--- | :--- | :--- | :--- | :--- |
| **TC-5.1** | Good Case | `DiscardCards_ValidCards_DiscardsAndDrawsReplacementCards` | Discard 1–5 kartu valid saat `Phase == Playing` dan `DiscardsRemaining > 0`. | `DiscardsRemaining` berkurang 1, kartu berpindah ke `DiscardPile`, tangan diisi kembali hingga `MaxHand`. |
| **TC-5.2** | Bad Case | `DiscardCards_NoDiscardsRemaining_ReturnsFailure` | Memanggil discard saat `DiscardsRemaining <= 0`. | Return `OperationResult.Fail("No discards remaining.")`. |
| **TC-5.3** | Bad Case | `DiscardCards_WhenNotInPlayingPhase_ReturnsFailure` | Memanggil discard saat fase `InShop` atau `SelectingBlind`. | Return `OperationResult.Fail("Cannot discard cards while in {Phase} phase.")`. |
| **TC-5.4** | Bad Case | `DiscardCards_CardsNotInHand_ReturnsFailure` | ID kartu yang dipilih untuk discard tidak ada di tangan. | Return `OperationResult.Fail("One or more selected cards are not in hand.")`. |
| **TC-5.5** | Good Case | `GetScorePreview_ValidCards_ReturnsCalculatedPreviewWithoutMutatingState` | Meminta preview skor dengan 1–5 kartu valid. | Return `OperationResult.Ok` berisi kalkulasi skor tanpa mengurangi `HandsRemaining` atau `Money`. |
| **TC-5.6** | Bad Case | `GetScorePreview_EmptyOrMoreThanFiveCards_ReturnsFailure` | Input list kartu kosong atau > 5 kartu. | Return `OperationResult.Fail("Select 1 to 5 cards for score preview.")`. |

---

### Modul 6: Blind Defeat, Cashout, & End-of-Round Effects

| ID | Kategori | Nama Test Case | Skenario Pengujian | Ekspektasi Hasil |
| :--- | :--- | :--- | :--- | :--- |
| **TC-6.1** | Good Case | `DefeatBlind_StandardCashout_CalculatesRewardHandsAndInterest` | Blind dikalahkan dengan sisa 2 hands dan uang $15. | Total cashout = Reward ($3-$5) + Sisa Hands ($2) + Interest ($3). Uang bertambah sesuai total. |
| **TC-6.2** | Good Case | `DefeatBlind_WithSeedMoneyVoucher_CapsInterestAtTenDollars` | Pemain memiliki voucher `SeedMoney` dan uang $60. | Bunga maksimal bertambah hingga $10 (bukan dibatasi di default $5). |
| **TC-6.3** | Good Case | `DefeatBlind_WithGoldCardsInHand_AwardsThreeDollarsPerGoldCard` | Terdapat 2 kartu Gold yang tidak didebuff di tangan saat round selesai. | Uang bertambah ekstra $6 (2 kartu * $3). |
| **TC-6.4** | Good Case | `DefeatBlind_WithJokers_TriggersEndOfRoundJokerEffects` | Memiliki `GoldenJoker` (+ $4) dan `Popcorn` (-4 mult). | `Money` bertambah $4, nilai Mult `Popcorn` berkurang 4. |

---

### Modul 7: Shop Purchases & Boosters

| ID | Kategori | Nama Test Case | Skenario Pengujian | Ekspektasi Hasil |
| :--- | :--- | :--- | :--- | :--- |
| **TC-7.1** | Good Case | `BuyCardFromShop_AffordableJoker_AddsToJokersAndDeductsMoney` | Membeli Joker dari penawaran shop dengan uang mencukupi. | Uang berkurang, Joker masuk ke `Deck.JokerCards`, dihapus dari offer shop. |
| **TC-7.2** | Good Case | `BuyCardFromShop_NegativeJokerWhenSlotsFull_SuccessfullyPurchases` | Membeli Joker edisi `Negative` saat slot Joker penuh (5/5). | Pembelian berhasil karena edisi Negative mengabaikan batas slot container. |
| **TC-7.3** | Bad Case | `BuyCardFromShop_InsufficientMoney_ReturnsFailure` | Mencoba membeli kartu dengan `Money < card.Price`. | Return `OperationResult.Fail("Not enough money.")`. |
| **TC-7.4** | Bad Case | `BuyCardFromShop_JokerSlotsFull_ReturnsFailure` | Membeli Joker non-negative saat slot Joker sudah penuh. | Return `OperationResult.Fail("Joker slots are full.")`. |
| **TC-7.5** | Bad Case | `BuyCardFromShop_ConsumableSlotsFull_ReturnsFailure` | Membeli Tarot/Planet saat slot consumable penuh (2/2). | Return `OperationResult.Fail("Consumable slots are full.")`. |
| **TC-7.6** | Good Case | `RerollShop_WithRerollSurplusVoucher_ReducesRerollCostByTwo` | Melakukan reroll shop dengan voucher `RerollSurplus` aktif. | Biaya reroll berkurang $2 (min $0), `Shop.RerollCount` naik, offer baru di-populate. |
| **TC-7.7** | Good Case | `BuyBoosterPack_ValidPack_DeductsMoneyAndOpensPack` | Membeli booster pack di shop. | Uang berkurang, `Shop.OpenedBoosterPack` terisi kartu dari generator. |
| **TC-7.8** | Good Case | `SelectBoosterCard_PicksCardUntilMaxPickReached_ClosesPack` | Memilih kartu dari booster hingga kuota `MaxPick` habis. | Kartu masuk inventaris yang sesuai, `OpenedBoosterPack` otomatis menjadi null. |
| **TC-7.9** | Good Case | `BuyVoucher_VoucherEffects_AppliesRespectivePermanentBonus` | Membeli voucher `Grabber`, `Wasteful`, `PaintBrush`, atau `Hieroglyph`. | Bonus permanen terpasang (`MaxHands+1`, `MaxDiscards+1`, `MaxHand+1`, dsb). |
| **TC-7.10** | Good Case | `LeaveShop_DefeatedAnteEightBoss_TriggersVictory` | Memanggil `LeaveShop()` setelah mengalahkan Boss Ante 8. | Memanggil `Win()`, `Phase = GameStatePhase.Victory`, return pesan kemenangan. |

---

### Modul 8: Consumables & Inventory Management

| ID | Kategori | Nama Test Case | Skenario Pengujian | Ekspektasi Hasil |
| :--- | :--- | :--- | :--- | :--- |
| **TC-8.1** | Good Case | `UseConsumable_ValidTarotCard_ExecutesEffectAndRemovesFromDeck` | Menggunakan kartu Tarot dari inventaris. | `_consumableHandler.UseTarot` dipanggil, `LastTarotUsed` tersimpan, kartu dihapus dari `UsableCards`. |
| **TC-8.2** | Bad Case | `UseConsumable_CardNotFound_ReturnsFailure` | Menggunakan kartu dengan ID yang tidak ada di inventaris. | Return `OperationResult.Fail("Consumable card not found in inventory.")`. |
| **TC-8.3** | Good Case | `SellCard_ExistingJoker_RemovesJokerAndIncreasesMoneyBySellValue` | Menjual Joker dari inventaris. | Joker terhapus dari `Deck.JokerCards`, `Money += SellValue`. |
| **TC-8.4** | Good Case | `SellCard_ExistingConsumable_RemovesConsumableAndAddsHalfPrice` | Menjual kartu Tarot/Planet. | Consumable terhapus, `Money += Math.Max(1, Price / 2)`. |
| **TC-8.5** | Bad Case | `SellCard_CardNotInInventory_ReturnsFailure` | Menjual kartu dengan ID yang tidak valid. | Return `OperationResult.Fail("Card not found in Jokers or Consumables.")`. |
| **TC-8.6** | Good Case | `ArrangeJokers_ValidOrder_ReordersJokerDeck` | Mengatur ulang urutan Joker dengan seluruh ID yang valid. | Urutan `Deck.JokerCards` diperbarui sesuai urutan input. |
| **TC-8.7** | Bad Case | `ArrangeJokers_CountMismatchOrInvalidId_ReturnsFailure` | Mengatur ulang Joker dengan jumlah ID tidak cocok atau ada ID fiktif. | Return `OperationResult.Fail(...)`, urutan Joker asli tidak berubah. |

---

### Modul 9: Structured Logging Verification (Serilog)

| ID | Kategori | Nama Test Case | Skenario Pengujian Log | Verifikasi Structured Logging |
| :--- | :--- | :--- | :--- | :--- |
| **TC-9.1** | Good Case | `PlayHand_WhenExecuted_LogsStructuredPlayHandEvent` | Menjalankan `PlayHand` berhasil. | Log `Information` tercatat dengan properti terstruktur `{HandType}`, `{Score}`, `{SessionId}`. |
| **TC-9.2** | Good Case | `DefeatBlind_WhenBlindDefeated_LogsStructuredCashoutAndPhaseTransition` | Mengalahkan blind. | Log `Information` memuat properti `{BlindName}`, `{TotalReward}`, `{Interest}`. |
| **TC-9.3** | Bad Case | `PlayHand_WhenInvalidCards_LogsStructuredWarningWithReason` | `PlayHand` gagal karena kartu tidak di tangan. | Log `Warning` mencatat `{Reason}`, `{Phase}`, `{SessionId}` secara terstruktur tanpa string formatting crash. |
| **TC-9.4** | Bad Case | `BuyCardFromShop_InsufficientMoney_LogsWarningWithCurrentAndRequiredMoney` | Gagal beli di shop karena uang kurang. | Log `Warning` mencatat `{CardName}`, `{Price}`, `{CurrentMoney}`. |
