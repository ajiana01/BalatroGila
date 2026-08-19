import Sprite from '../Sprite/Sprite';
import { cardBackSprite } from '../../data/sprites/cardBackSprites';

function CardBack({
                      type = 'Normal',
                      width = 100,
                      height = 140,
                      animated = false
                  }) {
    const cardBack = cardBackSprite.types[type];

    if (!cardBack) {
        console.error(`Card Back tidak ditemukan: ${type}`);
        return null;
    }

    return (
        <Sprite
            sprite={cardBackSprite}
            column={cardBack.column}
            row={cardBack.row}
            width={width}
            height={height}
            animated={animated}
        />
    );
}

export default CardBack;

// using example
// <CardBack type="Chip"/>