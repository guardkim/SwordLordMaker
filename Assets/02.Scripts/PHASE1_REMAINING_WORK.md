# Phase 1 완료: BGCodeGen Type-Safe 리팩토링

## 완료 상태

### 모든 Repository가 CodeGen 기반으로 변환 완료

| Repository | CodeGen 클래스 | 상태 |
|:---|:---|:---:|
| `SwordStatRepository.cs` | `DB_SwordStat` | ✅ 완료 |
| `EnemyStatRepository.cs` | `DB_EnemyStat` | ✅ 완료 |
| `BossStatRepository.cs` | `DB_BossStat` | ✅ 완료 |
| `PlayerStatRepository.cs` | `DB_PlayerStat` | ✅ 완료 |
| `CurrencyRepository.cs` | `DB_PlayerProfile` | ✅ 완료 |
| `StageRepository.cs` | `DB_StageStat` | ✅ 완료 |
| `OfflineRewardRepository.cs` | `DB_PlayerProfile` | ✅ 완료 |
| `UpgradeRepository.cs` | `DB_UpgradeData`, `DB_PlayerProfile` | ✅ 완료 |

---

## 변경 내용 요약

### 1. StageRepository.cs
- `BGRepo.I[TableName]` → `DB_StageStat.GetEntity(i)` 변경
- 문자열 상수 제거 (`TableName`, `StageIdField` 등)
- `entity.Get<T>(fieldName)` → `dbEntity.F_FieldName` 직접 접근

### 2. OfflineRewardRepository.cs
- `BGRepo.I[TableName]` → `DB_PlayerProfile.GetEntity(playerName)` 변경
- `FindEntityByName()` 메서드 제거
- `entity.Get<string>(LastLoginTimeField)` → `_playerEntity.F_LastLoginTime` 직접 접근

### 3. UpgradeRepository.cs
- `BGRepo.I[UpgradeDataTableName]` → `DB_UpgradeData.GetEntity(i)` 변경
- `BGRepo.I[PlayerProfileTableName]` → `DB_PlayerProfile.GetEntity(playerName)` 변경
- `entity.Get<T>(fieldName)` → `dbEntity.F_FieldName` 직접 접근
- `entity.Set(UpgradeLevelsField, json)` → `_playerEntity.F_UpgradeLevels = json` 직접 접근

---

## 검증 체크리스트

### 컴파일 확인
- [ ] Unity 에디터 컴파일 에러 없음

### 기능 테스트
- [ ] 스테이지 진행 정상 (StageRepository)
- [ ] 오프라인 보상 정상 (OfflineRewardRepository)
- [ ] 강화 시스템 정상 (UpgradeRepository)
  - [ ] 강화 데이터 로드
  - [ ] 강화 레벨 저장/로드
  - [ ] 강화 비용 계산
- [ ] 검 스탯 로드 정상 (SwordStatRepository)
- [ ] 적 스탯 로드 정상 (EnemyStatRepository)
- [ ] 보스 스탯 로드 정상 (BossStatRepository)
- [ ] 플레이어 스탯 로드/저장 정상 (PlayerStatRepository)
- [ ] 재화 로드/저장 정상 (CurrencyRepository)

---

## Phase 1 리팩토링 장점

1. **타입 안전성**: 컴파일 타임에 필드명/타입 오류 검출
2. **자동 완성**: IDE에서 `F_` 접두사로 필드명 자동 완성
3. **문자열 상수 제거**: 오타로 인한 런타임 오류 방지
4. **코드 간소화**: 불필요한 헬퍼 메서드 제거 (`FindEntityByName` 등)
5. **일관성**: 모든 Repository가 동일한 패턴으로 구현
