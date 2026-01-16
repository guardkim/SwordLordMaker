# Effect 시스템 기술 문서

## 1. 📂 모듈 개요 (Module Overview)

`EffectManager`는 SwordLordMaker 프로젝트에서 시각적 효과(VFX)를 효율적으로 관리하고 재생하기 위한 중앙 집중식 시스템입니다. 게임 내에서 발생하는 타격 효과, 스킬 연출 등의 이펙트를 오브젝트 풀링(Object Pooling) 기술을 통해 최적화하여 제공합니다.

### 주요 기능
- **중앙 관리**: 모든 이펙트 생성을 한 곳에서 제어하여 코드 복잡도 감소
- **오브젝트 풀링**: 빈번한 생성/파괴를 지양하여 런타임 성능 및 메모리 효율성 향상
- **자동 반환**: 이펙트 재생 후 지정된 시간이 지나면 자동으로 풀에 반환하는 수명 관리 시스템

---

## 2. 🏗️ 아키텍처 및 상호작용 (Architecture & Interactions)

### 계층 구조
- **Manager Layer**: 시스템의 핵심 로직이 위치하며, 싱글톤 패턴을 통해 애플리케이션 전역에서 접근 가능합니다.
- **Dependency**: `Singleton<T>` 유틸리티 클래스에 의존하며, Unity의 `GameObject` 시스템을 기반으로 동작합니다.

### 상호작용 흐름
1. **Request**: `Player` 또는 `Enemy` 시스템에서 `EffectManager.Instance.PlayHitVfx()` 호출
2. **Retrieve**: `EffectManager`가 내부 `Queue`에서 비활성화된 오브젝트를 추출 (풀이 비어있을 경우 새로 생성)
3. **Activation**: 위치와 회전값을 설정한 후 `SetActive(true)`로 활성화
4. **Deactivation**: 코루틴을 통해 지정된 `Lifetime` 후 `SetActive(false)` 처리 및 `Queue` 재삽입

---

## 3. 📝 상세 코드 분석 (Detailed Code Analysis)

### 핵심 클래스: `EffectManager.cs`

#### 1. 싱글톤 패턴 구현
`Singleton<EffectManager>`을 상속받아 구현되어 있어, `EffectManager.Instance`를 통해 어디서나 즉시 접근할 수 있습니다. `Initialize()` 메서드에서 초기 풀 설정을 수행합니다.

#### 2. 오브젝트 풀링 (Object Pooling)
`Queue<GameObject>` 자료구조를 사용하여 `HitVFX`와 `SkillVFX`를 각각 분리된 풀에서 관리합니다.
- `InitializePool()`: 시작 시 지정된 개수(`_poolSize`)만큼 프리팹을 미리 생성하여 메모리에 로드합니다.
- `GetFromPool()`: 큐에서 꺼내어 재사용하며, 큐가 비어있는 예외 상황에서는 `Instantiate`를 통해 동적으로 확장합니다.

#### 3. 수명 관리 (Lifetime Management)
`IEnumerator ReturnToPoolAfterDelay` 코루틴을 사용하여 이펙트가 화면에 표시된 후 자동으로 풀에 반환되도록 관리합니다. 이는 이펙트 프리팹 자체에 별도의 스크립트를 붙이지 않아도 매니저에서 일괄 제어가 가능하게 합니다.

---

## 4. 💡 설계 의도 및 구현 이유 (Design Rationale)

- **성능 최적화**: 모바일 RPG 환경에서는 수많은 타격 효과가 동시에 발생합니다. 매번 `Instantiate`와 `Destroy`를 호출하면 CPU 부하와 가비지 컬렉션(GC) 유발로 인한 프레임 드랍이 발생하므로, 오브젝트 풀링을 통해 이를 방지했습니다.
- **유지보수성**: 이펙트의 수명이나 종류를 `EffectManager` 한 곳에서 관리하므로, 전체 이펙트의 밸런싱이나 최적화 설정을 변경하기 용이합니다.
- **확장성**: 현재는 `Hit`와 `Skill`로 구분되어 있으나, 동일한 패턴을 사용하여 다양한 타입의 이펙트를 추가하기 쉬운 구조로 설계되었습니다.

---

## 5. 🎮 사용 가이드 (Usage Guide within Unity)

### Inspector 설정
1. **Hit VFX Prefab**: 타격 시 사용할 파티클 시스템 프리팹을 할당합니다.
2. **VFX Lifetime**: 이펙트가 화면에 머무를 시간을 설정합니다.
3. **Pool Size**: 게임 시작 시 미리 생성해 둘 이펙트 개수를 설정합니다. (기본값: 20)
4. **Skill VFX 관련 설정**: 스킬용 프리팹과 수명, 풀 크기를 동일하게 설정합니다.

### 코드 사용 예시
```csharp
// 일반 타격 이펙트 재생
EffectManager.Instance.PlayHitVfx(targetPosition);

// 회전값이 포함된 타격 이펙트 재생
EffectManager.Instance.PlayHitVfx(targetPosition, targetRotation);

// 스킬 이펙트 재생
EffectManager.Instance.PlaySkillVfx(skillPosition);
```

---

## 6. ⚠️ 크리티컬 리뷰 및 개선점 (Critical Review)

### 현재 구현의 한계
1. **가비지 생성**: `ReturnToPoolAfterDelay` 코루틴에서 매번 `new WaitForSeconds(delay)`를 호출하여 미세한 가비지를 생성합니다.
2. **풀 확장 제한 부재**: 풀이 비어있을 때 계속해서 새로 생성하므로, 과도한 이펙트 요청 시 메모리 점유율이 급격히 상승할 위험이 있습니다.
3. **유연성 부족**: 현재는 `Hit`와 `Skill` 두 가지 타입으로 하드코딩되어 있어, 수십 종류의 다른 이펙트를 관리하기에는 구조적 확장이 필요합니다.

### 향후 개선 방향
- **YieldInstruction 캐싱**: `WaitForSeconds` 객체를 딕셔너리 등에 캐싱하여 재사용함으로써 가비지 생성을 억제해야 합니다.
- **Dictionary 기반 관리**: `Enum` 또는 `String ID`를 키로 하는 `Dictionary<EffectType, Queue<GameObject>>` 구조로 변경하여 다양한 이펙트를 유연하게 등록하고 사용할 수 있도록 개선이 권장됩니다.
- **상한치 설정**: 풀의 최대 크기를 제한하고, 최대치 도달 시 가장 오래된 이펙트를 강제 재사용하는 로직 추가가 필요합니다.
