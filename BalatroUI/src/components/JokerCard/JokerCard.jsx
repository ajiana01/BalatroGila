import Sprite from '../Sprite/Sprite';
import CardActionTabs from '../CardActionTabs/CardActionTabs';
import { jokerSprite } from '../../data/sprites/jokerSprites';
import './JokerCard.css';

function JokerCard({
    id,
    width = 100,
    height = 140,
    animated = false,
    isSelected = false,
    onSelect,
    onSell,
    sellPrice = 2
}) {
    const card = jokerSprite.cards[id];

    if (!card) {
        console.error(`Joker tidak ditemukan: ${id}`);
        return null;
    }

    return (
        <div
            className={`joker-card-container ${isSelected ? 'selected' : ''}`}
            onClick={onSelect}
            style={{ width: `${width}px`, height: `${height}px` }}
        >
            <Sprite
                sprite={jokerSprite}
                column={card.column}
                row={card.row}
                width={width}
                height={height}
                animated={animated}
            />

            {isSelected && (
                <CardActionTabs
                    canUse={false}
                    sellPrice={sellPrice}
                    onSell={onSell}
                />
            )}
        </div>
    );
}

export default JokerCard;