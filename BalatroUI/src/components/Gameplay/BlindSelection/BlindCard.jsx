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
    onSelect,
    onSkip
}) {
    const cardStatus = status || (active ? 'active' : 'upcoming');
    const isActive = cardStatus === 'active';
    const isDefeated = cardStatus === 'defeated' || cardStatus === 'skipped';

    const blindKey = blind || (
        type === 'small' ? 'SmallBlind' :
        type === 'big' ? 'BigBlind' : 'TheGoad'
    );

    const headerText = isActive
        ? 'Select'
        : isDefeated
            ? (cardStatus === 'skipped' ? 'Skipped' : 'Defeated')
            : 'Upcoming';

    return (
        <article
            className={`blind-card ${type} ${cardStatus}`}
        >
            <div className="blind-card-header">
                {headerText}
            </div>

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

            <div className="blind-actions">
                {isActive ? (
                    <>
                        <button
                            onClick={onSelect}
                            className="select-button"
                        >
                            Select
                        </button>

                        {onSkip ? (
                            <button
                                onClick={onSkip}
                                className="skip-button"
                            >
                                Skip Blind
                            </button>
                        ) : (
                            <div className="action-placeholder" />
                        )}
                    </>
                ) : isDefeated ? (
                    <div className="defeated-badge">
                        <span>{cardStatus === 'skipped' ? 'SKIPPED' : 'DEFEATED'}</span>
                    </div>
                ) : (
                    <div className="upcoming-badge">
                        <span>LOCKED</span>
                    </div>
                )}
            </div>
        </article>
    );
}

export default BlindCard;