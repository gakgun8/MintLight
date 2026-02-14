using Game.Config;
using Game.Equipment;
using UnityEngine;

namespace Game.Cloth
{
    public sealed class ClothModel : EquipmentModel
    {
        public ClothElementType ClothType;
        public Mesh Mesh;
        public AnimatorOverrideController AnimationOverride;

        public float Armor => Attributes[AttributeType.Armor];

        public ClothModel(ClothConfig config, int serial, int level) : base(config, serial, level)
        {
            ClothType = config.ClothType;
            Mesh = config.Mesh;
            AnimationOverride = config.AnimationOverride;

            UpdateStats();
        }

        public override void SetLocalParameters()
        {

        }
    }

    public sealed class ClothView : MonoBehaviour
    {

    }
}

