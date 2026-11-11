# StageMap 통합 진입 연출의 조립·로딩·자원 소유권 — 2026-11-06

- 목적: Framework/View 작업자에게 노드 낙하·연결선 그리기·카메라 왕복의 연결 변경을 공유한다.
- 전달 상태: **미전달**. 외부 메시지 발송 없음.
- 범위: [통합 연출 계획](../plans/2026-11-06-stage-map-intro-plan-Hojin.md) Phase 1 MI1~MI8. 코드 작성·정적 검토 완료, **컴파일 미확인**. Unity 실행 결과는 같은 계획의 U1~U9에 별도 기록한다.
- 선행 연결: [노드 낙하 공유 메모](2026-11-06-stage-node-drop-framework-share-Hojin.md). 기존 효과 서비스·로딩 준비 어댑터를 이어 사용한다.

## 생성과 서비스 경계

`StageMapSceneFactory.Create` 내부에서 Drop·Link·CameraTour·Intro·InteractionPolicy를 생성하여 Scope에 주입한다. Factory는 조립하고, 각 표현의 계산은 해당 부품이 맡는다. 공개 Factory/`CreatePreparation` 계약과 `StageMapLoadingPreparation`은 유지하며 새 서비스·키·Group·입력 Layer를 등록하지 않는다.

`StageMapViewSystem` 수명의 `StageNodeDropHistory`는 전체 연출을 담당하는 `StageMapIntroHistory`로 이름을 바꾸었다. 기존 `.meta` GUID와 Run/챕터별 이력 동작을 유지하고 `StageMapViewServices.IntroHistory`로 전달한다. 낙하·선·카메라별 이력이나 별도 누적 시계를 만들지 않는다. 공통 시간표는 `StageMapIntroTimeline`, 경과 시간과 전체 완료는 `StageMapIntroPresentation`이 소유한다.

기존 Global Context의 카메라 Catalog·색상 Handle·`RenderServiceCommand.CoreGroup.EffectSystemServices` 연결을 사용한다. Effects의 System 수명과 씬별 효과 소유 세대도 유지한다. 직접 Store/Pool에 접근하거나 공용 Camera/Render/Loading에 StageMap 분기를 추가하지 않는다.

## 로딩 준비와 입력

기존 EnterLoaded·AfterLoad 카메라 연결·로딩 준비·Reveal 순서를 유지한다. 준비는 Run/맵 확인 → 배경/노드/현재 마커 → 노드 위치와 대상 수집 → 카메라 범위 → 초기 에디터 구도/범위 재계산 → 핸들 확인 → 시간표 → Intro 준비 순이다. 초기 구도 준비는 사용자의 카메라 입력과 분리하고 최신 Run/맵/준비 버전을 검사한다.

핸들 미준비 등의 **정상 Pending은 준비 버전을 바꾸거나 전체 연출을 취소하지 않는다.** 아직 인수하지 않은 대상만 복원하고 같은 버전으로 재시도한다. 실제 취소·문맥 상실·실패만 준비를 무효화한다. 복원 실패는 Failed로 보고한다. 준비 Complete는 표시·숨김·시작 구도가 준비됐다는 뜻이며, 로딩 종료나 연출 완료를 기다리지 않는다.

로딩 종료 후 Intro가 공통 scaled 시각을 Drop → Link → CameraTour에 전달한다. 설정 창에서 모두 정지하며, 취소는 정지 상태에서도 즉시 처리한다. 전체 완료는 낙하·선·카메라 복귀·효과 정리가 모두 끝난 시점이다.

`StageMapIntroInteractionPolicy`의 임시 차단을 Scope의 기존 준비/오류/Layer/팝업 조건과 조합한다. 일반 선택·호버·배경 드래그·휠과 에디터 Alt 입력을 함께 막고 차단 중 누름은 release까지 유지한다. 설정 창은 기존 Common 입력 경로로 연다. 전체 완료 후 보류 팝업 복원 → 증강/선택 → 호버 순서를 유지한다. 후속 기능은 추가 정책을 교체할 수 있으며, 기존 유효성 검사와 카메라 단일 소유권은 별개다.

## 자원과 취소 소유권

| 자원 | 소유·반환 |
|---|---|
| 선 원본 Mesh/Material/프리팹 | 기존 에셋을 읽기만 함. 전체 Transform과 점선 패턴 유지 |
| 선 임시 Mesh | `StageLinkView`가 생성·파괴. 원본 정점/UV를 보관하여 도착 쪽만 보간하고 완성/취소 시 원본 재연결 |
| 선 조작 대상 | Presentation이 빌려 쓰는 세대별 계약. 만료된 대상은 새 표시를 수정하지 않음 |
| 색상 Handle | 기존 선 View가 반환. 임시 메시를 먼저 정리한 후 Handle·표시 오브젝트 정리 |
| 착지 효과 | 기존 `StageNodeDropEffects`/EffectScope 사용. 해당 세대의 `StopAll`로 반환하고 자기 받침만 파괴. 빌린 파티클 직접 파괴 없음 |
| 카메라 | 기존 `StageMapCameraDrag`의 Catalog 대여 하나 유지. 좁은 자동 조작 계약과 세대 토큰으로 소유하고 복귀 시 수동 offset도 복원 |

자동 왕복은 기존 각도·렌즈·가로 구도를 유지하고 실제 보스 위치로 진행축 offset을 계산한다. 준비 중 플레이어가 보이도록 필요한 보정을 한 뒤 시작 구도를 보관한다. 자동 소유 중 수동 입력과 PreviewFraming 적용을 막으며 EditorPrefs/저장 구도를 쓰지 않는다. 새 카메라 대여나 Camera Transform 직접 쓰기를 추가하지 않는다.

취소는 상태/준비 세대 무효화 → 효과 반환/노드 복원 → 선 복원 → 자동 카메라 반환 → Scene 표시 파괴/핸들 반환 순이다. 정리 예외를 모아 기존 오류 경계로 보고하며, 실패를 정상 완료로 바꾸지 않는다. 현재 Scope는 시작 구도를 복원하고 Supersede된 Scope는 새 카메라를 복원하지 않는다.

## 정적 검토와 확인 인계

SSOT·SOLID 5원칙·JMH/FW와 변경 독립성을 변경 코드에 대조했고, 별도 읽기 전용 검수에서 확정 수정 지적은 없었다. 선 표현·낙하 수식·카메라 보간·임시 입력 정책은 각 부품에서 변경할 수 있다. 공통 시간표·이력·수명 변경은 Intro/Scope 통합 확인이 필요하다.

Core·Buffer 구현·FieldCamera·Architecture·공용 Camera/Render/Loading·셰이더·연결선 원본 에셋은 이번 통합 연출에서 수정하지 않았다. 선행 낙하의 사용자 기본 재생 확인과 새 통합 연출의 검증 상태를 구분한다.

사용자는 [계획 U1~U9](../plans/2026-11-06-stage-map-intro-plan-Hojin.md#사용자-확인-u1u9)의 최초 전개·점선 고정·정확한 왕복·입력 차단·정지·이력·설정 경계·취소/오류·기존 DB 검증을 Unity에서 확인한다. 컴파일, 실제 구도와 점선 표현, 콜백 재진입·반환 시나리오는 정적 검토로 통과를 확정하지 않았다.
