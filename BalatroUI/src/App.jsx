import { BrowserRouter, Routes, Route } from 'react-router-dom';
import MainMenu from './pages/MainMenu';
import Gameplay from './pages/Gameplay';

function App() {
    return (
        <BrowserRouter>
            <Routes>
                <Route path="/" element={<MainMenu />} />
                <Route path="/game" element={<Gameplay />} />
            </Routes>
        </BrowserRouter>
    );
}

export default App