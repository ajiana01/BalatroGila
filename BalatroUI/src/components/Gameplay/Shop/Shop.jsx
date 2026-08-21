import ShopItem from './ShopItem';

import './Shop.css';

function Shop({
                  gameData,
                  onContinue
              }) {

    return (
        <div className="shop">

            <header className="shop-header">

                <h1>Shop</h1>

                <div className="shop-money">
                    ${gameData.money}
                </div>

            </header>

            <div className="shop-items">

                <ShopItem
                    title="Joker"
                    description="+4 Mult"
                    price={4}
                />

                <ShopItem
                    title="Tarot"
                    description="Enhance a card"
                    price={3}
                />

                <ShopItem
                    title="Planet"
                    description="Upgrade Poker Hand"
                    price={5}
                />

            </div>

            <button
                className="shop-continue"
                onClick={onContinue}
            >
                Continue
            </button>

        </div>
    );
}

export default Shop;