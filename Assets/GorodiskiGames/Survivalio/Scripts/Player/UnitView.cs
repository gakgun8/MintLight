using System.Collections;
using System.Collections.Generic;
using Core;
using Game.Player;
using UnityEngine;

namespace Game.Unit
{
    public enum AnimatorStateType
    {
        Attack,
        Walk,
        Jump,
        Die,
        Idle
    }

    public enum AnimatorParameterType
    {
        Speed
    }

    public abstract class UnitView : BehaviourWithModel<UnitModel>
    {
        private static readonly int Hash_AttackIndex = Animator.StringToHash("AttackIndex");
        private static readonly int Hash_AttackTrigger = Animator.StringToHash("AttackTrigger");

        [SerializeField] private CapsuleCollider _collider;
        [SerializeField] private Animator _animator;
        [SerializeField] private Transform _rotateNode;
        [SerializeField] private Transform _bulletNode;
        [SerializeField] private Transform _aimNode;
        [SerializeField] private Renderer[] _renderers;
        [SerializeField] private float _radius = 0.5f;

        public Transform RotateNode => _rotateNode;
        public Transform BulletNode => _bulletNode;
        public float Radius => _radius;

        private Material[] _materials;
        private RuntimeAnimatorController _defaultRuntimeAnimatorController;
        private AnimatorCullingMode _defaultCullingMode;
        private AnimatorUpdateMode _defaultUpdateMode;
        private float _defaultAnimatorSpeed = 1f;

        private Coroutine _blinkCoroutine;
        private Coroutine _attackMoveCoroutine;
        private static readonly int BlinkAmountShaderProperty = Shader.PropertyToID("_BlinkAmount");
        private static readonly int Hash_Speed = Animator.StringToHash("Speed");
        private static readonly int Hash_IsWalk = Animator.StringToHash("IsWalk");

        private readonly List<int> _attackStateHashes = new List<int>(8);
        private int _currentBaseStateHash;
        private bool _isAttackPlaying;
        private float _attackLockUntilTime;
        private bool _hasSpeedParameter;
        private bool _hasIsWalkParameter;
        private float _moveAnimHoldUntil;
        private float _nextStopDebugLogTime;

        public Vector3 Position
        {
            get => transform.position;
            set => transform.position = value;
        }

        public Vector3 AimPosition
        {
            get => _aimNode.position;
            set => _aimNode.position = value;
        }

        public Quaternion Rotation
        {
            get => _rotateNode.rotation;
            set => _rotateNode.rotation = value;
        }

        protected virtual void Awake()
        {
            if (_animator == null)
                _animator = GetComponentInChildren<Animator>(true);

            if (_collider == null)
                _collider = GetComponent<CapsuleCollider>();

            if (_rotateNode == null)
                _rotateNode = transform;

            if (_aimNode == null)
                _aimNode = _rotateNode;

            if (_bulletNode == null)
                _bulletNode = _rotateNode;

            RefreshRenderersIfNeeded();

            if (_animator != null)
            {
                _defaultRuntimeAnimatorController = _animator.runtimeAnimatorController;
                _defaultCullingMode = _animator.cullingMode;
                _defaultUpdateMode = _animator.updateMode;
                _defaultAnimatorSpeed = _animator.speed;
                _currentBaseStateHash = _animator.GetCurrentAnimatorStateInfo(0).shortNameHash;
                CacheAnimatorParameters();
                CacheAttackHashes();
            }

            CacheMaterials();

            if (_collider != null)
                _collider.radius = _radius;
        }

        private void OnDisable()
        {
            StopBlink();
            SetBlinkAmount(0f);
        }

        public void SetCollider(bool value)
        {
            if (_collider != null)
                _collider.enabled = value;
        }

        public float GetCurrentStateLength => _animator != null ? _animator.GetCurrentAnimatorStateInfo(0).length : 0f;
        public bool IsAttackPlaying => _isAttackPlaying;

        public float CurrentAttackNormalizedTime
        {
            get
            {
                if (_animator == null)
                    return 1f;

                var info = _animator.GetCurrentAnimatorStateInfo(0);
                return IsAttackState(info.shortNameHash) ? info.normalizedTime : 1f;
            }
        }

        // ----------------------------
        // Stable animation control
        // ----------------------------

        public void Idle() => EnsureState(AnimatorStateType.Idle);
        public void Walk() => EnsureState(AnimatorStateType.Walk);
        public void Jump() => EnsureState(AnimatorStateType.Jump);
        public void Die()  => EnsureState(AnimatorStateType.Die);
        public void Attack(float normalizedTime = float.NegativeInfinity) => EnsureState(AnimatorStateType.Attack, normalizedTime);

        public void SetMoveSpeed(float speed)
        {
            if (_animator == null)
                return;

            var clampedSpeed = Mathf.Max(0f, speed);

            if (_hasSpeedParameter)
                _animator.SetFloat(Hash_Speed, clampedSpeed);

            if (_hasIsWalkParameter)
                _animator.SetBool(Hash_IsWalk, clampedSpeed > 0.01f);

            if (clampedSpeed > 0.01f)
            {
                _moveAnimHoldUntil = Time.time + 0.12f; // 120ms 유지
                if (!_hasSpeedParameter && !_hasIsWalkParameter)
                    EnsureState(AnimatorStateType.Walk);
            }
            else
            {
                // 공격 중에는 Idle로 강제 전환하지 않음
                if (!_isAttackPlaying && Time.time > _moveAnimHoldUntil)
                {
                    if (!_hasSpeedParameter && !_hasIsWalkParameter)
                        EnsureState(AnimatorStateType.Idle);
                }
            }
        }

        private void Update()
        {
            if (_animator == null)
                return;

            var info = _animator.GetCurrentAnimatorStateInfo(0);
            _currentBaseStateHash = info.shortNameHash;

            if (Time.time >= _nextStopDebugLogTime)
            {
                _nextStopDebugLogTime = Time.time + 0.25f;
                var speed = _hasSpeedParameter ? _animator.GetFloat(Hash_Speed) : 0f;
                var isWalk = _hasIsWalkParameter && _animator.GetBool(Hash_IsWalk);
                if (speed <= 0.01f && !isWalk)
                    Debug.Log($"[UnitView][StopDebug] {name} Speed={speed:F3} StateHash={info.shortNameHash} IsWalk={(_hasIsWalkParameter ? isWalk.ToString() : "N/A")}");
            }

            if (!_isAttackPlaying)
                return;

            if (IsAttackState(info.shortNameHash))
            {
                if (info.normalizedTime >= 0.98f)
                    _isAttackPlaying = false;
                return;
            }

            if (Time.time >= _attackLockUntilTime)
                _isAttackPlaying = false;
        }

        /// <summary>
        /// IMPORTANT:
        /// PlayerController가 타겟 없을 때 매 프레임 Idle()을 호출할 수 있음.
        /// 그래서 "이미 같은 상태면" Play를 다시 호출하지 않도록 막아야 떨림이 사라짐.
        /// </summary>
        private void EnsureState(AnimatorStateType state, float normalizedTime = float.NegativeInfinity)
        {
            if (_animator == null)
                return;

            if ((state == AnimatorStateType.Walk || state == AnimatorStateType.Idle) && (_hasSpeedParameter || _hasIsWalkParameter))
            {
                _currentBaseStateHash = _animator.GetCurrentAnimatorStateInfo(0).shortNameHash;
                return;
            }

            int hash = Animator.StringToHash(state.ToString());
            var info = _animator.GetCurrentAnimatorStateInfo(0);

            // 이동이 발생하는 순간에는 공격 락 상태라도 Walk 전환을 허용한다.
            // (조이스틱 이동/자동추적 이동 시 즉시 Walk 애니메이션 반영)
            if (_isAttackPlaying && state == AnimatorStateType.Idle)
                return;


            // ✅ 이동 중에는 Idle로 덮어쓰지 못하게 차단 (Walk 멈춤 방지)
            if (state == AnimatorStateType.Idle)
            {
                // Speed 파라미터가 있으면 그 값을 기반으로 이동 여부 판단
                if (_hasSpeedParameter)
                {
                    float s = _animator.GetFloat(Hash_Speed);
                    if (s > 0.01f && Time.time <= _moveAnimHoldUntil) // 이동 중(짧은 홀드 시간 내)
                        return;
                }
                else
                {
                    // Speed 파라미터가 없다면, 현재 상태가 Walk면 Idle로 못 바꾸게 방어
                    var cur = _animator.GetCurrentAnimatorStateInfo(0);
                    int walkHash = Animator.StringToHash(AnimatorStateType.Walk.ToString());
                    if (cur.shortNameHash == walkHash || _currentBaseStateHash == walkHash)
                        return;
                }
            }
            if (state == AnimatorStateType.Walk)
            {
                _isAttackPlaying = false;
                if (_hasIsWalkParameter)
                    _animator.SetBool(Hash_IsWalk, true);
            }
            else if (state == AnimatorStateType.Idle)
            {
                if (_hasSpeedParameter)
                    _animator.SetFloat(Hash_Speed, 0f);

                if (_hasIsWalkParameter)
                    _animator.SetBool(Hash_IsWalk, false);
            }

            bool isSameState = info.shortNameHash == hash || _currentBaseStateHash == hash;
            if (isSameState && float.IsNegativeInfinity(normalizedTime))
                return; // ✅ 같은 상태면 재시작 금지(Idle 떨림 방지)

            if (!TryCrossFadeState(state, normalizedTime))
            {
                if (state == AnimatorStateType.Walk)
                    LogAnimationStateChange("Walk state not found. Speed-only locomotion fallback.");
                else if (state == AnimatorStateType.Idle)
                    LogAnimationStateChange("Idle state not found. Parameter-driven locomotion fallback.");
                return;
            }

            if (ShouldForceImmediateAnimatorUpdate(state))
                _animator.Update(0f);
        }

        protected virtual bool ShouldForceImmediateAnimatorUpdate(AnimatorStateType animationState)
        {
            // 템플릿에서는 즉시 반영이 필요한 케이스가 많아서 true 유지
            return true;
        }

        private bool TryCrossFadeState(AnimatorStateType state, float normalizedTime)
        {
            string[] candidates = BuildStateCandidates(state);
            int targetHash = 0;
            string targetStateName = null;
            var candidateChecks = new List<string>(candidates.Length);

            for (int i = 0; i < candidates.Length; i++)
            {
                var candidate = candidates[i];
                if (string.IsNullOrEmpty(candidate))
                    continue;

                int candidateHash = Animator.StringToHash(candidate);
                bool found = _animator.HasState(0, candidateHash);
                candidateChecks.Add($"{candidate}:{found}");
                if (!found)
                    continue;

                targetHash = candidateHash;
                targetStateName = candidate;
                break;
            }

            if (targetHash == 0)
            {
                Debug.LogWarning($"[UnitView] TryCrossFadeState({state}) failed. Candidate HasState results => {string.Join(", ", candidateChecks)}");
                return false;
            }

            _animator.CrossFadeInFixedTime(targetHash, 0.08f, 0,
                float.IsNegativeInfinity(normalizedTime) ? 0f : normalizedTime);

            _currentBaseStateHash = Animator.StringToHash(GetShortStateName(targetStateName));
            LogAnimationStateChange($"State => {targetStateName} (requested:{state})");
            return true;
        }

        private static string GetShortStateName(string stateName)
        {
            if (string.IsNullOrEmpty(stateName))
                return string.Empty;

            int lastDot = stateName.LastIndexOf('.');
            return lastDot >= 0 && lastDot + 1 < stateName.Length ? stateName.Substring(lastDot + 1) : stateName;
        }

        private string[] BuildStateCandidates(AnimatorStateType state)
        {
            var stateName = state.ToString();
            var candidates = new List<string>(12)
            {
                stateName,
                $"Base Layer.{stateName}",
            };

            if (state == AnimatorStateType.Walk)
            {
                candidates.Add("Move");
                candidates.Add("Run");
                candidates.Add("Locomotion");
                candidates.Add("WalkBlendTree");
                candidates.Add("Locomotion.Walk");
                candidates.Add("Base Layer.Move");
                candidates.Add("Base Layer.Run");
                candidates.Add("Base Layer.Locomotion");
                candidates.Add("Base Layer.WalkBlendTree");
                candidates.Add("Base Layer.Locomotion.Walk");
            }
            else if (state == AnimatorStateType.Idle)
            {
                candidates.Add("Locomotion.Idle");
                candidates.Add("Base Layer.Locomotion.Idle");
            }

            return candidates.ToArray();
        }

        // ----------------------------
        // Code-driven combo attack
        // ----------------------------

        /// <summary>
        /// Animator 전이/조건 없이, 코드가 Attack_01/02/03 상태를 직접 재생.
        /// (서브 스테이트 머신이 없다고 했으니 Base Layer 기준 이름을 우선으로 시도)
        /// </summary>
        public void PlayAttackCombo(int comboIndex)
        {
            if (_animator == null)
                return;

            if (_isAttackPlaying)
            {
                var attackProgress = CurrentAttackNormalizedTime;
                if (attackProgress < 0.9f)
                    return;
            }

            comboIndex = Mathf.Clamp(comboIndex, 1, 3);

            // 혹시 남아있는 트리거/인덱스 전이 제거
            _animator.ResetTrigger(Hash_AttackTrigger);
            _animator.SetInteger(Hash_AttackIndex, 0);

            // Base Layer state names in your screenshot:
            // Attack_01 / Attack_02 / Attack_03
            string s00 = $"Attack_{comboIndex:00}";
            string s01 = $"Attack{comboIndex:00}";
            string s1  = $"Attack{comboIndex}";
            string s_1 = $"Attack_{comboIndex}";

            string[] candidates =
            {
                s00, s01, s1, s_1,
                // just in case Unity stored full paths
                $"Base Layer.{s00}", $"Base Layer.{s01}", $"Base Layer.{s1}", $"Base Layer.{s_1}",
            };

            bool played = TryPlayAnyState(candidates, normalizedTime: 0f);

            // Fallback: parameter-based trigger (if someone renamed states)
            if (!played)
            {
                _animator.SetInteger(Hash_AttackIndex, comboIndex);
                _animator.SetTrigger(Hash_AttackTrigger);
                _animator.Update(0f);
                MarkAttackStarted();
                LogAnimationStateChange($"Attack trigger => combo:{comboIndex}");
                return;
            }

            MarkAttackStarted();
            LogAnimationStateChange($"Attack play => combo:{comboIndex}");
        }

        /// <summary>
        /// AutoCombatConfig의 AttackConfig를 그대로 사용해서 애니를 재생한다.
        /// - cfg.animatorTrigger가 Animator Trigger 파라미터면 Trigger를 쏘고,
        /// - 아니라면 상태 이름(Attack_01 등)으로 Play를 시도한다.
        /// </summary>
        public void PlayAttackByConfig(AttackConfig cfg)
        {
            if (_animator == null || cfg == null)
                return;

            // 1) Trigger parameter first
            if (!string.IsNullOrEmpty(cfg.animatorTrigger))
            {
                for (int i = 0; i < _animator.parameterCount; i++)
                {
                    var p = _animator.GetParameter(i);
                    if (p.type == AnimatorControllerParameterType.Trigger && p.name == cfg.animatorTrigger)
                    {
                        _animator.ResetTrigger(cfg.animatorTrigger);
                        _animator.SetTrigger(cfg.animatorTrigger);
                        _animator.Update(0f);
                        MarkAttackStarted();
                        LogAnimationStateChange($"Attack trigger => {cfg.animatorTrigger}");
                        return;
                    }
                }
            }

            // 2) Treat animatorTrigger as a state name (with fallbacks)
            var candidates = new System.Collections.Generic.List<string>(8);
            if (!string.IsNullOrEmpty(cfg.animatorTrigger))
            {
                candidates.Add(cfg.animatorTrigger);
                candidates.Add($"Base Layer.{cfg.animatorTrigger}");
            }

            if (!string.IsNullOrEmpty(cfg.id))
            {
                string pretty = cfg.id.Replace("attack", "Attack").Replace("ATK", "Attack");
                candidates.Add(pretty);
                candidates.Add($"Base Layer.{pretty}");

                if (cfg.id.Contains("01")) candidates.Add("Attack_01");
                if (cfg.id.Contains("02")) candidates.Add("Attack_02");
                if (cfg.id.Contains("03")) candidates.Add("Attack_03");
            }

            if (!TryPlayAnyState(candidates.ToArray(), normalizedTime: 0f))
            {
                EnsureState(AnimatorStateType.Attack, 0f);
            }

            MarkAttackStarted();
        }

        /// <summary>
        /// 공격 중 이동(대시). AttackConfig.move.enabled에 의해 호출.
        /// </summary>
        public void StartAttackMove(float distance, float duration, AnimationCurve curve, Vector3 worldDirection)
        {
            if (distance <= 0.0001f || duration <= 0.0001f)
                return;

            if (_attackMoveCoroutine != null)
            {
                StopCoroutine(_attackMoveCoroutine);
                _attackMoveCoroutine = null;
            }

            _attackMoveCoroutine = StartCoroutine(AttackMoveCoroutine(distance, duration, curve, worldDirection));
        }

        private IEnumerator AttackMoveCoroutine(float distance, float duration, AnimationCurve curve, Vector3 worldDirection)
        {
            Vector3 dir = worldDirection;
            dir.y = 0f;
            if (dir.sqrMagnitude < 0.0001f)
                dir = transform.forward;
            dir.Normalize();

            Vector3 start = transform.position;

            for (float t = 0f; t < duration; t += Time.deltaTime)
            {
                float u = Mathf.Clamp01(t / duration);
                float k = curve != null && curve.keys != null && curve.length > 0 ? curve.Evaluate(u) : u;
                transform.position = start + dir * (distance * k);
                yield return null;
            }

            transform.position = start + dir * distance;
            _attackMoveCoroutine = null;
        }

        private bool TryPlayAnyState(string[] candidates, float normalizedTime)
        {
            if (_animator == null || candidates == null || candidates.Length == 0)
                return false;

            for (int i = 0; i < candidates.Length; i++)
            {
                var name = candidates[i];
                if (string.IsNullOrEmpty(name))
                    continue;

                int hash = Animator.StringToHash(name);
                if (!_animator.HasState(0, hash))
                    continue;

                _animator.CrossFadeInFixedTime(hash, 0.05f, 0, normalizedTime);
                _animator.Update(0f);
                _currentBaseStateHash = hash;
                LogAnimationStateChange($"State => {name}");
                return true;
            }

            return false;
        }

        private void MarkAttackStarted()
        {
            _isAttackPlaying = true;

            float clipLen = _animator.GetCurrentAnimatorStateInfo(0).length;
            if (clipLen <= 0.01f)
                clipLen = 0.45f;

            float speed = Mathf.Abs(_animator.speed) < 0.0001f ? 1f : _animator.speed;
            _attackLockUntilTime = Time.time + Mathf.Max(0.15f, clipLen / speed);
        }

        private void CacheAnimatorParameters()
        {
            _hasSpeedParameter = false;
            _hasIsWalkParameter = false;

            if (_animator == null)
                return;

            var parameters = _animator.parameters;
            if (parameters == null || parameters.Length == 0)
                return;

            for (int i = 0; i < parameters.Length; i++)
            {
                var parameter = parameters[i];
                if (parameter.type == AnimatorControllerParameterType.Float && parameter.nameHash == Hash_Speed)
                {
                    _hasSpeedParameter = true;
                    continue;
                }

                if (parameter.type == AnimatorControllerParameterType.Bool && parameter.nameHash == Hash_IsWalk)
                    _hasIsWalkParameter = true;

                if (_hasSpeedParameter && _hasIsWalkParameter)
                    break;
            }
        }

        private void CacheAttackHashes()
        {
            _attackStateHashes.Clear();
            string[] names =
            {
                "Attack", "Attack_01", "Attack_02", "Attack_03",
                "Attack01", "Attack02", "Attack03",
                "Attack1", "Attack2", "Attack3"
            };

            for (int i = 0; i < names.Length; i++)
                _attackStateHashes.Add(Animator.StringToHash(names[i]));
        }

        private bool IsAttackState(int stateHash)
        {
            for (int i = 0; i < _attackStateHashes.Count; i++)
            {
                if (_attackStateHashes[i] == stateHash)
                    return true;
            }

            return false;
        }

        private void LogAnimationStateChange(string message)
        {
            Debug.Log($"[UnitView] {name} {message}");
        }

        // ----------------------------
        // Animator controller binding
        // ----------------------------

        public void SetMenuPreviewMode(bool value)
        {
            if (_animator == null)
                return;

            _animator.updateMode = value ? AnimatorUpdateMode.UnscaledTime : _defaultUpdateMode;
            _animator.cullingMode = value ? AnimatorCullingMode.AlwaysAnimate : _defaultCullingMode;
            _animator.speed = _defaultAnimatorSpeed;
        }

        public void InitializeAnimationBinding(AnimatorOverrideController animationOverride, bool menuPreviewMode)
        {
            if (_animator == null)
                return;

            SetMenuPreviewMode(menuPreviewMode);
            ApplyAnimationOverride(animationOverride);
        }

        public void ApplyAnimationOverride(AnimatorOverrideController animationOverride)
        {
            if (_animator == null)
                return;

            if (_defaultRuntimeAnimatorController == null)
                _defaultRuntimeAnimatorController = _animator.runtimeAnimatorController;

            _animator.runtimeAnimatorController = animationOverride == null
                ? _defaultRuntimeAnimatorController
                : animationOverride;

            _animator.Rebind();
            _animator.Update(0f);

            // RuntimeAnimatorController가 바뀌면 파라미터/상태 캐시를 다시 잡아야 한다.
            // (초기 Awake()에서 캐시한 Speed 유무가 오래된 값이면 이동 애니메이션이 갱신되지 않을 수 있음)
            try
            {
                CacheAnimatorParameters();
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"[UnitView] {name} Failed to cache animator parameters: {ex.Message}");
                _hasSpeedParameter = false;
                _hasIsWalkParameter = false;
            }

            CacheAttackHashes();
            _currentBaseStateHash = _animator.GetCurrentAnimatorStateInfo(0).shortNameHash;
        }

        // ----------------------------
        // Blink/Damage
        // ----------------------------

        public void Damage(float blinkDuration)
        {
            if (!CacheMaterials())
                return;

            StopBlink();
            _blinkCoroutine = StartCoroutine(BlinkCoroutine(blinkDuration));
        }

        private IEnumerator BlinkCoroutine(float blinkDuration)
        {
            if (blinkDuration <= 0f)
            {
                SetBlinkAmount(1f);
                yield return null;
                SetBlinkAmount(0f);
                _blinkCoroutine = null;
                yield break;
            }

            float halfDuration = blinkDuration * 0.5f;
            for (float t = 0; t < halfDuration; t += Time.deltaTime)
            {
                float value = Mathf.Clamp01(t / halfDuration);
                SetBlinkAmount(value);
                yield return null;
            }

            SetBlinkAmount(1f);
            yield return null;

            for (float t = 0; t < halfDuration; t += Time.deltaTime)
            {
                float value = Mathf.Clamp01(1f - (t / halfDuration));
                SetBlinkAmount(value);
                yield return null;
            }

            SetBlinkAmount(0f);
            _blinkCoroutine = null;
        }

        private bool CacheMaterials()
        {
            RefreshRenderersIfNeeded();

            if (_renderers == null || _renderers.Length == 0)
            {
                _materials = null;
                return false;
            }

            bool hasMaterial = false;
            _materials = new Material[_renderers.Length];
            for (int i = 0; i < _renderers.Length; i++)
            {
                var renderer = _renderers[i];
                if (renderer == null)
                    continue;

                var material = renderer.material;
                _materials[i] = material;
                hasMaterial |= material != null;
            }

            return hasMaterial;
        }

        private void RefreshRenderersIfNeeded()
        {
            if (_renderers == null || _renderers.Length == 0 || HasMissingRendererReference())
                _renderers = GetComponentsInChildren<Renderer>(true);
        }

        private bool HasMissingRendererReference()
        {
            if (_renderers == null)
                return false;

            for (int i = 0; i < _renderers.Length; i++)
            {
                if (_renderers[i] == null)
                    return true;
            }

            return false;
        }

        private void StopBlink()
        {
            if (_blinkCoroutine == null)
                return;

            StopCoroutine(_blinkCoroutine);
            _blinkCoroutine = null;
        }

        private void SetBlinkAmount(float value)
        {
            if (_materials == null)
                return;

            foreach (var mat in _materials)
            {
                if (mat == null)
                    continue;

                mat.SetFloat(BlinkAmountShaderProperty, value);
            }
        }
       

    }
}
