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
        private float _nextAttackTime;
        private float _lastCooldownLogTime = -999f;
        private int _comboIndex;
        private float _lastAttackTime = -999f;
        private UnitController _pendingHitTarget;
        private GameManager _pendingHitGameManager;
        private bool _hadTargetLastTick;
        private bool _isAutoMoving;
        private bool _comboResetByMovement;
        private AttackConfig _currentAttackConfig;
        private readonly HashSet<EnemyController> _hitEnemiesInCurrentAttack = new HashSet<EnemyController>();
        private const float AutoMoveResumeDelay = 0.15f;
        private const float ManualInputGraceDuration = 0.10f;
        private const float MovementDebugLogInterval = 0.35f;
        private const float ManualInputPressDeadzone = 0.12f;
        private const float ManualInputReleaseDeadzone = 0.08f;
        private float _manualInputMagnitude;
        private bool _hasManualInput;
        private float _lastManualInputTime = -999f;
        private float _lastAttackRequestTime = -999f;
        private float _nextMovementDebugLogTime;
        private Vector3 _lastTickPosition;

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

            _view.ON_ATTACK_HIT += OnAttackHit;
            _view.ON_ATTACK_DASH += OnAttackDash;
            _timer.TICK += OnTick;

            _lastTickPosition = _view.Position;
        }

        public void Dispose()
        {
            _timer.TICK -= OnTick;
            _view.ON_ATTACK_HIT -= OnAttackHit;
            _view.ON_ATTACK_DASH -= OnAttackDash;
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

            var isManualNow = _hasManualInput;
            var manualLock = (currentTime - _lastManualInputTime) < ManualInputGraceDuration;
            var hasManualInput = isManualNow || manualLock;
            var hasTarget = _currentTarget != null;
            var didRotateToTarget = false;
            var autoCombatRunning = false;

            if (isManualNow)
            {
                StopAutoMovement(keepManualAnim: true);
                LogMovementState(hasManualInput, hasTarget, autoCombatRunning, didRotateToTarget, deltaPos, speed);
                return;
            }

            if (currentTime >= _nextScanTime)
            {
                RefreshTarget();
                _nextScanTime = currentTime + Mathf.Max(0.05f, _autoCombatConfig.targetScanInterval);
            }

            hasTarget = _currentTarget != null;
            var canResumeAutoMove = !manualLock && (currentTime - _lastManualInputTime) >= AutoMoveResumeDelay;
            var autoMoveEligible = hasTarget && !_view.IsAttackPlaying;
            var allowAutoMove = autoMoveEligible && canResumeAutoMove;
            autoCombatRunning = hasTarget;

            if (!hasTarget)
            {
                StopAutoMovement();
                ResetComboChain();

                if (!hasManualInput)
                {
                    if (!_view.IsAttackPlaying || _view.CurrentAttackNormalizedTime >= 0.98f)
                        _view.Idle();

                    _view.SetMoveSpeed(0f);
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
            if (!manualLock)
            {
                RotateToTarget(targetPosition);
                didRotateToTarget = true;
            }

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

            if (currentTime < _nextAttackTime)
            {
                if (currentTime - _lastCooldownLogTime >= 0.25f)
                {
                    var remain = _nextAttackTime - currentTime;
                    LogCombat($"Attack skipped - cooldown. remaining={remain:F2}s");
                    _lastCooldownLogTime = currentTime;
                }

                LogMovementState(hasManualInput, hasTarget, autoCombatRunning, didRotateToTarget, deltaPos, speed);
                return;
            }

            TryAttack(currentTime);
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
                if (!_view.IsAttackPlaying)
                    _view.Idle();
                StopAutoMovement();
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
                return;
            }

            var direction = toTarget.normalized;
            var moveSpeed = Mathf.Max(0.1f, _model.WalkSpeed * Mathf.Max(0.1f, _autoCombatConfig.approachSpeedMultiplier));
            var beforePosition = _view.Position;
            _view.Position += direction * moveSpeed * Time.deltaTime;
            var movedDistance = Vector3.Distance(beforePosition, _view.Position);
            var normalizedSpeed = movedDistance / Mathf.Max(0.0001f, _model.WalkSpeed * Time.deltaTime);

            _view.SetMoveSpeed(normalizedSpeed);
            _view.Walk();
            if (!_comboResetByMovement)
            {
                ResetComboChain();
                _comboResetByMovement = true;
            }

            if (!_isAutoMoving)
            {
                _isAutoMoving = true;
                LogCombat("Auto approach started.");
            }
        }

        private void StopAutoMovement(bool keepManualAnim = false)
        {
            if (!_isAutoMoving)
                return;

            _isAutoMoving = false;

            if (!keepManualAnim)
            {
                _view.SetMoveSpeed(0f);
                if (!_view.IsAttackPlaying)
                    _view.Idle();
            }

            LogCombat("Auto approach stopped.");
        }

        public void ReportManualInput(Vector2 inputDirection)
        {
            _manualInputMagnitude = inputDirection.magnitude;

            // Deadzone jitter로 인한 manual 상태 플리커/고착을 줄이기 위해 히스테리시스를 사용한다.
            var pressThreshold = ManualInputPressDeadzone;
            var releaseThreshold = Mathf.Min(pressThreshold, ManualInputReleaseDeadzone);

            if (_hasManualInput)
                _hasManualInput = _manualInputMagnitude > releaseThreshold;
            else
                _hasManualInput = _manualInputMagnitude > pressThreshold;

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

        private void TryAttack(float currentTime)
        {
            if (_currentTarget == null)
            {
                LogCombat("Attack skipped - no target.");
                return;
            }

            if (_view.IsAttackPlaying && _view.CurrentAttackNormalizedTime < 0.9f)
            {
                LogCombat($"Attack skipped - attack locked. progress={_view.CurrentAttackNormalizedTime:F2}");
                return;
            }

            var nextComboIndex = (_comboIndex % 3) + 1;
            var nextConfig = GetAttackConfigForCombo(nextComboIndex);
            var cooldownSource = nextConfig != null ? nextConfig : _attackConfig;
            var cooldown = cooldownSource != null ? Mathf.Max(0.01f, cooldownSource.attackCooldown) : 0.5f;
            _lastAttackRequestTime = currentTime;
            _nextAttackTime = currentTime + cooldown;

            StartAttackCombo(_currentTarget, _gameManager);
        }

        private void ResetComboChain()
        {
            _comboIndex = 0;
            _lastAttackTime = -999f;
            _currentAttackConfig = null;
            _hitEnemiesInCurrentAttack.Clear();
            _pendingHitTarget = null;
            _pendingHitGameManager = null;
        }

        public void StartAttackCombo(UnitController target, GameManager gameManager)
        {
            if (target == null)
                return;

            _pendingHitTarget = target;
            _pendingHitGameManager = gameManager;

            if (_comboIndex < 0 || _comboIndex > 3)
                _comboIndex = 0;

            _comboIndex = (_comboIndex % 3) + 1;
            _lastAttackTime = Time.time;

            _currentAttackConfig = GetAttackConfigForCombo(_comboIndex);
            _hitEnemiesInCurrentAttack.Clear();

            if (_currentAttackConfig != null)
                _view.PlayAttackByConfig(_currentAttackConfig);
            else
                _view.PlayAttackCombo(_comboIndex);

            var targetName = target.View != null ? target.View.name : "NULL";
            var attackId = _currentAttackConfig != null ? _currentAttackConfig.id : $"combo_{_comboIndex}";
            Debug.Log($"[PlayerCombat] StartAttackCombo combo={_comboIndex}, attack={attackId}, target={targetName}");
        }

        private void OnAttackHit()
        {
            Debug.Log("[PlayerCombat] OnAttackHit received.");

            var attackConfig = _currentAttackConfig != null ? _currentAttackConfig : _attackConfig;
            var targets = ResolveAttackTargets(attackConfig);

            // ShapeType 판정을 통과한 대상이 없으면 공격은 빗나가야 한다.
            // (fallback 강제 타격은 hit shape 설정을 무시하게 만들 수 있음)
            if (attackConfig == null && targets.Count == 0 && _pendingHitTarget is EnemyController pendingEnemy)
                targets.Add(pendingEnemy);

            if (targets.Count == 0)
            {
                _pendingHitTarget = null;
                _pendingHitGameManager = null;
                return;
            }

            var attackValue = _model.GetAttribute(UnitAttributeType.Attack);
            var multiplier = attackConfig != null ? Mathf.Max(0.01f, attackConfig.damageMultiplier) : 1f;
            var damage = Mathf.Max(1, Mathf.RoundToInt(attackValue * multiplier));

            for (int i = 0; i < targets.Count; i++)
            {
                var enemy = targets[i];
                if (enemy == null || enemy.View == null || enemy.Model == null || enemy.Model.Health <= 0)
                    continue;

                var direction = enemy.View.Position - _view.Position;
                direction.y = 0f;
                direction = direction.sqrMagnitude > 0.0001f
                    ? direction.normalized
                    : (_view.RotateNode != null ? _view.RotateNode.forward : _view.transform.forward);
                enemy.TryToDamage(damage, direction);
                _hitEnemiesInCurrentAttack.Add(enemy);
                Debug.Log($"[PlayerCombat] Damage applied. target={enemy.View.name}, damage={damage}, attack={(attackConfig != null ? attackConfig.id : "default")}");
            }

            _pendingHitTarget = null;
            _pendingHitGameManager = null;
        }

        private void OnAttackDash()
        {
            if (_currentAttackConfig == null || !_currentAttackConfig.move.enabled)
                return;

            var dir = _view.RotateNode != null ? _view.RotateNode.forward : _view.transform.forward;
            _view.StartAttackMove(_currentAttackConfig.move.distance, _currentAttackConfig.move.duration, _currentAttackConfig.move.curve, dir);
        }

        private AttackConfig GetAttackConfigForCombo(int comboIndex)
        {
            if (_comboConfigs != null && _comboConfigs.Length > 0)
            {
                var idx = comboIndex - 1;
                if (idx >= 0 && idx < _comboConfigs.Length && _comboConfigs[idx] != null)
                    return _comboConfigs[idx];

                return null;
            }

            return null;
        }

        private List<EnemyController> ResolveAttackTargets(AttackConfig attackConfig)
        {
            var result = new List<EnemyController>();
            if (_gameManager == null || _gameManager.Enemies == null)
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

            for (int i = 0; i < _gameManager.Enemies.Count; i++)
            {
                var enemy = _gameManager.Enemies[i];
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
                {
                    var radius = cfg.circle.radius > 0.01f ? cfg.circle.radius : Mathf.Max(cfg.hitRadius, cfg.attackRange);
                    return delta.sqrMagnitude <= radius * radius;
                }
                case AttackShapeType.LineBox:
                {
                    var localForward = Vector3.Dot(delta, forward);
                    var localRight = Mathf.Abs(Vector3.Dot(delta, right));
                    var length = cfg.lineBox.length > 0.01f ? cfg.lineBox.length : cfg.attackRange;
                    var halfWidth = Mathf.Max(0.05f, cfg.lineBox.width * 0.5f);
                    return localForward >= 0f && localForward <= length && localRight <= halfWidth;
                }
                case AttackShapeType.Sector:
                default:
                {
                    var radius = cfg.sector.radius > 0.01f ? cfg.sector.radius : cfg.attackRange;
                    if (delta.sqrMagnitude > radius * radius)
                        return false;

                    var angle = cfg.sector.angle > 0.01f ? cfg.sector.angle : 90f;
                    var half = angle * 0.5f;
                    var targetDir = delta.normalized;
                    var toAngle = Vector3.Angle(forward, targetDir);
                    return toAngle <= half;
                }
            }
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
            Debug.Log($"[ANIM] Idle() called  frame={Time.frameCount}");
            _stateManager.SwitchToState(new PlayerIdleState());
        }

        public void IdleMenu()
        {
            _stateManager.SwitchToState(new PlayerIdleMenuState());
        }

        public void Walk()
        {
            Debug.Log($"[ANIM] Walk() called  frame={Time.frameCount}");
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
