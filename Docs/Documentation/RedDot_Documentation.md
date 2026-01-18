# [파일명: RedDot_Documentation.md]

## 1. 📂 모듈 개요 (Module Overview)

이 모듈은 방치형 RPG 게임에서 사용되는 RedDot(빨간 점 알림) 시스템을 제공합니다. 트리 구조로 상태를 관리하며, 하위 노드가 활성화되면 상위 노드도 자동으로 활성화됩니다.

**주요 스크립트:**
- `RedDotKey.cs` (Enum) - RedDot 식별 키
- `IRedDotCondition.cs` (Interface) - 조건 체크 인터페이스
- `RedDotNode.cs` (Data) - 트리 노드 클래스
- `RedDotManager.cs` (Manager Layer) - RedDot 상태 관리 싱글톤
- `RedDotView.cs` (UI Layer) - RedDot 표시 UI 컴포넌트
- `UpgradeRedDotCondition.cs` (Condition) - 강화 가능 조건 구현

---

## 2. 🏗️ 아키텍처 및 상호작용 (Architecture & Interactions)

### Script Flow
```
RedDotManager (Singleton, Manager Layer)
    └─> RedDotNode (트리 구조)
        └─> IRedDotCondition (조건 체크)
            └─> UpgradeManager, CurrencyManager 이벤트 구독
    └─> RedDotView (UI Layer)
        └─> RedDotManager.Subscribe() 호출
```

### Dependencies
- **외부 시스템:**
  - `DontDestroySingleton<T>`: 싱글톤 베이스 클래스
  - `CurrencyManager`: 골드 변경 이벤트 구독
  - `UpgradeManager`: 강화 이벤트 구독
  - `DOTween`: RedDot 애니메이션

### Tree Structure
```
MainMenu
├── Upgrade
│   ├── UpgradePlayer
│   │   ├── UpgradePlayerHealth ◀ 조건: 골드 >= 강화비용
│   │   └── UpgradePlayerMoveSpeed ◀ 조건: 골드 >= 강화비용
│   └── UpgradeSword
│       ├── UpgradeSwordAttackDamage ◀ 조건
│       ├── UpgradeSwordCooldown ◀ 조건
│       ├── UpgradeSwordMoveSpeed ◀ 조건
│       ├── UpgradeSwordCritChance ◀ 조건
│       └── UpgradeSwordCritDamage ◀ 조건
├── Shop
│   ├── ShopFreeReward
│   └── ShopDailyDeal
├── Inventory
│   └── InventoryNewItem
├── Quest
│   ├── QuestDaily
│   ├── QuestWeekly
│   └── QuestAchievement
└── Mail
    └── MailReward
```

### 상태 전파 흐름
```
┌────────────────────────────────────────────────────────┐
│ 1. CurrencyManager.OnCurrencyChanged 발생              │
└───────────────────────┬────────────────────────────────┘
                        ▼
┌────────────────────────────────────────────────────────┐
│ 2. UpgradeRedDotCondition.OnConditionChanged 발행      │
└───────────────────────┬────────────────────────────────┘
                        ▼
┌────────────────────────────────────────────────────────┐
│ 3. RedDotNode.Evaluate() 호출                          │
│    - CheckSelfConditions() → 자신의 조건 체크          │
│    - CheckChildrenActive() → 자식 노드 활성화 여부     │
└───────────────────────┬────────────────────────────────┘
                        ▼
┌────────────────────────────────────────────────────────┐
│ 4. 상태 변경 시 부모 노드에 전파 (재귀)                │
│    UpgradeSwordAttackDamage → UpgradeSword → Upgrade  │
└───────────────────────┬────────────────────────────────┘
                        ▼
┌────────────────────────────────────────────────────────┐
│ 5. RedDotNode.OnStateChanged 이벤트 발행               │
└───────────────────────┬────────────────────────────────┘
                        ▼
┌────────────────────────────────────────────────────────┐
│ 6. RedDotView.HandleRedDotStateChanged() 호출          │
│    - _redDotObject 활성화/비활성화                     │
│    - DOTween 애니메이션 재생                           │
└────────────────────────────────────────────────────────┘
```

---

## 3. 📝 상세 코드 분석 (Detailed Code Analysis)

### RedDotKey (Enum)
- **기능:** RedDot 노드 식별 키
- **구조:**
  - `None (0)` - 미지정
  - `MainMenu (1)` - 메인 메뉴 루트
  - `Upgrade (100~)` - 강화 관련
  - `Shop (200~)` - 상점 관련
  - `Inventory (300~)` - 인벤토리 관련
  - `Quest (400~)` - 퀘스트 관련
  - `Mail (500~)` - 우편함 관련

### IRedDotCondition (Interface)
- **기능:** RedDot 조건 체크 인터페이스
- **프로퍼티:** `Key` - 조건이 적용되는 노드 키
- **메서드:** `CheckCondition()` - 조건 충족 여부 반환
- **이벤트:** `OnConditionChanged` - 조건 변경 시 발행

### RedDotNode
- **기능:** 트리 구조의 노드
- **주요 필드:**
  - `_key` → RedDotKey
  - `_children` → 자식 노드 리스트
  - `_conditions` → 조건 리스트
  - `_parent` → 부모 노드
  - `_isActive` → 활성화 상태
- **메서드:**
  - `AddChild(RedDotNode)` - 자식 노드 추가
  - `AddCondition(IRedDotCondition)` - 조건 추가
  - `Evaluate()` - 상태 재평가 (조건 + 자식 상태)
  - `ForceEvaluate()` - 전체 트리 강제 평가
- **이벤트:** `OnStateChanged(RedDotKey, bool)`

### RedDotManager (Manager Layer)
- **디자인 패턴:** Singleton (DontDestroySingleton 상속)
- **주요 필드:**
  - `_nodes` → 키-노드 딕셔너리
  - `_registeredConditions` → 등록된 조건 리스트
- **초기화 순서:**
  1. `BuildTree()` - 트리 구조 생성
  2. `RegisterConditions()` - 조건 등록
  3. `EvaluateAll()` - 전체 평가
- **핵심 메서드:**
  - `RegisterCondition(RedDotKey, IRedDotCondition)` - 조건 등록
  - `IsActive(RedDotKey)` - 활성화 여부 조회
  - `Subscribe(RedDotKey, Action)` - 상태 변경 구독
  - `Unsubscribe(RedDotKey, Action)` - 구독 해제
  - `Evaluate(RedDotKey)` - 특정 노드 평가
  - `EvaluateAll()` - 전체 트리 평가
- **이벤트:** `OnRedDotStateChanged(RedDotKey, bool)`

### RedDotView (UI Layer)
- **기능:** RedDot 이미지 표시 UI 컴포넌트
- **주요 필드:**
  - `_key` → 구독할 RedDotKey
  - `_redDotObject` → 활성화할 GameObject
  - `_redDotImage` → RedDot 이미지 (선택)
  - `_useAnimation` → 애니메이션 사용 여부
  - `_usePulse` → 펄스 애니메이션 사용 여부
- **애니메이션:**
  - 활성화: Scale 0 → 1 (OutBack)
  - 비활성화: Scale 1 → 0 (InBack)
  - 펄스: Scale 1 ↔ 1.2 (반복)
- **메서드:**
  - `SetKey(RedDotKey)` - 런타임에 키 변경
  - `ForceRefresh()` - 강제 갱신

### UpgradeRedDotCondition
- **기능:** 강화 가능 여부 조건
- **조건 로직:**
  1. 최대 레벨이 아님
  2. 현재 골드 >= 강화 비용
- **이벤트 구독:**
  - `CurrencyManager.OnCurrencyChanged` - 골드 변경
  - `UpgradeManager.OnUpgraded` - 강화 완료

---

## 4. 💡 설계 의도 및 구현 이유 (Design Rationale)

### 1. 왜 트리 구조를 사용했는가?
- **목적:** 자식 노드 활성화 시 부모 자동 활성화
- **효과:** 강화버튼 RedDot → 강화팝업버튼 RedDot → 메인메뉴 RedDot 자동 전파

### 2. 왜 IRedDotCondition 인터페이스를 분리했는가?
- **목적:** 다양한 조건 타입 확장 용이
- **효과:** 강화, 상점, 퀘스트 등 각각 다른 조건 로직 구현 가능

### 3. 왜 이벤트 기반 업데이트를 사용했는가?
- **목적:** 폴링 방지, 성능 최적화
- **효과:** 골드 변경, 강화 완료 등 실제 변경 시에만 평가

### 4. 왜 RedDotView를 별도 컴포넌트로 분리했는가?
- **목적:** 버튼마다 쉽게 RedDot 추가
- **효과:** Inspector에서 Key만 설정하면 자동 동작

### 5. 왜 애니메이션을 추가했는가?
- **목적:** 시각적 주목도 향상
- **효과:** 펄스 효과로 사용자 주의 유도

---

## 5. 🎮 사용 가이드 (Usage Guide within Unity)

### Inspector 설정

**RedDotView:**
- `Key`: 구독할 RedDotKey 선택
- `RedDotObject`: 활성화할 GameObject (빨간 점 이미지)
- `RedDotImage`: Image 컴포넌트 (선택)
- `UseAnimation`: 애니메이션 사용 여부
- `AnimationDuration`: 애니메이션 시간 (기본: 0.3초)
- `UsePulse`: 펄스 애니메이션 사용 여부
- `PulseScale`: 펄스 최대 크기 (기본: 1.2)
- `PulseDuration`: 펄스 주기 (기본: 0.5초)

### 기본 사용법

**1. 버튼에 RedDot 추가:**
```
1. 버튼 GameObject에 RedDotView 컴포넌트 추가
2. 버튼 하위에 RedDot 이미지 오브젝트 생성
3. RedDotView의 Key 설정 (예: UpgradeSwordAttackDamage)
4. RedDotView의 RedDotObject에 이미지 오브젝트 할당
```

**2. 상위 버튼에 RedDot 추가:**
```
1. 강화 팝업 호출 버튼에 RedDotView 추가
2. Key를 Upgrade로 설정
→ 하위 강화 항목 중 하나라도 활성화되면 자동으로 활성화
```

### 코드 사용 예시

```csharp
// RedDot 상태 조회
bool isUpgradeActive = RedDotManager.Instance.IsActive(RedDotKey.Upgrade);

// RedDot 상태 구독
RedDotManager.Instance.Subscribe(RedDotKey.Shop, (key, isActive) =>
{
    Debug.Log($"Shop RedDot: {isActive}");
});

// 전체 재평가 (데이터 갱신 후)
RedDotManager.Instance.EvaluateAll();

// 특정 노드만 재평가
RedDotManager.Instance.Evaluate(RedDotKey.Quest);
```

### 새로운 조건 추가하기

**1. 조건 클래스 생성:**
```csharp
public class QuestRedDotCondition : IRedDotCondition
{
    private readonly RedDotKey _key;

    public RedDotKey Key => _key;
    public event Action OnConditionChanged;

    public QuestRedDotCondition(RedDotKey key)
    {
        _key = key;
        // 퀘스트 매니저 이벤트 구독
        QuestManager.Instance.OnQuestCompleted += HandleQuestCompleted;
    }

    public bool CheckCondition()
    {
        // 완료 가능한 퀘스트가 있는지 체크
        return QuestManager.Instance.HasCompletableQuest();
    }

    private void HandleQuestCompleted()
    {
        OnConditionChanged?.Invoke();
    }
}
```

**2. RedDotManager에 등록:**
```csharp
private void RegisterConditions()
{
    // 기존 조건들...

    // 퀘스트 조건 추가
    RegisterCondition(RedDotKey.QuestDaily,
        new QuestRedDotCondition(RedDotKey.QuestDaily));
}
```

### 새로운 트리 노드 추가하기

**1. RedDotKey에 키 추가:**
```csharp
public enum RedDotKey
{
    // 기존 키들...

    // 새로운 기능
    Guild = 600,
    GuildDonation = 601,
    GuildRaid = 602
}
```

**2. RedDotManager.BuildTree()에 노드 추가:**
```csharp
private void BuildTree()
{
    // 기존 트리...

    // 길드 트리 추가
    var guild = CreateNode(RedDotKey.Guild);
    mainMenu.AddChild(guild);

    guild.AddChild(CreateNode(RedDotKey.GuildDonation));
    guild.AddChild(CreateNode(RedDotKey.GuildRaid));
}
```

---

## 6. ⚠️ 크리티컬 리뷰 및 개선점 (Critical Review)

### 잠재적 오류
1. **Manager 초기화 순서**
   - **위치:** `UpgradeRedDotCondition` 생성자
   - **문제:** CurrencyManager/UpgradeManager가 아직 초기화되지 않았을 수 있음
   - **방어:** null 체크로 처리됨

2. **이벤트 구독 해제**
   - **위치:** `UpgradeRedDotCondition.Dispose()`
   - **문제:** Dispose가 호출되지 않으면 메모리 누수
   - **개선:** RedDotManager.OnDestroy에서 조건들의 Dispose 호출 필요

### 성능 이슈
1. **트리 순회 비용**
   - **위치:** `RedDotNode.Evaluate()`
   - **현재:** 상태 변경 시 부모까지 재귀 평가
   - **최적화:** 더티 플래그 사용하여 불필요한 평가 방지

2. **Dictionary 조회**
   - **위치:** `RedDotManager.IsActive()`
   - **현재:** 매 호출마다 TryGetValue
   - **최적화:** 자주 조회되는 키는 캐싱

### Refactoring 제안
1. **조건 Dispose 자동화**
   - **현재:** 수동 호출 필요
   - **개선:** IDisposable 구현 및 RedDotManager에서 자동 정리

```csharp
private void OnDestroy()
{
    foreach (var condition in _registeredConditions)
    {
        if (condition is IDisposable disposable)
        {
            disposable.Dispose();
        }
    }
}
```

2. **ScriptableObject 기반 트리 구성**
   - **현재:** 코드에서 트리 구조 하드코딩
   - **개선:** ScriptableObject로 트리 구조 정의하여 기획자가 수정 가능

3. **비동기 조건 지원**
   - **현재:** 동기식 CheckCondition()만 지원
   - **개선:** 서버 데이터 기반 조건을 위한 비동기 지원

### 코드 스타일
- ✅ DontDestroySingleton 상속 올바름
- ✅ 이벤트 기반 통신 올바름
- ✅ 트리 구조로 상태 전파
- ✅ DOTween 애니메이션 적용
- ⚠️ 조건 Dispose 자동화 필요
- ⚠️ 트리 구조 하드코딩 (ScriptableObject 전환 고려)
