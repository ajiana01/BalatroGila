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
        
        Chip: {
            column: 1,
            row: 1
        },

        Mult: {
            column: 2,
            row: 1
        },
    }
};