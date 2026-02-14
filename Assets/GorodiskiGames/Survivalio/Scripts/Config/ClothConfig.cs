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
        [Tooltip("Character part prefab to instantiate when this cloth is equipped.")]
        public GameObject Prefab;
        public Mesh Mesh;
        [Tooltip("Optional: animation override controller used while this cloth is equipped.")]
        public AnimatorOverrideController AnimationOverride;
    }
}
