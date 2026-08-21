import { useState } from 'react';
import { Reorder } from 'framer-motion';
import PlayingCard from '../../PlayingCard/PlayingCard';
import './PlayerHand.css';

const initialCards = [
    { id: 'card-1', suit: 'Spades', rank: 'A' },
    { id: 'card-2', suit: 'Hearts', rank: 'K' },
    { id: 'card-3', suit: 'Diamonds', rank: '7' },
    { id: 'card-4', suit: 'Clubs', rank: 'Q' },
    { id: 'card-5', suit: 'Hearts', rank: '3' }
];

function PlayerHand({ maxSelected = 5 }) {
    const [cards, setCards] = useState(initialCards);
    const [selectedIds, setSelectedIds] = useState([]);

    const toggleSelectCard = (id) => {
        setSelectedIds((prev) => {
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
        <div className="player-hand-container">
            <Reorder.Group
                axis="x"
                values={cards}
                onReorder={setCards}
                className="player-hand"
                as="div"
            >
                {cards.map((card) => {
                    const isSelected = selectedIds.includes(card.id);

                    return (
                        <Reorder.Item
                            key={card.id}
                            value={card}
                            className={`player-card-item ${isSelected ? 'selected' : ''}`}
                            onClick={() => toggleSelectCard(card.id)}
                            whileHover={{ scale: 1.05, y: isSelected ? -26 : -10 }}
                            whileDrag={{
                                scale: 1.15,
                                zIndex: 100,
                                cursor: 'grabbing',
                                filter: 'drop-shadow(0 15px 25px rgba(0,0,0,0.6))'
                            }}
                            animate={{ y: isSelected ? -22 : 0 }}
                            transition={{ type: 'spring', stiffness: 400, damping: 28 }}
                        >
                            <PlayingCard
                                suit={card.suit}
                                rank={card.rank}
                                width={100}
                                height={140}
                            />
                        </Reorder.Item>
                    );
                })}
            </Reorder.Group>
        </div>
    );
}

export default PlayerHand;