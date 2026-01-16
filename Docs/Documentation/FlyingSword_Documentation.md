# 비행검 (Flying Sword) 시스템 기술 문서

본 문서는 `SwordLordMaker` 프로젝트의 핵심 전투 시스템인 비행검(Flying Sword) 시스템과 이를 보조하는 데미지 플로터(Damage Floater) 모듈의 구조 및 구현 상세를 설명합니다.

---

## 1. 📂 모듈 개요 (Module Overview)

비행검 시스템은 플레이어 주위를 부유하며 자동으로 적을 추적하고 공격하는 핵심 공격 모듈입니다. 데미지 플로터 시스템은 비행검의 공격 결과를 시각적으로 피드백하는 역할을 수행합니다.

### 1.1 주요 기능
- **다양한 공격 궤도**: 수학적 공식을 활용한 3가지 고유 궤도(Adel, Hypo, Pixel) 제공.
- **자동 타겟팅**: 사거리 내의 적을 자동으로 탐색하고 최적의 경로로 공격.
- **무한 스케일링 지원**: `BigInteger`를 지원하여 수치 인플레이션에 대응하는 데미지 표시.
- **풍부한 시각적 효과**: DOTween 기반의 7가지 데미지 애니메이션 스타일 제공.

---

## 2. 🏗️ 아키텍처 및 상호작용 (Architecture & Interactions)

본 시스템은 유연성과 확장성을 위해 다양한 디자인 패턴을 조합하여 설계되었습니다.

### 2.1 적용된 디자인 패턴
- **Strategy Pattern**: `BaseFlyingSword`와 `BaseSwordController`를 추상화하여 새로운 궤도 타입을 코드 수정 없이 추가할 수 있는 구조를 갖추었습니다.
- **Singleton Pattern**: `ControllerManager`와 `DamageFloaterManager`를 통해 시스템 전역에서 접근 가능한 단일 진입점을 제공합니다.
- **Object Pool Pattern**: 빈번하게 생성/파괴되는 `DamageFloater` 객체의 성능 부하를 줄이기 위해 풀링 기법을 적용했습니다.
- **Factory Pattern**: `DamageFloaterManager`가 상황(Single/Multi/BigInt)에 맞는 플로터를 생성하고 옵션을 주입하는 팩토리 역할을 수행합니다.

### 2.2 시스템 상호작용 흐름
1. `ControllerManager`가 현재 활성화된 검 타입을 판단하고 `Fire()`를 호출합니다.
2. 각 타입의 `SwordController`가 `SwordPrefab`을 생성하고 초기화합니다.
3. 생성된 `FlyingSword`는 고유의 알고리즘에 따라 적에게 접근하여 충돌을 감지합니다.
4. 충돌 발생 시 `IDamageable` 인터페이스를 통해 데미지를 전달하고, `DamageFloaterManager`에 출력 요청을 보냅니다.

---

## 3. 📝 상세 코드 분석 (Detailed Code Analysis)

### 3.1 기반 클래스 (Base Classes)
- **`BaseFlyingSword`**: 모든 비행검의 부모 클래스로, 높이 제한(`ClampHeight`), 데미지 처리(`TryDealDamage`), `SwordStat` 초기화 로직을 포함합니다.
- **`BaseSwordController`**: 검의 생성 및 타겟 탐색(`FindEnemies`)을 담당하는 추상 컨트롤러입니다.

### 3.2 궤도별 구현 특징
- **Adel (8자 궤도)**: `Mathf.Sin`과 `Mathf.Sin(2*t)`를 조합하여 8자 모양의 리사주 곡선을 생성합니다. 사출(Eject), 공격(Attack), 복귀(Return)의 상태 머신을 가집니다.
- **Hypo (하이포사이클로이드)**: 별 모양의 장미 곡선(Hypocycloid Rose)을 수학적으로 계산합니다. 타겟 주변을 화려하게 회전하며 다단 히트 공격을 수행합니다.
- **Pixel (무한 루프)**: 두 개의 원형 궤도를 교차하며 무한대(∞) 기호를 그리듯 이동합니다. 원의 중심을 주기적으로 변경하는 로직이 핵심입니다.

### 3.3 데미지 플로터 시스템
- **`DamageFloaterManager`**: 단일 히트, 연타, `BigInteger` 데미지 출력 API를 제공합니다.
- **`PixelTextHelper`**: TextMesh Pro의 태그 기능을 활용하여 글자별 지그재그 효과와 픽셀 폰트 렌더링을 처리합니다.
- **애니메이션 스타일**: `Basic`, `Blade`, `Volcano` 등 7가지 스타일이 DOTween Sequence로 구현되어 있어 직관적인 연출 수정이 가능합니다.

---

## 4. 💡 설계 의도 및 구현 이유 (Design Rationale)

### 4.1 Strategy 패턴 사용 이유
방치형 RPG 특성상 새로운 등급의 검이나 특수 스킬이 추가될 가능성이 높습니다. 궤도 로직을 별도의 클래스로 분리함으로써 기존 로직의 손상 없이 새로운 공격 패턴을 안전하게 확장할 수 있습니다.

### 4.2 3가지 궤도 타입의 구분
- **Adel**: 안정적인 타격감과 명확한 공격 주기를 제공합니다.
- **Hypo**: 화려한 시각적 효과와 다단 히트를 통해 속도감을 강조합니다.
- **Pixel**: 예측 불가능하고 역동적인 움직임으로 전투의 단조로움을 해소합니다.

### 4.3 Object Pool 및 BigInteger 지원
수백 개의 데미지 텍스트가 동시에 발생하는 환경에서 가비지 컬렉션을 최소화하기 위해 객체 풀링은 필수적입니다. 또한, 게임 후반부의 거대 수치를 표현하기 위해 모든 UI 시스템을 문자열 포맷팅 기반의 `BigInteger` 대응 구조로 설계했습니다.

---

## 5. 🎮 사용 가이드 (Usage Guide within Unity)

### 5.1 ControllerManager 설정
1. 씬에 `ControllerManager` 오브젝트를 생성합니다.
2. 각 필드(`_adelController`, `_hypoController`, `_pixelController`)에 씬 내에 배치된 해당 컨트롤러를 할당합니다.
3. `ModeText` 필드에 현재 모드를 표시할 TMP UI를 연결합니다.

### 5.2 DamageFloaterManager 설정
1. `DamageFloaterPrefab`에 `DamageFloater` 컴포넌트가 포함된 프리팹을 할당합니다.
2. `SingleFloaterOption` 및 `MultiFloaterOption`에서 기본 애니메이션 스타일과 지속 시간 등을 설정합니다.
3. `DamageFloater` 프리팹 내의 `NonCritFont`와 `CritFont`에 각각 일반/크리티컬용 TMP Sprite Asset을 할당해야 합니다.

---

## 6. ⚠️ 크리티컬 리뷰 및 개선점 (Critical Review)

### 6.1 잠재적 성능 이슈
- **타겟 재탐색**: 현재 `AdelFlyingSwordController` 등에서 주기적으로 `FindGameObjectsWithTag`를 호출하고 있습니다. 적의 수가 많아질 경우 성능 저하의 원인이 될 수 있으므로 `EnemyManager` 등에서 리스트를 관리받는 방식으로 개선이 필요합니다.
- **수학 연산 부하**: `Hypo`나 `Pixel` 궤도는 매 프레임 삼각함수 연산을 수행합니다. 검의 개수가 매우 많아질 경우를 대비해 연산 결과를 캐싱하거나 Job System으로의 이전을 고려해볼 수 있습니다.

### 6.2 구조적 복잡성
- **궤도 변경 로직**: 현재 `ControllerManager`에서 직접 구체 클래스를 참조하고 있어 DIP(의존 역전 원칙)를 완벽히 준수하지 못하고 있습니다. 향후 컨트롤러 리스트를 인터페이스 기반으로 관리하도록 리팩토링할 것을 권장합니다.
