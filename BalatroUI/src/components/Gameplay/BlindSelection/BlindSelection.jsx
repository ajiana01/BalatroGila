import GameSidebar from '../GameSidebar/GameSidebar';
import BlindCard from './BlindCard';

import './BlindSelection.css';

function BlindSelection({
                            gameData,
                            onSelectBlind,
                            onSkipBlind
                        }) {

    return (
        <div className="blind-selection">

            <GameSidebar gameData={gameData} />

            <section className="blind-content">

                <div className="blind-counter">
                    0 / 5
                </div>

                <div className="blind-cards">

                    <BlindCard
                        type="small"
                        title="Small Blind"
                        score="300"
                        reward="$$$+"
                        active
                        onSelect={onSelectBlind}
                        onSkip={onSkipBlind}
                    />

                    <BlindCard
                        type="big"
                        title="Big Blind"
                        score="450"
                        reward="$$$$+"
                    />

                    <BlindCard
                        type="boss"
                        title="The Goad"
                        score="600"
                        reward="$$$$$+"
                        description="All Spade cards are debuffed"
                    />

                </div>

                <div className="deck-counter">

                    <div className="deck-card">
                        <span>♠</span>
                    </div>

                    <strong>
                        {gameData.deckRemaining}/52
                    </strong>

                </div>

            </section>

        </div>
    );
}

export default BlindSelection;