// Balatro Poker Hand Evaluator & Scoring Engine

export const RANK_ORDER = ['2', '3', '4', '5', '6', '7', '8', '9', '10', 'J', 'Q', 'K', 'A'];

export const RANK_VALUES = {
    '2': 2, '3': 3, '4': 4, '5': 5, '6': 6, '7': 7, '8': 8,
    '9': 9, '10': 10, 'J': 10, 'Q': 10, 'K': 10, 'A': 11
};

export const RANK_NUMERICAL = {
    '2': 2, '3': 3, '4': 4, '5': 5, '6': 6, '7': 7, '8': 8,
    '9': 9, '10': 10, 'J': 11, 'Q': 12, 'K': 13, 'A': 14
};

export const POKER_HAND_BASE_STATS = {
    'Straight Flush': { id: 'straight_flush', name: 'Straight Flush', chips: 100, mult: 8, chipLvl: 40, multLvl: 4 },
    'Four of a Kind': { id: 'four_of_a_kind', name: 'Four of a Kind', chips: 60, mult: 7, chipLvl: 30, multLvl: 3 },
    'Full House': { id: 'full_house', name: 'Full House', chips: 40, mult: 4, chipLvl: 25, multLvl: 2 },
    'Flush': { id: 'flush', name: 'Flush', chips: 35, mult: 4, chipLvl: 15, multLvl: 2 },
    'Straight': { id: 'straight', name: 'Straight', chips: 30, mult: 4, chipLvl: 30, multLvl: 3 },
    'Three of a Kind': { id: 'three_of_a_kind', name: 'Three of a Kind', chips: 30, mult: 3, chipLvl: 20, multLvl: 2 },
    'Two Pair': { id: 'two_pair', name: 'Two Pair', chips: 20, mult: 2, chipLvl: 20, multLvl: 1 },
    'Pair': { id: 'pair', name: 'Pair', chips: 10, mult: 2, chipLvl: 15, multLvl: 1 },
    'High Card': { id: 'high_card', name: 'High Card', chips: 5, mult: 1, chipLvl: 10, multLvl: 1 }
};

/**
 * Evaluates the poker hand formed by 1 to 5 cards.
 * Returns the best poker hand, its base stats, and which cards score.
 */
export function evaluatePokerHand(cards = [], handLevels = {}) {
    if (!cards || cards.length === 0) {
        return null;
    }

    // Sort cards descending by rank value
    const sorted = [...cards].sort((a, b) => {
        return (RANK_NUMERICAL[b.rank] || 0) - (RANK_NUMERICAL[a.rank] || 0);
    });

    const counts = {};
    const rankCards = {};
    sorted.forEach(card => {
        counts[card.rank] = (counts[card.rank] || 0) + 1;
        if (!rankCards[card.rank]) rankCards[card.rank] = [];
        rankCards[card.rank].push(card);
    });

    const suits = {};
    sorted.forEach(card => {
        suits[card.suit] = (suits[card.suit] || 0) + 1;
    });

    const uniqueRanksDesc = Object.keys(counts).sort(
        (a, b) => (RANK_NUMERICAL[b] || 0) - (RANK_NUMERICAL[a] || 0)
    );

    const isFlush = Object.values(suits).some(count => count >= 5);
    const flushSuit = Object.keys(suits).find(suit => suits[suit] >= 5);

    // Check for Straight (5 consecutive cards)
    let isStraight = false;
    let straightCardIds = [];
    if (cards.length >= 5) {
        // Get unique numerical ranks in descending order
        const numRanks = [...new Set(sorted.map(c => RANK_NUMERICAL[c.rank]))].sort((a, b) => b - a);

        // Check standard 5 consecutive
        for (let i = 0; i <= numRanks.length - 5; i++) {
            if (
                numRanks[i] - 1 === numRanks[i + 1] &&
                numRanks[i + 1] - 1 === numRanks[i + 2] &&
                numRanks[i + 2] - 1 === numRanks[i + 3] &&
                numRanks[i + 3] - 1 === numRanks[i + 4]
            ) {
                isStraight = true;
                const targetNums = [numRanks[i], numRanks[i + 1], numRanks[i + 2], numRanks[i + 3], numRanks[i + 4]];
                straightCardIds = targetNums.map(num => sorted.find(c => RANK_NUMERICAL[c.rank] === num)?.id).filter(Boolean);
                break;
            }
        }

        // Check Ace-low straight (A, 5, 4, 3, 2)
        if (!isStraight && numRanks.includes(14) && numRanks.includes(5) && numRanks.includes(4) && numRanks.includes(3) && numRanks.includes(2)) {
            isStraight = true;
            const targetNums = [5, 4, 3, 2, 14];
            straightCardIds = targetNums.map(num => sorted.find(c => RANK_NUMERICAL[c.rank] === num)?.id).filter(Boolean);
        }
    }

    let handName = 'High Card';
    let scoringCardIds = [];

    // 1. Straight Flush
    if (isFlush && isStraight) {
        const flushCards = sorted.filter(c => c.suit === flushSuit);
        const flushNums = [...new Set(flushCards.map(c => RANK_NUMERICAL[c.rank]))].sort((a, b) => b - a);
        let sfFound = false;
        for (let i = 0; i <= flushNums.length - 5; i++) {
            if (
                flushNums[i] - 1 === flushNums[i + 1] &&
                flushNums[i + 1] - 1 === flushNums[i + 2] &&
                flushNums[i + 2] - 1 === flushNums[i + 3] &&
                flushNums[i + 3] - 1 === flushNums[i + 4]
            ) {
                sfFound = true;
                const targetNums = [flushNums[i], flushNums[i + 1], flushNums[i + 2], flushNums[i + 3], flushNums[i + 4]];
                scoringCardIds = targetNums.map(num => flushCards.find(c => RANK_NUMERICAL[c.rank] === num)?.id);
                break;
            }
        }
        if (!sfFound && flushNums.includes(14) && flushNums.includes(5) && flushNums.includes(4) && flushNums.includes(3) && flushNums.includes(2)) {
            sfFound = true;
            scoringCardIds = [5, 4, 3, 2, 14].map(num => flushCards.find(c => RANK_NUMERICAL[c.rank] === num)?.id);
        }
        if (sfFound) {
            handName = 'Straight Flush';
        }
    }

    // 2. Four of a Kind
    if (handName === 'High Card') {
        const fourRank = uniqueRanksDesc.find(r => counts[r] >= 4);
        if (fourRank) {
            handName = 'Four of a Kind';
            scoringCardIds = rankCards[fourRank].slice(0, 4).map(c => c.id);
        }
    }

    // 3. Full House (3 + 2)
    if (handName === 'High Card') {
        const threeRank = uniqueRanksDesc.find(r => counts[r] >= 3);
        if (threeRank) {
            const pairRank = uniqueRanksDesc.find(r => r !== threeRank && counts[r] >= 2);
            if (pairRank) {
                handName = 'Full House';
                scoringCardIds = [
                    ...rankCards[threeRank].slice(0, 3).map(c => c.id),
                    ...rankCards[pairRank].slice(0, 2).map(c => c.id)
                ];
            }
        }
    }

    // 4. Flush
    if (handName === 'High Card' && isFlush) {
        handName = 'Flush';
        scoringCardIds = sorted.filter(c => c.suit === flushSuit).slice(0, 5).map(c => c.id);
    }

    // 5. Straight
    if (handName === 'High Card' && isStraight) {
        handName = 'Straight';
        scoringCardIds = straightCardIds;
    }

    // 6. Three of a Kind
    if (handName === 'High Card') {
        const threeRank = uniqueRanksDesc.find(r => counts[r] >= 3);
        if (threeRank) {
            handName = 'Three of a Kind';
            scoringCardIds = rankCards[threeRank].slice(0, 3).map(c => c.id);
        }
    }

    // 7. Two Pair
    if (handName === 'High Card') {
        const pairs = uniqueRanksDesc.filter(r => counts[r] >= 2);
        if (pairs.length >= 2) {
            handName = 'Two Pair';
            scoringCardIds = [
                ...rankCards[pairs[0]].slice(0, 2).map(c => c.id),
                ...rankCards[pairs[1]].slice(0, 2).map(c => c.id)
            ];
        }
    }

    // 8. Pair
    if (handName === 'High Card') {
        const pairRank = uniqueRanksDesc.find(r => counts[r] >= 2);
        if (pairRank) {
            handName = 'Pair';
            scoringCardIds = rankCards[pairRank].slice(0, 2).map(c => c.id);
        }
    }

    // 9. High Card
    if (handName === 'High Card') {
        scoringCardIds = [sorted[0].id];
    }

    // Add any stone cards to scoringCardIds
    sorted.forEach(card => {
        const enh = String(card.enhancement || '').toLowerCase();
        if (enh.includes('stone') && !scoringCardIds.includes(card.id)) {
            scoringCardIds.push(card.id);
        }
    });

    const baseInfo = POKER_HAND_BASE_STATS[handName] || POKER_HAND_BASE_STATS['High Card'];
    const level = handLevels[handName] || 1;
    const chips = baseInfo.chips + (level - 1) * (baseInfo.chipLvl || 15);
    const mult = baseInfo.mult + (level - 1) * (baseInfo.multLvl || 1);

    return {
        handName,
        level,
        chips,
        mult,
        scoringCardIds: new Set(scoringCardIds),
        sortedCards: sorted
    };
}

/**
 * Returns chip value for a card rank or card object with enhancements
 */
export function getCardChipValue(cardOrRank) {
    if (typeof cardOrRank === 'object' && cardOrRank !== null) {
        if (cardOrRank.isDebuffed) return 0;
        const enh = String(cardOrRank.enhancement || '').toLowerCase();
        if (enh.includes('stone')) return 50;
        let chips = cardOrRank.baseChips || RANK_VALUES[cardOrRank.rank] || 0;
        if (enh.includes('bonus')) chips += 30;
        const ed = String(cardOrRank.edition || '').toLowerCase();
        if (ed.includes('foil')) chips += 50;
        return chips;
    }
    return RANK_VALUES[cardOrRank] || 0;
}

/**
 * Calculates Joker contributions
 */
export function evaluateJokerEffects(jokers = [], playedCards = [], remainingHandCards = []) {
    const effects = [];

    jokers.forEach((joker, index) => {
        if (!joker) return;
        const id = joker.id;

        if (id === 'Joker') {
            effects.push({
                index,
                id,
                title: joker.title || 'Joker',
                type: 'mult',
                amount: 4,
                text: '+4 Mult',
                sound: 'joker'
            });
        } else if (id === 'AbstractJoker') {
            const amount = jokers.filter(Boolean).length * 3;
            effects.push({
                index,
                id,
                title: joker.title || 'Abstract Joker',
                type: 'mult',
                amount,
                text: `+${amount} Mult`,
                sound: 'joker'
            });
        } else if (id === 'ScaryFace') {
            const faceCount = playedCards.filter(c => ['J', 'Q', 'K'].includes(c.rank)).length;
            if (faceCount > 0) {
                effects.push({
                    index,
                    id,
                    title: joker.title || 'Scary Face',
                    type: 'chips',
                    amount: faceCount * 30,
                    text: `+${faceCount * 30} Chips`,
                    sound: 'joker'
                });
            }
        } else if (id === 'RaisedFist') {
            if (remainingHandCards.length > 0) {
                const sortedRemaining = [...remainingHandCards].sort(
                    (a, b) => (RANK_NUMERICAL[a.rank] || 0) - (RANK_NUMERICAL[b.rank] || 0)
                );
                const lowest = sortedRemaining[0];
                const amount = (RANK_NUMERICAL[lowest.rank] || 2) * 2;
                effects.push({
                    index,
                    id,
                    title: joker.title || 'Raised Fist',
                    type: 'mult',
                    amount,
                    text: `+${amount} Mult`,
                    sound: 'joker'
                });
            }
        } else if (id === 'GrosMichel') {
            effects.push({
                index,
                id,
                title: joker.title || 'Gros Michel',
                type: 'mult',
                amount: 15,
                text: '+15 Mult',
                sound: 'joker'
            });
        } else if (id === 'HalfJoker' && playedCards.length <= 3) {
            effects.push({
                index,
                id,
                title: joker.title || 'Half Joker',
                type: 'mult',
                amount: 20,
                text: '+20 Mult',
                sound: 'joker'
            });
        }
    });

    return effects;
}
