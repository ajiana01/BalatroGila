import { useEffect, useState, useRef } from 'react';
import { useNavigate } from 'react-router-dom';

import Balatro from '../components/BalatroBackground/BalatroBackground.jsx';
import logo from '../assets/Balatro-Logo.png';
import bgm from '../assets/music/1-main-theme.mp3';

import PlayingCard from '../components/PlayingCard/PlayingCard';

const useMock = true;
const apiUrl = import.meta.env.VITE_API_URL;

function MainMenu() {

    const navigate = useNavigate();

    const [data, setData] = useState(null);
    const [loading, setLoading] = useState(true);

    const audioRef = useRef(null);
    const [isMusicPlaying, setIsMusicPlaying] = useState(true);

    const toggleMusic = () => {
        if (audioRef.current.paused) {
            audioRef.current.play();
            setIsMusicPlaying(true);
        } else {
            audioRef.current.pause();
            setIsMusicPlaying(false);
        }
    };

    useEffect(() => {

        const audio = audioRef.current;

        if (audio) {
            audio.play()
                .then(() => setIsMusicPlaying(true))
                .catch(() => setIsMusicPlaying(false));
        }

        if (useMock) {
            setData({
                message: 'Mock Server',
                timestamp: new Date().toISOString(),
                server: 'Development'
            });

            setLoading(false);
            return;
        }

        fetch(`${apiUrl}/api/status`)
            .then(res => {
                if (!res.ok) {
                    throw new Error('Server error');
                }

                return res.json();
            })
            .then(result => {
                setData(result);
                setLoading(false);
            })
            .catch(error => {
                console.error(error);
                setData(null);
                setLoading(false);
            });

    }, []);

    return (
        <div
            style={{
                position: 'relative',
                minHeight: '100vh',
                fontFamily: 'sans-serif'
            }}
        >

            <audio
                ref={audioRef}
                src={bgm}
                loop
            />

            {/* Background */}
            <div
                style={{
                    position: 'fixed',
                    inset: 0,
                    zIndex: -1
                }}
            >
                <Balatro />
            </div>

            {/* Main Menu */}
            <div
                style={{
                    position: 'relative',
                    zIndex: 1,
                    minHeight: '100vh',
                    display: 'flex',
                    justifyContent: 'center',
                    alignItems: 'center',
                    flexDirection: 'column',
                    color: 'white'
                }}
            >

                <div
                    style={{
                        position: 'relative',
                        width: '600px',
                        maxWidth: '80%',
                        marginBottom: '30px'
                    }}
                >

                    <img
                        src={logo}
                        alt="Balatro Gila"
                        style={{
                            width: '100%',
                            height: 'auto',
                            display: 'block'
                        }}
                    />

                    <div
                        style={{
                            position: 'absolute',
                            top: '50%',
                            left: '50%',
                            transform: 'translate(-50%, -50%)',
                            zIndex: 2
                        }}
                    >
                        <PlayingCard
                            rank="A"
                            suit="Spades"
                            width={110}
                            height={150}
                            effect="effect-3d"
                        />
                    </div>

                </div>

                {/* Music */}
                <button
                    onClick={toggleMusic}
                    style={{
                        position: 'fixed',
                        bottom: '20px',
                        right: '20px',
                        zIndex: 10,
                        padding: '10px 16px',
                        fontSize: '16px',
                        cursor: 'pointer',
                        borderRadius: '8px',
                        border: '1px solid white',
                        color: 'white',
                        backgroundColor: 'rgba(0, 0, 0, 0.5)'
                    }}
                >
                    {isMusicPlaying ? '🔊 Music ON' : '🔇 Music OFF'}
                </button>

                {/* Server */}
                <div
                    style={{
                        padding: '20px',
                        border: '1px solid gray',
                        borderRadius: '8px',
                        backgroundColor: 'rgba(0, 0, 0, 0.5)',
                        width: '400px',
                        maxWidth: 'calc(100% - 40px)',
                        textAlign: 'center'
                    }}
                >

                    {loading ? (

                        <p>Mencari koneksi ke .NET...</p>

                    ) : data ? (

                        <button
                            onClick={() => navigate('/game')}
                            style={{
                                padding: '12px 40px',
                                fontSize: '20px',
                                fontWeight: 'bold',
                                cursor: 'pointer',
                                borderRadius: '8px',
                                border: '1px solid white',
                                color: '#FFFFFF',
                                backgroundColor: '#0164ac'
                            }}
                        >
                            PLAY
                        </button>

                    ) : (

                        <>
                            <h2>Status Server</h2>

                            <p style={{ color: '#ff6b6b' }}>
                                Koneksi terputus.
                            </p>

                            <p>
                                Pastikan .NET backend sedang aktif.
                            </p>
                        </>

                    )}

                </div>

            </div>

        </div>
    );
}

export default MainMenu;