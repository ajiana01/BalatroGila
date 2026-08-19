import Sprite from '../Sprite/Sprite';
import { planetSprite } from '../../data/sprites/planetSprites';

function PlanetCard({
                        planet,
                        width = 100,
                        height = 140,
                        animated = false
                    }) {
    const planetData = planetSprite.planets[planet];

    if (!planetData) {
        console.error(`Planet Card tidak ditemukan: ${planet}`);
        return null;
    }

    return (
        <Sprite
            sprite={planetSprite}
            column={planetData.column}
            row={planetData.row}
            width={width}
            height={height}
            animated={animated}
        />
    );
}

export default PlanetCard;