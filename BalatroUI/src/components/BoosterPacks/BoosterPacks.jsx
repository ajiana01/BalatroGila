import Sprite from '../Sprite/Sprite';
import { boosterPackSprite } from '../../data/sprites/boosterPackSprites';

function BoosterPack({
    type = 'Arcana_Normal',
    number = 1,
    width = 100,
    height = 140,
    animated = false
}) {
    // 1. Resolve packType with fallback
    let resolvedType = type;
    if (!boosterPackSprite[resolvedType]) {
        // Try appending _Normal if only kind was passed (e.g., 'Arcana' -> 'Arcana_Normal')
        if (boosterPackSprite[`${resolvedType}_Normal`]) {
            resolvedType = `${resolvedType}_Normal`;
        } else if (resolvedType.includes('Buffoon')) {
            resolvedType = resolvedType.replace('Buffoon', 'Buffon');
        } else {
            console.warn(`[BoosterPack] Unknown type '${type}', falling back to 'Arcana_Normal'`);
            resolvedType = 'Arcana_Normal';
        }
    }

    const packType = boosterPackSprite[resolvedType] || boosterPackSprite.Arcana_Normal;

    // 2. Resolve variant number with fallback
    let pack = packType ? packType[number] : null;
    if (!pack && packType) {
        const availableKeys = Object.keys(packType);
        const fallbackKey = availableKeys[0];
        pack = packType[fallbackKey];
    }

    if (!pack) {
        console.error(`[BoosterPack] Sprite not found for: type='${type}' (resolved='${resolvedType}'), number='${number}'`);
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

// using example
// <BoosterPacks
// type="Arcana_Jumbo"
// number="2"
//     />