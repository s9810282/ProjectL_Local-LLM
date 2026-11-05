# 배치 미완료 전투 시작 확인 — 2026-10-19

## 동작 수정 — 사망 개체 제외

- 일반 편성 확인에서는 Character Buffer의 Health.IsDead가 true인 보유 개체를 필요한 배치 대상에서 제외한다. 즉 살아 있는 편성 개체가 모두 필드에 있으면 사망자 수와 관계없이 바로 시작한다.
- 런타임 Character가 아직 없는 보유 개체는 사망자로 추정하지 않고 미배치로 취급한다. 사망은 Excluded 영역과 HP 0의 불변 조회값으로 남는다.
- 앞선 메인 배치 우선 검사는 유지한다. 메인이 사망했거나 미배치라면 Y/N 없이 기존 안내를 사용하며, 최종 전투 진입 규칙은 Core가 계속 검증한다.
- 단일 Phase Task: 사망 사실 조회와 확인 대상 제외 → 사망 서브만 남은 경우/다른 미배치 생존자/사망 메인 회귀 검증. Core·UI 등록·서비스 조립은 변경하지 않는다.
- 검증: 코드·테스트 어셈블리 컴파일 오류 0건. 기존 6건과 사망 관련 3건을 PowerShell/.NET에서 직접 실행하여 총 9건 통과했다. Unity EditMode·화면 검증은 미실행이다.

## 동작 수정 — 메인 배치 우선

- 전투 진입 허용 상태를 검사한 다음, 편성된 Main 개체의 필드 배치·생존을 먼저 확인한다. 메인이 없으면 기존 CombatActionNotice의 메인 미배치 문구를 일반 안내로 표시하고 명령이나 Y/N 확인창을 만들지 않는다.
- 메인이 배치된 경우에만 나머지 편성 개체 확인을 진행한다. Y 확정 때도 메인 배치를 다시 검사한다. Core 최종 진입 검증은 그대로 유지한다.
- Y/N은 전투 시작 확인 모드에서만 활성·상호작용 가능하다. 일반 안내 지정, 취소, 숨김과 비활성화에서는 버튼을 숨기고 interactable도 해제한다.
- Task: 우선 검사와 기존 문구 재사용 → 일반 안내의 버튼 초기화 → 메인 누락·Y 확정 전 누락 회귀 및 일반 안내 재사용 검증. 단일 Phase 변경이다. 서비스 등록·조회 경로는 바꾸지 않는다.
- CombatDeploymentConfirmationTests는 기존 4건과 메인 우선/확정 재검사 2건, 총 6건으로 확장했고 PowerShell/.NET 직접 실행에서 전부 통과했다. 코드·테스트 어셈블리 컴파일 오류는 0건이다. 일반 안내 재표시 때 Y/N의 activeSelf와 interactable이 모두 false인지도 기존 EventSystem 테스트에 추가했다. Unity 화면·입력 검증은 별도다.

## 설계와 단일 구현 Phase

요청: 편성 개체가 모두 배치되지 않았다면 전투 시작 전에 사용자가 만든 NoticePannel의 Y/N을 거친다. Y는 전투 시작, N은 배치 계속이다. 모두 배치됐다면 기존처럼 바로 시작한다. 종류 수나 고정 숫자 4가 아니라 OwnedId별 배치 사실을 확인한다.

Task:

1. 기존 Notice의 등록과 새 Y/N 참조, 버튼·단축키의 공통 진입 경로 확인.
2. Notice에 확인 표시 모드와 명시적 응답 계약 추가. 일반 안내 및 이전 예약으로 확인창이 닫히지 않도록 보호.
3. 전투 진입 확인 협력 객체를 조립하고 두 입력을 같은 경로로 연결. 확인 중 배치·아이템 입력 차단, Y에서 현재 허용 상태 재검사.
4. 순수 분기 테스트, EventSystem 기반 버튼 테스트와 컴파일 검증. 이 Phase에서 멈춘다.

## UI 등록·연결·수명

- 등록 단위는 기존 GlobalUICanvas/NoticePannel의 TextNoticeUI 하나다. ID는 Notice(20), 부모는 None, GlobalUIBase의 요청형 루트이며 OpenWithGlobalUI=false다. 기존 기본 Policy=Exclusive와 Global 고정 열기 경로를 유지한다. Global UI이므로 뒤로가기 이력에는 들어가지 않는다.
- 최초 UISystem.BindScene에서 비활성 UI까지 수집하고 등록·바인딩한다. Y/N은 등록 화면 내부 위젯이며 각각 UIBase로 등록하지 않는다. 기존 옵션 Confirm UI와는 별개다.
- 입력은 UIInputBinding을 각 버튼에 한 번 연결한다. PointerClick/Submit이 명시적인 IUIInputReceiver로 전달되며 Inspector UnityEvent는 비워 둔다. 기본 선택은 N이다.
- 열기: CombatViewSystem 공통 EnterCombat 입력 → CombatDeploymentConfirmation → UITextPresentation의 IUIConfirmationPresentation 계약 → IUISystemService.OpenUI(Notice) → TextNoticeUI.Open.
- 닫기: Y/N 또는 소유자의 HideConfirmation → IUISystemService.CloseUI(Notice) → TextNoticeUI.Hide. 응답 대상과 선택을 정리한 뒤 응답을 한 번 전달한다. 실제 활성 변경과 내부 버튼 표시는 해당 등록 UI가 맡는다.
- N은 명령을 제출하지 않는다. Y는 현재 Policy/Gate 조건을 다시 검사하고 기존 Router에 EnterCombat을 넘긴다. 닫은 뒤 반복 입력은 무시한다.
- 씬 해제·오류 정지·제출 정지 때 소유자의 확인 요청을 닫는다. 현재 상태가 전투 시작을 허용하지 않게 바뀌어도 Sync에서 닫는다. 전역 Notice 객체는 재사용하며 Hide/OnDisable에서 응답 대상을 제거한다.
- 일반 Notice에는 Y/N을 표시하지 않는다. 확인 중 들어온 일반 안내는 덮어쓰지 않으며, 확인 진입은 이전 안내의 자동 닫힘 예약을 무효화한다.

## 서비스·경계·독립성 리뷰

- 생성: GlobalSystemFactory가 기존 UITextPresentation 하나를 만든다.
- 내부 등록: Global Builder에 IUITextPresentation을 한 번 등록하고 Shutdown 모듈로 보관한다. 새 확인 계약은 같은 객체의 좁은 기능 계약이며 별도 서비스 등록이 아니다.
- 상위 Export/소비: 기존 GlobalSystemGroup.UITextPresentation 경로로 CombatViewSystem이 받는다. 확인 기능은 전달받은 인스턴스의 IUIConfirmationPresentation 계약으로 사용한다. 루트 Builder 재노출이나 루트 resolver 직접 조회를 추가하지 않는다. FW-001/002 등록과 프로젝트 Global 계층을 유지한다.
- SSOT: 보유 개체와 필드 배치·생존 사실은 Character Buffer, 전투 시작 허용은 기존 CombatInputPolicy와 Gate다. View가 Core 규칙이나 고정 편성 수를 복제하지 않는다.
- SRP/OCP: 확인 필요 여부와 확정은 CombatDeploymentConfirmation, UI 표시·입력은 TextNoticeUI, 조립·공통 입력 경유는 CombatViewSystem이 맡는다. 일반 안내/호버 API는 유지한다.
- LSP/ISP/DIP: 기존 IUITextPresentation 구현체와 테스트 대역의 계약은 바꾸지 않는다. 확인 표시는 별도 좁은 계약으로 받으며, 미배치 확인이 필요한데 계약이 없으면 명시적 구성 예외를 던진다. 새 Framework 서비스나 이벤트 구독은 추가하지 않는다.
- 독립성: 배치 확인 정책 변경은 해당 협력 객체와 테스트에 한정된다. 공용 Notice 확인 동작 변경은 TextNoticeUI/UITextPresentation 및 해당 테스트에서 처리한다. Core·FieldCamera·Architecture와 기존 옵션 확인은 수정하지 않는다.

## 검증

- ProjectL.Systems 및 ProjectL.Tests.Framework MSBuild 컴파일 성공. 신규 소스는 임시 targets로 포함하고 산출물은 OS 임시 폴더에 생성했다.
- CombatDeploymentConfirmationTests 4건을 PowerShell/.NET에서 NUnit 검증문과 함께 직접 실행해 통과했다. 중복 종류의 미배치 개체, N 취소, Y 재검사, 전원 배치 직행, 상태 무효화·명시적 닫기를 확인했다. Unity EditMode 실행 증거와 구분한다.
- 기존 TextNoticeUITests에 PointerClick/Submit 응답 1회 및 일반 안내/타이머 간섭 방지 2개 케이스를 추가했다. 컴파일은 통과했으나 Unity 실행은 아직 하지 않았다.
- Prefab의 새 Y/N Button 참조와 빈 Inspector UnityEvent를 정적으로 확인했다. 사용자가 만든 버튼·레이아웃은 유지하고 참조 두 개만 추가했다.
- 남은 Unity 확인: 미배치 상태에서 시작 버튼/단축키 → 확인창, N → 배치 유지, Y → 시작, 전원 배치 → 즉시 시작. 확인 중 필드·덱·가방 입력 차단과 씬 전환 시 닫힘도 확인한다.
- 커밋·push하지 않는다.
