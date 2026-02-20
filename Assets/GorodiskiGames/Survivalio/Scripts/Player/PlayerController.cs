using System;
using System.Collections.Generic;
using Core;
using Game.Cloth;
using Game.Config;
using Game.Core;
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
        private AutoCombatConfig autoCombatConfig;   // ✅ 런타임 참조로 변경
        private AttackConfig currentAttack;
        private Transform currentTarget;

        public event Action ON_DAMAGE;


        private const string _damageFormat = "-{0}";
        private const float _distance = 1.5f;
        private const float _speed = 15f;

        private readonly PlayerView _view;
        private readonly PlayerModel _model;

        public PlayerModel Model => _model;
        public new PlayerView View => _view;
        public override TeamIDType TeamID => TeamIDType.Player;

        private readonly StateManager<PlayerState> _stateManager;

        private float _spinDirection = 1f;
        public float SpinDirection => _spinDirection *= -1f;

        public PlayerController(PlayerView view, PlayerModel model, Context context) : base(view)
        {
            _view = view;
            _model = model;

            // ✅ (추가) Inspector에서 PlayerView에 연결한 AutoCombatConfig를 여기서 받아옴
            autoCombatConfig = _view.AutoCombatConfig;

            // ✅ (추가) 애니 이벤트(=PlayerView.FireAttackXXHit) 수신
            _view.ON_ATTACK_HIT += OnAttackHit;

            var subContext = new Context(context);
            var injector = new Injector(subContext);

            subContext.Install(this);
            subContext.Install(injector);

            var gameConfig = context.Get<GameConfig>();
            var showLogs = gameConfig.LogEntityMap[EntityType.Player];
            _stateManager = new StateManager<PlayerState>();
            _stateManager.IsLogEnabled = showLogs;

            injector.Inject(_stateManager);

            _view.Model = model;
            _view.InitializeAnimationBinding(_model.GetCurrentClothAnimationController(), false);
            Visibility(true);
            _view.SetCollider(true);
        }

        public void Dispose()
        {
            // ✅ (추가) 구독 해제
            if (_view != null) _view.ON_ATTACK_HIT -= OnAttackHit;

            _stateManager.Dispose();
            Visibility(false);
        }

        private void OnAttackHit(int comboIndex)
        {
            if (autoCombatConfig == null || autoCombatConfig.combo == null) return;
            if (comboIndex < 0 || comboIndex >= autoCombatConfig.combo.Length) return;

            currentAttack = autoCombatConfig.combo[comboIndex];
            if (currentAttack == null) return;

            ExecuteAttack(currentAttack);
        }

        private void ExecuteAttack(AttackConfig attack)
        {
            // 공격 방향은 RotateNode 기준이 자연스러움
            Vector3 forward = _view.RotateNode.forward;

            // originOffset을 “로컬 오프셋”으로 보고, RotateNode 기준으로 월드 변환
            Vector3 origin = _view.Position + _view.RotateNode.TransformVector(attack.originOffset);

            // (임시) Sector만 먼저 처리 (Circle/LineBox는 다음 단계에서 확장)
            float radius = attack.sector.radius;
            float halfAngle = attack.sector.angle * 0.5f;

            Collider[] hits = Physics.OverlapSphere(origin, radius, attack.targetMask);

            int count = 0;
            foreach (var hit in hits)
            {
                // 타겟 방향각 필터
                Vector3 to = (hit.transform.position - _view.Position);
                to.y = 0f;
                if (to.sqrMagnitude < 0.0001f) continue;

                float ang = Vector3.Angle(forward, to.normalized);
                if (ang > halfAngle) continue;

                // ✅ 여기서 데미지 적용(몹 스크립트에 맞춰 연결)
                // hit.GetComponent<EnemyController>()?.TryToDamage(...);

                // ✅ 넉백 (attack_03 등)
                if (attack.knockback.enabled)
                {
                    Vector3 dir = to.normalized;
                    hit.transform.position += dir * attack.knockback.distance;
                }

                count++;
                if (count >= attack.maxTargets) break;
            }
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
