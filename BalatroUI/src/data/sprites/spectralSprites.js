import image from '../../assets/Balatro-TarotPlanetAndSpectralCards.png';

export const spectralSprite = {
    image,

    sheetWidth: 710,
    sheetHeight: 570,

    columns: 10,
    rows: 6,

    cellWidth: 710 / 10,
    cellHeight: 570 / 6,

    spectrals: {
        Familiar: {
            column: 0,
            row: 4
        },
        Grim: {
            column: 1,
            row: 4
        },
        Incantantion: {
            column: 2,
            row: 4
        },
        Talisman: {
            column: 3,
            row: 4
        },
    }
};