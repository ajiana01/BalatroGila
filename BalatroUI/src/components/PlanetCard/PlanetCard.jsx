import Sprite from '../Sprite/Sprite';
import CardActionTabs from '../CardActionTabs/CardActionTabs';
import { planetSprite } from '../../data/sprites/planetSprites';
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
    sellPrice = 1
}) {
    const planetData = planetSprite.planets[planet];

    if (!planetData) {
        console.error(`Planet Card tidak ditemukan: ${planet}`);
        return null;
    }

    return (
        <div
            className={`planet-card-container ${isSelected ? 'selected' : ''}`}
            onClick={onSelect}
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