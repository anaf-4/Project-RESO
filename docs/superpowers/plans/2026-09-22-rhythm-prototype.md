# RESO 리듬 프로토타입 Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** FMOD로 재생되는 테스트 곡(`Perfect_Run`)과 동기화된 4레인 노트 낙하 + PERFECT/GREAT/GOOD/BAD/MISS 판정 루프를 Unity에서 플레이 가능하게 만든다.

**Architecture:** FMOD `EventInstance`의 타임라인 위치를 매 프레임 폴링하는 `SongConductor`를 단일 시간 기준으로 삼고, `NoteSpawner`/`NoteView`/`JudgmentSystem`이 전부 이 시간에 맞춰 동작한다. 판정 등급 분류(`JudgmentClassifier`)와 노트 이동 계산(`NoteMotion`)은 MonoBehaviour와 분리된 순수 함수로 빼서 EditMode 테스트로 검증한다.

**Tech Stack:** Unity 6 (URP 2D), FMOD for Unity (`Assets/Plugins/FMOD`, 어셈블리명 `FMODUnity`), Unity Input System (`Unity.InputSystem`), Unity Test Framework (NUnit, EditMode).

**Spec:** [docs/superpowers/specs/2026-09-22-rhythm-prototype-design.md](../specs/2026-09-22-rhythm-prototype-design.md)

## Global Constraints

- 판정 오차 허용치(원본 그대로): PERFECT ±18ms, GREAT ±40ms, GOOD ±75ms, BAD ±120ms, 그 외 MISS
- 레인 4개, 키 매핑: `D F J K` (인덱스 0~3), Unity Input System `Keyboard.current` 사용
- 리드타임(스폰→판정선 낙하 시간): 2.0초 고정
- FMOD 이벤트 경로: `event:/Perfect_Run` (마스터 폴더 직속, 하위 폴더 없음)
- FMOD 어셈블리 참조명: `FMODUnity`
- 채보 JSON 스키마: `{"notes":[{"time":<seconds:float>,"lane":<0-3:int>}, ...]}`
- 콤보 카운터는 스프라이트 숫자가 아닌 `UnityEngine.UI.Text`로 표시 (스펙 3장 스킵 항목)

---

## Task 1: FMOD 뱅크 소스 경로 연결

**Files:**
- Modify: `Assets/Plugins/FMOD/Resources/FMODStudioSettings.asset:237-239`

**Interfaces:**
- Consumes: 없음 (설정 파일 편집)
- Produces: `sourceProjectPath`가 설정되어 이후 태스크에서 `RuntimeManager.CreateInstance("event:/Perfect_Run")`가 실제 뱅크를 찾을 수 있음

- [ ] **Step 1: sourceProjectPath 값 설정**

`Assets/Plugins/FMOD/Resources/FMODStudioSettings.asset`의 237~239번 줄:

```yaml
  sourceProjectPath: 
  sourceBankPath: 
  sourceBankPathUnformatted: 
```

를 다음으로 교체:

```yaml
  sourceProjectPath: FMOD_Project/Song_Perfect_Run/Song_Perfect_Run.fspro
  sourceBankPath: 
  sourceBankPathUnformatted: 
```

- [ ] **Step 2: 뱅크 갱신 트리거**

Unity MCP의 `Unity_RunCommand` 툴로 Editor에서 `UnityEditor.AssetDatabase.Refresh();`를 실행한다. FMOD 통합은 `sourceProjectPath`가 설정된 상태에서 에디터 포커스/리프레시 시 자동으로 뱅크를 `Assets/StreamingAssets/`로 복사한다.

- [ ] **Step 3: 검증**

`Unity_GetConsoleLogs`(또는 `Unity_ReadConsole`)로 FMOD 관련 에러가 없는지 확인. 다음 명령으로 뱅크가 복사됐는지 확인:

```bash
find "E:/Project RESO/Assets/StreamingAssets" -iname "*.bank"
```

`Master.bank`, `Master.strings.bank`가 나와야 한다. 안 나오면 Unity 메뉴 `FMOD > Refresh Banks`를 `Unity_ManageMenuItem`으로 실행 후 재확인.

- [ ] **Step 4: Commit**

```bash
git add "Assets/Plugins/FMOD/Resources/FMODStudioSettings.asset"
git commit -m "chore: connect FMOD source project path for bank sync"
```

(뱅크 파일 자체는 `Assets/StreamingAssets/`에 생성되며 커밋 대상에 포함해도 된다 — `git add -A`로 함께 커밋)

---

## Task 2: Grade enum + JudgmentClassifier (판정 등급 분류)

**Files:**
- Create: `Assets/Scripts/Rhythm/Rhythm.asmdef`
- Create: `Assets/Scripts/Rhythm/Tests/EditMode/Rhythm.EditModeTests.asmdef`
- Create: `Assets/Scripts/Rhythm/Grade.cs`
- Create: `Assets/Scripts/Rhythm/JudgmentClassifier.cs`
- Test: `Assets/Scripts/Rhythm/Tests/EditMode/JudgmentClassifierTests.cs`

**Interfaces:**
- Consumes: 없음
- Produces: `enum Grade { Perfect, Great, Good, Bad, Miss }`, `static class JudgmentClassifier { const float PerfectWindowMs=18f, GreatWindowMs=40f, GoodWindowMs=75f, BadWindowMs=120f; static Grade Classify(float deltaMs); }` — 이후 모든 태스크가 `JudgmentClassifier.BadWindowMs` 등 상수와 `Classify`를 사용한다.

- [ ] **Step 1: 런타임 어셈블리 정의 생성**

`Assets/Scripts/Rhythm/Rhythm.asmdef`:

```json
{
    "name": "Rhythm",
    "rootNamespace": "",
    "references": [
        "FMODUnity",
        "Unity.InputSystem"
    ],
    "includePlatforms": [],
    "excludePlatforms": [],
    "allowUnsafeCode": false,
    "overrideReferences": false,
    "precompiledReferences": [],
    "autoReferenced": true,
    "defineConstraints": [],
    "versionDefines": [],
    "noEngineReferences": false
}
```

- [ ] **Step 2: EditMode 테스트 어셈블리 정의 생성**

`Assets/Scripts/Rhythm/Tests/EditMode/Rhythm.EditModeTests.asmdef`:

```json
{
    "name": "Rhythm.EditModeTests",
    "rootNamespace": "",
    "references": [
        "Rhythm",
        "UnityEngine.TestRunner",
        "UnityEditor.TestRunner"
    ],
    "includePlatforms": [
        "Editor"
    ],
    "excludePlatforms": [],
    "allowUnsafeCode": false,
    "overrideReferences": true,
    "precompiledReferences": [
        "nunit.framework.dll"
    ],
    "autoReferenced": true,
    "defineConstraints": [
        "UNITY_INCLUDE_TESTS"
    ],
    "versionDefines": [],
    "noEngineReferences": false
}
```

- [ ] **Step 3: Grade enum 작성**

`Assets/Scripts/Rhythm/Grade.cs`:

```csharp
public enum Grade
{
    Perfect,
    Great,
    Good,
    Bad,
    Miss
}
```

- [ ] **Step 4: 실패하는 테스트 작성**

`Assets/Scripts/Rhythm/Tests/EditMode/JudgmentClassifierTests.cs`:

```csharp
using NUnit.Framework;

public class JudgmentClassifierTests
{
    [TestCase(0f, Grade.Perfect)]
    [TestCase(18f, Grade.Perfect)]
    [TestCase(-18f, Grade.Perfect)]
    [TestCase(18.1f, Grade.Great)]
    [TestCase(-40f, Grade.Great)]
    [TestCase(40.1f, Grade.Good)]
    [TestCase(75f, Grade.Good)]
    [TestCase(75.1f, Grade.Bad)]
    [TestCase(120f, Grade.Bad)]
    [TestCase(120.1f, Grade.Miss)]
    [TestCase(500f, Grade.Miss)]
    [TestCase(-500f, Grade.Miss)]
    public void Classify_ReturnsExpectedGrade(float deltaMs, Grade expected)
    {
        Assert.AreEqual(expected, JudgmentClassifier.Classify(deltaMs));
    }
}
```

- [ ] **Step 5: 테스트 실행 → 실패 확인**

Unity Test Runner(EditMode)에서 `JudgmentClassifierTests` 실행. `JudgmentClassifier`가 없으므로 컴파일 에러로 FAIL해야 한다. (`Unity_ManageMenuItem`으로 `Window/General/Test Runner`를 열거나, `Unity_RunCommand`로 테스트 실행 API 호출)

- [ ] **Step 6: 최소 구현 작성**

`Assets/Scripts/Rhythm/JudgmentClassifier.cs`:

```csharp
public static class JudgmentClassifier
{
    public const float PerfectWindowMs = 18f;
    public const float GreatWindowMs = 40f;
    public const float GoodWindowMs = 75f;
    public const float BadWindowMs = 120f;

    public static Grade Classify(float deltaMs)
    {
        float abs = System.Math.Abs(deltaMs);
        if (abs <= PerfectWindowMs) return Grade.Perfect;
        if (abs <= GreatWindowMs) return Grade.Great;
        if (abs <= GoodWindowMs) return Grade.Good;
        if (abs <= BadWindowMs) return Grade.Bad;
        return Grade.Miss;
    }
}
```

- [ ] **Step 7: 테스트 실행 → 통과 확인**

같은 방법으로 `JudgmentClassifierTests` 재실행. 12개 케이스 모두 PASS.

- [ ] **Step 8: Commit**

```bash
git add Assets/Scripts/Rhythm/Rhythm.asmdef Assets/Scripts/Rhythm/Tests/EditMode/Rhythm.EditModeTests.asmdef Assets/Scripts/Rhythm/Grade.cs Assets/Scripts/Rhythm/JudgmentClassifier.cs Assets/Scripts/Rhythm/Tests/EditMode/JudgmentClassifierTests.cs
git commit -m "feat: add judgment grade classification"
```

---

## Task 3: ChartData / ChartLoader + 테스트 채보

**Files:**
- Create: `Assets/Scripts/Rhythm/ChartData.cs`
- Create: `Assets/Scripts/Rhythm/ChartLoader.cs`
- Create: `Assets/Resources/Charts/perfect_run_test.json`
- Test: `Assets/Scripts/Rhythm/Tests/EditMode/ChartLoaderTests.cs`

**Interfaces:**
- Consumes: 없음
- Produces: `[System.Serializable] class NoteData { float time; int lane; }`, `[System.Serializable] class ChartData { NoteData[] notes; }`, `static class ChartLoader { static ChartData LoadFromResources(string resourcePath); }` — Task 9(NoteSpawner)가 `ChartLoader.LoadFromResources("Charts/perfect_run_test")`로 사용한다.

- [ ] **Step 1: 실패하는 테스트 작성**

`Assets/Scripts/Rhythm/Tests/EditMode/ChartLoaderTests.cs`:

```csharp
using NUnit.Framework;

public class ChartLoaderTests
{
    [Test]
    public void LoadFromResources_ParsesTestChart()
    {
        ChartData chart = ChartLoader.LoadFromResources("Charts/perfect_run_test");

        Assert.IsNotNull(chart);
        Assert.IsNotNull(chart.notes);
        Assert.GreaterOrEqual(chart.notes.Length, 15);
        Assert.AreEqual(0f, chart.notes[0].time, 0.0001f);
        Assert.AreEqual(0, chart.notes[0].lane);
    }
}
```

- [ ] **Step 2: 테스트 실행 → 실패 확인**

`ChartLoader`/`ChartData`/리소스 파일이 없어 컴파일 에러 또는 FileNotFoundException으로 FAIL.

- [ ] **Step 3: 데이터 클래스 작성**

`Assets/Scripts/Rhythm/ChartData.cs`:

```csharp
[System.Serializable]
public class NoteData
{
    public float time;
    public int lane;
}

[System.Serializable]
public class ChartData
{
    public NoteData[] notes;
}
```

- [ ] **Step 4: 로더 작성**

`Assets/Scripts/Rhythm/ChartLoader.cs`:

```csharp
using UnityEngine;

public static class ChartLoader
{
    public static ChartData LoadFromResources(string resourcePath)
    {
        TextAsset textAsset = Resources.Load<TextAsset>(resourcePath);
        if (textAsset == null)
        {
            throw new System.IO.FileNotFoundException(
                $"Chart not found at Resources/{resourcePath}.json");
        }
        return JsonUtility.FromJson<ChartData>(textAsset.text);
    }
}
```

- [ ] **Step 5: 테스트 채보 작성**

`Assets/Resources/Charts/perfect_run_test.json` — 곡 첫 15~20초 구간, 4레인에 걸쳐 18개 노트 (임의 타이밍, 판정 로직 검증용):

```json
{
  "notes": [
    {"time": 0.0, "lane": 0},
    {"time": 0.8, "lane": 1},
    {"time": 1.6, "lane": 2},
    {"time": 2.4, "lane": 3},
    {"time": 3.2, "lane": 0},
    {"time": 4.0, "lane": 1},
    {"time": 4.4, "lane": 2},
    {"time": 4.8, "lane": 3},
    {"time": 5.6, "lane": 0},
    {"time": 6.4, "lane": 1},
    {"time": 7.2, "lane": 2},
    {"time": 8.0, "lane": 3},
    {"time": 8.8, "lane": 0},
    {"time": 9.6, "lane": 1},
    {"time": 10.4, "lane": 2},
    {"time": 11.2, "lane": 3},
    {"time": 12.0, "lane": 0},
    {"time": 12.8, "lane": 1}
  ]
}
```

- [ ] **Step 6: 테스트 실행 → 통과 확인**

`ChartLoaderTests.LoadFromResources_ParsesTestChart` PASS.

- [ ] **Step 7: Commit**

```bash
git add Assets/Scripts/Rhythm/ChartData.cs Assets/Scripts/Rhythm/ChartLoader.cs Assets/Resources/Charts/perfect_run_test.json Assets/Scripts/Rhythm/Tests/EditMode/ChartLoaderTests.cs
git commit -m "feat: add chart data model and JSON loader"
```

---

## Task 4: SongConductor (FMOD 재생 + 곡 시간 제공)

**Files:**
- Create: `Assets/Scripts/Rhythm/SongConductor.cs`

**Interfaces:**
- Consumes: `FMODUnity.RuntimeManager.CreateInstance(string)`, `FMOD.Studio.EventInstance`
- Produces: `class SongConductor : MonoBehaviour { string eventPath = "event:/Perfect_Run"; float SongTimeSeconds { get; } bool IsPlaying { get; } void Play(); }` — Task 6(NoteView), 7(NoteSpawner), 9(JudgmentSystem 아님, Update 참조), 11(InputHandler)가 `conductor.SongTimeSeconds`를 읽는다.

이 태스크는 FMOD 런타임 의존성 때문에 EditMode 테스트가 불가능하다 (실제 오디오 시스템이 있는 Play Mode 필요). 로직에 분기가 없어(단순 값 폴링) 순수 함수로 뺄 것도 없으므로, Step 5의 수동 Play Mode 검증으로 테스트를 대신한다.

- [ ] **Step 1: 작성**

`Assets/Scripts/Rhythm/SongConductor.cs`:

```csharp
using FMOD.Studio;
using FMODUnity;
using UnityEngine;

public class SongConductor : MonoBehaviour
{
    [SerializeField] private string eventPath = "event:/Perfect_Run";

    private EventInstance instance;

    public float SongTimeSeconds { get; private set; }
    public bool IsPlaying { get; private set; }

    public void Play()
    {
        instance = RuntimeManager.CreateInstance(eventPath);
        instance.start();
        IsPlaying = true;
    }

    private void Update()
    {
        if (!IsPlaying) return;
        instance.getTimelinePosition(out int positionMs);
        SongTimeSeconds = positionMs / 1000f;
    }

    private void OnDestroy()
    {
        if (!IsPlaying) return;
        instance.stop(FMOD.Studio.STOP_MODE.IMMEDIATE);
        instance.release();
    }
}
```

- [ ] **Step 2: Commit**

```bash
git add Assets/Scripts/Rhythm/SongConductor.cs
git commit -m "feat: add SongConductor for FMOD timeline sync"
```

(Play Mode 검증은 Task 11에서 씬에 배치된 후 Task 12 통합 플레이테스트 때 함께 확인한다.)

---

## Task 5: 에셋 슬라이싱 (assets.png → 판정 라벨/노트/레인 스프라이트)

**Files:**
- Create: `Assets/Art/assets.png` (원본 `C:\Project RESO\assets\assets.png` 복사)

**Interfaces:**
- Consumes: 없음
- Produces: `Assets/Art/assets.png` 내부에 이름이 붙은 서브 스프라이트: `judgment_perfect`, `judgment_great`, `judgment_good`, `judgment_bad`, `judgment_miss`, `note_purple`, `note_blue`, `note_white`, `note_red`, `lane_rail_blue` — Task 6(NoteSpawner)와 Task 12(JudgmentUI)가 이 이름으로 스프라이트를 참조한다.

- [ ] **Step 1: 원본 파일 복사**

```bash
mkdir -p "E:/Project RESO/Assets/Art"
cp "C:/Project RESO/assets/assets.png" "E:/Project RESO/Assets/Art/assets.png"
```

- [ ] **Step 2: 텍스처를 Sprite(Multiple)로 임포트**

Unity MCP `Unity_ManageAsset`으로 `Assets/Art/assets.png`의 TextureImporter 설정을 `textureType: Sprite`, `spriteImportMode: Multiple`로 변경.

- [ ] **Step 3: 자동 슬라이싱 + 이름 지정**

`unity:sprite-editor` 스킬을 호출해 `Assets/Art/assets.png`에 알파 채널 기반 자동 슬라이싱(Automatic)을 실행하고, 아래 영역에 해당하는 슬라이스를 지정된 이름으로 rename한다 (이미지는 1536x1024, 좌상단이 원점):

| 이름 | 원본 위치 설명 |
|---|---|
| `judgment_perfect` | 우측 상단 클러스터, "PERFECT" 금색 라벨 |
| `judgment_great` | "PERFECT" 바로 아래, "GREAT" 은색 라벨 |
| `judgment_good` | "GREAT" 아래, "GOOD" 초록 라벨 |
| `judgment_bad` | "GOOD" 아래, "BAD" 주황 라벨 |
| `judgment_miss` | "BAD" 아래, "MISS" 빨강 라벨 |
| `note_purple` | 좌상단 첫 번째 행, 첫 번째(보라) 다이아몬드 아이콘 |
| `note_blue` | 좌상단 두 번째 행, 첫 번째(파랑) 다이아몬드 아이콘 |
| `note_white` | 좌상단 세 번째 행, 첫 번째(화이트/실버) 다이아몬드 아이콘 |
| `note_red` | 좌상단 네 번째 행, 첫 번째(빨강) 다이아몬드 아이콘 |
| `lane_rail_blue` | 상단 중앙, 파란색 세로 바(둥근 캡슐형 rail) 중 첫 번째 |

- [ ] **Step 4: 검증**

`Unity_FindProjectAssets` 또는 `Unity_ReadResource`로 `Assets/Art/assets.png` 하위에 위 10개 이름의 스프라이트가 모두 존재하는지 확인.

- [ ] **Step 5: Commit**

```bash
git add Assets/Art/assets.png Assets/Art/assets.png.meta
git commit -m "feat: import and slice UI sprite sheet"
```

---

## Task 6: NoteMotion (순수 낙하 보간 함수) + NoteView

**Files:**
- Create: `Assets/Scripts/Rhythm/NoteMotion.cs`
- Create: `Assets/Scripts/Rhythm/NoteView.cs`
- Test: `Assets/Scripts/Rhythm/Tests/EditMode/NoteMotionTests.cs`

**Interfaces:**
- Consumes: 없음
- Produces: `static class NoteMotion { static Vector3 ComputePosition(float spawnTime, float hitTime, float now, Vector3 spawnPosition, Vector3 judgmentPosition); }`, `class NoteView : MonoBehaviour { int Lane { get; } float HitTime { get; } bool IsResolved { get; } void Initialize(int lane, float hitTime, float spawnTime, Vector3 spawnPosition, Vector3 judgmentPosition, SongConductor conductor, Sprite sprite); void Resolve(); }` — Task 8(NoteSpawner)이 `Initialize`를, Task 7(JudgmentSystem)이 `Lane`/`HitTime`/`Resolve()`를 사용한다.

- [ ] **Step 1: 실패하는 테스트 작성**

`Assets/Scripts/Rhythm/Tests/EditMode/NoteMotionTests.cs`:

```csharp
using NUnit.Framework;
using UnityEngine;

public class NoteMotionTests
{
    [Test]
    public void ComputePosition_AtSpawnTime_ReturnsSpawnPosition()
    {
        Vector3 result = NoteMotion.ComputePosition(
            spawnTime: 0f, hitTime: 2f, now: 0f,
            spawnPosition: new Vector3(0, 5, 0), judgmentPosition: new Vector3(0, -3, 0));

        Assert.AreEqual(new Vector3(0, 5, 0), result);
    }

    [Test]
    public void ComputePosition_AtHitTime_ReturnsJudgmentPosition()
    {
        Vector3 result = NoteMotion.ComputePosition(
            spawnTime: 0f, hitTime: 2f, now: 2f,
            spawnPosition: new Vector3(0, 5, 0), judgmentPosition: new Vector3(0, -3, 0));

        Assert.AreEqual(new Vector3(0, -3, 0), result);
    }

    [Test]
    public void ComputePosition_Halfway_ReturnsMidpoint()
    {
        Vector3 result = NoteMotion.ComputePosition(
            spawnTime: 0f, hitTime: 2f, now: 1f,
            spawnPosition: new Vector3(0, 5, 0), judgmentPosition: new Vector3(0, -3, 0));

        Assert.AreEqual(new Vector3(0, 1, 0), result);
    }
}
```

- [ ] **Step 2: 테스트 실행 → 실패 확인**

`NoteMotion`이 없어 컴파일 에러로 FAIL.

- [ ] **Step 3: 구현 작성**

`Assets/Scripts/Rhythm/NoteMotion.cs`:

```csharp
using UnityEngine;

public static class NoteMotion
{
    public static Vector3 ComputePosition(
        float spawnTime, float hitTime, float now,
        Vector3 spawnPosition, Vector3 judgmentPosition)
    {
        float t = hitTime > spawnTime ? (now - spawnTime) / (hitTime - spawnTime) : 1f;
        return Vector3.LerpUnclamped(spawnPosition, judgmentPosition, t);
    }
}
```

- [ ] **Step 4: 테스트 실행 → 통과 확인**

3개 케이스 모두 PASS.

- [ ] **Step 5: NoteView 작성**

`Assets/Scripts/Rhythm/NoteView.cs`:

```csharp
using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class NoteView : MonoBehaviour
{
    public int Lane { get; private set; }
    public float HitTime { get; private set; }
    public bool IsResolved { get; private set; }

    private float spawnTime;
    private Vector3 spawnPosition;
    private Vector3 judgmentPosition;
    private SongConductor conductor;
    private SpriteRenderer spriteRenderer;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    public void Initialize(
        int lane, float hitTime, float spawnTime,
        Vector3 spawnPosition, Vector3 judgmentPosition,
        SongConductor conductor, Sprite sprite)
    {
        Lane = lane;
        HitTime = hitTime;
        this.spawnTime = spawnTime;
        this.spawnPosition = spawnPosition;
        this.judgmentPosition = judgmentPosition;
        this.conductor = conductor;

        if (spriteRenderer == null) spriteRenderer = GetComponent<SpriteRenderer>();
        spriteRenderer.sprite = sprite;

        transform.position = spawnPosition;
    }

    public void Resolve()
    {
        IsResolved = true;
        Destroy(gameObject);
    }

    private void Update()
    {
        if (IsResolved) return;
        transform.position = NoteMotion.ComputePosition(
            spawnTime, HitTime, conductor.SongTimeSeconds, spawnPosition, judgmentPosition);
    }
}
```

- [ ] **Step 6: Commit**

```bash
git add Assets/Scripts/Rhythm/NoteMotion.cs Assets/Scripts/Rhythm/NoteView.cs Assets/Scripts/Rhythm/Tests/EditMode/NoteMotionTests.cs
git commit -m "feat: add note fall motion and NoteView"
```

---

## Task 7: JudgmentSystem (판정 큐 + 미스 스윕)

**Files:**
- Create: `Assets/Scripts/Rhythm/JudgmentSystem.cs`
- Test: `Assets/Scripts/Rhythm/Tests/EditMode/JudgmentSystemTests.cs`

**Interfaces:**
- Consumes: `Grade`, `JudgmentClassifier.Classify(float)`, `JudgmentClassifier.BadWindowMs`, `NoteView.Lane/HitTime/Resolve()`
- Produces: `class JudgmentSystem : MonoBehaviour { SongConductor conductor; JudgmentEvent OnJudged; void Register(NoteView note); void TryHit(int lane, float inputTimeSeconds); }`, `class JudgmentEvent : UnityEvent<int, Grade>` — Task 8(NoteSpawner)이 `Register`를, Task 11(InputHandler)이 `TryHit`을, Task 12(JudgmentUI)가 `OnJudged`를 사용한다.

- [ ] **Step 1: 실패하는 테스트 작성**

`Assets/Scripts/Rhythm/Tests/EditMode/JudgmentSystemTests.cs`:

```csharp
using NUnit.Framework;
using UnityEngine;

public class JudgmentSystemTests
{
    private static JudgmentSystem CreateSystem()
    {
        var go = new GameObject("JudgmentSystem");
        return go.AddComponent<JudgmentSystem>();
    }

    private static NoteView CreateNote(int lane, float hitTime)
    {
        var go = new GameObject("NoteView");
        var note = go.AddComponent<NoteView>();
        note.Initialize(lane, hitTime, hitTime - 2f, Vector3.zero, Vector3.zero, null, null);
        return note;
    }

    [Test]
    public void TryHit_WithinPerfectWindow_FiresPerfectAndConsumesNote()
    {
        JudgmentSystem system = CreateSystem();
        NoteView note = CreateNote(lane: 0, hitTime: 10f);
        system.Register(note);

        Grade? firedGrade = null;
        system.OnJudged.AddListener((lane, grade) => firedGrade = grade);

        system.TryHit(lane: 0, inputTimeSeconds: 10.01f);

        Assert.AreEqual(Grade.Perfect, firedGrade);
    }

    [Test]
    public void TryHit_OutsideBadWindow_DoesNotFire()
    {
        JudgmentSystem system = CreateSystem();
        NoteView note = CreateNote(lane: 1, hitTime: 10f);
        system.Register(note);

        bool fired = false;
        system.OnJudged.AddListener((_, __) => fired = true);

        system.TryHit(lane: 1, inputTimeSeconds: 10.5f);

        Assert.IsFalse(fired);
    }

    [Test]
    public void TryHit_OnEmptyLaneQueue_DoesNotThrow()
    {
        JudgmentSystem system = CreateSystem();
        Assert.DoesNotThrow(() => system.TryHit(lane: 2, inputTimeSeconds: 5f));
    }
}
```

- [ ] **Step 2: 테스트 실행 → 실패 확인**

`JudgmentSystem`이 없어 컴파일 에러로 FAIL.

- [ ] **Step 3: 구현 작성**

`Assets/Scripts/Rhythm/JudgmentSystem.cs`:

```csharp
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

[System.Serializable]
public class JudgmentEvent : UnityEvent<int, Grade> { }

public class JudgmentSystem : MonoBehaviour
{
    [SerializeField] private SongConductor conductor;
    public JudgmentEvent OnJudged = new JudgmentEvent();

    private readonly List<Queue<NoteView>> laneQueues = new List<Queue<NoteView>>
    {
        new Queue<NoteView>(), new Queue<NoteView>(), new Queue<NoteView>(), new Queue<NoteView>()
    };

    public void Register(NoteView note)
    {
        laneQueues[note.Lane].Enqueue(note);
    }

    public void TryHit(int lane, float inputTimeSeconds)
    {
        Queue<NoteView> queue = laneQueues[lane];
        if (queue.Count == 0) return;

        NoteView note = queue.Peek();
        float deltaMs = (inputTimeSeconds - note.HitTime) * 1000f;
        Grade grade = JudgmentClassifier.Classify(deltaMs);
        if (grade == Grade.Miss) return;

        queue.Dequeue();
        note.Resolve();
        OnJudged.Invoke(lane, grade);
    }

    private void Update()
    {
        if (conductor == null) return;
        float now = conductor.SongTimeSeconds;
        float missWindowSeconds = JudgmentClassifier.BadWindowMs / 1000f;

        foreach (Queue<NoteView> queue in laneQueues)
        {
            while (queue.Count > 0 && now - queue.Peek().HitTime > missWindowSeconds)
            {
                NoteView missed = queue.Dequeue();
                int lane = missed.Lane;
                missed.Resolve();
                OnJudged.Invoke(lane, Grade.Miss);
            }
        }
    }
}
```

- [ ] **Step 4: 테스트 실행 → 통과 확인**

3개 케이스 모두 PASS. (테스트는 `Update()`를 호출하지 않으므로 `conductor == null`이어도 안전하다.)

- [ ] **Step 5: Commit**

```bash
git add Assets/Scripts/Rhythm/JudgmentSystem.cs Assets/Scripts/Rhythm/Tests/EditMode/JudgmentSystemTests.cs
git commit -m "feat: add judgment queue with miss sweep"
```

---

## Task 8: NoteSpawner

**Files:**
- Create: `Assets/Scripts/Rhythm/NoteSpawner.cs`

**Interfaces:**
- Consumes: `ChartLoader.LoadFromResources`, `SongConductor.SongTimeSeconds`, `NoteView.Initialize`, `JudgmentSystem.Register`
- Produces: `class NoteSpawner : MonoBehaviour { SongConductor conductor; JudgmentSystem judgmentSystem; NoteView notePrefab; Transform[] laneSpawnPoints (4); Transform[] laneJudgmentPoints (4); Sprite[] laneNoteSprites (4); float leadTimeSeconds = 2f; string chartResourcePath = "Charts/perfect_run_test"; }` — Task 11(씬 조립)이 이 필드들을 인스펙터에서 연결한다.

- [ ] **Step 1: 작성**

`Assets/Scripts/Rhythm/NoteSpawner.cs`:

```csharp
using UnityEngine;

public class NoteSpawner : MonoBehaviour
{
    [SerializeField] private SongConductor conductor;
    [SerializeField] private JudgmentSystem judgmentSystem;
    [SerializeField] private NoteView notePrefab;
    [SerializeField] private Transform[] laneSpawnPoints = new Transform[4];
    [SerializeField] private Transform[] laneJudgmentPoints = new Transform[4];
    [SerializeField] private Sprite[] laneNoteSprites = new Sprite[4];
    [SerializeField] private float leadTimeSeconds = 2f;
    [SerializeField] private string chartResourcePath = "Charts/perfect_run_test";

    private ChartData chart;
    private int nextNoteIndex;

    private void Awake()
    {
        chart = ChartLoader.LoadFromResources(chartResourcePath);
    }

    private void Update()
    {
        if (chart?.notes == null) return;

        while (nextNoteIndex < chart.notes.Length &&
               chart.notes[nextNoteIndex].time - leadTimeSeconds <= conductor.SongTimeSeconds)
        {
            SpawnNote(chart.notes[nextNoteIndex]);
            nextNoteIndex++;
        }
    }

    private void SpawnNote(NoteData data)
    {
        float spawnTime = data.time - leadTimeSeconds;
        Transform spawnPoint = laneSpawnPoints[data.lane];
        Transform judgmentPoint = laneJudgmentPoints[data.lane];

        NoteView note = Instantiate(notePrefab, spawnPoint.position, Quaternion.identity);
        note.Initialize(
            data.lane, data.time, spawnTime,
            spawnPoint.position, judgmentPoint.position,
            conductor, laneNoteSprites[data.lane]);

        judgmentSystem.Register(note);
    }
}
```

- [ ] **Step 2: Commit**

```bash
git add Assets/Scripts/Rhythm/NoteSpawner.cs
git commit -m "feat: add NoteSpawner"
```

(Play Mode 검증은 씬 배치 후 Task 12에서.)

---

## Task 9: InputHandler

**Files:**
- Create: `Assets/Scripts/Rhythm/InputHandler.cs`

**Interfaces:**
- Consumes: `SongConductor.SongTimeSeconds`, `JudgmentSystem.TryHit(int, float)`, `UnityEngine.InputSystem.Keyboard`
- Produces: `class InputHandler : MonoBehaviour { SongConductor conductor; JudgmentSystem judgmentSystem; }` — Task 11(씬 조립)이 필드를 연결한다.

- [ ] **Step 1: 작성**

`Assets/Scripts/Rhythm/InputHandler.cs`:

```csharp
using UnityEngine;
using UnityEngine.InputSystem;

public class InputHandler : MonoBehaviour
{
    [SerializeField] private SongConductor conductor;
    [SerializeField] private JudgmentSystem judgmentSystem;

    private static readonly Key[] LaneKeys = { Key.D, Key.F, Key.J, Key.K };

    private void Update()
    {
        Keyboard keyboard = Keyboard.current;
        if (keyboard == null) return;

        for (int lane = 0; lane < LaneKeys.Length; lane++)
        {
            if (keyboard[LaneKeys[lane]].wasPressedThisFrame)
            {
                judgmentSystem.TryHit(lane, conductor.SongTimeSeconds);
            }
        }
    }
}
```

- [ ] **Step 2: Commit**

```bash
git add Assets/Scripts/Rhythm/InputHandler.cs
git commit -m "feat: add DFJK input handler"
```

---

## Task 10: JudgmentUI

**Files:**
- Create: `Assets/Scripts/Rhythm/JudgmentUI.cs`

**Interfaces:**
- Consumes: `JudgmentSystem.OnJudged`, `Grade`
- Produces: `class JudgmentUI : MonoBehaviour { JudgmentSystem judgmentSystem; Image judgmentLabel; Text comboText; Sprite perfectSprite/greatSprite/goodSprite/badSprite/missSprite; float labelVisibleSeconds = 0.4f; }` — Task 11(씬 조립)이 필드를 연결한다.

- [ ] **Step 1: 작성**

`Assets/Scripts/Rhythm/JudgmentUI.cs`:

```csharp
using UnityEngine;
using UnityEngine.UI;

public class JudgmentUI : MonoBehaviour
{
    [SerializeField] private JudgmentSystem judgmentSystem;
    [SerializeField] private Image judgmentLabel;
    [SerializeField] private Text comboText;
    [SerializeField] private Sprite perfectSprite;
    [SerializeField] private Sprite greatSprite;
    [SerializeField] private Sprite goodSprite;
    [SerializeField] private Sprite badSprite;
    [SerializeField] private Sprite missSprite;
    [SerializeField] private float labelVisibleSeconds = 0.4f;

    private int combo;
    private float labelHideAt;

    private void OnEnable() => judgmentSystem.OnJudged.AddListener(HandleJudged);
    private void OnDisable() => judgmentSystem.OnJudged.RemoveListener(HandleJudged);

    private void HandleJudged(int lane, Grade grade)
    {
        combo = (grade == Grade.Miss || grade == Grade.Bad) ? 0 : combo + 1;
        comboText.text = combo.ToString();

        judgmentLabel.sprite = SpriteFor(grade);
        judgmentLabel.enabled = true;
        labelHideAt = Time.time + labelVisibleSeconds;
    }

    private Sprite SpriteFor(Grade grade)
    {
        switch (grade)
        {
            case Grade.Perfect: return perfectSprite;
            case Grade.Great: return greatSprite;
            case Grade.Good: return goodSprite;
            case Grade.Bad: return badSprite;
            default: return missSprite;
        }
    }

    private void Update()
    {
        if (judgmentLabel.enabled && Time.time >= labelHideAt)
        {
            judgmentLabel.enabled = false;
        }
    }
}
```

- [ ] **Step 2: Commit**

```bash
git add Assets/Scripts/Rhythm/JudgmentUI.cs
git commit -m "feat: add judgment label and combo UI"
```

---

## Task 11: 씬/프리팹 조립

**Files:**
- Create: `Assets/Prefabs/NoteView.prefab`
- Create: `Assets/Scenes/RhythmPrototype.unity`

**Interfaces:**
- Consumes: 모든 이전 태스크의 컴포넌트
- Produces: 재생 가능한 씬 — Task 12(플레이테스트)가 이 씬을 연다.

- [ ] **Step 1: NoteView 프리팹 생성**

Unity MCP(`Unity_ManageGameObject`, `Unity_ManageAsset`)로 GameObject `NoteView` 생성: `SpriteRenderer` + `NoteView` 컴포넌트, `transform.localScale = (0.5, 0.5, 1)`. `Assets/Prefabs/NoteView.prefab`으로 저장.

- [ ] **Step 2: 씬 생성 및 카메라 설정**

`Assets/Scenes/RhythmPrototype.unity` 새 씬 생성. `Main Camera`: `orthographic = true`, `orthographicSize = 6`, `position = (0, 1, -10)`.

- [ ] **Step 3: 레인 스폰/판정 포인트**

빈 GameObject `SpawnPoints` 아래 자식 4개 `Lane0`~`Lane3`, position `(x, 5, 0)`. 빈 GameObject `JudgmentPoints` 아래 자식 4개 `Lane0`~`Lane3`, position `(x, -3, 0)`. `x` 값: 레인 0=-1.5, 1=-0.5, 2=0.5, 3=1.5.

- [ ] **Step 4: 레인 레일 비주얼**

GameObject `LaneRail_0`~`LaneRail_3`: `SpriteRenderer.sprite = lane_rail_blue`(Task 5에서 슬라이스), position `(x, 1, 0)`, `localScale = (1, 8, 1)`.

- [ ] **Step 5: 게임플레이 루트 오브젝트**

GameObject `RhythmGameplay`에 컴포넌트 부착: `SongConductor`, `NoteSpawner`, `JudgmentSystem`, `InputHandler`.

- `NoteSpawner`: `conductor` = 같은 오브젝트의 `SongConductor`, `judgmentSystem` = 같은 오브젝트의 `JudgmentSystem`, `notePrefab` = `Assets/Prefabs/NoteView.prefab`, `laneSpawnPoints` = `SpawnPoints/Lane0..3`, `laneJudgmentPoints` = `JudgmentPoints/Lane0..3`, `laneNoteSprites` = `[note_purple, note_blue, note_white, note_red]`
- `JudgmentSystem`: `conductor` = 같은 오브젝트의 `SongConductor`
- `InputHandler`: `conductor`/`judgmentSystem` = 같은 오브젝트의 컴포넌트

- [ ] **Step 6: UI 캔버스**

`Canvas`(Screen Space - Overlay) 생성. 자식 `JudgmentLabel`(`Image`, 앵커 중앙 상단, `enabled = false`), 자식 `ComboText`(`Text`, 앵커 상단 중앙, 기본 텍스트 `"0"`).

GameObject `JudgmentUI`에 `JudgmentUI` 컴포넌트 부착: `judgmentSystem` = `RhythmGameplay`의 `JudgmentSystem`, `judgmentLabel` = `JudgmentLabel`의 `Image`, `comboText` = `ComboText`의 `Text`, `perfectSprite`~`missSprite` = Task 5의 `judgment_perfect`~`judgment_miss`.

- [ ] **Step 7: 씬 저장 + 검증**

`Unity_ManageScene`으로 씬 저장. `Unity_GetConsoleLogs`로 컴파일/참조 에러 없는지 확인.

- [ ] **Step 8: Commit**

```bash
git add Assets/Prefabs/NoteView.prefab Assets/Prefabs/NoteView.prefab.meta Assets/Scenes/RhythmPrototype.unity Assets/Scenes/RhythmPrototype.unity.meta
git commit -m "feat: assemble rhythm prototype scene"
```

---

## Task 12: 통합 플레이테스트

**Files:** 없음 (검증 전용)

**Interfaces:**
- Consumes: `RhythmPrototype.unity`
- Produces: 없음

- [ ] **Step 1: SongConductor 자동 재생 임시 연결**

`RhythmGameplay`의 `SongConductor`를 씬 `Start()`에서 재생하려면, 임시로 `NoteSpawner.Awake()` 다음 줄에 `conductor.Play();` 호출이 필요하다 — `Assets/Scripts/Rhythm/NoteSpawner.cs`의 `Awake()`를 다음으로 교체:

```csharp
    private void Awake()
    {
        chart = ChartLoader.LoadFromResources(chartResourcePath);
        conductor.Play();
    }
```

- [ ] **Step 2: Play Mode 진입**

`Unity_ManageEditor`(`Action: Play`)로 `RhythmPrototype.unity` 씬을 Play Mode로 진입.

- [ ] **Step 3: 수동 검증**

- 곡 재생과 함께 4개 레인 상단에서 노트가 낙하하는지 확인
- `D F J K` 입력 시 판정선 근처에서 해당 레인 노트가 사라지고 `JudgmentLabel`에 등급 스프라이트가 표시되는지 확인
- 입력하지 않고 노트를 지나치면 `MISS`가 표시되고 콤보가 0으로 리셋되는지 확인
- `Unity_GetConsoleLogs`로 런타임 에러(NullReferenceException 등) 없는지 확인

- [ ] **Step 4: Play Mode 종료**

`Unity_ManageEditor`(`Action: Stop`).

- [ ] **Step 5: Commit**

```bash
git add Assets/Scripts/Rhythm/NoteSpawner.cs
git commit -m "feat: auto-start song conductor on spawner awake"
```

---

## 이후 단계 (범위 밖)

메뉴/선택 화면, 스킨 시스템, 코스·대전 모드, 리더보드/네트워크, 모바일 포팅 — 각각 별도 브레인스토밍 → 스펙 → 플랜 사이클로 진행한다.
