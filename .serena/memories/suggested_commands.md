# 개발 명령어

## 시스템 (Windows)
- `dir`: 디렉토리 목록 (ls 대신)
- `cd`: 디렉토리 이동
- `type`: 파일 내용 출력 (cat 대신)
- `findstr`: 텍스트 검색 (grep 대신)

## Git
- `git status`: 변경 상태 확인
- `git diff`: 변경 내용 확인
- `git add <file>`: 스테이징
- `git commit -m "message"`: 커밋
- `git push`: 푸시
- `git pull`: 풀

## Unity
- 빌드: Unity 에디터 > File > Build Settings
- 테스트: Unity 에디터 > Window > General > Test Runner
- 플레이: Unity 에디터에서 Play 버튼

## 프로젝트 구조
```
Assets/
├── 01.Scenes/          # MainScene, StartScene
├── 02.Scripts/         # 프로젝트 스크립트
├── 03.Prefabs/         # 게임 프리팹
├── DamageFloater/      # 핵심 게임플레이 모듈
└── Settings/           # URP 설정
```
