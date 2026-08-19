import { useNavigate } from 'react-router-dom';
import { useState } from 'react';

import Balatro from '../components/BalatroBackground/BalatroBackground.jsx';

function Gameplay() {

    const navigate = useNavigate();
    const [showSettings, setShowSettings] = useState(false);

    return (
        <div
            style={{
                position: 'relative',
                minHeight: '100vh',
                overflow: 'hidden',
                color: 'white'
            }}
        >

            {/* Gameplay Background */}
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

            {/* Gameplay UI */}
            <div
                style={{
                    position: 'relative',
                    minHeight: '100vh'
                }}
            >

                {/* Settings Button */}
                <button
                    onClick={() => setShowSettings(true)}
                    style={{
                        position: 'fixed',
                        top: '20px',
                        right: '20px',
                        zIndex: 10,
                        fontSize: '24px',
                        padding: '10px 15px',
                        cursor: 'pointer',
                        borderRadius: '8px',
                        border: '1px solid white',
                        color: 'white',
                        backgroundColor: 'rgba(0, 0, 0, 0.6)'
                    }}
                >
                    ⚙️
                </button>

                {/* Gameplay content */}
                <div
                    style={{
                        display: 'flex',
                        justifyContent: 'center',
                        alignItems: 'center',
                        minHeight: '100vh'
                    }}
                >
                    <h1>
                        GAMEPLAY
                    </h1>
                </div>

            </div>

            {/* Settings Overlay */}
            {showSettings && (
                <div
                    style={{
                        position: 'fixed',
                        inset: 0,
                        zIndex: 20,
                        display: 'flex',
                        justifyContent: 'center',
                        alignItems: 'center',
                        backgroundColor: 'rgba(0, 0, 0, 0.7)'
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