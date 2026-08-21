import Sprite from '../Sprite/Sprite';
import CardActionTabs from '../CardActionTabs/CardActionTabs';
import { tarotSprite } from '../../data/sprites/tarotSprites';
import './TarotCard.css';

function TarotCard({
    tarot,
    width = 100,
    height = 140,
    animated = false,
    isSelected = false,
    onSelect,
    onSell,
    onUse,
    sellPrice = 1
}) {
    const tarotData = tarotSprite.tarots[tarot];

    if (!tarotData) {
        console.error(`Tarot Card tidak ditemukan: ${tarot}`);
        return null;
    }

    return (
        <div
            className={`tarot-card-container ${isSelected ? 'selected' : ''}`}
            onClick={onSelect}
            style={{ width: `${width}px`, height: `${height}px` }}
        >
            <Sprite
                sprite={tarotSprite}
                column={tarotData.column}
                row={tarotData.row}
                width={width}
                height={height}
                animated={animated}
            />

            {isSelected && (
                <CardActionTabs
                    canUse={true}
                    sellPrice={sellPrice}
                    onSell={onSell}
                    onUse={onUse}
                />
            )}
        </div>
    );
}

export default TarotCard;