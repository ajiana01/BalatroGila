import Sprite from '../Sprite/Sprite';
import { tarotSprite } from '../../data/sprites/tarotSprites';

function TarotCard({
                       tarot,
                       width = 100,
                       height = 140,
                       animated = false
                   }) {
    const tarotData = tarotSprite.tarots[tarot];

    if (!tarotData) {
        console.error(`Tarot Card tidak ditemukan: ${tarot}`);
        return null;
    }

    return (
        <Sprite
            sprite={tarotSprite}
            column={tarotData.column}
            row={tarotData.row}
            width={width}
            height={height}
            animated={animated}
        />
    );
}

export default TarotCard;