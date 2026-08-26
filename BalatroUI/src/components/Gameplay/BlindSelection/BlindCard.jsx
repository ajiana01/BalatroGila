import Blind from '../../Blind/Blind';

function BlindCard({
    type,
    blind,
    title,
    score,
    reward,
    description,
    status,
    active = false,
    onSelect
}) {
    const cardStatus = status || (active ? 'active' : 'upcoming');
    const isActive = cardStatus === 'active';
    const isDefeated = cardStatus === 'defeated';

    const blindKey = blind || (
        type === 'small' ? 'SmallBlind' :
        type === 'big' ? 'BigBlind' : 'TheGoad'
    );

    return (
        <article
            className={`blind-card ${type} ${cardStatus}`}
        >
            {/* TOP HEADER: SELECT BUTTON (if active) OR STATUS BANNER */}
            {isActive ? (
                <button
                    onClick={onSelect}
                    className="blind-card-header select-button"
                >
                    Select
                </button>
            ) : isDefeated ? (
                <div className="blind-card-header defeated-header">
                    Defeated
                </div>
            ) : (
                <div className="blind-card-header upcoming-header">
                    Upcoming
                </div>
            )}

            <div className="blind-card-title">
                {title}
            </div>

            <div className="blind-icon-container">
                <Blind
                    blind={blindKey}
                    width={90}
                    height={90}
                    animated={isActive}
                />
            </div>

            <p className="blind-description">
                {description || '\u00A0'}
            </p>

            <div className="blind-score">
                <span>Score at least</span>
                <strong>{score}</strong>
            </div>

            <div className="blind-reward">
                Reward: {reward}
            </div>
        </article>
    );
}

export default BlindCard;