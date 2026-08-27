import { useState, useEffect, useMemo, useRef } from 'react';
import { Reorder } from 'framer-motion';
import GameSidebar from '../GameSidebar/GameSidebar';
import BlindCard from './BlindCard';
import JokerCard from '../../JokerCard/JokerCard';
import TarotCard from '../../TarotCard/TarotCard';
import PlanetCard from '../../PlanetCard/PlanetCard';
import SpectralCard from '../../SpectralCard/SpectralCard';
import CardBack from '../../CardBack/CardBack';
import DeckViewModal from '../GameBoard/DeckViewModal';
import DeckHoverPreview from '../GameBoard/DeckHoverPreview';
import {
    mapBackendBlinds,
    mapBackendJokers,
    mapBackendConsumables
} from '../../../utils/cardMapper';
import {
    sellCard,
    useConsumable,
    reorderJokers,
    reorderConsumables
} from '../../../services/api';
import { sfx } from '../../../utils/sfx';
import './BlindSelection.css';

function BlindSelection({
    gameData,
    onSelectBlind,
    onOpenSettings,
    onSyncState,
    onShowToast
}) {
    const rawBlinds = gameData?.availableBlinds?.length ? gameData.availableBlinds : [];
    const blinds = rawBlinds.length > 0 ? mapBackendBlinds(rawBlinds) : [
        {
            id: 1,
            type: 'small',
            blind: 'SmallBlind',
            title: 'Small Blind',
            score: 300,
            reward: '$$$+',
            isDefeated: false
        },
        {
            id: 2,
            type: 'big',
            blind: 'BigBlind',
            title: 'Big Blind',
            score: 450,
            reward: '$$$$+',
            isDefeated: false
        },
        {
            id: 3,
            type: 'boss',
            blind: 'TheGoad',
            title: 'The Goad',
            score: 600,
            reward: '$$$$$+',
            isDefeated: false
        }
    ];

    // Find first undefeated blind index
    const activeIndex = blinds.findIndex(b => !b.isDefeated);

    const getStatus = (index, blind) => {
        if (blind.isDefeated) return 'defeated';
        if (index === (activeIndex !== -1 ? activeIndex : 0)) return 'active';
        return 'upcoming';
    };

    // Jokers and Consumables management
    const [localJokers, setLocalJokers] = useState(() => gameData?.jokers || []);
    const [localConsumables, setLocalConsumables] = useState(() => gameData?.consumables || []);
    const [activeSlot, setActiveSlot] = useState(null); // { type: 'joker' | 'consumable', index: number } | null

    const maxJokers = gameData?.maxJokers || 5;
    const maxConsumables = gameData?.maxConsumables || 2;

    const latestJokersRef = useRef(localJokers);
    const latestConsumablesRef = useRef(localConsumables);
    const isJokerDraggingRef = useRef(false);
    const isConsumableDraggingRef = useRef(false);

    // Deck Peek and Hover preview
    const [isDeckHovered, setIsDeckHovered] = useState(false);
    const [isDeckModalOpen, setIsDeckModalOpen] = useState(false);
    const deckRemaining = gameData?.deckRemaining ?? 52;
    const totalDeckCount = gameData?.totalDeckCount || gameData?.fullDeck?.length || 52;

    // Sync state when gameData updates
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

    // Handle Joker selection & actions
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
        isJokerDraggingRef.current = true;
        setLocalJokers(newJokers);
        latestJokersRef.current = newJokers;
        setActiveSlot(null);

        console.log(
            '%c[BlindSelection - Joker Arranging]%c ' + newJokers.map((j, idx) => `[Slot ${idx}] ${j.title || j.name || j.jokerKey}`).join('  ➔  '),
            'background: #7952b3; color: white; font-weight: bold; padding: 2px 6px; border-radius: 3px;',
            'color: #e599f7; font-weight: bold;'
        );
    };

    const handleJokerDragEnd = async () => {
        if (!isJokerDraggingRef.current) return;
        isJokerDraggingRef.current = false;
        const jokerIds = latestJokersRef.current.map(j => j.id);
        if (jokerIds.length <= 1) return;

        console.group('%c[BlindSelection - Joker Arrange Confirmed]', 'background: #2b393b; color: #00e5ff; font-weight: bold; padding: 4px 8px; border-radius: 4px;');
        console.table(latestJokersRef.current.map((j, idx) => ({
            Slot: idx,
            Name: j.title || j.name || j.jokerKey,
            Id: j.id
        })));
        console.log('%cSyncing new order to Backend:', 'color: #69db7c;', jokerIds);

        try {
            const state = await reorderJokers(jokerIds);
            console.log('%cBackend Response State (Jokers Synced):', 'color: #4dabf7; font-weight: bold;', (state?.jokers || []).map((j, idx) => `[Slot ${idx}] ${j.name || j.jokerId}`));
            console.groupEnd();
            if (onSyncState) onSyncState(state);
        } catch (err) {
            console.error('Failed to reorder jokers:', err);
            console.groupEnd();
        }
    };

    const handleConsumableReorder = (newConsumables) => {
        isConsumableDraggingRef.current = true;
        setLocalConsumables(newConsumables);
        latestConsumablesRef.current = newConsumables;
        setActiveSlot(null);

        console.log(
            '%c[BlindSelection - Consumable Arranging]%c ' + newConsumables.map((c, idx) => `[Slot ${idx}] ${c.title || c.name || c.id}`).join('  ➔  '),
            'background: #00897b; color: white; font-weight: bold; padding: 2px 6px; border-radius: 3px;',
            'color: #80cbc4; font-weight: bold;'
        );
    };

    const handleConsumableDragEnd = async () => {
        if (!isConsumableDraggingRef.current) return;
        isConsumableDraggingRef.current = false;
        const consumableIds = latestConsumablesRef.current.map(c => c.id);
        if (consumableIds.length <= 1) return;

        console.group('%c[BlindSelection - Consumable Arrange Confirmed]', 'background: #2b393b; color: #26a69a; font-weight: bold; padding: 4px 8px; border-radius: 4px;');
        console.table(latestConsumablesRef.current.map((c, idx) => ({
            Slot: idx,
            Name: c.title || c.name || c.id,
            Type: c.type,
            Id: c.id
        })));
        console.log('%cSyncing new order to Backend:', 'color: #69db7c;', consumableIds);

        try {
            const state = await reorderConsumables(consumableIds);
            console.log('%cBackend Response State (Consumables Synced):', 'color: #4dabf7; font-weight: bold;', (state?.consumables || []).map((c, idx) => `[Slot ${idx}] ${c.name || c.id}`));
            console.groupEnd();
            if (onSyncState) onSyncState(state);
        } catch (err) {
            console.error('Failed to reorder consumables:', err);
            console.groupEnd();
        }
    };

    const handleSellJoker = async (index) => {
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
        const consumable = localConsumables[index];
        if (!consumable) return;

        try {
            const state = await useConsumable(consumable.id, []);
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

    return (
        <div className="blind-selection" onClick={() => setActiveSlot(null)}>
            <GameSidebar
                gameData={gameData}
                onOpenSettings={onOpenSettings}
                isBlindSelection={true}
            />

            <section className="blind-main-area">
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

                {/* 2. CENTER SECTION: BLIND CARDS + RIGHT DECK */}
                <div className="blind-center-layout">
                    {/* BLIND CARDS CONTAINER */}
                    <div className="blind-cards-content">
                        <div className="blind-cards">
                            {blinds.map((blind, idx) => {
                                const status = getStatus(idx, blind);
                                return (
                                    <BlindCard
                                        key={blind.id || idx}
                                        type={blind.type}
                                        blind={blind.blind}
                                        title={blind.title}
                                        score={blind.score}
                                        reward={blind.reward}
                                        description={blind.description}
                                        status={status}
                                        onSelect={() => onSelectBlind(blind)}
                                    />
                                );
                            })}
                        </div>
                    </div>

                    {/* DECK COUNTER AREA (FAR RIGHT COLUMN) */}
                    <div className="game-deck-column blind-deck-column">
                        {isDeckHovered && (
                            <DeckHoverPreview
                                gameData={gameData}
                                handCards={gameData?.handCards || []}
                            />
                        )}

                        <div
                            className="game-deck-area"
                            onClick={() => setIsDeckModalOpen(true)}
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
                                {deckRemaining}/{totalDeckCount}
                            </div>
                        </div>
                    </div>
                </div>

                {/* PEEK DECK MODAL */}
                <DeckViewModal
                    isOpen={isDeckModalOpen}
                    onClose={() => setIsDeckModalOpen(false)}
                    gameData={gameData}
                    handCards={gameData?.handCards || []}
                />
            </section>
        </div>
    );
}

export default BlindSelection;
