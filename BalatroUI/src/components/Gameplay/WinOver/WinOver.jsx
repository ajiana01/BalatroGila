import { useState } from 'react';
import Sprite from '../../Sprite/Sprite';
import { jokerSprite } from '../../../data/sprites/jokerSprites';
import './WinOver.css';

function RedChipIcon() {
    return (
        <svg width="18" height="18" viewBox="0 0 24 24" fill="none" className="win-chip-icon">
            <circle cx="12" cy="12" r="10" fill="#e74c3c" stroke="#b02626" strokeWidth="2" />
            <circle cx="12" cy="12" r="7" stroke="#ffffff" strokeWidth="1.5" strokeDasharray="3 2" />
            <circle cx="12" cy="12" r="3.5" fill="#ffffff" />
        </svg>
    );
}

const VICTORY_QUOTES = [
    "You made some heads up plays!",
    "What a legendary run!",
    "Ante 8 defeated!",
    "The house never saw it coming!",
    "Calculated risks paid off!",
    "Pure Balatro mastery!"
];

const CONFETTI_PIECES = [
    { color: '#ff4757', left: '10%', top: '15%', size: 9, delay: '0s', rot: 25 },
    { color: '#2ed573', left: '85%', top: '20%', size: 11, delay: '0.4s', rot: -40 },
    { color: '#ffa502', left: '15%', top: '65%', size: 8, delay: '0.8s', rot: 60 },
    { color: '#1e90ff', left: '78%', top: '75%', size: 10, delay: '1.2s', rot: -15 },
    { color: '#e056fd', left: '25%', top: '85%', size: 12, delay: '0.2s', rot: 45 },
    { color: '#ff6348', left: '6%', top: '40%', size: 10, delay: '0.6s', rot: 30 },
    { color: '#70a1ff', left: '92%', top: '45%', size: 8, delay: '1.0s', rot: -50 },
    { color: '#ffd32a', left: '4%', top: '80%', size: 11, delay: '1.4s', rot: 15 },
    { color: '#0be881', left: '88%', top: '10%', size: 9, delay: '0.7s', rot: -20 },
    { color: '#ff5e57', left: '22%', top: '30%', size: 10, delay: '0.3s', rot: 75 }
];

function WinOver({
    gameData = {},
    onRestart,
    onMainMenu
}) {
    const [quoteIndex, setQuoteIndex] = useState(0);
    const [hoveredStat, setHoveredStat] = useState(null);

    const stats = gameData.stats || {};

    const ante = gameData.ante || gameData.currentAnte || 8;
    const round = gameData.round || gameData.currentRound || 24;
    const bestScore = stats.bestHandScore ?? (gameData.score ?? 0);
    const bestHandName = stats.bestHandName || 'High Card';
    const mostPlayed = stats.mostPlayedHand || 'None';
    const mostPlayedCount = stats.mostPlayedCount ?? 0;
    const handsHistory = stats.handsHistory || {};

    const handleNextQuote = () => {
        setQuoteIndex((prev) => (prev + 1) % VICTORY_QUOTES.length);
    };

    return (
        <div className="win-over-backdrop">
            {/* BACKGROUND CONFETTI PARTICLES */}
            <div className="win-confetti-container" aria-hidden="true">
                {CONFETTI_PIECES.map((piece, i) => (
                    <div
                        key={i}
                        className="win-confetti-piece"
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

            <div className="win-over-layout">
                {/* LEFT MASCOT SECTION (JIMBO & SPEECH BUBBLE) */}
                <div className="win-mascot-wrapper" onClick={handleNextQuote} title="Click to hear Jimbo!">
                    <div className="win-jimbo-card">
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
                        <div className="jimbo-confetti c4" />
                    </div>

                    <div className="win-speech-bubble">
                        <p>{VICTORY_QUOTES[quoteIndex]}</p>
                        <div className="speech-bubble-tail" />
                    </div>
                </div>

                {/* MAIN "YOU WIN!" MODAL PANEL */}
                <div className="win-modal-panel">
                    {/* TITLE */}
                    <div className="win-header">
                        <h1 className="win-title">YOU WIN!</h1>
                    </div>

                    {/* STATS & ACTIONS BODY */}
                    <div className="win-body-content">
                        <div className="win-stats-grid">
                            {/* 1. BEST HAND */}
                            <div
                                className="win-stat-row full-width"
                                onMouseEnter={() => setHoveredStat('bestHand')}
                                onMouseLeave={() => setHoveredStat(null)}
                            >
                                <div className="win-pill-label">Best Hand</div>
                                <div className="win-pill-value value-best-hand">
                                    <RedChipIcon />
                                    <span className="best-score-text">{bestScore.toLocaleString()}</span>
                                    <span className="high-score-badge top-right">High Score!</span>
                                </div>
                                {hoveredStat === 'bestHand' && (
                                    <div className="win-tooltip">
                                        Highest scoring single hand in this run ({bestHandName})
                                    </div>
                                )}
                            </div>

                            {/* 2. MOST PLAYED HAND */}
                            <div
                                className="win-stat-row full-width"
                                onMouseEnter={() => setHoveredStat('mostPlayed')}
                                onMouseLeave={() => setHoveredStat(null)}
                            >
                                <div className="win-pill-label">Most Played Hand</div>
                                <div className="win-pill-value value-hand-name">
                                    <span>{mostPlayed}</span>
                                    <span className="hand-count-sub">({mostPlayedCount})</span>
                                </div>
                                {hoveredStat === 'mostPlayed' && (
                                    <div className="win-tooltip hands-breakdown">
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

                            {/* 3. ANTE & ROUND (2 COLS) */}
                            <div className="win-two-col-row">
                                <div
                                    className="win-stat-col"
                                    onMouseEnter={() => setHoveredStat('ante')}
                                    onMouseLeave={() => setHoveredStat(null)}
                                >
                                    <div className="win-pill-label">Ante</div>
                                    <div className="win-pill-value value-badge-dark">
                                        {ante}
                                        <span className="high-score-badge">Conquered!</span>
                                    </div>
                                    {hoveredStat === 'ante' && (
                                        <div className="win-tooltip">Highest Ante reached (Ante 8 Boss conquered)</div>
                                    )}
                                </div>

                                <div
                                    className="win-stat-col"
                                    onMouseEnter={() => setHoveredStat('round')}
                                    onMouseLeave={() => setHoveredStat(null)}
                                >
                                    <div className="win-pill-label">Round</div>
                                    <div className="win-pill-value value-badge-dark">
                                        {round}
                                    </div>
                                    {hoveredStat === 'round' && (
                                        <div className="win-tooltip">Total rounds completed in this run</div>
                                    )}
                                </div>
                            </div>

                            {/* 4. ACTION BUTTONS */}
                            <div className="win-actions-stacked">
                                <button
                                    type="button"
                                    className="win-action-btn btn-new-run"
                                    onClick={onRestart}
                                >
                                    New Run
                                </button>

                                <button
                                    type="button"
                                    className="win-action-btn btn-main-menu"
                                    onClick={onMainMenu}
                                >
                                    Main Menu
                                </button>
                            </div>
                        </div>
                    </div>
                </div>
            </div>
        </div>
    );
}

export default WinOver;