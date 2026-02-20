using System;
using System.Collections.Generic;
using Core;
using Game.Cloth;
using Game.Config;
using Game.Core;
using Game.Enemy;
using Game.Effect;
using Game.Managers;
using Game.Modules;
using Game.Player.States;
using Game.Unit;
using Game.Weapon;
using Injection;
using UnityEngine;

namespace Game.Player
{
    public enum UnitAttributeType
    {
        Attack,
        Health,
        CollectDistance,
        HealthRecovery
    }

    public abstract class UnitModel : Observable
    {
        public float HealthNominal => _healthNominal;

        private float _healthNominal;

        protected Dictionary<UnitAttributeType, float> _attributes;

        public float GetAttribute(UnitAttributeType type)
        {
            return _attributes.TryGetValue(type, out float value) ? value : 0f;
        }

        public void SetAttribute(UnitAttributeType type, float value)
        {
            _attributes[type] = value;
        }

        public void UpdateNominalHealth(float value)
        {
            _healthNominal = value;
        }
    }

    public sealed class PlayerModel : UnitModel
    {
        public Sprite Icon;
        public string Label;
        public float WalkSpeed;
        public float RotateSpeed;
        public bool HasAllClothMeshes;
        public Mesh FullSkinnedMesh;
        public Mesh HelmetMesh;

        private readonly PlayerData _data;
        public Dictionary<ClothElementType, Mesh> ClothMeshMap;
        public Dictionary<ClothElementType, GameObject> ClothPrefabMap;
        public Dictionary<ClothElementType, AnimatorOverrideController> ClothAnimationControllerMap;
        public bool HasAllClothPrefabs;


        public int EquippedWeapon
        {
            get { return _data.EquippedWeapon; }
            set { _data.EquippedWeapon = value; }
        }
        public Dictionary<int, int> StoredWeapons => _data.StoredWeapons;
        public Dictionary<int, int> WeaponLevels => _data.WeaponLevels;

        public List<int> EquippedCloth => _data.EquippedCloth;
        public Dictionary<int, int> StoredCloth => _data.StoredCloth;
        public Dictionary<int, int> ClothLevels => _data.ClothLevels;

        public PlayerModel(GameConfig gameConfig)
        {
            _data = PlayerData.Load(gameConfig);
            _attributes = new Dictionary<UnitAttributeType, float>();

            var config = gameConfig.PlayerConfig;

            Label = config.Label;
            HasAllClothMeshes = config.HasAllClothMeshes;
            Icon = config.Icon;
            WalkSpeed = config.WalkSpeed;
            RotateSpeed = config.RotateSpeed;
            FullSkinnedMesh = config.FullSkinnedMesh;

            var attributeInfos = config.AttributeInfos;
            foreach (var attributeInfo in attributeInfos)
            {
                var type = attributeInfo.Type;
                var value = attributeInfo.Value;
                SetAttribute(type, value);
            }

            //attack
            if(StoredWeapons.ContainsKey(EquippedWeapon))
            {
                var index = StoredWeapons[EquippedWeapon];
                var weaponConfig = gameConfig.WeaponMap[index];
                var level = WeaponLevels[EquippedWeapon];
                var weaponModel = new WeaponModel(weaponConfig, EquippedWeapon, level);
                var value = weaponModel.Attack;
                SetAttribute(UnitAttributeType.Attack, value);
            }

            //health
            ClothMeshMap = new Dictionary<ClothElementType, Mesh>();
            var health = config.Health;
            ClothMeshMap = new Dictionary<ClothElementType, Mesh>();
            ClothPrefabMap = new Dictionary<ClothElementType, GameObject>();
            ClothAnimationControllerMap = new Dictionary<ClothElementType, AnimatorOverrideController>();
            HasAllClothPrefabs = false;

            foreach (var serial in EquippedCloth)
            {
                var configIndex = StoredCloth[serial];
                var clothConfig = gameConfig.ClothMap[configIndex];
                var level = ClothLevels[serial];
                var clothModel = new ClothModel(clothConfig, serial, level);
                ClothMeshMap[clothModel.ClothType] = clothModel.Mesh;
                ClothPrefabMap[clothModel.ClothType] = clothModel.Prefab;
                ClothAnimationControllerMap[clothModel.ClothType] = clothModel.AnimationOverride;

                health += (int)clothModel.Armor;
            }

            SetAttribute(UnitAttributeType.Health, health);
            UpdateNominalHealth(health);
        }

        public void Save()
        {
            _data.Save();
        }

        public void SetClothAnimationController(ClothElementType clothType, AnimatorOverrideController animationOverride)
        {
            ClothAnimationControllerMap[clothType] = animationOverride;
        }

        public void RemoveClothAnimationController(ClothElementType clothType)
        {
            ClothAnimationControllerMap.Remove(clothType);
        }

        public AnimatorOverrideController GetCurrentClothAnimationController()
        {
            foreach (var animationOverride in ClothAnimationControllerMap.Values)
            {
                if (animationOverride != null)
                    return animationOverride;
            }

            return null;
        }
    }

    public sealed class PlayerController : UnitController, IDisposable
    {
        public event Action ON_DAMAGE; //used in the DamageBorderHudMediator

        private const string _damageFormat = "-{0}";
        private const float _distance = 1.5f;
        private const float _speed = 15f;

        private readonly PlayerView _view;
        private readonly PlayerModel _model;
        private readonly Timer _timer;
        private readonly GameManager _gameManager;
        private readonly AutoCombatConfig _autoCombatConfig;
        private readonly AttackConfig _attackConfig;

        public PlayerModel Model => _model;
        public new PlayerView View => _view;
        public override TeamIDType TeamID => TeamIDType.Player;

        private readonly StateManager<PlayerState> _stateManager;

        private float _spinDirection = 1f;
        public float SpinDirection => _spinDirection *= -1f;

        private EnemyController _currentTarget;
        private float _nextScanTime;
        private float _nextAttackTime;
        private float _scheduledHitTime = -1f;
        private float _lastCooldownLogTime = -999f;
        private EnemyController _scheduledHitTarget;
        private bool _awaitingAnimationHit;

        public PlayerController(PlayerView view, PlayerModel model, Context context) : base(view)
        {
            _view = view;
            _model = model;
            _timer = context.Get<Timer>();
            _gameManager = context.Get<GameManager>();

            var subContext = new Context(context);
            var injector = new Injector(subContext);

            subContext.Install(this);
            subContext.Install(injector);

            var gameConfig = context.Get<GameConfig>();
            _autoCombatConfig = gameConfig.AutoCombatConfig != null ? gameConfig.AutoCombatConfig : (gameConfig.PlayerConfig != null ? gameConfig.PlayerConfig.autoCombat : null);
            _attackConfig = gameConfig.AttackConfig;

            if (_autoCombatConfig == null)
                Debug.LogWarning("[PlayerCombat] AutoCombatConfig is NULL. Auto combat disabled.");
            if (_attackConfig == null)
                Debug.LogWarning("[PlayerCombat] AttackConfig is NULL. Auto combat disabled.");

            var showLogs = gameConfig.LogEntityMap[EntityType.Player];
            _stateManager = new StateManager<PlayerState>();
            _stateManager.IsLogEnabled = showLogs;

            injector.Inject(_stateManager);

            _view.Model = model;
            _view.InitializeAnimationBinding(_model.GetCurrentClothAnimationController(), false);
            Visibility(true);
            _view.SetCollider(true);

            _view.ON_ATTACK_HIT += OnAnimationAttackHit;
            _timer.TICK += OnTick;
        }

        public void Dispose()
        {
            _timer.TICK -= OnTick;
            _view.ON_ATTACK_HIT -= OnAnimationAttackHit;
            _stateManager.Dispose();
            Visibility(false);
        }

        private void OnTick()
        {
            if (_autoCombatConfig == null || _attackConfig == null || !_autoCombatConfig.enableAutoCombat)
                return;

            var currentTime = _timer.Time;

            if (_scheduledHitTarget != null && currentTime >= _scheduledHitTime)
            {
                LogCombat("Attack fallback hitDelay elapsed. Applying fallback hit.");
                var hitTarget = _scheduledHitTarget;
                _awaitingAnimationHit = false;
                _scheduledHitTarget = null;
                _scheduledHitTime = -1f;
                TryApplyHit(hitTarget);
            }

            if (currentTime >= _nextScanTime)
            {
                RefreshTarget();
                _nextScanTime = currentTime + Mathf.Max(0.05f, _autoCombatConfig.targetScanInterval);
            }

            if (_currentTarget == null)
                return;

            var targetPosition = _currentTarget.View.Position;
            var attackRange = Mathf.Max(0.1f, _attackConfig.attackRange);
            var distance = Vector3.Distance(_view.Position, targetPosition);

            if (distance <= attackRange || _autoCombatConfig.outOfRangeBehaviour == OutOfRangeBehaviour.RotateOnly)
                RotateToTarget(targetPosition);

            if (currentTime < _nextAttackTime)
            {
                if (currentTime - _lastCooldownLogTime >= 0.25f)
                {
                    var remain = _nextAttackTime - currentTime;
                    LogCombat($"Attack skipped - cooldown. remaining={remain:F2}s");
                    _lastCooldownLogTime = currentTime;
                }
                return;
            }

            if (distance > attackRange)
            {
                LogCombat($"Attack skipped - out of range. distance={distance:F2}, range={attackRange:F2}");
                return;
            }

            TryAttack(currentTime);
        }

        private void RefreshTarget()
        {
            EnemyController bestEnemy = null;
            var shortestDistance = float.MaxValue;
            var playerPosition = _view.Position;
            var maxDistance = Mathf.Max(0.1f, _autoCombatConfig.detectionRadius);

            if (_currentTarget != null && (_currentTarget.Model == null || _currentTarget.Model.Health <= 0 || _currentTarget.View == null))
                _currentTarget = null;

            if (_currentTarget != null)
            {
                var chaseDistance = Vector3.Distance(playerPosition, _currentTarget.View.Position);
                if (chaseDistance <= Mathf.Max(maxDistance, _autoCombatConfig.chaseRadius))
                {
                    bestEnemy = _currentTarget;
                    shortestDistance = chaseDistance;
                }
            }

            for (int i = 0; i < _gameManager.Enemies.Count; i++)
            {
                var enemy = _gameManager.Enemies[i];
                if (enemy == null || enemy.Model == null || enemy.View == null)
                    continue;

                if (enemy.Model.Health <= 0)
                    continue;

                var distance = Vector3.Distance(playerPosition, enemy.View.Position);
                if (distance > maxDistance)
                    continue;

                if (bestEnemy != null && distance >= shortestDistance)
                    continue;

                shortestDistance = distance;
                bestEnemy = enemy;
            }

            if (_currentTarget == bestEnemy)
                return;

            if (bestEnemy == null && _currentTarget != null)
                LogCombat($"Target lost: {_currentTarget.View.name}");
            else if (bestEnemy != null)
                LogCombat($"Target acquired: {bestEnemy.View.name}");

            _currentTarget = bestEnemy;
        }

        private void RotateToTarget(Vector3 targetPosition)
        {
            if (_view.RotateNode == null)
            {
                Debug.LogError("[PlayerCombat] RotateNode is NULL. Cannot rotate towards target.");
                return;
            }

            var direction = targetPosition - _view.RotateNode.position;
            direction.y = 0f;
            if (direction.sqrMagnitude < 0.0001f)
                return;

            var targetRotation = Quaternion.LookRotation(direction.normalized);
            _view.RotateNode.rotation = Quaternion.Lerp(
                _view.RotateNode.rotation,
                targetRotation,
                _model.RotateSpeed * Time.deltaTime);
        }

        private void TryAttack(float currentTime)
        {
            if (_currentTarget == null)
            {
                LogCombat("Attack skipped - no target.");
                return;
            }

            var cooldown = Mathf.Max(0.01f, _attackConfig.attackCooldown);
            _nextAttackTime = currentTime + cooldown;

            _view.Attack();
            LogCombat($"Attack() called for target={_currentTarget.View.name}");

            _awaitingAnimationHit = true;
            _scheduledHitTarget = _currentTarget;

            if (_attackConfig.hitDelay > 0f)
            {
                _scheduledHitTime = currentTime + _attackConfig.hitDelay;
                LogCombat($"Attack scheduled in {_attackConfig.hitDelay:F2}s for {_scheduledHitTarget.View.name}");
            }
            else
            {
                LogCombat("Attack fallback hitDelay=0. Applying immediate fallback hit.");
                var target = _scheduledHitTarget;
                _awaitingAnimationHit = false;
                _scheduledHitTarget = null;
                _scheduledHitTime = -1f;
                TryApplyHit(target);
            }
        }

        private void OnAnimationAttackHit()
        {
            if (!_awaitingAnimationHit || _scheduledHitTarget == null)
                return;

            LogCombat("ON_ATTACK_HIT received. Applying hit by animation event.");

            var target = _scheduledHitTarget;
            _awaitingAnimationHit = false;
            _scheduledHitTarget = null;
            _scheduledHitTime = -1f;
            TryApplyHit(target);
        }

        private void TryApplyHit(EnemyController enemy)
        {
            if (enemy == null || enemy.Model == null || enemy.View == null)
            {
                Debug.LogWarning("[PlayerCombat] Hit failed - target reference is NULL.");
                return;
            }

            if (enemy.Model.Health <= 0)
                return;

            var toEnemy = enemy.View.Position - _view.Position;
            toEnemy.y = 0f;

            if (!IsInHitArea(toEnemy))
            {
                LogCombat($"Hit failed - target {enemy.View.name} outside hit area.");
                return;
            }

            var attackValue = _model.GetAttribute(UnitAttributeType.Attack);
            var damage = Mathf.Max(1, Mathf.RoundToInt(attackValue * Mathf.Max(0f, _attackConfig.damageMultiplier)));

            var direction = toEnemy.sqrMagnitude > 0.0001f ? toEnemy.normalized : _view.RotateNode.forward;
            enemy.TryToDamage(damage, direction);
            LogCombat($"Hit success - target={enemy.View.name}, damage={damage}");
        }

        private bool IsInHitArea(Vector3 toEnemy)
        {
            var range = Mathf.Max(0.1f, _attackConfig.attackRange);
            var planarDistance = new Vector2(toEnemy.x, toEnemy.z).magnitude;
            if (planarDistance > range)
                return false;

            var radius = _attackConfig.hitRadius > 0f
                ? _attackConfig.hitRadius
                : Mathf.Max(_attackConfig.hitBox.x, _attackConfig.hitBox.z) * 0.5f;

            if (radius <= 0f)
                radius = range;

            return planarDistance <= radius;
        }

        private void LogCombat(string message)
        {
            if (_autoCombatConfig != null && _autoCombatConfig.enableDebugLogs)
                Debug.Log($"[PlayerCombat] {message}");
        }

        private void Visibility(bool value)
        {
            _view.gameObject.SetActive(value);
        }

        public void Idle()
        {
            _stateManager.SwitchToState(new PlayerIdleState());
        }

        public void IdleMenu()
        {
            _stateManager.SwitchToState(new PlayerIdleMenuState());
        }

        public void Walk()
        {
            _stateManager.SwitchToState(new PlayerWalkState());
        }

        private void Die()
        {
            _stateManager.SwitchToState(new PlayerDieState());
        }

        public void ChangeCloth()
        {
            _stateManager.SwitchToState(new PlayerChangeClothState());
        }

        public void Win()
        {
            _stateManager.SwitchToState(new PlayerWinState());
        }

        public void TryToDamage(int damage, Vector3 direction, GameManager gameManager)
        {
            var type = UnitAttributeType.Health;
            var health = (int) _model.GetAttribute(type);

            damage = Mathf.Min(damage, health);
            if (damage <= 0)
                return;

            health -= damage;
            _model.SetAttribute(type, health);
            _model.SetChanged();

            var blinkDuration = _distance / _speed * 0.5f;
            _view.Damage(blinkDuration);

            var colorType = UINotificationColorType.White;
            if (health <= 0)
            {
                colorType = UINotificationColorType.Red;
                Die();
            }

            var position = _view.AimPosition;
            gameManager.FireSpawnEffect(EffectType.Blood, position, direction);

            var info = string.Format(_damageFormat, damage);
            gameManager.FireSpawnNotificationPopUp(info, position, colorType);

            ON_DAMAGE?.Invoke();
        }
    }
}
