# [파일명: PopupManager_Documentation.md]

## 1. 📂 모듈 개요 (Module Overview)

이 모듈은 게임 내 팝업 UI의 생명주기와 우선순위를 관리합니다. 스택 기반으로 팝업을 관리하며, 우선순위 시스템을 통해 Global Announce나 시스템 메시지가 항상 최상단에 표시되도록 보장합니다.

**주요 스크립트:**
- `PopupPriority.cs` (Enum) - 팝업 우선순위 열거형
- `PopupBase.cs` (Base) - 팝업 추상 기반 클래스
- `PopupBlocker.cs` - 배경 블로커 컴포넌트
- `PopupManager.cs` (Manager Layer) - 팝업 관리 싱글톤

---

## 2. 🏗️ 아키텍처 및 상호작용 (Architecture & Interactions)

### Script Flow
```
PopupManager (Singleton, Manager Layer)
    └─> PopupBase (구체 팝업들의 기반 클래스)
        └─> ShopPopup, SettingsPopup 등 (PopupBase 상속)
    └─> PopupBlocker (배경 터치 감지)
        └─> PopupManager.ClosePopup() 호출
```

### Dependencies
- **외부 시스템:**
  - `DontDestroySingleton<T>`: 싱글톤 베이스 클래스
  - `Canvas`: Unity UI 렌더링
  - `GraphicRaycaster`: UI 터치 감지

### Diagram Description
```
┌─────────────────────────────────────────────────────┐
│              PopupManager                           │
│           (DontDestroySingleton)                    │
│  - _popupStacks (우선순위별 팝업 리스트)             │
│  - _blockerPool (블로커 오브젝트 풀)                │
│  - OnPopupOpened, OnPopupClosed (Event)             │
└────────────┬────────────────────────────────────────┘
             │ OpenPopup() / ClosePopup()
             ▼
    ┌────────────────────────┐
    │      PopupBase         │
    │  - Priority            │
    │  - CloseOnBlockerClick │
    │  - Open() / Close()    │
    └────────┬───────────────┘
             │
             ▼ 블로커 클릭 시
    ┌────────────────────────┐
    │    PopupBlocker        │
    │  - SetClickAction()    │
    │  - Show() / Hide()     │
    └────────────────────────┘
```

### SortingOrder 계산 방식
```
sortingOrder = 기본값(100) + 우선순위값 × 100 + 순서 × 10

예시:
┌─────────────────┬──────────────┬───────┬──────────────┐
│ 팝업            │ Priority     │ Order │ SortingOrder │
├─────────────────┼──────────────┼───────┼──────────────┤
│ 상점 팝업       │ Normal (100) │ 0     │ 10100        │
│ 아이템 상세     │ Normal (100) │ 1     │ 10110        │
│ 레벨업 알림     │ High (200)   │ 0     │ 20100        │
│ 네트워크 오류   │ System (300) │ 0     │ 30100        │
│ 긴급 공지       │ GlobalAnnounce (400) │ 0 │ 40100   │
└─────────────────┴──────────────┴───────┴──────────────┘

블로커 sortingOrder = 팝업 sortingOrder - 1
```

---

## 3. 📝 상세 코드 분석 (Detailed Code Analysis)

### PopupPriority (Enum)
- **기능:** 팝업 우선순위 정의
- **값:**
  - `Low (0)` - 일반 정보성 팝업 (도움말, 팁)
  - `Normal (100)` - 기본 팝업 (상점, 인벤토리)
  - `High (200)` - 중요 팝업 (보상 획득, 레벨업)
  - `System (300)` - 시스템 팝업 (네트워크 오류, 점검)
  - `GlobalAnnounce (400)` - 최상위 공지

### PopupBase (Base Class)
- **기능:** 모든 팝업의 추상 기반 클래스
- **주요 필드:**
  - `_priority` → PopupPriority (Inspector 설정)
  - `_closeOnBlockerClick` → 배경 클릭 시 닫힘 여부
  - `_showBlocker` → 배경 블로커 표시 여부
- **프로퍼티:** `Priority`, `CloseOnBlockerClick`, `ShowBlocker`, `IsOpen`, `Canvas`
- **이벤트:** `OnOpened`, `OnClosed`
- **메서드:**
  - `Open()` - 팝업 열기 (PopupManager가 호출)
  - `Close()` - 팝업 닫기 (PopupManager가 호출)
  - `RequestClose()` - 닫기 요청 (닫기 버튼에서 호출)
  - `SetSortingOrder(int)` - Canvas sortingOrder 설정
- **가상 메서드:**
  - `OnOpen()` - 팝업 열릴 때 추가 로직
  - `OnClose()` - 팝업 닫힐 때 추가 로직

### PopupBlocker
- **기능:** 배경 딤 처리 및 클릭 감지
- **주요 필드:**
  - `_blockerImage` → 배경 이미지
  - `_dimmedColor` → 딤 색상 (기본: 반투명 검정)
- **메서드:**
  - `SetSortingOrder(int)` - Canvas sortingOrder 설정
  - `SetClickAction(UnityAction)` - 클릭 콜백 설정
  - `Show()` / `Hide()` - 표시/숨김

### PopupManager (Manager Layer)
- **디자인 패턴:** Singleton (DontDestroySingleton 상속)
- **주요 필드:**
  - `_popupStacks` → 우선순위별 팝업 리스트 (SortedDictionary)
  - `_popupBlockers` → 팝업-블로커 매핑
  - `_blockerPool` → 블로커 오브젝트 풀
- **이벤트:**
  - `OnPopupOpened` - 팝업 열릴 때
  - `OnPopupClosed` - 팝업 닫힐 때
  - `OnAllPopupsClosed` - 모든 팝업 닫힐 때
- **핵심 메서드:**
  - `OpenPopup(PopupBase)` - 팝업 열기
  - `ClosePopup(PopupBase)` - 특정 팝업 닫기
  - `CloseTopPopup()` - 최상단 팝업 닫기
  - `CloseAllPopups()` - 모든 팝업 닫기
  - `ClosePopupsBelowPriority(PopupPriority)` - 특정 우선순위 미만 팝업 닫기
  - `GetTopPopup()` - 현재 최상단 팝업 반환
  - `HasPopupWithPriority(PopupPriority)` - 특정 우선순위 팝업 존재 여부
- **Android Back 버튼:** Update()에서 Escape 키 감지하여 CloseTopPopup() 호출

---

## 4. 💡 설계 의도 및 구현 이유 (Design Rationale)

### 1. 왜 우선순위 시스템을 사용했는가?
- **목적:** Global Announce, 시스템 메시지가 항상 최상단에 표시
- **효과:** 일반 팝업(상점, 인벤토리)이 열려 있어도 긴급 공지가 위에 표시됨

### 2. 왜 SortedDictionary를 사용했는가?
- **목적:** 우선순위별 자동 정렬
- **효과:** GetTopPopup() 호출 시 높은 우선순위부터 탐색 (O(1)에 가까운 성능)

### 3. 왜 개별 블로커 방식을 선택했는가?
- **목적:** 팝업별 독립적인 블로커 설정 가능
- **효과:** 특정 팝업만 배경 클릭 비활성화 가능 (강제 확인 팝업 등)
- **대안 비교:**
  - 단일 공유 블로커: 메모리 절약, 팝업 전환 시 깜빡임 발생
  - 개별 블로커 (채택): 유연성 확보, 풀링으로 메모리 최소화

### 4. 왜 오브젝트 풀링을 사용했는가?
- **목적:** 블로커 생성/파괴 비용 절감
- **효과:** 팝업 열기/닫기 시 GC 압박 감소

### 5. 왜 이벤트 기반 통신을 사용했는가?
- **목적:** UI와 Manager의 결합도 감소
- **효과:** PopupManager는 구체 팝업 클래스를 알 필요 없음 (느슨한 결합)

### 6. 왜 Android Back 버튼을 지원하는가?
- **목적:** 모바일 UX 향상
- **효과:** 사용자가 Back 버튼으로 팝업 닫기 가능 (CloseOnBlockerClick이 true인 경우만)

---

## 5. 🎮 사용 가이드 (Usage Guide within Unity)

### Inspector 설정

**PopupManager:**
- `BaseSortingOrder`: 기본 sortingOrder (기본값: 100)
- `SortingOrderStep`: 팝업 간 sortingOrder 간격 (기본값: 10)
- `BlockerPrefab`: 블로커 프리팹 (선택, 없으면 자동 생성)

**PopupBase 상속 클래스:**
- `Priority`: 팝업 우선순위 선택
- `CloseOnBlockerClick`: 배경 클릭 시 닫힘 여부
- `ShowBlocker`: 배경 블로커 표시 여부

### 새 팝업 만들기
1. PopupBase를 상속하는 새 클래스 생성
2. Canvas 컴포넌트 추가 (자동으로 RequireComponent)
3. Inspector에서 Priority, CloseOnBlockerClick 설정
4. 닫기 버튼에 `RequestClose()` 연결

```csharp
public class ShopPopup : PopupBase
{
    [SerializeField] private Button _closeButton;

    protected override void Awake()
    {
        base.Awake();
        _closeButton.onClick.AddListener(RequestClose);
    }

    protected override void OnOpen()
    {
        // 팝업 열릴 때 로직 (예: 상품 목록 로드)
    }

    protected override void OnClose()
    {
        // 팝업 닫힐 때 로직 (예: 리소스 정리)
    }
}
```

### 사용 예시
```csharp
// 팝업 열기
PopupManager.Instance.OpenPopup(shopPopup);

// 최상단 팝업 닫기
PopupManager.Instance.CloseTopPopup();

// 특정 팝업 닫기
PopupManager.Instance.ClosePopup(shopPopup);

// 모든 팝업 닫기
PopupManager.Instance.CloseAllPopups();

// 시스템 팝업 이하만 닫기 (긴급 공지는 유지)
PopupManager.Instance.ClosePopupsBelowPriority(PopupPriority.GlobalAnnounce);

// 팝업 이벤트 구독
PopupManager.Instance.OnPopupOpened += (popup) =>
{
    Debug.Log($"팝업 열림: {popup.name}");
};

PopupManager.Instance.OnAllPopupsClosed += () =>
{
    Debug.Log("모든 팝업 닫힘, 게임 재개");
};
```

### 팝업에서 닫기 버튼 연결
```csharp
// Unity Inspector에서 Button.OnClick에 연결
public void OnCloseButtonClicked()
{
    RequestClose();
}
```

---

## 6. ⚠️ 크리티컬 리뷰 및 개선점 (Critical Review)

### 잠재적 오류
1. **중복 Open 방지**
   - **위치:** `PopupManager.OpenPopup()`
   - **현재:** `popup.IsOpen` 체크로 중복 방지
   - **상태:** ✅ 처리됨

2. **Null 체크**
   - **위치:** `PopupManager.OpenPopup()`, `ClosePopup()`
   - **현재:** null 체크 및 경고 로그 출력
   - **상태:** ✅ 처리됨

### 성능 이슈
1. **List.Contains() 호출**
   - **위치:** `PopupManager.ClosePopup()` - `stack.Contains(popup)`
   - **문제:** O(n) 복잡도
   - **개선:** HashSet 사용 또는 Dictionary로 팝업 추적

2. **SortedDictionary 순회**
   - **위치:** `GetTopPopup()`
   - **현재:** foreach로 전체 순회
   - **개선:** 최상단 팝업 캐싱

### Refactoring 제안
1. **애니메이션 지원 추가**
   - **현재:** 즉시 Show/Hide
   - **개선:** DOTween 연동하여 Open/Close 애니메이션

```csharp
protected override void OnOpen()
{
    transform.localScale = Vector3.zero;
    transform.DOScale(Vector3.one, 0.3f).SetEase(Ease.OutBack);
}
```

2. **프리팹 기반 팝업 생성**
   - **현재:** 씬에 미리 배치된 팝업만 사용
   - **개선:** 프리팹 인스턴스화 지원

```csharp
public T OpenPopup<T>(T prefab) where T : PopupBase
{
    T instance = Instantiate(prefab, transform);
    OpenPopup(instance);
    return instance;
}
```

3. **팝업 히스토리 (뒤로가기 네비게이션)**
   - **현재:** 미지원
   - **개선:** 닫힌 팝업 스택 저장하여 GoBack() 지원

### 코드 스타일
- ✅ DontDestroySingleton 상속 올바름
- ✅ 이벤트 기반 통신 올바름
- ✅ 오브젝트 풀링 적용
- ✅ Android Back 버튼 지원
- ⚠️ 애니메이션 미지원 (필요시 추가)
