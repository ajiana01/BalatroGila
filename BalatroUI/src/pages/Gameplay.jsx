import { useNavigate } from 'react-router-dom';
import { useState } from 'react';

import Balatro from '../components/BalatroBackground/BalatroBackground.jsx';

import BlindSelection from '../components/Gameplay/BlindSelection/BlindSelection.jsx';
import GameBoard from '../components/Gameplay/GameBoard/GameBoard.jsx';
import Cashout from '../components/Gameplay/Cashout/Cashout.jsx';
import Shop from '../components/Gameplay/Shop/Shop.jsx';
import GameOver from '../components/Gameplay/GameOver/GameOver.jsx';
import WinOver from '../components/Gameplay/WinOver/WinOver.jsx';


function Gameplay() {

    const navigate = useNavigate();

    // =========================
    // SETTINGS
    // =========================

    const [showSettings, setShowSettings] = useState(false);


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
        round: 0,

        score: 0,
        targetScore: 300,

        hands: 4,
        discards: 4,

        deckRemaining: 52
    });


    // =========================
    // GAME FLOW
    // =========================

    function handleSelectBlind() {

        setGameData(prev => ({
            ...prev,
            round: prev.round + 1
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


    function handleCashout() {

        setGameData(prev => ({
            ...prev,
            money: prev.money + 4
        }));

        setGameState(GAME_STATE.SHOP);
    }


    function handleLeaveShop() {

        // Jika sudah menyelesaikan Ante terakhir
        if (gameData.ante >= 8) {

            setGameState(GAME_STATE.WIN_OVER);

            return;
        }


        setGameData(prev => ({
            ...prev,

            ante: prev.ante + 1,
            round: 0,

            score: 0,
            targetScore: 300,

            hands: 4,
            discards: 4
        }));

        setGameState(GAME_STATE.BLIND_SELECTION);
    }


    function handleRestart() {

        setGameData({
            money: 4,

            ante: 1,
            round: 0,

            score: 0,
            targetScore: 300,

            hands: 4,
            discards: 4,

            deckRemaining: 52
        });

        setGameState(GAME_STATE.BLIND_SELECTION);
    }


    return (

        <div
            style={{
                position: 'relative',
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
                    minHeight: '100vh'
                }}
            >

                {/* =================================
                    SETTINGS BUTTON
                ================================== */}

                <button
                    onClick={() => setShowSettings(true)}
                    style={{
                        position: 'fixed',
                        top: '20px',
                        right: '20px',

                        zIndex: 100,

                        fontSize: '24px',
                        padding: '10px 15px',

                        cursor: 'pointer',

                        borderRadius: '8px',
                        border: '1px solid white',

                        color: 'white',

                        backgroundColor:
                            'rgba(0, 0, 0, 0.6)'
                    }}
                >
                    ⚙️
                </button>


                {/* =================================
                    GAME STATE
                ================================== */}

                {gameState === GAME_STATE.BLIND_SELECTION && (

                    <BlindSelection
                        gameData={gameData}
                        onSelectBlind={handleSelectBlind}
                        onSkipBlind={handleSkipBlind}
                    />

                )}


                {gameState === GAME_STATE.GAMEPLAY && (

                    <GameBoard
                        gameData={gameData}
                        onWin={handleRoundWin}
                        onLose={handleRoundLose}
                    />

                )}


                {gameState === GAME_STATE.CASHOUT && (

                    <Cashout
                        gameData={gameData}
                        onContinue={handleCashout}
                    />

                )}


                {gameState === GAME_STATE.SHOP && (

                    <Shop
                        gameData={gameData}
                        onContinue={handleLeaveShop}
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
                SETTINGS OVERLAY
            ====================================== */}

            {showSettings && (

                <div
                    style={{
                        position: 'fixed',
                        inset: 0,

                        zIndex: 200,

                        display: 'flex',
                        justifyContent: 'center',
                        alignItems: 'center',

                        backgroundColor:
                            'rgba(0, 0, 0, 0.7)'
                    }}
                >

                    <div
                        style={{
                            width: '400px',
                            maxWidth: '80%',

                            padding: '30px',

                            borderRadius: '12px',

                            backgroundColor: '#222',

                            textAlign: 'center'
                        }}
                    >

                        <h2>
                            SETTINGS
                        </h2>


                        {/* MAIN MENU */}

                        <button
                            onClick={() => navigate('/')}
                            style={{
                                display: 'block',
                                width: '100%',

                                padding: '12px',

                                marginTop: '20px',

                                cursor: 'pointer',

                                fontSize: '18px'
                            }}
                        >
                            MAIN MENU
                        </button>


                        {/* BACK TO GAME */}

                        <button
                            onClick={() => setShowSettings(false)}
                            style={{
                                display: 'block',
                                width: '100%',

                                padding: '12px',

                                marginTop: '10px',

                                cursor: 'pointer',

                                fontSize: '18px'
                            }}
                        >
                            BACK TO GAME
                        </button>

                    </div>

                </div>

            )}

        </div>
    );
}

export default Gameplay;