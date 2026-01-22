# SwordLordMaker 개선점 요약

## Flying Sword 시스템

CritDamageMultiplier가 float로 구현되어 있어 거대 수치에서 정밀도 손실이 발생할 수 있으므로 BigInteger 기반 백분율 시스템으로 전환이 필요합니다. 검 타입별 사운드 및 VFX 피드백이 차별화되지 않아 있으므로 각 궤도 특성에 맞는 시각/청각 피드백 추가를 권장합니다. 현재 3가지 궤도(Adel, Hypo, Pixel)만 구현되어 있으므로 새로운 궤도(나선형, 지그재그 등) 확장 시 현재 Strategy 패턴 구조를 활용할 수 있습니다. Adel의 순차 공격 로직이 복잡하므로 상태 전환 로직 간소화 또는 상태 머신 시각화 툴 도입을 고려합니다. 검 사출 시 쿨타임 배율 계산이 UpgradeManager에 의존하므로 캐싱 메커니즘 도입으로 성능 최적화가 가능합니다.

---

## DamageFloater 시스템

현재 Instantiate/Destroy를 빈번하게 호출하고 있으므로 Generic Object Pool 적용으로 메모리 파편화 및 GC 부하를 제거해야 합니다. 7가지 애니메이션 스타일 중 Volcano2가 성능 효율이 좋으므로 이를 기본값으로 검토하거나 성능 프로파일링이 필요합니다. PixelTextHelper의 TMP Rich Text Tag 사용 시 텍스트 길이에 따른 드로우콜 증가 가능성이 있으므로 텍스트 길이 제한 또는 캐싱을 고려합니다. FloaterOption의 구조체가 [Serializable]로 되어 있으나 인스펙터에서 수동 조정이 필요하므로 프리셋 시스템 도입을 권장합니다. Billboard 기능이 LateUpdate에서 매 프레임 회전을 수행하므로 성능 최적화를 위해 카메라 이동 시에만 갱신하도록 수정합니다.

---

## Currency 시스템

Ruby는 즉시 저장되고 Gold는 60초 배치 저장되는 비일관적인 저장 정책이 있으므로 저장 정책 통일 또는 설정 외부화가 필요합니다. TrySpend/TrySpendRuby 실패 시 사용자 피드백이 없으므로 UI 알림 또는 사운드 피드백 추가가 필요합니다. CurrencyRepository가 PlayerSessionManager에 의존하므로 테스트 가능성을 위해 의존성 주입 방식으로 수정을 권장합니다. 60초 자동 저장 간격이 하드코딩되어 있으므로 ScriptableObject 등 설정 외부화가 필요합니다. OnCurrencyChanged 이벤트 파라미터가 (CurrencyType, BigInteger)이므로 변경 내역(증감)을 포함하도록 확장하여 UI 애니메이션 효과 지원을 고려합니다.

---

## Enemy 시스템

EnemySpawner.Return()에서 보스를 풀에 반환하지 않고 Destroy 처리하는 로직이 안전하지만, 명시적인 IsBoss 체크 로직 개선이 필요합니다. NavMeshAgent 업데이트 주기가 거리에 따라 유동적으로 조절되지만, 타겟탐색 최적화를 위해 공간 분할(QuadTree 등) 도입을 고려합니다. EnemyHPBar의 (double)current / (double)max 연산 시 매우 큰 BigInteger 변환 오버헤드가 있으므로 캐싱 또는 0~1 정규화 사전 계산이 필요합니다. 보스 AoE 스킬의 차징 시간(_skillChargeTime)이 고정되어 있으므로 스테이지별 난이도에 따른 가변 설정을 지원해야 합니다. EnemyAnimation의 Rebind() 호출이 풀링 시마다 발생하므로 오버헤드를 줄이기 위해 상태 캐싱을 도입합니다.

---

## Player 시스템

CritDamage가 float로 구현되어 있어 무한 스케일링 시 정밀도 문제가 발생하므로 BigInteger로 전환 작업이 시급합니다. PlayerHealth의 사망/부활 로직이 GameManager와 강하게 결합되어 있으므로 이벤트 기반으로 완전히 분리할 것을 권장합니다. PlayerAutoMovement의 방황 및 회피 로직이 단순하므로 상태 머신 도입 또는 행동 트리(Behavior Tree) 확장을 고려합니다. PlayerStatManager의 레벨업 로직이 경험치 계산에만 집중되어 있으므로 레벨업 보상 시스템(스킬 해금, 스탯 포인트 등)을 확장합니다. PlayerMovement와 PlayerAutoMovement가 동시에 활성화될 수 없으므로 상태 관리 로직(인터페이스 기반 전환)으로 개선합니다.

---

## Stage 시스템

스테이지 전환 시 남은 적들을 태그 기반으로 검색하여 제거하는 로직(GameObject.FindGameObjectsWithTag)이 성능상 비효율적입니다. 1초 간격 스폰 코루틴이 보스 스폰 전까지 무한히 실행되므로 최대 스폰 수 제한 또는 스폰 지연 도입을 고려합니다. StageStat의 BossStatId가 null일 경우 보스 스폰이 무시되므로 디버깅용 로깅 강화가 필요합니다. 스테이지 리셋 시 StageManager.RestartFromStage가 호출되지만, 재시작 애니메이션 또는 딜레이가 부족하여 갑작스러운 전환이 발생합니다. _maxStageId가 Repository에서 한 번만 로드되므로 런타임 스테이지 추가가 불가능하므로 동적 로드 방식으로 개선합니다.

---

## Upgrade 시스템

UpgradeData.IsMaxLevel이 항상 false를 반환하므로 MaxLevel 체크 로직(currentLevel >= MaxLevel)를 추가하여 무한 강화 방어가 필요합니다. CritDamageMultiplier의 float 타입이 BigInteger 시스템과 불일치하므로 BigInteger 기반 백분율 시스템으로 전환해야 합니다. ApplyUpgrades 메서드가 매 호출 시 강화 레벨을 반복하여 계산하므로 결과 캐싱(CacheInvalidation 포함)으로 성능 최적화가 필요합니다. UI가 최대 레벨 도달 시 'MAX' 표시를 하지 않으므로 UpgradeSlotUI에 최대 레벨 표시 기능 추가가 필요합니다. UpgradeRepository의 BGDatabase 연동 시 JSON 직렬화가 PlayerProfile에 의존하므로 직렬화 방식을 Repository 내부로 캡슐화해야 합니다.

---

## UI 시스템

CurrencyUI의 LateUpdate 매 프레임 Dirty 플래그 체크는 비효율적이므로 이벤트 기반 직접 갱신으로 변경하여 성능 최적화가 필요합니다. RedDotManager의 조건 체크 로직이分散되어 있으므로 중앙 집중식 체크 스케줄러 도입으로 관리 효율성을 높입니다. PopupManager의 PopupPriority enum이 있으나 팝업 스택 관리 로직이 복잡하므로 스택 시각화 툴 또는 간단한 로직으로 개선합니다. UpgradeSlotUI가 OnCurrencyChanged 이벤트를 구독하여 버튼 색상을 변경하지만, 복잡한 조건 체크로 인해 리팩토링이 필요합니다. TabButton의 하단 탭 전환 로직이 BottomTap에 의존하므로 인터페이스 기반 확장을 통해 유연한 탭 시스템을 구현합니다.

---

## 아키텍처 및 디자인 패턴

BigInteger와 float의 혼재(CritDamage 등)로 인한 타입 일관성 문제를 해결하기 위해 전체 데이터 타입 재정의가 필요합니다. Manager 간 통신에 이벤트 기반 Observer 패턴이 적용되었으나, 이벤트 파라미터의 확장성이 부족하므로 제네릭 이벤트 시스템 도입을 고려합니다. EnemySpawner에만 ObjectPool이 적용되어 있으므로 DamageFloater 등 다른 시스템에도 풀링 확장이 필요합니다. Repository가 BGDatabase에 강하게 결합되어 있으므로 인터페이스 분리 원칙(ISP)을 적용하여 단일 책임으로 분리합니다. Singleton이 DontDestroySingleton으로 구현되어 있으나 테스트 가능성을 위해 의존성 주입 컨테이너 도입을 고려합니다.

---

## 데이터 계층 및 Repository

SwordStat.CritDamageMultiplier가 float로 선언되어 있어 무한 스케일링 시 정밀도 손실 위험이 있으므로 BigInteger 또는 고정 소수점으로 전환해야 합니다. BGDatabase가 BigInteger를 지원하지 않으므로 string 변환 로직이 각 Repository에 중복되어 있으므로 공용 Converter 클래스로 추출합니다. 9개의 Repository가 구현되어 있지만 일부 Repository가 특정 DB 엔티티에 직접 의존하므로 인터페이스 완전 분리가 필요합니다. UpgradeData의 비용 계산(double 사용)과 BigInteger 간의 혼합 사용으로 인한 정밀도 문제를 해결하기 위해 BigInteger만 사용하는 통일된 계산 방식을 적용합니다. PlayerUpgradeLevels의 JSON 직렬화가 BGEntity 외부에서 수행되므로 Repository 내부로 캡슐화하여 데이터 접근을 통제해야 합니다.
