import './SpritePlayingCard.css';
import playingCardSprite from '../../assets/Balatro-Playing Cards.png';
import { playingCardSprites } from '../../data/playingCardSprites';

const SPRITE_SHEET_WIDTH = 923;
const SPRITE_SHEET_HEIGHT = 380;

const SPRITE_COLUMNS = 13;
const SPRITE_ROWS = 4;

const CARD_WIDTH = SPRITE_SHEET_WIDTH / SPRITE_COLUMNS;
const CARD_HEIGHT = SPRITE_SHEET_HEIGHT / SPRITE_ROWS;

function SpritePlayingCard({
                        rank,
                        suit,
                        width = CARD_WIDTH,
                        height = CARD_HEIGHT,
                        animated = false,
                        className = ''
                    }) {
    const card = playingCardSprites[suit]?.[rank];

    if (!card) {
        console.error(`Card tidak ditemukan: ${rank} of ${suit}`);
        return null;
    }

    const scaleX = width / CARD_WIDTH;
    const scaleY = height / CARD_HEIGHT;

    return (
        <div
            className={`sprite-card ${animated ? 'sprite-card-animated' : ''} ${className}`}
            style={{
                width: `${width}px`,
                height: `${height}px`,

                backgroundImage: `url(${playingCardSprite})`,

                backgroundSize: `
                    ${CARD_WIDTH * SPRITE_COLUMNS * scaleX}px
                    ${CARD_HEIGHT * SPRITE_ROWS * scaleY}px
                `,

                backgroundPosition: `
                    -${card.column * CARD_WIDTH * scaleX}px
                    -${card.row * CARD_HEIGHT * scaleY}px
                `,

                backgroundRepeat: 'no-repeat'
            }}
        />
    );
}

export default SpritePlayingCard;