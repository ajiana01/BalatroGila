import Sprite from '../Sprite/Sprite';
import { blindSprite } from '../../data/sprites/blindSprites';

function Blind({
                   blind,
                   width = 100,
                   height = 100,
                   animated = false
               }) {
    const rawKey = blind || 'SmallBlind';
    const cleanedKey = blindSprite.blinds[rawKey] ? rawKey :
        (Object.keys(blindSprite.blinds).find(k => k.toLowerCase() === rawKey.replace(/[^a-zA-Z0-9]/g, '').toLowerCase()) || 'SmallBlind');
    const blindData = blindSprite.blinds[cleanedKey] || blindSprite.blinds['SmallBlind'];

    if (!blindData) {
        return null;
    }

    return (
        <Sprite
            sprite={blindSprite}
            column={blindData.column}
            row={blindData.row}
            width={width}
            height={height}
            animated={animated}
        />
    );
}

export default Blind;

//using example
// import Blind from './components/Blind/Blind';
//
// <Blind blind="SmallBlind" />