import { useNavigate } from 'react-router-dom';
import { useState, useEffect, useRef } from 'react';

import Balatro from '../components/BalatroBackground/BalatroBackground.jsx';
import bgm from '../assets/music/1-main-theme.mp3';

import BlindSelection from '../components/Gameplay/BlindSelection/BlindSelection.jsx';
import GameBoard from '../components/Gameplay/GameBoard/GameBoard.jsx';
import Cashout from '../components/Gameplay/Cashout/Cashout.jsx';
import Shop from '../components/Gameplay/Shop/Shop.jsx';
import GameOver from '../components/Gameplay/GameOver/GameOver.jsx';
import WinOver from '../components/Gameplay/WinOver/WinOver.jsx';
import OptionsModal from '../components/Gameplay/OptionsModal/OptionsModal.jsx';
import DebugMenu from '../components/Gameplay/DebugMenu/DebugMenu.jsx';

function Gameplay() {

    const navigate = useNavigate();

    // =========================
    // SETTINGS & AUDIO
    // =========================

    const [showSettings, setShowSettings] = useState(false);
    const audioRef = useRef(null);

    const [musicVolume, setMusicVolume] = useState(() => {
        const saved = localStorage.getItem('balatro_music_volume');
        return saved !== null ? parseFloat(saved) : 0.7;
    });

    const [isMusicMuted, setIsMusicMuted] = useState(() => {
        const saved = localStorage.getItem('balatro_music_muted');
        return saved !== null ? saved === 'true' : false;
    });

    const [sfxVolume, setSfxVolume] = useState(() => {
        const saved = localStorage.getItem('balatro_sfx_volume');
        return saved !== null ? parseFloat(saved) : 0.8;
    });

    const [gameSpeed, setGameSpeed] = useState(() => {
        const saved = localStorage.getItem('balatro_game_speed');
        return saved !== null ? parseInt(saved, 10) : 1;
    });

    const [highContrast, setHighContrast] = useState(() => {
        const saved = localStorage.getItem('balatro_high_contrast');
        return saved !== null ? saved === 'true' : false;
    });

    // Handle music volume & mute
    useEffect(() => {
        if (audioRef.current) {
            audioRef.current.volume = isMusicMuted ? 0 : musicVolume;
            if (!isMusicMuted && musicVolume > 0) {
                audioRef.current.play().catch(() => {});
            } else {
                audioRef.current.pause();
            }
        }
        localStorage.setItem('balatro_music_volume', musicVolume.toString());
        localStorage.setItem('balatro_music_muted', isMusicMuted.toString());
    }, [musicVolume, isMusicMuted]);

    // Handle SFX, speed, contrast storage
    useEffect(() => {
        localStorage.setItem('balatro_sfx_volume', sfxVolume.toString());
    }, [sfxVolume]);

    useEffect(() => {
        localStorage.setItem('balatro_game_speed', gameSpeed.toString());
    }, [gameSpeed]);

    useEffect(() => {
        localStorage.setItem('balatro_high_contrast', highContrast.toString());
    }, [highContrast]);

    // Attempt autoplay on mount
    useEffect(() => {
        if (audioRef.current) {
            audioRef.current.volume = isMusicMuted ? 0 : musicVolume;
            if (!isMusicMuted && musicVolume > 0) {
                audioRef.current.play().catch(() => {});
            }
        }
    }, []);


    // =========================
    // GAME STATE
    // =========================

    const GAME_STATE = {
        BLIND_SELECTION: 'blind-selection',
        GAMEPLAY: 'gameplay',
        CASHOUT: 'cashout',
        SHOP: 'shop',
        GAME_OVER: 'game-over',
        WIN_OVER: 'win-over'
    };

    const [gameState, setGameState] = useState(
        GAME_STATE.BLIND_SELECTION
    );


    // =========================
    // GAME DATA
    // =========================

    const [gameData, setGameData] = useState({
        money: 4,

        ante: 1,
        round: 1,
        blindIndex: 0, // 0: Small Blind, 1: Big Blind, 2: Boss Blind

        score: 0,
        targetScore: 300,

        hands: 4,
        discards: 4,

        deckRemaining: 52,

        maxJokers: 5,
        jokers: [
            { id: 'ScaryFace', title: 'Scary Face' },
            { id: 'Joker', title: 'Joker' },
            { id: 'RaisedFist', title: 'Raised Fist' },
            { id: 'AbstractJoker', title: 'Abstract Joker' }
        ],

        maxConsumables: 2,
        consumables: [
            { type: 'tarot', id: 'TheTower', title: 'The Tower' }
        ],

        currentBlind: {
            type: 'small',
            blind: 'SmallBlind',
            title: 'Small Blind',
            score: 300,
            reward: '$$$+'
        },

        currentHandName: '',
        currentHandLevel: 1,
        currentHandChips: 0,
        currentHandMult: 0,
        redeemedVouchers: []
    });


    // =========================
    // GAME FLOW
    // =========================

    function handleSelectBlind(selectedBlind) {
        const blind = selectedBlind || {
            type: 'small',
            blind: 'SmallBlind',
            title: 'Small Blind',
            score: 300,
            reward: '$$$+'
        };

        const target = typeof blind.score === 'number' ? blind.score : parseInt(blind.score, 10) || 300;

        setGameData(prev => ({
            ...prev,
            score: 0,
            hands: 4,
            discards: 4,
            targetScore: target,
            currentBlind: blind
        }));

        setGameState(GAME_STATE.GAMEPLAY);
    }


    function handleSkipBlind() {

        setGameData(prev => ({
            ...prev,
            money: prev.money + 2
        }));

        setGameState(GAME_STATE.CASHOUT);
    }


    function handleRoundWin() {

        setGameState(GAME_STATE.CASHOUT);
    }


    function handleRoundLose() {

        setGameState(GAME_STATE.GAME_OVER);
    }


    function handleCashout(earnedAmount) {
        const amount = typeof earnedAmount === 'number' ? earnedAmount : 4;

        setGameData(prev => ({
            ...prev,
            money: prev.money + amount
        }));

        setGameState(GAME_STATE.SHOP);
    }


    function handleLeaveShop() {

        if (gameData.blindIndex >= 2) {
            // Completed the Boss Blind of current Ante
            if (gameData.ante >= 8) {
                setGameState(GAME_STATE.WIN_OVER);
                return;
            }

            // Move to next Ante
            setGameData(prev => ({
                ...prev,
                ante: prev.ante + 1,
                round: prev.round + 1,
                blindIndex: 0,
                score: 0,
                hands: 4,
                discards: 4
            }));
        } else {
            // Move to next Blind in current Ante
            setGameData(prev => ({
                ...prev,
                round: prev.round + 1,
                blindIndex: prev.blindIndex + 1,
                score: 0,
                hands: 4,
                discards: 4
            }));
        }

        setGameState(GAME_STATE.BLIND_SELECTION);
    }


    function handleForceWin() {
        setGameState(GAME_STATE.WIN_OVER);
    }

    function handleForceLose() {
        setGameState(GAME_STATE.GAME_OVER);
    }

    function handleJumpToAnte8Boss() {
        setGameData(prev => ({
            ...prev,
            ante: 8,
            round: 24,
            blindIndex: 2,
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
        setGameState(GAME_STATE.GAMEPLAY);
    }

    // Global keyboard shortcuts for debug & testing
    useEffect(() => {
        const handleKeyDown = (e) => {
            if (['INPUT', 'TEXTAREA'].includes(e.target?.tagName)) return;

            if (e.shiftKey && (e.key === 'W' || e.key === 'w')) {
                e.preventDefault();
                handleForceWin();
            } else if (e.shiftKey && (e.key === 'L' || e.key === 'l')) {
                e.preventDefault();
                handleForceLose();
            } else if (e.shiftKey && (e.key === 'R' || e.key === 'r')) {
                e.preventDefault();
                handleRestart();
            }
        };

        window.addEventListener('keydown', handleKeyDown);
        return () => window.removeEventListener('keydown', handleKeyDown);
    }, []);

    function handleRestart() {

        setGameData({
            money: 4,

            ante: 1,
            round: 1,
            blindIndex: 0,

            score: 0,
            targetScore: 300,

            hands: 4,
            discards: 4,

            deckRemaining: 52,

            maxJokers: 5,
            jokers: [
                { id: 'ScaryFace', title: 'Scary Face' },
                { id: 'Joker', title: 'Joker' },
                { id: 'RaisedFist', title: 'Raised Fist' },
                { id: 'AbstractJoker', title: 'Abstract Joker' }
            ],

            maxConsumables: 2,
            consumables: [
                { type: 'tarot', id: 'TheTower', title: 'The Tower' }
            ],

            currentBlind: {
                type: 'small',
                blind: 'SmallBlind',
                title: 'Small Blind',
                score: 300,
                reward: '$$$+'
            },

            currentHandName: '',
            currentHandLevel: 1,
            currentHandChips: 0,
            currentHandMult: 0,
            redeemedVouchers: []
        });

        setGameState(GAME_STATE.BLIND_SELECTION);
    }


    return (

        <div
            style={{
                position: 'relative',
                width: '100vw',
                height: '100vh',
                minHeight: '100vh',
                overflow: 'hidden',
                color: 'white'
            }}
        >

            {/* =====================================
                BALATRO BACKGROUND
            ====================================== */}

            <div
                style={{
                    position: 'fixed',
                    inset: 0,
                    zIndex: -1
                }}
            >

                <Balatro
                    theme="green"
                    spinRotation={-0.5}
                    spinSpeed={1.5}
                    spinAmount={0.08}
                    spinEase={0.5}
                    contrast={2.5}
                    lighting={0.25}
                    pixelFilter={1000}
                    isRotate={false}
                    mouseInteraction={false}
                />

            </div>


            {/* =====================================
                GAMEPLAY UI
            ====================================== */}

            <div
                style={{
                    position: 'relative',
                    width: '100%',
                    height: '100%',
                    overflow: 'hidden'
                }}
            >

                {/* =================================
                    CURRENT GAME STATE
                ================================== */}

                {gameState === GAME_STATE.BLIND_SELECTION && (
                    <BlindSelection
                        gameData={gameData}
                        onSelectBlind={handleSelectBlind}
                        onOpenSettings={() => setShowSettings(true)}
                    />
                )}


                {gameState === GAME_STATE.GAMEPLAY && (

                    <GameBoard
                        gameData={gameData}
                        onWin={handleRoundWin}
                        onLose={handleRoundLose}
                        onOpenSettings={() => setShowSettings(true)}
                    />

                )}


                {gameState === GAME_STATE.CASHOUT && (

                    <Cashout
                        gameData={gameData}
                        onContinue={handleCashout}
                        onOpenSettings={() => setShowSettings(true)}
                    />

                )}


                {gameState === GAME_STATE.SHOP && (

                    <Shop
                        gameData={gameData}
                        onContinue={handleLeaveShop}
                        onOpenSettings={() => setShowSettings(true)}
                    />

                )}


                {gameState === GAME_STATE.GAME_OVER && (

                    <GameOver
                        gameData={gameData}
                        onRestart={handleRestart}
                    />

                )}


                {gameState === GAME_STATE.WIN_OVER && (

                    <WinOver
                        gameData={gameData}
                        onRestart={handleRestart}
                    />

                )}

            </div>


            {/* =====================================
                AUDIO ELEMENT
            ====================================== */}
            <audio
                ref={audioRef}
                src={bgm}
                loop
                preload="auto"
            />

            {/* =====================================
                OPTIONS / SETTINGS MODAL
            ====================================== */}
            <OptionsModal
                isOpen={showSettings}
                onClose={() => setShowSettings(false)}
                onMainMenu={() => navigate('/')}
                musicVolume={musicVolume}
                setMusicVolume={setMusicVolume}
                isMusicMuted={isMusicMuted}
                setIsMusicMuted={setIsMusicMuted}
                sfxVolume={sfxVolume}
                setSfxVolume={setSfxVolume}
                gameSpeed={gameSpeed}
                setGameSpeed={setGameSpeed}
                highContrast={highContrast}
                setHighContrast={setHighContrast}
                onForceWin={handleForceWin}
                onForceLose={handleForceLose}
                onJumpAnte8={handleJumpToAnte8Boss}
            />

            {/* =====================================
                DEV / DEBUG TESTING MENU
            ====================================== */}
            <DebugMenu
                gameState={gameState}
                setGameState={setGameState}
                gameData={gameData}
                setGameData={setGameData}
                onForceWin={handleForceWin}
                onForceLose={handleForceLose}
                onRoundWin={handleRoundWin}
                onRoundLose={handleRoundLose}
                onRestart={handleRestart}
            />

        </div>
    );
}

export default Gameplay;