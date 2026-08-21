import './WinOver.css';

function WinOver({
                     gameData,
                     onRestart
                 }) {

    return (
        <div className="win-over">

            <div className="win-over-panel">

                <div className="win-icon">
                    ★
                </div>

                <h1>YOU WIN!</h1>

                <p>
                    Congratulations!
                </p>

                <div className="win-stats">

                    <span>
                        Final Ante
                    </span>

                    <strong>
                        {gameData.ante} / 8
                    </strong>

                    <span>
                        Money
                    </span>

                    <strong>
                        ${gameData.money}
                    </strong>

                </div>

                <button onClick={onRestart}>
                    New Run
                </button>

            </div>

        </div>
    );
}

export default WinOver;