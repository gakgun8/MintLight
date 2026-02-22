using System;
using System.Collections.Generic;
using System.Linq;
using Game.Config;
using Game.Enemy;
using Game.Managers;
using Game.Unit;
using UnityEngine;

namespace Game.Player
{
    /// <summary>
    /// Gameplay authority for attack request/hit timing.
    /// Animator is presentation-only; hit is applied only on FireAttackHit event.
    /// </summary>
    public sealed class PlayerCombatController : IAnimEventReceiver
    {
        private readonly PlayerView _view;
        private readonly PlayerModel _model;
        private readonly AutoCombatConfig _autoCombatConfig;
        private readonly AttackConfig _defaultAttackConfig;
        private readonly AttackConfig[] _comboConfigs;

        private readonly HashSet<EnemyController> _hitEnemiesInCurrentAttack = new HashSet<EnemyController>();

        private UnitController _pendingHitTarget;
        private GameManager _pendingHitGameManager;
        private AttackConfig _currentAttackConfig;
        private int _comboIndex;
        private float _nextAttackTime;

        public PlayerCombatController(PlayerView view, PlayerModel model, AutoCombatConfig autoCombatConfig, AttackConfig defaultAttackConfig)
        {
            _view = view;
            _model = model;
            _autoCombatConfig = autoCombatConfig;
            _defaultAttackConfig = defaultAttackConfig;
            _comboConfigs = _autoCombatConfig != null ? _autoCombatConfig.combo : null;
        }

        public bool CanRequestAttack(float now) => now >= _nextAttackTime && !_view.IsAttackPlaying;

        public void RequestAttack(UnitController target, GameManager gameManager, float now)
        {
            if (target == null)
                return;

            _pendingHitTarget = target;
            _pendingHitGameManager = gameManager;

            _comboIndex = (_comboIndex % 3) + 1;
            _currentAttackConfig = GetAttackConfigForCombo(_comboIndex);
            _hitEnemiesInCurrentAttack.Clear();

            var cooldownSource = _currentAttackConfig ?? _defaultAttackConfig;
            var cooldown = cooldownSource != null ? Mathf.Max(0.01f, cooldownSource.attackCooldown) : 0.5f;
            _nextAttackTime = now + cooldown;

            _view.PlayAttackCombo(_comboIndex);
        }

        public void ResetCombo()
        {
            _comboIndex = 0;
            _currentAttackConfig = null;
            _pendingHitTarget = null;
            _pendingHitGameManager = null;
            _hitEnemiesInCurrentAttack.Clear();
        }

        public void OnFireAttackHit()
        {
            var attackConfig = _currentAttackConfig ?? _defaultAttackConfig;
            var targets = ResolveAttackTargets(attackConfig);
            if (targets.Count == 0)
            {
                _pendingHitTarget = null;
                _pendingHitGameManager = null;
                return;
            }

            var attackValue = _model.GetAttribute(UnitAttributeType.Attack);
            var multiplier = attackConfig != null ? Mathf.Max(0.01f, attackConfig.damageMultiplier) : 1f;
            var damage = Mathf.Max(1, Mathf.RoundToInt(attackValue * multiplier));

            for (var i = 0; i < targets.Count; i++)
            {
                var enemy = targets[i];
                if (enemy == null || enemy.View == null || enemy.Model == null || enemy.Model.Health <= 0)
                    continue;

                var direction = enemy.View.Position - _view.Position;
                direction.y = 0f;
                direction = direction.sqrMagnitude > 0.0001f ? direction.normalized : _view.transform.forward;
                enemy.TryToDamage(damage, direction);
                _hitEnemiesInCurrentAttack.Add(enemy);
            }

            _pendingHitTarget = null;
            _pendingHitGameManager = null;
        }

        public void OnAttackWindupStart() { }
        public void OnAttackRecoverEnd() { }

        private AttackConfig GetAttackConfigForCombo(int comboIndex)
        {
            if (_comboConfigs == null || _comboConfigs.Length == 0)
                return null;

            var idx = comboIndex - 1;
            return idx >= 0 && idx < _comboConfigs.Length ? _comboConfigs[idx] : null;
        }

        private List<EnemyController> ResolveAttackTargets(AttackConfig attackConfig)
        {
            var result = new List<EnemyController>();
            if (_pendingHitGameManager == null || _pendingHitGameManager.Enemies == null)
                return result;

            if (attackConfig == null)
            {
                if (_pendingHitTarget is EnemyController fallbackEnemy)
                    result.Add(fallbackEnemy);
                return result;
            }

            var rotateNode = _view.RotateNode != null ? _view.RotateNode : _view.transform;
            var origin = rotateNode.position + rotateNode.TransformDirection(attackConfig.originOffset);
            var forward = rotateNode.forward;
            forward.y = 0f;
            if (forward.sqrMagnitude < 0.0001f)
                forward = _view.transform.forward;
            forward.Normalize();
            var right = Vector3.Cross(Vector3.up, forward);

            for (var i = 0; i < _pendingHitGameManager.Enemies.Count; i++)
            {
                var enemy = _pendingHitGameManager.Enemies[i];
                if (enemy == null || enemy.View == null || enemy.Model == null || enemy.Model.Health <= 0)
                    continue;

                if (attackConfig.hitOncePerAttack && _hitEnemiesInCurrentAttack.Contains(enemy))
                    continue;

                var targetPos = enemy.View.AimPosition;
                if (!IsInsideAttackShape(attackConfig, origin, forward, right, targetPos))
                    continue;

                result.Add(enemy);
            }

            if (attackConfig.maxTargets > 0 && result.Count > attackConfig.maxTargets)
                result = result.OrderBy(e => (e.View.Position - _view.Position).sqrMagnitude).Take(attackConfig.maxTargets).ToList();

            return result;
        }

        private static bool IsInsideAttackShape(AttackConfig cfg, Vector3 origin, Vector3 forward, Vector3 right, Vector3 target)
        {
            var delta = target - origin;
            delta.y = 0f;

            switch (cfg.shapeType)
            {
                case AttackShapeType.Circle:
                    var radius = cfg.circle.radius > 0.01f ? cfg.circle.radius : Mathf.Max(cfg.hitRadius, cfg.attackRange);
                    return delta.sqrMagnitude <= radius * radius;
                case AttackShapeType.LineBox:
                    var localForward = Vector3.Dot(delta, forward);
                    var localRight = Mathf.Abs(Vector3.Dot(delta, right));
                    var length = cfg.lineBox.length > 0.01f ? cfg.lineBox.length : cfg.attackRange;
                    var halfWidth = Mathf.Max(0.05f, cfg.lineBox.width * 0.5f);
                    return localForward >= 0f && localForward <= length && localRight <= halfWidth;
                default:
                    var sectorRadius = cfg.sector.radius > 0.01f ? cfg.sector.radius : cfg.attackRange;
                    if (delta.sqrMagnitude > sectorRadius * sectorRadius)
                        return false;
                    var angle = cfg.sector.angle > 0.01f ? cfg.sector.angle : 90f;
                    return Vector3.Angle(forward, delta.normalized) <= angle * 0.5f;
            }
        }
    }
}
