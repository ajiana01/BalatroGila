import { useState, useEffect, useMemo, useRef } from 'react';
import { Reorder } from 'framer-motion';
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
import BoosterPackOpening from './BoosterPackOpening';
import {
    mapBackendJoker,
    mapBackendConsumable,
    mapBackendCard,
    mapBackendJokers,
    mapBackendConsumables,
    mapBackendBoosterPack,
    mapBackendBoosterPacks
} from '../../../utils/cardMapper';
import {
    buyCard,
    rerollShop,
    buyBooster,
    selectBoosterCard,
    skipBooster,
    buyVoucher,
    leaveShop,
    sellCard,
    useConsumable,
    reorderJokers,
    reorderConsumables
} from '../../../services/api';
import { sfx } from '../../../utils/sfx';
import './Shop.css';

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

// Convert Backend ShopDto to Shop Card items
function mapShopCardsFromDto(shopDto) {
    if (!shopDto) return [];
    const items = [];

    (shopDto.jokerCards || []).forEach((j, idx) => {
        const mapped = mapBackendJoker(j);
        items.push({
            slotId: `shop-joker-${j.id || idx}`,
            type: 'joker',
            id: j.id,
            spriteId: mapped.spriteId,
            title: mapped.title,
            rarity: mapped.rarity,
            price: mapped.price,
            description: mapped.description,
            isSold: false
        });
    });

    (shopDto.tarotCards || []).forEach((t, idx) => {
        const mapped = mapBackendConsumable(t);
        items.push({
            slotId: `shop-tarot-${t.id || idx}`,
            type: 'tarot',
            id: t.id,
            spriteId: mapped.spriteId,
            title: mapped.title,
            rarity: 'Tarot',
            price: mapped.price,
            description: mapped.description,
            isSold: false
        });
    });

    (shopDto.planetCards || []).forEach((p, idx) => {
        const mapped = mapBackendConsumable(p);
        items.push({
            slotId: `shop-planet-${p.id || idx}`,
            type: 'planet',
            id: p.id,
            spriteId: mapped.spriteId,
            title: mapped.title,
            rarity: 'Planet',
            price: mapped.price,
            description: mapped.description,
            isSold: false
        });
    });

    (shopDto.spectralCards || []).forEach((s, idx) => {
        const mapped = mapBackendConsumable(s);
        items.push({
            slotId: `shop-spectral-${s.id || idx}`,
            type: 'spectral',
            id: s.id,
            spriteId: mapped.spriteId,
            title: mapped.title,
            rarity: 'Spectral',
            price: mapped.price,
            description: mapped.description,
            isSold: false
        });
    });

    (shopDto.playingCards || []).forEach((pc, idx) => {
        const mapped = mapBackendCard(pc);
        items.push({
            slotId: `shop-card-${pc.id || idx}`,
            type: 'playingCard',
            id: pc.id,
            rank: mapped.rank,
            suit: mapped.suit,
            title: mapped.title,
            rarity: 'Playing Card',
            price: mapped.price,
            description: `${mapped.rank} of ${mapped.suit}`,
            isSold: false
        });
    });

    return items;
}

function mapBoosterPacksFromDto(shopDto) {
    if (!shopDto?.boosterPacks) return [];
    return mapBackendBoosterPacks(shopDto.boosterPacks);
}

function mapOpenedBoosterCards(packDto) {
    if (!packDto) return [];
    const cards = [];

    (packDto.tarotCards || []).forEach((t, i) => {
        const mapped = mapBackendConsumable(t);
        cards.push({
            cardInstanceId: `opened-tarot-${t.id || i}`,
            id: t.id,
            type: 'tarot',
            spriteId: mapped.spriteId,
            title: mapped.title,
            description: mapped.description
        });
    });

    (packDto.planetCards || []).forEach((p, i) => {
        const mapped = mapBackendConsumable(p);
        cards.push({
            cardInstanceId: `opened-planet-${p.id || i}`,
            id: p.id,
            type: 'planet',
            spriteId: mapped.spriteId,
            title: mapped.title,
            description: mapped.description
        });
    });

    (packDto.spectralCards || []).forEach((s, i) => {
        const mapped = mapBackendConsumable(s);
        cards.push({
            cardInstanceId: `opened-spectral-${s.id || i}`,
            id: s.id,
            type: 'spectral',
            spriteId: mapped.spriteId,
            title: mapped.title,
            description: mapped.description
        });
    });

    (packDto.jokerCards || []).forEach((j, i) => {
        const mapped = mapBackendJoker(j);
        cards.push({
            cardInstanceId: `opened-joker-${j.id || i}`,
            id: j.id,
            type: 'joker',
            spriteId: mapped.spriteId,
            title: mapped.title,
            rarity: mapped.rarity,
            description: mapped.description,
            price: mapped.price
        });
    });

    (packDto.playingCards || []).forEach((pc, i) => {
        const mapped = mapBackendCard(pc);
        cards.push({
            cardInstanceId: `opened-card-${pc.id || i}`,
            id: pc.id,
            type: 'playingCard',
            rank: mapped.rank,
            suit: mapped.suit,
            title: mapped.title,
            chipsBonus: `+${mapped.baseChips} chips`
        });
    });

    return cards;
}

function Shop({
    gameData,
    onContinue,
    onOpenSettings,
    onSyncState
}) {
    const money = gameData?.money ?? 10;
    const currentAnte = gameData?.ante || 1;

    // Deck view modal & hover preview
    const [isDeckModalOpen, setIsDeckModalOpen] = useState(false);
    const [isDeckHovered, setIsDeckHovered] = useState(false);

    // Inventory
    const maxJokers = gameData?.maxJokers || 5;
    const [localJokers, setLocalJokers] = useState(() => gameData?.jokers || []);
    const latestJokersRef = useRef(localJokers);

    const maxConsumables = gameData?.maxConsumables || 2;
    const [localConsumables, setLocalConsumables] = useState(() => gameData?.consumables || []);
    const latestConsumablesRef = useRef(localConsumables);

    useEffect(() => {
        if (gameData?.jokers) {
            setLocalJokers(gameData.jokers);
            latestJokersRef.current = gameData.jokers;
        }
    }, [gameData?.jokers]);

    useEffect(() => {
        if (gameData?.consumables) {
            setLocalConsumables(gameData.consumables);
            latestConsumablesRef.current = gameData.consumables;
        }
    }, [gameData?.consumables]);

    const [activeSlot, setActiveSlot] = useState(null);

    // Shop state from gameData.shop
    const [shopCards, setShopCards] = useState(() => mapShopCardsFromDto(gameData?.shop));
    const [boosterPacks, setBoosterPacks] = useState(() => mapBoosterPacksFromDto(gameData?.shop));
    const [rerollCost, setRerollCost] = useState(() => gameData?.shop?.rerollCost || 5);
    const [activeTooltipItem, setActiveTooltipItem] = useState(null);
    const [selectedShopItem, setSelectedShopItem] = useState(null);

    // Voucher from backend shop
    const voucher = gameData?.shop?.voucher ? {
        id: gameData.shop.voucher.id,
        effect: gameData.shop.voucher.effect || gameData.shop.voucher.name,
        title: gameData.shop.voucher.name || 'Voucher',
        price: gameData.shop.voucher.price || 10,
        description: gameData.shop.voucher.description || 'Ante Voucher'
    } : null;

    const [isVoucherPurchased, setIsVoucherPurchased] = useState(false);

    // Booster packs & opened booster pack modal
    const [purchasedBoosters, setPurchasedBoosters] = useState([]);
    const [activeBoosterPack, setActiveBoosterPack] = useState(null);

    // Sync shop items when gameData.shop changes
    useEffect(() => {
        console.log('[Balatro Shop] Shop loaded / updated with gameData:', {
            money: gameData?.money,
            shop: gameData?.shop,
            jokers: gameData?.jokers,
            consumables: gameData?.consumables,
            currentAnte
        });

        if (gameData?.shop) {
            const mappedCards = mapShopCardsFromDto(gameData.shop);
            const mappedBoosters = mapBoosterPacksFromDto(gameData.shop);

            console.log('[Balatro Shop] Mapped Shop Cards:', mappedCards);
            console.log('[Balatro Shop] Mapped Booster Packs:', mappedBoosters);
            console.log('[Balatro Shop] Available Voucher:', gameData.shop.voucher);

            setShopCards(mappedCards);
            setBoosterPacks(mappedBoosters);
            setRerollCost(gameData.shop.rerollCost || 5);
            if (!gameData.shop.voucher) {
                setIsVoucherPurchased(true);
            } else {
                setIsVoucherPurchased(false);
            }

            if (gameData.shop.openedBoosterPack) {
                const rawPack = gameData.shop.openedBoosterPack;
                const mappedPack = mapBackendBoosterPack(rawPack);
                const mappedCards = mapOpenedBoosterCards(rawPack);

                console.log('[Balatro Shop] Opened Booster Pack state active:', {
                    rawPack,
                    mappedPack,
                    mappedCards
                });

                setActiveBoosterPack({
                    pack: mappedPack,
                    cards: mappedCards,
                    picksRemaining: mappedPack.picks_allowed ?? 1
                });
            } else {
                setActiveBoosterPack(null);
            }
        }
    }, [gameData?.shop]);

    // Toast message for feedback
    const [toastMessage, setToastMessage] = useState('');

    const showToast = (msg) => {
        setToastMessage(msg);
        setTimeout(() => {
            setToastMessage('');
        }, 2500);
    };

    // Toggle Selection Handlers
    const handleCardClick = (item, idx) => {
        console.log('[Balatro Shop] Card clicked:', { item, index: idx });
        if (selectedShopItem?.type === 'card' && selectedShopItem.index === idx) {
            setSelectedShopItem(null);
        } else {
            setSelectedShopItem({ type: 'card', index: idx, slotId: item.slotId });
            setActiveTooltipItem(item);
        }
    };

    const handleVoucherClick = () => {
        console.log('[Balatro Shop] Voucher clicked:', voucher);
        if (isVoucherPurchased || !voucher) return;
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
        console.log('[Balatro Shop] Booster Pack clicked:', { pack, index: idx });
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

    // Top bar interactions (Sell / Use)
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

    const isJokerDraggingRef = useRef(false);
    const isConsumableDraggingRef = useRef(false);

    const handleJokerReorder = (newJokers) => {
        isJokerDraggingRef.current = true;
        setLocalJokers(newJokers);
        latestJokersRef.current = newJokers;
        setActiveSlot(null);
    };

    const handleJokerDragEnd = async () => {
        if (!isJokerDraggingRef.current) return;
        isJokerDraggingRef.current = false;
        const jokerIds = latestJokersRef.current.map(j => j.id);
        if (jokerIds.length <= 1) return;

        try {
            const state = await reorderJokers(jokerIds);
            if (onSyncState) onSyncState(state);
        } catch (err) {
            console.error('Failed to reorder jokers in shop:', err);
        }
    };

    const handleConsumableReorder = (newConsumables) => {
        isConsumableDraggingRef.current = true;
        setLocalConsumables(newConsumables);
        latestConsumablesRef.current = newConsumables;
        setActiveSlot(null);
    };

    const handleConsumableDragEnd = async () => {
        if (!isConsumableDraggingRef.current) return;
        isConsumableDraggingRef.current = false;
        const consumableIds = latestConsumablesRef.current.map(c => c.id);
        if (consumableIds.length <= 1) return;

        try {
            const state = await reorderConsumables(consumableIds);
            if (onSyncState) onSyncState(state);
        } catch (err) {
            console.error('Failed to reorder consumables in shop:', err);
        }
    };

    const handleSellJoker = async (index) => {
        const joker = localJokers[index];
        if (!joker) return;
        console.log('[Balatro Shop] Selling Joker:', joker);

        try {
            const state = await sellCard(joker.id);
            console.log('[Balatro Shop] sellCard response:', state);
            if (onSyncState) onSyncState(state);
            setActiveSlot(null);
            showToast(`Sold ${joker.title || 'Joker'}!`);
        } catch (err) {
            console.error('[Balatro Shop] Error selling Joker:', err);
            showToast(err.message);
        }
    };

    const handleSellConsumable = async (index) => {
        const consumable = localConsumables[index];
        if (!consumable) return;
        console.log('[Balatro Shop] Selling Consumable:', consumable);

        try {
            const state = await sellCard(consumable.id);
            console.log('[Balatro Shop] sellCard consumable response:', state);
            if (onSyncState) onSyncState(state);
            setActiveSlot(null);
            showToast(`Sold ${consumable.title || 'Consumable'}!`);
        } catch (err) {
            console.error('[Balatro Shop] Error selling consumable:', err);
            showToast(err.message);
        }
    };

    const handleUseConsumable = async (index) => {
        const consumable = localConsumables[index];
        if (!consumable) return;
        console.log('[Balatro Shop] Using Consumable:', consumable);

        try {
            const state = await useConsumable(consumable.id, []);
            console.log('[Balatro Shop] useConsumable response:', state);
            if (onSyncState) onSyncState(state);
            setActiveSlot(null);

            if (consumable.type === 'planet') {
                sfx.playLevelUp();
            }
            const msg = state?.lastMessage || (consumable.type === 'planet' ? `Level Up! Upgraded ${consumable.title}!` : `Used ${consumable.title || 'Consumable'}!`);
            showToast(msg);
        } catch (err) {
            console.error('[Balatro Shop] Error using consumable:', err);
            showToast(err.message);
        }
    };

    // Shop Actions (Reroll / Buy)
    const handleReroll = async () => {
        console.log('[Balatro Shop] Reroll clicked, cost:', rerollCost, 'current money:', money);
        if (money < rerollCost) {
            showToast("Not enough money to reroll!");
            return;
        }

        try {
            const state = await rerollShop();
            console.log('[Balatro Shop] rerollShop response:', state);
            if (onSyncState) onSyncState(state);
            setActiveTooltipItem(null);
            setSelectedShopItem(null);
            showToast(`Shop rerolled!`);
        } catch (err) {
            console.error('[Balatro Shop] Error rerolling shop:', err);
            showToast(err.message);
        }
    };

    const handleBuyCard = async (item, index) => {
        if (item.isSold) return;
        console.log('[Balatro Shop] Buying card:', { item, index, money });

        if (money < item.price) {
            showToast("Not enough money!");
            return;
        }

        try {
            const state = await buyCard(item.id);
            console.log('[Balatro Shop] buyCard response:', state);
            if (onSyncState) onSyncState(state);

            setShopCards(prev => prev.map((c, i) => i === index ? { ...c, isSold: true } : c));
            setActiveTooltipItem(null);
            setSelectedShopItem(null);
            showToast(`Bought ${item.title} for $${item.price}!`);
        } catch (err) {
            console.error('[Balatro Shop] Error buying card:', err);
            showToast(err.message);
        }
    };

    const handleBuyVoucher = async () => {
        if (isVoucherPurchased || !voucher) return;
        console.log('[Balatro Shop] Buying voucher:', voucher);

        if (money < voucher.price) {
            showToast("Not enough money for Voucher!");
            return;
        }

        try {
            const state = await buyVoucher(voucher.id);
            console.log('[Balatro Shop] buyVoucher response:', state);
            if (onSyncState) onSyncState(state);

            setIsVoucherPurchased(true);
            setActiveTooltipItem(null);
            setSelectedShopItem(null);
            showToast(`Redeemed Voucher: ${voucher.title}!`);
        } catch (err) {
            console.error('[Balatro Shop] Error buying voucher:', err);
            showToast(err.message);
        }
    };

    const handleBuyBooster = async (pack, index) => {
        console.log('[Balatro Shop] Buying booster pack:', { pack, index, money });
        if (purchasedBoosters.includes(pack.slotId)) return;
        if (money < pack.price) {
            showToast("Not enough money for Booster Pack!");
            return;
        }

        try {
            const state = await buyBooster(pack.id);
            console.log('[Balatro Shop] buyBooster response state:', state);
            if (onSyncState) onSyncState(state);

            setPurchasedBoosters(prev => [...prev, pack.slotId]);
            setActiveTooltipItem(null);
            setSelectedShopItem(null);

            if (state.shop?.openedBoosterPack) {
                const opened = state.shop.openedBoosterPack;
                const mappedPack = mapBackendBoosterPack(opened);
                const mappedCards = mapOpenedBoosterCards(opened);
                console.log('[Balatro Shop] Opened booster pack successfully mapped:', {
                    mappedPack,
                    mappedCards
                });
                setActiveBoosterPack({
                    pack: mappedPack,
                    cards: mappedCards,
                    picksRemaining: mappedPack.picks_allowed || 1
                });
            }

            showToast(`Opened ${pack.title}!`);
        } catch (err) {
            console.error('[Balatro Shop] Error buying booster pack:', err);
            showToast(err.message);
        }
    };

    const handlePickBoosterCard = async (card, cardIndex) => {
        if (!activeBoosterPack) return;
        console.log('[Balatro Shop] Picking booster card:', { card, cardIndex });

        try {
            const state = await selectBoosterCard(card.id);
            console.log('[Balatro Shop] selectBoosterCard response:', state);
            if (onSyncState) onSyncState(state);

            showToast(`Selected ${card.title}!`);

            if (state.shop?.openedBoosterPack) {
                const opened = state.shop.openedBoosterPack;
                const mappedPack = mapBackendBoosterPack(opened);
                const mappedCards = mapOpenedBoosterCards(opened);
                setActiveBoosterPack({
                    pack: mappedPack,
                    cards: mappedCards,
                    picksRemaining: mappedPack.picks_allowed ?? Math.max(0, (activeBoosterPack.picksRemaining || 1) - 1)
                });
            } else {
                setTimeout(() => {
                    setActiveBoosterPack(null);
                }, 300);
            }
        } catch (err) {
            console.error('[Balatro Shop] Error selecting booster card:', err);
            showToast(err.message);
        }
    };

    const handleSkipBooster = async () => {
        console.log('[Balatro Shop] Skipping booster pack');
        try {
            const state = await skipBooster();
            if (onSyncState) onSyncState(state);
        } catch (err) {
            console.error('[Balatro Shop] Error skipping booster pack:', err);
        }
        showToast("Skipped Booster Pack");
        setActiveBoosterPack(null);
    };

    const handleAdvanceRound = async () => {
        console.log('[Balatro Shop] Next Round clicked');
        try {
            const state = await leaveShop();
            console.log('[Balatro Shop] leaveShop response:', state);
            if (onSyncState) onSyncState(state);
            if (onContinue) onContinue();
        } catch (err) {
            console.error('[Balatro Shop] Failed to leave shop:', err);
            if (onContinue) onContinue();
        }
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
                    jokers={localJokers}
                    consumables={localConsumables}
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
                            <Reorder.Group
                                axis="x"
                                values={localJokers}
                                onReorder={handleJokerReorder}
                                className="jokers-slots-box"
                                as="div"
                            >
                                {localJokers.map((joker, index) => {
                                    const isSelected = activeSlot?.type === 'joker' && activeSlot.index === index;
                                    return (
                                        <Reorder.Item
                                            key={joker.id || index}
                                            value={joker}
                                            as="div"
                                            className="joker-slot occupied"
                                            onDragEnd={handleJokerDragEnd}
                                            whileDrag={{
                                                scale: 1.12,
                                                zIndex: 100,
                                                cursor: 'grabbing',
                                                filter: 'drop-shadow(0 10px 20px rgba(0,0,0,0.6))'
                                            }}
                                        >
                                            <JokerCard
                                                id={joker.id}
                                                spriteId={joker.spriteId}
                                                title={joker.title}
                                                description={joker.description}
                                                rarity={joker.rarity}
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
                                        </Reorder.Item>
                                    );
                                })}

                                {Array.from({ length: Math.max(0, maxJokers - localJokers.length) }).map((_, i) => (
                                    <div
                                        key={`empty-joker-${i}`}
                                        className="joker-slot empty"
                                    />
                                ))}
                            </Reorder.Group>

                            <div className="slot-counter-text">
                                {localJokers.length}/{maxJokers}
                            </div>
                        </div>

                        {/* CONSUMABLES CONTAINER */}
                        <div className="consumables-container-wrapper">
                            <Reorder.Group
                                axis="x"
                                values={localConsumables}
                                onReorder={handleConsumableReorder}
                                className="consumables-slots-box"
                                as="div"
                            >
                                {localConsumables.map((consumable, index) => {
                                    const isSelected = activeSlot?.type === 'consumable' && activeSlot.index === index;
                                    return (
                                        <Reorder.Item
                                            key={consumable.id || index}
                                            value={consumable}
                                            as="div"
                                            className="consumable-slot occupied"
                                            onDragEnd={handleConsumableDragEnd}
                                            whileDrag={{
                                                scale: 1.12,
                                                zIndex: 100,
                                                cursor: 'grabbing',
                                                filter: 'drop-shadow(0 10px 20px rgba(0,0,0,0.6))'
                                            }}
                                        >
                                            {consumable.type === 'planet' ? (
                                                <PlanetCard
                                                    planet={consumable.id}
                                                    spriteId={consumable.spriteId}
                                                    title={consumable.title}
                                                    description={consumable.description}
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
                                                    spriteId={consumable.spriteId}
                                                    title={consumable.title}
                                                    description={consumable.description}
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
                                            )}
                                        </Reorder.Item>
                                    );
                                })}

                                {Array.from({ length: Math.max(0, maxConsumables - localConsumables.length) }).map((_, i) => (
                                    <div
                                        key={`empty-consumable-${i}`}
                                        className="consumable-slot empty"
                                    />
                                ))}
                            </Reorder.Group>

                            <div className="slot-counter-text align-right">
                                {localConsumables.length}/{maxConsumables}
                            </div>
                        </div>
                    </div>

                    {/* CENTER AREA: BALATRO SHOP CONSOLE */}
                    <div className="shop-center-container" onClick={(e) => e.stopPropagation()}>
                        <div className="shop-console-frame">
                            {/* TOP ROW: ACTIONS & SHOP CARDS */}
                            <div className="shop-console-top-row">
                                {/* LEFT ACTIONS COLUMN */}
                                <div className="shop-actions-column">
                                    <button
                                        className="shop-action-btn next-round-btn"
                                        onClick={handleAdvanceRound}
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
                                                                spriteId={item.spriteId}
                                                                title={item.title}
                                                                description={item.description}
                                                                rarity={item.rarity}
                                                                width={82}
                                                                height={114}
                                                                animated={true}
                                                                showHoverTooltip={false}
                                                            />
                                                        )}
                                                        {item.type === 'tarot' && (
                                                            <TarotCard
                                                                tarot={item.id}
                                                                spriteId={item.spriteId}
                                                                title={item.title}
                                                                description={item.description}
                                                                width={82}
                                                                height={114}
                                                                animated={true}
                                                                showHoverTooltip={false}
                                                            />
                                                        )}
                                                        {item.type === 'planet' && (
                                                            <PlanetCard
                                                                planet={item.id}
                                                                spriteId={item.spriteId}
                                                                title={item.title}
                                                                description={item.description}
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

                                    {!voucher || isVoucherPurchased ? (
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
                                                        voucher={voucher.effect || voucher.id}
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