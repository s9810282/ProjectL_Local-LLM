# EventPlayer 슬롯 번호·호출부 정비 설계·Task 기록 (2026-11-02)

## 1. 범위와 기준

사용자 지시에 따라 변경된 Boot 플레이어·슬롯 번호에 열거 값과 호출부를 맞춘다. 경로 기반 검색이나 이전 구성 폴백은 사용하지 않는다. 사용자가 복원한 StageChanger·Train·TrainBell과 Boot 변경은 그대로 유지한다.

## 2. 설계

- AMBGM: BGM 0, AMB 1, UIClick 2, UIHover 3, UIReward 4, GameStart 5, StageChanger 7.
- Unit: UnitPick 0, UnitCancel 1, UnitMovement 2, UnitDead 3, UnitAttack 4, ObsDamaged 5, ObsBroken 6. 기존 SfxEvent의 해당 값을 UnitEvent로 분리한다.
- SFX: Tornado 0, TrainCall 7, TrainBell 8. TrainBell은 번호만 제공하며 전 턴 재생 연결은 Phase 2다.
- AudioSystem은 각 열거 값을 해당 플레이어의 슬롯 번호로 직접 넘긴다. Unit 바인딩·해제 및 IAudioSystemService.Play(UnitEvent)만 추가한다. 기존 숫자·이름 지정 일반 API와 프리셋 처리는 유지한다.
- 입력의 유닛 선택·취소와 사건의 이동·공격·사망·장애물 소리 호출을 UnitEvent로 바꾼다. CombatSoundCue는 Unit 슬롯을 구분해 가지고 CombatEventAudio가 맞는 Play 오버로드를 호출한다.
- TraitActivationPreview는 이전 2플레이어 구성을 유지하지 않고 Boot와 동일한 3플레이어 슬롯 구성으로 갱신한다. 플레이어/오브젝트 기존 ID를 유지하고 SFX 컴포넌트만 추가한다.
- 기존 프리셋 번호·DB 값·재생 시점은 바꾸지 않는다. 새 효과음 연결은 별도 Phase다.

## 3. Task·Phase

### Phase 1 — 기존 호출 복구·완료

1. 잘못 추가한 이벤트 경로 검색·경로표·이전 구성 폴백을 제거했다.
2. 플레이어별 슬롯 열거와 Unit 재생 서비스를 맞췄다.
3. 유닛 입력·사건·소리 단서의 호출부를 UnitEvent로 변경했다.
4. 개발 미리보기 씬의 플레이어 정의·슬롯을 Boot 구성으로 갱신했다.
5. 숫자·경로·호출부 및 diff를 정적으로 대조하고 Unity 확인을 인계한다. 단계 리뷰 지점에서 멈춘다.

### Phase 2 — 별도 진행

- TrainBell의 기차 등장 전 턴 호출: Core/Buffer에 이미 공개된 사실·사건 계약과 재생 시점을 확인한다. View가 규칙을 계산하지 않는다.
- 미연결 상태인 신규 피격·지형·오브젝트 효과음은 의미·프리셋 계약 확인 뒤 연결한다.

## 4. 구조 점검

- SSOT: 현재 슬롯 번호 계약은 AudioSystem.cs의 플레이어별 열거 정의에 모은다. 호출부는 숫자를 복제하지 않고 열거를 사용하며 프리셋 값은 기존 View DB에서 받는다. 씬 슬롯 순서 변경 시 이 계약을 함께 갱신한다.
- SOLID: S는 AudioSystem이 플레이어별 재생만 담당하고 사건 선택·재생 시점은 View 소유자에 남는다. O는 새 플레이어의 열거·오버로드 추가로 표현한다. L은 기존 재생·프리셋·수명 계약을 유지한다. I/D는 필요한 재생 오버로드를 기존 IAudioSystemService에 추가하며 소비자는 인터페이스를 사용한다.
- FW/JMH: AudioSystemFactory 생성 → GlobalSystemFactory 내부 Builder 서비스·모듈 등록 → GlobalSystemGroup.AudioServiceCommand 보관 → 루트에는 Global 그룹 Export → CombatDisplayServices.From 등 소비자가 Global Context로 접근한다. 이번 변경에 Factory/Builder/Export/조회 계층 변경은 없다.
- 기능 독립성: 유닛 플레이어 분리에 필요한 오디오 단서·입력·소비자만 변경한다. 전투 계산·연출 조립·UI·프리셋 값은 변경하지 않는다. 두 개발/운영 씬은 같은 슬롯 계약으로 맞춘다.

## 5. 검증과 사용자 확인

- Boot·TraitActivationPreview 각각 플레이어별 열거 슬롯 17개(TrainBell 포함)가 실제 이벤트 경로와 모두 일치했다. 두 씬의 객체 ID 중복은 0개이며 SFX 컴포넌트 참조는 각각 1개였다.
- 기존 유닛 소리를 SfxEvent로 호출하는 잔여 사용처 없음. 추가했던 이벤트 경로 검색·폴백 코드 제거 확인. 변경 파일 git diff --check 통과.
- View Test 코드 작성·수정·실행 없음. MSBuild 실행 없음. 컴파일 미확인.
- Unity 확인: 게임 시작, BGM·AMB, UI 클릭/호버/보상, 낮→밤 전환음, 유닛 선택/취소/이동/공격/사망, 장애물 피해/파괴, 기차 통과, 토네이도, 특성 미리보기 씬의 기존 소리.
