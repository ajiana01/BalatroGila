import {useEffect, useState, useRef} from 'react'
import Balatro from './components/BalatroBackground/BalatroBackground.jsx';
import logo from './assets/Balatro-Logo.png';
import bgm from './assets/music/1-main-theme.mp3';
import SpritePlayingCard from './components/SpritePlayingCard/SpritePlayingCard.jsx';

const useMock = true ; //for debug
const apiUrl = import.meta.env.VITE_API_URL;

function App() {
       
    const [data, setData] = useState(null)
    const [loading, setLoading] = useState(true)

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

        if (!audio) return;

        audio.play()
            .then(() => {
                setIsMusicPlaying(true);
            })
            .catch(() => {
                setIsMusicPlaying(false);
            });
        
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
        <div style={{position: 'relative', minHeight: '100vh', fontFamily: 'sans-serif'}}>
            <audio
                ref={audioRef}
                src={bgm}
                loop
            />

            {/* 2. LAYER BACKGROUND: Fixed di belakang layar (zIndex: -1) */}
            <div style={{
                position: 'fixed',
                top: 0,
                left: 0,
                width: '100%',
                height: '100%',
                zIndex: -1
            }}>
                <Balatro
                    // theme="green"
                    // spinRotation={-0.5}
                    // spinSpeed={1.5}
                    // spinAmount={0.08}
                    // spinEase={0.5}
                    // contrast={2.5}
                    // lighting={0.25}
                    // pixelFilter={1000}
                    // isRotate={false}
                    // mouseInteraction={false}

                />
            </div>

            <div style={{
                position: 'relative',
                zIndex: 1,
                minHeight: '100vh',
                display: 'flex',
                justifyContent: 'center',
                alignItems: 'center',
                flexDirection: 'column',
                color: 'white'
            }}>

                <img
                    src={logo}
                    alt="Balatro Gila"
                    style={{
                        width: '600px',
                        maxWidth: '80%',
                        height: 'auto',
                        marginBottom: '30px'
                    }}
                />

                <SpritePlayingCard rank="A" suit="Spades" animated />

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


                <div style={{
                    padding: '20px',
                    border: '1px solid gray',
                    borderRadius: '8px',
                    backgroundColor: 'rgba(0, 0, 0, 0.5)',
                    width: '400px',
                    maxWidth: 'calc(100% - 40px)',
                    textAlign: 'center'
                }}>
                    {loading ? (
                        <p>Mencari koneksi ke .NET...</p>
                    ) : data ? (
                        <button
                            onClick={() => console.log('PLAY')}
                            style={{
                                padding: '12px 40px',
                                fontSize: '20px',
                                fontWeight: 'bold',
                                cursor: 'pointer',
                                borderRadius: '8px',
                                border: '1',
                                color: '#FFFFFF',
                                backgroundColor: '#0164ac'
                            }}
                        >
                            PLAY
                        </button>
                    ) : (
                        <>
                            <h2>Status Server</h2>
                            <p style={{color: '#ff6b6b'}}>
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
    )
}

export default App