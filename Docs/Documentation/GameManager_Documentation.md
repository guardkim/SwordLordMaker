# GameManager 및 공통 시스템 기술 문서

## 1. 📂 모듈 개요 (Module Overview)

SwordLordMaker의 핵심 흐름과 객체 간의 공통 상호작용을 정의하는 시스템입니다. 이 모듈은 게임의 전역 상태 관리, 플레이어의 생명주기 제어, 그리고 데미지 처리를 위한 규약을 제공합니다.

- **GameManager**: 게임의 전체적인 흐름(사망, 부활, 스테이지 전환 등)을 조율하는 중앙 컨트롤러입니다.
- **IDamageable**: 전투 시스템에서 데미지를 입을 수 있는 모든 실체(Entity)가 구현해야 하는 표준 인터페이스입니다.
- **FirstScript**: 게임 시작 시 필요한 초기화 로직을 담당하는 진입점 스크립트입니다.

---

## 2. 🏗️ 아키텍처 및 상호작용 (Architecture & Interactions)

### 디자인 패턴 적용
- **Singleton 패턴 (GameManager)**: `DontDestroySingleton<T>`을 상속받아 구현되었습니다. 게임 전체에서 단 하나만 존재하며, 어느 곳에서든 `GameManager.Instance`를 통해 접근할 수 있는 전역 접근점을 제공합니다.
- **Observer 패턴 (이벤트 기반 통신)**: `Action` 기반 이벤트를 사용하여 시스템 간의 결합도를 낮췄습니다. `GameManager`는 자신의 상태 변화를 이벤트를 통해 전파하며, UI나 스테이지 시스템은 이를 구독하여 반응합니다.
- **Interface 패턴 (IDamageable)**: 구체적인 클래스 타입에 의존하지 않고 '데미지를 입을 수 있는 기능' 자체에 의존하도록 설계하여 다형성을 보장합니다.

### 시스템 간 상호작용
1. `PlayerHealth`가 생성될 때 `GameManager.RegisterPlayer()`를 호출하여 자신을 등록합니다.
2. 플레이어 사망 시 `GameManager`가 이를 감지하고 `OnPlayerDeath` 이벤트를 발생시킵니다.
3. 5초의 대기 시간 후, `GameManager`는 스테이지 리셋을 요청(`OnRequestStageRestart`)하고 플레이어를 부활시킵니다.

---

## 3. 📝 상세 코드 분석 (Detailed Code Analysis)

### GameManager.cs
- **핵심 필드**:
    - `RESPAWN_DELAY`: 플레이어 사망 후 부활까지의 대기 시간(5초)입니다.
    - `RESPAWN_STAGE_ID`: 사망 시 되돌아갈 스테이지 번호(1번)입니다.
- **주요 이벤트**:
    - `OnPlayerDeath`: 플레이어 사망 시 발생합니다.
    - `OnPlayerRevive`: 플레이어 부활 완료 시 발생합니다.
    - `OnRequestStageRestart<int>`: 특정 스테이지로의 재시작을 요청할 때 발생합니다.
- **프로세스**: 플레이어 등록 시 `OnDeath` 이벤트를 구독하며, 사망 핸들러에서 코루틴(`RespawnSequence`)을 통해 부활 및 스테이지 초기화 시퀀스를 실행합니다.

### IDamageable.cs
- **메서드**: `void TakeDamage(BigInteger damage, bool isCrit)`
- **특징**: `BigInteger` 타입을 사용하여 방치형 게임 특유의 무한한 데미지 스케일링을 지원합니다. 크리티컬 여부를 함께 전달하여 데미지 플로터 등의 연출에 활용할 수 있게 합니다.

### FirstScript.cs
- **역할**: 현재는 기본적인 Unity 생명주기 메서드만 포함된 상태로, 향후 전역 설정 로드나 초기화 순서 제어를 위한 진입점으로 활용될 수 있는 구조를 가지고 있습니다.

---

## 4. 💡 설계 의도 및 구현 이유 (Design Rationale)

- **전역 상태 관리의 효율성**: 게임의 흐름은 UI, 전투, 스테이지 등 여러 시스템에 영향을 미칩니다. 이를 `GameManager`라는 싱글톤 객체로 중앙 집중화하여 상태 관리의 일관성을 유지하고 접근성을 높였습니다.
- **확장성을 고려한 데미지 시스템**: `IDamageable` 인터페이스를 사용함으로써, 향후 새로운 타입의 적이나 파괴 가능한 오브젝트가 추가되더라도 기존의 전투 로직을 수정할 필요 없이 인터페이스만 구현하면 즉시 시스템에 통합될 수 있습니다.
- **비동기 흐름 제어**: 사망 후 부활까지의 지연 시간 처리를 코루틴으로 구현하여, 메인 로직을 방해하지 않으면서 자연스러운 시간 흐름을 제어합니다.

---

## 5. 🎮 사용 가이드 (Usage Guide within Unity)

### GameManager 설정
1. Hierarchy 창에서 빈 GameObject를 생성하고 이름을 `GameManager`로 설정합니다.
2. `GameManager.cs` 스크립트를 해당 오브젝트에 추가합니다.
3. 별도의 Inspector 설정은 필요하지 않으며, 씬 전환 시에도 자동으로 유지됩니다.

### IDamageable 구현 방법
1. 데미지를 입어야 하는 클래스(예: `Enemy`, `PlayerHealth`) 선언 시 `IDamageable`을 상속받습니다.
2. `TakeDamage` 메서드를 구현하여 체력 감소 및 사망 로직을 작성합니다.

---

## 6. ⚠️ 크리티컬 리뷰 및 개선점 (Critical Review)

- **잠재적 NullReference 가능성**: `GameManager`가 `PlayerHealth`를 등록받기 전이나, 부활 대기 시간 도중에 플레이어 오브젝트가 예기치 않게 파괴될 경우 `_playerHealth` 참조에 의한 런타임 에러가 발생할 수 있습니다. 각 참조 사용 시점에 추가적인 Null 체크가 권장됩니다.
- **초기화 순서 의존성**: `RegisterPlayer`가 호출되어야만 시스템이 정상 작동하므로, 플레이어 생성 시점과 매니저 활성화 시점 간의 순서 보장이 중요합니다.
- **하드코딩된 값**: 부활 지연 시간(5s)과 재시작 스테이지 ID(1)가 상수로 선언되어 있습니다. 게임 밸런싱을 위해 이를 Inspector에서 수정 가능한 필드로 노출하거나 데이터베이스와 연동하는 것이 좋습니다.
- **FirstScript의 미활용**: 현재 로직이 비어 있는 상태이므로, 명확한 초기화 목적이 없다면 제거하거나 `GameManager`로 로직을 통합하는 것이 코드 클린업 측면에서 유리합니다.
