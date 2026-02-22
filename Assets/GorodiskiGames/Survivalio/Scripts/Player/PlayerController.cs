using System;
using System.Collections.Generic;
using System.Linq;
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
        private readonly AttackConfig[] _comboConfigs;

        public PlayerModel Model => _model;
        public new PlayerView View => _view;
        public override TeamIDType TeamID => TeamIDType.Player;

        private readonly StateManager<PlayerState> _stateManager;

        private float _spinDirection = 1f;
        public float SpinDirection => _spinDirection *= -1f;

        private EnemyController _currentTarget;
        private float _nextScanTime;
        private float _lastAttackRequestTime = -999f;
        private float _lastCooldownLogTime = -999f;
        private float _lastAttackTime = -999f;
        private bool _hadTargetLastTick;
        private bool _isAutoMoving;
        private bool _comboResetByMovement;
        private PlayerCombatController _combatController;
        private const float AutoMoveResumeDelay = 0.15f;
        private const float MovementDebugLogInterval = 0.35f;
        private const float ManualInputDeadzone = 0.1f;
        private float _manualInputMagnitude;
        private bool _hasManualInput;
        private float _lastManualInputTime = -999f;
        private float _nextMovementDebugLogTime;
        private Vector3 _lastTickPosition;
        private float _nextIdleEnsureTime;
        private const float IdleEnsureInterval = 0.08f;

        public bool HasManualInput => _hasManualInput;
        public float LastAttackRequestTime => _lastAttackRequestTime;

        public PlayerController(PlayerView view, PlayerModel model, Context context) : base(view)
        {
            _view = view;
            _model = model;
            _timer = context.Get<Timer>();

            if (!context.TryGet(out _gameManager))
                Debug.LogWarning("[PlayerController] GameManager was not found in context. Player combat target scan is disabled for this mode.");

            var subContext = new Context(context);
            var injector = new Injector(subContext);

            subContext.Install(this);
            subContext.Install(injector);

            var gameConfig = context.Get<GameConfig>();
            _autoCombatConfig = gameConfig.AutoCombatConfig != null ? gameConfig.AutoCombatConfig : (gameConfig.PlayerConfig != null ? gameConfig.PlayerConfig.autoCombat : null);
            _attackConfig = gameConfig.AttackConfig;
            _comboConfigs = _autoCombatConfig != null ? _autoCombatConfig.combo : null;

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

                        _combatController = _view.GetComponent<PlayerCombatController>();
            if (_combatController == null)
                _combatController = _view.gameObject.AddComponent<PlayerCombatController>();

            _combatController.Initialize(_view, _model, _autoCombatConfig, _attackConfig);

            var relay = _view.GetComponentInChildren<AnimationEventsRelay>(true);
            if (relay != null) relay.RegisterReceiver(_combatController);
            _timer.TICK += OnTick;

            _lastTickPosition = _view.Position;
        }

        public void Dispose()
        {
            _timer.TICK -= OnTick;
            var relay = _view.GetComponentInChildren<AnimationEventsRelay>(true);
            if (relay != null) relay.UnregisterReceiver(_combatController);
            _stateManager.Dispose();
            Visibility(false);
        }

        private void OnTick()
        {
            if (_autoCombatConfig == null || _attackConfig == null || !_autoCombatConfig.enableAutoCombat)
                return;

            var currentTime = _timer.Time;
            var currentPosition = _view.Position;
            var deltaPos = currentPosition - _lastTickPosition;
            var deltaTime = Mathf.Max(0.0001f, Time.deltaTime);
            var speed = deltaPos.magnitude / deltaTime;
            _lastTickPosition = currentPosition;

            var hasManualInput = _hasManualInput;
            var hasTarget = _currentTarget != null;
            var didRotateToTarget = false;
            var autoCombatRunning = false;

            if (hasManualInput)
            {
                StopAutoMovement();
                LogMovementState(hasManualInput, hasTarget, autoCombatRunning, didRotateToTarget, deltaPos, speed);
                return;
            }

            if (currentTime >= _nextScanTime)
            {
                RefreshTarget();
                _nextScanTime = currentTime + Mathf.Max(0.05f, _autoCombatConfig.targetScanInterval);
            }

            hasTarget = _currentTarget != null;
            var canResumeAutoMove = !hasManualInput && (currentTime - _lastManualInputTime) >= AutoMoveResumeDelay;
            var autoMoveEligible = hasTarget && !_view.IsAttackPlaying;
            var allowAutoMove = autoMoveEligible && canResumeAutoMove;
            autoCombatRunning = hasTarget;

            if (!hasTarget)
            {
                StopAutoMovement();
                ResetComboChain();

                if (!hasManualInput)
                {
                    _view.SetMoveSpeed(0f);
                    EnsureIdleWhenStopped();
                }

                _hadTargetLastTick = false;
                LogMovementState(hasManualInput, hasTarget, autoCombatRunning, didRotateToTarget, deltaPos, speed);
                return;
            }

            _hadTargetLastTick = true;

            var targetPosition = _currentTarget.View.Position;
            var attackRange = Mathf.Max(0.1f, _attackConfig.attackRange);
            var stopDistance = Mathf.Max(0.1f, _autoCombatConfig.stopDistance);
            var desiredRange = Mathf.Max(stopDistance, attackRange * 0.95f);
            var distance = Vector3.Distance(_view.Position, targetPosition);
            RotateToTarget(targetPosition);
            didRotateToTarget = true;

            if (!allowAutoMove)
            {
                StopAutoMovement();
                LogMovementState(hasManualInput, hasTarget, autoCombatRunning, didRotateToTarget, deltaPos, speed);
                return;
            }

            // 공격 가능 거리 안에 들어오면 자동 이동 없이 즉시 공격 로직으로 진입한다.
            if (distance > attackRange)
            {
                HandleApproach(targetPosition, desiredRange);
                LogMovementState(hasManualInput, hasTarget, autoCombatRunning, didRotateToTarget, deltaPos, speed);
                return;
            }

            _comboResetByMovement = false;
            StopAutoMovement();

            // IMPORTANT:
            // If target is already inside attack range, prioritize chaining attack immediately.
            // Forcing one more Walk tick here causes Attack_01 re-entry patterns and combo starvation.
            _view.SetMoveSpeed(0f);
            TryAttack(currentTime, 0f);
            LogMovementState(hasManualInput, hasTarget, autoCombatRunning, didRotateToTarget, deltaPos, speed);
        }

        private void RefreshTarget()
        {
            if (_gameManager == null)
                return;

            EnemyController bestEnemy = null;
            var shortestDistance = float.MaxValue;
            var playerPosition = _view.Position;
            var maxDistance = Mathf.Max(0.1f, _autoCombatConfig.detectionRadius);

            if (_currentTarget != null && (_currentTarget.Model == null || _currentTarget.Model.Health <= 0 || _currentTarget.View == null))
                _currentTarget = null;

            // Keep the locked target until it dies.
            // This prevents combo/animation drops when a single target briefly leaves detection range.
            if (_currentTarget != null)
                return;

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
            {
                LogCombat($"Target lost: {_currentTarget.View.name}");
                ResetComboChain();
            }
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

        private void HandleApproach(Vector3 targetPosition, float desiredRange)
        {
            if (_autoCombatConfig.outOfRangeBehaviour == OutOfRangeBehaviour.RotateOnly)
            {
                StopAutoMovement();
                EnsureIdleWhenStopped();
                return;
            }

            if (_view.IsAttackPlaying)
                return;

            var toTarget = targetPosition - _view.Position;
            toTarget.y = 0f;
            var sqrDistance = toTarget.sqrMagnitude;
            if (sqrDistance <= desiredRange * desiredRange)
            {
                StopAutoMovement();
                EnsureIdleWhenStopped();
                return;
            }

            var direction = toTarget.normalized;
            var moveSpeed = Mathf.Max(0.1f, _model.WalkSpeed * Mathf.Max(0.1f, _autoCombatConfig.approachSpeedMultiplier));
            var beforePosition = _view.Position;
            _view.Position += direction * moveSpeed * Time.deltaTime;
            var movedDistance = Vector3.Distance(beforePosition, _view.Position);
            var displacementSpeed = movedDistance / Mathf.Max(0.0001f, _model.WalkSpeed * Time.deltaTime);
            var intentSpeed = Mathf.Clamp01(moveSpeed / Mathf.Max(0.0001f, _model.WalkSpeed));
            var normalizedSpeed = Mathf.Max(intentSpeed, displacementSpeed);

            _view.SetMoveSpeed(normalizedSpeed);
            _view.Walk();

            if (!_isAutoMoving)
            {
                _isAutoMoving = true;
                LogCombat("Auto approach started.");
            }
        }

        private void StopAutoMovement()
        {
            if (!_isAutoMoving)
                return;

            _isAutoMoving = false;
            _view.SetMoveSpeed(0f);
            EnsureIdleWhenStopped();

            LogCombat("Auto approach stopped.");
        }

        private void EnsureIdleWhenStopped()
        {
            if (_timer.Time < _nextIdleEnsureTime)
                return;

            _nextIdleEnsureTime = _timer.Time + IdleEnsureInterval;

            if (!_view.IsAttackPlaying || _view.CurrentAttackNormalizedTime >= 0.98f)
                _view.Idle();
        }

        public void ReportManualInput(Vector2 inputDirection)
        {
            _manualInputMagnitude = inputDirection.magnitude;
            _hasManualInput = _manualInputMagnitude > ManualInputDeadzone;

            if (_hasManualInput)
                _lastManualInputTime = _timer.Time;
        }

        private void LogMovementState(bool hasManualInput, bool hasTarget, bool autoCombatRunning, bool didRotateToTarget, Vector3 deltaPos, float speed)
        {
            if (_timer.Time < _nextMovementDebugLogTime)
                return;

            _nextMovementDebugLogTime = _timer.Time + MovementDebugLogInterval;
            Debug.Log($"[PlayerMove] hasManualInput={hasManualInput} inputMag={_manualInputMagnitude:F3} hasTarget={hasTarget} " +
                      $"autoCombatRunning={autoCombatRunning} didRotateToTarget={didRotateToTarget} deltaPos={deltaPos} speed={speed:F3}");

            if (hasManualInput && didRotateToTarget)
                Debug.LogWarning("[PlayerMove] hasManualInput=true 인데 RotateToTarget가 호출되었습니다. 자동 회전 경합을 확인하세요.");
        }

        private void TryAttack(float currentTime, float currentSpeed)
        {
            if (_currentTarget == null)
            {
                LogCombat("Attack skipped - no target.");
                return;
            }

            
            // ✅ Never request attacks while moving (prevents Animator transition churn: Walk <-> Attack).
            if (currentSpeed > 0.05f || _isAutoMoving || _hasManualInput)
            {
                LogCombat($"Attack skipped - moving. speed={currentSpeed:F2} autoMove={_isAutoMoving} manual={_hasManualInput}");
                return;
            }
if (_view.IsAttackPlaying && _view.CurrentAttackNormalizedTime < 0.9f)
            {
                LogCombat($"Attack skipped - attack locked. progress={_view.CurrentAttackNormalizedTime:F2}");
                return;
            }

            if (!_combatController.CanRequestAttack(currentTime))
                return;

            _lastAttackRequestTime = currentTime;
            _view.LogAttackTriggerRequest("PlayerController.TryAttack");
            _combatController.RequestAttack(_currentTarget, _gameManager, currentTime);
        }

        private void ResetComboChain()
        {
            _lastAttackTime = -999f;
            _combatController.ResetCombo();
        }

        private void LogCombat(string message)
        {
            if (_autoCombatConfig != null && _autoCombatConfig.enableDebugLogs)
                Debug.Log($"[PlayerCombat] {message}");
        }

        /// <summary>
        /// EnemyView에는 Controller 프로퍼티가 없다.
        /// 콜라이더/뷰로부터 EnemyController가 필요하면 GameManager.Enemies에서 View 매칭으로 역추적한다.
        /// </summary>
        private Game.Enemy.EnemyController ResolveEnemyController(Game.Enemy.EnemyView view)
        {
            if (view == null || _gameManager == null || _gameManager.Enemies == null)
                return null;

            for (int i = 0; i < _gameManager.Enemies.Count; i++)
            {
                var e = _gameManager.Enemies[i];
                if (e == null)
                    continue;

                if (e.View == view)
                    return e;
            }

            return null;
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
