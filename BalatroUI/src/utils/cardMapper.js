// BalatroUI/src/utils/cardMapper.js
import { jokerSprite } from '../data/sprites/jokerSprites';

export const RANK_MAP = {
  'Two': '2', '2': '2',
  'Three': '3', '3': '3',
  'Four': '4', '4': '4',
  'Five': '5', '5': '5',
  'Six': '6', '6': '6',
  'Seven': '7', '7': '7',
  'Eight': '8', '8': '8',
  'Nine': '9', '9': '9',
  'Ten': '10', '10': '10',
  'Jack': 'J', '11': 'J', 'J': 'J',
  'Queen': 'Q', '12': 'Q', 'Q': 'Q',
  'King': 'K', '13': 'K', 'K': 'K',
  'Ace': 'A', '14': 'A', 'A': 'A'
};

export const REVERSE_RANK_MAP = {
  '2': 'Two', '3': 'Three', '4': 'Four', '5': 'Five', '6': 'Six',
  '7': 'Seven', '8': 'Eight', '9': 'Nine', '10': 'Ten',
  'J': 'Jack', 'Q': 'Queen', 'K': 'King', 'A': 'Ace'
};

// Normalisasi sprite key untuk Joker
export function normalizeJokerSpriteKey(nameOrKey) {
  if (!nameOrKey) return 'Joker';
  // Check exact match in jokerSprite
  if (jokerSprite.cards[nameOrKey]) return nameOrKey;

  // Remove spaces and non-alphanumeric
  const cleaned = nameOrKey.replace(/[^a-zA-Z0-9]/g, '');
  for (const key of Object.keys(jokerSprite.cards)) {
    if (key.toLowerCase() === cleaned.toLowerCase()) {
      return key;
    }
  }
  return 'Joker';
}

// Map Backend PlayingCard -> Frontend PlayingCard
export function mapBackendCard(backendCard) {
  if (!backendCard) return null;
  const rankStr = String(backendCard.rank);
  const normalizedRank = RANK_MAP[rankStr] || rankStr;

  return {
    id: backendCard.id || `card-${Date.now()}-${Math.random()}`,
    suit: backendCard.suit,
    rank: normalizedRank,
    enhancement: backendCard.enhancement || 'None',
    edition: backendCard.edition || 'Base',
    baseChips: backendCard.baseChips || 0,
    baseMult: backendCard.baseMult || 0,
    baseXMult: backendCard.baseXMult || 1,
    price: backendCard.price || 1,
    isDebuffed: Boolean(backendCard.isDebuffed),
    title: `${normalizedRank} of ${backendCard.suit}`
  };
}

export function mapBackendCards(cards) {
  return (cards || []).map(mapBackendCard).filter(Boolean);
}

// Map Backend JokerCard -> Frontend Joker item
export function mapBackendJoker(backendJoker) {
  if (!backendJoker) return null;
  const spriteKey = normalizeJokerSpriteKey(backendJoker.jokerKey || backendJoker.name);

  return {
    id: backendJoker.id,
    spriteId: spriteKey,
    title: backendJoker.name || spriteKey,
    name: backendJoker.name || spriteKey,
    edition: backendJoker.edition || 'Base',
    rarity: backendJoker.rarity || 'Common',
    jokerModifierType: backendJoker.jokerModifierType,
    chipsValue: backendJoker.chipsValue || 0,
    multValue: backendJoker.multValue || 0,
    xMultValue: backendJoker.xMultValue || 1,
    moneyValue: backendJoker.moneyValue || 0,
    price: backendJoker.price || 4,
    sellPrice: backendJoker.sellValue || Math.max(1, Math.floor((backendJoker.price || 4) / 2)),
    description: backendJoker.description || '',
    jokerKey: backendJoker.jokerKey || spriteKey
  };
}

export function mapBackendJokers(jokers) {
  return (jokers || []).map(mapBackendJoker).filter(Boolean);
}

// Map Backend Consumable (Tarot, Planet, Spectral) -> Frontend consumable
export function mapBackendConsumable(c) {
  if (!c) return null;
  const id = c.id || `c-${Date.now()}`;
  const name = c.name || '';
  const price = c.price || 3;
  const sellPrice = c.sellValue || Math.max(1, Math.floor(price / 2));
  const desc = c.description || '';

  // Determine type
  let type = 'tarot';
  let spriteId = name.replace(/\s+/g, '');

  if (c.planetType || c.pokerHandType || /Mercury|Venus|Earth|Mars|Jupiter|Saturn|Uranus|Neptune|Pluto/i.test(name)) {
    type = 'planet';
    spriteId = c.planetType || name.replace(/\s+/g, '');
  } else if (c.spectralType || /Ankh|Hex|Wraith|Sigil|Ouija|Ectoplasm|Immolate|Talisman|Aura|Familiar|Grim|Incantation|Deja_vu|Trance|Medium|Cryptid|TheSoul|BlackHole/i.test(name)) {
    type = 'spectral';
    spriteId = c.spectralType || name.replace(/\s+/g, '');
  } else {
    type = 'tarot';
    spriteId = c.type || name.replace(/\s+/g, '');
  }

  return {
    id,
    type,
    spriteId,
    title: name || spriteId,
    price,
    sellPrice,
    description: desc
  };
}

export function mapBackendConsumables(consumables) {
  return (consumables || []).map(mapBackendConsumable).filter(Boolean);
}

// Map Blind from Backend
export function mapBackendBlind(backendBlind) {
  if (!backendBlind) return null;

  // If already properly mapped
  if (backendBlind.type && backendBlind.blind && backendBlind.title) {
    return backendBlind;
  }

  const rawType = String(backendBlind.blindType || backendBlind.type || 'Small');
  const type = rawType.toLowerCase().includes('boss') ? 'boss' :
               rawType.toLowerCase().includes('big') ? 'big' : 'small';

  // Blind Sprite Key
  let blindSpriteKey = 'SmallBlind';
  if (type === 'big') {
    blindSpriteKey = 'BigBlind';
  } else if (type === 'boss') {
    const rawName = backendBlind.blindId || backendBlind.name || backendBlind.title || 'TheGoad';
    const cleaned = String(rawName).replace(/[^a-zA-Z0-9]/g, '');
    blindSpriteKey = cleaned || 'TheGoad';
  }

  const score = backendBlind.scoreToDefeat || backendBlind.score || 300;
  const rewardMoney = backendBlind.rewardMoney || (type === 'boss' ? 5 : type === 'big' ? 4 : 3);
  const rewardStr = backendBlind.reward || ('$'.repeat(rewardMoney) + '+');

  return {
    id: backendBlind.id,
    blindId: backendBlind.blindId || '',
    type,
    blind: blindSpriteKey,
    title: backendBlind.name || backendBlind.title || (type === 'small' ? 'Small Blind' : type === 'big' ? 'Big Blind' : 'Boss Blind'),
    score,
    rewardMoney,
    reward: rewardStr,
    description: backendBlind.description || '',
    isDefeated: Boolean(backendBlind.isDefeated),
    bossKey: backendBlind.blindId || backendBlind.bossKey || ''
  };
}

export function mapBackendBlinds(blinds) {
  return (blinds || []).map(mapBackendBlind).filter(Boolean);
}

// Map Booster Pack from Backend
export function mapBackendBoosterPack(pack, idx = 0) {
  if (!pack) return null;

  const rawType = String(pack.boosterPackType || pack.type || 'Arcana');
  let packKind = 'Arcana';
  if (/Celestial/i.test(rawType)) packKind = 'Celestial';
  else if (/Standard/i.test(rawType)) packKind = 'Standard';
  else if (/Buff/i.test(rawType)) packKind = 'Buffoon';
  else if (/Spectral/i.test(rawType)) packKind = 'Spectral';

  const rawSize = String(pack.packSize || pack.size || 'Normal');
  let size = 'Normal';
  if (/Jumbo/i.test(rawSize)) size = 'Jumbo';
  else if (/Mega/i.test(rawSize)) size = 'Mega';

  const spriteType = `${packKind}_${size}`;
  const number = 1;

  const maxPick = pack.maxPick !== undefined ? pack.maxPick : (size === 'Mega' ? 2 : 1);
  const totalCards = pack.totalCard || (size === 'Normal' ? 3 : 5);
  const price = pack.price || (size === 'Mega' ? 8 : size === 'Jumbo' ? 6 : 4);
  const title = pack.name || `${size !== 'Normal' ? size + ' ' : ''}${packKind} Pack`;
  const description = `Contains ${totalCards} cards. Choose ${maxPick}.`;

  return {
    slotId: `booster-${pack.id || idx}`,
    id: pack.id,
    title,
    description,
    price,
    packKind,
    type: spriteType,
    number,
    picks_allowed: maxPick,
    totalCards,
    rawPack: pack
  };
}

export function mapBackendBoosterPacks(packs) {
  return (packs || []).map(mapBackendBoosterPack).filter(Boolean);
}
