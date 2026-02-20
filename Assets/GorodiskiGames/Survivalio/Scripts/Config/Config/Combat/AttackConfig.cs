using System;
using UnityEngine;

public enum AttackShapeType
{
    Circle,
    Sector,
    LineBox
}

[CreateAssetMenu(menuName = "Game/Combat/AttackConfig", fileName = "AttackConfig_")]
public class AttackConfig : ScriptableObject
{
    [Header("Identity")]
    public string id = "attack_01";

    [Header("Animation")]
    public string animatorTrigger = "attack_01";
    public float windupTime = 0.05f;
    public float recoverTime = 0.10f;
    public bool lockFacingToTarget = true;

    [Header("Core Melee Settings")]
    public float attackRange = 1.75f;
    public float attackCooldown = 0.75f;
    public float hitDelay = 0f;
    public float hitRadius = 1.5f;
    public Vector3 hitBox = new Vector3(1.5f, 1f, 1.5f);
    public float damageMultiplier = 1f;

    [Header("Move During Attack (Transform-based)")]
    public AttackMove move;

    [Header("Hit Shape")]
    public AttackShapeType shapeType = AttackShapeType.Sector;
    public ShapeCircle circle;
    public ShapeSector sector;
    public ShapeLineBox lineBox;
    public Vector3 originOffset = new Vector3(0f, 0.9f, 0.5f);

    [Header("Targeting")]
    public int maxTargets = 3;
    public LayerMask targetMask;
    public bool hitOncePerAttack = true;

    [Header("Knockback (optional)")]
    public Knockback knockback;

    [Serializable]
    public struct AttackMove
    {
        public bool enabled;
        public float distance;
        public float duration;
        public AnimationCurve curve;
    }

    [Serializable]
    public struct ShapeCircle
    {
        public float radius;
    }

    [Serializable]
    public struct ShapeSector
    {
        public float radius;
        [Range(0f, 360f)] public float angle;
    }

    [Serializable]
    public struct ShapeLineBox
    {
        public float length;
        public float width;
    }

    [Serializable]
    public struct Knockback
    {
        public bool enabled;
        public float distance;
        public float duration;
        public float staggerTime;
    }
}
