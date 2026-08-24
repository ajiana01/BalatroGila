import { useState } from 'react';
import Sprite from '../Sprite/Sprite';
import CardActionTabs from '../CardActionTabs/CardActionTabs';
import { jokerSprite } from '../../data/sprites/jokerSprites';
import { getCardInfo } from '../../data/shopData';
import './JokerCard.css';

function JokerCard({
    id,
    width = 100,
    height = 140,
    animated = false,
    isSelected = false,
    onSelect,
    onSell,
    sellPrice = 2,
    showHoverTooltip = true
}) {
    const [isHovered, setIsHovered] = useState(false);
    const card = jokerSprite.cards[id];
    const info = getCardInfo(id, 'joker');

    if (!card) {
        console.error(`Joker tidak ditemukan: ${id}`);
        return null;
    }

    return (
        <div
            className={`joker-card-container ${isSelected ? 'selected' : ''}`}
            onClick={onSelect}
            onMouseEnter={() => setIsHovered(true)}
            onMouseLeave={() => setIsHovered(false)}
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

            {/* Hover Floating Tooltip */}
            {showHoverTooltip && isHovered && !isSelected && (
                <div className="card-floating-tooltip">
                    <div className="card-floating-title">{info.title}</div>
                    <div className="card-floating-description">{info.description}</div>
                    <div className={`card-floating-rarity rarity-${(info.rarity || 'common').toLowerCase().replace(' ', '-')}`}>
                        {info.rarity || 'Common'}
                    </div>
                </div>
            )}

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