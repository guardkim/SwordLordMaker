# SwordLordMaker 컨텐츠 요약

## Flying Sword 시스템 (비행 검)

전략 패턴 기반으로 3가지 궤도(Adel, Hypo, Pixel)가 독립적으로 구현되어 있습니다. Adel은 8자 궤도로 지속적인 가디언 형태, Hypo는 하이포사이클로이드 수학 공식을 사용한 꽃잎 폭격, Pixel은 무한 루프(∞) 형태로 중심 흡착 공격을 수행합니다. ControllerManager 싱글톤이 검 모드 전환 및 자동 발사를 관리하며, UpgradeManager 이벤트를 구독하여 실시간 스탯 갱신을 지원합니다. 모든 궤도는 BaseFlyingSword와 BaseSwordController 추상 클래스를 기반으로 하여 OCP를 준수하며, BigInteger를 통한 무한 스케일링을 지원합니다. 3D/2D 물리 환경 모두를 지원하며 유연한 확장성을 가집니다.

---

## DamageFloater 시스템 (데미지 텍스트)

DamageFloaterManager 싱글톤이 외부 요청을 처리하며 DOTween 기반의 7가지 애니메이션 스타일(Basic, Blade, Volcano 등)을 제공합니다. PixelTextHelper는 TMP Rich Text Tag를 활용하여 픽셀 폰트 스타일을 렌더링하며, Critical 시 접두어를 자동 추가합니다. BigInteger 지원을 위해 CurrencyFormatter를 사용하여 숫자를 포맷팅하며, Billboard 기능으로 카메라를 바라보도록 회전합니다. FloaterOption 구조체를 통해 인스펙터에서 연출 파라미터를 자유롭게 조정할 수 있으며, Z-Order 관리로 Z-Fighting을 방지합니다. 현재는 Instantiate/Destroy를 사용하므로 오브젝트 풀링 미도입 상태입니다.

---

## Currency 시스템 (재화 관리)

CurrencyManager 싱글톤이 Gold와 Ruby를 BigInteger로 관리하며, 60초 간격의 자동 저장과 앱 종료 시 동기 저장을 지원합니다. ICurrencyRepository 인터페이스를 통해 데이터 영속화를 추상화하였으며, BGDatabase와 연동 시 string ↔ BigInteger 변환을 처리합니다. OnCurrencyChanged 이벤트를 통해 UI에 실시간 갱신 알림을 전송하며, Ruby는 즉시 저장되고 Gold는 배치 저장됩니다. TrySpend/Add/TrySpendRuby 등의 안전한 재화 연산 메서드를 제공하며, 비즈니스 로직은 Manager 계층에만 존재합니다. UI는 이벤트만 구독하여 비즈니스 로직 침투가 없습니다.

---

## Enemy 시스템 (적 관리)

EnemySpawner가 UnityEngine.Pool.ObjectPool을 사용하여 일반 몬스터를 효율적으로 관리합니다. FSM 기반의 EnemyAI가 Idle, Chase, Attack, SkillAttack, Hit, Dead 6가지 상태로 동작하며, NavMeshAgent를 사용하여 이동합니다. EnemyStat 레코드는 MaxHP, AttackDamage, GoldReward를 BigInteger 타입으로 정의하여 무한 스케일링을 지원합니다. 스테이지 배율을 적용하여 스폰하며, float 배율 적용 시 정밀도 손실을 줄이기 위해 1000을 곱하는 보정 로직이 있습니다. 보스는 별도 스폰 로직과 AoE 스킬 공격을 수행하며, 보스는 풀링하지 않고 Destroy 처리합니다. EnemyAnimation이 풀링 시 Rebind()로 초기화를 지원합니다.

---

## Player 시스템 (플레이어)

PlayerMovement(CharacterController)와 PlayerAutoMovement(NavMeshAgent) 두 가지 이동 시스템을 지원합니다. PlayerHealth가 BigInteger 기반의 체력을 관리하며 IDamageable을 구현하여 피격을 처리합니다. PlayerStat 레코드가 불변성을 보장하며, PlayerStatManager 싱글톤이 레벨업 로직을 담당합니다. OnHealthChanged 이벤트가 UI에 HP 갱신을 알리며, 사망 시 OnDeath → GameManager.OnPlayerDeath → StageManager로 전달되는 이벤트 체인이 구현되어 있습니다. 데스 후 5초 대기 후 자동 부활 및 1스테이지 리셋이 진행되며, UpgradeManager 이벤트를 구독하여 체력 증가 시 갱신합니다.

---

## Stage 시스템 (스테이지 진행)

StageManager 싱글톤이 스테이지 진행을 관리하며, OnStageStarted/OnStageCleared 이벤트를 제공합니다. 1초 간격으로 일반 몬스터를 무한 스폰하며, 보스 스폰 시 스폰 중지 후 모든 몬스터 제거가 발생합니다. StageStat 레코드가 스테이지 데이터를 저장하며, 스테이지 배율을 EnemySpawner에 전달합니다. 보스 처치 시 5초 딜레이 후 다음 스테이지로 전환되며, 최대 스테이지 도달 시 무한 반복됩니다. GameManager.OnRequestStageRestart 이벤트를 구독하여 스테이지 리셋을 지원하며, 플레이어 사망 시 스폰 중지 및 적 제거를 수행합니다.

---

## Upgrade 시스템 (강화)

UpgradeManager 싱글톤이 TryUpgrade 메서드로 골드 소모 및 레벨업 로직을 수행합니다. UpgradeData 레코드가 BaseCost * (CostMultiplier ^ CurrentLevel) 수식으로 비용을 계산하며, BigInteger를 사용하여 거대 비용 처리를 지원합니다. ApplyUpgrades 메서드가 현재 강화 레벨을 기반으로 스탯 보너스를 합산하여 최종 스탯을 산출합니다. 수량형 보너스(공격력, 체력)는 BigInteger로, 비율형 보너스(치명타 확률)는 float로 하이브리드 계산을 수행합니다. OnUpgraded 이벤트가 UI에 강화 완료를 알리며, CurrencyManager.OnCurrencyChanged가 강화 버튼 활성/비활성을 제어합니다. UpgradeRepository가 BGDatabase와 연동하며, PlayerUpgradeLevels가 JSON 직렬화로 상태를 저장합니다.

---

## UI 시스템 (사용자 인터페이스)

DDD Presentation Layer로 설계되어 비즈니스 로직이 침투하지 않습니다. Manager 이벤트(CurrencyManager.OnCurrencyChanged, UpgradeManager.OnUpgraded 등)를 구독하여 실시간 갱신을 수행합니다. CurrencyUI가 BigInteger 재화를 Dirty 플래그로 LateUpdate에 효율적으로 갱신하며, UpgradeSlotUI가 강화 정보를 표시합니다. RedDotManager, PopupManager, TabButton 등 공통 UI 컴포넌트가 구현되어 있습니다. FadeManager가 씬 전환 효과를 처리하며, PlayerProfileUI가 플레이어 정보를 표시합니다. StageUI가 스테이지 진행 상황을 표시하며, 모든 UI는 Manager를 직접 호출하지 않고 이벤트만 구독합니다.

---

## 아키텍처 및 디자인 패턴

DDD 4계층 구조(Data, Repository, Manager, UI)가 100% 준수되어 있으며, 모든 Manager가 DontDestroySingleton을 상속받아 싱글톤 패턴이 적용되었습니다. Observer 패턴(System.Action 이벤트)이 적용되어 Manager ↔ UI 간 느슨한 결합을 달성했습니다. Strategy 패턴이 Flying Sword 시스템에 적용되어 궤도 전략이 캡슐화되어 있으며, Repository 패턴이 데이터 영속화를 추상화합니다. ObjectPool 패턴이 EnemySpawner에 적용되어 GC 부하를 최소화합니다. 총 9개의 Repository가 구현되어 있으며, BigInteger가 전투/재화 필드에 전면 적용되어 무한 스케일링을 지원합니다.

---

## 데이터 계층 및 Repository

Data Layer가 record 타입을 사용하여 불변성을 보장하며, SwordStat, PlayerStat, EnemyStat 등이 record로 정의되어 있습니다. BigInteger가 MaxHP, AttackDamage, Gold, Ruby 등 핵심 필드에 적용되어 있으며, CritDamageMultiplier만 float로 구현되어 있습니다. BGDatabase가 BigInteger를 지원하지 않으므로 string ↔ BigInteger 변환으로 연동합니다. 총 9개의 Repository가 구현되어 있으며(ICurrencyRepository, IUpgradeRepository, IPlayerStatRepository 등), 인터페이스를 통해 의존성이 분리되어 있습니다. UpgradeData.IsMaxLevel이 항상 false를 반환하여 무한 강화가 가능한 상태입니다.
