import { motion } from 'framer-motion';
import PlayingCard from '../PlayingCard/PlayingCard.jsx';

function GameTransition({ onComplete }) {

    return (
        <motion.div
            initial={{ opacity: 0 }}
            animate={{ opacity: 1 }}
            transition={{
                duration: 0.4,
                ease: 'easeOut'
            }}
            onAnimationComplete={() => {
                setTimeout(onComplete, 700);
            }}
            style={{
                position: 'fixed',
                inset: 0,
                zIndex: 9999,

                display: 'flex',
                justifyContent: 'center',
                alignItems: 'center',

                background: 'rgba(0, 0, 0, 0.8)',

                perspective: '1000px',

                pointerEvents: 'none'
            }}
        >

            <motion.div
                initial={{
                    scale: 0.65,
                    rotateY: 0,
                    opacity: 0
                }}
                animate={{
                    scale: [0.65, 1.05, 1],
                    rotateY: 180,
                    opacity: 1
                }}
                transition={{
                    duration: 1.8,
                    ease: [0.22, 1, 0.36, 1],

                    scale: {
                        duration: 1.8,
                        ease: [0.22, 1, 0.36, 1]
                    },

                    rotateY: {
                        duration: 1.5,
                        ease: [0.22, 1, 0.36, 1]
                    },

                    opacity: {
                        duration: 0.35
                    }
                }}
                style={{
                    transformStyle: 'preserve-3d'
                }}
            >

                {/* CARD BACK */}
                <div
                    style={{
                        position: 'absolute',

                        width: '110px',
                        height: '150px',

                        backfaceVisibility: 'hidden'
                    }}
                >
                    <PlayingCard
                        rank="A"
                        suit="Spades"
                        width={110}
                        height={150}
                        effect="effect-3d"
                        showBack
                        backType="BackNormal"
                    />
                </div>

                {/* CARD FRONT */}
                <div
                    style={{
                        width: '110px',
                        height: '150px',

                        backfaceVisibility: 'hidden',

                        transform: 'rotateY(180deg)'
                    }}
                >
                    <PlayingCard
                        rank="A"
                        suit="Spades"
                        width={110}
                        height={150}
                        effect="effect-3d"
                    />
                </div>

            </motion.div>

        </motion.div>
    );
}

export default GameTransition;