# Core 버프·디버프 View 연동 Task 계획

설계 원본: [Core 버프·디버프 View 연동 설계](../specs/2026-10-31-status-effect-view-integration-design.md).

상태: **Phase A~E 및 F의 코드·정적 검증/문서 완료. D6(Core 응답), F1(실제 에디터 컴파일·임포트), F2의 기존 DataBaseAssetTests 실행은 대기**. 사용자 요청으로 A~F를 순차 구현했다. 몸체 텍스트의 이전 A~J와 다른 계획이다. 현재 Play를 유지하라는 후속 지시에 따라 Unity 입력·Stop·Refresh·Test Runner를 실행하지 않았다.

기준: Core `a0ec23c0e`, 병합 커밋 `0a4d3303c`와 현재 작업 트리. 새 구현 전에 git 상태·동시 작업을 다시 확인한다. 커밋·push는 사용자 담당이며 새 View 테스트·자동 검증 메뉴는 만들지 않는다. 단일 Phase를 진행하면 끝에 보고·멈춤, 명시적인 일괄 진행 요청이면 승인된 Phase를 순차 진행하고 각 끝에 보고한다.

## 1. 순서와 외부 의존성

```text
A 전체 사실/순서 경계 확인
 → B 효과 키 View DB·formatter와 에셋 이전
 → C HUD·출처 상세·몸체 피드백 연결
 → D 카드·설명·Retired 정리
 → E 조립 시연 확장·프리셋
 → F 컴파일·정적 검증·사용자 인계
```

- B는 입력/Data/Query/기존 9 SO·테이블/formatter 소비자를 같은 Phase에서 전환해 중간 단계의 키·시그니처 불일치를 남기지 않는다. C는 실제 입력 수명·피드백 대응을 마무리한다.
- D의 **전투 밖 재생/위압 정량 설명**은 Core CVI-R1 응답 뒤에만 구현/완료할 수 있다. 답이 없으면 해당 Task만 대기로 두고 E/F 및 나머지 D를 진행한다. View의 InGame 조회·수치 기본값으로 메우지 않는다.
- 깨진 방패 Sprite 확정은 아트 인수 의존성이다. 기존 선택적 fallback으로 동작 확인과 전용 아트 완료를 구분한다.
- Description Panel/Text 생성·연결, Core/FieldCamera/Architecture/Framework 기반 수정, Missing Script 옛 에셋 삭제, 무관한 스테이지/성장 데이터 수리는 범위 밖이다.
- Play·가독성·Profiler는 사용자가 확인한다. 코드 검증은 컴파일/정적 대조이며, DB 에셋 변경에는 AGENTS의 기존 DataBaseAssetTests 실행도 포함한다. 새 View 테스트는 만들지 않는다. 사용자가 후속 실행 범위를 더 제한하면 그 제한을 따르고 미실행을 기록한다.

## 2. 파일과 책임

경로는 `Assets/ProjectL/Script/Systems/` 기준이다. 파일 책임 분할은 허용하되 아래 의미·타이밍·소유권을 바꾸는 결정은 먼저 확인한다. 새/이동 `.cs`·에셋은 meta를 함께 다루고 기존 GUID를 유지한다.

| 영역 | 대상/역할 |
|---|---|
| `DataBaseSystem/Data/View/Table/UnitStatus/` | effectKey 입력/불변 Data·요약 틀, 옛 UnitStatusKind 정리 |
| `DataBaseSystem/Queries/ViewDataBase/ViewTable/UnitStatusVisualTable.cs` | StatusEffectKey 조회·키/구성 검증. 기존 Query/Export 유지 |
| `Factory/DBFactory/Group/ViewDataBaseFactory`, `ServiceKeys/DBFactory/Group/ViewTableKey`, `.../ViewDataBase.cs` | 기존 생성/등록/보관/소비 추적. 불필요한 새 등록·상위 재노출 없음 확인 |
| `Combat/Display/Unit/CombatUnitStatuses.cs` | 같은 사실의 Core Capture → 효과 목록. 레거시 합계/종류별 규칙 제거 |
| `Combat/Display/Unit/CombatEffectFormatters.cs`, `CombatStatusDescriptionFormatter.cs` | 효과별 typed 값·공통 출처/수명 설명. 새 내부 협력 객체 |
| `Factory/Runtime/InGame/View/CombatUnitStatusFactory.cs`, `CombatDisplayTables` | 불변 formatter 등록·필요한 조회 주입. 기존 Scene Factory/시연 공통 경로 |
| `Combat/Playback/Ordering/CombatEventTypeTable`, `Handlers/CombatFactEventHandler`, `Support/CombatShownFacts` | 이미 구현된 전체 사실 경로 보존·기하/HP 경계 검토. 필요한 좁은 보정만 |
| `Combat/Playback/Support/CombatTraitStateEvidence`, `CombatTraitFeedbackProfiles`, `CombatTraitFeedbackFactory` | canonical 근거·약점 노출 등록. Plan/Session·텍스트 풀 유지 |
| `GlobalSystem/UISystem/Screens/Stage/UnitHUD`, `Combat/Display/Unit/FieldUnitPresenter` | 기존 슬롯·키별 상세·고정/hover 수명 검토. 레이아웃/고정 아이템 유지 |
| `GlobalSystem/UISystem/Screens/Global/UnitCardTraitContent`, `UnitCardUI`, `.../Screens/Stage/DeckEditUI` | 같은 Snapshot의 설정 표시와 공통 카드 변환 |
| `ViewSystem/StageMap/Deck/DeckEditDisplayModel` 및 모델 생산자 | Core 응답 후 두 설정 DTO 전달. 새 규칙 계산 없음 |
| `Dev/TraitPreview/Scenario/`·`Editor/CombatPreview/`·해당 Factory | canonical 명시 입력·버전 이전·조립 진단·프리셋 |
| `DataBaseSO/ViewGroup/UnitStatusVisual`·`TraitVisual` | 효과명/키/설명·테이블 이전, 약점 노출 연출 줄, Retired 활성 등록 제외 |
| `docs/superpowers/viewDocs/` | 효과·특성·Activation/시연의 지원 범위와 사용법 갱신 |

## 3. Phase A — 현재 전체 사실 재생과 보존 경계

- [x] A1. 실제 구현 착수 시 git/사용자 변경·기존 body-text 프리팹/에셋·Core 인수 커밋·기존 DataBaseAssetTests의 ShotGun 입력 가정·AGENTS를 재확인하고 보존 기준을 기록한다.
- [x] A2. TraitState의 Facts/Subject/Origin 등록, ApplyCharacter(Final), Render→AfterFacts가 이미 있다는 것을 코드로 대조한다. 중복 처리기·새 상태 저장소·새 이벤트 스트림을 만들지 않는다.
- [x] A3. Expected/Final의 상태·HP·기하·Stats·아이템·행동·경험치가 통째로 유지되는 소비 경계를 확인한다. 최신 Buffer 선반영·디버프 일부 복원으로 바꾸지 않는다. optional 유효 능력치 쌍을 불필요하게 저장하지 않는다.
- [x] A4. 끌어오기 도착 후 TraitState/HP 사건, root/child 공격·희생·보호막 순서를 대조한다. 기존 도착 보정이 필요한 곳은 표시 기하만 보존하고 원래 상태 사건 객체 식별을 유지하도록 좁게 연결한다. 사실 계약 자체가 불명확하면 Core 요청으로 남긴다.
- [x] A5. 옛 CombatHuntersMarkEvent/ID39를 읽기 호환으로 한정하고 신규 약점 노출 생성과 혼동하는 주석/신규 사용을 정리한다. Core 보존 타입을 삭제하지 않는다. Hunter 지정 대상 Mark·보상 경로는 유지한다.
- [x] A6. 컴파일·정적 사건 순서/호출 경계를 확인하고 실제 실행 여부와 구분해 보고한다.

리뷰 지점: 이미 있는 것/변경한 것 구분, 전체 사실 원본과 표시 기하, 이벤트 식별·피드백 경계. Phase 완료 시 보고한다.

## 4. Phase B — 효과 키 View DB·표시 변환·에셋 이전

- [x] B1. 활성/비등록 상태 SO·GUID·Sprite·설명 틀·모든 UnitStatusKind/Polarity 소비를 조사한다. 단순 enum 캐스팅 없이 설계 §4~5의 명시 이전 표를 확정한다.
- [x] B2. 기존 SO/Data/Query를 StatusEffectKey로 바꾸고 분류 중복 입력을 제거한다. 기존 테이블/서비스 키/Export를 재사용한다. Key 검증·중복/누락·설명 값 수 진단을 유지한다.
- [x] B3. typed Snapshot→요약 값의 좁은 formatter와 공통 출처/수명 formatter를 나눈다. registry에 Key/형식/값 수를 함께 등록하고 중복/누락을 조립 전에 거절한다.
- [x] B4. 기존 대표 7 SO를 효과명으로 이동하며 GUID를 유지하고, DefenseIncrease 1 SO/meta를 추가한다. Acceleration/ArmorBreak는 새 필드로 이전하되 테이블에서 빼고 삭제하지 않는다. 활성 행은 8개다.
- [x] B5. 요약 설명을 새 의미로 고친다. AttackIncrease는 총량/부여/Held, DefenseDecrease는 Core 유효 방어와 출처별 감소율, Shield는 유효 예산/비활성 상세를 분리한다. 옛 소모 표식·가속 별도 행을 남기지 않는다.
- [x] B6. 새 formatter Factory를 CombatDisplayTables의 기존 조립에 연결하고 CombatUnitStatuses가 같은 사실의 Core Capture를 사용하도록 전환한다. 운영/시연 공용 FieldUnitPresenter API를 유지한다.
- [x] B7. 새 Query 서명·소비·에셋 등록을 같은 Phase에서 컴파일 가능하게 전환한 뒤 옛 enum과 남은 생산 참조를 정리한다. 구 필드의 영구 fallback·별도 mirror enum/테이블을 만들지 않는다.
- [x] B8. 런타임/Editor 컴파일, meta/외부 참조·GUID·DB 등록·틀 인자 수를 정적 검증한다. 깨진 방패 Sprite 확정/연결 여부를 별도 기록하고 미제공을 아트 완료로 체크하지 않는다.

리뷰 지점: 13개 키/8개 제공 행의 차이, 서비스 생성→Export→조회 추적, 실제 SO 이전 표·기본 아이콘 한계. Phase 완료 시 보고한다.

## 5. Phase C — HUD·상세 수명·발동 피드백 연결

- [x] C1. 효과 키당 아이콘 하나, 버프→디버프/효과 키 순서, 안정된 `StatusEffect:<key>` 식별을 연결한다. 출처·시간 변경으로 슬롯이 증식하지 않게 한다. 아이템/카테고리/보유 특성 제외 정책은 유지한다.
- [x] C2. 출처별 typed 수치·원본 순서·nullable Origin·Eligible·서로 다른 LifetimeKind를 설명에 표시한다. 남은 턴 최대/최소 합치기, 보호막 Reset Count를 잔여 턴으로 표시하기, View 재집계를 하지 않는다.
- [x] C3. 동일 효과 갱신 시 열린/고정 설명 갱신, 소멸·Clear·비활성·풀 반환 시 설명/hover 정리를 확인한다. 기존 Description Panel/Text가 비어 있는 선택 상태는 유지한다. 공유 HUD 프리팹/새 팝업은 만들지 않는다.
- [x] C4. CleanSweep 근거를 canonical AttackIncreaseStatus의 해당 출처 변경으로 옮긴다. 가속 Held/실제 이동 N, 희생 Start·복귀, 텍스트 프리팹/풀·generation·공격 Gate를 보존한다.
- [x] C5. WeaknessExposure의 추가/값·수명 갱신 근거를 등록하고 동일 공격/대상/부여 출처로 대응한다. Magnitude=1을 증가율로 취급하지 않으며, 최대값 불변·약한 값/짧은 수명 갱신도 다룬다.
- [x] C6. HuntersMark/StatusApplied SO 줄을 StateApplied·Targets·Count None·기본 특성 이름/공통 텍스트 프리팹으로 연결한다. 기존 파티클/Shake/공격 덮어쓰기·행동은 보존하고 임의 연출을 추가하지 않는다.
- [x] C7. 재생 상태 진행과 실제 HealthRecovered/HP 사건을 구분한다. HUD 갱신·취소·재바인딩이 새 회복/특성 텍스트를 만들지 않는 경계를 확인한다.
- [x] C8. 컴파일·등록/대상/근거/회수 정적 대조와 사용자 수동 기대 결과를 보고한다.

리뷰 지점: 2출처 1아이콘·fallback/갱신, 상세 수명, 원본/적용 효과 이름 분리, 기존 세 특성 회귀. Phase 완료 시 보고한다.

## 6. Phase D — 카드·특성 설명·퇴역 등록 정리

- [x] D1. TraitVisual_HuntersMark/Intimidation/Regeneration의 정책 문구를 새 Core에 맞춘다. 표식 소모/다른 아군 한정/무기한, 위압 우선도, 모든 캐릭터 행동 종료, 숫자 `n` 문구를 정리한다. SO는 정책 문구, Snapshot은 설정 수치로 분리한다.
- [x] D2. UnitCardTraitContent의 옛 표식 정책 문자열을 제거하고 같은 Core 설정을 덧붙이는 공통 변환을 만든다. 전투 상태 Snapshot의 재생 회복량/주기·위압 감소율을 소비하되 실제 HP/잔여 수명으로 오인하지 않는다.
- [x] D3. 현재 제공된 HuntersMarkDescription의 증가율/기간을 UnitCardUI·DeckEditCardModel·DeckEditModelBuilder·DeckEditUI·전투 정보 경로 끝까지 전달/소비한다. 일반/강화 값과 현재 대상 잔여 수명을 구분한다.
- [x] D4. MoveAttackDiscount 활성 TraitVisual 등록을 빼되 SO/GUID와 역사적 식별은 보존한다. Core Availability의 Retired/Deferred를 구분해 표시하고 DeferredPriorityComponent의 사라진 사용을 정리한다. 일반 Priority·아이템 효과는 유지한다. 전투 밖 카드에는 같은 Owned 조회의 TraitAvailability도 함께 전달하고, TraitGrowthPopupModelBuilder는 이미 받은 Core choice.Description/후보를 유지해 View의 별도 퇴역 판정을 만들지 않는다.
- [x] D5. 키만 있는 개발 표시와 실제 정량 보장 경로를 구분한다. 후자에서 필수 설정 누락은 진단하고 InGame DB/임의 기본값/다른 시점 유닛 사실로 채우지 않는다.
- [ ] D6. **Core 응답 의존:** CVI-R1이 제공한 전투 밖 Regeneration/Intimidation 설정을 같은 Owned 조회의 카드 모델과 공통 변환에 연결한다. Core 파일은 수정하지 않는다. 미제공이면 이 Task와 해당 정량/강화 인수를 대기로 기록한다.
- [x] D7. 컴파일·폐기된 문구/참조·활성 테이블·호출 경로를 대조하고 D6 상태를 숨기지 않고 보고한다.

리뷰 지점: 정책/설정/실제 상태 세 가지 원본, 모든 카드 경로, D6 외부 의존성. D6 미해결만으로 E/F를 막지 않는다.

## 7. Phase E — canonical 조립 입력과 시연 프리셋

- [x] E1. 기존 State enum 값·프리셋 버전/ID/GUID·입력 보존 사본을 기록한다. 기존 값은 유지하고 새 payload를 뒤에 추가하는 v2/명시 Import 이전을 설계대로 연결한다. SerializeReference는 추가하지 않는다.
- [x] E2. 8종 효과의 typed 입력·출처 Source ID·명시 Set/Remove를 기존 writer 등록에 추가한다. 필요한 필드만 표시하고 동일 Source ID의 대상/키/출처 무단 변경과 유효하지 않은 수치/수명을 구체적으로 진단한다.
- [x] E3. 기존 AttackGrant를 canonical 상태로 이전하고 다른 효과/HP/기하를 보존한다. Held 가속과 재생 진행은 trait status로, DefenseIncrease는 명시적인 CohesionDefenseBonus 입력으로 유지한다. 시연의 TraitAttackBonus 갱신도 이미 공개된 Core 읽기 API로 받아 레거시 합계 수식을 복제하지 않는다. 입력한 사실 외 자동 효과/턴/피해/회복 계산은 하지 않는다.
- [x] E4. 기존 Packet State가 전체 CombatTraitStateEvent를 만들고 운영과 같은 Capture/formatter/피드백 경로를 사용하는지 확인한다. StateApplied 단독 Moment 제한과 같은 Packet 순서를 유지한다.
- [x] E5. 약점 노출 2출처/fallback/소멸/약한 갱신, 일반 부여+가속, 보호막 예산/활성, 제한/방어 공존, 재생 진행/실제 HP 회복, Hand/EnemyWaiting 제거를 **조립 블록 데이터**로 저장한다. 자동 턴·귀환 정리 규칙을 구현하지 않는다.
- [x] E6. 기존 가속/CleanSweep/희생 생존·사망/ManualStep의 사건·시간·GUID를 비교한다. 구형 표식·퇴역 SO의 활성 선택은 등록/사전 진단에서 구분한다.
- [x] E7. 런타임/Editor 컴파일·입력 복사/직렬화·공용 경로·사전 진단을 정적으로 확인한다. 실제 프리셋 Play는 사용자 후속으로 넘긴다.

리뷰 지점: 조립식 유지, 합성 사실/실제 Core 규칙 검증 구분, 이전 결과·대표 사용 순서. Phase 완료 시 보고한다.

## 8. Phase F — 최종 검증·구조 재검토·인계

- [ ] F1. 실제 Unity 런타임/Editor 컴파일과 생성된 컴파일 목록을 확인한다. MSBuild 임시 소스 포함 성공만으로 Unity 새 파일 임포트 성공을 주장하지 않는다. 이 프로젝트의 새 Pool/View 등록 누락 이력을 반영한다.
- [ ] F2. 변경 소스 UTF-8/LF/meta·GUID·이전 필드·충돌 표지·등록·직렬화·참조·틀 값 수·중복 키를 대조한다. DB 에셋 변경에 대해 기존 DataBaseAssetTests를 실행하고 결과를 기록한다. 현재 그 시험은 ShotGun=HuntersMark30%/1을 가정하지만 병합에서 유지한 실제 ShotGun은 Sacrifice라서 입력 가정이 다름을 사전 확인했다. 기존 입력 불일치와 새 View DB 오류를 구분하고, 통과를 위해 Core 시험/운영 특성을 임의 변경하지 않는다. 실행하지 못한 시험은 성공으로 기록하지 않는다.
- [x] F3. SSOT/SOLID5/JMH/FW/캡슐화/기능 독립성의 실제 생성→등록/Export→조회→UI 소비/해제 경로를 기록한다. 분류/합계/타이머 중복, 의미 없는 조건문·Tick·매 프레임 조회/할당을 점검한다.
- [x] F4. trait-effects-and-activation-reference, trait-activation-preview-guide, trait-body-feedback-guide의 현재 지원/시점·효과 키·합성 한계·정량 설정을 갱신한다. 옛 특성 수를 새 사실로 반복하지 않는다.
- [x] F5. 아래 Play 목록과 CVI-R1·깨진 방패·보류 설명 패널을 구분해 인계한다. 정적/컴파일 성공과 실제 동작·Profiler 미확인을 분리하고 기존 사용자 변경/타 작업 보존을 확인한다.
- [x] F6. Phase별 결과·변경 파일·잔여 조건·미전달 문서를 기록해 사용자에게 보고한다. 커밋·push하지 않는다. Core 응답/아트/Play가 없는데 전체 인수를 완료로 기록하지 않는다.

## 9. 사용자 Play 인수 목록

아래는 후속 기대 결과이며 실행 성공 기록이 아니다. 조립 시연과 실제 인게임 Core 발동을 각각 확인한다.

| 사례 | 기대 결과 |
|---|---|
| 약점 노출 최초 적중/다음 적중 | 첫 적중 뒤 HUD·대상 문구. 다음 피해는 Core가 확정한 값으로 표시. View의 추가 배율 없음 |
| 20%/2 + 30%/1 | 아이콘 하나·유효30·출처 두 줄. 다음 대상 진영 끝에20/1, 그다음 소멸. 다른 진영 끝에는 유지 |
| 동일 출처의 약한/짧은 갱신 | 새 공개 값으로 설명 갱신, 출처/아이콘 증식 없음. 상태 변경 없는 동일 재부여는 중복 문구 없음 |
| 출처 사망/이탈·대상 귀환 | 출처 제거로 수신 효과가 사라지지 않음. 대상 Hand/EnemyWaiting 귀환·재배치는 Core 최종 사실대로 표시 |
| 가속+일반 공격력 부여 | AttackIncrease 한 아이콘·Held/기간 별도 상세. 가속2칸/증가10은 몸체 `가속 X 2` |
| 보호막 | Core 유효 예산과 개별 예산/비활성 구분. Reset Count를 잔여 턴으로 쓰지 않음 |
| 공격 감소·Disarm·Bind·방어 감소 | 효과 공존·각 수명 표시, 다음 전체 State/HP 사건에서 다른 효과가 빠지지 않음 |
| 재생 주기2 | 진영 종료마다 Core 진행값 반영. 회복 시 실제 cap 양만 HP/발동에 반영. 만피 회복 기회에 가짜 HP/텍스트 없음 |
| 치명적 종료/마지막 밤·Main | 치명 피해 뒤 회복 없음. Core가 준 밤 회복→회수 순서·Main 유지 상태 보존 |
| 희생·끌어오기·연쇄 공격 | 올바른 실제 수신자 효과·HP, 도착/복귀 기하 보존. 희생 시작 텍스트와 기존 행동 정상 |
| 카드 일반/강화 | 표식 기간/증가율, 위압 공격 감소, 재생 주기/회복량. 전투 밖 후자 둘은 CVI-R1 제공 후 인수 |
| 퇴역/별도 Hunter | 비용 할인 신규 노출/실행 없음. Hunter의 지정 Mark·영구 보상 유지 |
| HUD 수명 | 동일 효과 상세 갱신·고정 유지, 소멸 시 정리. 고정 아이템/카테고리·버프→디버프 배치 유지 |
| Stop/Reset/반복/재바인딩 | 효과 시계·회복·발동의 추가 실행 없음. 기존 텍스트/풀/구독 회수 |

설명 패널이 미연결이면 상세 텍스트 가시성은 보류다. 깨진 방패 자원이 미확정이면 기본 아이콘으로 동작 확인한 것과 전용 아트 완료를 구분한다. 측정하지 않은 성능 개선을 주장하지 않는다.

## 10. 작성 기록

2026-10-31: 현재 코드·새 Core DTO·인계·기존 HUD/몸체 피드백/조립 도구를 읽어 설계와 본 계획을 작성했다. 이미 등록된 TraitState/전체 사실/Render 후 통지를 확인했고, Core 인계의 해당 과거 View 지적은 신규 구현 항목에서 제외했다. Magnitude=1·현재 8종 Capture·전투 밖 설정 공백·기존 설명 패널 보류를 계획에 반영했다.

**계획 작성 당시 기록:** 이번 변경은 새 설계/계획과 Core 요청 문서 추가뿐이다. 코드·SO·프리팹·Core·테스트·Git 인덱스를 수정하지 않았으며 Unity 컴파일/Play도 실행하지 않았다. 이전 병합 컴파일 성공을 이번 계획의 구현 검증으로 재사용하지 않는다. 모든 Phase 체크박스는 미착수다.


작성 검증: 새 두 문서의 UTF-8/LF·로컬 링크/절 링크를 확인했고 42개 Task가 모두 미착수임을 대조했다. 소스 변경/새 구현 파일/인덱스 변경은 없다. 조사 기준 5,740개 파일 중 5,738개는 동일했으며, 작성 도중 별도 변경된 CombatCameraSettings_Default·StageFieldView_Default 두 에셋은 건드리지 않고 보존했다. 해당 카메라/필드 설정 변경을 이번 설계의 구현 결과로 집계하지 않는다.


## 11. 구현·검증 기록 — 2026-10-31

### 11.1 범위·검증 조건

- 승인: “계획대로 Phase A~F 순차적으로 구현하고, Core 응답이 필요한 부분은 대기로”. 각 Phase 결과를 보고하며 진행했다. 새 View 테스트·검증 메뉴·커밋·push·스테이징은 하지 않았다.
- 최종 사용자 지시: **“현재 Play 유지 — 에디터 검증은 대기로 기록”**. 따라서 실제 에디터 재컴파일/임포트와 기존 DataBaseAssetTests는 미실행이다. Play/가독성/Profiler도 사용자 후속이다.
- 기준 사본: `Temp/StatusEffectView/baseline.json`(7,711파일), `before/`, `status-before.txt`. 기존 몸체 텍스트·폰트·프리팹·카메라/필드·운영 유닛 변경을 보존했다. 이 기록은 ignored Temp에 있으며 배포/커밋 대상이 아니다.

### 11.2 Phase A — 전체 사실

`CombatShownFacts.ApplyTraitState`가 원래 Final 전체를 반영한다. 끌어오기 도착 증거가 남은 Field 유닛만 이미 보인 기하를 WithPlacement로 보정한다. 사건 객체는 바꾸지 않아 피드백 Plan의 식별을 유지한다. 비Field에서는 도착 증거를 해제한다. HP/Stats/상태/아이템/행동/경험치는 Final을 유지한다. 기존 Facts→Render→AfterFacts·ID39 읽기 호환을 유지하며 생산자를 추가하지 않았다. A 런타임 컴파일 통과.

### 11.3 Phase B — 효과 DB·변환

기존 UnitStatusVisual 표/서비스를 StatusEffectKey 조회로 전환하고 분류는 Core Catalog에서 받는다. 옛 UnitStatusKind/UnitStatusPolarity 코드와 meta를 제거했다. `CombatUnitStatuses`는 현재 표시 사실의 Core Capture를 사용한다. 새 EffectFormatter/공통 출처 설명/Factory는 내부 협력 객체이며 서비스가 아니다.

| 기존 SO | 현재 SO / 키 | 보존 |
|---|---|---|
| AttackBonus | AttackIncrease / 1 | GUID·아이콘 |
| Intimidation | AttackDecrease / 2 | GUID·아이콘 |
| DefenseReduction | DefenseDecrease / 4 | GUID·아이콘 |
| HuntersMark | WeaknessExposure / 13 | GUID·아이콘 |
| AnkleBreaker | Bind / 11 | GUID·아이콘 |
| Disarm, Shield | 동일 이름 / 10, 7 | GUID·아이콘 |
| 없음 | DefenseIncrease / 3 | 새 SO/meta |

대표 7개를 재사용했으며 실제 파일명 이동은 5개다. Acceleration/ArmorBreak는 새 스키마로 이전하고 활성 표에서만 제외했다. 활성 8행이 필수 formatter와 대응한다. 요약 틀 인자 수·키 중복·누락·typed Snapshot 불일치를 조립 때 거절한다. 깨진 방패 Sprite는 미확정이며 전용 아트 완료가 아니다. B 런타임/Editor 코드 컴파일 통과.

### 11.4 Phase C — HUD·피드백

키 `StatusEffect:<숫자>`로 효과당 슬롯 하나, Core 분류/순서를 그대로 소비한다. 공통 상세는 공개 출처/수명/Eligible/과거 Reset 정보를 구분하고 RuleKey를 제품 문구에 노출하지 않는다. 기존 UnitHUD의 동일 키 설명 갱신·고정/hover 해제·Clear/반환 경로를 코드로 대조했다. 공유 HUD 프리팹은 수정하지 않았다.

CleanSweep 근거를 canonical AttackIncreaseStatus의 해당 출처로 전환했다. 약점 노출의 출처 추가/값/수명 변경을 근거로 등록했으며 Magnitude=1과 증가율을 비교하지 않는다. 최대값이 그대로인 변경도 허용한다. HuntersMark/StatusApplied 줄에 StateApplied·Targets·Count None·공통 프리팹 몸체 텍스트만 추가했다. 기존 가속/깔끔한 뒷정리/희생의 행동·텍스트 자원 수명과 회복 사건 경로를 유지했다. C 컴파일 통과; 실제 Gate/화면 회귀는 §9 대기.

### 11.5 Phase D — 카드

세 특성의 정책 문구를 SO에 두고 공통 카드 변환이 같은 조회의 설정 수치를 덧붙인다. HuntersMark 증가율/기간과 Availability를 UnitCard/Deck 모델/Deck UI로 전달했다. 전투의 위압/재생은 같은 TraitStatuses를 사용한다. MoveAttackDiscount 활성 외형 등록만 제거하고 SO/GUID는 유지한다. 성장 후보/설명은 기존 Core choice를 유지한다.

**D6 대기:** Owned 조회에는 아직 Regeneration/Intimidation 설정 DTO가 없다. §55 CVI-R1은 초안·미전달이며 Core 구현/기본값/운영 SO를 대신 수정하지 않았다. 전투 밖 두 특성의 정량·강화 인수만 대기한다. D 코드 컴파일 통과.

### 11.6 Phase E — 조립 입력 v2

기존 v1 프리셋 10개(에셋/meta 총20파일)는 byte 단위로 보존했다. v1을 사본에서 v2로 명시 이전하며 기존 유닛/묶음/블록/부여 ID와 GUID는 유지한다. AttackGrant 필드는 이전 전용이고 새 작성은 Effect다. 기존 State enum 0/1을 보존하고 Effect2/Cohesion3/Regeneration4를 추가했다. SerializeReference는 없다.

설계 §9의 “기존 Health/Return”은 실제 도구에 독립 입력이 없었다. 계획된 명시 HP/귀환 예제를 위해 Block enum의 기존0~5를 보존하고 Health6/Return7 입력을 추가했다. HP/귀환 사실만 만들며 만료·회복·귀환 정리 규칙은 추가하지 않는다. EnemyWaiting에는 손패 슬롯을 넣지 않는다.

안정 Source ID는 대상/키/Origin 변경을 거절한다. typed Set/Remove는 무관한 상태를 보존하고 유효 공격력 투영은 공개 Core API를 사용한다. DefenseIncrease는 CohesionDefenseBonus, 가속은 Held, 재생은 진행 설정 입력이다. 임의 출처/턴을 만들지 않는다. 활성 TraitVisual 입력 등록에서 선택·사전 진단하며 연결된 Host의 Catalog를 우선한다. 퇴역 에셋과 기존 DoubleShot 프리셋은 보존하지만 등록되지 않은 외형 재생은 진단한다.

새 v2 프리셋 5개: WeaknessExposure, WeaknessExposure_Refresh, EffectSources, RegenerationCadence, EffectReturns. 자세한 조립 방법은 [사용 안내](../viewDocs/trait-activation-preview-guide.md)를 따른다. E 런타임/Editor 컴파일 통과. 실제 Unity 직렬화 임포트·Play는 미실행이다.

### 11.7 Phase F — 결과와 구조 검토

| 확인 | 결과 / 증거 |
|---|---|
| Unity 제공 Roslyn·현재 Bee 응답 파일 기반 코드 컴파일 | 런타임·Editor·비Editor 정의 모두 종료0. `Temp/StatusEffectView/f-final-{runtime,editor,player}-csc.log` |
| 새 파일 컴파일 포함 | 런타임 새5개와 Editor 소비자를 응답 파일에 추가해 컴파일. 실제 Unity 임포트/생성 목록 갱신 성공을 뜻하지 않음 |
| MSBuild | 초기 시도는 출력 없이 중단. 성공 근거로 사용하지 않음 |
| 정적 대조 | 변경 소스33개/신규5개, UTF-8/LF/meta/SerializeReference 없음, 효과8행·대표GUID7개·기존프리셋20파일·신규5프리셋 식별/참조 확인. `Temp/StatusEffectView/static-results.json` |
| 소유권 | 기준 사본의 Core/Framework/FieldCamera/Architecture/공유 CombatOnly UI 2,131파일 동일. Git 인덱스 그대로. 작업 중 별도 변경된 CombatCameraSettings_Default는 덮어쓰지 않고 보존. 옛 Missing Script48 에셋 삭제 없음 |
| 실제 Unity 컴파일·에셋 임포트 / DataBaseAssetTests | **사용자 지시로 대기**. 시험의 기존 ShotGun 가정 차이는 F2에 기록; 실행 결과 없음 |
| Play·Profiler | 사용자 후속, 통과/성능 개선 주장 없음 |

SSOT: 합계·분류·유효값·수명은 Core Capture, 외형/요약은 기존 View 표, 정책은 TraitVisual, 시점은 ShownFacts다. View 만료 시계/최대 출처 선정/별도 효과 분류 enum은 없다.

SOLID: 사실 반영/typed 요약/공통 상세/DB 조회/조립 입력을 분리(S), 새 효과는 formatter+등록+외형으로 확장(O), 기존 사건/완료·Cancel·Release·UI 재사용 계약 보존(L), 내부 formatter와 읽기 Query로 의존 축소(I), Factory에서 구성하고 소비자는 주입(D). 내부 registry는 private이며 외부 변경 API가 없다.

JMH/FW: 기존 `ViewDataBaseFactory.Create → UnitStatusVisualTable.ExportServices → ViewTableKey.UnitStatusVisual → IViewQuery → CombatDisplayTables → CombatUnitStatusFactory → FieldUnitPresenter/UnitHUD`를 유지했다. 새 IServiceModule/루트 재등록/Builder 재Build/AddModule은 없다. UIId·UIGroup·Widget/Button 입력·풀 반환 경로를 우회하지 않았다. Bus/Mesh/JobQueue는 수정하지 않았으며 해당 실행 계약 검증을 주장하지 않는다.

독립성/if/Tick: 아이콘·요약 수정은 해당 효과 SO, typed 표현은 formatter, 새 의존성은 Factory에 한정된다. 이동/카메라/공격/풀을 불필요하게 확장하지 않았다. 기존 사실 갱신 때만 목록을 만들고 새 Update/Tick/Buffer polling은 없다. nullable Origin/선택 아이콘/미등록/취소 검사는 실제 입력·수명 경계다. 성능/할당량 실측은 하지 않았다.

후속: 사용자가 Play 종료 후 실제 컴파일/임포트 및 기존 DB 시험, §9 Play 목록을 확인한다. CVI-R1 응답, 깨진 방패 아트, 미연결 Description Panel/Text는 각각 별도 대기다. Core 요청 문서는 작성만 했으며 외부 전송하지 않았다.
