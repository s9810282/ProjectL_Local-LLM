# 임시 공격 연출 4종 이식 Task 계획

- 작성: View 작업, 2026-10-13
- 설계 원본: [임시 공격 연출 이식 설계](../specs/2026-10-13-temporary-attack-presentation-design.md)
- 상태: 일반 총알·근접·Ballista·CatusCannon 범위 사용자 확정. 2026-10-13 C4 코드·독립 Unity 자동 검증 완료, GameView 외형 리뷰 대기. C4 리뷰 지점에서 정지하며 C5는 미착수다.
- 후속 요청: 사용자가 피격·사망 모션의 R1·R2 순차 진행을 지시했다. R1 구현·검증 상태는 §7.3, 명시적 R2 진행 지시 뒤 사망 모델 유지·모션·풀 반환을 완료한 기록은 §7.4다. R2 리뷰 지점에서 멈추며 C5는 미착수다.
- 각 Phase 완료 후 보고하고 멈춘다. 다음 Phase는 사용자 진행 지시 후 시작한다.
- 커밋·push·다른 작업자에게 메시지 보내기는 사용자가 담당한다.

## 0 조사와 설계

- [x] `develop-4week`의 일반 총알·근접·Ballista·CatusCannon과 잔류 가시밭 코드 확인.
- [x] 원본 커밋·코드·프리팹·설정 경로를 설계 §1에 기록.
- [x] 현재 사건 재생·후처리 Refresh·가시밭 Buffer·적 연출 동시 변경 확인.
- [x] 사용자 요청으로 공격 4종과 CatusCannon 가시밭을 범위에 포함.
- [x] SSOT·SOLID·JMH/FW, 취소·풀 수명, Core 경계와 표현 시점의 한계를 설계에 기록.
- [x] 설계 §8과 첫 구현 Phase C1 사용자 리뷰. 다른 세션 작업이 없다는 사용자 확인 뒤 진행 지시를 받았다.

계획 작성 당시 변경 중이던 적 연출 코드·씬·로드아웃·Core 전달 문서·FMOD 캐시는 기존 작업이다. 계획 작성 때는 새 설계·계획 파일만 만들었다. C1 착수는 사용자가 다른 진행 작업이 없다고 확인한 뒤 지시했으며 기존 변경을 보존했다. 통합 View 계획에는 이 계획 링크와 상태만 등록한다. 상세 Task·실행 기록의 원본은 이 파일이다.

## 1 C1 공통 공격 표시와 에셋 기반

범위는 설계 A2/A3, 공통 좌표·설정·Tick·수명이다. 이 단계에서는 실제 공격 사건의 대기·완료 동작을 켜지 않는다.

- [x] **C1.1** 적 연출 작업의 완료 보고·리뷰 상태와 작업 트리 변경을 확인한다. 같은 파일을 편집 중이면 연결 부분을 보류하고 독립적인 에셋 의존성 조사부터 진행한다.
- [x] **C1.2** 옛 GUID를 현재 에셋과 대조한다. Bullet/CactusBomb/ThornCactus/BallistaBolt 및 필요한 SO·머터리얼·FBX만 목록화하고 Missing Script 에셋·원본 PACK을 보존한다. 기존 유닛 전체를 복원하지 않는다.
- [x] **C1.3** 공유 View 공격 설정과 유닛별 종류 연결을 만든다. `UnitVisualData`의 optional 표현 설정, null일 때 기존 즉시 동작, 설정된 프로필의 참조·수치 검증을 정의한다. 유닛별 과거 override 목록을 기록한다.
- [x] **C1.4** 현재 CustomPool 기반 발사체 표시를 만든다. 직선·곡사, 외부 Tick, 대여 수명·중복 반환 방지, Clear/Release와 부분 시작 실패 정리를 구현한다. 관통 접촉 조립은 C4다.
- [x] **C1.5** 표시 공격 소유자 `CombatAttackPresenter`의 준비/시작/완료/취소 입구와 단일 활성 동작 수명을 만든다. 설정·Mapper·이미 빌린 View의 좁은 접근만 전달한다. 운영 사건에 연결하기 전까지 동작 변화가 없어야 한다.
- [x] **C1.6** 회전된 필드·비단위 스케일·셀 간격에서 목표 좌표와 곡사 높이, 완료 전 자원 회수, 반복 Cancel/Release, 늦은 콜백·대여 재사용을 검증한다.
- [x] **C1.7** 관련 빌드와 Pool/좌표/설정 테스트를 실행한다. DB 연결 에셋을 바꾸면 `DataBaseAssetTests`를 포함한다. Unity 에셋 로딩 결과와 관리 테스트를 구분한다.

**리뷰 보고:** 새 코드·에셋과 GUID 의존성, 표현 설정 목록, 수명 검증, 아직 켜지지 않은 기능. C1 완료 후 멈춘다.

## 2 C2 CatusCannon 곡사 공격과 가시밭

C1 리뷰 뒤 진행한다. 사용자의 최초 요청을 먼저 완성한다. 범위는 설계 A6과 가시밭 표시다.

- [x] **C2.1** 현재 `StageUnit_CatusCannon`의 모델·스케일·HUD를 보존하고 Bucket 피벗과 준비·발사·복귀 표시를 연결한다. `MovedFrom`·GUID 처리와 Missing Script 부재를 검사한다.
- [x] **C2.2** 과거 CatusCannon SO 값과 CactusBomb 외형을 연결한다. 목표 월드 방향, 발사 순간 피벗, 준비→발사→곡사·회전→착탄 회수→자세 복귀를 외부 Tick으로 재생한다.
- [x] **C2.3** 기존 Pacing 뒤 실제 공격 사건을 AttackPresenter에 연결한다. 착탄과 자세 복귀가 끝나기 전 후속 피해·사망과 결과 확인이 진행되지 않도록 기존 완료 계약을 사용한다.
- [x] **C2.4** CatusCannon의 기존 공격 오디오 프리셋을 발사 시점에 1회 재생한다. 기존 즉시 공격 소리와 중복되지 않게 한다. 별도 착탄 프리셋은 현재 Audio 공개 계약·프리셋 연결이 확인될 때만 기존 것으로 연결하고 새 AudioSystem 구현은 추가하지 않는다.
- [x] **C2.5** `FieldThornPresenter`를 만든다. Install/결과 완료 Refresh에서 전달된 실제 `ThornFields`를 칸별 대조하고 생성·재사용·반환한다. `CombatThornConsumedEvent`의 제거를 현재 사건 순서에 연결한다.
- [x] **C2.6** 공격 완료 Refresh에서 잔류 가시밭 생성, 수명 만료·밤 종료·밟아서 소비, 중첩 공격 재사용, 벽·점유·가장자리에서 실제 Core 목록과 일치함을 검증한다. 생성 칸·남은 턴을 View가 계산하지 않는다.
- [x] **C2.7** Cancel/Detach/오류/재연결/유닛 비활성화·풀 반환에서 포탄·무기·콜백·가시밭 표시를 정리한다. 포탄 다음 대여를 이전 공격의 회수가 건드리지 않는지 검증한다.
- [x] **C2.8** 관련 빌드·EditMode와 Unity 재생을 확인한다. CatusCannon 공격→잔류→소비/만료→재공격의 실제 재생과 Core/Buffer 회귀 증거를 기록한다.

**리뷰 보고:** 실제 공격·가시밭 생성 시점, 순서·오디오·취소·재사용 결과, Unity 미확인 항목. C2 완료 후 멈춘다.

## 3 C3 일반 총알과 근접 몸통 박치기

C2 리뷰 뒤 진행한다. 범위는 설계 A4/A5다.

- [x] **C3.1** Akimbo·Sniper·ShotGun·MainCharacter 및 근접 유닛의 과거 연결을 확인하고 현재 UnitVisual 표현 프로필을 명시한다. InGame DB/사거리 판정으로 표현 종류를 선택하지 않는다.
- [x] **C3.2** 일반 총알의 목표 방향·발사·직선 비행·반동과 완료 대기를 연결한다. 단일 셀·방향·다중 영향·빈 셀 결과에서 사건 사실을 사용한다.
- [x] **C3.3** 근접 준비·기울임·전진·복귀 동작을 현재 모델 표시 오프셋으로 연결한다. 실제 위치·Core 방향·Buffer를 수정하지 않으며 HUD·숫자 방향을 유지한다.
- [x] **C3.4** 타격 이전 피해/사망 표시 금지, 최초 근접 복귀 이후 후속 사건, 총알 도착과 반동 복귀 모두 완료, 음수/큰 프레임·반복 공격을 검증한다.
- [x] **C3.5** 공격별 기존 오디오를 발사/타격 시점에 1회 재생한다. 공격 설정 없는 유닛의 기존 즉시 동작과 소리를 보존한다.
- [x] **C3.6** 이동→공격·적 방향 전환·체력 갱신·강제 이동·풀 재사용에서 임시 자세 유지/복구를 확인하고 기존 Pacing 테스트를 함께 실행한다.
- [x] **C3.7** 관련 빌드·EditMode·유닛별 Unity 재생을 확인한다. 피격 흔들림·사망 넘어짐은 추가하지 않는다.

**리뷰 보고:** 연결 유닛 목록, 총알·근접의 피해 표시 시점, HUD·방향·Pacing·취소 회귀. C3 완료 후 멈춘다.

## 4 C4 Ballista 관통 화살과 접촉별 표시

C3 리뷰 뒤 진행한다. 범위는 설계 A7이며 관통 표현에 필요한 사실 검사를 먼저 완료한다.

- [x] **C4.1** 실제 Ballista Buffer 결과를 인수한다. 방향/여러 직선, `AffectedCells`, 벽 차단, 종점과 `PiercingShotDelivery` 제공 여부, 후속 피해·사망 사건의 출처/공격 그룹 대응을 확인한다. Core 사실이 부족하면 Core 전달 문서에 요청을 기록하고 해당 구현 전에 사용자에게 묻는다.
- [x] **C4.2** 결과 시작 정보와 결과 끝 표시 완료를 전달하는 좁은 계약을 기존 Ordered/Display에 연결한다. 사건 순서와 기존 결과 확인을 유지한다. 환경 피해·연쇄 공격·여러 출처가 혼재한 대응 규칙을 테스트한다.
- [x] **C4.3** 현재 Ballista 프리팹에 Bolt/String 참조와 원본 활시위·장전 표시를 연결한다. 실제 제공된 방향만 발사하고 3방향을 고정하지 않는다.
- [x] **C4.4** 화살을 실제 경로·종점에 따라 보간하고 접촉 도달 상태를 기록한다. 해당 후속 사건은 접촉 뒤에만 표시하며 목록을 재정렬하거나 피해를 선반영하지 않는다.
- [x] **C4.5** 피격 없음·여러 피격·벽/보드 끝·빈 관통 경로에서도 유한하게 완료한다. 반동/마지막 접촉 전 결과 확인 금지와 턴 종료 재장전, 종점 조기 회수를 검증한다.
- [x] **C4.6** 사망·반환·Cancel/Detach/부분 발사 실패에서 화살·활시위·장전·접촉·늦은 통지를 정리한다. 이전 공격 접촉이 다음 공격을 풀지 않아야 한다.
- [x] **C4.7** 원본 BallistaVolley/Presentation 및 AttackImpactTiming 테스트의 검증 의도를 현재 결과 계약으로 옮긴다. 관련 빌드·Unity EditMode와 1방향/여러 방향 실제 재생을 확인한다.

**리뷰 보고:** 인수한 실제 결과 형식, 접촉별 피해 표시 근거, 과거 무한 직진과 현재 종점의 차이, 완료·취소·재장전 결과. C4 완료 후 멈춘다.

## 5 C5 통합 재생과 인계

피격·사망 추가 설계가 승인되면 §7의 R1·R2 리뷰 뒤 C5를 진행한다. 아래 체력·사망 사실 표시 검증에 피격 복귀·사망 모델 유지와 반환 검증을 함께 포함한다.

- [ ] 4종 각각 정상 공격·반복 공격·빈 대상/방향 공격·피격/사망·다중 대상 결과를 확인한다.
- [ ] 이동→공격·같은 적의 연속 공격·턴 종료·밤 종료에서 기존 행동/턴 대기와 방향 복귀가 중복되지 않음을 확인한다.
- [ ] CatusCannon 잔류 가시밭을 실제 `ThornFields`와 대조하고 생성·중첩·소비·수명 만료·재진입을 확인한다.
- [ ] 정상 승리·패배·포기에서 마지막 결과를 소비한 뒤 이탈한다. 예외 정지·씬 비활성화·중간 취소 후 활성 발사체 0, 대기 콜백 0과 무기 자세 복구를 확인한다.
- [ ] Clear 반복·Release 반복·Install 재시도·다음 대여에서 풀 수명·ID·자세·프리팹 참조가 새지 않음을 확인한다.
- [ ] 실제 에셋 로딩·DB 교차 검증·기존 CombatPresentationCleanup/SceneEventPresenter/Movement/DisplayReuse/Pacing 회귀를 실행한다. 결과 확인·입력 Layer·정상 이탈 테스트를 포함한다.
- [ ] RecordInputOnly는 기존 입력 기록 동작을 유지하며 Core 결과가 없는 경로에서 가짜 공격/가시밭을 생성하지 않는다. Live 개발 fixture와 운영 Database 재생 증거를 구분한다.
- [ ] SSOT·SOLID·JMH/FW 점검, 변경 파일·설정 조절법·검증 원본·미확인 항목을 실행 기록과 인계에 남긴다. 사용자 커밋·push 전에 검토할 수 있도록 diff를 정리한다.

**리뷰 보고:** 4종 완료 범위, Unity 재생·회귀 결과, 실제 동작의 한계, 남은 외부 조건. C5 완료 후 멈춘다.

## 6 완료 판단과 실행 기록

코드 작성, 관리 테스트, Unity EditMode, 실제 재생을 각각 기록한다. 한 Phase의 코드 완료를 전체 이식 완료로 표시하지 않는다. CatusCannon 가시밭은 새 생성 사건 없이 결과 완료 Refresh에 표시하는 설계이며, 정확한 착탄 순간 생성은 별도 요구다.

2026-10-13: 조사·설계·Task 계획 작성. 공격 4종 범위 사용자 확정. 구현·씬 연결·컴파일·테스트 실행은 아직 하지 않았다. 현재 적 연출·씬·DB·Core 전달 문서 등 기존 진행 변경은 보존했다.

### 6.1 C1 공통 기반 완료 (2026-10-13)

사용자가 다른 진행 작업이 없다고 확인하고 진행을 지시했다. C1 착수 시 기존 적 연출 코드·씬·계획 변경을 확인하고 보존했다. C1은 아래 기반만 구현했으며 실제 공격 사건이나 씬에 연결하지 않았다.

- **설정:** `Data/View/CombatPresentation`의 종류·공유 SO·불변 Data와 `UnitVisualData.AttackPresentation` 선택 연결. 미연결은 null, 설정된 종류에 필요한 참조·유한 수치·시간·발사 비율을 검증한다. 과거 SO 값을 하나의 공유 표시 프로필에 입력할 기반이며 런타임 상태를 저장하지 않는다. 현재 `.asset`은 수정하지 않았다.
- **좌표:** 기존 `FieldGridMapper`에 필드 법선 `WorldUp`과 스케일을 반영한 `WorldCellWidth`만 추가했다. 기존 칸 변환·회전 계산은 유지한다.
- **발사체:** 기존 `CustomPool`을 사용하는 외부 Tick 직선·곡사 표시. 대여마다 고유 lease를 발급하고 반환 전에 lease·콜백을 무효화한다. 정상 완료는 반환 뒤 한 번 통지하고, 취소·Clear·Release는 성공 통지를 하지 않는다. Clear는 재사용, Release는 생성한 복제본과 비활성 보관 자식만 파괴하며 입력 root/prefab을 보존한다.
- **공격 소유자:** `ICombatAttackAnimation`과 `CombatAttackPresenter`가 단일 동작 Begin/Tick/Cancel/Clear/Release를 소유한다. 종류별 조립은 이후 Phase이며 완료 전 Release, 정리 실패 모으기, 완료 콜백에서 시작한 다음 동작 보호를 구현했다. 새 서비스·실행 루프·Coroutine·Task·전역 구독은 없다.
- **원본 조사:** [에셋·설정 조사](../handoffs/2026-10-13-temporary-attack-source-inventory.md)에 고정 커밋, 프리팹·SO 9개·머터리얼·FBX·텍스처 GUID와 로컬 LFS bytes를 기록했다. 기존 Ballista PACK은 재사용하며 현재 같은 이름의 Thorn FBX는 다른 GUID·내용이므로 원본으로 remap하지 않는다. Akimbo/MainCharacter는 과거 BodyBash였고 C3 연결 시 이를 기준으로 확인한다. 실제 복원은 C2 이후다.

#### 검증과 수정

1. VS 2022 MSBuild `ProjectL.Tests.Framework.csproj` 개발/Editor 정의 빌드 성공, 오류 0. 최초 전체 빌드의 경고 14개와 최종 증분 빌드의 경고 13개는 기존 미할당 필드·Core nullable 주석이며 이번 파일의 새 경고는 없다. 신규 파일은 생성된 csproj를 수정하지 않고 `Temp/TemporaryAttackC1/NewFiles.targets`로 추가했다. 원본 로그는 `msbuild.log`·`msbuild-final.log`다.
2. 독립 Unity 6000.0.73f1 프로젝트 `Temp/TemporaryAttackC1/Probe`에서 현재 빌드 runtime DLL과 테스트 소스를 사용해 **Unity Test Runner EditMode 188 통과 / 0 실패 / 0 건너뜀**. reflection 직접 실행 결과가 아니다. 신규 설정 44·AttackPresenter 12·Projectile 20 = 76건, 기존 Cleanup 6·MovementView 22·EnemyPacing 19·EnemyPresentation 46·DisplayReuse 19 = 112건이다. 실제 GameObject 활성/비활성·기울기/배율/셀 간격·반복 대여·늦은 토큰·콜백 재진입·부분 실패·외부 파괴를 검증했다. 설정 테스트는 고유 임시 폴더에 SO/프리팹을 저장·동기 재import해 공유 참조를 확인하고 자기 폴더를 정리했다.
3. 첫 실제 실행에서 Pool의 OnDisable→Release가 활성화 변경 중 `DestroyImmediate`를 호출하는 결함을 발견했다. lease·콜백은 즉시 닫고 내부 SetActive가 끝난 뒤 같은 호출에서 파괴 큐를 처리하도록 수정했다. OnEnable/OnDisable과 중첩 Clear→Release에서도 모든 생성 자원 파괴를 확인했다. transient SO의 EditorJson 참조 테스트는 저장된 자산을 사용하는 검증으로 고쳤다.
4. 최초 넓은 필터의 실패 29건은 C1 2건과 별도 27건이었다. C1 2건은 위와 같이 수정했다. 씬을 복사하지 않은 독립 프로젝트의 Combat.unity 의존 25건과 기존 손패 테스트 helper의 일반 Transform→RectTransform 캐스팅 2건은 최종 C1 필터에서 제외했고 해당 기존 코드를 수정하지 않았다. 초기 결과는 `test-runner-initial.xml`, 최종 결과는 `test-runner.xml`·`test-runner-summary.txt`·`unity-tests-verified.log`에 보존했다. 따라서 원본 씬 회귀가 모두 통과했다고 판단하지 않는다.
5. 독립 프로젝트 초기 Package Manager IPC 권한 실패는 필요한 실행 권한으로 재실행해 해소했다. 테스트 조립의 DLL 참조와 임시 복사본 메타 import를 정리한 뒤 최종 실행했다. 원본 프로젝트의 test asmdef·기존 메타는 수정하지 않았다.
6. `git diff --check` 통과. 변경 C# 12개는 UTF-8·LF, 새 C#은 모두 `.meta` 포함. 현재 Assets 중복 GUID 0. SSOT·SOLID·JMH/FW 검토에서 Core 사실·Playback 결과 확인·기존 Pacing 책임을 복제하지 않았으며 추가 서비스 등록은 없다.

#### 완료 범위와 다음 리뷰

C1 완료는 공통 코드·좌표·설정 직렬화·수명 검증이다. 기존 DB 연결 에셋·유닛 프리팹·씬·Core·Buffer·FieldCamera·Architecture는 수정하지 않았다. 실제 DB 에셋 값 변경이 없어 `DataBaseAssetTests`는 이번 실행에 포함하지 않았다. 운영 프리팹 복원·원본 프로젝트 Test Runner·공격 실제 재생은 아직 수행하지 않았다.

계획대로 C1에서 멈춘다. 다음은 사용자 진행 지시 뒤 **C2 CatusCannon 곡사 공격과 결과 완료 Refresh의 잔류 가시밭 표시**다. C3·C4·C5는 미착수다. 커밋·push·외부 전달은 하지 않았다.

### 6.2 C2 CatusCannon 공격·가시밭 연결 (2026-10-13)

사용자가 C2와 공격 후 가시밭 프리팹 연결을 명시적으로 지시했다. 설계 A6과 C2.1~C2.8 범위를 구현하고 아래 자동 검증을 실행했다. C3~C5는 시작하지 않았다.

#### 동작·에셋·책임

- **공격 순서:** 기존 적 Pacing 뒤 성공한 공격 사건의 `PrimaryCell`을 목표로 삼는다. `CatusCannonWeaponView`가 이미 대여된 유닛의 Bucket과 발사 피벗을 연결하고, `CombatMortarAnimation`이 준비·발사·반동·무기 복귀를 외부 Tick으로 재생한다. 곡사 포탄 도착과 자세 복귀가 모두 끝나야 기존 Ordered 완료 콜백을 호출한다. 후속 피해·사망·결과 확인은 기존 순서를 유지한다.
- **원본 값:** 준비 0.15초, Shoot 0.42초의 발사 비율 0.3(준비 시작 후 0.276초), 곡사 비행 0.8초, 높이 `WorldCellWidth × 2`, 초당 회전 `(60,30,15)`, Bucket -20도·복귀 0.25초, 반동 0.04·2도다. 시간은 기존 Tick이 전달하는 값으로 진행하며 새 Coroutine·Update·서비스를 만들지 않았다. 큰 프레임에서도 발사 순간의 자세·피벗을 먼저 샘플링한다.
- **오디오:** 기존 공격 프리셋 9를 발사 때 1회 사용한다. 기존 즉시 공격 소리와 중복하지 않는다. 프리셋 10은 Minecart이므로 착탄 소리로 연결하지 않았다. 별도 착탄 프리셋은 확인되지 않아 추가하지 않았다.
- **잔류 표시:** 발사체는 착탄 때 회수한다. 별도 `FieldThornPresenter`가 최초 Install과 전체 결과 완료 `FinalizeResult.Refresh`에서 `CommandState.ThornFields`만 대조한다. 생성·만료·밤 종료·벽·점유·가장자리의 규칙을 View에서 계산하지 않는다. `CombatThornConsumedEvent.Source.Cell`은 사건 순서에서 제거하며, 남아 있는 칸의 인스턴스는 재사용한다. 새 가시밭은 착탄 후 **전체 결과 완료 Refresh**에 표시한다.
- **자세·수명:** 유닛의 임시 위치·방향은 대여 ID·버전으로 보호하고 HUD/체력 숫자의 위치·방향을 유지한다. Cancel/Detach/실패/비활성화/풀 반환은 포탄·무기·콜백·가시 표시를 정리한다. 취소는 성공 완료를 알리지 않는다. 비활성화로 중단된 공격은 자원을 먼저 회수하고 다음 Tick의 기존 오류 경계에 보고한다. Release는 소유한 복제본·비활성 보관 자식만 파괴한다.
- **복원:** `Prefabs/Combat/CactusBomb.prefab`·`ThornCactus.prefab`, 필요한 CactusBomb/꼬마선인장 FBX·텍스처를 고정된 과거 커밋에서 복원했다. 모델 bytes와 원본 GUID를 유지했고 옛 Bullet/MortarWeapon/UnitProceduralMotion 스크립트를 복원하지 않았다. 현재 유닛 프리팹은 바인딩 1개만 추가해 모델·스케일·HUD를 유지했다. 새 타입이므로 `MovedFrom` 대상은 없으며 새 C#은 `.meta`를 포함한다. 의존성 원본은 [C1 조사](../handoffs/2026-10-13-temporary-attack-source-inventory.md)다.
- **설정 원본:** `DataBaseSO/ViewGroup/CombatPresentation/CombatAttack_CactusCannon.asset`을 만들고 `UnitVisual_CactusCannon.asset`에 연결했다. 이 공유 View 프로필에서 시간·반동·포탄·가시 프리팹을 조절한다. 규칙 DB나 런타임 상태를 이 프로필에 저장하지 않는다.
- **범위 보존:** Core/Buffer/Flow/Bootstrap/FieldCamera/Architecture·Missing Script SO 48개를 수정하지 않았다. 현재 작업 트리의 Combat.unity·Mine/SheriffBadge 프리팹·MovementRangeRequest 등 다른 변경은 보존했다. C2는 Combat.unity를 편집하지 않았다.

#### 자동 검증과 증거 범위

1. VS 2022 MSBuild 개발/Editor 빌드 성공, 오류 0. 최종 빌드의 기존 CS0649 경고 13개는 C2 파일에서 발생하지 않았다. 로그는 `Temp/TemporaryAttackC2/msbuild-c2-final.log`다. 신규 파일은 생성 csproj를 수정하지 않고 `NewFiles.targets`로 보완했다.
2. 독립 Unity 6000.0.73f1 `Temp/TemporaryAttackC2/AssetProbe`에서 실제 Catalog/Combat 씬/복원 프리팹 의존성을 복사하고 현재 runtime DLL·테스트 소스로 Unity Test Runner를 실행했다. **EditMode 480건 중 476 통과 / 4 실패 / 0 건너뜀**. C2 신규 Animation 17·Thorn 16·Presentation 19·Asset 6 = **58건 모두 통과**. C1 설정/공격 소유자/발사체, 기존 이동/적 Pacing/정리/Display/Playback/ViewSystem과 Core의 CactusLobbedAttack·ThornField·공격 효과·타겟·환경 피해 회귀도 포함했다. 결과 원본은 `c2-tests-regression.xml`, `AssetProbe/c2-tests-summary.txt`, `unity-c2-final.log`다.
3. 별도 UnityTest가 `EnterPlayMode`로 실제 재생 상태에 진입해 무기만 비활성화·유닛 전체 비활성화의 즉시 OnDisable 회수를 확인했다. **1 통과 / 0 실패**이며 두 조건 모두 다음 Tick 전 활성 포탄 0·성공 콜백 0을 검사한다. 증거는 `play-lifecycle.xml`, `unity-play-lifecycle.log`다. 일반 EditMode 테스트와 실제 재생 콜백 검증을 구분한다.
4. 실제 DB의 로스터에서 Core `AcquireSub(Unit_CactusCannon)`으로 유닛을 획득하고 운영 전투 명령·Buffer 결과를 표시했다. 공격 2회, 첫 가시 8칸→남은 유닛 턴 1→만료 0칸, 다음 공격 7칸, NightEnd의 빈 목록을 확인했다. 공격 후 피해 표시 대기·발사 오디오 9 두 번·최종 Core 목록 일치도 검증했다. 이 실행의 실제 ThornConsumed 사건은 0건이므로 운영 전투에서 밟아서 소비까지 관찰했다고 주장하지 않는다. 소비 사건의 native 씬 표시/재사용은 별도 typed View fixture, 소비·벽·점유·가장자리 규칙은 기존 Core 테스트로 검증했다. 운영 경로의 filtered 증거는 `operational-diagnostics.xml`, `unity-operational-diagnostics.log`다.
5. 실제 에셋 테스트는 Catalog에서 연결한 공유 프로필, Bucket 피벗(`SM_Wep_Hot_Oil_Bucket_02`), Missing Script 부재, mesh/material 참조, 복원 스케일·HUD, 반복 공격·Cancel·재설치·Release 뒤 원본 에셋 불변을 확인한다. 사건/최종 목록을 직접 조립하는 앞선 5건은 View 경계 fixture이며 Core 명령 테스트 1건과 구분한다.
6. 원본 프로젝트를 중복으로 열지 않도록 독립 프로젝트를 별도 경로에서 실행했다. 실제 에셋의 `m_Script`만 스크래치의 precompiled DLL MonoScript로 재연결했다. 남은 42개 참조는 URP Editor AssetVersion/ShaderGraphImporter 등 에디터 전용 의존성이며 원본 자산은 수정하지 않았다. 이 실행은 GameView의 URP 셰이더 외관·카메라에서의 시각 품질을 보장하지 않는다. 원본 프로젝트의 전체 EditMode 테스트를 실행한 증거도 아니다.

#### 발견 사항과 남은 검증

- 첫 기반 native 실행 220건 중 1건은 일반 MonoBehaviour의 EditMode 비활성 콜백을 기대한 테스트였다. EditMode의 Tick 감지와 위 `EnterPlayMode` 즉시 콜백 검증으로 분리했다. 첫 넓은 실행 472/8에서는 C2 테스트의 실제 Bucket 이름, Playback.Abort 뒤 새 Attach 필요, 반복 전투의 재배치 조건을 바로잡았다. 프로덕션 계약을 테스트 편의로 바꾸지 않았다. 초기 결과는 `unity-foundation.log`·`c2-tests-initial.xml`에 남겼다.
- 최종 실패 4건 중 기존 Visibility 2건은 일반 MonoBehaviour의 EditMode OnEnable/OnDisable 콜백에 의존한다. scratch native 진단에서 SetActive·enabled 변경·DestroyImmediate까지 해당 콜백이 0회인 것을 확인했다. 기존 할당량 비교 1건은 Mono의 `GC.GetAllocatedBytesForCurrentThread`가 1KB/1MB 실제 배열 생성·KeepAlive에도 0을 반환해 성능을 판정할 수 없었다. 진단 원본은 `AssetProbe/c2-legacy-diagnostics.txt`, `unity-legacy-diagnostics.log`다.
- 나머지 기존 `DataBaseAssetTests.EveryCombatStage_EnvironmentAndViewRotationsReachOperationalDefinition` 1건은 원본 DB Stage1101의 sourceId206/208 `(6,3)/(6,5)`와 기존 테스트 기대값 `(7,3)/(7,5)` 불일치다. 관련 DB·기존 테스트는 HEAD와 동일하며 수정하지 않았다. 초기 기존 Railway 테스트의 비전투 node2 진입 실패는 무작위 맵의 고정 노드 가정 문제로, 최종 실행에서는 통과했다. 이 조건을 C2 연출 결함으로 처리하지 않는다.
- 조립 중 native 자원 생성은 필수 참조 검증 뒤로 미뤘고, 취소 콜백에서 새 공격을 시작하면 이전 Tick이 새 공격 시간을 진행하지 않도록 버전 검사했다. HUD 카메라가 없는 경우에도 원래 방향을 복원한다. 취소/부분 실패/정리 재진입/반복 Clear·Release/다음 대여 보호를 C2 테스트로 확인했다.
- SSOT·SOLID·JMH/FW 리뷰: PrimaryCell·가시 칸·수명은 Core 사실, 표현값은 공유 View SO, 단일 공격 수명은 AttackPresenter, 완료/순서는 기존 Ordered·Playback, 적 대기는 기존 Pacing에 유지했다. 새로운 서비스·Builder·루프·전역 구독 없이 기존 CustomPool·조립·정리 경계를 사용한다. Core에 직접 명령하거나 Buffer에 쓰는 production 코드는 추가하지 않았다.
- C2 C# 13개 UTF-8·LF, 새 C# 메타 포함, `git diff --check` 통과. 중복 GUID·C2 프리팹 의존성 누락 없음. 원본 에디터에서 **CatusCannon의 준비→발사→곡사→복귀→결과 완료 가시 표시의 GameView 외형**은 사용자 리뷰 대기다.

**리뷰 지점:** C2 코드·자동 검증을 완료하고 멈춘다. 다음 C3는 별도 진행 지시 뒤 시작한다. 커밋·push·외부 전달은 하지 않았다.

### 6.3 C3 일반 총알·근접 박치기 연결 (2026-10-13)

사용자가 C3 진행을 지시했다. 설계 A4/A5와 C3.1~C3.7을 구현했다. 유닛별 과거 동작 원본은 [에셋 조사 §3](../handoffs/2026-10-13-temporary-attack-source-inventory.md#3-유닛별-과거-표시와-override)이며, C4·C5는 시작하지 않았다.

#### 연결과 동작

| 유닛 | 공유 View 프로필 | 기존 공격 소리 |
|---|---|---|
| ShotGun · Sniper | `CombatAttack_Linear.asset` | 6 · 2 |
| MainCharacter · Akimbo | `CombatAttack_Melee.asset` | 0 · 1 |
| Demon · Gravekeeper | `CombatAttack_Melee.asset` | Demon은 기존처럼 미연결, Gravekeeper 7 |

- **총알:** 과거 Bullet prefab/material GUID·mesh·스케일 `(0.12,0.12,0.4)`·BaseColor `(1,0.72,0.16,1)`를 복원했고 옛 Bullet 컴포넌트는 제외했다. 속도 20·필드 법선 방향 높이 0.6, 반동 0.1·7도, Shoot 0.42초·발사 비율 0.3을 공유 프로필에서 받는다. 발사 때 기존 공격 소리를 1회 내며, 포탄 도착과 반동 복귀가 모두 끝나야 후속 사건을 진행한다. 원본 0.2 출발 오프셋은 가까운 목표를 넘지 않도록 제한한다.
- **박치기:** 0.48초, 준비 각도 22·타격 각도 55·당김 0.08·전진 0.25와 원본 진행 구간 `0~0.3 / 0.3~0.55 / 0.55~1`을 복원했다. 소리는 0.55 타격 시점에 한 번 내고, 피해·사망은 전체 복귀 완료 후 기존 순서로 표시한다. 피격 흔들림·사망 넘어짐은 추가하지 않았다.
- **사실·종점:** 단일 지정은 정확한 `PrimaryCell`이다. Direction/All의 주 셀이 없으면 실제 `AttackDirection`과 `AffectedCells`에서 방향 앞쪽의 가장 먼 제공 셀을 고르며, 같은 거리에서는 방향 축에 가까운 제공 셀을 사용한다. 영향 셀의 첫 항목을 조준 셀로 가정하지 않는다. 빈 방향 결과는 출발점에서 유한하게 끝내며, 사거리·보드 끝·벽·가짜 목표 셀을 View에서 만들지 않는다. 여러 영향 셀에서도 일반 총알 하나의 표시이며 피해 횟수를 새로 계산하지 않는다.
- **수명:** `CombatLinearAttackAnimation`·`CombatMeleeAttackAnimation`은 `CombatAttackMotion`의 자세·Tick·취소 수명을 공유한다. 반동 곡선은 CatusCannon도 같은 함수로 사용한다. 현재 Pacing 뒤 기존 AttackPresenter/Ordered/Playback 완료 경계에 연결하며 별도 Gate·서비스·Coroutine·Update가 없다. 미연결 유닛은 기존 즉시 표시·오디오를 유지한다.
- **유닛 표시:** 이미 빌린 View의 위치·방향만 임시 적용하고 HUD/체력 숫자와 원래 표시 위치를 보존한다. 자세 대여에 선택 취소 콜백을 연결해 비활성화·풀 반환·새 ID 바인딩에서 먼저 자세와 대여를 닫고 공격 자원을 회수한다. 이전 어댑터/콜백이 다음 대여의 자세를 복원하지 못하도록 ID·버전·참조를 검사한다. 완료 콜백에서 시작된 새 공격은 이전 Tick으로 진행하지 않는다.
- **변경 범위:** 유닛 prefab·Combat 씬·Core/Buffer/Flow/입력/FieldCamera/Architecture를 변경하지 않았다. UnitVisual 6개에 공유 프로필 참조 한 줄씩 연결했다. 새 모델 컴포넌트·옛 UnitProceduralMotion·Animator fallback을 유닛에 추가하지 않았다. 새 타입은 이동한 타입이 아니므로 MovedFrom 대상이 없으며 C#·에셋 메타를 포함한다.

#### 검증과 보완

1. VS 2022 MSBuild 개발/Editor 빌드 성공, 오류 0. 최종 기존 CS0649 경고 13개는 C3 파일에서 발생하지 않았다. 생성 csproj를 수정하지 않고 `Temp/TemporaryAttackC3/NewFiles.targets`로 신규 파일을 보완했다. 로그는 `msbuild.log`·`msbuild-final.log`다.
2. C2의 독립 Unity 6000.0.73f1 AssetProbe에 최신 runtime DLL·실제 View DB/총알·테스트 소스를 반영했다. **EditMode 507건 중 503 통과 / 4 실패 / 0 건너뜀**. C3 신규 순서/수명/좌표 21·실제 프로필/프리팹 6 = **27건 모두 통과**. C1/C2·이동·Pacing·정리·재사용·Playback/ViewSystem·DB 교차 검증과 관련 Core 회귀를 함께 실행했다. 원본은 `Temp/TemporaryAttackC3/c3-tests-final.xml`·`c3-tests-summary.txt`·`unity-tests-final.log`다.
3. 별도 UnityTest가 EnterPlayMode로 진입해 **총알·박치기 양쪽의 유닛 비활성화 즉시 회수**를 검사했다. 다음 Tick 전 활성 총알 0·성공 콜백 0, 다음 Tick의 기존 실패 경계 전달을 확인했고 **1 통과 / 0 실패**다. 따라서 C3 신규 검증은 총 **28건 통과**다. 증거는 `play-lifecycle.xml`·`unity-play-lifecycle.log`다.
4. 실제 Catalog에서 6종의 공유 프로필·기존 prefab·mesh/material 참조·Missing Script 부재를 확인하고 유닛별 native 반복 공격 2회, 원래 위치/회전/스케일 복구·숫자 위치·기존 오디오 인덱스를 검사했다. 이것은 typed View 사건 재생이며 플레이어 6종의 Core AI/공격 명령 전체를 실제 게임 입력으로 조작한 검증은 아니다. GameView 카메라·URP 외형 품질의 사용자 리뷰는 남는다.
5. 첫 실행은 500 통과/7 실패였다. 신규 테스트의 Direction selection은 계약상 주 셀 없는 AdditionalCells를 가질 수 없으므로 지정/영향 셀 사실으로 고쳤다. 연속 공격 테스트는 두 번째 적 행동 간격을 먼저 기다리도록 보완했다. 기존 `Audio_FollowsPresentedMovementTurnDeathAndBackgroundEvents`도 C3 프로필의 발사/타격·복귀를 기다린 뒤 다음 사건을 제출하도록 갱신했다. 공격을 다시 즉시 처리하도록 production을 바꾸지 않았다. 최초 결과는 `c3-tests-initial.xml`·`unity-tests.log`에 보존했다.
6. 남은 기존 실패 4건은 C2에서 확인한 동일한 조건이다: 일반 MonoBehaviour의 EditMode visibility callback 의존 2건, Mono 할당량 counter 0에 대한 엄격 비교 1건, DB 벽 `(6,3)/(6,5)`와 기존 기대값 `(7,3)/(7,5)` 불일치 1건. 원인은 §6.2의 native 진단 원본을 따른다. 해당 로직·DB·검증 조건을 C3에서 바꾸지 않았다.
7. C2 마지막 filtered 검사(무작위 맵의 전투 노드를 공개 API로 찾도록 보완한 뒤)는 5/1이었다. 실패는 첫 공격 후 Cactus가 사망해 두 번째 공격까지 생존하는 조건을 만족하지 못한 운영 전투였다. 이 신규 운영 테스트는 실제 첫 곡사·Core 가시 수명 만료를 검사하도록 분리했고, 반복 표시/소비/밤 종료는 별도 native View fixture·Core 테스트 및 §6.2의 실제 통과 실행 증거와 구분한다. 최신 C2 Asset 6건은 이번 전체 실행에서 모두 통과했다. 이전에 기록한 실제 2공격·NightEnd 관찰은 성공한 실행의 증거이며 모든 운영 전투의 재현 보장이 아니다. 공개 StartNewRun/그래프/PrepareStageEntry로 실제 Stage1101 진입 노드를 찾으며 DB·Core 상태를 직접 수정하지 않는다.
8. SSOT/SOLID/JMH/FW 점검: 목표·피해·후속 순서는 Core 사건, 표현 값은 공유 SO, 단일 공격 수명은 AttackPresenter, 결과 확인은 기존 Playback, 적 대기는 기존 Pacing이다. 두 동작의 공통 자세/정리만 공유하고 Core 계산·Buffer 쓰기·새 서비스/Builder/전역 구독을 추가하지 않았다. View 소유 CustomPool 대여를 반환·해제하며 기존 FW-014/016·P5/P6/P7 경계를 유지한다.
9. 신규 C# 7개 UTF-8·LF·메타 포함, 후행 공백·깨진 문자 없음. Assets 중복 GUID 0, `git diff --check` 통과. 총알 prefab/material은 원본 GUID를 유지하며 기존 PACK·옛 Missing Script SO 48개를 보존했다. 커밋·push·외부 전달은 수행하지 않았다.

**리뷰 지점:** C3 코드·자동 검증을 완료하고 멈춘다. 원본 GameView의 6종 공격 외형 확인은 대기다. 다음 C4는 사용자의 별도 진행 지시 뒤 실제 Ballista 관통 결과 계약 인수부터 시작한다.

### 6.4 C4 Ballista

2026-10-13 사용자 C4 진행 지시로 관통 화살·접촉 이후 피해 표시·원본 무기 표시를 연결했다. 이후 사용자가 최소 Core 계약 구현과 인계 기록을 명시적으로 승인했다. Akimbo의 사거리 1은 표시 선택 기준이 아니며, 사용자가 직접 `UnitVisual_Akimbo.asset`의 프로필을 일반 총알로 바꾼 것을 보존했다. 기존 C3 실제 에셋 테스트의 Akimbo 기대만 이 선택에 맞췄다.

**인수와 Core 경계:** 운영 Ballista는 Cannon·단일 지정·PiercingFalloff(30, int.MaxValue)로 한 줄을 게시한다. `PiercingShotDelivery`가 실제 이동 셀·피격 접촉 순서·종점·종료 사유를 제공한다. Core는 빈 경로(예산 소진), Wall, Structure, Range, BoardEdge 등을 구분한다. 새 `AttackResult.AttackGroup`은 기존 `CommandAttackGroups.Direct`를 그대로 게시하며 피해량·벽·감쇠·피격 순서·그룹 발급을 변경하지 않았다. 명령 안의 그룹이며 전역 ID가 아니다. Core 파일·승인·검증은 [Core 공격 그룹 인계](../handoffs/2026-10-13-ballista-core-attack-group-handoff.md)가 원본이다. 해당 기록을 기존 DB 경계 인계 §5.11에도 연결했다.

**표시 연결:**

- `CombatAttack_Ballista.asset`: BallistaBolt, 속도 12·높이 0.6·반동 0.04/2도·Shoot 0.5초·발사 비율 0.3·String 이동 0.3/0.22초/진동 2회·Reload 0.18초. 기존 UnitVisual_Ballista에는 프로필 1개만 연결했다.
- BallistaBolt의 원본 GUID `e93bf59f3871b924380214b0a086eeb1`·Mesh·머터리얼·스케일을 유지하고 옛 Bullet 스크립트만 제외했다. 기존 PACK은 복제·수정하지 않았다.
- 현재 EnemyUnit_Ballista에 `BallistaWeaponView` 1개를 추가하고 실제 `SM_Wep_Ballista_Mobile_Bolt_01`·`SM_Wep_Ballista_Mobile_String_01`을 연결했다. 원본 몸체·모델·콜라이더를 유지했다.
- `CombatBallistaAnimation`은 발사 순간 공격 사건을 완료 통지하고, 단일 AttackPresenter 수명은 화살 종점·반동 완료까지 계속된다. 실제 경로가 한 줄이므로 한 발을 쏜다. 여러 직선이 없는 결과에서 가짜 3방향을 만들지 않는다.
- `CombatBallistaImpactMap`은 결과 시작에 사건 목록을 받아 최신 공격 사건 구간의 **공격 그룹·가해자·실제 접촉 대상**으로만 대응한다. 환경/연쇄 피해의 다른 그룹은 묶지 않는다. 여러 출처가 합쳐진 단일 사건에 직접 출처가 있으면 해당 접촉을 기다린다. 실제 관통 기록 또는 양수 그룹이 없는 Ballista는 가짜 경로 대신 명시적으로 거부한다.
- 선택 계약 `ICombatPresentationBarrier`로 Ordered가 연결된 피해·사망·보유 환경·장애물 반응의 접촉 도달, 다음 이동/공격/턴 전환의 남은 화살·반동, 결과 끝의 표시 완료를 기다린다. 사건 순서·기존 Playback/최종 Refresh/Buffer 확인을 유지한다. 목록이 먼 대상 사건부터면 가까운 대상 피해도 그 순서 뒤에 표시되며 사건을 재정렬하지 않는다. 피해 숫자 팝업은 추가하지 않았다.
- 체력 갱신으로 전체 표시를 다시 그려도 현재 공격자 자세를 덮지 않도록 FieldUnitView에서 활성 공격 자세를 유지한다. 이동·반환은 앞선 공격이 끝난 뒤 진행한다.
- 끝점 도달 때 화살은 즉시 풀 반환한다. 마지막 피격 뒤 빈 경로가 있으면 해당 종점까지 기다린다. 장전 상태는 무기 View에 남고 턴 종료는 재장전 0.18초까지 기다린다. Cancel/Detach/풀 반환/유닛·무기 비활성화/부분 발사 실패는 화살·접촉·활시위·장전·자세를 정리한다. 이전 접촉으로 다음 공격을 풀지 않는다. Ordered를 거치지 않는 직접 표시 호출도 접촉 이전 피해 적용이나 비행 중 이동을 명시적으로 거부한다.

**검증 원본:**

| 구분 | 결과·출처 |
|---|---|
| 최신 관리형 빌드 | 오류 0. 기존 runtime CS0649 7건, 테스트 CS0649 6건 유지. Core를 포함한 빌드의 기존 CS8632는 변경 파일 밖이다. `Temp/TemporaryAttackC4/msbuild-runtime-final.log`, `msbuild-tests-final.log`, `msbuild-final.log` |
| 독립 Unity 6000.0.73f1 EditMode | **712건 중 708 통과, 기존 실패 4건, skip 0**. 관련 순수 Core **275/275**, 신규 Ballista **21/21** 포함. `Temp/TemporaryAttackC4/tests-final.xml`, `tests-final-summary.txt`, `unity-tests-final.log` |
| 실제 Play 모드 수명 | **1/1**. 유닛 GameObject 비활성화 및 무기 enabled=false를 각각 확인. 다음 Tick 전 화살 0, 늦은 결과 완료 차단. `Temp/TemporaryAttackC4/play-lifecycle.xml`, `unity-play-lifecycle.log` |
| 신규 인수 합계 | **22/22**(EditMode 21 + Play 수명 1). 실제 Catalog/프리팹·반복 공격 2건은 회전·비균등 배율 필드도 포함. 접촉별 체력/사망·비어 있는 꼬리 경로·벽/보드 끝/Range/빈 예산·반동·재장전·환경/연쇄/혼합 출처·목록 역순·Cancel/Detach/다음 공격·풀 반환·예외를 검증했다. |

최초 Unity 실행은 696 통과/12 실패였다. 새 fixture의 원래 HP 5보다 큰 공격 후 HP를 넣은 7건을 HP 3/2로 보정했고, 일반 공격 완료 콜백 뒤 새 행동 간격까지 같은 Tick에서 소비하던 회귀 1건을 수정했다. 일반 공격 Tick의 즉시 반환을 유지하고 Ballista만 후속 접촉 처리를 허용한 뒤 최종 실행에서 신규/회귀 8건이 통과했다. 마지막 리뷰에서 직접 BeginEvent/BeginStep 호출도 접촉/잔여 비행 대기를 우회하지 못하게 보완하고 신규 검증 1건을 더했다. 재장전 완료 콜백에서 시작한 다음 공격도 이전 Tick으로 진행되지 않게 보호하는 검증 1건까지 포함해 최종 712건을 확인했다. 검증용 복사 경로와 실행 준비 오류도 임시 프로젝트 안에서 수정했으며 원본 에디터·소유하지 않은 프로세스를 종료하지 않았다.

최종 기존 실패 4건은 C3와 같다: ordinary MonoBehaviour EditMode 비활성화 콜백 검증 2건, 이 환경의 GC 할당 계측 0 때문에 strict less-than이 실패하는 1건, 기존 Stage 1101 환경 배치 기대(7,3)/(7,5)와 실제 SO(6,3)/(6,5) 불일치 1건. 기존 실패를 통과로 보이게 하기 위해 Core·SO·기존 테스트를 바꾸지 않았다. 실제 Play 모드 신규 비활성화 검증은 별도로 통과했다.

**SSOT/SOLID/JMH/FW 점검:** 경로/피해/그룹은 Core 불변 사실, 조절 값은 View 공유 SO, 좌표는 Mapper, 단일 동작 수명은 AttackPresenter, 접촉 대응은 결과별 Map, 장전 상태는 WeaponView가 소유한다. 선택적 좁은 Barrier로 기존 순서 재생 계약을 확장했고 계산·표시·풀 책임을 분리했다. 기존 View Tick·CustomPool·취소 경계를 사용하며 새 서비스/Builder/Update/Coroutine/전역 구독/Buffer 쓰기를 추가하지 않았다. 기존 Framework·FieldCamera·Architecture·옛 Missing Script SO 48개·PACK을 보존했다.

최종 정리: 변경 C# UTF-8/LF·메타·후행 공백·깨진 문자 검사를 통과했고 Assets 중복 GUID는 0이다. 현재 작업 트리의 HEAD 대비 diff 검사도 통과했다. 사용자가 스테이징한 내용은 보존했으며, 마지막 실행 기록 수치와 신규 에셋의 빈 YAML 값 공백 정리는 작업 트리 수정으로 남아 있다(커밋 전 사용자 재스테이징 대상). 커밋·push·스테이징은 Codex가 수행하지 않았다.

**리뷰 지점:** C4 코드·자동 검증을 완료하고 멈춘다. 운영 결과는 단일 관통 경로이며, 여러 방향 발사는 현재 제공되지 않아 구현/실제 재생 완료로 주장하지 않는다. 실제 GameView에서 화살 정렬·활시위 외형·재장전 감각을 확인하는 리뷰는 대기다. C5 통합 재생과 정상 이탈 인수는 별도 진행 지시 뒤 수행한다. 커밋·push·외부 전달은 하지 않았다.

## 7 피격·사망 모션 추가 계획 (2026-10-13)

사용자의 후속 요청으로 고정 과거 커밋의 `UnitProceduralMotion.Hit`·`FallBackward`, `UnitHitPresenter`, 사망 모델 유지 경로와 현재 사건·풀 경로를 조사했다. [설계 §9](../specs/2026-10-13-temporary-attack-presentation-design.md#9-피격사망-모션-추가-설계-2026-10-13)가 추가 동작의 원본이다. 이 기록은 코드 구현·컴파일·Unity 검증 완료를 뜻하지 않는다.

사용자가 **R1 → 리뷰 → R2 → 리뷰 → C5** 중 R1·R2 순차 진행을 지시했다. 최초 설계의 제외 범위 변경을 승인한 것으로 기록하며 R1부터 시작한다. 각 Phase 완료 후 보고하고 멈춘다. C4의 최소 Core 공격 그룹 변경 승인을 새로운 Core 수정 승인으로 확대하지 않는다.

### 7.1 R1 피격 모션과 공용 자세

- [x] **R1.1** 착수 직전 작업 트리와 다른 작업자의 동일 파일 편집을 확인한다. 현재 캐릭터 UnitVisual 목록에 반응 프로필을 명시하고 건물·장애물에는 연결하지 않는다.
- [x] **R1.2** 공격과 독립적인 공유 View 반응 SO·불변 Data를 만든다. 피격 0.18초·셀 폭 비율 0.05·4도, 사망 0.55초·유지 0.2초를 검증하고 미연결의 즉시 표시를 보존한다. 원본 동작 출처는 inventory §6이다.
- [x] **R1.3** 기존 FieldUnitView 자세 대여를 최소 일반화한다. 공격·피격의 배타 소유권, 모델 원래 자세·HUD 위치와 방향, ID·버전별 복원·취소를 유지한다. C2~C4 공격 재생을 회귀 검사한다.
- [x] **R1.4** 전용 반응 객체로 외부 Tick 피격 왕복을 구현한다. 필드 법선·셀 폭·현재 모델 회전을 사용하며 별도 Update·이벤트 구독을 추가하지 않는다.
- [x] **R1.5** 살아 있는 대상의 실제 체력 감소 사건만 연결한다. 사건 Final 체력을 모션 시작에 표시하고 피격 복귀 뒤 사건 완료를 통지한다. 회복·HealthNormalized·치명 피해에는 피격 왕복을 붙이지 않는다.
- [x] **R1.6** 단일 물리 출처/다중 물리 출처/출처 없음/같은 칸의 방향 규칙, 반복 피해, 큰 프레임, 회전·비균등 필드, HUD, Ballista 접촉 이후 시작·반응 중 계속 비행·원본 사건 순서를 검증한다.
- [x] **R1.7** Cancel/Detach/소유자 비활성화/풀 반환/콜백 재진입/부분 실패/다음 대여를 검증한다. 관련 빌드·Unity EditMode·실제 활성 수명 테스트와 DB 에셋 교차 검증을 기록한다.

**리뷰 보고:** 피격 대상·설정 연결, 체력 반영 시점, 공격과의 수명 회귀, 검증·GameView 미확인 항목. R1 완료 후 멈춘다. 사망은 이 Phase에서 기존 즉시 반환을 유지한다.

### 7.2 R2 사망 모션과 모델 유지·반환

- [x] **R2.1** 이미 빌린 캐릭터 View의 사망 유지권을 FieldUnitPresenter에 연결한다. Died 사실 적용과 RenderFacts 이전에 획득하고, 최신 Buffer나 규칙의 죽음 상태를 되돌리지 않는다. 없는 View를 연출용으로 새로 만들지 않는다.
- [x] **R2.2** 현재 표시 방향의 뒤로 SmoothStep 90도 넘어짐 0.55초·유지 0.2초를 구현한다. 사망 Final 체력과 기존 사망 오디오는 시작 시 1회 반영한다. 치명 피해는 사망 모션만 재생한다.
- [x] **R2.3** 유지 중 다른 대상의 사실 갱신에도 모델이 반환되지 않게 한다. 완료 때 자세·유지권을 정리하고 기존 풀에 반환한 뒤 사건 완료를 1회 통지한다. 구조물·장애물 파괴는 기존 경로를 유지한다.
- [x] **R2.4** Ballista 접촉 이전 사망 표시 금지, 사망 모션 중 화살 비행·공격 반동 유지, 마지막 사망과 종점·반동 완료 전 결과 확인 금지를 검증한다. 일반 총알·곡사·근접의 기존 완료 시점을 유지한다.
- [x] **R2.5** 다중 대상 사망·가시밭/연쇄/폭발 출처·공격자 사망·모델 없음·반응 미설정·반복 Died 방어와 다음 대여 자세를 검증한다.
- [x] **R2.6** Clear/Release/Detach/오류/비활성화/부분 시작·정리 실패에서 유지권 0·대기 콜백 0·반환 1회를 확인한다. 취소는 완료 통지를 하지 않는다. 실제 OnDisable 수명과 콜백 재진입을 검사한다.
- [x] **R2.7** 관련 빌드·Unity EditMode·DB 교차 검증을 실행하고 정상 전투 종료가 마지막 모션·반환·결과 확인 뒤 진행됨을 확인한다. C5에서 실제 GameView와 운영 정상 이탈을 통합 인수한다.

**리뷰 보고:** 사망 사실/표시 모델 수명 구분, 완료·반환 순서, 정상 종료·취소·다음 대여 검증, 실제 외형 미확인 항목. R2 완료 후 멈추며 C5는 별도 진행 지시를 받는다.

### 7.3 R1 피격·공용 자세 완료 (2026-10-13)

사용자가 R1·R2의 순차 진행을 지시해 설계 §9를 승인했다. R1.1~R1.7을 구현·검증하고 Phase 리뷰 지점에서 멈춘다. R2 사망 모델 유지·넘어짐·반환은 미착수이며 현재 치명 피해의 즉시 반환은 유지한다. C5도 시작하지 않았다. 착수 때 작업 트리는 앞서 작성한 설계·계획·조사 문서 4개만 변경돼 있었고 기존 Core·씬·프리팹 진행 변경은 없었다.

**구현:**

- `CombatUnitReactionPresentationSO`·불변 Data와 `CombatUnitReaction_Default.asset`을 추가했다. 원본 피격 0.18초·셀 폭 비율 0.05·기울기 4도, R2에서 사용할 사망 0.55초·유지 0.2초를 입력한다. 유한 수치·양의 시간·음수/범위 오류를 검사한다. 런타임 진행 상태는 SO에 저장하지 않는다.
- `UnitVisualData.reactionPresentation`은 공격 프로필과 독립적인 optional 연결이다. 현재 캐릭터 8종(MainCharacter/Akimbo/ShotGun/Sniper/CactusCannon/Ballista/Demon/Gravekeeper)이 공유하며 구조물·장애물은 미연결이다. Akimbo의 사용자 지정 일반 총알 프로필과 유닛 프리팹은 보존했다.
- `ICombatUnitPose`와 FieldUnitView의 공용 자세 대여로 공격·반응의 배타 소유권, 기본 위치·회전, HUD/체력 숫자 위치·방향, ID·대여 버전별 복원을 유지한다. 기존 공격 계약은 공용 계약을 상속해 현재 공격 구현의 호출 보장을 유지한다.
- `CombatUnitReactionPresenter`가 sin 왕복 오프셋·기울기, 외부 Tick, 완료 전 복원, 취소·Release·실패 정리를 소유한다. 물리 출처가 명확하면 사건 셀에서 대상 셀로 향하고, 출처 없음/같은 칸/여러 물리 출처는 모델 뒤쪽을 사용한다. Mapper의 법선·월드 셀 폭을 따른다.
- EventPresenter는 살아 있는 캐릭터의 `HealthChanged`에서 실제 체력이 감소할 때만 연결한다. Final 체력은 시작 때 표시하고 복귀 뒤 완료 통지를 1회 한다. 회복·동일 체력·HealthNormalized·치명 피해는 피격 왕복을 하지 않는다. 프로필·기존 View가 없으면 즉시 사실 표시를 유지한다.
- C4 접촉 대기 뒤 피격을 시작하며, 피격 대기 중에도 Ballista 화살·공격 반동은 계속 Tick한다. 원본 사건 순서를 유지하고 최종 Refresh/다음 사건/결과 확인은 피격 완료를 기다린다. 완료 콜백의 다음 반응에는 이전 프레임 시간을 넘기지 않는다.
- Cancel/Detach/반환·비활성화에서는 콜백을 무효화하고 자세를 복원한다. 성공 완료를 통지하지 않으며 소유자 비활성화는 다음 Tick에서 기존 오류 경계에 알린다. 새 대여와 완료 콜백에서 시작한 새 반응을 이전 정리가 건드리지 않는다.

**검증:**

| 구분 | 결과 |
|---|---|
| Runtime·Framework Tests MSBuild | 오류 0. 기존 CS0649 경고 runtime 7개·tests 6개, 신규 파일 경고 없음 |
| 독립 Unity EditMode | **739건 중 735 통과 / 기존 실패 4 / 건너뜀 0** |
| 신규 피격·프로필 EditMode | **27/27 통과**. 평면/회전·비균등 필드, HUD, 출처 4종, 체력 변화 종류, 즉시 fallback, 취소/풀 반환, 배타 자세·늦은 복원, 콜백 재진입·예외, 실패 자세 정리, 관통 비행과 최종 완료를 검사 |
| 실제 PlayMode 수명 | **1/1 통과**. EnterPlayMode 후 GameObject 또는 FieldUnitView 비활성화에서 다음 Tick 이전 복원·완료 미통지·오류 경계 전달을 검사 |
| 신규 인수 합계 | **28/28 통과**(EditMode 27 + Play 수명 1) |
| 기존 공격/규칙 회귀 | Ballista 21/21, Mortar 42/42, StandardAttack 27/27, 선택 Core 관련 275/275. 기존 결과 소비·이동·Pacing·View 재사용 및 실제 Catalog DB 교차 검사 포함 |
| 정적 검사 | `git diff --check` 통과, 새 C# 7개 UTF-8/LF·meta 포함, Assets 중복 GUID 0 |

기존 실패 4건은 C4 §6.4와 동일하다. 일반 EditMode의 OnEnable/OnDisable을 전제한 기존 테스트 2건, GC 측정 API가 0을 돌려 strict-less 비교가 실패하는 1건, Stage1101 환경 배치 SO와 기존 기대 좌표의 불일치 1건이다. 해당 원본 코드를 바꿔 실패를 숨기지 않았다.

초기 신규 실패는 fixture 최대 체력을 5로 고정한 기대값과 비균등 필드의 좌표 완전 일치 비교였으며, 실제 사건의 최대 체력과 기존 좌표 검사 수준의 허용 오차로 보정했다. 기존 공격 테스트 중 private 자세 필드의 이름 조회와 실제 에셋 공격 후 피격 대기는 새 계약에 맞춰 검증 의도를 유지했다. 피격 연결 코드를 제거하거나 테스트를 제외해 통과시키지 않았다.

검증 원본은 `Temp/TemporaryAttackR1/`의 `msbuild-runtime-final.log`, `msbuild-tests-verified.log`, `verified-r1-tests.xml`, `verified-r1-tests-summary.txt`, `unity-edit-verified.log`, `play-lifecycle.xml`, `unity-play.log`다. 이전 시도 결과도 같은 폴더에 보존했다. 기존 독립 프로젝트 `Temp/TemporaryAttackC2/AssetProbe`에 현재 runtime DLL·테스트 소스·실제 Catalog/에셋 복사본을 넣고 원본 script GUID를 독립 프로젝트 DLL MonoScript에 remap한 뒤 Unity Test Runner로 실행했다. 원본 에디터를 종료하거나 원본 테스트 asmdef·기존 SO 48개를 변경하지 않았다.

**설계 점검:** 사건·체력·출처·순서는 Core/Buffer 사실, 표현 값은 공유 View 프로필, 좌표는 Mapper, 자세 소유권은 FieldUnitView가 원본이다. 반응 상태는 전용 객체에 두고 EventPresenter에는 사건 연결만 추가했다(SSOT/SOLID/JMH). 기존 Tick·조립·완료·오류 경계·CustomPool 반환을 사용해 FW-001/002/003/004/014/016을 유지한다. Core·Buffer·FieldCamera·Architecture·입력·Flow·씬·기존 프리팹은 수정하지 않았다.

**리뷰 지점:** R1 코드·자동 검증 완료, 실제 GameView에서 흔들림의 크기·방향·타이밍을 보는 리뷰는 대기다. 사용자 AGENTS.md의 Phase 종료 보고·정지 규칙을 따라 여기서 멈춘다. R2는 설계된 사망 모델 유지·넘어짐·반환을 연결하는 다음 Phase다. 커밋·push·외부 전달은 하지 않았다.

### 7.4 R2 사망 모델 유지·모션·풀 반환 완료 (2026-10-13)

사용자의 명시적 R2 진행 지시로 R2.1~R2.7을 구현·검증했다. §7.3은 R1 완료 시점의 이력이며 현재 사망 동작은 이 절을 따른다. C5는 시작하지 않았다.

**연결과 수명:**

- `FieldUnitPresenter.DeathPresentationHold`가 현재 ID·View 인스턴스에만 적용되는 유지권을 발급한다. EventPresenter가 Died 사실 적용·RenderFacts 전에 유지권과 자세를 준비한다. 반응 프로필 또는 이미 빌린 활성 View가 없으면 기존 즉시 표시로 끝내며 새 모델을 만들지 않는다.
- 운영 Core의 사망 Final은 `Excluded`이고 Anchor·Rotation·OccupiedCells가 비어 있다. 이 사실을 그대로 적용하면서 유지권이 있는 기존 모델의 Transform만 보존해 체력 0과 HUD를 표시한다. Field 상태·가짜 좌표·살아 있음으로 사실을 되돌리지 않는다. 다른 대상의 RenderFacts에서도 같은 대여를 유지한다.
- 기존 `CombatUnitReactionPresenter`의 외부 Tick에서 현재 모델 뒤로 SmoothStep 90도 넘어짐 0.55초·유지 0.2초를 진행한다. 값은 R1의 공유 반응 프로필이 원본이다. 치명 피해는 피격 왕복을 거치지 않고 사망 모션만 시작하며 기존 사망 소리는 시작 때 1회 재생한다.
- 정상 종료는 자세 복원 → 유지권 제거 → `EntityViewSet.Release(id, expected)` → 기존 CustomPool 반환 → 사건 완료 1회 순서다. 반환 전에 ID 연결을 제거해 반환 콜백의 재진입을 보호한다. 이전 유지권은 다음 대여를 반환할 수 없으며 반복 Died는 모델 대여·사망 소리를 반복하지 않는다.
- Clear/Release/Detach/취소/오류에서는 성공 완료를 통지하지 않고 자세와 유지권·모델을 정리한다. FieldUnitPresenter 전체 반환은 유지권부터 무효화해 풀 반환 콜백과의 중복 반환·열거 중 변경을 막는다. View 컴포넌트 비활성화로 반환된 모델도 다음 OnGet에서 enabled를 복구한다. 부분 시작 실패·자세 쓰기/복원 실패·반환 실패에서도 정리를 시도하며 완료 콜백에서 시작한 다음 반응을 이전 실패가 취소하지 않는다.
- Ballista 대상은 접촉 대기를 통과한 뒤 사망을 시작하고 모션 중에도 화살 비행·공격 반동이 진행한다. 공격자 자신의 사망은 잔여 비행 뒤에 시작한다. 여러 사망은 원본 사건 순서대로 기다리며 마지막 모션·반환과 화살 종점 완료 전에 결과 확인을 열지 않는다. 일반 총알·곡사·근접 완료 시점, 구조물·장애물 파괴 경로는 기존 계약을 유지한다.

**검증과 증거 범위:**

| 구분 | 결과 |
|---|---|
| Runtime·Framework Tests MSBuild | 오류 0. 기존 CS0649 경고 runtime 7개·tests 6개, 변경 파일 새 경고 없음 |
| 독립 Unity EditMode | **780건 중 776 통과 / 기존 실패 4 / 건너뜀 0** |
| 신규 R2 EditMode | **24/24 통과**. 위치 없는 사망 Final, 평면/회전·비균등 필드, HUD, 다중 사망·출처 4종·공격자 사망, 모델 없음·반응 미설정 회귀, 반복 Died, Ballista 접촉/잔여 비행, 취소/전체 반환/Detach, 다음 대여·콜백 재진입/예외·부분 실패·정리 실패를 검사 |
| 실제 Play 모드 수명 | **1/1 통과**. EnterPlayMode 후 GameObject 또는 FieldUnitView 비활성화에서 다음 Tick 이전 유지권 0·풀에 반환 1회·성공 통지 0, 다음 Tick 오류 전달, 같은 인스턴스 재대여·사망 모션 완료를 검사 |
| 신규 R2 인수 합계 | **25/25 통과**(EditMode 24 + 실제 Play 수명 1) |
| 기존 반응/공격/규칙 회귀 | R1 피격·프로필 27/27, Ballista 21/21, Mortar 42/42, StandardAttack 27/27, 관련 Core 275/275. 실제 Catalog DB 교차 검사·운영 Cactus 공격/가시 수명 검사 포함 |
| 완료·정상 이탈 계약 | 마지막 사망과 반환 전 Playback 최종 확인 차단을 신규 실제 View fixture로 검사. 기존 Runtime·Core·Buffer·Job/Plan 진행 통합 15/15, NormalExit 최종 결과·Runner 대기 2/2 통과. 후자의 씬 로드/표시는 대역이며 운영 GameView 정상 이탈 인수는 C5에 남김 |
| 정적 검사 | `git diff --check` 통과, 새 테스트 C# 2개 UTF-8/LF·meta 포함, Assets 중복 GUID 0 |

최초 실행은 780건 중 774 통과/6 실패였다. 운영 사망의 위치 제거를 새 유지권 경로에서 빠뜨린 문제를 보완하고, 기존 오디오·이동 테스트가 사망 모션 완료를 기다리도록 고쳤다. 기존 비활성화 콜백 테스트도 실제 사망 반환 시점까지 Tick하도록 보완해, 단순 모션 대기 때문에 콜백 검증이 우연히 통과하지 않게 했다. 새 사망 fixture는 운영 계약과 같은 Excluded·위치 없음 사실을 사용한다. 최종 기존 실패 4건은 §7.3과 동일한 일반 EditMode visibility 콜백 의존 2건·GC 할당 계측 0의 엄격 비교 1건·Stage1101 DB와 기대 좌표 불일치 1건이다. 실패를 숨기려고 원본 DB·Core·검증 조건을 바꾸지 않았다.

원본은 `Temp/TemporaryAttackR2/`의 `msbuild-runtime-final.log`, `msbuild-tests-final.log`, `tests-initial.xml`, `tests-final.xml`, `tests-final-summary.txt`, `unity-edit-final.log`, `play-lifecycle.xml`, `unity-play.log`다. 기존 AssetProbe에 최신 runtime DLL·테스트 소스·실제 에셋 복사본을 반영해 Unity 6000.0.73f1 Test Runner를 실행했다. 원본 에디터를 종료하거나 원본 프로젝트의 전체 테스트를 실행한 증거가 아니다. 원본 GameView에서 넘어지는 방향·HUD·가려짐·타이밍과 운영 정상 이탈은 사용자 리뷰/C5 대기다.

**SSOT/SOLID/JMH/FW:** 체력·사망·위치 제거·점유·승패는 Core/Buffer 불변 사실, 표현값은 기존 공유 View SO, 자세는 FieldUnitView, 반응 진행은 ReactionPresenter, 모델 대여·반환은 FieldUnitPresenter, 사건 순서·결과 확인은 기존 Ordered/Playback이 소유한다. 유지권에는 자원 식별만 저장한다. 기존 Tick·조립·오류·CustomPool 경계를 사용해 FW-001/002/003/004/014/016을 유지하며 새 서비스·Builder·Update·Coroutine·Core 명령·Buffer 쓰기를 추가하지 않았다. R2에서 Core·Buffer·Flow·FieldCamera·Architecture·씬·DB 에셋·프리팹을 수정하지 않았고 R1 연결 8종·Missing Script SO 48개를 보존했다.

**리뷰 지점:** R2 코드·자동 검증을 완료하고 멈춘다. C5는 별도 진행 지시 뒤 시작한다. 커밋·push·외부 전달은 하지 않았다.
