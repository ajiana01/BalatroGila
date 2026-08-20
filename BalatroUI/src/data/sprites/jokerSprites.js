import image from '../../assets/Balatro-Jokers.png';

export const jokerSprite = {
    image,

    sheetWidth: 710,
    sheetHeight: 1520,

    columns: 10,
    rows: 16,

    cellWidth: 710 / 10,
    cellHeight: 1520 / 16,

    // Base (standard card)
    // Foil (+50 Chips)
    // Holographic (+10 Mult)
    // Polychrome (X1.5 Mult)
    // Negative (+1 Joker slot)

    cards = {
        Joker: {
            column: 0,
            row: 0
        },
        GreedyJoker: {
            column: 6,
            row: 1
        },
        LustyJoker: {
            column: 7,
            row: 1
        },
        WrathfulJoker: {
            column: 8,
            row: 1
        },
        GluttonousJoker: {
            column: 9,
            row: 1
        },
        JollyJoker: {
            column: 2,
            row: 0
        },
        ZanyJoker: {
            column: 3,
            row: 0
        },
        MadJoker: {
            column: 4,
            row: 0
        },
        CrazyJoker: {
            column: 5,
            row: 0
        },
        DrollJoker: {
            column: 6,
            row: 0
        },
        SlyJoker: {
            column: 0,
            row: 14
        },
        WilyJoker: {
            column: 1,
            row: 14
        },
        CleverJoker: {
            column: 2,
            row: 14
        },
        DeviousJoker: {
            column: 3,
            row: 14
        },
        CraftyJoker: {
            column: 4,
            row: 14
        },
        HalfJoker: {
            column: 7,
            row: 0
        },
        JokerStencil: {
            column: 2,
            row: 5
        },
        FourFingers: {
            column: 6,
            row: 6
        },
        Mime: {
            column: 4,
            row: 1
        },
        CreditCard: {
            column: 5,
            row: 1
        },
        CeremonialDagger: {
            column: 5,
            row: 5
        },
        Banner: {
            column: 1,
            row: 2
        },
        MysticSummit: {
            column: 2,
            row: 2
        },
        MarbleJoker: {
            column: 3,
            row: 2
        },
        LoyaltyCard: {
            column: 4,
            row: 2
        },
        EightBall: {
            column: 0,
            row: 5
        },
        Misprint: {
            column: 6,
            row: 2
        },
        Dusk: {
            column: 4,
            row: 7
        },
        RaisedFist: {
            column: 8,
            row: 2
        },
        ChaostheClown: {
            column: 1,
            row: 0
        },
        Fibonacci: {
            column: 1,
            row: 5
        },
        SteelJoker: {
            column: 7,
            row: 2
        },
        ScaryFace: {
            column: 2,
            row: 3
        },
        AbstractJoker: {
            column: 3,
            row: 3
        },
        DelayedGratification: {
            column: 4,
            row: 3
        },
        Hack: {
            column: 5,
            row: 2
        },
        Pareidolia: {
            column: 6,
            row: 3
        },
        GrosMichel: {
            column: 7,
            row: 6
        },
        EvenSteven: {
            column: 8,
            row: 3
        },
        OddTodd: {
            column: 9,
            row: 3
        },
        Scholar: {
            column: 0,
            row: 4
        },
        BusinessCard: {
            column: 1,
            row: 4
        },
        Supernova: {
            column: 2,
            row: 4
        },
        RidetheBus: {
            column: 1,
            row: 6
        },
        SpaceJoker: {
            column: 3,
            row: 5
        },
        Egg: {
            column: 0,
            row: 10
        },
        Burglar: {
            column: 1,
            row: 10
        },
        Blackboard: {
            column: 2,
            row: 10
        },
        Runner: {
            column: 3,
            row: 10
        },
        IceCream: {
            column: 4,
            row: 10
        },
        DNA: {
            column: 5,
            row: 10
        },
        Splash: {
            column: 6,
            row: 10
        },
        BlueJoker: {
            column: 7,
            row: 10
        },
        SixthSense: {
            column: 8,
            row: 10
        },
        Constellation: {
            column: 9,
            row: 10
        },
        Hiker: {
            column: 0,
            row: 11
        },
        FacelessJoker: {
            column: 1,
            row: 11
        },
        GreenJoker: {
            column: 2,
            row: 11
        },
        Superposition: {
            column: 3,
            row: 11
        },
        ToDoList: {
            column: 4,
            row: 11
        },
        Cavendish: {
            column: 5,
            row: 11
        },
        CardSharp: {
            column: 6,
            row: 11
        },
        RedCard: {
            column: 7,
            row: 11
        },
        Madness: {
            column: 8,
            row: 11
        },
        SquareJoker: {
            column: 9,
            row: 11
        },
        Seance: {
            column: 0,
            row: 12
        },
        RiffRaff: {
            column: 1,
            row: 12
        },
        Vampire: {
            column: 2,
            row: 12
        },
        Shortcut: {
            column: 3,
            row: 12
        },
        Hologram: {
            column: 4,
            row: 12
        },
        Vagabond: {
            column: 5,
            row: 12
        },
        Baron: {
            column: 6,
            row: 12
        },
        Cloud9: {
            column: 7,
            row: 12
        },
        Rocket: {
            column: 8,
            row: 12
        },
        Obelisk: {
            column: 9,
            row: 12
        },
        MidasMask: {
            column: 0,
            row: 13
        },
        Luchador: {
            column: 1,
            row: 13
        },
        Photograph: {
            column: 2,
            row: 13
        },
        GiftCard: {
            column: 3,
            row: 13
        },
        TurtleBean: {
            column: 4,
            row: 13
        },
        Erosion: {
            column: 5,
            row: 13
        },
        ReservedParking: {
            column: 6,
            row: 13
        },
        MailInRebate: {
            column: 7,
            row: 13
        },
        TotheMoon: {
            column: 8,
            row: 13
        },
        Hallucination: {
            column: 9,
            row: 13
        },
        FortuneTeller: {
            column: 7,
            row: 5
        },
        Juggler: {
            column: 0,
            row: 1
        },
        Drunkard: {
            column: 1,
            row: 1
        },
        StoneJoker: {
            column: 9,
            row: 0
        },
        GoldenJoker: {
            column: 9,
            row: 2
        },
        LuckyCat: {
            column: 5,
            row: 14
        },
        BaseballCard: {
            column: 6,
            row: 14
        },
        Bull: {
            column: 7,
            row: 14
        },
        DietCola: {
            column: 8,
            row: 14
        },
        TradingCard: {
            column: 9,
            row: 14
        },
        FlashCard: {
            column: 0,
            row: 15
        },
        Popcorn: {
            column: 1,
            row: 15
        },
        SpareTrousers: {
            column: 4,
            row: 15
        },
        AncientJoker: {
            column: 7,
            row: 15
        },
        Ramen: {
            column: 2,
            row: 15
        },
        WalkieTalkie: {
            column: 8,
            row: 15
        },
        Seltzer: {
            column: 3,
            row: 15
        },
        Castle: {
            column: 9,
            row: 15
        },
        SmileyFace: {
            column: 6,
            row: 15
        },
        Campfire: {
            column: 5,
            row: 15
        }
    }
};