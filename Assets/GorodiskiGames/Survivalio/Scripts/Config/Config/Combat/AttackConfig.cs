using System;
using UnityEngine;

public enum AttackShapeType
{
    Circle,     // 원형(주변)
    Sector,     // 부채꼴(반원/전방 부채꼴)
    LineBox     // 직선(박스)
}

[CreateAssetMenu(menuName = "Game/Combat/AttackConfig", fileName = "AttackConfig_")]
public class AttackConfig : ScriptableObject
{
    [Header("Identity")]
    public string id = "attack_01";

    [Header("Animation")]
    public string animatorTrigger = "attack_01"; // 트리거 or state name
    public float windupTime = 0.05f;             // 공격 시작 후 히트까지 대기(초)
    public float recoverTime = 0.10f;            // 후딜(초)
    public bool lockFacingToTarget = true;       // 공격 중 타겟 방향 고정

    [Header("Move During Attack (Transform-based)")]
    public AttackMove move; // 02/03에만 넣어도 됨

    [Header("Hit Shape")]
    public AttackShapeType shapeType = AttackShapeType.Sector;
    public ShapeCircle circle;
    public ShapeSector sector;
    public ShapeLineBox lineBox;
    public Vector3 originOffset = new Vector3(0f, 0.9f, 0.5f); // 공격 원점(가슴/무기 앞)

    [Header("Targeting")]
    public int maxTargets = 3;
    public LayerMask targetMask;  // 몬스터 레이어
    public bool hitOncePerAttack = true;

    [Header("Knockback (optional)")]
    public Knockback knockback;   // attack_03에만 주로 사용

    [Serializable]
    public struct AttackMove
    {
        public bool enabled;
        public float distance;   // 총 이동거리
        public float duration;   // 이동에 걸리는 시간
        public AnimationCurve curve; // 0~1 (없으면 Linear로 사용)
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
        [Range(0f, 360f)] public float angle; // 예: 120 = 전방 120도
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
        public float staggerTime; // 짧은 경직(선택)
    }
}