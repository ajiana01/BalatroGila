import { useState } from 'react';
import Sprite from '../Sprite/Sprite';
import CardActionTabs from '../CardActionTabs/CardActionTabs';
import { jokerSprite } from '../../data/sprites/jokerSprites';
import { getCardInfo } from '../../data/shopData';
import { normalizeJokerSpriteKey } from '../../utils/cardMapper';
import './JokerCard.css';

function JokerCard({
    id,
    spriteId,
    jokerKey,
    name,
    title,
    description,
    rarity,
    width = 100,
    height = 140,
    animated = false,
    isSelected = false,
    isTriggered = false,
    triggeredText = '',
    onSelect,
    onSell,
    sellPrice = 2,
    showHoverTooltip = true
}) {
    const [isHovered, setIsHovered] = useState(false);
    const rawKey = spriteId || jokerKey || name || title || id;
    const resolvedKey = normalizeJokerSpriteKey(rawKey);
    const card = jokerSprite.cards[resolvedKey] || jokerSprite.cards['Joker'];
    const fallbackInfo = getCardInfo(resolvedKey, 'joker');
    const info = {
        title: title || name || fallbackInfo?.title || resolvedKey,
        description: description || fallbackInfo?.description || '',
        rarity: rarity || fallbackInfo?.rarity || 'Common'
    };

    if (!card) {
        return null;
    }

    return (
        <div
            className={`joker-card-container ${isSelected ? 'selected' : ''} ${isTriggered ? 'triggered' : ''}`}
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

            {/* Triggered floating score text */}
            {isTriggered && triggeredText && (
                <div className="joker-triggered-badge">
                    {triggeredText}
                </div>
            )}

            {/* Hover Floating Tooltip */}
            {showHoverTooltip && isHovered && !isSelected && !isTriggered && (
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