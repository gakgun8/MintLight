using Game.Config;
using Game.Player;
using Game.Unit;
using UnityEngine;

namespace Game.Enemy
{
    public sealed class EnemyView : UnitView
    {
        [SerializeField] private EnemyConfig _config;
        [SerializeField] private ParticleSystem _effect;

        public EnemyConfig Config => _config;

        protected override void OnModelChanged(UnitModel model)
        {

        }

        protected override bool ShouldForceImmediateAnimatorUpdate(AnimatorStateType animationState)
        {
            // Enemy spawn can happen in bursts, so avoid forcing synchronous animator evaluation
            // on every Walk/Idle transition. Keep immediate update for Die to preserve
            // GetCurrentStateLength usage in EnemyDieState.
            return animationState == AnimatorStateType.Die;
        }

        public void PlayEffect(bool isPlay)
        {
            if(_effect == null)
                return;

            if(isPlay)
                _effect.Play();
            else
                _effect.Stop();
        }
    }
}
