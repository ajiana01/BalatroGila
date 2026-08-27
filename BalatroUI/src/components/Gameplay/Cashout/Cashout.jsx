import { useState, useEffect, useRef } from 'react';
import { Reorder } from 'framer-motion';
import GameSidebar from '../GameSidebar/GameSidebar';
import JokerCard from '../../JokerCard/JokerCard';
import TarotCard from '../../TarotCard/TarotCard';
import PlanetCard from '../../PlanetCard/PlanetCard';
import SpectralCard from '../../SpectralCard/SpectralCard';
import Blind from '../../Blind/Blind';
import CardBack from '../../CardBack/CardBack';
import DeckViewModal from '../GameBoard/DeckViewModal';
import { sellCard, useConsumable, reorderJokers, reorderConsumables } from '../../../services/api';
import './Cashout.css';

function Cashout({
    gameData,
    onContinue,
    onOpenSettings,
    onSyncState
}) {
    const [isDeckModalOpen, setIsDeckModalOpen] = useState(false);

    // Jokers & Consumables inventory
    const maxJokers = gameData?.maxJokers || 5;
    const [localJokers, setLocalJokers] = useState(() => gameData?.jokers || []);
    const latestJokersRef = useRef(localJokers);

    const maxConsumables = gameData?.maxConsumables || 2;
    const [localConsumables, setLocalConsumables] = useState(() => gameData?.consumables || []);
    const latestConsumablesRef = useRef(localConsumables);

    const [activeSlot, setActiveSlot] = useState(null);

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

    const handleToggleJoker = (index) => {
        setActiveSlot(prev => {
            if (prev?.type === 'joker' && prev.index === index) {
                return null;
            }
            return { type: 'joker', index };
        });
    };

    const handleToggleConsumable = (index) => {
        setActiveSlot(prev => {
            if (prev?.type === 'consumable' && prev.index === index) {
                return null;
            }
            return { type: 'consumable', index };
        });
    };

    const handleJokerReorder = (newJokers) => {
        setLocalJokers(newJokers);
        latestJokersRef.current = newJokers;
        setActiveSlot(null);
    };

    const handleJokerDragEnd = async () => {
        try {
            const jokerIds = latestJokersRef.current.map(j => j.id);
            const state = await reorderJokers(jokerIds);
            if (onSyncState) onSyncState(state);
        } catch (err) {
            console.error('Failed to reorder jokers in cashout:', err);
        }
    };

    const handleConsumableReorder = (newConsumables) => {
        setLocalConsumables(newConsumables);
        latestConsumablesRef.current = newConsumables;
        setActiveSlot(null);
    };

    const handleConsumableDragEnd = async () => {
        try {
            const consumableIds = latestConsumablesRef.current.map(c => c.id);
            const state = await reorderConsumables(consumableIds);
            if (onSyncState) onSyncState(state);
        } catch (err) {
            console.error('Failed to reorder consumables in cashout:', err);
        }
    };

    const handleSellJoker = async (index) => {
        const joker = localJokers[index];
        if (!joker) return;

        try {
            const state = await sellCard(joker.id);
            if (onSyncState) onSyncState(state);
            setActiveSlot(null);
        } catch (err) {
            console.error('Failed to sell joker in cashout:', err);
        }
    };

    const handleSellConsumable = async (index) => {
        const consumable = localConsumables[index];
        if (!consumable) return;

        try {
            const state = await sellCard(consumable.id);
            if (onSyncState) onSyncState(state);
            setActiveSlot(null);
        } catch (err) {
            console.error('Failed to sell consumable in cashout:', err);
        }
    };

    const handleUseConsumable = async (index) => {
        const consumable = localConsumables[index];
        if (!consumable) return;

        try {
            const state = await useConsumable(consumable.id, []);
            if (onSyncState) onSyncState(state);
            setActiveSlot(null);
        } catch (err) {
            console.error('Failed to use consumable in cashout:', err);
        }
    };

    // Calculate blind key and rewards
    const blindKey = gameData?.currentBlind?.blind || (
        gameData?.currentBlind?.type === 'big' ? 'BigBlind' :
        gameData?.currentBlind?.type === 'boss' ? 'TheGoad' : 'SmallBlind'
    );

    const blindScoreTarget = (gameData?.targetScore || 2800).toLocaleString();

    // Reward breakdown
    const blindRewardDollars = gameData?.currentBlind?.type === 'big' ? 4 :
        gameData?.currentBlind?.type === 'boss' ? 5 : 3;
    const blindRewardSymbol = '$'.repeat(blindRewardDollars);

    const remainingHands = gameData?.hands ?? 2;
    const remainingHandsDollars = remainingHands * 1;
    const remainingHandsSymbol = '$'.repeat(Math.max(1, remainingHandsDollars));

    const interestDollars = Math.min(5, Math.floor((gameData?.money || 5) / 5));
    const interestSymbol = '$'.repeat(Math.max(1, interestDollars));

    const totalCashout = blindRewardDollars + remainingHandsDollars + interestDollars;

    return (
        <div className="cashout-screen" onClick={() => setActiveSlot(null)}>
            {/* LEFT GAME SIDEBAR */}
            <GameSidebar
                gameData={gameData}
                onOpenSettings={onOpenSettings}
                isBlindSelection={false}
                isCashout={true}
            />

            {/* MAIN CASHOUT AREA */}
            <section className="cashout-main">
                {/* 1. TOP CONTAINERS: JOKERS + CONSUMABLES */}
                <div className="cashout-top-area" onClick={(e) => e.stopPropagation()}>
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
                                                planet={consumable.id}
                                                spriteId={consumable.spriteId}
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
                                                spectral={consumable.spriteId || consumable.id}
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
                                                tarot={consumable.id}
                                                spriteId={consumable.spriteId}
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

                {/* 2. CENTER: CASHOUT DIALOGUE BOX */}
                <div className="cashout-center-container">
                    <div className="cashout-box">
                        {/* CASHOUT BUTTON HEADER */}
                        <button
                            type="button"
                            className="cashout-btn-header"
                            onClick={() => onContinue(totalCashout)}
                        >
                            Cash Out: ${totalCashout}
                        </button>

                        {/* ROW 1: BLIND SCORE & REWARD */}
                        <div className="cashout-row blind-row">
                            <div className="cashout-blind-icon">
                                <Blind
                                    blind={blindKey}
                                    width={48}
                                    height={48}
                                    animated={true}
                                />
                            </div>

                            <div className="cashout-row-center">
                                <span className="row-sub-label">Score at least</span>
                                <div className="row-target-score">
                                    <span className="chip-token">✱</span> {blindScoreTarget}
                                </div>
                            </div>

                            <div className="cashout-row-reward">
                                {blindRewardSymbol}
                            </div>
                        </div>

                        <div className="cashout-divider" />

                        {/* ROW 2: REMAINING HANDS */}
                        <div className="cashout-row item-row">
                            <div className="row-stat-num hands-num">
                                {remainingHands}
                            </div>

                            <div className="cashout-row-text">
                                Remaining Hands ($1 each)
                            </div>

                            <div className="cashout-row-reward">
                                {remainingHandsSymbol}
                            </div>
                        </div>

                        {/* ROW 3: INTEREST */}
                        <div className="cashout-row item-row">
                            <div className="row-stat-num interest-num">
                                {interestDollars}
                            </div>

                            <div className="cashout-row-text">
                                1 interest per $5 (5 max)
                            </div>

                            <div className="cashout-row-reward">
                                {interestSymbol}
                            </div>
                        </div>
                    </div>
                </div>

                {/* 3. DECK COUNTER (BOTTOM RIGHT) */}
                <div
                    className="game-deck-area"
                    onClick={() => setIsDeckModalOpen(true)}
                    title="Click to Peek Deck"
                >
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
                        {gameData?.deckRemaining ?? 52}/52
                    </div>
                </div>

                {/* PEEK DECK MODAL */}
                <DeckViewModal
                    isOpen={isDeckModalOpen}
                    onClose={() => setIsDeckModalOpen(false)}
                    gameData={gameData}
                />
            </section>
        </div>
    );
}

export default Cashout;