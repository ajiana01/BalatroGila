import { useState, useMemo } from 'react';
import { useNavigate } from 'react-router-dom';

import Balatro from '../components/BalatroBackground/BalatroBackground';
import PlayingCard from '../components/PlayingCard/PlayingCard';
import JokerCard from '../components/JokerCard/JokerCard';
import TarotCard from '../components/TarotCard/TarotCard';
import PlanetCard from '../components/PlanetCard/PlanetCard';
import SpectralCard from '../components/SpectralCard/SpectralCard';
import Voucher from '../components/Voucher/Voucher';
import BoosterPack from '../components/BoosterPacks/BoosterPacks';
import Blind from '../components/Blind/Blind';
import CardBack from '../components/CardBack/CardBack';

import BlindSelection from '../components/Gameplay/BlindSelection/BlindSelection';
import GameBoard from '../components/Gameplay/GameBoard/GameBoard';
import Cashout from '../components/Gameplay/Cashout/Cashout';
import Shop from '../components/Gameplay/Shop/Shop';
import GameOver from '../components/Gameplay/GameOver/GameOver';
import WinOver from '../components/Gameplay/WinOver/WinOver';
import OptionsModal from '../components/Gameplay/OptionsModal/OptionsModal';

import { jokerSprite } from '../data/sprites/jokerSprites';
import { tarotSprite } from '../data/sprites/tarotSprites';
import { planetSprite } from '../data/sprites/planetSprites';
import { spectralSprite } from '../data/sprites/spectralSprites';
import { voucherSprite } from '../data/sprites/voucherSprites';
import { boosterPackSprite } from '../data/sprites/boosterPackSprites';
import { blindSprite } from '../data/sprites/blindSprites';
import { cardBackSprite } from '../data/sprites/cardBackSprites';
import { getCardInfo, SHOP_JOKERS } from '../data/shopData';
import { sfx } from '../utils/sfx';

import './UIPlayground.css';

const SUITS = ['Spades', 'Hearts', 'Clubs', 'Diamonds'];
const RANKS = ['2', '3', '4', '5', '6', '7', '8', '9', '10', 'J', 'Q', 'K', 'A'];
const ENHANCEMENTS = ['None', 'Bonus', 'Mult', 'Wild', 'Glass', 'Steel', 'Stone', 'Gold', 'Lucky'];
const EDITIONS = ['Base', 'Foil', 'Holographic', 'Polychrome'];
const SEALS = ['None', 'RedSeal', 'BlueSeal', 'GoldSeal', 'PurpleSeal'];
const BACK_TYPES = ['Normal', 'Red', 'Blue', 'Yellow', 'Green', 'Black', 'Magic', 'Nebula', 'Ghost', 'Abandoned', 'Checkered', 'Zodiac', 'Painted', 'Anaglyph', 'Plasma', 'Erratic'];

// Jokers that have active implementation in .NET Backend (ShopService & ScoringService)
export const BACKEND_IMPLEMENTED_JOKERS = new Set([
    'Joker',
    'GreedyJoker',
    'LustyJoker',
    'WrathfulJoker',
    'GluttonousJoker',
    'JollyJoker',
    'ZanyJoker',
    'MadJoker',
    'CrazyJoker',
    'DrollJoker',
    'SlyJoker',
    'WilyJoker',
    'CleverJoker',
    'DeviousJoker',
    'CraftyJoker',
    'HalfJoker',
    'Banner',
    'MysticSummit',
    'Misprint',
    'RaisedFist',
    'ChaosTheClown',
    'Fibonacci',
    'ScaryFace',
    'SmileyFace',
    'Photograph',
    'AbstractJoker',
    'GrosMichel',
    'Cavendish',
    'EvenSteven',
    'OddTodd',
    'Scholar',
    'WalkieTalkie',
    'Baron',
    'Blackboard',
    'Bull',
    'Popcorn',
    'IceCream',
    'BlueJoker',
    'Constellation',
    'TheDuo',
    'TheTrio',
    'TheOrder',
    'TheTribe',
    'TheFamily',
    'GoldenJoker',
    'AncientJoker',
    'Castle',
    'Supernova',
    'CardSharp',
    'Mime',
    'JokerStencil',
    'FourFingers',
    'BaseballCard',
    'Campfire',
    'Ramen'
]);

export function getJokerImplementationStatus(key) {
    if (BACKEND_IMPLEMENTED_JOKERS.has(key)) {
        return {
            type: 'backend',
            label: 'Backend Ready',
            icon: '⚙️',
            badgeClass: 'backend',
            description: 'Logika skor & efek sudah terimplementasi di backend .NET'
        };
    }
    const hasFrontendData = SHOP_JOKERS.some(j => j.id === key);
    if (hasFrontendData) {
        return {
            type: 'frontend',
            label: 'UI Catalog',
            icon: '📝',
            badgeClass: 'frontend',
            description: 'Data deskripsi ada di UI catalog, handler backend belum terimplementasi'
        };
    }
    return {
        type: 'placeholder',
        label: 'Default (+4 Mult)',
        icon: '⚠️',
        badgeClass: 'placeholder',
        description: 'Belum diimplementasikan (menggunakan default fallback +4 Mult)'
    };
}

// Mock GameData generator for testing screens
function createMockGameData(overrides = {}) {
    return {
        money: 25,
        ante: 2,
        maxAnte: 8,
        round: 4,
        blindIndex: 1,
        score: 1250,
        targetScore: 2400,
        hands: 3,
        maxHands: 4,
        discards: 2,
        maxDiscards: 4,
        deckRemaining: 44,
        handCards: [
            { id: 101, suit: 'Hearts', rank: 'A', enhancement: 'None', edition: 'Base', seal: 'None' },
            { id: 102, suit: 'Hearts', rank: 'K', enhancement: 'Bonus', edition: 'Foil', seal: 'RedSeal' },
            { id: 103, suit: 'Spades', rank: 'Q', enhancement: 'Mult', edition: 'Holographic', seal: 'None' },
            { id: 104, suit: 'Diamonds', rank: 'J', enhancement: 'Wild', edition: 'Polychrome', seal: 'BlueSeal' },
            { id: 105, suit: 'Clubs', rank: '10', enhancement: 'Glass', edition: 'Base', seal: 'None' },
            { id: 106, suit: 'Diamonds', rank: '9', enhancement: 'Steel', edition: 'Base', seal: 'None' },
            { id: 107, suit: 'Hearts', rank: '8', enhancement: 'Gold', edition: 'Base', seal: 'GoldSeal' },
            { id: 108, suit: 'Spades', rank: '7', enhancement: 'Lucky', edition: 'Base', seal: 'None' }
        ],
        jokers: [
            { id: 'j-1', jokerKey: 'Joker', title: 'Joker', rarity: 'Common', price: 4, description: '+4 Mult' },
            { id: 'j-2', jokerKey: 'Banner', title: 'Banner', rarity: 'Common', price: 5, description: '+40 Chips for each remaining discard' },
            { id: 'j-3', jokerKey: 'AncientJoker', title: 'Ancient Joker', rarity: 'Rare', price: 8, description: 'Diamond cards give X1.5 Mult' },
            { id: 'j-4', jokerKey: 'Cavendish', title: 'Cavendish', rarity: 'Common', price: 5, description: 'X3 Mult' }
        ],
        maxJokers: 5,
        consumables: [
            { id: 'c-1', name: 'TheFool', title: 'The Fool', type: 'tarot', price: 3, description: 'Creates the last Tarot or Planet card used' },
            { id: 'c-2', name: 'Mercury', title: 'Mercury', type: 'planet', price: 3, description: 'Level up Pair (+1 Mult, +15 Chips)' }
        ],
        maxConsumables: 2,
        currentBlind: {
            id: 2,
            type: 'big',
            blind: 'BigBlind',
            title: 'Big Blind',
            score: 2400,
            reward: '$$$$'
        },
        availableBlinds: [
            { id: 1, type: 'small', blind: 'SmallBlind', title: 'Small Blind', score: 1600, reward: '$$$', status: 'defeated' },
            { id: 2, type: 'big', blind: 'BigBlind', title: 'Big Blind', score: 2400, reward: '$$$$', status: 'current' },
            { id: 3, type: 'boss', blind: 'TheHook', title: 'The Hook (Boss)', score: 3200, reward: '$$$$$', status: 'upcoming' }
        ],
        stats: {
            bestHandScore: 32450,
            bestHandName: 'Full House',
            mostPlayedHand: 'Flush',
            mostPlayedCount: 7,
            cardsPlayed: 28,
            cardsDiscarded: 14,
            cardsPurchased: 5,
            timesRerolled: 2,
            handsHistory: { 'Flush': 7, 'Pair': 4, 'Full House': 2, 'High Card': 1 }
        },
        shop: {
            items: [
                { id: 'shop-j-1', cardType: 'joker', name: 'Blueprint', title: 'Blueprint', price: 10, rarity: 'Rare', description: 'Copies ability of Joker to the right' },
                { id: 'shop-j-2', cardType: 'joker', name: 'Baron', title: 'Baron', price: 8, rarity: 'Rare', description: 'Each King held in hand gives X1.5 Mult' },
                { id: 'shop-t-1', cardType: 'tarot', name: 'TheWorld', title: 'The World', price: 3, description: 'Converts up to 3 selected cards to Spades' }
            ],
            boosterPacks: [
                { id: 'pack-1', type: 'Arcana_Jumbo', number: 1, price: 6, title: 'Jumbo Arcana Pack' },
                { id: 'pack-2', type: 'Buffon_Normal', number: 1, price: 4, title: 'Buffoon Pack' }
            ],
            vouchers: [
                { id: 'v-1', name: 'SeedMoney', title: 'Seed Money', price: 10, effect: 'Raise interest cap to $10' }
            ],
            rerollCost: 5
        },
        ...overrides
    };
}

function UIPlayground() {
    const navigate = useNavigate();

    // Active Category Tab
    const [activeTab, setActiveTab] = useState('cards');

    // 1. PLAYING CARDS LAB STATE
    const [cardSuit, setCardSuit] = useState('Spades');
    const [cardRank, setCardRank] = useState('A');
    const [cardEnhancement, setCardEnhancement] = useState('None');
    const [cardEdition, setCardEdition] = useState('Base');
    const [cardSeal, setCardSeal] = useState('None');
    const [cardBackType, setCardBackType] = useState('Normal');
    const [cardDebuffed, setCardDebuffed] = useState(false);
    const [cardFaceDown, setCardFaceDown] = useState(false);
    const [cardWidth, setCardWidth] = useState(110);
    const [cardHeight, setCardHeight] = useState(154);

    // 2. JOKERS LAB STATE
    const [jokerFilterRarity, setJokerFilterRarity] = useState('All');
    const [jokerFilterImpl, setJokerFilterImpl] = useState('All');
    const [jokerSearch, setJokerSearch] = useState('');
    const [selectedJokerForTrigger, setSelectedJokerForTrigger] = useState(null);
    const [triggeredJokerKey, setTriggeredJokerKey] = useState(null);
    const [triggeredText, setTriggeredText] = useState('+4 Mult');

    // 3. CONSUMABLES STATE
    const [consumableTab, setConsumableTab] = useState('tarot');

    // 4. BOOSTER PACKS & VOUCHERS STATE
    const [selectedPackPreview, setSelectedPackPreview] = useState(null);

    // 5. SCREEN SANDBOX STATE
    const [sandboxScreen, setSandboxScreen] = useState(null); // 'gameboard', 'blind-select', 'shop', 'cashout', 'gameover', 'winover', 'options'
    const [mockGameData, setMockGameData] = useState(() => createMockGameData());
    const [isOptionsOpen, setIsOptionsOpen] = useState(false);

    // Implementation status statistics
    const jokerStatusCounts = useMemo(() => {
        const keys = Object.keys(jokerSprite.cards || {});
        let backend = 0;
        let frontend = 0;
        let placeholder = 0;
        keys.forEach(k => {
            const status = getJokerImplementationStatus(k).type;
            if (status === 'backend') backend++;
            else if (status === 'frontend') frontend++;
            else placeholder++;
        });
        return { total: keys.length, backend, frontend, placeholder };
    }, []);

    // Filtered Jokers List
    const allJokerKeys = useMemo(() => {
        const keys = Object.keys(jokerSprite.cards || {});
        return keys.filter(k => {
            const info = getCardInfo(k, 'joker') || {};
            const status = getJokerImplementationStatus(k);
            const matchesRarity = jokerFilterRarity === 'All' || (info.rarity?.toLowerCase() === jokerFilterRarity.toLowerCase());
            const matchesSearch = !jokerSearch || k.toLowerCase().includes(jokerSearch.toLowerCase()) || (info.title?.toLowerCase().includes(jokerSearch.toLowerCase()));
            const matchesImpl = jokerFilterImpl === 'All' || (status.type === jokerFilterImpl);
            return matchesRarity && matchesSearch && matchesImpl;
        });
    }, [jokerFilterRarity, jokerSearch, jokerFilterImpl]);

    // Unique Tarots (deduplicate alias keys like Fool & TheFool)
    const uniqueTarotKeys = useMemo(() => {
        const seen = new Set();
        const result = [];
        const keys = Object.keys(tarotSprite.tarots).sort((a, b) => (a.startsWith('The') ? -1 : 1));
        for (const k of keys) {
            const data = tarotSprite.tarots[k];
            const coord = `${data.column}-${data.row}`;
            if (!seen.has(coord)) {
                seen.add(coord);
                result.push(k);
            }
        }
        return result;
    }, []);

    // Unique Spectrals (deduplicate alias keys like Dejavu / DejaVu / Deja_vu)
    const uniqueSpectralKeys = useMemo(() => {
        const seen = new Set();
        const result = [];
        for (const [k, data] of Object.entries(spectralSprite.spectrals)) {
            const coord = `${data.column}-${data.row}`;
            if (!seen.has(coord)) {
                seen.add(coord);
                result.push(k);
            }
        }
        return result;
    }, []);

    // Handle Joker Trigger simulation
    const handleSimulateTrigger = (key) => {
        sfx.playJokerTrigger();
        setTriggeredJokerKey(key);
        setTimeout(() => {
            setTriggeredJokerKey(null);
        }, 1200);
    };

    return (
        <div className="ui-playground-root">
            {/* Background Shader */}
            <div style={{ position: 'fixed', inset: 0, zIndex: 0, opacity: 0.85 }}>
                <Balatro
                    theme="green"
                    spinSpeed={0.8}
                    contrast={2.0}
                    lighting={0.2}
                    pixelFilter={900}
                />
            </div>

            <div className="playground-container">

                {/* HEADER NAVBAR */}
                <header className="playground-header">
                    <div className="playground-branding">
                        <div className="playground-logo-badge">LAB</div>
                        <div>
                            <h1 className="playground-title">Balatro UI Component Playground</h1>
                            <div className="playground-subtitle">Front End Component Testing & Visual Regression Lab</div>
                        </div>
                    </div>

                    <div className="playground-nav-actions">
                        <button className="playground-nav-btn" onClick={() => navigate('/')}>
                            🏠 Main Menu
                        </button>
                        <button className="playground-nav-btn primary" onClick={() => navigate('/game')}>
                            🎮 Launch Game
                        </button>
                    </div>
                </header>

                {/* TABS ROW */}
                <nav className="playground-tabs">
                    <button
                        className={`playground-tab-btn ${activeTab === 'cards' ? 'active' : ''}`}
                        onClick={() => setActiveTab('cards')}
                    >
                        <span className="tab-icon">🃏</span> Playing Cards Lab
                    </button>

                    <button
                        className={`playground-tab-btn ${activeTab === 'jokers' ? 'active' : ''}`}
                        onClick={() => setActiveTab('jokers')}
                    >
                        <span className="tab-icon">🤡</span> Jokers Lab ({allJokerKeys.length})
                    </button>

                    <button
                        className={`playground-tab-btn ${activeTab === 'consumables' ? 'active' : ''}`}
                        onClick={() => setActiveTab('consumables')}
                    >
                        <span className="tab-icon">✨</span> Consumables
                    </button>

                    <button
                        className={`playground-tab-btn ${activeTab === 'vouchers' ? 'active' : ''}`}
                        onClick={() => setActiveTab('vouchers')}
                    >
                        <span className="tab-icon">🎟️</span> Vouchers & Booster Packs
                    </button>

                    <button
                        className={`playground-tab-btn ${activeTab === 'blinds' ? 'active' : ''}`}
                        onClick={() => setActiveTab('blinds')}
                    >
                        <span className="tab-icon">👁️</span> Blinds & Chips
                    </button>

                    <button
                        className={`playground-tab-btn ${activeTab === 'screens' ? 'active' : ''}`}
                        onClick={() => setActiveTab('screens')}
                    >
                        <span className="tab-icon">🎮</span> Full Screen Mock Sandbox
                    </button>

                    <button
                        className={`playground-tab-btn ${activeTab === 'sfx' ? 'active' : ''}`}
                        onClick={() => setActiveTab('sfx')}
                    >
                        <span className="tab-icon">🔊</span> SFX Soundboard
                    </button>
                </nav>

                {/* ======================================================== */}
                {/* 1. PLAYING CARDS LAB */}
                {/* ======================================================== */}
                {activeTab === 'cards' && (
                    <section className="playground-section">
                        <div className="section-header">
                            <div>
                                <h2 className="section-title">🃏 Playing Card Customizer & Inspector</h2>
                                <div className="section-desc">Test suit sprites, enhancements, foil/holo/polychrome editions, seals, and debuffs.</div>
                            </div>
                        </div>

                        {/* Interactive Bench */}
                        <div className="interactive-bench">
                            <div className="bench-card-preview">
                                <PlayingCard
                                    suit={cardSuit}
                                    rank={cardRank}
                                    enhancement={cardEnhancement}
                                    edition={cardEdition}
                                    seal={cardSeal}
                                    backType={cardBackType}
                                    isDebuffed={cardDebuffed}
                                    showBack={cardFaceDown}
                                    width={cardWidth}
                                    height={cardHeight}
                                    effect="effect-3d"
                                />
                                <div style={{ marginTop: '16px', fontSize: '13px', color: '#90e0b5', fontWeight: 'bold' }}>
                                    {cardRank} of {cardSuit} • {cardEnhancement !== 'None' ? `${cardEnhancement} ` : ''}{cardEdition !== 'Base' ? `(${cardEdition}) ` : ''}{cardSeal !== 'None' ? `[${cardSeal}]` : ''}
                                </div>
                            </div>

                            <div className="bench-props-panel">
                                <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: '12px' }}>
                                    <div className="control-item">
                                        <label className="control-label">Rank</label>
                                        <select className="control-select" value={cardRank} onChange={e => { setCardRank(e.target.value); sfx.playCardSelect(); }}>
                                            {RANKS.map(r => <option key={r} value={r}>{r}</option>)}
                                        </select>
                                    </div>

                                    <div className="control-item">
                                        <label className="control-label">Suit</label>
                                        <select className="control-select" value={cardSuit} onChange={e => { setCardSuit(e.target.value); sfx.playCardSelect(); }}>
                                            {SUITS.map(s => <option key={s} value={s}>{s}</option>)}
                                        </select>
                                    </div>

                                    <div className="control-item">
                                        <label className="control-label">Enhancement</label>
                                        <select className="control-select" value={cardEnhancement} onChange={e => { setCardEnhancement(e.target.value); sfx.playCardSelect(); }}>
                                            {ENHANCEMENTS.map(enh => <option key={enh} value={enh}>{enh}</option>)}
                                        </select>
                                    </div>

                                    <div className="control-item">
                                        <label className="control-label">Edition</label>
                                        <select className="control-select" value={cardEdition} onChange={e => { setCardEdition(e.target.value); sfx.playFoilShimmer(); }}>
                                            {EDITIONS.map(ed => <option key={ed} value={ed}>{ed}</option>)}
                                        </select>
                                    </div>

                                    <div className="control-item">
                                        <label className="control-label">Seal</label>
                                        <select className="control-select" value={cardSeal} onChange={e => { setCardSeal(e.target.value); sfx.playCardSelect(); }}>
                                            {SEALS.map(seal => <option key={seal} value={seal}>{seal}</option>)}
                                        </select>
                                    </div>

                                    <div className="control-item">
                                        <label className="control-label">Deck Back Style</label>
                                        <select className="control-select" value={cardBackType} onChange={e => setCardBackType(e.target.value)}>
                                            {BACK_TYPES.map(bk => <option key={bk} value={bk}>{bk}</option>)}
                                        </select>
                                    </div>
                                </div>

                                <div style={{ display: 'flex', gap: '10px', marginTop: '6px' }}>
                                    <button
                                        className={`control-toggle-btn ${cardDebuffed ? 'active' : ''}`}
                                        onClick={() => setCardDebuffed(!cardDebuffed)}
                                    >
                                        Debuff: {cardDebuffed ? 'ON' : 'OFF'}
                                    </button>

                                    <button
                                        className={`control-toggle-btn ${cardFaceDown ? 'active' : ''}`}
                                        onClick={() => setCardFaceDown(!cardFaceDown)}
                                    >
                                        Face Down: {cardFaceDown ? 'ON' : 'OFF'}
                                    </button>
                                </div>
                            </div>
                        </div>

                        {/* Quick Enhancement Showcase Gallery */}
                        <div className="section-header" style={{ marginTop: '28px' }}>
                            <div>
                                <h3 className="section-title" style={{ fontSize: '16px' }}>✨ Enhancement Variants Gallery (Ace of Spades)</h3>
                            </div>
                        </div>

                        <div className="playground-grid">
                            {ENHANCEMENTS.map(enh => (
                                <div
                                    key={enh}
                                    className="grid-item-wrapper"
                                    onClick={() => { setCardEnhancement(enh); sfx.playCardSelect(); }}
                                >
                                    <PlayingCard
                                        suit="Spades"
                                        rank="A"
                                        enhancement={enh}
                                        width={80}
                                        height={112}
                                    />
                                    <div className="grid-item-label">{enh}</div>
                                </div>
                            ))}
                        </div>
                    </section>
                )}

                {/* ======================================================== */}
                {/* 2. JOKERS LAB */}
                {/* ======================================================== */}
                {activeTab === 'jokers' && (
                    <section className="playground-section">
                        <div className="section-header">
                            <div>
                                <h2 className="section-title">🤡 Joker Cards & Trigger Simulator</h2>
                                <div className="section-desc">Search and test joker tooltips, rarity badges, and distinguish backend implemented vs default placeholders.</div>
                            </div>
                        </div>

                        {/* Implementation Status Filter Bar */}
                        <div className="joker-status-summary-bar">
                            <span style={{ fontSize: '12px', fontWeight: 800, color: '#8dafa1', textTransform: 'uppercase', letterSpacing: '0.8px' }}>
                                Status Filter:
                            </span>

                            <button
                                className={`status-stat-pill ${jokerFilterImpl === 'All' ? 'selected' : ''}`}
                                style={{ background: 'rgba(255,255,255,0.08)', color: '#fff', borderColor: jokerFilterImpl === 'All' ? '#3fe0a5' : 'transparent' }}
                                onClick={() => setJokerFilterImpl('All')}
                            >
                                All ({jokerStatusCounts.total})
                            </button>

                            <button
                                className={`status-stat-pill backend ${jokerFilterImpl === 'backend' ? 'selected' : ''}`}
                                onClick={() => setJokerFilterImpl('backend')}
                                title="Jokers with fully implemented logic in .NET backend"
                            >
                                ⚙️ Backend Ready ({jokerStatusCounts.backend})
                            </button>

                            <button
                                className={`status-stat-pill frontend ${jokerFilterImpl === 'frontend' ? 'selected' : ''}`}
                                onClick={() => setJokerFilterImpl('frontend')}
                                title="Jokers defined in UI catalog (custom description) without backend handler"
                            >
                                📝 UI Catalog Only ({jokerStatusCounts.frontend})
                            </button>

                            <button
                                className={`status-stat-pill placeholder ${jokerFilterImpl === 'placeholder' ? 'selected' : ''}`}
                                onClick={() => setJokerFilterImpl('placeholder')}
                                title="Jokers without implementation yet (fallback to default +4 Mult)"
                            >
                                ⚠️ Default (+4 Mult) ({jokerStatusCounts.placeholder})
                            </button>
                        </div>

                        {/* Controls */}
                        <div className="playground-controls-bar">
                            <div className="control-item">
                                <label className="control-label">Search Joker</label>
                                <input
                                    type="text"
                                    className="control-input"
                                    placeholder="e.g. Cavendish, Baron..."
                                    value={jokerSearch}
                                    onChange={e => setJokerSearch(e.target.value)}
                                />
                            </div>

                            <div className="control-item">
                                <label className="control-label">Filter Rarity</label>
                                <select className="control-select" value={jokerFilterRarity} onChange={e => setJokerFilterRarity(e.target.value)}>
                                    <option value="All">All Rarities</option>
                                    <option value="Common">Common</option>
                                    <option value="Uncommon">Uncommon</option>
                                    <option value="Rare">Rare</option>
                                    <option value="Legendary">Legendary</option>
                                </select>
                            </div>

                            <div className="control-item">
                                <label className="control-label">Trigger Text Preset</label>
                                <select className="control-select" value={triggeredText} onChange={e => setTriggeredText(e.target.value)}>
                                    <option value="+4 Mult">+4 Mult</option>
                                    <option value="+100 Chips">+100 Chips</option>
                                    <option value="X1.5 Mult">X1.5 Mult</option>
                                    <option value="X3 Mult">X3 Mult</option>
                                    <option value="+$3 Earned">+$3 Earned</option>
                                    <option value="Again!">Again!</option>
                                </select>
                            </div>
                        </div>

                        {/* Jokers Grid */}
                        <div className="playground-grid" style={{ gridTemplateColumns: 'repeat(auto-fill, minmax(140px, 1fr))' }}>
                            {allJokerKeys.map(key => {
                                const isTriggered = triggeredJokerKey === key;
                                const isSelected = selectedJokerForTrigger === key;
                                const info = getCardInfo(key, 'joker') || {};
                                const impl = getJokerImplementationStatus(key);

                                return (
                                    <div
                                        key={key}
                                        className={`grid-item-wrapper ${impl.type === 'placeholder' ? 'is-placeholder' : ''}`}
                                        style={{ position: 'relative' }}
                                        title={impl.description}
                                    >
                                        {/* Implementation Status Badge */}
                                        <div style={{ marginBottom: '4px', width: '100%', display: 'flex', justifyContent: 'center' }}>
                                            <span className={`impl-badge ${impl.badgeClass}`}>
                                                {impl.icon} {impl.label}
                                            </span>
                                        </div>

                                        <JokerCard
                                            jokerKey={key}
                                            width={90}
                                            height={126}
                                            isTriggered={isTriggered}
                                            triggeredText={triggeredText}
                                            isSelected={isSelected}
                                            onSelect={() => {
                                                setSelectedJokerForTrigger(isSelected ? null : key);
                                                sfx.playCardSelect();
                                            }}
                                            onSell={() => sfx.playCashRegister()}
                                        />
                                        <div className="grid-item-label">{info.title || key}</div>
                                        <div className="grid-item-sublabel">{info.rarity || 'Common'}</div>

                                        <button
                                            style={{
                                                marginTop: '4px',
                                                padding: '4px 8px',
                                                fontSize: '11px',
                                                background: '#254a3e',
                                                border: '1px solid #3fe0a5',
                                                color: '#fff',
                                                borderRadius: '4px',
                                                cursor: 'pointer',
                                                fontWeight: 'bold'
                                            }}
                                            onClick={(e) => {
                                                e.stopPropagation();
                                                handleSimulateTrigger(key);
                                            }}
                                        >
                                            ⚡ Trigger
                                        </button>
                                    </div>
                                );
                            })}
                        </div>
                    </section>
                )}

                {/* ======================================================== */}
                {/* 3. CONSUMABLES (TAROT / PLANET / SPECTRAL) */}
                {/* ======================================================== */}
                {activeTab === 'consumables' && (
                    <section className="playground-section">
                        <div className="section-header">
                            <div>
                                <h2 className="section-title">✨ Consumables Lab</h2>
                                <div className="section-desc">Inspect Tarot, Planet, and Spectral cards with interactive hover tooltips and Action tabs.</div>
                            </div>

                            <div style={{ display: 'flex', gap: '8px' }}>
                                <button
                                    className={`control-toggle-btn ${consumableTab === 'tarot' ? 'active' : ''}`}
                                    onClick={() => setConsumableTab('tarot')}
                                >
                                    Tarots ({uniqueTarotKeys.length})
                                </button>
                                <button
                                    className={`control-toggle-btn ${consumableTab === 'planet' ? 'active' : ''}`}
                                    onClick={() => setConsumableTab('planet')}
                                >
                                    Planets ({Object.keys(planetSprite.planets).length})
                                </button>
                                <button
                                    className={`control-toggle-btn ${consumableTab === 'spectral' ? 'active' : ''}`}
                                    onClick={() => setConsumableTab('spectral')}
                                >
                                    Spectrals ({uniqueSpectralKeys.length})
                                </button>
                            </div>
                        </div>

                        {/* TAROTS */}
                        {consumableTab === 'tarot' && (
                            <div className="playground-grid">
                                {uniqueTarotKeys.map(t => {
                                    const info = getCardInfo(t, 'tarot') || {};
                                    return (
                                        <div key={t} className="grid-item-wrapper">
                                            <TarotCard
                                                tarot={t}
                                                width={90}
                                                height={126}
                                                onSelect={() => sfx.playCardSelect()}
                                                onUse={() => sfx.playFireEffect()}
                                                onSell={() => sfx.playCashRegister()}
                                            />
                                            <div className="grid-item-label">{info.title || t}</div>
                                        </div>
                                    );
                                })}
                            </div>
                        )}

                        {/* PLANETS */}
                        {consumableTab === 'planet' && (
                            <div className="playground-grid">
                                {Object.keys(planetSprite.planets).map(p => {
                                    const info = getCardInfo(p, 'planet') || {};
                                    return (
                                        <div key={p} className="grid-item-wrapper">
                                            <PlanetCard
                                                planet={p}
                                                width={90}
                                                height={126}
                                                onSelect={() => sfx.playCardSelect()}
                                                onUse={() => sfx.playMultTick()}
                                                onSell={() => sfx.playCashRegister()}
                                            />
                                            <div className="grid-item-label">{info.title || p}</div>
                                        </div>
                                    );
                                })}
                            </div>
                        )}

                        {/* SPECTRALS */}
                        {consumableTab === 'spectral' && (
                            <div className="playground-grid">
                                {uniqueSpectralKeys.map(s => {
                                    const formattedTitle = s.replace(/([A-Z])/g, ' $1').trim();
                                    return (
                                        <div key={s} className="grid-item-wrapper">
                                            <SpectralCard
                                                spectral={s}
                                                width={90}
                                                height={126}
                                                onSelect={() => sfx.playCardSelect()}
                                                onUse={() => sfx.playFireEffect()}
                                                onSell={() => sfx.playCashRegister()}
                                            />
                                            <div className="grid-item-label">{formattedTitle}</div>
                                        </div>
                                    );
                                })}
                            </div>
                        )}
                    </section>
                )}

                {/* ======================================================== */}
                {/* 4. VOUCHERS & BOOSTER PACKS */}
                {/* ======================================================== */}
                {activeTab === 'vouchers' && (
                    <section className="playground-section">
                        <div className="section-header">
                            <div>
                                <h2 className="section-title">🎟️ Vouchers & Booster Packs Showcase</h2>
                                <div className="section-desc">Preview all Voucher effects and Booster Pack tiers (Normal, Jumbo, Mega).</div>
                            </div>
                        </div>

                        {/* Vouchers */}
                        <h3 className="section-title" style={{ fontSize: '16px', marginBottom: '14px' }}>📜 Vouchers Gallery</h3>
                        <div className="playground-grid" style={{ marginBottom: '32px' }}>
                            {Object.keys(voucherSprite.vouchers).map(v => (
                                <div key={v} className="grid-item-wrapper" onClick={() => sfx.playCardSelect()}>
                                    <Voucher voucher={v} width={80} height={112} />
                                    <div className="grid-item-label">{v}</div>
                                </div>
                            ))}
                        </div>

                        {/* Booster Packs */}
                        <h3 className="section-title" style={{ fontSize: '16px', marginBottom: '14px' }}>🎁 Booster Packs Gallery</h3>
                        <div className="playground-grid">
                            {Object.keys(boosterPackSprite).map(typeKey => (
                                <div key={typeKey} className="grid-item-wrapper" onClick={() => sfx.playCardSelect()}>
                                    <BoosterPack type={typeKey} number={1} width={90} height={126} />
                                    <div className="grid-item-label">{typeKey.replace('_', ' ')}</div>
                                </div>
                            ))}
                        </div>
                    </section>
                )}

                {/* ======================================================== */}
                {/* 5. BLINDS & CHIP CALCULATOR */}
                {/* ======================================================== */}
                {activeTab === 'blinds' && (
                    <section className="playground-section">
                        <div className="section-header">
                            <div>
                                <h2 className="section-title">👁️ Blinds & Boss Blinds Gallery</h2>
                                <div className="section-desc">Explore all Boss Blind chip emblems and Ante target requirements.</div>
                            </div>
                        </div>

                        <div className="playground-grid" style={{ gridTemplateColumns: 'repeat(auto-fill, minmax(120px, 1fr))' }}>
                            {Object.keys(blindSprite.blinds).map(b => (
                                <div key={b} className="grid-item-wrapper" onClick={() => sfx.playCardSelect()}>
                                    <Blind blind={b} width={80} height={80} />
                                    <div className="grid-item-label">{b}</div>
                                </div>
                            ))}
                        </div>
                    </section>
                )}

                {/* ======================================================== */}
                {/* 6. FULL SCREEN MOCK SANDBOX */}
                {/* ======================================================== */}
                {activeTab === 'screens' && (
                    <section className="playground-section">
                        <div className="section-header">
                            <div>
                                <h2 className="section-title">🎮 Full Game Screen Sandbox</h2>
                                <div className="section-desc">Launch and inspect entire gameplay screens in isolated mock mode without backend dependency.</div>
                            </div>
                        </div>

                        <div style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fill, minmax(280px, 1fr))', gap: '16px' }}>
                            <div className="grid-item-wrapper" style={{ padding: '20px', alignItems: 'flex-start' }}>
                                <h3 style={{ margin: '0 0 8px', color: '#3fe0a5' }}>🎯 Blind Selection</h3>
                                <p style={{ fontSize: '12px', color: '#8dafa1', margin: '0 0 16px' }}>Preview Small, Big, and Boss blinds selection panel.</p>
                                <button className="playground-nav-btn primary" style={{ width: '100%' }} onClick={() => setSandboxScreen('blind-selection')}>
                                    Open Blind Selection
                                </button>
                            </div>

                            <div className="grid-item-wrapper" style={{ padding: '20px', alignItems: 'flex-start' }}>
                                <h3 style={{ margin: '0 0 8px', color: '#3fe0a5' }}>🃏 Main GameBoard</h3>
                                <p style={{ fontSize: '12px', color: '#8dafa1', margin: '0 0 16px' }}>Interactive Hand, scoring area, jokers, and deck modal.</p>
                                <button className="playground-nav-btn primary" style={{ width: '100%' }} onClick={() => setSandboxScreen('gameboard')}>
                                    Open GameBoard
                                </button>
                            </div>

                            <div className="grid-item-wrapper" style={{ padding: '20px', alignItems: 'flex-start' }}>
                                <h3 style={{ margin: '0 0 8px', color: '#3fe0a5' }}>🛍️ Shop Screen</h3>
                                <p style={{ fontSize: '12px', color: '#8dafa1', margin: '0 0 16px' }}>Test purchasing jokers, packs, vouchers, and reroll.</p>
                                <button className="playground-nav-btn primary" style={{ width: '100%' }} onClick={() => setSandboxScreen('shop')}>
                                    Open Shop
                                </button>
                            </div>

                            <div className="grid-item-wrapper" style={{ padding: '20px', alignItems: 'flex-start' }}>
                                <h3 style={{ margin: '0 0 8px', color: '#3fe0a5' }}>💰 Cashout Screen</h3>
                                <p style={{ fontSize: '12px', color: '#8dafa1', margin: '0 0 16px' }}>Interest calculations, remaining hands bonus breakdown.</p>
                                <button className="playground-nav-btn primary" style={{ width: '100%' }} onClick={() => setSandboxScreen('cashout')}>
                                    Open Cashout
                                </button>
                            </div>

                            <div className="grid-item-wrapper" style={{ padding: '20px', alignItems: 'flex-start' }}>
                                <h3 style={{ margin: '0 0 8px', color: '#ff6b6b' }}>☠️ Game Over Screen</h3>
                                <p style={{ fontSize: '12px', color: '#8dafa1', margin: '0 0 16px' }}>Inspect defeat animations, round summary, and stats.</p>
                                <button className="playground-nav-btn" style={{ width: '100%', borderColor: '#ff6b6b', color: '#ff6b6b' }} onClick={() => setSandboxScreen('gameover')}>
                                    Open Game Over
                                </button>
                            </div>

                            <div className="grid-item-wrapper" style={{ padding: '20px', alignItems: 'flex-start' }}>
                                <h3 style={{ margin: '0 0 8px', color: '#ffd32a' }}>🏆 Victory Win Screen</h3>
                                <p style={{ fontSize: '12px', color: '#8dafa1', margin: '0 0 16px' }}>Ante 8 final victory screen with stats & fireworks.</p>
                                <button className="playground-nav-btn" style={{ width: '100%', borderColor: '#ffd32a', color: '#ffd32a' }} onClick={() => setSandboxScreen('winover')}>
                                    Open Win Screen
                                </button>
                            </div>

                            <div className="grid-item-wrapper" style={{ padding: '20px', alignItems: 'flex-start' }}>
                                <h3 style={{ margin: '0 0 8px', color: '#00d2be' }}>⚙️ Options Modal</h3>
                                <p style={{ fontSize: '12px', color: '#8dafa1', margin: '0 0 16px' }}>Audio volumes, game speed cycles, and controls.</p>
                                <button className="playground-nav-btn" style={{ width: '100%' }} onClick={() => setIsOptionsOpen(true)}>
                                    Open Options Modal
                                </button>
                            </div>
                        </div>
                    </section>
                )}

                {/* ======================================================== */}
                {/* 7. SFX SOUNDBOARD */}
                {/* ======================================================== */}
                {activeTab === 'sfx' && (
                    <section className="playground-section">
                        <div className="section-header">
                            <div>
                                <h2 className="section-title">🔊 Procedural Web Audio SFX Tester</h2>
                                <div className="section-desc">Click any button to trigger and audition procedural synth sound effects.</div>
                            </div>
                        </div>

                        <div className="sfx-grid">
                            <button className="sfx-btn" onClick={() => sfx.playCardSelect()}>
                                <span className="sfx-btn-icon">🃏</span> Card Select
                            </button>
                            <button className="sfx-btn" onClick={() => sfx.playCardSlide()}>
                                <span className="sfx-btn-icon">🎴</span> Card Slide
                            </button>
                            <button className="sfx-btn" onClick={() => sfx.playPlayHand()}>
                                <span className="sfx-btn-icon">💨</span> Play Hand (Whoosh)
                            </button>
                            <button className="sfx-btn" onClick={() => sfx.playChipTick()}>
                                <span className="sfx-btn-icon">🔵</span> Chip Tick (Scoring)
                            </button>
                            <button className="sfx-btn" onClick={() => sfx.playMultTick()}>
                                <span className="sfx-btn-icon">🔴</span> Mult Tick (Red)
                            </button>
                            <button className="sfx-btn" onClick={() => sfx.playMultMultiply()}>
                                <span className="sfx-btn-icon">✖️</span> X Mult Multiply
                            </button>
                            <button className="sfx-btn" onClick={() => sfx.playJokerTrigger()}>
                                <span className="sfx-btn-icon">🤡</span> Joker Trigger
                            </button>
                            <button className="sfx-btn" onClick={() => sfx.playCashRegister()}>
                                <span className="sfx-btn-icon">💰</span> Cash Register / Buy
                            </button>
                            <button className="sfx-btn" onClick={() => sfx.playReroll()}>
                                <span className="sfx-btn-icon">🎲</span> Shop Reroll
                            </button>
                            <button className="sfx-btn" onClick={() => sfx.playFireEffect()}>
                                <span className="sfx-btn-icon">🔥</span> Fire / Flame
                            </button>
                            <button className="sfx-btn" onClick={() => sfx.playFoilShimmer()}>
                                <span className="sfx-btn-icon">✨</span> Foil / Holo Shimmer
                            </button>
                            <button className="sfx-btn" onClick={() => sfx.playWinRound()}>
                                <span className="sfx-btn-icon">🎺</span> Round Won Fanfare
                            </button>
                            <button className="sfx-btn" onClick={() => sfx.playGameOver()}>
                                <span className="sfx-btn-icon">☠️</span> Game Over Sound
                            </button>
                        </div>
                    </section>
                )}

            </div>

            {/* FULLSCREEN SANDBOX OVERLAY MODAL */}
            {sandboxScreen && (
                <div className="sandbox-modal-container">
                    <div className="sandbox-close-bar">
                        <button className="sandbox-close-btn" onClick={() => setSandboxScreen(null)}>
                            ✕ Close Sandbox Preview
                        </button>
                    </div>

                    {sandboxScreen === 'blind-selection' && (
                        <BlindSelection
                            gameData={mockGameData}
                            onSelectBlind={() => setSandboxScreen('gameboard')}
                            onOpenSettings={() => setIsOptionsOpen(true)}
                            onSyncState={setMockGameData}
                            onShowToast={(msg) => alert(msg)}
                        />
                    )}

                    {sandboxScreen === 'gameboard' && (
                        <GameBoard
                            gameData={mockGameData}
                            onWin={() => setSandboxScreen('cashout')}
                            onLose={() => setSandboxScreen('gameover')}
                            onOpenSettings={() => setIsOptionsOpen(true)}
                            onSyncState={setMockGameData}
                            onShowToast={(msg) => alert(msg)}
                        />
                    )}

                    {sandboxScreen === 'shop' && (
                        <Shop
                            gameData={mockGameData}
                            onContinue={() => setSandboxScreen('blind-selection')}
                            onOpenSettings={() => setIsOptionsOpen(true)}
                            onSyncState={setMockGameData}
                            onShowToast={(msg) => alert(msg)}
                        />
                    )}

                    {sandboxScreen === 'cashout' && (
                        <Cashout
                            gameData={mockGameData}
                            onContinue={() => setSandboxScreen('shop')}
                            onOpenSettings={() => setIsOptionsOpen(true)}
                            onSyncState={setMockGameData}
                            onShowToast={(msg) => alert(msg)}
                        />
                    )}

                    {sandboxScreen === 'gameover' && (
                        <GameOver
                            gameData={mockGameData}
                            onRestart={() => setSandboxScreen('blind-selection')}
                            onMainMenu={() => setSandboxScreen(null)}
                        />
                    )}

                    {sandboxScreen === 'winover' && (
                        <WinOver
                            gameData={mockGameData}
                            onRestart={() => setSandboxScreen('blind-selection')}
                            onMainMenu={() => setSandboxScreen(null)}
                        />
                    )}
                </div>
            )}

            {/* OPTIONS MODAL TEST */}
            <OptionsModal
                isOpen={isOptionsOpen}
                onClose={() => setIsOptionsOpen(false)}
                onMainMenu={() => setSandboxScreen(null)}
            />

        </div>
    );
}

export default UIPlayground;
