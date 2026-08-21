function BlindCard({
                       type,
                       title,
                       score,
                       reward,
                       description,
                       active = false,
                       onSelect,
                       onSkip
                   }) {

    return (
        <article
            className={`blind-card ${type} ${
                active ? 'active' : 'disabled'
            }`}
        >

            <div className="blind-card-header">
                {active ? 'Select' : 'Upcoming'}
            </div>

            <div className="blind-card-title">
                {title}
            </div>

            <div className="blind-icon">

                {type === 'small' && '◉'}
                {type === 'big' && '●'}
                {type === 'boss' && '◐'}

            </div>

            {description && (
                <p className="blind-description">
                    {description}
                </p>
            )}

            <div className="blind-score">
                Score at least

                <strong>
                    {score}
                </strong>
            </div>

            <div className="blind-reward">
                Reward: {reward}
            </div>

            {active && (
                <div className="blind-actions">

                    <button
                        onClick={onSelect}
                        className="select-button"
                    >
                        Select
                    </button>

                    <button
                        onClick={onSkip}
                        className="skip-button"
                    >
                        Skip Blind
                    </button>

                </div>
            )}

        </article>
    );
}

export default BlindCard;