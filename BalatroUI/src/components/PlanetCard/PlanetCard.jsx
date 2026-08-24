import { useState } from 'react';
import Sprite from '../Sprite/Sprite';
import CardActionTabs from '../CardActionTabs/CardActionTabs';
import { planetSprite } from '../../data/sprites/planetSprites';
import { getCardInfo } from '../../data/shopData';
import './PlanetCard.css';

function PlanetCard({
    planet,
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
    const planetData = planetSprite.planets[planet];
    const info = getCardInfo(planet, 'planet');

    if (!planetData) {
        console.error(`Planet Card tidak ditemukan: ${planet}`);
        return null;
    }

    return (
        <div
            className={`planet-card-container ${isSelected ? 'selected' : ''}`}
            onClick={onSelect}
            onMouseEnter={() => setIsHovered(true)}
            onMouseLeave={() => setIsHovered(false)}
            style={{ width: `${width}px`, height: `${height}px` }}
        >
            <Sprite
                sprite={planetSprite}
                column={planetData.column}
                row={planetData.row}
                width={width}
                height={height}
                animated={animated}
            />

            {/* Hover Floating Tooltip */}
            {showHoverTooltip && isHovered && !isSelected && (
                <div className="card-floating-tooltip">
                    <div className="card-floating-title">{info.title}</div>
                    <div className="card-floating-description">{info.description}</div>
                    <div className="card-floating-rarity rarity-planet">
                        Planet
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

export default PlanetCard;