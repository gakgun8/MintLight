using System.Collections.Generic;
using UnityEngine;

namespace Game.Unit
{
    /// <summary>
    /// Single executor for Animator access.
    /// Keep Animator writes centralized to avoid Idle/Walk/Attack control conflicts.
    /// </summary>
    public sealed class UnitAnimatorDriver : MonoBehaviour
    {
        private static readonly int SpeedHash = Animator.StringToHash("Speed");
        private static readonly int AttackIndexHash = Animator.StringToHash("AttackIndex");
        private static readonly int AttackTriggerHash = Animator.StringToHash("AttackTrigger");
        private static readonly int HitHash = Animator.StringToHash("Hit");
        private static readonly int DieHash = Animator.StringToHash("Die");

        [SerializeField] private Animator _animator;
        [SerializeField] private bool _forceUpdateOnDie = true;

        private readonly List<int> _attackStateHashes = new List<int>(8);

        public bool IsAttackPlaying
        {
            get
            {
                if (_animator == null) return false;
                var info = _animator.GetCurrentAnimatorStateInfo(0);
                return IsAttackState(info.shortNameHash) && info.normalizedTime < 0.98f;
            }
        }

        public float CurrentAttackNormalizedTime
        {
            get
            {
                if (_animator == null) return 1f;
                var info = _animator.GetCurrentAnimatorStateInfo(0);
                return IsAttackState(info.shortNameHash) ? info.normalizedTime : 1f;
            }
        }

        private void Awake()
        {
            if (_animator == null)
                _animator = GetComponentInChildren<Animator>(true);

            CacheAttackHashes();
        }

        public void SetMoveSpeed(float speed)
        {
            if (_animator == null)
                return;

            _animator.SetFloat(SpeedHash, Mathf.Max(0f, speed));
        }

        public void PlayAttack(int attackIndex)
        {
            if (_animator == null)
                return;

            if (IsAttackPlaying && CurrentAttackNormalizedTime < 0.9f)
                return;

            _animator.SetInteger(AttackIndexHash, Mathf.Clamp(attackIndex, 1, 3));
            _animator.ResetTrigger(AttackTriggerHash);
            _animator.SetTrigger(AttackTriggerHash);
        }

        public void PlayHit()
        {
            if (_animator == null)
                return;

            _animator.CrossFadeInFixedTime(HitHash, 0.05f);
        }

        public void PlayDie()
        {
            if (_animator == null)
                return;

            _animator.CrossFadeInFixedTime(DieHash, 0.05f);
            if (_forceUpdateOnDie)
                _animator.Update(0f);
        }

        private void CacheAttackHashes()
        {
            _attackStateHashes.Clear();
            var names = new[] { "Attack", "Attack_01", "Attack_02", "Attack_03", "Attack01", "Attack02", "Attack03", "Attack1", "Attack2", "Attack3" };
            for (var i = 0; i < names.Length; i++)
                _attackStateHashes.Add(Animator.StringToHash(names[i]));
        }

        private bool IsAttackState(int stateHash)
        {
            for (var i = 0; i < _attackStateHashes.Count; i++)
            {
                if (_attackStateHashes[i] == stateHash)
                    return true;
            }

            return false;
        }
    }
}
