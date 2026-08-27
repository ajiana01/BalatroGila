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
        Familiar: { column: 0, row: 4 },
        Grim: { column: 1, row: 4 },
        Incantation: { column: 2, row: 4 },
        Incantantion: { column: 2, row: 4 },
        Talisman: { column: 3, row: 4 },
        Aura: { column: 4, row: 4 },
        Wraith: { column: 5, row: 4 },
        Sigil: { column: 6, row: 4 },
        Ouija: { column: 7, row: 4 },
        Ectoplasm: { column: 8, row: 4 },
        Immolate: { column: 9, row: 4 },
        Immobile: { column: 9, row: 4 },
        Ankh: { column: 0, row: 5 },
        DejaVu: { column: 1, row: 5 },
        Dejavu: { column: 1, row: 5 },
        Deja_vu: { column: 1, row: 5 },
        Hex: { column: 2, row: 5 },
        Trance: { column: 3, row: 5 },
        Medium: { column: 4, row: 5 },
        Cryptid: { column: 5, row: 5 },
        TheSoul: { column: 6, row: 5 },
        Soul: { column: 6, row: 5 },
        BlackHole: { column: 7, row: 5 }
    }
};