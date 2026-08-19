import { motion } from 'framer-motion';

function PageTransition({ children }) {
    return (
        <motion.div
            initial={{
                opacity: 0,
                scale: 1.08
            }}
            animate={{
                opacity: 1,
                scale: 1
            }}
            exit={{
                opacity: 0,
                scale: 0.92
            }}
            transition={{
                duration: 0.45,
                ease: [0.22, 1, 0.36, 1]
            }}
            style={{
                minHeight: '100vh'
            }}
        >
            {children}
        </motion.div>
    );
}

export default PageTransition;