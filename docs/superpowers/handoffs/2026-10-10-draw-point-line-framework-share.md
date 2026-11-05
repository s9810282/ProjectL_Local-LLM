# DrawPointLineSystem 런타임 연결 공유 메모

2026-10-10 작성. 미전달. 사용자 승인 범위: Factory 등록과 기존 이동 경로 표시 소비자 연결.

## 공개 연결 변경

- RenderSystemServiceKey.DrawPointLineSystem: IDrawPointLineSystemService 키 추가.
- IRenderViewServiceContext.DrawPointLineSystemServices: 공개 서비스 조회 추가.
- RenderSystemFactory: Mesh/Color/Effect 뒤에 DrawPointLineSystem 서비스·모듈 등록. 상위 선 시스템을 역순 Shutdown에서 먼저 정리한다. Builder 추가 Build는 없다.
- InitData는 IHasMaterialQuery로 _LineColor 머티리얼을 조회한다. PostBindService는 Render/Core의 Mesh·Color·Effect 공개 서비스를 받는다. 게임 Core와는 무관한 렌더 인프라 의존성이다.

## 소비자·리소스

- CombatViewSystem.PostBindService에서 공개 서비스를 한 번 받아 CombatSceneDisplay → FieldView → FieldOutlineRenderer에 IDrawPointLineSystemService를 명시적으로 넘긴다. 소비자는 구체 Pool이나 System에 의존하지 않는다.
- GridOutline MovementDraft 슬롯은 ILineHandle, 나머지 슬롯은 IAreaOutlineHandle이다. CellPrefab의 기존 색 표시는 유지한다.
- MovementPath.Cells의 순서를 FieldHighlightState의 읽기 전용 복사본에 보존한다. 경로를 재계산하지 않고 Buffer/Core 경계도 추가하지 않는다.
- 사용자 준비 LineColor SO(_LineColor)를 MaterialTable에 등록하고 LineShader 머티리얼(URP Lit)에 연결했다. Shader·머티리얼 내용은 편집하지 않았다.
- 끝점 전용 Effect는 없으므로 Empty를 사용하며 기존 호버 테두리가 상태를 표시한다. 전용 파티클 연결은 후속이다.

## 점검·검증

- SSOT: 순서는 호출자가 보유한 MovementPath.Cells에서 받는다. EndPoint는 마지막 점으로 도출한다. 칸별 비트는 표시용 파생값이다.
- SOLID: System 연결/수명, Pool 대여/조립, Handle 표현 갱신을 분리하고 서비스·반환 인터페이스를 유지한다.
- FW: 서비스와 모듈 함께 등록, Builder 1회, 반환 참조 폐기, 소비자 핸들 우선 정리. Framework Architecture·게임 Core·FieldCamera는 수정하지 않았다.
- Systems·Tests.Framework 컴파일 성공(오류 0, 신규 경고 0). 순수 상태 테스트 7건 관리 코드 직접 실행 통과.
- Unity Renderer/DrawPointLineSystem/DataBaseAssetTests 실행과 GridOutline 경로 화면 확인은 대기다. 이미 열린 프로젝트로 batchmode를 별도로 열 수 없다.

검증/후속 원본: ../plans/2026-10-10-draw-point-line-plan.md의 Phase 2 실행 기록.
