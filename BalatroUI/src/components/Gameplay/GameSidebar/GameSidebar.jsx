import './GameSidebar.css';

function GameSidebar({ gameData, onOpenSettings }) {

    return (
        <aside className="game-sidebar">

            <div className="sidebar-title">
                <span>Choose your</span>
                <span>next Blind</span>
            </div>

            <div className="round-score">
                <span>Round</span>
                <span>Score</span>

                <strong>
                    🃏 {gameData.score}
                </strong>
            </div>

            <div className="score-multiplier">

                <div className="score-number">
                    {gameData.score}
                </div>

                <span>×</span>

                <div className="score-number red">
                    0
                </div>

            </div>

            <div className="sidebar-stats">

                <div className="stat">
                    <span>Hands</span>
                    <strong>{gameData.hands}</strong>
                </div>

                <div className="stat">
                    <span>Discards</span>
                    <strong>{gameData.discards}</strong>
                </div>

            </div>

            <div className="money">
                ${gameData.money}
            </div>

            <div className="sidebar-buttons">

                <button className="sidebar-button red">
                    Run Info
                </button>

                <button className="sidebar-button orange"
                        onClick={onOpenSettings}
                >
                    Options
                </button>

            </div>

            <div className="ante-info">

                <div>
                    <span>Ante</span>
                    <strong>{gameData.ante} / 8</strong>
                </div>

                <div>
                    <span>Round</span>
                    <strong>{gameData.round}</strong>
                </div>

            </div>

        </aside>
    );
}

export default GameSidebar;