# 맵 작업실 SO 저장과 DB 연결

그래픽 작업자를 위한 단계별 사용 설명서는 [FBX 맵 작업실 사용 설명서](fbx-map-workspace-user-guide.md)를 먼저 참고합니다. 이 문서는 저장 구조와 DB 연결을 다루는 기술 문서입니다.

`ProjectL > FBX 배치`의 가상 씬에서 맵을 구성한다.

1. `맵 SO 저장`으로 배치 프리팹과 `<이름>_Map.asset`을 저장한다. 베이크 전에도 저장할 수 있다.
2. 정적 오브젝트의 전역 조명 참여, UV2, Baked/Mixed 라이트를 설정하고 `베이크 + 맵 SO 저장`을 누른다.
3. 기존 `StageViewPropDataSO`의 `Stage View Prop Data > Map`에 생성된 `_Map.asset`을 지정한다. 해당 Item SO는 기존 StageViewProp DB 테이블 목록에 등록한다.
4. 수정할 때는 작업실의 `불러오기…`에서 `_Map.asset`을 선택하거나, `열 맵 SO`에 같은 SO를 넣고 `맵 불러오기 (베이크 포함)`를 누른다. 배치·라이트·재질을 바꾸면 다시 베이크한다.

베이크가 끝나거나 베이크된 맵을 불러오면 가상 씬의 카메라와 라이트맵 연결을 복원하고, 전체 맵을 화면에 맞춰 완성본으로 표시한다. 완성본에서는 편집 격자와 라이트 표시를 숨긴다. 상단 `배치 편집으로`로 돌아오며, 오브젝트를 선택하거나 편집해도 편집 표시로 돌아온다. 저장한 결과를 다시 보려면 상단 `베이크 완성본` 또는 베이크 패널의 `베이크 완성본 보기`를 누른다. 결과 표시와 불러오기는 재베이크하지 않는다.

좌측 `볼륨` 탭에서 `글로벌 볼륨 추가`를 누르면 현재 배치 레이어에 Unity Volume이 추가된다. 목록에서 볼륨을 고르고 영향도·우선순위·프로필의 Override를 편집한다. 기본 프로필에는 색 보정, 블룸, 톤매핑, 비네트, 화이트밸런스가 있으며 `Add Override`로 다른 URP 효과도 추가할 수 있다. `프로필 가져오기`는 기존 Volume Profile과 각 효과를 복사하므로 원본 프로필을 수정하지 않는다. `후처리 미리보기`를 끄면 적용 전후를 비교할 수 있다. 볼륨 편집은 베이크 완성본 표시를 유지하며 재베이크가 필요하지 않다.

`맵 SO 저장` 시 볼륨은 프리팹에 포함되고 작업용 프로필의 저장본은 `<이름>_Volumes/`에 생성된다. 맵을 불러오면 이 프로필을 다시 작업용으로 복사해서 편집한다. 기존 저장본과 작업 중인 복사본은 분리되며, DB에서 생성한 맵에도 저장된 볼륨이 포함된다. 게임 카메라는 URP 후처리가 켜져 있고 볼륨 오브젝트의 Unity 레이어를 Volume Layer Mask에 포함해야 한다.

저장 구조:

```text
MapDefinitionSO (_Map.asset)
├─ Prefab: 배치·메시·재질·Unity Light를 담은 맵 프리팹
└─ Baked Lighting: MapBakedLightingDataSO
   ├─ 라이트맵 색상·방향·Shadowmask 텍스처
   ├─ Renderer의 계층 경로·메시·라이트맵 인덱스·UV 배율/오프셋
   └─ Light의 Mixed 모드·Shadowmask 채널 등 베이크 출력
```

새 베이크 정보 SO는 `<이름>_Lighting/Bake_<시간>_<식별자>/BakedLighting.asset`에 저장된다. 같은 맵을 다시 저장하면 맵 SO의 GUID를 유지하므로 DB에서 다시 연결할 필요가 없다. 기존 인라인 베이크 프리팹을 작업실에서 저장하면 베이크 정보를 별도 `_Lighting.asset`으로 옮긴다.

DB 조회는 데이터만 반환한다. 실제 실행 경로는 `StageViewPropTable 조회 → StageViewPropPresenter.Show → MapAssembler.Instantiate → MapBakedLighting 적용`이다. 새 인스턴스의 각 Renderer에 해당 라이트맵 인덱스와 UV 영역을 연결한다. 같은 이름의 오브젝트는 자식 순번과 컴포넌트 순번으로 구분한다. 맵 해제 시 사용 중인 다른 맵의 라이트맵은 유지한다.

새 배치 프리팹은 베이크 연결 컴포넌트를 포함하지 않는다. 실행 시에는 맵 SO와 `MapAssembler`를 사용해야 한다. 기존 DB의 `Game Object` 필드는 이전 프리팹 경로로 계속 동작하며, `Map`이 지정되면 맵 SO를 우선한다.

현재 범위는 정적 Renderer의 라이트맵과 Light 베이크 출력이다. Light Probe/APV 데이터의 이동·조립은 포함하지 않는다. 함께 표시하는 씬과 맵은 같은 Directional Mode를 사용해야 한다. GI 대상의 정적 배칭 제외 정책은 기존과 같다.

베이크는 일반 씬을 저장한 뒤 전용 씬에서 수행한다. 취소·실패 시 기존 프리팹과 맵 SO를 미리 덮어쓰지 않는다.
