# [파일명: Sword_Documentation.md]

## 1. 📂 모듈 개요 (Module Overview)

이 모듈은 게임 내 검(Sword)의 스탯 데이터를 관리합니다. BigInteger를 사용하여 공격력 무한 스케일링을 지원하며, 강화 시 스탯 보너스가 적용됩니다.

**주요 스크립트:**
- `SwordStat.cs` (Data Layer) - 검 스탯 데이터 모델
- `ISwordStatRepository.cs` - 검 저장소 인터페이스
- `SwordStatRepository.cs` (Repository Layer) - BGDatabase 연동

---

## 2. 🏗️ 아키텍처 및 상호작용 (Architecture & Interactions)

### Script Flow
```
UpgradeManager (Manager Layer)
    └─> SwordStatRepository (Repository Layer) → BGDatabase
    └─> SwordStat (Data Layer) - 검 스탯 데이터
    └─> ApplyUpgrades(SwordStat baseStat)
        └─> 강화 보너스 적용된 검 스탯 반환
```

### Dependencies
- **외부 시스템:**
  - `BGDatabase`: 검 스탯 데이터 영속화
  - `UpgradeManager`: 강화 시 검 스탯 보너스 적용

### Diagram Description
```
┌─────────────────────────────────────────────┐
│          UpgradeManager                     │
│  - ApplyUpgrades(SwordStat)                 │
│  - GetBigIntBonus(Sword_AttackDamage)       │
└────────────┬───────────────────────────────┘
             │
             ▼
    ┌────────────────┐
    │ SwordStatRepo  │
    │  (Repository)  │
    └────────────────┘
             │
             ▼
    ┌────────────────┐
    │   SwordStat    │
    │  - AttackDamage│ (BigInteger)
    │  - Cooldown    │ (float)
    │  - MoveSpeed   │ (float)
    │  - CritDamageMultiplier│ (float)
    │  - CritChance  │ (float)
    │  - CalculateDamage(bool isCrit)         │
    └────────────────┘
             │
             ▼ 강화 보너스 적용
    ┌────────────────┐
    │ SwordStat (강화)│
    └────────────────┘
```

---

## 3. 📝 상세 코드 분석 (Detailed Code Analysis)

### SwordStat (Data Layer)
- **기능:** 불변 데이터 클래스 (record)
- **주요 필드:**
  - `AttackDamage` → BigInteger (기본 공격력)
  - `Cooldown` → float (쿨타임, 초)
  - `MoveSpeed` → float (이동 속도)
  - `CritDamageMultiplier` → float (치명타 데미지 배율, 2.0 = 2배)
  - `CritChance` → float (치명타 확률 0~1)
- **메서드:**
  - `CalculateDamage(bool isCrit)` - 치명타 시 최종 데미지 계산
    - 일반: `AttackDamage`
    - 치명타: `new BigInteger((double)AttackDamage * CritDamageMultiplier)`

### ISwordStatRepository
- **기능:** 검 저장소 인터페이스
- **메서드:** `GetById(string swordId)`

### SwordStatRepository (Repository Layer)
- **기능:** BGDatabase에서 SwordStat 로드
- **테이블:** `SwordStat` 테이블
- **핵심 메서드:**
  - `GetById(string swordId)` - ID로 검 스탯 조회
  - string → BigInteger 변환 로직

---

## 4. 💡 설계 의도 및 구현 이유 (Design Rationale)

### 1. 왜 AttackDamage는 BigInteger이고 CritDamageMultiplier는 float인가?
- **목적:** 공격력은 무한 스케일링 필요, 배율은 소수 단위 조절 필요
- **효과:** 방치형 게임의 무한 성장 + 정밀한 밸런싱

### 2. 왜 CalculateDamage 메서드를 가졌는가?
- **목적:** 치명타 계산 로직 캡슐화
- **효과:** 검 사용 시스템(Flying Sword 등)이 직접 계산할 필요 없음

### 3. 왜 record를 사용했는가?
- **목적:** 불변 데이터 보장
- **효과:** 검 스탯 무결성 보장

---

## 5. 🎮 사용 가이드 (Usage Guide within Unity)

### Inspector 설정
**비행검 시스템:**
- `ControllerManager`: 검 프리팹 할당
- 검 프리팹에 SwordStat 컴포넌트 필요 (또는 Repository에서 로드)

### 초기화 방법
1. BGDatabase에 `SwordStat` 테이블 생성
2. 테이블에 검 데이터 입력 (ID, 스탯 등)
3. `UpgradeManager.ApplyUpgrades()`로 강화 스탯 적용

### 사용 예시
```csharp
// 기본 스탯 로드
SwordStat baseStat = SwordStatRepository.Instance.GetById("SWORD_IRON");

// 강화 스탯 적용
SwordStat upgradedStat = UpgradeManager.Instance.ApplyUpgrades(baseStat);

// 데미지 계산
BigInteger damage = upgradedStat.CalculateDamage(isCrit: true);
```

---

## 6. ⚠️ 크리티컬 리뷰 및 개선점 (Critical Review)

### 잠재적 오류
1. **CritDamage 타입 미스매치 (중요)**
   - **위치:** `SwordStat.CritDamageMultiplier` (float)
   - **문제:** TDD는 BigInteger를 요구하나 실제 코드에서 float 사용
   - **영향:** 무한 스케일링 시 정밀도 손실
   - **해결:** `CritDamageMultiplier`를 `BigInteger CritDamage`로 변경
   - **영향 파일:** `SwordStat.cs`, `UpgradeManager.cs`

### 정밀도 이슈
1. **CalculateDamage의 double 캐스팅**
   - **위치:** `SwordStat.CalculateDamage()`
   - **문제:** `new BigInteger((double)AttackDamage * CritDamageMultiplier)`
   - **문제:** BigInteger → double → BigInteger 변환 시 정밀도 손실 가능
   - **개선:** BigInteger 연산으로 전환 (`AttackDamage * new BigInteger(CritDamageMultiplier) / 100`)

### Refactoring 제안
1. **SwordStat 데이터 타입 통일**
   - **현재:** BigInteger (AttackDamage) + float (CritDamageMultiplier)
   - **개선:** 모든 스탯을 BigInteger로 통일 (CritDamage: 배율 * 100을 BigInteger로 저장)

2. **CritDamage와 CritChance의 이름 개선**
   - **현재:** `CritDamageMultiplier`, `CritChance`
   - **개선:** `CritDamageBonus`, `CritChanceBonus` (강화 보너스 명확화)

3. **CalculateDamage 로직 개선**
   - **현재:** 치명타 계산 로직이 SwordStat에 있음
   - **개선:** `DamageCalculator` 별도 유틸리티로 분리

### 코드 스타일
- ✅ BigInteger 사용 올바름 (AttackDamage)
- ⚠️ float와 BigInteger 혼합 사용 문제 (CritDamage)
- ⚠️ double 캐스팅 정밀도 손실 가능성
- ⚠️ `CritDamageMultiplier` 이름이 모호함 (배율인지 절댓값인지)
