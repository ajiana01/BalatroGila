import Sprite from '../Sprite/Sprite';
import CardBack from '../CardBack/CardBack';
import { playingCardSprite } from '../../data/sprites/playingCardSprites';
import './PlayingCard.css';

function PlayingCard({
                         rank,
                         suit,

                         // Card back
                         backType = 'Normal',

                         width = 100,
                         height = 140,

                         animated = false
                     }) {
    const card = playingCardSprite[suit]?.[rank];

    if (!card) {
        console.error(`Playing Card tidak ditemukan: ${rank} of ${suit}`);
        return null;
    }

    return (
        <div
            className="playing-card"
            style={{
                width: `${width}px`,
                height: `${height}px`
            }}
        >
            {/* BACKGROUND */}
            <div className="playing-card-back">
                <CardBack
                    type={backType}
                    width={width}
                    height={height}
                />
            </div>

            {/* CARD FACE */}
            <div className="playing-card-face">
                <Sprite
                    sprite={playingCardSprite}
                    column={card.column}
                    row={card.row}
                    width={width}
                    height={height}
                    animated={animated}
                />
            </div>
        </div>
    );
}

export default PlayingCard;

//using
// <PlayingCard
//     rank="K"
//     suit="Hearts"
//     backType="Chip"
// />