# Upgrade 시스템 기술 문서 (Upgrade System Documentation)

## 1. 📂 모듈 개요 (Module Overview)

`Upgrade` 시스템은 **SwordLordMaker** 프로젝트에서 플레이어의 성장과 무기의 성능을 영구적으로 향상시키는 핵심 시스템입니다. 이 시스템은 재화(Gold)를 소모하여 특정 능력치의 레벨을 올리고, 적용된 보너스를 게임 플레이의 다양한 수치에 반영하는 역할을 합니다.

### 핵심 기능
- **다양한 스탯 강화**: 체력, 이동 속도, 공격력, 쿨다운, 크리티컬 확률/데미지 등 지원.
- **무한 스케일링 대응**: `BigInteger` 타입을 지원하여 수치 인플레이션에 안전하게 대응.
- **데이터 영속화**: BGDatabase를 연동하여 강화 레벨 데이터를 저장 및 로드.
- **실시간 UI 갱신**: 이벤트 기반 통신을 통해 강화 즉시 UI와 스탯 반영.

---

## 2. 🏗️ 아키텍처 및 상호작용 (Architecture & Interactions)

본 시스템은 **DDD(Domain-Driven Design) 4계층 구조**를 충실히 따르며, 각 계층은 명확한 책임을 가집니다.

### 계층별 구조
1. **Data Layer (Domain)**
   - `UpgradeData`: 강화 항목의 기본 정보와 비용/보너스 계산 공식을 담은 불변 Record.
   - `UpgradeId`: 시스템 전역에서 사용하는 강화 항목 식별자 상수.
   - `PlayerUpgradeLevels`: 플레이어의 현재 강화 레벨 상태를 관리하는 컨테이너.

2. **Repository Layer (Infrastructure)**
   - `UpgradeRepository`: BGDatabase와의 데이터 교환 및 캐싱 담당.
   - `IUpgradeRepository`: 테스트 및 유연한 데이터 교환을 위한 인터페이스.

3. **Manager Layer (Application)**
   - `UpgradeManager`: 강화 로직의 핵심 비즈니스 규칙 처리. 싱글톤 패턴으로 구현.

4. **UI Layer (Presentation)**
   - `UpgradeUI`: 전체 강화 창 관리 및 슬롯 초기화.
   - `UpgradeSlotUI`: 개별 강화 항목 표시, 사용자 입력 처리 및 실시간 상태 갱신.

### 시스템 상호작용 및 의존성
- **CurrencyManager**: 강화 시 골드 보유량을 확인하고 차감(`TrySpendGold`).
- **PlayerSessionManager**: 현재 로그인된 플레이어의 프로필 정보를 식별하여 데이터 로드.
- **이벤트 기반 통신**: 
  - `UpgradeManager.OnUpgraded`: 강화 성공 시 발행되어 UI 및 스탯 시스템에 알림.
  - `CurrencyManager.OnCurrencyChanged`: 골드 변동 시 구독하여 강화 버튼의 활성 상태 실시간 업데이트.

---

## 3. 📝 상세 코드 분석 (Detailed Code Analysis)

### UpgradeData.cs (Data Layer)
- **Record 타입**: 데이터의 불변성을 보장하며 선언적인 구조를 가집니다.
- **비용 계산 (`GetCost`)**: `BaseCost * (CostMultiplier ^ Level)` 공식을 사용하여 지수적으로 상승하는 비용 구조를 구현합니다.
- **이중 보너스 지원**: 
  - `GetTotalBonus`: `float` 기반 보너스 반환 (이동 속도, 쿨다운 등).
  - `GetTotalBigIntBonus`: `BigInteger` 기반 보너스 반환 (체력, 공격력 등).

### UpgradeManager.cs (Manager Layer)
- **TryUpgrade**: 
  - 최대 레벨 도달 여부 검사.
  - `CurrencyManager`를 통한 재화 차감 시도.
  - 성공 시 `PlayerUpgradeLevels` 증분 및 `UpgradeRepository`를 통한 즉시 저장.
  - `OnUpgraded` 이벤트 전파.
- **ApplyUpgrades**: `SwordStat` 구조체를 받아 현재 강화 레벨이 적용된 새로운 스탯을 반환 (C# `with` 키워드 활용).

### UpgradeRepository.cs (Repository Layer)
- **데이터 캐싱**: `_upgradeDataCache` (Dictionary)를 사용하여 초기화 시 BGDatabase의 모든 데이터를 메모리에 로드, 런타임 조회 성능 극대화.
- **데이터 변환**: BGDatabase의 엔티티를 `UpgradeData` 레코드 객체로 변환하는 팩토리 메서드 포함.

---

## 4. 💡 설계 의도 및 구현 이유 (Design Rationale)

### BigInteger와 float의 분리 사용
- **이유**: 공격력과 체력 같은 수치는 무한히 커질 수 있어 `float`의 정밀도 한계를 넘어서지만, 쿨다운이나 이동 속도는 소수점 단위의 정밀한 계산이 더 중요하기 때문입니다. 이를 위해 `GetTotalBigIntBonus`와 `GetTotalBonus`를 명확히 분리하여 지원합니다.

### BGDatabase 내 string 타입 저장
- **이유**: BGDatabase가 `BigInteger` 타입을 직접 지원하지 않으므로, 손실 없는 데이터 저장을 위해 `string`으로 저장한 뒤 런타임에 파싱하는 방식을 채택했습니다.

### Repository 캐싱 전략
- **이유**: 매번 강화를 시도하거나 UI를 갱신할 때마다 데이터베이스에 직접 접근하는 것은 성능 저하를 유발할 수 있습니다. 초기화 시 전체 데이터를 `Dictionary`에 캐싱하여 조회 속도를 O(1)로 최적화했습니다.

### PlayerUpgradeLevels 클래스 분리
- **이유**: 플레이어의 강화 레벨 데이터는 유동적인 Dictionary 형태를 띱니다. 이를 유니티의 `JsonUtility`로 직렬화하여 저장하기 위해 `SerializableWrapper`를 포함한 전용 클래스로 분리하여 관리 효율을 높였습니다.

---

## 5. 🎮 사용 가이드 (Usage Guide within Unity)

### UpgradeUI 설정
1. 씬에 `UpgradeUI` 프리팹을 배치합니다.
2. `UpgradeUI` 컴포넌트의 인스펙터에서 각 강화 슬롯(`UpgradeSlotUI`)을 해당 필드에 할당합니다.
3. 활성화/비활성화를 제어할 `Panel` 오브젝트를 할당합니다.

### UpgradeSlotUI 설정
1. 인스펙터의 **Upgrade ID** 필드에 `UpgradeId.cs`에 정의된 상숫값을 입력합니다 (예: `Sword_AttackDamage`).
2. 해당 슬롯에서 정보를 표시할 `TextMeshProUGUI` 요소(이름, 레벨, 비용, 보너스)를 연결합니다.
3. 강화 실행을 위한 `Button`을 연결합니다.

---

## 6. ⚠️ 크리티컬 리뷰 및 개선점 (Critical Review)

### 잠재적 위험 요소
- **싱글톤 널 참조**: `UpgradeManager.Instance`나 `CurrencyManager.Instance` 호출 시, 씬 전환이나 초기화 순서에 따라 `NullReferenceException`이 발생할 가능성이 존재합니다.
- **중복 파싱 로직**: `BonusPerLevel` 문자열을 `BigInteger`로 파싱하는 코드가 `UpgradeData`와 `UpgradeSlotUI`에 각각 존재합니다. 이는 유지보수 시 누락의 위험이 있으므로 별도의 유틸리티 함수로 통합이 필요합니다.

### 코드 미비점
- **UpgradeManager L76**: 주석에 언급된 "내부적으로 BigInteger를 반환하도록 수정되어야 한다"는 내용이 `UpgradeData`에는 반영되었으나, 매니저의 일부 주석이 최신화되지 않아 혼동을 줄 수 있습니다.
- **GetTotalBigIntBonus**: `double`에서 `BigInteger`로 변환 시 데이터 유실 가능성에 대한 검토가 필요합니다.

### 성능 및 확장성
- **UI 갱신 최적화**: 현재 `RefreshAll`이 호출되거나 각 슬롯이 개별 이벤트를 구독하고 있습니다. 강화 항목이 수백 개로 늘어날 경우, `ScrollRect`의 가상화와 연동된 갱신 전략이 필요할 수 있습니다.

---
**문서 작성일**: 2026-01-16
**작성자**: Antigravity (Technical Writer Agent)
