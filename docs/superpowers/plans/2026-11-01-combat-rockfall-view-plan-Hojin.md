# 낙석 전투 View 계획

- 작성자: 호진(이호진)
- 상태: RF-a~RF-c 커밋됨, 돌 낙하 재생 확인(§6.6). RF-d 안내·문서 완료(컴파일 미확인, Ud 대기, §6.8). RP는 사용자 지시로 그대로. 돌 출발 능선 RG-a~RG-c 완료(RG-a·RG-b Unity 문제 없음 — 사용자, RG-c 컴파일 미확인, 공유 미전달, §7.5).
- 설계: [낙석 View 설계](../specs/2026-11-01-combat-rockfall-view-design-Hojin.md)
- 기준: [Core → View 인계](../handoffs/2026-11-01-tornado-rockfall-haboob-core-view-handoff.md) §2·§5·§8·§9.4
- 앞뒤 순서: [토네이도](2026-11-01-combat-tornado-view-plan-Hojin.md) → 낙석 → [하붑](../specs/2026-11-01-combat-haboob-view-design-Hojin.md)

## 0 진행 규칙

- Phase가 끝나면 멈추고 보고한다. 커밋·push는 사용자가 한다.
- 새 시험을 만들지 않고 MSBuild를 돌리지 않는다. 보고에 "컴파일 미확인"을 적는다. 컴파일·재생 확인은 사용자가 Unity에서 한다.
- Core는 고치지 않는다. 공유 파일을 고치면 Phase RF-d에서 담당자 공유 문서에 적는다.
- 작업 트리의 다른 변경은 건드리지 않는다.

착수 조건: 토네이도 커밋 뒤에 시작한다(사건 형식 표·처리기 표·필드 창구·표시 Factory가 같은 파일). 토네이도 추가 수정은 2026-11-01 사용자 지시로 낙석 뒤로 미뤘다.

코드로 확인한 기존 시험 영향은 없다. `IViewQuery`, `CombatDisplayTables`, `CombatFieldEventPorts`, `CombatEnvironmentText`, `CombatSceneDisplay`를 만들거나 구현하는 시험이 지금 없다. 강조 층을 쓰는 시험(`Assets/Tests/Editor/CombatField/`)도 팔레트 생성·`ushort` 형변환·채우기 `Apply` 호출이 없다.

## 1 계획에서 정한 것(확인 대상)

| ID | 정한 것 | 이유 | 설계와 |
|---|---|---|---|
| RP-1 | **2026-11-02 바뀜(사용자 지시, §6.3):** 터널 자리 = 맵 프리팹 안의 빈 오브젝트 `RockfallTunnelBoom`의 위치·회전. 없으면 터널 없음. 옛 안(`StageViewPropData`의 칸 셋 `hasRockfallTunnel`·`rockfallTunnelPosition`·`rockfallTunnelYaw`)은 걷음 | 터널 모델과 같은 프리팹에서 보며 맞출 수 있고, 배경을 옮기면 함께 움직인다. 값이 한 곳에만 있다 | 칸 하나 → 오브젝트 |
| RP-2 | 경고 칸마다 테두리·채우기(강조 층, RP-16·RP-17)와 바닥에 눕힌 느낌표(RP-18)를 함께 보인다(2026-11-01 사용자 A안). 호버 설명은 그대로 둔다 | 기획서의 "표시가 생긴다"를 지키고, 있는 칸 강조와 한 몸으로 보이게 한다 | **다름**(Q-RF1은 표지 없음, §3.5는 선택 칸) |
| RP-3 | 이펙트·흔들림은 있는 큐 입력 둘로 받는다: `warningCues`, `impactCues`. 새 큐 칸 `CombatCueEvent.RockfallWarning = 13`(Start = 경고 시작)·`RockfallImpact = 14`(End = 착지), 자리는 둘 다 EventCell | 자리 보정·지연·쥠 끝을 큐가 이미 가진다. `EffectType` 칸을 따로 두면 규칙이 두 벌이 된다 | **다름**(§3.5는 터널·착지 `EffectType` 칸) |
| RP-4 | 필드 창구에 좁은 계약 `ICombatRockfallField` 필수 인자 하나를 더한다 | 토네이도 TP-3과 같다 | 같음 |
| RP-5 | 돌은 표시가 풀로 가진다. 조각이 빌려 위에서 떨어뜨리고, 칸에 닿으면 돌려줘 사라지게 한다. 착지 순간 그 칸에 민형님 이펙트 `FallingRocks`를 재생한다(2026-11-01 사용자). 새 `EffectType.FallingRocks`(열거 끝)와 Render 이펙트 표 항목으로 등록한다. 풀 최대 16(`Boom`과 같음) | 칸 수만큼 매번 만들지 않는다. 착지 순간이 조각 끝이라 값으로 맞출 필요가 없다 | 같음(착지 이펙트만 지정) |
| RP-6 | **2026-11-02 궤적 바뀜(§6.6, 출발 자리는 §7 능선이 대신함):** 돌은 위에서 떨어지되 터널 쪽으로 `dropSideDistance`만큼 치우친 자리에서 시작해 곡선을 그린다(아래로 제곱·옆으로 일정, 코드 고정). 터널이 없으면 칸 위에서 곧게. 낙하 기본값: 높이 8, 옆 거리 5, 시간 0.5초. 돌은 순서를 섞어 `dropIntervalSeconds`(기본 0.1초) 간격으로 차례로 떨어진다(2026-11-01 사용자). 맞은 유닛이 있는 칸의 돌은 빈 칸의 돌 다음에 함께 떨어진다(사용자 확인, 재생 뒤 다시 판단). 시작 지연은 `CombatPieceStart.AtStart(delay)`, 섞기는 View 안 난수이고 규칙에 닿지 않는다. 맞은 유닛의 칸은 `RecipientIds`의 보인 사실에서 읽는다 | 임시 값, SO에서 조절. `WarnedCells`는 줄·칸 순서로 정렬돼 그대로 쓰면 줄을 따라 쓸려 보인다. 유닛 반응은 돌이 모두 착지한 뒤 함께 시작하므로(RP-8·Q-RF2), 맞은 칸의 돌을 마지막에 두면 맞는 순간과 반응이 맞는다 | 값 추가 |
| RP-7 | 터널 큐 기본값: `Boom` 하나, Start, EventCell, 회전 보정 0(터널이 보는 방향으로 터짐, 화약통·지뢰의 위로 터지는 (−90,0,0)을 뺀 값). **경고 시작에 터진다**(기획서 9쪽 "폭발까지 2턴, 낙석 떨어지기까지 2턴", Core 설계와 같음, 2026-11-01 사용자 확인). 그 뒤 2턴 경고가 있고 돌이 떨어진다. 충돌 큐 기본값: 칸마다 착지(End)에 `FallingRocks` 하나, 흔들림 하나(피격 흔들림 값과 같게) | 있는 에셋만 쓴다(Q-RF1). 방향은 재생에서 맞춘다. 흔들림 겹침은 창구가 "더 센 것만"으로 처리한다 | 값 추가 |
| RP-8 | 동시 묶음 범위: Impact 바로 뒤에 **이어지는** 근원 없는 `CombatCharacterEvent`·`CombatTraitStateEvent` 중 주체가 `RecipientIds`에 있는 것. 다른 사건을 만나면 멈춘다. 묶음 번호는 `_keys.Count` 위에서 새로 센다 | 같은 유닛의 체력 → 속박 순서는 `CanOverlap`이 지킨다 | 구체화 |
| RP-9 | 소리는 처리기가 낸다. 경고 소리는 Warning 순간 한 번, 착지 소리는 모든 돌이 착지한 때 한 번. 슬롯은 연결 대기(null) | 설정 값을 읽는 길이 토네이도와 같다. 칸마다 내면 겹쳐 커진다 | 구체화 |
| RP-10 | 호버: `CombatEnvironmentText.Build(CombatStateData state, CellCoord cell)`로 인자를 줄인다. 설명의 `{n}`은 `RockfallGimmickSpec.MaximumHealthLossPercent` | 인자가 일곱에서 또 는다. 호출은 호버 하나다 | 구체화 |
| RP-11 | 이동 불가 문구는 안내(`CombatActionNotice`)와 함께 짧은 글자(`CombatActionStateText`의 "발목꺾임" → "이동불가")도 바꾼다 | 같은 거절의 두 표시다. 낙석 속박에 "발목꺾임"이 나오면 틀리다 | 추가 |
| RP-12 | **2026-11-02 바뀜(§6.3):** 터널 자리의 회전 = 그 오브젝트의 월드 회전. 터널 큐가 이 자리를 쓴다. 오브젝트가 없으면 폭발 없이 진행하고, 둘이면 예외다 | 폭발이 터널 방향으로 나가야 한다(RP-1) | 추가 |
| RP-13 | 임시 에셋: `Prefabs/Combat/Rockfall/`에 돌(Sphere 0.6, URP Lit 회색 재질)과 느낌표(RP-18). 레이어 20(`ThornCactus`와 같음), 충돌체 없음. **2026-11-02 돌은 사용자 지정 `Prefabs/Stage/MapProp/smallrock (3).prefab`으로 바뀜(§6.4).** 프리팹 레이어가 21이라 돌을 만들 때 코드가 20으로 맞춘다 | Q-RF1. 낮·밤 카메라에 모두 보이는 값 | 구체화 |
| RP-14 | 켜기와 터널 자리는 사용자가 `Test`에서 한다(`rockfall.enabled` 지금 0) | 토네이도 TP-11과 같다 | 같음 |
| RP-15 | 터널 막대를 만들지 않는다(2026-11-01 사용자: 막대는 터널 폭발이 없을 때의 대안이었다). 지난 폭발은 다시 내지 않는다. 진입·재연결·결과 끝에는 `WarnedCells`로 경고 칸 표시(RP-2)만 맞춘다 | 폭발은 사건 순간의 연출이고, 경고 칸은 Buffer 사실이다 | 같음 |
| RP-16 | 강조 층: `FieldHighlightLayer`를 `uint`로 넓히고 `RockfallWarning = 1 << 16`을 더한다. 층 표 자리는 `HazardRange` 바로 뒤, 채움·평소에도 그림 모두 참. 색은 `FieldHighlightColor.RockfallWarning = 19`, 팔레트 칸(`fillAlpha` 앞), 씬 값 `FieldView.rockfallWarningColor`(기본은 느낌표 이미지 색 (0.918, 0.278, 0.271)). `FieldView`에 켜기·끄기 입구 둘(넉백 입구 `:400`·`:411` 꼴) | 16비트를 다 썼고 층 표가 층의 속성을 한 줄에 둔다. 평소 층이라 `IsActing`을 켜지 않는다 | 구체화 |
| RP-17 | 평소 채우기: `FieldCellFillStack.Apply`가 행동 중이 아니면 평소에도 그리는 채움 층만 쌓는다. 호버 칸·배치 구역은 쌓지 않는다. 조건은 층 표에서 읽고, 지금 그런 층은 `RockfallWarning` 하나다 | 지금 채우기는 행동 중에만 그려진다. 다른 층의 화면은 바뀌지 않는다 | 추가 |
| RP-18 | 느낌표: 표 칸 `warningMarkPrefab`(필수)·`warningMarkSize`(칸 대비 판 크기, 기본 0.9)·`warningMarkLift`(기본 0.01). 칸 표시 높이(`CellOverlayToWorld`, 채우기·테두리와 같음)에 눕히고 바라보는 방향은 프리팹 그대로. 색은 이미지 그대로이고 코드가 입히지 않는다. 정렬 순서 `FieldSortingOrders.RockfallWarningMark = 90`(테두리 위·궤적 아래)을 코드가 넣고 그림자를 끈다. 유닛이 일부를 가리는 것은 그대로 둔다 | 빨간 이미지에 색을 곱하면 테두리보다 어두워진다. 이미지 안의 느낌표가 판 높이의 약 66%라 0.9면 칸의 약 60%다. 투명 표시 순서의 원본은 정렬 순서 표다 | 추가 |
| RP-19 | 느낌표 에셋: 사용자 이미지 `Assets/ProjectL/Sprites/rockfall_warning.png`(1254×1254, 배경 투명, 느낌표 색 (234, 71, 69), 2026-11-01 사용자). 새로 그리지 않는다. URP Unlit 투명 재질(기본 색 흰색)과 프리팹(루트 + X 90° Quad)을 `Prefabs/Combat/Rockfall/`에 만든다. 이미지 파일 설정은 고치지 않는다. 기운 그림 `rockfall_warning_Side.png`는 쓰지 않는다 | 이미지가 이미 있다. 나중에 이 파일만 바꾸면 재질·프리팹은 그대로다 | **다름**(임시 흰색 이미지 대신) |
| RP-20 | 경고 색은 이미지 색에 맞춰 시작한다(RP-16). 채우기는 `fillAlpha`로 옅다. 씬 색을 바꾸면 느낌표 이미지 색과 어긋나므로 둘을 함께 바꾼다. 재생에서 어울리지 않으면 그때 정한다 | 값을 더 두지 않는다. 색의 원본이 씬 값·이미지 둘인 것은 아트 색이라 받아들인다 | 추가 |

## 2 Phase

### RF-a 값(화면 변화 없음)

| Task | 내용 | 파일 |
|---|---|---|
| Ra.1 | 새 표 `RockfallVisual`: 돌·느낌표 프리팹(필수), 느낌표 크기·띄움(RP-18), 낙하 높이·시간·간격(RP-6), 큐 둘, 소리 프리셋 둘(기본 -1), 이름·설명. 값 검사 | `DataBaseSystem/Data/View/Table/Rockfall/Item/RockfallVisualDataSO.cs`, `Table/Rockfall/RockfallVisualDataTableSO.cs` |
| Ra.2 | 조회 | `Queries/ViewDataBase/ViewTable/RockfallVisualTable.cs` |
| Ra.3 | 큐 칸 둘과 규칙 줄 | `DV/CombatPresentationCueRules.cs` |
| Ra.4 | 연결 줄 | `ServiceKeys/DBFactory/Group/ViewTableKey.cs`, `ViewDataBase.cs`, `Factory/DBFactory/Group/ViewDataBaseFactory.cs`, `CombatDisplayTables.cs` |
| Ra.5 | 스테이지 터널 칸(RP-1) | `Table/StageViewProp/Item/StageViewPropDataSO.cs` |
| Ra.6 | 임시 에셋(RP-13·RP-19), 설정 에셋·표 에셋·`ViewGroup.asset` 한 줄 | `Prefabs/Combat/Rockfall/`, `DataBaseSO/ViewGroup/RockfallVisual/` |
| Ra.7 | 이펙트 등록(RP-5): 열거 값, 표 항목 에셋, 표 한 줄 | `Data/Render/Table/Effect/Enum/EffectType.cs`, `DataBaseSO/RenderGroup/EffectTable/Items/FallingRocks.asset`, `EffectDataTable.asset` |

확인: 컴파일, Combat 진입에 변화 없음, 인스펙터에 칸이 보임.

### RF-b 경고(터널 폭발·경고 칸이 보임)

| Task | 내용 | 파일 |
|---|---|---|
| Rb.1 | 좁은 계약: 설정, 경고 칸 켜기·끄기, 터널 자리 읽기, 돌 빌리기·돌려주기 | 새 `Display/Map/ICombatRockfallField.cs` |
| Rb.2 | 표시: 돌 풀, 터널 자리 읽기(RP-12), 경고 칸 표시. `Render(gimmicks, mapper)`는 경고 단계면 `WarnedCells`로 켜고 아니면 끈다(RP-15). `Clear`·`Release` | 새 `Display/Map/FieldRockfallPresenter.cs` |
| Rb.3 | 표시 연결: 결과 끝 `Render`, 정리 목록 | `Display/CombatSceneDisplay.cs` |
| Rb.4 | 창구·조립: 필수 인자, 표시 생성 | `Playback/CombatFieldEventPorts.cs`, `CombatSceneDisplayFactory.cs`, `CombatTraitPreviewFactory.cs`(빈 표시, 토네이도와 같음) |
| Rb.5 | 사건 줄·처리기 칸 | `Playback/Ordering/CombatEventTypeTable.cs`, `Playback/Handlers/CombatEventHandlerTable.cs` |
| Rb.6 | 처리기 Warning: 터널 큐(`Boom`)를 곁들임 실행으로(비었거나 터널 자리가 없으면 시작 안 함), `Final.WarnedCells`로 경고 칸 켜기, 경고 소리. Impact는 Rc 전까지 경고 칸을 끄고 바로 끝냄 | 새 `Playback/Handlers/CombatRockfallEventHandler.cs`, `Playback/CombatSceneEventPresenter.cs` |
| Rb.7 | 소리 표 슬롯 둘(연결 대기) | `Playback/Support/CombatEventSounds.cs` |
| Rb.8 | 강조 층(RP-16): 열거 형식·값, 층 표 한 줄, 색 종류, 팔레트 칸, 씬 색, 켜기·끄기 입구 | `Display/Field/FieldHighlightLayers.cs`, `FieldHighlightPalette.cs`, `FieldView.cs` |
| Rb.9 | 평소 채우기(RP-17) | `Display/Field/FieldCellFillStack.cs` |
| Rb.10 | 느낌표(RP-18): 칸마다 인스턴스 재사용, 크기, 정렬 순서, `Show(cells)`·`Clear`·`Release`. 표에 없는 정렬 값 한 줄 | 새 `Display/Field/FieldCellMarkers.cs`, `Display/Field/FieldSortingOrders.cs` |

확인(`Test`에서 켜고 터널 자리 입력): 경고 턴에 터널 방향으로 `Boom`이 터지고 떨어질 칸에 테두리·채우기·느낌표가 나옴, 재시작·재연결 때 폭발은 다시 나오지 않고 경고 칸은 다시 보임, 대기 단계면 없음, 터널 자리가 비면 폭발 없이 경고 칸만 나옴. 이때 Impact에 돌은 아직 없고 체력·속박은 차례로 나온다.

### RF-c 충돌(돌이 떨어짐)

| Task | 내용 | 파일 |
|---|---|---|
| Rc.1 | 돌 낙하 조각: 시작에 빌려 칸 위에 두고, ease-in으로 칸 표면까지, 착지·취소 때 돌려줌 | 새 `Presentation/Pieces/CombatRockfallDropPiece.cs` |
| Rc.2 | 처리기 Impact: 경고 칸을 끄고, `Expected.WarnedCells`로 칸마다 조각 하나를 RP-6의 순서·지연으로 `ForAction`에 넣고, 칸마다 충돌 큐를 그 조각 End에 붙임(`FallingRocks`·흔들림). 모두 착지하면 사건 완료, 착지 소리. 칸이 없으면 바로 끝 | `CombatRockfallEventHandler.cs` |
| Rc.3 | 동시 묶음 `AttachRockfallRecipients`(RP-8) | `Playback/Ordering/CombatConcurrentGroups.cs` |

확인: §3 전체.

### RF-d 안내·문서·검토

| Task | 내용 | 파일 |
|---|---|---|
| Rd.1 | 이동 불가 문구(RP-11) | `Input/Assist/CombatActionNotice.cs`, `Display/Hud/CombatActionStateText.cs` |
| Rd.2 | 호버 설명(RP-10) | `Input/Assist/CombatEnvironmentText.cs`, `CombatEnvironmentHover.cs`, `CombatSceneInputFactory.cs` |
| Rd.3 | 문서: 폴더 배치 안내, 설계 구현 메모·§3.5 고침, 사운드 작업자 안내 | `docs/superpowers/` |
| Rd.4 | 공유(작성만): 민형님 Factory 공유(표 연결·표시 줄, `FallingRocks` 등록·프리팹의 `ParticleStopNotifier`), 호진 설정 표 메모(스테이지 칸), 현강님(문구 둘), 용근님 메모(미리보기 줄), 천명님 Core 전달 문서(대응 완료) | `docs/superpowers/handoffs/` |
| Rd.5 | 묶음 끝 검토(새 에이전트 하나, 코드만 주고 평가), 반영은 사용자 결정 | |

확인: 호버 설명·문구, 다른 전투 연출에 변화 없음.

## 3 Unity 확인 목록(인계 §9.4)

1. 경고 칸 표시와 호버 설명이 재연결·재시작 뒤에도 나오고, 대기 단계에는 없다.
2. 돌은 `Final`이 아니라 경고했던 칸에 떨어진다(대기 단계로 넘어간 뒤 빈 칸에 떨어지지 않음).
3. 경고 칸이 비었거나 맞은 유닛이 없어도 정상으로 끝난다.
4. 체력 감소·피해 0·속박·사망이 한 번씩만 나온다. 여러 명이 함께 맞으면 동시에 반응한다.
5. 속박된 유닛은 이동이 막히고 공격은 된다. 풀리는 시점은 상태 표시와 같다.
6. 낙석으로 전투가 끝나면(`Final` null) 연출을 마친 뒤 안전하게 지워진다.
7. 값을 바꾼 스테이지(시간·칸 모양)에서 View가 그대로 따른다.
8. 같은 턴에 토네이도 유닛이 공중에 있으면 낙석은 착지 뒤에 시작한다.
9. 돌이 칸에 닿는 순간 돌이 사라지고 `FallingRocks`가 그 칸에 나온다. 맞은 유닛의 피격 반응도 그때 시작한다. 이펙트가 스스로 끝난다.
10. 경고 시작에 터널 `Boom`이 터널 방향으로 터진다. 2턴 경고 뒤 돌이 떨어진다(RP-1·RP-7).
11. 떨어질 칸에 테두리·채우기·느낌표가 어울리는 색으로 보이고, 입력이 없는 평소 화면에서도 남는다. 돌이 떨어지기 시작하면 사라진다(RP-2).
12. 이동·공격 범위를 보는 동안 경고 칸의 채우기는 안쪽으로 작아지고 느낌표는 가려지지 않는다. 호버 색과 다른 층의 평소 화면은 그대로다(RP-16~RP-18).
13. 낮·밤 화면, 타일·지형 높이가 다른 칸에서 느낌표가 채우기와 어긋나지 않는다.
14. 돌이 줄 순서가 아니라 섞인 순서로 조금씩 떨어지고, 유닛이 맞은 칸의 돌은 마지막에 떨어져 착지와 피격 반응이 같은 순간이다(RP-6).

인계 §9.2의 시험은 새 시험을 만들지 않는 규칙으로 재생 확인이 대신한다.

## 4 커밋 묶음

사용자가 Phase마다 또는 묶어서 커밋한다. 한 커밋이어야 하는 것:

- RF-a: 새 표 코드와 `DataBaseSO/ViewGroup/RockfallVisual/`·`ViewGroup.asset`, 임시 프리팹·재질(`.meta`와 폴더 `.meta` 포함), 아직 커밋 전인 `Sprites/rockfall_warning.png`(+ `.meta`). 없으면 DB 생성이 실패한다. `EffectType` 값과 이펙트 표 항목·표 한 줄, `FallingRocks.prefab`의 `ParticleStopNotifier`도 같은 커밋이다.
- RF-b: 새 `.cs`와 `.meta`. 창구 인자와 Factory 둘은 함께.

## 5 원칙 점검(코드 전 예상)

| 기준 | 판단 |
|---|---|
| SSOT | 칸·단계·맞은 유닛·피해는 Core 값. 묶음 규칙은 `CombatConcurrentGroups` 한 곳, 큐 규칙은 큐 규칙 표 한 곳 |
| SOLID | 표시·처리기·조각을 나눈다. 약점은 처리기 표와 필드 창구 인자가 는다는 것이다 |
| FW | 새 서비스 없음. 표는 ThornField와 같은 길, 이펙트·카메라는 있는 창구 |
| 독립성 | 토네이도·하붑 코드를 고치지 않는다. 공유 파일에는 줄만 더한다. 묶음 함수는 낙석 사건이 없으면 아무 일도 하지 않는다 |

## 6 실행 기록

### 6.1 RF-a 코드·에셋 완료(2026-11-01 사용자 "토네이도의 추가 수정은 나중에 하고, RF-a 시작해줘" = RP 그대로, 컴파일 미확인, 새 시험 없음, 시험 변경 없음)

착수 조건 중 토네이도 수정은 사용자 지시로 미뤘다. 토네이도 코드는 사용자 커밋 `4a38c7c9a`로 들어가 작업 트리가 비어 있었다.

- 새 표: `Table/Rockfall/Item/RockfallVisualDataSO.cs`(값 `RockfallVisualData`), `Table/Rockfall/RockfallVisualDataTableSO.cs`, 조회 `Queries/ViewDataBase/ViewTable/RockfallVisualTable.cs`(+ `.meta`, 폴더 `.meta`).
- 큐 칸: `CombatCueEvent.RockfallWarning = 13`(Start)·`RockfallImpact = 14`(End), 자리는 둘 다 EventCell.
- 연결 줄: `ViewTableKey`, `ViewDataBase`, `ViewDataBaseFactory`, `CombatDisplayTables`(생성자 인자 하나, 부르는 곳은 `From` 하나).
- 스테이지 칸: `StageViewPropData`의 `hasRockfallTunnel`·`rockfallTunnelPosition`·`rockfallTunnelYaw`.
- 이펙트 등록: `EffectType.FallingRocks`(15), `EffectTable/Items/FallingRocks.asset`, 표 한 줄.
- 에셋: `Prefabs/Combat/Rockfall/`의 돌·느낌표 프리팹과 재질(느낌표 재질이 `Sprites/rockfall_warning.png`를 씀), `DataBaseSO/ViewGroup/RockfallVisual/`(표·설정, 문구 초안), `ViewGroup.asset` 한 줄.

계획과 다른 점: 없음. 표를 읽는 곳이 아직 없어 화면 변화는 없다.

찾은 것: 민형님 `FallingRocks.prefab` 루트에 `ParticleStopNotifier`가 없었다. 이펙트 풀은 첫 재생 때 이 컴포넌트가 없으면 예외를 던진다(`EffectPool.cs:31-33`). 사용자가 루트에 더했고(2026-11-01, 파일로 확인, 커밋 전) 파티클 값은 그대로다. 프리팹 변경은 RF-d의 민형님 공유에 적는다.

Unity 확인(Ua):

1. 컴파일이 되고 Combat 진입에 변화가 없다.
2. `RockfallVisual_Default` 인스펙터에 돌·느낌표·큐·소리·문구 칸이 보이고 연결이 깨지지 않았다.
3. 스테이지 View SO에 "낙석 터널" 칸 셋이 보인다.
4. `RockfallWarningMark.prefab`을 열면 빨간 느낌표가 위에서 보이도록 눕혀 있다. `RockfallStone.prefab`은 회색 공이다.
5. `EffectDataTable`에 `FallingRocks`가 있고 Type이 FallingRocks다.

Ua 문제 없음(2026-11-01 사용자 "확인됨").

### 6.2 RF-b 코드 완료(2026-11-01 사용자 "확인됨, RF-b 진행", 컴파일 미확인, 새 시험 없음, 시험 변경 없음, 에셋 변경 없음)

- 강조 층: `FieldHighlightLayer` 바탕 형식 `uint`, 값 `RockfallWarning = 1 << 16`, 층 표 한 줄(`HazardRange` 뒤, 채움·평소 모두 참), `FieldHighlightColor.RockfallWarning = 19`, 팔레트 칸, 씬 값 `FieldView.rockfallWarningColor`.
- 평소 채우기: 층 표에서 만드는 `FieldHighlightLayers.IdleFillLayers`. `FieldCellFillStack.Apply`는 행동 중이 아니면 이 층이 칠해졌을 때만 쌓고, `Stack`은 이 층만 쌓으며 호버와 호버 테두리 계산은 건너뛴다. 이 층이 없으면 옛 동작과 같다.
- 느낌표: 새 `Display/Field/FieldCellMarkers.cs`(+ `.meta`), 정렬 값 `FieldSortingOrders.RockfallWarningMark = 90`. 칸 표시 높이 + `warningMarkLift`, 필드 회전 × 프리팹 회전, 칸 크기 × `warningMarkSize`, 그림자 끔, 색은 프리팹 그대로.
- 표시: 새 `Display/Map/ICombatRockfallField.cs`·`FieldRockfallPresenter.cs`(+ `.meta`). `Render(gimmicks, stageKey, mapper)`는 경고 단계면 `WarnedCells`로 켜고 아니면 끈다. 터널 자세 = 스테이지 값 위치, 필드 회전 × yaw. `CombatSceneDisplay` 결과 끝 `Render`·정리 목록 셋.
- 사건: 처리기 칸 `Rockfall`, 줄 `Row<CombatRockfallEvent>`(기본 = 묶음 밖 장벽이라 공중의 유닛이 착지한 뒤 시작), 새 `Playback/Handlers/CombatRockfallEventHandler.cs`(+ `.meta`). Warning은 경고 칸 켜기 → 터널 큐 곁들임 → 경고 소리, Impact는 경고 칸 끄기만. 소리 슬롯 둘은 연결 대기.
- 연결 줄: `CombatFieldEventPorts` 끝 인자 `rockfall`, `CombatSceneEventPresenter` 처리기 표 한 줄, `CombatSceneDisplayFactory`(생성·창구 인자·표시 인자), `CombatTraitPreviewFactory`(빈 표시). 민형님 공유는 RF-d.

계획과 다른 점 셋:

1. 돌 풀(Rb.1의 돌 빌리기·돌려주기, Rb.2의 돌 풀)은 RF-c로 옮겼다. 쓰는 곳이 RF-c 낙하 조각뿐이라 지금 만들면 아무도 부르지 않는다.
2. `FieldView`에 켜기·끄기 입구를 더하지 않았다(RP-16). 재생 강조·밀려날 칸과 같은 기존 `SetLayerCells`·`ClearLayer`로 충분하다.
3. `Render`가 `stageKey` 인자를 더 받는다. 터널 자리를 그 스테이지의 View 값에서 읽으려고 결과 끝마다 맞춘다(RP-12).

Unity 확인(Ub, `Test` 스테이지의 전투 데이터에서 `rockfall.enabled`를 켜고 스테이지 View SO에 낙석 터널 자리를 넣는다. 턴은 진영 턴 기준이라 기본값은 대기 2 → 경고 2):

1. 컴파일이 되고 낙석을 끈 스테이지의 전투 진입·범위 표시에 변화가 없다.
2. 경고가 시작되는 턴에 터널 자리에서 터널 방향으로 `Boom`이 터지고, 떨어질 칸에 테두리·채우기·느낌표가 나온다.
3. 입력이 없는 평소 화면에서도 남는다. 다른 평소 화면(호버, 배치 구역)은 그대로다.
4. 유닛을 고르거나 호버해 범위를 보면 경고 칸 채우기가 범위 위 안쪽으로 작아지고 느낌표는 가려지지 않는다.
5. 느낌표 위쪽이 필드 앞쪽(+Z)을 향한다. 다르면 프리팹 회전으로 고칠 수 있다.
6. 전투를 다시 시작하거나 재연결하면 경고 칸은 다시 보이고 폭발은 다시 나오지 않는다. 대기 단계에는 없다.
7. 터널 자리를 끄면 폭발 없이 경고 칸만 나온다.
8. 충돌 턴에 경고 칸이 꺼지고, 돌 없이 체력·속박이 차례로 나온다(돌과 동시 반응은 RF-c).

Ub 중 보고 처리(2026-11-02, 컴파일 미확인, 새 시험 없음, 시험 변경 없음):

- **HUD 멈춤 수정:** 낙석 속박의 출처 종류 `StageGimmick`을 상태 설명이 몰라 충돌 턴에 `InvalidOperationException`(F1)이 났다. `CombatStatusDescriptionFormatter.Origin`에 `기믹 {키}` 갈래를 더했다. 문구는 옆 줄(`유물 {키}`)과 같은 임시 꼴이고, 낙석 이름으로 바꿀지는 RF-d에서 본다.
- **터널 폭발 안 보임(코드 변경 없음):** 디스크의 View `Test.asset`에 `hasRockfallTunnel` 줄이 없어 기본값 꺼짐으로 읽힌다. 꺼져 있으면 폭발 없이 진행하는 것이 RP-12다. 켜고 저장한 뒤 다시 확인한다. 폭발 큐 값(`Boom`·Start·EventCell)은 코드·에셋으로 대조했고 재생으로는 아직 확인하지 못했다.

### 6.3 터널 자리를 맵 프리팹 오브젝트로(2026-11-02 사용자 "`RockfallTunnelBoom` 빈 오브젝트에 Boom, Position·Rotation을 조절", 컴파일 미확인, 새 시험 없음, 시험 변경 없음)

- 배경 표시 `StageViewPropPresenter`가 세운 프랍 아래(비활성 포함)에서 이름 `RockfallTunnelBoom`을 찾아 `RockfallTunnel`로 낸다. 없으면 null, 둘이면 예외다.
- `FieldRockfallPresenter`는 그 Transform을 생성 인자로 받아 폭발 순간의 월드 위치·회전을 읽는다. `Render`의 `stageKey`와 스테이지 값 조회를 걷었다(§6.2 "계획과 다른 점" 3을 대신함). 계약은 `TryGetTunnelPose(out Pose)`.
- `StageViewPropData`의 칸 셋과 `Test.asset`의 값 세 줄을 걷었다. 다른 에셋에는 값이 없다.
- Factory 두 줄(`CombatSceneDisplayFactory`·`CombatTraitPreviewFactory`)의 생성 인자가 바뀌었다. 민형님 공유는 RF-d에 함께 적는다.
- 폭발 방향은 오브젝트의 파란 Z축이고, 큐의 위치·회전 보정이 그 위에 더해진다. 배율은 쓰지 않는다.
- 이름이 다르면 오류 없이 폭발만 빠진다. 사용자 프리팹 `NewStageTest.prefab`은 저장돼 있고 커밋 전이다.

Unity 확인: Ub 2·6·7을 이 방식으로 다시 본다. 7번은 오브젝트가 없는 맵(예: `Combat_Normal_A`)에서 낙석을 켜면 폭발 없이 경고 칸만 나오는 것으로 본다. 프리팹에서 오브젝트를 옮기거나 돌리면 폭발 자리·방향이 따라와야 한다. 터널 폭발은 사용자가 재생으로 확인했다(2026-11-02).

### 6.4 RF-c 충돌 코드·에셋 완료(2026-11-02 사용자 "돌은 smallrock (3) 프리팹, 배치판에서 끝나면 FallingRocks", 컴파일 미확인, 새 시험 없음, 시험 변경 없음)

- 돌 에셋: `RockfallVisual_Default.stonePrefab` → `Prefabs/Stage/MapProp/smallrock (3).prefab`. 이 프리팹은 FBX 변형이라 참조 번호를 계산해 넣었다(인스턴스 번호 XOR 원본 루트 번호, 다른 프리팹 셋으로 식 검증). 비면 부트 DB 생성이 실패한다.
- 풀: `FieldRockfallPresenter`의 `RentStone`·`ReturnStone`(계약 `ICombatRockfallField`). 필드 칸 루트 아래에 만들고 레이어를 20으로 맞춘다. 프리팹 레이어가 21(`AfterRendering`)이어서다(계획과 다른 점 1). 풀 최대값은 두지 않는다(계획 RP-5의 16과 다른 점 2). 경고 칸 수만큼만 생긴다.
- 조각: 새 `Presentation/Pieces/CombatRockfallDropPiece.cs`(+ `.meta`). 시작에 빌려 칸 위에 두고 제곱 곡선으로 칸 표면까지, 착지·취소 때 돌려준다.
- 처리기: `CombatRockfallEventHandler.TryBeginAfterFacts`. 경고 칸은 `ApplyFacts`에서 먼저 끈다. `Expected.WarnedCells`를 빈 칸(섞음)과 맞는 칸으로 나눠 빈 칸은 `dropIntervalSeconds` 간격으로, 맞는 칸은 그 뒤에 함께 떨어뜨린다. 칸마다 충돌 큐(`FallingRocks`·흔들림)를 그 돌의 끝 신호에 잇는다. 모두 착지하면 착지 소리 뒤 사건 완료다. 칸이 없으면 바로 끝난다. 생성 인자 넷이 늘었다(Presenter 한 줄).
- 동시 묶음: `CombatConcurrentGroups.AttachRockfallRecipients`(RP-8).
- 재생 중 정할 수 있는 값: 돌 프리팹의 메시 피벗이 바닥이 아니면 착지 때 칸에 묻히거나 뜬다. 돌 크기는 프리팹 값(약 0.88) × 필드 칸 루트 배율이다. 프리팹을 바꾸지 않았고 아직 재생으로 보지 못했다.
- 보고만: `Prefabs/Combat/Rockfall/RockfallStone.prefab`·`.mat`은 이제 참조가 없다(지우지 않음).

Unity 확인: §3의 2·3·4·6·8·9·14번. 돌이 칸에 닿는 순간 사라지고 그 칸에 `FallingRocks`가 나오며, 유닛이 있는 칸의 돌이 마지막에 떨어지고 그때 피격 반응이 함께 시작한다.

### 6.5 돌을 터널에서 곡선으로(2026-11-02 사용자 "터널에서부터 날아온 돌처럼 곡선", 컴파일 미확인, 새 시험 없음, 시험 변경 없음, 에셋 변경 없음)

- 궤적: 터널이 있으면 `RockfallTunnelBoom` 자리에서 칸 표면까지, 곡사 식 `CombatShotPath.ArcPoint` 하나를 공유한다. 진행은 일정하다. 터널이 없으면 지금처럼 칸 위에서 곧게 점점 빨라진다.
- 값 뜻: `dropHeight` = 터널과 칸을 이은 선 위로 곡선이 솟는 높이(터널 없으면 시작 높이), `dropSeconds` = 터널에서 칸까지 시간. 필드 이름은 그대로라 에셋 값이 유지된다.
- 돌 크기·회전은 그대로이고 날아가는 동안 돌지 않는다.
- 값 조절 자리: `DataBaseSO/ViewGroup/RockfallVisual/Item/RockfallVisual_Default.asset`의 Drop Height·Drop Seconds·Drop Interval Seconds.

### 6.6 돌을 위에서 곡선으로(2026-11-02 사용자 "터널에서 나오는 게 아니라 위에서 떨어지는데 곡선", 컴파일 미확인, 새 시험 없음, 시험 변경 없음) — §6.5를 대신함

- 궤적: 칸 + 위 `dropHeight` + 터널 쪽 옆 `dropSideDistance` 자리에서 시작한다. 진행률 t에 위치 = 칸 + 옆 × (1−t) + 위 × (1−t²). 시작은 옆으로 움직이고 끝은 거의 수직으로 떨어진다. 터널 방향은 칸마다 가로로만 구한다.
- 값: 새 칸 `dropSideDistance`(기본 5, 0 이상, 에셋에도 5). `dropHeight`는 다시 시작 높이다. 사용자가 넣은 에셋 값 15·1·0.3은 그대로다.
- 터널이 없으면 옆 거리를 쓰지 않고 칸 위에서 곧게 떨어진다. 터널 자세는 방향에만 쓴다.
- `CombatRockfallDropPiece`는 `CombatShotPath`를 더 쓰지 않는다. 사용자 말의 "곡선"을 이 모양으로 해석했고 재생에서 다시 본다.
- 재생 확인(2026-11-02 사용자): "폭발하는 것 빼고는 수정할 것 없어 보임". 곡선은 이 모양으로 확정하고, 사용자가 `dropSideDistance`를 15로 조정했다. §3 목록을 항목별로 확인받은 것은 아니다.

### 6.7 터널 폭발 강하게(2026-11-02 사용자 "모양과 방향은 맞는데 너무 약함", 에셋만, 코드 변경 없음, 재생 미확인)

- `RockfallVisual_Default.warningCues`: `Boom` 셋(지연 0·0.1·0.2, 자리를 조금씩 어긋나게)과 흔들림 하나(세기 0.5, 0.4초). RP-7의 "`Boom` 하나, 흔들림 없음"을 대신한다.
- 크기를 키우는 길은 쓰지 않았다. `BOOM.prefab`의 파티클은 Scaling Mode가 Local이라 받침 배율이 먹지 않고, 프리팹 값은 공격 착탄·지뢰·화약통과 함께 쓴다. 이것으로 부족하면 낙석 전용 이펙트(새 `EffectType`·표 항목·프리팹 사본)가 다음 안이다.
- `NewStageTest` 카메라에 흔들림 수신기가 있어 흔들림이 난다(프리팹 대조). 수신기 없는 맵에서는 흔들림만 빠진다.

### 6.8 RF-d 안내·문서(2026-11-02 사용자 "RF-d 시작해주세요", 컴파일 미확인, 새 시험 없음, 시험 변경 없음, 에셋 변경 없음)

- Rd.1: `AnkleBroken` 안내 "이동 불가 상태라 이동할 수 없습니다."(설계 RF6 문구), 짧은 글자 "이동불가". 이 사유는 `StatusEffectKey.Bind`이고 특성·낙석이 함께 쓴다. 상태 이름은 이미 "속박"이라 에셋은 그대로다.
- Rd.2: `CombatEnvironmentText.Build(CombatStateData state, CellCoord cell)`, 생성자 끝 인자 `RockfallVisualData`, 호버 호출 한 줄, Factory 한 줄. `{n}`은 `RockfallGimmickSpec.MaximumHealthLossPercent`이다. `WarnedCells`가 대기 단계에 비어 있어 단계를 따로 보지 않는다.
- 추가(§6.2의 "RF-d에서 본다"): 상태 설명 출처를 낙석이면 표 이름으로. `CombatStatusDescriptionFormatter` 생성자 끝 인자, `CombatUnitStatusFactory.Create` 끝 인자, `CombatDisplayTables` 한 줄. 다른 기믹은 `기믹 {키}` 그대로다.
- Rd.3: 폴더 배치 안내, 설계 §3.5·§9, 사운드 작업자 안내, 이펙트·소리 창구 안내.
- Rd.4(모두 미전달): 민형님 Factory 공유 §47, 현강님 메모 §19, 호진 설정 표 메모 §13, 용근님 메모 끝 절, Core 전달 문서 §63(제목 함께).

Unity 확인(Ud): 경고 칸 호버에 "낙석" 설명과 지금 값의 숫자(`{n}` 아님)가 나온다. 속박된 유닛을 움직이면 안내가 나오고 짧은 글자가 "이동불가"다. 속박 아이콘 설명의 출처가 "낙석"이다. 그 밖의 환경 호버(지형·오브젝트·장애물·토네이도)는 그대로다.

### 6.9 묶음 끝 검토(2026-11-02, 새 에이전트 하나, 코드·에셋만, 실행 없음, 약 9.2, 높음 0·중간 2·낮음 15, 반영은 사용자 결정 대기)

- M1 터널 폭발 큐가 끝 값 Natural인데 `BOOM` 자식 Smoke·Fire가 Looping이라 끝나지 않는 이펙트 경고가 남는다. 연기가 남거나 잘리는지는 재생으로 본다. 권장: 큐 끝을 Seconds로.
- M2 `CombatDisplayTables.cs:57`이 Factory를 거꾸로 참조한다. 낙석이 만든 구조가 아니라 낙석 밖 후보다.
- 권장 반영 후보: L1 속박 세부 문구 "행동 제한"(Core는 이동만 막음), L14 경고 칸이 비어도 폭발·흔들림이 나옴, L9 `RockfallTunnelBoom` 이름 오타를 알 수 없음.
- 그대로 둘 후보: L2~L8, L10~L13, L15(주석·계약 나누기·풀 공용화·에셋 값 등). L2의 "낮·밤 카메라 모두" 주석은 맞지 않고, 평소 화면에는 Main 카메라로 보인다.
- 화면 확인: 경고 표시가 첫 돌과 함께 꺼져 돌 5개면 최대 약 2.2초 칸 표시가 없음, 옆 거리 20이 고정이라 터널에 가까운 칸은 돌이 터널 너머에서 출발할 수 있음(추정).
- 문제를 찾지 못한 것: 컴파일 후보, 연결부, DB 생성, 시험 호출, 재생 멈춤, 수신자 묶음. 실행 확인은 아니다.
- 보고만: `Prefabs/Combat/Rockfall/RockfallStone.prefab`은 참조가 없다(지우지 않음).

### 6.10 검토 넷 반영(2026-11-02 사용자 "넷 반영", 컴파일 미확인, 새 시험 없음, 시험 변경 없음)

- M1: `RockfallVisual_Default.warningCues`의 `Boom` 셋을 끝 Seconds·1초로(`BOOM` 뿌리 파티클 길이와 같음). 1초 뒤 방출이 멎고 남은 입자가 사라진다. 값은 임의이고 재생에서 고칠 수 있다.
- L1: 상태 세부 문구가 `Bind`면 "이동 제한", 그 밖의 행동 제한은 "행동 제한" 그대로.
- L14: 경고 칸이 비면 `CombatRockfallEventHandler.ApplyFacts`가 표시를 끄고 폭발·소리를 내지 않는다.
- L9: `FieldRockfallPresenter.TryGetTunnelPose`가 터널이 없으면 처음 한 번 경고한다. 낙석 사건이 와야 묻기 때문에 낙석을 안 쓰는 맵은 경고가 없다. 폭발 없이 진행하는 동작(RP-12)은 그대로다. 이름 상수를 `internal`로.

Unity 확인(Ud 추가): 터널 폭발 뒤 연기·불이 남지 않고 끝나지 않는 이펙트 경고 로그가 없다. 폭발 길이가 짧게 느껴지면 End Seconds를 올린다. 속박 설명이 "이동 제한"이다. `RockfallTunnelBoom`이 없는 맵에서 낙석을 켜면 경고 로그가 한 번 나온다.

### 6.11 토네이도·낙석 규약 검사(2026-11-02 사용자 요청, 새 에이전트 하나, 코드 변경 없음, 실행 없음)

규칙 문서 대조: 높음 0, 중간 2, 낮음 9. 영역·코딩 규칙·시험은 지킴. 중간 둘은 코드와 설계로 직접 확인했다.

- M1(결함): `Row<CombatTornado…>`의 `overlapsUnitRuns: true`가 막기를 통과한 뒤 `CombatTornadoEventHandler`가 `ForAction` 실행을 연다. `CombatSubjectBlocking` 주석은 "행동 실행을 여는 사건은 모든 유닛 연출 뒤에만"이고, `CombatPieceRunPlayer.Tick`이 false를 돌려 걸음·반응 재생기가 폭풍 한 칸 동안 멈춘다. 끝나지 않는 멈춤은 아니다. 토네이도 TN10 때 생겼다.
- M2(규칙 위반 아님): 토네이도 칸을 칸으로 그리지 않는 것은 인계 §3·§9.3과 다르지만 사용자 결정 Q-TN1이고 토네이도 설계 §1·§6에 기록돼 있다.
- 낮음: 폴더 안내가 금지한 이름 `Presentation/Environment/`(L1), 값 검사 두 곳(L2), 돌 풀 직접 구현(L3), 토네이도 높이 식이 안내 §2 목록 밖(L4), 레이어 20 숫자 두 벌(L5), 묶음의 사건 형식 직접 비교(L6), 낡은 "2×2" 주석(L7), 전역 난수(L8), mapper 검사 순서(L9).
- 보고만: `RockfallStone.prefab`·`RockfallStone.mat`·`Sprites/rockfall_warning_Side.png`는 참조가 없다(지우지 않음).

### 6.12 규약 검사 반영(2026-11-02 사용자 "고쳐 주세요", 컴파일 미확인, 새 시험 없음, 시험 변경 없음, 에셋 변경 없음)

- M1: 새 조립 입구 `CombatComposition.ForFieldAction()`(값 `PausesOtherPlayers = false`). 조각 실행 재생기 Tick이 이 값을 보고 뒤 재생기를 멈추지 않는다. 토네이도 처리기가 이 입구를 쓴다. 다음 사건을 막는 것(`_actionRun`의 `PieceBusy`)은 그대로다. 막기 규칙 주석에 예외를 적었다. 낙석·포효·소환·공격은 그대로 멈춘다.
- L1: 폴더 안내 §4에 `Presentation/Environment/` 예외 한 줄.
- L5: 새 `Display/Field/FieldRenderLayers.cs`(+ `.meta`, `BothRendering = 20`). 돌과 가시밭이 이 값을 읽고 옛 상수 둘을 지웠다. 값은 그대로라 화면 변화는 없다. 폴더 안내 표 한 줄.
- L7: `TerrainVisualDataSO` 주석의 "2×2"를 뺐다.
- L9: `FieldRockfallPresenter.ShowWarning`이 느낌표(인자 검사)를 먼저 부른 뒤 강조 층을 바꾼다.
- 그대로: L2·L3·L4·L6·L8, M2(사용자 결정 Q-TN1).

Unity 확인: 컴파일. 토네이도가 이동하는 동안 다른 유닛의 걸음·피격 반응이 멈추지 않는다. 폭풍이 유닛을 날리는 모습과 착지 뒤 사건 순서는 전과 같다. 가시밭과 돌이 그대로 보인다.

## 7 돌 출발 능선 계획(RG-a~RG-c)

설계 [§10](../specs/2026-11-01-combat-rockfall-view-design-Hojin.md). 결정 Q-RG1~Q-RG3 모두 A(2026-11-02 사용자): 끝점 오브젝트 둘, 출발 높이는 능선 높이, 회전은 구르는 축 + 작은 흔들림.

§0 규칙을 그대로 따른다. Phase가 끝나면 멈추고 보고하며, 새 시험과 MSBuild는 없고, Core는 고치지 않는다. 작업 트리의 다른 변경(`DeckEditUI.prefab`·`StageSelect.unity`·FMOD·글꼴)은 건드리지 않는다.

코드로 확인한 기존 시험 영향은 없다. `Assets/Tests`에 낙석 표시·돌 조각·`RockfallVisualData`를 만들거나 부르는 곳이 없고, `new RockfallVisualData`는 SO의 `GetData()` 한 곳이다.

### 7.1 계획에서 정한 것(확인 대상)

| ID | 정한 것 | 이유 |
|---|---|---|
| RGP-1 | 배경이 `RockfallRidgeStart`·`RockfallRidgeEnd` 속성 둘로 내고, 표시 생성 인자도 둘이다. 값 묶음 형식을 새로 만들지 않는다 | 터널과 같은 꼴이고 파일이 하나 적다 |
| RGP-2 | 끝점이 하나만 있으면 없는 것과 같이 곧게 떨어지고, 경고 로그는 전투마다 한 번이다. 문구는 없는 쪽 이름을 적지 않고 두 이름을 함께 적는다 | 한쪽만 있는 구간은 쓸 수 없고, 오타를 찾으려면 두 이름이면 충분하다 |
| RGP-3 | 능선이 칸보다 낮으면 위 변위를 0으로 둔다. 돌이 아래에서 솟아오르지 않고 옆으로만 온다. 멈추지 않는다 | 배치 실수가 전투를 멈출 일이 아니고, 화면에서 바로 보인다 |
| RGP-4 | 흩뿌림은 구 안의 무작위 점이고 값 하나(`dropStartScatter`)로 조절한다. 회전축 흔들림 0.25는 코드 상수다 | 칸을 늘리지 않는다. 재생에서 필요하면 그때 칸으로 뺀다 |
| RGP-5 | 돌 크기의 기준은 프리팹 `localScale`이고, 빌릴 때마다 기준 × 배율로 다시 넣는다. 시작 회전은 무작위라 프리팹 회전을 쓰지 않는다 | 풀에서 다시 쓰는 돌에 지난 배율이 쌓이지 않는다 |
| RGP-6 | 돌마다 값은 조립할 때 한 번 뽑는다. 시작 지연을 기다리는 동안 값이 바뀌지 않는다 | 난수 위치가 규칙 함수 한 곳에 남는다 |
| RGP-7 | 칸 범위 검사는 Data 생성자에 둔다: `dropStartScatter` 0 이상, 배율 최소 양수·최대 ≥ 최소, 회전 최소 0 이상·최대 ≥ 최소. SO 칸은 `Min`만 건다 | 지금 파일 규칙과 같다 |
| RGP-8 | `dropHeight` 툴팁을 "능선이 없는 맵에서 떨어지기 시작하는 높이"로 바꾼다. 필드 이름과 에셋 값 20은 그대로다 | 칸의 뜻이 바뀐다 |
| RGP-9 | 도구는 터널 세기를 이름을 받는 함수 하나로 합치고, 3D 미리보기에는 능선을 그리지 않는다 | 사본을 늘리지 않는다. 프리팹 안 오브젝트라 3D 보기에 어차피 프리팹 그대로 나온다 |

### 7.2 Phase

#### RG-a 능선 읽기·값(화면 변화 없음)

| Task | 파일 | 내용 |
|---|---|---|
| Ra.1 | `RockfallVisualDataSO.cs` | "돌" 묶음에 칸 다섯(설계 §10.2 RG4), Data 속성 다섯·검사(RGP-7), `GetData()` 인자. `dropSideDistance`는 아직 둔다 |
| Ra.2 | `RockfallVisual_Default.asset` | 새 칸 시작 값 1.5·0.7·1.3·180·540 |
| Ra.3 | `StageViewPropPresenter.cs` | 이름 상수 둘, `FindRockfallTunnel` → 이름을 받는 `FindUniqueByName(root, name, stageKey)`, 속성 둘, 설치·`Release` 줄 |
| Ra.4 | `ICombatRockfallField.cs`, `FieldRockfallPresenter.cs` | `TryGetRidge(out Vector3 start, out Vector3 end)`, 생성 인자 둘, 경고 한 번(RGP-2) |
| Ra.5 | `CombatSceneDisplayFactory.cs:145`, `CombatTraitPreviewFactory.cs:74` | 생성 줄 인자 둘 |

Ua: 컴파일. 부트 DB 생성이 된다. 낙석이 지금과 똑같이 나온다(아직 아무도 능선을 묻지 않아 경고도 없다).

#### RG-b 돌 낙하(화면이 바뀜)

| Task | 파일 | 내용 |
|---|---|---|
| Rb.1 | 새 `Presentation/Pieces/CombatRockfallDrop.cs`(+ `.meta`) | 읽기 전용 값: 칸·옆 변위·위 변위·크기·회전축·회전 속도·시작 회전 |
| Rb.2 | 새 `Presentation/Pieces/CombatRockfallDrops.cs`(+ `.meta`) | 아래 "규칙" |
| Rb.3 | `ICombatRockfallField.cs`, `FieldRockfallPresenter.cs` | `RentStone(float scale)`(RGP-5). 터널 없음 경고에서 돌 문구를 뺀다(RG6) |
| Rb.4 | `CombatRockfallDropPiece.cs` | 생성자가 값 하나와 시간을 받는다. `Begin`이 크기·위치·시작 회전, `Tick`이 위치 + `AngleAxis(속도 × 경과, 축) × 시작 회전` |
| Rb.5 | `CombatRockfallEventHandler.cs` | `TryBeginAfterFacts`가 능선을 한 번 읽고, `AddDrop`은 규칙을 불러 조각에 넘긴다. 터널 방향 계산을 지운다. 처리기 머리 주석을 고친다 |
| Rb.6 | `RockfallVisualDataSO.cs`, `RockfallVisual_Default.asset` | `dropSideDistance` 칸·속성·검사·인자·에셋 줄 삭제, `dropHeight` 문구(RGP-8) |

규칙 `CombatRockfallDrops`:

- `FromRidge(landing, up, start, end, settings)`: 두 끝점에서 칸을 뺀 값을 가로 평면으로 눌러 구간 비율 t(0~1)를 구한다. 구간 길이가 0이면 t = 0이다. 출발 자리 = `Lerp(start, end, t)` + 구 안 무작위 × `dropStartScatter`. 칸에서 출발 자리까지를 옆 변위(가로)와 위 변위(세로, 0 아래는 0, RGP-3)로 나눈다.
- `Straight(landing, up, settings)`: 옆 변위 0, 위 변위 `up × dropHeight`. 출발 흩뿌림을 쓰지 않는다.
- 공통 `WithLook`: 크기·회전 속도는 최소~최대 난수, 시작 회전은 무작위. 진행 방향이 있으면 축 = `Cross(up, 진행 방향)` + 구 안 무작위 × 0.25, 없으면 무작위 단위 벡터.

곡선 식 `칸 + 옆 × (1−t) + 위 × (1−t²)`는 그대로다.

사용자 에디터 작업(Rb 뒤): `NewStageTest.prefab`에 빈 오브젝트 `RockfallRidgeStart`·`RockfallRidgeEnd`를 돌산의 전투판 쪽 경사면 위에 놓는다.

Ub: 아래 §7.3.

#### RG-c 도구·문서(화면 변화 없음)

| Task | 파일 | 내용 |
|---|---|---|
| Rc.1 | `CombatStageSoAssets.cs` | 이름 상수 사본 둘, `RockfallTunnelCount`의 몸을 이름을 받는 함수 하나로, 능선 세기 입구, 주석의 함수 이름 |
| Rc.2 | `CombatLayoutWindow.SoUx.cs`, `CombatStageSoValidation.cs` | 낙석이 켜져 있고 끝점이 없으면 경고, 같은 이름이 둘 이상이면 오류(낙석이 꺼져 있어도). 호출 줄은 그대로다 |
| Rc.3 | `Tool/SO모드 사용법.md` §5.11·§8 표 | "돌 출발 능선" 표(터널 표와 같은 꼴) |
| Rc.4 | 폴더 배치 안내 | 머리 줄과 `Pieces/` 줄에 새 파일 둘 |
| Rc.5 | 설계 §10 구현 메모, 이 계획 실행 기록, `CLAUDE.md` | 결과 요약 |
| Rc.6 | 공유(미전달) | 민형님 Factory 공유 §49(표시 생성 줄 둘), 호진 설정 표 메모 §15(값 칸·능선 놓는 법), 용근님 메모 끝 절(`CombatTraitPreviewFactory` 인자). Core 변경이 없어 Core 전달 문서는 고치지 않는다 |
| Rc.7 | 묶음 끝 검토 | 새 에이전트 하나 |

Uc: 도구에서 낙석을 켠 스테이지의 검사 결과가 §10.3 표대로 나온다.

### 7.3 Unity 확인(Ub)

능선 놓기 전:

1. 돌이 칸 위에서 곧게 떨어지고 돌면서 크기가 제각각이다.
2. 능선 없음 경고 로그가 전투마다 한 번 나오고 전투가 멈추지 않는다.

능선 놓은 뒤:

3. 돌들이 능선 넓이에서 나란히 내려온다. 위에서 볼 때 한 점으로 모이지 않는다.
4. 끝점을 옮기면 출발 자리가 따라온다. 끝점을 돌산 쪽으로 멀리 두어도 돌이 칸에 정확히 닿는다.
5. 돌이 진행 방향으로 구르는 것처럼 보인다. 피벗이 돌 가운데에서 벗어나 휘둘려 보이면 `smallrock (3)` 대신 돌 전용 프리팹이 필요하다(§6.4의 피벗 확인과 같은 문제).
6. 착지 순간에 돌이 사라지고 그 칸에 `FallingRocks`와 흔들림이 나온다.
7. 유닛이 있는 칸의 돌이 마지막에 함께 착지하고, 피격 반응이 그때 함께 시작한다.
8. 돌이 바위 메시를 뚫고 지나가면 끝점을 옮겨 본다.
9. 터널 폭발은 전과 같고, 터널을 옮겨도 돌 경로가 바뀌지 않는다.
10. 전투를 나갔다 들어와도 돌 크기가 어긋나거나 남지 않는다.

값 조절 자리: `RockfallVisual_Default.asset`의 Drop Start Scatter, Stone Scale, Spin Degrees, Drop Seconds, Drop Interval Seconds.

### 7.4 커밋 묶음

- Ra.1·Ra.2와 Rb.6은 각각 SO 코드와 에셋을 한 커밋에 넣는다. 에셋만 있거나 코드만 있으면 값이 어긋난다.
- 새 `.cs` 둘은 `.meta`와 함께 넣는다.
- 능선을 놓은 `NewStageTest.prefab`은 사용자 변경이다. 함께 올리면 다른 PC에서도 능선이 보인다.

### 7.5 실행 기록

**RG-a 코드·에셋 완료(2026-11-02 사용자 "그대로, RG-a" = RGP-1~RGP-9 그대로, 새 시험 없음, 시험 변경 없음, Ua 문제 없음 — 사용자):**

- 값: `RockfallVisualDataSO`에 `dropStartScatter`·`stoneScaleMin`·`stoneScaleMax`·`spinDegreesMin`·`spinDegreesMax`, 범위 검사, 에셋 값 1.5·0.7·1.3·180·540. 아직 읽는 곳이 없다.
- 배경: `StageViewPropPresenter`의 이름 상수 둘·속성 둘, 터널 찾기를 `FindUniqueByName` 하나로 합침. 같은 이름이 둘 이상이면 낙석이 꺼져 있어도 진입 때 멈춘다(터널과 같음). 도구 오류는 RG-c다.
- 표시: `ICombatRockfallField.TryGetRidge`, `FieldRockfallPresenter` 생성 인자 둘, 끝점이 빠지면 경고 한 번. 아직 부르는 곳이 없어 경고도 나오지 않는다.
- 연결: `CombatSceneDisplayFactory`·`CombatTraitPreviewFactory` 생성 줄(공유는 RG-c).
- 계획과 다른 점 없음. 화면 변화 없음.

**RG-b 코드·에셋 완료(2026-11-02 사용자 "문제 없음, RG-b", 컴파일 미확인, 새 시험 없음, 시험 변경 없음, Ub 대기):**

- 새 값 `Presentation/Pieces/CombatRockfallDrop.cs`, 새 규칙 `CombatRockfallDrops.cs`(+ `.meta`): 능선 위 가장 가까운 점 + 흩뿌림, 크기·회전 난수.
- `RentStone(float scale)`, 낙하 조각은 값 하나를 그리기만 한다. 곡선 식은 그대로다.
- 처리기가 터널 방향 계산을 걷었다. 터널은 경고 폭발에만 쓴다.
- `dropSideDistance` 칸·검사·에셋 줄 삭제, `dropHeight`는 능선이 없는 맵에서만 쓴다.
- 계획과 다른 점 하나: 능선을 `TryBeginAfterFacts`에서 한 번 읽지 않고 `AddDrop`이 돌마다 읽는다. 인자를 셋 줄이고, 경고는 표시가 한 번만 낸다.
- 설계 §3.5·§9의 옆 거리 문구, 호진 설정 표 메모의 옛 값은 RG-c에서 고친다.
- 능선 끝점은 아직 프리팹에 없다. 놓기 전까지 돌은 칸 위에서 곧게 떨어지고 경고 로그가 한 번 나온다. 지금 터널 쪽 곡선은 없어졌다.
- Ub: 사용자 "문제 없음"(2026-11-02). 그때 저장소의 맵 프리팹에는 능선 끝점이 없어(`RockfallRidge` 검색 0), 능선을 놓은 재생을 확인한 것인지는 알 수 없다. §7.3의 3~5·8번은 능선을 저장한 뒤 다시 본다.

**RG-c 코드·문서 완료(2026-11-02 사용자 "문제 없음, RG-c", 컴파일 미확인, 새 시험 없음, 시험 변경 없음, 공유 미전달):**

- 도구: 이름 사본 둘, 세기 함수를 이름을 받는 `RockfallObjectCount` 하나로, 같은 이름 둘 이상은 저장 오류(`RockfallUniqueObjectNames`를 돈다), 끝점이 빠지면 낙석이 켜졌을 때만 경고.
- 계획과 다른 점 하나: 경고 함수의 이름이 터널만을 뜻해 `AddSoRockfallPropWarnings`로 바꿨고 호출 줄 둘도 바뀌었다(Rc.2의 "호출 줄은 그대로"와 다름).
- 문서: 사용법 §5.11·검사 표, 폴더 안내, 설계 §3.5·§5·§9·§10.9.
- 공유(모두 미전달): 민형님 Factory 공유 §49, 호진 설정 표 메모 §15, 용근님 메모 끝 절. Core 변경이 없어 Core 전달 문서는 고치지 않았다.

**묶음 끝 검토(2026-11-02, 새 에이전트 하나, 코드·에셋만, 실행 없음, 약 9.5, 높음 0·중간 1·낮음 11):**

- M1(결정 대기) `smallrock` 메시 피벗이 돌 가운데에서 떨어져 있어(에이전트가 FBX 정점을 읽은 값, 월드 약 0.42~0.79 추정) 돌면 공전하듯 휘둘리고 착지 자리가 어긋날 수 있다. 권장: 코드는 두고, 빈 루트 아래에 메시를 가운데 맞춘 돌 전용 프리팹을 `stonePrefab`에 연결. 화면 정도는 재생으로 본다.
- 반영(2026-11-02 사용자 "셋 다 반영", 컴파일 미확인): L2 맵 SO 안내 문구·주석에 능선을 더함, L3 설계 RG5의 값·규칙 이름과 §10.9 문구를 구현에 맞춤, L10 처리기 지역 변수 `dropLook`·`dropIndex`. 동작 변화 없음.
- 그대로 둘 후보: L1 도구 이름 사본, L4·L5 넓은 계약, L6 전역 난수, L7 앵커 수정 자리, L8 Transform 노출, L9 `Try` 안 경고, L11 진입 때 세 번 훑기.

**능선 끝점 자리(2026-11-02 사용자 "위치 수정할 수 있어?", 재생 미확인):**

- 사용자가 만든 끝점 둘(부모 `RockfallTunnelBoom`, 로컬 (0, 0, 0)이던 것)의 로컬 위치를 `RockfallRidgeStart` (−9.5, 10.5, 0)·`RockfallRidgeEnd` (10.5, 10.5, 0)으로 넣었다. 프리팹의 다른 줄은 건드리지 않았다.
- 필드 기준 자리: X −18.4(터널과 같은 줄), 높이 9, Z −7.5 ~ 7.5(9×9 판 −6.75 ~ 6.75보다 반 칸씩 넓게). 화면에서는 판의 왼쪽 위 변과 나란하다.
- 계산: `Test` 배경은 회전 −90°·배율 0.75이고, 타일 맞춤 뒤 배경 자리가 (0.375, 0, 0.75)라 타일 맵 가운데가 판 가운데(필드 원점)에 온다. 터널 쪽 바위의 경계 상자 윗면은 가까운 줄(X −13 ~ −23) 약 5~8, 그 뒤 줄 약 8~12다(FBX 정점에서 계산). 돌린 상자라 실제 윗면은 이보다 낮을 수 있다.
- 옆 거리는 판 가까운 줄 약 12, 먼 줄 약 24다. 기본 확대(직교 크기 8)에서는 출발 자리가 화면 왼쪽 위 밖이라 돌이 화면 밖에서 들어오고, 가장 멀리 축소(12)하면 일부가 보인다.
- 높이 고침(2026-11-02 사용자 "돌이 더 위에서 떨어지면 좋겠어요"): 사용자가 Unity에서 시작점 로컬 y를 30, 끝점을 5로 바꿔 둔 상태였고, 끝점도 30으로 맞췄다. 필드 기준 높이는 둘 다 약 23.6이다(옛 칸 위 `dropHeight` 20과 비슷). 재생 미확인.
- 문제를 찾지 못한 것: 호출 서명, 곡선 끝값, 능선 투영·구간 0, 회전 방향, 풀 재사용 크기, 취소, 착지 순서, `.meta`, 도구와 게임의 일치. 실행 확인은 아니다.
- 간접 근거: `ProjectL.Systems.dll`·`ProjectL.Tool.Editor.dll`이 소스보다 새롭고 Editor.log에 컴파일 오류가 없다는 에이전트 보고. 사용자 Unity 확인을 대신하지 않는다.
