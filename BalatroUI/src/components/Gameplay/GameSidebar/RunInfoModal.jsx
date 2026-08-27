import { useState, useEffect } from 'react';
import PlayingCard from '../../PlayingCard/PlayingCard';
import Blind from '../../Blind/Blind';
import Voucher from '../../Voucher/Voucher';
import './RunInfoModal.css';

function PokerChipMiniIcon() {
    return (
        <svg width="18" height="18" viewBox="0 0 24 24" fill="currentColor" style={{ verticalAlign: 'middle', marginRight: '4px' }}>
            <circle cx="12" cy="12" r="10.5" fill="#e8edf0" />
            <circle cx="12" cy="12" r="7.5" fill="none" stroke="#243033" strokeWidth="2" strokeDasharray="3.8 2.5" />
            <circle cx="12" cy="12" r="4" fill="#e8edf0" />
        </svg>
    );
}

const VOUCHER_INFO = {
    DirectorsCut: {
        title: "Director's Cut",
        desc: "Reroll Boss Blind 1 time per Ante. $10 per roll"
    },
    Overstock: {
        title: "Overstock",
        desc: "+1 card slot available in shop"
    },
    OverstockPlus: {
        title: "Overstock Plus",
        desc: "+1 card slot available in shop"
    },
    ClearanceSale: {
        title: "Clearance Sale",
        desc: "All cards and packs in shop are 25% off"
    },
    Liquidation: {
        title: "Liquidation",
        desc: "All cards and packs in shop are 50% off"
    },
    TarotMerchant: {
        title: "Tarot Merchant",
        desc: "Tarot cards appear 2X more frequently in the shop"
    },
    TarotTycoon: {
        title: "Tarot Tycoon",
        desc: "Tarot cards appear 4X more frequently in the shop"
    },
    PlanetMerchant: {
        title: "Planet Merchant",
        desc: "Planet cards appear 2X more frequently in the shop"
    },
    PlanetTycoon: {
        title: "Planet Tycoon",
        desc: "Planet cards appear 4X more frequently in the shop"
    },
    Hone: {
        title: "Hone",
        desc: "Foil, Holographic, and Polychrome cards appear 2X more often"
    },
    GlowUp: {
        title: "Glow Up",
        desc: "Foil, Holographic, and Polychrome cards appear 4X more often"
    },
    Grabber: {
        title: "Grabber",
        desc: "Permanently gain +1 hand per round"
    },
    NachoTong: {
        title: "Nacho Tong",
        desc: "Permanently gain +1 hand per round"
    },
    Wasteful: {
        title: "Wasteful",
        desc: "Permanently gain +1 discard per round"
    },
    Recyclomancy: {
        title: "Recyclomancy",
        desc: "Permanently gain +1 discard per round"
    },
    Blank: {
        title: "Blank",
        desc: "Does nothing?"
    },
    Antimatter: {
        title: "Antimatter",
        desc: "+1 Joker slot"
    },
    RerollSurplus: {
        title: "Reroll Surplus",
        desc: "Rerolls cost $2 less"
    },
    RerollGlut: {
        title: "Reroll Glut",
        desc: "Rerolls cost an additional $2 less"
    },
    SeedMoney: {
        title: "Seed Money",
        desc: "Raise the cap on interest earned per round to $10"
    },
    MoneyTree: {
        title: "Money Tree",
        desc: "Raise the cap on interest earned per round to $20"
    },
    CrystalBall: {
        title: "Crystal Ball",
        desc: "+1 consumable slot"
    },
    OmenGlobe: {
        title: "Omen Globe",
        desc: "Spectral cards may appear in any of the Arcana Packs"
    },
    Telescope: {
        title: "Telescope",
        desc: "Celestial Packs always contain the Planet card for your most played poker hand"
    },
    Observatory: {
        title: "Observatory",
        desc: "Planet cards in your consumable area give X1.5 Mult for their specified poker hand"
    },
    MagicTrick: {
        title: "Magic Trick",
        desc: "Playing cards can be purchased from the shop"
    },
    Illusion: {
        title: "Illusion",
        desc: "Playing cards in shop may have an Enhancement, Edition, and/or a Seal"
    },
    Hieroglyph: {
        title: "Hieroglyph",
        desc: "-1 Ante, -1 hand each round"
    },
    Petroglyph: {
        title: "Petroglyph",
        desc: "-1 Ante, -1 discard each round"
    },
    PaintBrush: {
        title: "Paint Brush",
        desc: "+1 hand size"
    },
    Palette: {
        title: "Palette",
        desc: "+1 hand size"
    }
};

function formatVoucherDescription(desc) {
    if (!desc) return '';
    const parts = desc.split(/(\$\d+|\b\d+(?:X)?\b|\b\d+%\b)/g);
    return parts.map((part, i) => {
        if (/^\$\d+/.test(part) || /\b\d+%\b/.test(part)) {
            return <span key={i} style={{ color: '#ff9d00', fontWeight: 900 }}>{part}</span>;
        }
        if (/^\d+(?:X)?$/.test(part)) {
            return <span key={i} style={{ color: '#ea580c', fontWeight: 900 }}>{part}</span>;
        }
        return part;
    });
}

const POKER_HAND_DEFINITIONS = [
    { id: 'straight_flush', name: 'Straight Flush', baseChips: 100, baseMult: 8, chipLvl: 40, multLvl: 4 },
    { id: 'four_of_a_kind', name: 'Four of a Kind', baseChips: 60, baseMult: 7, chipLvl: 30, multLvl: 3 },
    { id: 'full_house', name: 'Full House', baseChips: 40, baseMult: 4, chipLvl: 25, multLvl: 2 },
    { id: 'flush', name: 'Flush', baseChips: 35, baseMult: 4, chipLvl: 15, multLvl: 2 },
    { id: 'straight', name: 'Straight', baseChips: 30, baseMult: 4, chipLvl: 30, multLvl: 3 },
    { id: 'three_of_a_kind', name: 'Three of a Kind', baseChips: 30, baseMult: 3, chipLvl: 20, multLvl: 2 },
    { id: 'two_pair', name: 'Two Pair', baseChips: 20, baseMult: 2, chipLvl: 20, multLvl: 1 },
    { id: 'pair', name: 'Pair', baseChips: 10, baseMult: 2, chipLvl: 15, multLvl: 1 },
    { id: 'high_card', name: 'High Card', baseChips: 5, baseMult: 1, chipLvl: 10, multLvl: 1 }
];

function normalizeHandKey(nameOrKey) {
    return String(nameOrKey || '').toLowerCase().replace(/[^a-z0-9]/g, '');
}

const ENUM_INDEX_TO_HAND = {
    '0': 'highcard',
    '1': 'pair',
    '2': 'twopair',
    '3': 'threeofakind',
    '4': 'straight',
    '5': 'flush',
    '6': 'fullhouse',
    '7': 'fourofakind',
    '8': 'straightflush'
};

function getHandLevel(handLevels, handName) {
    if (!handLevels || typeof handLevels !== 'object') return 1;
    const target = normalizeHandKey(handName);

    for (const [k, v] of Object.entries(handLevels)) {
        if (normalizeHandKey(k) === target) return v || 1;
        if (ENUM_INDEX_TO_HAND[k] === target) return v || 1;
    }
    return 1;
}

function getHandPlayed(stats, handName) {
    const history = stats?.handsHistory || stats?.pokerHandPlayed || stats || {};
    if (!history || typeof history !== 'object') return 0;
    const target = normalizeHandKey(handName);

    for (const [k, v] of Object.entries(history)) {
        if (normalizeHandKey(k) === target) return v || 0;
        if (ENUM_INDEX_TO_HAND[k] === target) return v || 0;
    }
    return 0;
}

const POKER_HAND_INFO = {
    straight_flush: {
        description: '5 cards in a row (consecutive ranks) with all cards sharing the same suit',
        cards: [
            { rank: '10', suit: 'Hearts', scored: true },
            { rank: '9', suit: 'Hearts', scored: true },
            { rank: '8', suit: 'Hearts', scored: true },
            { rank: '7', suit: 'Hearts', scored: true },
            { rank: '6', suit: 'Hearts', scored: true }
        ]
    },
    four_of_a_kind: {
        description: '4 cards with the same rank. They may be played with 1 other unscored card',
        cards: [
            { rank: '7', suit: 'Hearts', scored: true },
            { rank: '7', suit: 'Clubs', scored: true },
            { rank: '7', suit: 'Diamonds', scored: true },
            { rank: '7', suit: 'Spades', scored: true },
            { rank: '2', suit: 'Hearts', scored: false }
        ]
    },
    full_house: {
        description: 'A Three of a Kind and a Pair',
        cards: [
            { rank: 'K', suit: 'Hearts', scored: true },
            { rank: 'K', suit: 'Spades', scored: true },
            { rank: 'K', suit: 'Diamonds', scored: true },
            { rank: '4', suit: 'Clubs', scored: true },
            { rank: '4', suit: 'Hearts', scored: true }
        ]
    },
    flush: {
        description: '5 cards that share the same suit',
        cards: [
            { rank: 'K', suit: 'Spades', scored: true },
            { rank: '10', suit: 'Spades', scored: true },
            { rank: '7', suit: 'Spades', scored: true },
            { rank: '6', suit: 'Spades', scored: true },
            { rank: '2', suit: 'Spades', scored: true }
        ]
    },
    straight: {
        description: '5 cards in a row (consecutive ranks)',
        cards: [
            { rank: '9', suit: 'Hearts', scored: true },
            { rank: '8', suit: 'Spades', scored: true },
            { rank: '7', suit: 'Clubs', scored: true },
            { rank: '6', suit: 'Diamonds', scored: true },
            { rank: '5', suit: 'Hearts', scored: true }
        ]
    },
    three_of_a_kind: {
        description: '3 cards with the same rank. They may be played with up to 2 other unscored cards',
        cards: [
            { rank: 'Q', suit: 'Diamonds', scored: true },
            { rank: 'Q', suit: 'Spades', scored: true },
            { rank: 'Q', suit: 'Hearts', scored: true },
            { rank: '8', suit: 'Clubs', scored: false },
            { rank: '3', suit: 'Diamonds', scored: false }
        ]
    },
    two_pair: {
        description: '2 cards with a matching rank, and 2 cards with a different matching rank. May be played with 1 other unscored card',
        cards: [
            { rank: 'J', suit: 'Clubs', scored: true },
            { rank: 'J', suit: 'Hearts', scored: true },
            { rank: '8', suit: 'Spades', scored: true },
            { rank: '8', suit: 'Diamonds', scored: true },
            { rank: '4', suit: 'Spades', scored: false }
        ]
    },
    pair: {
        description: '2 cards with the same rank. They may be played with up to 3 other unscored cards',
        cards: [
            { rank: '10', suit: 'Diamonds', scored: true },
            { rank: '10', suit: 'Spades', scored: true },
            { rank: 'K', suit: 'Hearts', scored: false },
            { rank: '7', suit: 'Clubs', scored: false },
            { rank: '3', suit: 'Hearts', scored: false }
        ]
    },
    high_card: {
        description: 'If the played hand is not any of the above hands, only the highest ranked card scores',
        cards: [
            { rank: 'A', suit: 'Spades', scored: true },
            { rank: 'Q', suit: 'Diamonds', scored: false },
            { rank: '9', suit: 'Diamonds', scored: false },
            { rank: '5', suit: 'Clubs', scored: false },
            { rank: '2', suit: 'Diamonds', scored: false }
        ]
    }
};

function RunInfoModal({ isOpen, onClose, gameData }) {
    const [activeTab, setActiveTab] = useState('hands'); // 'hands' | 'blinds' | 'vouchers'
    const [hoveredHand, setHoveredHand] = useState(null);
    const [hoveredVoucher, setHoveredVoucher] = useState(null);

    // Close on Escape or switch tabs with keyboard
    useEffect(() => {
        if (!isOpen) return;

        const handleKeyDown = (e) => {
            if (e.key === 'Escape') {
                onClose();
            } else if (e.key === 'ArrowLeft' || e.key.toLowerCase() === 'q') {
                setActiveTab(prev => (prev === 'vouchers' ? 'blinds' : (prev === 'blinds' ? 'hands' : 'hands')));
            } else if (e.key === 'ArrowRight' || e.key.toLowerCase() === 'e') {
                setActiveTab(prev => (prev === 'hands' ? 'blinds' : (prev === 'blinds' ? 'vouchers' : 'vouchers')));
            }
        };

        window.addEventListener('keydown', handleKeyDown);
        return () => window.removeEventListener('keydown', handleKeyDown);
    }, [isOpen, onClose]);

    if (!isOpen) return null;

    const handLevels = gameData?.handLevels || gameData?.pokerHandLevels || {};
    const statsHistory = gameData?.pokerHandPlayed || gameData?.stats?.handsHistory || {};

    const pokerHands = POKER_HAND_DEFINITIONS.map(def => {
        const level = getHandLevel(handLevels, def.name);
        const played = getHandPlayed(statsHistory, def.name);
        const chips = def.baseChips + (level - 1) * def.chipLvl;
        const mult = def.baseMult + (level - 1) * def.multLvl;

        return {
            id: def.id,
            name: def.name,
            level,
            chips,
            mult,
            played
        };
    });

    const ante = gameData?.ante || 1;
    const blindIndex = gameData?.blindIndex || 0;
    const availableBlinds = gameData?.availableBlinds || [];

    const baseScore = 300 * Math.pow(1.5, Math.max(0, ante - 1));
    const smallScore = Math.round(baseScore);
    const bigScore = Math.round(baseScore * 1.5);
    const bossScore = Math.round(baseScore * 2);

    const smallBlindData = availableBlinds[0] ? {
        ...availableBlinds[0],
        status: availableBlinds[0].isDefeated ? 'Defeated' : blindIndex === 0 ? 'Current' : 'Upcoming'
    } : {
        type: 'small',
        blind: 'SmallBlind',
        title: 'Small Blind',
        score: smallScore,
        reward: '$$$+',
        status: blindIndex === 0 ? 'Current' : blindIndex > 0 ? 'Defeated' : 'Upcoming'
    };

    const bigBlindData = availableBlinds[1] ? {
        ...availableBlinds[1],
        status: availableBlinds[1].isDefeated ? 'Defeated' : blindIndex === 1 ? 'Current' : 'Upcoming'
    } : {
        type: 'big',
        blind: 'BigBlind',
        title: 'Big Blind',
        score: bigScore,
        reward: '$$$$+',
        status: blindIndex === 1 ? 'Current' : blindIndex > 1 ? 'Defeated' : 'Upcoming'
    };

    const bossBlindData = availableBlinds[2] ? {
        ...availableBlinds[2],
        status: availableBlinds[2].isDefeated ? 'Defeated' : blindIndex === 2 ? 'Current' : 'Upcoming'
    } : {
        type: 'boss',
        blind: gameData?.currentBlind?.blind || 'TheGoad',
        title: gameData?.currentBlind?.title || 'The Goad',
        score: bossScore,
        reward: '$$$$$+',
        description: gameData?.currentBlind?.description || '',
        status: blindIndex === 2 ? 'Current' : 'Upcoming'
    };

    const redeemedVouchers = gameData?.redeemedVouchers || (
        Array.isArray(gameData?.vouchers)
            ? gameData.vouchers.filter(v => v.active || typeof v === 'string').map(v => typeof v === 'string' ? v : v.id)
            : []
    );

    return (
        <div className="run-info-backdrop" onClick={onClose}>
            <div className="run-info-container" onClick={(e) => e.stopPropagation()}>
                {/* TOP TABS */}
                <div className="run-info-tabs-wrapper">
                    <div className="run-info-tab-container">
                        {activeTab === 'hands' && <div className="tab-indicator-arrow" />}
                        <button
                            className={`run-info-tab-btn ${activeTab === 'hands' ? 'active' : ''}`}
                            onClick={() => setActiveTab('hands')}
                        >
                            Poker Hands
                        </button>
                    </div>

                    <div className="run-info-tab-container">
                        {activeTab === 'blinds' && <div className="tab-indicator-arrow" />}
                        <button
                            className={`run-info-tab-btn ${activeTab === 'blinds' ? 'active' : ''}`}
                            onClick={() => setActiveTab('blinds')}
                        >
                            Blinds
                        </button>
                    </div>

                    <div className="run-info-tab-container">
                        {activeTab === 'vouchers' && <div className="tab-indicator-arrow" />}
                        <button
                            className={`run-info-tab-btn ${activeTab === 'vouchers' ? 'active' : ''}`}
                            onClick={() => setActiveTab('vouchers')}
                        >
                            Vouchers
                        </button>
                    </div>
                </div>

                {/* CONTENT AREA */}
                <div className="run-info-content">
                    {/* 1. POKER HANDS TAB */}
                    {activeTab === 'hands' && (() => {
                        const hoveredIndex = hoveredHand ? pokerHands.findIndex(h => h.id === hoveredHand.id) : -1;
                        const isTopHalf = hoveredIndex !== -1 && hoveredIndex <= 4;
                        const posClass = isTopHalf ? 'pos-bottom' : 'pos-top';

                        return (
                            <div className="poker-hands-list">
                                {pokerHands.map((hand) => (
                                    <div
                                        key={hand.id}
                                        className={`poker-hand-row ${hoveredHand?.id === hand.id ? 'is-hovered' : ''}`}
                                        onMouseEnter={() => setHoveredHand(hand)}
                                        onMouseLeave={() => setHoveredHand(null)}
                                    >
                                        {/* Level Badge */}
                                        <div className="hand-level-badge">
                                            lvl.{hand.level || 1}
                                        </div>

                                        {/* Hand Name */}
                                        <div className="hand-name-label">
                                            {hand.name}
                                        </div>

                                        {/* Chips & Mult Badge */}
                                        <div className="hand-score-pill">
                                            <span className="pill-chips">{hand.chips}</span>
                                            <span className="pill-x">X</span>
                                            <span className="pill-mult">{hand.mult}</span>
                                        </div>

                                        {/* Played Count */}
                                        <div className="hand-played-badge">
                                            <span className="hash-sym">#</span>
                                            <span className="played-count">{hand.played ?? 0}</span>
                                        </div>
                                    </div>
                                ))}

                                {/* HOVER PREVIEW TOOLTIP */}
                                {hoveredHand && POKER_HAND_INFO[hoveredHand.id] && (
                                    <div className={`hand-preview-tooltip ${posClass}`}>
                                        <div className="tooltip-description">
                                            {POKER_HAND_INFO[hoveredHand.id].description}
                                        </div>
                                        <div className="tooltip-cards-row">
                                            {POKER_HAND_INFO[hoveredHand.id].cards.map((card, cIdx) => (
                                                <div
                                                    key={cIdx}
                                                    className={`tooltip-card-item ${card.scored ? 'is-scored' : 'is-unscored'}`}
                                                >
                                                    <PlayingCard
                                                        rank={card.rank}
                                                        suit={card.suit}
                                                        width={44}
                                                        height={62}
                                                    />
                                                </div>
                                            ))}
                                        </div>
                                    </div>
                                )}
                            </div>
                        );
                    })()}

                    {/* 2. BLINDS TAB */}
                    {activeTab === 'blinds' && (
                        <div className="run-info-blinds-grid">
                            {/* Small Blind */}
                            <div className="run-info-blind-column">
                                <div className={`blind-status-pill status-${smallBlindData.status.toLowerCase()}`}>
                                    {smallBlindData.status}
                                </div>
                                <div className="blind-col-title type-small">
                                    {smallBlindData.title}
                                </div>
                                <div className="blind-col-token">
                                    <Blind blind="SmallBlind" width={68} height={68} animated={smallBlindData.status === 'Current'} />
                                </div>
                                <div className="blind-col-ability placeholder-ability">
                                    {'\u00A0'}
                                </div>
                                <div className="blind-col-score-box">
                                    <span className="blind-score-lbl">Score at least</span>
                                    <div className="blind-score-target">
                                        <PokerChipMiniIcon />
                                        <span className="score-num">{smallBlindData.score}</span>
                                    </div>
                                    <span className="blind-score-reward">Reward: {smallBlindData.reward}</span>
                                </div>
                            </div>

                            {/* Big Blind */}
                            <div className="run-info-blind-column">
                                <div className={`blind-status-pill status-${bigBlindData.status.toLowerCase()}`}>
                                    {bigBlindData.status}
                                </div>
                                <div className="blind-col-title type-big">
                                    {bigBlindData.title}
                                </div>
                                <div className="blind-col-token">
                                    <Blind blind="BigBlind" width={68} height={68} animated={bigBlindData.status === 'Current'} />
                                </div>
                                <div className="blind-col-ability placeholder-ability">
                                    {'\u00A0'}
                                </div>
                                <div className="blind-col-score-box">
                                    <span className="blind-score-lbl">Score at least</span>
                                    <div className="blind-score-target">
                                        <PokerChipMiniIcon />
                                        <span className="score-num">{bigBlindData.score}</span>
                                    </div>
                                    <span className="blind-score-reward">Reward: {bigBlindData.reward}</span>
                                </div>
                            </div>

                            {/* Boss Blind */}
                            <div className="run-info-blind-column boss-column">
                                <div className={`blind-status-pill status-${bossBlindData.status.toLowerCase()}`}>
                                    {bossBlindData.status}
                                </div>
                                <div className="blind-col-title type-boss">
                                    {bossBlindData.title}
                                </div>
                                <div className="blind-col-token">
                                    <Blind blind={bossBlindData.blind} width={68} height={68} animated={bossBlindData.status === 'Current'} />
                                </div>
                                {bossBlindData.description ? (
                                    <div className="blind-col-ability">
                                        {bossBlindData.description}
                                    </div>
                                ) : null}
                                <div className="blind-col-score-box">
                                    <span className="blind-score-lbl">Score at least</span>
                                    <div className="blind-score-target">
                                        <PokerChipMiniIcon />
                                        <span className="score-num">{bossBlindData.score}</span>
                                    </div>
                                    <span className="blind-score-reward">Reward: {bossBlindData.reward}</span>
                                </div>
                            </div>
                        </div>
                    )}

                    {/* 3. VOUCHERS TAB */}
                    {activeTab === 'vouchers' && (
                        <div className="run-info-vouchers-view">
                            <h3 className="vouchers-redeemed-heading">
                                Vouchers redeemed this run
                            </h3>
                            <div className="vouchers-redeemed-panel">
                                {redeemedVouchers.length > 0 ? (
                                    <div className="vouchers-grid-list">
                                        {redeemedVouchers.map((vItem, idx) => {
                                            const vId = typeof vItem === 'string' ? vItem : vItem.id;
                                            const info = VOUCHER_INFO[vId] || {
                                                title: vItem.title || vId,
                                                desc: vItem.desc || vItem.description || 'Active voucher bonus'
                                            };
                                            const isHovered = hoveredVoucher === vId || hoveredVoucher === idx;

                                            return (
                                                <div
                                                    key={idx}
                                                    className="redeemed-voucher-slot"
                                                    onMouseEnter={() => setHoveredVoucher(vId)}
                                                    onMouseLeave={() => setHoveredVoucher(null)}
                                                >
                                                    {/* TOOLTIP POPUP */}
                                                    {isHovered && (
                                                        <div className="voucher-hover-tooltip">
                                                            <div className="voucher-tooltip-header">
                                                                {info.title}
                                                            </div>
                                                            <div className="voucher-tooltip-desc-card">
                                                                {formatVoucherDescription(info.desc)}
                                                            </div>
                                                            <div className="voucher-tooltip-badge">
                                                                Voucher
                                                            </div>
                                                        </div>
                                                    )}

                                                    <div className="voucher-card-item">
                                                        <Voucher
                                                            voucher={vId}
                                                            width={84}
                                                            height={118}
                                                        />
                                                    </div>
                                                </div>
                                            );
                                        })}
                                    </div>
                                ) : (
                                    <div className="vouchers-empty-state" />
                                )}
                            </div>
                        </div>
                    )}
                </div>

                {/* BOTTOM BACK BUTTON */}
                <div className="run-info-footer">
                    <button className="run-info-back-btn" onClick={onClose}>
                        Back
                    </button>
                </div>
            </div>
        </div>
    );
}

export default RunInfoModal;
