# Player 시스템 기술 문서 (Player System Documentation)

## 1. 📂 모듈 개요 (Module Overview)

Player 시스템은 `SwordLordMaker` 프로젝트에서 플레이어의 상태(Stat), 생존(Health), 이동(Movement), 애니메이션(Animation) 및 데이터 영속화(Persistence)를 담당하는 핵심 모듈입니다. 이 시스템은 무한 성장을 지원하기 위해 `BigInteger`를 사용하며, DDD(Domain-Driven Design) 4계층 구조를 준수하여 설계되었습니다.

### 주요 기능
- **상태 관리**: 레벨, 경험치, 기본 스탯(체력, 이동속도) 관리 및 레벨업 로직 처리
- **생존 시스템**: 데미지 처리, 체력 회복, 사망 및 부활 프로세스 관리
- **이동 및 카메라**: 쿼터뷰(Quarter-View) 기반의 캐릭터 이동 및 부하가 적은 부드러운 카메라 추적
- **시각적 피드백**: 이동 속도 및 상태에 따른 애니메이션 제어 및 UI 업데이트
- **데이터 영속화**: BGDatabase와 PlayerPrefs를 활용한 플레이어 정보 저장 및 로드

---

## 2. 🏗️ 아키텍처 및 상호작용 (Architecture & Interactions)

Player 시스템은 의존성 분리와 유지보수성을 위해 DDD 4계층 구조와 다양한 디자인 패턴을 적용하였습니다.

### DDD 4계층 구조 적용
1.  **Data Layer (Domain)**: `PlayerStat` (불변 데이터 record)
2.  **Repository Layer (Infrastructure)**: `PlayerStatRepository` (BGDatabase 연동)
3.  **Manager Layer (Application)**: `PlayerStatManager`, `PlayerSessionManager` (비즈니스 로직 및 싱글톤)
4.  **UI Layer (Presentation)**: `PlayerHealthUI` (사용자 인터페이스)

### 디자인 패턴
- **Singleton**: `PlayerStatManager`, `PlayerSessionManager` (전역 접근 보장)
- **Observer (Event)**: `OnLevelUp`, `OnExpChanged`, `OnHealthChanged`, `OnDeath` 등의 이벤트를 통한 객체 간 느슨한 결합
- **Repository**: 데이터 저장소 로직을 추상화하여 비즈니스 로직과 분리
- **Record**: C#의 `record` 타입을 사용하여 플레이어 스탯의 불변성(Immutability) 보장

### 상호작용 흐름
- **Upgrade 연동**: `PlayerHealth`와 `PlayerMovement`는 `UpgradeManager`의 이벤트를 구독하여 강화 발생 시 실시간으로 스탯을 갱신합니다.
- **Game 관리**: `PlayerHealth`는 `Start` 시점에 `GameManager`에 자신을 등록하여 게임 흐름(사망/부활)의 제어를 받습니다.
- **데이터 로드**: `PlayerStatManager`는 초기화 시 `PlayerSessionManager`로부터 이름을 가져와 `PlayerStatRepository`를 통해 데이터를 복구합니다.

---

## 3. 📝 상세 코드 분석 (Detailed Code Analysis)

### 3.1. 데이터 및 영속화 (Data & Repository)
- **`PlayerStat.cs`**: 플레이어의 기초 데이터를 담는 `record` 타입입니다. `BigInteger`를 사용하여 체력의 무한 스케일링을 지원하며, `with` 키워드를 통한 불변 갱신을 지원합니다.
- **`PlayerStatRepository.cs`**: BGDatabase의 `PlayerStat` 테이블과 통신합니다. `BigInteger`를 직접 지원하지 않는 DB 특성에 맞춰 `string`으로 변환하여 저장/로드하는 어댑터 역할을 수행합니다.

### 3.2. 비즈니스 로직 (Manager)
- **`PlayerStatManager.cs`**: 경험치 획득 및 레벨업 로직을 담당합니다. 레벨업 시 필요한 경험치를 지수적으로 계산하며, 상태 변경 시 이벤트를 발행하여 UI와 다른 시스템에 알립니다.
- **`PlayerSessionManager.cs`**: 세션 기반의 플레이어 이름을 관리합니다. `PlayerPrefs`를 사용하여 마지막 플레이어 이름을 저장하며, 이를 통해 멀티 세션 대응의 기초를 마련합니다.

### 3.3. 실행 로직 (Logic & Component)
- **`PlayerHealth.cs`**: `IDamageable` 인터페이스를 구현합니다. `PlayerStatManager`의 기본 체력과 `UpgradeManager`의 보너스 체력을 합산하여 최종 체력을 계산합니다. 사망 시 `OnDeath` 이벤트를 발생시켜 게임 규칙을 트리거합니다.
- **`PlayerMovement.cs`**: `CharacterController`를 사용하여 이동을 처리합니다. 쿼터뷰 시점에 맞춰 입력 방향을 45도 회전(Yaw)시키며, `UpgradeManager`로부터 이동 속도 보너스를 실시간으로 반영합니다.
- **`PlayerAnimation.cs`**: `PlayerMovement`의 속도 값을 읽어 애니메이션의 `Speed` 파라미터를 업데이트하고, 사망/부활 상태를 `Animator`와 동기화합니다.
- **`QuarterViewCamera.cs`**: 타겟(플레이어)을 일정한 오프셋에서 추적합니다. `LateUpdate`에서 `Lerp`를 사용하여 프레임 드랍 시에도 부드러운 추적 성능을 유지합니다.

### 3.4. 사용자 인터페이스 (UI)
- **`PlayerHealthUI.cs`**: `PlayerHealth`의 이벤트를 구독하여 HP 바를 갱신합니다. `BigInteger` 연산 시 정밀도 유지를 위해 부동소수점 변환 전 정수 배율 연산을 수행합니다.

---

## 4. 💡 설계 의도 및 구현 이유 (Design Rationale)

1.  **`record` 타입 사용**: 스탯 데이터의 원자성을 보장하기 위함입니다. 멀티스레드 환경이나 복잡한 상태 변경 로직에서 데이터가 예기치 않게 변경되는 것을 방지하고, `with` 식을 통해 변경된 부분만 안전하게 교체할 수 있습니다.
2.  **`BigInteger` 사용**: 방치형 RPG 특성상 수치가 기하급수적으로 증가합니다. `double`이나 `float`의 정밀도 한계를 넘어서는 무한 성장을 구현하기 위해 모든 전투/재화 관련 수치에 `BigInteger`를 적용했습니다.
3.  **이벤트 기반 통신 (Observer Pattern)**: 시스템 간의 직접적인 참조를 최소화하여 결합도를 낮추었습니다. 예를 들어, `PlayerStatManager`는 UI가 존재하는지 알 필요 없이 이벤트만 발행하며, 이는 코드의 유연성과 확장성을 높여줍니다.
4.  **`UpgradeManager` 실시간 구독**: 플레이어가 강화 버튼을 누르는 즉시 게임 세션을 재시작하지 않고도 체력이나 이동 속도가 반영되도록 하여 사용자 경험(UX)을 향상시켰습니다.

---

## 5. 🎮 사용 가이드 (Usage Guide within Unity)

### 5.1. Inspector 설정 가이드

- **PlayerHealth**
    - `Player Animation`: 캐릭터의 `PlayerAnimation` 컴포넌트 할당
    - `Player Movement`: 캐릭터의 `PlayerMovement` 컴포넌트 할당
- **PlayerHealthUI**
    - `Player Health`: 씬 내의 `Player` 오브젝트 혹은 `PlayerHealth` 컴포넌트 할당
    - `Health Fill Image`: UI HP 바의 `Image` (Fill Method: Horizontal 권장) 할당
- **PlayerMovement**
    - `Rotation Speed`: 회전 민감도 (권장: 10~15)
    - `Quarter View Yaw`: 카메라 시점과 일치하도록 설정 (권장: 45)
- **PlayerAnimation**
    - `Animator`: 캐릭터의 `Animator` 컴포넌트 할당
    - `Speed Param / IsDead Param`: Animator Controller에 설정된 파라미터 이름과 일치 필수
- **QuarterViewCamera**
    - `Target`: 플레이어의 `Transform` 할당
    - `Position Offset`: 카메라의 상대적 위치 (기본값: -10, 9, -10)
    - `Rotation`: 카메라 각도 (기본값: 30, 45, 0)

---

## 6. ⚠️ 크리티컬 리뷰 및 개선점 (Critical Review)

### 6.1. 잠재적 위험 요소
- **NullReference 가능성**: `PlayerHealthUI`나 `PlayerHealth`에서 `Awake`/`Start` 시점에 `Manager`들이 인스턴스화되지 않았을 경우에 대한 폴백은 있으나, 초기 로딩 순서에 따른 예외 발생 가능성이 존재합니다.
- **Update 내 연산**: `PlayerMovement`와 `PlayerAnimation`이 매 프레임 연산을 수행합니다. 현재는 단순 연산이나, 로직이 복잡해질 경우 성능 저하의 원인이 될 수 있습니다.

### 6.2. 개선 제안
- **초기화 순서 명시**: `Unity Execution Order` 설정을 통해 Manager 계층이 항상 먼저 초기화되도록 강제하는 것이 안전합니다.
- **객체 풀링 고려**: 현재는 단일 플레이어 시스템이나, 추후 소환수나 멀티 캐릭터 시스템 도입 시 `IDamageable`을 처리하는 로직을 통합 관리자로 위임하는 것이 좋습니다.
- **BigInteger 최적화**: `BigInteger` 연산은 기본 자료형보다 무거우므로, UI 업데이트 등에서는 일정 주기(Coroutines)로 갱신하거나 값이 크게 변했을 때만 연산하도록 최적화할 수 있습니다.
- **상태 머신(FSM) 도입**: 현재는 `bool _isDead` 등으로 상태를 관리하지만, 상태가 늘어날 경우(스턴, 변신 등) `StateMachine` 패턴을 도입하여 복잡도를 관리할 것을 권장합니다.
