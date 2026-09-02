# Test Cases Specification: Supporting Game Services

Dokumen ini berisi spesifikasi unit test untuk:

- `ConsumableEffectHandler.cs`
- `PokerHandEvaluator.cs`
- `ScoringService.cs`
- `GameSessionService.cs`
- `ShopService.cs`

Target implementasi test:

- `ConsumableEffectHandlerServiceTests.cs`
- `PokerHandEvaluatorServiceTest.cs`
- `ScoringServiceTests.cs`
- `GameSessionServiceTests.cs`
- `ShopServiceTest.cs`

Gunakan NUnit dan Moq sesuai dependensi project. Nama test mengikuti pola `Method_State_ExpectedBehavior`.

## Aturan Implementasi

1. Satu ID di bawah merepresentasikan satu test method. Kasus yang bertanda "parameterized" sebaiknya memakai `[TestCase]` agar tidak membuat banyak method duplikat.
2. Setiap test harus mandiri dan membuat state baru pada `[SetUp]` atau di dalam test.
3. Jangan mengandalkan urutan koleksi acak kecuali RNG sudah dapat dikontrol.
4. Cabang yang menggunakan `Random` harus diuji dengan RNG terinjeksi/terbungkus. Jika production code belum menyediakan seam RNG, tandai test `[Explicit]` atau lakukan refactor kecil terpisah setelah mendapat persetujuan; jangan membuat retry loop probabilistik.
5. Untuk angka `float`, gunakan tolerance, misalnya `Within(0.001f)`.
6. Selain hasil utama, pastikan state yang tidak semestinya berubah tetap sama pada bad case.

---

# Modul A — ConsumableEffectHandler

## Tarot: The Fool dan pembuat kartu

### CEH-1.1 — Good Case — `UseTarot_TheFoolAfterTarot_CreatesCloneOfLastTarot`

- Setup: `LastTarotUsed` berisi Tarot selain `TheFool`; inventory masih memiliki slot.
- Aksi: gunakan `TheFool`.
- Ekspektasi: return `true`; satu Tarot baru ditambahkan dengan `Name`, `Price`, dan `Type` yang sama tetapi instance/ID berbeda; pesan `The Fool created {Name}!`.

### CEH-1.2 — Good Case — `UseTarot_TheFoolAfterPlanet_CreatesCloneOfLastPlanet`

- Setup: `LastTarotUsed = null`, `LastPlanetUsed` terisi, inventory masih memiliki slot.
- Ekspektasi: return `true`; Planet baru memiliki nama dan `PokerHandType` yang sama; pesan sesuai.

### CEH-1.3 — Bad Case — `UseTarot_TheFoolWithoutHistory_ReturnsFalse`

- Setup: `LastTarotUsed` dan `LastPlanetUsed` null.
- Ekspektasi: return `false`; pesan `No previous Tarot or Planet card was used!`; inventory tidak berubah.

### CEH-1.4 — Bad Case — `UseTarot_TheFoolWhenInventoryFull_ReturnsFalse`

- Setup: ada history valid tetapi consumable container penuh.
- Ekspektasi: return `false`; pesan `Consumable inventory is full!`; tidak ada clone.

### CEH-1.5 — Good Case — `UseTarot_HighPriestessWithCapacity_CreatesUpToTwoPlanetCards`

- Setup: slot tersedia; sertakan kartu Tarot aktif dalam inventory sesuai alur nyata `GameController.UseConsumable`.
- Ekspektasi: return `true`; maksimal dua Planet dibuat tanpa melewati kapasitas efektif setelah Tarot dikonsumsi; pesan `Created Planet cards!`.

### CEH-1.6 — Bad Case — `UseTarot_HighPriestessWhenInventoryFull_ReturnsFalse`

- Ekspektasi: return `false`; pesan `Consumable slots are full!`; inventory tidak berubah.

### CEH-1.7 — Good Case — `UseTarot_EmperorWithCapacity_CreatesUpToTwoTarotCards`

- Ekspektasi: return `true`; maksimal dua Tarot valid dibuat; setiap `Type` merupakan nilai enum `TarotType`; pesan `Created Tarot cards!`.

### CEH-1.8 — Bad Case — `UseTarot_EmperorWhenInventoryFull_ReturnsFalse`

- Ekspektasi sama dengan CEH-1.6.

## Tarot: enhancement dan transformasi kartu

### CEH-2.1 — Good Case, parameterized — `UseTarot_OneOrTwoTargets_AppliesExpectedEnhancement`

- Kasus: `TheMagician -> LuckyCards`, `TheEmpress -> MultCards`, `TheHierophant -> BonusCards`.
- Setup: pilih satu dan dua ID kartu yang benar-benar berada di `Hand`.
- Ekspektasi: return `true`; seluruh target mendapat enhancement yang sesuai; kartu non-target tidak berubah; pesan menyebut jumlah target.

### CEH-2.2 — Bad Case, parameterized — `UseTarot_EnhancementTargetCountOutsideRange_ReturnsFalse`

- Kasus: Tarot pada CEH-2.1 dengan nol target atau tiga target.
- Ekspektasi: return `false`; pesan validasi spesifik Tarot; tidak ada kartu berubah.

### CEH-2.3 — Good Case, parameterized — `UseTarot_ExactlyOneTarget_AppliesExpectedEnhancement`

- Kasus: `TheLovers -> WildCards`, `TheChariot -> SteelCards`, `Justice -> GlassCards`, `TheDevil -> GoldCards`, `TheTower -> StoneCards`.
- Ekspektasi: return `true`; tepat satu target berubah; pesan sesuai tipe kartu.

### CEH-2.4 — Bad Case, parameterized — `UseTarot_ExactlyOneTargetWithInvalidCount_ReturnsFalse`

- Kasus: Tarot pada CEH-2.3 dengan nol atau dua target.
- Ekspektasi: return `false`; pesan `Select exactly 1 card ...`; state kartu tidak berubah.

### CEH-2.5 — Good Case, parameterized — `UseTarot_SuitConversion_ConvertsOneToThreeCards`

- Kasus: `TheStar -> Diamonds`, `TheMoon -> Clubs`, `TheSun -> Hearts`, `TheWorld -> Spades`.
- Ekspektasi: satu sampai tiga target berubah ke suit yang ditentukan; rank/enhancement/edition tetap.

### CEH-2.6 — Bad Case, parameterized — `UseTarot_SuitConversionWithInvalidCount_ReturnsFalse`

- Input: nol target atau empat target.
- Ekspektasi: return `false`; pesan `Select 1 to 3 cards ...`; tidak ada suit berubah.

### CEH-2.7 — Good Case — `UseTarot_Strength_IncrementsRanksAndRecalculatesBaseChips`

- Setup: target rank `Nine` dan `King`.
- Ekspektasi: rank naik satu tingkat dan `BaseChips` sesuai `CalculateDefaultBaseChips`.

### CEH-2.8 — Edge Case — `UseTarot_StrengthOnAce_WrapsToTwo`

- Ekspektasi: `Ace` menjadi `Two`; `BaseChips = 2`.

### CEH-2.9 — Good Case — `UseTarot_HangedMan_RemovesSelectedCardsFromHand`

- Setup: satu atau dua target valid.
- Ekspektasi: return `true`; target hilang dari `Hand`; kartu lain tetap; pesan menyebut jumlah kartu.

### CEH-2.10 — Bad Case — `UseTarot_HangedManWithInvalidCount_ReturnsFalse`

- Input: nol atau tiga target.
- Ekspektasi: pesan `Select 1 or 2 cards to destroy.` dan hand tidak berubah.

### CEH-2.11 — Good Case — `UseTarot_Death_CopiesRightCardPropertiesToLeftCard`

- Setup: dua kartu dengan rank, suit, enhancement, edition, chips, mult, dan x-mult berbeda.
- Ekspektasi: semua properti gameplay kartu kiri sama dengan kartu kanan; ID kartu kiri tidak berubah.

### CEH-2.12 — Bad Case — `UseTarot_DeathWithoutExactlyTwoTargets_ReturnsFalse`

- Ekspektasi: pesan `Select exactly 2 cards (first card converts into the second card).`; kedua kartu tidak berubah.

## Tarot: ekonomi, Joker, dan random

### CEH-3.1 — Good Case, parameterized — `UseTarot_Hermit_DoublesMoneyWithTwentyDollarGainCap`

- Kasus: uang `$10 -> $20`; uang `$30 -> $50`.
- Ekspektasi: gain sama dengan `min(20, MoneyBefore)`; pesan menyebut gain.

### CEH-3.2 — Good Case, parameterized — `UseTarot_Temperance_AddsJokerSellValueWithFiftyDollarCap`

- Kasus: total sell value di bawah $50 dan di atas $50.
- Ekspektasi: uang bertambah `min(50, sum SellValue)`; Joker tidak dihapus.

### CEH-3.3 — Bad Case — `UseTarot_WheelOfFortuneWithoutJokers_ReturnsFalse`

- Ekspektasi: pesan `No Jokers available to upgrade!`.

### CEH-3.4 — Good/Random Case — `UseTarot_WheelOfFortuneOnHit_UpgradesOneBaseJokerEdition`

- Gunakan RNG terkontrol untuk menghasilkan hit dan edition tertentu.
- Ekspektasi: tepat satu Joker `Base` berubah menjadi `Foil`, `Holographic`, atau `Polychrome`; return `true`; pesan menyebut Joker dan edition.

### CEH-3.5 — Edge/Random Case — `UseTarot_WheelOfFortuneOnMiss_ReturnsTrueWithoutMutation`

- Gunakan RNG terkontrol untuk miss.
- Ekspektasi: return `true`; pesan `Nope! Wheel of Fortune gave nothing.`; semua Joker tetap.

### CEH-3.6 — Good Case — `UseTarot_JudgementWithFreeSlot_AddsRandomJoker`

- Ekspektasi: return `true`; jumlah Joker bertambah satu; Joker baru valid; pesan `Judgement spawned ...`.

### CEH-3.7 — Bad Case — `UseTarot_JudgementWhenJokerSlotsFull_ReturnsFalse`

- Ekspektasi: pesan `Joker slots are full!`; deck Joker tidak berubah.

## Planet

### CEH-4.1 — Good Case — `UsePlanet_ExistingHandType_IncrementsLevelAndReturnsMessage`

- Setup: level `Flush = 2`.
- Ekspektasi: level menjadi 3; return `true`; pesan `Upgraded Flush to Level 3!`.

### CEH-4.2 — Edge Case — `UsePlanet_MissingHandType_InitializesLevelTwo`

- Setup: hapus key hand type dari dictionary.
- Ekspektasi: key dibuat dengan level 2.

### CEH-4.3 — Good Case — `UsePlanet_WithConstellation_IncrementsXMultByPointOne`

- Setup: Joker `Constellation` dengan `XMultValue = 1.0`.
- Ekspektasi: menjadi `1.1` dengan tolerance; level hand juga naik.

### CEH-4.4 — Edge Case — `UsePlanet_WithoutConstellation_DoesNotMutateOtherJokers`

- Ekspektasi: hanya level poker hand berubah.

## Spectral

### CEH-5.1 — Good/Random Case, parameterized — `UseSpectral_DestroyAndCreate_ReplacesOneCardWithExpectedCards`

- Kasus: `Familiar` menghancurkan 1 dan menambah 3 enhanced face cards; `Grim` menambah 2 enhanced Aces; `Incantation` menambah 4 enhanced numbered cards.
- Gunakan RNG terkontrol untuk kartu yang dihancurkan dan properti kartu baru.
- Ekspektasi: return `true`; kartu lama hilang; delta ukuran hand masing-masing `+2`, `+1`, `+3`; seluruh kartu baru memenuhi rank/enhancement yang disyaratkan.

### CEH-5.2 — Bad Case, parameterized — `UseSpectral_DestroyAndCreateWithEmptyHand_ReturnsFalse`

- Kasus: `Familiar`, `Grim`, `Incantation`.
- Ekspektasi: return `false`; pesan `Hand is empty!`.

### CEH-5.3 — Good Case — `UseSpectral_WraithWithFreeSlot_AddsRareJokerAndResetsMoney`

- Ekspektasi: Joker bertambah satu, rarity `Rare`, uang menjadi 0, pesan sesuai.

### CEH-5.4 — Bad Case — `UseSpectral_WraithWhenJokerSlotsFull_ReturnsFalse`

- Ekspektasi: pesan `Joker slots are full!`; uang dan Joker tidak berubah.

### CEH-5.5 — Good/Random Case — `UseSpectral_Sigil_ConvertsEntireHandToOneSuit`

- Gunakan RNG terkontrol untuk suit.
- Ekspektasi: seluruh kartu memiliki suit sama; rank dan enhancement tetap.

### CEH-5.6 — Bad Case — `UseSpectral_SigilWithEmptyHand_ReturnsFalse`

- Ekspektasi: pesan `Hand is empty!`.

---

# Modul B — PokerHandEvaluator

### PHE-1.1 — Bad/Edge Case, parameterized — `Evaluate_NullOrEmptyCards_ReturnsEmptyHighCardResult`

- Ekspektasi: `HandType = HighCard`; `ScoringCards` dan `UnscoredCards` kosong; tidak melempar exception.

### PHE-1.2 — Edge Case — `Evaluate_AllStoneCards_ReturnsAllCardsAsScoringHighCard`

- Ekspektasi: `HighCard`; semua Stone Cards berada di scoring; unscored kosong.

### PHE-1.3 — Good Case — `Evaluate_HighCard_ReturnsHighestStandardCardOnly`

- Ekspektasi: kartu rank tertinggi menjadi satu-satunya scoring card; sisanya unscored.

### PHE-1.4 — Good Case — `Evaluate_Pair_ReturnsPairAndKickersUnscored`

- Ekspektasi: dua kartu dengan rank sama scoring; kicker unscored.

### PHE-1.5 — Good Case — `Evaluate_TwoPair_ReturnsTwoHighestPairs`

- Setup: dua pair plus kicker.
- Ekspektasi: empat kartu pair scoring; kicker unscored.

### PHE-1.6 — Good Case — `Evaluate_ThreeOfAKind_ReturnsThreeMatchingCards`

- Ekspektasi: trips scoring dan kartu lain unscored.

### PHE-1.7 — Good Case — `Evaluate_Straight_ReturnsFiveSequentialDistinctRanks`

- Setup: minimal lima rank berurutan dengan suit campuran.
- Ekspektasi: `Straight`; tepat lima kartu scoring dalam urutan rank menurun.

### PHE-1.8 — Edge Case — `Evaluate_AceLowStraight_RecognizesWheel`

- Setup: Ace, 2, 3, 4, 5.
- Ekspektasi: `Straight`; scoring order `5,4,3,2,Ace`.

### PHE-1.9 — Bad/Edge Case — `Evaluate_DuplicateRanksDoNotFormStraight`

- Setup: kurang dari lima rank unik walaupun jumlah kartu >= 5.
- Ekspektasi: bukan `Straight`/`StraightFlush`; klasifikasi mengikuti pair/trips yang ada.

### PHE-1.10 — Good Case — `Evaluate_Flush_ReturnsFiveSameSuitCards`

- Ekspektasi: `Flush`; lima kartu scoring.

### PHE-1.11 — Good Case — `Evaluate_WildCardsCompleteFlush`

- Setup: empat kartu satu suit dan satu kartu enhancement `WildCards` dengan suit berbeda.
- Ekspektasi: tetap `Flush`.

### PHE-1.12 — Good Case — `Evaluate_FullHouse_ReturnsTripsAndPair`

- Ekspektasi: lima kartu scoring; kartu ekstra unscored.

### PHE-1.13 — Good Case — `Evaluate_FourOfAKind_ReturnsFourMatchingCards`

- Ekspektasi: empat kartu scoring; kicker unscored.

### PHE-1.14 — Good Case — `Evaluate_StraightFlush_TakesPrecedenceOverStraightAndFlush`

- Ekspektasi: `StraightFlush`; lima kartu sequential dengan flush scoring.

### PHE-1.15 — Edge Case — `Evaluate_CompetingHands_UsesDocumentedPrecedence`

- Setup parameterized untuk input yang sekaligus memuat beberapa pola.
- Ekspektasi precedence: `StraightFlush > FourOfAKind > FullHouse > Flush > Straight > ThreeOfAKind > TwoPair > Pair > HighCard`.

### PHE-1.16 — Edge Case — `Evaluate_StoneCardsAreExcludedFromStandardPatternAndReturnedUnscored`

- Setup: pattern standard plus Stone Card.
- Ekspektasi: Stone tidak membantu pair/straight/flush di evaluator dan muncul sebagai unscored, kecuali seluruh input Stone seperti PHE-1.2.

---

# Modul C — ScoringService

## Nilai dasar dan level

### SS-1.1 — Good Case, parameterized — `GetBaseChipsAndMult_LevelOne_ReturnsDefaultMatrix`

- Kasus dan ekspektasi: `HighCard (5,1)`, `Pair (10,2)`, `TwoPair (20,2)`, `ThreeOfAKind (30,3)`, `Straight (30,4)`, `Flush (35,4)`, `FullHouse (40,4)`, `FourOfAKind (60,7)`, `StraightFlush (100,8)`.

### SS-1.2 — Good Case, parameterized — `GetLevelUpBonus_ReturnsConfiguredMatrix`

- Ekspektasi: `HighCard (10,1)`, `Pair (15,1)`, `TwoPair (20,1)`, `ThreeOfAKind (20,2)`, `Straight (30,3)`, `Flush (15,2)`, `FullHouse (25,2)`, `FourOfAKind (30,3)`, `StraightFlush (40,4)`.

### SS-1.3 — Good Case — `GetBaseChipsAndMult_HigherLevel_AppliesLinearLevelBonus`

- Setup: minimal dua hand type pada level 3.
- Ekspektasi: default + `(level-1) * bonus` untuk chips dan mult.

### SS-1.4 — Edge Case, parameterized — `GetBaseChipsAndMult_LevelBelowOne_ClampsToLevelOne`

- Input: level 0 dan negatif.
- Ekspektasi: sama dengan level 1.

## Kalkulasi inti

### SS-2.1 — Good Case — `CalculateScore_BasicHand_ComposesBaseCardsAndFinalScore`

- Mock evaluator mengembalikan hand/scoring/unscored deterministik.
- Ekspektasi: `TotalChips = BaseChips + CardChips`; `TotalMult = (BaseMult + CardMult) * CardXMult`; `FinalScore = floor(TotalChips * TotalMult)`; DTO menyimpan level dan list evaluator.

### SS-2.2 — Edge Case — `CalculateScore_MissingHandLevel_DefaultsToOne`

- Setup: dictionary tidak memiliki key hasil evaluator.
- Ekspektasi: `HandLevel = 1` dan nilai base level 1.

### SS-2.3 — Good Case — `CalculateScore_StoneCardOutsideEvaluatorScoring_IsAddedToScoring`

- Setup: evaluator menaruh Stone Card di unscored.
- Ekspektasi: Stone dipindah ke scoring, menambah effective chips, dan hilang dari unscored.

### SS-2.4 — Good Case — `CalculateScore_TheFlint_HalvesBaseValuesWithMinimumOne`

- Ekspektasi: chips dan mult dasar di-floor setelah dibagi dua; minimum masing-masing 1; card/Joker modifiers tetap dihitung normal.

### SS-2.5 — Good Case — `CalculateScore_CardEnhancementsAndEditions_ApplyEffectiveValues`

- Parameterized: Bonus, Mult, Stone, Glass, Foil, Holographic, Polychrome.
- Ekspektasi: `CardChips`, `CardMult`, `CardXMult`, dan final score sesuai `PlayingCard.GetEffective*`.

### SS-2.6 — Bad/Edge Case — `CalculateScore_DebuffedScoringCardContributesNoChipsOrMult`

- Ekspektasi: kartu debuffed tidak memberi chips/mult/x-mult tambahan dan tidak memicu efek kondisional Joker.

### SS-2.7 — Good Case — `CalculateScore_SteelCardsHeldInHand_MultiplyCardXMult`

- Setup: dua Steel Cards non-debuffed pada `handCardsRemaining`.
- Ekspektasi: `CardXMult = 1.5^2`; kartu Steel yang debuffed tidak dihitung.

### SS-2.8 — Good Case, parameterized — `CalculateScore_JokerEdition_AppliesEditionBonus`

- Kasus: Foil `+50 chips`, Holographic `+10 mult`, Polychrome `x1.5`.
- Ekspektasi: bonus masuk ke `JokerChips/Mult/XMult` dan `JokerTriggers`.

### SS-2.9 — Good Case — `CalculateScore_JokerBaseValuesAndOrder_AggregateCorrectly`

- Setup: beberapa Joker dengan `ChipsValue`, `MultValue`, dan `XMultValue`.
- Ekspektasi: additive values dijumlah, x-mult dikalikan, `JokerIndex` sesuai urutan input, final score di-floor.

### SS-2.10 — Edge/Random Case — `CalculateScore_LuckyCard_HandlesMultAndMoneyProc`

- Gunakan RNG terkontrol untuk empat skenario: tidak proc, hanya `+20 Mult`, hanya `+$20`, keduanya.
- Ekspektasi: `CardMult`, `LuckyMoneyWon`, dan trigger message tepat; kartu Lucky debuffed tidak proc.

## Joker spesifik

### SS-3.1 — Good Case, parameterized — `CalculateScore_FaceCardJokers_ApplyExpectedBonus`

- Kasus: `ScaryFace +30 chips/face`, `SmileyFace +5 mult/face`, `Photograph x2` untuk face pertama.
- Sertakan face debuffed untuk memastikan tidak dihitung.

### SS-3.2 — Good Case — `CalculateScore_HalfJoker_TriggersOnlyForAtMostThreePlayedCards`

- Kasus boundary: 3 kartu memicu `+20 mult`; 4 kartu tidak.

### SS-3.3 — Good Case, parameterized — `CalculateScore_HeldCardJokers_UseRemainingHand`

- Kasus: `RaisedFist` memakai dua kali rank terendah; `Baron` memberi `1.5^KingCount`; `Blackboard` memberi x3 jika seluruh held cards Spades/Clubs.
- Bad branch: hand kosong, King debuffed, atau ada Hearts/Diamonds tidak memicu.

### SS-3.4 — Good Case, parameterized — `CalculateScore_ResourceJokers_UseSuppliedContext`

- Kasus: `Banner = remainingDiscards*30 chips`, `MysticSummit = +15 mult ketika discard 0`, `AbstractJoker = jokerCount*3 mult`, `Bull = money*2 chips`, `BlueJoker = remainingDeckCards*2 chips`.
- Sertakan boundary 0 untuk memastikan efek yang mensyaratkan nilai positif tidak memicu.

### SS-3.5 — Good Case, parameterized — `CalculateScore_SuitJokers_CountMatchingAndWildCards`

- Kasus: Greedy/Diamonds, Lusty/Hearts, Wrathful/Spades, Gluttonous/Clubs; masing-masing `+3 mult` per kartu.
- Wild Card dihitung sebagai match; kartu debuffed tidak dihitung.

### SS-3.6 — Good Case, parameterized — `CalculateScore_RankJokers_ApplyExpectedBonuses`

- Kasus: Fibonacci `+8 mult`; Even Steven `+4 mult`; Odd Todd `+31 chips`; Scholar `+20 chips +4 mult`; Walkie Talkie `+10 chips +4 mult` per rank yang cocok.
- Sertakan kartu rank yang tidak cocok dan kartu debuffed.

### SS-3.7 — Good Case, parameterized — `CalculateScore_HandTypeMultJokers_TriggerForCompatibleHands`

- Kasus: Jolly, Zany, Mad, Crazy, Droll.
- Uji hand utama dan hand turunan yang diterima, misalnya Jolly pada Pair/TwoPair/FullHouse dan Crazy pada Straight/StraightFlush.

### SS-3.8 — Good Case, parameterized — `CalculateScore_HandTypeChipJokers_TriggerForCompatibleHands`

- Kasus: Sly, Wily, Clever, Devious, Crafty dengan bonus chips sesuai source.

### SS-3.9 — Bad Case, parameterized — `CalculateScore_ConditionalJokerWithUnmatchedCondition_DoesNotTrigger`

- Untuk setiap keluarga SS-3.1–SS-3.8, gunakan satu input non-matching.
- Ekspektasi: tidak ada bonus spesifik dan tidak ada trigger message spesifik.

### SS-3.10 — Edge/Random Case — `CalculateScore_Misprint_AddsRandomMultWithinZeroToTwentyThree`

- Gunakan RNG terkontrol untuk nilai batas 0 dan 23.
- Ekspektasi: bonus dan pesan sesuai; final score mengikuti nilai tersebut.

### SS-3.11 — Good Case — `CalculateScore_TriggerDtosAndMessages_DescribeEveryTriggeredJoker`

- Setup: beberapa Joker yang pasti memicu.
- Ekspektasi: satu `JokerTriggerEffectDto` per Joker yang memiliki message; ID/index/chips/mult/x-mult akurat; `JokerTriggerMessages` berisi ringkasan nama Joker.

---

# Modul D — GameSessionService

### GSS-1.1 — Good Case — `GetOrCreateSession_NullOrBlankId_UsesDefaultSession`

- Parameterized: null, empty, whitespace.
- Ekspektasi: session memiliki `SessionId = "default"`; `StartGame` telah dijalankan; state awal valid.

### GSS-1.2 — Good Case — `GetOrCreateSession_NewId_CreatesConfiguredEngine`

- Ekspektasi: ID sesuai input; player name sesuai input atau `Player 1`; dependency service yang sama digunakan; game sudah dimulai.

### GSS-1.3 — Good Case — `GetOrCreateSession_ExistingId_ReturnsSameInstance`

- Panggil dua kali dengan ID sama dan playerName berbeda.
- Ekspektasi: instance sama; nama player tidak ditimpa pada pemanggilan kedua.

### GSS-1.4 — Concurrency Case — `GetOrCreateSession_ConcurrentSameId_ReturnsSingleInstance`

- Jalankan banyak task dengan ID sama.
- Ekspektasi: seluruh result reference-equal dan hanya satu session tersimpan/diinisialisasi.

### GSS-1.5 — Good Case — `GetSession_ExistingId_ReturnsSession`

- Ekspektasi: instance sama dengan yang dibuat sebelumnya.

### GSS-1.6 — Bad Case — `GetSession_UnknownId_ReturnsNull`

- Ekspektasi: null dan tidak membuat session baru.

### GSS-1.7 — Good Case — `CreateNewSession_CreatesUniqueThirtyTwoCharacterId`

- Buat dua session.
- Ekspektasi: ID berbeda, format Guid `N` sepanjang 32 karakter, keduanya dapat diambil, player name terpasang.

### GSS-1.8 — Good Case — `RemoveSession_ExistingId_RemovesSessionAndReturnsTrue`

- Ekspektasi: return `true`; `GetSession(id)` null; cleanup listener dijalankan; log removal ditulis jika logger tersedia.

### GSS-1.9 — Bad Case — `RemoveSession_UnknownId_ReturnsFalse`

- Ekspektasi: return `false`; tidak ada log removal dan tidak melempar exception.

### GSS-1.10 — Good Case — `ConfiguredEngine_EventsWriteStructuredLogs`

- Trigger event publik melalui alur engine: select blind, play hand, score, defeat, cashout/shop, next round/ante, add card, win, game over.
- Ekspektasi: logger menerima template dan parameter terstruktur yang benar, terutama `SessionId`; jangan assert string timestamp atau seluruh rendered text.

### GSS-1.11 — Good Case — `RemoveSession_UnsubscribesAllEngineEventHandlers`

- Simpan reference engine, remove session, lalu trigger event pada reference lama.
- Ekspektasi: tidak ada log event baru dari handler session service.

### GSS-1.12 — Edge Case — `SessionService_WithNullLogger_OperatesWithoutExceptions`

- Ekspektasi: create/get/remove dan event engine tetap berjalan tanpa logger.

---

# Modul E — ShopService

## Populate dan reroll

### SHOP-1.1 — Good Case — `PopulateShop_DefaultState_ResetsAndCreatesTwoCardsAndTwoBoosters`

- Setup: shop memiliki offer, reroll count, dan opened pack lama.
- Ekspektasi: state lama dibersihkan; `RerollCount = 0`; total card offers = 2; booster packs = 2; opened pack null.

### SHOP-1.2 — Good Case — `PopulateShop_WithOverstock_CreatesThreeCardOffers`

- Ekspektasi: `MaxItemCardOffers = 3` dan total lintas Joker/Playing/Tarot/Planet/Spectral offers = 3.

### SHOP-1.3 — Good Case — `PopulateShop_WithCurrentAnteVoucher_ShowsVoucher`

- Setup: voucher belum dibeli.
- Ekspektasi: `Shop.Voucher` reference-equal dengan `currentAnteVoucher`.

### SHOP-1.4 — Good Case, parameterized — `PopulateShop_VoucherUnavailable_ClearsVoucher`

- Kasus: `currentAnteVoucher = null`; atau `isAnteVoucherPurchased = true`.
- Ekspektasi: `Shop.Voucher = null`.

### SHOP-1.5 — Good Case — `RerollShop_ExistingOffers_ReplacesOnlyCardOffers`

- Ekspektasi: seluruh card offer lama hilang dan jumlah offer baru = `MaxItemCardOffers`; booster, voucher, reroll count, dan opened pack tidak diubah oleh service ini.

### SHOP-1.6 — Edge/Random Case — `GenerateRandomShopCard_ClearanceSale_AppliesTwentyFivePercentDiscountWithMinimumOne`

- Gunakan RNG terkontrol untuk tiap cabang Joker/Tarot/Planet/PlayingCard.
- Ekspektasi: price `floor(base*0.75)`, minimum 1.

### SHOP-1.7 — Edge/Random Case — `GenerateRandomShopCard_MerchantAndMagicTrickVouchers_UseConfiguredWeights`

- Gunakan RNG terkontrol pada batas interval untuk TarotMerchant, PlanetMerchant, dan MagicTrick.
- Ekspektasi: roll batas masuk ke tipe offer yang benar; jangan gunakan test statistik berulang.

## Joker generation

### SHOP-2.1 — Good Case — `GenerateRandomJoker_ReturnsIndependentCopyFromCatalog`

- Ekspektasi: ID non-empty dan unik antar hasil; field gameplay berasal dari salah satu catalog entry; memodifikasi hasil pertama tidak mengubah hasil kedua/template.

### SHOP-2.2 — Good/Random Case, parameterized — `GenerateRandomJoker_EditionRoll_AssignsExpectedEdition`

- RNG terkontrol untuk Base, Foil, Holographic, Polychrome.
- Ekspektasi: threshold normal 10%; dengan Hone 20%; type split 50/35/15.

### SHOP-2.3 — Edge Case — `GenerateRandomJoker_AlwaysReturnsValidCatalogMetadata`

- Ekspektasi: nama tidak kosong, rarity/modifier valid, price non-negatif, description tidak kosong, `XMultValue` valid.

## Booster pack

### SHOP-3.1 — Good Case, parameterized — `OpenBoosterPack_ByType_GeneratesExpectedCardCollection`

- Kasus: Arcana -> Tarot, Celestial -> Planet, Standard -> PlayingCard, Buffoon -> Joker, Spectral -> SpectralCard.
- Ekspektasi: `IsOpened = true`; hanya collection yang sesuai terisi; jumlah sama dengan `TotalCard`; harga kartu isi pack 0 bila constructor menetapkannya demikian.

### SHOP-3.2 — Good Case — `OpenBoosterPack_PrepopulatedPack_ClearsOldContentsBeforeGeneration`

- Ekspektasi: semua isi lama dihapus sebelum item baru dibuat; tidak ada card lama tersisa.

### SHOP-3.3 — Good Case — `OpenBoosterPack_CelestialWithTelescope_FirstPlanetMatchesMostPlayedHand`

- Ekspektasi: Planet pertama memiliki `PokerHandType` yang diberikan; jumlah total tetap `TotalCard`.

### SHOP-3.4 — Edge Case — `OpenBoosterPack_ZeroTotalCards_OpensWithEmptyCollections`

- Ekspektasi: `IsOpened = true`; seluruh collection kosong; tidak melempar exception.

### SHOP-3.5 — Edge/Random Case, parameterized — `GeneratedBoosterPack_SizeControlsPriceCardCountAndMaxPick`

- Akses melalui `PopulateShop` atau ekstrak generator dengan seam internal.
- Ekspektasi: Normal `3 cards/1 pick`, Jumbo `5/1`, Mega `5/2`; Buffoon/Spectral base price 6, lainnya 4; Jumbo +2, Mega +4; ClearanceSale memberi diskon 25% minimum 1.

## Voucher generation

### SHOP-4.1 — Good Case — `GenerateVoucherForAnte_WithAvailableEffects_ReturnsUnpurchasedEffect`

- Ekspektasi: voucher tidak null; effect tidak ada dalam `purchasedVouchers`; price 10; nama sesuai effect. Nilai `ante` tidak mengubah pool pada implementasi saat ini.

### SHOP-4.2 — Good Case — `GenerateVoucherForAnte_ExcludesEveryPurchasedEffect`

- Setup: beli semua kecuali satu effect.
- Ekspektasi: effect yang tersisa selalu dipilih.

### SHOP-4.3 — Bad/Edge Case — `GenerateVoucherForAnte_AllEffectsPurchased_ReturnsNull`

- Ekspektasi: null tanpa mutation pada list pembelian.

### SHOP-4.4 — Edge Case — `GenerateVoucherForAnte_DuplicatePurchasedEntries_RemainsSafe`

- Ekspektasi: duplicate effect tetap hanya mengecualikan effect tersebut; hasil berasal dari effect lain.

---

# Urutan Implementasi yang Disarankan

1. `PokerHandEvaluatorServiceTest.cs` — deterministic dan dependency-free.
2. `GameSessionServiceTests.cs` — fokus lifecycle dictionary dan event cleanup.
3. `ShopServiceTest.cs` — mulai dari booster/voucher deterministic, lalu RNG seam.
4. `ConsumableEffectHandlerServiceTests.cs` — kelompokkan dengan parameterized tests.
5. `ScoringServiceTests.cs` — implementasikan kalkulasi inti dahulu, kemudian matriks Joker.

# Definition of Done

- Seluruh good case memverifikasi return value, output object, dan mutation yang memang diharapkan.
- Seluruh bad case memverifikasi pesan/hasil gagal dan memastikan state utama tidak berubah.
- Test random tidak menggunakan retry loop atau asumsi probabilitas.
- Test parameterized memiliki nama kasus yang terbaca jelas di output NUnit.
- Tidak ada ketergantungan antar-test atau penggunaan state static yang bocor.
- `dotnet test BackendBalatro.Tests/BackendBalatro.Tests.csproj` lulus tanpa skipped test yang tidak terdokumentasi.
