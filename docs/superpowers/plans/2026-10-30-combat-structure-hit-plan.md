# Structure 피격 표시 Task 계획

- 날짜: 2026-10-30
- 상태: **2026-11-01 Phase 1~5 코드·에셋 연결 완료 / 코드 리뷰 완료 / Unity 실행 인수 미완료**. 아래 체크는 구현 완료와 실제 실행 검증을 구분한다. Unity 관련 검증 Task는 미실행 항목을 남겼다.
- 기준: [Structure 피격 설계](../specs/2026-10-30-combat-structure-hit-design.md) SH1~SH16. 2026-11-01 선택·좌하단 정보 요구와 7기준 보완 반영, 구현 보류 유지.
- 2026-11-01 사용자 전체 Task 진행 지시로 구현했다. 커밋·push는 하지 않았다. Phase별 중간 보고 후 계속 진행했으며 실제 실행 증거와 한계는 아래 실행 기록을 따른다.
- 시작 시 현행 코드와 작업 트리를 다시 대조한다. 특성 유닛 행동 변경 중인 Presenter·ShownFacts·공격 조립 파일의 기존 변경을 덮어쓰지 않는다.

## Phase 1 — 외형 설정·규칙

- [x] S1. `UnitVisualDataSO.cs`에 빈 값 호환 `structureHitCues`와 조회 계약 추가. 기존 필드·GUID 유지.
- [x] S2. `CombatCueEvent.StructureHit` 검사 추가: 이펙트/Start/지연 0/SubjectBody/기존 위치·회전 보정만 허용. 장애물 키의 사용 거절.
- [x] S3. `CombatCueSources`에 EntityKey 기반 조회 추가. Structure 큐 캐시는 `CombatMomentCuePlayback`만 소유하고 연결 해제 때 폐기. Sources/처리기 캐시 중복 금지, InGame DB 조회 없음.
- [ ] S4. 규칙 검증: 빈 값, 정상 이펙트, 잘못된 앵커·지연·자리·소리·플래시·흔들림, 장애물 키. 기존 외형 에셋 로딩 호환 확인.

완료 조건: 기존 에셋의 동작 유지, 새 큐 입력/검증 가능. **끝나면 보고하고 멈춤.** 실제 사건 피드백은 아직 연결되지 않음.

## Phase 2 — Structure 숫자 위젯 수명

- [x] S5. UnitDamageText 독립 재생의 대여 토큰/자연 완료 수신자 추가(SH8). 상태 확정·수신자 분리 후 자연 완료 1회, Clear/취소/OnDisable 통지 0회. 기존 Update/설정/캐릭터 2슬롯 동작 유지, Presenter 완료 폴링 금지.
- [x] S6. Structure 숫자 Presenter는 활성 대여/토큰·투영 대여와 고정 월드 앵커를 소유. Framework CustomPool 재사용, 생성/반환 콜백은 Factory 조립. 기존 Pool API·반환 순서 먼저 대조하고 계약 부족 시 설계 확인. 독립적인 범용 Pool 구현 금지.
- [x] S7. UnitHUDLayer 내부 투영 항목을 공통화하고 일반 숫자 위젯 등록/해제 대여 추가(SH7). HUD API는 어댑터로 유지. 카메라/목록 공개 및 Structure 분기 금지. 기존 UI 등록/개폐·HUD 호버와 숫자 입력 비차단 정책 보존.
- [x] S7a. Factory가 템플릿 읽기 전용 입구·완결된 하위 TMP 참조를 검증하고 생성 함수/Pool/숫자 표시 입구 연결(SH9). 런타임 HUD 계층 검색 없음. Layer 연결→숫자 Presenter→처리기 순서, 서비스/루트 재등록 없음.
- [x] S8. 반환 순서(활성 대여 제거/토큰 무효화→투영 해제→Clear→비활성화→Pool 반환) 단일화. 정상 완료/취소/오류 정지 공통 회수. 씬 해제 시 숫자 활성 대여·Pool을 Layer보다 먼저 정리. 정상 결과 완료는 숫자를 지우지 않음.
- [ ] S8a. 완료 안 재대여·이전 토큰/중복 통지·반복 취소·화면 밖/카메라 교체·Layer 숨김 검증. 생성/투영 등록/시작 실패의 부분 자원 회수와 잘못된 템플릿 외부 TMP 참조 거절 확인.
- [ ] S9. 기존 캐릭터 숫자 2슬롯·확대/상승/페이드와 HUD 화면 밖 투영 회귀 확인. 필수 템플릿 누락은 구성 예외 확인.

완료 조건: Structure 모델을 즉시 회수해도 숫자가 끝까지 보이고 해제 후 잔류 없음. **끝나면 보고하고 멈춤.** 독립 UI 화면·전역 서비스·별도 숫자 duration 금지.

## Phase 3 — 피격 사건 연결

- [x] S10. Structure 전용 사건 처리기 추가(+ `.meta`). SH1의 HP 감소/적/네이티브 장애물 제외를 한 곳에서 판정.
- [x] S11. 사건 슬롯·처리기 표·Presenter 조립 연결. Facts 처리기의 Structure 반영 갈래는 새 처리기로 이전해 단일화. 기존 주체·근원·Gate 대응은 유지.
- [x] S12. Final 반영 전 몸 자리/숫자 앵커 확보 → 큐 Accent와 숫자 시작 → 기존 틀에서 Final 반영·Render. SH7의 사실/큐/숫자 입구만 주입, DB 전체·UI·카메라·Pool 전달 없음. 별도 사건 완료/대기/타이머 없음.
- [ ] S13. 빈 큐, 회복/동일 HP/위치만 변경, 비치명타, 치명타, 장애물 제외에 대한 사건 경로 검증. 숫자·이펙트 각각 1회 확인.
- [ ] S14. 근접 ClipEvent + HitDelay 전/후와 원거리 접촉 전/후 회귀 확인. 다중 대상/관통의 대상별 해제와 취소 세대 유지. 환경 피해는 사건 시점 확인.

완료 조건: 코드 인수 기준 충족, 생존·치명타 모두 같은 사건 시점의 피드백. **끝나면 보고하고 멈춤.** Core·공격 타이밍 정책 수정은 범위 밖.

## Phase 4 — 에셋 연결·Unity 인수

- [x] S15. 적 Structure 외형 목록/실제 사용 키 확인. 기존 등록 이펙트 중 적합한 것으로 `structureHitCues` 연결(새 아트 제작은 별도). 숫자 템플릿 참조 확인. 지정 가능한 이펙트가 없으면 사용자에게 선택 요청.
- [ ] S16. Unity 컴파일·관련 EditMode 테스트 실행. DB 에셋 변경 시 `DataBaseAssetTests` 실행. 해당 테스트/어셈블리가 현행 트리에 없으면 이름을 지어 통과 처리하지 말고 실제 교차 검증 경로와 미검증을 보고.
- [ ] S17. Unity에서 근접/원거리 생존·치명타, 빠른 반복 피격, 복수 Structure, 화면 밖/카메라 이동, 장애물 피격/파괴, 전투 취소·씬 재진입 확인.
- [ ] S18. 공통 변경 회귀: 캐릭터 숫자·기존 장애물 큐·공격 Contact 큐·HUD 투영/호버/수명. 생성→UI 연결→소비→반환을 설계 §3의 FW 표와 대조. FW-008/009·011/012·014 적용, FW-103 권장사항 구분. SSOT·SOLID 5원칙·독립성·캡슐화·C#/Unity·If/Tick 재검토.
- [x] S19. 결과 기록: 구현 파일/에셋, 실제 실행한 검증, Unity 미확인 항목, 외형 편집 담당자 공유 메모(외부 발송하지 않음).

완료 조건: 설계 §4 인수 기준 모두 증거로 구분해 기록. **끝나면 보고하고 멈춤.** 사용자만 커밋·push.

## Phase 5 — Structure 선택·좌하단 정보 (2026-11-01 추가)

- [x] S20. 현행 Inspection/Router→SelectCell→SelectionReader→CombatUnitInfo 경로 대조. SH11의 Structure 선택·캐릭터 우선·명령 행위자 분리를 적용하고 기존 SelectedEntityId 하나 유지.
- [x] S21. Character 전용 Find/Synchronize 확장: 살아 있는 일반 적 Structure 점유 칸 선택·단일 개체 조회. CharacterData 변환·별도 선택 상태·InGame DB 조회 금지.
- [x] S22. 공통 정보 표시값과 캐릭터/Structure별 변환을 분리해 기존 CombatUnitInfo/View에 연결. Structure 이름/설명/아이콘/진영/HP만 표시, 캐릭터 전용 칸 정리/복원. 기존 UIId·Parent·Policy·UISystem 개폐 유지.
- [x] S23. 패널 보류 전환을 선택 ID 기반으로 처리. 표시 사실 반영 경계에서 선택 정보 갱신, 파괴 시 선택 해제/닫기. ShownFacts 단일 원본과 좁은 조회 계약 사용, 최신 Buffer HP 선반영·새 Tick 감시 금지.
- [ ] S24. 여러 칸 선택, 캐릭터↔Structure 교체, 호버/빈 칸, 공격 조준 중 클릭, 피격 전/후 HP, 전환 중 파괴, 국면/씬 재진입 검증. 기존 캐릭터 명령·선택·장애물 제외 회귀 확인.
- [x] S21a. SH14의 ICombatInspectionFactsReader/TryGet/TryPick와 불변 읽기 값 정의. ShownFacts가 단일 구현, 선택 가능 조건 재사용. Selection의 Character 목록 보관/Find를 reader로 대체. 초기/최종 Replace와 사건 Apply에서 같은 원본을 읽는 테스트 추가.
- [x] S22a. SH16의 공통 CombatInspectionInfo + 선택적 CharacterInfoSection 정의. 기존 실제 표시 필드만 포함. 캐릭터/Structure 변환 분리, View의 전용 그룹 적용/정리 한 곳으로 수렴. Structure↔캐릭터 복원·늦은 가용성 응답 검증.
- [x] S23a. SH15의 CombatInspectionPresenter에 선택 검증→변환→패널 갱신 조정. Display는 위임/조립만 수행. ShownFacts.Render 완료의 단일 수신자 연결, 개별 사건 처리기 알림/최신 Buffer fallback/dirty 상태 추가 금지.
- [x] S23b. 전환 완료는 ID 기준 정보 재요청. 조정자의 연결 세대·현재 선택 검증, 해제 시 수신자 분리, UI 콜백 재진입·해제 뒤 통지 검증. 표시 중 값은 과거 전환의 지연 적용에 쓰지 않음.
- [ ] S24a. 캐릭터 피격/이동·Structure 파괴·최종 Replace의 공통 갱신 회귀, 계산 완료 Buffer와 표시 사실 시점 차이 검증. 새 Update/Tick/전체 대상 감시·정보 종류 분기 중복이 없는지 7기준 리뷰.

Phase 5 내부 순서: S20 경로 확인 → S21/S21a 읽기·선택 → S22/S22a 표시값·View → S23/S23a/S23b 조정·수명 → S24/S24a 검증. 기존 S21~S23의 추상 표현은 SH14~SH16과 추가 Task가 구체화한다.

완료 조건: 설계 §6 추가 인수 기준 충족. **끝나면 보고하고 멈춤.** Phase 1~4 완료와 별개로 기록한다. 이 Task 추가는 구현 승인이 아니다.

## 검증 전략과 범위

- 새 테스트는 규칙 거절·실제 사건 시점·치명타 수명·취소/재사용 같은 동작 계약에 집중한다. 구현 줄을 그대로 따라가는 테스트는 만들지 않는다.
- 순수 사건 테스트는 실제 이전/이후 HP와 근원을 사용한다. Unity 투영/이펙트 자연 종료/숫자 잔류는 실제 재생으로 확인한다.
- 클래스 수가 늘었다는 사실을 독립성 개선의 증거로 쓰지 않는다. Structure 피드백 변경에 환경/아이템 갈래가 따라오지 않는지 확인한다.
- 새 파일은 네임스페이스·Allman·한국어 주석·UTF-8/LF·`.meta`를 확인한다. 기존 코드/에셋 이동·GUID 변경은 계획하지 않는다.
- 설계 변경이 필요하면 구현을 멈추고 먼저 묻는다. Phase 완료는 전체 기능 완료와 구분한다.
- 이펙트 전용/지연 0·장애물 제외는 이번 범위 정책(SH10)이며 구조 원칙의 필수조건으로 판정하지 않는다.
- 숫자 완료를 매 프레임 훑는 검사·새 경과 타이머·SO 반복 조회·중복 카메라 투영 루프가 없는지 확인한다. 기존 숫자 Update와 Layer 렌더 콜백은 실제 애니메이션/투영 책임이므로 유지한다.

## 실행 기록

### 2026-10-30 — 문서 작성

- 현행 Structure 사건·Gate·몸 조회·외형 큐·UnitDamageText·HUD Layer 수명 대조.
- 기존 설계 재사용 범위와 치명타 숫자 수명 보완 상세안 작성.
- 코드/에셋 수정 및 실행 검증 없음. 모든 구현 Task 미착수.

### 2026-10-30 — 7기준 리뷰 권장 보완 반영

- 사용자 지시: 권장 보완을 설계/Task에 반영하되 구현하지 않음.
- 투영 공통 등록·캡슐화(SH7), 자연 완료/취소/재사용 토큰(SH8), Factory/CustomPool/반환 순서(SH9), 범위 정책 구분(SH10) 구체화.
- Structure 큐 캐시 소유자를 MomentCuePlayback 한 곳으로 확정. FW 번호 일괄 표기를 적용 경로별 근거로 교체.
- S3·S5~S8·S12·S18 보완, S7a·S8a와 관련 인수 기준 추가. 모든 구현 Task 미착수 유지. 문서만 변경.

### 2026-11-01 — 선택·좌하단 정보 요구 추가

- Structure 분류 유지, 기존 선택 원본과 정보창 재사용, 조작 행위자와 구분하는 SH11~SH13 추가.
- 사건 시점 HP 동기화·파괴 시 해제·패널 전환 중 대상 교체·캐릭터 전용 칸 정리를 Phase 5 S20~S24에 반영.
- 문서만 변경. 코드/에셋 수정·Unity 검증 없음, 모든 Task 미착수 유지.

### 2026-11-01 — 선택·정보 7기준 리뷰 보완

- 사용자 "보완해": 표시 사실 읽기 계약(SH14), 갱신 조정자/해제 소유권(SH15), 공통 표시값/전용 그룹(SH16)을 구체화.
- 완료 목록↔재생 사실의 소비자 전환을 제거하고 ShownFacts 단일 reader로 정리. Display는 기존 완료 Refresh와 위임만 담당.
- Phase 5 S21a·S22a·S23a/S23b·S24a와 인수 기준 추가. 문서만 수정, 구현/Unity 검증 미실행.

### 2026-11-01 — 전체 구현과 코드 검토

- 사용자 지시: 모든 Task 진행 후 검토. 이번 실행에서는 Phase마다 보고 후 전체 구현을 이어 갔다. 아래 각 Phase의 기본 멈춤 규칙을 이 실행에 한해 사용자 지시로 대체했다.
- Phase 1: 외형 StructureHit 큐·규칙 추가, Sources는 조회만 수행하고 MomentCuePlayback이 키별 캐시 소유.
- Phase 2: 독립 숫자 재생 토큰/자연 완료 계약, Framework CustomPool 조립, 숫자 활성 대여·투영 대여·정리 구현. CustomPool에 파괴 API가 없어 Factory가 생성한 객체 목록의 파괴를 소유한다. 풀 내부를 새로 구현하지 않았다.
- HUD.prefab의 UnitDamageText가 HUD 루트에 있어 숫자만 복제할 수 없었다. 같은 컴포넌트 fileID와 시간/모양 값을 보존하며 DamageNumbers 자식으로 옮기고 두 TMP도 함께 옮겼다. 원래 UnitHUD의 직렬화 참조는 유지했다. 별도 duration/모양 설정 복사 없음.
- Phase 3: 전용 Structure 처리기·슬롯 연결. 기존 Facts 갈래 제거. 기존 공격 Gate/접촉 정책은 변경하지 않았다. 치명타도 Final 반영 전 몸 자리로 숫자·큐를 시작한다.
- Phase 4: Headquarters 외형에 기존 Hit 이펙트 연결(stayInPlace=true, 지연 0). DeckUI에 이름·Structure 설명/진영 텍스트 연결. prefab 로컬 fileID 중복 없음 확인.
- Phase 5: ShownFacts reader·선택 원본·순수 C# 정보 조정자·공통 표시값과 캐릭터 전용 그룹 구현. 공통 Render 완료 경계에서 정보/선택 갱신. 최신 Buffer HP fallback·새 감시 Tick 없음.

#### 실제 검증 증거

| 검증 | 결과 |
|---|---|
| VS 2022 MSBuild ProjectL.Systems | 오류 0. 기존 MainCharactorUI/CombatLogUI/DeckCharacterImagesSO의 CS0649 경고가 출력됨. 이번 변경 파일 경고 없음 |
| MSBuild ProjectL.CombatField.Editor.Tests | 오류/경고 0. 테스트 실행 결과와 구분 |
| 에디터 로그 | 변경 뒤 `Mono: successfully reloaded assembly`·LogAssemblyErrors 0ms 확인. 이는 Test Runner/Play 검증이 아님 |
| 관리 코드 테스트 직접 호출 6건 | 모두 통과: 비치명타/치명타/HP 동일 처리기 3건, 장애물 중복 방지 1건, Structure 정보 선택/빈 칸/국면 해제 1건, 파괴 대상 무효화 1건. NUnit Test Runner 실행은 아님. 로그 `Logs/structure-managed-checks.log` |
| prefab/meta/diff | 두 prefab 로컬 fileID 중복 없음, 새 .cs의 .meta 존재, 변경 diff 공백 검사 통과 |
| Unity 런타임 필요한 테스트 | 작성/컴파일만 완료. 에디터 밖 큐 생성 검증 시 Unity ECall 호출 불가를 확인해 해당 검증을 통과로 계산하지 않음 |
| Unity batchmode | 사용자가 열어 둔 프로젝트 인스턴스 때문에 거절됨. 기존 에디터를 종료하지 않음 |
| Unity UI 접근 | 자동 승인 검토에서 Unity 조작을 허용하지 않아 실행하지 못함. 우회 조작하지 않음 |

#### 남은 실제 실행 인수

- S4/S8a/S9/S14/S16/S17/S24/S24a: 새 StructureHitPresentationTests를 Unity EditMode Test Runner에서 실행하고 숫자 확대/페이드·치명타 잔류·풀/씬 수명·카메라 투영·정보창 전환/레이아웃을 재생 확인한다. S13은 관리 코드 처리기 검증만 통과했으며 큐/숫자 동시 표시 인수는 남았다.
- 기존 DataBaseAssetTests는 현행 트리에 없어 이름만으로 실행했다고 기록하지 않는다. 대신 `DatabaseAssets_AllUnitVisualCueInputsRemainValid`와 설정 에셋 교차 검증을 추가했으며 Unity 실행은 미확인이다.
- S18은 아래 별도 코드 리뷰 문서의 정적 검토를 마쳤지만, 공통 HUD/캐릭터 숫자/장애물/Contact 큐의 실제 회귀는 미완료라 체크를 남긴다.
- **모든 Task 검증 완료라고 판단하지 않는다.** 구현 체크와 미실행 검증 체크를 분리해 유지한다.
- 검토: [Structure 피격·선택 구현 리뷰](../specs/2026-11-01-combat-structure-hit-implementation-review.md). 외형/UI 담당 공유: [공유 메모](../handoffs/2026-11-01-structure-hit-view-share.md), 미전달.

### 2026-11-01 — CanvasGroup 오류 수정·Unity 직접 검증

- 사용자 보고: `DamageNumbers(Clone)`의 CanvasGroup 누락 예외. `ProjectionLease`의 `GetComponent<CanvasGroup>() ?? AddComponent`를 Unity Object의 `== null` 검사로 교체했다. 에디터의 미연결 Unity Object는 CLR null과 달라 기존 식이 컴포넌트 생성을 건너뛸 수 있었다. 새 타이머·Tick·설정값은 추가하지 않았다.
- 컴포넌트가 없는 숫자 루트의 투영 등록·CanvasGroup 생성·반복 해제를 검증하는 회귀 테스트 추가.
- 사용자 명시 요청으로 컴퓨터 사용 검증을 다시 진행했고 이번에는 UI 조작이 가능했다. Unity Test Runner에서 `ProjectL.CombatField.Editor.Tests` **47/47 통과**. 초기 실행 실패 중 에셋 검증은 이번 Structure 큐 계약으로 범위를 바로잡았고, EditMode의 비활성화 수명 테스트는 비활성 오브젝트의 SendMessage 대신 OnDisable을 명시 호출하도록 수정했다. 런타임 코드를 테스트 통과용으로 바꾸지 않았다.
- 실제 Play: 기존 개발 설정의 Test 스테이지(10000), 아군 배치→전투 시작→Structure 선택→와이엇 이동→본부 근접 공격 2회→Play 종료. 설정·씬을 저장하거나 변경하지 않았다.
- 본부 선택 시 HP 512/512·이름·적 진영 표시와 캐릭터 전용 칸 숨김 확인. 유닛으로 다시 선택하면 캐릭터 전용 칸 복원 확인.
- 본부 첫 피격 뒤 HP 505/512, 반복 피격 뒤 495/512 확인. 피격 이펙트와 독립 피해 숫자 `7`을 Game 확대·일시정지/프레임 진행으로 확인했고 재생 재개 후 숫자가 사라졌다. 런타임 Inspector에서 복제 위젯에 CanvasGroup이 생성되고 입력 차단 두 옵션이 꺼진 것도 확인했다.
- 검증 구간 Editor.log에 새 Exception/오류 보고 없음. 증거: `Logs/structure-unity-editmode-results.xml`, `Logs/structure-unity-play.log`(로컬 검증 산출물). 종료 후 EditMode와 기존 창 배치로 복원했다.
- 이번 실행은 근접·비치명타 사례다. 원거리 투사체 접촉의 프레임 대조, 치명타 숫자 잔류, 카메라 이동/씬 이탈 중 반환 및 장애물의 실제 회귀는 여전히 별도 인수로 남긴다. 관련 Task 전체를 일괄 완료로 변경하지 않는다.

### 후속 Phase A — 건물 오프셋·배치 조회

- [x] A1. 일반 Structure 오프셋 분리, 유닛/장애물/Rider 기존 값 유지.
- [x] A2. 배치 정보 조회 정책·선택·패널 확장. 배치 Drag 확정 클릭은 기존 입력 소유권 유지.
- [x] A3. 관련 컴파일·선택 회귀 검증. 실제 배치 입력/Deck 전환 검증은 아래 기록.
- [x] A4. Phase 검토·보고. SSOT/SOLID/FW/독립성/캡슐화/Unity 구조/불필요 Tick 점검.
- [x] A5. 사용자 확인한 Combat 최초 배치 진입의 Deck 목록 닫힘·버튼으로 열기 반영/검증.
- 이전 스테이지 내용은 유지하고 후속 변경을 자동 스테이징하지 않는다.

#### 후속 Phase A 실행·검토

- FieldUnitPresenter의 structureHudOffset 신설. 기존 호출이 알고 있는 캐릭터/Structure 구분을 HUD 조립에 전달하고 네이티브 장애물 오프셋은 보존했다. 값 조정은 Combat 씬의 FieldUnitPresenter Inspector에서 한다. Rider 추가 Y는 HUD.prefab의 기존 필드이며 변경하지 않았다.
- 기존 CanSelectUnit·SelectionState·CombatUnitInfo의 Deployment 조회 범위만 확장했다. RouteInput에서 진행 중인 Drag는 정보 선택을 건너뛰어 배치 확정을 유지한다. 공격 명령/미리보기 정책은 바꾸지 않았다.
- 사용자 4번 답변 "처음부터 닫기" 반영. Bootstrap의 OpenCombatUI 마지막 호출을 기존 UISystem.CloseChildUI(Deck)로 변경했다. 새 UI 등록/직접 SetActive 제어 없음.
- Systems MSBuild 오류 0(기존 CS0649 7건), 테스트 어셈블리 오류/경고 0. Unity EditMode 전투 View **53/53 통과**. 배치 선택·국면 변경 해제와 대기/모달/자동 처리 제한 회귀 포함. 로컬 증거 `Logs/structure-followup-build.log`, `Logs/structure-followup-editmode-results.xml`.
- Unity 직접 재생: 최초 Deck 닫힘→배치 중 적 캐릭터/본부 선택 정보→빈 칸 해제→Deck 버튼으로 목록 열기→메인 유닛 배치→아군 클릭 정보 표시→기존 집기/같은 칸 재배치→선택 HUD 복원 확인. 집은 동안 모델 HUD 숨김은 기존 배치 동작이다. Play 종료 후 편집 화면으로 복귀. 씬·DB 값 저장 없음. 로그 `Logs/structure-followup-play.log`.
- SSOT: 표시 HP/선택/입력 허가 원본 유지. SOLID/독립성: 기존 조회·표현·조립 경계를 확장하고 Core/카메라/Architecture 변경 없음. FW/캡슐화: 같은 UIId·부모·배타 정책과 UISystem 개폐, 외부 목록/내부 상태 노출 없음. C#/Unity: 직렬화 private offset과 사실 기반 분류. 새 If는 국면/Drag 입력 소유권과 HUD 조립 분기뿐이며 새 Tick·구독·폴링 없음.
- 새 오프셋의 임의 값 조정·Rider 개별 외형별 시각 검증 및 기존 원거리/치명타 잔여 인수는 이번 실행에 포함하지 않았다.
