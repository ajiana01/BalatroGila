import Sprite from '../Sprite/Sprite';
import { voucherSprite } from '../../data/sprites/voucherSprites';

function Voucher({
                     voucher,
                     width = 100,
                     height = 140,
                     animated = false
                 }) {
    const voucherData = voucherSprite.vouchers[voucher];

    if (!voucherData) {
        console.error(`Voucher tidak ditemukan: ${voucher}`);
        return null;
    }

    return (
        <Sprite
            sprite={voucherSprite}
            column={voucherData.column}
            row={voucherData.row}
            width={width}
            height={height}
            animated={animated}
        />
    );
}

export default Voucher;

// using
// <Voucher voucher="Overstock" />