import { useState } from 'react';
import './DebugMenu.css';

function DebugMenu({
    gameState,
    setGameState,
    gameData,
    setGameData,
    onForceWin,
    onForceLose,
    onRoundWin,
    onRoundLose,
    onRestart
}) {
    const [isOpen, setIsOpen] = useState(false);
    const [isMinimized, setIsMinimized] = useState(false);

    // Helper functions for state modification
    const handleSetMoney = (amount) => {
        setGameData(prev => ({
            ...prev,
            money: Math.max(0, prev.money + amount)
        }));
    };

    const handleSetExactMoney = (val) => {
        setGameData(prev => ({
            ...prev,
            money: Math.max(0, val)
        }));
    };

    const handleSetAnte = (anteNum) => {
        setGameData(prev => ({
            ...prev,
            ante: anteNum,
            round: (anteNum - 1) * 3 + prev.blindIndex + 1
        }));
    };

    const handleJumpToAnte8Boss = () => {
        setGameData(prev => ({
            ...prev,
            ante: 8,
            round: 24,
            blindIndex: 2, // Boss blind
            score: 0,
            targetScore: 100000,
            hands: 4,
            discards: 4,
            currentBlind: {
                type: 'boss',
                blind: 'AmberAcorn',
                title: 'Amber Acorn (Boss)',
                score: 100000,
                reward: '$$$$$$'
            }
        }));
        setGameState('gameplay');
    };

    const handleSetHands = (amount) => {
        setGameData(prev => ({
            ...prev,
            hands: Math.max(0, prev.hands + amount)
        }));
    };

    const handleSetDiscards = (amount) => {
        setGameData(prev => ({
            ...prev,
            discards: Math.max(0, prev.discards + amount)
        }));
    };

    return (
        <div className={`debug-menu-wrapper ${isOpen ? 'open' : 'closed'} ${isMinimized ? 'minimized' : ''}`}>
            {/* TOGGLE PILL BUTTON */}
            {!isOpen && (
                <button
                    className="debug-toggle-btn"
                    onClick={() => setIsOpen(true)}
                    title="Open Debug DevTools (Shortcut: Shift + D or F2)"
                >
                    <span className="debug-toggle-icon">🛠️</span>
                    <span className="debug-toggle-label">DEBUG / TEST</span>
                </button>
            )}

            {/* EXPANDED DEBUG PANEL */}
            {isOpen && (
                <div className="debug-panel">
                    <div className="debug-header">
                        <div className="debug-title-row">
                            <span className="debug-header-badge">DEV TOOLS</span>
                            <span className="debug-header-state">State: <strong>{gameState}</strong></span>
                        </div>
                        <div className="debug-header-actions">
                            <button
                                className="debug-min-btn"
                                onClick={() => setIsMinimized(!isMinimized)}
                                title={isMinimized ? "Expand" : "Minimize"}
                            >
                                {isMinimized ? '▲' : '▼'}
                            </button>
                            <button
                                className="debug-close-btn"
                                onClick={() => setIsOpen(false)}
                                title="Close Debug Panel"
                            >
                                ✕
                            </button>
                        </div>
                    </div>

                    {!isMinimized && (
                        <div className="debug-body">
                            {/* SECTION 1: PRIMARY WIN / LOSE OUTCOMES */}
                            <div className="debug-section highlight-section">
                                <div className="debug-section-title">🏆 TEST GAME OUTCOMES</div>
                                <div className="debug-btn-grid">
                                    <button
                                        className="debug-action-btn win-btn"
                                        onClick={onForceWin}
                                        title="Trigger YOU WIN! screen instantly (Shift + W)"
                                    >
                                        <span className="btn-icon">★</span>
                                        <strong>Trigger Win</strong>
                                        <span className="btn-subtext">(YOU WIN!)</span>
                                    </button>

                                    <button
                                        className="debug-action-btn lose-btn"
                                        onClick={onForceLose}
                                        title="Trigger GAME OVER screen instantly (Shift + L)"
                                    >
                                        <span className="btn-icon">☠</span>
                                        <strong>Trigger Game Over</strong>
                                        <span className="btn-subtext">(GAME OVER)</span>
                                    </button>
                                </div>
                            </div>

                            {/* SECTION 2: FAST-FORWARD / JUMP TO ANTE 8 */}
                            <div className="debug-section">
                                <div className="debug-section-title">⏩ ANTE 8 VICTORY TEST</div>
                                <button
                                    className="debug-action-btn jump-ante8-btn"
                                    onClick={handleJumpToAnte8Boss}
                                    title="Jump straight to Ante 8 Boss Blind to test natural win after Shop"
                                >
                                    👑 Jump to Ante 8 Boss Blind (Play & Win)
                                </button>
                            </div>

                            {/* SECTION 3: ROUND OUTCOMES (IN GAMEPLAY) */}
                            <div className="debug-section">
                                <div className="debug-section-title">🃏 ROUND OUTCOMES</div>
                                <div className="debug-btn-row">
                                    <button
                                        className="debug-btn green-btn"
                                        onClick={onRoundWin}
                                        title="Win current round -> Cashout"
                                    >
                                        ✓ Win Round (Cashout)
                                    </button>
                                    <button
                                        className="debug-btn red-btn"
                                        onClick={onRoundLose}
                                        title="Lose current round -> Game Over"
                                    >
                                        ✗ Lose Round (Game Over)
                                    </button>
                                </div>
                            </div>

                            {/* SECTION 4: DIRECT GAME STATE JUMP */}
                            <div className="debug-section">
                                <div className="debug-section-title">🔄 SWITCH GAME STATE</div>
                                <div className="debug-states-grid">
                                    <button
                                        className={`debug-state-btn ${gameState === 'blind-selection' ? 'active' : ''}`}
                                        onClick={() => setGameState('blind-selection')}
                                    >
                                        Blind Select
                                    </button>
                                    <button
                                        className={`debug-state-btn ${gameState === 'gameplay' ? 'active' : ''}`}
                                        onClick={() => setGameState('gameplay')}
                                    >
                                        GameBoard
                                    </button>
                                    <button
                                        className={`debug-state-btn ${gameState === 'cashout' ? 'active' : ''}`}
                                        onClick={() => setGameState('cashout')}
                                    >
                                        Cashout
                                    </button>
                                    <button
                                        className={`debug-state-btn ${gameState === 'shop' ? 'active' : ''}`}
                                        onClick={() => setGameState('shop')}
                                    >
                                        Shop
                                    </button>
                                    <button
                                        className={`debug-state-btn ${gameState === 'win-over' ? 'active' : ''}`}
                                        onClick={() => setGameState('win-over')}
                                    >
                                        Win Screen
                                    </button>
                                    <button
                                        className={`debug-state-btn ${gameState === 'game-over' ? 'active' : ''}`}
                                        onClick={() => setGameState('game-over')}
                                    >
                                        Game Over
                                    </button>
                                </div>
                            </div>

                            {/* SECTION 5: STATS MODIFIERS */}
                            <div className="debug-section">
                                <div className="debug-section-title">💰 ECONOMY & STATS</div>
                                
                                {/* Ante selector */}
                                <div className="debug-stat-row">
                                    <span className="stat-name">Ante ({gameData?.ante || 1}/8):</span>
                                    <div className="stat-btn-group">
                                        {[1, 2, 4, 6, 8].map(num => (
                                            <button
                                                key={num}
                                                className={`stat-pill-btn ${gameData?.ante === num ? 'selected' : ''}`}
                                                onClick={() => handleSetAnte(num)}
                                            >
                                                {num}
                                            </button>
                                        ))}
                                    </div>
                                </div>

                                {/* Money modifier */}
                                <div className="debug-stat-row">
                                    <span className="stat-name">Money (${gameData?.money || 0}):</span>
                                    <div className="stat-btn-group">
                                        <button className="stat-btn" onClick={() => handleSetMoney(10)}>+$10</button>
                                        <button className="stat-btn" onClick={() => handleSetMoney(50)}>+$50</button>
                                        <button className="stat-btn" onClick={() => handleSetMoney(100)}>+$100</button>
                                        <button className="stat-btn" onClick={() => handleSetExactMoney(0)}>Reset $0</button>
                                    </div>
                                </div>

                                {/* Hands & Discards */}
                                <div className="debug-stat-row">
                                    <span className="stat-name">Hands ({gameData?.hands || 0}):</span>
                                    <div className="stat-btn-group">
                                        <button className="stat-btn" onClick={() => handleSetHands(1)}>+1</button>
                                        <button className="stat-btn" onClick={() => handleSetHands(-1)}>-1</button>
                                        <button className="stat-btn" onClick={() => handleSetHands(-gameData.hands)}>Set 0</button>
                                    </div>
                                </div>

                                <div className="debug-stat-row">
                                    <span className="stat-name">Discards ({gameData?.discards || 0}):</span>
                                    <div className="stat-btn-group">
                                        <button className="stat-btn" onClick={() => handleSetDiscards(1)}>+1</button>
                                        <button className="stat-btn" onClick={() => handleSetDiscards(-1)}>-1</button>
                                    </div>
                                </div>
                            </div>

                            {/* SECTION 6: RUN CONTROLS & SHORTCUTS HELP */}
                            <div className="debug-section footer-section">
                                <button className="debug-restart-btn" onClick={onRestart}>
                                    🔄 Reset Run to Ante 1
                                </button>
                                
                                <div className="debug-hotkeys-help">
                                    <div className="hotkey-title">Keyboard Shortcuts:</div>
                                    <div className="hotkey-item"><code>Shift + W</code> : Win Game</div>
                                    <div className="hotkey-item"><code>Shift + L</code> : Game Over</div>
                                    <div className="hotkey-item"><code>Shift + D</code> / <code>F2</code> : Toggle Debug</div>
                                </div>
                            </div>
                        </div>
                    )}
                </div>
            )}
        </div>
    );
}

export default DebugMenu;
