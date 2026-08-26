import { useEffect, useState } from 'react';
import './OptionsModal.css';

function OptionsModal({
    isOpen,
    onClose,
    onMainMenu,
    musicVolume = 0.7,
    setMusicVolume,
    isMusicMuted = false,
    setIsMusicMuted,
    sfxVolume = 0.8,
    setSfxVolume,
    gameSpeed = 1,
    setGameSpeed,
    highContrast = false,
    setHighContrast,
    onForceWin,
    onForceLose,
    onJumpAnte8
}) {
    const [localMusicVol, setLocalMusicVol] = useState(Math.round(musicVolume * 100));
    const [localMusicMuted, setLocalMusicMuted] = useState(isMusicMuted);
    const [localSfxVol, setLocalSfxVol] = useState(Math.round(sfxVolume * 100));
    const [localGameSpeed, setLocalGameSpeed] = useState(gameSpeed);
    const [localHighContrast, setLocalHighContrast] = useState(highContrast);

    // Sync with props
    useEffect(() => {
        setLocalMusicVol(Math.round(musicVolume * 100));
        setLocalMusicMuted(isMusicMuted);
        setLocalSfxVol(Math.round(sfxVolume * 100));
        setLocalGameSpeed(gameSpeed);
        setLocalHighContrast(highContrast);
    }, [musicVolume, isMusicMuted, sfxVolume, gameSpeed, highContrast, isOpen]);

    // Handle Escape key
    useEffect(() => {
        if (!isOpen) return;

        const handleKeyDown = (e) => {
            if (e.key === 'Escape') {
                onClose();
            }
        };

        window.addEventListener('keydown', handleKeyDown);
        return () => window.removeEventListener('keydown', handleKeyDown);
    }, [isOpen, onClose]);

    if (!isOpen) return null;

    const handleMusicVolChange = (val) => {
        const clamped = Math.max(0, Math.min(100, val));
        setLocalMusicVol(clamped);
        if (setMusicVolume) {
            setMusicVolume(clamped / 100);
        }
        if (clamped > 0 && localMusicMuted && setIsMusicMuted) {
            setLocalMusicMuted(false);
            setIsMusicMuted(false);
        }
    };

    const handleToggleMute = () => {
        const next = !localMusicMuted;
        setLocalMusicMuted(next);
        if (setIsMusicMuted) {
            setIsMusicMuted(next);
        }
    };

    const handleSfxVolChange = (val) => {
        const clamped = Math.max(0, Math.min(100, val));
        setLocalSfxVol(clamped);
        if (setSfxVolume) {
            setSfxVolume(clamped / 100);
        }
    };

    const handleCycleSpeed = () => {
        const speeds = [1, 2, 4];
        const nextIdx = (speeds.indexOf(localGameSpeed) + 1) % speeds.length;
        const nextSpeed = speeds[nextIdx];
        setLocalGameSpeed(nextSpeed);
        if (setGameSpeed) {
            setGameSpeed(nextSpeed);
        }
    };

    const handleToggleHighContrast = () => {
        const next = !localHighContrast;
        setLocalHighContrast(next);
        if (setHighContrast) {
            setHighContrast(next);
        }
    };

    return (
        <div className="options-modal-backdrop" onClick={onClose}>
            <div className="options-modal-container" onClick={(e) => e.stopPropagation()}>
                {/* HEADER */}
                <div className="options-modal-header">
                    <div className="options-title-pill">
                        OPTIONS
                    </div>
                </div>

                {/* SETTINGS GROUPS */}
                <div className="options-modal-body">
                    {/* SECTION 1: AUDIO SETTINGS */}
                    <div className="options-group">
                        <div className="group-label">AUDIO & MUSIC</div>

                        {/* Music Mute Toggle */}
                        <div className="option-row">
                            <span className="option-name">Music</span>
                            <button
                                className={`option-toggle-btn ${!localMusicMuted ? 'active-green' : 'inactive-red'}`}
                                onClick={handleToggleMute}
                            >
                                {!localMusicMuted ? '🔊 ON' : '🔇 OFF'}
                            </button>
                        </div>

                        {/* Music Volume Slider */}
                        <div className="option-row">
                            <span className="option-name">Music Volume</span>
                            <div className="slider-control-box">
                                <button
                                    className="slider-step-btn"
                                    onClick={() => handleMusicVolChange(localMusicVol - 10)}
                                    disabled={localMusicVol <= 0}
                                >
                                    -
                                </button>
                                <div className="slider-track-wrapper">
                                    <input
                                        type="range"
                                        min="0"
                                        max="100"
                                        step="5"
                                        value={localMusicMuted ? 0 : localMusicVol}
                                        onChange={(e) => handleMusicVolChange(Number(e.target.value))}
                                        className="options-range-slider"
                                    />
                                    <div className="slider-val-text">
                                        {localMusicMuted ? '0%' : `${localMusicVol}%`}
                                    </div>
                                </div>
                                <button
                                    className="slider-step-btn"
                                    onClick={() => handleMusicVolChange(localMusicVol + 10)}
                                    disabled={localMusicVol >= 100}
                                >
                                    +
                                </button>
                            </div>
                        </div>

                        {/* SFX Volume */}
                        <div className="option-row">
                            <span className="option-name">Sound Effects</span>
                            <div className="slider-control-box">
                                <button
                                    className="slider-step-btn"
                                    onClick={() => handleSfxVolChange(localSfxVol - 10)}
                                    disabled={localSfxVol <= 0}
                                >
                                    -
                                </button>
                                <div className="slider-track-wrapper">
                                    <input
                                        type="range"
                                        min="0"
                                        max="100"
                                        step="5"
                                        value={localSfxVol}
                                        onChange={(e) => handleSfxVolChange(Number(e.target.value))}
                                        className="options-range-slider"
                                    />
                                    <div className="slider-val-text">
                                        {`${localSfxVol}%`}
                                    </div>
                                </div>
                                <button
                                    className="slider-step-btn"
                                    onClick={() => handleSfxVolChange(localSfxVol + 10)}
                                    disabled={localSfxVol >= 100}
                                >
                                    +
                                </button>
                            </div>
                        </div>
                    </div>

                    {/* SECTION 2: GAMEPLAY SETTINGS */}
                    <div className="options-group">
                        <div className="group-label">GAMEPLAY</div>

                        {/* Game Speed */}
                        <div className="option-row">
                            <span className="option-name">Game Speed</span>
                            <button className="option-cycle-btn" onClick={handleCycleSpeed}>
                                {localGameSpeed}X
                            </button>
                        </div>

                        {/* High Contrast Cards */}
                        <div className="option-row">
                            <span className="option-name">High Contrast Cards</span>
                            <button
                                className={`option-toggle-btn ${localHighContrast ? 'active-green' : 'inactive-gray'}`}
                                onClick={handleToggleHighContrast}
                            >
                                {localHighContrast ? 'ON' : 'OFF'}
                            </button>
                        </div>
                    </div>

                    {/* SECTION 3: QUICK DEBUG & TESTING */}
                    {(onForceWin || onForceLose || onJumpAnte8) && (
                        <div className="options-group debug-options-group">
                            <div className="group-label" style={{ color: '#ff9d00' }}>🛠️ DEBUG & TESTING</div>
                            <div className="option-row debug-buttons-row" style={{ gap: '8px' }}>
                                {onForceWin && (
                                    <button
                                        className="options-debug-btn win"
                                        onClick={() => {
                                            onClose();
                                            onForceWin();
                                        }}
                                        title="Trigger YOU WIN! screen"
                                    >
                                        ★ Trigger Win
                                    </button>
                                )}
                                {onForceLose && (
                                    <button
                                        className="options-debug-btn lose"
                                        onClick={() => {
                                            onClose();
                                            onForceLose();
                                        }}
                                        title="Trigger GAME OVER screen"
                                    >
                                        ☠ Game Over
                                    </button>
                                )}
                                {onJumpAnte8 && (
                                    <button
                                        className="options-debug-btn jump"
                                        onClick={() => {
                                            onClose();
                                            onJumpAnte8();
                                        }}
                                        title="Jump to Ante 8 Boss Blind"
                                    >
                                        👑 Ante 8 Boss
                                    </button>
                                )}
                            </div>
                        </div>
                    )}
                </div>

                {/* BOTTOM ACTION BUTTONS */}
                <div className="options-modal-footer">
                    <button className="options-btn btn-main-menu" onClick={onMainMenu}>
                        Main Menu
                    </button>
                    <button className="options-btn btn-resume" onClick={onClose}>
                        Back to Game
                    </button>
                </div>
            </div>
        </div>
    );
}

export default OptionsModal;
