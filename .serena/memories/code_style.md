# C# 코딩 스타일 가이드

## 포맷팅
- 들여쓰기: 공백 4칸 (탭 금지)
- 중괄호: Allman 스타일 (개행 후 중괄호)
- 한 줄에 하나의 문장만

## 명명 규칙
| 대상 | 규칙 | 예시 |
|-----|------|-----|
| 클래스, 메서드, 프로퍼티 | PascalCase | `DamageFloater`, `ShowDamage()` |
| 로컬 변수, 파라미터 | camelCase | `damageValue` |
| private 필드 | `_` 접두사 | `_activeTexts` |
| static 필드 | `s_` 접두사 | `s_instance` |
| 인터페이스 | `I` 접두사 | `IDamageable` |

## 언어 기능
- 타입 키워드: `int`, `string` (Int32, String 아님)
- `var`: 타입 명확할 때만
- 문자열: 보간 사용 `$"{value}"`
- 비동기: `async/await` 권장
- 대리자: `Action<>`, `Func<>` 사용

## 금지 사항
- `catch (Exception e)` 금지 - 구체적 예외만
- 연속 밑줄 `__` 금지
- 단일 문자 변수명 금지 (루프 `i`, `j` 제외)
- XML 주석 `<summary>` 금지

## 설계 원칙
- SOLID (특히 SRP, OCP)
- 디미터의 법칙: `a.getB().getC()` 금지
- 함수는 하나의 작업만 수행
