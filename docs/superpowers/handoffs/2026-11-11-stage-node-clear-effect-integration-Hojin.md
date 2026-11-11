# 스테이지 노드 파괴 효과 develop 병합·키 재배정 및 인수 현황 — 2026-11-11

- 작성: Codex, 호진님 요청. 민형님에게 **미전달**.
- 원본 구성 범위는 Phase A / Task A1이며, 후속 B~D 사용자 승인에 따른 운영 연결 상태는 [작업 계획](../plans/2026-11-11-stage-node-clear-presentation-plan-Hojin.md)의 실행 기록을 따른다.
- 상태: **A~D 에셋·코드 연결 및 E 정적 점검 완료. 사용자 Unity 컴파일·기본 완료 연출 정상 확인. 효과별 전체 재생/정렬/반복·취소와 DataBaseAssetTests 인수는 대기.**
- 기준 HEAD: `5fcbc0e0887b7b876e083c2b28c17b2299cc1b4a`. 원본 파괴 에셋은 `f3406ea`에서 병합된 파일이다.
- 후속 Phase D에서 자동 파괴 → 실제 풀 반환 확인 → End → 작은 점프까지 코드 연결했다. 아래 Phase A 기록은 당시 검증 범위이며, 최신 인수 결과는 작업 계획의 **Phase E 실행 기록**, 세부 확인 절차는 **Phase D 사용자 Unity 확인 순서**를 따른다. 기본 정상 확인과 Gate A/D 세부 인수를 구분한다.

## develop 병합 후 현재 기준 — 2026-11-11

- develop의 기존 효과 번호 0~20을 유지하고, 노드 파괴 효과 8종을 21~28로 재배정했다. 효과 SO 8개와 노드 외형 SO 10개의 참조도 함께 변경했다.
- 효과 테이블은 develop의 19개 항목 뒤에 노드 파괴 효과 8개를 합친 27개다. 아래 매핑 표와 DB 확인 항목은 병합 후 기준이다.
- Phase A~E 및 연결된 계획·설계의 18~25/24개 검증 기록은 병합 전 이력이다. 이번 병합의 컴파일·재생은 미확인이며, 기존 Unity 정상 확인을 이번 결과로 간주하지 않는다.

## 1. 반영 파일과 등록

원본 위치: `Assets/ProjectL/Script/Render/Effect/BackGroundEffect/StageNode/`.

신규 Variant 위치: `Assets/ProjectL/Prefabs/StageMap/Effects/`.

신규 EffectDataSO 위치: `Assets/ProjectL/DataBaseSO/RenderGroup/EffectTable/Items/`. 에셋 이름은 아래 EffectType과 같다.

| EffectType | 숫자 | Variant 프리팹 |
|---|---:|---|
| StageNodeBreakCombat | 21 | StandardBattleStageNodeEffect_Pooled |
| StageNodeBreakBoss | 22 | BossBattleStageNodeEffect_Pooled |
| StageNodeBreakMainCharacter | 23 | MainCharacterUpgradeStageNodeEffect_Pooled |
| StageNodeBreakMainSkill | 24 | MainSkillUpgradeStageNodeEffect_Pooled |
| StageNodeBreakRecruitment | 25 | MercenaryRecruitmentStageNodeEffect_Pooled |
| StageNodeBreakRandomEvent | 26 | RandomEventStageNodeEffect_Pooled |
| StageNodeBreakRecovery | 27 | RecoveryStageNodeEffect_Pooled |
| StageNodeBreakSupportingCharacter | 28 | SupportingCharacterGrowthStageNodeEffect_Pooled |

develop의 기존 enum 값 0~20은 그대로 두고 새 키 21~28을 추가했다. `EffectDataTable.asset`의 기존 19개 참조 뒤에 8개를 추가했다. 신규 항목은 `effectPoolInitializeCount=0`, `effectPoolMaxCount=1`이다. MaxCount가 동시 실행 상한을 강제하는 것은 아니며, 단일 완료 연출 소유는 Phase D의 책임이다.

노드 외형 SO의 BreakEffect 연결은 Phase B다. 시작 장식은 대상이 아니고, 보스 등록 자체가 기존 로비/챕터 전환 정책을 바꾸지는 않는다.

## 2. 수명 계약과 설정 근거

원본은 직접 수정하지 않았다. Variant가 원본을 상속하고 기존 `ParticleStopNotifier`를 루트에 추가한다. 공용 EffectSystem/EffectScope/EffectPool/notifier C#은 변경하지 않았다.

아래 값은 **파일 설정 대조 결과**다. Unity 실제 재생 증거와 구분한다.

| 원본 계열 | 자식 수 | 방출 지연 | 방출 방식 | 최대 입자 생존 | Duration | Loop | 제어 입자 생존 |
|---|---:|---:|---|---:|---:|---|---:|
| StandardBattle | 7 | 0초 | 0초에 1개씩, 1회 | 0.85초 | 1초 | 꺼짐 | 1초 |
| BossBattle | 7 | 0초 | 0초에 1개씩, 1회 | 0.85초 | 1초 | 꺼짐 | 1초 |
| MainCharacterUpgrade | 7 | 0초 | 0초에 1개씩, 1회 | 0.85초 | 1초 | 꺼짐 | 1초 |
| MainSkillUpgrade | 7 | 0초 | 0초에 1개씩, 1회 | 0.85초 | 1초 | 꺼짐 | 1초 |
| MercenaryRecruitment | 7 | 0초 | 0초에 1개씩, 1회 | 0.85초 | 1초 | 꺼짐 | 1초 |
| RandomEvent | 7 | 0초 | 0초에 1개씩, 1회 | 0.85초 | 1초 | 꺼짐 | 1초 |
| Recovery | 7 | 0초 | 0초에 1개씩, 1회 | 0.85초 | 1초 | 꺼짐 | 1초 |
| SupportingCharacterGrowth | 7 | 0초 | 0초에 1개씩, 1회 | 0.85초 | 1초 | 꺼짐 | 1초 |

- 자식 Start Lifetime/Delay는 상수이며 Rate over Time/Distance는 0이다. Sub Emitters, Trails, Lifetime by Emitter Speed는 꺼져 있어 별도 수명 연장이 없다. Simulation Speed는 1이다.
- 루트는 Renderer를 끄고, 0초 Burst 1개 / Lifetime 1초 / Max Particles 1 / Start Speed 0으로 구성했다. 지속 방출과 Shape는 꺼져 있다. 자식의 마지막 입자 종료 계산값은 `0 + 0 + 0.85 = 0.85초`이며 루트 입자는 그 이후까지 남는다.
- 루트 Stop Action은 Callback이다. 콜백은 기존 notifier와 풀 반환 경로가 소유하며 새 수동 콜백을 덧붙이지 않는다.
- 루트·자식 모두 게임 시간 사용, Play On Awake 꺼짐, Culling Mode는 Always Simulate다. 보이지 않는 루트나 화면 밖 파편의 컬링 때문에 완료 시점이 멈추는 것을 피한다.
- 이후 원본 파편의 지연·생존·버스트·루프·수명 연장 모듈을 바꾸면 이 인수도 다시 해야 한다. 1초를 View 타이머로 복제하지 않는다.

## 3. 정렬과 지원 범위

현재 운영 설정은 `StageMapView_Default.nodeScale=1`, `StageSelect.unity`의 MapRoot와 부모 StageMapSceneRoot 스케일 `(1,1,1)`이다. 대응 건물 프리팹 8종과 원본 효과 루트는 모두 `(2,2,2)`다.

- Variant의 시험 배치 위치를 `(0,0,0)`, 회전을 identity로 정리하고 루트 스케일 `(2,2,2)`를 유지했다. 자식 파편의 위치·회전·메시·머티리얼은 원본을 상속한다.
- 자식 ParticleSystem의 Scaling Mode를 Hierarchy로, 자식 Renderer Alignment를 Local로 덮어썼다. 부모 크기 2와 실제 건물 회전을 따르도록 하기 위한 Phase A 정렬 설정이다. 원본 Local Scaling/World Alignment를 유지하면 부모 크기·회전과 파편 외형이 어긋날 수 있다. 근거: [Unity Scaling Mode](https://docs.unity3d.com/6000.0/Documentation/ScriptReference/ParticleSystemScalingMode.html), [Unity Render Space 정의](https://github.com/Unity-Technologies/UnityCsReference/blob/master/Modules/ParticleSystem/Managed/ParticleSystemEnums.cs).
- Phase D의 받침은 **단위 월드 스케일**, 위치·회전은 `StageNodeView.PlaceBuilding` 이후 실제 건물 루트의 WorldPose여야 한다. 바닥 높이 0.05나 RotationOffset 180도를 효과 쪽에서 다시 더하지 않는다.
- 건물 루트 스케일 2까지 받침에 복사하면 Variant의 2와 중복된다. Position/Rotation만 복사한다.
- 비단위/비균일 맵·노드·받침 스케일 지원은 이번 인수 범위 밖이다. 이후 연결에서 잘못된 구성을 명시적으로 검사한다.
- **수치 설정은 대조했지만 실제 파편의 시작 외형·바닥·회전 일치는 Unity 미리보기 전이므로 미확인이다.**

## 4. 이번 정적 확인 결과

- Variant 8개 각각의 원본 GUID/대상 fileID, 루트 notifier 연결, SO → Variant → 원본 연결을 대조했다.
- 신규 Variant/SO GUID 16개의 Assets 내 중복이 없음을 확인했다. 신규 폴더·프리팹·SO에는 meta를 함께 만들었다.
- 기존 enum 값 18개와 테이블 참조 16개를 HEAD와 비교해 보존됐음을 확인했다.
- 8종 × 자식 7개의 방출/수명/루프/수명 연장 설정을 대조했다. 계산된 마지막 파편 종료는 모두 0.85초다.
- 원본 파괴 폴더·공용 EffectSystem 폴더·StageSelect 씬에는 diff가 없다. 새 시험/도우미·MSBuild·Unity 재생은 실행하지 않았다.
- Unity CLI는 프로젝트 실행 흔적을 찾았지만 Pipeline 패키지가 없어 연결할 수 없었다. 따라서 에디터 import/컴파일/미리보기 결과를 정적 확인으로 대신 선언하지 않는다.
- 별도 읽기 전용 검수에서도 구체적인 수정 필요 결함은 발견되지 않았다. 폴더를 포함한 신규 meta GUID 18개 중복 없음, 원본 8종의 `f3406ea` 대비 보존을 추가 확인했다. 검수에서 유보한 import/버스트 override 해석·정렬·실제 종료 순서는 아래 Gate A에, Scope 반환/StopAll/재대여는 Gate D에 남긴다. 이 유보는 성공으로 간주하지 않는다. 최종 문서 검수는 작성자가 수행한다.

## 5. 사용자 Unity 확인 순서 — Gate A

### Import 오류 후속 수정 (2026-11-11)

사용자 확인에서 `PPtr cast failed ... Casting from PrefabInstance to Prefab at FileID 100100000`이 발생했다. `Logs/AssetImportWorker9.log`에서 신규 Boss Variant의 PreviewImporter 실행 중 같은 오류를 확인했고, Worker4에서는 EffectDataTable 미리보기 중 발생했다.

원인은 Codex가 Variant 8개의 로컬 `PrefabInstance` ID를 `100100000`으로 잘못 지정한 것이다. 이 값은 Unity가 import 때 만드는 프리팹 에셋 핸들의 ID다([Unity 직렬화 문서](https://docs.unity.com/en-us/engine/6000.0/manual/working-with-scenes/text-scene-format/yaml-prefab-serialization#the-prefab-asset-handle)). 앞선 정적 검수는 GUID와 명시된 fileID 참조를 대조했지만 import가 생성하는 핸들과의 충돌을 놓쳤다.

8개의 로컬 Instance ID와 각 stripped GameObject의 `m_PrefabInstance` 참조를 별도의 64비트 ID로 수정했다. 외부 원본을 가리키는 `m_SourcePrefab.fileID=100100000`은 정상 값이므로 유지했다. 각 Variant의 override 70개·에셋 GUID·DB가 참조하는 루트 GameObject ID는 유지했다. 수정 후 로컬 예약 ID 선언 0건, Instance 참조 일치 8/8을 정적으로 확인했다. **Unity 재import 후 오류 소멸은 아직 미확인**이다.

먼저 Effects 폴더의 8개 Variant를 Reimport하고 Console을 Clear한 뒤 다시 열어 같은 오류가 새로 발생하는지 확인한다. 아래 항목은 그 뒤 계속한다.

### 수정 후 추가 재점검 (2026-11-11)

- 사용자 추가 검수 요청으로 8종의 override **560개**를 대상 Unity 클래스 타입과 전체 속성 경로까지 대조했다. Burst 신규 배열 필드는 원본 자식의 실제 직렬화 구조와 비교했다. 로컬 Instance/stripped GameObject/notifier 소유 관계와 예약 ID도 다시 확인했으며 추가 불일치는 발견하지 못했다.
- 테이블 24개 참조/24개 고유 EffectType, 새 매핑 8개, 기존 enum 18개 값/기존 테이블 16개 순서 보존을 재확인했다. 원본 효과·공용 EffectSystem·StageSelect 씬에는 diff가 없다.
- 원본 자식 56개의 상수 수명 0.85초, 지연/연속 방출/루프/추가 수명 모듈 설정을 다시 대조했다. 이 값은 재생 검증이 아닌 설정 근거다.
- **새 Import 증거:** 현재 `Editor.log` 30099~30106행에 수정된 Variant 8종 각각의 PrefabImporter 완료 및 새 artifact ID가 기록돼 있다. 그 뒤부터 이번 확인 시점의 로그 끝까지 동일 PPtr 오류 및 새 import 오류는 기록되지 않았다. Worker4/9에 남은 이전 오류는 이 재import 이전 기록이다.
- 결론: 현재 파일과 수정 후 Import 로그 범위에서 추가 수정 필요 결함은 발견하지 못했다. Prefab Mode에서 다시 열었을 때의 미리보기, 실제 파편 정렬·반복 재생·종료 순서는 여전히 사용자 확인 대상이다. **컴파일 미확인, Gate A 재생 인수 대기**를 유지한다. 이번 재점검에서는 에셋/C#을 추가 수정하지 않았다.

### 확인 항목

1. **Import/컴파일:** Unity로 돌아가 import를 마친 뒤 Console에 새 오류가 없는지 확인한다. 8개 프리팹이 Variant로 인식되고 원본 연결 및 루트 ParticleStopNotifier가 정상인지 확인한다. Missing Script/잘못된 property override가 없어야 한다.
2. **DB:** EffectDataTable의 항목 수는 27개다. 새 키 21~28에 해당하는 SO와 같은 계열 Variant가 연결됐는지, 초기 풀 0 / MaxCount 1인지 확인한다. 기존 `DataBaseAssetTests`도 사용자 Unity에서 확인한다.
3. **최초/반복 미리보기:** 위 8종을 각각 Prefab Mode의 기존 Particle 미리보기에서 자식 포함 재생한다. 정지·초기화 후 다시 재생해 파편 7개가 반복해서 나타나는지, 루트의 제어 입자가 화면에 보이지 않는지 확인한다. Play On Awake는 꺼져 있으므로 직접 재생한다.
4. **수명:** 자식 파편이 약 0.85초 내 사라지고, 루트 제어 입자는 1초까지 유지되는지 확인한다. 루트가 먼저 끝나 파편이 잘리거나 반복 방출되는 현상이 없어야 한다. 실제 프레임 경계 차이는 별도로 기록한다.
5. **정렬:** 저장하지 않을 임시 배치에서 운영 건물과 효과를 같은 최종 위치·회전에 겹쳐 비교한다. 받침 월드 스케일은 1, 효과 로컬 위치/회전은 0/identity, 효과 루트 스케일은 2다. 시작 구간의 파편이 건물 크기·바닥·방향에 맞아야 한다. 운영 외형의 180도 회전도 확인하며, 2챕터 상점/강화 외형은 같은 Recruitment/Supporting 계열을 확인한다. 정상 건물의 회복 불꽃 등 부가 표현까지 파괴 프리팹에 복제하는 작업은 없다.

결과를 `8종 모두 정상 / 문제가 난 프리팹 이름과 증상`으로 알려주면 된다. 이 항목들이 미확인인 동안 Gate A는 통과로 기록하지 않는다.

**Phase A에서 확인하지 않는 것:** 운영 EffectScope 자연 반환, StopAll, pause/재개, 두 노드 연속 방문, 파괴 뒤 작은 점프. 실제 호출자가 연결되는 Phase D2에서 확인한다. 에디터 미리보기는 운영 풀 반환의 증거가 아니다.

## 6. Phase A 당시 구조 점검과 후속 연결

- SSOT: EffectType → 프리팹은 기존 Render DB 한 곳이다. View에 프리팹 직접 참조나 별도 시간 상수를 만들지 않았다.
- SOLID: 단일 책임은 에셋 등록/정렬에 머문다(S). 새 enum/DB 항목으로 확장한다(O). 기존 루트 ParticleSystem/notifier 요구를 만족하도록 구성했다(L). 공개 서비스 인터페이스 추가 없이 기존 계약을 유지한다(I/D).
- FW-011/012: 풀의 대여·반환 소유권을 우회하지 않고, 기존 Effect DB의 유효 키/루트 컴포넌트 조건을 따른다. 서비스 Factory/Group/Export/소비자 경로는 변경하지 않았다.
- 기능 독립성: 파편 외형 변경은 원본/Variant, 키 등록은 Effect DB에 국한된다. Core·FieldCamera·Architecture·이동·완료 조립 코드는 수정하지 않았다.
- 다음 Phase에서는 이 등록을 사용할 BreakEffect 외형 키를 연결한다. 효과 자연 종료의 운영 증거는 Phase D Gate까지 남긴다.

## 7. Phase D 운영 연결 — 2026-11-11

- 기존 Global Render Context로 받은 EffectSystemServices를 System 수명의 전용 어댑터에 주입하고, SceneFactory가 완료 조정자와 작은 점프를 조립한다. Factory 생성 계약을 추가했으며 서비스 재등록은 없다.
- 실제 건물 최종 월드 Pose의 독립 받침에서 실행한다. 반환된 EffectScope 자식이 없어진 뒤 End로 받침/세대를 정리하고 작은 점프를 시작한다. 전투 복귀 로딩과 비전투 팝업 종료를 기다린다.
- EffectSystem/EffectScope/ParticleStopNotifier와 원본 파괴 에셋은 D에서 변경하지 않았다. 반환/재대여/StopAll/정지 재개/다른 효과와의 간섭은 사용자 Unity에서 확인해야 한다. 컴파일 미확인. 민형님에게 자동 발송하지 않았다.

## 8. Phase E 최신 인수·공유 요약 — 2026-11-11

- 사용자 답변: **“컴파일과 기본 완료 연출 정상 확인”**. 기본 동작 확인을 인수했으며 확인 노드/효과 종류·반복 횟수·취소 시나리오는 특정되지 않았다. 이전 절의 컴파일 미확인은 당시 기록이다. 최신 상태와 남은 범위는 [작업 계획 Phase E](../plans/2026-11-11-stage-node-clear-presentation-plan-Hojin.md)를 원본으로 한다.
- 현재 파일 재대조: 효과 키 18~25 및 테이블 24개 키 중복 없음, 노드 외형 10개→8종 Variant 매핑 정상, Variant의 원본 GUID/notifier/로컬 fileID 참조 및 루트 게임 시간·Callback 설정 정상. 자식 56개 0.85초, 루트 제어 입자 1초 설정은 확인했으나 이것만으로 실제 전체 종료를 인수하지 않는다.
- 변경 경계: 공용 EffectType enum에 8개 추가, View SO에 선택 키/MarkerVisit 연결, System에서 기존 Global Render Context를 조회하고 StageMapSceneFactory.CreateCompletion으로 조립한다. 원본 파괴 에셋과 EffectSystem/EffectScope/EffectPool/ParticleStopNotifier는 HEAD 대비 변경 없음. E에서는 게임 코드·에셋을 추가 수정하지 않았다.
- 정렬 계약: 건물 보정 후 월드 Position/Rotation만 단위 받침으로 복사한다. Variant의 스케일 2와 중복되지 않게 건물 스케일을 받침에 복사하지 않는다. 민형님 원본의 파편 수명·지연·루프·메시가 바뀌면 전체 종료와 정렬을 다시 확인해야 한다.
- 남은 인수: 효과별 최초/반복·정렬, 풀 재대여/StopAll/pause, 전투 복귀/비전투 팝업, 취소·새 Run·기존 전환, 기존 DataBaseAssetTests. 공용 반환 계약 문제로 판명되면 View에서 타이머나 직접 Destroy로 우회하지 않고 공용 담당자와 계약을 조정한다.
- 이 문서는 민형님께 전달할 준비를 마친 메모이며 **미전달**이다. 외부 발송·커밋·push는 하지 않았다.
