# DrawPointLineSystem 설계

상태: 2026-10-10 Phase 2 코드 완료. 사용자 승인으로 Factory 등록과 GridOutline의 이동 경로 소비자를 연결했다. InitData는 데이터, PostBindService는 서비스이며 Node 없이 핸들 자체를 풀링하고 Pool 인터페이스에 의존한다. Unity 실행·화면 확인은 대기다.

## 1. 목적과 경계

`RenderSystem/View/DrawPointLineSystem/`에 순서가 있는 격자점을 선으로 연결하고 마지막 점의 Effect를 표시하는 상위 렌더 시스템 `DrawPointLineSystem`을 추가한다.

- 이동 가능 여부나 경로를 계산하지 않는다. 호출자가 Buffer에서 얻은 표시할 좌표를 넘긴다.
- 게임 Core, InGame DB, Buffer를 직접 참조하지 않는다.
- MeshSystem·ColorSystem·EffectSystem은 **Render/Core의 렌더 인프라**다. 게임 Core와 다른 계층이며 공개 서비스/핸들을 사용한다.
- Phase 1은 기존 소비자를 변경하지 않았다. Phase 2 후속 승인으로 GridOutline의 MovementDraft 소비자만 연결한다. AreaOutline·SelectOutline 구현 자체는 변경하지 않는다.

## 2. AreaOutline에서 참고하는 것

- System의 BindScene → InitData → PostBindService → Initialize → Shutdown 역할.
- System이 머티리얼을 구하고 풀에 전달하는 구조.
- 외부에는 Create와 Update/Dispose 핸들만 노출.
- 메시 갱신은 Mesh Handle, 머티리얼 프로퍼티 갱신은 Color Handle로 위임.
- 반환 시 상태 제거와 Shutdown 시 대여 자원 정리.

**요청에 따른 차이:** AreaOutline은 System.Create에서 하위 핸들을 조립한다. DrawPointLineSystem에서는 LinePool이 서비스 인터페이스를 받아 Mesh·Color·Effect 핸들이 조립된 LineHandle을 반환한다. AreaOutline처럼 별도 Node 없이 핸들 자체를 풀링한다. 기존 AreaOutline 자체를 수정하지 않는다.

## 3. 책임과 계약 제안

| 구성 | 책임 |
|---|---|
| `IDrawPointLineSystemService` | `ILineHandle Create()` |
| `DrawPointLineSystem` | 초기 데이터·렌더 서비스 연결, LinePool 생성과 전체 수명 관리. 보관/사용은 ILinePoolService로 한다 |
| `ILinePoolService` / `LinePool` | LineHandle 자체 대여/반환, 하위 핸들 조립과 실패 시 롤백 |
| `ILineHandlePoolService` | Handle이 Pool 구현을 모르고 자신을 반환하는 좁은 계약 |
| `ILineHandle` / `LineHandle` | `Update(DrawPointLineData)`, `SetOverrideData<T>(TypeSettingKey<T>, T)`, `Dispose()` |
| `DrawPointLineData` | 순서 있는 Cells, GridOrigin, CellSize, YOffset, Thickness, UvScale, Color, 끝점 EffectType |

### 초기화

- BindScene: RenderRuntimeRoot 아래 `DrawPointLineRoot`를 확보한다.
- InitData: `IHasMaterialQuery`에서 `_LineColor` 키의 머티리얼을 받고 `_BaseColor` 지원을 검사한다. 준비된 LineColor SO를 MaterialTable에 등록하고 LineShader(URP Lit) 머티리얼에 연결했다.
- PostBindService: Global.RenderServiceCommand.CoreGroup에서 `IMeshSystemService`, `IColorSystemService`, `IEffectSystemServices`만 꺼내 보관한다. 서비스 키 ParticleSystem의 실제 제공 서비스는 EffectSystem이다.
- Initialize: 준비 상태를 검사하고 머티리얼·루트·세 서비스를 LinePool에 넘긴다. 다른 System을 직접 new하지 않는다.
- Create: LinePool에서 조립 완료된 LineHandle을 받는다. 서비스와 모듈은 Framework에 각각 등록한다.

### LinePool과 핸들 조립

- 별도 Node는 없다. CustomPool<LineHandle>이 핸들 자체를 보관한다. LineHandle이 GameObject, MeshFilter, MeshRenderer, EndPointAnchor와 재사용할 EffectScope를 가진다.
- MeshRenderer.sharedMaterial에 초기 머티리얼을 연결한다. 머티리얼 인스턴스를 매번 복제하지 않는다.
- 대여 시 MeshSystem.Register와 ColorSystem.CreateColorHandle로 하위 핸들을 받는다.
- LineHandle은 ILineHandlePoolService만 받으며 구체 LinePool을 필드/생성자 인자로 갖지 않는다. DrawPointLineSystem도 ILinePoolService로 사용한다.
- 핸들 자체가 재사용되므로 반환한 참조는 폐기한다(FW-103). 반납 중인 핸들의 갱신은 금지지만 재대여 후 옛 참조와 새 참조는 같은 객체다. 이전 버전의 별도 대여 객체 보장은 제거했다.
- EffectScope는 풀링되는 LineHandle당 한 번만 받는다. 현재 EffectSystem에는 외부 Scope 해제 API가 없으므로 매 대여마다 Scope를 새로 만들어 시스템 목록을 늘리지 않는다. LineHandle이 이를 사용하고 반환 시 StopAll한다.
- 조립 실패 시 이미 받은 Mesh/Color 핸들을 반환하고 LineHandle을 초기화해 풀에 돌려준다.

### Update

1. Cells 배열 순서를 유지한다. 집합화·정렬·경로 탐색을 하지 않는다.
2. 격자 셀 중심을 XZ 좌표로 변환한다. `(0,0)` 중심은 GridOrigin, Y는 GridOrigin.y + YOffset이다.
3. `MultiLineCreateData`로 IMeshRenderHandle.Update를 호출한다. 기존 MeshSystem을 수정하지 않는다.
4. 메시의 vertex color는 흰색으로 두고, 요청 Color는 IColorHandle의 `_BaseColor`로 적용한다. 색의 원본을 두 곳에서 곱하지 않는다.
5. 마지막 변환점을 EndPointAnchor 위치로 사용한다. 별도 EndPoint 좌표 필드를 두지 않는다.
6. EffectScope.Play에 Anchor를 부모로 전달한다. 좌표만 바뀌면 Anchor만 옮겨 Effect를 매 Update마다 재생/대여하지 않는다. Effect 종류가 바뀌면 기존 것을 StopAll 후 교체한다.

빈 입력 정책: 0개는 선과 Effect 숨김, 1개는 끝점 Effect만 표시, 2개 이상은 선+끝점 표시. EndPointEffect가 Empty이면 선만 표시한다(미지정 자원을 다른 Effect로 대체하지 않는다). 끝점은 표시를 유지하는 looping Effect 프리팹을 전제로 한다. 루프가 아닌 자연 종료 Effect의 반복 재생 정책은 별도 요구가 없으면 추가하지 않는다.

### 반환과 Shutdown

- LineHandle.Dispose는 한 대여 안에서 멱등이다. 반납 중 Update/SetOverrideData는 ObjectDisposedException이다. 재대여 후 옛 참조를 사용하는 것은 계약 위반이다.
- 반환은 ILineHandlePoolService.Return을 통해 수행한다. Pool의 OnBeforeReturn에서 Handle.Reset(EffectScope.StopAll, Mesh/Color Dispose, PropertyBlock 초기화, 비활성화)을 호출한다.
- DrawPointLineSystem.Shutdown은 활성 핸들을 먼저 반환하고 자체 GameObject·Anchor를 파괴한다. EffectPool 내부 객체는 직접 파괴하지 않는다.
- Factory 등록 순서는 렌더 하위 시스템 뒤로 두어 역순 Shutdown에서 상위 시스템이 먼저 정리되게 한다.
- Update에서 Find/GetComponent/LINQ와 EffectScope 생성은 하지 않는다. 변환 배열은 필요 크기 변경 시에만 재할당한다. 기존 MeshSystem 내부 메시 생성 비용은 별도 소유 영역이다.

## 4. 등록과 변경 범위

- 신규: DrawPointLineSystem.cs, DrawPointLineData.cs, LinePool.cs, LineHandle.cs 및 Unity .meta. 폴더/파일 이름 변경 시 기존 GUID를 유지한다.
- 연결: RenderSystemFactory, RenderViewGroup, RenderSystemServiceKey에 필요한 줄만 추가.
- 검증: 렌더 조합·수명 EditMode 테스트. Framework 공용 연결 변경은 민형님 공유 메모에 기록한다.
- 제외: 게임 Core, FieldCamera, Framework Architecture, 기존 렌더 하위 시스템 리팩터링, 끝점 Effect 자원 제작.

## 5. 원칙 점검

- SSOT: 경로 원본은 호출자가 받은 Buffer 값. EndPoint는 마지막 점에서 도출. 머티리얼 원본은 DB, 런타임 색 오버라이드는 Color Handle.
- SRP: System(연결/수명), Pool(대여/조립), Handle(표시 갱신), Mesh(형상), Color(프로퍼티), Effect(파티클).
- OCP/DIP: 새 상위 시스템을 추가하고 기존 공개 렌더 서비스를 사용한다. Pool에 전체 Global Reader를 넘기지 않는다.
- FW-001/002/003: 제공 서비스 인터페이스 + 모듈 등록, Factory Builder는 한 번 Build.
- FW-010/011: 구체 Mesh DTO와 공개 Handle 반환 경로 사용.
- FW-012: 등록된 머티리얼/EffectType을 사용한다. Effect 프리팹 루트에는 ParticleSystem과 ParticleStopNotifier가 필요하다.
- FW-014/016: CustomPool의 Get/Return Lifecycle과 Framework 초기화 선행 조건을 따른다.

## 6. 구현 전 확인

- Phase 1 당시 라인용 등록은 없었다. Phase 2에서 사용자가 준비한 `_LineColor` SO·LineShader 머티리얼을 연결하고 Catalog 경유 등록 검사 테스트를 추가했다.
- 현재 EffectType은 Empty/TestFire뿐이다. EndPoint 전용 타입과 looping 프리팹을 추가할지, 지정한 기존 Effect를 사용할지 확인한다. 임의로 TestFire를 사용하지 않는다.
- 리소스 미확정 상태에서 필수 DB 조회를 Factory에 연결하면 기존 부팅을 실패시킬 수 있으므로 통합 Phase는 리소스 확보 뒤에 한다.

## 7. Phase 2 — 이동 경로 소비자 계약

- Factory는 Mesh·Color·Effect 뒤에 시스템을 등록한다. 역순 Shutdown에서 선 핸들을 하위 시스템보다 먼저 반환한다.
- 서비스 연결: RenderViewGroup.DrawPointLineSystemServices → CombatViewSystem.PostBindService → CombatSceneDisplay → FieldView → FieldOutlineRenderer. 소비자는 인터페이스만 받고 Pool을 받지 않는다.
- CombatSceneDisplay.ShowDraft의 MovementPath.Cells가 순서의 원본이다. FieldHighlightState는 받은 순서를 복사해 읽기 전용 MovementPath로 노출한다. 기존 칸별 MovementDraft 비트는 포함 여부·CellPrefab 색 표시용 파생값이다. 경로 탐색이나 규칙 판단을 추가하지 않는다.
- 칸 집합이 같아도 경로 순서가 달라지면 Repaint를 알린다. ClearLayer/Reset/Clear는 경로와 비트를 함께 비운다. 다른 층처럼 격자 밖 좌표는 제외하며 유효한 좌표의 순서·중복 방문은 유지한다.
- GridOutline의 MovementDraft 슬롯만 ILineHandle을 대여한다. 나머지 6슬롯은 AreaOutline이다. 전투 수명 동안 유지하며 빈 경로 Update로 숨기고 Release에서 함께 반환한다. 생성 실패·반환 실패 때 다른 핸들도 정리한다.
- CellPrefab 모드는 기존 색 표시를 유지한다. 범위·영향·행위자·호버·배치 구역도 기존 AreaOutline 표시를 유지한다.
- 현재 끝점 전용 Effect가 없으므로 EndPointEffect.Empty로 전달한다. 유효/무효/대기 상태는 기존 호버 테두리가 계속 표시한다. TestFire 대체나 신규 자원 제작은 하지 않는다.

SSOT·SOLID·FW 점검: 경로 순서 원본은 MovementPath.Cells, 표시 복사본의 변경 소유자는 FieldView뿐이다. Renderer는 읽기 계약을 사용한다. 전역 서비스 조회는 PostBindService 한 곳에서만 하고 이하에는 좁은 인터페이스를 명시적으로 전달한다. 게임 Core/InGame DB 접근을 추가하지 않았다. FW-001/002 등록, FW-003 Builder 1회, FW-103 반환 참조 폐기, 하위 핸들보다 상위 핸들 먼저 해제 계약을 유지한다.
