using System;
using UnityEngine;

namespace Game.Config
{
    public enum ClothElementType
    {
        Helmet,
        Vest,
        Uniform,
        Gloves,
        Shoes
    }

    [Serializable]
    [CreateAssetMenu(menuName = "Config/ClothConfig")]
    public class ClothConfig : EquipmentConfig
    {
        public ClothElementType ClothType;
        public Mesh Mesh;
        [Tooltip("Optional: animation override controller used while this cloth is equipped.")]
        public AnimatorOverrideController AnimationOverride;
    }
}
