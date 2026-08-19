import Sprite from '../Sprite/Sprite';
import { boosterPackSprite } from '../../data/sprites/boosterPackSprites';

function BoosterPack({
                         type,
                         number,
                         width = 100,
                         height = 140,
                         animated = false
                     }) {
    const packType = boosterPackSprite[type];

    if (!packType) {
        console.error(`Booster Pack type tidak ditemukan: ${type}`);
        return null;
    }

    const pack = packType[number];

    if (!pack) {
        console.error(
            `Booster Pack tidak ditemukan: ${type} ${number}`
        );
        return null;
    }

    return (
        <Sprite
            sprite={boosterPackSprite}
            column={pack.column}
            row={pack.row}
            width={width}
            height={height}
            animated={animated}
        />
    );
}

export default BoosterPack;