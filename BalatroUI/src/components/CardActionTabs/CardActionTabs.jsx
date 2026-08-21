import './CardActionTabs.css';

function CardActionTabs({
    canUse = false,
    sellPrice = 1,
    onSell,
    onUse
}) {
    return (
        <div className="card-action-tabs" onClick={(e) => e.stopPropagation()}>
            {/* SELL BUTTON */}
            <button
                type="button"
                className="card-action-tab sell-tab"
                onClick={(e) => {
                    e.stopPropagation();
                    if (onSell) onSell();
                }}
                title={`Sell for $${sellPrice}`}
            >
                <span className="tab-text-sell">SELL</span>
                <span className="tab-text-price">${sellPrice}</span>
            </button>

            {/* USE BUTTON */}
            {canUse && (
                <button
                    type="button"
                    className="card-action-tab use-tab"
                    onClick={(e) => {
                        e.stopPropagation();
                        if (onUse) onUse();
                    }}
                    title="Use Card"
                >
                    <span className="tab-text-use">USE</span>
                </button>
            )}
        </div>
    );
}

export default CardActionTabs;
