# UI(공통) 시스템 기술 문서

## 1. 📂 모듈 개요 (Module Overview)
SwordLordMaker 프로젝트의 공통 UI 시스템은 플레이어의 상태 정보, 현재 진행 중인 스테이지 정보, 그리고 보스 전투 진입과 같은 핵심 게임 흐름을 사용자에게 시각적으로 전달하고 상호작용을 처리하는 모듈입니다. 이 시스템은 Unity의 UI Toolkit이 아닌 UGUI(Unity GUI)와 TextMesh Pro를 기반으로 구축되었습니다.

### 주요 컴포넌트
- **BossSpawnUI**: 보스 전투 진입 버튼의 상태 관리 및 호출을 담당합니다.
- **MainSceneLoader**: 게임의 메인 씬으로 전환하는 간단한 유틸리티 기능을 제공합니다.
- **PlayerProfileUI**: 플레이어의 닉네임, 레벨, 경험치(EXP) 진행도를 표시합니다.
- **StageUI**: 현재 스테이지의 번호와 이름을 파싱하여 화면에 표시합니다.

## 2. 🏗️ 아키텍처 및 상호작용 (Architecture & Interactions)

### UI Layer 독립성
본 프로젝트는 DDD(Domain-Driven Design) 4계층 구조를 따르며, UI 시스템은 최상위인 **UI Layer (Presentation)**에 해당합니다. UI 클래스들은 데이터 계산이나 게임 로직을 직접 수행하지 않으며, 오직 Manager Layer로부터 데이터를 받아 화면에 출력하거나 사용자 입력을 Manager로 전달하는 역할만 수행합니다.

### Manager 이벤트 구독 (Observer 패턴)
UI 시스템은 **Observer 디자인 패턴**을 적극적으로 활용합니다. 각 UI 컴포넌트는 `Start` 또는 `OnEnable` 시점에 필요한 Manager의 이벤트를 구독하고, Manager의 상태가 변경될 때 발생하는 이벤트를 통해 UI를 자동 갱신합니다.

- **상호작용 흐름**:
  1. `Manager`에서 데이터 변경 발생 (예: 경험치 획득)
  2. `Manager`가 등록된 `Action` 또는 `delegate` 이벤트 전송
  3. 이를 구독하고 있던 `UI` 컴포넌트의 핸들러 함수 실행
  4. UI 요소(Text, Slider 등) 갱신

## 3. 📝 상세 코드 분석 (Detailed Code Analysis)

### BossSpawnUI.cs
보스 전투 시스템과의 접점 역할을 합니다.
- **기능**: 보스 소환 가능 여부에 따라 버튼의 상호작용(`interactable`) 상태와 텍스트를 변경합니다.
- **주요 이벤트**: `StageManager.Instance.OnBossSpawned`, `StageManager.Instance.OnStageStarted`를 구독하여 보스 전투 중에는 버튼을 비활성화하고, 새로운 스테이지가 시작되면 다시 활성화합니다.
- **동작**: 버튼 클릭 시 `StageManager.Instance.SpawnBoss()`를 호출하여 실제 보스 소환 로직을 트리거합니다.

### PlayerProfileUI.cs
플레이어의 성장 지표를 시각화합니다.
- **기능**: 플레이어 ID, 현재 레벨, 경험치 비율을 표시합니다.
- **주요 데이터**: `PlayerSessionManager`에서 닉네임을, `PlayerStatManager`에서 레벨 및 경험치 데이터를 가져옵니다.
- **동작**: 경험치 변경(`OnExpChanged`) 및 레벨업(`OnLevelUp`) 이벤트를 수신하여 슬라이더(`Slider`)의 value와 텍스트를 업데이트합니다.

### StageUI.cs
스테이지 진행 상황을 사용자에게 인지시킵니다.
- **기능**: "1-10 고요한 숲"과 같은 형태의 전체 스테이지 이름을 "1-10"과 "고요한 숲"으로 분리하여 각각 다른 UI 요소에 배치합니다.
- **동작**: `StageManager.Instance.OnStageStarted` 이벤트 수신 시 스테이지 이름을 파싱(`ParseStageName`)하여 화면에 출력합니다.

### MainSceneLoader.cs
씬 관리 유틸리티입니다.
- **기능**: `SceneManager.LoadScene`을 호출하여 지정된 "MainScene"으로 씬을 전환합니다.
- **용도**: 주로 시작 화면이나 결과 화면에서 게임 본편으로 돌아갈 때 사용됩니다.

## 4. 💡 설계 의도 및 구현 이유 (Design Rationale)

### 프레젠테이션 책임 분리
UI 클래스에서 비즈니스 로직을 제거함으로써, 게임 규칙이 변경되더라도 UI 코드를 수정할 필요가 없도록 설계되었습니다. 예를 들어, 경험치 계산 공식이 바뀌더라도 `PlayerProfileUI`는 단순히 전달받은 값만 표시하므로 코드 수정을 최소화할 수 있습니다.

### 성능 및 효율성
매 프레임마다 `Update` 메서드에서 데이터를 체크(Polling)하는 대신, 이벤트가 발생할 때만 UI를 갱신하도록 구현하여 CPU 자원을 절약하고 프레임 드랍을 방지했습니다.

### 문자열 파싱 로직의 UI 포함
`StageUI`에서 스테이지 이름을 분리하는 로직을 UI 계층에 둔 이유는, 데이터 계층에서는 "전체 이름"을 하나의 데이터로 관리하되 이를 어떻게 보여줄지는 전적으로 프레젠테이션 영역의 결정이기 때문입니다.

## 5. 🎮 사용 가이드 (Usage Guide within Unity)

### Inspector 설정 가이드
모든 UI 스크립트는 MonoBehaviour를 상속받으므로 프리팹 또는 씬의 게임 오브젝트에 컴포넌트로 부착하여 사용합니다.

1. **참조 할당**: 각 스크립트의 `[SerializeField]` 필드(텍스트, 슬라이더, 버튼 등)에 해당하는 Hierarchy 상의 UI 오브젝트를 드래그 앤 드롭으로 할당해야 합니다.
2. **이벤트 연결**: `BossSpawnUI`와 `MainSceneLoader`의 경우 버튼의 `OnClick` 이벤트에 스크립트의 공용 메서드를 연결하거나, 내부적으로 `AddListener`를 통해 연결되도록 구성되어 있습니다.

## 6. ⚠️ 크리티컬 리뷰 및 개선점 (Critical Review)

### NullReferenceException 방지
현재 코드에서 `Instance` 참조 시 널 체크를 수행하고는 있으나, Inspector에서 UI 요소(Text, Slider 등)를 할당하지 않았을 경우에 대한 방어 로직이 더 강화될 필요가 있습니다.

### UI 갱신 최적화
방치형 게임의 특성상 경험치 획득이 매우 빈번하게 발생할 수 있습니다. `OnExpChanged`가 매 초 수십 번씩 호출될 경우 UI 갱신 비용이 누적될 수 있으므로, 일정 시간 간격으로 UI를 모아서 갱신하거나 `DOTween` 등을 활용하여 부드러운 연출과 함께 갱신 빈도를 조절하는 최적화가 권장됩니다.

### 하드코딩된 문자열
"보스 전투 중", "보스입장", "MainScene" 등의 문자열이 코드 내에 직접 입력되어 있습니다. 이는 향후 다국어 지원(Localization)이나 프로젝트 구조 변경 시 유지보수를 어렵게 만드는 요인이므로, 별도의 상수 클래스나 로컬라이징 테이블을 사용하는 방식으로 개선이 필요합니다.
