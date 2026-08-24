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
    },

    Celestial_Jumbo: {
        1: { column: 0, row: 3 },
        2: { column: 1, row: 3 },
    },

    Celestial_Mega: {
        1: { column: 2, row: 3 },
        2: { column: 3, row: 3 },
    },

    Standard_Normal: {
        1: { column: 0, row: 6 },
        2: { column: 1, row: 6 },
        3: { column: 2, row: 6 },
        4: { column: 3, row: 6 },
    },
    
    Standard_Jumbo: {
        1: { column: 0, row: 7 },
        2: { column: 1, row: 7 },
    },

    Standard_Mega: {
        1: { column: 2, row: 7 },
        2: { column: 3, row: 7 },
    },

    Buffon_Normal: {
        1: { column: 0, row: 8 },
        2: { column: 1, row: 8 },
    },

    Buffon_Jumbo: {
        1: { column: 2, row: 8 },
    },

    Buffon_Mega: {
        1: { column: 3, row: 8 },
    },

    Spectral_Normal: {
        1: { column: 0, row: 4 },
        2: { column: 1, row: 4 },
    },

    Spectral_Jumbo: {
        1: { column: 2, row: 4 },
    },

    Spectral_Mega: {
        1: { column: 3, row: 4 },
    },

    Blank: {
        1: { column: 0, row: 5 },
    }
};