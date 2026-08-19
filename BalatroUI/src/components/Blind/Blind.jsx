import Sprite from '../Sprite/Sprite';
import { blindSprite } from '../../data/sprites/blindSprites';

function Blind({
                   blind,
                   width = 100,
                   height = 100,
                   animated = false
               }) {
    const blindData = blindSprite.blinds[blind];

    if (!blindData) {
        console.error(`Blind tidak ditemukan: ${blind}`);
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