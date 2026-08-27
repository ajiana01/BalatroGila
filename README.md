# 🃏 BalatroGila

A web-based recreation of the roguelike deck-building card game **Balatro**, built with a **.NET 8 backend** and a **React (Vite) frontend**. The backend serves as the core game engine — handling all game logic, scoring, and state management — while the frontend provides an interactive UI to play the game in your browser.

---

## ✨ Features

- **Full Game Loop** — Start a new run, select blinds, play poker hands, discard cards, visit the shop, and progress through antes.
- **Poker Hand Evaluation** — Automatically detects hand types (Flush, Straight, Full House, etc.) and calculates chip + multiplier scoring.
- **Boss Blinds** — Unique boss blind mechanics that alter gameplay rules each ante.
- **Shop System** — Buy jokers, consumables (tarot/planet/spectral cards), vouchers, and booster packs between rounds.
- **Joker System** — Collectible jokers that modify scoring with various effects and editions.
- **Consumable Cards** — Use tarot, planet, and spectral cards to enhance your deck and strategy.
- **Voucher System** — Purchase permanent upgrades that persist through your run.
- **Booster Packs** — Open packs to discover new cards and jokers.
- **Session Management** — Multiple concurrent game sessions supported via session IDs.
- **RESTful API** — Fully documented API with Swagger/OpenAPI support.
- **Animated React UI** — Smooth card animations powered by Framer Motion with custom card sprites.

---

## 🏗️ Architecture

```
BalatroGila/
├── BackendBalatro/          # .NET 8 Web API — Core Game Engine
│   ├── Controllers/         # REST API endpoints (Game, Action, Shop)
│   ├── Models/              # DTOs, Entities, Interfaces
│   ├── Enums/               # Game enumerations (Suits, Ranks, Phases, etc.)
│   └── Services/            # Business logic
│       ├── Core/            # Game engine & state machine
│       ├── Evaluators/      # Poker hand evaluation & scoring
│       ├── Sessions/        # Game session management
│       ├── Shop/            # Shop item generation & purchasing
│       └── Consumables/     # Tarot, Planet, Spectral card effects
├── BackendBalatro.Tests/    # xUnit test suite
├── BalatroUI/               # React frontend (Vite)
│   └── src/
│       ├── components/      # UI components (cards, shop, blinds, etc.)
│       ├── pages/           # Main Menu & Gameplay pages
│       ├── services/        # API client layer
│       └── assets/          # Sprites and images
└── BalatroRules/            # Game rules documentation (markdown)
```

---

## 🛠️ Tech Stack

| Layer     | Technology                                                  |
|-----------|-------------------------------------------------------------|
| Backend   | .NET 8, ASP.NET Core Web API, Swagger / OpenAPI             |
| Frontend  | React 19, Vite 8, Framer Motion, React Router               |
| Testing   | xUnit, Microsoft.NET.Test.Sdk                               |
| Language  | C# (backend), JavaScript / JSX (frontend)                   |

---

## 📋 Prerequisites

Make sure you have the following installed on your machine:

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) (or later)
- [Node.js](https://nodejs.org/) (v18 or later recommended)
- [npm](https://www.npmjs.com/) (comes with Node.js)

---

## 🚀 Getting Started

### 1. Clone the Repository

```bash
git clone https://github.com/<your-username>/BalatroGila.git
cd BalatroGila
```

### 2. Run the Backend

```bash
cd BackendBalatro
dotnet restore
dotnet run
```

The API server will start at **`http://localhost:5264`** by default.

> **Tip:** Visit `http://localhost:5264/swagger` to explore the interactive API documentation.

### 3. Run the Frontend

Open a new terminal:

```bash
cd BalatroUI
npm install
npm run dev
```

The React dev server will start at **`http://localhost:5173`**.

### 4. Play the Game

Open your browser and navigate to **`http://localhost:5173`** — you're ready to play!

---

## 🧪 Running Tests

The project includes a comprehensive xUnit test suite covering the game engine:

```bash
cd BackendBalatro.Tests
dotnet test
```

**Test coverage includes:**
- Poker hand evaluation
- Scoring calculations
- Boss blind mechanics
- Shop & economy system
- Game state machine & progression
- Deck initialization
- API controller integration
- Win conditions & ante progression

---

## 🔌 API Overview

All endpoints use `X-Session-Id` header (or `sessionId` query param) for session management.

### Game Management (`/api/game`)

| Method | Endpoint              | Description                          |
|--------|-----------------------|--------------------------------------|
| POST   | `/api/game/start`     | Start a new game session             |
| GET    | `/api/game/state`     | Get current game state               |
| GET    | `/api/game/blinds`    | Get available blinds for selection   |
| POST   | `/api/game/blinds/select` | Select a blind to play           |

### Player Actions (`/api/action`)

| Method | Endpoint                       | Description                           |
|--------|--------------------------------|---------------------------------------|
| POST   | `/api/action/play-hand`        | Play selected cards as a poker hand   |
| POST   | `/api/action/discard`          | Discard selected cards and draw new   |
| POST   | `/api/action/score-preview`    | Preview score without playing         |
| POST   | `/api/action/use-consumable`   | Use a consumable card                 |
| POST   | `/api/action/sell-card`        | Sell a card for money                 |
| POST   | `/api/action/reorder-jokers`   | Rearrange joker order (affects scoring) |
| POST   | `/api/action/reorder-consumables` | Rearrange consumable order         |

### Shop (`/api/shop`)

| Method | Endpoint                       | Description                          |
|--------|--------------------------------|--------------------------------------|
| GET    | `/api/shop`                    | View shop inventory                  |
| POST   | `/api/shop/buy-card`           | Buy a card from the shop             |
| POST   | `/api/shop/reroll`             | Reroll shop items (costs money)      |
| POST   | `/api/shop/buy-booster`        | Buy a booster pack                   |
| POST   | `/api/shop/select-booster-card`| Select a card from an opened pack    |
| POST   | `/api/shop/skip-booster`       | Skip remaining booster pack cards    |
| POST   | `/api/shop/buy-voucher`        | Purchase a voucher upgrade           |
| POST   | `/api/shop/leave`              | Leave the shop and proceed           |

---

## 📄 Game Rules Reference

The `BalatroRules/` directory contains detailed markdown documentation for all game mechanics:

- **Blinds** — Small, Big, and Boss blind rules & scoring targets
- **Jokers** — Complete joker catalog with effects and rarities
- **Tarot Cards** — Card enhancement and modification effects
- **Planet Cards** — Poker hand level-up mechanics
- **Spectral Cards** — Rare powerful card effects
- **Vouchers** — Permanent run upgrades
- **Booster Packs** — Pack types and drop rates
- **The Shop** — Shop mechanics and pricing
- **Card Editions & Enhancements** — Foil, holographic, polychrome, and more

---

## 🤝 Contributing

1. Fork the repository
2. Create a feature branch (`git checkout -b feature/amazing-feature`)
3. Commit your changes (`git commit -m 'Add amazing feature'`)
4. Push to the branch (`git push origin feature/amazing-feature`)
5. Open a Pull Request

---

## 📝 License

This project is for educational and personal use. Balatro is a game by [LocalThunk](https://www.playbalatro.com/).
