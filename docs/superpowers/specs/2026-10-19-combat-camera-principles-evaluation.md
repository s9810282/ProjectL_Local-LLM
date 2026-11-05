# 카메라 연출 작업 원칙 검사 (K-1~K-6, 대여 분리 뒤 4차)

- 작성일: 2026-10-19. 코드 변경 없음(검사만). 1차(K-1~K-3)는 아래 "1차 평가 기록"에 그대로 둔다.
- 2차 요청: "카메라 작업 내용 SSOT(한 사실은 한 곳에만)와 SOLID, JMH Framework 개발 지침·FW 계약, 기능 변경의 독립성로 평가해"(K-6 뒤).
- 기준: [설계 세션 인계](../handoffs/2026-09-30-combat-view-design-session-handoff.md) §4. 점수는 1~10(10 = 지적할 것이 없음).
- 2차에 더한 대상(경로 약칭 `C/` = `Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/ViewSystem/Combat/`):
  - K-4 피격·사망 흔들림: 창구 겹침 규칙·시계(`C/Display/CombatCameraWindow.cs`), `C/CombatCameraMotionSettings.cs`, `C/CombatCharacterEventHandler.cs`, 사건 Presenter·Factory, 설정 Data·에셋.
  - K-5·K-6 기차 흔들림·줌인: 새 `C/CombatTrainCamera.cs`, `C/CombatRailwayContactPlayback.cs`(진입·이탈 알림), `C/Display/RailwayContactSchedule.cs`(`TryGetFieldSpan`), `C/Display/RailwayTravelPath.cs`(`LeaveTime`), `C/Display/CombatContactGeometry.cs`(`PlaneLeaveDistance`·`Reach`), `C/Display/FieldGridMapper.cs`(`CellBody`), `C/CombatSceneEventPlayback.cs`, 설정 Data·에셋.
  - Listener 노이즈 프로필 `Assets/ProjectL/Prefabs/Stage/MapProp/MapCameraShakeNoise.asset`(코드 아님).
- K-4~K-6은 사용자 지시로 검토 에이전트를 돌리지 않았다. 이 검사가 그 대신은 아니다(코드 읽기 기준).

## 4차 (2026-10-19, 전체 재검사 — K-1~K-6 + 대여 분리 뒤 지금 코드)

- 사용자 요청: "카메라 관련 SSOT(한 사실은 한 곳에만)와 SOLID, JMH Framework 개발 지침·FW 계약, 기능 변경의 독립성 진행". 코드 변경 없음(검사만).
- 확인한 사실: Framework·Render 파일 작업 트리 변경 0. 카메라 연출 값 생성자 인자 **16개**(2차·3차의 "17개"는 잘못 센 것, 바로잡음). 넣는 조건 `camera != null && motion != null && Has… && Can…`은 포효 둘·피격·사망 네 곳, 기차 카메라는 같은 규칙의 다른 모양 두 줄. `None` 기본값 대입은 처리기 둘·재생 조정 하나. 진행 중 흔들림 기록은 창구의 끝 시각·세기 하나. 줌 자기 시계는 줌 조각과 기차 카메라 두 곳. 장면 채널의 "유닛 비어 있음"은 조립(적기)과 대여 묶음(찾기) 두 곳.

| 기준 | 3차 | 4차 | 판정 |
|---|---|---|---|
| SSOT | 7 | **7** | 그대로. C11(겹침 기록 하나뿐)·C12(줌 수명 두 곳)·C4·C5, 새 낮음 C18(장면 채널 빈 유닛 약속 두 곳) |
| SOLID | 9 | **9** | 그대로. C13(값 인자 16개) |
| FW 지침·계약 | 9 | **9** | 그대로. C8(효과 덮어쓰기 규칙·두 시계) |
| 변경 독립성 | 9 | **9** | 그대로. C12·C13 |
| **종합** | 8.5 | **8.5** | 3차 판단이 지금 코드에서도 맞다 |

### 4차 새 발견

| # | 등급 | 기준 | 내용 | 권장 |
|---|---|---|---|---|
| C18 | 낮음 | SSOT | 장면 채널 요청은 유닛이 비어 있다는 약속을 조립(`LeaseCameraZoom`이 `EntityId.Empty`로 적음)과 대여 묶음(`CameraZoom()`이 `EntityId.Empty`로 찾음)이 따로 안다(L-2 검토 낮음 2). 장면 채널이 하나라 지금은 어긋날 일이 없다 | 장면 채널이 늘 때 찾기를 "장면 채널이면 유닛을 보지 않음"으로 바꾼다 |
| C19 | 참고 | SSOT(문서) | 흔들림 느낌을 정하는 손잡이가 세 곳이다 — 설정 표(사건별 세기·시간·모양), `Stage.prefab` Listener(Gain·Duration·Secondary Noise), 노이즈 프로필(`MapCameraShakeNoise`). 서로 다른 사실이라 중복은 아니지만 한눈에 보는 안내가 없다 | 카메라 사용 안내(viewDocs)를 만들 때 세 곳과 겹침 규칙(C11·C16)을 함께 적는다(1차 C2 권장과 같은 문서) |
| C20 | 참고 | SOLID-I | `CombatTrainCamera.IsZooming`은 시험만 읽는다 | 수용(창구 `CanZoom`으로 대신 볼 수 있으나 의도가 덜 드러남) |

### 남은 일 순서(권장)

1. **C11**(겹침 기록 하나뿐): 동작이 조금 바뀌므로 결정 필요. 진행 중 흔들림을 작은 목록으로 두고 "가장 센 것보다 세야 보냄".
2. **C14·C13**(넣는 조건 반복, 값 인자 16개): 다음에 카메라 값·사건을 더할 때 함께.
3. **C12**(줌 수명 두 곳): 세 번째 사용처나 효과의 끝 알림이 생길 때.
4. **C19·C2**(안내 문서): 카메라 viewDocs 한 장.

## 3차 (2026-10-19, 대여 분리 L-0~L-2 뒤)

[대여 분리 계획](../plans/2026-10-19-combat-piece-run-leases-plan.md)으로 C6·C7을 풀었다. 동작 동일이며, 검토 에이전트 두 번(L-1·L-2) 모두 높음 0이었다.

| 기준 | 2차 | 3차 | 바뀐 이유 |
|---|---|---|---|
| SSOT | 7 | **7** | 그대로(C11·C12·C4·C5). 대여의 빌리기·꺼내기·되돌리기는 묶음 한 곳, 장면 채널 판정은 `IsSceneChannel` 한 곳("100번대") |
| SOLID | 7 | **9** | C6 해소: 실행 642 → 499줄, 대여는 `CombatPieceRunLeases`(166줄). C7 해소: 유닛·장면 채널이 조립 목록·대여 기록 모두 한 모양. −1: C13 그대로 |
| FW 지침·계약 | 9 | **9** | 그대로(C8). 구체 클래스 하나·`switch` 하나, 채널 표·인터페이스 없음(§27). 되돌리기 대리자는 사건마다(§19) |
| 변경 독립성 | 8 | **9** | 새 유닛 채널(클립 `Clip`) = 열거 값 + 묶음 한 칸 + 꺼내기 한 줄. 새 장면 채널 = 100번대 값 + 한 칸 + 조립 전용 함수. −1: C12·C13 그대로 |
| **종합** | 7.8 | **8.5** | |

- 남은 것: C11(겹침 기록 하나뿐, 결정 필요), C12(줌 수명 두 곳), C13(카메라 값 인자 17개), C14(넣는 조건 반복), C4·C5.
- 다른 계획에 남긴 것: 폴더 정리 계획 §3에 새 파일 한 줄(M 착수 때), 클립 AC-2는 `CombatPieceRunTests` 시험 가짜의 새 플래그 둘(`ThrowOnScaleRestore`·`ThrowOnRestore`)을 남겨야 한다.

## 2차 결론

| 기준 | 1차 | 2차 | 판정 | 깎은 이유 |
|---|---|---|---|---|
| SSOT | 8 | **7** | 대체로 지킴 | 겹침 규칙·세기 → 효과 값 변환·필드 구간·칸 몸·면 거리 식은 한 곳씩이다. −1: 그림자 사본이 하나 늘었다 — 진행 중 흔들림 기록이 하나뿐이라 짧고 센 흔들림이 길고 약한 흔들림을 덮어 잊는다(C11, C2와 같은 종류). −1: 줌 수명 규칙(대여 → 줌 → 자기 시계 → 반환)이 조각 실행과 기차 카메라 두 곳(C12). −1: 1차의 C4·C5 그대로 |
| SOLID | 7 | **7** | 지킴, 주의 둘 | 기차 쪽은 잘 나뉘었다 — 접촉 예약은 시각만 알리고(카메라 모름), 무엇을 낼지는 `CombatTrainCamera`, 연결은 재생 조정. 창구 시계는 생성자 주입. −2: C6(`CombatPieceRun` 642줄) 그대로. −1: 카메라 값 생성자 인자 17개를 자리로 넘김(C13) |
| FW 지침·계약 | 9 | **9** | PASS | Framework·Render 변경 0(작업 트리 확인), 새 Service·Key·AddModule 0, Factory 변경은 시계 람다 한 줄과 인자 전달뿐. 시간 기준이 같다 — 창구 시계 `Time.time` = Cinemachine 임펄스 시간(`CinemachineCore.CurrentTime`, 덮어쓰기·`IgnoreTimeScale` 없을 때), 연출 Tick = 부트의 `Time.deltaTime`. −1: C8 그대로 |
| 변경 독립성 | 8 | **8** | 좋음 | 1차 감점 "출처당 한 번 규칙 없음"은 K-4 겹침 규칙으로 풀렸다. 값·축(노이즈)·끄기는 에셋만. −1: C6·C7 그대로. −1: 새 카메라 값 하나에 다섯 곳(설정 필드·접근자·생성자 인자·`From`·속성, C13)과 줌 규칙 두 곳(C12) |
| **종합** | 8.0 | **7.8** | | 네 기준 평균. SSOT만 내려갔다 |

발견: 높음 0, 중간 1(C6, 그대로), 새 낮음 4(C11~C14), 새 참고 3(C15~C17). 해소: C9(Unity가 `CombatMapCameraTests`를 가져옴 — `Library/ScriptAssemblies`에 있음), C10(Listener가 붙어 흔들림 값 오류가 첫 흔들림에 드러남), 1차 독립성 감점(출처당 한 번 규칙).

### 2차 SSOT 표(새 사실만)

| 사실 | 원본 | 다른 곳 | 판정 |
|---|---|---|---|
| 흔들림 겹침 규칙 | `CombatCameraWindow.Shake` | 없음(포효·피격·사망·기차 모두 창구를 지남) | 지킴 |
| 세기 → `CameraShakeData` 변환 | `CombatCameraMotionSettings.ShakeValues.Build` | 없음 | 지킴 |
| 피격·사망·기차 흔들림 값, 기차 줌 값 | View DB 설정 표 + 에셋 | 없음(카메라 값 클래스는 복사만) | 지킴 |
| **진행 중 흔들림** | Cinemachine 임펄스 목록 | 창구의 끝 시각·세기 하나 | **손실 있는 사본(C11)** |
| 기차 필드 구간(진입·이탈) | 경로 + 레일 칸에서 파생 | `RailwayContactSchedule.TryGetFieldSpan` 한 곳에서 계산 | 지킴(P7) |
| 면 거리 식 | `CombatContactGeometry`(`Reach` 공유) | 접촉·이탈이 같은 식 | 지킴 |
| 칸 몸(칸 크기 사각형) | `FieldGridMapper.CellBody` | 없음 | 지킴 |
| **줌 수명 규칙** | 조각 실행 대여 + `CombatCameraZoomPiece` | `CombatTrainCamera` | **두 곳(C12)** |
| 넣는 조건(창구 있음·값 있음·낼 수 있음) | 설계 K7 | 포효 둘·피격·사망·기차 카메라 다섯 줄 | 같은 식 반복(C14, DRY) |
| 시간 기준 | Unity `Time.time`/`deltaTime` | 창구 시계(Factory 람다)·Cinemachine | 같은 값(C15 참고) |

### 2차 발견 사항

| # | 등급 | 기준 | 내용 | 권장 |
|---|---|---|---|---|
| C6 | 중간 | SOLID-S, 독립성 | (1차 그대로) `CombatPieceRun` 642줄, 대여 묶음 분리 전 | 클립 AC-3 전에 실행 대여 묶음 분리. 계획 [piece-run-leases-plan](../plans/2026-10-19-combat-piece-run-leases-plan.md)(L-0~L-3, C7·조립 요청 통일 포함, 2026-10-19 작성) | → **3차에서 해소**
| C11 | 낮음 | SSOT·정확성 | 창구가 진행 중 흔들림을 하나(끝 시각·세기)만 기억한다. 길고 약한 흔들림 중에 짧고 센 흔들림이 오면 기록이 그것으로 바뀌고, 그것이 끝나면 아직 진행 중인 약한 흔들림을 잊는다. 예: 코드 기본값 기차 .2·1.5초 중 사망 .3·.3초 → .3초 뒤 피격 .15가 통과(지금은 기차 중 피격이 없어 드러나지 않음) | 진행 중 흔들림을 작은 목록으로 두고 "아직 진행 중인 것 중 가장 센 것보다 세야 보냄"으로 바꾼다. 규칙(Q-K8 B)의 문구는 그대로지만 동작이 달라지므로 결정 뒤 |
| C12 | 낮음 | SSOT·독립성 | 줌 수명 규칙(빌림 → 줌 → 자기 시계로 끝 → 반환, 취소 시 기준값 0초)이 조각 실행·줌 조각과 `CombatTrainCamera` 두 곳에 있다. 원인은 기차가 조립이 아닌 전용 재생이라서(Q-C3 A) | 지금은 둘 다 작아 두는 것을 권장. 세 번째 사용처가 생기거나 효과에 끝 알림이 생기면(C8) "줌 시간표" 도우미 하나로 모은다 |
| C13 | 낮음 | SOLID·독립성 | `CombatCameraMotionSettings` 생성자 인자 17개, `From`이 자리로 넘긴다. 카메라 값 하나를 더하면 설정 필드·접근자·생성자 인자·`From`·속성 다섯 곳을 고친다. 순서 실수는 `From` 복사 시험(값마다 다른 수)이 잡는다 | 효과별 값 묶음(흔들림 셋·줌 셋)을 인자로 받거나 `From`이 묶음을 바로 만들게 바꾼다. 다음 카메라 값을 더할 때 함께 |
| C14 | 낮음 | DRY | 넣는 조건 `camera != null && motion != null && Has… && camera.Can…`이 포효 둘·피격·사망에 같은 모양으로 있고, 기차 카메라도 비슷하다 | 흔들림 조각 쪽에 "조건이 맞으면 조립에 더함" 도우미 하나. 규칙이 바뀔 때(예: 진영별 끄기) 한 곳만 고치게 |
| C15 | 참고 | SSOT(시간) | 창구 시계는 Factory 람다 `Time.time`, Cinemachine은 `CinemachineCore.CurrentTime`. 지금은 같지만 `CurrentTimeOverride`·`IgnoreTimeScale`을 켜면 어긋난다 | 수용. 그 설정을 쓰게 되면 시계를 Cinemachine 시간으로 바꾼다 |
| C16 | 참고 | 규칙 | 겹침 규칙의 "세기"는 속도 크기라 모양 차이(Explosion 첫 튐 1.4배, Rumble 가운데 최대)를 보지 않는다. 지금 에셋은 피격·사망·포효·기차가 모두 Explosion이고, 기차 세기 3이 사망 .3보다 커서 기차가 흔드는 1.5초 동안 치인 유닛의 사망 흔들림이 막힌다 | 의도 확인. 설정 툴팁에 "세기는 모양과 무관"을 적는 것을 권장 |
| C17 | 참고 | 검증 | Unity 전용으로 남은 것: 접촉 예약 진입·이탈 알림 시험 5개, 기차 줌·흔들림 재생, 동시 피격 한 번 흔들림 재생. K-4~K-6 검토 에이전트는 돌리지 않았다(사용자 지시) | Unity 묶음 결과로 확인. 검토 에이전트는 원하면 따로 |

### 2차 잘 된 점

- 기차 쪽 책임이 깔끔하게 나뉘었다. 접촉 예약은 "언제"(진입·이탈 시각, 사망과의 순서)만, `CombatTrainCamera`는 "무엇을"(흔들림·줌), 재생 조정은 연결만 한다. 줌아웃을 "열어 두기"로 남긴 자리(진입 알림)를 K-6이 그대로 썼다.
- 면 거리 식을 `Reach` 하나로 접촉·이탈이 함께 쓰고, 필드 구간은 순수 함수 하나에서 나와 관리 실행으로 시험된다.
- 겹침 규칙이 창구 한 곳이라 포효·피격·사망·기차가 같은 규칙을 탄다. 시계를 주입해 가짜 시계로 시험한다.
- 여러 축 떨림은 코드가 아니라 Listener 노이즈 프로필(에셋)로 풀었다. Framework·우리 코드 변경 없이 축을 바꿀 수 있다.

---

# 1차 평가 기록 (K-1~K-3)

- 작성일: 2026-10-19. 코드 변경 없음(검사만).
- 사용자 요청: "K-3까지 Phase 모두 마무리 되면 SSOT(한 사실은 한 곳에만)와 SOLID, JMH Framework 개발 지침·FW 계약, 기능 변경의 독립성으로 카메라 관련 작업 내용 검사해"
- 기준: [설계 세션 인계](../handoffs/2026-09-30-combat-view-design-session-handoff.md) §4(4.1 SSOT, 4.2 SOLID, 4.3 FW 계약, 4.6 변경 독립성).
- 대상: [카메라 연출 조각 설계](2026-10-19-combat-camera-pieces-design.md)와 [계획](../plans/2026-10-19-combat-camera-pieces-plan.md) K-1~K-3 코드. 경로 약칭 `C/` = `Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/ViewSystem/Combat/`.
  - 새 코드: `C/Display/CombatCameraWindow.cs`·`C/Display/ICombatCameraZoom.cs`·`C/Display/CombatMapCameras.cs`·`C/CombatCameraZoomPiece.cs`·`C/CombatCameraShakePiece.cs`·`C/CombatCameraMotionSettings.cs`
  - 바꾼 코드: `C/CombatPieceRun.cs`·`C/CombatPieceContext.cs`·`C/CombatComposition.cs`·`C/CombatPieceRunPlayer.cs`·`C/CombatPieceStart.cs`·`C/ICombatPresentationPiece.cs`·`C/CombatFacingAttackPiece.cs`·`C/CombatAttackPlayback.cs`·`C/CombatRoarEventHandler.cs`·`C/CombatSceneEventPresenter.cs`·`C/CombatDisplayServices.cs`·`C/Display/StageViewPropPresenter.cs`·`CombatSceneDisplayFactory.cs`·설정 Data·설정 에셋
- 검사 중 검토 에이전트 세 번(K-1·K-2·K-3)의 지적은 이미 반영했다(계획 §6). 이 문서는 그 뒤 상태를 본다.

## 1. 결론

점수는 1~10(사용자 요청, 10 = 지적할 것이 없음)이다.

| 기준 | 점수 | 판정 | 깎은 이유 |
|---|---|---|---|
| SSOT | **8** | 대체로 지킴 | 값·조건·순서·신호 규칙은 한 곳씩이다. −1: "지금 줌 배율"의 그림자 사본(C2, Framework에 조회가 없어 불가피). −1: 줌 시간 검사 중복(C4)과 클립 계획 문서의 낡은 신호 번호(C5) |
| SOLID | **7** | 지킴, 주의 하나 | 창구·조각·조립이 나뉘고 Framework는 계약으로만 쓴다. −2: 조각 실행 `CombatPieceRun`이 470 → 642줄, 시간·신호·미도착·완료·대여 다섯 책임(C6). −1: 대여 경로가 유닛 `switch`와 장면 경로 두 모양(C7) |
| FW 지침·계약 | **9** | PASS | Framework·Render 파일 변경 0, 새 Service·Key·AddModule 0, 공개 `Dispatch`와 구체 DTO만 사용, 할당은 사건마다. −1: 효과의 빈 곳을 감싸느라 덮어쓰기 규칙·두 시계에 기댄다(C3·C8) |
| 변경 독립성 | **8** | 좋음 | 값은 에셋, 카메라 추가는 0곳, 새 신호는 열거 값 + 조각. −1: 피격 흔들림에 필요한 "출처당 한 번" 규칙이 없다. −1: 새 장면 채널이 조립·실행·맥락·재생기·창구 다섯 곳에 닿는다(C6·C7) |
| **종합** | **8.0** | | 네 기준 평균 |

발견: 높음 0, 중간 1(C6), 낮음 5, 참고 4. 점수를 가장 많이 올릴 수 있는 것은 C6(실행 대여 묶음 분리, SOLID 7 → 9 예상)이다.

## 2. SSOT — 한 사실은 한 곳에만

| 사실 | 원본 | 다른 곳 | 판정 |
|---|---|---|---|
| 맵 카메라 이름 | Stage.prefab 오브젝트 이름 | 창구가 프랍을 세울 때 읽은 사본 | 지킴(파생 사본, 우리 쪽 문자열 없음) |
| 맵 카메라 기준 렌즈값 | 프리팹 버추얼 카메라 렌즈 | 창구 사본 | 지킴(세울 때 한 번 읽음) |
| 흔들림 채널 | 프리팹 Listener 채널 마스크 | 창구가 합친 값 | 지킴 |
| **지금 줌 배율** | 실제 렌즈(Cinemachine) | 줌 대여의 기록 배율 `_ratio` | **그림자 사본(C2)** |
| 줌 보간 곡선 | `CameraZoomEvent`(SmoothStep) | 없음 | 지킴 |
| 줌 끝 시각 | 효과의 보간 시계(`DelayService`) | 줌 조각의 자기 시계 | 두 시계(C3, 설계 K2에서 수용) |
| 흔들림 값 조건 | `CameraShakeEvent` | 없음(결정 A) | 지킴 |
| 줌 시간 조건 | `CameraZoomEvent`(`Duration` 유한·0 이상) | 대여 `ZoomTo`의 `ValidateSeconds`, 줌 조각 생성자의 `ValidateSeconds` | **중복(C4)** |
| 줌 배율·흔들림 세기 조건 | 설정 Data 조건 함수 | `Validate`·`CombatCameraMotionSettings` 생성자·조각·대여가 같은 함수를 부름 | 지킴 |
| 카메라 연출 값 | View DB 설정 표(+ 에셋) | 없음. 줌 시간은 포효 맥박 절반에서 계산(P7) | 지킴 |
| 포효 순서(카메라 항목 포함) | `CombatRoarEventHandler.Compose` | 없음 | 지킴 |
| 신호 규칙(시간·미도착·입구) | 조각·조립 설계 BP2 보충 | 실행 머리 주석은 요약·가리킴 | 지킴 |
| 신호의 뜻 | 클립 설계 AC6 신호 표 | `CombatPieceStart` 열거 문서는 요약 | 지킴. 단 클립 계획 T15·T16이 낡음(C5) |
| 화면 전환 대상 카메라 이름 | `CombatPhaseTransitionSettings`의 문자열(민형님 코드) | 프리팹 오브젝트 이름 | 기존 중복(C1, 우리 범위 밖) |

## 3. SOLID

| 원칙 | 판단 | 근거 |
|---|---|---|
| S 단일 책임 | 지킴, 주의 하나 | 창구 = 전투 카메라 자원(맵 카메라 사실·줌 대여·흔들림 포트) 하나의 책임. Unity 컴포넌트 읽기는 `CombatMapCameras`로 나눠 창구는 관리 실행으로 시험된다. 조각은 자기 시계만, 순서는 조립만 안다. **주의: `CombatPieceRun`이 시작·시간 규칙, 신호·미도착, 완료, 유닛 대여, 장면 대여를 함께 가진다(C6)** |
| O 개방-폐쇄 | 지킴 | 새 카메라 연출 = 조각 + 조립 한 줄. 새 신호 = 열거 값 + 내는 조각(K-2 일반화, 클립 AC-3이 그대로 씀). 새 장면 채널은 조립·실행·맥락·재생기를 고친다(설계에서 예상한 공통 경계). 유닛 채널은 `switch`, 카메라는 따로 된 경로라 모양이 둘이다(C7) |
| L 리스코프 | 지킴 | `ICombatCameraZoom`은 유닛 대여와 같은 보장(버전, 취소 콜백, 늦은 되돌림 무시)을 지키고 시험이 있다. 카메라 조각은 BP1 계약(시작·Tick·끝·취소)을 지킨다. 정면 공격 조각은 두 모션 종류 모두 `Hit`을 `End`보다 먼저 낸다(K-2 검토 확인) |
| I 인터페이스 분리 | 지킴 | 조각은 맥락에서 필요한 것만 받는다(`CameraZoom()`·`SignalCallback`). 맥락 멤버가 늘었지만 각각 좁다 |
| D 의존성 역전 | 지킴(관례 따름) | 창구는 `ICameraEffectService` 계약에 의존한다. 흔들림 조각·포효 처리기는 구체 창구를 받는다. 소리 조각이 구체 `CombatEventAudio`를 받는 것과 같은 관례이고, 시험은 진짜 창구에 가짜 효과만 넣어 돈다 |

## 4. JMH Framework 개발 지침·FW 계약

| 항목 | 판정 | 근거 |
|---|---|---|
| FW-001~003 서비스·모듈·Builder | PASS | 새 Service·ServiceKey·`AddModule`·Builder 없음. Factory는 기존 서비스 묶음에 인자 하나(`cameraEffects`)를 더했다 |
| FW-004 내부 상태 우회 금지 | PASS | 렌즈는 프랍 인스턴스의 참조에서 읽기만 한다(카탈로그 문서가 허용한 "가진 참조에서 설정 복사"). 쓰기는 모두 `Dispatch` |
| FW-010~012 렌더 DTO·대여 반환 | PASS | 구체 DTO(`CameraZoomData`·`CameraShakeData`)를 그대로 쓴다(흔들림 값 중복은 K-1 중 걷어냄). 줌 대여는 실행 끝·취소에서 되돌린다. 카탈로그 핸들은 효과가 요청마다 빌리고 반환한다 |
| Framework 수정 금지(사용자 조건) | PASS | `CameraGroup`·`CameraEffectSystem`·카탈로그·`DelayService` 변경 0 |
| §14 문자열 조회 최소화 | PASS | 카메라 이름 문자열을 코드에 적지 않는다(프랍 오브젝트에서 읽음). 이름 조회는 카탈로그 API의 방식이다 |
| §19 할당 | PASS | 사건마다(조각, 카메라별 `CameraZoomData`, `Dispatch` 박싱, 시작 안 신호가 있을 때의 목록). 프레임마다 할당 없음 |
| §20·21 구독 수명 | PASS | 이벤트 구독 없음. 콜백은 실행 세대 안에서만 유효 |
| §25 GetComponent/Find | PASS | 자기 프랍 인스턴스 안에서 세울 때 한 번 |
| §27 과도한 추상화 | PASS | `ICombatCameraZoom`은 구현이 하나지만, 창구 안에 숨긴 대여 클래스를 좁게 내보내는 용도이고 유닛 채널 대여 셋과 같은 모양이다 |
| Framework 동작에 기대는 것 | 참고(C8) | 취소·되돌림은 "같은 카메라의 새 줌이 앞 줌을 덮어씀" 규칙에 기댄다. 덮어쓴 줌의 지연 예약은 원래 시간까지 `DelayService` 큐에 남는다(효과 문서). Framework의 빈 곳(끝 알림·취소·렌즈 조회)은 민형님 메모 §4에 "요청 아님"으로 적었다 |

## 5. 기능 변경의 독립성

| 시나리오 | 고칠 곳 | 이해할 코드 | 검증 | 판정 |
|---|---|---|---|---|
| 포효 줌 배율·흔들림 값 바꾸기 | 설정 에셋 값 | 없음 | 포효 재생 | 좋음 |
| 소환 착지에 흔들림 넣기 | 소환 조립 한 줄 + 값 | 조각 계약, 흔들림 조각 | 소환 조립 시험 + 재생 | 좋음 |
| 피격에 흔들림 넣기 | 피격 조립 + "출처당 한 번" 규칙(새로 정해야 함) | 동시 처리 묶음, 조각 계약 | 동시 피격 재생 | 보통(규칙이 아직 없음) |
| 맵 프랍에 카메라 추가 | 없음 | 없음 | 재생 | 좋음 |
| 맵 카메라 이름 바꾸기 | 우리 쪽 없음. 화면 전환 설정(민형님)·시험 러너 값은 바꿔야 함 | — | 전환 재생 | 우리 쪽 좋음(C1) |
| 클립 신호(`ClipStart`·`ClipEvent`) 더하기 | 열거 값 + 클립 조각 | 신호 입구 | 신호 시험 | 좋음(K-2 일반화 재사용) |
| 카메라 이동 같은 새 효과 | Framework(민형님) + 창구 함수 + 조각 | 창구 | 창구 시험 + 재생 | 예상대로(Framework 범위) |
| 새 장면 채널(예: 카메라 위치) | 조립·실행·맥락·재생기·창구 | 실행의 대여 경로 | 실행 시험 전체 | 보통(C6·C7) |

남는 공통 경계: ① 신호 열거와 실행의 신호 처리, ② 실행의 장면 채널 경로, ③ 창구 계약(`CanZoom`·`TryBeginZoom`·`CanShake`·`Shake`). 이것을 바꾸면 모든 조립에 닿는다.

## 6. 발견 사항

| # | 등급 | 기준 | 내용 | 권장 |
|---|---|---|---|---|
| C6 | 중간 | SOLID-S, 독립성 | `CombatPieceRun`이 470 → 642줄. 시간 규칙·신호·미도착·완료에 대여(유닛 셋 + 장면 하나)까지 한 클래스다. 클립 AC-3이 `Clip` 채널을 더하면 대여 분기가 또 는다 | 대여 빌리기·되돌리기·접근자를 "실행 대여 묶음" 클래스로 뽑는다. 동작 동일 리팩터링이라 클립 AC-3 착수 전에 하는 것이 좋다(새 계획 필요) | → **3차에서 해소**
| C2 | 낮음 | SSOT | "지금 줌 배율"을 대여가 기록한다(`_ratio`). 실제 값은 렌즈에 있고 Framework에 조회가 없어 사본이 불가피하다. 다른 곳이 맵 카메라 줌을 바꾸면 기록이 어긋난다(지금 다른 사용자 없음) | 규칙 "맵 카메라 줌은 `CombatCameraWindow`로만"을 창구 주석에 적었다(이 검사 때). 카메라 사용 안내(viewDocs)를 만들 때 같이 적는다 |
| C4 | 낮음 | SSOT | 줌 시간 검사가 `CameraZoomEvent`와 대여 `ZoomTo` 두 곳(조각 생성자 검사는 조각 시계를 지키는 용도라 별개) | 사용자 결정으로 두었다(2026-10-19 "그냥 두고"). 흔들림처럼 Framework 하나로 줄이려면 `ZoomTo`의 한 줄만 지우면 된다 |
| C5 | 낮음 | SSOT(문서) | 클립 계획 T15·T16이 `ClipStart = 2`와 없어진 `_endRequested`를 가리키고, K-2가 이미 한 일(Trigger·미도착·머리 주석)을 다시 하라고 적혀 있다 | 클립 작업 세션이 착수 때 고친다(다른 세션 문서라 여기서 안 고침). 번호는 3·4, 일반화는 BP2 보충을 쓴다 |
| C7 | 낮음 | SOLID-O | 실행의 대여가 유닛 채널(`switch`)과 장면 채널(따로 된 경로) 두 모양이다 | C6에서 함께 정리 |
| C3 | 낮음 | SSOT | 줌 끝을 조각 시계와 효과 시계가 따로 센다(최대 한 프레임 차이, 다음 줌이 기록 배율에서 시작해 튀지 않음) | 수용(설계 K2). 재생 배속이 생기면 다시 본다 |
| C1 | 참고 | SSOT | 화면 전환 설정(민형님 코드)·시험 러너에 Day/Night 이름 문자열이 있다. 우리 카메라 쪽은 이름을 프랍에서 읽어 영향이 없다 | 민형님 영역. 공유할 때 알린다 |
| C8 | 참고 | FW | 취소·되돌림이 효과의 덮어쓰기 규칙에 기대고, 덮어쓴 예약이 큐에 남는다 | 수용. 효과 쪽에 취소가 생기면 창구의 되돌림을 줄인다(민형님 메모 §4) |
| C9 | 참고 | 검증 | 맵 카메라 읽기·프랍 Presenter Unity 시험 5개를 Unity가 가져오지 않아 K-1·K-2 Unity 확인에서 빠졌다. 파일 이름을 바꿔 다시 가져오게 했다 | K-3 Unity 확인 때 Test Runner에 `CombatMapCameraTests`가 보이는지 확인 |
| C10 | 참고 | 검증 | 결정 A로 흔들림 시간·모양의 잘못된 값은 처음 흔들 때(타격 순간, 공격 재생기 Tick 쪽) 예외다. Listener가 붙기 전에는 드러나지 않는다 | 수용. Listener가 붙은 뒤 포효 재생으로 확인 |

## 7. 잘 된 점

- Framework를 하나도 고치지 않고, 효과의 빈 곳(끝·취소·렌즈 조회)을 View 창구 한 곳이 감쌌다. 감싼 방법은 민형님 메모에 적어, 효과 쪽에 기능이 생기면 줄일 자리가 분명하다.
- 흔들림 값 형식과 검사 중복을 사용자 지적으로 걷어내 Framework 형식을 그대로 쓴다.
- 신호 일반화(K-2)는 신호 종류에 매이지 않아, 클립 작업이 열거 값만 더하면 된다. 검토에서 HEAD에도 있던 일반 완료 누락 결함을 찾아 함께 고쳤다.
- 카메라 항목이 없으면 포효가 이전과 같다(창구 없음·값 없음·줌 대여 중·수신기 없음, K-3 검토 확인). 카메라 항목이 있어도 끝 프레임과 끝 순서가 같다.

## 8. 검증 상태

- MSBuild 오류 0, 카메라 작업의 새 경고 0(기준선 대비 새 경고는 바깥 변경 `FieldOutlineRenderer.cs` CS0162뿐).
- 관리 실행: 기준선(HEAD `a0f9cd95` + 제 파일은 변경 전 판) 대비 기존 시험 상태 변화 0, 새 시험 55개(PASS 50, Unity 전용 5). 기록은 계획 §6.
- Unity: K-1·K-2 묶음 문제 없음(사용자 확인, 단 C9의 5개 빠짐). K-3 문제 없음(사용자 확인).
