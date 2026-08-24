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

        currentHandName: 'Flush',
        currentHandLevel: 1,
        currentHandChips: 73,
        currentHandMult: 4
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

            currentHandName: 'Flush',
            currentHandLevel: 1,
            currentHandChips: 73,
            currentHandMult: 4
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
                        onSkipBlind={handleSkipBlind}
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