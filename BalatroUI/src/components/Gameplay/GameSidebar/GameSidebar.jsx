import { useState } from 'react';
import RunInfoModal from './RunInfoModal';
import ShopSign from '../../ShopSign/ShopSign';
import Blind from '../../Blind/Blind';
import './GameSidebar.css';

// SVG Icons for authentic Balatro look
function PokerChipIcon() {
    return (
        <svg className="poker-chip-svg" width="22" height="22" viewBox="0 0 24 24" fill="currentColor">
            <circle cx="12" cy="12" r="10.5" fill="#e8edf0" />
            <circle cx="12" cy="12" r="7.5" fill="none" stroke="#243033" strokeWidth="2" strokeDasharray="3.8 2.5" />
            <circle cx="12" cy="12" r="4" fill="#e8edf0" />
        </svg>
    );
}

function MenuIcon() {
    return (
        <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2.5" strokeLinecap="round">
            <circle cx="12" cy="12" r="10" stroke="currentColor" strokeWidth="2" fill="rgba(0,0,0,0.25)" />
            <line x1="7.5" y1="8.5" x2="16.5" y2="8.5" stroke="white" />
            <line x1="7.5" y1="12" x2="16.5" y2="12" stroke="white" />
            <line x1="7.5" y1="15.5" x2="16.5" y2="15.5" stroke="white" />
        </svg>
    );
}

function RunInfoIcon() {
    return (
        <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2.5" strokeLinecap="round">
            <circle cx="12" cy="12" r="10" stroke="currentColor" strokeWidth="2" fill="rgba(0,0,0,0.25)" />
            <rect x="7" y="7" width="6" height="8" rx="1" fill="none" stroke="white" strokeWidth="2" />
            <rect x="11" y="9" width="6" height="8" rx="1" fill="none" stroke="white" strokeWidth="2" />
        </svg>
    );
}

function GameSidebar({
    gameData,
    onOpenSettings,
    isBlindSelection = false,
    isShop = false,
    isCashout = false
}) {
    const [isRunInfoOpen, setIsRunInfoOpen] = useState(false);

    const isOutsideRound = isShop || isCashout || isBlindSelection;

    const blind = gameData?.currentBlind || {
        type: 'big',
        title: 'Big Blind',
        score: gameData?.targetScore || 450,
        reward: '$$$$'
    };

    const blindType = blind.type || 'big';
    const blindTitle = blind.title || 'Big Blind';
    const blindScore = blind.score || gameData?.targetScore || 450;
    const blindReward = blind.reward || '$$$$';
    const blindKey = blind.blind || (
        blindType === 'small' ? 'SmallBlind' :
        blindType === 'big' ? 'BigBlind' : 'TheGoad'
    );

    const displayRoundScore = isOutsideRound ? 0 : (gameData?.score || 0);
    const displayHands = isOutsideRound ? (gameData?.maxHands || 4) : (gameData?.hands ?? (gameData?.maxHands || 4));
    const displayDiscards = isOutsideRound ? (gameData?.maxDiscards || 3) : (gameData?.discards ?? (gameData?.maxDiscards || 3));
    const displayChips = isOutsideRound ? 0 : (gameData?.currentHandChips || 0);
    const displayMult = isOutsideRound ? 0 : (gameData?.currentHandMult || 0);

    return (
        <aside className="game-sidebar">
            {/* TOP SECTION: Shop Sign OR Choose Blind OR Active Blind */}
            {isShop ? (
                <div className="sidebar-shop-sign-container">
                    <ShopSign
                        width={210}
                        height={106}
                        animated={true}
                        fps={6}
                    />
                </div>
            ) : isBlindSelection ? (
                <div className="sidebar-blind-section selection-mode">
                    <div className="sidebar-selection-title">
                        <span>Choose your</span>
                        <span>next Blind</span>
                    </div>
                </div>
            ) : (
                <div className={`sidebar-blind-section blind-${blindType}`}>
                    <div className="blind-header-pill">
                        {blindTitle}
                    </div>

                    <div className="blind-info-box">
                        <div className="blind-token-visual">
                            <Blind
                                blind={blindKey}
                                width={62}
                                height={62}
                                animated={true}
                            />
                        </div>

                        <div className="blind-score-pill">
                            <span className="score-label">Score at least</span>
                            <div className="score-target-row">
                                <PokerChipIcon />
                                <span className="target-number">{blindScore}</span>
                            </div>
                            <span className="reward-label">Reward: {blindReward}</span>
                        </div>
                    </div>

                    {blind.description && (
                        <div className="blind-debuff-description">
                            {blind.description}
                        </div>
                    )}
                </div>
            )}

            {/* ROUND SCORE */}
            <div className="sidebar-box round-score-box">
                <div className="round-score-label">
                    <span>Round</span>
                    <span>score</span>
                </div>

                <div className="round-score-value">
                    <PokerChipIcon />
                    <strong>{displayRoundScore}</strong>
                </div>
            </div>

            {/* POKER HAND & MULTIPLIER */}
            <div className={`sidebar-box hand-eval-box ${isOutsideRound ? 'shop-eval-box' : ''}`}>
                {!isOutsideRound && (
                    <div className="hand-name-lvl">
                        {gameData?.currentHandName ? (
                            <>
                                {gameData.currentHandName} <span className="hand-lvl">lvl.{gameData.currentHandLevel || 1}</span>
                            </>
                        ) : (
                            '\u00A0'
                        )}
                    </div>
                )}

                <div className="chips-mult-row">
                    <div className="chips-box">
                        {displayChips}
                    </div>

                    <span className="mult-sign">X</span>

                    <div className="mult-box">
                        {displayMult}
                    </div>
                </div>
            </div>

            {/* BOTTOM 2-COLUMN GRID */}
            <div className="sidebar-bottom-grid">
                {/* Left Column: Action Buttons */}
                <div className="sidebar-left-col">
                    <button
                        className="sidebar-btn btn-run-info"
                        title="Run Info"
                        onClick={() => setIsRunInfoOpen(true)}
                    >
                        <span className="btn-text">Run Info</span>
                        <RunInfoIcon />
                    </button>

                    <button
                        className="sidebar-btn btn-options"
                        onClick={onOpenSettings}
                        title="Options"
                    >
                        <span className="btn-text">Options</span>
                        <MenuIcon />
                    </button>
                </div>

                {/* Right Column: Stats */}
                <div className="sidebar-right-col">
                    {/* Row 1: Hands & Discards */}
                    <div className="stat-split-box">
                        <div className="split-item hands-stat">
                            <span className="stat-label">Hands</span>
                            <strong className="stat-val blue">{displayHands}</strong>
                        </div>
                        <div className="split-item discards-stat">
                            <span className="stat-label">Discards</span>
                            <strong className="stat-val red">{displayDiscards}</strong>
                        </div>
                    </div>

                    {/* Row 2: Money */}
                    <div className="money-box">
                        <strong>${gameData?.money ?? 0}</strong>
                    </div>

                    {/* Row 3: Ante & Round */}
                    <div className="stat-split-box">
                        <div className="split-item ante-stat">
                            <span className="stat-label">Ante</span>
                            <strong className="stat-val orange">
                                {gameData?.ante ?? 1} <span className="stat-sub">/ 8</span>
                            </strong>
                        </div>
                        <div className="split-item round-stat">
                            <span className="stat-label">Round</span>
                            <strong className="stat-val orange">{gameData?.round ?? 1}</strong>
                        </div>
                    </div>
                </div>
            </div>

            {/* RUN INFO MODAL */}
            <RunInfoModal
                isOpen={isRunInfoOpen}
                onClose={() => setIsRunInfoOpen(false)}
                gameData={gameData}
            />
        </aside>
    );
}

export default GameSidebar;