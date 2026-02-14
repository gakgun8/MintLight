using System.Collections;
using Core;
using Game.Player;
using UnityEngine;

namespace Game.Unit
{
    public enum AnimatorStateType
    {
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
        private static readonly int BlinkAmountShaderProperty = Shader.PropertyToID("_BlinkAmount");

        public Vector3 Position
        {
            get { return transform.position; }
            set { transform.position = value; }
        }

        public Vector3 AimPosition
        {
            get { return _aimNode.position; }
            set { _aimNode.position = value; }
        }

        public Quaternion Rotation
        {
            get { return _rotateNode.rotation; }
            set { _rotateNode.rotation = value; }
        }

        private void Awake()
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

            if (_renderers == null || _renderers.Length == 0)
                _renderers = GetComponentsInChildren<Renderer>(true);

            if (_animator != null)
                _defaultRuntimeAnimatorController = _animator.runtimeAnimatorController;

            _materials = new Material[_renderers?.Length ?? 0];
            for (int i = 0; i < _materials.Length; i++)
            {
                _materials[i] = _renderers[i].material;
            }

            if (_collider != null)
                _collider.radius = _radius;
        }

        public void SetCollider(bool value)
        {
            if (_collider != null)
                _collider.enabled = value;
        }

        public float GetCurrentStateLength => _animator != null ? _animator.GetCurrentAnimatorStateInfo(0).length : 0f;

        public void Idle()
        {
            PlayAnimation(AnimatorStateType.Idle, Random.Range(0, 1f));
        }

        public void Walk()
        {
            PlayAnimation(AnimatorStateType.Walk, float.NegativeInfinity);
        }

        public void Jump()
        {
            PlayAnimation(AnimatorStateType.Jump, float.NegativeInfinity);
        }

        public void Die()
        {
            PlayAnimation(AnimatorStateType.Die, float.NegativeInfinity);
        }

        private void PlayAnimation(AnimatorStateType animationState, float timeValue)
        {
            if (_animator == null)
                return;

            var nameHash = Animator.StringToHash(animationState.ToString());
            _animator.PlayInFixedTime(nameHash, 0, timeValue);

            _animator.Update(0);
        }


        public void SetMenuPreviewMode(bool value)
        {
            if (_animator == null)
                return;

            _animator.updateMode = value ? AnimatorUpdateMode.UnscaledTime : AnimatorUpdateMode.Normal;
        }

        public void ApplyAnimationOverride(AnimatorOverrideController animationOverride)
        {
            if (_animator == null)
                return;

            _animator.runtimeAnimatorController = animationOverride == null
                ? _defaultRuntimeAnimatorController
                : animationOverride;

            _animator.Update(0f);
        }

        public void Damage(float blinkDuration)
        {
            if (_materials == null || _materials.Length == 0)
                return;

            StartCoroutine(BlinkCoroutine(blinkDuration));
        }

        private IEnumerator BlinkCoroutine(float blinkDuration)
        {
            if (blinkDuration <= 0f)
            {
                SetBlinkAmount(1f);
                SetBlinkAmount(0f);
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
