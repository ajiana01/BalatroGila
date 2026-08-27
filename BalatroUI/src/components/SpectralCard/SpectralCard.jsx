import { useState } from 'react';
import Sprite from '../Sprite/Sprite';
import CardActionTabs from '../CardActionTabs/CardActionTabs';
import { spectralSprite } from '../../data/sprites/spectralSprites';
import { normalizeSpectralSpriteKey } from '../../utils/cardMapper';
import './SpectralCard.css';

function SpectralCard({
    spectral,
    spriteId,
    name,
    title,
    description,
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
    const rawKey = spriteId || name || title || (spectralSprite.spectrals[spectral] ? spectral : null);
    const cleanedKey = normalizeSpectralSpriteKey(rawKey || spectral);
    const spectralData = spectralSprite.spectrals[cleanedKey] || spectralSprite.spectrals['Familiar'];
    const info = {
        title: title || name || cleanedKey,
        description: description || ''
    };

    if (!spectralData) {
        return null;
    }

    return (
        <div
            className={`spectral-card-container ${isSelected ? 'selected' : ''}`}
            onClick={onSelect}
            onMouseEnter={() => setIsHovered(true)}
            onMouseLeave={() => setIsHovered(false)}
            style={{ width: `${width}px`, height: `${height}px` }}
        >
            <Sprite
                sprite={spectralSprite}
                column={spectralData.column}
                row={spectralData.row}
                width={width}
                height={height}
                animated={animated}
            />

            {/* Hover Floating Tooltip */}
            {showHoverTooltip && isHovered && !isSelected && (
                <div className="card-floating-tooltip">
                    <div className="card-floating-title">{info.title}</div>
                    {info.description && <div className="card-floating-description">{info.description}</div>}
                    <div className="card-floating-rarity rarity-spectral">
                        Spectral
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

export default SpectralCard;