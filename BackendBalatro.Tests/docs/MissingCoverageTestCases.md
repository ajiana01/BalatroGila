# Test Cases yang Belum Tercakup Coverage

Dokumen ini dibuat dari hasil aktual:

```text
dotnet test BackendBalatro.Tests/BackendBalatro.Tests.csproj --collect:"XPlat Code Coverage"
Tests: 293 passed
Line coverage: 79.59% (2391/3004)
Branch coverage: 78.03% (995/1275)
```

Angka dapat sedikit berubah pada run berikutnya karena production code masih membuat `Random` secara langsung. Daftar di bawah memprioritaskan line/branch yang memiliki hit `0` atau branch parsial pada laporan Cobertura.

## Aturan pengerjaan

- Kerjakan hanya test; jangan mengubah production code kecuali tugas terpisah secara eksplisit mengizinkannya.
- Untuk controller, gunakan mock `IGameSessionService` dan `IGameController`, serta pasang `ControllerContext` dengan `DefaultHttpContext`.
- Untuk jalur RNG yang tidak dapat dibuat deterministik, tandai kebutuhan seam/injeksi RNG sebagai blocker. Jangan memakai loop statistik untuk membuktikan nilai batas.
- Assert status/result, mutation state, pemanggilan dependency, dan pesan penting. Hindari assert timestamp atau seluruh rendered log.
- Jalankan coverage ulang setelah setiap modul. Coverage 100% seluruh assembly juga memerlukan pengujian `Program.cs`, controller, DTO, dan model—bukan hanya service.

# Prioritas 1 — API Controllers (saat ini 0%)

## API session resolution

### API-1.1 — `ActionController_SessionIdHeaderPresent_UsesHeaderValue`

- Setup header `X-Session-Id`, query berbeda.
- Ekspektasi: header memiliki prioritas dan diteruskan ke `GetOrCreateSession`.

### API-1.2 — `ActionController_HeaderMissing_UsesQuerySessionId`

- Ekspektasi: query `sessionId` dipakai.

### API-1.3 — `ActionController_BlankHeaderAndQuery_UsesDefaultSession`

- Ekspektasi: session ID `default`.

### API-1.4 — `ShopController_SessionResolution_HeaderQueryAndDefault`

- Parameterized untuk header, query, dan fallback default.

### API-1.5 — `GameController_SessionResolution_HeaderRequestAndDefault`

- Parameterized: header menang atas request/query; request ID dipakai tanpa header; blank menjadi `default`.

## ActionController

### API-2.1 — `PlayHand_WhenPhaseNotPlaying_ReturnsBadRequest`

- Ekspektasi: HTTP 400; engine `PlayHand` tidak dipanggil.

### API-2.2 — `PlayHand_EngineFailure_ReturnsBadRequest`

- Setup `Phase=Playing`, engine mengembalikan failure.
- Ekspektasi: HTTP 400 dengan pesan engine.

### API-2.3 — `PlayHand_EngineSuccess_ReturnsOkWithStateAndScore`

- Ekspektasi: HTTP 200; `GetGameState(message, scoreResult)` dipanggil.

### API-2.4 — `Discard_PhaseFailureEngineFailureAndSuccess_ReturnExpectedResponses`

- Parameterized tiga jalur: bukan Playing, engine failure, success.

### API-2.5 — `GetScorePreview_FailureOrNullResult_ReturnsBadRequest`

- Parameterized: `Success=false`; atau `Success=true` tetapi result null.

### API-2.6 — `GetScorePreview_Success_ReturnsScoreDto`

- Ekspektasi HTTP 200 dan DTO reference-equal.

### API-2.7 — `UseConsumable_FailureAndSuccess_ReturnExpectedResponses`

### API-2.8 — `SellCard_FailureAndSuccess_ReturnExpectedResponses`

### API-2.9 — `ReorderJokers_FailureAndSuccess_ReturnExpectedResponses`

### API-2.10 — `ReorderConsumables_FailureAndSuccess_ReturnExpectedResponses`

- Untuk API-2.7–2.10: failure -> 400 tanpa state; success -> 200 dan `GetGameState(message)` dipanggil.

## HTTP GameController

### API-3.1 — `StartGame_NullRequest_UsesDefaultPlayerAndSession`

- Ekspektasi: session default dibuat, `StartGame` dan `GetGameState` dipanggil, HTTP 200.

### API-3.2 — `StartGame_RequestAndHeader_ResolveSessionAndPlayerCorrectly`

- Header menang atas `request.SessionId`; player name diteruskan.

### API-3.3 — `GetState_ReturnsCurrentSessionState`

### API-3.4 — `GetBlinds_ReturnsAnteAndAvailableBlinds`

### API-3.5 — `SelectBlind_WhenWrongPhase_ReturnsBadRequestWithoutSelecting`

### API-3.6 — `SelectBlind_InvalidOrDefeated_ReturnsBadRequest`

### API-3.7 — `SelectBlind_Valid_ReturnsOkWithSelectedBlindState`

### API-3.8 — `RerollBossBlind_FailureAndSuccess_ReturnExpectedResponses`

## ShopController

### API-4.1 — `GetShop_WhenClosed_ReturnsBadRequest`

### API-4.2 — `GetShop_WhenOpen_ReturnsShopDto`

### API-4.3 — `BuyCard_WrongPhaseEngineFailureAndSuccess_ReturnExpectedResponses`

### API-4.4 — `Reroll_WrongPhaseEngineFailureAndSuccess_ReturnExpectedResponses`

### API-4.5 — `BuyBooster_WrongPhaseEngineFailureAndSuccess_ReturnExpectedResponses`

### API-4.6 — `SelectBoosterCard_WrongPhaseEngineFailureAndSuccess_ReturnExpectedResponses`

### API-4.7 — `SkipBooster_WrongPhaseAndSuccess_ReturnExpectedResponses`

### API-4.8 — `BuyVoucher_WrongPhaseEngineFailureAndSuccess_ReturnExpectedResponses`

### API-4.9 — `LeaveShop_WrongPhaseEngineFailureAndSuccess_ReturnExpectedResponses`

### API-4.10 — `RerollBossBlind_FailureAndSuccess_ReturnExpectedResponses`

- Semua success harus memverifikasi `GetGameState(message)` dan response envelope; semua phase failure tidak boleh memanggil operasi engine.

# Prioritas 2 — GameController engine (82.3% line / 75.2% branch)

## Blind, draw, dan lifecycle

### GC-GAP-1.1 — `GetAvailableBlinds_WhenAnteCacheMissing_RegeneratesBlinds`

- Hapus entry ante aktif melalui setup/reflection yang aman.
- Ekspektasi: tiga blind dibuat dan dikembalikan.

### GC-GAP-1.2 — `SelectBlind_ThePillar_DebuffsCardsPreviouslyPlayedThisAnte`

- Mainkan kartu pada blind sebelumnya, kemudian pilih The Pillar.
- Ekspektasi: hanya ID kartu yang pernah dimainkan menjadi debuffed.

### GC-GAP-1.3 — `DrawCards_WhenRequestedCountIsZeroOrHandFull_ReturnsEmpty`

### GC-GAP-1.4 — `DrawCards_WhenDrawPileInsufficient_RecyclesDiscardPile`

- Ekspektasi: discard dipindah, di-shuffle, lalu kartu ditarik hingga kebutuhan terpenuhi.

### GC-GAP-1.5 — `DrawCards_WhileBossActive_AppliesDebuffToNewlyDrawnCards`

### GC-GAP-1.6 — `DefeatBlind_WhenNoCurrentBlind_ReturnsFalseWithoutMutation`

### GC-GAP-1.7 — `Cashout_WhenNoCurrentBlind_ReturnsZero`

### GC-GAP-1.8 — `LeaveShop_WhenNotInShop_ReturnsFailure`

### GC-GAP-1.9 — `LeaveShop_WithNullCurrentBlind_ReturnsToBlindSelection`

### GC-GAP-1.10 — `LeaveShop_AfterBossBeforeAnteEight_AdvancesAnteAndRound`

- Ekspektasi: ante +1, round +1, phase SelectingBlind, voucher/blinds ante baru dibuat.

## Joker end-of-hand/end-of-round dan booster hand

### GC-GAP-2.1 — `PlayHand_WithIceCream_ReducesChipsValueByFiveWithZeroFloor`

- Parameterized ChipsValue 100 dan 3.

### GC-GAP-2.2 — `DefeatBlind_GrosMichelDestroyProc_RemovesJoker`

### GC-GAP-2.3 — `DefeatBlind_CavendishDestroyProc_RemovesJoker`

- GC-GAP-2.2/2.3 membutuhkan RNG seam agar proc dan miss dapat diuji deterministik. Jika production tidak boleh diubah, dokumentasikan blocker.

### GC-GAP-2.4 — `BuyArcanaOrSpectralBooster_WhenHandEmpty_DrawsTemporaryHand`

- Parameterized Arcana dan Spectral; ekspektasi hand diisi hingga `MaxHand`.

### GC-GAP-2.5 — `BuyNonConsumableBooster_WhenHandEmpty_DoesNotDrawHand`

## Play/discard/preview branches

### GC-GAP-3.1 — `PlayHand_WhenPokerHandPlayedDictionaryMissingKey_InitializesCountToOne`

### GC-GAP-3.2 — `DiscardCards_NullEmptyOrMoreThanFive_ReturnsRangeFailure`

- Parameterized null, empty, dan enam ID. Coverage menunjukkan body validasi ini belum dieksekusi.

### GC-GAP-3.3 — `GetScorePreview_CardsNotInHand_PassesOnlyResolvedCardsToScoring`

- Dokumentasikan perilaku saat ini; method tidak memvalidasi jumlah ID yang ditemukan.

## Consumable dispatch dan arrangement

### GC-GAP-4.1 — `UseConsumable_TarotHandlerFailure_KeepsCardAndHistory`

### GC-GAP-4.2 — `UseConsumable_TheFoolSuccess_DoesNotReplaceLastTarotUsed`

### GC-GAP-4.3 — `UseConsumable_PlanetSuccess_StoresHistoryAndRemovesCard`

### GC-GAP-4.4 — `UseConsumable_PlanetFailure_KeepsCardAndHistory`

### GC-GAP-4.5 — `UseConsumable_SpectralSuccess_RemovesCard`

### GC-GAP-4.6 — `UseConsumable_SpectralFailure_KeepsCard`

### GC-GAP-4.7 — `ArrangeConsumables_ValidOrder_ReordersInventory`

### GC-GAP-4.8 — `ArrangeConsumables_NullCountMismatchOrUnknownId_ReturnsFailureWithoutMutation`

## Shop card purchase branches

### GC-GAP-5.1 — `BuyCardFromShop_AffordableTarot_AddsConsumableAndDeductsMoney`

### GC-GAP-5.2 — `BuyCardFromShop_TarotInsufficientMoneyOrFullSlots_ReturnsFailure`

### GC-GAP-5.3 — `BuyCardFromShop_AffordablePlanet_AddsConsumableAndDeductsMoney`

### GC-GAP-5.4 — `BuyCardFromShop_PlanetInsufficientMoneyOrFullSlots_ReturnsFailure`

### GC-GAP-5.5 — `BuyCardFromShop_AffordableSpectral_AddsConsumableAndDeductsMoney`

### GC-GAP-5.6 — `BuyCardFromShop_SpectralInsufficientMoneyOrFullSlots_ReturnsFailure`

### GC-GAP-5.7 — `BuyCardFromShop_AffordablePlayingCard_AddsToDrawPileAndRaisesEvent`

### GC-GAP-5.8 — `BuyCardFromShop_PlayingCardInsufficientMoney_ReturnsFailure`

### GC-GAP-5.9 — `BuyCardFromShop_UnknownOfferId_ReturnsFailure`

### GC-GAP-5.10 — `RerollShop_WhenNotInShopOrInsufficientMoney_ReturnsFailure`

### GC-GAP-5.11 — `RerollShop_WithClearanceAndChaosBranches_UsesEffectiveCost`

## Booster selection and skipping

### GC-GAP-6.1 — `BuyBoosterPack_WhenNotInShopUnknownIdOrInsufficientMoney_ReturnsFailure`

### GC-GAP-6.2 — `SelectBoosterCard_WhenNoPackOpen_ReturnsFailure`

### GC-GAP-6.3 — `SelectBoosterCard_JokerSlotsFull_ReturnsFailure`

### GC-GAP-6.4 — `SelectBoosterCard_NegativeJokerWithFullSlots_Succeeds`

### GC-GAP-6.5 — `SelectBoosterCard_TarotPlanetOrSpectral_AddsToConsumables`

- Parameterized ketiga tipe; sertakan branch consumable slots full.

### GC-GAP-6.6 — `SelectBoosterCard_PlayingCard_AddsToDrawPileAndRaisesEvent`

### GC-GAP-6.7 — `SelectBoosterCard_UnknownId_ReturnsFailure`

### GC-GAP-6.8 — `SelectBoosterCard_FirstPickBelowQuota_KeepsPackOpen`

### GC-GAP-6.9 — `SkipBoosterPack_WhenNoPackOpen_ReturnsFailure`

### GC-GAP-6.10 — `SkipBoosterPack_WhenPackOpen_ClosesPack`

## Voucher branches

### GC-GAP-7.1 — `BuyVoucher_WhenNotInShopMissingVoucherWrongIdOrInsufficientMoney_ReturnsFailure`

### GC-GAP-7.2 — `BuyVoucher_CrystalBall_IncreasesConsumableSlots`

### GC-GAP-7.3 — `BuyVoucher_RerollSurplusSeedMoneyBlankTarotMerchantPlanetMerchantMagicTrickDirectorsCut_HasNoImmediateStatMutation`

- Parameterized; voucher tetap dibeli, uang berkurang, dan flag pembelian terpasang.

### GC-GAP-7.4 — `BuyVoucher_HieroglyphAtAnteOne_DoesNotDropAnteBelowOneAndReducesHands`

### GC-GAP-7.5 — `BuyVoucher_PaintBrush_IncreasesMaxHand`

# Prioritas 3 — Service branch gaps

## ConsumableEffectHandler (95.5% line / 94.0% branch)

### CEH-GAP-1 — `UseTarot_TheFoolAfterPlanet_WhenInventoryFull_ReturnsFalse`

- Gap lines 36–38.

### CEH-GAP-2 — `UseTarot_WheelOfFortune_AssignsHolographicEdition`

- Gap lines 170–171; membutuhkan RNG seam.

### CEH-GAP-3 — `UseTarot_Strength_ZeroOrMoreThanTwoTargets_ReturnsFalse`

### CEH-GAP-4 — `UseTarot_Death_InvalidTargetCount_ReturnsFalseWithoutMutation`

### CEH-GAP-5 — `UseTarot_UnknownEnumValue_ReturnsDefaultSuccessMessage`

- Cast nilai di luar enum ke `TarotType`; ekspektasi `Tarot card used.`.

### CEH-GAP-6 — `UseSpectral_UnknownEnumValue_ReturnsDefaultSuccessMessage`

- Ekspektasi `Spectral card used.`.

## ScoringService (96.2% line / 88.6% branch)

### SS-GAP-1 — `GetBaseChipsAndMult_UnknownHandType_UsesDefaultValuesAndBonus`

- Cast enum di luar range; menutup default switch lines 41 dan 58.

### SS-GAP-2 — `CalculateScore_LuckyCard_AllProcCombinations`

- Empat skenario deterministic: none, mult-only, money-only, both; menutup lines 103–115. Membutuhkan RNG seam karena method membuat `new Random()`.

### SS-GAP-3 — `CalculateScore_HeldCardJokers_BadBranches`

- Raised Fist dengan hand kosong; Baron dengan King debuffed; Blackboard hand kosong atau mengandung Hearts/Diamonds.

### SS-GAP-4 — `CalculateScore_ResourceJokers_ZeroBoundariesDoNotTrigger`

- Banner discard 0; Bull money 0; Blue Joker deck 0.

### SS-GAP-5 — `CalculateScore_SuitJokers_NonMatchingAndDebuffedCardsDoNotTrigger`

### SS-GAP-6 — `CalculateScore_RankJokers_NonMatchingAndDebuffedCardsDoNotTrigger`

### SS-GAP-7 — `CalculateScore_AllHandTypeJokers_UnmatchedHandsDoNotTrigger`

- Parameterized Jolly/Zany/Mad/Crazy/Droll dan Sly/Wily/Clever/Devious/Crafty.

### SS-GAP-8 — `CalculateScore_Misprint_ExactBoundaryZeroAndTwentyThree`

- Membutuhkan RNG seam; test sekarang hanya memeriksa rentang.

## PokerHandEvaluator (100% line / 97.2% branch)

### PHE-GAP-1 — `Evaluate_FewerThanFiveStandardCards_DoesNotAttemptFlushOrStraight`

### PHE-GAP-2 — `Evaluate_FiveCardsWithFewerThanFiveDistinctRanks_ExitsStraightDetection`

- Target branch parsial lines 102 dan 115.

## ShopService (99.5% line / 94.9% branch)

### SHOP-GAP-1 — `GenerateRandomShopCard_HonePlayingCard_AssignsSpecialEdition`

- Target branch line 104. Membutuhkan RNG seam.

### SHOP-GAP-2 — `GeneratedBoosterPack_UnknownPackSize_UsesDefaultThreeCards`

- Target default switch line 217. Karena `PackSize` dipilih RNG dalam private generator, membutuhkan seam atau internal visibility.

### SHOP-GAP-3 — `GeneratedBoosterPack_WithoutClearance_DoesNotDiscount`

- Parameterized vouchers null, empty, dan non-Clearance untuk melengkapi short-circuit branches lines 207/212.

## GameSessionService (100% line / 70.6% branch)

### GSS-GAP-1 — `ConfiguredEngine_EventsWithNullLogger_DoNotInvokeLoggingBranches`

### GSS-GAP-2 — `ConfiguredEngine_EachEventWithLogger_WritesExpectedStructuredProperties`

- Parameterized seluruh 11 event dan assert setiap template/property. Coverage line sudah 100%, tetapi null-conditional logger/event branches belum lengkap.

### GSS-GAP-3 — `RemoveSession_WhenCleanupEntryMissing_StillReturnsTrueWithoutRemovalLog`

- Perlu setup/reflection terkontrol untuk menghapus cleanup dictionary lebih dulu.

# Prioritas 4 — Models dan DTO

## Entity constructors/properties

### MODEL-1.1 — `Blind_DefaultConstructor_UsesPropertyDefaults`

### MODEL-1.2 — `Blind_TypeConstructor_MapsSmallBigAndBossIds`

- Parameterized Small, Big, Boss; menutup lines 31–44.

### MODEL-1.3 — `BoosterPack_DefaultConstructor_UsesDefaults`

### MODEL-1.4 — `Deck_SizedConstructor_SetsBothContainerLimits`

### MODEL-1.5 — `Player_NameConstructor_SetsName`

### MODEL-1.6 — `JokerCard_JokerKey_GetterAndValidInvalidSetter`

- Valid key mengubah enum; invalid key mempertahankan nilai lama.

### MODEL-1.7 — `PlayingCard_DefaultConstructorAndNameSetter_WorkCorrectly`

### MODEL-1.8 — `PlayingCard_CalculateDefaultBaseChips_UnknownRank_UsesNumericValue`

### MODEL-1.9 — `PlayingCard_EffectiveValues_BaseChipsZeroAndBaseXMultNonPositive_UseFallbacks`

### MODEL-1.10 — `TarotPlanetSpectral_DefaultConstructorsAndCustomDescriptions_PreserveValues`

### MODEL-1.11 — `TarotPlanetSpectral_UnknownEnum_ReturnDefaultDescription`

### MODEL-1.12 — `Voucher_DefaultConstructor_CustomDescriptionAndUnknownEffect`

## Shop entity

### MODEL-2.1 — `Shop_RemoveOfferById_RemovesEachSupportedOfferType`

- Parameterized Joker, Playing, Tarot, Planet, Spectral.

### MODEL-2.2 — `Shop_RemoveOfferById_UnknownId_ReturnsFalseWithoutMutation`

## Result and response DTOs

### DTO-1.1 — `ApiResponse_OkAndFail_SetSuccessMessageAndData`

### DTO-1.2 — `OperationResult_DefaultConstructorAndDeconstruct_ReturnStoredValues`

### DTO-1.3 — `GenericOperationResult_DefaultConstructorAliasesAndDeconstruct_ReturnData`

- Assert `Data`, `Result`, dan `Value` reference-equal.

### DTO-1.4 — `RequestDtos_DefaultValuesAndSetters_RoundTrip`

- Parameterized seluruh request DTO di `RequestDtos.cs`.

### DTO-1.5 — `GameStateResponseDto_DefaultConstructor_CoversUninitializedProperties`

# Prioritas 5 — Program startup / integration

`Program.cs` saat ini 0%. Untuk mencapai 100% seluruh assembly, pilih salah satu strategi berikut dalam tugas terpisah:

1. Tambahkan integration test dengan `WebApplicationFactory<Program>` dan uji startup serta route minimal; atau
2. Eksklusi `Program.cs` dan generated/infrastructure DTO dari target unit coverage melalui `.runsettings`, bila definisi coverage proyek memang hanya domain/service.

Test yang diperlukan jika memilih integration coverage:

### HOST-1.1 — `ApplicationHost_StartsAndResolvesRegisteredServices`

### HOST-1.2 — `ApplicationHost_MapsControllersAndSwaggerConfiguration`

### HOST-1.3 — `ApplicationHost_CorsPolicyAllowsConfiguredOriginsMethodsAndHeaders`

### HOST-1.4 — `ApplicationHost_RootOrHealthRequest_CompletesWithoutStartupException`

# Definition of done

- Semua test lama dan baru lulus.
- Coverage dijalankan pada konfigurasi yang sama seperti baseline.
- Tidak ada line dengan hit 0 pada scope yang disepakati.
- Tidak ada branch parsial pada scope yang disepakati, kecuali branch RNG yang memiliki blocker terdokumentasi.
- Jika target tetap seluruh assembly tanpa exclusion, controller dan `Program.cs` wajib tercakup; unit test service saja tidak dapat menghasilkan 100%.
