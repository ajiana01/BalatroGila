import GameSidebar from '../GameSidebar/GameSidebar';
import BlindCard from './BlindCard';
import './BlindSelection.css';

function BlindSelection({
    gameData,
    onSelectBlind,
    onOpenSettings
}) {
    const currentBlindIndex = gameData.blindIndex ?? 0;

    const baseScore = 300 * Math.pow(1.5, Math.max(0, gameData.ante - 1));
    const smallScore = Math.round(baseScore);
    const bigScore = Math.round(baseScore * 1.5);
    const bossScore = Math.round(baseScore * 2);

    const smallBlindData = {
        type: 'small',
        blind: 'SmallBlind',
        title: 'Small Blind',
        score: smallScore,
        reward: '$$$+'
    };

    const bigBlindData = {
        type: 'big',
        blind: 'BigBlind',
        title: 'Big Blind',
        score: bigScore,
        reward: '$$$$+'
    };

    const bossBlindData = {
        type: 'boss',
        blind: 'TheGoad',
        title: 'The Goad',
        score: bossScore,
        reward: '$$$$$+',
        description: 'All Spade cards are debuffed'
    };

    const getStatus = (index) => {
        if (index === currentBlindIndex) return 'active';
        if (index < currentBlindIndex) return 'defeated';
        return 'upcoming';
    };

    return (
        <div className="blind-selection">
            <GameSidebar
                gameData={gameData}
                onOpenSettings={onOpenSettings}
                isBlindSelection={true}
            />

            <section className="blind-content">
                <div className="blind-cards">
                    <BlindCard
                        type="small"
                        blind={smallBlindData.blind}
                        title={smallBlindData.title}
                        score={smallBlindData.score}
                        reward={smallBlindData.reward}
                        status={getStatus(0)}
                        onSelect={() => onSelectBlind(smallBlindData)}
                    />

                    <BlindCard
                        type="big"
                        blind={bigBlindData.blind}
                        title={bigBlindData.title}
                        score={bigBlindData.score}
                        reward={bigBlindData.reward}
                        status={getStatus(1)}
                        onSelect={() => onSelectBlind(bigBlindData)}
                    />

                    <BlindCard
                        type="boss"
                        blind={bossBlindData.blind}
                        title={bossBlindData.title}
                        score={bossBlindData.score}
                        reward={bossBlindData.reward}
                        description={bossBlindData.description}
                        status={getStatus(2)}
                        onSelect={() => onSelectBlind(bossBlindData)}
                    />
                </div>
            </section>
        </div>
    );
}

export default BlindSelection;
