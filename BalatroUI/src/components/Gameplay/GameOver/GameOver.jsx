import { useState } from 'react';
import Sprite from '../../Sprite/Sprite';
import Blind from '../../Blind/Blind';
import { jokerSprite } from '../../../data/sprites/jokerSprites';
import './GameOver.css';

function RedChipIcon() {
    return (
        <svg width="18" height="18" viewBox="0 0 24 24" fill="none" className="game-over-chip-icon">
            <circle cx="12" cy="12" r="10" fill="#e74c3c" stroke="#b02626" strokeWidth="2" />
            <circle cx="12" cy="12" r="7" stroke="#ffffff" strokeWidth="1.5" strokeDasharray="3 2" />
            <circle cx="12" cy="12" r="3.5" fill="#ffffff" />
        </svg>
    );
}

const GAME_OVER_QUOTES = [
    "I'm literally a fool, what's your excuse?",
    "Better luck next run!",
    "Those blinds were brutal.",
    "The house always wins... usually.",
    "Try another strategy next time!",
    "One more run couldn't hurt..."
];

const CONFETTI_PIECES = [
    { color: '#ff4757', left: '8%', top: '25%', size: 9, delay: '0s', rot: 15 },
    { color: '#ffa502', left: '88%', top: '30%', size: 10, delay: '0.5s', rot: -30 },
    { color: '#70a1ff', left: '14%', top: '70%', size: 8, delay: '1s', rot: 45 },
    { color: '#ff6b81', left: '82%', top: '75%', size: 11, delay: '0.3s', rot: -20 },
    { color: '#eccc68', left: '20%', top: '40%', size: 10, delay: '0.8s', rot: 60 },
    { color: '#ff4757', left: '92%', top: '15%', size: 8, delay: '1.2s', rot: -45 }
];

function GameOver({
    gameData = {},
    onRestart,
    onMainMenu
}) {
    const [quoteIndex, setQuoteIndex] = useState(0);
    const [hoveredStat, setHoveredStat] = useState(null);

    const stats = gameData.stats || {};

    const ante = gameData.ante || gameData.currentAnte || 1;
    const round = gameData.round || gameData.currentRound || 1;
    const bestScore = stats.bestHandScore ?? (gameData.score ?? 0);
    const bestHandName = stats.bestHandName || 'High Card';
    const mostPlayed = stats.mostPlayedHand || 'None';
    const mostPlayedCount = stats.mostPlayedCount ?? 0;
    const handsHistory = stats.handsHistory || {};

    // Defeated by blind determination
    const blindType = gameData.currentBlind?.type || 'small';
    const blindTitle = gameData.currentBlind?.title || (
        blindType === 'small' ? 'Small Blind' :
        blindType === 'boss' ? 'Boss Blind' : 'Big Blind'
    );
    const blindKey = gameData.currentBlind?.blind || (
        blindType === 'small' ? 'SmallBlind' :
        blindType === 'boss' ? 'TheGoad' : 'BigBlind'
    );

    const handleNextQuote = () => {
        setQuoteIndex((prev) => (prev + 1) % GAME_OVER_QUOTES.length);
    };

    return (
        <div className="game-over-backdrop">
            {/* BACKGROUND CONFETTI PARTICLES */}
            <div className="game-over-confetti-container" aria-hidden="true">
                {CONFETTI_PIECES.map((piece, i) => (
                    <div
                        key={i}
                        className="game-over-confetti-piece"
                        style={{
                            backgroundColor: piece.color,
                            left: piece.left,
                            top: piece.top,
                            width: `${piece.size}px`,
                            height: `${piece.size}px`,
                            animationDelay: piece.delay,
                            transform: `rotate(${piece.rot}deg)`
                        }}
                    />
                ))}
            </div>

            <div className="game-over-layout">
                {/* LEFT MASCOT SECTION (JIMBO & SPEECH BUBBLE) */}
                <div className="game-over-mascot-wrapper" onClick={handleNextQuote} title="Click to hear Jimbo!">
                    <div className="game-over-jimbo-card">
                        <Sprite
                            sprite={jokerSprite}
                            column={0}
                            row={0}
                            width={92}
                            height={128}
                            animated={true}
                        />
                        {/* JIMBO CONFETTI BURST */}
                        <div className="jimbo-confetti c1" />
                        <div className="jimbo-confetti c2" />
                        <div className="jimbo-confetti c3" />
                    </div>

                    <div className="game-over-speech-bubble">
                        <p>{GAME_OVER_QUOTES[quoteIndex]}</p>
                        <div className="speech-bubble-tail" />
                    </div>
                </div>

                {/* MAIN "GAME OVER" MODAL PANEL */}
                <div className="game-over-modal-panel">
                    {/* TITLE */}
                    <div className="game-over-header">
                        <h1 className="game-over-title">GAME OVER</h1>
                    </div>

                    {/* STATS & ACTIONS BODY */}
                    <div className="game-over-body-content">
                        <div className="game-over-stats-grid">
                            {/* 1. BEST HAND */}
                            <div
                                className="game-over-stat-row full-width"
                                onMouseEnter={() => setHoveredStat('bestHand')}
                                onMouseLeave={() => setHoveredStat(null)}
                            >
                                <div className="game-over-pill-label">Best Hand</div>
                                <div className="game-over-pill-value value-best-hand">
                                    <RedChipIcon />
                                    <span className="best-score-text">{bestScore.toLocaleString()}</span>
                                </div>
                                {hoveredStat === 'bestHand' && (
                                    <div className="game-over-tooltip">
                                        Highest scoring single hand ({bestHandName})
                                    </div>
                                )}
                            </div>

                            {/* 2. MOST PLAYED HAND */}
                            <div
                                className="game-over-stat-row full-width"
                                onMouseEnter={() => setHoveredStat('mostPlayed')}
                                onMouseLeave={() => setHoveredStat(null)}
                            >
                                <div className="game-over-pill-label">Most Played Hand</div>
                                <div className="game-over-pill-value value-hand-name">
                                    <span>{mostPlayed}</span>
                                    <span className="hand-count-sub">({mostPlayedCount})</span>
                                </div>
                                {hoveredStat === 'mostPlayed' && (
                                    <div className="game-over-tooltip hands-breakdown">
                                        <div className="tooltip-title">Hands Played Breakdown:</div>
                                        {handsHistory && Object.keys(handsHistory).length > 0 ? (
                                            Object.entries(handsHistory)
                                                .filter(([_, count]) => count > 0)
                                                .map(([name, count]) => (
                                                    <div key={name} className="breakdown-row">
                                                        <span>{name}:</span>
                                                        <strong>{count}</strong>
                                                    </div>
                                                ))
                                        ) : (
                                            <div>{mostPlayed}: {mostPlayedCount} times</div>
                                        )}
                                    </div>
                                )}
                            </div>

                            {/* 3. ANTE / ROUND & DEFEATED BY (SPLIT ROW) */}
                            <div className="game-over-split-row">
                                <div className="game-over-split-left">
                                    <div
                                        className="game-over-stat-row"
                                        onMouseEnter={() => setHoveredStat('ante')}
                                        onMouseLeave={() => setHoveredStat(null)}
                                    >
                                        <div className="game-over-pill-label">Ante</div>
                                        <div className="game-over-pill-value value-badge-dark">{ante}</div>
                                        {hoveredStat === 'ante' && (
                                            <div className="game-over-tooltip">Ante reached before defeat</div>
                                        )}
                                    </div>

                                    <div
                                        className="game-over-stat-row"
                                        onMouseEnter={() => setHoveredStat('round')}
                                        onMouseLeave={() => setHoveredStat(null)}
                                    >
                                        <div className="game-over-pill-label">Round</div>
                                        <div className="game-over-pill-value value-badge-dark">{round}</div>
                                        {hoveredStat === 'round' && (
                                            <div className="game-over-tooltip">Total rounds played</div>
                                        )}
                                    </div>
                                </div>

                                {/* RIGHT SIDE: DEFEATED BY BLIND BOX */}
                                <div
                                    className="game-over-defeated-box"
                                    onMouseEnter={() => setHoveredStat('defeatedBy')}
                                    onMouseLeave={() => setHoveredStat(null)}
                                >
                                    <div className="defeated-label">Defeated By</div>
                                    <div className="defeated-title">{blindTitle}</div>
                                    <div className="defeated-blind-token">
                                        <Blind
                                            blind={blindKey}
                                            width={44}
                                            height={44}
                                            animated={true}
                                        />
                                    </div>
                                    {hoveredStat === 'defeatedBy' && (
                                        <div className="game-over-tooltip">
                                            Run ended by {blindTitle}
                                        </div>
                                    )}
                                </div>
                            </div>
                        </div>

                        {/* BOTTOM ACTION BUTTONS: NEW RUN & MAIN MENU */}
                        <div className="game-over-actions-stacked">
                            <button
                                type="button"
                                className="game-over-action-btn btn-new-run"
                                onClick={onRestart}
                            >
                                New Run
                            </button>

                            <button
                                type="button"
                                className="game-over-action-btn btn-main-menu"
                                onClick={onMainMenu}
                            >
                                Main Menu
                            </button>
                        </div>
                    </div>
                </div>
            </div>
        </div>
    );
}

export default GameOver;