import { useState, useMemo, useEffect } from 'react';
import PlayingCard from '../../PlayingCard/PlayingCard';
import './DeckViewModal.css';

const SUITS = ['Spades', 'Hearts', 'Clubs', 'Diamonds'];
const RANKS = ['A', 'K', 'Q', 'J', '10', '9', '8', '7', '6', '5', '4', '3', '2'];

// Generate full standard 52-card deck
function generateFullDeck() {
    const deck = [];
    SUITS.forEach(suit => {
        RANKS.forEach(rank => {
            deck.push({ suit, rank, id: `${suit}-${rank}` });
        });
    });
    return deck;
}

const FULL_DECK = generateFullDeck();

// Default hand cards if not provided (Spades A, Hearts K, Diamonds 7, Clubs Q, Hearts 3)
const DEFAULT_HAND = [
    { suit: 'Spades', rank: 'A' },
    { suit: 'Hearts', rank: 'K' },
    { suit: 'Diamonds', rank: '7' },
    { suit: 'Clubs', rank: 'Q' },
    { suit: 'Hearts', rank: '3' }
];

function DeckViewModal({
    isOpen,
    onClose,
    gameData,
    handCards = DEFAULT_HAND
}) {
    const [activeTab, setActiveTab] = useState('remaining'); // 'remaining' | 'full'

    // Close on Escape key or handle tab navigation
    useEffect(() => {
        if (!isOpen) return;

        const handleKeyDown = (e) => {
            if (e.key === 'Escape') {
                onClose();
            } else if (e.key === 'ArrowLeft' || e.key.toLowerCase() === 'q') {
                setActiveTab('remaining');
            } else if (e.key === 'ArrowRight' || e.key.toLowerCase() === 'e') {
                setActiveTab('full');
            }
        };

        window.addEventListener('keydown', handleKeyDown);
        return () => window.removeEventListener('keydown', handleKeyDown);
    }, [isOpen, onClose]);

    // Calculate remaining cards: all cards not currently in hand or played/discarded
    const remainingCardsSet = useMemo(() => {
        if (gameData?.remainingCardsList) {
            return new Set(gameData.remainingCardsList.map(c => `${c.suit}-${c.rank}`));
        }

        // If not explicitly provided in gameData, exclude current hand cards
        const handSet = new Set(handCards.map(c => `${c.suit}-${c.rank}`));
        const remSet = new Set();
        FULL_DECK.forEach(card => {
            const key = `${card.suit}-${card.rank}`;
            if (!handSet.has(key)) {
                remSet.add(key);
            }
        });
        return remSet;
    }, [gameData, handCards]);

    // Active deck subset according to selected tab
    const activeDeckCards = useMemo(() => {
        if (activeTab === 'full') {
            return FULL_DECK;
        }
        return FULL_DECK.filter(card => remainingCardsSet.has(`${card.suit}-${card.rank}`));
    }, [activeTab, remainingCardsSet]);

    // Calculate deck stats
    const stats = useMemo(() => {
        let aces = 0;
        let faces = 0;
        let numbers = 0;
        const suitsCount = { Spades: 0, Hearts: 0, Clubs: 0, Diamonds: 0 };
        const ranksCount = {};
        RANKS.forEach(r => { ranksCount[r] = 0; });

        activeDeckCards.forEach(card => {
            // Count suits
            if (suitsCount[card.suit] !== undefined) {
                suitsCount[card.suit]++;
            }

            // Count ranks
            if (ranksCount[card.rank] !== undefined) {
                ranksCount[card.rank]++;
            }

            // Count types
            if (card.rank === 'A') {
                aces++;
            } else if (['K', 'Q', 'J'].includes(card.rank)) {
                faces++;
            } else {
                numbers++;
            }
        });

        return {
            aces,
            faces,
            numbers,
            suitsCount,
            ranksCount,
            total: activeDeckCards.length
        };
    }, [activeDeckCards]);

    if (!isOpen) return null;

    return (
        <div className="deck-modal-backdrop" onClick={onClose}>
            <div className="deck-modal-container" onClick={(e) => e.stopPropagation()}>
                {/* TOP TABS */}
                <div className="deck-modal-header">
                    <div className="tab-key-hint">Lb</div>

                    <div className="deck-tabs-pill">
                        <button
                            className={`deck-tab-btn ${activeTab === 'remaining' ? 'active' : ''}`}
                            onClick={() => setActiveTab('remaining')}
                        >
                            {activeTab === 'remaining' && <div className="tab-arrow-pointer" />}
                            Remaining
                        </button>

                        <button
                            className={`deck-tab-btn ${activeTab === 'full' ? 'active' : ''}`}
                            onClick={() => setActiveTab('full')}
                        >
                            {activeTab === 'full' && <div className="tab-arrow-pointer" />}
                            Full Deck
                        </button>
                    </div>

                    <div className="tab-key-hint">Rb</div>

                    <button className="deck-modal-close-btn" onClick={onClose} title="Close">
                        ✕
                    </button>
                </div>

                {/* MAIN CONTENT (LEFT STATS + RANKS COLUMN + CARDS MATRIX) */}
                <div className="deck-modal-body">
                    {/* LEFT PANEL */}
                    <div className="deck-left-panel">
                        {/* DECK TYPE BOX */}
                        <div className="deck-type-card">
                            <div className="deck-type-title">Red Deck</div>
                            <div className="deck-type-perk">
                                <span className="perk-highlight">+1</span> discard every round
                            </div>
                        </div>

                        {/* BASE CARDS STATS */}
                        <div className="deck-base-cards-box">
                            <div className="base-cards-header">Base Cards</div>

                            {/* Card Category counts */}
                            <div className="base-cards-row categories">
                                <div className="base-stat-item">
                                    <div className="stat-icon-badge badge-ace">A</div>
                                    <span className="stat-number">{stats.aces}</span>
                                </div>
                                <div className="base-stat-item">
                                    <div className="stat-icon-badge badge-face">👑</div>
                                    <span className="stat-number">{stats.faces}</span>
                                </div>
                                <div className="base-stat-item">
                                    <div className="stat-icon-badge badge-number">#</div>
                                    <span className="stat-number">{stats.numbers}</span>
                                </div>
                            </div>

                            {/* Suits counts */}
                            <div className="base-cards-row suits">
                                <div className="base-stat-item">
                                    <div className="stat-icon-badge suit-spade">♠</div>
                                    <span className="stat-number">{stats.suitsCount.Spades}</span>
                                </div>
                                <div className="base-stat-item">
                                    <div className="stat-icon-badge suit-heart">♥</div>
                                    <span className="stat-number">{stats.suitsCount.Hearts}</span>
                                </div>
                            </div>

                            <div className="base-cards-row suits">
                                <div className="base-stat-item">
                                    <div className="stat-icon-badge suit-club">♣</div>
                                    <span className="stat-number">{stats.suitsCount.Clubs}</span>
                                </div>
                                <div className="base-stat-item">
                                    <div className="stat-icon-badge suit-diamond">♦</div>
                                    <span className="stat-number">{stats.suitsCount.Diamonds}</span>
                                </div>
                            </div>
                        </div>
                    </div>

                    {/* VERTICAL RANK COUNTS COLUMN */}
                    <div className="deck-ranks-column">
                        {RANKS.map(rank => (
                            <div key={rank} className="rank-count-pill">
                                <span className="rank-name">{rank}</span>
                                <span className="rank-val">{stats.ranksCount[rank]}</span>
                            </div>
                        ))}
                    </div>

                    {/* CARDS MATRIX (4 SUIT ROWS x 13 CARDS) */}
                    <div className="deck-cards-matrix">
                        {SUITS.map(suit => (
                            <div key={suit} className={`matrix-suit-row suit-${suit.toLowerCase()}`}>
                                {RANKS.map(rank => {
                                    const cardKey = `${suit}-${rank}`;
                                    const isCardInDeck = activeTab === 'full' || remainingCardsSet.has(cardKey);

                                    return (
                                        <div
                                            key={cardKey}
                                            className={`matrix-card-wrapper ${isCardInDeck ? 'in-deck' : 'out-of-deck'}`}
                                            title={`${rank} of ${suit}`}
                                        >
                                            <PlayingCard
                                                suit={suit}
                                                rank={rank}
                                                width={56}
                                                height={78}
                                            />
                                        </div>
                                    );
                                })}
                            </div>
                        ))}
                    </div>
                </div>
            </div>
        </div>
    );
}

export default DeckViewModal;
