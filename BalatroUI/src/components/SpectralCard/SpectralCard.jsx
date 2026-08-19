import Sprite from '../Sprite/Sprite';
import { spectralSprite } from '../../data/sprites/spectralSprites';

function SpectralCard({
                          spectral,
                          width = 100,
                          height = 140,
                          animated = false
                      }) {
    const spectralData = spectralSprite.spectrals[spectral];

    if (!spectralData) {
        console.error(`Spectral Card tidak ditemukan: ${spectral}`);
        return null;
    }

    return (
        <Sprite
            sprite={spectralSprite}
            column={spectralData.column}
            row={spectralData.row}
            width={width}
            height={height}
            animated={animated}
        />
    );
}

export default SpectralCard;

// using
// <SpectralCard spectral="Familiar" />