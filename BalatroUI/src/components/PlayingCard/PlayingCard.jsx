import Sprite from '../Sprite/Sprite';
import { playingCardSprite } from '../../data/sprites/playingCardSprites';
import './PlayingCard.css';

function PlayingCard({
                         rank,
                         suit,
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
        <Sprite
            sprite={playingCardSprite}
            column={card.column}
            row={card.row}
            width={width}
            height={height}
            animated={animated}
        />
    );
}

export default PlayingCard;