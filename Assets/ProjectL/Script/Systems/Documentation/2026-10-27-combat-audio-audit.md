# 전투 사운드 점검·수정 계획

- 점검일: 2026-10-27
- 브랜치: `feature/La/AudioSystem&UI`, HEAD: `56310e37`
- 점검 시작 시 작업 트리 변경 없음.
- Phase 1: 정적 호출 추적·설계·Task 계획 완료. 런타임 코드·씬·FMOD 설정 변경 없음.
- Unity 컴파일·EditMode·실제 청음은 이번 단계에서 실행하지 않았다. 프로젝트 전체의 정상 동작을 보증하는 점검은 아니다.

## 1. 현재 호출 구조

경로는 `ProjectL/Script/Systems/` 기준이다.

| 기능 | 현재 위치·시점 | 판정 |
|---|---|---|
| 배치 선택 Pick | `RuntimeSystem/InGameSystem/ViewSystem/Combat/Input/Interaction/PlacementDragInteraction.cs`, `BeginDrag` | 손패·필드 집기에서 재생 중 |
| 전투 선택 Pick | `.../Combat/Display/CombatSceneDisplay.cs`, `InspectUnit` → `CombatSelectionState.SelectCell` | 선택은 되지만 Pick 호출 없음. 공격 조준 Enter에도 Pick 없음 |
| 자동 턴 Pick | `.../Combat/Playback/Support/CombatEventSounds.cs`, `For` | 현재 ControllerTurnStarted에 Pick 분기 없음. 옛 문서의 턴 시작 호출 설명은 현재 코드와 다름 |
| 배치 확정 | `CombatEventSounds.For` | Placed 사운드 분기 없음 |
| 이동 | `CombatSceneEventPresenter` → `CombatUnitMotionSet`/`CombatUnitMotionPlayback` → `CombatEventAudio.PlayMovement` | 경로 시작 콜백 존재. `CombatEventSounds.Movement`에서 Owner.Enemy 제외 중 |
| 공격 클립 | `.../Combat/Presentation/Pieces/CombatClipAttack.cs`, `AddSound` | ClipEvent + SoundDelay에 공격음. 시작 시점이 아님 |
| 코드 모션 공격 | `.../Combat/Playback/Handlers/CombatAttackEventHandler.cs` → `CombatAttackPlayback` → 종류별 Animation | 소리를 콜백으로 전달. 곡사는 발사 시 호출 |
| 사망 | `CombatCharacterEventHandler.ComposeDeath`의 `CombatEventSoundPiece` | 반응 조립 시작에 호출. 연출 미시작 시 일반 사건 소리 경로 |
| 포효 | `CombatEventSounds.For`의 CombatRoarEvent | 포효자 공격 프리셋 사용. 일반 공격과 별도로 회귀 확인 필요 |
| 기타 | 같은 소리 표 및 `CombatSceneEventPlayback` | 전투 진입·밤 종료 BGM, 장애물, 기차, 토네이도 호출 존재 |

새 애니메이션 경로는 직접 FMOD를 호출하지 않고 소리 조각과 콜백을 이용한다. 따라서 예전 사건 재생 진입점만 고치면 클립 공격 시점은 바뀌지 않는다. 공격 처리기가 연출을 시작한 경우와 일반 사건 처리로 이어지는 경우를 함께 검증해야 한다.

## 2. Cannon 지연 원인

### 적 Cannon: 클립 경로

- `DataBaseSO/ViewGroup/CombatPresentation/AnimationClip/Enemy/CombatUnitAnimation_Cannon.asset`에 공격 클립·동반 클립 연결, soundDelay = 0.
- 원본은 `Resources/Characters/Colored/Models/Enemy/CannonEnemy.fbx`의 `Scull|CannonAttack`.
- `.fbx.meta`의 ClipStart = 0, ClipEvent = 0.77677053, ClipEnd = 1. 여기의 임포터 이벤트 시간과 런타임 클립의 초 단위를 혼동하지 않는다. 실제 초 단위는 임포트된 AnimationClip에서 확인한다.
- soundDelay가 0이어도 ClipEvent를 기다리므로 시작에 소리가 나오지 않는다.

### CactusCannon: 코드 모션 경로

- `UnitVisual_CactusCannon.asset`: animationPresentation 없음. 공격 설정은 Mortar.
- `CombatMortarAnimation`의 발사 시점은 PrepareDuration + ShootDuration × FireTime.
- 현재 에셋 값: 0.15 + 0.42 × 0.3 = 0.276초. 공격음 콜백도 Fire에서 실행된다.
- 시작 동작과 발사·착탄은 서로 다른 시점이다. 소리만 시작으로 옮기고 투사체 발사·타격 해제·착탄 시점은 유지하는 설계를 제안한다.
- 실제 FMOD 이벤트 내부 무음·선행 구간은 청음하지 않았다. 위 내용은 호출 지연의 코드상 원인이다.

## 3. Placement 연결 결정 필요

- `AudioSystem.cs`의 SfxEvent에는 UnitPlacement가 없다.
- `Assets/Scenes/Boot.unity`의 SFX 슬롯 9는 `Artifact`, 표시 이름 Placement, 경로 `event:/PostFX/SFX/Artifact/Placement`이다. 슬롯 7~9를 빈 슬롯으로 취급하면 안 된다.
- 저장소 FMOD Metadata/Event에서 확인된 Placement 이벤트도 이 Artifact 계열이다.
- **확인할 사항:** 유닛 배치에서 기존 Artifact/Placement를 재사용할지, 별도 Unit/Placement 이벤트·슬롯을 준비할지.
- 슬롯 번호와 이벤트를 추측해서 연결하지 않는다. 별도 이벤트가 필요하면 FMOD 이벤트·뱅크 제공 상태를 먼저 확인한다.

## 4. 수정 설계

1. Pick: 전투의 유효한 클릭 선택 경로에서 선택된 플레이어 유닛의 프리셋으로 재생한다. 배치 집기 Pick은 유지한다. 자동 턴·호버·표시 동기화에서는 재생하지 않는다. 적 정보 조회는 Pick 대상에서 제외하는 안을 제안한다.
2. Placement: 사용자 배치가 성공한 결과에서 손패 → 필드 전이를 확인하고 재생한다. 단순 Placed 종류만 검사하면 다음 밤 복귀 등에서도 재생할 수 있으므로 Expected/Final과 사건 출처를 대조한다. Enemy는 제외한다. 여기서 최초는 각 손패 → 필드 배치를 뜻하며 Run 동안 개체당 1회라는 별도 상태는 도입하지 않는 안이다.
3. Movement: 기존 경로 시작 호출과 Enemy 제외를 유지하고 회귀 검증한다. 강제 이동·경로 여러 칸·취소에 따른 중복 재생을 확인한다.
4. 공격: 클립 소리 기준을 ClipStart로 바꾸고 소리 지연은 그 기준에 적용하는 안이다. 코드 모션은 실제 Begin에 맞춘다. 기존 발사·타격 콜백에서는 공격음을 제거해 한 번만 재생한다. 일반 공격 외 포효의 정책은 유지한다.

## 5. Task·Phase 계획

### Phase 1 — 점검·설계 (완료, 리뷰 지점)

- 브랜치·작업 상태 확인.
- 선택·배치·이동·공격·새 클립 조립 경로 추적.
- Cannon 두 경로의 지연 원인 확인.
- Placement 이벤트 연결의 미정 사항 기록.

### Phase 2 — Pick·배치·이동

- 전투 클릭 선택 성공에 Pick 추가. 같은 유닛 재클릭도 유효 클릭으로 재생하는 안.
- Placement 이벤트 결정 후 슬롯 계약·씬 구성·확정 결과 호출 연결.
- Enemy 제외와 실패 배치·자동 복귀·다중 이동 중복 방지 검증.
- 기존 RecordingGameAudio와 선택·결과 재생 테스트를 확장하고 Unity 컴파일·관련 EditMode 실행.
- Phase 종료 시 멈추고 결과 보고.

### Phase 3 — 공격 시점

- 클립 공격 소리를 ClipStart 기준으로 변경하고 SoundDelay 설명·검증 갱신.
- 코드 모션의 Begin 시점에 소리 연결, 발사·타격 콜백의 소리 제거.
- 클립/코드 공격 각각 시작 1회·취소·재진입·실패·연출 미시작 대체 경로 검증. Cannon 발사·착탄 시점 유지 확인.
- Unity에서 Cannon·CactusCannon 및 대표 근접·직선 공격 청음. 필요하면 FMOD 이벤트 내부 선행 구간을 추가 조사.
- Phase 종료 시 멈추고 결과 보고.

## 6. 구조·계약 검토

- SSOT: 프리셋은 CombatSoundPresets, 사건별 소리 종류는 CombatEventSounds, 선택은 CombatSelectionState에 유지. 최초 배치 여부를 별도 컬렉션으로 복제하지 않는다.
- SOLID: 소리 선택과 AudioSystem 전송을 분리한 기존 구조를 유지한다. 애니메이션은 타이밍을 소유하고 구체 FMOD 구현을 참조하지 않는다. 기존 IAudioSystemService를 주입하며 새 서비스·상속 구조는 만들지 않는다.
- 생성: AudioSystemFactory가 AudioSystem 생성. 내부 등록: GlobalSystemFactory의 내부 builder에 IAudioSystemService + AddModule. 보관: GlobalSystemGroup이 내부 Reader에서 AudioServiceCommand로 보관. 소비: CombatDisplayServices.From(global) → CombatSceneDisplayFactory → CombatEventAudio/입력 소리 객체.
- 이 경로는 Global Context를 이용하므로 사운드 수정에 루트 직접 등록·조회 우회가 필요 없다. 이 점검은 해당 Audio 경로에 한정하며 Global 전체 등록 검사의 대체가 아니다.
- FW-001/002: 기존 서비스·모듈 등록 유지. Core·FieldCamera·Architecture 변경 없음. UI 등록 단위·UIId·정책·수명 변경 없음. 클릭은 기존 Input 경로를 사용한다.
- 변경 독립성: Pick 수정은 클릭 선택과 소리 피드백, 배치 수정은 결과의 소리 표와 슬롯 구성, 공격 시점 수정은 클립 조립·코드 모션 콜백에 한정한다. 조립 Factory는 의존성 추가가 실제 필요한 경우만 변경한다. 공격음 때문에 투사체·타격 판정을 수정하지 않는다.
- 공용 SfxEvent/Boot 슬롯을 변경한다면 슬롯 매핑·프리셋 구성 검증도 필요하다. 관련 없는 화면 전체 개편이나 Core 계산 검증은 범위 밖이다.
