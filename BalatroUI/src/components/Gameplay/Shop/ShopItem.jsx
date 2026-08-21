function ShopItem({
                      title,
                      description,
                      price
                  }) {

    return (
        <div className="shop-item">

            <div className="shop-item-icon">
                🃏
            </div>

            <h2>{title}</h2>

            <p>{description}</p>

            <button>
                ${price}
            </button>

        </div>
    );
}

export default ShopItem;