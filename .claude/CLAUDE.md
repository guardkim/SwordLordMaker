# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## 언어 및 역할

- 모든 응답은 **한국어**로 작성
- Unity 6 및 C# 전문가, 엄격한 코드 리뷰어 역할 수행
- **코드를 작성하기 전, 어떻게 코드를 작성할 것인지, 상세히 md파일을 작성 후 검토 받고 코드 작성**
- 구현은 사용자가 직접 수행하거나 별도 요청 시에만 진행

## 프로젝트 개요

SwordLordMaker는 Unity 6 기반 액션 게임 프로젝트입니다. 비행 검(Flying Sword) 시스템과 데미지 플로터가 핵심 게임플레이 요소입니다.

## 아키텍처

### 핵심 시스템 구조

```
Assets/
├── 01.Scenes/          # MainScene, StartScene
├── 02.Scripts/         # 프로젝트 스크립트 (UI 등)
├── 03.Prefabs/         # 게임 프리팹
├── DamageFloater/      # 핵심 게임플레이 모듈
│   └── 01.Scripts/
│       ├── DamageFloater/      # 데미지 표시 시스템
│       ├── FlyingSword/        # 비행 검 시스템
│       ├── Enum/               # SwordType, DamageStyle 등
│       └── Interface/          # IDamageable
└── Settings/           # URP 렌더링 설정
```

### 비행 검 시스템 (전략 패턴)

- `BaseFlyingSword` / `BaseSwordController`: 추상 기반 클래스
- 3가지 검 타입: `Adel`(8자 궤도), `Hypo`(하이포사이클로이드), `Pixel`(무한 루프)
- `ControllerManager`: 싱글톤, 검 타입 전환 관리
- `IDamageable`: 데미지 처리 인터페이스

### 데미지 플로터 시스템

- `DamageFloaterManager`: 싱글톤, 데미지 이펙트 인스턴싱
- `DamageFloater`: DOTween 기반 애니메이션 (7가지 스타일)
- `PixelTextHelper`: 픽셀 폰트 렌더링 + 지그재그 효과

### DDD 4계층 아키텍처

아웃게임 요소(인벤토리, 상점, 길드, 소셜 등)는 다음 4계층 구조를 따릅니다.

#### 1. Data (데이터 계층)

- 순수 데이터 클래스 (POCO)
- 외부 의존성 없음
- `record` 타입으로 불변성 보장 (Value Objects)
- Entities는 식별자(ID) 보유, 로직 캡슐화

```csharp
// 예시: 아이템 데이터
public record ItemData(string Id, string Name, int Price);

public class PlayerInventory
{
    public string PlayerId { get; }
    private readonly List<ItemData> _items = [];

    public void AddItem(ItemData item) { /* 도메인 로직 */ }
}
```

#### 2. Repository (저장소 계층)

- 데이터 영속화 담당 (DB, PlayerPrefs, 파일)
- Data 계층에 인터페이스(`IRepository`) 정의
- Repository 계층에 구체 구현
- 네트워크 통신 구현

```csharp
// Data 계층에 인터페이스 정의
public interface IInventoryRepository
{
    Task<PlayerInventory> LoadAsync(string playerId);
    Task SaveAsync(PlayerInventory inventory);
}

// Repository 계층에 구현
public class LocalInventoryRepository : IInventoryRepository
{
    public async Task<PlayerInventory> LoadAsync(string playerId) { /* PlayerPrefs 구현 */ }
    public async Task SaveAsync(PlayerInventory inventory) { /* PlayerPrefs 구현 */ }
}
```

#### 3. Manager (관리자 계층)

- 유스케이스(Use Cases) 구현
- Data와 Repository 오케스트레이션
- 비즈니스 흐름 제어
- 싱글톤 또는 DI로 접근

```csharp
public class InventoryManager
{
    private readonly IInventoryRepository _repository;
    private PlayerInventory _inventory;

    public async Task PurchaseItemAsync(ItemData item)
    {
        // 유스케이스: 아이템 구매
        _inventory.AddItem(item);
        await _repository.SaveAsync(_inventory);
    }
}
```

#### 4. UI (프레젠테이션 계층)

- UI Toolkit 뷰, 사용자 입력 처리
- **비즈니스 로직 포함 금지**
- Manager를 통해서만 데이터 접근
- 이벤트 기반 업데이트

```csharp
public class InventoryUI : MonoBehaviour
{
    [SerializeField] private InventoryManager _manager;

    public async void OnPurchaseButtonClicked(ItemData item)
    {
        // UI는 Manager에 위임만 함
        await _manager.PurchaseItemAsync(item);
        RefreshView();
    }
}
```

#### 계층 간 의존성 규칙

```
UI → Manager → Repository → Data
         ↓
       Data
```

- 상위 계층은 하위 계층만 참조
- Data는 어떤 계층도 참조하지 않음
- Repository는 Data만 참조
- Manager는 Repository 인터페이스와 Data 참조
- UI는 Manager만 참조

## 설계 원칙

- **SOLID**: 특히 SRP, OCP 철저 준수
- **디미터의 법칙**: `a.getB().getC()` 형태 금지
- **함수**: 하나의 함수는 하나의 작업만 수행

## C# 코딩 스타일

### 포맷팅

- 들여쓰기: 공백 4칸 (탭 금지)
- 중괄호: Allman 스타일 (개행 후 중괄호)
- 한 줄에 하나의 문장만 작성

### 명명 규칙

| 대상 | 규칙 | 예시 |
|-----|------|-----|
| 클래스, 메서드, 프로퍼티 | PascalCase | `DamageFloater`, `ShowDamage()` |
| 로컬 변수, 파라미터 | camelCase | `damageValue`, `targetEnemy` |
| private 필드 | `_` 접두사 | `_activeTexts` |
| static 필드 | `s_` 접두사 | `s_instance` |
| 인터페이스 | `I` 접두사 | `IDamageable` |

### 언어 기능

- 타입 키워드 사용: `int`, `string` (Int32, String 아님)
- `var`: 타입이 명확할 때만 (`var list = new List<int>()`)
- 문자열: 보간 사용 (`$"{value}"`), 대용량은 `StringBuilder`
- 비동기: `async/await` 사용 권장
- 대리자: `Action<>`, `Func<>` 사용

### 주석

- XML 주석(`<summary>`) 사용 금지
- 메서드/변수명으로 의도 표현
- 필요시 `//` 사용 (대문자 시작, 마침표 종료)

## 금지 사항

- `catch (Exception e)` 금지 - 구체적 예외만 처리
- 연속 밑줄 `__` 금지
- 단일 문자 변수명 금지 (루프 인덱스 `i`, `j` 제외)
- 구버전 C# 문법 지양

## 주요 의존성

- **DOTween**: 애니메이션 라이브러리
- **TextMesh Pro**: 텍스트 렌더링
- **BGDatabase**: 데이터 관리
- **Feel (MMFeedbacks)**: 피드백 시스템
- **URP**: Universal Render Pipeline

## 빌드 및 테스트

- Unity 에디터에서 빌드: File > Build Settings
- 테스트: Unity Test Runner (Window > General > Test Runner)
