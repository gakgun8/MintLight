using UnityEngine;

public enum TargetPriority
{
    Nearest,
    LowestHP,       // 확장용(몹 HP 접근 가능할 때)
    EliteFirst      // 확장용(엘리트 태그 있을 때)
}

[CreateAssetMenu(menuName = "Game/Combat/AutoCombatConfig", fileName = "AutoCombatConfig_")]
public class AutoCombatConfig : ScriptableObject
{
    [Header("Detection / Targeting")]
    public float detectionRadius = 6.0f;      // 1) 감지 거리
    public float chaseRadius = 8.0f;          // 추적 유지 거리(이탈하면 타겟 해제)
    public float retargetInterval = 0.25f;    // 타겟 재선정 주기
    public TargetPriority priority = TargetPriority.Nearest;
    public LayerMask enemyMask;

    [Header("Approach")]
    public float stopDistance = 1.25f;        // 이 거리 이내면 공격
    public float approachSpeedMultiplier = 1f;// 접근 속도 배율(모델 WalkSpeed에 곱)

    [Header("Attack Loop")]
    public float globalAttackCooldown = 0.1f; // 공격-공격 사이 최소 간격
    public AttackConfig[] combo;              // attack_01~03 순서대로 넣기
    public bool loopCombo = true;

    [Header("Combo Policy (optional)")]
    [Range(0f, 1f)] public float useAttack02Chance = 0.85f;
    [Range(0f, 1f)] public float useAttack03Chance = 0.55f;
    public float attack03Cooldown = 2.5f;     // 03은 강공이라 쿨 추천
}