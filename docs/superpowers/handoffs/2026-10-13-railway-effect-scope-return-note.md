# Railway 연출의 EffectScope 반환 실패 계약 확인 요청

작성일: 2026-10-13. 대상: Render 작업자. 상태: 미전달, Render 코드 변경 없음.

Railway View RW3는 기존 `IEffectSystemServices.GetEffectHandle()`로 사건 Scope를 만들고, 열차 앵커마다 `Play`한 뒤 완료·취소 때 `StopAll()`로 반환한다. Empty는 요청하지 않는다. [Task 계획의 RW3 기록](../plans/2026-10-12-railway-view-plan.md)을 따른다.

## 확인한 경계

[EffectScope.StopAll](../../../Assets/ProjectL/Script/Systems/RuntimeSystem/GlobalSystem/RenderSystem/Core/EffectSystem/EffectScope.cs)은 역순 반환 중 `ReturnElementNode`가 예외를 던지면 루프와 마지막 `elements.Clear()`가 중단된다. 앞서 반환된 노드와 미반환 노드를 호출자가 공개 계약으로 구분할 수 없다. Scope 전체를 다시 반환하면 앞서 반환된 노드를 중복 반환할 가능성이 있다.

RW3 네이티브 검사에서는 테스트 서비스의 반환 실패를 주입했다. 성공 콜백은 발생하지 않고 그래프는 정리되며 모든 열차는 숨겨진다. View가 대여 이펙트를 직접 Destroy하지 않도록 실패 후 Clear/Release에서는 숨긴 설치 앵커를 씬 안에 보존하고 오류를 전달한다. 해당 Presenter의 새 Begin/Install은 거절한다. 정상 반환·취소·Clear/Release에서는 이 보존 경로를 사용하지 않는다.

이는 운영 중 반환 실패가 실제 발생했다는 보고가 아니다. 공개 계약으로 보장할 수 있는 실패 정리 범위를 검사한 결과다. Render 내부를 View 작업에서 수정하지 않았다.

## 요청

- 부분 반환 뒤 안전하게 정리를 완료할 수 있는 Scope 계약을 확인해 주세요. 성공한 노드를 다시 반환하지 않고 남은 노드 정리를 끝까지 시도할 수 있어야 합니다.
- 반환 실패 중 앵커 수명을 어떻게 유지·해제할지 확인해 주세요. View가 대여 노드에 직접 접근하거나 제거하는 방식은 사용하지 않습니다(FW-011).
- 현재 기본 RailwayVisual은 Empty이고 반환 실패 검사는 등록 Effect를 대신한 테스트 서비스에서 실행했습니다.

## RW4 선택 TrainEffect 등록과 의존성 인수

RW4에서 실제 기차·선로·바닥과 Clip·Controller는 복원·연결했습니다. 선택 TrainEffect는 아래 조건이 남아 있어 현재 Empty로 유지합니다. 옛 숫자 2를 새 enum에 임의로 대입하거나 누락 참조를 포함한 프리팹을 운영 Assets에 추가하지 않았습니다.

- 출처: develop-4week `ae07af37e9e897e4462c102b67d6b05f7a0bb8db`의 `Assets/Resources/Test/Effect/TrainEffect/TrainEffect.prefab`, `TrainSpark.mat`, `Assets/DataBase/SO/RenderGroup/EffectTable/Items/TrainEffect.asset`.
- 현 EffectType은 Empty·TestFire이며 옛 2가 등록되지 않았습니다. FW-012에 맞는 enum·DB 항목·ParticleSystem/ParticleStopNotifier·Pool 등록을 인수해야 합니다.
- 원본 프리팹의 Material GUID `fdd3f7739e6420144bffcb85ff863ff3`, Mesh GUID `097f9dec74e108a46be9e2fb54db7d3a`는 해당 과거 브랜치 Assets meta에서 찾지 못했습니다. 실제 의존성을 제공하거나 해당 자식의 필요 여부를 확정해 주세요.
- TrainSpark의 URP ParticlesUnlit와 ParticleStopNotifier GUID는 현재 프로젝트에 있습니다. 이것만으로 원본 프리팹 전체의 정상 등록을 보장하지 않습니다.

정상 등록이 제공되면 View는 기존 RailwayVisual Effect 설정과 사건 Scope에 연결할 수 있습니다. Render enum·테이블·Pool·Scope 내부는 View 작업에서 수정하지 않았습니다.

이 문서는 외부 발송하지 않았습니다. Core·Buffer·Render 내부·Framework 등록 변경과 커밋·push는 없습니다.
