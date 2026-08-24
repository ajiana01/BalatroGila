import { useState } from 'react';
import Sprite from '../Sprite/Sprite';
import CardActionTabs from '../CardActionTabs/CardActionTabs';
import { tarotSprite } from '../../data/sprites/tarotSprites';
import { getCardInfo } from '../../data/shopData';
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
    sellPrice = 1,
    showHoverTooltip = true
}) {
    const [isHovered, setIsHovered] = useState(false);
    const tarotData = tarotSprite.tarots[tarot];
    const info = getCardInfo(tarot, 'tarot');

    if (!tarotData) {
        console.error(`Tarot Card tidak ditemukan: ${tarot}`);
        return null;
    }

    return (
        <div
            className={`tarot-card-container ${isSelected ? 'selected' : ''}`}
            onClick={onSelect}
            onMouseEnter={() => setIsHovered(true)}
            onMouseLeave={() => setIsHovered(false)}
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

            {/* Hover Floating Tooltip */}
            {showHoverTooltip && isHovered && !isSelected && (
                <div className="card-floating-tooltip">
                    <div className="card-floating-title">{info.title}</div>
                    <div className="card-floating-description">{info.description}</div>
                    <div className="card-floating-rarity rarity-tarot">
                        Tarot
                    </div>
                </div>
            )}

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