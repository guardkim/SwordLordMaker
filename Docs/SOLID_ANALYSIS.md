# SOLID 원칙 준수 분석 보고서

## 개요

SwordLordMaker 프로젝트의 주요 클래스들을 SOLID 원칙 기준으로 분석한 문서입니다.

| 원칙 | 전체 평가 | 상태 |
|:---|:---|:---:|
| **S** - Single Responsibility | 일부 위반 | ⚠️ |
| **O** - Open/Closed | 양호 | ✅ |
| **L** - Liskov Substitution | 양호 | ✅ |
| **I** - Interface Segregation | 양호 | ✅ |
| **D** - Dependency Inversion | 일부 위반 | ⚠️ |

---

## S - 단일 책임 원칙 (Single Responsibility Principle)

> "클래스는 단 하나의 변경 이유만 가져야 한다"

### ⚠️ 위반 사례

#### 1. EnemyAI.cs (619줄) - **심각**

| 현재 책임 | 분리 제안 |
|:---|:---|
| 상태 머신 (Idle, Chase, Attack, Hit, Dead) | `EnemyStateMachine` |
| AI 로직 (타겟 탐색, 추적, 공격) | `EnemyAI` (유지) |
| 데미지 처리 | `IDamageable` 구현 (유지) |
| **보상 지급** (골드, 경험치) | `EnemyDeathHandler` 또는 이벤트 |
| 애니메이션 제어 | `EnemyAnimation` (이미 분리됨) |
| 스킬 사용 | `EnemySkillHandler` |
| 넉백 처리 | `KnockbackHandler` |

**문제 코드 (Die 메서드)**:
```csharp
private void Die()
{
    // ... 상태 변경 ...

    // ❌ 보상 지급 로직이 EnemyAI에 있음
    if (CurrencyManager.Instance != null && _stat != null)
    {
        CurrencyManager.Instance.AddGold(_stat.GoldReward);
    }

    if (_stat != null && PlayerStatManager.Instance != null)
    {
        PlayerStatManager.Instance.AddExp(_stat.Exp);
    }

    // ❌ StageManager 직접 호출
    if (StageManager.Instance != null)
    {
        if (_isBoss)
            StageManager.Instance.OnBossDied(this);
        else
            StageManager.Instance.OnEnemyDied(this);
    }
}
```

**권장 개선안**:
```csharp
// EnemyAI.cs
public event Action<EnemyAI> OnDied;

private void Die()
{
    // 상태 변경만 담당
    _currentState = State.Dead;

    // 이벤트로 알림 (보상 지급은 구독자가 처리)
    OnDied?.Invoke(this);
}

// EnemyDeathHandler.cs (새로 생성) 또는 StageManager에서 구독
private void HandleEnemyDeath(EnemyAI enemy)
{
    CurrencyManager.Instance?.AddGold(enemy.Stat.GoldReward);
    PlayerStatManager.Instance?.AddExp(enemy.Stat.Exp);
}
```

---

#### 2. PopupManager.cs (326줄) - **중간**

| 현재 책임 | 분리 제안 |
|:---|:---|
| 팝업 열기/닫기 관리 | `PopupManager` (유지) |
| 팝업 스택 관리 | `PopupManager` (유지) |
| **블로커 풀링** | `PopupBlockerPool` |
| **정렬 순서 계산** | `PopupSortingOrderCalculator` |

**현재 상태**: 326줄로 다소 큰 편이나, 팝업 관리라는 단일 도메인 내에서의 책임이므로 **허용 가능한 수준**

---

#### 3. CurrencyManager.cs (216줄) - **양호**

| 책임 | 상태 |
|:---|:---:|
| 재화 데이터 관리 | ✅ |
| Repository 연동 | ✅ |
| 자동 저장 | ✅ (관련 책임) |

**평가**: 재화 관리라는 단일 책임에 집중, **양호**

---

### ✅ 준수 사례

| 클래스 | 줄 수 | 책임 | 평가 |
|:---|:---:|:---|:---:|
| `GameManager` | 70줄 | 게임 상태, 플레이어 사망/부활 | ✅ |
| `PlayerStatManager` | 121줄 | 플레이어 스탯/경험치 관리 | ✅ |
| `UpgradeManager` | 173줄 | 강화 시스템 관리 | ✅ |
| `StageManager` | 261줄 | 스테이지 진행 관리 | ✅ |
| `EnemySpawner` | 280줄 | 적 스폰/풀 관리 | ✅ |
| `BaseSwordController` | 42줄 | 검 컨트롤러 공통 로직 | ✅ |

---

## O - 개방/폐쇄 원칙 (Open/Closed Principle)

> "확장에는 열려있고, 수정에는 닫혀있어야 한다"

### ✅ 준수 사례

#### 1. 검 시스템 (전략 패턴)

```
BaseSwordController (추상)
    ├── AdelFlyingSwordController
    ├── HypoSwordController
    └── PixelSwordController
```

- 새로운 검 타입 추가 시 `BaseSwordController`를 상속받아 구현
- 기존 코드 수정 없이 확장 가능
- **OCP 완벽 준수** ✅

#### 2. Repository 패턴

```
ICurrencyRepository (인터페이스)
    └── LocalCurrencyRepository (구현)

IPlayerStatRepository (인터페이스)
    └── LocalPlayerStatRepository (구현)
```

- 새로운 저장소 구현체 추가 가능 (예: CloudRepository)
- Manager 코드 수정 없이 교체 가능
- **OCP 준수** ✅

---

### ⚠️ 개선 필요

#### Enum 기반 분기 (잠재적 OCP 위반)

```csharp
// PopupManager에서 PopupType으로 분기
public PopupBase OpenPopup(EPopupType type) { ... }

// SfxId로 사운드 재생
SoundManager.Instance.PlaySFX(ESfxId.SwordAttack);
```

**현재 상태**: Enum 추가 시 관련 코드 수정 필요하나, Unity 프로젝트 특성상 **허용 가능한 수준**

---

## L - 리스코프 치환 원칙 (Liskov Substitution Principle)

> "자식 클래스는 부모 클래스를 대체할 수 있어야 한다"

### ✅ 준수 사례

#### 1. Singleton<T> 상속 구조

```csharp
public class Singleton<T> : MonoBehaviour where T : Singleton<T>
{
    protected virtual void Initialize() { }
}

// 모든 Manager가 Singleton<T> 상속
public class CurrencyManager : Singleton<CurrencyManager> { }
public class PlayerStatManager : Singleton<PlayerStatManager> { }
```

- 모든 싱글톤 매니저가 `Singleton<T>` 대체 가능
- `Initialize()` 가상 메서드로 확장점 제공
- **LSP 준수** ✅

#### 2. BaseSwordController 상속

```csharp
public abstract class BaseSwordController : MonoBehaviour
{
    protected abstract void ResetSequence();
    protected IReadOnlyList<EnemyAI> FindEnemies() { ... }
}
```

- 모든 검 컨트롤러가 `BaseSwordController` 대체 가능
- `Fire()` 호출 시 동일한 동작 보장
- **LSP 준수** ✅

#### 3. IDamageable 인터페이스

```csharp
public interface IDamageable
{
    void TakeDamage(double damage, bool isCrit);
}

// EnemyAI, PlayerHealth 등이 구현
```

- 모든 `IDamageable` 구현체가 상호 교환 가능
- **LSP 준수** ✅

---

## I - 인터페이스 분리 원칙 (Interface Segregation Principle)

> "클라이언트는 사용하지 않는 메서드에 의존하면 안 된다"

### ✅ 준수 사례

#### 1. IDamageable (3줄)

```csharp
public interface IDamageable
{
    void TakeDamage(double damage, bool isCrit);
}
```

- 단일 메서드, 명확한 책임
- **ISP 완벽 준수** ✅

#### 2. ICurrencyRepository (7줄)

```csharp
public interface ICurrencyRepository
{
    Task<Currency> LoadAsync();
    Task SaveAsync(Currency currency);
    Task SaveGoldAsync(double gold);
    Task SaveRubyAsync(double ruby);
    void ForceSaveToDisk();
}
```

- 재화 저장소에 필요한 메서드만 정의
- 적절한 크기의 인터페이스
- **ISP 준수** ✅

---

### 개선 제안

#### 대형 인터페이스 분리 (선택적)

현재 프로젝트에는 대형 인터페이스가 없으나, 향후 확장 시:

```csharp
// 권장하지 않음
public interface IGameEntity
{
    void Move();
    void Attack();
    void TakeDamage();
    void Die();
    void PlayAnimation();
    void PlaySound();
}

// 권장
public interface IMovable { void Move(); }
public interface IAttackable { void Attack(); }
public interface IDamageable { void TakeDamage(); }
```

---

## D - 의존성 역전 원칙 (Dependency Inversion Principle)

> "상위 모듈은 하위 모듈에 의존하면 안 된다. 둘 다 추상화에 의존해야 한다"

### ✅ 준수 사례

#### 1. Repository 패턴

```csharp
// Manager (상위) → Interface (추상화) ← Repository (하위)
public class CurrencyManager : Singleton<CurrencyManager>
{
    private ICurrencyRepository _repository;  // ✅ 인터페이스 의존

    private void InitializeRepository()
    {
        _repository = new LocalCurrencyRepository();  // 구현체 주입
    }
}
```

- Manager가 인터페이스에 의존
- 구현체 교체 용이
- **DIP 준수** ✅

---

### ⚠️ 위반 사례

#### 1. EnemyAI에서 구체 클래스 직접 참조

```csharp
// EnemyAI.cs - Die() 메서드
if (CurrencyManager.Instance != null)  // ❌ 구체 클래스 직접 참조
{
    CurrencyManager.Instance.AddGold(_stat.GoldReward);
}

if (PlayerStatManager.Instance != null)  // ❌ 구체 클래스 직접 참조
{
    PlayerStatManager.Instance.AddExp(_stat.Exp);
}

if (StageManager.Instance != null)  // ❌ 구체 클래스 직접 참조
{
    StageManager.Instance.OnEnemyDied(this);
}
```

**문제점**:
- 도메인 객체(EnemyAI)가 여러 Manager에 직접 의존
- 테스트 어려움, 결합도 높음

**권장 개선안**:
```csharp
// 옵션 A: 이벤트 기반 (권장)
public event Action<EnemyAI> OnDied;

private void Die()
{
    OnDied?.Invoke(this);  // 구독자가 처리
}

// 옵션 B: 인터페이스 주입
public interface IRewardHandler
{
    void GiveReward(EnemyStat stat);
}
```

#### 2. 싱글톤 직접 참조 패턴

프로젝트 전반에서 `Manager.Instance` 패턴 사용:

```csharp
SoundManager.Instance.PlaySFX(ESfxId.SwordAttack);
CurrencyManager.Instance.AddGold(amount);
```

**현재 상태**: Unity 프로젝트에서 흔한 패턴이며, **실용적 트레이드오프**로 허용 가능

---

## 종합 평가

### 잘 지켜진 부분 ✅

| 영역 | 내용 |
|:---|:---|
| **검 시스템** | 전략 패턴으로 OCP, LSP 완벽 준수 |
| **Repository 패턴** | DIP, OCP 준수 |
| **인터페이스 설계** | ISP 준수 (작고 집중적인 인터페이스) |
| **Manager 클래스** | 대부분 SRP 준수 |
| **Singleton 베이스** | LSP 준수 |

### 개선이 필요한 부분 ⚠️

| 영역 | 문제 | 우선순위 | 권장 조치 |
|:---|:---|:---:|:---|
| **EnemyAI.Die()** | SRP/DIP 위반 (보상 지급 직접 처리) | **높음** | 이벤트 기반으로 분리 |
| **EnemyAI 클래스** | 619줄, 과도한 책임 | 중간 | 상태머신/스킬 분리 |
| **PopupManager** | 326줄, 블로커 풀링 포함 | 낮음 | 필요시 분리 |

---

## 권장 리팩토링 (Phase 7 연계)

REFACTOR_PLAN.md의 Phase 7과 연계하여:

1. **EnemyAI.Die() 이벤트 기반 전환**
   - `OnDied` 이벤트 추가
   - 보상 지급 로직을 구독자(StageManager 또는 별도 Handler)로 이동

2. **EnemyAI 책임 분리** (선택적)
   - `EnemyStateMachine` 분리
   - `EnemySkillHandler` 분리

이 개선은 Phase 7 "DDD 계층 위반 수정" 작업과 함께 진행하는 것을 권장합니다.
