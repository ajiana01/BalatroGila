import GameSidebar from '../GameSidebar/GameSidebar';
import ScorePanel from './ScorePanel';
import PlayingArea from './PlayingArea';
import PlayerHand from './PlayerHand';

import './GameBoard.css';

function GameBoard({
                       gameData,
                       onWin,
                       onLose,
                       onOpenSettings
                   }) {

    return (
        <div className="game-board">

            <GameSidebar gameData={gameData}
                         onOpenSettings={onOpenSettings} />

            <section className="game-main">

                <ScorePanel
                    score={gameData.score}
                    target={gameData.targetScore}
                />

                <PlayingArea />

                <PlayerHand />

                <div className="game-actions">

                    <button
                        onClick={onWin}
                        className="play-button"
                    >
                        Play Hand
                    </button>

                    <button className="discard-button">
                        Discard
                    </button>

                    <button
                        onClick={onLose}
                        className="debug-lose"
                    >
                        Debug Lose
                    </button>

                </div>

            </section>

        </div>
    );
}

export default GameBoard;