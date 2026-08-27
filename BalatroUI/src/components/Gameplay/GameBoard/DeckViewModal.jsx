import { useState, useMemo, useEffect } from 'react';
import PlayingCard from '../../PlayingCard/PlayingCard';
import './DeckViewModal.css';

const SUITS = ['Spades', 'Hearts', 'Clubs', 'Diamonds'];
const RANKS = ['A', 'K', 'Q', 'J', '10', '9', '8', '7', '6', '5', '4', '3', '2'];
const RANK_ORDER = {
    'A': 14, 'K': 13, 'Q': 12, 'J': 11, '10': 10,
    '9': 9, '8': 8, '7': 7, '6': 6, '5': 5, '4': 4, '3': 3, '2': 2
};

// Generate standard 52-card deck fallback
function generateFullDeck() {
    const deck = [];
    SUITS.forEach(suit => {
        RANKS.forEach(rank => {
            deck.push({
                id: `default-${suit}-${rank}`,
                suit,
                rank,
                enhancement: 'None',
                edition: 'Base',
                isDebuffed: false
            });
        });
    });
    return deck;
}

const DEFAULT_FULL_DECK = generateFullDeck();

function DeckViewModal({
    isOpen,
    onClose,
    gameData,
    handCards = []
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

    // Retrieve active full deck cards
    const fullDeckCards = useMemo(() => {
        if (gameData?.fullDeck && gameData.fullDeck.length > 0) {
            return gameData.fullDeck;
        }
        if (gameData?.deckCards && gameData.deckCards.length > 0) {
            return gameData.deckCards;
        }
        return DEFAULT_FULL_DECK;
    }, [gameData?.fullDeck, gameData?.deckCards]);

    // Calculate remaining deck cards
    const remainingDeckCards = useMemo(() => {
        if (gameData?.remainingCards && gameData.remainingCards.length > 0) {
            return gameData.remainingCards;
        }

        // If not directly supplied by backend, exclude current hand cards
        const usedHandKeys = new Map();
        handCards.forEach(c => {
            const k = `${c.suit}-${c.rank}`;
            usedHandKeys.set(k, (usedHandKeys.get(k) || 0) + 1);
        });

        const rem = [];
        const remKeysCount = new Map();

        fullDeckCards.forEach(card => {
            const k = `${card.suit}-${card.rank}`;
            const handCount = usedHandKeys.get(k) || 0;
            const alreadyExcluded = remKeysCount.get(k) || 0;

            if (alreadyExcluded < handCount) {
                remKeysCount.set(k, alreadyExcluded + 1);
            } else {
                rem.push(card);
            }
        });

        return rem;
    }, [gameData?.remainingCards, fullDeckCards, handCards]);

    // Active deck cards based on tab
    const activeDeckCards = useMemo(() => {
        return activeTab === 'full' ? fullDeckCards : remainingDeckCards;
    }, [activeTab, fullDeckCards, remainingDeckCards]);

    // Calculate deck stats
    const stats = useMemo(() => {
        let aces = 0;
        let faces = 0;
        let numbers = 0;
        const suitsCount = { Spades: 0, Hearts: 0, Clubs: 0, Diamonds: 0, Stone: 0 };
        const ranksCount = {};
        RANKS.forEach(r => { ranksCount[r] = 0; });

        activeDeckCards.forEach(card => {
            const isStone = card.enhancement === 'StoneCards' || card.enhancement === 'Stone';
            if (isStone) {
                suitsCount.Stone = (suitsCount.Stone || 0) + 1;
                numbers++;
                return;
            }

            // Count suits
            if (suitsCount[card.suit] !== undefined) {
                suitsCount[card.suit]++;
            }

            // Count ranks
            if (ranksCount[card.rank] !== undefined) {
                ranksCount[card.rank]++;
            }

            // Count categories
            if (card.rank === 'A') {
                aces++;
            } else if (['K', 'Q', 'J'].includes(card.rank)) {
                faces++;
            } else if (card.rank) {
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

    // Comprehensive console logging for debugging and verification
    useEffect(() => {
        if (!isOpen) return;

        console.group('%c[Balatro Deck View Modal Debug]', 'background: #2b393b; color: #fe4343; font-weight: bold; padding: 4px 8px; border-radius: 4px;');
        console.log('%cTab Aktif:', 'color: #00e5ff; font-weight: bold;', activeTab);
        console.log('%cTotal Kartu di Full Deck:', 'color: #ffd43b; font-weight: bold;', fullDeckCards.length);
        console.log('%cTotal Kartu di Remaining Deck:', 'color: #69db7c; font-weight: bold;', remainingDeckCards.length);
        console.log('%cTotal Kartu yang Ditampilkan Saat Ini:', 'color: #ff922b; font-weight: bold;', activeDeckCards.length);
        console.log('%cKartu Tangan (Hand Cards) yang Dikecualikan:', 'color: #adb5bd;', handCards.map(c => `${c.rank} of ${c.suit}`));
        console.log('%cStatistik Deck:', 'color: #e599f7;', stats);
        console.log('%cDaftar Lengkap Kartu yang Ditampilkan:', 'color: #ffffff;', activeDeckCards.map((c, i) => ({
            index: i + 1,
            id: c.id,
            card: `${c.rank} of ${c.suit}`,
            enhancement: c.enhancement || 'None',
            edition: c.edition || 'Base',
            seal: c.seal || 'None',
            isDebuffed: Boolean(c.isDebuffed)
        })));
        console.groupEnd();
    }, [isOpen, activeTab, fullDeckCards, remainingDeckCards, activeDeckCards, handCards, stats]);

    if (!isOpen) return null;

    // Separate Stone cards if any
    const stoneCards = activeDeckCards.filter(c => c.enhancement === 'StoneCards' || c.enhancement === 'Stone');

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
                            Remaining ({remainingDeckCards.length})
                        </button>

                        <button
                            className={`deck-tab-btn ${activeTab === 'full' ? 'active' : ''}`}
                            onClick={() => setActiveTab('full')}
                        >
                            {activeTab === 'full' && <div className="tab-arrow-pointer" />}
                            Full Deck ({fullDeckCards.length})
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
                            <div className="base-cards-header">Base Cards ({stats.total})</div>

                            {/* Card Category counts */}
                            <div className="base-cards-row categories">
                                <div className="base-stat-item" title="Aces">
                                    <div className="stat-icon-badge badge-ace">A</div>
                                    <span className="stat-number">{stats.aces}</span>
                                </div>
                                <div className="base-stat-item" title="Face Cards (K, Q, J)">
                                    <div className="stat-icon-badge badge-face">👑</div>
                                    <span className="stat-number">{stats.faces}</span>
                                </div>
                                <div className="base-stat-item" title="Numbered Cards">
                                    <div className="stat-icon-badge badge-number">#</div>
                                    <span className="stat-number">{stats.numbers}</span>
                                </div>
                            </div>

                            {/* Suits counts */}
                            <div className="base-cards-row suits">
                                <div className="base-stat-item" title="Spades">
                                    <div className="stat-icon-badge suit-spade">♠</div>
                                    <span className="stat-number">{stats.suitsCount.Spades || 0}</span>
                                </div>
                                <div className="base-stat-item" title="Hearts">
                                    <div className="stat-icon-badge suit-heart">♥</div>
                                    <span className="stat-number">{stats.suitsCount.Hearts || 0}</span>
                                </div>
                            </div>

                            <div className="base-cards-row suits">
                                <div className="base-stat-item" title="Clubs">
                                    <div className="stat-icon-badge suit-club">♣</div>
                                    <span className="stat-number">{stats.suitsCount.Clubs || 0}</span>
                                </div>
                                <div className="base-stat-item" title="Diamonds">
                                    <div className="stat-icon-badge suit-diamond">♦</div>
                                    <span className="stat-number">{stats.suitsCount.Diamonds || 0}</span>
                                </div>
                            </div>
                        </div>
                    </div>

                    {/* VERTICAL RANK COUNTS COLUMN */}
                    <div className="deck-ranks-column">
                        {RANKS.map(rank => (
                            <div key={rank} className="rank-count-pill">
                                <span className="rank-name">{rank}</span>
                                <span className="rank-val">{stats.ranksCount[rank] || 0}</span>
                            </div>
                        ))}
                    </div>

                    {/* CARDS MATRIX - EACH CARD IS RENDERED INDIVIDUALLY */}
                    <div className="deck-cards-matrix">
                        {SUITS.map(suit => {
                            // Extract and sort all individual cards for this suit
                            const suitCards = activeDeckCards
                                .filter(c => c.suit === suit && c.enhancement !== 'StoneCards' && c.enhancement !== 'Stone')
                                .sort((a, b) => (RANK_ORDER[b.rank] || 0) - (RANK_ORDER[a.rank] || 0));

                            return (
                                <div key={suit} className={`matrix-suit-row suit-${suit.toLowerCase()}`}>
                                    {suitCards.length > 0 ? (
                                        suitCards.map((card, idx) => (
                                            <div
                                                key={card.id || `${suit}-${card.rank}-${idx}`}
                                                className="matrix-card-wrapper in-deck"
                                                title={`${card.rank} of ${suit}${card.enhancement && card.enhancement !== 'None' ? ` (${card.enhancement})` : ''}${card.edition && card.edition !== 'Base' ? ` [${card.edition}]` : ''}`}
                                            >
                                                <PlayingCard
                                                    suit={card.suit}
                                                    rank={card.rank}
                                                    enhancement={card.enhancement}
                                                    edition={card.edition}
                                                    seal={card.seal}
                                                    isDebuffed={card.isDebuffed}
                                                    width={56}
                                                    height={78}
                                                />
                                            </div>
                                        ))
                                    ) : (
                                        <div className="matrix-empty-row-text">No {suit} in {activeTab === 'full' ? 'deck' : 'remaining cards'}</div>
                                    )}
                                </div>
                            );
                        })}

                        {/* Special Row for Stone Cards if any */}
                        {stoneCards.length > 0 && (
                            <div className="matrix-suit-row suit-stone">
                                {stoneCards.map((card, idx) => (
                                    <div
                                        key={card.id || `stone-${idx}`}
                                        className="matrix-card-wrapper in-deck"
                                        title={`Stone Card${card.edition && card.edition !== 'Base' ? ` [${card.edition}]` : ''}`}
                                    >
                                        <PlayingCard
                                            enhancement="StoneCards"
                                            edition={card.edition}
                                            seal={card.seal}
                                            isDebuffed={card.isDebuffed}
                                            width={56}
                                            height={78}
                                        />
                                    </div>
                                ))}
                            </div>
                        )}
                    </div>
                </div>
            </div>
        </div>
    );
}

export default DeckViewModal;
