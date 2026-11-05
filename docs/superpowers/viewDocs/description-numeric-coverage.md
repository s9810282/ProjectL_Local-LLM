# Description 수치 대응 범위와 남은 공개값

2026-11-02 작업 트리의 코드·등록 에셋 기준. **컴파일 미확인.** 이 목록은 정적 조사이며 Unity에서 모든 화면을 확인했다는 뜻이 아니다. 구현 원본은 기능별 토큰 등록이며, 이 문서는 인수·누락 추적용이다.

**후속 갱신:** `78fdc81`의 최종 `TraitSettingsSet`에 View를 연결하고 특성 33종·단일 수치 59개를 등록했다([전체 토큰 목록](description-token-guide.md#83-특성-토큰-이름과-필드)). SO 본문까지 이전한 것은 7종이다. 아래 특성의 ‘대기’는 본문 이전 대기이며 산탄 목록은 별도 표시 설계도 필요하다. 훈련/증강 결과·아이템·유물·잔여 환경 공개 대기는 유지한다.

## 1. 화면별 소비 경로

| 설명 | 소비 화면 / 변환 | 현재 상태 |
|---|---|---|
| 환경 | `CombatEnvironmentText` → `CombatEnvironmentDescriptions` | 기존 공개값을 명명 토큰으로 통합. 고정된 잔여 규칙값은 §3 |
| 특성 | `UnitCardUI`, `DeckSlotInfoUI`, `CombatInspectionInfoMapper` → `UnitCardTraitContent` → `TraitDescriptionFormatter` | 최종 `TraitSpec`의 33종·59개 토큰 등록. 보유/덱은 같은 조회의 TraitSettings, 전투/손패는 해당 전투 설정. SO 7종 이전 |
| 상태 효과 | `CombatUnitStatuses` → `CombatEffectFormatters` / `CombatStatusDescriptionFormatter` | 기존 positional formatter와 출처·수명·잔량 유지 |
| 아이템 | `ItemShopOfferContentBuilder`, `PackUI`, `FieldUnitPresenter.ItemStatus` | 기존 문자열 유지. 세 화면에 공통 설정값을 전달하는 Core 계약 대기 |
| 유물 | `ArtifactBoardRefresher` → 목록/상세 | 기존 View 문구 유지. 보유·배치 양쪽의 효과 설정 계약 대기 |
| 훈련 | `TrainingCardContentBuilder` | 현재/차이/다음 비교는 이미 동적. 본문은 Core 문자열이라 결과 설정 공개 대기 |
| 증강 | `TraitGrowthPopupModelBuilder` | `choice.Description` 유지. 획득/강화 결과의 설정 공개 대기 |
| 옛 강화 상점 | `EnhancementShopOfferContentBuilder` | builder는 남았으나 현재 운영 상품 생성 없음. 강화 스테이지는 Training으로 진입. 이 작업으로 활성화하지 않음 |
| 회복 | `RecoveryPopupModelBuilder` → `RecoveryDescriptionBindings` | 정량/비율/최대 체력 설정의 본문 토큰과 효과별 예상량 연결 |
| 유닛/구조물 | `CombatInspectionInfoMapper` | 일반 유닛 Description은 빈 문구 중심. native 화약통·보급품은 상세 검사 대상에서 제외돼 환경 호버만 사용 |
| 화면 바깥 글 | `StageOfferTextData` | 가격·골드·완성된 설명의 positional 레이아웃 유지. 회복 최대 체력 예상량 줄 추가 |
| 옛 설명 | `DescriptionMaster`, `UnitCardVisualsSO.SynergyDescription/FunctionDescription` | 선언/테이블 외 운영 소비 미발견. 삭제·이전·활성화하지 않음 |

새 토큰은 해당 소비 경로의 값 연결과 함께 추가해야 한다. 미전환 경로의 원문에 토큰만 넣어서는 안 된다. 현재 특성/회복/환경의 명명 토큰 경로는 미등록 토큰을 오류로 보고한다.

## 2. 특성별 분류

`ViewGroup/TraitVisual`의 에셋 42개 중 41개가 Table에 등록되어 있다. 등록 여부와 실행 지원 여부는 다르다. 사용 가능 여부의 원본은 `DeferredCombatFeatures`이며 View는 snapshot의 사유를 표시한다. 아래에서 ‘대기’는 현재 문구를 기본 숫자로 다시 고치지 않았다는 뜻이다.

| 특성 키 (다른 에셋 이름) | 본문 상태 | 수치/조건 또는 남은 계약 |
|---|---|---|
| ThornField | 토큰 이전, 실행 보류 안내 유지 | 피해·설정 수명·조건부 방어 감소. 현재 진영 턴 규칙에서는 DeferredUnitTurnTiming |
| Pull | 토큰 이전 | 추가 사거리. 덱/보유/손패/전투 공용 |
| HuntersMark | 토큰 이전 | 증가율·대상 진영 종료 횟수. 별도 중복 수치 줄 제거 |
| ReconSquad | 토큰 이전 | 증가율·직교 반경. 훈련 본문은 별도 공개값 대기 |
| ImpactBolt | 토큰 이전 | 밀치기 거리. 보유/덱 경로에 누락됐던 설정 연결 |
| Intimidation | 토큰 이전 | 기존 코드 수치 줄을 SO로 이전. 보유/전투 모두 공격 감소율 표시 |
| Regeneration | 토큰 이전 | 기존 코드 수치 줄을 SO로 이전. 보유/전투 모두 회복량·자기 진영 종료 주기 표시 |
| Knockback | 대기 | 보유/전투 거리 공개됨 |
| DistanceDamageScale (Shotgun) | 대기 | 보유/전투 거리별 비율 목록 공개됨. 목록 표시 방식과 거리별 범위 설명 필요 |
| PiercingFalloff | 대기 | 보유/전투 대상별 감소율·방향별 대상 수 상한 공개됨 |
| Necromancy | 실행 보류, 대기 | 보유/전투 소환수 한도·첫 행동 지연·소환 유닛 키 공개됨. 범위 문구·소환 이름 연결은 별도 확인 |
| Roar | 대기 | 보유/전투 피격 한도 공개됨. 누적 횟수와 구분. 밀치기 거리는 별도 넉백 설정 |
| Focus | 실행 보류, 대기 | 현재 중첩/예약 상태를 중첩당 추가 피해량으로 대용하지 않음 |
| ArmorBreaker | 대기 | 타격별 남은 방어력 감소율 |
| FinishingShot | 대기 | 공격력 대비 처치 임계율. View 20%와 성장 원문 15%를 임의 통일하지 않음 |
| FocusedBombardment | 대기 | 피해 증가율·대상 수 조건. View 50%와 성장 원문 70% |
| Hunter | 대기 | 표적 처치 보상량·스테이지 한도. 성공 보상 횟수와 설정량 분리 |
| StationaryFire | 대기 | 자발 이동 조건의 피해 증가율 |
| TrackingShot | 대기 | 대상 현재 체력 임계율·피해 증가율 |
| AnkleBreaker | 대기 | 이동 봉쇄 지속 단위/횟수 |
| CallOfBlood | 대기 | 적 최대 체력 대비 회복률 |
| CleanSweep | 대기 | 공격 증가량·지속 단위/횟수 |
| Disarm | 대기 | 공격 봉쇄 지속 단위/횟수 |
| MoraleBoost | 대기 | 적 기본 공격력 대비 증가율·지속 단위/횟수. View 20%와 성장 10% |
| SoloAction | 대기 | 주변 조건 반경·필요 점유 칸 수·보호막 설정량·초기화 주기 |
| Vanguard | 대기 | 공격 증가량·지속 단위/횟수 |
| Cohesion | 대기 | 아군 탐색 반경·아군당 방어력/보호막 |
| Entrenchment | 대기 | 반경·피해 감소율 |
| PositionSwap | 정책 문구, 설정 대기 | 현재 본문에 숫자 없음. `PositionSwapTraitSpec.ShieldAmount`가 있으므로 강화/훈련의 보호막 설명은 미완료 |
| Sacrifice | 대기 | 반경·상대 턴 발동 한도 |
| Taunt | 대기 | 반경. 등급별 피해 감소 설정도 본문에 필요 |
| Acceleration | 대기 | 이동 칸당 공격 증가량. 보유 보너스 상태를 설정으로 대용하지 않음 |
| DustScreen | 대기 | 사거리 감소·피해 감소·수명. 흔적 호버 토큰과 특성 설정은 별개 |
| GroundCompaction | 대기 | 이동 범위 보너스·흔적 수명 |
| KillChainAttack (KillChainField) | 고정 규칙 | `KillChainAttackTraitSpec`은 설정값 없이 임의의 적 캐릭터를 추가 공격하는 계약. ‘하나’는 수치 설정의 복제 아님 |
| Leap | 수치 없는 정책 | `LeapTraitSpec`에 수치 설정 없음 |
| SteadfastWill | 수치 없는 정책 | `SteadfastWillTraitSpec`에 수치 설정 없음 |
| DoorBreaching | 퇴역 | Table에는 남음. 가용성 `Retired`이면 원문 대신 퇴역 안내 |
| DoubleShot | 퇴역 | 위와 동일. 8칸/1회 문구를 현행 기능으로 활성화하지 않음 |
| LuckyBullet | 퇴역 | 위와 동일 |
| ChainExplosion | 퇴역 | 위와 동일 |
| MoveAttackDiscount | 퇴역·미등록 | SO는 보존되지만 Table에 없음 |
| ContemptForWeak / PrecisionShot | 퇴역 예약 키 | 해당 외형 에셋 미발견. 키를 재사용하거나 외형을 새로 등록하지 않음 |

미지원 가시밭도 기존에 제공되던 설정 설명은 표시하되, 사용 불가 안내를 함께 유지한다. 특성 수치가 표시된다는 이유로 Core 동작이 활성화된 것은 아니다.

## 3. 환경과 상태 효과

지뢰·깃발·약한 지반·칸 보정·먼지 흔적·화약통·보급품·토네이도·낙석의 연결 토큰은 [작성 안내](description-token-guide.md)에 있다. 전환된 수치 이외에도 다음이 남는다.

- 고지대 피해 감소율, 모래폭풍 사거리 감소/하한·피해 감소율은 `StaticTerrainFact`에 미공개다. View가 `EnvironmentTerrainRules`의 Domain 상수를 직접 참조하지 않는다.
- 지뢰의 주변 피해/범위, 화약통 범위 등 본문의 부가 조건은 공개 계약과 문구 의미를 대조해야 한다. 피해 토큰 하나의 연결로 전체 환경 완료라고 표시하지 않는다.
- 벽·절벽·보안관 배지·하붑은 현재 문구를 보존했다. 설정값이 추가되는 후속 작업은 실제 공개값과 함께 토큰을 등록한다.
- 상태 효과의 현재량·잔량·출처·수명은 기존 `UnitStatusVisualData.DescribeBody`와 formatter의 책임이다. 이번 변경은 해당 positional 형식이나 수명 계산에 닿지 않았다.

## 4. Core 공개 계약이 필요한 경계

| 경계 | 현재 읽을 수 있는 것 | 부족한 것 |
|---|---|---|
| 보유/전투 특성 | 보유 `TraitSettings`와 전투 `TraitSettings.For(character)`로 View 연결, 단일 수치 59개 등록 | 7종 외 SO 본문 이전 필요. 목록/문자열은 별도 표시 설계 |
| 훈련 | `TraitRankValues.Amount/SecondAmount`, 현재/다음 등급 | 본문이 요구하는 반경/수명/조건을 포함한 현재·다음 결과 설정. ReconSquad의 Amount만으로 Radius를 알 수 없음 |
| 증강의 획득·강화 | 키/이름/원문 문자열 | 후보를 선택한 결과의 설정, 추가/교체될 특성. 옛 강화 상점 상품은 비운영 |
| 아이템 | `ItemInventorySnapshot.Counts`, 장착 키, `ShopPurchaseSnapshot.ItemDescription` | Rope 우선도 감소, Lantern 방어 감소, Whiskey 임계율/회복률, Roket 임계율/피해 감소율. 세 화면의 같은 정의 |
| 유물 | Buffer의 ID/InstanceId/배치/점유 칸 | 보유·배치 유물의 효과 설정과 조건. `ArtifactPlacedEffectSnapshot`은 존재하나 `ArtifactLoadoutReader`가 Core 내부에서 InGame DB를 결합해 만든 전투 입력이므로 View 공개 경로의 대용이 아님 |
| 환경 | 위치/종류·일부 실제 수치 | 위 §3의 남은 규칙 수치 |

정식 요청은 [Core 인계 §64](../handoffs/2026-09-24-view-core-data-boundary-handoff.md#64-게임-description-수치-공개-요청-2026-11-02-설계안미구현미전달)다. 문자열에서 숫자를 역으로 파싱하거나, View에서 기본값·등급 계산을 복제해서 채우지 않는다.

후속 [Core 수정 범위 분석](../specs/2026-11-02-description-core-scope-analysis.md)에서 운영 경로를 재확인했다. 옛 강화 상품은 생성되지 않고, Rope는 현재 사용 보류다. 지뢰의 ‘주변 절반 피해’ 문구는 Core의 동일 피해 요청과 불일치하므로 별도 결정 대상이며, 토큰 연결만으로 완료 처리하지 않는다.

## 5. 회복과 검증

정량 회복은 `Option.Heal.Value`, 비율 회복은 같은 필드의 비율 의미를 읽는다. 최대 체력 증가는 `Option.MaxHealthIncrease`다. 본문의 설정량과 `ProjectedHealAmount`/`ProjectedMaxHealthIncrease`는 별개의 토큰이며, 예상량은 Core가 계산한 값을 그대로 표시한다. Full 모드는 숫자 설정이 없으며 예상 회복량만 공개한다.

사용자 Unity 확인: 같은 정의의 서로 다른 강화/등급 개체, 덱→손패→전투→복귀의 7종 특성 수치, 가시밭 추가 방어 감소/사용 불가 안내, 새 토큰을 작성한 특성의 값과 단위, 연출 중 선택 정보와 재입장 수명, 환경 호버, 정량/비율/전체 회복/최대 체력 증가, 효과 없음/골드 부족/구매 완료, Rich Text/줄바꿈, 기존 `DataBaseAssetTests`. 이번 작업에서 Test 코드·MSBuild·Unity 실행은 하지 않았다.
