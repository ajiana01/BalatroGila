import { useState, useEffect } from 'react';
import Sprite from '../Sprite/Sprite';
import { shopSignSprite } from '../../data/sprites/shopSignSprites';
import './ShopSign.css';

function ShopSign({
    width = 196,
    height = 99,
    animated = true,
    fps = 6,
    className = ''
}) {
    const [frame, setFrame] = useState(0);

    useEffect(() => {
        if (!animated) return;

        const interval = setInterval(() => {
            setFrame(prev => (prev + 1) % shopSignSprite.columns);
        }, 1000 / fps);

        return () => clearInterval(interval);
    }, [animated, fps]);

    return (
        <div className={`shop-sign-wrapper ${className}`}>
            <Sprite
                sprite={shopSignSprite}
                column={frame}
                row={0}
                width={width}
                height={height}
                className="shop-sign-sprite"
            />
        </div>
    );
}

export default ShopSign;
