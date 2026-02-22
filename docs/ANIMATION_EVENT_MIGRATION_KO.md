# AnimationEvent 중심 애니메이션 구조 가이드

## 왜 구조를 단일화했나
기존에는 `PlayerController`, State, View 등 여러 곳에서 Animator 상태를 직접 전환해서 Idle/Walk/Attack이 서로 덮어쓰는 충돌이 발생했습니다.
이제는 **Animator 접근을 `UnitAnimatorDriver` 한 곳으로 모으고**, **실제 타격 판정은 `AnimationEvent(FireAttackHit)` 시점**에만 실행하도록 분리했습니다.

## 구성 요소
- `UnitAnimatorDriver`
  - Animator 파라미터/트리거 쓰기 전담 (`Speed`, `AttackIndex`, `AttackTrigger`)
- `AnimationEventsRelay`
  - 애니메이션 클립 Event 함수 수신기
  - `FireAttackHit`(필수), `AttackWindupStart`(선택), `AttackRecoverEnd`(선택)
- `IAnimEventReceiver`
  - Relay가 전달하는 공용 인터페이스
- `PlayerCombatController` / `EnemyCombatController`
  - 플레이 로직(타겟/쿨타임/콤보/판정) 전담

## Animator / Clip 세팅
1. Attack 클립(Attack_01/02/03) 히트 프레임에 `FireAttackHit` 이벤트 추가
2. 필요 시 Windup/Recover 프레임에 이벤트 추가
   - `AttackWindupStart`
   - `AttackRecoverEnd`
3. Animator 파라미터 유지
   - `int AttackIndex`
   - `Trigger AttackTrigger`
   - `float Speed`
   - 기본 구현은 `AttackIndex`를 `0/1/2`로 사용합니다. (필요 시 `UnitAnimatorDriver`의 min/max로 조정)
4. Any State -> Attack 전이가 과도하면 재진입 조건을 강화해 연속 덮어쓰기 방지

## 적용 체크
- Locomotion은 `SetMoveSpeed`로만 갱신(Idle/Walk 강제 CrossFade 금지)
- 공격 요청은 CombatController 한 곳에서만 `PlayAttack` 호출
- 데미지는 반드시 `FireAttackHit` 이벤트에서만 적용


## 컴포넌트 추가 안내
- `PlayerCombatController` / `EnemyCombatController`는 `MonoBehaviour`이므로 필요하면 인스펙터에서 직접 붙일 수 있습니다.
- 현재 코드는 누락 시 런타임에 자동으로 `AddComponent` 하도록 되어 있어 프리팹에 미리 안 붙어 있어도 동작합니다.
