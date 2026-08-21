import { useState } from 'react';
import GameSidebar from '../GameSidebar/GameSidebar';
import JokerCard from '../../JokerCard/JokerCard';
import TarotCard from '../../TarotCard/TarotCard';
import PlanetCard from '../../PlanetCard/PlanetCard';
import Blind from '../../Blind/Blind';
import CardBack from '../../CardBack/CardBack';
import DeckViewModal from '../GameBoard/DeckViewModal';
import './Cashout.css';

const defaultJokers = [
    { id: 'ScaryFace', title: 'Scary Face' },
    { id: 'Joker', title: 'Joker' },
    { id: 'RaisedFist', title: 'Raised Fist' },
    { id: 'AbstractJoker', title: 'Abstract Joker' }
];

const defaultConsumables = [
    { type: 'tarot', id: 'TheEmperor', title: 'The Emperor' },
    { type: 'planet', id: 'Mars', title: 'Mars' }
];

function Cashout({
    gameData,
    onContinue,
    onOpenSettings
}) {
    const [isDeckModalOpen, setIsDeckModalOpen] = useState(false);

    const maxJokers = gameData?.maxJokers || 5;
    const jokers = gameData?.jokers || defaultJokers;

    const maxConsumables = gameData?.maxConsumables || 2;
    const consumables = gameData?.consumables || defaultConsumables;

    // Calculate blind key and rewards
    const blindKey = gameData?.currentBlind?.blind || (
        gameData?.currentBlind?.type === 'big' ? 'BigBlind' :
        gameData?.currentBlind?.type === 'boss' ? 'TheGoad' : 'SmallBlind'
    );

    const blindScoreTarget = (gameData?.targetScore || 2800).toLocaleString();

    // Reward breakdown
    const blindRewardDollars = gameData?.currentBlind?.type === 'big' ? 4 :
        gameData?.currentBlind?.type === 'boss' ? 5 : 3;
    const blindRewardSymbol = '$'.repeat(blindRewardDollars);

    const remainingHands = gameData?.hands ?? 2;
    const remainingHandsDollars = remainingHands * 1;
    const remainingHandsSymbol = '$'.repeat(Math.max(1, remainingHandsDollars));

    const interestDollars = Math.min(5, Math.floor((gameData?.money || 5) / 5));
    const interestSymbol = '$'.repeat(Math.max(1, interestDollars));

    const totalCashout = blindRewardDollars + remainingHandsDollars + interestDollars;

    return (
        <div className="cashout-screen">
            {/* LEFT GAME SIDEBAR */}
            <GameSidebar
                gameData={gameData}
                onOpenSettings={onOpenSettings}
                isBlindSelection={false}
            />

            {/* MAIN CASHOUT AREA */}
            <section className="cashout-main">
                {/* 1. TOP CONTAINERS: JOKERS + CONSUMABLES */}
                <div className="cashout-top-area">
                    {/* JOKERS CONTAINER */}
                    <div className="jokers-container-wrapper">
                        <div className="jokers-slots-box">
                            {Array.from({ length: maxJokers }).map((_, index) => {
                                const joker = jokers[index];
                                return (
                                    <div
                                        key={index}
                                        className={`joker-slot ${joker ? 'occupied' : 'empty'}`}
                                    >
                                        {joker ? (
                                            <div className="card-item-hover" title={joker.title || joker.id}>
                                                <JokerCard
                                                    id={joker.id}
                                                    width={78}
                                                    height={108}
                                                    animated={true}
                                                />
                                            </div>
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
                                return (
                                    <div
                                        key={index}
                                        className={`consumable-slot ${consumable ? 'occupied' : 'empty'}`}
                                    >
                                        {consumable ? (
                                            <div className="card-item-hover" title={consumable.title || consumable.id}>
                                                {consumable.type === 'planet' ? (
                                                    <PlanetCard
                                                        planet={consumable.id}
                                                        width={78}
                                                        height={108}
                                                        animated={true}
                                                    />
                                                ) : (
                                                    <TarotCard
                                                        tarot={consumable.id}
                                                        width={78}
                                                        height={108}
                                                        animated={true}
                                                    />
                                                )}
                                            </div>
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

                {/* 2. CENTER: CASHOUT DIALOGUE BOX */}
                <div className="cashout-center-container">
                    <div className="cashout-box">
                        {/* CASHOUT BUTTON HEADER */}
                        <button
                            type="button"
                            className="cashout-btn-header"
                            onClick={() => onContinue(totalCashout)}
                        >
                            Cash Out: ${totalCashout}
                        </button>

                        {/* ROW 1: BLIND SCORE & REWARD */}
                        <div className="cashout-row blind-row">
                            <div className="cashout-blind-icon">
                                <Blind
                                    blind={blindKey}
                                    width={48}
                                    height={48}
                                    animated={true}
                                />
                            </div>

                            <div className="cashout-row-center">
                                <span className="row-sub-label">Score at least</span>
                                <div className="row-target-score">
                                    <span className="chip-token">✱</span> {blindScoreTarget}
                                </div>
                            </div>

                            <div className="cashout-row-reward">
                                {blindRewardSymbol}
                            </div>
                        </div>

                        <div className="cashout-divider" />

                        {/* ROW 2: REMAINING HANDS */}
                        <div className="cashout-row item-row">
                            <div className="row-stat-num hands-num">
                                {remainingHands}
                            </div>

                            <div className="cashout-row-text">
                                Remaining Hands ($1 each)
                            </div>

                            <div className="cashout-row-reward">
                                {remainingHandsSymbol}
                            </div>
                        </div>

                        {/* ROW 3: INTEREST */}
                        <div className="cashout-row item-row">
                            <div className="row-stat-num interest-num">
                                {interestDollars}
                            </div>

                            <div className="cashout-row-text">
                                1 interest per $5 (5 max)
                            </div>

                            <div className="cashout-row-reward">
                                {interestSymbol}
                            </div>
                        </div>
                    </div>
                </div>

                {/* 3. DECK COUNTER (BOTTOM RIGHT) */}
                <div
                    className="game-deck-area"
                    onClick={() => setIsDeckModalOpen(true)}
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
                        {gameData?.deckRemaining || 55}/55
                    </div>
                </div>

                {/* PEEK DECK MODAL */}
                <DeckViewModal
                    isOpen={isDeckModalOpen}
                    onClose={() => setIsDeckModalOpen(false)}
                    gameData={gameData}
                />
            </section>
        </div>
    );
}

export default Cashout;