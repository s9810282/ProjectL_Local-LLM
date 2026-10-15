# ProjectL Stage Map Core 구현 가이드

> 과거 기록 (2026-10-06 정리): 아래 내용은 Framework/Buffer 전환 전 구현이며 현재 구현 지침이 아닙니다. 삭제된 계획·설계 원문은 Git 커밋 `606fdf4c`에서 확인할 수 있습니다. 현재 구조와 API는 [Framework 개발 가이드](../../../Assets/ProjectL/Script/Systems/Documentation/framework-programmer-guide.md)와 실제 코드를 기준으로 확인합니다.

- 작성일: 2026-08-25
- 설계 기준: 과거 문서 2026-08-25-stage-map-core-design.md (Git 이력)
- 선행 구현 순서: 과거 문서 2026-08-25-stage-map-core.md (Git 이력)의 Task 1~4, 과거 문서 2026-08-30-stage-initial-enemy-bootstrap.md (Git 이력)의 Task 1~4, 과거 문서 2026-08-26-stage-gimmick-timeline.md (Git 이력)의 Task 1~8
- 이 문서의 범위: 연결 정책 완성, Application 공개 경계, View 경계, 최종 검증
- 주의: 이 문서 작성 작업에서는 실제 시스템 코드를 구현하지 않았다.
- 중간 선행 Enemy 설계: 과거 문서 2026-08-30-stage-initial-enemy-bootstrap-design.md (Git 이력)
- 중간 선행 Enemy 구현 계획: 과거 문서 2026-08-30-stage-initial-enemy-bootstrap.md (Git 이력)
- 중간 선행 설계: 과거 문서 2026-08-26-stage-gimmick-timeline-design.md (Git 이력)
- 중간 선행 구현 계획: 과거 문서 2026-08-26-stage-gimmick-timeline.md (Git 이력)

## 1. 이 가이드를 읽는 순서

1. Stage Map 설계 문서에서 용어, 불변식, 범위 제외 사항을 확인한다.
2. Stage Map 구현 계획 Task 1부터 Task 4까지 TDD 순서로 진행한다.
3. Stage Initial Enemy Bootstrap 설계와 구현 계획 Task 1부터 Task 4까지 진행한다.
4. Stage Gimmick & Timeline 설계 문서를 읽는다.
5. Stage Gimmick & Timeline 구현 계획 Task 1부터 Task 8까지 진행한다.
6. 이 문서로 돌아와 Task 5부터 Task 7까지 진행한다.
7. Final Acceptance Checklist를 모두 통과해 Stage Map Core를 완결한다.
8. 세 경계의 전체 회귀 테스트 후 별도 Unity Data/Bootstrap 연결을 진행한다.

이 문서는 StageMapGenerator를 범용 Graph 엔진으로 확장하지 않는다. 한 종류의 Layered DAG 생성 정책과 한 종류의 진행 상태만 구현한다.

Task 5 시작 직전에는 Enemy Bootstrap과 Gimmick 계획까지 커밋된 깨끗한 작업 트리인지 확인하고 기준 커밋을 기록한다.

~~~powershell
git status --short
$stageMapGuideBase = git rev-parse HEAD
~~~

Expected: `git status --short` 출력 없음. 다른 터미널에서 최종 검증한다면 출력된 커밋 해시를 따로 기록해 `$stageMapGuideBase` 대신 사용한다.

---

## Task 5: 가까운 연결, 확률 분기, Incoming 보정, Boss 수렴

**Files:**

- Modify: Assets/Scripts/ProjectL/Core/Domain/StageMap/StageMapGenerator.cs
- Modify: Assets/Tests/EditMode/StageMapGeneratorTests.cs

**Interfaces:**

- Consumes: 구현 계획 Task 4의 MutableNode Layer
- Produces: 기획 규칙을 모두 만족하는 Layered DAG

- [ ] **Step 1: 256개 Seed의 전체 DAG 불변식 테스트를 작성한다**

~~~csharp
[Test]
public void Generate_SeedRange_SatisfiesEveryDagInvariant()
{
    for (int seed = 0; seed < 256; seed++)
    {
        StageMapRunState state = CreateGeneratedState(seed, chance: 0.35);
        AssertDagInvariants(state, seed);
    }
}
~~~

AssertDagInvariants는 각 Seed를 assertion message에 포함하고 다음을 모두 검사한다.

~~~text
Depth 0 node count == 1
last Depth node count == 1
per-depth count in 1..WidthMax
WidthIndex is 0..count-1
unique NodeId
every non-final node has outgoing
final node has no outgoing
every edge target exists
target.Depth == source.Depth + 1
every non-start node incoming >= 1
all nodes reachable from start
boss reachable from every node
every pre-boss node next ids == [boss id]
~~~

Reachability helper는 production State의 검증 결과만 믿지 않고 Test가 독립적으로 Queue 기반 BFS를 수행한다.

- [ ] **Step 2: AdditionalConnectionChance 0의 최근접 기본 Edge를 검증한다**

~~~csharp
[Test]
public void Generate_ZeroChance_EachSourceContainsNearestTarget()
{
    for (int seed = 0; seed < 128; seed++)
    {
        StageMapRunState state = CreateGeneratedState(seed, chance: 0.0);
        StageMapNode[] nodes = state.Nodes.ToArray();

        for (int depth = 0; depth < state.DepthCount - 2; depth++)
        {
            StageMapNode[] from = GetLayer(nodes, depth);
            StageMapNode[] to = GetLayer(nodes, depth + 1);

            for (int sourceIndex = 0; sourceIndex < from.Length; sourceIndex++)
            {
                StageMapNode nearest = FindExpectedNearest(
                    sourceIndex,
                    from.Length,
                    to);

                Assert.That(
                    from[sourceIndex].NextNodeIds.ToArray(),
                    Does.Contain(nearest.NodeId),
                    $"seed={seed}, depth={depth}, source={sourceIndex}");
            }
        }
    }
}
~~~

Test의 거리 수식:

~~~csharp
private static double LogicalPosition(int widthIndex, int layerCount)
{
    return (widthIndex + 0.5) / layerCount;
}
~~~

거리 동률이면 작은 WidthIndex가 정답이다. Incoming 보정이 Edge를 추가했더라도 각 source의 기본 최근접 Edge가 남아 있어야 한다.

- [ ] **Step 3: 분기와 Boss 이외 병합이 실제 생성 가능한지 검증한다**

~~~csharp
[Test]
public void Generate_FullChance_SeedRangeContainsBranchAndMerge()
{
    bool sawBranch = false;
    bool sawMerge = false;

    for (int seed = 0; seed < 256 && (!sawBranch || !sawMerge); seed++)
    {
        StageMapRunState state = CreateGeneratedState(
            seed,
            chance: 1.0,
            depthCount: 8,
            widthMax: 4);
        StageMapNode[] nodes = state.Nodes.ToArray();
        StageNodeId bossId = nodes[nodes.Length - 1].NodeId;

        sawBranch |= nodes.Any(node =>
            node.Depth < state.DepthCount - 2 &&
            node.NextNodeIds.Length > 1);

        Dictionary<StageNodeId, int> incoming = CountIncoming(nodes);
        sawMerge |= incoming.Any(pair =>
            pair.Key != bossId &&
            pair.Value > 1);
    }

    Assert.That(sawBranch, Is.True);
    Assert.That(sawMerge, Is.True);
}
~~~

Boss 직전의 의도된 전체 병합만으로 sawMerge가 true가 되지 않도록 Boss NodeId를 제외한다.

- [ ] **Step 4: 확률 추가 Edge가 아직 없어서 red가 되는지 확인한다**

~~~powershell
dotnet test ProjectL.Tests.Core.csproj --no-restore --filter "FullyQualifiedName~StageMapGeneratorTests" --nologo
~~~

Expected: DAG와 최근접 검사는 PASS할 수 있지만 branch 검사는 FAIL한다.

- [ ] **Step 5: 정규화 거리 후보 정렬 helper를 구현한다**

~~~csharp
private static int[] GetTargetIndexesByDistance(
    int sourceIndex,
    int sourceCount,
    int targetCount)
{
    int[] indexes = new int[targetCount];
    for (int i = 0; i < targetCount; i++)
        indexes[i] = i;

    double sourcePosition = LogicalPosition(sourceIndex, sourceCount);
    Array.Sort(indexes, (left, right) =>
    {
        double leftDistance =
            Math.Abs(sourcePosition - LogicalPosition(left, targetCount));
        double rightDistance =
            Math.Abs(sourcePosition - LogicalPosition(right, targetCount));
        int distanceComparison = leftDistance.CompareTo(rightDistance);
        return distanceComparison != 0
            ? distanceComparison
            : left.CompareTo(right);
    });

    return indexes;
}

private static double LogicalPosition(int widthIndex, int layerCount)
{
    return (widthIndex + 0.5) / layerCount;
}
~~~

Layer count는 생성 불변식상 항상 1 이상이다.

- [ ] **Step 6: 일반 Layer 연결을 네 단계로 완성한다**

일반 Layer 쌍마다 정확히 다음 순서로 실행한다.

~~~csharp
// 1. base nearest
for (int sourceIndex = 0; sourceIndex < fromLayer.Length; sourceIndex++)
{
    int[] candidates = GetTargetIndexesByDistance(
        sourceIndex,
        fromLayer.Length,
        toLayer.Length);
    AddUnique(
        fromLayer[sourceIndex].NextNodeIds,
        toLayer[candidates[0]].NodeId);
}

// 2. one probabilistic second-nearest edge
if (toLayer.Length > 1)
{
    for (int sourceIndex = 0; sourceIndex < fromLayer.Length; sourceIndex++)
    {
        if (random.NextDouble() >= config.AdditionalConnectionChance)
            continue;

        int[] candidates = GetTargetIndexesByDistance(
            sourceIndex,
            fromLayer.Length,
            toLayer.Length);
        AddUnique(
            fromLayer[sourceIndex].NextNodeIds,
            toLayer[candidates[1]].NodeId);
    }
}

// 3. incoming repair
for (int targetIndex = 0; targetIndex < toLayer.Length; targetIndex++)
{
    StageNodeId targetId = toLayer[targetIndex].NodeId;
    if (HasIncoming(fromLayer, targetId))
        continue;

    int sourceIndex = FindNearestSourceIndex(
        targetIndex,
        toLayer.Length,
        fromLayer.Length);
    AddUnique(fromLayer[sourceIndex].NextNodeIds, targetId);
}

// 4. sort during immutable conversion
~~~

FindNearestSourceIndex는 target LogicalPosition과 각 source LogicalPosition의 거리를 비교하고 동률이면 작은 source index를 반환한다.

AddUnique는 선형 검색으로 중복을 막는다. WidthMax가 작은 맵 생성 단계이므로 HashSet을 Node 상태로 유지하지 않는다.

- [ ] **Step 7: Boss 직전 연결을 일반 알고리즘보다 먼저 분기한다**

~~~csharp
bool targetIsBossLayer = depth == config.DepthCount - 2;
if (targetIsBossLayer)
{
    StageNodeId bossId = toLayer[0].NodeId;
    for (int i = 0; i < fromLayer.Length; i++)
    {
        fromLayer[i].NextNodeIds.Clear();
        fromLayer[i].NextNodeIds.Add(bossId);
    }
    continue;
}
~~~

Boss Layer는 생성 단계에서 정확히 하나다. 이 Layer에 확률 Edge와 Incoming 보정을 중복 적용하지 않는다.

- [ ] **Step 8: Generator와 Domain 회귀를 통과시킨다**

~~~powershell
dotnet test ProjectL.Tests.Core.csproj --no-restore --filter "FullyQualifiedName~StageMapGeneratorTests|FullyQualifiedName~StageMapRunStateTests" --nologo
dotnet build ProjectL.Core.Domain.csproj --no-restore --nologo --verbosity:minimal
rg -n "PreviousNode|UnityEngine|Vector2|Vector3|Transform|BSP" Assets/Scripts/ProjectL/Core/Domain/StageMap
git diff --check
~~~

Expected:

- focused tests PASS
- Domain build exit 0
- 금지 문자열 검색 결과 0
- whitespace error 0

- [ ] **Step 9: 연결 정책을 커밋한다**

~~~powershell
git add -- Assets/Scripts/ProjectL/Core/Domain/StageMap/StageMapGenerator.cs Assets/Tests/EditMode/StageMapGeneratorTests.cs
git commit -m "feat: connect stage map layered dag"
~~~

---

## Task 6: Application Session과 최신 Snapshot 거래

**Files:**

- Create: Assets/Scripts/ProjectL/Core/Application/StageMap/StageMapSnapshotBuilder.cs
- Create: Assets/Scripts/ProjectL/Core/Application/StageMap/StageMapSession.cs
- Create: Assets/Scripts/ProjectL/Core/Application/StageMap/StageMapSessionFactory.cs
- Create: Assets/Tests/EditMode/StageMapSessionTests.cs

**Interfaces:**

- Consumes: StageMapGenerator, StageMapRunState, Stage Map Contracts
- Produces: public StageMapSessionFactory, public StageMapSession

- [ ] **Step 1: Factory 생성 직후 Snapshot 테스트를 작성한다**

~~~csharp
[Test]
public void Factory_Create_ReturnsViewReadyInitialSnapshot()
{
    StageMapSession session = CreateSession(seed: 51);

    StageMapSnapshot snapshot = session.GetSnapshot();

    Assert.That(snapshot.DepthCount, Is.EqualTo(6));
    Assert.That(snapshot.WidthMax, Is.EqualTo(3));
    Assert.That(snapshot.CurrentNodeId, Is.EqualTo(new StageNodeId(1)));
    Assert.That(snapshot.IsAtFinalNode, Is.False);
    Assert.That(snapshot.Nodes.Span[0].IsStart, Is.True);
    Assert.That(snapshot.Nodes.Span[0].IsCurrent, Is.True);
    Assert.That(snapshot.Nodes.Span[0].StageKey, Is.EqualTo(StageKey.Empty));

    StageMapNodeSnapshot[] selectable = snapshot.Nodes.ToArray()
        .Where(node => node.IsSelectable)
        .ToArray();
    Assert.That(selectable.Length, Is.GreaterThanOrEqualTo(1));
    Assert.That(selectable.All(node => node.Depth == 1), Is.True);
}
~~~

- [ ] **Step 2: 성공, 실패, Query 불변 테스트를 작성한다**

~~~csharp
[Test]
public void TrySelectNode_Success_ReturnsStageKeyAndPostSelectionSnapshot()
{
    StageMapSession session = CreateSession(seed: 51);
    StageMapSnapshot before = session.GetSnapshot();
    StageMapNodeSnapshot target = before.Nodes.ToArray()
        .First(node => node.IsSelectable);

    StageSelectionResult result = session.TrySelectNode(target.NodeId);

    Assert.That(result.Succeeded, Is.True);
    Assert.That(result.SelectedStageKey, Is.EqualTo(target.StageKey));
    Assert.That(result.CurrentMap.CurrentNodeId, Is.EqualTo(target.NodeId));
    Assert.That(
        result.CurrentMap.Nodes.ToArray().Single(node => node.IsCurrent).NodeId,
        Is.EqualTo(target.NodeId));
}

[Test]
public void TrySelectNode_Failure_ReturnsUnchangedCurrentSnapshot()
{
    StageMapSession session = CreateSession(seed: 51);
    StageMapSnapshot before = session.GetSnapshot();
    StageMapNodeSnapshot notConnected = before.Nodes.ToArray()
        .First(node => node.Depth > 1);

    StageSelectionResult result = session.TrySelectNode(notConnected.NodeId);

    Assert.That(result.Succeeded, Is.False);
    Assert.That(result.FailureReason, Is.EqualTo(StageSelectionFailureReason.NotConnected));
    Assert.That(result.SelectedStageKey, Is.EqualTo(StageKey.Empty));
    Assert.That(result.CurrentMap.CurrentNodeId, Is.EqualTo(before.CurrentNodeId));
}

[Test]
public void CanSelectNode_IsAQueryAndDoesNotMove()
{
    StageMapSession session = CreateSession(seed: 51);
    StageMapSnapshot before = session.GetSnapshot();
    StageNodeId target = before.Nodes.ToArray()
        .First(node => node.IsSelectable)
        .NodeId;

    Assert.That(session.CanSelectNode(target), Is.True);
    Assert.That(session.CanSelectNode(target), Is.True);
    Assert.That(session.GetSnapshot().CurrentNodeId, Is.EqualTo(before.CurrentNodeId));
}
~~~

테스트 helper:

~~~csharp
private static StageMapSession CreateSession(int seed)
{
    return StageMapSessionFactory.Create(
        new StageMapGenerationConfig(6, 3, 0.35, seed),
        new[]
        {
            new StageKey(100),
            new StageKey(100),
            new StageKey(200),
            new StageKey(300)
        },
        new StageKey(900));
}
~~~

regular pool의 중복 StageKey 100은 가중치 2를 뜻한다.

- [ ] **Step 3: Application 타입 부재로 red를 확인한다**

~~~powershell
dotnet test ProjectL.Tests.Core.csproj --no-restore --filter "FullyQualifiedName~StageMapSessionTests" --nologo
~~~

- [ ] **Step 4: StageMapSnapshotBuilder를 구현한다**

~~~csharp
internal sealed class StageMapSnapshotBuilder
{
    internal StageMapSnapshot Build(StageMapRunState state)
    {
        if (state == null)
            throw new ArgumentNullException(nameof(state));

        ReadOnlySpan<StageMapNode> nodes = state.Nodes.Span;
        StageMapNodeSnapshot[] snapshots =
            new StageMapNodeSnapshot[nodes.Length];

        for (int i = 0; i < nodes.Length; i++)
        {
            StageMapNode node = nodes[i];
            snapshots[i] = new StageMapNodeSnapshot(
                node.NodeId,
                node.StageKey,
                node.Depth,
                node.WidthIndex,
                isStart: node.Depth == 0,
                isBoss: node.Depth == state.DepthCount - 1,
                isCurrent: node.NodeId == state.CurrentNodeId,
                isSelectable: state.CanSelectNode(node.NodeId),
                node.NextNodeIds.ToArray());
        }

        return new StageMapSnapshot(
            state.DepthCount,
            state.WidthMax,
            state.CurrentNodeId,
            state.IsAtFinalNode,
            snapshots);
    }
}
~~~

- [ ] **Step 5: StageMapSession을 얇은 공개 거래 경계로 구현한다**

~~~csharp
public sealed class StageMapSession
{
    private readonly StageMapRunState _state;
    private readonly StageMapSnapshotBuilder _snapshotBuilder;

    internal StageMapSession(
        StageMapRunState state,
        StageMapSnapshotBuilder snapshotBuilder)
    {
        _state = state ?? throw new ArgumentNullException(nameof(state));
        _snapshotBuilder = snapshotBuilder ??
            throw new ArgumentNullException(nameof(snapshotBuilder));
    }

    public StageMapSnapshot GetSnapshot() =>
        _snapshotBuilder.Build(_state);

    public bool CanSelectNode(StageNodeId nodeId) =>
        _state.CanSelectNode(nodeId);

    public StageSelectionResult TrySelectNode(StageNodeId nodeId)
    {
        StageSelectionFailureReason failureReason =
            _state.TrySelectNode(nodeId, out StageKey selectedStageKey);
        StageMapSnapshot currentMap = GetSnapshot();

        return failureReason == StageSelectionFailureReason.None
            ? StageSelectionResult.Success(selectedStageKey, currentMap)
            : StageSelectionResult.Failure(failureReason, currentMap);
    }
}
~~~

- [ ] **Step 6: StageMapSessionFactory를 기존 StageSessionFactory와 분리해 구현한다**

~~~csharp
public static class StageMapSessionFactory
{
    public static StageMapSession Create(
        StageMapGenerationConfig config,
        IReadOnlyList<StageKey> regularStageKeys,
        StageKey bossStageKey)
    {
        StageMapRunState state = new StageMapGenerator()
            .Generate(config, regularStageKeys, bossStageKey);
        return new StageMapSession(
            state,
            new StageMapSnapshotBuilder());
    }
}
~~~

Factory는 StageSessionFactory를 호출하지 않고 Stage Definition Catalog도 보관하지 않는다.

- [ ] **Step 7: Application tests와 공개 assembly build를 통과시킨다**

~~~powershell
dotnet test ProjectL.Tests.Core.csproj --no-restore --filter "FullyQualifiedName~StageMapSessionTests|FullyQualifiedName~StageMapGeneratorTests|FullyQualifiedName~StageMapRunStateTests" --nologo
dotnet build ProjectL.Core.Application.csproj --no-restore --nologo --verbosity:minimal
git diff --check
~~~

- [ ] **Step 8: Application 공개 경계를 커밋한다**

~~~powershell
git add -- Assets/Scripts/ProjectL/Core/Application/StageMap Assets/Tests/EditMode/StageMapSessionTests.cs
git commit -m "feat: add stage map session boundary"
~~~

---

## Task 7: Domain 없는 View 경계와 전체 검증

**Files:**

- Create: Assets/Tests/EditMode/CombatViewBoundary/StageMapViewBoundaryTests.cs
- Unity generates: 새 폴더와 C# 파일의 .meta
- Verify only: 기존 asmdef, StageSession, StageData, StageNodeData

**Interfaces:**

- Consumes: ProjectL.Core.Application.StageMap과 ProjectL.Core.Contracts만 사용
- Produces: View가 NodeId만 전달하고 StageKey와 Snapshot을 받는 컴파일 증거

- [ ] **Step 1: 기존 View boundary assembly에 Stage Map 테스트를 추가한다**

이 파일에는 ProjectL.Core.Domain using이 없어야 한다.

~~~csharp
using System.Linq;
using NUnit.Framework;
using ProjectL.Core.Application.StageMap;
using ProjectL.Core.Contracts.Commands;
using ProjectL.Core.Contracts.Ids;
using ProjectL.Core.Contracts.Results;
using ProjectL.Core.Contracts.Snapshots;

namespace ProjectL.Tests.CombatViewBoundary
{
    public sealed class StageMapViewBoundaryTests
    {
        [Test]
        public void View_CreatesRendersAndSelectsUsingOnlyContractsAndApplication()
        {
            StageMapSession session = StageMapSessionFactory.Create(
                new StageMapGenerationConfig(6, 3, 0.25, seed: 7301),
                new[]
                {
                    new StageKey(100),
                    new StageKey(100),
                    new StageKey(200)
                },
                new StageKey(900));

            StageMapSnapshot before = session.GetSnapshot();
            StageMapNodeSnapshot target = before.Nodes.ToArray()
                .First(node => node.IsSelectable);

            StageSelectionResult result =
                session.TrySelectNode(target.NodeId);

            Assert.That(result.Succeeded, Is.True);
            Assert.That(result.SelectedStageKey, Is.EqualTo(target.StageKey));
            Assert.That(result.CurrentMap.CurrentNodeId, Is.EqualTo(target.NodeId));
        }

        [Test]
        public void View_OldSnapshotIsNotSelectionAuthority()
        {
            StageMapSession session = StageMapSessionFactory.Create(
                new StageMapGenerationConfig(6, 3, 1.0, seed: 42),
                new[] { new StageKey(100), new StageKey(200) },
                new StageKey(900));
            StageMapSnapshot initial = session.GetSnapshot();
            StageMapNodeSnapshot first = initial.Nodes.ToArray()
                .First(node => node.IsSelectable);
            Assert.That(session.TrySelectNode(first.NodeId).Succeeded, Is.True);

            StageMapNodeSnapshot staleChoice = initial.Nodes.ToArray()
                .FirstOrDefault(node =>
                    node.IsSelectable && node.NodeId != first.NodeId);

            if (staleChoice == null)
                return;

            StageSelectionResult rejected =
                session.TrySelectNode(staleChoice.NodeId);

            Assert.That(rejected.Succeeded, Is.False);
            Assert.That(rejected.FailureReason, Is.EqualTo(StageSelectionFailureReason.NotConnected));
            Assert.That(rejected.CurrentMap.CurrentNodeId, Is.EqualTo(first.NodeId));
        }
    }
}
~~~

두 번째 테스트는 오래된 Snapshot의 IsSelectable 표시가 Command 허가증이 아님을 증명한다.

- [ ] **Step 2: Unity import로 .meta와 generated csproj를 갱신한다**

~~~powershell
$unity = "D:\Program Files\Unity\Hub\Editor\6000.0.73f1\Editor\Unity.exe"
$project = "D:\My_ProjectWork\Github_desktop\Project_L"
$log = Join-Path $project "Temp\stage-map-import.log"
$arguments = @(
    "-batchmode",
    "-nographics",
    "-quit",
    "-projectPath", $project,
    "-logFile", $log
)

$process = Start-Process -FilePath $unity -ArgumentList $arguments -Wait -PassThru -WindowStyle Hidden
if ($process.ExitCode -ne 0) {
    Get-Content -Tail 160 -LiteralPath $log
    throw "Unity import failed with exit code $($process.ExitCode)."
}
~~~

Expected: Unity exit code 0, 새 .meta 생성, 기존 asmdef 내용 변경 없음.

- [ ] **Step 3: 세 Core assembly와 View boundary를 build한다**

~~~powershell
dotnet build ProjectL.Core.Contracts.csproj --no-restore --nologo --verbosity:minimal
dotnet build ProjectL.Core.Domain.csproj --no-restore --nologo --verbosity:minimal
dotnet build ProjectL.Core.Application.csproj --no-restore --nologo --verbosity:minimal
dotnet build ProjectL.Tests.CombatViewBoundary.csproj --no-restore --nologo --verbosity:minimal
~~~

- [ ] **Step 4: focused test와 기존 Core 전체 회귀를 실행한다**

~~~powershell
dotnet test ProjectL.Tests.Core.csproj --no-restore --filter "FullyQualifiedName~StageMap" --nologo
dotnet test ProjectL.Tests.Core.csproj --no-restore --nologo
dotnet test ProjectL.Tests.CombatViewBoundary.csproj --no-restore --nologo
~~~

Expected: 모든 test exit 0.

- [ ] **Step 5: Unity EditMode 전체를 실행한다**

~~~powershell
$unity = "D:\Program Files\Unity\Hub\Editor\6000.0.73f1\Editor\Unity.exe"
$project = "D:\My_ProjectWork\Github_desktop\Project_L"
$results = Join-Path $project "Temp\stage-map-editmode-results.xml"
$log = Join-Path $project "Temp\stage-map-editmode.log"
$arguments = @(
    "-batchmode",
    "-nographics",
    "-projectPath", $project,
    "-runTests",
    "-testPlatform", "EditMode",
    "-testResults", $results,
    "-logFile", $log
)

$process = Start-Process -FilePath $unity -ArgumentList $arguments -Wait -PassThru -WindowStyle Hidden
if ($process.ExitCode -ne 0) {
    Get-Content -Tail 200 -LiteralPath $log
    throw "Unity EditMode tests failed with exit code $($process.ExitCode)."
}

[xml]$xml = Get-Content -Raw -LiteralPath $results
$run = $xml."test-run"
if ($run.result -ne "Passed" -or [int]$run.failed -ne 0) {
    throw "Unity result was not fully passed."
}

"TOTAL=$($run.total) PASSED=$($run.passed) FAILED=$($run.failed)"
~~~

Expected: result Passed, failed 0.

- [ ] **Step 6: 금지 의존성과 기존 파일 무변경을 검사한다**

~~~powershell
$forbidden = rg -n "UnityEngine|Vector2|Vector3|Transform|GameObject|ScriptableObject|PreviousNode|BSP" Assets/Scripts/ProjectL/Core/Domain/StageMap Assets/Scripts/ProjectL/Core/Application/StageMap
if ($LASTEXITCODE -eq 0) {
    $forbidden
    throw "Stage Map Core contains a forbidden dependency or concept."
}
if ($LASTEXITCODE -ne 1) {
    throw "rg failed with exit code $LASTEXITCODE."
}

$viewDomain = rg -n "ProjectL\.Core\.Domain|9b019b81075a3af4d9f545e338271abd" Assets/Tests/EditMode/CombatViewBoundary/StageMapViewBoundaryTests.cs
if ($LASTEXITCODE -eq 0) {
    $viewDomain
    throw "View boundary references Domain."
}
if ($LASTEXITCODE -ne 1) {
    throw "rg failed with exit code $LASTEXITCODE."
}

git diff --exit-code $stageMapGuideBase HEAD -- Assets/Scripts/ProjectL/Core/Application/StageRun/StageSession.cs Assets/Scripts/ProjectL/Core/Application/StageRun/StageSessionFactory.cs Assets/Scripts/Stage/Core/StageData.cs Assets/Scripts/Stage/Core/StageNodeData.cs
git diff --exit-code -- Assets/Scripts/ProjectL/Core/Application/StageRun/StageSession.cs Assets/Scripts/ProjectL/Core/Application/StageRun/StageSessionFactory.cs Assets/Scripts/Stage/Core/StageData.cs Assets/Scripts/Stage/Core/StageNodeData.cs
~~~

Expected: forbidden match 0, View Domain match 0, Enemy Bootstrap과 Gimmick 완료 기준 커밋 이후 네 보호 파일의 committed/uncommitted diff 0.

StageSession과 StageSessionFactory에는 앞서 완료한 Enemy Bootstrap과 Gimmick 계획의 정당한 변경이 존재할 수 있다. 따라서 저장소 최초 상태가 아니라 두 계획 완료 직후 기록한 `$stageMapGuideBase`를 기준으로, 돌아온 Stage Map Task 5~7이 네 보호 파일을 추가 변경하지 않았는지 검사한다.

- [ ] **Step 7: 최종 whitespace와 변경 범위를 확인한다**

~~~powershell
git diff --check
git status --short
~~~

예상 변경은 두 문서의 File Map에 있는 새 Stage Map 파일, Test, Unity 생성 .meta뿐이다.

- [ ] **Step 8: 마지막 View 경계와 meta를 커밋한다**

~~~powershell
git add -- Assets/Scripts/ProjectL/Core/Contracts Assets/Scripts/ProjectL/Core/Domain/StageMap Assets/Scripts/ProjectL/Core/Application/StageMap Assets/Tests/EditMode/StageMapContractTests.cs Assets/Tests/EditMode/StageMapRunStateTests.cs Assets/Tests/EditMode/StageMapGeneratorTests.cs Assets/Tests/EditMode/StageMapSessionTests.cs Assets/Tests/EditMode/CombatViewBoundary/StageMapViewBoundaryTests.cs
git commit -m "test: verify stage map core boundary"
~~~

---

## 2. Unity 연결 시 사용할 공개 흐름

~~~csharp
StageMapSession mapSession = StageMapSessionFactory.Create(
    new StageMapGenerationConfig(
        depthCount: 6,
        widthMax: 3,
        additionalConnectionChance: 0.25,
        seed: runSeed),
    weightedRegularStageKeys,
    bossStageKey);

StageMapSnapshot map = mapSession.GetSnapshot();

// View:
// map.Nodes의 Depth, WidthIndex로 표시 위치 계산
// NextNodeIds로 연결선 표시
// IsCurrent, IsSelectable로 표시 상태 결정

StageSelectionResult selection =
    mapSession.TrySelectNode(clickedNodeId);

ApplyMapSnapshot(selection.CurrentMap);

if (selection.Succeeded)
{
    StageKey stageKey = selection.SelectedStageKey;
    // 상위 Bootstrap/Application이 기존 Stage Definition을 resolve한다.
}
~~~

View는 이전 Snapshot의 IsSelectable을 화면 표시 용도로만 사용한다. 선택의 최종 권한은 TrySelectNode와 CurrentMap에 있다.

후속 연결은 StageMapSession 안에서 하지 않는다. 선택 성공 후 상위 Bootstrap이 SelectedStageKey로 Combat Stage Definition을 찾고 InitialEnemyPlacements, Initial RailCells, StageGimmickSpec[]을 포함한 StageConfig를 구성해 StageSessionFactory에 넘긴다. 구체 계약은 Stage Initial Enemy Bootstrap 및 Stage Gimmick & Timeline 설계서의 연계 절을 따른다.

## 3. 자주 틀리는 구현과 정답

| 잘못된 구현 | 문제 | 정답 |
|---|---|---|
| raw WidthIndex 차이로 가까운 노드 계산 | 서로 다른 너비에서 오른쪽 정렬이 무너짐 | 슬롯 중심 정규화 위치 사용 |
| 모든 다음 Node 중 무작위 추가 연결 | 교차와 비직관적 분기 증가 | 두 번째로 가까운 대상만 추가 |
| target Incoming 보정 없이 source Outgoing만 보장 | 고립된 표시 노드 생성 | 기본/추가 후 target별 Incoming 보정 |
| Boss도 일반 Layer 연결 처리 | Boss 규칙이 확률에 의존 | 직전 Layer 모두 Boss 하나에 연결 |
| PreviousNodeIds 저장 | 데이터 중복과 불일치 가능 | 검증 때만 지역 reverse adjacency 생성 |
| View가 Depth + 1을 확인하고 상태 이동 | View가 게임 규칙 소유 | NodeId만 TrySelectNode에 전달 |
| StageMapSession이 StageSession을 생성 | Run Map과 실제 Stage 수명 결합 | StageKey만 반환하고 상위 조립에 위임 |
| StageData를 Core에서 직접 참조 | Unity 의존으로 noEngineReferences 위반 | Unity가 StageID를 StageKey로 변환 |
| StageMapGenerationConfig 하나 때문에 Configs 폴더 신설 | 기존 입력 DTO 분류와 어긋나고 폴더만 늘어남 | 현재 Commands 관례를 유지하고 설정 계약이 충분히 늘 때만 재검토 |
| Boss 선택 즉시 RunCleared 처리 | Boss 전투와 노드 도달 혼동 | IsAtFinalNode만 true |
| 실패 시 Snapshot 생략 | View 원상 복구 기준 없음 | 성공/실패 모두 CurrentMap 반환 |

## 4. Final Acceptance Checklist

- [ ] StageKey와 StageNodeId가 서로 다른 value object다.
- [ ] DepthCount가 Start와 Boss를 포함한다.
- [ ] Start Node는 StageKey.Empty이고 NodeId 1이다.
- [ ] 중간 노드 StageKey는 입력 regular pool에서만 나온다.
- [ ] regular pool 중복은 가중치로 동작한다.
- [ ] bossStageKey는 중간 노드에 배치되지 않는다.
- [ ] Boss는 마지막 Depth에 정확히 하나다.
- [ ] 각 Layer 너비는 1..WidthMax다.
- [ ] NodeId는 Depth/Width 순으로 연속 발급된다.
- [ ] 기본 연결은 정규화 위치가 가장 가까운 대상을 포함한다.
- [ ] 확률 추가 연결은 두 번째로 가까운 대상 하나만 추가한다.
- [ ] Incoming 보정 후 비-Start 고립 노드가 없다.
- [ ] 모든 Edge는 Depth + 1이다.
- [ ] 모든 비-Boss 노드가 Outgoing을 가진다.
- [ ] 모든 노드는 Start에서 도달 가능하다.
- [ ] 모든 노드에서 Boss로 도달 가능하다.
- [ ] Boss 직전 모든 노드는 Boss에만 연결된다.
- [ ] Node는 PreviousNodeIds를 보관하지 않는다.
- [ ] CanSelectNode는 상태를 바꾸지 않는다.
- [ ] TrySelectNode는 실행 시점의 현재 Edge를 다시 검증한다.
- [ ] 실패 선택은 CurrentNodeId를 보존한다.
- [ ] 성공 선택은 StageKey와 변경 후 Snapshot을 반환한다.
- [ ] Snapshot 배열은 방어적으로 복사된다.
- [ ] View boundary는 Application과 Contracts만 참조한다.
- [ ] Core Stage Map 코드에 UnityEngine 타입이 없다.
- [ ] Stage Map 계획 Task 1~4와 돌아온 가이드 Task 5~7은 기존 StageSession, StageSessionFactory, StageMapState, StageData, StageNodeData를 변경하지 않는다. 단, 그 사이 Enemy Bootstrap과 Gimmick 계획이 StageSession과 StageSessionFactory에 가한 명시적 변경은 허용한다.
- [ ] Stage Map 계획 Task 1~4 뒤 Enemy Bootstrap Task 1~4와 Gimmick & Timeline Task 1~8을 완료하고, 그 다음 이 가이드 Task 5~7을 진행했다.
- [ ] focused, Core 전체, View boundary, Unity EditMode 테스트가 모두 통과한다.
