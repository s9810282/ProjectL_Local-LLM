# Colored 5종 전투 시험 계획

- 작성자: 호진(이호진)
- 상태: C1~C4 완료(2026-10-18). 이어서 사용자 지시로 실험 코드·테스트 SO를 지우고 문서와 모델·프리팹만 남겼다(사용자 커밋 `bf4ff76b`). **이 시험 장면은 지금 저장소에서 바로 재현되지 않는다.** 2026-10-19에 카우보이·쉴더 운영 SO를 만들었다(§3.8).
- 설계: 따로 없다. 규칙 원본은 쉴더 설계(git 기록 `2026-10-15-shielder-intimidation-design.md`)·카우보이 설계(git 기록 `2026-10-16-cowboy-frenzy-design.md`)·[쉴더 위압 View 인계](../handoffs/2026-10-15-shielder-intimidation-view-handoff.md)다.
- 정리: 2026-10-27 1부(시험 계획)·2부(특성·HUD 검증 기록)·3부(카우보이·쉴더 DB 준비)를 결과 요약으로 줄였다. 옛 중복 원본 `plans/2026-10-18-colored-five-combat-test-plan.md`는 1부와 같아 따로 옮길 것이 없었다. Task 단계·시험 수치·XML 해시·커밋 대상 목록은 git `10914570`.

## 1 진행 요약

| 작업 | 내용 | 상태 | 확인 | 커밋 |
|---|---|---|---|---|
| C1 모델·프리팹 | Colored FBX 5개·공용 PNG·재질 하나, 프리팹 wrapper 5개 | 완료 2026-10-18 | 작업 세션이 Unity 에디터에서 프리팹 검사, 원본 UV 한계(§3.2) | `bf4ff76b`(남김) |
| C2 HUD 표시 | 위압·광분 디버프 formatter 2개, Presenter 연결 9줄 | 완료 2026-10-18 | 합성 사실로 표시 연결만 확인 | 지움 |
| C3 식별자·테스트 DB | `EntityKey.Unit_Shielder = 23`, 별도 테스트 Catalog·SO 30개 | 완료 2026-10-18 | 공개 준비 경로로 5종 손패 확인 | 지움 |
| C4 개발 진입·특성 확인 | 개발 프리셋·추가 획득 도우미·Play From Boot 설정, 5종 특성 실제 명령 확인 | 완료 2026-10-18 | 특성 5종 확인(§3.6), HUD는 아이콘만 | 지움 |
| 정리 R1~R3 | 코드·테스트 SO 97파일 지움, 기존 소스 5개 되돌림, 모델 29파일 남김 | 완료 2026-10-18 | 남긴 프리팹 검사 통과 | `bf4ff76b` |
| 카우보이·쉴더 DB 준비 | 운영 폴더에 Unit·지정 방식·외형 SO | 완료 2026-10-19 | Unity 시험 미실행 | `4a6be1ba` |

## 2 남은 일·지금 상태

지금 저장소에 남은 것과 그 뒤 바뀐 것이다(2026-10-27 코드 대조).

| 항목 | 지금 상태 |
|---|---|
| Colored 모델·재질·텍스처 | 민형님 리소스 폴더 정리(`cb54d5d8`, 2026-10-18)로 옮겨졌다. FBX는 `Assets/ProjectL/Resources/Characters/Colored/Models/Playerble/`, PNG·재질은 같은 `Colored/Textures/`·`Colored/Materials/` |
| Colored 프리팹 5개 | 같은 정리로 `Assets/ProjectL/Prefabs/Characters/Colored/`. DB에 자동 등록하지 않았다 |
| `EntityKey.Unit_Shielder = 23` | C3에서 넣었다가 정리 때 뺐다. 지금은 천명님 `f38de8d4`(2026-10-18)가 넣은 줄이 있다 |
| 카우보이 | `Unit_Cowboy`가 운영 `UnitDataTable`·`PlayerLoadout_1`·`UnitRoster_1`에, `UnitVisual_Cowboy`가 외형 표에 들어갔다(2026-10-20 용근님 `4ca929a3`·`7949eaa7`, 천명님 `8a4350e6`). 광분 특성은 천명님 `4bd6b574`(2026-10-26)가 지웠고, 지금 카우보이 특성은 끌어오기(Pull)다 |
| 쉴더 | `Unit_Shielder`·`UnitVisual_Shielder`(프리팹 비어 있음)·`TargetingMethod_Shielder`는 있지만 Unit 표·외형 표에 없다. `TraitVisual_Intimidation`은 특성 외형 표에 있다 |
| 디버프 HUD 표시(위압·광분) | formatter와 Presenter 연결을 지웠다. 위압 HUD 표시는 다시 만들어야 한다 |
| 테스트 Catalog·개발 프리셋 | 지웠다. 같은 장면을 다시 만들려면 §3.1 조건과 별도 Catalog 경계를 되살리고 지금 Core에 맞춰 다시 확인한다 |

남은 일:

- [ ] 쉴더를 실제 플레이에 넣으려면 유효한 프리팹을 `UnitVisual_Shielder`에 연결한 뒤 Unit 표·외형 표에 등록하고 진입·획득 경로를 정한다. 빈 프리팹을 운영 표에 넣거나 필수 프리팹 검사를 느슨하게 하지 않는다.
- [ ] 쉴더 위압의 디버프 HUD 표시(아이콘·설명)를 다시 정한다. 저장된 Combat `HUD.prefab`에는 설명 패널 참조가 없다(§3.3).
- [ ] 아트 한계: Akimbo·Cowboy 원본 UV 일부가 아틀라스의 빈 흰 영역을 가리킨다. Cactus 원본에는 따로 움직일 대포 메시가 없다. 고칠지는 따로 정한다.
- [ ] 3부 SO의 Unity 에셋 로드·`DataBaseAssetTests`는 돌려 보지 않았다(§3.8).

## 3 작업별 결과

### 3.1 시험 구성(당시 값)

| 모델 | 실제 키 | 특성 | 값 |
|---|---|---|---|
| Akimbo | MainCharacter 14(메인) | 난사 `KillChainAttack` | 운영 `Unit_MainCharacter` 그대로(HP50·공격1·사거리1) |
| Shotgun | ShotGun 11 | 산탄 `DistanceDamageScale` | 운영 그대로(150%·50%) |
| Cactuscannon | CactusCannon 16 | 가시밭 `ThornField` | 운영 그대로(공격15·사거리4·가시5·수명5) |
| Cowboy | Cowboy 19 | 광분 `Frenzy`만 | 승인값 HP25·공격10·우선도15·Delay10·사거리1·이동범위99·이동비용2·공격비용4·Rider·감소5% |
| Parasol | Shielder 23 | 위압 `Intimidation` | 승인값 HP20·공격15·우선도9·Delay50·사거리1·이동범위3·이동비용1·공격비용4·Heavy·CardinalLine/Single/None, 위압 20%·우선도 감소3, 보호 범위 x=-2..2·y=1..2(PPT 24쪽, 보드 +Y) |

- 로스터: 테스트 로드아웃 9001(메인 + Shotgun·Cowboy·Shielder, 시작 손패 5) → `StartNewRun(9001)` → `Characters.AcquireSub(Cactus16)` 한 번 → `EnterChapter(1)` → 노드 2 전투. 첫 로스터는 서브가 정확히 셋이어야 해서 넷째를 공개 획득으로 더했다.
- 필드: 9×9, 양쪽 배치 깊이 2, 예비칸 5(운영 4로는 다섯 장 준비가 막힌다). 맵: map1 복사, depth3·width1·seed42 고정.
- 적: 약한 13(HP1·공격1·우선도10)을 (2,7)·(4,7)·(6,7), 튼튼한 17(HP100·공격10·우선도20)을 (3,7)·(5,7). 자발적 이동 없음, 적 본진 (4,8). 처음 계획한 y6은 배치 깊이 2와 맞지 않아 사용자 승인으로 y7로 바꿨다.
- 운영 Catalog·로드아웃 1·운영 능력치는 바꾸지 않았다. 전투 정의는 Database를 썼다(`DevBattleInitializationSource`는 특성 내용이 옛것이라 쓰지 않음).

### 3.2 C1 모델·프리팹

- 한 것: FBX 5개와 공용 PNG를 가져오고 URP/Lit 공용 재질 하나와 프리팹 wrapper 5개를 만들었다(카메라·조명·애니메이션 가져오기 끔, 레이어 8, BoxCollider, 높이 2, 발 y0). Parasol·Cactus는 yaw 180으로 +Z를 보게 했다.
- 한계: Akimbo·Cowboy 일부 메시의 원본 UV가 빈 흰 영역이나 (0,0)을 가리켜 참고 이미지 색과 다르다. UV·아틀라스·메시를 고치지 않았다. Cactus는 대포 메시가 없어 빈 `MortarPivot`·`Muzzle`만 연결했다. Cactus FBX 가져오기에 중복 메시 이름 경고가 있다.

<a id="phase-c2--hud-implementation-and-isolated-presentation-verification-si5-first"></a>

### 3.3 C2 HUD 표시(SI5 먼저)

- 한 것: `CombatDebuffStatusFormatter.Create(CharacterDebuffState, Sprite) → UnitHUDStatus?`(키 `debuff:Intimidation`)와 `CombatFrenzyStatusFormatter`(키 `debuff:Frenzy`), 기존 `FieldUnitPresenter.RenderUnitHud`에 9줄. 기존 `UnitHUD`·프리팹·입력은 고치지 않았다. 기록은 [SI5 인계 §8](../handoffs/2026-10-15-shielder-intimidation-view-handoff.md#8-si5-execution-record--2026-10-18).
- 확인 수준: 시험 안에서 합성 디버프 사실을 넣어 표시·고정·재사용·제거만 확인했다. 실제 공격이 상태를 만드는지는 C4에서 봤다.
- 발견: 저장된 Combat `HUD.prefab`의 `UnitHUD.descriptionPanel`·`descriptionText`가 비어 있다.
- 지금: 정리 때 둘 다 지웠다.

### 3.4 C3 식별자·테스트 DB

- 한 것: 사용자 승인으로 Core 파일 `EntityKey.cs`에 `Unit_Shielder = 23` 한 줄만 더했다(특성·로스터·Buffer 규칙 변경 없음). 별도 폴더 `DataBaseSO/Tests/ColoredCombat/`에 바뀐 그룹·표·항목만 복제한 테스트 Catalog를 만들었다(운영 Main·Shotgun·Cactus SO는 참조만).
- 확인: `DataBaseFactory.Create(testCatalog)` → 공개 준비 경로로 메인 1·서브 4, 손패 14·11·16·19·23, AP6, 예비칸 5, 적 위치, 시작1 → 전투2 → 보스3 그래프. Buffer에 직접 쓰지 않았다.
- 기존 `DataBaseAssetTests` 실패 3건(운영 Stage1101 환경 ID 213 추가, Stage2101 철도 0칸, `LineShader.mat` 없음)은 운영 데이터 문제라 고치지 않았다.
- 지금: 식별자 줄과 테스트 DB 모두 정리 때 지웠다(식별자는 천명님 줄이 다시 있다, §2).

### 3.5 C4 개발 진입

- 한 것: 개발 프리셋 `DevCombatTestPresetSO`, 공개 획득 도우미 `DevCombatRosterSetup.AcquireAdditional`, `GameBootstrap`·`GameBootstrapEditorOverrides`·Play From Boot 설정 창의 프리셋 칸(EditorPrefs GUID). 프리셋 Catalog를 DB 생성 전에 고른다.
- 사용자 결정(2026-10-18): **HUD 파일은 고치지 않고 아이콘만 확인한다.** 설명 패널·툴팁·고정 입력은 이 시험 범위에서 뺐다(완료가 아니다).
- 실제 씬에서 찾아 테스트 Catalog 안에서만 고친 것: 빈 환경인데 운영 Stage1101의 오브젝트 회전 101·102·103을 참조해 `FieldObjectPresenter`가 거절함 → 테스트 전용 환경 외형 표, 한국어 표시 이름이 UnitCard `LiberationSans`에 없어 네모로 나옴 → 테스트 이름을 영문 모델명으로.
- 확인 방법: 저장 DB + 실제 Core 서비스로 공개 명령을 낸 EditMode 시험, 실제 Boot → Combat 네 번 재생(5종 동시 배치·아이콘, 재진입 중복 획득 없음, 광분 아이콘 생성·제거, 선인장 발사·가시·접촉·만료, 프리셋 비우면 일반 로비). **마우스로 모든 공격을 조작한 확인은 아니다.**
- 검증 뒤 로컬 설정(override·프리셋 GUID·Play From Boot)을 꺼진 상태로 되돌렸다.

<a id="2부-colored-5종-특성hud-검증-기록"></a>

### 3.6 특성 확인 결과

새 특성 계산 코드 없이 기존 Core 계산·스케줄·게시 경로를 썼다.

| 외형 / 키 | 특성 | 관찰값 |
|---|---|---|
| Akimbo / 14 | 난사 | HP1 표적 처치 뒤 다른 적에 무료 추가타(공격력 1). 추가타 수만큼 AP를 더 쓰지 않았다(이동 뒤 4 → 공격 뒤 3) |
| Shotgun / 11 | 산탄 | 같은 HP100 표적에 거리 2에서 3, 거리 1에서 9 피해(공격력 6 × 50%·150%) |
| Parasol / 23 | 위압 | 첫 타 공격력 20% 감소, 둘째 타부터 우선도 3 감소(겹치지 않음). 우선도 20 → 17. 표적 AI 공격 피해 10 → 8, 공격 뒤 효과가 지워지고 우선도 20으로 돌아옴 |
| Cowboy / 19 | 광분 | 직접 처치 → 파동 1, 파동 처치 → 파동 2. 두 번 맞아 누적 10%, 우선도 20 → 18. 피격자 다음 턴 시작에 상태·아이콘 제거 |
| Cactuscannon / 16 | 가시밭 | 공격 확정 뒤 피해 5·수명 5 가시밭. 접촉 HP 20 → 15와 칸 소모, 다른 칸은 완료된 유닛 턴마다 4 → 3 → 2 → 1 → 제거 |

뜻 정리: 광분 5%는 **피격자의 우선도 감소율**이다(명중·공격력·AP가 아님). 위압은 공격력 20% 감소와 별도의 **우선도 정수 3 감소**다. 가시밭 수명 5는 선인장 자신의 5턴이 아니라 Core가 완료로 센 유닛 턴 5회다.

<a id="13-모델-보존코드so-정리-실행-기록"></a>

### 3.7 정리(모델 보존·코드/SO 삭제)

- 사용자 지시(2026-10-18): 문서 4개와 Colored 모델·텍스처·재질·프리팹을 남기고, 이번 실험 코드·테스트 SO를 지운다.
- 지운 것: 새 코드·시험 11개(+ `.meta`: formatter 둘, Dev 두 파일, 시험 일곱 — `ColoredCombatAssetTests`·`ColoredCombatPresetTests`·`ColoredCombatLiveTests`·`ColoredCombatBootSmokeTests`·`CombatDebuffHudTestFixture`·`IntimidationStatusDisplayTests`·`FrenzyStatusDisplayTests`), 테스트 SO 33개와 폴더 `.meta`(빈 상위 `DataBaseSO/Tests/`도), 모두 97파일.
- 되돌린 것(검토한 Colored 부분만): `FieldUnitPresenter.cs`(9줄), `GameBootstrap.cs`, `GameBootstrapEditorOverrides.cs`, `PlayFromBootSettingsWindow.cs`, `EntityKey.cs`(`Unit_Shielder` 줄).
- 남긴 것: FBX 5·PNG 1·재질 1·프리팹 5와 파일·폴더 `.meta` 17, 모두 29파일(지금 위치는 §2).
- 확인: 남긴 프리팹 5개 검사·`UnitHUDTests` 통과, `DataBaseAssetTests`는 §3.4의 기존 실패 3건 그대로. 같은 때 돌린 일반 Boot 시험 `LobbyBootSmokeTests.BootToLobby_ShowsSpaceWithoutStartingRun`은 1920×1080에서 7번 캐릭터 표시와 3D FacilityLabel 겹침으로 실패했다(이 정리와 무관, 다른 작업자 영역이라 고치지 않음).
- 전달 뜻: 이 기록은 "기존 Core의 5종 특성을 별도 시험 구성에서 확인했다"는 것이다. 운영 캐릭터 등록·새 HUD·Framework 변경을 전달한 것이 아니다.

### 3.8 카우보이·쉴더 DB 준비(2026-10-19)

- 사용자 요청으로 운영 폴더에 SO를 만들고 참조를 이었다. 모델·임시 프리팹을 연결하지 않는다는 앞선 요청은 유지했다.

| 항목 | 당시 상태 |
|---|---|
| `Unit_Cowboy` | 기존 `TargetingMethod_1`, 광분 5% |
| `Unit_Shielder` | 새 `TargetingMethod_Shielder`(키 6, CardinalLine/Single/None, ChoiceAngle.Unrestricted, 지정 방식 표에 등록), 위압 |
| 위압·광분 TraitVisual | 특성 외형 표에 등록, 아이콘·설명 비어 있음 |
| UnitVisual 둘 | 공통 근접 공격·기본 피격 연출, 프리팹·아이콘 비어 있음 |

- 값 출처: 카우보이 설계 확정값, 쉴더 설계의 HP20·공격15·우선도9·Delay50·이동비용1·공격비용4, 이동범위 3과 보호 범위 10칸은 §3.1 구성. 기본 방어력은 운영 SO와 `DataBaseAssetTests.AllAuthoredUnits_ExportFivePercentBaseDefense`에 맞춰 5. 최대 행동 비용은 공통 규칙 6.
- 위압 수치: 기획서(`캐릭터 기획서.pptx` 24쪽, `이벤트 스테이지 기획서.pptx` 15쪽)가 `n%`·`n`으로 적혀 있어, 사용자 지시로 **공격력 20% 감소·우선도 3 감소**를 초기값으로 정했다. 플레이로 확정한 밸런스는 아니다.
- Unit 표·외형 표·로스터·로드아웃은 그때 고치지 않았다. `DataBaseAssetTests`는 다른 Editor가 프로젝트를 쓰고 있어 시작 전에 끝났다(미확인). 그 뒤 바뀐 것은 §2.

## 4 공유 메모

| 메모 | 받는 사람 | 전달 |
|---|---|---|
| [쉴더 위압 View 인계 §8](../handoffs/2026-10-15-shielder-intimidation-view-handoff.md#8-si5-execution-record--2026-10-18)(C2 기록, 호진 문서 아님) | 전투 View·Core | 해당 없음(기록) |
| Core 경계 문서의 C3 식별자 기록(§5.44)·쉴더 식별자 조사(§22) | 천명님 | 미전달. 당시 `handoffs/2026-09-24-view-core-data-boundary-handoff.md`에 적었으나 지금 그 문서에는 없다(같은 번호는 다른 내용). git `bf4ff76b`에서 볼 수 있다 |
