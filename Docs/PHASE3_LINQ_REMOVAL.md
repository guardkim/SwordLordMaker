# Phase 3: LINQ 제거 작업 계획

## 개요

| 항목 | 내용 |
|:---|:---|
| **목표** | 성능 민감한 구간에서 LINQ 제거 (GC 부하 감소) |
| **범위** | `BaseSwordController.cs`, `AdelFlyingSwordController.cs`, `StageManager.cs` |
| **위험도** | 낮음 |
| **예상 결과** | 프레임당 메모리 할당량 감소 |

---

## 현재 LINQ 사용 현황

### 1. BaseSwordController.cs (42-51줄)

**메서드**: `FindEnemies()`

```csharp
// 현재 코드
protected GameObject[] FindEnemies()
{
    return GameObject.FindGameObjectsWithTag("Enemy")
        .Where(e =>
        {
            var enemy = e.GetComponent<EnemyAI>();
            return enemy == null || !enemy.IsDead;
        })
        .ToArray();
}
```

**문제점**:
- `.Where()`: 매 호출 시 새로운 IEnumerable 생성
- `.ToArray()`: 새로운 배열 할당 (GC 부하)
- `GetComponent<EnemyAI>()`: 매 적마다 호출 (성능 비용)

---

### 2. AdelFlyingSwordController.cs (159-165줄)

**메서드**: `FilterEnemiesByDistance()`

```csharp
// 현재 코드
private GameObject[] FilterEnemiesByDistance(GameObject[] enemies)
{
    Vector3 playerPos = transform.position;
    return enemies
        .Where(e => Vector3.Distance(e.transform.position, playerPos) <= MaxTargetDistance)
        .ToArray();
}
```

**문제점**:
- `.Where().ToArray()`: 매 호출 시 새로운 배열 할당

---

### 3. AdelFlyingSwordController.cs (217-238줄)

**메서드**: `RetargetSwords()`

```csharp
// 현재 코드 (문제 부분만)
if (_activeSwords.All(s => s.HasTarget())) return;
```

**문제점**:
- `.All()`: LINQ 확장 메서드, 매 호출 시 델리게이트 생성

---

### 4. AdelFlyingSwordController.cs (204-215줄)

**메서드**: `IncrementTurnIndex()`

```csharp
// 현재 코드
private void IncrementTurnIndex()
{
    if (_activeSwords.Count == 0) return;

    _activeSwords.Sort((a, b) => a.OrderIndex.CompareTo(b.OrderIndex));
    AdelFlyingSword nextSword = _activeSwords.FirstOrDefault(s => s.OrderIndex > _currentAttackerOrderIndex);

    if (!nextSword)
        nextSword = _activeSwords[0];

    _currentAttackerOrderIndex = nextSword.OrderIndex;
}
```

**문제점**:
- `.FirstOrDefault()`: LINQ 확장 메서드, 매 호출 시 델리게이트 생성

---

### 5. StageManager.cs (251-284줄)

**메서드**: `ClearAllEnemies()`

```csharp
// 현재 코드 (문제 부분만)
foreach (var enemy in _aliveEnemies.ToArray())
{
    if (enemy != null)
    {
        EnemySpawner.Instance?.Return(enemy);
    }
}
_aliveEnemies.Clear();
```

**문제점**:
- `.ToArray()`: 컬렉션 순회 중 수정을 피하기 위해 사용하지만, 매 호출 시 새 배열 할당

---

### 6. HypoSwordController.cs, PixelSwordController.cs (128-142줄)

**메서드**: `FilterEnemiesByDistance()`

```csharp
// 현재 코드 - 이미 foreach 사용하지만 ToArray() 남아있음
private GameObject[] FilterEnemiesByDistance(GameObject[] enemies)
{
    var result = new List<GameObject>();  // 매 호출 시 새 List 생성
    Vector3 playerPos = transform.position;

    foreach (var enemy in enemies)
    {
        if (Vector3.Distance(enemy.transform.position, playerPos) <= MaxTargetDistance)
        {
            result.Add(enemy);
        }
    }

    return result.ToArray();  // 새 배열 할당
}
```

**문제점**:
- `new List<GameObject>()`: 매 호출 시 새 리스트 생성
- `.ToArray()`: 최종 배열 할당

---

## 리팩토링 계획

### 작업 1: BaseSwordController.FindEnemies() 단순화

**변경 전**:
```csharp
protected GameObject[] FindEnemies()
{
    return GameObject.FindGameObjectsWithTag("Enemy")
        .Where(e =>
        {
            var enemy = e.GetComponent<EnemyAI>();
            return enemy == null || !enemy.IsDead;
        })
        .ToArray();
}
```

**변경 후** (EnemySpawner.AliveEnemies 직접 접근):
```csharp
protected IReadOnlyList<EnemyAI> FindEnemies()
{
    return EnemySpawner.Instance?.AliveEnemies;
}
```

**개선점**:
- LINQ `.Where().ToArray()` 제거
- `FindGameObjectsWithTag("Enemy")` 제거
- `GetComponent<EnemyAI>()` 제거
- 버퍼/변환 불필요 - **매니저에 직접 접근**
- 반환 타입 `IReadOnlyList<EnemyAI>`로 단순화

**선행 작업**: EnemySpawner에 `AliveEnemies` 관리 추가 필요 (추가 고려사항 섹션 참조)

**호출부 수정 필요**: 반환 타입 변경으로 `GetRandomEnemyTarget()` 등 수정 필요

---

### 작업 2: AdelFlyingSwordController.FilterEnemiesByDistance() 수정

**변경 전**:
```csharp
private GameObject[] FilterEnemiesByDistance(GameObject[] enemies)
{
    Vector3 playerPos = transform.position;
    return enemies
        .Where(e => Vector3.Distance(e.transform.position, playerPos) <= MaxTargetDistance)
        .ToArray();
}
```

**변경 후**:
```csharp
// 재사용 가능한 리스트 (필드로 선언)
private readonly List<GameObject> _filteredEnemyBuffer = new();

private GameObject[] FilterEnemiesByDistance(GameObject[] enemies)
{
    _filteredEnemyBuffer.Clear();
    Vector3 playerPos = transform.position;
    float maxDistSqr = MaxTargetDistance * MaxTargetDistance;

    foreach (GameObject enemy in enemies)
    {
        float distSqr = (enemy.transform.position - playerPos).sqrMagnitude;
        if (distSqr <= maxDistSqr)
        {
            _filteredEnemyBuffer.Add(enemy);
        }
    }

    return _filteredEnemyBuffer.ToArray();
}
```

**개선점**:
- LINQ `.Where().ToArray()` 제거
- `sqrMagnitude` 사용으로 `Vector3.Distance()` 내부의 `Mathf.Sqrt()` 제거 (추가 최적화)
- `_filteredEnemyBuffer` 재사용

---

### 작업 3: AdelFlyingSwordController.RetargetSwords() 수정

**변경 전**:
```csharp
private void RetargetSwords()
{
    if (_activeSwords.Count == 0) return;
    if (_activeSwords.All(s => s.HasTarget())) return;  // LINQ
    // ...
}
```

**변경 후**:
```csharp
private void RetargetSwords()
{
    if (_activeSwords.Count == 0) return;

    // All() 대체: 타겟 없는 검이 있는지 확인
    bool allHaveTargets = true;
    foreach (AdelFlyingSword sword in _activeSwords)
    {
        if (!sword.HasTarget())
        {
            allHaveTargets = false;
            break;
        }
    }
    if (allHaveTargets) return;

    // 나머지 로직 동일...
}
```

**개선점**:
- LINQ `.All()` 제거
- 조기 종료(early exit)로 불필요한 순회 방지

---

### 작업 4: AdelFlyingSwordController.IncrementTurnIndex() 수정

**변경 전**:
```csharp
private void IncrementTurnIndex()
{
    if (_activeSwords.Count == 0) return;

    _activeSwords.Sort((a, b) => a.OrderIndex.CompareTo(b.OrderIndex));
    AdelFlyingSword nextSword = _activeSwords.FirstOrDefault(s => s.OrderIndex > _currentAttackerOrderIndex);

    if (!nextSword)
        nextSword = _activeSwords[0];

    _currentAttackerOrderIndex = nextSword.OrderIndex;
}
```

**변경 후**:
```csharp
private void IncrementTurnIndex()
{
    if (_activeSwords.Count == 0) return;

    _activeSwords.Sort((a, b) => a.OrderIndex.CompareTo(b.OrderIndex));

    // FirstOrDefault() 대체
    AdelFlyingSword nextSword = null;
    foreach (AdelFlyingSword sword in _activeSwords)
    {
        if (sword.OrderIndex > _currentAttackerOrderIndex)
        {
            nextSword = sword;
            break;
        }
    }

    if (nextSword == null)
        nextSword = _activeSwords[0];

    _currentAttackerOrderIndex = nextSword.OrderIndex;
}
```

**개선점**:
- LINQ `.FirstOrDefault()` 제거
- 조기 종료(early exit)로 불필요한 순회 방지

---

### 작업 5: StageManager.ClearAllEnemies() 수정

**변경 전**:
```csharp
foreach (var enemy in _aliveEnemies.ToArray())
{
    if (enemy != null)
    {
        EnemySpawner.Instance?.Return(enemy);
    }
}
_aliveEnemies.Clear();
```

**변경 후**:
```csharp
// 역순 순회로 ToArray() 제거
for (int i = _aliveEnemies.Count - 1; i >= 0; i--)
{
    EnemyAI enemy = _aliveEnemies[i];
    if (enemy != null)
    {
        EnemySpawner.Instance?.Return(enemy);
    }
}
_aliveEnemies.Clear();
```

**개선점**:
- `.ToArray()` 제거
- 역순 순회로 컬렉션 수정 문제 회피 (단, Clear()로 일괄 삭제하므로 순회 중 삭제 없음)

**대안** (더 안전한 방법):
```csharp
// 임시 버퍼 재사용 (필드로 선언)
private readonly List<EnemyAI> _enemyClearBuffer = new();

private void ClearAllEnemies()
{
    _enemyClearBuffer.Clear();
    _enemyClearBuffer.AddRange(_aliveEnemies);

    foreach (EnemyAI enemy in _enemyClearBuffer)
    {
        if (enemy != null)
        {
            EnemySpawner.Instance?.Return(enemy);
        }
    }
    _aliveEnemies.Clear();
    // ...
}
```

---

### 작업 6: HypoSwordController, PixelSwordController 최적화

두 클래스는 이미 foreach를 사용하지만, 버퍼 재사용 패턴 적용 필요.

**변경 후**:
```csharp
// 재사용 가능한 리스트 (필드로 선언)
private readonly List<GameObject> _filteredEnemyBuffer = new();

private GameObject[] FilterEnemiesByDistance(GameObject[] enemies)
{
    _filteredEnemyBuffer.Clear();
    Vector3 playerPos = transform.position;
    float maxDistSqr = MaxTargetDistance * MaxTargetDistance;

    foreach (GameObject enemy in enemies)
    {
        float distSqr = (enemy.transform.position - playerPos).sqrMagnitude;
        if (distSqr <= maxDistSqr)
        {
            _filteredEnemyBuffer.Add(enemy);
        }
    }

    return _filteredEnemyBuffer.ToArray();
}
```

---

## 파일별 변경 요약

| 파일 | 변경 내용 | 추가/제거 |
|:---|:---|:---|
| `EnemySpawner.cs` | `AliveEnemies` 관리 추가, `ReturnAll()` 메서드 추가 | `_aliveEnemies`, `AliveEnemies`, `ReturnAll()` 추가 |
| `StageManager.cs` | `_aliveEnemies` 제거, `ClearAllEnemies()` 단순화 | `_aliveEnemies` 제거 |
| `BaseSwordController.cs` | `FindEnemies()` → `EnemySpawner.Instance.AliveEnemies` 직접 접근 | 반환타입 `IReadOnlyList<EnemyAI>`로 변경 |
| `AdelFlyingSwordController.cs` | `FilterEnemiesByDistance()` `.Where()` 제거, `RetargetSwords()` `.All()` 제거, `IncrementTurnIndex()` `.FirstOrDefault()` 제거 | `_filteredEnemyBuffer` 추가 |
| `HypoSwordController.cs` | `FilterEnemiesByDistance()` 버퍼 재사용 + sqrMagnitude 최적화 | `_filteredEnemyBuffer` 추가 |
| `PixelSwordController.cs` | `FilterEnemiesByDistance()` 버퍼 재사용 + sqrMagnitude 최적화 | `_filteredEnemyBuffer` 추가 |

---

## 테스트 체크리스트

### EnemySpawner (AliveEnemies 관리)
- [ ] 적 스폰 시 `AliveEnemies`에 정상 추가되는지 확인
- [ ] 적 사망/반환 시 `AliveEnemies`에서 정상 제거되는지 확인
- [ ] `ReturnAll()` 호출 시 모든 적이 풀로 반환되는지 확인
- [ ] `AliveEnemies.Count`가 실제 활성 적 수와 일치하는지 확인

### 검 시스템 (SwordController)
- [ ] 플레이어 주변에 적이 있을 때 검이 정상적으로 적을 타겟팅하는지 확인
- [ ] 적이 사망한 후 검이 다른 적으로 리타겟팅되는지 확인
- [ ] 적이 없을 때 null 참조 오류가 발생하지 않는지 확인
- [ ] 검 타입 전환(Adel, Hypo, Pixel) 시 정상 동작 확인
- [ ] Adel 검의 공격 순서(턴) 정상 진행 확인

### 스테이지 시스템 (StageManager)
- [ ] 스테이지 클리어 시 `EnemySpawner.ReturnAll()` 정상 호출 확인
- [ ] 스테이지 재시작 시 적 풀 반환 정상 동작 확인
- [ ] 보스 처치 후 스테이지 전환 정상 확인
- [ ] `AliveEnemyCount` 프로퍼티가 `EnemySpawner.AliveEnemies.Count` 반환 확인

### 성능
- [ ] Unity Profiler로 GC Alloc 감소 확인 (선택)
- [ ] `FindGameObjectsWithTag("Enemy")` 호출 제거 확인

---

## 롤백 전략

1. 작업 전 브랜치 생성: `git checkout -b phase3-linq-removal`
2. 각 파일 수정 후 개별 커밋
3. 문제 발생 시: `git checkout Refactor` 또는 `git revert`

---

## 추가 고려사항

### ToArray() 완전 제거 (선택적)

현재 `ToArray()`는 호출자가 배열을 기대하기 때문에 유지됩니다. 완전히 제거하려면:

1. 반환 타입을 `IReadOnlyList<GameObject>`로 변경
2. 호출하는 모든 메서드에서 배열 대신 IReadOnlyList 사용
3. 랜덤 접근 시 인덱서 사용 (`enemies[Random.Range(0, enemies.Count)]`)

이는 Phase 3 범위를 벗어나므로, 필요시 별도 작업으로 진행.

### 책임 분리 리팩토링: AliveEnemies 관리 주체 변경 (권장)

**현재 문제: 책임 분리 위반**

| 클래스 | 현재 역할 | 올바른 역할 |
|:---|:---|:---|
| `StageManager` | 스테이지 관리 + `_aliveEnemies` 관리 | **스테이지 데이터/진행만** |
| `EnemySpawner` | Pool 관리만 | Pool 관리 + **활성화된 적 관리** |

`StageManager`는 스테이지 정보(현재 스테이지, 스테이지 데이터)를 관리해야 하고,
`EnemySpawner`는 몬스터 스폰과 **살아있는 몬스터 관리**를 담당해야 합니다.

---

**작업 7: EnemySpawner에 AliveEnemies 관리 추가**

```csharp
// EnemySpawner.cs
private readonly List<EnemyAI> _aliveEnemies = new();
public IReadOnlyList<EnemyAI> AliveEnemies => _aliveEnemies;

private void OnTakeFromPool(EnemyAI enemy)
{
    enemy.gameObject.SetActive(true);
    _aliveEnemies.Add(enemy);  // 추가
}

private void OnReturnedToPool(EnemyAI enemy)
{
    _aliveEnemies.Remove(enemy);  // 추가
    enemy.ResetForPool();
    enemy.gameObject.SetActive(false);
}
```

---

**작업 8: StageManager에서 _aliveEnemies 제거**

```csharp
// StageManager.cs - 변경 전
private readonly List<EnemyAI> _aliveEnemies = new();

private void SpawnEnemy(StageStat stage)
{
    EnemyAI enemy = EnemySpawner.Instance.SpawnWithMultiplier(...);
    _aliveEnemies.Add(enemy);  // 제거
}

public void OnEnemyDied(EnemyAI enemy)
{
    _aliveEnemies.Remove(enemy);  // 제거
}
```

```csharp
// StageManager.cs - 변경 후
// _aliveEnemies 필드 제거
// AliveEnemyCount 프로퍼티 수정
public int AliveEnemyCount => EnemySpawner.Instance?.AliveEnemies.Count ?? 0;

private void SpawnEnemy(StageStat stage)
{
    EnemySpawner.Instance.SpawnWithMultiplier(...);
    // EnemySpawner가 자동으로 AliveEnemies에 추가
}

// OnEnemyDied는 EnemySpawner.Return() 호출 시 자동 처리
```

---

**작업 9: BaseSwordController.FindEnemies() 단순화**

**변경 전:**
```csharp
protected GameObject[] FindEnemies()
{
    return GameObject.FindGameObjectsWithTag("Enemy")
        .Where(e =>
        {
            var enemy = e.GetComponent<EnemyAI>();
            return enemy == null || !enemy.IsDead;
        })
        .ToArray();
}
```

**변경 후 (단순화):**
```csharp
protected IReadOnlyList<EnemyAI> FindEnemies()
{
    return EnemySpawner.Instance?.AliveEnemies;
}
```

**개선점:**
- `FindGameObjectsWithTag("Enemy")` 제거
- `GetComponent<EnemyAI>()` 제거
- LINQ `.Where().ToArray()` 제거
- 버퍼/변환 불필요 - **직접 접근**
- 반환 타입을 `IReadOnlyList<EnemyAI>`로 변경하여 `GameObject` 변환 불필요

---

**작업 10: FindEnemies() 호출부 수정**

반환 타입이 `GameObject[]` → `IReadOnlyList<EnemyAI>`로 변경되므로 호출부 수정 필요:

```csharp
// 변경 전
GameObject[] enemies = FindEnemies();
Transform target = enemies[Random.Range(0, enemies.Length)].transform;

// 변경 후
IReadOnlyList<EnemyAI> enemies = FindEnemies();
if (enemies == null || enemies.Count == 0) return;
Transform target = enemies[Random.Range(0, enemies.Count)].transform;
```

---

**작업 11: StageManager.ClearAllEnemies() 단순화**

```csharp
private void ClearAllEnemies()
{
    // EnemySpawner에게 모든 적 반환 위임
    EnemySpawner.Instance?.ReturnAll();

    // 보스 제거
    if (_currentBoss != null)
    {
        Destroy(_currentBoss.gameObject);
        _currentBoss = null;
    }
    _bossSpawned = false;
}
```

```csharp
// EnemySpawner.cs에 추가
public void ReturnAll()
{
    for (int i = _aliveEnemies.Count - 1; i >= 0; i--)
    {
        Return(_aliveEnemies[i]);
    }
}
```
