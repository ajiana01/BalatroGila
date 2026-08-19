import Sprite from '../Sprite/Sprite';
import { jokerSprite } from '../../data/sprites/jokerSprites';
import './JokerCard.css';

function JokerCard({
                       id,
                       width = 100,
                       height = 140,
                       animated = false
                   }) {
    const card = jokerSprite.cards[id];

    if (!card) {
        console.error(`Joker tidak ditemukan: ${id}`);
        return null;
    }

    return (
        <Sprite
            sprite={jokerSprite}
            column={card.column}
            row={card.row}
            width={width}
            height={height}
            animated={animated}
        />
    );
}

export default JokerCard;