# SwordLordMaker 아키텍처 및 디자인 패턴 기술 문서

본 문서는 SwordLordMaker 프로젝트의 전체 시스템 구조와 적용된 주요 디자인 패턴에 대해 상세히 기술합니다. 본 프로젝트는 방치형 RPG의 특성에 맞춘 무한한 수치 확장성과 유지보수성을 극대화하기 위해 설계되었습니다.

---

## 1. 📂 모듈 개요 (Module Overview)

SwordLordMaker는 **도메인 주도 설계(DDD)** 사상을 기반으로 한 4계층 아키텍처를 채택하고 있습니다. 각 모듈은 독립적인 책임을 가지며, 데이터의 흐름과 의존성 방향이 명확하게 정의되어 있어 대규모 기능 추가 시에도 코드 간의 결합도를 낮게 유지합니다.

- **핵심 목표**: 방치형 RPG의 무한한 스케일링 지원, UI와 비즈니스 로직의 완벽한 분리, 테스트 가능한 구조 확보.

---

## 2. 🏗️ 아키텍처 및 상호작용 (Architecture & Interactions)

### DDD 4계층 구조

프로젝트는 다음 4가지 계층으로 구분됩니다:

1.  **UI Layer (Presentation)**
    - 사용자 인터페이스 요소 (`CurrencyUI`, `UpgradeUI`, `StageUI` 등)를 담당합니다.
    - **규칙**: 비즈니스 로직을 포함하지 않으며, 오직 `Manager Layer`의 이벤트를 구독하여 화면을 갱신하거나 사용자 입력을 Manager로 전달합니다.

2.  **Manager Layer (Application)**
    - 애플리케이션의 유스케이스를 구현합니다 (`CurrencyManager`, `UpgradeManager` 등).
    - **규칙**: 싱글톤 패턴을 사용하여 전역적으로 접근 가능하며, `Data`와 `Repository`를 조정하여 비즈니스 로직을 수행하고 결과(이벤트)를 UI에 알립니다.

3.  **Repository Layer (Infrastructure)**
    - 데이터의 영속화를 담당합니다 (`CurrencyRepository`, `UpgradeRepository` 등).
    - **규칙**: `BGDatabase`와 연동하여 데이터를 저장하고 불러옵니다. `Data Layer`에 정의된 인터페이스를 구현하여 인프라의 세부 사항을 숨깁니다.

4.  **Data Layer (Domain)**
    - 순수 데이터 클래스(POCO) 및 인터페이스를 포함합니다 (`PlayerStat`, `SwordStat` 등).
    - **규칙**: `record` 타입을 사용하여 불변성을 보장하며, 전투 및 재화 수치에는 `BigInteger`를 사용하여 무한 스케일링을 지원합니다.

### 계층 간 의존성 방향
**UI Layer → Manager Layer → Repository Layer → Data Layer**
의존성은 상위 계층에서 하위 계층으로만 흐르며, 하위 계층은 상위 계층의 존재를 알지 못합니다 (이벤트를 통한 간접 통신 제외).

---

## 3. 📝 상세 코드 분석 (Detailed Code Analysis)

### 핵심 디자인 패턴

| 패턴 | 설명 및 적용 예시 |
| :--- | :--- |
| **Singleton** | `DontDestroySingleton<T>`을 상속받아 모든 Manager 클래스를 전역에서 유일하게 관리합니다. |
| **Observer** | C# `Action` 이벤트를 사용하여 Manager의 상태 변화를 UI에 전달합니다. (예: `OnCurrencyChanged`) |
| **Strategy** | 비행 검 시스템(`BaseFlyingSword`, `BaseSwordController`)에서 궤도 타입(Adel, Hypo, Pixel)을 런타임에 전환합니다. |
| **Repository** | 데이터 접근 로직을 캡슐화하여 비즈니스 로직과 저장소 로직을 분리합니다. |
| **Object Pool** | `EnemySpawner`와 `DamageFloaterManager`에서 빈번한 생성/파괴를 방지하기 위해 `UnityEngine.Pool`을 사용합니다. |
| **Record** | `PlayerStat`, `SwordStat` 등 데이터 모델에 `record`를 사용하여 불변(Immutable) 상태를 보장합니다. |
| **Factory** | `UpgradeRepository` 등에서 데이터 로드 시 객체 생성 로직을 팩토리 방식으로 처리합니다. |

---

## 4. 💡 설계 의도 및 구현 이유 (Design Rationale)

1.  **DDD 4계층 구조 선택 이유**
    - 유지보수성: 기능별로 계층이 분리되어 있어 특정 기능 수정 시 영향 범위를 최소화할 수 있습니다.
    - 확장성: 새로운 시스템 추가 시 기존 코드의 수정 없이 새로운 모듈을 계층에 맞춰 추가하기 용이합니다.

2.  **Singleton 사용 이유**
    - 전역 상태 관리: 게임 전체에서 공유되는 재화, 스테이지, 플레이어 스탯 정보를 어디서든 일관되게 접근하기 위함입니다.

3.  **이벤트 기반 통신 사용 이유**
    - 결합도 감소: Manager가 UI를 직접 참조하지 않음으로써 순환 참조를 방지하고, UI 시스템이 교체되어도 비즈니스 로직에 영향을 주지 않습니다.

4.  **Record 및 BigInteger 사용 이유**
    - 불변성 보장: `record`를 통해 의도치 않은 데이터 오염을 방지합니다.
    - 무한 스케일링: `double`이나 `float`의 한계를 넘어선 방치형 게임의 기하급수적인 수치 증가를 지원합니다.

---

## 5. 🎮 사용 가이드 (Usage Guide within Unity)

### 새로운 기능 추가 시 워크플로우
1.  **Data Layer**: 데이터 구조를 `record` 또는 클래스로 정의하고 `BigInteger`가 필요한 필드를 설정합니다.
2.  **Repository Layer**: `BGDatabase` 연동 로직을 작성하고 인터페이스를 정의합니다.
3.  **Manager Layer**: `DontDestroySingleton<T>`을 상속받고, `Initialize()`에서 Repository를 초기화합니다. 필요한 이벤트를 선언합니다.
4.  **UI Layer**: Manager의 이벤트를 `OnEnable`에서 구독하고 `OnDisable`에서 해제하며, 화면을 갱신합니다.

### 이벤트 발행 및 구독 패턴
```csharp
// Manager에서의 발행
public event Action<BigInteger> OnStatChanged;
private void UpdateStat(BigInteger newValue) {
    OnStatChanged?.Invoke(newValue);
}

// UI에서의 구독
private void OnEnable() {
    SomeManager.Instance.OnStatChanged += UpdateUI;
}
```

### BigInteger 사용 주의사항
- **저장**: `BGDatabase`는 `BigInteger`를 직접 지원하지 않으므로 `string` 타입으로 저장 후, 로드 시 `BigInteger.Parse()`를 통해 변환해야 합니다.
- **연산**: `float`와 곱셈 시 정밀도 손실을 방지하기 위해 정수 배율(예: 1000 곱한 후 나누기) 처리를 권장합니다.

---

## 6. ⚠️ 크리티컬 리뷰 및 개선점 (Critical Review)

### 현재 발견된 문제점
1.  **CritDamage 타입 미스매치**: `SwordStat.CritDamageMultiplier`가 `float`로 되어 있어, 공격력이 매우 높아질 경우 치명타 데미지 계산 시 `double` 캐스팅 과정에서 정밀도 손실이 발생할 수 있습니다. 이를 `BigInteger` 기반 보너스 시스템으로 통일해야 합니다.
2.  **보스 오브젝트 풀 버그**: `EnemySpawner`에서 보스는 풀링하지 않고 Destroy 처리하고 있으나, 다른 시스템(예: 이펙트)에서 보스 객체를 풀에 반환하려고 시도할 경우 런타임 에러가 발생할 가능성이 있습니다.
3.  **초기화 순서 문제**: 싱글톤 매니저들이 서로를 참조하는 경우 (`PlayerStatManager`가 `UpgradeManager`를 참조 등), `Awake` 시점의 초기화 순서에 따라 `NullReferenceException`이 발생할 수 있습니다. `EnsureInitialized()`와 같은 방어 로직이 더 강화되어야 합니다.
4.  **NullReference 방어 미흡**: 일부 매니저에서 `_currency?.`와 같이 null 조건부 연산자를 사용 중이나, 데이터 로드 실패 시의 근본적인 예외 처리 로직이 더 보강될 필요가 있습니다.

---

**작성자**: Antigravity Technical Writer
**최종 수정일**: 2026-01-16
