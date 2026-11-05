# 두루마리 자식 UI 프리팹 Task 계획 (2026-11-02)

설계 원본: [두루마리 연출 설계](../specs/2026-11-02-scroll-panel-presentation-design.md).

## Phase 1 — 독립 연출 위젯 프리팹

- [x] P1.1 `ViewSystem/Common/ScrollPanel`에 진행/레이아웃 컴포넌트와 종이 Graphic 작성.
- [x] P1.2 종이 색/요철/그림자 PNG와 uGUI 셰이더·Material 작성. 이전 미리보기의 질감 재사용.
- [x] P1.3 `Prefabs/UI/Common/ScrollPanelPresentation.prefab` 조립. 고정 ContentRoot와 RectMask2D 연결.
- [x] P1.4 사용 안내: 기존 UIBase 연결, 정상 닫기 완료 경로, 강제 Hide 정리, 입력 소유, Unity 수동 확인.
- [x] P1.5 새 `.meta`/GUID/직렬화 참조·금지 영역 미변경 정적 확인. 컴파일 미확인 보고 후 멈춤.

이 Phase는 기존 화면을 교체하지 않는다. 실제 화면의 UIId·Policy·부모·Factory·입력 세션은 변경하지 않는다. 공용 프리팹 내부의 게임 기능/자동 완료 판정도 넣지 않는다.

## 후속 Phase — 특정 화면 연결 (별도 리뷰 뒤)

대상 화면이 정해지면 기존 UIBase/Controller의 열기·사용 완료·강제 Hide에 연결한다. 현재 `PopupViewScope.Close()`보다 먼저 닫힘 연출이 끝나도록 해당 기능의 소유자 경계에서 조립한다. 공통 UISystem/PopupViewScope의 비동기 계약 변경으로 확대하지 않는다.

## Phase 2 — 덱 편집·이벤트 노드 화면 연결 (2026-11-02 사용자 요청)

대상은 `DeckEditUI.prefab`과 이벤트 노드(용병·훈련·상점·회복)가 사용하는 `StageOfferUI.prefab`이다. 목록 열람용 LobbyDeckUI는 덱 편집과 다른 화면이므로 이번 대상이 아니다.

- [x] P2.1 기존 UIId/ParentID/Policy를 유지하고, 두 프리팹 안에 공용 두루마리를 중첩한다. 기존 자식 참조와 덱 프리팹의 사용자 머티리얼 변경을 보존한다.
- [x] P2.2 기존 내용은 고정 기준 크기의 ContentFrame 안에 두고 안전 영역에 맞춰 전체 비율만 조절한다. 원래 레이아웃·카드 동작은 유지하고 기존 큰 배경은 투명화한다.
- [x] P2.3 표시 화면/어댑터의 선택적 두루마리 계약으로 열기·강제 Hide를 연결한다. 새 UIId·서비스·Canvas·카메라·레이어 추가 없음.
- [x] P2.4 StagePopupViewLifetime에 정상 닫기 연출 대기와 완료 후속 호출을 연결한다. 이 소유 부품 안에서만 지연하고 PopupViewScope/UISystem의 동기 계약은 유지한다. 두루마리가 없는 기존 화면/대체 구현은 기존 동기 경로를 유지한다.
- [x] P2.5 덱 출전 및 상점/회복의 닫기 후 동작을 완료 콜백으로 옮긴다. 처리 완료 사실/명령을 다시 제출하지 않으며 대기 중 새 조작을 차단한다. 강제 Hide·Dispose는 대기를 취소한다.
- [x] P2.6 프리팹 중첩·직렬화 참조·기존 사용자 변경 보존·코드 경계의 정적 확인, 문서 갱신 후 멈춘다. Test 코드·MSBuild·Unity 실행 없음. 컴파일 미확인.

## Phase 3 — 제공한 그림에 맞춘 종이 외형

- [x] P3.1 크림색 면/갈색 선/접힘/말림 끝을 벡터 원본과 투명 PNG 아틀라스로 작성.
- [x] P3.2 공통 Graphic과 UI 셰이더를 아틀라스 본체/양 끝 구성으로 변경. 공용 프리팹·Material 참조 연결.
- [x] P3.3 안쪽 x 여백을 보정해 기존 덱·이벤트 ContentRoot 최종 크기를 유지.
- [x] P3.4 펼침/닫힘 에셋 조합 이미지와 GUID/참조/여백 정적 확인. 문서 갱신 후 Phase 종료. 컴파일 미확인.

## Phase 4 — 증강 화면 두루마리 연결 (2026-11-02 사용자 요청)

- [x] P4.1 TraitUI에 공용 프리팹 중첩과 ContentFrame 연결. 기존 자식/카드/참조 보존, 큰 배경 투명화.
- [x] P4.2 TraitGrowthUI의 두루마리 필수 정책 설정. 기존 선택·연속 제안·최종 완료 닫기·강제 Hide 경로 재사용.
- [x] P4.3 원본/중첩/씬 참조와 마스크·내용 영역·입력/닫기 경로 정적 확인. 문서 갱신 후 멈춤. 컴파일 미확인.

## 실행 기록

### 후속 수정 — TraitUI 진입 예외

- [x] 공유 레이아웃의 두루마리를 선택 참조로 전환하고 필수 여부를 화면 소유자에서 검사한다. StageOfferUI는 필수, 증강은 선택이다.
- [x] 참조가 없는 증강의 표시/숨김과 동기 닫기 경로, 이벤트의 필수 검사 유지 여부를 정적으로 확인한다. Test 코드·MSBuild·Unity 실행 없음, 컴파일 미확인.

사용자 실행 오류에 대한 수정: 기존 참조는 선택적 계약인데 공통 레이아웃에서 필수로 검사한 범위 오류였다. StageOfferScreenUI의 필수 정책을 StageOfferUI에서만 켰다. Validate/Open/공개 참조 조회는 필수 정책을 검사하고, TraitGrowthUI는 null 연출을 허용한다. SetVisible은 참조가 있을 때만 PlayOpen/ResetHidden을 호출한다. 수명 부품의 기존 null→동기 닫기 분기를 확인했으며 프리팹/씬/증강의 기능 코드는 수정하지 않았다. diff 공백 검사 통과. **컴파일·Unity 재생 미확인**이므로 StageSelect 진입·증강 열기/선택·이벤트 두루마리 열기/닫기 확인 필요.

- 2026-11-02: 설계/계획 작성. 기존 UIBase/FW-008·009와 자식 위젯 예외를 확인. 작업 트리에 로비·덱·폰트 및 Render UI 파일 이동 등 사용자 작업이 있어 신규 경로만 사용한다. Test 작성·MSBuild·Unity 실행 없음.
- 2026-11-02 Phase 1 코드/에셋 작성 완료: `ScrollPanelPresentation` / `ScrollPaperGraphic` / 전용 셰이더 / Material / PNG 3개 / 프리팹. 기본 종이는 1440×900 기준으로 화면 안에 맞춰 축소된다. ContentRoot는 찢김·구멍 안쪽 여백을 둔다. 닫기 중 내용과 접지 그림자도 함께 줄어든다.
- 정적 확인: 신규 Assets 파일 20개(폴더 포함 meta 12개), 프리팹 직렬화 객체 25개. 자체 GUID 중복·미해결 로컬 fileID·미해결 외부 참조·직렬화 필드 누락 0. uGUI Image/RawImage/RectMask2D GUID와 RectMask2D API 및 UV0의 Vector4 지원을 설치 패키지 소스에서 확인했다. 신규 자산만 작성했으며 기존 Core/FieldCamera/Architecture/공용 계약/씬/사용자 변경 파일은 수정하지 않았다.
- **컴파일 미확인 / Unity 임포트·셰이더·프리팹 표시·재생 미확인.** 프리팹은 YAML로 조립했으며 Unity에서 저장/실행한 결과를 뜻하지 않는다. Test 코드는 추가하지 않았고 MSBuild·Unity도 실행하지 않았다. [사용 안내](../viewDocs/scroll-panel-presentation-guide.md)의 수동 항목 인계 후 Phase 1 리뷰 지점에서 멈춘다.
- 2026-11-02 Phase 2 코드/프리팹 연결 완료: 덱 편집과 StageOffer 공통 레이아웃에 공용 프리팹 중첩, 원본 자식들을 ScrollContentFrame으로 재배치, 큰 배경만 투명화. 덱 드래그 미리보기도 동일 프레임 좌표계로 이동했다. UIId·부모·정책·등록 및 서비스 조회 경로는 그대로이며 실제 씬은 기존 원본 프리팹 참조를 통해 연결된다.
- 정상 닫기는 선택적 IScrollPopupDisplay 참조가 있을 때만 대기한다. Close(bool)의 기존 서명을 보존하고 완료 후속은 CloseThen으로 추가했다. 덱 출전·상점/회복 맵 갱신을 완료 후 전달하며, 기존 Core 명령의 내용/제출 횟수/판정은 변경하지 않는다. 새 조작은 대기 중 차단하고 외부 Hide·부모 비활성화·Dispose에서는 완료 후속을 버린다.
- 정적 확인: 덱 원본 객체 389개·기존 자식 15개·기존 중첩 17개 보존, 이벤트 원본 객체 70개·기존 자식 3개·기존 중첩 3개 보존. 각 프리팹에 새 객체 7개씩 추가했고 새 미해결 로컬 참조 0. 덱 Rider 사용자 머티리얼 override 보존. 관련 참조 프리팹 7개의 마스크 우회 설정 없음 확인. StageSelect 씬의 원본 참조 확인 및 씬 미수정. diff 공백 검사 통과.
- **Phase 2 컴파일 미확인 / Unity 임포트·표시·열기·닫기·드래그·출전·이벤트 완료·씬 이탈 재생 미확인.** Test 코드는 작성·수정하지 않았으며 MSBuild·Unity를 실행하지 않았다. 증강의 기존 동기 닫기 경로를 포함해 사용 안내의 수동 확인을 인계하고 이 Phase에서 멈춘다.
- 2026-11-02 Phase 3 외형 교체: 제공한 두 그림을 기준으로 벡터 원본과 3072×2048 투명 PNG를 작성했다. 크림색 면·갈색 선·황갈색 접힘·말림 단면을 적용하고 이전 요철 조명을 제거했다. 기존 텍스처는 보존했다. 공용 프리팹·Material의 아틀라스 참조만 교체하고 기존 GUID는 유지했다.
- 기본 본체 폭 1440×0.84와 x 여백 73.76으로 ContentRoot 1062.08×560을 유지했다. 덱/이벤트는 1840×0.84와 x 여백 99.36으로 1346.88×670을 유지했다. 두 화면의 기존 자식/중첩 프리팹/사용자 Rider 머티리얼 및 StageSelect 원본 참조 보존 확인. 컴포넌트의 공개 API와 열기·닫기·입력/완료 경로는 변경하지 않았다.
- PNG의 투명 배경과 닫힘·중간·펼침 에셋 조합을 확인했다. 이 이미지는 Unity 렌더 결과가 아니다. 정적 참조/공백 확인 후 Phase 3을 마친다. **컴파일 미확인 / Unity 표시·재생 미확인.** Test 코드·MSBuild·Unity 실행 없음.
- 2026-11-02 Phase 4 증강 연결: 사용자 요청으로 기존 제외 결정을 대체했다. TraitUI 원본 객체 78개와 카드 프리팹 3개를 보존하고 공통 중첩/ContentFrame 객체 7개를 추가했다. Panel/Cards/ErrorPanel의 부모만 Frame으로 바꿨으며 글·카드·버튼·오류 참조와 카드 내부 이미지는 그대로다. 새 종이 참조를 연결하고 큰 Panel 배경만 투명화했다. TraitGrowthUI도 두루마리를 필수로 요구한다.
- 정적 확인: 총 85개 객체, 원본 변경은 루트/부모 3개/레이아웃 참조/배경 색의 6개뿐이다. 중복 ID·미해결 로컬 참조·계층 불일치 없음. 직접 자식의 마스크 우회 없음. StageSelect→TraitGrowthUI→TraitUI 원본 연결과 기존 공용 source ID 확인. 외부 프리팹/씬/기능 Controller/Core 변경 없음. 연속 제안은 기존 Render, 최종 완료는 기존 StagePopupViewLifetime의 말림 대기, 강제 Hide/Dispose는 기존 정리를 사용한다. diff 공백 검사 통과.
- **Phase 4 컴파일 미확인 / Unity 표시·선택·연속 제안·최종 닫기·씬 이탈 재생 미확인.** Test 코드·MSBuild·Unity 실행 없이 수동 확인 항목을 인계하고 이 Phase에서 멈춘다. 커밋·push 없음.
