import GameSidebar from '../GameSidebar/GameSidebar';
import BlindCard from './BlindCard';
import { mapBackendBlinds } from '../../../utils/cardMapper';
import './BlindSelection.css';

function BlindSelection({
    gameData,
    onSelectBlind,
    onOpenSettings
}) {
    const rawBlinds = gameData?.availableBlinds?.length ? gameData.availableBlinds : [];
    const blinds = rawBlinds.length > 0 ? mapBackendBlinds(rawBlinds) : [
        {
            id: 1,
            type: 'small',
            blind: 'SmallBlind',
            title: 'Small Blind',
            score: 300,
            reward: '$$$+',
            isDefeated: false
        },
        {
            id: 2,
            type: 'big',
            blind: 'BigBlind',
            title: 'Big Blind',
            score: 450,
            reward: '$$$$+',
            isDefeated: false
        },
        {
            id: 3,
            type: 'boss',
            blind: 'TheGoad',
            title: 'The Goad',
            score: 600,
            reward: '$$$$$+',
            description: 'All Spade cards are debuffed',
            isDefeated: false
        }
    ];

    // Find first undefeated blind index
    const activeIndex = blinds.findIndex(b => !b.isDefeated);

    const getStatus = (index, blind) => {
        if (blind.isDefeated) return 'defeated';
        if (index === activeIndex) return 'active';
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
                    {blinds.map((blind, idx) => {
                        const status = getStatus(idx, blind);
                        return (
                            <BlindCard
                                key={blind.id || idx}
                                type={blind.type}
                                blind={blind.blind}
                                title={blind.title}
                                score={blind.score}
                                reward={blind.reward}
                                description={blind.description}
                                status={status}
                                onSelect={() => onSelectBlind(blind)}
                            />
                        );
                    })}
                </div>
            </section>
        </div>
    );
}

export default BlindSelection;
