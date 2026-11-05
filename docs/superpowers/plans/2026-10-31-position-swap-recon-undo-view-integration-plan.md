# 위치 교환·정찰 부대·효과 인식 이동 취소 View Task 계획

작성: 2026-10-31, View/Codex. 설계 원본: [View 대응 설계](../specs/2026-10-31-position-swap-recon-undo-view-integration-design.md).

상태: **Phase A~E 구현 및 F의 코드 컴파일·정적 검증·문서 인계 완료. 실제 에디터 임포트/컴파일·DB 시험·Play는 사용자 후속**. 사용자 A~F 일괄 구현 승인 후 Core 병합0403babef(제공자2bd95ad34)을 확인해 G1/G2를 해제하고 순차 구현했다. F1/F2의 실제 에디터 검증은 대기로 남긴다. 현재 실행 원본은 §10이다.

기준: 설계 작성 당시 `8ac16c1ed`, 구현 착수 시 Core 병합 `0403babef`와 현재 작업 트리. 기존6개 사용자 변경(FMOD 캐시, 전투 Test/Akimbo, 가속 SO, 배경 타일 프리팹, Combat 씬)을 보존한다. 커밋·push·스테이징·외부 발송은 사용자 별도 지시 없이는 하지 않는다. 새 View 테스트·자동 검증 메뉴를 만들지 않는다. 구현 승인을 받으면 Phase 순서로 진행하고 끝마다 보고/멈춘다. 사용자가 일괄 진행을 명시하면 해당 승인 범위에서만 연속 진행한다.

## 1. 의존성과 진행 순서

```text
A 현 API의 보호막 표시·기존 연결 보존
  → B 새 Core 인수 + 효과 계약 갱신 [G1 해제]
    → C 신규 외형·카드·발동 근거 [G1 해제]
    → D 교환/쌍 복원 표시 [G1/G2 해제]
    → E 조립 v3·프리셋 [G1/G2 해제]
  → F 완료 범위 컴파일·정적 검증·사용자 인계
```

| ID | 해제 조건 | 현재 / 적용 Task |
|---|---|---|
| G1 | 사용자가 제공/병합한 Core 커밋과 실제 공개 DTO·생성자·검증 확인 | **해제:0403babef**. B~E 공개 API·생성자·검증 인수 완료 |
| G2 | 전방 교환의 종료점2사건·취소1/2 Returned와 후속 순서의 유일한 대응 확인 | **해제**. ExchangeEndpoint의 연속2 Moved와 MovementEffectUndo의 선행1/2 Returned→후속 상태/흔적 확인 |
| UI/아트 | 사용자 Description Panel/Text 연결, 깨진 방패·신규 특성 아이콘 확정 | 기존 후속 유지. 문자열 준비/기본 아이콘과 실제 표시 인수 구분 |
| CVI-R1 | 전투 밖 재생/위압 설정 Core 응답 | 이전 계획 D6 대기 유지. 이번 Recon 정량 조회와 별개 |

Core 병합 작업 자체는 본 구현 승인에 포함하지 않는다. 알려지지 않은 커밋을 찾거나 자동 병합하지 않는다. 타입이 아직 없으면 숫자 enum·reflection·조건부 alias·View mirror DTO로 우회하지 않는다. API가 문서와 달라도 다른 Phase를 무조건 중단하지 않고 영향을 받는 Task만 대기시키며 설계 차이는 사용자에게 먼저 확인한다.

## 2. Phase A — 현 API의 표시 보완과 보존 경계

상태: **코드 완료**. 새 Core 타입 참조 없이 활성 보호막 표시만 보완했다.

- [x] A1. 착수 시 AGENTS·git/동시 작업·Core 부재·현재 가속 SO 사용자 값·Body Text3차 입력/프리셋/GUID를 다시 기록한다. 첨부 §8과 달리 TraitState/발동 등록·SourceRevision 대응이 이미 있음을 실행 코드로 확인한다.
- [x] A2. CombatUnitStatuses의 Shield 활성 슬롯을 RemainingEligibleBudget>0으로 제한한다. 다른 효과 필터·Core 상태·Sources/리셋 이력은 바꾸지 않는다. 기존 표/키/분류/formatter를 재사용한다.
- [x] A3. Shield 슬롯 소멸 시 기존 UnitHUD의 고정/hover 설명 정리가 이어지는지 대조한다. 활성 보호막 상세는 비활성 출처도 유지한다. 공유 HUD 프리팹·새 설명 창을 만들지 않는다.
- [x] A4. 기존 TraitState 전체 Final/Render→AfterFacts, Field→Field Return, trail ID/Active, 결과 끝 Buffer Refresh·acknowledgment 및 Tracker/Undo token 제출을 보존한다. 실제 결함이 없는 경로는 재구현하지 않는다.
- [x] A5. 현 API 런타임/Editor 코드 컴파일·슬롯 조건/키·수명 정리·SSOT/소유권을 정적으로 대조해 보고한다. G1 대기 상태와 실제 Play 미실행을 함께 기록한다.

리뷰 지점: 보호막 활성 표시와 이력의 차이, 기존 공용 경로 보존. 이 Phase만 끝나도 새 Core 대응 전체 완료가 아니다.

## 3. Phase B — Core 계약 인수와 효과 표시

상태: **코드 완료**. G1을 해제한 뒤 실제 공개 계약으로 전환했다.

- [x] B1. 제공된 Core 커밋·병합 상태와 설계 §3의 공개 타입/불변성/생성자·Result 검증을 대조한다. 인계의 실제 규칙·발동 Magnitude/Origin/수명 및 기존 View의 소비자 차이를 기록한다. Core 파일은 수정하지 않는다.
- [x] B2. WeaknessExposure formatter를 EffectiveIncreasePercent로 전환한다. 키13/기존 SO/템플릿 인자 수를 유지하고 MaximumIncreasePercent 생산 참조를 정리한다. View 합계/최대 계산을 추가하지 않는다.
- [x] B3. 공통 상세 formatter에 UntilReserveReturnOrDeath와 nullable clock을 연결한다. timed의 양수/untimed null을 구분하고 억제 출처·원본 순서·출처 이름을 보존한다. source death/return을 대상 만료로 판정하지 않는다.
- [x] B4. HuntersMark 정책 SO와 효과/도구 문서의 옛 전체 최대값 설명을 새 그룹 최대·그룹 간 합산 계약으로 고친다. 규칙 숫자·고정 특성 수·RuleKey 제품 문구·임의 강화값은 넣지 않는다.
- [x] B5. 기존 State/HP/아이템 사건이 다른 효과·최대HP·장비·진행을 통째로 보존하고 반환 중간 상태 후 마지막 Buffer로 맞추는지 대조한다. 반환 첫 사건에서 전체 정리가 끝났다고 가정하지 않는다.
- [x] B6. 런타임/Editor 코드 컴파일과 10+20+30→50의 공개 요약 소비·수명 문구·0/비활성 보호막·nullable 출처를 정적으로 대조한다. 실제 Core 계산/시험을 View에서 중복 구현하지 않는다.

리뷰 지점: API 변경 컴파일, 그룹 합산 원본, 무기한/시한 수명 및 보호막 표시 계약. G1 불일치 부분은 대기 사유와 해당 파일을 기록한다.

## 4. Phase C — 신규 특성 외형·정량 카드·HUD 직후 문구

상태: **코드 완료**. B의 효과 의미 전환 후 진행했다.

- [x] C1. PositionSwap/ReconSquad TraitVisual SO/meta와 기존 활성 테이블 등록을 추가한다. 표시명/정책은 View, 자격/수치/강화/획득은 Core다. 아이콘 미제공은 기존 선택적 경로로 남기고 전용 아트 완료로 주장하지 않는다. 운영 유닛/획득 풀은 수정하지 않는다.
- [x] C2. UnitCardTraitContent에 Recon 설정의 공통 변환을 추가하고 같은 Owned 조회의 ReconSquadDescription을 UnitCardUI·DeckEditModelBuilder/CardModel/DeckEditUI까지 전달한다. 전투는 같은 TraitStatuses 사용. 미보유/필수 설정 누락·타입/키 불일치를 진단하며 InGame DB/기본10·1로 채우지 않는다.
- [x] C3. 약점 노출 evidence를 단일 SingleOrDefault에서 Origin/optional Movement로 식별되는 출처 목록 전후 변경으로 전환한다. 여러 이동 기여분·억제 출처·약한/짧은 timed 갱신을 다루고 목록 순서 변화만으로 발동하지 않는다. 새 Magnitude 의미를 추정하지 않는다.
- [x] C4. 공용 Profiles와 기존 Factory에 ReconSquad/StatusApplied 근거를 등록한다. StateApplied·Targets·Count None·기본 이름 줄을 연결한다. 같은 대상/발동당 한 번이며 상태 갱신/취소/역적용으로 forward 문구를 만들지 않는다.
- [x] C5. PositionSwap/MovementChanged에는 ActivationStart·Source·Count None 줄을 연결한다. 두 대상/최종 칸은 요약임을 유지하고 이동/UnitActions·공격 덮어쓰기·임의 파티클/Shake를 추가하지 않는다. Body Text 색/크기/offset/직접 문구/Prefab Override와 기존4줄 사용자 값은 유지한다.
- [x] C6. 런타임/Editor 컴파일, DB 등록/템플릿·GUID·필수 설정 전달·근거/대상/중복 방지·성장 choice 유지·가속/깔끔한 뒷정리/희생 보존을 정적으로 검증해 보고한다.

리뷰 지점: 설정 조회와 효과 수신 상태 구분, 복수 이동 출처의 발동 근거, 입력/외형의 단일 원본. 전투 밖 재생/위압 CVI-R1은 별도 대기로 유지한다.

## 5. Phase D — 전방 교환과 쌍 복원 표시

상태: **코드 완료**. G1/G2 확인 후 기존 입력/게임 상태를 유지하며 표시만 연결했다.

- [x] D1. 실제 Core 결과의 주체 경로·종료점2 Moved, undo의 Participants 및1/2 Returned, 도착 후속/상태 순서를 확인해 G2를 해제한다. 여러 후보/사건 사이 모호함이 있으면 해당 그룹을 구현하지 않고 Core 경계 인계에 확인 요청만 적는다.
- [x] D2. Result당 한 번 구성되는 내부 불변 placement batch plan을 추가하고 기존 Factory에서 조립한다. 원래 사건 참조/완료 대응만 보관한다. Summary/Magnitude/인접 사건/최신 Buffer로 위치나 승객을 만들어 내지 않는다.
- [x] D3. 전방 교환은 기존 주체 걸음의 도착 완료 경계에 두 Final을 함께 적용하고 Render한다. 승객의 독립 경로/자발 이동/가속·정찰 발동을 생성하지 않는다. 도착 효과 사건은 그 뒤 원래 순서로 진행한다.
- [x] D4. Field→Field Returned의 단일 즉시 복귀를 유지하고 쌍은 두 Final을 한 번에 표시한다. Participants/scalar/RemovedEffects로 중복 적용하지 않으며 Hand/EnemyWaiting 제거/풀 반환과 구분한다. 다른 상태 사건을 넘어 묶거나 사건 순서를 바꾸지 않는다.
- [x] D5. 실제 사용 잠금은 기존 revision 응답에 맡긴다. A→B효과 사용·보호막만 맞은 양수 피해·일시 점유 해제·B취소 후 A잠금 유지·오래된 토큰/거절·선택 유지·Abort/Detach/재연결을 회귀 대상으로 정리한다. 새 checkpoint/token 표·대기 상태 머신·Tick은 추가하지 않는다.
- [x] D6. 런타임/Editor 컴파일·사건별 한 번 소비·배치 원자 표시·후속 Gate·단일/일반 이동·끌어오기·희생·추가 공격의 공통 완료/Cancel/Release 경계를 정적으로 검증한다. 실제 쌍 타이밍/hover/풀 수명은 사용자 Play 대기로 남긴다.

리뷰 지점: 표시 배치와 Core 원자 거래의 차이, 원래 사건 식별/순서·완료 토큰, 전체 사실·선택 보존. G2가 막혀도 B/C와 쌍 입력 외 E/F의 독립 작업은 진행할 수 있다.

## 6. Phase E — 조립 시연 v3·대표 프리셋

상태: **코드 완료**. 공개 Result 검증을 만족하는 단일/쌍 필드 복원과 v3 입력/프리셋을 연결했다.

- [x] E1. 기존 v1/v2 프리셋·입력/enum 번호·안정 ID·GUID·Body Text 연출 값을 사본으로 기록한다. v1→기존v2→v3 명시 Import를 연결한다. v2 약점 노출은 timed/Movement 없음으로 옮기고 원본 에셋을 자동 저장하지 않는다.
- [x] E2. Effect 입력에 실제 Core 수명 enum과 optional Movement의 편집용 payload를 추가한다. 키에 필요한 필드만 표시하고 v3 저장/복제/Undo·Source ID의 불변 대상/키/Origin/Movement 진단을 연결한다. 무기한 clock은 null로 변환하고0턴 sentinel/SerializeReference는 쓰지 않는다.
- [x] E3. 기존 블록 번호0~7을 보존하며 명시 FieldPlacement 블록을 뒤에 추가한다. 한/두 대상의 최종 위치·회전·공개 행동/거리 사실과 기대값을 대응하고 지정하지 않은 HP/상태/아이템은 보존한다. 기존 Hand/EnemyWaiting Return 의미는 바꾸지 않는다.
- [x] E4. 실제 공개 생성자/검증을 만족하는 Result/Participants로 구성하고 운영과 같은 placement/effect/피드백 재생 경로를 사용한다. 공개 API만으로 합성이 안 되면 해당 Task만 Core 확인 대기로 둔다. Core 내부/운영 Buffer 주입·검증 우회는 금지한다.
- [x] E5. 공통 블록 데이터로 PositionSwapRestore, ReconExposureGroups, ReconMoveRemoval, ShieldHistory, ReserveCleanup 프리셋5개/meta를 저장한다. 자동 접촉/효과 만료/사용 확정/피해·회복·환불·귀환 정리를 계산하지 않는다. Native lock/재고·최대HP 전체 인수와 합성 표시 범위를 구분한다.
- [x] E6. 기존5개 효과 및 가속/CleanSweep/희생 생존·사망/ManualStep 프리셋의 GUID·시간·원래 입력을 비교한다. 등록 외형 선택/StateApplied 사전 진단·직접 문구/색/크기/offset/Override를 보존하고 바뀐 수명/합산 의미만 문서화한다.
- [x] E7. 런타임/Editor 컴파일·입력 사본 이전·직렬화 참조/필드/ID·사전 진단을 정적으로 대조하고 새 프리셋별 조작/기대 결과를 작성한다. 실제 Unity 임포트/Play는 사용자 검증 조건에 따라 미실행을 기록한다.

리뷰 지점: 조립식 유지, 공개 Result 계약과 합성 사실 경계, 기존 자료 보존. 새 특성마다 전용 실행 코드를 만들지 않는다.

## 7. Phase F — 검증·구조 재검토·인계

상태: 앞선 Phase에서 완료한 범위만 진행 가능. G1/G2/Core·아트/설명 UI·사용자 Play가 없는데 전체 완료로 기록하지 않는다.

- [ ] F1. 생성된 소스/meta/asmdef 경계와 실제 Unity 컴파일 목록을 확인한다. 런타임/Editor·비Editor 코드 컴파일과 실제 에디터 임포트/재컴파일 결과를 구분한다. 임시 응답 파일/MSBuild 소스 보완 성공만으로 Unity 등록 성공을 주장하지 않는다.
- [ ] F2. UTF-8/LF·GUID·이동/삭제·충돌 표지·새 DB 등록/키·템플릿·옛 API/수명·임의 숫자/순회·사용자 변경/인덱스를 대조한다. DB 변경 시 기존 DataBaseAssetTests를 에디터 검증 조건에 맞게 실행하거나 대기로 기록한다. 새 View 테스트/검증 메뉴는 없다.
- [x] F3. Core/공유 기반 소유권과 SSOT/SOLID5/JMH/FW·캡슐화·기능 독립성을 생성→등록/Export→조회→소비/해제 경로에서 재검토한다. 새 서비스/루트 재등록·중복 상태·불필요한 if/Tick·프레임별 검색/할당을 점검한다.
- [x] F4. 기존 효과/Activation 참조·몸체 텍스트·시연 사용법을 새 그룹 합산/무기한/보호막 활성 필터/쌍 복원/신규 카드/조립v3로 갱신한다. 과거 계획·인계의 미등록 지적을 현재 미구현으로 복사하지 않는다.
- [x] F5. §8의 실제 Core Play와 합성 시연, 검증 미실행·G1/G2·CVI-R1·아이콘/패널·미전달 요청을 구분해 인계한다. Core native 시험 기록의 StageKey2101 실패/성장 진단을 이번 View 성공/신규 회귀와 섞지 않는다.
- [x] F6. Phase별 파일·컴파일/정적 근거·잔여 조건·사용자 변경 보존·커밋/push 미수행을 기록하고 보고한다. Play/Profiler 미측정이면 성공·성능 개선을 주장하지 않는다.

## 8. 사용자 Play 인수 목록

아래는 아직 실행하지 않은 기대 결과다. 합성 프리셋과 실제 Core 명령을 각각 확인한다.

| 사례 | 기대 결과 / 방법 |
|---|---|
| 안전한 교환/undo | A(0,0)→(1,0)→B(2,0), A/B교환 후 A취소로 둘 다 복원. 승객 방향/행동·거리 유지, A만 이동 거리2 복원. 쌍 표시 중 중복 몸/일시 풀 반환/선택 이탈 없음 |
| 승객 행동·오래된 토큰 | 승객의 실제 행동 뒤 A취소 불가. 오래된 제출 거절은 두 몸 모두 낙관 이동/자동 재시도 없음. Core 실제 명령 확인 |
| 혼합 약점 노출 | Recon10+20+HuntersMark30→50 한 아이콘. 모든 출처 상세·서로 다른 수명 유지. 억제10이 추가돼 최대값이 그대로여도 대응 문구 한 번 |
| 양수 입력/보호막만 흡수 | HP 변화0이어도 Core가 선택한 이동 기여분 취소 불가. 낮아 억제된 출처는 다른 조건이 허용하면 취소 가능. 0입력/즉사/미리보기는 가짜 사용 확정 없음 |
| 다른 이동의 효과 사용 | A Ground/Dust를 B가 실제 사용한 후 B를취소해도 A잠금 유지. 무관한 행동은 다른 상태/체크포인트를 View에서 지우지 않음 |
| 선택적 역적용 | unused 혼합 교환 취소는 그 이동의 살아 있는 Recon·가속/흔적 기여만 제거. 별도 디버프·아이템·수신자 HP 유지. 대상이 먼저 귀환해 지운 기여분은 되살아나지 않음 |
| 보호막 이력 | 유효 예산0/비활성은 칩 없음. 출처/소비·초기화 이력은 유지, 재배치로 새 보호막 생성 없음. 정상 Core reset의 양수 예산은 칩 복귀 |
| 실제 예비 영역 귀환 | Returned→cleanup State→필요 장비 사건→최종 Buffer를 순서대로 표시. 임시 상태·가속/재생 진행 정리, 수동 장착템 재고1회 환불/소모템 미환불·밤 폐기 정책 유지. View의 추가 HP/회복/골드 연출 없음 |
| Field Main·독립 상태 | 밤에 Field Main은 예비 영역 정리를 하지 않음. 영구 Hunter/성장/기록, 다른 출처 예산/HP는 Core Final대로 유지 |
| nullable/무기한 수명 | timed는 대상 진영 잔여 횟수, untimed는 귀환/사망까지. 생산자 사망/귀환은 타 대상 효과 제거가 아님 |
| 카드·가용성 | 전투/덱/유닛 카드의 Recon 증가율·반경이 같은 Core 설정과 일치. 실제 존재하는 일반/강화만 검증. 미제공 설정은 임의 기본값 표시 없음 |
| 수명·회귀 | 결과 중 Cancel/Detach/재연결, 반복/Stop/Reset에서 위치·효과·환불·허용 횟수 중복 실행 없음. 가속 X N·CleanSweep·희생·끌어오기·추가 공격 Gate 유지 |

패널 미연결이면 상세 팝업 가시성은 후속이고, 기본 아이콘은 전용 아트 완료가 아니다. Profiler는 별도 실측이며 현재 계획으로 성능 성공을 주장하지 않는다.

## 9. 계획 작성 당시 기록

2026-10-31: 첨부 Core 인계와 현재 코드/기존 View 구현·구조 지침을 대조해 본 설계·계획을 작성했다. 문서 안의 Core 완료/시험 결과는 제공자의 인계 정보이며 이 작업의 실행 증거가 아니다. §8의 TraitState/발동 등록·revision 누락 지적은 현재 코드와 달라 재구현 Task에서 제외했다.

현재 G1은 미해제이며 B~E의 새 타입 소비를 대기로 표시했다(계획 작성 당시 기록; 병합 뒤 상태는 §10). A의 현 API Shield 필터와 기존 경로 보존은 구현 승인 후 독립 진행 가능하다. 계획 작성으로 구현/병합/컴파일·DB시험·Play가 완료된 것은 아니다. 이번 변경은 새 Markdown2개뿐이며 외부 문서를 복사/변경하지 않는다. 소스·SO·씬·프리팹·Core·인덱스와 현재 Play는 유지한다.

작성 검증: 새2문서의 UTF-8/LF·후행 공백/충돌 표지·파일/절 링크10개와 미착수 Task36개를 확인했다. G1/Core 병합과 G2/사건 대응의 해제 조건·적용 범위를 각각 표기했다. Git 인덱스는 작성 전과 동일하다. 착수 사본7,794파일 중 다른 작업의 Render EffectType/효과 에셋 변경과 새 Render 에셋 생성이 관측됐으며 덮어쓰거나 스테이지하지 않았다. 본 작업이 쓴 파일은 새 설계/계획2개뿐이다. 근거는 ignored `Temp/PositionSwapReconViewPlan/verification.json`이며 코드 컴파일·에디터/DB시험·Play는 실행하지 않았다.


## 10. 실행 기록 — 2026-10-31

### 10.1 승인·Core 인수·보존

사용자는 A~F 순차 구현, Core 병합 필요 부분 대기, 최종 코드 컴파일/정적 검증, Play는 사용자 별도 수행을 지시했다. 이어 “Core 랑 병합 진행했는데 확인 후 진행”으로 새 계약 인수를 요청했다. 착수 시 HEAD0403babef가2bd95ad34를 포함함을 확인했다. G1의 실제 타입/생성자·nullable lifetime·Movement/Participants/RemovedEffects·Owned Recon 설정이 제공됐으며 native 출력 소스를 확인해 G2도 해제했다. 새 Core 코드/설계/인계는 저장소에 들어왔고 최초 첨부를 예고 계약으로만 취급하던 대기는 끝났다.

기준 사본은 ignored `Temp/PositionSwapReconViewRun/baseline.json`(7,852파일)과 status/index 사본이다. 기존 FMOD·전투 Test/Akimbo·피격 설정·가속 사용자 값·배경 타일·Combat 씬을 덮어쓰지 않았다. 커밋·push·스테이징·외부 발송은 하지 않았다. 실제 Unity Play/Refresh/Test Runner를 제어하지 않았다.

### 10.2 Phase별 구현

| Phase | 실제 결과 |
|---|---|
| A | CombatUnitStatuses에서 Shield 유효 예산0 행만 활성 칩에서 제외. 다른 효과·canonical source/리셋 이력 그대로. 기존 UnitHUD의 key 재선택/hover 정리·설명 갱신 경로 재사용 |
| B | EffectiveIncreasePercent로 전환, 무기한 수명/null clock 문구 지원. source의 이동 기록 번호도 상세에 표시. HuntersMark 정책을 같은 특성 최대·다른 제공자 합산으로 수정. 전체 TraitState/HP/장비 사실과 최종 Buffer 순서 유지 |
| C | 위치 교환·정찰 부대 TraitVisual SO/meta2개와 기존 표 등록. Recon 설정을 전투/Owned 카드·덱 모델/UI 끝까지 전달. evidence는 해당 생산자의 출처 목록 SameValues 변경으로 전환, 순서 변경/제거를 새 부여로 보지 않음. Recon은 공용 WeaknessExposure 프로필, Source/Target/Origin/사건 참조 보존 |
| D | CombatPlacementBatchPlan이 실제 연속2사건의 참조를 불변 보관하고 private Session이 시작을 관리. 기존 MovementPresentation이 주체 도착 Final과 승객 Final을 예약해 ShownFacts가 한 번 Render. 승객 레인은 만들지 않고 원래 partner 사건만 완료. 쌍 Returned도 한 번 함께 배치. Cancel/Replace/Clear에 예약 제거 |
| E | 버전3, v1→v2→v3 사본 Import, old enum0~7 유지 후 FieldPlacement8 추가. 수명/optional Movement·명시 ActionUse/누적거리·한/두 필드 배치 입력과 Editor 진단. 공개 Undo/참여자/정산/Result 검증을 통과하는 단독 복원 묶음 구성. 신규 프리셋5개 저장, 기존15개 에셋/meta30파일 보존 |
| F | 최종 런타임/Editor/비Editor 코드 컴파일, 정적 보존/등록/직렬화/링크 대조, 구조 재검토와 사용법 갱신. 실제 에디터 검증/Play는 완료로 기록하지 않음 |

기존 양수 가속/공격 부여 근거는 각 reader가 양수 delta를 검사한다. 공통 registry의 일괄 Magnitude>0 전제를 제거해 Recon의 실제 부여 수치/출처 변경을 읽는다. Magnitude를 효과 합계 계산에 쓰지 않는다. multiple move의 새/갱신 출처는 기존 출처 전체와 SameValues로 비교하므로 SingleOrDefault 예외나 목록 순서만의 재출력이 없다.

순차 재생/이동 인터페이스·일반 경로는 변경하지 않았다. 새 배치 plan은 결과에서 파생하는 값이므로 기존 MovementPresentation의 결과 준비 경계에서 한 번 만든다. 별도 런타임 의존성/서비스 Factory를 추가하지 않았고 기존 Presenter/Factory의 소유 수명을 따른다. 합성 구성 확인에서도 같은 builder로 출력 대응을 검증한다.

### 10.3 시연 범위와 한계

신규 프리셋은 `Scenario_PositionSwapRestore`, `Scenario_ReconExposureGroups`, `Scenario_ReconMoveRemoval`, `Scenario_ShieldHistory`, `Scenario_ReserveCleanup`이다. 순서/기대값은 [시연 사용법 §12](../viewDocs/trait-activation-preview-guide.md#12-위치-교환정찰-부대-연동-v3)를 따른다.

FieldPlacement Returned는 해당 블록만 있는 단독 묶음이다. 공개 Result의 RemovedEffects가 비어 있는 안전한 배치/행동/거리 복원 형식을 사용하며, 효과 제거는 별도 State 사실로 작성한다. 실제 Core의 mixed receipt 역적용·사용 잠금·재고 환불·최대HP/밤 정리는 운영 사건으로만 인수한다. 도구에서 이런 규칙/영수증을 만들어내지 않는다. 운영 mixed undo는 D의 원래 전체 사실 소비로 지원한다.

Source ID는 편집 식별, optional Movement는 공개 기여분 식별이다. 초기/새 입력의 수명·식별을 명시하며 기존v2는 timed/Movement 없음, 행동Available/거리0의 기존 의미를 보존한다. 이전 과정은 원본 프리셋/ID/GUID를 저장하지 않는다. 신규복원은 지정하지 않은 HP·효과·장비를 현재 사실에서 보존한다.

### 10.4 검증 결과

| 확인 | 증거 / 상태 |
|---|---|
| 코드 컴파일 | Unity 제공 Roslyn·현재 Bee 응답 파일로 런타임/Editor/비Editor 정의 검증. 최종 세 빌드 모두 종료0. 로그 `Temp/StatusEffectView/psr-release-{runtime,editor,player}-csc.log` |
| 새 파일 | 새 C#2개와 meta. 임시 응답 파일에 포함해 컴파일하며 실제 Unity 임포트 성공과 구분 |
| 정적 대조 | UTF-8/LF/meta·기존 GUID/새 참조/활성 등록·기존 프리셋30파일·새5개 입력 ID/참조·enum 번호·전체 사실/출처 수명·보호막 조건·배치 소비/해제·링크/체크박스. `Temp/PositionSwapReconViewRun/verification.json` |
| 실제 에디터 임포트·재컴파일 | **F1 해당 부분 대기**. 현재 에디터/Play를 제어하지 않았으며 코드 컴파일 성공만으로 새 파일 임포트 성공을 주장하지 않음 |
| DataBaseAssetTests | **F2 실행 부분 대기**. 새 View SO/표 변경의 실제 에셋 검증은 사용자 후속. Core 시험의 기존 ShotGun 가정/StageKey2101·성장 진단은 별도 |
| Play·Profiler | 사용자 후속. 실제 교환 타이밍/선택·발동·수명/환불·누수·가독성·성능 성공은 미확인 |

구조 재검토: 효과 합계/사용 확정/역적용·최신성은 Core/Buffer가 원본이고 활성 슬롯/상세는 기존 표현 변환이다. 작은 profile/evidence·카드 변환·placement plan/현재 session·기존 pool을 분리(S), 등록/블록 데이터로 확장(O), 기존 완료/Cancel/Release와 단일 Returned 유지(L), 필요한 값/Query만 전달(I), 기존 Factory/포트와 주입 수명 유지(D). 새 IServiceModule/Export/루트 재등록/Builder/UIId는 없다. private 예약은 해당 도착에서 제거하고 Cancel/Replace/Clear에서도 비우며 매 프레임 새 조회/효과 시계는 없다. Group/Query→CombatDisplayTables→기존 Factory/Presenter/UI와 입력 Tracker→UndoInput→Gate→Flow/Job 경계를 유지했다. 공통 이동 완료 경계의 회귀는 §8에 남겼다.

### 10.5 잔여 조건

G1/Core 병합 및 G2/사건 대응 대기는 해제했다. 기존 CVI-R1(전투 밖 재생/위압 설정)은 현재 Owned DTO에도 없어 대기 유지다. Recon 설정 전달은 완료했다. 깨진 방패·신규 특성 아이콘 아트, Description Panel/Text와 실제 Play/에디터 인수는 별도다. 신규 특성을 운영 유닛/획득 풀에 장착/등록하지 않았다. 설계 범위 밖 Core·필드 카메라·공유 HUD·프레임워크·옛 Missing Script 에셋은 수정하지 않았다.


최종 정적 결과: 기존 변경 소스23개+신규2개, 보호 영역3,014파일 및 기존 프리셋30파일 동일, 새 TraitVisual2행·프리셋5개·meta/GUID/입력 참조 정상. 문서 링크52개·Task34개 완료/F1·F2 실제 에디터 검증 부분 대기·Git 인덱스 유지 확인. 변경 범위의 diff --check 통과. 런타임/Editor/비Editor 정의의 최종 코드 컴파일은 모두 종료0이며 native Editor/DB시험/Play 통과로 해석하지 않는다.
