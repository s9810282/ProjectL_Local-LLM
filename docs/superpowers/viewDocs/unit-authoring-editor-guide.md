# 유닛 통합 에디터 사용 안내 — 2026-11-10

메뉴: **ProjectL > 유닛 통합 에디터**. Phase 1·2 및 리뷰 4건 수정 코드 완료, **컴파일 미확인**.

## 새 몬스터 만들기

1. `복제하여 생성` 탭에서 기존 몬스터 Unit SO를 선택한다. 같은 Key의 Visual SO가 자동으로 연결된다.
2. `새 enum 항목 추가`를 켜고 `Unit_새이름`과 양의 정수 번호를 입력한다. `빈 번호 제안`으로 현재 소스·SO에서 사용하지 않은 번호를 제안받을 수 있다.
3. 표시 이름, 두 저장 폴더, 두 등록 테이블과 표시된 Catalog 연결을 확인한다. 실제 재생에서 사용하는 Catalog에도 이 테이블들이 연결돼 있어야 한다.
4. `생성 · 등록`을 누른다. EntityKey.cs가 바뀌고 Unity 재컴파일이 진행된다. 성공하면 SO 두 개를 복제하고 같은 Key를 지정하여 각 테이블에 등록한다.
5. 완료하면 `규칙 · 외형 편집` 탭으로 이동한다. 능력치·특성, 프리팹·아이콘·연출 등을 수정하고 `검증 · 저장`을 누른다.
6. 스테이지 직접 배치 또는 EnemyLoadout·증원 후보에서 새 Unit SO를 연결한다. 랜덤 출현·증원은 해당 스테이지의 UnitRoster에도 추가한다.

원본 유닛·외형 SO는 먼저 저장해야 한다. 특성·연출·프리팹 등 연결된 에셋은 공유한다. 이 도구는 참조 에셋까지 복제하지 않는다.
저장 폴더는 Assets 아래 기존 폴더를 사용하며 StreamingAssets 및 하위 폴더는 사용할 수 없다.

## 기존 데이터 편집

`규칙 · 외형 편집` 탭에서 Unit을 고르면 같은 Key의 규칙·외형을 함께 편집한다. Key는 읽기 전용이다. 기존 특성 선택 drawer를 그대로 사용한다.

값은 실제 SO에 즉시 반영되고 Undo/Redo를 지원한다. `검증 · 저장`은 두 SO의 기존 검증을 실행한 뒤 저장한다. 창을 닫거나 다른 유닛을 고르면 기존 변경은 SO의 미저장 상태로 남는다. 일반 Unity Save Project로도 저장될 수 있으므로, 이 창의 검증은 모든 외부 저장을 차단하는 기능은 아니다.

짝 Visual 누락이나 같은 Key의 SO 중복은 오류로 표시한다. 이 도구에서 기존 키를 바꾸거나 에셋을 삭제하지 않는다.

## 실패·중단 후

- **컴파일 오류:** Console 오류를 해결하고 `재검사 · 재개`를 누른다. 컴파일 성공 전에는 SO와 테이블을 만들지 않는다.
- **원본 변경:** 아직 복제할 SO가 있을 때 원본이 바뀌면 중단한다. 이미 만든 두 SO는 재복제하지 않으며 현재 내용을 검증하여 사용한다.
- **등록 실패:** 생성한 SO·GUID·완료한 등록을 보존한다. `재검사 · 재개`로 남은 단계부터 이어간다. 완료 기록과 실제 GUID·키·참조가 다르면 자동 덮어쓰기 없이 오류를 표시한다.
- **취소:** `요청 취소 · 생성물 삭제`는 이번 요청의 SO와 해당 테이블 참조를 삭제한다. 취소 도중 중단된 요청을 재개하면 남은 삭제만 이어간다. 추가한 enum 항목은 유지된다. `새 enum 항목 추가`를 끄면 SO에서 사용하지 않은 기존 Unit_ 키를 다시 선택할 수 있다.
- **컴파일 후 키 불일치:** 컴파일·재로드가 끝났는데 입력한 이름·번호의 키가 없으면 오류를 표시한다. 완료 이벤트나 재로드를 확인하지 못하는 상태도 제한 대기 뒤 오류로 전환한다. 소스와 Console을 확인한 뒤 재개한다.
- **enum 원본 백업:** `Library/ProjectLUnitAuthoring/backups/<작업 ID>/EntityKey.cs.original`에 보관하고 창에 경로를 표시한다. 원본을 직접 덮어쓰지 않고 완성된 임시 파일로 교체한다. 백업을 복원할 필요가 있으면 현재 소스와 비교하여 후속 변경을 보존한다. 자동 복원은 하지 않는다.
- **창 닫기:** 같은 Unity 세션에서는 컴파일 후 자동으로 이어진다. 창을 다시 열어 결과를 확인한다.
- **Unity 재시작:** 보관된 요청을 표시한다. 내용을 확인하고 명시적으로 재개한다. 기록 위치는 `Library/ProjectLUnitAuthoring/request.json`이다.
- **강제 종료의 예외:** 에셋 파일 생성 직후 GUID 기록 전에 프로세스가 종료되면 소유권을 확정할 수 없다. 이 경우 파일을 자동 삭제하지 않고 경로 충돌로 중단하므로 해당 파일을 직접 확인한다.

등록 테이블이 여럿이거나 두 테이블을 함께 포함하는 Catalog가 없으면 구성을 먼저 확인한다. 도구가 Catalog나 스테이지를 임의로 고치지 않는다.

## Unity 확인 항목

컴파일 → 새 키 생성 → 재컴파일 후 두 SO의 같은 Key → 각 테이블에 1회 등록 → 규칙·외형 편집과 저장·Undo 순서로 확인한다. 실제 DB 에셋 변경 후 기존 `DataBaseAssetTests`도 확인한다. 상세 실패·재시작 확인 항목은 [Task 계획](../plans/2026-11-10-unit-authoring-editor-plan.md)에 있다.

구현 참고: Unity의 [RequestScriptCompilation](https://docs.unity3d.com/6000.0/Documentation/ScriptReference/Compilation.CompilationPipeline.RequestScriptCompilation.html)으로 컴파일을 요청하고 [scriptCompilationFailed](https://docs.unity3d.com/6000.0/Documentation/ScriptReference/EditorUtility-scriptCompilationFailed.html)와 로드된 enum을 확인한다. 값 편집은 [SerializedObject](https://docs.unity3d.com/6000.0/Documentation/ScriptReference/SerializedObject.html)를 사용한다.
리뷰 수정은 [CompilationPipeline의 시작·완료·어셈블리 오류 이벤트](https://github.com/Unity-Technologies/UnityCsReference/blob/6000.0/Editor/Mono/Scripting/ScriptCompilation/CompilationPipeline.cs)를 함께 기록한다. 소스 교체는 [.NET File.Replace](https://learn.microsoft.com/en-us/dotnet/api/system.io.file.replace)를 사용한다.
