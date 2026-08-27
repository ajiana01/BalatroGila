import { useMemo } from 'react';
import './DeckHoverPreview.css';

const SUITS = [
    { key: 'Spades', icon: '♠', label: 'Spades', className: 'suit-spades' },
    { key: 'Hearts', icon: '♥', label: 'Hearts', className: 'suit-hearts' },
    { key: 'Clubs', icon: '♣', label: 'Clubs', className: 'suit-clubs' },
    { key: 'Diamonds', icon: '♦', label: 'Diamonds', className: 'suit-diamonds' }
];

const RANKS = ['A', 'K', 'Q', 'J', '10', '9', '8', '7', '6', '5', '4', '3', '2'];

function DeckHoverPreview({ gameData, handCards = [] }) {
    // Calculate matrix of remaining cards in deck
    const { matrix, suitTotals, rankTotals } = useMemo(() => {
        let remainingCards = gameData?.remainingCards;

        if (!remainingCards || remainingCards.length === 0) {
            // Fallback: derive from fullDeck minus handCards
            const fullDeck = gameData?.fullDeck?.length ? gameData.fullDeck : [];
            const usedHandKeys = new Map();
            handCards.forEach(c => {
                const k = `${c.suit}-${c.rank}`;
                usedHandKeys.set(k, (usedHandKeys.get(k) || 0) + 1);
            });

            const rem = [];
            const remCount = new Map();
            fullDeck.forEach(c => {
                const k = `${c.suit}-${c.rank}`;
                const handCount = usedHandKeys.get(k) || 0;
                const alreadyExcluded = remCount.get(k) || 0;
                if (alreadyExcluded < handCount) {
                    remCount.set(k, alreadyExcluded + 1);
                } else {
                    rem.push(c);
                }
            });
            remainingCards = rem;
        }

        const matrix = {};
        const suitTotals = {};
        const rankTotals = {};

        RANKS.forEach(r => { rankTotals[r] = 0; });

        SUITS.forEach(s => {
            matrix[s.key] = {};
            let sTotal = 0;

            RANKS.forEach(r => {
                const count = remainingCards.filter(c => c.suit === s.key && c.rank === r).length;
                matrix[s.key][r] = count;
                sTotal += count;
                rankTotals[r] += count;
            });

            suitTotals[s.key] = sTotal;
        });

        return { matrix, suitTotals, rankTotals };
    }, [gameData, handCards]);

    return (
        <div className="deck-hover-preview-container">
            <div className="deck-hover-matrix-box">
                {/* TOP HEADER ROW: RANKS & TOTAL PER RANK */}
                <div className="deck-matrix-header-row">
                    <div className="matrix-header-spacer" />
                    <div className="matrix-ranks-list">
                        {RANKS.map(rank => (
                            <div key={rank} className="matrix-rank-column-header">
                                <div className="rank-white-pill">
                                    <span className="rank-letter">{rank}</span>
                                    <span className="rank-count-badge">{rankTotals[rank]}</span>
                                </div>
                            </div>
                        ))}
                    </div>
                </div>

                {/* SUIT ROWS */}
                <div className="deck-matrix-body">
                    {SUITS.map(suit => (
                        <div key={suit.key} className={`deck-matrix-suit-row ${suit.className}`}>
                            {/* LEFT SUIT BADGE */}
                            <div className="suit-left-badge">
                                <span className="suit-symbol">{suit.icon}</span>
                                <span className="suit-total-num">{suitTotals[suit.key]}</span>
                            </div>

                            {/* ROW CELL VALUES TRACK */}
                            <div className="suit-cells-track">
                                {RANKS.map(rank => {
                                    const count = matrix[suit.key]?.[rank] ?? 0;
                                    const isZero = count === 0;

                                    return (
                                        <div
                                            key={rank}
                                            className={`matrix-count-cell ${isZero ? 'count-zero' : 'count-positive'}`}
                                        >
                                            {count}
                                        </div>
                                    );
                                })}
                            </div>
                        </div>
                    ))}
                </div>
            </div>
        </div>
    );
}

export default DeckHoverPreview;
