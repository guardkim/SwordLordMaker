# SwordLordMaker 프로젝트 개요

## 목적
Unity 6 기반 액션 게임. 비행 검(Flying Sword) 시스템과 데미지 플로터가 핵심 게임플레이 요소.

## 기술 스택
- **엔진**: Unity 6
- **언어**: C#
- **렌더링**: URP (Universal Render Pipeline)
- **주요 라이브러리**:
  - DOTween: 애니메이션
  - TextMesh Pro: 텍스트 렌더링
  - BGDatabase: 데이터 관리
  - Feel (MMFeedbacks): 피드백 시스템

## 아키텍처
- 인게임: 전략 패턴 (비행 검 시스템)
- 아웃게임: DDD 4계층 (Data → Repository → Manager → UI)

## 핵심 시스템
1. **비행 검 시스템**: BaseFlyingSword, BaseSwordController, 3가지 검 타입
2. **데미지 플로터**: DamageFloaterManager, 7가지 스타일
3. **컨트롤러 관리**: ControllerManager 싱글톤
