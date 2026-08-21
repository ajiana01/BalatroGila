const cards = [
    { suit: '♠', rank: 'A', red: false },
    { suit: '♥', rank: 'K', red: true },
    { suit: '♦', rank: '7', red: true },
    { suit: '♣', rank: 'Q', red: false },
    { suit: '♥', rank: '3', red: true },
];

function PlayerHand() {

    return (
        <div className="player-hand">

            {cards.map((card, index) => (

                <div
                    key={index}
                    className={`playing-card ${
                        card.red ? 'red' : ''
                    }`}
                >

                    <span>
                        {card.rank}
                    </span>

                    <span>
                        {card.suit}
                    </span>

                </div>

            ))}

        </div>
    );
}

export default PlayerHand;