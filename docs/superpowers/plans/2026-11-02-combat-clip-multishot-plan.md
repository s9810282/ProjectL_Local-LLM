# 전투 공격 클립의 여러 발 발사 Task 계획 — 선택 ClipEvent2·3 (2026-11-02)

- 작성: View 작업, 2026-11-02, 3차(리뷰 보완 반영).
- 상태: **MS-a~MS-c 코드 완료·MS-c 리뷰 지점. MS-d 미착수, 사용자 Unity Ua/Ub/Uc 대기, 컴파일 미확인.**
- 원본: [선택 이벤트 설계 MS1~MS5](../specs/2026-11-02-combat-clip-multishot-design.md). 기존 ClipFire/Fire 시점 계획을 이 개정으로 대체한다.
- 순서: 설계/계획 리뷰 → MS-a → 보고/중단 → MS-b → 보고/중단 → MS-c → 보고/중단 → MS-d → 보고/중단 → 사용자 Unity 인수.
- 경로: `D/` = `Assets/ProjectL/Script/Systems/DataBaseSystem/Data/View/CombatPresentation/`, `C/` = `Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/ViewSystem/Combat/`. 새 소스/에셋은 meta를 함께 다룬다.

## 0. 작업 경계

- 사용자 "보완점 보완하고 작업 시작해"로 리뷰 보완과 계획 순서의 착수를 승인받았다. Phase가 끝나면 변경·정적 대조·미확인·다음 Phase를 보고하고 멈춘다.
- 새 Test 코드·MSBuild·Codex의 Unity 컴파일/재생/시험 실행을 하지 않는다. 보고에는 **컴파일 미확인**을 적는다.
- 기존 시험은 변경으로 컴파일이 깨진 호출/가짜 구현만 최소로 맞춘다. 실행 실패는 우리가 쓴 시험이면 확인 내용을 유지하고 호출만 맞추며, 다른 사람 시험이면 수정 전에 묻는다.
- Core·Buffer·FieldCamera·Architecture·공용 Framework 구현을 수정하지 않는다. 필요하면 중단하고 설계 변경을 묻는다. Core 요청이 생기면 지정 인계 문서에 요청으로 기록하고 제목도 최신 주제/날짜로 고친다.
- 착수 때 git status와 대상 diff를 다시 확인한다. 다른 작업의 Core/SO/씬/시험 변경은 되돌리거나 이번 결과로 보고하지 않는다. 이미 수정된 InGame Unit_Akimbo.asset은 대상이 아니다.
- 커밋·push·스테이징은 하지 않는다. 기존 에셋 일괄 재저장/이벤트 추가는 하지 않는다.

## Phase MS-a — 선택 이벤트 값·커서·신호

목표: 기존 세 마커만 있는 클립을 보존하면서 선택 ClipEvent2·3를 읽고 각각 한 번 전달한다. 운영 에셋은 아직 변경하지 않는다.

대상: 기존 D/CombatAnimationEvents.cs, CombatClipEvents.cs, CombatClipData.cs, CombatUnitAnimationSO.cs, CombatUnitAnimationRules.cs; C/Presentation/Pieces/CombatClipEventCursor.cs, CombatClipPlayback.cs, CombatClipPiece.cs, CombatPieceStart.cs. 번호별 클립 시각의 불변 값/식별자가 별도 파일이면 D/에 두고 meta를 만든다.

- [x] MS-a.1 모든 클립 읽기/생성자/커서/Playback/신호 소비자와 기존 시험의 직접 호출을 검색한다. 기존 enum 숫자와 생성자 호환 범위를 기록한다.
- [x] MS-a.2 선택 함수 ClipEvent2·3와 번호별 읽기를 추가한다. 선택 번호 빈칸을 허용하며 필수 셋·중복/순서/길이·부속 이벤트 금지를 검사한다.
- [x] MS-a.3 CombatClipTimings에 시각을 모으고 기존 EventTime/세 시각 생성 경로를 연결한다. readonly 항목과 내부 배열의 읽기 전용 래퍼로 노출하고 시각 원본을 중복 보관하지 않는다.
- [x] MS-a.4 커서의 enum + 1 가정을 제거하고 실제 존재하는 시각을 순서대로 진행한다. 같은 시각의 Start→1→2→3→End와 취소 뒤 중단을 정적 대조한다.
- [x] MS-a.5 Playback/ClipPiece에 서로 다른 번호별 신호를 연결한다. 기존 실행기의 동일 신호 중복 제거는 유지한다. 추가 시계·Coroutine·Evaluate는 만들지 않는다.
- [x] MS-a.6 기존 호출/시험은 컴파일 영향만 최소로 맞춘다. Allman·접근자·한국어 주석·UTF-8/LF·meta를 확인하고 보고 후 중단한다. 복수 발사 완료로 보고하지 않는다.

사용자 Ua: 기존 카탈로그/단발/피격 클립이 일괄 수정 없이 로드되고 기존 시점에 동작하는지. 필요하면 기존 DataBaseAssetTests로 교차 확인한다.

## Phase MS-b — 발사 연결·추가 지점·접촉 집계

목표: 범용 이벤트 번호를 발사 지점에 연결하고, 모든 탄 접촉 뒤 기존 피해 후속을 한 번 연다. 운영 Akimbo 입력은 아직 적용하지 않는다.

대상:

- D/CombatUnitAnimationSO.cs, CombatClipAttackData.cs, CombatAnimationEvents.cs, CombatUnitAnimationRules.cs; 신규 발사 연결/불변 값과 지점 한 쌍의 파일/meta.
- C/Display/Unit/ICombatUnitClip.cs, UnitClipChannel.cs, FieldUnitView.cs.
- C/Presentation/Pieces/CombatClipAttack.cs, CombatFirePiece.cs, CombatPieceStart.cs; 신규 CombatClipRangedAttack.cs, CombatVolleyReleasePiece.cs/meta(예정 이름).
- 기존 CombatAttackStart/CombatAttackPlayback의 좁은 발사 함수 연결은 필요한 인자만 맞춘다. 투사체 전략·Core 접촉 대상 해석·Gate 정책은 변경하지 않는다.

- [x] MS-b.1 선택 shots 입력을 추가한다. 빈 목록은 기존 Event1/지점0 단발로 정규화하고 명시 목록에는 기본 발사를 중복 추가하지 않는다. 이벤트 존재·원거리 종류·중복·명시 발사의 끝 시각을 검사한다.
- [x] MS-b.2 기존 muzzle/effectPoint를 지점0으로 보존하고 선택 추가 지점 목록을 더한다. 번호 해석을 한 곳에 모으고 대여의 번호 읽기를 연결한다. 실제 사용 총구/프리팹 소속을 검사한다.
- [x] MS-b.3 공통 클립 조립을 유지하고 원거리 조립만 분리한다. 각 탄은 선택한 번호별 클립 신호 뒤에 연결한다. Akimbo 키 분기·별도 시각 오프셋을 두지 않는다.
- [x] MS-b.4 개별 접촉과 해제를 분리한다. 시계 없는 집계 조각을 클립/0초 발사보다 먼저 준비한다. 마지막 최초 접촉은 완료 표시를 먼저 확정하고 release→Hit→End를 한 번 알린다. 보호된 콜백으로 재진입/세대 변경을 막고 Cancel은 입력을 닫는다. 집계가 손잡이/가변 맥락을 노출하거나 Tick에서 목록을 훑지 않게 한다.
- [x] MS-b.5 단발·동시 두 총구·같은 지점 반복·접촉 순서 역전·한 Tick 여러 이벤트·동기/중복/취소 뒤 콜백을 정적 대조한다. 미발사/실패를 타임아웃 성공으로 바꾸지 않는다.
- [x] MS-b.6 가로채기 앞 항목 뒤 발사와 집계 Hit 뒤 복귀, 모든 탄 도착/클립 끝/지연 큐까지 전체 수명, 취소 시 모든 손잡이 회수를 확인하고 보고 후 중단한다.

사용자 Ub: 기존 단발 직선/곡사의 피해 후속·종료와 가로채기·취소 동작. 두 발 운영 재생은 MS-d에서 인수한다.

## Phase MS-c — 범용 Cue2·3·번호별 지점·추가 공격음

목표: 새 발사 전용 시점 없이 번호별 범용 이벤트에 소리/이펙트를 연결한다.

대상:

- D/CombatCueAnchor.cs, CombatCueEntries.cs, CombatEventCues.cs, CombatPresentationCueRules.cs, CombatAttackPresentationSO.cs 설명.
- Data/View/Table/Trait/Item/TraitActivationVisual.cs의 덮어쓰기 제약 연결.
- C/Presentation/Pieces/CombatCueAnchors.cs, CombatCueComposer.cs, CombatCuePoints.cs, CombatEffectPiece.cs, CombatClipAttack.cs 및 실제 번호별 클립 시점을 조립하는 소비자. 새 공통 시점 연결/투사체 소리 도우미는 같은 책임 폴더와 meta로 둔다.
- 실제 시점/필드를 직접 열거하는 Editor 소비자는 검색 후 항목·설명만 맞춘다. 단순 enum 자동 표시는 불필요하게 수정하지 않는다.

- [x] MS-c.1 Cue2·3를 enum 뒤에 추가하고 원거리 공격 클립에서만 번호별 시점을 한 도우미로 연결한다. 없는 클립/이벤트 입력은 DB에서 거절하고 미설정 시점 예외를 유지한다. 근접/피격의 새 소비 확장은 별도 범위다.
- [x] MS-c.2 코드 모션·근접·피격·사망/사실 사건·임의 유닛용 특성 덮어쓰기의 미지원 조합을 거절한다. 첫 이벤트의 기존 근접/피격 연결은 변경하지 않는다.
- [x] MS-c.3 SO의 기존 sound와 추가 입력을 값 변환에서 사건 소리 시점 목록 하나로 합친다. 빈 큐/허용 사건/이벤트 존재 검사와 조립이 그 목록을 공유하게 한다. 시점 사용 검사에는 사건 소리를 포함한 모든 큐를 넣고 소리에만 Cue2가 있는 경우도 대조한다. 새 목록은 복사·읽기 전용 노출로 캡슐화한다.
- [x] MS-c.4 이펙트의 선택 actorPoint(기본0)를 총구/이펙트 지점에만 적용한다. 해당 번호·프리팹 소속·필요 effectPoint를 DB에서 검사한다. 다른 자리의 잘못된 번호는 거절한다.
- [x] MS-c.5 기존 큐의 보정·놓기·회수를 재사용한다. 이벤트 번호와 지점 번호를 같은 값으로 추측하지 않는다. Contact 이펙트는 각 탄 접촉을 재사용한다.
- [x] MS-c.6 투사체 비행/착탄 소리는 공통 조립 도우미에서 각 탄 발사/접촉 시점에 연결한다. 기존 단발/코드 모션도 같은 도우미를 사용하며 이중 재생하지 않는다.
- [x] MS-c.7 기존 Cue·Hit·Start/End 의미와 대표 근접/피격/사망/특성 큐의 좁은 회귀를 정적 대조하고 보고 후 중단한다.

사용자 Uc: 기본 큐 한 번, 선택 Cue2/3만 추가 실행, 올바른 actorPoint, 무음 프리셋 유지, 기존 단발/일반 큐 회귀.

## Phase MS-d — Akimbo 에셋·안내·묶음 리뷰

대상: New/Playerable/Akimbo.prefab, 해당 공격 클립의 이벤트 구간, CombatUnitAnimation_Akimbo.asset, 신규 CombatAttack_Akimbo.asset/meta, UnitVisual_Akimbo.asset의 공격 설정 참조. 공용 CombatAttack_Linear와 InGame 유닛은 변경하지 않는다. 공유 클립이면 Akimbo 전용 복제/meta만 만든다.

- [ ] MS-d.1 실제 두 발사 프레임·먼저/나중 무기 뼈·기존 소리가 단발인지 연사 묶음인지 확인한다. 정적으로 확정할 수 없으면 사용자 Unity 확인을 받아 기록하고 해당 에셋 입력을 확정한다. MS-a~c와 독립적인 선행 확인이다.
- [ ] MS-d.2 공유 참조를 검사해 기존 클립의 이벤트만 바꿀지 전용 복제로 연결할지 정한다. 첫 이벤트를 실제 첫 발에 맞추고 선택 ClipEvent2를 둘째에 둔다. 3번은 넣지 않는다. 원본 곡선/다른 유닛은 변경하지 않는다.
- [ ] MS-d.3 Akimbo에만 shots 두 줄과 추가 지점 한 쌍을 연결한다. 이벤트1→지점0, 이벤트2→지점1을 실제 프리팹 뼈에 맞춘다.
- [ ] MS-d.4 전용 공격 설정에서 기존 부품을 참조한다. 단발 소리면 기존 Cue + 추가 Cue2, 연사 묶음 소리면 기존 한 번을 선택한다. 사용자 지정 총구 효과만 Cue/actorPoint0 및 Cue2/actorPoint1에 연결하며 임의 효과를 고르지 않는다.
- [ ] MS-d.5 GUID·meta·새 필드 기본값·다른 외형의 공용 설정 유지·InGame 무변경을 확인한다. 기존 이펙트/소리 안내에는 본 설계를 원본 링크로 연결하고 실제 값/미확인을 기록한다.
- [ ] MS-d.6 설계 §9의 SSOT·SOLID 5원칙·JMH/FW·기능 독립성을 실제 변경과 대조해 보고 후 중단한다. 별도 에이전트나 Unity 실행을 이 Task가 승인하지 않는다.

## 사용자 Unity 인수

아래는 새 시험 작성 Task가 아니다. 사용자가 컴파일·재생·필요한 기존 시험을 확인한다.

- [ ] U1 컴파일/DB 생성과 기존 DataBaseAssetTests. 단발에 이벤트2/3나 프리팹 일괄 재저장이 필요 없는지.
- [ ] U2 Akimbo의 실제 두 발사 시점·총구·정확히 두 투사체, 첫 이벤트의 기본 발사가 중복 추가되지 않는지.
- [ ] U3 첫 탄에 체력/숫자/피격/사망이 선반영되지 않고 두 탄 접촉 뒤 Core 후속이 한 번 표시되는지. 처치/비처치 각각 확인.
- [ ] U4 각 탄의 소리/접촉 효과와 대상 피격 소리 구분. 단발 소리/연사 묶음 리소스의 중복 발음 여부. 총구 VFX 미지정은 별도 기록.
- [ ] U5 짧은/긴 거리·대상 없는 공격·빗나감·낮은 프레임에서 이벤트/발사 누락이나 대기 멈춤이 없는지. 접촉 순서 역전도 둘 다 기다리는지.
- [ ] U6 클립 끝보다 비행이 긴 경우 전체 종료, 발사 전/두 발 사이/비행 중 취소와 씬 전환에서 남는 탄·지연 소리·늦은 피해 표시가 없는지.
- [ ] U7 가로채기·공격자 자신의 외형을 쓰는 추가 공격·다음 행동/기차/결과 완료 수명 확인.
- [ ] U8 대표 단발 직선·곡사·근접·피격 클립과 기존 큐의 제한된 회귀. 기존 시험의 확인 내용이나 저자 불명 시험을 임의 수정하지 않는다.

## 중단 조건과 실행 기록

- 모든 클립의 이벤트/프리팹 일괄 변환, 별도 시계/Coroutine, Gate 정책 변경, Core 결과 분할이 필요해지면 중단하고 묻는다.
- 4번 이후 이벤트·실제 탄별 피해 판정·새 오디오/VFX 제작·새 개발 도구가 필요하면 별도 범위로 제안한다.
- 정확한 프레임/손/소리 분량을 확인하지 못하면 MS-d의 해당 설정을 확정하지 않는다. 경과 시간은 확인을 대신하지 않는다.
- 2026-11-02 1차: 선택 ClipFire와 발사별 Fire 시점 설계/계획 작성, 구현 없음.
- 2026-11-02 2차: 사용자 피드백에 따라 선택 ClipEvent2/3, 범용 번호별 시점, 이벤트→발사 연결로 문서를 개정했다. 구현 Task는 모두 미착수다. 코드·에셋·시험·빌드·Unity 실행·커밋·push 변경 없음. **컴파일 미확인.**
- 2026-11-02 3차: 사용자 "보완점 보완하고 작업 시작해" 승인으로 리뷰 R1~R5를 설계와 이 Task에 반영했다. 선택 번호 빈칸 허용, 시각/목록 캡슐화, 집계 release→Hit→End, 단일 사건 소리 시점 목록과 전체 큐 검사, 근접/피격 새 소비의 범위 분리를 채택했다. 현재 MS-a만 착수하며 나머지는 후속 Phase다.

### MS-a 코드 완료·정적 대조 (2026-11-02)

- 변경: 기존 소스 9개와 신규 [CombatClipTimings.cs](../../../Assets/ProjectL/Script/Systems/DataBaseSystem/Data/View/CombatPresentation/CombatClipTimings.cs)/meta. Data 쪽 AnimationEvents·ClipData·ClipEvents·AnimationSO·AnimationRules(원본 안내 주석만), 런타임의 Cursor·Playback·ClipPiece·PieceStart다. 기존 meta GUID는 유지했고 새 GUID는 한 곳에만 있다.
- 이벤트 이름표는 기존 필수 셋 + 선택 ClipEvent2/3다. 두 번째 없이 세 번째만 허용한다. 부속 이벤트 금지와 클립 길이/반복/루트 검사 코드는 유지했다.
- Timings는 내부 생성 배열을 ReadOnlyCollection으로 감추고 중간 항목을 readonly struct로 둔다. ClipData의 세 시각은 Timings를 읽는 접근이며 커서는 같은 Timings 참조와 진행 인덱스만 보관한다.
- 기존 호환: ClipEvents.Read의 세 out, ClipData의 세 시각 생성자/FindTimeError/프로퍼티, Cursor의 세 시각 생성자, Playback의 기존 인자 호출을 유지했다. 운영 SO는 선택 이벤트를 잃지 않는 ReadTimings→ClipData→Cursor 경로로 연결했다.
- enum: 기존 ClipEventKind Start=1/Event=2/End=3 유지, Event2=4/Event3=5 추가. PieceSignal의 기존 1~5 유지, ClipEvent2=6/ClipEvent3=7 추가. 신호 중복 비트의 범위 안이며 기존 실행기/맥락은 수정하지 않았다.
- 직접 호출 검색: 운영 ReadClip→ClipData, Playback→Cursor, ClipPiece→Playback 한 경로를 확인했다. 현재 Assets/Tests의 C# 검색에서는 해당 타입/호출 직접 참조가 나오지 않았고 기존 시험 파일을 수정하지 않았다. 이 검색은 시험 실행이나 컴파일 확인을 뜻하지 않는다.
- Editor CharacterClipCopier는 기존 이벤트가 있으면 전체 배열을 보존하고 없으면 필수 셋만 만든다. 선택 이벤트와 충돌하지 않아 코드/에셋을 수정하지 않았다.

| 정적으로 대조한 경로 | 코드상의 결과 |
|---|---|
| 기존 필수 셋만 있음 | 중간 목록 1개, Start→Event→End |
| 선택 2번만 추가 | Start→Event→Event2→End |
| 2번 없이 선택 3번 추가 | 빈 번호를 강제하지 않고 Start→Event→Event3→End |
| 2·3번 모두 추가/같은 시각 | 번호순 한 번씩 처리, End는 마지막 |
| 한 Tick에 여러 시점 교차 | Emit의 기존 while이 아직 남은 시점을 순서대로 소비 |
| 콜백 안에서 Cancel | 다음 반복 전에 기존 취소 조건으로 중단, 클립 복원은 기존 대여 주인이 담당 |
| 중복/필수 누락/미지 함수/역순/NaN·Infinity/끝 초과 | 읽기/공통 시각 규칙에서 예외. 선택 번호의 빈칸만 정상 입력 |

- Tick에 배열 생성·파싱·정렬·씬 검색은 추가하지 않았다. 선택 콜백 존재 확인은 Begin에서 한 번이며, 존재하지 않으면 콜백도 생성하지 않는다. 추가 시계·Coroutine·Animator Evaluate·Service/Module/Factory/Export 변경 없음.
- 정적 확인: 대상 diff 공백 검사 통과, UTF-8/LF·U+FFFD 없음·새 meta GUID 단일성 확인. 시각 참조의 SSOT·읽기 전용 노출·기존 숫자 보존·단발 신호/끝 경로를 대조했다. 실행 검증은 아니다.
- MS-a 리뷰 지점에서 중단한다. **컴파일 미확인**, 새 시험·기존 시험 변경·MSBuild·Unity 재생/시험 실행 없음. Core·운영 SO/프리팹/클립·씬은 이번 작업으로 수정하지 않았다. Akimbo의 실제 두 발은 아직 구현하지 않았다.
- 다음 MS-b: shots 입력·지점 읽기·원거리 조립·접촉 집계/취소. 이후 MS-c에서 소리/번호별 큐를 연결하고 MS-d에서 Akimbo 입력을 적용한다. 사용자 Ua는 기존 단발/피격·카탈로그와 필요 시 기존 DataBaseAssetTests다.

### MS-b 코드 완료·정적 대조 (2026-11-02)

- 사용자 "다음 작업 시작."으로 MS-b를 진행했다. 기존 MS-a 및 다른 작업자의 미커밋 변경을 유지했다. 사용자 Unity Ua 통과를 전달받은 것은 아니며 Ua/Ub는 미확인 상태다.
- MS-b 변경: 기존 소스 11개, 신규 소스 4개와 각 meta. 신규 파일은 [CombatClipShot](../../../Assets/ProjectL/Script/Systems/DataBaseSystem/Data/View/CombatPresentation/CombatClipShot.cs), [CombatAttackPoint](../../../Assets/ProjectL/Script/Systems/DataBaseSystem/Data/View/CombatPresentation/CombatAttackPoint.cs), [CombatClipRangedAttack](../../../Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/ViewSystem/Combat/Presentation/Pieces/CombatClipRangedAttack.cs), [CombatVolleyReleasePiece](../../../Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/ViewSystem/Combat/Presentation/Pieces/CombatVolleyReleasePiece.cs)다. 기존 11개에는 근접/집계가 공유하는 CombatHitMoment의 설명 한 줄 변경도 포함한다. MS-a 변경까지 합친 git diff 수와 이번 Phase의 파일 수를 구분한다.
- 입력: AnimationSO의 선택 shots(eventId, point). 빈/누락 목록은 기존 Event/지점0 한 발, 명시 목록은 그 전체만 사용한다. readonly CombatClipShot을 복사해 읽기 전용 목록으로 제공하고 명시 입력 출처를 유지한다. 없는 이벤트·끝 시점의 명시 발사·같은 이벤트/지점 중복·음수 지점·공격 클립 없는 목록은 거절한다. 원거리 종류 판정은 기존 FiresProjectile 한 곳을 사용한다.
- 지점: 기존 muzzle/effectPoint 필드는 그대로 지점0이다. 선택 additionalAttackPoints 첫 원소가 지점1이다. CaptureAttackPoints가 복사한 CombatAttackPoints 정의 안에서 번호를 해석하며 DB 검사/UnitClipChannel이 같은 형식을 읽는다. 가변 입력 배열을 대여에 노출하지 않는다. 대여의 기존 번호 없는 호출은 0번을 읽으며 번호별 위치/자세 읽기를 추가했다.
- 유닛 검사: 각 발사가 사용하는 총구와 프리팹 소속을 확인한다. 현재 공격 전체 큐가 쓰는 기본 총구/effectPoint의 소속도 확인한다. 추가 지점 effectPoint의 실제 큐 사용 검사는 actorPoint를 도입하는 MS-c에 남는다.
- 조립: 공통 앞부분은 출처/대여/가로채기 앞 항목/방향을 유지한다. 원거리는 같은 시작 조건에서 집계→클립 순으로 준비하고 각 발사 조각을 해당 클립 이벤트 신호에 연결한다. 근접은 기존 방향→클립→해제 순서를 유지한다. Akimbo 키/손 개수 분기는 없다.
- 수명: 집계는 발사별 첫 접촉만 세고 완료 표시를 먼저 확정한 뒤 기존 CombatHitMoment를 재사용해 release→Hit, 이어 보호된 End를 알린다. 재진입 취소로 필드가 비워져도 이미 캡처한 보호된 콜백으로 끝을 처리한다. 지연/취소 후 접촉은 무시한다. 발사 조각은 자기 손잡이만 회수하며 발사 함수 안에서 동기 취소/도착한 뒤 손잡이가 반환돼도 회수한다.
- 물리 Contact 신호를 8번으로 추가했다. 기존 신호 1~7은 유지하고 집계의 Hit과 구분한다. 같은 신호 비트의 중복 제거·실행기·Gate·Core·투사체 전략은 변경하지 않았다. Contact는 후속 MS-c의 탄별 소리 연결에서도 쓸 수 있다.
- 검색: ICombatUnitClip의 현재 실제 구현은 FieldUnitView.PresentationUnitClip이며 번호별 계약을 연결했다. Assets/Tests의 ignore 제외 검색에서도 해당 인터페이스/FirePiece/ClipAttackData 직접 참조가 나오지 않았고 시험 파일을 수정하지 않았다. 검색을 컴파일/시험 통과로 보지 않는다.

| 정적 경로 대조 | 코드상의 처리 |
|---|---|
| 기본 단발 | 정규화된 한 발 접촉으로 집계 해제 1회, 클립 끝/탄 도착까지 전체 수명 유지 |
| 명시 두 발/세 발 | 기본 발사를 더하지 않고 각 이벤트 신호에 연결, 전체 접촉 전에는 해제하지 않음 |
| 같은 이벤트의 다른 총구 | 입력 목록 순서대로 별도 항목/손잡이, 각각 다른 집계 인덱스 |
| 같은 총구를 다른 이벤트에 사용 | 서로 다른 발사로 인정, 현재 총구 위치를 각 발사 때 읽음 |
| 접촉 순서 역전/중복 | 인덱스별 첫 접촉만 남은 수에서 차감, 마지막 최초 접촉이 해제 |
| 0초 이벤트/동기 콜백 | 집계 Begin이 클립 Begin보다 앞서며 콜백/End를 먼저 준비 |
| release 중 취소/재진입 | 완료 상태 선확정, 기존 세대/실행 보호 콜백과 취소 상태로 늦은 Hit/End 차단 |
| 발사 안에서 취소 후 손잡이 반환 | 닫힌 발사 조각이 반환된 손잡이를 즉시 Recall |
| 비행 도중 취소/클립 끝보다 긴 비행 | 시작한 발사별 손잡이 회수, 미시작 항목은 기존 실행기가 차단; 정상 실행은 모든 항목 끝까지 대기 |
| 가로채기 앞/뒤 항목 | 같은 시작 조건으로 함께 밀리며 복귀는 집계 Hit 뒤에 연결 |

- SSOT/캡슐화: 시각은 기존 Timings, 발사 연결은 복사된 Shots, 지점 대응은 복사된 정의, 접촉 집계는 실행별 조각, 대상 해제는 기존 Gate 한 곳이다. 집계의 가변 배열/맥락은 외부에 내주지 않고 접촉 콜백만 주입한다.
- SOLID/기능 독립성: 조립·단일 발사·집계를 분리했고 근접 해제/코드 모션/피격의 기능은 확장하지 않았다. 기존 Hit 순서 도우미를 재사용한다. 이후 발사 시점/총구 입력 변경은 해당 입력과 원거리 확인에 머무른다.
- JMH/FW/성능: Service/Module/Builder/Factory/Export/루트 조회 변경 없음. 기존 대여·풀·소스 수명을 유지한다. 집계와 발사의 Tick은 비어 있고 완료는 접촉/도착 콜백으로 처리한다. 목록 복사/중복 검사는 DB 생성에서만, 새 순회/할당/검색/시계/Coroutine/Evaluate는 Tick에 추가하지 않았다.
- 정적 확인: 대상 diff 공백·UTF-8/LF·U+FFFD 없음·신규 meta GUID 단일성 대조. **컴파일 미확인**, 새/기존 시험 수정·시험 실행·MSBuild·Unity 재생 없음. 운영 SO/프리팹/클립/씬과 Core는 이번 Phase에서 수정하지 않았다.
- MS-b 리뷰 지점에서 중단한다. **MS-c(번호별 큐·소리 목록·탄별 비행/착탄 소리)와 MS-d(Akimbo 에셋 적용)는 아직 미착수다.** 현재 공격 전체 큐/투사체 소리 연결은 기존 한 번 경로이며, 복수 탄의 소리 완성으로 보고하지 않는다. 접촉 이펙트는 기존 FireShot의 탄별 접촉 경로를 그대로 재사용한다.
- 사용자 Ub: 컴파일/DB 생성, 기존 단발 직선·곡사의 접촉/피격·종료, 가로채기·취소. DB 프리팹 소속 검사 조건이 늘었으므로 기존 DataBaseAssetTests 확인도 권장한다. 다음 MS-c는 이 계획의 §Phase MS-c 파일/검사 범위만 진행한다.

### MS-c 코드 완료·정적 대조 (2026-11-02)

- 사용자 "다음 진행해"로 MS-c를 진행했다. Ua/Ub 통과를 전달받은 것은 아니며 Ua/Ub/Uc는 미확인이다. 기존 미커밋 변경을 유지했고 운영 에셋에는 적용하지 않았다.
- 시점: 기존 CombatCueAnchor 1~5를 유지하고 Cue2=6·Cue3=7을 추가했다. CombatClipRangedAttack.ConnectEventCues가 실제 존재하는 선택 이벤트만 연결하며, DB 존재 검사와 런타임 연결은 같은 AnchorOf 대응을 읽는다. 근접/피격/코드 모션의 추가 이벤트 소비는 확장하지 않았고, 미연결 시점 예외를 유지했다.
- 검증: 원거리 공격 큐만 Cue2/3를 허용하고 유닛 검사에서 실제 공격 클립/해당 이벤트를 요구한다. 근접·피격·사망/사실 사건은 사건별 기존 규칙으로 거절한다. 임의 유닛용 특성 공격 덮어쓰기는 UsesAdditionalEvents로 거절한다. 유닛 자신의 공격 표시를 쓰는 추가 공격은 같은 발사 목록을 재사용한다.
- 소리: 기존 SO의 sound 필드를 유지하고 선택 additionalSounds를 추가했다. CombatEventCues가 입력을 복사해 읽기 전용 SoundTimings 하나로 정규화한다. 기존 Sound 읽기는 목록 첫 항목의 호환 접근일 뿐 별도 상태가 아니다. IsEmpty·허용 사건·UsesAnchor·조립이 모두 같은 목록을 읽는다. 사망 소리 입력은 기존 한 시점이다.
- 지점: 이펙트 입력/값에 actorPoint(기본0)를 추가했다. 0이 아닌 값은 ActorMuzzle/ActorEffectPoint에서만 받으며, 유닛 검사에서 해당 번호의 실제 Transform과 프리팹 소속을 검사한다. 재생은 큐가 시작하는 순간 번호별 위치를 읽고 기존 보정·놓기·회수 경로를 쓴다. 이벤트 번호에서 손 번호를 추측하지 않는다.
- 투사체 소리: 신규 [CombatProjectileCueComposer](../../../Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/ViewSystem/Combat/Presentation/Pieces/CombatProjectileCueComposer.cs)와 meta를 추가했다. 원거리 조립이 읽기 전용 탄별 발사/Contact 시점만 만들고, 공통 큐 조립이 기존 위치에서 소리 도우미를 한 번 호출한다. 단발도 같은 한 행 경로이며 코드 모션은 기존 Cue/Hit 시점을 같은 도우미에 넘긴다. 기존 프리셋·지연을 재사용하고 무음 프리셋/AudioSystem 슬롯 연결 대기는 기존 None 필터를 유지한다. 기존 공격 시작 설명의 '소리 한 번' 문구도 선택 시점과 집계에 맞췄다.
- Editor 검색에서 새 필드/시점을 수동 열거하는 소비자는 발견되지 않아 드로어는 수정하지 않았다. Assets/Tests/EditMode의 ignore 제외 검색에서 변경한 FindUnitError/CombatClipRangedAttack과 관련 생성자의 직접 호출을 발견하지 않았고 시험 파일을 수정하지 않았다.

| 정적 경로 대조 | 코드상의 처리 |
|---|---|
| 기존 단발 입력 | sound 한 항목, actorPoint0, 기존 Cue/Start/End/집계 Hit 시점 유지 |
| 소리에만 Cue2/3 사용 | UsesAnchor가 SoundTimings도 순회하므로 해당 클립/이벤트 없으면 DB 거절 |
| Event2 없이 Event3만 있음 | 실제 시각 목록의 Event3만 Cue3에 연결, Cue2 입력은 DB 거절 |
| 근접/피격/사망/사실 사건/특성 덮어쓰기의 Cue2 | 사건 규칙 또는 특성 덮어쓰기 검사로 거절, 런타임 대체 없음 |
| 원거리 코드 모션(공격 클립 없음)의 Cue2 | 유닛 큐 검사에서 해당 공격 클립/이벤트 부재로 거절 |
| 추가 effectPoint/총구 | 큐가 고른 번호만 검증/읽기, 없는 번호·다른 프리팹 지점·다른 자리의 비0 번호는 거절 |
| 두 총구 동시 발사/같은 총구 반복 | 발사 행마다 소리 시점 하나, 각 탄의 Contact에 착탄 소리 연결 |
| 피해 후속/대상 피격 | 탄별 소리가 집계 상태/Gate를 변경하지 않으며 모든 접촉 뒤 Hit 한 번 유지 |
| 지연 큐/취소 | 기존 CombatPieceStart와 실행의 지연/세대/취소 수명을 재사용, 별도 시계 없음 |
| 일반 피격·사망·특성 큐 | 기존 기준 시점 연결·플래시·프리셋 소리·효과 자리/회수 경로 유지 |

- 구조 대조: 소리 시점은 SoundTimings, 이벤트 시각은 Timings, 지점 대응은 CombatAttackPoints가 소유한다(SSOT). 입력과 실행 값을 분리하고 새 목록은 내부 배열을 노출하지 않는다. 조립·발사·집계·효과/소리 구현을 분리하며, 기존 계약/취소 경로를 유지하고 추가 서비스나 미래용 인터페이스를 만들지 않았다(SOLID S/O/L/I/D·기능 변경의 독립성).
- JMH/FW: Service/Module/Builder/Factory/Export·Framework·Core·FieldCamera 변경 없음. 대여/창구/기존 조각의 수명을 재사용했다. 새 분기는 DB 검사/조립/효과 시작에만 있고 Tick 검색·할당·폴링·Coroutine·별도 Update를 추가하지 않았다.
- 정적 확인: 대상 diff 공백 검사 통과, UTF-8 무BOM/LF·U+FFFD 없음, 신규 meta GUID가 Assets 내 한 파일에만 있는 것을 확인했다. 이 기록은 컴파일/실행 검증이 아니다. **컴파일 미확인**, 새/기존 시험 작성·수정·실행, MSBuild·Unity 컴파일/재생 없음. Core·운영 SO/프리팹/클립/씬은 이번 Phase에서 수정하지 않았다.
- MS-c 리뷰 지점에서 중단한다. 다음 MS-d는 실제 발사 프레임/손 순서/소리 분량을 확인하고 Akimbo 전용 클립·지점·발사 목록·공격 설정을 적용하는 단계다. 사용자 Unity Uc는 기본 큐 한 번/선택 시점/번호별 지점/무음·기존 일반 큐 회귀를 확인한다.
