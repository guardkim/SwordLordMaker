# REFACTOR_PLAN.md

## A. 목표/비목표 (Do/Don't)

### ✅ 목표 (Do)

1. **내가 통제 가능한 손코딩 스타일**로 단순화
2. 과도한 추상화/복잡성 제거 및 코드 가독성 향상
3. 프로젝트 동작 변경 금지 (기능 보존)
4. Unity 성능 최적화 (GC 감소, 정밀도 문제 해결)
5. 유지보수 용이한 구조로 단순화

### ❌ 비목표 (Don't)

1. 기능 추가 (새로운 기능 구현 금지)
2. 임의의 확장 (미래 확장성 과도 고려 금지)
3. 불필요한 DDD/클린아키텍처 엄격 적용

---

## B. 현재 코드 문제 목록 (구체적 사례)


### 2. record/init-only 남용 (Immutability Overuse)
| 파일/라인 | 문제 | 위험도 |
|:---|:---|:---:|
| `PlayerStat.cs` | 동적 플레이어 데이터에 `record` 사용 (with 키워드로 매번 새 객체 생성) | **높음** (GC 부하) |
| `SwordStat.cs` | 정적 검 데이터이나 계산용으로 사용됨 | 중간 |
| `UpgradeData.cs` | 정적 강화 데이터 - 적절함 | 낮음 |

### 3. LINQ 과다 사용 (성능 이슈)
| 파일/라인 | 문제 | 위험도 |
|:---|:---|:---:|
| `BaseSwordController.cs` | `FindGameObjectsWithTag("Enemy").Where(...).ToArray()` 체이닝 | 중간 |
| `AdelFlyingSwordController.cs` | Update 루프 내에서 `Where().ToArray()`, `All()`, `FirstOrDefault()` 사용 | **높음** (프레임당 GC) |

### 4. 과도한 파일 분리 (File Fragmentation)
| 파일/라인 | 문제 | 위험도 |
|:---|:---|:---:|
| `CurrencyType.cs` (5줄) | 단순 Enum이나 별도 파일 | 낮음 |
| `PopupPriority.cs` (8줄) | 단순 Enum이나 별도 파일 | 낮음 |
| `DontDestroyOnLoadObject.cs` (10줄) | 단순 래퍼 | 낮음 |
| `FirstScript.cs` (18줄) | 불필요한 템플릿 코드 | 낮음 |

### 5. 복잡한 패턴/매직
| 파일/라인 | 분석 | |
|:---|:---|
| `SpringManager.cs` | `System.Reflection` 임포트 있으나 실제 사용 거의 없음 |
| `SerializableWrapper` | Unity JsonUtility 한계 극복을 위한 정당화된 래퍼 |

### 6. 데이터 타입/정밀도 문제
| 파일/라인 | 문제 | 위험도 |
|:---|:---|:---:|
| `SwordStat.cs:21` | `(double)AttackDamage * CritDamageMultiplier` - 큰 수치에서 정밀도 손실 | **높음** (치명적) |
| `UpgradeData.cs:GetCost` | `Math.Pow(double)` 사용 - 높은 레벨에서 비용 오류 | **중간** |
| `EnemyAI.cs` | `MultiplyBigInteger` 수동 구현 - 유틸리티로 분리 필요 | 낮음 |

### 7. DDD/아키텍처 위반 및 과잉
| 파일/라인 | 문제 | 위험도 |
|:---|:---|:---:|
| `EnemyAI.Die()` | 직접 `CurrencyManager`, `PlayerStatManager`, `StageManager` 호출 | 중간 |
| `PlayerHealth.Start()` | 직접 `GameManager.RegisterPlayer()` 호출 | 낮음 |
| `StageManager.ClearAllEnemies()` | `GameObject.FindGameObjectsWithTag("Enemy")` 사용 | 낮음 |
| `PlayerSessionManager.cs` | 세션 관리 + DB 생성 로직 병합 (SRP 위반) | 중간 |

### 9. 이벤트 시스템 복잡성
| 파일/라인 | 문제 | 위험도 |
|:---|:---|:---:|
| `GameManager.OnPlayerDeath` | 정의되나 구독자 없음 | 낮음 (불필요 코드) |
| `GameManager.OnRequestStageRestart` | `StageManager`만 구독 - 직접 호출로 대체 가능 | 낮음 |
| `StageManager.OnStageCleared` | 구독자 없음 | 낮음 |
| `PopupBase.OnOpened/OnClosed` | 구독자 없음 | 낮음 |
| `RedDotManager.OnRedDotStateChanged` | 구독자 없음 | 낮음 |
| **전체 구조** | **Action/Action<T> 기반 이벤트 분산 - EventManager 중앙화 필요** | **중간** |

---

## C. 리팩토링 원칙 (코딩 규칙, 금지/권장)

### 🚫 금지 (Don't)
1. **record 남용**: 동적 데이터에는 `class` 사용 (with 키워드로 인한 GC 부하 방지), record는 아예 사용 금지.
2. **float/double 캐스팅**: BigInteger 연산 중간에 float/double 변환 금지 (정밀도 손실 방지)
3. **LINQ in Update 루프**: 성능 민감한 부분에서는 `foreach`/`if` 사용
4. **이벤트 분산**: Action/Action<T> 기반 이벤트 개별 정의 금지 (EventManager 사용)

### ✅ 권장 (Do)
1. **명시적 for/foreach**: LINQ 대신 명시적 루프 사용
2. **단순 POCO/class**: record 대신 단순 class 사용
3. **인터페이스 유지**: 레포지토리 패턴 일관성 유지를 위해 단일 구현체여도 인터페이스 유지
4. **직접 메서드 호출**: 단일 구독자 이벤트는 직접 호출로 단순화
5. **BigInteger 대신 double 사용** 모든 BigInteger 사용중인 변수를 double로 변환, Parsing 관련도 제거
6. **struct 활용**: 자주 생성되는 소규모 데이터는 struct 사용 (GC 회피)

---


### 모듈별 구조 분류

| 모듈 | 구조 방식 | 이유 |
|:---|:---:|:---|
| **Currency** | DDD 4계층 유지 | 복잡한 데이터 매핑, 영속성, 확장성 필요 |
| **PlayerStat** | DDD 4계층 유지 | 복잡한 스탯 계산, 레벨업 로직, 영속성 필요 |
| **Upgrade** | DDD 4계층 유지 | 강화 보너스 계산, 비용 로직, 영속성 필요 |
| **Stage** | 단순 구조 | 스테이지 데이터는 정적, Manager만 유지 |
| **Sound** | 단순 구조 | 사운드 관리는 단순 매니저로 충분 |
| **Effect** | 단순 구조 | VFX 관리는 단순 매니저로 충분 |

---

## E. 단계별 작업 플랜 (Phase 1~N)

### Phase 1: 치명적 버그 수정 (우선순위: 최상)
**목표**: CritDamage 정밀도 손실 해결  
**범위**: `SwordStat.cs`, `UpgradeManager.cs`  
**위험도**: 중간  
**작업:**
1. `CritDamageMultiplier`를 **struct CritDamage**로 변경 (스택 할당, GC 회피)
2. `UpgradeManager.ApplyUpgrades()`에서 CritDamage 계산 로직 수정
3. 테스트: 큰 수치에서 데미지 계산 정밀도 검증
4. 위 버그는 double로 모든 수치들을 변경하면 해결될 것으로 보임.(10^16 이상 차이나는 손실은 감수예정)

---

### Phase 2: record → class 변환 (우선순위: 높음)
**목표**: PlayerStat 등 동적 데이터의 GC 부하 감소  
**범위**: `PlayerStat.cs`, `PlayerStatManager.cs`  
**위험도**: 낮음  
**작업:**
1. `PlayerStat`을 `class`로 변경 (with 키워드 제거)
2. `PlayerStatManager`에서 필드 직접 수정 로직으로 변경
3. 테스트: 경험치 증가 시 성능 개선 확인

---

### Phase 3: LINQ 제거 (우선순위: 높음)
**목표**: 성능 민감한 구간에서 LINQ 제거  
**범위**: `BaseSwordController.cs`, `AdelFlyingSwordController.cs`  
**위험도**: 낮음  
**작업:**
1. `FindGameObjectsWithTag("Enemy").Where(...).ToArray()` → foreach 루프로 변경
2. Update 루프 내 LINQ (`Where`, `All`, `FirstOrDefault`) → 명시적 if/foreach로 변경
3. 테스트: 프레임당 할당량 감소 확인

---

### Phase 4: 파일 병합 및 정리 (우선순위: 중간)
**목표**: 파일 파편화 감소 (단순 구조 모듈 중심)  
**범위**: `Enemy`, `Boss`, `Combat`, `Sound`, `Effect` 모듈  
**위험도**: 낮음  
**작업:**
1. Enum파일들은 앞에 (E)(클래스명)으로 통일 ex.ESwordType
2. `DontDestroyOnLoadObject.cs`를 삭제
3. `FirstScript.cs` 삭제
4. 테스트: 빌드 확인

---

### Phase 5: 싱글톤/DDOL 오용 수정 (우선순위: 중간)
**목표**: 씬 전환 시 참조 유실 문제 해결  
**범위**: `EnemySpawner.cs`, `ControllerManager.cs`, `DamageFloaterManager.cs`  
**위험도**: 중간  
**작업:**
1. `EnemySpawner`를 `Singleton<T>`(씬 파괴)로 변경
2. `ControllerManager` 로직을 플레이어 프리팹 내부 컴포넌트로 이관
3. `DamageFloaterManager`의 `DontDestroySingleton` 상속 제거
4. 테스트: 씬 전환 시 정상 동작 확인

---

### Phase 6: EventManager 구현 (우선순위: 높음)
**목표**: 이벤트 시스템 중앙화  
**범위**: 새로운 `EventManager.cs` 생성, 전체 이벤트 마이그레이션  
**위험도**: 중간  
**작업:**
1. `EventManager.cs` 싱글톤 클래스 생성 (이벤트 등록/해제/발생 기능)
2. 기존 Action/Action<T> 기반 이벤트를 EventManager로 마이그레이션
3. 모든 매니저에서 Action 구독 → EventManager 구독으로 변경
4. 테스트: 이벤트 기능 정상 동작 확인

---

### Phase 7: DDD 계층 위반 수정 (우선순위: 낮음)
**목표**: 도메인 레이어에서 매니저 직접 호출 제거  
**범위**: `EnemyAI.cs`, `PlayerHealth.cs`, `StageManager.cs`  
**위험도**: 중간  
**작업:**
1. `EnemyAI.Die()`에서 매니저 호출 제거하고 EventManager를 통한 `OnDeath` 이벤트 발생
2. `PlayerHealth.Start()`에서 매니저 호출 제거
3. `StageManager.ClearAllEnemies()`에서 `FindGameObjectsWithTag` 제거
4. 테스트: 적 사망 후 보상 및 스테이지 진행 정상

---

### Phase 8: BigInteger 정밀도 최적화 (우선순위: 중간)
**목표**: UpgradeData 비용 계산에서 double 변환 제거  
**범위**: `UpgradeData.cs`  
**위험도**: 중간  
**작업:**
1. `GetCost` 메서드를 BigInteger 전용 연산으로 변경
2. `Math.Pow` 사용 제거
3. 테스트: 높은 레벨에서 비용 계산 정밀도 검증
4. 위 작업도 또한 모든 BigInteger를 double로 변환하면서 해결될 예정

---

### Phase 9: 공통 유틸리티 추출 (우선순위: 낮음)
**목표**: 코드 중복 제거  
**범위**: `EnemyAI.cs`, 관련 수학 연산 파일들  
**위험도**: 낮음  
**작업:**
1. `MultiplyBigInteger`를 `BigIntMath.cs` 유틸리티로 추출
2. `int` 기반 레거시 메서드 `[Obsolete]` 처리
3. 테스트: 빌드 확인
4. BigInteger를 제거함으로, BigInteger관련 코드는 제거

---

## F. 안전장치

### 1. 컴파일/플레이모드 체크 방법
- **Unity 빌드**: File > Build Settings > Build
- **에디터 플레이**: Unity 에디터에서 ▶ 플레이 버튼 클릭
- **에러 체크**: Console 창에서 빨강색 에러 메시지 확인

### 2. 최소 테스트 시나리오 (수동 테스트)
1. **플레이어 데미지**: 플레이어 공격 시 데미지 정상 표시
2. **적 사망**: 적 처치 시 보상(금화, 경험치) 정상 지급
3. **스테이지 진행**: 스테이지 클리어 후 다음 스테이지로 이동
4. **씬 전환**: 스테이지 리스폰 후 적 정상 스폰
5. **강화**: 검 강화 시 데미지 정상 증가
6. **이벤트**: EventManager를 통한 이벤트 발생/구독 정상

### 3. 롤백 전략
- **Git 브랜치**: 각 Phase 시작 전 새 브랜치 생성
- **커밋 단위**: Phase 단위 커밋 (작은 단위)
- **롤백 방법**: `git checkout [브랜치]` 또는 `git revert`

---

## G. EventManager 설계 (새로 추가)

### EventManager 구조
```csharp
// Core/EventManager.cs
public class EventManager : DontDestroySingleton<EventManager>
{
    private Dictionary<string, List<Delegate>> _eventDict = new();

    public void Subscribe(string eventName, Action listener)
    {
        if (!_eventDict.ContainsKey(eventName))
            _eventDict[eventName] = new List<Delegate>();
        _eventDict[eventName].Add(listener);
    }

    public void Unsubscribe(string eventName, Action listener)
    {
        if (_eventDict.TryGetValue(eventName, out var listeners))
        {
            listeners.Remove(listener);
        }
    }

    public void Invoke(string eventName)
    {
        if (_eventDict.TryGetValue(eventName, out var listeners))
        {
            foreach (var listener in listeners)
            {
                listener.DynamicInvoke();
            }
        }
    }
}
```

### 이벤트 이름 규칙
- **형식**: `[Module].[EventName]`
- **예시**:
  - `GameManager.OnPlayerDeath`
  - `Enemy.OnDeath`
  - `Currency.OnChanged`
  - `Stage.OnStageCleared`

---


수정사항 반영:
- ✅ 아웃게임 요소는 DDD 구조 유지, 이외는 단순 구조로 변경
- ✅ 인터페이스 유지 (옵션 C)
- ✅ EventManager 중앙화 구조 도입
- ✅ CritDamage는 struct로 변경
- ✅ 매니저는 최소 통합 (옵션 A)
