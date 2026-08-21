import PlayingCard from '../../PlayingCard/PlayingCard';
import './PlayerHand.css';

const cards = [
    { suit: 'Spades', rank: 'A' },
    { suit: 'Hearts', rank: 'K' },
    { suit: 'Diamonds', rank: '7' },
    { suit: 'Clubs', rank: 'Q' },
    { suit: 'Hearts', rank: '3' }
];

function PlayerHand() {

    return (
        <div className="player-hand">

            {cards.map((card, index) => (

                <PlayingCard
                    key={index}
                    suit={card.suit}
                    rank={card.rank}
                    width={100}
                    height={140}
                />

            ))}

        </div>
    );
}

export default PlayerHand;