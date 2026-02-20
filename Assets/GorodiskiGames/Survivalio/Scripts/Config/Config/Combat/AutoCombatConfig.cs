using UnityEngine;

public enum TargetPriority
{
    Nearest,
    LowestHP,
    EliteFirst
}

public enum OutOfRangeBehaviour
{
    WaitForRange,
    RotateOnly
}

[CreateAssetMenu(menuName = "Game/Combat/AutoCombatConfig", fileName = "AutoCombatConfig_")]
public class AutoCombatConfig : ScriptableObject
{
    [Header("General")]
    public bool enableAutoCombat = true;
    public bool enableDebugLogs = true;

    [Header("Detection / Targeting")]
    public float detectionRadius = 6.0f;
    public float chaseRadius = 8.0f;
    public float retargetInterval = 0.25f;
    public float targetScanInterval = 0.25f;
    public TargetPriority priority = TargetPriority.Nearest;
    public LayerMask enemyMask;

    [Header("Out of Range")]
    public OutOfRangeBehaviour outOfRangeBehaviour = OutOfRangeBehaviour.WaitForRange;

    [Header("Approach")]
    public float stopDistance = 1.25f;
    public float approachSpeedMultiplier = 1f;

    [Header("Attack Loop")]
    public float globalAttackCooldown = 0.1f;
    public AttackConfig[] combo;
    public bool loopCombo = true;

    [Header("Combo Policy (optional)")]
    [Range(0f, 1f)] public float useAttack02Chance = 0.85f;
    [Range(0f, 1f)] public float useAttack03Chance = 0.55f;
    public float attack03Cooldown = 2.5f;
}
