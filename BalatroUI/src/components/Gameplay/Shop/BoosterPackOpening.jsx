import { useState, useMemo, useEffect } from 'react';
import Balatro from '../../BalatroBackground/BalatroBackground';
import JokerCard from '../../JokerCard/JokerCard';
import TarotCard from '../../TarotCard/TarotCard';
import PlanetCard from '../../PlanetCard/PlanetCard';
import PlayingCard from '../../PlayingCard/PlayingCard';
import SpectralCard from '../../SpectralCard/SpectralCard';
import CardBack from '../../CardBack/CardBack';
import DeckViewModal from '../GameBoard/DeckViewModal';
import DeckHoverPreview from '../GameBoard/DeckHoverPreview';
import './BoosterPackOpening.css';

// Default hand cards shown for Spectral pack / reference (matches screenshot 1)
const DEFAULT_DISPLAY_HAND = [
    { rank: 'J', suit: 'Spades' },
    { rank: '7', suit: 'Spades' },
    { rank: '5', suit: 'Hearts' },
    { rank: '10', suit: 'Clubs' },
    { rank: '8', suit: 'Clubs' },
    { rank: '9', suit: 'Diamonds' },
    { rank: '3', suit: 'Diamonds' },
    { rank: '2', suit: 'Diamonds' }
];

// Planet hand mapping & descriptions matching screenshot 3
const PLANET_HAND_MAP = {
    Pluto: { hand: 'High Card', mult: '+1 Mult', chips: '+10 chips', typeName: 'Dwarf Planet' },
    Mercury: { hand: 'Pair', mult: '+1 Mult', chips: '+15 chips', typeName: 'Planet' },
    Venus: { hand: 'Three of a Kind', mult: '+2 Mult', chips: '+20 chips', typeName: 'Planet' },
    Earth: { hand: 'Full House', mult: '+2 Mult', chips: '+25 chips', typeName: 'Planet' },
    Mars: { hand: 'Four of a Kind', mult: '+3 Mult', chips: '+30 chips', typeName: 'Planet' },
    Jupiter: { hand: 'Flush', mult: '+2 Mult', chips: '+15 chips', typeName: 'Planet' },
    Saturn: { hand: 'Straight', mult: '+3 Mult', chips: '+30 chips', typeName: 'Planet' },
    Uranus: { hand: 'Two Pair', mult: '+1 Mult', chips: '+20 chips', typeName: 'Planet' },
    Neptune: { hand: 'Straight Flush', mult: '+4 Mult', chips: '+40 chips', typeName: 'Planet' }
};

// Particle generator for background ember / star squares
function BoosterParticles({ color = '#0094ff', count = 35 }) {
    const particles = useMemo(() => {
        return Array.from({ length: count }).map((_, i) => ({
            id: i,
            left: `${Math.random() * 100}%`,
            top: `${Math.random() * 100}%`,
            size: `${Math.floor(Math.random() * 6 + 3)}px`,
            opacity: (Math.random() * 0.5 + 0.3).toFixed(2),
            duration: `${(Math.random() * 6 + 4).toFixed(1)}s`,
            delay: `${(Math.random() * 4).toFixed(1)}s`
        }));
    }, [count]);

    return (
        <div className="booster-particles-layer">
            {particles.map(p => (
                <div
                    key={p.id}
                    className="booster-particle"
                    style={{
                        left: p.left,
                        top: p.top,
                        width: p.size,
                        height: p.size,
                        backgroundColor: color,
                        boxShadow: `0 0 8px ${color}`,
                        opacity: p.opacity,
                        animationDuration: p.duration,
                        animationDelay: p.delay
                    }}
                />
            ))}
        </div>
    );
}

// Tooltip component tailored specifically to screenshot requirements
function BoosterCardTooltip({ card }) {
    if (!card) return null;

    if (card.type === 'spectral') {
        return (
            <div className="booster-card-tooltip spectral-tooltip">
                <div className="tooltip-header-badge">
                    Not Discovered
                </div>
                <div className="tooltip-main-box">
                    <div className="tooltip-desc-text">
                        {card.description}
                    </div>
                </div>
            </div>
        );
    }

    if (card.type === 'playingCard') {
        const isRed = card.suit === 'Hearts' || card.suit === 'Diamonds';
        return (
            <div className="booster-card-tooltip playing-card-tooltip">
                <div className={`tooltip-card-title ${isRed ? 'suit-red' : 'suit-black'}`}>
                    {card.title}
                </div>
                <div className="tooltip-chips-badge">
                    {card.chipsBonus || '+10 chips'}
                </div>
            </div>
        );
    }

    if (card.type === 'planet') {
        const planetInfo = PLANET_HAND_MAP[card.id] || {
            hand: 'Poker Hand',
            mult: '+1 Mult',
            chips: '+15 chips',
            typeName: 'Planet'
        };

        return (
            <div className="booster-card-tooltip planet-tooltip">
                <div className="planet-tooltip-title">
                    {card.title || card.id}
                </div>
                <div className="planet-tooltip-body">
                    <div className="lvl-prefix">[lvl.1] Level up</div>
                    <div className="hand-highlight">{planetInfo.hand}</div>
                    <div className="mult-chips-line">
                        <span className="mult-highlight">{planetInfo.mult}</span> and
                    </div>
                    <div className="chips-highlight">{planetInfo.chips}</div>
                </div>
                <div className="planet-type-pill">
                    {planetInfo.typeName}
                </div>
            </div>
        );
    }

    if (card.type === 'tarot') {
        return (
            <div className="booster-card-tooltip tarot-tooltip">
                <div className="tooltip-title">{card.title}</div>
                <div className="tooltip-desc-text">{card.description}</div>
                <div className="tooltip-pill tarot-pill">Tarot</div>
            </div>
        );
    }

    if (card.type === 'joker') {
        return (
            <div className="booster-card-tooltip joker-tooltip">
                <div className="tooltip-title">{card.title}</div>
                <div className="tooltip-desc-text">{card.description}</div>
                <div className={`tooltip-pill rarity-${(card.rarity || 'common').toLowerCase()}`}>
                    {card.rarity || 'Common'}
                </div>
            </div>
        );
    }

    return null;
}

function BoosterPackOpening({
    pack,
    cards,
    gameData,
    jokers,
    consumables,
    maxJokers = 5,
    maxConsumables = 2,
    activeSlot,
    onToggleJoker,
    onToggleConsumable,
    onSellJoker,
    onSellConsumable,
    onUseConsumable,
    onPickCard,
    onSkip,
    picksRemaining = 1
}) {
    // Hovered card index in pack choices
    const [hoveredCardIndex, setHoveredCardIndex] = useState(0);
    // Selected card index (for controller/click focus)
    const [focusedCardIndex, setFocusedCardIndex] = useState(0);

    // Keep focus within bounds if cards array shrinks after picking
    useEffect(() => {
        if (cards?.length > 0) {
            if (focusedCardIndex >= cards.length) {
                setFocusedCardIndex(Math.max(0, cards.length - 1));
            }
            if (hoveredCardIndex >= cards.length) {
                setHoveredCardIndex(Math.max(0, cards.length - 1));
            }
        }
    }, [cards?.length, focusedCardIndex, hoveredCardIndex]);

    // Deck modal
    const [isDeckModalOpen, setIsDeckModalOpen] = useState(false);
    const [isDeckHovered, setIsDeckHovered] = useState(false);

    // Selected hand cards (for targeting in spectral mode)
    const [selectedHandIndex, setSelectedHandIndex] = useState(null);

    // Color theme configuration
    const packKind = pack?.packKind || 'Spectral';
    const isSpectral = packKind === 'Spectral';
    const isStandard = packKind === 'Standard';
    const isCelestial = packKind === 'Celestial';
    const isArcana = packKind === 'Arcana';
    const isBuffoon = packKind === 'Buffoon';

    const themeColors = useMemo(() => {
        if (isSpectral) {
            return { color1: '#002b66', color2: '#0066cc', color3: '#020d1a', border: '#0094ff', particle: '#0094ff' };
        }
        if (isStandard) {
            return { color1: '#851212', color2: '#c92a2a', color3: '#180404', border: '#fe4747', particle: '#fe4747' };
        }
        if (isCelestial) {
            return { color1: '#062d3e', color2: '#028090', color3: '#041118', border: '#00b4d8', particle: '#00b4d8' };
        }
        if (isArcana) {
            return { color1: '#5e1255', color2: '#9b2c8c', color3: '#150517', border: '#b545d6', particle: '#b545d6' };
        }
        return { color1: '#8a4b08', color2: '#c67817', color3: '#180d03', border: '#ff9d00', particle: '#ff9d00' };
    }, [isSpectral, isStandard, isCelestial, isArcana, isBuffoon]);

    const isUseAction = pack?.action === 'use_immediately' || isSpectral || isCelestial || isArcana;
    const actionLabel = isUseAction ? 'USE' : 'SELECT';
    const actionBtnClass = isUseAction ? 'btn-use-red' : 'btn-select-green';

    // Player hand to display (uses real handCards if available, or default sample hand matching screenshot)
    const handCards = gameData?.handCards?.length ? gameData.handCards : DEFAULT_DISPLAY_HAND;

    return (
        <div className={`booster-opening-screen pack-${packKind.toLowerCase()}`}>
            {/* 1. DYNAMIC SHADER BACKGROUND */}
            <div className="booster-background-layer">
                <Balatro
                    spinRotation={-0.8}
                    spinSpeed={2.2}
                    spinAmount={0.12}
                    spinEase={0.7}
                    contrast={3.0}
                    lighting={0.3}
                    pixelFilter={850}
                    isRotate={false}
                    mouseInteraction={true}
                    color1={themeColors.color1}
                    color2={themeColors.color2}
                    color3={themeColors.color3}
                />
                <BoosterParticles color={themeColors.particle} count={35} />
            </div>

            {/* 2. MAIN BOOSTER PACK CONTENT AREA */}
            <div className="booster-content-container">
                {/* TOP SLOTS: JOKERS & CONSUMABLES */}
                <div className="shop-top-area booster-top-area" onClick={(e) => e.stopPropagation()}>
                    {/* JOKERS CONTAINER */}
                    <div className="jokers-container-wrapper">
                        <div className="jokers-slots-box">
                            {Array.from({ length: maxJokers }).map((_, index) => {
                                const joker = jokers[index];
                                const isSelected = activeSlot?.type === 'joker' && activeSlot.index === index;
                                return (
                                    <div
                                        key={index}
                                        className={`joker-slot ${joker ? 'occupied' : 'empty'}`}
                                    >
                                        {joker ? (
                                            <JokerCard
                                                id={joker.id}
                                                width={78}
                                                height={108}
                                                animated={true}
                                                isSelected={isSelected}
                                                onSelect={(e) => {
                                                    e.stopPropagation();
                                                    onToggleJoker(index);
                                                }}
                                                onSell={() => onSellJoker(index)}
                                                sellPrice={joker.sellPrice || 2}
                                            />
                                        ) : null}
                                    </div>
                                );
                            })}
                        </div>
                        <div className="slot-counter-text">
                            {jokers.length}/{maxJokers}
                        </div>
                    </div>

                    {/* CONSUMABLES CONTAINER */}
                    <div className="consumables-container-wrapper">
                        <div className="consumables-slots-box">
                            {Array.from({ length: maxConsumables }).map((_, index) => {
                                const consumable = consumables[index];
                                const isSelected = activeSlot?.type === 'consumable' && activeSlot.index === index;
                                return (
                                    <div
                                        key={index}
                                        className={`consumable-slot ${consumable ? 'occupied' : 'empty'}`}
                                    >
                                        {consumable ? (
                                            consumable.type === 'planet' ? (
                                                <PlanetCard
                                                    planet={consumable.id}
                                                    width={78}
                                                    height={108}
                                                    animated={true}
                                                    isSelected={isSelected}
                                                    onSelect={(e) => {
                                                        e.stopPropagation();
                                                        onToggleConsumable(index);
                                                    }}
                                                    onSell={() => onSellConsumable(index)}
                                                    onUse={() => onUseConsumable(index)}
                                                    sellPrice={consumable.sellPrice || 1}
                                                />
                                            ) : (
                                                <TarotCard
                                                    tarot={consumable.id}
                                                    width={78}
                                                    height={108}
                                                    animated={true}
                                                    isSelected={isSelected}
                                                    onSelect={(e) => {
                                                        e.stopPropagation();
                                                        onToggleConsumable(index);
                                                    }}
                                                    onSell={() => onSellConsumable(index)}
                                                    onUse={() => onUseConsumable(index)}
                                                    sellPrice={consumable.sellPrice || 1}
                                                />
                                            )
                                        ) : null}
                                    </div>
                                );
                            })}
                        </div>
                        <div className="slot-counter-text align-right">
                            {consumables.length}/{maxConsumables}
                        </div>
                    </div>
                </div>

                {/* PLAYER HAND DISPLAY ROW (Matches screenshot 1 for spectral/tarot cards) */}
                {isSpectral && (
                    <div className="booster-player-hand-section">
                        <div className="booster-hand-cards-row">
                            {handCards.map((card, idx) => {
                                const isSelected = selectedHandIndex === idx;
                                return (
                                    <div
                                        key={idx}
                                        className={`booster-hand-card-item ${isSelected ? 'hand-card-selected' : ''}`}
                                        onClick={() => setSelectedHandIndex(isSelected ? null : idx)}
                                    >
                                        <PlayingCard
                                            rank={card.rank}
                                            suit={card.suit}
                                            width={72}
                                            height={100}
                                        />
                                    </div>
                                );
                            })}
                        </div>
                    </div>
                )}

                {/* CENTER AREA: BOOSTER CARDS CHOICES */}
                <div className="booster-center-stage">
                    <div className="booster-cards-display-row">
                        {cards.map((card, idx) => {
                            const isFocused = (focusedCardIndex === idx) || (hoveredCardIndex === idx);

                            return (
                                <div
                                    key={card.cardInstanceId || idx}
                                    className={`booster-card-wrapper ${isFocused ? 'is-elevated' : ''}`}
                                    onMouseEnter={() => {
                                        setHoveredCardIndex(idx);
                                        setFocusedCardIndex(idx);
                                    }}
                                    onClick={() => setFocusedCardIndex(idx)}
                                >
                                    {/* TOOLTIP POPPING DIRECTLY ABOVE CARD */}
                                    {isFocused && (
                                        <BoosterCardTooltip card={card} />
                                    )}

                                    {/* CARD VISUAL COMPONENT */}
                                    <div className="booster-card-visual-box">
                                        {card.type === 'spectral' && (
                                            <SpectralCard
                                                spectral={card.spriteId || card.id}
                                                width={90}
                                                height={126}
                                                animated={true}
                                            />
                                        )}

                                        {card.type === 'playingCard' && (
                                            <PlayingCard
                                                rank={card.rank}
                                                suit={card.suit}
                                                width={90}
                                                height={126}
                                            />
                                        )}

                                        {card.type === 'planet' && (
                                            <PlanetCard
                                                planet={card.id}
                                                spriteId={card.spriteId}
                                                title={card.title}
                                                description={card.description}
                                                width={90}
                                                height={126}
                                                animated={true}
                                            />
                                        )}

                                        {card.type === 'tarot' && (
                                            <TarotCard
                                                tarot={card.id}
                                                spriteId={card.spriteId}
                                                title={card.title}
                                                description={card.description}
                                                width={90}
                                                height={126}
                                                animated={true}
                                            />
                                        )}

                                        {card.type === 'joker' && (
                                            <JokerCard
                                                id={card.id}
                                                spriteId={card.spriteId}
                                                title={card.title}
                                                description={card.description}
                                                rarity={card.rarity}
                                                width={90}
                                                height={126}
                                                animated={true}
                                                showHoverTooltip={false}
                                            />
                                        )}
                                    </div>

                                    {/* ACTION BUTTON ATTACHED TO RIGHT SIDE OF FOCUSED CARD */}
                                    {isFocused && (
                                        <div
                                            className={`booster-action-tab ${actionBtnClass}`}
                                            onClick={(e) => {
                                                e.stopPropagation();
                                                onPickCard(card, idx);
                                            }}
                                            title={`${actionLabel} Card`}
                                        >
                                            <div className="controller-keycap">Rb</div>
                                            <div className="action-tab-label">{actionLabel}</div>
                                        </div>
                                    )}
                                </div>
                            );
                        })}
                    </div>
                </div>

                {/* BOTTOM BANNER: PACK NAME & SKIP BUTTON */}
                <div className="booster-bottom-bar">
                    {/* PACK TITLE BANNER */}
                    <div
                        className="booster-title-banner"
                        style={{ borderColor: themeColors.border }}
                    >
                        <div className="banner-pack-title">
                            {pack?.title || `${packKind} Pack`}
                        </div>
                        <div className="banner-pack-subtitle">
                            Choose {picksRemaining}
                        </div>
                    </div>

                    {/* SKIP BUTTON */}
                    <button
                        type="button"
                        className="booster-skip-btn"
                        onClick={onSkip}
                        title="Skip remaining choices and return to Shop"
                    >
                        <span className="skip-text">Skip</span>
                        <span className="controller-btn-badge">Y</span>
                    </button>
                </div>
            </div>

            {/* 3. BOTTOM RIGHT: DECK COUNTER AREA */}
            {isDeckHovered && (
                <DeckHoverPreview
                    gameData={gameData}
                    handCards={gameData?.handCards || []}
                />
            )}

            <div
                className="game-deck-area"
                onClick={() => setIsDeckModalOpen(true)}
                onMouseEnter={() => setIsDeckHovered(true)}
                onMouseLeave={() => setIsDeckHovered(false)}
                title="Click to Peek Deck"
            >
                <div className="deck-card-stack">
                    <div className="deck-card-visual">
                        <CardBack
                            type="BackNormal"
                            width={80}
                            height={112}
                        />
                    </div>
                </div>

                <div className="deck-count-text">
                    {gameData?.deckRemaining || 52}/52
                </div>
            </div>

            {/* PEEK DECK MODAL */}
            <DeckViewModal
                isOpen={isDeckModalOpen}
                onClose={() => setIsDeckModalOpen(false)}
                gameData={gameData}
            />
        </div>
    );
}

export default BoosterPackOpening;
