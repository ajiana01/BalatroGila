import GameSidebar from '../GameSidebar/GameSidebar';
import ScorePanel from './ScorePanel';
import PlayingArea from './PlayingArea';
import PlayerHand from './PlayerHand';
import CardBack from '../../CardBack/CardBack';
import './GameBoard.css';

function GameBoard({
                       gameData,
                       onWin,
                       onLose,
                       onOpenSettings
                   }) {

    return (
        <div className="game-board">

            <GameSidebar
                gameData={gameData}
                onOpenSettings={onOpenSettings}
                isBlindSelection={false}
            />

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

                {/* Deck Counter Area on bottom right */}
                <div className="game-deck-area">
                    <div className="peek-deck-label">
                        <span>PEEK</span>
                        <span>DECK</span>
                        <div className="deck-key-hint">LT</div>
                    </div>

                    <div className="deck-card-stack">
                        <div className="deck-card-visual">
                            <CardBack
                                type="BackNormal"
                                width={84}
                                height={118}
                            />
                        </div>
                    </div>

                    <div className="deck-count-text">
                        {gameData.deckRemaining || 52}/52
                    </div>
                </div>

            </section>

        </div>
    );
}

export default GameBoard;