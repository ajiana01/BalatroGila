import './GameOver.css';

function GameOver({
                      gameData,
                      onRestart
                  }) {

    return (
        <div className="game-over">

            <div className="game-over-panel">

                <h1>GAME OVER</h1>

                <p>
                    Your run has ended.
                </p>

                <div className="game-over-stats">

                    <span>
                        Ante
                    </span>

                    <strong>
                        {gameData.ante} / 8
                    </strong>

                </div>

                <button onClick={onRestart}>
                    New Run
                </button>

            </div>

        </div>
    );
}

export default GameOver;