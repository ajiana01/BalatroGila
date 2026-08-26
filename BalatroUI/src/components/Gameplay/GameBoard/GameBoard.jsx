import { useState, useEffect, useMemo, useRef } from 'react';
import GameSidebar from '../GameSidebar/GameSidebar';
import PlayerHand from './PlayerHand';
import PlayingArea from './PlayingArea';
import CardBack from '../../CardBack/CardBack';
import JokerCard from '../../JokerCard/JokerCard';
import TarotCard from '../../TarotCard/TarotCard';
import PlanetCard from '../../PlanetCard/PlanetCard';
import DeckViewModal from './DeckViewModal';
import DeckHoverPreview from './DeckHoverPreview';
import {
    evaluatePokerHand,
    getCardChipValue,
    evaluateJokerEffects,
    RANK_VALUES,
    RANK_NUMERICAL
} from '../../../utils/pokerEvaluator';
import { sfx } from '../../../utils/sfx';
import './GameBoard.css';

const SUITS = ['Spades', 'Hearts', 'Clubs', 'Diamonds'];
const RANKS = ['A', 'K', 'Q', 'J', '10', '9', '8', '7', '6', '5', '4', '3', '2'];

const SUIT_VALUES = {
    'Spades': 4, 'Hearts': 3, 'Clubs': 2, 'Diamonds': 1
};

// Initial default hand (Matching Flush demonstration)
const defaultHandCards = [
    { id: 'c-1', suit: 'Hearts', rank: 'A' },
    { id: 'c-2', suit: 'Hearts', rank: 'Q' },
    { id: 'c-3', suit: 'Diamonds', rank: '10' },
    { id: 'c-4', suit: 'Clubs', rank: '9' },
    { id: 'c-5', suit: 'Spades', rank: '7' },
    { id: 'c-6', suit: 'Spades', rank: '3' },
    { id: 'c-7', suit: 'Hearts', rank: '3' },
    { id: 'c-8', suit: 'Spades', rank: '2' }
];

const defaultJokers = [
    { id: 'ScaryFace', title: 'Scary Face' },
    { id: 'Joker', title: 'Joker' },
    { id: 'RaisedFist', title: 'Raised Fist' },
    { id: 'AbstractJoker', title: 'Abstract Joker' }
];

const defaultConsumables = [
    { type: 'tarot', id: 'TheTower', title: 'The Tower' }
];

function generateShuffledDeck(excludeCards = []) {
    const excludeSet = new Set(excludeCards.map(c => `${c.suit}-${c.rank}`));
    const deck = [];
    SUITS.forEach(suit => {
        RANKS.forEach(rank => {
            if (!excludeSet.has(`${suit}-${rank}`)) {
                deck.push({
                    id: `c-${suit}-${rank}-${Math.random().toString(36).substr(2, 6)}`,
                    suit,
                    rank
                });
            }
        });
    });
    // Fisher-Yates shuffle
    for (let i = deck.length - 1; i > 0; i--) {
        const j = Math.floor(Math.random() * (i + 1));
        [deck[i], deck[j]] = [deck[j], deck[i]];
    }
    return deck;
}

function GameBoard({
    gameData,
    onWin,
    onLose,
    onOpenSettings
}) {
    const [isDeckModalOpen, setIsDeckModalOpen] = useState(false);
    const [isDeckHovered, setIsDeckHovered] = useState(false);

    // Cards in hand
    const [cards, setCards] = useState(() => gameData?.handCards || defaultHandCards);
    const [selectedIds, setSelectedIds] = useState([]);

    // Deck remaining
    const [deck, setDeck] = useState(() => generateShuffledDeck(gameData?.handCards || defaultHandCards));

    // Jokers & Consumables
    const maxJokers = gameData?.maxJokers || 5;
    const [jokers, setJokers] = useState(gameData?.jokers || defaultJokers);

    const maxConsumables = gameData?.maxConsumables || 2;
    const [consumables, setConsumables] = useState(gameData?.consumables || defaultConsumables);

    const [activeSlot, setActiveSlot] = useState(null); // { type: 'joker' | 'consumable', index: number } | null
    const maxHandSize = gameData?.maxHandSize || 8;

    // =========================================
    // SCORING & ANIMATION STATE
    // =========================================
    const [isScoring, setIsScoring] = useState(false);
    const [playedCards, setPlayedCards] = useState([]);
    const [scoringCardIndex, setScoringCardIndex] = useState(-1);
    const [scoringCardIds, setScoringCardIds] = useState(new Set());
    const [floatingScores, setFloatingScores] = useState({});
    const [activeJokerTrigger, setActiveJokerTrigger] = useState(null); // { index: number, text: string }

    // Display overrides for sidebar during scoring / live preview
    const [sidebarScore, setSidebarScore] = useState(null);
    const [sidebarChips, setSidebarChips] = useState(null);
    const [sidebarMult, setSidebarMult] = useState(null);
    const [sidebarHandName, setSidebarHandName] = useState(null);
    const [sidebarHandLevel, setSidebarHandLevel] = useState(null);

    // Keep deck remaining count synced with gameData
    useEffect(() => {
        if (gameData) {
            gameData.deckRemaining = deck.length;
            gameData.handCards = cards;
            gameData.jokers = jokers;
            gameData.consumables = consumables;
        }
    }, [deck.length, cards, jokers, consumables, gameData]);

    // Live preview evaluated poker hand when cards are selected (when NOT currently scoring)
    const selectedCards = useMemo(() => {
        return cards.filter(c => selectedIds.includes(c.id));
    }, [cards, selectedIds]);

    const liveHandPreview = useMemo(() => {
        if (isScoring) return null;
        if (selectedCards.length === 0) return null;
        return evaluatePokerHand(selectedCards, gameData?.handLevels);
    }, [selectedCards, isScoring, gameData?.handLevels]);

    // Synchronize live preview with sidebar
    useEffect(() => {
        if (isScoring) return;

        if (liveHandPreview) {
            setSidebarHandName(liveHandPreview.handName);
            setSidebarHandLevel(liveHandPreview.level);
            setSidebarChips(liveHandPreview.chips);
            setSidebarMult(liveHandPreview.mult);
        } else {
            setSidebarHandName('');
            setSidebarHandLevel(1);
            setSidebarChips(0);
            setSidebarMult(0);
        }
    }, [liveHandPreview, isScoring]);

    // Joker & Consumable handlers
    const handleToggleJoker = (index) => {
        if (isScoring) return;
        setActiveSlot(prev => {
            if (prev?.type === 'joker' && prev.index === index) {
                return null;
            }
            return { type: 'joker', index };
        });
    };

    const handleToggleConsumable = (index) => {
        if (isScoring) return;
        setActiveSlot(prev => {
            if (prev?.type === 'consumable' && prev.index === index) {
                return null;
            }
            return { type: 'consumable', index };
        });
    };

    const handleSellJoker = (index) => {
        if (isScoring) return;
        const joker = jokers[index];
        const price = joker?.sellPrice || 2;
        if (gameData?.money !== undefined) {
            gameData.money += price;
        }
        setJokers(prev => prev.filter((_, i) => i !== index));
        setActiveSlot(null);
    };

    const handleSellConsumable = (index) => {
        if (isScoring) return;
        const consumable = consumables[index];
        const price = consumable?.sellPrice || 1;
        if (gameData?.money !== undefined) {
            gameData.money += price;
        }
        setConsumables(prev => prev.filter((_, i) => i !== index));
        setActiveSlot(null);
    };

    const handleUseConsumable = (index) => {
        if (isScoring) return;
        setConsumables(prev => prev.filter((_, i) => i !== index));
        setActiveSlot(null);
    };

    const handleToggleSelectCard = (id) => {
        if (isScoring) return;

        setSelectedIds((prev) => {
            if (prev.includes(id)) {
                return prev.filter(cardId => cardId !== id);
            }
            if (prev.length >= 5) {
                return prev;
            }
            return [...prev, id];
        });
    };

    const handleSortByRank = () => {
        if (isScoring) return;
        sfx.playCardSelect(1.2);
        setCards((prev) => {
            return [...prev].sort((a, b) => {
                const diffRank = (RANK_NUMERICAL[b.rank] || 0) - (RANK_NUMERICAL[a.rank] || 0);
                if (diffRank !== 0) return diffRank;
                return (SUIT_VALUES[b.suit] || 0) - (SUIT_VALUES[a.suit] || 0);
            });
        });
    };

    const handleSortBySuit = () => {
        if (isScoring) return;
        sfx.playCardSelect(1.2);
        setCards((prev) => {
            return [...prev].sort((a, b) => {
                const diffSuit = (SUIT_VALUES[b.suit] || 0) - (SUIT_VALUES[a.suit] || 0);
                if (diffSuit !== 0) return diffSuit;
                return (RANK_NUMERICAL[b.rank] || 0) - (RANK_NUMERICAL[a.rank] || 0);
            });
        });
    };

    // Helper for timing delays
    const getGameSpeed = () => {
        const saved = localStorage.getItem('balatro_game_speed');
        const speed = saved !== null ? parseInt(saved, 10) : 1;
        return isNaN(speed) || speed <= 0 ? 1 : speed;
    };

    const waitDelay = (ms) => {
        const speed = getGameSpeed();
        return new Promise(resolve => setTimeout(resolve, Math.max(60, ms / speed)));
    };

    // =========================================
    // PLAY HAND & SCORING ANIMATION SEQUENCE
    // =========================================
    const handlePlayHand = async () => {
        if (selectedIds.length === 0 || isScoring || (gameData?.hands !== undefined && gameData.hands <= 0)) {
            return;
        }

        // 1. Deduct Hand Count
        if (gameData?.hands !== undefined) {
            gameData.hands = Math.max(0, gameData.hands - 1);
        }

        // 2. Separate Played Cards and Remaining Hand Cards
        const currentPlayedCards = cards.filter(c => selectedIds.includes(c.id));
        const currentRemainingCards = cards.filter(c => !selectedIds.includes(c.id));

        // Start Scoring Mode
        setIsScoring(true);
        setPlayedCards(currentPlayedCards);
        setCards(currentRemainingCards);
        setSelectedIds([]);
        setActiveSlot(null);

        // Sound effect
        sfx.playPlayHand();

        // 3. Evaluate Poker Hand
        const evalResult = evaluatePokerHand(currentPlayedCards, gameData?.handLevels);
        if (!evalResult) {
            setIsScoring(false);
            return;
        }

        const { handName, level, chips: baseChips, mult: baseMult, scoringCardIds: activeScoringIds } = evalResult;
        setScoringCardIds(activeScoringIds);

        // Set Base Chips & Mult on Sidebar
        let currentChips = baseChips;
        let currentMult = baseMult;
        setSidebarHandName(handName);
        setSidebarHandLevel(level);
        setSidebarChips(currentChips);
        setSidebarMult(currentMult);

        await waitDelay(450);

        // 4. STEP-BY-STEP CARD SCORING
        for (let i = 0; i < currentPlayedCards.length; i++) {
            const card = currentPlayedCards[i];
            setScoringCardIndex(i);

            if (activeScoringIds.has(card.id)) {
                const chipValue = getCardChipValue(card.rank);
                currentChips += chipValue;
                setSidebarChips(currentChips);

                // Show floating score badge over this card
                setFloatingScores(prev => ({
                    ...prev,
                    [card.id]: {
                        key: `${card.id}-${Date.now()}`,
                        text: `+${chipValue}`,
                        type: 'chips'
                    }
                }));

                // Ascending sound per scoring card
                sfx.playCardScore(i);
            }

            await waitDelay(400);
        }

        // Clear card scoring highlight
        setScoringCardIndex(-1);
        await waitDelay(300);

        // 5. JOKERS TRIGGER PHASE
        const jokerEffects = evaluateJokerEffects(jokers, currentPlayedCards, currentRemainingCards);
        for (const effect of jokerEffects) {
            setActiveJokerTrigger({
                index: effect.index,
                text: effect.text
            });
            sfx.playJokerTrigger();

            if (effect.type === 'mult') {
                currentMult += effect.amount;
                setSidebarMult(currentMult);
            } else if (effect.type === 'chips') {
                currentChips += effect.amount;
                setSidebarChips(currentChips);
            }

            await waitDelay(550);
            setActiveJokerTrigger(null);
            await waitDelay(150);
        }

        // 6. MULTIPLICATION CALCULATION
        sfx.playMultiply();
        const handScore = currentChips * currentMult;
        await waitDelay(450);

        // 7. ADD TO ROUND SCORE
        sfx.playScoreSlam();
        const targetScore = gameData?.targetScore || gameData?.currentBlind?.score || 300;
        const currentRoundScore = gameData?.score || 0;
        const newRoundScore = currentRoundScore + handScore;

        if (gameData) {
            gameData.score = newRoundScore;

            // Update stats
            if (gameData.stats) {
                gameData.stats.cardsPlayed = (gameData.stats.cardsPlayed || 0) + currentPlayedCards.length;
                if (!gameData.stats.handsHistory) gameData.stats.handsHistory = {};
                gameData.stats.handsHistory[handName] = (gameData.stats.handsHistory[handName] || 0) + 1;

                if (handScore > (gameData.stats.bestHandScore || 0)) {
                    gameData.stats.bestHandScore = handScore;
                    gameData.stats.bestHandName = handName;
                }
            }
        }
        setSidebarScore(newRoundScore);
        await waitDelay(600);

        // 8. CHECK OUTCOME OR REFILL HAND
        if (newRoundScore >= targetScore) {
            // ROUND WON!
            await waitDelay(500);
            if (onWin) onWin();
            return;
        } else if (gameData?.hands !== undefined && gameData.hands <= 0) {
            // ROUND LOST!
            await waitDelay(500);
            if (onLose) onLose();
            return;
        }

        // 9. CLEANUP & DRAW REPLACEMENT CARDS
        const neededCards = Math.max(0, maxHandSize - currentRemainingCards.length);
        let currentDeck = [...deck];
        const drawnCards = [];

        for (let i = 0; i < neededCards && currentDeck.length > 0; i++) {
            drawnCards.push(currentDeck.shift());
        }

        setDeck(currentDeck);
        setCards([...currentRemainingCards, ...drawnCards]);
        setPlayedCards([]);
        setFloatingScores({});
        setScoringCardIds(new Set());
        setIsScoring(false);

        // Reset sidebar preview
        setSidebarHandName('');
        setSidebarHandLevel(1);
        setSidebarChips(0);
        setSidebarMult(0);

        sfx.playCardDeal();
    };

    // =========================================
    // DISCARD HAND
    // =========================================
    const handleDiscard = () => {
        if (selectedIds.length === 0 || isScoring || (gameData?.discards !== undefined && gameData.discards <= 0)) {
            return;
        }

        if (gameData?.discards !== undefined) {
            gameData.discards = Math.max(0, gameData.discards - 1);
        }

        if (gameData?.stats) {
            gameData.stats.cardsDiscarded = (gameData.stats.cardsDiscarded || 0) + selectedIds.length;
        }

        const remainingCards = cards.filter(c => !selectedIds.includes(c.id));
        const neededCards = Math.max(0, maxHandSize - remainingCards.length);

        let currentDeck = [...deck];
        const drawnCards = [];

        for (let i = 0; i < neededCards && currentDeck.length > 0; i++) {
            drawnCards.push(currentDeck.shift());
        }

        setDeck(currentDeck);
        setCards([...remainingCards, ...drawnCards]);
        setSelectedIds([]);
        setActiveSlot(null);

        sfx.playCardSelect(0.8);
    };

    // Construct sidebar data object with live previews
    const dynamicSidebarGameData = useMemo(() => {
        return {
            ...gameData,
            score: sidebarScore !== null ? sidebarScore : (gameData?.score || 0),
            currentHandName: sidebarHandName !== null ? sidebarHandName : (gameData?.currentHandName || ''),
            currentHandLevel: sidebarHandLevel !== null ? sidebarHandLevel : (gameData?.currentHandLevel || 1),
            currentHandChips: sidebarChips !== null ? sidebarChips : (gameData?.currentHandChips || 0),
            currentHandMult: sidebarMult !== null ? sidebarMult : (gameData?.currentHandMult || 0),
            deckRemaining: deck.length
        };
    }, [gameData, sidebarScore, sidebarHandName, sidebarHandLevel, sidebarChips, sidebarMult, deck.length]);

    return (
        <div className="game-board" onClick={() => setActiveSlot(null)}>
            <GameSidebar
                gameData={dynamicSidebarGameData}
                onOpenSettings={onOpenSettings}
                isBlindSelection={false}
            />

            <section className="game-main">
                {/* 1. TOP CONTAINERS: JOKERS (MAX 5) + CONSUMABLES (MAX 2) */}
                <div className="game-top-area" onClick={(e) => e.stopPropagation()}>
                    {/* JOKERS CONTAINER */}
                    <div className="jokers-container-wrapper">
                        <div className="jokers-slots-box">
                            {Array.from({ length: maxJokers }).map((_, index) => {
                                const joker = jokers[index];
                                const isSelected = activeSlot?.type === 'joker' && activeSlot.index === index;
                                const isTriggered = activeJokerTrigger?.index === index;
                                const triggeredText = isTriggered ? activeJokerTrigger.text : '';

                                return (
                                    <div
                                        key={index}
                                        className={`joker-slot ${joker ? 'occupied' : 'empty'}`}
                                    >
                                        {joker ? (
                                            <JokerCard
                                                id={joker.id}
                                                width={78}
                                                height={108}
                                                animated={true}
                                                isSelected={isSelected}
                                                isTriggered={isTriggered}
                                                triggeredText={triggeredText}
                                                onSelect={(e) => {
                                                    e.stopPropagation();
                                                    handleToggleJoker(index);
                                                }}
                                                onSell={() => handleSellJoker(index)}
                                                sellPrice={joker.sellPrice || 2}
                                            />
                                        ) : null}
                                    </div>
                                );
                            })}
                        </div>

                        <div className="slot-counter-text">
                            {jokers.length}/{maxJokers}
                        </div>
                    </div>

                    {/* CONSUMABLES CONTAINER */}
                    <div className="consumables-container-wrapper">
                        <div className="consumables-slots-box">
                            {Array.from({ length: maxConsumables }).map((_, index) => {
                                const consumable = consumables[index];
                                const isSelected = activeSlot?.type === 'consumable' && activeSlot.index === index;
                                return (
                                    <div
                                        key={index}
                                        className={`consumable-slot ${consumable ? 'occupied' : 'empty'}`}
                                    >
                                        {consumable ? (
                                            consumable.type === 'planet' ? (
                                                <PlanetCard
                                                    planet={consumable.id}
                                                    width={78}
                                                    height={108}
                                                    animated={true}
                                                    isSelected={isSelected}
                                                    onSelect={(e) => {
                                                        e.stopPropagation();
                                                        handleToggleConsumable(index);
                                                    }}
                                                    onSell={() => handleSellConsumable(index)}
                                                    onUse={() => handleUseConsumable(index)}
                                                    sellPrice={consumable.sellPrice || 1}
                                                />
                                            ) : (
                                                <TarotCard
                                                    tarot={consumable.id}
                                                    width={78}
                                                    height={108}
                                                    animated={true}
                                                    isSelected={isSelected}
                                                    onSelect={(e) => {
                                                        e.stopPropagation();
                                                        handleToggleConsumable(index);
                                                    }}
                                                    onSell={() => handleSellConsumable(index)}
                                                    onUse={() => handleUseConsumable(index)}
                                                    sellPrice={consumable.sellPrice || 1}
                                                />
                                            )
                                        ) : null}
                                    </div>
                                );
                            })}
                        </div>

                        <div className="slot-counter-text align-right">
                            {consumables.length}/{maxConsumables}
                        </div>
                    </div>
                </div>

                {/* 2. PLAY SECTION: CENTER (PLAYED CARDS + HAND + ACTIONS) & RIGHT (DECK) */}
                <div className={`game-play-section ${isScoring ? 'scoring-active' : ''}`}>
                    <div className="game-play-center">
                        {/* UPPER CENTER: PLAYED CARDS (DURING SCORING) */}
                        {isScoring && (
                            <div className="game-played-cards-area">
                                <PlayingArea
                                    playedCards={playedCards}
                                    scoringCardIndex={scoringCardIndex}
                                    scoringCardIds={scoringCardIds}
                                    floatingScores={floatingScores}
                                />
                            </div>
                        )}

                        {/* LOWER CENTER: PLAYER HAND (EITHER NORMAL INTERACTIVE OR LOWERED DURING SCORING) */}
                        <div className={`game-hand-area ${isScoring ? 'lowered-area' : ''}`}>
                            <PlayerHand
                                cards={cards}
                                setCards={setCards}
                                selectedIds={selectedIds}
                                onToggleSelect={handleToggleSelectCard}
                                maxSelected={5}
                                isScoring={isScoring}
                                maxHandSize={maxHandSize}
                            />
                        </div>

                        {/* BOTTOM ACTIONS: ONLY VISIBLE WHEN NOT SCORING */}
                        {!isScoring && (
                            <div className="game-actions-wrapper">
                                <div className="hand-cards-counter">
                                    {cards.length}/{maxHandSize}
                                </div>

                                <div className="game-actions">
                                    <button
                                        onClick={handlePlayHand}
                                        disabled={selectedIds.length === 0 || (gameData?.hands !== undefined && gameData.hands <= 0)}
                                        className={`action-btn play-button ${selectedIds.length > 0 && (gameData?.hands === undefined || gameData.hands > 0) ? 'active' : 'disabled'}`}
                                    >
                                        Play Hand
                                    </button>

                                    <div className="sort-hand-container">
                                        <span className="sort-label">Sort Hand</span>
                                        <div className="sort-buttons-row">
                                            <button
                                                className="sort-btn rank-btn"
                                                onClick={handleSortByRank}
                                                title="Sort by Rank"
                                            >
                                                Rank
                                            </button>
                                            <button
                                                className="sort-btn suit-btn"
                                                onClick={handleSortBySuit}
                                                title="Sort by Suit"
                                            >
                                                Suit
                                            </button>
                                        </div>
                                    </div>

                                    <button
                                        onClick={handleDiscard}
                                        disabled={selectedIds.length === 0 || (gameData?.discards !== undefined && gameData.discards <= 0)}
                                        className={`action-btn discard-button ${selectedIds.length > 0 && (gameData?.discards === undefined || gameData.discards > 0) ? 'active' : 'disabled'}`}
                                    >
                                        Discard
                                    </button>
                                </div>
                            </div>
                        )}
                    </div>

                    {/* DECK COUNTER AREA (FAR RIGHT COLUMN) */}
                    <div className="game-deck-column">
                        {/* DECK HOVER REMAINING CARDS BREAKDOWN */}
                        {isDeckHovered && (
                            <DeckHoverPreview
                                gameData={dynamicSidebarGameData}
                                handCards={cards}
                            />
                        )}

                        <div
                            className="game-deck-area"
                            onClick={() => !isScoring && setIsDeckModalOpen(true)}
                            onMouseEnter={() => setIsDeckHovered(true)}
                            onMouseLeave={() => setIsDeckHovered(false)}
                            title="Click to Peek Deck"
                        >
                            <div className="peek-deck-label">
                                <span>PEEK</span>
                                <span>DECK</span>
                                <div className="deck-key-hint">LT</div>
                            </div>

                            <div className="deck-card-stack">
                                <div className="deck-card-visual">
                                    <CardBack
                                        type="BackNormal"
                                        width={80}
                                        height={112}
                                    />
                                </div>
                            </div>

                            <div className="deck-count-text">
                                {deck.length}/52
                            </div>
                        </div>
                    </div>
                </div>

                {/* PEEK DECK MODAL */}
                <DeckViewModal
                    isOpen={isDeckModalOpen}
                    onClose={() => setIsDeckModalOpen(false)}
                    gameData={dynamicSidebarGameData}
                    handCards={cards}
                />
            </section>
        </div>
    );
}

export default GameBoard;
