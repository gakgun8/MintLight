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
        public GameObject Prefab;

        public float Armor => Attributes[AttributeType.Armor];

        public ClothModel(ClothConfig config, int serial, int level) : base(config, serial, level)
        {
            ClothType = config.ClothType;
            Mesh = config.Mesh;
            AnimationOverride = config.AnimationOverride;
            Prefab = config.Prefab;

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

