# Project: RESO — 1단계 프로토타입 설계

**문서 버전**: v1.0
**작성일**: 2026-09-22
**범위**: 개발 로드맵 1단계 (오디오-노트 동기화, 기본 판정 로직, 키 입력 최적화) — 원본 기획서 [Project_RESO_제안서.md](../../../../../Project%20RESO/Project_RESO_제안서.md) 9장 기준

## 목표

4K 레인 기준으로 곡 재생과 노트 낙하가 동기화되고, 판정선에서 입력 시각과 노트 시각의 오차를 PERFECT/GREAT/GOOD/BAD/MISS로 분류하는 최소 플레이 가능 루프를 만든다. 메뉴, 스킨 시스템, 대전, 리더보드, 모바일 포팅은 범위 밖이며 이후 별도 서브프로젝트로 설계한다.

## 전제 조건 (확인됨)

- FMOD가 `Assets/Plugins/FMOD`에 임포트되어 있음
- 테스트 곡: `FMOD_Project/Song_Perfect_Run/Song_Perfect_Run.fspro`, 이벤트명 `Perfect_Run`, 길이 165.9초
- 뱅크 빌드 완료: `FMOD_Project/Song_Perfect_Run/Build/Desktop/{Master.bank, Master.strings.bank}`
- Unity FMOD 연동의 `sourceProjectPath`가 비어 있어 뱅크 자동 동기화가 안 걸려 있음 — 구현 시 `Song_Perfect_Run.fspro` 경로로 설정 필요
- FMOD 프로젝트에 템포 마커가 없어 실제 박자와 정렬된 채보는 아님 (테스트용 임의 타이밍)

## 데이터 흐름

```
FMOD(Perfect_Run) 재생
  → SongConductor: 매 프레임 getTimelinePosition() 폴링 → "현재 곡 시간(초)" 제공
      → NoteSpawner: 곡 시간 + 리드타임(2.0s) 기준으로 다가올 노트를 스폰
      → NoteView: 스폰~히트 시각 사이를 시간 보간으로 낙하 (프레임 드랍에도 곡 시간 기준이라 밀리지 않음)
      → InputHandler(D/F/J/K): 키 입력 시각 = 같은 SongConductor 시계 기준
      → JudgmentSystem: 레인 내 미판정 노트 중 가장 이른 것과 시간차 계산 → 등급 분류 → 제거
      → JudgmentUI: 등급 스프라이트 표시, 콤보/정확도 갱신
```

## 컴포넌트

| 컴포넌트 | 역할 | 의존성 |
|---|---|---|
| `ChartData` | JSON 역직렬화 순수 데이터 클래스 (`notes: [{time, lane}]`) | 없음 |
| `SongConductor` | FMOD `EventInstance` 재생 제어 + 매 프레임 곡 시간(초) 제공 | FMOD |
| `NoteSpawner` | 곡 시간 기준 리드타임만큼 앞선 노트를 `NoteView`로 스폰 | `ChartData`, `SongConductor` |
| `NoteView` | 스폰~히트 시각 사이 시간 보간 낙하, 판정선 통과 후 자동 MISS | `SongConductor` |
| `JudgmentClassifier` | 순수 함수: 시간차(ms) → 등급. MonoBehaviour 아님, 독립 테스트 가능 | 없음 |
| `JudgmentSystem` | 레인별 미판정 노트 큐에서 최선참 노트 판정, 결과 이벤트 발행 | `JudgmentClassifier`, `NoteSpawner` |
| `InputHandler` | Unity Input System으로 D/F/J/K 키다운 감지 → `JudgmentSystem.TryHit(lane, songTime)` | Input System |
| `JudgmentUI` | 판정 이벤트 구독 → 슬라이스된 PERFECT/GREAT/... 스프라이트 표시, 콤보(TMP 텍스트) 갱신 | 판정 이벤트 |

## 판정 기준 (기획서 5.1 그대로)

| 등급 | 허용 오차 |
|---|---|
| PERFECT | ±18ms |
| GREAT | ±40ms |
| GOOD | ±75ms |
| BAD | ±120ms |
| MISS | 범위 초과 또는 미입력 |

## 에셋 슬라이싱 계획

원본: `C:\Project RESO\assets\assets.png`. Unity Sprite Editor 자동 슬라이싱(알파 채널 바운딩박스, 내장 기능)으로 처리 — 별도 슬라이싱 툴 불필요.

| 용도 | 원본 소스 | 비고 |
|---|---|---|
| 판정 텍스트 5종 | PERFECT/GREAT/GOOD/BAD/MISS 라벨 | 판정 시 판정선 위 표시 |
| 노트 토큰 4종 | 좌상단 다이아몬드 4색(보라/파랑/화이트/레드) | 레인 0~3 매핑 |
| 레인 트랙 | 파란 세로 바(rail) 1종 | 4레인 배경 반복 배치 |

**범위 밖(스킵)**: 콤보 숫자 스프라이트, 프로필/배경 연출, 랭크 배지, 메뉴 버튼. 콤보 카운트는 TMP 텍스트로 대체.

## 채보·입력 세부값

- 레인 4개, 키 매핑: `D F J K` (`Keyboard.current`)
- 채보 포맷: `{"notes":[{"time":1.5,"lane":0}, ...]}` (time = 곡 시작 기준 초)
- 리드타임: 2.0초 고정
- 테스트 채보: 곡 첫 15~20초 구간에 15~20개 노트 수동 배치
- MISS: 노트 시각 + 120ms까지 미입력 시 자동 MISS

## 테스트

`JudgmentClassifier`는 순수 함수이므로 EditMode 테스트로 경계값(±18/40/75/120ms 및 그 초과)을 검증한다. 이것이 판정 로직에 대한 최소 runnable check다.

## 이후 단계 (범위 밖, 별도 설계 필요)

메뉴/선택 화면, 스킨 시스템, 코스·대전 모드, 리더보드/네트워크, 모바일 포팅 — 이 프로토타입이 동작을 확인한 뒤 각각 브레인스토밍한다.
