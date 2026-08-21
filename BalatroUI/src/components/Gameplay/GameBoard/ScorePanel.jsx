function ScorePanel({
                        score,
                        target
                    }) {

    return (
        <div className="score-panel">

            <div>
                Score
                <strong>
                    {score}
                </strong>
            </div>

            <span>/</span>

            <div>
                Target
                <strong>
                    {target}
                </strong>
            </div>

        </div>
    );
}

export default ScorePanel;