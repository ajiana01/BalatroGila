import image from '../../assets/Balatro-BlindChips.png';

export const blindSprite = {
    image,

    sheetWidth: 714,
    sheetHeight: 1054,

    columns: 21,
    rows: 31,

    cellWidth: 714 / 21,
    cellHeight: 1054 / 31,

    blinds: {
        SmallBlind: {
            column: 0,
            row: 0
        },

        BigBlind: {
            column: 0,
            row: 1
        },

        BossBlind: {
            column: 0,
            row: 30
        },
    }
};