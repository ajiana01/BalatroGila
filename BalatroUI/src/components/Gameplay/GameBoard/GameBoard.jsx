import { useState } from 'react';
import GameSidebar from '../GameSidebar/GameSidebar';
import PlayerHand from './PlayerHand';
import CardBack from '../../CardBack/CardBack';
import JokerCard from '../../JokerCard/JokerCard';
import TarotCard from '../../TarotCard/TarotCard';
import PlanetCard from '../../PlanetCard/PlanetCard';
import DeckViewModal from './DeckViewModal';
import './GameBoard.css';

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

const RANK_VALUES = {
    'A': 14, 'K': 13, 'Q': 12, 'J': 11, '10': 10,
    '9': 9, '8': 8, '7': 7, '6': 6, '5': 5, '4': 4, '3': 3, '2': 2
};

const SUIT_VALUES = {
    'Spades': 4, 'Hearts': 3, 'Clubs': 2, 'Diamonds': 1
};

function GameBoard({
    gameData,
    onWin,
    onLose,
    onOpenSettings
}) {
    const [isDeckModalOpen, setIsDeckModalOpen] = useState(false);
    const [cards, setCards] = useState(gameData?.handCards || defaultHandCards);
    const [selectedIds, setSelectedIds] = useState([]);

    const maxJokers = gameData?.maxJokers || 5;
    const [jokers, setJokers] = useState(gameData?.jokers || defaultJokers);

    const maxConsumables = gameData?.maxConsumables || 2;
    const [consumables, setConsumables] = useState(gameData?.consumables || defaultConsumables);

    const [activeSlot, setActiveSlot] = useState(null); // { type: 'joker' | 'consumable', index: number } | null

    const maxHandSize = gameData?.maxHandSize || 8;

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

    const handleSellJoker = (index) => {
        const joker = jokers[index];
        const price = joker?.sellPrice || 2;
        if (gameData?.money !== undefined) {
            gameData.money += price;
        }
        setJokers(prev => prev.filter((_, i) => i !== index));
        setActiveSlot(null);
    };

    const handleSellConsumable = (index) => {
        const consumable = consumables[index];
        const price = consumable?.sellPrice || 1;
        if (gameData?.money !== undefined) {
            gameData.money += price;
        }
        setConsumables(prev => prev.filter((_, i) => i !== index));
        setActiveSlot(null);
    };

    const handleUseConsumable = (index) => {
        setConsumables(prev => prev.filter((_, i) => i !== index));
        setActiveSlot(null);
    };

    const handleToggleSelectCard = (id) => {
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
        setCards((prev) => {
            return [...prev].sort((a, b) => {
                const diffRank = (RANK_VALUES[b.rank] || 0) - (RANK_VALUES[a.rank] || 0);
                if (diffRank !== 0) return diffRank;
                return (SUIT_VALUES[b.suit] || 0) - (SUIT_VALUES[a.suit] || 0);
            });
        });
    };

    const handleSortBySuit = () => {
        setCards((prev) => {
            return [...prev].sort((a, b) => {
                const diffSuit = (SUIT_VALUES[b.suit] || 0) - (SUIT_VALUES[a.suit] || 0);
                if (diffSuit !== 0) return diffSuit;
                return (RANK_VALUES[b.rank] || 0) - (RANK_VALUES[a.rank] || 0);
            });
        });
    };

    const handlePlayHand = () => {
        if (selectedIds.length === 0) return;
        if (onWin) {
            onWin();
        }
    };

    const handleDiscard = () => {
        if (selectedIds.length === 0) return;
        setCards(prev => prev.filter(c => !selectedIds.includes(c.id)));
        setSelectedIds([]);
    };

    return (
        <div className="game-board" onClick={() => setActiveSlot(null)}>
            <GameSidebar
                gameData={gameData}
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

                {/* 2. MIDDLE AREA: PLAYER HAND */}
                <div className="game-hand-area">
                    <PlayerHand
                        cards={cards}
                        setCards={setCards}
                        selectedIds={selectedIds}
                        onToggleSelect={handleToggleSelectCard}
                        maxSelected={5}
                    />
                </div>

                {/* 3. BOTTOM ACTIONS: PLAY HAND | SORT (RANK / SUIT) | DISCARD */}
                <div className="game-actions-wrapper">
                    <div className="hand-cards-counter">
                        {cards.length}/{maxHandSize}
                    </div>

                    <div className="game-actions">
                        <button
                            onClick={handlePlayHand}
                            disabled={selectedIds.length === 0}
                            className={`action-btn play-button ${selectedIds.length > 0 ? 'active' : 'disabled'}`}
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
                            disabled={selectedIds.length === 0}
                            className={`action-btn discard-button ${selectedIds.length > 0 ? 'active' : 'disabled'}`}
                        >
                            Discard
                        </button>
                    </div>
                </div>

                {/* 4. DECK COUNTER AREA (BOTTOM RIGHT) */}
                <div
                    className="game-deck-area"
                    onClick={() => setIsDeckModalOpen(true)}
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
                        {gameData.deckRemaining || 52}/52
                    </div>
                </div>

                {/* PEEK DECK MODAL */}
                <DeckViewModal
                    isOpen={isDeckModalOpen}
                    onClose={() => setIsDeckModalOpen(false)}
                    gameData={gameData}
                    handCards={cards}
                />
            </section>
        </div>
    );
}

export default GameBoard;

