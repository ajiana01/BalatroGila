// BalatroUI/src/utils/cardMapper.js
import { jokerSprite } from '../data/sprites/jokerSprites';
import { tarotSprite } from '../data/sprites/tarotSprites';
import { spectralSprite } from '../data/sprites/spectralSprites';
import { planetSprite } from '../data/sprites/planetSprites';

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
  if (jokerSprite.cards[nameOrKey]) return nameOrKey;

  const str = String(nameOrKey);
  const cleaned = str.replace(/[^a-zA-Z0-9]/g, '').toLowerCase();

  for (const key of Object.keys(jokerSprite.cards)) {
    if (key.replace(/[^a-zA-Z0-9]/g, '').toLowerCase() === cleaned) {
      return key;
    }
  }

  const SPECIAL_MAP = {
    '8ball': 'EightBall',
    'eightball': 'EightBall',
    'chaostheclown': 'ChaosTheClown',
    'ridethebus': 'RideTheBus',
    'tothemoon': 'ToTheMoon',
    'dna': 'DNA',
    'theduo': 'TheDuo',
    'thetrio': 'TheTrio',
    'thefamily': 'TheFamily',
    'theorder': 'TheOrder',
    'thetribe': 'TheTribe'
  };

  if (SPECIAL_MAP[cleaned] && jokerSprite.cards[SPECIAL_MAP[cleaned]]) {
    return SPECIAL_MAP[cleaned];
  }

  return 'Joker';
}

// Normalisasi sprite key untuk Tarot
export function normalizeTarotSpriteKey(nameOrKey) {
  if (!nameOrKey) return 'TheFool';
  if (tarotSprite.tarots[nameOrKey]) return nameOrKey;

  const str = String(nameOrKey);
  const cleaned = str.replace(/[^a-zA-Z0-9]/g, '').toLowerCase();

  for (const key of Object.keys(tarotSprite.tarots)) {
    if (key.replace(/[^a-zA-Z0-9]/g, '').toLowerCase() === cleaned) {
      return key;
    }
  }

  const SPECIAL_MAP = {
    'thefool': 'TheFool',
    'fool': 'TheFool',
    'themagician': 'TheMagician',
    'magician': 'TheMagician',
    'thehighpriestess': 'TheHighPriestess',
    'highpriestess': 'TheHighPriestess',
    'theempress': 'TheEmpress',
    'empress': 'TheEmpress',
    'theemperor': 'TheEmperor',
    'emperor': 'TheEmperor',
    'thehierophant': 'TheHierophant',
    'hierophant': 'TheHierophant',
    'thelovers': 'TheLovers',
    'lovers': 'TheLovers',
    'thechariot': 'TheChariot',
    'chariot': 'TheChariot',
    'justice': 'TheJustice',
    'thejustice': 'TheJustice',
    'thehermit': 'TheHermit',
    'hermit': 'TheHermit',
    'thewheeloffortune': 'TheWheelOfFortune',
    'thewheelfortune': 'TheWheelOfFortune',
    'wheelfortune': 'TheWheelOfFortune',
    'wheeloffortune': 'TheWheelOfFortune',
    'thestrength': 'TheStrength',
    'strength': 'TheStrength',
    'thehangedman': 'TheHangedMan',
    'hangedman': 'TheHangedMan',
    'death': 'TheDeath',
    'thedeath': 'TheDeath',
    'temperance': 'TheTemperance',
    'thetemperance': 'TheTemperance',
    'thedevil': 'TheDevil',
    'devil': 'TheDevil',
    'thetower': 'TheTower',
    'tower': 'TheTower',
    'thestar': 'TheStar',
    'star': 'TheStar',
    'themoon': 'TheMoon',
    'moon': 'TheMoon',
    'thesun': 'TheSun',
    'sun': 'TheSun',
    'judgement': 'TheJudgement',
    'thejudgement': 'TheJudgement',
    'theworld': 'TheWorld',
    'world': 'TheWorld'
  };

  if (SPECIAL_MAP[cleaned] && tarotSprite.tarots[SPECIAL_MAP[cleaned]]) {
    return SPECIAL_MAP[cleaned];
  }

  return 'TheFool';
}

// Normalisasi sprite key untuk Spectral
export function normalizeSpectralSpriteKey(nameOrKey) {
  if (!nameOrKey) return 'Familiar';
  if (spectralSprite.spectrals[nameOrKey]) return nameOrKey;

  const str = String(nameOrKey);
  const cleaned = str.replace(/[^a-zA-Z0-9]/g, '').toLowerCase();

  for (const key of Object.keys(spectralSprite.spectrals)) {
    if (key.replace(/[^a-zA-Z0-9]/g, '').toLowerCase() === cleaned) {
      return key;
    }
  }

  const SPECIAL_MAP = {
    'incantation': 'Incantation',
    'incantantion': 'Incantation',
    'immolate': 'Immolate',
    'immobile': 'Immolate',
    'dejavu': 'DejaVu',
    'deja_vu': 'DejaVu',
    'thesoul': 'TheSoul',
    'soul': 'TheSoul',
    'blackhole': 'BlackHole'
  };

  if (SPECIAL_MAP[cleaned] && spectralSprite.spectrals[SPECIAL_MAP[cleaned]]) {
    return SPECIAL_MAP[cleaned];
  }

  return 'Familiar';
}

// Normalisasi sprite key untuk Planet
export function normalizePlanetSpriteKey(nameOrKey) {
  if (!nameOrKey) return 'Mercury';
  if (planetSprite.planets[nameOrKey]) return nameOrKey;

  const str = String(nameOrKey);
  const cleaned = str.replace(/[^a-zA-Z0-9]/g, '').toLowerCase();

  for (const key of Object.keys(planetSprite.planets)) {
    if (key.replace(/[^a-zA-Z0-9]/g, '').toLowerCase() === cleaned) {
      return key;
    }
  }

  return 'Mercury';
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
  const spriteKey = normalizeJokerSpriteKey(backendJoker.jokerId || backendJoker.jokerKey || backendJoker.name);

  return {
    id: backendJoker.id,
    jokerId: backendJoker.jokerId || spriteKey,
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

  // Determine type and resolved spriteId
  let type = 'tarot';
  let spriteId = '';

  if (c.planetType || c.pokerHandType || /Mercury|Venus|Earth|Mars|Jupiter|Saturn|Uranus|Neptune|Pluto/i.test(name)) {
    type = 'planet';
    spriteId = normalizePlanetSpriteKey(c.planetType || name);
  } else if (c.spectralType || /Ankh|Hex|Wraith|Sigil|Ouija|Ectoplasm|Immolate|Talisman|Aura|Familiar|Grim|Incantation|Deja_vu|Trance|Medium|Cryptid|TheSoul|BlackHole/i.test(name)) {
    type = 'spectral';
    spriteId = normalizeSpectralSpriteKey(c.spectralType || name);
  } else {
    type = 'tarot';
    spriteId = normalizeTarotSpriteKey(c.type || name);
  }

  return {
    id,
    type,
    spriteId,
    title: name || spriteId,
    name: name || spriteId,
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
