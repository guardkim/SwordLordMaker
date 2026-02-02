# SwordStat 리팩토링 작업 계획서

## 문서 정보
- **버전**: 1.1
- **작성일**: 2026-02-02
- **수정일**: 2026-02-02
- **작성자**: Sisyphus (AI Assistant)
- **상태**: ✅ 구현 완료

---

## 1. 개요

### 1.1 배경
Phase 2 리팩토링(record → class) 완료 후, UpgradeManager와 여러 SwordController(Adel, Hypo, Pixel)에서 SwordStat 객체의 속성을 복사하는 로직이 여전히 중복되어 있습니다.

### 1.2 목적
코드 중복을 제거하고 유지보수성을 높이기 위해 SwordStat 클래스 내에 복사 생성자와 CopyFrom 메서드를 추가합니다.

- **복사 생성자**: 새로운 SwordStat 인스턴스를 생성할 때 사용 (예: LoadBaseStat)
- **CopyFrom 메서드**: 기존 SwordStat 인스턴스의 내용을 덮어쓸 때 사용 (예: ApplyUpgrades fallback)

### 1.3 기대 효과
- 중복 코드 약 30라인 제거
- 새 필드 추가 시 수정 범위 최소화 (5곳 → 1곳)
- SOLID 원칙 준수 (SRP, OCP, DRY)

---

## 2. 변경 대상 파일

| 순서 | 파일 경로 | 변경 유형 | 우선순위 |
|------|----------|----------|----------|
| 1 | `Assets/02.Scripts/Sword/Data/SwordStat.cs` | 추가 | 🔴 높음 |
| 2 | `Assets/02.Scripts/Upgrade/Manager/UpgradeManager.cs` | 수정 | 🔴 높음 |
| 3 | `Assets/DamageFloater/01.Scripts/Adel/AdelFlyingSwordController.cs` | 수정 | 🔴 높음 |
| 4 | `Assets/DamageFloater/01.Scripts/Hypocycloid/HypoSwordController.cs` | 수정 | 🔴 높음 |
| 5 | `Assets/DamageFloater/01.Scripts/PixelHunterLike/PixelSwordController.cs` | 수정 | 🔴 높음 |

---

## 3. 상세 변경 계획

### 3.1 SwordStat.cs (메인 수정)

**파일 경로**: `Assets/02.Scripts/Sword/Data/SwordStat.cs`

#### 3.1.1 현재 코드
```csharp
public class SwordStat
{
    public string Id { get; private set; }
    public double AttackDamage { get; set; }
    public float Cooldown { get; set; }
    public float MoveSpeed { get; set; }
    public float CritDamageMultiplier { get; set; }
    public float CritChance { get; set; }

    public SwordStat(
        string id,
        double attackDamage,
        float cooldown,
        float moveSpeed,
        float critDamageMultiplier,
        float critChance)
    {
        Id = id;
        AttackDamage = attackDamage;
        Cooldown = cooldown;
        MoveSpeed = moveSpeed;
        CritDamageMultiplier = critDamageMultiplier;
        CritChance = critChance;
    }

    public double CalculateDamage(bool isCrit)
    {
        if (!isCrit) return AttackDamage;
        return AttackDamage * CritDamageMultiplier;
    }
}
```

#### 3.1.2 변경 후 코드
```csharp
public class SwordStat
{
    public string Id { get; private set; }
    public double AttackDamage { get; set; }
    public float Cooldown { get; set; }
    public float MoveSpeed { get; set; }
    public float CritDamageMultiplier { get; set; }
    public float CritChance { get; set; }

    // 기본 생성자
    public SwordStat(
        string id,
        double attackDamage,
        float cooldown,
        float moveSpeed,
        float critDamageMultiplier,
        float critChance)
    {
        Id = id;
        AttackDamage = attackDamage;
        Cooldown = cooldown;
        MoveSpeed = moveSpeed;
        CritDamageMultiplier = critDamageMultiplier;
        CritChance = critChance;
    }

    // 복사 생성자: 새로운 SwordStat 인스턴스를 생성할 때 사용
    public SwordStat(SwordStat other)
    {
        Id = other.Id;
        AttackDamage = other.AttackDamage;
        Cooldown = other.Cooldown;
        MoveSpeed = other.MoveSpeed;
        CritDamageMultiplier = other.CritDamageMultiplier;
        CritChance = other.CritChance;
    }

    // CopyFrom 메서드: 기존 인스턴스에 값 복사 (Id 제외)
    public void CopyFrom(SwordStat source)
    {
        AttackDamage = source.AttackDamage;
        Cooldown = source.Cooldown;
        MoveSpeed = source.MoveSpeed;
        CritDamageMultiplier = source.CritDamageMultiplier;
        CritChance = source.CritChance;
    }

    public double CalculateDamage(bool isCrit)
    {
        if (!isCrit) return AttackDamage;
        return AttackDamage * CritDamageMultiplier;
    }
}
```

#### 3.1.3 변경 내용 요약
- **복사 생성자 추가**: `SwordStat(SwordStat other)` - 깊은 복사 후 새 인스턴스 반환
- **CopyFrom 메서드 추가**: `void CopyFrom(SwordStat source)` - 기존 인스턴스에 덮어쓰기 (Id 제외)

---

### 3.2 UpgradeManager.cs (수정)

**파일 경로**: `Assets/02.Scripts/Upgrade/Manager/UpgradeManager.cs`

#### 3.2.1 현재 코드 (159-176행)
```csharp
public void ApplyUpgrades(SwordStat baseStat, SwordStat targetStat)
{
    if (_repository == null)
    {
        targetStat.AttackDamage = baseStat.AttackDamage;
        targetStat.Cooldown = baseStat.Cooldown;
        targetStat.MoveSpeed = baseStat.MoveSpeed;
        targetStat.CritDamageMultiplier = baseStat.CritDamageMultiplier;
        targetStat.CritChance = baseStat.CritChance;
        return;
    }

    targetStat.AttackDamage = baseStat.AttackDamage + GetDoubleBonus(UpgradeId.SwordAttackDamage.ToKey());
    targetStat.Cooldown = Mathf.Max(0.1f, baseStat.Cooldown - GetBonus(UpgradeId.SwordCooldown.ToKey()));
    targetStat.MoveSpeed = baseStat.MoveSpeed + GetBonus(UpgradeId.SwordMoveSpeed.ToKey());
    targetStat.CritDamageMultiplier = baseStat.CritDamageMultiplier + GetBonus(UpgradeId.SwordCritDamage.ToKey());
    targetStat.CritChance = Mathf.Min(1f, baseStat.CritChance + GetBonus(UpgradeId.SwordCritChance.ToKey()));
}
```

#### 3.2.2 변경 후 코드
```csharp
public void ApplyUpgrades(SwordStat baseStat, SwordStat targetStat)
{
    if (_repository == null)
    {
        targetStat.CopyFrom(baseStat);
        return;
    }

    // 강화 보너스 적용 (baseStat 기준으로 계산)
    targetStat.AttackDamage = baseStat.AttackDamage + GetDoubleBonus(UpgradeId.SwordAttackDamage.ToKey());
    targetStat.Cooldown = Mathf.Max(0.1f, baseStat.Cooldown - GetBonus(UpgradeId.SwordCooldown.ToKey()));
    targetStat.MoveSpeed = baseStat.MoveSpeed + GetBonus(UpgradeId.SwordMoveSpeed.ToKey());
    targetStat.CritDamageMultiplier = baseStat.CritDamageMultiplier + GetBonus(UpgradeId.SwordCritDamage.ToKey());
    targetStat.CritChance = Mathf.Min(1f, baseStat.CritChance + GetBonus(UpgradeId.SwordCritChance.ToKey()));
}
```

#### 3.2.3 변경 내용 요약
- **163-167행**: 프로퍼티 복사 5줄 → `targetStat.CopyFrom(baseStat);` 1줄로 단순화
- **보너스 적용 로직은 유지**: 각 필드별로 다른 계산이 필요하므로 그대로 유지

---

### 3.3 Controller 3개 (공통 패턴)

**대상 파일**:
- `AdelFlyingSwordController.cs`
- `HypoSwordController.cs`
- `PixelSwordController.cs`

#### 3.3.1 현재 코드 (LoadBaseStat)
```csharp
private void LoadBaseStat()
{
    if (_isInitialized) return;

    var repository = new SwordStatRepository();
    _baseStat = repository.GetById(_swordStatId);

    _swordStat = new SwordStat(
        _baseStat.Id,
        _baseStat.AttackDamage,
        _baseStat.Cooldown,
        _baseStat.MoveSpeed,
        _baseStat.CritDamageMultiplier,
        _baseStat.CritChance
    );

    _isInitialized = true;
}
```

#### 3.3.2 변경 후 코드 (LoadBaseStat)
```csharp
private void LoadBaseStat()
{
    if (_isInitialized) return;

    var repository = new SwordStatRepository();
    _baseStat = repository.GetById(_swordStatId);
    _swordStat = new SwordStat(_baseStat);

    _isInitialized = true;
}
```

#### 3.3.3 현재 코드 (ApplyUpgrades)
```csharp
private void ApplyUpgrades()
{
    if (_baseStat == null || _swordStat == null) return;

    if (UpgradeManager.Instance != null)
    {
        UpgradeManager.Instance.ApplyUpgrades(_baseStat, _swordStat);
    }
    else
    {
        _swordStat.AttackDamage = _baseStat.AttackDamage;
        _swordStat.Cooldown = _baseStat.Cooldown;
        _swordStat.MoveSpeed = _baseStat.MoveSpeed;
        _swordStat.CritDamageMultiplier = _baseStat.CritDamageMultiplier;
        _swordStat.CritChance = _baseStat.CritChance;
    }
}
```

#### 3.3.4 변경 후 코드 (ApplyUpgrades)
```csharp
private void ApplyUpgrades()
{
    if (_baseStat == null || _swordStat == null) return;

    if (UpgradeManager.Instance != null)
    {
        UpgradeManager.Instance.ApplyUpgrades(_baseStat, _swordStat);
    }
    else
    {
        _swordStat.CopyFrom(_baseStat);
    }
}
```

#### 3.3.5 변경 내용 요약
- **LoadBaseStat**: `new SwordStat(6개 파라미터)` → `new SwordStat(_baseStat)` (6줄 → 1줄)
- **ApplyUpgrades**: 프로퍼티 복사 5줄 → `_swordStat.CopyFrom(_baseStat)` (5줄 → 1줄)

---

## 4. 요약 통계

### 4.1 중복 코드 제거

| 위치 | 현재 | 변경 후 | 감소 |
|------|------|--------|------|
| **Controller 3개** (LoadBaseStat) | 6줄 × 3 = 18줄 | 1줄 × 3 = 3줄 | -15줄 |
| **Controller 3개** (ApplyUpgrades) | 5줄 × 3 = 15줄 | 1줄 × 3 = 3줄 | -12줄 |
| **UpgradeManager** (fallback) | 5줄 | 1줄 | -4줄 |
| **SwordStat** (추가) | 0줄 | +11줄 | +11줄 |
| **총계** | 38줄 | 18줄 | **-20줄** |

### 4.2 수정 범위 변화

| 시나리오 | 현재 | 변경 후 | 개선 |
|----------|------|--------|------|
| **새 필드 추가 시** | 5곳 수정 필요 | 1곳만 수정 (SwordStat) | **5배 개선** |
| **복사 로직 버그 수정** | 5곳 모두 수정 | 1곳만 수정 (SwordStat) | **5배 개선** |

---

## 5. 검증 체크리스트

### 5.1 코드 리뷰 체크리스트
- [ ] SwordStat.cs에 복사 생성자가 올바르게 구현됨
- [ ] SwordStat.cs에 CopyFrom 메서드가 올바르게 구현됨
- [ ] 모든 중복 코드가 제거됨

### 5.2 기능 테스트 체크리스트
- [ ] 각 Controller의 LoadBaseStat가 정상 동작
- [ ] 각 Controller의 ApplyUpgrades가 정상 동작
- [ ] UpgradeManager의 ApplyUpgrades가 정상 동작
- [ ] 모든 검이 올바르게 발사됨
- [ ] 강화 후 스탯이 올바르게 적용됨

### 5.3 정적 분석 체크리스트
- [ ] 컴파일 에러 없음
- [ ] 경고 메시지 없음

---

## 6. 작업 순서

| 단계 | 작업 내용 | 의존성 |
|------|----------|--------|
| **1** | SwordStat.cs에 복사 생성자와 CopyFrom 추가 | 없음 |
| **2** | UpgradeManager.cs ApplyUpgrades 수정 | 1 완료 |
| **3** | AdelFlyingSwordController.cs 수정 | 1 완료 |
| **4** | HypoSwordController.cs 수정 | 1 완료 |
| **5** | PixelSwordController.cs 수정 | 1 완료 |
| **6** | 테스트 및 검증 | 1-5 완료 |

---

## 7. SOLID 원칙 준수 검증

| 원칙 | 현재 | 변경 후 | 개선 여부 |
|------|------|--------|----------|
| **SRP** | ⚠️ 위반 (복사 로직 분산) | ✅ 준수 (SwordStat에 캡슐화) | ✅ 개선 |
| **OCP** | ⚠️ 위반 (수정 범위 넓음) | ✅ 준수 (수정 범위 좁힘) | ✅ 개선 |
| **DRY** | ⚠️ 위반 (38줄 중복) | ✅ 준수 (중복 제거) | ✅ 개선 |

---

**문서 끝**
