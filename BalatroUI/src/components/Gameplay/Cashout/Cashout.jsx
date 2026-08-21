import './Cashout.css';

function Cashout({
                     gameData,
                     onContinue
                 }) {

    return (
        <div className="cashout">

            <div className="cashout-panel">

                <h1>Blind Complete!</h1>

                <div className="cashout-score">
                    <span>Score</span>

                    <strong>
                        {gameData.score}
                    </strong>

                    <small>
                        Target: {gameData.targetScore}
                    </small>
                </div>

                <div className="cashout-reward">
                    + $4
                </div>

                <button onClick={onContinue}>
                    Continue
                </button>

            </div>

        </div>
    );
}

export default Cashout;