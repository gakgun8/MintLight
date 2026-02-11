using UnityEngine;
using UnityEngine.UI;

namespace Game.UI.Hud
{
    public sealed class PurchaseEnergyHudView : BaseHud
    {
        [SerializeField] private Button _closeButton;
        [SerializeField] private ShopProductView _productExchange;
        [SerializeField] private ShopProductView _productAds;

        public Button CloseButon => _closeButton;
        public ShopProductView ProductExchange => _productExchange;
        public ShopProductView ProductAds => _productAds;

        protected override void OnEnable()
        {

        }

        protected override void OnDisable()
        {

        }
    }
}

