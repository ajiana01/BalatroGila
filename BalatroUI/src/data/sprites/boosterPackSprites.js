import image from '../../assets/Balatro-BoosterPacks.png';

export const boosterPackSprite = {
    image,

    sheetWidth: 284,
    sheetHeight: 855,

    columns: 4,
    rows: 9,

    cellWidth: 284 / 4,
    cellHeight: 855 / 9,
    
    Arcana_Normal: {
        1: { column: 0, row: 0 },
        2: { column: 1, row: 0 },
        3: { column: 2, row: 0 },
        4: { column: 3, row: 0 },
    },

    Arcana_Jumbo: {
        1: { column: 0, row: 2 },
        2: { column: 1, row: 2 },
    },

    Arcana_Mega: {
        1: { column: 2, row: 2 },
        2: { column: 3, row: 2 },
    },

    Celestial_Normal: {
        1: { column: 0, row: 1 },
        2: { column: 1, row: 1 },
        3: { column: 2, row: 1 },
        4: { column: 3, row: 1 },
    }
};