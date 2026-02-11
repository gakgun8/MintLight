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
    }
}

