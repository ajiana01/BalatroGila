import image from '../../assets/Balatro-Vouchers.png';

export const voucherSprite = {
    image,

    sheetWidth: 639,
    sheetHeight: 380,

    columns: 9,
    rows: 4,

    cellWidth: 639 / 9,
    cellHeight: 380 / 4,

    vouchers: {
        Overstock: {
            column: 0,
            row: 0
        },
        TarotMerchant: {
            column: 1,
            row: 0
        },
        PlanetMerchant: {
            column: 2,
            row: 0
        },
        Hone: {
            column: 3,
            row: 0
        },
        Grabber: {
            column: 4,
            row: 0
        },
        Wasteful: {
            column: 5,
            row: 0
        },
        Blank: {
            column: 6,
            row: 0
        },
    }
};