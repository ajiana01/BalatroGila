import Sprite from '../Sprite/Sprite';
import CardBack from '../CardBack/CardBack';
import { playingCardSprite } from '../../data/sprites/playingCardSprites';
import './PlayingCard.css';

function PlayingCard({
                         rank,
                         suit,
                         backType = 'Normal',
                         width = 100,
                         height = 140,
                         effect = ''
                     }) {
    const card = playingCardSprite[suit]?.[rank];

    if (!card) {
        console.error(`Playing Card tidak ditemukan: ${rank} of ${suit}`);
        return null;
    }

    return (
        <div
            className={`playing-card ${effect}`}
            style={{
                width: `${width}px`,
                height: `${height}px`
            }}
        >
            <div className="playing-card-back">
                <CardBack
                    type={backType}
                    width={width}
                    height={height}
                />
            </div>

            <div className="playing-card-face">
                <Sprite
                    sprite={playingCardSprite}
                    column={card.column}
                    row={card.row}
                    width={width}
                    height={height}
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