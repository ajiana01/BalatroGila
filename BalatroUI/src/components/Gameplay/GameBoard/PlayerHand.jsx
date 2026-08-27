import { useState } from 'react';
import { Reorder, motion } from 'framer-motion';
import PlayingCard from '../../PlayingCard/PlayingCard';
import { sfx } from '../../../utils/sfx';
import './PlayerHand.css';

const defaultHandCards = [
    { id: 'card-1', suit: 'Hearts', rank: 'A' },
    { id: 'card-2', suit: 'Hearts', rank: 'Q' },
    { id: 'card-3', suit: 'Diamonds', rank: '10' },
    { id: 'card-4', suit: 'Clubs', rank: '9' },
    { id: 'card-5', suit: 'Spades', rank: '7' },
    { id: 'card-6', suit: 'Spades', rank: '3' },
    { id: 'card-7', suit: 'Hearts', rank: '3' },
    { id: 'card-8', suit: 'Spades', rank: '2' }
];

function PlayerHand({
    cards: propCards,
    setCards: propSetCards,
    selectedIds: propSelectedIds,
    onToggleSelect: propOnToggleSelect,
    maxSelected = 5,
    isScoring = false,
    maxHandSize = 8
}) {
    const [internalCards, setInternalCards] = useState(defaultHandCards);
    const [internalSelected, setInternalSelected] = useState([]);

    const cards = propCards || internalCards;
    const setCards = propSetCards || setInternalCards;
    const selectedIds = propSelectedIds || internalSelected;

    const toggleSelectCard = (id) => {
        if (isScoring) return;

        const isCurrentlySelected = selectedIds.includes(id);
        sfx.playCardSelect(isCurrentlySelected ? 0.9 : 1.1);

        if (propOnToggleSelect) {
            propOnToggleSelect(id);
            return;
        }

        setInternalSelected((prev) => {
            if (prev.includes(id)) {
                return prev.filter((cardId) => cardId !== id);
            }
            if (prev.length >= maxSelected) {
                return prev;
            }
            return [...prev, id];
        });
    };

    return (
        <div className={`player-hand-container ${isScoring ? 'scoring-mode' : ''}`}>
            {isScoring ? (
                /* Scoring mode: non-draggable lowered cards with count underneath */
                <div className="player-hand-lowered-wrapper">
                    <div className="player-hand lowered">
                        {cards.map((card, idx) => {
                            const n = cards.length;
                            const centerIndex = (n - 1) / 2;
                            const diff = idx - centerIndex;
                            const rotateAngle = diff * 1.5;
                            const arcY = Math.abs(diff) * 1.2;

                            return (
                                <motion.div
                                    key={card.id}
                                    className="player-card-item lowered"
                                    initial={{ y: 20, opacity: 0.8 }}
                                    animate={{
                                        y: arcY,
                                        rotate: rotateAngle,
                                        zIndex: idx + 1
                                    }}
                                    transition={{ type: 'spring', stiffness: 350, damping: 25 }}
                                    style={{
                                        marginLeft: idx === 0 ? '0px' : '-20px',
                                        zIndex: idx + 1
                                    }}
                                >
                                    <PlayingCard
                                        suit={card.suit}
                                        rank={card.rank}
                                        isDebuffed={card.isDebuffed}
                                        width={92}
                                        height={130}
                                    />
                                </motion.div>
                            );
                        })}
                    </div>

                    <div className="lowered-hand-count">
                        {cards.length}/{maxHandSize}
                    </div>
                </div>
            ) : (
                /* Normal Interactive Reorder Group */
                <Reorder.Group
                    axis="x"
                    values={cards}
                    onReorder={setCards}
                    className="player-hand"
                    as="div"
                >
                    {cards.map((card, idx) => {
                        const isSelected = selectedIds.includes(card.id);
                        const n = cards.length;
                        const centerIndex = (n - 1) / 2;
                        const diff = idx - centerIndex;
                        const rotateAngle = diff * 1.8;
                        const arcY = Math.abs(diff) * 1.2;

                        return (
                            <Reorder.Item
                                key={card.id}
                                value={card}
                                className={`player-card-item ${isSelected ? 'selected' : ''}`}
                                onClick={() => toggleSelectCard(card.id)}
                                whileHover={{
                                    scale: 1.08,
                                    y: isSelected ? -34 : -16,
                                    zIndex: 80,
                                    rotate: 0,
                                    transition: { duration: 0.12 }
                                }}
                                whileDrag={{
                                    scale: 1.15,
                                    zIndex: 100,
                                    rotate: 0,
                                    cursor: 'grabbing',
                                    filter: 'drop-shadow(0 15px 25px rgba(0,0,0,0.6))'
                                }}
                                animate={{
                                    y: isSelected ? -28 : arcY,
                                    rotate: isSelected ? 0 : rotateAngle,
                                    zIndex: isSelected ? 60 : idx + 1
                                }}
                                transition={{ type: 'spring', stiffness: 450, damping: 30 }}
                                style={{
                                    marginLeft: idx === 0 ? '0px' : '-22px',
                                    zIndex: isSelected ? 60 : idx + 1
                                }}
                            >
                                <PlayingCard
                                    suit={card.suit}
                                    rank={card.rank}
                                    isDebuffed={card.isDebuffed}
                                    width={92}
                                    height={130}
                                />
                            </Reorder.Item>
                        );
                    })}
                </Reorder.Group>
            )}
        </div>
    );
}

export default PlayerHand;