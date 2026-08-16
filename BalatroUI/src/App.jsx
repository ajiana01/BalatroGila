import {useEffect, useState} from 'react'
import Balatro from './components/BalatroBackground/BalatroBackground.jsx';

function App() {
  const [data, setData] = useState(null)
  const [loading, setLoading] = useState(true)

  useEffect(() => {
    fetch('http://localhost:5264/api/status')
        .then(response => {
          if (!response.ok) throw new Error("Gagal menghubungi server")
          return response.json();
        })
        .then(jsonData => {
          setData(jsonData);
          setLoading(false);
        })
        .catch(error => {
          console.error(error);
          setLoading(false);
        });
  }, []);

  return (
      <div style={{position: 'relative', minHeight: '100vh', fontFamily: 'sans-serif'}}>

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
              isRotate={false}
              mouseInteraction
              pixelFilter={745}
              color1="#DE443B"
              color2="#006BB4"
              color3="#162325"
          />
        </div>

        {/* 3. LAYER UI KONTEN: Diberi zIndex positif agar melayang di atas background */}
        <div style={{position: 'relative', zIndex: 1, padding: '20px', color: 'white'}}>
          <h1>BALATROO GILAAAA</h1>

          <div style={{
            padding: '20px',
            border: '1px solid gray',
            borderRadius: '8px',
            backgroundColor: 'rgba(0, 0, 0, 0.5)', // Memberi latar belakang semi-transparan agar teks mudah dibaca
            maxWidth: '400px',
            margin: '0 auto'
          }}>
            <h2>Status Server:</h2>

            {loading ? (
                <p>Mencari koneksi ke .NET...</p>
            ) : data ? (
                <div>
                  <p><strong>Pesan:</strong> {data.message}</p>
                  <p><strong>Waktu:</strong> {new Date(data.timestamp).toLocaleString()}</p>
                  <p><strong>OS:</strong> {data.server}</p>
                </div>
            ) : (
                <p style={{color: '#ff6b6b'}}>Koneksi terputus. Pastikan dotnet run sedang aktif.</p>
            )}
          </div>
        </div>

      </div>
  )
}

export default App