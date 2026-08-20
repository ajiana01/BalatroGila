import image from '../../assets/Balatro-CardBacksEnhancersAndSeals.png';

export const cardBackSprite= {
    image,

    sheetWidth: 497,
    sheetHeight: 475,

    columns: 7,
    rows: 5,

    cellWidth: 497 / 7,
    cellHeight: 475 / 5,
    
    types: {
        BackNormal: {
            column: 0,
            row: 0
        },

        Normal: {
            column: 1,
            row: 0
        },
        
        BonusCards: {
            column: 1,
            row: 1
        },

        MultCards: {
            column: 2,
            row: 1
        },

        WildCards: {
            column: 3,
            row: 1
        },

        GlassCards: {
            column: 5,
            row: 1
        },

        SteelCards: {
            column: 6,
            row: 1
        },

        StoneCards: {
            column: 5,
            row: 0
        },

        GoldCards: {
            column: 6,
            row: 0
        },

        LuckyCards: {
            column: 4,
            row: 1
        },
        
        // SEAL
        GoldSeal: {
            column: 2,
            row: 0
        },

        RedSeal: {
            column: 5,
            row: 4
        },

        BlueSeal: {
            column: 6,
            row: 4
        },

        PurpleSeal: {
            column: 4,
            row: 4
        },
    }
};