import Sprite from '../Sprite/Sprite';
import CardBack from '../CardBack/CardBack';
import { playingCardSprite } from '../../data/sprites/playingCardSprites';
import { cardBackSprite } from '../../data/sprites/cardBackSprites';
import './PlayingCard.css';

export function getEnhancementBackType(enhancement, explicitBackType) {
    if (explicitBackType && explicitBackType !== 'Normal' && cardBackSprite.types[explicitBackType]) {
        return explicitBackType;
    }
    if (!enhancement || enhancement === 'None' || enhancement === '0') {
        return 'Normal';
    }

    const str = String(enhancement).replace(/[^a-zA-Z0-9]/g, '').toLowerCase();
    const map = {
        'bonus': 'BonusCards',
        'bonuscards': 'BonusCards',
        'mult': 'MultCards',
        'multcards': 'MultCards',
        'wild': 'WildCards',
        'wildcards': 'WildCards',
        'glass': 'GlassCards',
        'glasscards': 'GlassCards',
        'steel': 'SteelCards',
        'steelcards': 'SteelCards',
        'stone': 'StoneCards',
        'stonecards': 'StoneCards',
        'gold': 'GoldCards',
        'goldcards': 'GoldCards',
        'lucky': 'LuckyCards',
        'luckycards': 'LuckyCards'
    };

    return map[str] || 'Normal';
}

function PlayingCard({
    rank,
    suit,
    enhancement = 'None',
    edition = 'Base',
    seal = 'None',
    backType,
    width = 100,
    height = 140,
    effect = '',
    isDebuffed = false,
    showBack = false,
    className = '',
    style = {}
}) {
    const resolvedBackType = getEnhancementBackType(enhancement, backType);
    const isStone = resolvedBackType === 'StoneCards';
    const card = (!isStone && suit && rank) ? playingCardSprite[suit]?.[rank] : null;

    // Edition class
    const editionStr = String(edition || 'Base').toLowerCase();
    const editionClass = editionStr.includes('foil') ? 'edition-foil' :
                         editionStr.includes('holo') ? 'edition-holographic' :
                         editionStr.includes('poly') ? 'edition-polychrome' : '';

    return (
        <div
            className={`playing-card ${effect} ${editionClass} ${isDebuffed ? 'is-debuffed' : ''} ${className}`}
            style={{
                width: `${width}px`,
                height: `${height}px`,
                ...style
            }}
        >
            {showBack ? (
                <div className="playing-card-back">
                    <CardBack
                        type={backType || 'Normal'}
                        width={width}
                        height={height}
                    />
                </div>
            ) : (
                <>
                    <div className="playing-card-back">
                        <CardBack
                            type={resolvedBackType}
                            width={width}
                            height={height}
                        />
                    </div>

                    {!isStone && card && (
                        <div className="playing-card-face">
                            <Sprite
                                sprite={playingCardSprite}
                                column={card.column}
                                row={card.row}
                                width={width}
                                height={height}
                            />
                        </div>
                    )}

                    {/* Seal badge if any */}
                    {seal && seal !== 'None' && cardBackSprite.types[seal] && (
                        <div className="playing-card-seal">
                            <Sprite
                                sprite={cardBackSprite}
                                column={cardBackSprite.types[seal].column}
                                row={cardBackSprite.types[seal].row}
                                width={width * 0.35}
                                height={height * 0.35}
                            />
                        </div>
                    )}

                    {isDebuffed && (
                        <div className="playing-card-debuff-overlay">
                            <div className="debuff-badge">DEBUFF</div>
                        </div>
                    )}
                </>
            )}
        </div>
    );
}

export default PlayingCard;