import { useState, useEffect, useMemo, useRef } from 'react';
import GameSidebar from '../GameSidebar/GameSidebar';
import PlayerHand from './PlayerHand';
import PlayingArea from './PlayingArea';
import CardBack from '../../CardBack/CardBack';
import JokerCard from '../../JokerCard/JokerCard';
import TarotCard from '../../TarotCard/TarotCard';
import PlanetCard from '../../PlanetCard/PlanetCard';
import SpectralCard from '../../SpectralCard/SpectralCard';
import DeckViewModal from './DeckViewModal';
import DeckHoverPreview from './DeckHoverPreview';
import {
    evaluatePokerHand,
    getCardChipValue,
    evaluateJokerEffects,
    RANK_NUMERICAL
} from '../../../utils/pokerEvaluator';
import { mapBackendCards } from '../../../utils/cardMapper';
import { Reorder } from 'framer-motion';
import { playHand, discardCards, getScorePreview, useConsumable, sellCard, reorderJokers, reorderConsumables } from '../../../services/api';
import { sfx } from '../../../utils/sfx';
import './GameBoard.css';

const SUIT_VALUES = {
    'Spades': 4, 'Hearts': 3, 'Clubs': 2, 'Diamonds': 1
};

function GameBoard({
    gameData,
    onWin,
    onLose,
    onOpenSettings,
    onSyncState,
    onShowToast
}) {
    const [isDeckModalOpen, setIsDeckModalOpen] = useState(false);
    const [isDeckHovered, setIsDeckHovered] = useState(false);

    // Cards in hand (synced with gameData.handCards)
    const [cards, setCards] = useState(() => gameData?.handCards || []);
    const [selectedIds, setSelectedIds] = useState([]);

    // Jokers & Consumables
    const maxJokers = gameData?.maxJokers || 5;
    const [localJokers, setLocalJokers] = useState(() => gameData?.jokers || []);
    const latestJokersRef = useRef(localJokers);

    const maxConsumables = gameData?.maxConsumables || 2;
    const [localConsumables, setLocalConsumables] = useState(() => gameData?.consumables || []);
    const latestConsumablesRef = useRef(localConsumables);

    useEffect(() => {
        if (gameData?.jokers) {
            setLocalJokers(gameData.jokers);
            latestJokersRef.current = gameData.jokers;
        }
    }, [gameData?.jokers]);

    useEffect(() => {
        if (gameData?.consumables) {
            setLocalConsumables(gameData.consumables);
            latestConsumablesRef.current = gameData.consumables;
        }
    }, [gameData?.consumables]);

    const [activeSlot, setActiveSlot] = useState(null); // { type: 'joker' | 'consumable', index: number } | null
    const maxHandSize = gameData?.maxHandSize || 8;
    const deckRemaining = gameData?.deckRemaining ?? 52;

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

    // Synchronize cards from gameData when not scoring
    useEffect(() => {
        if (!isScoring && gameData?.handCards) {
            setCards(gameData.handCards);
        }
    }, [gameData?.handCards, isScoring]);

    // Live preview evaluated poker hand when cards are selected (when NOT currently scoring)
    const selectedCards = useMemo(() => {
        return cards.filter(c => selectedIds.includes(c.id));
    }, [cards, selectedIds]);

    const liveHandPreview = useMemo(() => {
        if (isScoring || selectedCards.length === 0) return null;
        return evaluatePokerHand(selectedCards, gameData?.handLevels);
    }, [selectedCards, isScoring, gameData?.handLevels]);

    // Query backend score-preview when card selection changes
    useEffect(() => {
        if (isScoring) return;

        if (selectedIds.length === 0) {
            setSidebarHandName('');
            setSidebarHandLevel(1);
            setSidebarChips(0);
            setSidebarMult(0);
            return;
        }

        let isCurrent = true;

        // Try backend score preview API
        getScorePreview(selectedIds)
            .then(preview => {
                if (!isCurrent || isScoring) return;
                setSidebarHandName(preview.handName);
                setSidebarHandLevel(preview.handLevel || 1);
                setSidebarChips(preview.baseChips ?? (preview.BaseChips ?? (liveHandPreview?.chips || 0)));
                setSidebarMult(preview.baseMult ?? (preview.BaseMult ?? (liveHandPreview?.mult || 0)));
            })
            .catch(() => {
                if (!isCurrent || isScoring) return;
                if (liveHandPreview) {
                    setSidebarHandName(liveHandPreview.handName);
                    setSidebarHandLevel(liveHandPreview.level);
                    setSidebarChips(liveHandPreview.chips);
                    setSidebarMult(liveHandPreview.mult);
                }
            });

        return () => {
            isCurrent = false;
        };
    }, [selectedIds, isScoring, liveHandPreview]);

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

    const isJokerDraggingRef = useRef(false);
    const isConsumableDraggingRef = useRef(false);

    const handleJokerReorder = (newJokers) => {
        if (isScoring) return;
        isJokerDraggingRef.current = true;
        setLocalJokers(newJokers);
        latestJokersRef.current = newJokers;
        setActiveSlot(null);
    };

    const handleJokerDragEnd = async () => {
        if (isScoring || !isJokerDraggingRef.current) return;
        isJokerDraggingRef.current = false;
        const jokerIds = latestJokersRef.current.map(j => j.id);
        if (jokerIds.length <= 1) return;

        try {
            const state = await reorderJokers(jokerIds);
            if (onSyncState) onSyncState(state);
        } catch (err) {
            console.error('Failed to reorder jokers:', err);
        }
    };

    const handleConsumableReorder = (newConsumables) => {
        if (isScoring) return;
        isConsumableDraggingRef.current = true;
        setLocalConsumables(newConsumables);
        latestConsumablesRef.current = newConsumables;
        setActiveSlot(null);
    };

    const handleConsumableDragEnd = async () => {
        if (isScoring || !isConsumableDraggingRef.current) return;
        isConsumableDraggingRef.current = false;
        const consumableIds = latestConsumablesRef.current.map(c => c.id);
        if (consumableIds.length <= 1) return;

        try {
            const state = await reorderConsumables(consumableIds);
            if (onSyncState) onSyncState(state);
        } catch (err) {
            console.error('Failed to reorder consumables:', err);
        }
    };

    const handleSellJoker = async (index) => {
        if (isScoring) return;
        const joker = localJokers[index];
        if (!joker) return;

        try {
            const state = await sellCard(joker.id);
            if (onSyncState) onSyncState(state);
            setActiveSlot(null);
            if (onShowToast) onShowToast(`Sold ${joker.title || 'Joker'}!`);
        } catch (err) {
            console.error('Failed to sell joker:', err);
            if (onShowToast) onShowToast(err.message);
        }
    };

    const handleSellConsumable = async (index) => {
        if (isScoring) return;
        const consumable = localConsumables[index];
        if (!consumable) return;

        try {
            const state = await sellCard(consumable.id);
            if (onSyncState) onSyncState(state);
            setActiveSlot(null);
            if (onShowToast) onShowToast(`Sold ${consumable.title || 'Consumable'}!`);
        } catch (err) {
            console.error('Failed to sell consumable:', err);
            if (onShowToast) onShowToast(err.message);
        }
    };

    const handleUseConsumable = async (index) => {
        if (isScoring) return;
        const consumable = localConsumables[index];
        if (!consumable) return;

        try {
            // Selected hand cards as targets if any
            const targetIds = selectedIds.slice(0, 3);
            const state = await useConsumable(consumable.id, targetIds);
            if (onSyncState) onSyncState(state);
            setActiveSlot(null);

            if (consumable.type === 'planet') {
                sfx.playLevelUp();
            }
            const msg = state?.lastMessage || (consumable.type === 'planet' ? `Level Up! Upgraded ${consumable.title}!` : `Used ${consumable.title || 'Consumable'}!`);
            if (onShowToast) onShowToast(msg);
        } catch (err) {
            console.error('Failed to use consumable:', err);
            if (onShowToast) onShowToast(err.message);
        }
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
        return new Promise(resolve => setTimeout(resolve, Math.max(50, ms / speed)));
    };

    // =========================================
    // PLAY HAND & SCORING ANIMATION SEQUENCE
    // =========================================
    const handlePlayHand = async () => {
        if (selectedIds.length === 0 || isScoring || (gameData?.hands !== undefined && gameData.hands <= 0)) {
            return;
        }

        const currentPlayedCards = cards.filter(c => selectedIds.includes(c.id));
        const currentRemainingCards = cards.filter(c => !selectedIds.includes(c.id));

        setIsScoring(true);
        setPlayedCards(currentPlayedCards);
        setCards(currentRemainingCards);
        setSelectedIds([]);
        setActiveSlot(null);

        sfx.playPlayHand();

        try {
            // Panggil API backend untuk play hand
            const state = await playHand(currentPlayedCards.map(c => c.id));
            const scoreResult = state.lastScoreResult;

            if (scoreResult) {
                const {
                    handName,
                    handLevel,
                    baseChips,
                    baseMult,
                    totalChips,
                    totalMult,
                    finalScore,
                    scoringCards: backendScoringCards,
                    jokerTriggerMessages
                } = scoreResult;

                const activeScoringIds = new Set((backendScoringCards || []).map(c => c.id));
                setScoringCardIds(activeScoringIds);

                let currentChips = baseChips;
                let currentMult = baseMult;
                setSidebarHandName(handName);
                setSidebarHandLevel(handLevel || 1);
                setSidebarChips(currentChips);
                setSidebarMult(currentMult);

                await waitDelay(450);

                // 4. STEP-BY-STEP CARD SCORING
                for (let i = 0; i < currentPlayedCards.length; i++) {
                    const card = currentPlayedCards[i];
                    setScoringCardIndex(i);

                    const isCardScoring = activeScoringIds.has(card.id) || activeScoringIds.size === 0;
                    if (isCardScoring) {
                        const chipValue = card.baseChips || getCardChipValue(card.rank);
                        currentChips += chipValue;
                        setSidebarChips(currentChips);

                        setFloatingScores(prev => ({
                            ...prev,
                            [card.id]: {
                                key: `${card.id}-${Date.now()}`,
                                text: `+${chipValue}`,
                                type: 'chips'
                            }
                        }));

                        sfx.playCardScore(i);
                    }

                    await waitDelay(400);
                }

                setScoringCardIndex(-1);
                await waitDelay(300);

                // 5. JOKERS TRIGGER PHASE
                if (jokerTriggerMessages && jokerTriggerMessages.length > 0) {
                    for (let i = 0; i < jokerTriggerMessages.length; i++) {
                        setActiveJokerTrigger({
                            index: i % (localJokers.length || 1),
                            text: jokerTriggerMessages[i]
                        });
                        sfx.playJokerTrigger();
                        await waitDelay(500);
                        setActiveJokerTrigger(null);
                        await waitDelay(150);
                    }
                } else {
                    const jokerEffects = evaluateJokerEffects(localJokers, currentPlayedCards, currentRemainingCards);
                    for (const effect of jokerEffects) {
                        setActiveJokerTrigger({
                            index: effect.index,
                            text: effect.text
                        });
                        sfx.playJokerTrigger();

                        if (effect.type === 'mult') {
                            currentMult += effect.amount;
                        } else if (effect.type === 'chips') {
                            currentChips += effect.amount;
                        }

                        await waitDelay(500);
                        setActiveJokerTrigger(null);
                        await waitDelay(150);
                    }
                }

                setSidebarChips(totalChips);
                setSidebarMult(totalMult);

                // 6. MULTIPLICATION
                sfx.playMultiply();
                await waitDelay(450);

                // 7. ADD TO ROUND SCORE
                sfx.playScoreSlam();
                setSidebarScore(state.currentScore);
                await waitDelay(600);
            }

            // Sync full state to Gameplay
            if (onSyncState) {
                onSyncState(state);
            }

            const phase = state.phaseName || state.phase;
            if (phase === 'InShop' || state.currentScore >= state.targetScore) {
                await waitDelay(500);
                if (onWin) onWin();
                return;
            } else if (phase === 'GameOver') {
                await waitDelay(500);
                if (onLose) onLose();
                return;
            } else if (phase === 'Victory' || phase === 'Won') {
                await waitDelay(500);
                if (onWin) onWin();
                return;
            }

            // Refill hand from backend
            const mappedCards = mapBackendCards(state.hand);
            setCards(mappedCards);
            setPlayedCards([]);
            setFloatingScores({});
            setScoringCardIds(new Set());
            setIsScoring(false);

            setSidebarHandName('');
            setSidebarHandLevel(1);
            setSidebarChips(0);
            setSidebarMult(0);

            sfx.playCardDeal();

        } catch (err) {
            console.error('Play hand error:', err);
            setIsScoring(false);
            setCards([...currentPlayedCards, ...currentRemainingCards]);
            setPlayedCards([]);
            if (onShowToast) onShowToast(err.message);
        }
    };

    // =========================================
    // DISCARD HAND
    // =========================================
    const handleDiscard = async () => {
        if (selectedIds.length === 0 || isScoring || (gameData?.discards !== undefined && gameData.discards <= 0)) {
            return;
        }

        try {
            const state = await discardCards(selectedIds);
            if (onSyncState) onSyncState(state);

            const mappedCards = mapBackendCards(state.hand);
            setCards(mappedCards);
            setSelectedIds([]);
            setActiveSlot(null);

            sfx.playCardSelect(0.8);
        } catch (err) {
            console.error('Discard error:', err);
            if (onShowToast) onShowToast(err.message);
        }
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
            deckRemaining
        };
    }, [gameData, sidebarScore, sidebarHandName, sidebarHandLevel, sidebarChips, sidebarMult, deckRemaining]);

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
                        <Reorder.Group
                            axis="x"
                            values={localJokers}
                            onReorder={handleJokerReorder}
                            className="jokers-slots-box"
                            as="div"
                        >
                            {localJokers.map((joker, index) => {
                                const isSelected = activeSlot?.type === 'joker' && activeSlot.index === index;
                                const isTriggered = activeJokerTrigger?.index === index;
                                const triggeredText = isTriggered ? activeJokerTrigger.text : '';

                                return (
                                    <Reorder.Item
                                        key={joker.id || index}
                                        value={joker}
                                        as="div"
                                        className="joker-slot occupied"
                                        onDragEnd={handleJokerDragEnd}
                                        whileDrag={{
                                            scale: 1.12,
                                            zIndex: 100,
                                            cursor: 'grabbing',
                                            filter: 'drop-shadow(0 10px 20px rgba(0,0,0,0.6))'
                                        }}
                                    >
                                        <JokerCard
                                            id={joker.id}
                                            spriteId={joker.spriteId}
                                            title={joker.title}
                                            description={joker.description}
                                            rarity={joker.rarity}
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
                                    </Reorder.Item>
                                );
                            })}

                            {Array.from({ length: Math.max(0, maxJokers - localJokers.length) }).map((_, i) => (
                                <div
                                    key={`empty-joker-${i}`}
                                    className="joker-slot empty"
                                />
                            ))}
                        </Reorder.Group>

                        <div className="slot-counter-text">
                            {localJokers.length}/{maxJokers}
                        </div>
                    </div>

                    {/* CONSUMABLES CONTAINER */}
                    <div className="consumables-container-wrapper">
                        <Reorder.Group
                            axis="x"
                            values={localConsumables}
                            onReorder={handleConsumableReorder}
                            className="consumables-slots-box"
                            as="div"
                        >
                            {localConsumables.map((consumable, index) => {
                                const isSelected = activeSlot?.type === 'consumable' && activeSlot.index === index;
                                return (
                                    <Reorder.Item
                                        key={consumable.id || index}
                                        value={consumable}
                                        as="div"
                                        className="consumable-slot occupied"
                                        onDragEnd={handleConsumableDragEnd}
                                        whileDrag={{
                                            scale: 1.12,
                                            zIndex: 100,
                                            cursor: 'grabbing',
                                            filter: 'drop-shadow(0 10px 20px rgba(0,0,0,0.6))'
                                        }}
                                    >
                                        {consumable.type === 'planet' ? (
                                            <PlanetCard
                                                id={consumable.id}
                                                planet={consumable.id}
                                                spriteId={consumable.spriteId}
                                                name={consumable.name}
                                                title={consumable.title}
                                                description={consumable.description}
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
                                        ) : consumable.type === 'spectral' ? (
                                            <SpectralCard
                                                id={consumable.id}
                                                spectral={consumable.id}
                                                spriteId={consumable.spriteId}
                                                name={consumable.name}
                                                title={consumable.title}
                                                description={consumable.description}
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
                                                id={consumable.id}
                                                tarot={consumable.id}
                                                spriteId={consumable.spriteId}
                                                name={consumable.name}
                                                title={consumable.title}
                                                description={consumable.description}
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
                                        )}
                                    </Reorder.Item>
                                );
                            })}

                            {Array.from({ length: Math.max(0, maxConsumables - localConsumables.length) }).map((_, i) => (
                                <div
                                    key={`empty-consumable-${i}`}
                                    className="consumable-slot empty"
                                />
                            ))}
                        </Reorder.Group>

                        <div className="slot-counter-text align-right">
                            {localConsumables.length}/{maxConsumables}
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

                        {/* LOWER CENTER: PLAYER HAND */}
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
                                {deckRemaining}/52
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
