import { useNavigate } from 'react-router-dom';
import { useState, useEffect, useRef, useCallback } from 'react';

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

import { startGame, getGameState, selectBlind, generateSessionId } from '../services/api.js';
import { mapBackendCards, mapBackendJokers, mapBackendConsumables, mapBackendBlind, mapBackendBlinds } from '../utils/cardMapper.js';

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
    // GAME STATE & PHASES
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

    const [isLoading, setIsLoading] = useState(false);
    const [toastMessage, setToastMessage] = useState('');

    const showToast = (msg) => {
        setToastMessage(msg);
        setTimeout(() => {
            setToastMessage('');
        }, 2500);
    };

    // =========================
    // GAME DATA
    // =========================

    const DEFAULT_GAME_STATS = {
        bestHandScore: 0,
        bestHandName: 'High Card',
        mostPlayedHand: 'None',
        mostPlayedCount: 0,
        cardsPlayed: 0,
        cardsDiscarded: 0,
        cardsPurchased: 0,
        timesRerolled: 0,
        handsHistory: {}
    };

    const [gameData, setGameData] = useState({
        money: 4,
        ante: 1,
        maxAnte: 8,
        round: 1,
        blindIndex: 0,

        score: 0,
        targetScore: 300,

        hands: 4,
        maxHands: 4,
        discards: 4,
        maxDiscards: 4,

        deckRemaining: 52,
        handCards: [],

        maxJokers: 5,
        jokers: [],

        maxConsumables: 2,
        consumables: [],

        currentBlind: {
            id: 1,
            type: 'small',
            blind: 'SmallBlind',
            title: 'Small Blind',
            score: 300,
            reward: '$$$+'
        },
        availableBlinds: [],

        currentHandName: '',
        currentHandLevel: 1,
        currentHandChips: 0,
        currentHandMult: 0,
        redeemedVouchers: [],
        shop: null,
        stats: { ...DEFAULT_GAME_STATS },
        isEndless: false
    });

    // =========================
    // SYNC STATE HELPER
    // =========================

    const syncGameData = useCallback((apiState) => {
        if (!apiState) return;

        const mappedHandCards = mapBackendCards(apiState.hand);
        const mappedFullDeck = mapBackendCards(apiState.fullDeck);
        const mappedRemainingCards = mapBackendCards(apiState.remainingCards);
        const mappedJokers = mapBackendJokers(apiState.jokers);
        const mappedConsumables = mapBackendConsumables(apiState.consumables);
        const mappedBlind = apiState.currentBlind ? mapBackendBlind(apiState.currentBlind) : null;
        const mappedAvailableBlinds = mapBackendBlinds(apiState.availableBlinds);

        setGameData(prev => {
            const newStats = { ...(prev.stats || DEFAULT_GAME_STATS) };

            if (apiState.pokerHandPlayed) {
                newStats.handsHistory = { ...(newStats.handsHistory || {}), ...apiState.pokerHandPlayed };
                let maxCount = 0;
                let mostPlayed = 'High Card';
                for (const [h, count] of Object.entries(apiState.pokerHandPlayed)) {
                    if (count > maxCount) {
                        maxCount = count;
                        mostPlayed = h;
                    }
                }
                newStats.mostPlayedHand = mostPlayed;
                newStats.mostPlayedCount = maxCount;
            }

            if (apiState.lastScoreResult?.finalScore && apiState.lastScoreResult.finalScore > (newStats.bestHandScore || 0)) {
                newStats.bestHandScore = apiState.lastScoreResult.finalScore;
                newStats.bestHandName = apiState.lastScoreResult.handName;
            }

            const totalDeck = apiState.deckRemainingCount ?? (apiState.drawPileCount + apiState.discardPileCount);

            return {
                ...prev,
                money: apiState.money,
                ante: apiState.currentAnte,
                maxAnte: apiState.maxAnte || 8,
                round: apiState.currentRound,
                blindIndex: apiState.currentBlind ? (apiState.currentBlind.id - 1) : prev.blindIndex,
                score: apiState.currentScore,
                targetScore: apiState.targetScore || (mappedBlind?.score || prev.targetScore),
                hands: apiState.handsRemaining,
                maxHands: apiState.maxHands,
                discards: apiState.discardsRemaining,
                maxDiscards: apiState.maxDiscards,
                deckRemaining: totalDeck || mappedRemainingCards?.length || 52,
                totalDeckCount: mappedFullDeck?.length || 52,
                fullDeck: mappedFullDeck?.length ? mappedFullDeck : (prev.fullDeck || []),
                remainingCards: mappedRemainingCards?.length ? mappedRemainingCards : (prev.remainingCards || []),
                handCards: mappedHandCards,
                jokers: mappedJokers,
                maxJokers: apiState.maxJokers || 5,
                consumables: mappedConsumables,
                maxConsumables: apiState.maxConsumables || 2,
                currentBlind: mappedBlind || prev.currentBlind,
                availableBlinds: mappedAvailableBlinds,
                redeemedVouchers: (apiState.purchasedVouchers || []).map(v => v.effect || v.name),
                shop: apiState.shop,
                handLevels: apiState.pokerHandLevels || apiState.PokerHandLevels || prev.handLevels || {},
                pokerHandPlayed: apiState.pokerHandPlayed || apiState.PokerHandPlayed || prev.pokerHandPlayed || {},
                stats: newStats
            };
        });
    }, []);

    // Initial game start on mount
    useEffect(() => {
        async function initGame() {
            try {
                setIsLoading(true);
                generateSessionId();
                const state = await startGame('Player 1');
                syncGameData(state);
            } catch (err) {
                console.error('Failed to start game:', err);
                showToast(`Koneksi API: ${err.message}`);
            } finally {
                setIsLoading(false);
            }
        }
        initGame();
    }, [syncGameData]);

    // =========================
    // GAME FLOW ACTIONS
    // =========================

    async function handleSelectBlind(selectedBlind) {
        try {
            setIsLoading(true);
            const blindId = selectedBlind?.id || 1;
            const state = await selectBlind(blindId);
            syncGameData(state);
            setGameState(GAME_STATE.GAMEPLAY);
        } catch (err) {
            console.error('Failed to select blind:', err);
            showToast(err.message);
            // Fallback for visual transition if offline
            setGameState(GAME_STATE.GAMEPLAY);
        } finally {
            setIsLoading(false);
        }
    }

    function handleRoundWin() {
        const isFinalBoss = (gameData.ante >= 8 && (gameData.blindIndex >= 2 || gameData.currentBlind?.type === 'boss')) && !gameData.isEndless;
        if (isFinalBoss) {
            setGameState(GAME_STATE.WIN_OVER);
            return;
        }

        setGameState(GAME_STATE.CASHOUT);
    }

    function handleRoundLose() {
        setGameState(GAME_STATE.GAME_OVER);
    }

    function handleCashout(earnedAmount) {
        setGameState(GAME_STATE.SHOP);
    }

    function handleLeaveShop() {
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
                id: 3,
                type: 'boss',
                blind: 'AmberAcorn',
                title: 'Amber Acorn (Boss)',
                score: 100000,
                reward: '$$$$$$'
            }
        }));
        setGameState(GAME_STATE.GAMEPLAY);
    }

    async function handleRestart() {
        try {
            setIsLoading(true);
            generateSessionId();
            const state = await startGame('Player 1');
            syncGameData(state);
            setGameState(GAME_STATE.BLIND_SELECTION);
        } catch (err) {
            console.error('Failed to restart:', err);
            showToast(err.message);
            setGameState(GAME_STATE.BLIND_SELECTION);
        } finally {
            setIsLoading(false);
        }
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

            {/* TOAST MESSAGE */}
            {toastMessage && (
                <div
                    style={{
                        position: 'fixed',
                        top: '20px',
                        left: '50%',
                        transform: 'translateX(-50%)',
                        zIndex: 9999,
                        background: 'rgba(20, 20, 20, 0.95)',
                        border: '2px solid #fe4747',
                        padding: '12px 24px',
                        borderRadius: '8px',
                        color: '#fff',
                        fontWeight: 'bold',
                        boxShadow: '0 8px 24px rgba(0,0,0,0.6)'
                    }}
                >
                    {toastMessage}
                </div>
            )}

            {/* BALATRO BACKGROUND */}
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

            {/* GAMEPLAY UI */}
            <div
                style={{
                    position: 'relative',
                    width: '100%',
                    height: '100%',
                    overflow: 'hidden'
                }}
            >

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
                        onSyncState={syncGameData}
                        onShowToast={showToast}
                    />
                )}

                {gameState === GAME_STATE.CASHOUT && (
                    <Cashout
                        gameData={gameData}
                        onContinue={handleCashout}
                        onOpenSettings={() => setShowSettings(true)}
                        onSyncState={syncGameData}
                        onShowToast={showToast}
                    />
                )}

                {gameState === GAME_STATE.SHOP && (
                    <Shop
                        gameData={gameData}
                        onContinue={handleLeaveShop}
                        onOpenSettings={() => setShowSettings(true)}
                        onSyncState={syncGameData}
                        onShowToast={showToast}
                    />
                )}

                {gameState === GAME_STATE.GAME_OVER && (
                    <>
                        <GameBoard
                            gameData={gameData}
                            onWin={handleRoundWin}
                            onLose={handleRoundLose}
                            onOpenSettings={() => setShowSettings(true)}
                            onSyncState={syncGameData}
                            onShowToast={showToast}
                        />
                        <GameOver
                            gameData={gameData}
                            onRestart={handleRestart}
                            onMainMenu={() => navigate('/')}
                        />
                    </>
                )}

                {gameState === GAME_STATE.WIN_OVER && (
                    <>
                        <GameBoard
                            gameData={gameData}
                            onWin={handleRoundWin}
                            onLose={handleRoundLose}
                            onOpenSettings={() => setShowSettings(true)}
                            onSyncState={syncGameData}
                            onShowToast={showToast}
                        />
                        <WinOver
                            gameData={gameData}
                            onRestart={handleRestart}
                            onMainMenu={() => navigate('/')}
                        />
                    </>
                )}

            </div>

            {/* AUDIO ELEMENT */}
            <audio
                ref={audioRef}
                src={bgm}
                loop
                preload="auto"
            />

            {/* OPTIONS / SETTINGS MODAL */}
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

            {/* DEV / DEBUG TESTING MENU */}
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