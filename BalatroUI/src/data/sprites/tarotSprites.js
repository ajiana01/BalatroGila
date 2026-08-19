import image from '../../assets/Balatro-TarotPlanetAndSpectralCards.png';

export const tarotSprite = {
    image,

    sheetWidth: 710,
    sheetHeight: 570,

    columns: 10,
    rows: 6,

    cellWidth: 710 / 10,
    cellHeight: 570 / 6,

    tarots: {
        TheFool: {
            column: 0,
            row: 0
        },
        TheMagician: {
            column: 1,
            row: 0
        },
        TheHighPriestess: {
            column: 2,
            row: 0
        },
        TheEmpress: {
            column: 3,
            row: 0
        },

    }
};