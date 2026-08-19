import image from '../../assets/Balatro-TarotPlanetAndSpectralCards.png';

export const planetSprite = {
    image,

    sheetWidth: 710,
    sheetHeight: 570,

    columns: 10,
    rows: 6,

    cellWidth: 710 / 10,
    cellHeight: 570 / 6,
    
    planets: {
        Pluto: {
            column: 8,
            row: 3
        },
        Mercury: {
            column: 0,
            row: 3
        },
        Venus: {
            column: 1,
            row: 3
        },
        Earth: {
            column: 2,
            row: 3
        },
        Mars: {
            column: 3,
            row: 3
        },
        Jupiter: {
            column: 4,
            row: 3
        },
        Saturn: {
            column: 5,
            row: 3
        },
        Uranus: {
            column: 6,
            row: 3
        },
        Neptune: {
            column: 7,
            row: 3
        },
    }
};