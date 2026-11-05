# DrawPointLineSystem Task 계획

설계 원본: [DrawPointLine 설계](../specs/2026-10-10-draw-point-line-design.md).

## Phase 0 — 설계 확인 (완료)

- [x] AreaOutline의 System/Pool/Handle/Data 구현 대조.
- [x] Mesh·Color·Effect의 공개 계약과 자원 반환 경로 확인.
- [x] SSOT·SOLID·JMH 지침·FW 계약 점검을 설계에 기록.
- [x] LineColor/EndPoint 리소스 공백 기록.
- [x] 사용자 Phase 1 구현 승인: InitData는 데이터, PostBindService는 서비스. 리소스와 Factory 연결은 Phase 2에 남긴다.

## Phase 1 — 독립 시스템·풀·핸들

- [x] View/DrawPointLineSystem에 서비스/DTO/System/LinePool/LineHandle과 GUID가 고유한 .meta 추가.
- [x] LinePool에서 하위 핸들을 조립해 반환, 실패 시 롤백.
- [x] 순서 있는 격자점 → MultiLine, Color 오버라이드, 끝점 Anchor·EffectScope 적용.
- [x] 대여/반환/Shutdown. 사용자 후속 지시로 핸들 자체를 재사용하며, 반환한 참조는 폐기하는 계약으로 정정.
- [x] EditMode 테스트 13케이스 작성: 데이터/서비스 분리와 서비스·모듈 등록, 좌표/순서, 0·1·2점 이상, 색, 임의 프로퍼티, 끝점 이동 시 Effect 중복 재생 없음, 반환·핸들 재대여·Shutdown·실패 롤백·잘못된 입력·반환 인터페이스만으로 Handle 사용.
- [x] 시스템/테스트 어셈블리 MSBuild 컴파일 성공. Unity 실행 제한 확인. Phase 종료 후 멈춰 보고.

Phase 1에서는 Factory에 추가하지 않았다. Phase 2 후속 승인에서 준비된 리소스와 함께 등록했다. 게임 Core/렌더 하위 System은 수정하지 않는다.

## Phase 2 — 리소스와 런타임 등록

아래는 초기 계획이다. 최신 승인 범위·완료 여부는 아래 "Phase 2 후속 승인"과 실행 기록을 따른다. 끝점 Effect는 제외하고 Factory와 이동 경로 소비자 연결까지 승인됐다.

- [ ] LineColor 머티리얼과 EndPoint Effect 등록/선정 확인. 새 에셋 제작이나 하위 계약 변경이 필요하면 별도 사용자 확인.
- [ ] RenderSystemFactory·RenderViewGroup·서비스 키에 등록/조회 추가.
- [ ] Framework 공용 연결 변경의 공유 문서 기록.
- [ ] 구성 테스트·DB 에셋 변경 시 DataBaseAssetTests, Unity 화면 확인: 꺾인 선, 색 변경, 끝점 유지/이동/숨김, 재대여 상태 초기화.
- [ ] Phase 종료 후 멈춰 보고. Combat 이동 경로 소비자 연결은 별도 작업이다.

## 현재 실행 기록

- Phase 1 코드 완료. InitData에서 LineColor 머티리얼 조회/검증, PostBindService에서 렌더 서비스만 연결, Initialize에서 LinePool 생성.
- 신규 파일과 폴더 GUID 6/6 고유함을 확인했다. 게임 Core/렌더 하위 시스템은 수정하지 않았다.
- `ProjectL.Tests.Framework.csproj`와 참조 어셈블리 MSBuild 성공(오류 0). 기존 변경하지 않은 파일 경고 10건, 신규 파일 경고 0건. 새 파일은 임시 CompileOverrides.targets로 추가해 검증했으며 Unity 생성 csproj는 수정하지 않았다.
- Unity batchmode EditMode 실행은 프로젝트 중복 열기 제한으로 종료됐다. `Temp/DrawPointLineTests/editor.log`에 `another Unity instance is running with this project open` 기록. 결과 XML이 없으므로 테스트 통과로 기록하지 않는다. 기존 Unity 인스턴스를 종료하거나 잠금 파일을 삭제하지 않았다.
- Factory·서비스 키·ViewGroup 등록과 실제 LineColor/EndPoint 에셋, Combat 이동 경로 소비자 연결은 하지 않았다.
- 이전 SelectOutlineSystem 이름 변경과 다른 사용자 수정은 그대로 보존한다.

### 사용자 정정 — Node 제거·Pool 인터페이스·System 이름

- 별도 LineNode를 제거하고 CustomPool<LineHandle>로 변경. LinePool.GetHandle이 조립된 LineHandle을 반환한다.
- LineHandle은 구체 LinePool 대신 ILineHandlePoolService에만 의존한다. DrawPointLineSystem의 Pool 필드도 ILinePoolService다.
- 클래스/서비스/폴더/네임스페이스/테스트를 DrawPointLineSystem 이름으로 정리. .meta GUID는 모두 유지한다.
- 핸들 자체 재사용에 맞춰 이전 별도 대여 객체 테스트를 교체하고, 인터페이스 반환 테스트를 추가했다.
- 정정 후 시스템·테스트 어셈블리 모두 MSBuild 성공(오류 0, 신규 파일 경고 0). 기존 UI/테스트 파일 경고만 출력됐다.
- 신규 소스의 LF·공백 및 GUID 6개 고유성 확인. 파일·폴더 이름 변경은 기존 GUID를 유지했다.
- 앞서 확인한 Unity 프로젝트 중복 열기 제한은 그대로이며 EditMode 13케이스 실행 통과 증거는 아직 없다. Phase 1 정정 작업은 여기서 멈춘다.
- 리소스·Factory 연결은 이번 정정 범위가 아니며 Phase 2에 유지한다.

## Phase 2 후속 승인 — Factory와 이동 경로 소비자 연결

사용자 지시: Factory에 연결하고 AreaOutline 갱신 부분에서도 새 시스템을 사용한다. 기존 대화에서 확정한 대상은 이동 중 강조되는 경로다.

- [x] 준비된 _LineColor 에셋을 LineShader(URP Lit) 머티리얼로 연결하고 MaterialTable에 등록. 기존 Shader/머티리얼 자체는 수정하지 않는다.
- [x] Factory·RenderViewGroup·서비스 키에 DrawPointLineSystem 등록.
- [x] PostBindService → CombatViewSystem → CombatSceneDisplay → FieldView → FieldOutlineRenderer로 명시적 서비스 전달.
- [x] FieldHighlightState에서 MovementDraft의 입력 순서를 보존. 칸 비트는 표시용 파생값이며 경로를 다시 계산하지 않는다.
- [x] GridOutline 모드의 MovementDraft만 DrawPointLineSystem으로 표시. 범위/영향/호버/행위자/배치 구역은 AreaOutline 유지. CellPrefab 모드의 기존 색 표시는 유지.
- [x] 입력 순서·지우기 상태 테스트 작성과 관리 코드 실행. 선 갱신·해제·실패 정리·기존 표시 조합·DB 등록 Unity 테스트 작성/컴파일 완료.
- [ ] Unity EditMode 테스트 실행 및 GridOutline 화면 확인. DataBaseAssetTests도 실행한다.
- [x] Framework 연결 변경 공유 메모와 컴파일 결과 기록 후 멈춰 보고.

끝점 전용 Effect는 현재 미등록이므로 임의 TestFire 대체 없이 Empty를 유지한다. 새 Effect 제작/등록은 하지 않는다. Core·FieldCamera·Architecture, 사용자 에셋 이동은 건드리지 않는다.

### Phase 2 실행 기록 (2026-10-10)

- 코드 연결 완료. 머티리얼 SO GUID 73cfa37745bceab449c1ea001c45f57c → MaterialTable, 머티리얼 GUID 57ea059c63ab8ba41b274f17749421f0 → LineColor SO 연결을 확인했다. URP Lit Shader GUID와 _BaseColor 선언도 확인했다.
- Systems·Tests.Framework MSBuild 모두 오류 0. 기존 UI 경고 7건·테스트 경고 2건뿐이며 이번 변경에 새 경고는 없다. 새 파일은 기존 임시 CompileOverrides.targets를 통해 포함했으며 Unity csproj를 편집하지 않았다.
- FieldHighlightStateTests 중 Unity 객체가 필요 없는 상태 테스트 7건을 컴파일 산출물에서 관리 코드로 직접 실행해 모두 통과했다(신규 순서/복사/읽기 전용/정리 테스트 2건 포함). Unity Test Runner 결과가 아니다.
- 신규 Renderer 테스트 3건(순서/캐시/재정렬/빈 경로와 다른 층 유지, 선 반환 실패 정리, 선 생성 실패 롤백)·Catalog 라인 등록 테스트 1건 추가. 기존 생성자/서비스 테스트 호출도 변경했다. Unity 실행 통과로 기록하지 않는다.
- Unity가 이미 같은 프로젝트를 열고 있어 Phase 1에서 확인한 batchmode 중복 프로젝트 제한이 유지된다. 실행 중인 에디터 종료·잠금 삭제·동일한 실패 재시도는 하지 않았다.
- 공유 메모: ../handoffs/2026-10-10-draw-point-line-framework-share.md (준비만 했으며 미전달).
- 끝점 파티클 제작·등록은 미실시. 화면에서는 기존 호버 테두리가 끝점 상태를 표시한다. 이번 Phase 구현은 여기서 멈추고 리뷰를 받는다.
