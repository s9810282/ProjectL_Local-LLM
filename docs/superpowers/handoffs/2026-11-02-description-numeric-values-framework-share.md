# 환경·특성·회복 Description 토큰 조립 공유 — 2026-11-02

상태: **Phase A·D 회복, B 특성 33종·59개 토큰 등록/읽기 연결 반영. SO 본문은 7종 이전. 컴파일 미확인, 미전달.** Core 직접 수정은 하지 않았다. 전체 범위와 대기 항목은 [계획](../plans/2026-11-02-description-numeric-values-plan.md)을 따른다.

## 변경한 조립

- `Factory/Runtime/InGame/View/CombatSceneInputFactory.CreateEnvironmentHover`에서 `CombatEnvironmentDescriptionFactory.Create(tables)`의 결과를 `CombatEnvironmentText`에 전달한다. `CreateEnvironmentHover`의 외부 서명은 유지한다.
- 신규 `CombatEnvironmentDescriptionFactory`는 기존 `CombatDisplayTables`의 View 외형과 기능별 토큰 등록을 연결한다. 등록된 설명은 생성 시 검사하고, 수치는 호버의 현재 공개 사실에서 읽는다.
- `CombatEnvironmentText`의 내부 생성자에 `CombatEnvironmentDescriptions` 인자를 추가했다. 운영 직접 생성자는 위 Factory 한 곳이고, `Assets/Tests`의 직접/문자열 참조는 검색되지 않아 시험 파일을 변경하지 않았다.

## 등록·수명 경계

| 점검 | 결과 |
|---|---|
| 생성 | `CombatSceneInputFactory` → `CombatEnvironmentDescriptionFactory` → 검사한 설명 틀 묶음 |
| 내부 보관 | `CombatEnvironmentText` → `CombatEnvironmentDescriptions`의 불변 연결·틀 |
| 상위 등록/Export | 없음. 서비스/모듈이 아닌 기존 입력 기능의 로컬 부품 |
| 루트 Builder | 변경 없음. Global 하위 서비스 재등록 없음 |
| 소비 | 기존 `CombatSceneInput` → `CombatEnvironmentHover.Refresh` → `CombatEnvironmentText.Build` |
| 해제 | 기존 환경 호버 Clear/씬 입력 해제 유지. 신규 이벤트·Unity 자원·독립 UI 없음 |

공통 `RuntimeSystem/Common/Descriptions`는 Systems 안의 문자열 변환 부품이다. Framework 및 Core를 참조하지 않으며 새 서비스 키·asmdef·입력/렌더/UI 등록은 추가하지 않았다. 실제 값 연결만 View의 환경 기능에 있다.

기존 `{n}` 별칭을 유지하면서 환경 SO 6개의 이름을 명명 토큰으로 이전했다. 소비하는 숫자와 환경 공개/중복 문구/범위 정책은 유지한다. 작성 방법과 실제 등록 위치는 [토큰 작성 안내](../viewDocs/description-token-guide.md)를 따른다.

SSOT·SOLID 5원칙·JMH/FW 검토와 사용자 Unity 확인 항목은 [계획 §9](../plans/2026-11-02-description-numeric-values-plan.md#9-phase-a-실행-기록-2026-11-02)에 기록했다. Test 코드·MSBuild·Unity 실행·커밋·push·외부 발송은 하지 않았다.

## 특성·회복의 후속 연결

| 점검 | 특성 | 회복 |
|---|---|---|
| 조립/등록 | `TraitDescriptionBindings`의 불변 등록 → 종류별 `TraitDescriptionBinding`. 공통 parser는 종류를 모름 | `RecoveryDescriptionBindings`의 효과/회복 방식별 등록 |
| 호출/생성 | `UnitCardTraitContent`의 기존 콘텐츠 생성 때 View SO와 같은 표시 snapshot으로 틀 검사·문자열 생성 | `RecoveryPopupModelBuilder.Build`의 기존 선택지 갱신 때 틀 검사·문자열 생성 |
| 내부 보관 | 개체 수치/완성 문자열의 전역 저장 없음 | 기존 팝업 모델만 최종 문구 보관 |
| 소비 | UnitCardUI(보유/전투 손패), DeckSlotInfoUI, CombatInspectionInfoMapper | 기존 StageOfferPopup |
| 상위 Export/루트 Builder | 추가 없음 | 추가 없음 |
| UI/입력/해제 | 기존 등록·열기·닫기·선택·호버 수명 유지 | 기존 등록·완료·재사용·종료 수명 유지 |

`DeckEditCardModel`은 같은 읽기의 `DescriptionSource`를 전달한다. 기존 6개 기본 생성자 인자는 유지했고 추가 설정 인자를 묶었다. 외형 Data에는 가시밭의 조건부 문장 필드만 추가했다. 회복 레이아웃 enum의 기존 번호는 유지하고 15번 `RecoveryMaxHealthProjected`를 추가했다. 씬/프리팹 재연결 없음.

## 최종 특성 설정으로 이전한 연결

`78fdc81` 이후 특성 등록은 `DescriptionToken<TraitSpec>`을 사용한다. 보유/덱은 기존 동일 조회의 `TraitSettings`, 손패/카드는 해당 전투의 `TraitSettings.For(character)`를 전달한다. 옛 특성별 설명 속성 소비와 상태 snapshot의 설정 수치 읽기를 제거했다. 위압/재생은 같은 코드 수치 줄을 SO로 옮겼다.

전투 상세의 생성 주체는 기존 `CombatSceneDisplayFactory`이며 조립 변경은 없다. `CombatSceneDisplay.RefreshCore` → `CombatInspectionPresenter.Synchronize`로 같은 표시의 `BattleTraitSettings` 참조를 전달한다. Presenter는 연출 중 표시 사실에 맞는 캐릭터 설정을 선택해 Mapper에 넘기고 Clear/Release에서 참조를 해제한다. 전역 상태·완성 문자열 캐시·별도 서비스·상위 Export·루트 등록·새 UI/입력/이벤트 수명은 추가하지 않았다.

특성 문구 변경은 해당 SO, 새 단일 수치는 해당 typed 등록에서 다룬다. 공통 parser·환경·회복은 수정하지 않았다. 등록된 59개 값과 SO 7개 토큰/테이블을 정적으로 대조했으며 사용자 Unity 확인 전이다. **컴파일 미확인.**
