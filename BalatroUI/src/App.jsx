import { BrowserRouter, Routes, Route } from 'react-router-dom';
import MainMenu from './pages/MainMenu';
import Gameplay from './pages/Gameplay';
import UIPlayground from './pages/UIPlayground';

function App() {
    return (
        <BrowserRouter>
            <Routes>
                <Route path="/" element={<MainMenu />} />
                <Route path="/game" element={<Gameplay />} />
                <Route path="/playground" element={<UIPlayground />} />
                <Route path="/test-ui" element={<UIPlayground />} />
            </Routes>
        </BrowserRouter>
    );
}

export default App