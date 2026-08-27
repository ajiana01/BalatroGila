import { useState } from 'react';
import GameSidebar from '../GameSidebar/GameSidebar';
import JokerCard from '../../JokerCard/JokerCard';
import TarotCard from '../../TarotCard/TarotCard';
import PlanetCard from '../../PlanetCard/PlanetCard';
import PlayingCard from '../../PlayingCard/PlayingCard';
import Voucher from '../../Voucher/Voucher';
import BoosterPack from '../../BoosterPacks/BoosterPacks';
import CardBack from '../../CardBack/CardBack';
import DeckViewModal from '../GameBoard/DeckViewModal';
import DeckHoverPreview from '../GameBoard/DeckHoverPreview';
import { generateShopCards, getAnteVoucher, generateBoosterPacks, generateBoosterCards, SHOP_JOKERS } from '../../../data/shopData';
import BoosterPackOpening from './BoosterPackOpening';
import './Shop.css';

const defaultJokers = [
    { id: 'ScaryFace', title: 'Scary Face', sellPrice: 2 }
];

const defaultConsumables = [
    { type: 'tarot', id: 'TheSun', title: 'The Sun', sellPrice: 1 }
];

function ShopItemTooltip({ item, side = 'left' }) {
    if (!item) return null;
    return (
        <div className={`shop-item-tooltip-box pos-${side}`}>
            <div className="tooltip-title">
                {item.title || 'Card Info'}
            </div>

            <div className="tooltip-description">
                {item.description}
            </div>

            {item.rarity && (
                <div className={`tooltip-rarity-pill rarity-${(item.rarity || 'common').toLowerCase().replace(' ', '-')}`}>
                    {item.rarity}
                </div>
            )}
        </div>
    );
}

function Shop({
    gameData,
    onContinue,
    onOpenSettings
}) {
    // Money state synchronized with gameData
    const [money, setMoney] = useState(gameData?.money ?? 10);

    // Deck view modal & hover preview
    const [isDeckModalOpen, setIsDeckModalOpen] = useState(false);
    const [isDeckHovered, setIsDeckHovered] = useState(false);

    // Jokers & Consumables inventory
    const maxJokers = gameData?.maxJokers || 5;
    const [jokers, setJokers] = useState(gameData?.jokers || defaultJokers);

    const maxConsumables = gameData?.maxConsumables || 2;
    const [consumables, setConsumables] = useState(gameData?.consumables || defaultConsumables);

    // Active selected slot in top container (for selling/using)
    const [activeSlot, setActiveSlot] = useState(null);

    // Reroll cost state (starts at $5)
    const [rerollCost, setRerollCost] = useState(5);

    // Shop Cards (default 2 cards, starting with Ancient Joker and another item)
    const [shopCards, setShopCards] = useState(() => {
        const initial = generateShopCards(2);
        // Ensure first card is Ancient Joker like screenshot if available
        const ancientJoker = SHOP_JOKERS.find(j => j.id === 'AncientJoker') || SHOP_JOKERS[0];
        initial[0] = {
            slotId: `shop-joker-init-0`,
            type: 'joker',
            id: ancientJoker.id,
            title: ancientJoker.title,
            rarity: ancientJoker.rarity,
            price: ancientJoker.price,
            description: ancientJoker.description
        };
        return initial;
    });

    // Active tooltip item shown in the description box (null when not hovering)
    const [activeTooltipItem, setActiveTooltipItem] = useState(null);

    // Selected item in shop (for BUY, REDEEM, OPEN action button toggle)
    const [selectedShopItem, setSelectedShopItem] = useState(null);

    // Ante Voucher
    const currentAnte = gameData?.ante || 2;
    const [voucher] = useState(() => getAnteVoucher(currentAnte));
    const [isVoucherPurchased, setIsVoucherPurchased] = useState(false);

    // Booster packs
    const [boosterPacks] = useState(() => generateBoosterPacks());
    const [purchasedBoosters, setPurchasedBoosters] = useState([]);
    const [activeBoosterPack, setActiveBoosterPack] = useState(null);

    // Toast message for feedback
    const [toastMessage, setToastMessage] = useState('');

    const showToast = (msg) => {
        setToastMessage(msg);
        setTimeout(() => {
            setToastMessage('');
        }, 2000);
    };

    // Toggle Selection Handlers
    const handleCardClick = (item, idx) => {
        if (selectedShopItem?.type === 'card' && selectedShopItem.index === idx) {
            setSelectedShopItem(null);
        } else {
            setSelectedShopItem({ type: 'card', index: idx, slotId: item.slotId });
            setActiveTooltipItem(item);
        }
    };

    const handleVoucherClick = () => {
        if (isVoucherPurchased) return;
        if (selectedShopItem?.type === 'voucher') {
            setSelectedShopItem(null);
        } else {
            setSelectedShopItem({ type: 'voucher', slotId: 'voucher' });
            setActiveTooltipItem({
                slotId: 'voucher',
                title: voucher.title,
                description: voucher.description,
                rarity: 'Voucher'
            });
        }
    };

    const handleBoosterClick = (pack, idx) => {
        if (purchasedBoosters.includes(pack.slotId)) return;
        if (selectedShopItem?.type === 'booster' && selectedShopItem.index === idx) {
            setSelectedShopItem(null);
        } else {
            setSelectedShopItem({ type: 'booster', index: idx, slotId: pack.slotId });
            setActiveTooltipItem({
                slotId: pack.slotId,
                title: pack.title,
                description: pack.description,
                rarity: 'Booster'
            });
        }
    };

    // Update player money
    const updateMoney = (newAmount) => {
        setMoney(newAmount);
        if (gameData) {
            gameData.money = newAmount;
        }
    };

    // =========================================
    // TOP BAR INTERACTIONS (SELL / USE)
    // =========================================
    const handleToggleJoker = (index) => {
        setActiveSlot(prev => {
            if (prev?.type === 'joker' && prev.index === index) {
                return null;
            }
            return { type: 'joker', index };
        });
    };

    const handleToggleConsumable = (index) => {
        setActiveSlot(prev => {
            if (prev?.type === 'consumable' && prev.index === index) {
                return null;
            }
            return { type: 'consumable', index };
        });
    };

    const handleSellJoker = (index) => {
        const joker = jokers[index];
        const sellPrice = joker?.sellPrice || 2;
        const newMoney = money + sellPrice;
        updateMoney(newMoney);

        const updated = jokers.filter((_, i) => i !== index);
        setJokers(updated);
        if (gameData) {
            gameData.jokers = updated;
        }
        setActiveSlot(null);
        showToast(`Sold ${joker.title || 'Joker'} for +$${sellPrice}`);
    };

    const handleSellConsumable = (index) => {
        const consumable = consumables[index];
        const sellPrice = consumable?.sellPrice || 1;
        const newMoney = money + sellPrice;
        updateMoney(newMoney);

        const updated = consumables.filter((_, i) => i !== index);
        setConsumables(updated);
        if (gameData) {
            gameData.consumables = updated;
        }
        setActiveSlot(null);
        showToast(`Sold ${consumable.title || 'Consumable'} for +$${sellPrice}`);
    };

    const handleUseConsumable = (index) => {
        const consumable = consumables[index];
        const updated = consumables.filter((_, i) => i !== index);
        setConsumables(updated);
        if (gameData) {
            gameData.consumables = updated;
        }
        setActiveSlot(null);
        showToast(`Used ${consumable.title || 'Consumable'}`);
    };

    // =========================================
    // SHOP ACTIONS (REROLL / BUY)
    // =========================================
    const handleReroll = () => {
        if (money < rerollCost) {
            showToast("Not enough money to reroll!");
            return;
        }

        const newMoney = money - rerollCost;
        updateMoney(newMoney);
        setRerollCost(prev => prev + 1);

        if (gameData?.stats) {
            gameData.stats.timesRerolled = (gameData.stats.timesRerolled || 0) + 1;
        }

        const newCards = generateShopCards(2);
        setShopCards(newCards);
        setActiveTooltipItem(null);
        showToast(`Rerolled shop cards (-$${rerollCost})`);
    };

    const handleBuyCard = (item, index) => {
        if (item.isSold) return;

        if (money < item.price) {
            showToast("Not enough money!");
            return;
        }

        if (item.type === 'joker') {
            if (jokers.length >= maxJokers) {
                showToast("No space for Jokers (Max 5)!");
                return;
            }
            const newJoker = {
                id: item.id,
                title: item.title,
                sellPrice: Math.max(1, Math.floor(item.price / 2))
            };
            const updated = [...jokers, newJoker];
            setJokers(updated);
            if (gameData) {
                gameData.jokers = updated;
            }
        } else if (item.type === 'tarot' || item.type === 'planet') {
            if (consumables.length >= maxConsumables) {
                showToast("No space for Consumables (Max 2)!");
                return;
            }
            const newConsumable = {
                type: item.type,
                id: item.id,
                title: item.title,
                sellPrice: Math.max(1, Math.floor(item.price / 2))
            };
            const updated = [...consumables, newConsumable];
            setConsumables(updated);
            if (gameData) {
                gameData.consumables = updated;
            }
        } else if (item.type === 'playingCard') {
            if (gameData) {
                gameData.deckRemaining = (gameData.deckRemaining || 52) + 1;
            }
        }

        if (gameData?.stats) {
            gameData.stats.cardsPurchased = (gameData.stats.cardsPurchased || 0) + 1;
        }

        const newMoney = money - item.price;
        updateMoney(newMoney);

        // Mark item as sold and clear active tooltip
        setShopCards(prev => prev.map((c, i) => i === index ? { ...c, isSold: true } : c));
        setActiveTooltipItem(null);
        showToast(`Bought ${item.title} for $${item.price}`);
    };

    const handleBuyVoucher = () => {
        if (isVoucherPurchased) return;
        if (money < voucher.price) {
            showToast("Not enough money for Voucher!");
            return;
        }

        const newMoney = money - voucher.price;
        updateMoney(newMoney);
        setIsVoucherPurchased(true);
        setActiveTooltipItem(null);

        // Apply voucher effects
        if (gameData) {
            gameData.redeemedVouchers = [...(gameData.redeemedVouchers || []), voucher.id];
            if (gameData.stats) {
                gameData.stats.cardsPurchased = (gameData.stats.cardsPurchased || 0) + 1;
            }
        }
        if (voucher.id === 'Wasteful' && gameData) {
            gameData.discards = (gameData.discards || 4) + 1;
        } else if (voucher.id === 'Grabber' && gameData) {
            gameData.hands = (gameData.hands || 4) + 1;
        }

        showToast(`Redeemed Voucher: ${voucher.title}!`);
    };

    const handleBuyBooster = (pack, index) => {
        if (purchasedBoosters.includes(pack.slotId)) return;
        if (money < pack.price) {
            showToast("Not enough money for Booster Pack!");
            return;
        }

        const newMoney = money - pack.price;
        updateMoney(newMoney);
        setPurchasedBoosters(prev => [...prev, pack.slotId]);
        setActiveTooltipItem(null);
        setSelectedShopItem(null);

        if (gameData?.stats) {
            gameData.stats.cardsPurchased = (gameData.stats.cardsPurchased || 0) + 1;
        }

        const generatedCards = generateBoosterCards(pack, gameData);
        const picks = pack.picks_allowed || 1;

        setActiveBoosterPack({
            pack,
            cards: generatedCards,
            picksRemaining: picks
        });

        showToast(`Opened ${pack.title}!`);
    };

    const handlePickBoosterCard = (card, cardIndex) => {
        if (!activeBoosterPack) return;

        const { pack, cards, picksRemaining } = activeBoosterPack;

        // Process card effect
        if (card.type === 'planet') {
            const planetHandMap = {
                Pluto: 'High Card', Mercury: 'Pair', Venus: 'Three of a Kind',
                Earth: 'Full House', Mars: 'Four of a Kind', Jupiter: 'Flush',
                Saturn: 'Straight', Uranus: 'Two Pair', Neptune: 'Straight Flush'
            };
            const handName = planetHandMap[card.id] || 'Hand';
            showToast(`Level up ${handName}!`);
            if (gameData) {
                gameData.currentHandLevel = (gameData.currentHandLevel || 1) + 1;
            }
        } else if (card.type === 'playingCard') {
            if (gameData) {
                gameData.deckRemaining = (gameData.deckRemaining || 52) + 1;
                if (gameData.handCards) {
                    gameData.handCards.push({ rank: card.rank, suit: card.suit });
                }
            }
            showToast(`Added ${card.title} to deck!`);
        } else if (card.type === 'joker') {
            if (jokers.length < maxJokers) {
                const newJoker = {
                    id: card.id,
                    title: card.title,
                    sellPrice: Math.max(1, Math.floor((card.price || 4) / 2))
                };
                const updated = [...jokers, newJoker];
                setJokers(updated);
                if (gameData) {
                    gameData.jokers = updated;
                }
                showToast(`Added ${card.title} to Jokers!`);
            } else {
                showToast("Joker slots full!");
            }
        } else if (card.type === 'tarot') {
            showToast(`Used Tarot: ${card.title}!`);
        } else if (card.type === 'spectral') {
            showToast(`Used Spectral: ${card.title}!`);
        }

        const nextPicks = picksRemaining - 1;
        const remainingCards = cards.filter((_, idx) => idx !== cardIndex);

        if (nextPicks <= 0 || remainingCards.length === 0) {
            setTimeout(() => {
                setActiveBoosterPack(null);
            }, 300);
        } else {
            setActiveBoosterPack({
                ...activeBoosterPack,
                cards: remainingCards,
                picksRemaining: nextPicks
            });
        }
    };

    const handleSkipBooster = () => {
        showToast("Skipped Booster Pack");
        setActiveBoosterPack(null);
    };

    return (
        <div className="shop-screen" onClick={() => setActiveSlot(null)}>
            {/* 1. LEFT GAME SIDEBAR */}
            <GameSidebar
                gameData={{ ...gameData, money }}
                onOpenSettings={onOpenSettings}
                isShop={true}
            />

            {/* 2. MAIN VIEW: BOOSTER PACK OPENING OR MAIN SHOP CANVAS */}
            {activeBoosterPack ? (
                <BoosterPackOpening
                    pack={activeBoosterPack.pack}
                    cards={activeBoosterPack.cards}
                    picksRemaining={activeBoosterPack.picksRemaining}
                    gameData={{ ...gameData, money }}
                    jokers={jokers}
                    consumables={consumables}
                    maxJokers={maxJokers}
                    maxConsumables={maxConsumables}
                    activeSlot={activeSlot}
                    onToggleJoker={handleToggleJoker}
                    onToggleConsumable={handleToggleConsumable}
                    onSellJoker={handleSellJoker}
                    onSellConsumable={handleSellConsumable}
                    onUseConsumable={handleUseConsumable}
                    onPickCard={handlePickBoosterCard}
                    onSkip={handleSkipBooster}
                />
            ) : (
                <section className="shop-main-section">
                    {/* TOAST NOTIFICATION */}
                    {toastMessage && (
                        <div className="shop-toast-notification">
                            {toastMessage}
                        </div>
                    )}

                    {/* TOP CONTAINERS: JOKERS & CONSUMABLES */}
                    <div className="shop-top-area" onClick={(e) => e.stopPropagation()}>
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
                                                        handleToggleJoker(index);
                                                    }}
                                                    onSell={() => handleSellJoker(index)}
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
                                                            handleToggleConsumable(index);
                                                        }}
                                                        onSell={() => handleSellConsumable(index)}
                                                        onUse={() => handleUseConsumable(index)}
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
                                                            handleToggleConsumable(index);
                                                        }}
                                                        onSell={() => handleSellConsumable(index)}
                                                        onUse={() => handleUseConsumable(index)}
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

                    {/* CENTER AREA: THE AUTHENTIC BALATRO SHOP CONSOLE */}
                    <div className="shop-center-container" onClick={(e) => e.stopPropagation()}>
                        <div className="shop-console-frame">
                            {/* TOP ROW: ACTIONS & SHOP CARDS */}
                            <div className="shop-console-top-row">
                                {/* LEFT ACTIONS COLUMN */}
                                <div className="shop-actions-column">
                                    <button
                                        className="shop-action-btn next-round-btn"
                                        onClick={onContinue}
                                        title="Advance to Next Round"
                                    >
                                        <span>Next</span>
                                        <span>Round</span>
                                    </button>

                                    <button
                                        className={`shop-action-btn reroll-btn ${money < rerollCost ? 'disabled' : ''}`}
                                        onClick={handleReroll}
                                        disabled={money < rerollCost}
                                        title={`Reroll Shop Cards for $${rerollCost}`}
                                    >
                                        <span>Reroll</span>
                                        <span className="reroll-price-tag">${rerollCost}</span>
                                    </button>
                                </div>

                                {/* SHOP CARDS ROW & SLOTS */}
                                <div className="shop-cards-area">
                                    <div className="shop-cards-slots">
                                        {shopCards.map((item, idx) => {
                                            if (item.isSold) {
                                                return (
                                                    <div key={item.slotId || idx} className="shop-card-slot sold-out-slot">
                                                        <div className="sold-out-stamp">SOLD OUT</div>
                                                    </div>
                                                );
                                            }

                                            const canAfford = money >= item.price;
                                            const isHovered = activeTooltipItem?.slotId === item.slotId;
                                            const isSelected = selectedShopItem?.type === 'card' && selectedShopItem.index === idx;

                                            return (
                                                <div
                                                    key={item.slotId || idx}
                                                    className={`shop-card-slot ${isHovered ? 'active-hover' : ''} ${isSelected ? 'is-selected' : ''} ${canAfford ? 'can-afford' : 'cant-afford'}`}
                                                    onMouseEnter={() => setActiveTooltipItem(item)}
                                                    onMouseLeave={() => {
                                                        if (selectedShopItem?.slotId !== item.slotId) {
                                                            if (!selectedShopItem) setActiveTooltipItem(null);
                                                        }
                                                    }}
                                                    onClick={() => handleCardClick(item, idx)}
                                                    title={isSelected ? "Click to deselect" : `Click to select (Cost: $${item.price})`}
                                                >
                                                    {/* DYNAMIC SIDE TOOLTIP */}
                                                    {(isHovered || isSelected) && (
                                                        <ShopItemTooltip
                                                            item={item}
                                                            side={idx === 0 ? 'left' : 'left'}
                                                        />
                                                    )}

                                                    <div className="shop-price-badge">
                                                        ${item.price}
                                                    </div>

                                                    <div className="shop-card-visual">
                                                        {item.type === 'joker' && (
                                                            <JokerCard
                                                                id={item.id}
                                                                width={82}
                                                                height={114}
                                                                animated={true}
                                                                showHoverTooltip={false}
                                                            />
                                                        )}
                                                        {item.type === 'tarot' && (
                                                            <TarotCard
                                                                tarot={item.id}
                                                                width={82}
                                                                height={114}
                                                                animated={true}
                                                                showHoverTooltip={false}
                                                            />
                                                        )}
                                                        {item.type === 'planet' && (
                                                            <PlanetCard
                                                                planet={item.id}
                                                                width={82}
                                                                height={114}
                                                                animated={true}
                                                                showHoverTooltip={false}
                                                            />
                                                        )}
                                                        {item.type === 'playingCard' && (
                                                            <PlayingCard
                                                                rank={item.rank}
                                                                suit={item.suit}
                                                                width={82}
                                                                height={114}
                                                            />
                                                        )}
                                                    </div>

                                                    {/* ACTION BUTTON (BUY) */}
                                                    {isSelected && (
                                                        <button
                                                            className={`shop-item-action-btn buy-btn ${canAfford ? 'can-buy' : 'cant-buy'}`}
                                                            onClick={(e) => {
                                                                e.stopPropagation();
                                                                if (canAfford) {
                                                                    handleBuyCard(item, idx);
                                                                    setSelectedShopItem(null);
                                                                }
                                                            }}
                                                            disabled={!canAfford}
                                                        >
                                                            BUY
                                                        </button>
                                                    )}
                                                </div>
                                            );
                                        })}
                                    </div>
                                </div>
                            </div>

                            {/* BOTTOM ROW: VOUCHER & BOOSTER PACKS */}
                            <div className="shop-console-bottom-row">
                                {/* VOUCHER CONTAINER */}
                                <div className="shop-voucher-container">
                                    <div className="voucher-ante-label">
                                        ANTE {currentAnte} VOUCHER
                                    </div>

                                    {isVoucherPurchased ? (
                                        <div className="voucher-card-slot sold-out-slot">
                                            <div className="sold-out-stamp">SOLD OUT</div>
                                        </div>
                                    ) : (() => {
                                        const isVoucherSelected = selectedShopItem?.type === 'voucher';
                                        const isVoucherHovered = activeTooltipItem?.slotId === 'voucher';
                                        const canAffordVoucher = money >= voucher.price;

                                        return (
                                            <div
                                                className={`voucher-card-slot ${isVoucherSelected ? 'is-selected' : ''} ${canAffordVoucher ? 'can-afford' : 'cant-afford'}`}
                                                onMouseEnter={() => setActiveTooltipItem({
                                                    slotId: 'voucher',
                                                    title: voucher.title,
                                                    description: voucher.description,
                                                    rarity: 'Voucher'
                                                })}
                                                onMouseLeave={() => {
                                                    if (selectedShopItem?.slotId !== 'voucher') {
                                                        if (!selectedShopItem) setActiveTooltipItem(null);
                                                    }
                                                }}
                                                onClick={handleVoucherClick}
                                                title={isVoucherSelected ? "Click to deselect" : `Click to select (Cost: $${voucher.price})`}
                                            >
                                                {/* DYNAMIC SIDE TOOLTIP */}
                                                {(isVoucherHovered || isVoucherSelected) && (
                                                    <ShopItemTooltip
                                                        item={{
                                                            title: voucher.title,
                                                            description: voucher.description,
                                                            rarity: 'Voucher'
                                                        }}
                                                        side="right"
                                                    />
                                                )}

                                                <div className="shop-price-badge">
                                                    ${voucher.price}
                                                </div>

                                                <div className="voucher-visual">
                                                    <Voucher
                                                        voucher={voucher.id}
                                                        width={82}
                                                        height={114}
                                                        animated={true}
                                                    />
                                                </div>

                                                {/* ACTION BUTTON (REDEEM) */}
                                                {isVoucherSelected && (
                                                    <button
                                                        className={`shop-item-action-btn redeem-btn ${canAffordVoucher ? 'can-buy' : 'cant-buy'}`}
                                                        onClick={(e) => {
                                                            e.stopPropagation();
                                                            if (canAffordVoucher) {
                                                                handleBuyVoucher();
                                                                setSelectedShopItem(null);
                                                            }
                                                        }}
                                                        disabled={!canAffordVoucher}
                                                    >
                                                        REDEEM
                                                    </button>
                                                )}
                                            </div>
                                        );
                                    })()}
                                </div>

                                {/* BOOSTER PACKS CONTAINER */}
                                <div className="shop-boosters-container">
                                    {boosterPacks.map((pack, idx) => {
                                        const isPurchased = purchasedBoosters.includes(pack.slotId);
                                        const canAfford = money >= pack.price;
                                        const isBoosterSelected = selectedShopItem?.type === 'booster' && selectedShopItem.index === idx;
                                        const isBoosterHovered = activeTooltipItem?.slotId === pack.slotId;

                                        if (isPurchased) {
                                            return (
                                                <div key={pack.slotId || idx} className="booster-card-slot sold-out-slot">
                                                    <div className="sold-out-stamp">OPENED</div>
                                                </div>
                                            );
                                        }

                                        return (
                                            <div
                                                key={pack.slotId || idx}
                                                className={`booster-card-slot ${isBoosterSelected ? 'is-selected' : ''} ${canAfford ? 'can-afford' : 'cant-afford'}`}
                                                onMouseEnter={() => setActiveTooltipItem({
                                                    slotId: pack.slotId,
                                                    title: pack.title,
                                                    description: pack.description,
                                                    rarity: 'Booster'
                                                })}
                                                onMouseLeave={() => {
                                                    if (selectedShopItem?.slotId !== pack.slotId) {
                                                        if (!selectedShopItem) setActiveTooltipItem(null);
                                                    }
                                                }}
                                                onClick={() => handleBoosterClick(pack, idx)}
                                                title={isBoosterSelected ? "Click to deselect" : `Click to select (Cost: $${pack.price})`}
                                            >
                                                {/* DYNAMIC SIDE TOOLTIP */}
                                                {(isBoosterHovered || isBoosterSelected) && (
                                                    <ShopItemTooltip
                                                        item={{
                                                            title: pack.title,
                                                            description: pack.description,
                                                            rarity: 'Booster'
                                                        }}
                                                        side={idx === boosterPacks.length - 1 ? 'left' : 'right'}
                                                    />
                                                )}

                                                <div className="shop-price-badge">
                                                    ${pack.price}
                                                </div>

                                                <div className="booster-visual">
                                                    <BoosterPack
                                                        type={pack.type}
                                                        number={pack.number}
                                                        width={82}
                                                        height={114}
                                                        animated={true}
                                                    />
                                                </div>

                                                {/* ACTION BUTTON (OPEN) */}
                                                {isBoosterSelected && (
                                                    <button
                                                        className={`shop-item-action-btn open-btn ${canAfford ? 'can-buy' : 'cant-buy'}`}
                                                        onClick={(e) => {
                                                            e.stopPropagation();
                                                            if (canAfford) {
                                                                handleBuyBooster(pack, idx);
                                                                setSelectedShopItem(null);
                                                            }
                                                        }}
                                                        disabled={!canAfford}
                                                    >
                                                        OPEN
                                                    </button>
                                                )}
                                            </div>
                                        );
                                    })}
                                </div>
                            </div>
                        </div>
                    </div>

                    {/* BOTTOM RIGHT: DECK COUNTER AREA */}
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
                        <div className="peek-deck-label">
                            <span>PEEK</span>
                            <span>DECK</span>
                            <div className="deck-key-hint">LT</div>
                        </div>

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
                        gameData={{ ...gameData, money }}
                    />
                </section>
            )}
        </div>
    );
}

export default Shop;