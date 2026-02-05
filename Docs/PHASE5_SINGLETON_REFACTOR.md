# Phase 5: 싱글톤/DDOL 오용 수정

## 개요

| 항목 | 내용 |
|:---|:---|
| **목표** | 씬 전환 시 SerializeField 참조 유실 문제 해결 |
| **범위** | `EnemySpawner.cs`, `ControllerManager.cs`, `DamageFloaterManager.cs` |
| **위험도** | 중간 |

---

## 현재 문제점 분석

### 문제의 핵심

`DontDestroySingleton`은 **씬 전환 후에도 유지**되는 객체입니다.
그러나 해당 객체의 **SerializeField 참조들은 씬에 종속적**입니다.

씬 전환 시:
1. DontDestroySingleton 객체는 유지됨
2. 기존 씬의 오브젝트들은 파괴됨
3. SerializeField 참조들이 `null`이 됨 (Missing Reference)

### 현재 싱글톤 구조

| 클래스 | 현재 상속 | SerializeField 참조 | 문제 |
|:---|:---|:---|:---|
| `EnemySpawner` | `DontDestroySingleton` | `_enemyPrefab`, `_spawnPoints[]`, `_bossSpawnPoint` | 씬 전환 시 유실 |
| `ControllerManager` | `DontDestroySingleton` | `_adelController`, `_hypoController`, `_pixelController` | 씬 전환 시 유실 |
| `DamageFloaterManager` | `DontDestroySingleton` | `DamageFloaterPrefab`, `SpawnPos` | 씬 전환 시 유실 |

### 참조 관계

```
EnemySpawner 참조하는 곳:
├── BaseSwordController.FindEnemies() → AliveEnemies
├── EnemyAI.ReturnToPoolAfterDelay() → Return()
└── StageManager
    ├── AliveEnemyCount 프로퍼티
    ├── SubscribeToEvents() → OnBossDiedEvent
    ├── SpawnEnemy() → SpawnWithMultiplier()
    ├── SpawnBoss() → SpawnBoss()
    └── ClearAllEnemies() → ReturnAll()

ControllerManager 참조하는 곳:
└── (외부 참조 없음 - 자체 동작만)

DamageFloaterManager 참조하는 곳:
├── EnemyAI.ShowDamageEffects() → ShowDamage()
└── DamageTestUI (테스트용)
```

---

## 해결 방안

### 1. EnemySpawner: `DontDestroySingleton` → `Singleton` 변경

**이유:**
- `_spawnPoints`, `_enemyPrefab` 등이 씬에 배치된 오브젝트
- 씬마다 다른 스폰 포인트를 가질 수 있음
- 씬 전환 시 새 씬의 EnemySpawner가 활성화되어야 함

**변경:**
```csharp
// 변경 전
public class EnemySpawner : DontDestroySingleton<EnemySpawner>

// 변경 후
public class EnemySpawner : Singleton<EnemySpawner>
```

**영향:**
- `HasInstance` 프로퍼티 사용 불가 → `Instance != null` 체크로 변경 필요
- StageManager의 `UnsubscribeFromEvents()`에서 `HasInstance` 사용 중

**추가 작업:**
- `Singleton<T>`에 `HasInstance` 프로퍼티 추가 필요

---

### 2. ControllerManager: 플레이어 프리팹 내부 컴포넌트로 이관

**이유:**
- 검 컨트롤러들(`_adelController` 등)이 플레이어 프리팹의 자식으로 존재
- 외부에서 `ControllerManager.Instance` 참조하는 곳 없음
- 플레이어와 생명주기 동일하게 관리하는 것이 자연스러움

**변경:**
```csharp
// 변경 전
public class ControllerManager : DontDestroySingleton<ControllerManager>

// 변경 후
public class ControllerManager : MonoBehaviour
```

**작업:**
1. 싱글톤 상속 제거
2. `Initialize()` → `Awake()` 또는 `Start()`로 변경
3. 플레이어 프리팹에 컴포넌트로 부착
4. 검 컨트롤러 참조를 `GetComponentInChildren<T>()`로 자동 탐색 (선택적)

---

### 3. DamageFloaterManager: `DontDestroySingleton` → `Singleton` 변경

**이유:**
- `DamageFloaterPrefab`이 씬/프로젝트 에셋 참조
- 씬마다 다른 데미지 플로터 설정을 가질 수 있음
- 프리팹 참조는 유지되나, `SpawnPos` 같은 씬 오브젝트 참조가 유실될 수 있음

**변경:**
```csharp
// 변경 전
public class DamageFloaterManager : DontDestroySingleton<DamageFloaterManager>

// 변경 후
public class DamageFloaterManager : Singleton<DamageFloaterManager>
```

---

## 작업 순서

### Phase 5-1: Singleton Instance 자동 생성 제거

**파일:** `Assets/02.Scripts/Util/Singleton.cs`

**변경 전:**
```csharp
public static T Instance
{
    get
    {
        if (_instance == null)
        {
            _instance = new GameObject(nameof(T)).AddComponent<T>();  // 자동 생성
        }
        return _instance;
    }
}
```

**변경 후:**
```csharp
public static T Instance => _instance;
```

- 씬에 배치된 싱글톤만 사용
- 없으면 `null` 반환
- 호출하는 쪽에서 `Instance != null` 체크

---

### Phase 5-2: StageManager HasInstance 제거

**파일:** `Assets/02.Scripts/Stage/Manager/StageManager.cs`

`EnemySpawner.HasInstance` → `EnemySpawner.Instance != null`로 변경

---

### Phase 5-3: EnemySpawner 변경

**파일:** `Assets/02.Scripts/Enemy/Manager/EnemySpawner.cs`

1. 상속 변경: `DontDestroySingleton<EnemySpawner>` → `Singleton<EnemySpawner>`

---

### Phase 5-4: ControllerManager 변경

**파일:** `Assets/DamageFloater/01.Scripts/Manager/ControllerManager.cs`

1. 상속 변경: `DontDestroySingleton<ControllerManager>` → `MonoBehaviour`
2. `Initialize()` 오버라이드 제거, `Start()`로 변경
3. 플레이어 프리팹 구조 확인 후 컴포넌트 이관

**주의:**
- 현재 씬에서 ControllerManager가 어디에 배치되어 있는지 확인 필요
- 플레이어 프리팹과 검 컨트롤러들의 계층 구조 확인 필요

---

### Phase 5-5: DamageFloaterManager 변경

**파일:** `Assets/DamageFloater/01.Scripts/DamageFloater/DamageFloaterManager.cs`

1. 상속 변경: `DontDestroySingleton<DamageFloaterManager>` → `Singleton<DamageFloaterManager>`
2. `SpawnPos` 필드 제거 (사용처 확인 후, 테스트용이면 제거)

---

## 파일별 변경 요약

| 파일 | 변경 내용 |
|:---|:---|
| `Singleton.cs` | `Instance` 자동 생성 제거 (`=> _instance`) |
| `StageManager.cs` | `HasInstance` → `Instance != null` 체크로 변경 |
| `EnemySpawner.cs` | `DontDestroySingleton` → `Singleton` |
| `ControllerManager.cs` | `DontDestroySingleton` → `MonoBehaviour`, 플레이어 프리팹 내부로 이관 |
| `DamageFloaterManager.cs` | `DontDestroySingleton` → `Singleton` |

---

## 테스트 체크리스트

- [ ] 게임 시작 시 적 정상 스폰
- [ ] 검 자동 발사 정상 동작
- [ ] 데미지 플로터 정상 표시
- [ ] (씬 전환 기능이 있다면) 씬 전환 후 모든 기능 정상 동작
- [ ] 스테이지 클리어 후 다음 스테이지 적 정상 스폰

---

## 롤백 전략

1. 작업 전 현재 상태 커밋
2. Phase 5 완료 후 별도 커밋
3. 문제 발생 시 `git revert`

---

## 추가 고려사항

### ControllerManager 이관 시 주의점

현재 ControllerManager는:
- `_adelController`, `_hypoController`, `_pixelController`를 SerializeField로 받음
- 이들이 플레이어 프리팹 내부에 있다면, 계층 구조 확인 필요

**두 가지 방안:**

**A. SerializeField 유지 (권장)**
```
Player (프리팹)
├── ControllerManager (컴포넌트)
├── AdelFlyingSwordController
├── HypoSwordController
└── PixelSwordController
```
- 프리팹 내부에서 직접 참조 연결

**B. GetComponentInChildren 사용**
```csharp
private void Awake()
{
    _adelController = GetComponentInChildren<AdelFlyingSwordController>();
    _hypoController = GetComponentInChildren<HypoSwordController>();
    _pixelController = GetComponentInChildren<PixelSwordController>();
}
```
- 자동 탐색으로 연결
