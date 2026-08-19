import './Sprite.css';

function Sprite({
                    sprite,
                    column,
                    row,
                    width,
                    height,
                    animated = false,
                    className = ''
                }) {
    const scaleX = width / sprite.cellWidth;
    const scaleY = height / sprite.cellHeight;

    return (
        <div
            className={`sprite ${animated ? 'sprite-animated' : ''} ${className}`}
            style={{
                width: `${width}px`,
                height: `${height}px`,

                backgroundImage: `url("${sprite.image}")`,

                backgroundSize: `
                    ${sprite.sheetWidth * scaleX}px
                    ${sprite.sheetHeight * scaleY}px
                `,

                backgroundPosition: `
                    -${column * sprite.cellWidth * scaleX}px
                    -${row * sprite.cellHeight * scaleY}px
                `,

                backgroundRepeat: 'no-repeat'
            }}
        />
    );
}

export default Sprite;