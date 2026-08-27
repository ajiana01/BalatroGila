import { motion, AnimatePresence } from 'framer-motion';
import PlayingCard from '../../PlayingCard/PlayingCard';
import './PlayingArea.css';

function PlayingArea({
    playedCards = [],
    scoringCardIndex = -1,
    scoringCardIds = new Set(),
    floatingScores = {}
}) {
    if (!playedCards || playedCards.length === 0) {
        return null;
    }

    return (
        <div className="playing-area-container">
            <motion.div
                className="playing-cards-row"
                initial={{ opacity: 0, y: 30 }}
                animate={{ opacity: 1, y: 0 }}
                exit={{ opacity: 0, y: -40 }}
                transition={{ type: 'spring', stiffness: 400, damping: 28 }}
            >
                {playedCards.map((card, idx) => {
                    const isCurrentlyScoring = scoringCardIndex === idx;
                    const isScoringCard = scoringCardIds.has(card.id);
                    const floatingScore = floatingScores[card.id];

                    return (
                        <motion.div
                            key={card.id || `${card.suit}-${card.rank}-${idx}`}
                            className={`played-card-wrapper ${
                                isCurrentlyScoring ? 'scoring-active' : ''
                            } ${!isScoringCard && scoringCardIndex >= 0 ? 'unscored' : ''}`}
                            initial={{ y: 60, scale: 0.85, opacity: 0 }}
                            animate={{
                                y: isCurrentlyScoring ? -18 : 0,
                                scale: isCurrentlyScoring ? 1.12 : 1,
                                opacity: 1,
                                rotate: isCurrentlyScoring ? (idx % 2 === 0 ? -2 : 2) : 0
                            }}
                            exit={{ y: -80, opacity: 0, scale: 0.7 }}
                            transition={{
                                type: 'spring',
                                stiffness: 480,
                                damping: 25,
                                delay: idx * 0.04
                            }}
                        >
                            <AnimatePresence>
                                {floatingScore && (
                                    <motion.div
                                        key={floatingScore.key || `${card.id}-score`}
                                        className={`card-floating-score ${floatingScore.type || 'chips'}`}
                                        initial={{ opacity: 0, scale: 0.5, y: 10 }}
                                        animate={{ opacity: 1, scale: 1.2, y: -16 }}
                                        exit={{ opacity: 0, scale: 0.8, y: -26 }}
                                        transition={{ duration: 0.45, ease: 'easeOut' }}
                                    >
                                        {floatingScore.text}
                                    </motion.div>
                                )}
                            </AnimatePresence>

                            <PlayingCard
                                suit={card.suit}
                                rank={card.rank}
                                enhancement={card.enhancement}
                                edition={card.edition}
                                seal={card.seal}
                                isDebuffed={card.isDebuffed}
                                width={92}
                                height={130}
                            />
                        </motion.div>
                    );
                })}
            </motion.div>
        </div>
    );
}

export default PlayingArea;