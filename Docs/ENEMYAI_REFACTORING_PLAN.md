# EnemyAI 리팩토링 계획

## 개요

| 항목 | 내용 |
|:---|:---|
| **목표** | EnemyAI를 SOLID 원칙에 맞게 리팩토링 (SRP, DIP 준수) |
| **현재 상태** | 619줄, 8가지 이상의 책임 혼재 |
| **목표 상태** | 이벤트 기반 구조, 책임 분리 |
| **위험도** | 중간 (핵심 게임플레이 영향) |

---

## 현재 문제점 분석

### EnemyAI의 현재 책임 (8가지)

| # | 책임 | 코드 위치 | 문제 |
|:---:|:---|:---|:---|
| 1 | 상태 관리 | `State` enum, `UpdateState()`, `OnStateChanged()` | - |
| 2 | AI 로직 | `ExecuteChase()`, `ExecuteAttack()`, `FindTarget()` | - |
| 3 | 데미지 처리 | `TakeDamage()`, `TriggerHitState()` | - |
| 4 | **보상 지급** | `Die()` 내 골드/경험치 지급 | **SRP 위반** |
| 5 | **Manager 직접 호출** | `Die()` 내 StageManager 호출 | **DIP 위반** |
| 6 | 넉백 처리 | `ApplyKnockback()` | - |
| 7 | 보스 스킬 | `ExecuteSkillAttack()`, `ApplyAoEDamage()` | 분리 권장 |
| 8 | 풀 관리 | `Initialize()`, `ResetForPool()` | - |

### 핵심 문제: Die() 메서드 (361-415줄)

```csharp
private void Die()
{
    // 상태 변경 (OK)
    _currentState = State.Dead;

    // ❌ SRP 위반: 보상 지급 로직
    CurrencyManager.Instance.AddGold(_stat.GoldReward);
    PlayerStatManager.Instance.AddExp(_stat.Exp);

    // ❌ DIP 위반: 구체 클래스 직접 참조
    StageManager.Instance.OnBossDied(this);
    StageManager.Instance.OnEnemyDied(this);

    // 애니메이션/풀 반환 (OK)
    _enemyAnimation.Die();
    StartCoroutine(ReturnToPoolAfterDelay());
}
```

---

## 리팩토링 계획

### Phase A: 이벤트 기반 구조로 전환 (필수)

#### 작업 A-1: EnemyAI에 OnDied 이벤트 추가

**변경 전**:
```csharp
public class EnemyAI : MonoBehaviour, IDamageable
{
    // 이벤트 없음
}
```

**변경 후**:
```csharp
public class EnemyAI : MonoBehaviour, IDamageable
{
    // 사망 시 발생하는 이벤트 (보상 정보 포함)
    public event System.Action<EnemyAI, EnemyStat> OnDied;

    // 스탯 접근용 프로퍼티 추가
    public EnemyStat Stat => _stat;
}
```

---

#### 작업 A-2: Die() 메서드에서 보상 지급/Manager 호출 제거

**변경 전**:
```csharp
private void Die()
{
    if (_currentState == State.Dead) return;
    _currentState = State.Dead;

    // Agent 정지
    if (_agent != null)
    {
        _agent.isStopped = true;
        _agent.enabled = false;
    }

    // 사운드
    SoundManager.Instance?.PlaySFX(ESfxId.MonsterDead, transform.position);

    // ❌ 제거 대상: 보상 지급
    CurrencyManager.Instance?.AddGold(_stat.GoldReward);
    PlayerStatManager.Instance?.AddExp(_stat.Exp);

    // ❌ 제거 대상: StageManager 호출
    if (_isBoss)
        StageManager.Instance.OnBossDied(this);
    else
        StageManager.Instance.OnEnemyDied(this);

    // 애니메이션
    _enemyAnimation?.Die();

    // 풀 반환
    StartCoroutine(ReturnToPoolAfterDelay(DEATH_POOL_RETURN_DELAY));
}
```

**변경 후**:
```csharp
private void Die()
{
    if (_currentState == State.Dead) return;
    _currentState = State.Dead;

    // Agent 정지
    if (_agent != null)
    {
        _agent.isStopped = true;
        _agent.enabled = false;
    }

    // 사운드
    SoundManager.Instance?.PlaySFX(ESfxId.MonsterDead, transform.position);

    // ✅ 이벤트 발생 (보상 지급은 구독자가 처리)
    OnDied?.Invoke(this, _stat);

    // 애니메이션
    _enemyAnimation?.Die();

    // 풀 반환
    StartCoroutine(ReturnToPoolAfterDelay(DEATH_POOL_RETURN_DELAY));
}
```

---

#### 작업 A-3: EnemySpawner에서 이벤트 구독 및 처리

**EnemySpawner.cs 수정**:

```csharp
public class EnemySpawner : Singleton<EnemySpawner>
{
    // 스폰 시 이벤트 구독
    private void OnTakeFromPool(EnemyAI enemy)
    {
        enemy.gameObject.SetActive(true);
        _aliveEnemies.Add(enemy);

        // ✅ 사망 이벤트 구독
        enemy.OnDied += HandleEnemyDied;
    }

    // 풀 반환 시 이벤트 해제
    private void OnReturnedToPool(EnemyAI enemy)
    {
        // ✅ 사망 이벤트 해제
        enemy.OnDied -= HandleEnemyDied;

        _aliveEnemies.Remove(enemy);
        enemy.ResetForPool();
        enemy.gameObject.SetActive(false);
    }

    // ✅ 사망 처리 핸들러
    private void HandleEnemyDied(EnemyAI enemy, EnemyStat stat)
    {
        // 보상 지급
        CurrencyManager.Instance?.AddGold(stat.GoldReward);
        PlayerStatManager.Instance?.AddExp(stat.Exp);

        // StageManager에 알림
        if (enemy.IsBoss)
        {
            StageManager.Instance?.OnBossDied(enemy);
        }
        else
        {
            StageManager.Instance?.OnEnemyDied(enemy);
        }
    }
}
```

---

#### 작업 A-4: ResetForPool()에서 이벤트 초기화

```csharp
public void ResetForPool()
{
    // 기존 리셋 로직...

    // ✅ 이벤트 초기화 (구독자가 남아있지 않도록)
    OnDied = null;
}
```

---

### Phase B: 보스 스킬 분리 (권장)

현재 보스 스킬 관련 코드가 EnemyAI에 직접 포함되어 있습니다.
보스 전용 로직을 분리하면 일반 적 AI의 복잡도가 감소합니다.

#### 작업 B-1: EnemySkillHandler 클래스 생성

**새 파일**: `Assets/02.Scripts/Enemy/EnemySkillHandler.cs`

```csharp
using System.Collections;
using UnityEngine;

public class EnemySkillHandler : MonoBehaviour
{
    [Header("▼ 스킬 설정")]
    [SerializeField] private float _skillCooldown = 5f;
    [SerializeField] private float _skillRadius = 3f;
    [SerializeField] private float _skillChargeTime = 1f;
    [SerializeField] private float _skillDamageMultiplier = 2f;

    private EnemyAI _enemyAI;
    private EnemyAnimation _enemyAnimation;
    private float _lastSkillTime;
    private bool _isUsingSkill;

    public bool IsUsingSkill => _isUsingSkill;

    public void Initialize(EnemyAI enemyAI, EnemyAnimation animation)
    {
        _enemyAI = enemyAI;
        _enemyAnimation = animation;
        _lastSkillTime = -_skillCooldown; // 시작 시 바로 사용 가능
        _isUsingSkill = false;
    }

    public void Reset()
    {
        _lastSkillTime = 0f;
        _isUsingSkill = false;
        StopAllCoroutines();
    }

    public bool CanUseSkill()
    {
        if (_isUsingSkill) return false;
        return Time.time - _lastSkillTime >= _skillCooldown;
    }

    public void TryUseSkill(EnemyStat stat, System.Action onSkillStart, System.Action onSkillEnd)
    {
        if (!CanUseSkill()) return;
        StartCoroutine(ExecuteSkillAttack(stat, onSkillStart, onSkillEnd));
    }

    private IEnumerator ExecuteSkillAttack(EnemyStat stat, System.Action onSkillStart, System.Action onSkillEnd)
    {
        _isUsingSkill = true;
        onSkillStart?.Invoke();

        _enemyAnimation?.TriggerSkill();

        yield return new WaitForSeconds(_skillChargeTime);

        ApplyAoEDamage(stat);

        _lastSkillTime = Time.time;
        _isUsingSkill = false;
        onSkillEnd?.Invoke();
    }

    private void ApplyAoEDamage(EnemyStat stat)
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, _skillRadius);
        foreach (Collider hit in hits)
        {
            if (hit.CompareTag("Player"))
            {
                IDamageable target = hit.GetComponent<IDamageable>();
                if (target != null)
                {
                    double skillDamage = stat.AttackDamage * _skillDamageMultiplier;
                    target.TakeDamage(skillDamage, false);
                }
            }
        }

        EffectManager.Instance?.PlaySkillVfx(transform.position);
    }
}
```

---

#### 작업 B-2: EnemyAI에서 스킬 관련 코드 제거 및 위임

**EnemyAI.cs 수정**:

```csharp
public class EnemyAI : MonoBehaviour, IDamageable
{
    // ✅ 스킬 핸들러 참조 추가
    [SerializeField] private EnemySkillHandler _skillHandler;

    // ❌ 제거: 스킬 관련 필드들
    // private float _skillCooldown = 5f;
    // private float _skillRadius = 3f;
    // private float _skillChargeTime = 1f;
    // private float _skillDamageMultiplier = 2f;
    // private float _lastSkillTime;
    // private bool _isUsingSkill;

    // ✅ 프로퍼티 수정
    public bool IsUsingSkill => _skillHandler != null && _skillHandler.IsUsingSkill;

    public void InitializeAsBoss(EnemyStat stat)
    {
        Initialize(stat);
        _isBoss = true;
        _attackRange = _attackRange * 3f;

        // ✅ 스킬 핸들러 초기화
        if (_skillHandler != null)
        {
            _skillHandler.Initialize(this, _enemyAnimation);
        }
    }

    private void UpdateState(float distanceToTarget)
    {
        if (_skillHandler != null && _skillHandler.IsUsingSkill) return;

        // 보스 스킬 체크
        if (_isBoss && distanceToTarget <= _attackRange && _skillHandler != null && _skillHandler.CanUseSkill())
        {
            _skillHandler.TryUseSkill(_stat, OnSkillStart, OnSkillEnd);
            return;
        }

        // 나머지 상태 업데이트...
    }

    private void OnSkillStart()
    {
        _currentState = State.SkillAttack;
        if (_agent != null && _agent.isOnNavMesh)
        {
            _agent.isStopped = true;
        }
    }

    private void OnSkillEnd()
    {
        _currentState = State.Idle;
        if (_agent != null && _agent.enabled)
        {
            _agent.isStopped = false;
        }
    }

    // ❌ 제거: CanUseSkill(), ExecuteSkillAttack(), ApplyAoEDamage()
}
```

---

### Phase C: 상태 머신 분리 (선택적 - 권장하지 않음)

#### 분석

현재 상태 머신은:
- 6개 상태 (Idle, Chase, Attack, SkillAttack, Hit, Dead)
- 전환 로직이 간단함
- EnemyAI와 밀접하게 연결됨

#### 결론

**분리하지 않는 것을 권장**합니다.

이유:
1. 상태 머신이 충분히 간단함
2. 분리 시 EnemyAI ↔ StateMachine 간 데이터 공유 복잡도 증가
3. 오버엔지니어링 우려
4. 현재 구조로도 충분히 유지보수 가능

---

## 파일별 변경 요약

### Phase A (필수)

| 파일 | 변경 내용 |
|:---|:---|
| `EnemyAI.cs` | `OnDied` 이벤트 추가, `Stat` 프로퍼티 추가, `Die()` 수정, `ResetForPool()` 수정 |
| `EnemySpawner.cs` | `HandleEnemyDied()` 추가, `OnTakeFromPool/OnReturnedToPool` 수정 |

### Phase B (권장)

| 파일 | 변경 내용 |
|:---|:---|
| `EnemySkillHandler.cs` | **신규 생성** |
| `EnemyAI.cs` | 스킬 관련 필드/메서드 제거, `_skillHandler` 위임 |
| `Enemy 프리팹` | `EnemySkillHandler` 컴포넌트 추가 (보스 전용) |

---

## 작업 순서

### 1단계: Phase A 구현
1. `EnemyAI.cs`에 `OnDied` 이벤트, `Stat` 프로퍼티 추가
2. `EnemyAI.Die()` 수정 (보상 지급/Manager 호출 제거)
3. `EnemyAI.ResetForPool()` 수정 (이벤트 초기화)
4. `EnemySpawner.cs` 수정 (이벤트 구독/처리)
5. 테스트: 적 사망 → 보상 지급 → 스테이지 진행 확인

### 2단계: Phase B 구현 (선택)
1. `EnemySkillHandler.cs` 생성
2. `EnemyAI.cs`에서 스킬 코드 제거/위임
3. 보스 프리팹에 `EnemySkillHandler` 컴포넌트 추가
4. 테스트: 보스 스킬 정상 동작 확인

---

## 테스트 체크리스트

### Phase A 테스트

- [ ] 일반 적 사망 시 골드 지급 정상
- [ ] 일반 적 사망 시 경험치 지급 정상
- [ ] 일반 적 사망 시 StageManager.OnEnemyDied() 호출됨
- [ ] 보스 사망 시 골드 지급 정상
- [ ] 보스 사망 시 경험치 지급 정상
- [ ] 보스 사망 시 StageManager.OnBossDied() 호출됨
- [ ] 적 풀 반환 후 재사용 시 이벤트 중복 구독 없음
- [ ] 스테이지 클리어 후 다음 스테이지 진행 정상

### Phase B 테스트

- [ ] 보스 스킬 쿨다운 정상 동작
- [ ] 보스 스킬 차징 시간 정상
- [ ] 보스 스킬 범위 데미지 정상
- [ ] 보스 스킬 VFX 재생 정상
- [ ] 일반 적은 스킬 사용 안 함

---

## 롤백 전략

1. 작업 전: `git status` 확인
2. Phase A 완료 후: 커밋
3. Phase B 완료 후: 별도 커밋
4. 문제 발생 시: `git revert` 또는 `git checkout`

---

## 예상 결과

### 코드 라인 수 변화

| 클래스 | 변경 전 | 변경 후 (예상) |
|:---|:---:|:---:|
| `EnemyAI.cs` | 619줄 | ~520줄 (-100줄) |
| `EnemySpawner.cs` | 280줄 | ~310줄 (+30줄) |
| `EnemySkillHandler.cs` | - | ~80줄 (신규) |

### SOLID 준수 개선

| 원칙 | 변경 전 | 변경 후 |
|:---|:---:|:---:|
| **SRP** | ⚠️ 위반 | ✅ 준수 |
| **DIP** | ⚠️ 위반 | ✅ 준수 |
| **OCP** | ✅ | ✅ |
| **LSP** | ✅ | ✅ |
| **ISP** | ✅ | ✅ |
