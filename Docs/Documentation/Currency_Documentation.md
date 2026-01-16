# [파일명: Currency_Documentation.md]

## 1. 📂 모듈 개요 (Module Overview)

이 모듈은 게임 내 재화(Currency)의 관리, 저장, UI 표시를 담당합니다. BigInteger를 사용하여 무한 스케일링을 지원하며, 골드(Gold)와 루비(Ruby) 두 가지 재화를 관리합니다.

**주요 스크립트:**
- `Currency.cs` (Data Layer) - 재화 데이터 클래스
- `CurrencyType.cs` - 재화 타입 열거형
- `ICurrencyRepository.cs` - 재화 저장소 인터페이스
- `CurrencyManager.cs` (Manager Layer) - 재화 관리 비즈니스 로직
- `CurrencyRepository.cs` (Repository Layer) - BGDatabase 연동
- `CurrencyUI.cs` (UI Layer) - 재화 UI 표시
- `CurrencyFormatter.cs` (Util) - BigInteger 포맷팅 유틸리티

---

## 2. 🏗️ 아키텍처 및 상호작용 (Architecture & Interactions)

### Script Flow
```
CurrencyManager (Singleton, Manager Layer)
    └─> CurrencyRepository (Repository Layer) → BGDatabase
    └─> Currency (Data Layer) - OnChanged 이벤트 발행
        └─> CurrencyUI (UI Layer) - OnChanged 구독
        └─> UpgradeManager - TrySpendGold() 호출
```

### Dependencies
- **외부 시스템:**
  - `BGDatabase`: 재화 데이터 영속화
  - `UpgradeManager`: 강화 시 골드 소모

### Diagram Description
```
┌─────────────────────────────────────────────┐
│           CurrencyManager                 │
│        (DontDestroySingleton)             │
│  - Gold, Ruby (BigInteger)                │
│  - OnCurrencyChanged (Event)               │
└────────────┬──────────────────────────────┘
             │
             ▼
    ┌────────────────┐
    │ CurrencyRepo   │
    │  (Repository)  │
    └────────────────┘
             │
             ▼
    ┌────────────────┐
    │   Currency     │
    │  - Add()       │
    │  - TrySpend()  │
    │  - Get()       │
    └────┬───────────┘
         │
         ▼ OnChanged 구독
    ┌────────────────┐
    │  CurrencyUI    │
    └────────────────┘
         ▼
    ┌────────────────┐
    │CurrencyFormatter│ (약어 변환)
    └────────────────┘
```

---

## 3. 📝 상세 코드 분석 (Detailed Code Analysis)

### Currency (Data Layer)
- **기능:** 재화 데이터 클래스
- **주요 필드:**
  - `_gold`, `_ruby` → BigInteger
- **이벤트:** `OnChanged` (CurrencyType, BigInteger)
- **메서드:**
  - `Add(CurrencyType, BigInteger)` - 재화 추가
  - `TrySpend(CurrencyType, BigInteger)` - 재화 소모 (성공 시 true)
  - `Get(CurrencyType)` - 재화 조회

### CurrencyType
- **기능:** 재화 타입 열거형
- **값:** `Gold`, `Ruby`

### ICurrencyRepository
- **기능:** 재화 저장소 인터페이스
- **메서드:** `Load()`, `Save(Currency)`

### CurrencyRepository (Repository Layer)
- **기능:** BGDatabase에서 Currency 로드/저장
- **테이블:** `PlayerProfile` 테이블의 `Gold`, `Ruby` 필드

### CurrencyManager (Manager Layer)
- **디자인 패턴:** Singleton
- **핵심 메서드:**
  - `AddGold(BigInteger)` - 골드 추가
  - `AddRuby(BigInteger)` - 루비 추가
  - `TrySpendGold(BigInteger)` - 골드 소모
  - `TrySpendRuby(BigInteger)` - 루비 소모
- **이벤트:** `OnCurrencyChanged` - 재화 변경 시 발행
- **오토세이브:** 골드 변경 시 1분마다 자동 저장

### CurrencyUI (UI Layer)
- **기능:** 재화 UI 표시
- **구독:** `CurrencyManager.OnCurrencyChanged`
- **포맷팅:** `CurrencyFormatter.FormatAbbreviated()` 사용

### CurrencyFormatter (Util)
- **기능:** BigInteger를 약어로 변환 (예: 1,234,567 → "1.23M")
- **주요 메서드:**
  - `FormatAbbreviated(BigInteger)` - K, M, B, T 약어 변환

---

## 4. 💡 설계 의도 및 구현 이유 (Design Rationale)

### 1. 왜 BigInteger를 사용했는가?
- **목적:** 방치형 게임의 무한 스케일링 지원
- **효과:** int/float 오버플로우 방지, 억 단위 이상의 재화 처리

### 2. 왜 Currency를 별도 클래스로 분리했는가?
- **목적:** 재화 로직 중앙 집중화
- **효과:** 재화 관련 로직 재사용성 향상

### 3. 왜 이벤트 기반 통신을 사용했는가?
- **목적:** UI와 Manager의 결합도 감소
- **효과:** CurrencyManager는 UI 존재 여부를 모름 (느슨한 결합)

### 4. 왜 CurrencyFormatter를 분리했는가?
- **목적:** 포맷팅 로직 재사용
- **효과:** UpgradeUI 등 다른 UI에서도 사용 가능

### 5. 왜 오토세이브를 사용했는가?
- **목적:** 데이터 손실 방지
- **구현:** 골드 변경 시 1분마다 BGRepo.I.Save() 호출

---

## 5. 🎮 사용 가이드 (Usage Guide within Unity)

### Inspector 설정
**CurrencyManager:**
- 특별 설정 없음 (자동 초기화)

**CurrencyUI:**
- `GoldText`: 골드 표시 TextMeshProUGUI
- `RubyText`: 루비 표시 TextMeshProUGUI

### 초기화 방법
1. 씬에 CurrencyManager 프리팹 배치 (또는 자동 생성)
2. CurrencyUI 프리팹 배치
3. BGDatabase에 `PlayerProfile` 테이블 생성
4. 테이블에 `Gold`, `Ruby` 필드 추가 (string 타입)
5. 스크립트에서 `CurrencyManager.Instance.AddGold()` 등 호출

### 사용 예시
```csharp
// 골드 추가
CurrencyManager.Instance.AddGold(1000);

// 골드 소모 (성공 여부 확인)
if (CurrencyManager.Instance.TrySpendGold(500))
{
    Debug.Log("강화 성공");
}

// 재화 이벤트 구독
CurrencyManager.Instance.OnCurrencyChanged += (type, amount) =>
{
    Debug.Log($"{type}: {amount}");
};
```

---

## 6. ⚠️ 크리티컬 리뷰 및 개선점 (Critical Review)

### 잠재적 오류
1. **NullReference 가능성**
   - **위치:** `CurrencyUI.cs`
   - **문제:** `CurrencyManager.Instance`가 null일 때
   - **방어:** null 체크가 필요함

### 성능 이슈
1. **약어 포맷팅 복잡도**
   - **위치:** `CurrencyFormatter.FormatAbbreviated()`
   - **문제:** 매 호출 시 반복 계산
   - **개선:** 캐싱 또는 더 효율적인 알고리즘

### Refactoring 제안
1. **Currency 오토세이브 로직 분리**
   - **현재:** CurrencyManager 내부에서 처리
   - **개선:** 별도 `AutoSaveManager`로 분리

2. **CurrencyFormatter의 스트링 할당 최적화**
   - **현재:** 매 호출 새로운 문자열 생성
   - **개선:** StringBuilder 사용

3. **이벤트 구독 관리 개선**
   - **현재:** OnDestroy에서 수동 구독 해제
   - **개선:** `UnityEvent` 또는 C# 9.0의 `using` 문 사용

### 코드 스타일
- ✅ BigInteger 사용 올바름
- ✅ 이벤트 기반 통신 올바름
- ✅ 네이밍 명확함
- ⚠️ 오토세이브 로직이 CurrencyManager에 섞여 있음
