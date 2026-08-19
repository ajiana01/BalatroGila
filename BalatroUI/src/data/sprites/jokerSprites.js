import image from '../../assets/Balatro-Jokers.png';

export const jokerSprite = {
    image,

    sheetWidth: 710,
    sheetHeight: 1520,

    columns: 10,
    rows: 16,

    cellWidth: 710 / 10,
    cellHeight: 1520 / 16,

    cards: {
        Joker: {
            column: 0,
            row: 0
        },

        GreedyJoker: {
            column: 1,
            row: 0
        },

        LustyJoker: {
            column: 2,
            row: 0
        }
    }
};