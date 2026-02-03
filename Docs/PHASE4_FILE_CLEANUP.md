# Phase 4: 파일 병합 및 정리 작업 계획

## 개요

| 항목 | 내용 |
|:---|:---|
| **목표** | 파일 파편화 감소, Enum 파일 명명 규칙 통일 |
| **범위** | Enum 파일들, 불필요한 스크립트 파일 |
| **위험도** | 낮음 |
| **예상 결과** | 파일 구조 정리, 명명 규칙 일관성 확보 |

---

## 현재 상태 분석

### 1. Enum 파일 현황

| 현재 파일명 | 경로 | 내용 |
|:---|:---|:---|
| `CurrencyType.cs` | `Assets/02.Scripts/Currency/Data/` | Gold, Ruby |
| `PopupType.cs` | `Assets/02.Scripts/UI/Popup/Enum/` | None, Upgrade, Shop 등 (32줄) |
| `PopupPriority.cs` | `Assets/02.Scripts/UI/Popup/Enum/` | Low, Normal, High, System, GlobalAnnounce |
| `SwordType.cs` | `Assets/DamageFloater/01.Scripts/` | Adel, Hypo, Pixel |
| `BgmId.cs` | `Assets/02.Scripts/Sound/Data/` | Title, Main, Battle 등 |
| `SfxId.cs` | `Assets/02.Scripts/Sound/Data/` | SwordAttack, Hit, ButtonClick 등 (36줄) |
| `UpgradeId.cs` | `Assets/02.Scripts/Upgrade/Data/` | PlayerHealth, SwordAttackDamage 등 |
| `DamageStyle.cs` | `Assets/DamageFloater/01.Scripts/DamageFloater/` | Basic, Blade, Volcano 등 |

### 2. 삭제 대상 파일

| 파일명 | 경로 | 이유 |
|:---|:---|:---|
| `DontDestroyOnLoadObject.cs` | `Assets/02.Scripts/Util/` | 단순 래퍼, 참조 없음 |
| `FirstScript.cs` | `Assets/02.Scripts/` | Unity 템플릿 코드, 빈 클래스 |

---

## 작업 내용

### 작업 1: Enum 파일명 변경 (E 접두사 추가)

**명명 규칙**: `E{EnumName}.cs`

| 변경 전 | 변경 후 |
|:---|:---|
| `CurrencyType.cs` | `ECurrencyType.cs` |
| `PopupType.cs` | `EPopupType.cs` |
| `PopupPriority.cs` | `EPopupPriority.cs` |
| `SwordType.cs` | `ESwordType.cs` |
| `BgmId.cs` | `EBgmId.cs` |
| `SfxId.cs` | `ESfxId.cs` |
| `UpgradeId.cs` | `EUpgradeId.cs` |
| `DamageStyle.cs` | `EDamageStyle.cs` |

**주의사항**:
- **파일명만 변경** (enum 타입명은 유지)
- Unity `.meta` 파일은 자동으로 처리됨
- IDE에서 리네임 기능 사용 권장

---

### 작업 2: 불필요한 파일 삭제

#### 2-1. DontDestroyOnLoadObject.cs 삭제

**현재 코드**:
```csharp
public class DontDestroyOnLoadObject : MonoBehaviour
{
    private void Awake()
    {
        DontDestroyOnLoad(gameObject);
    }
}
```

**삭제 이유**:
- 코드에서 참조하는 곳 없음
- `DontDestroySingleton<T>` 베이스 클래스가 이미 존재
- 단순 래퍼로 불필요

**삭제 전 확인**:
- [ ] 씬에서 이 컴포넌트를 사용하는 GameObject가 없는지 확인
- [ ] 프리팹에서 이 컴포넌트를 사용하는지 확인

---

#### 2-2. FirstScript.cs 삭제

**현재 코드**:
```csharp
public class FirstScript : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {

    }
}
```

**삭제 이유**:
- Unity 프로젝트 생성 시 자동 생성된 템플릿 코드
- 빈 클래스, 어떤 로직도 없음
- 사용되지 않음

---

## 파일별 변경 요약

| 작업 | 파일 | 변경 내용 |
|:---|:---|:---|
| 리네임 | `CurrencyType.cs` | → `ECurrencyType.cs` |
| 리네임 | `PopupType.cs` | → `EPopupType.cs` |
| 리네임 | `PopupPriority.cs` | → `EPopupPriority.cs` |
| 리네임 | `SwordType.cs` | → `ESwordType.cs` |
| 리네임 | `BgmId.cs` | → `EBgmId.cs` |
| 리네임 | `SfxId.cs` | → `ESfxId.cs` |
| 리네임 | `UpgradeId.cs` | → `EUpgradeId.cs` |
| 리네임 | `DamageStyle.cs` | → `EDamageStyle.cs` |
| 삭제 | `DontDestroyOnLoadObject.cs` | 파일 및 .meta 삭제 |
| 삭제 | `FirstScript.cs` | 파일 및 .meta 삭제 |

---

## 작업 순서

1. **Unity 에디터 닫기** (파일 충돌 방지)
2. **Git 브랜치 확인** (`Refactor` 브랜치)
3. **Enum 파일 리네임** (8개 파일)
4. **불필요한 파일 삭제** (2개 파일)
5. **Unity 에디터 열기**
6. **컴파일 오류 확인**
7. **씬/프리팹에서 Missing Script 확인**
8. **플레이 테스트**
9. **커밋**

---

## 테스트 체크리스트

### 컴파일
- [ ] Unity 에디터에서 컴파일 오류 없음

### 씬/프리팹
- [ ] MainScene에서 Missing Script 없음
- [ ] StartScene에서 Missing Script 없음
- [ ] 프리팹에서 Missing Script 없음

### 기능
- [ ] 게임 시작 정상
- [ ] 검 타입 전환 정상 (SwordType 사용)
- [ ] 재화 시스템 정상 (CurrencyType 사용)
- [ ] 사운드 재생 정상 (BgmId, SfxId 사용)
- [ ] 팝업 열기/닫기 정상 (PopupType, PopupPriority 사용)
- [ ] 강화 시스템 정상 (UpgradeId 사용)
- [ ] 데미지 플로터 정상 (DamageStyle 사용)

---

## 롤백 전략

1. 작업 전 현재 상태 확인: `git status`
2. 문제 발생 시: `git checkout -- .` (모든 변경 취소)
3. 커밋 후 문제 발생 시: `git revert HEAD`

---

## 추가 고려사항

### Enum 폴더 구조 통일 (선택적)

현재 Enum 파일들이 여러 곳에 분산되어 있음:
- `Assets/02.Scripts/Currency/Data/`
- `Assets/02.Scripts/UI/Popup/Enum/`
- `Assets/02.Scripts/Sound/Data/`
- `Assets/02.Scripts/Upgrade/Data/`
- `Assets/DamageFloater/01.Scripts/`
- `Assets/DamageFloater/01.Scripts/DamageFloater/`

**옵션 A**: 현재 구조 유지 (각 모듈 내 Enum 파일)
- 장점: 모듈별 응집도 유지
- 단점: Enum 파일 위치 파악 어려움

**옵션 B**: 중앙 Enum 폴더 생성 (`Assets/02.Scripts/Enum/`)
- 장점: Enum 파일 한 곳에서 관리
- 단점: 모듈 응집도 감소

**권장**: 옵션 A (현재 구조 유지) - Phase 4 범위 최소화
