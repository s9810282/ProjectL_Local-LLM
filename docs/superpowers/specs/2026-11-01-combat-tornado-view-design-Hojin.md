# 토네이도 전투 View 설계

- 작성자: 호진(이호진)
- 상태: 결정 완료(2026-11-01 사용자), 코드 변경 없음. Task 계획 [tornado-view-plan](../plans/2026-11-01-combat-tornado-view-plan-Hojin.md).
- 근거: [Core → View 인계](../handoffs/2026-11-01-tornado-rockfall-haboob-core-view-handoff.md) §2·§3·§4·§8·§9.3(이 설계의 기준), Core 설계 [tornado-stage-gimmick-design](2026-11-01-tornado-stage-gimmick-design.md), 기획서 `Dropbox/Project L/기획 문서/기믹 기획서.pptx` 5쪽(Ver0.1).
- 순서: 토네이도 → [낙석](2026-11-01-combat-rockfall-view-design-Hojin.md) → [하붑](2026-11-01-combat-haboob-view-design-Hojin.md)(2026-11-01 사용자 결정, 인계 §7).
- 범위: Core가 낸 토네이도 사실을 전투 화면에 보이고, 옛 지형 토네이도 표시를 대신하는 일. Core 규칙은 바꾸지 않는다.
- 범위 밖: 낙석·하붑, 증기 기관차 경고, 토네이도 전용 아트.
- 정리: 2026-11-02 옛 지형 토네이도 설계·계획(10-17~10-31, View 코드는 TN-f에서 걷음)을 §11·계획 §5로 줄여 합쳤다. 옛 판은 git `3ad97e734`.

## 1 지금 코드에서 확인한 것

| 사실 | 근거 | 설계에 주는 영향 |
|---|---|---|
| 폭풍이 지형 목록에서 빠졌다 | `BattleDefinitionAdapter:85`, `TornadoGimmickState` | 지형 표시로는 보이지 않는다. 새 표시가 필요하다 |
| 플레이어에게 허용된 영역을 읽는 입구가 있다 | `CombatStateData.ReadTornadoAreas(Owner.Player)` | 원본 전체(`Members`)를 읽지 않아 숨김 규칙을 지킨다 |
| 옛 사건은 더 이상 생기지 않는다 | `new CombatTornadoEvent` 없음 | 옛 줄·소리·숨기기 창구는 닿지 않는 코드다 |
| 새 사건 4종이 형식 표에 없다 | `CombatEventTypeTable` | 지금은 경고 후 건너뛴다 |
| 던짐은 있는 재배치 사건이다 | `CombatCharacterEvent.TornadoContact` | 들어 올림 연출을 다시 쓸 수 있다 |
| 던짐 연출이 옛 표시를 찾는다 | `FieldTerrainPresenter.TornadoLiftTarget:397` | 지금 던지면 예외로 전투가 멈춘다(코드 판단, 재생 미확인) |
| 몸이 빠져나가면 폭풍을 숨긴다 | `CombatTornadoLiftPiece.DecideLeave` → 숨기기 콜백 | 폭풍은 이제 사라지지 않는다. 걷는다 |
| 자리 계산이 옛 2×2다 | `FieldTerrainPresenter.LocalPosition`, `TornadoBaseHeight` | 중심 기준으로 새로 쓴다 |
| 외형 전환 부품은 쓸 수 있다 | `CombatTornadoAppearance`(생성·축소) | 처음 등장·정리에 그대로 쓴다 |
| 설정이 이미 있다 | `TornadoPresentationSettingsSO`, `TerrainVisual_Tornado` | 값 칸만 더한다 |
| 연출 조각 길이 있다 | `CombatComposition.ForAction`, `CombatPieceRunPlayer` | 폭풍 이동을 있는 수명·취소 길에 태운다(인계 §2.5) |
| 놓은 스테이지가 없다 | `Terrain_Tornado` 참조 0 | 확인은 사용자가 `Test`에 놓고 한다 |

## 2 화면

단계·중심·방향은 Core 값을 그대로 쓴다. 기본 값 기준 시간표는 인계 §4.1이고, View에 적지 않는다.

| 시점 | 화면 | 다음 사건 |
|---|---|---|
| 전투 진입·재연결 | 허용된 폭풍마다 이펙트(몸)를 둔다. 배치 단계에도 보인다 | - |
| 경고(`TornadoMember`, 이동 아님) | 그 폭풍에 방향 화살표를 켠다. 경로 전체는 보이지 않는다 | 막지 않음 |
| 중심 도달(`TornadoCenter`) | 0번은 출발 자리 확인뿐이다. 1번부터 다음 중심까지 이펙트가 움직인다 | 도착까지 막음 |
| 던짐(`Relocated`) | 그 사건의 중심으로 빨려 들어가 올라갔다가 목적지에 떨어진다. 폭풍은 그대로 있다 | 착지까지 막음 |
| 이동 끝(`TornadoMember`, 이동) | 화살표를 끄고 최종 자리로 맞춘다. 이미 경고 단계면 새 화살표를 켠다 | 막지 않음 |
| 진입 시작(`TornadoEntry`) | 표시 없음. 던짐은 뒤따르는 재배치 사건이 보인다 | 막지 않음 |
| 결과 끝 | Buffer 최종 상태로 다시 맞춘다 | - |

요약 사건(`TornadoAdvanced`)으로는 아무것도 옮기지 않는다. 최종 값이 들어 있어 먼저 쓰면 접촉 전에 폭풍이 앞질러 간다(인계 §4.4).

이펙트는 한 칸씩 끊기지 않게 미끄러져 가고, 칸에 도착할 때마다 그 3×3에 닿은 유닛의 던짐이 끝난 뒤 다음 칸으로 간다.

숨겨진 폭풍은 이펙트·화살표를 그리지 않고, 유닛 던짐만 재생한다(인계 §4.2).

## 3 구조

### 3.1 사건 줄(TN1)

`CombatEventTypeTable`에 네 줄을 더하고 새 처리기 칸 `Tornado`로 묶는다(인계 §1.2 기능별 처리). 주체·근원이 없어 묶음 밖 장벽이고, 재생 순서는 Core 순서 그대로다.

새 `Playback/Handlers/CombatTornadoEventHandler.cs`:

- `TornadoAdvanced`, `TornadoEntry`: 할 일 없음.
- `TornadoCenter`: 보이는 폭풍이고 1번 이상이면 이동 조각을 시작하고, 도착하면 사건을 끝낸다. 그 밖은 자리만 맞춘다.
- `TornadoMember`: `Advance.Final`을 표시에 반영한다.

보이는 폭풍인지는 표시에 그 `SourceId` 인스턴스가 있는지로 답한다. 인스턴스는 허용 영역으로만 만들어지므로 숨김 판단이 한 곳에 모인다.

### 3.2 범위 표시(TN2)

3×3 범위는 상시 칸 강조를 두지 않는다(Q-TN1). 프리팹 이펙트가 범위를 보여 주고, 정확한 칸은 호버 설명으로 알 수 있다. TA가 만들 최종 이펙트로 프리팹만 바꾼다. **2026-11-02 사용자 요청으로 호버 중에만 현재 그룹 영역을 강조하는 기능을 추가했다.** 정책·구현 원본은 [기믹 호버 범위 설계](2026-11-02-combat-gimmick-hover-range-design.md), 실행은 해당 계획 GH-a다(컴파일 미확인).

### 3.3 토네이도 표시(TN3)

새 `Display/Map/FieldTornadoPresenter.cs`: `SourceId`마다 몸(`CombatTornadoAppearance`)·방향 화살표 하나씩.

- `Render(areas, mapper)`: 진입·결과 끝. 목록에 없는 원본은 지운다. `RefreshCore`에서 부른다.
- `Place(sourceId, center, side, mapper)`: 자리를 바로 맞춘다.
- `SetDirection(sourceId, direction)`: 화살표. 비거나 0이면 끈다.
- `TryGetLiftTarget(sourceId, ...)`: 들어 올림 조회.
- `Clear`, `Release`: 표시 정리 목록에 더한다. 전투가 바뀌면(`BattleInstanceId`) 이 경로로 비워 지난 전투의 원본을 남기지 않는다.

자리 계산은 `CellBaseToLocal(center)` + 값 보정이고, 높이는 덮는 칸의 타일 바탕 중 최댓값이다(옛 `TornadoBaseHeight`를 공개된 칸 목록으로 일반화). 옛 반 칸 이동은 넣지 않는다. 몸 배율은 프리팹 배율 × (`SideLength` ÷ 기준 칸 수)다.

이벤트 중 보이는 중심은 이 인스턴스들이 갖는다. 게임 상태가 아니며 결과 끝에 Buffer로 다시 맞춘다(인계 §2.2).

### 3.4 폭풍 이동 조각(TN4)

새 `Presentation/Pieces/CombatTornadoMovePiece.cs`: 현재 중심 → 사건 중심을 값의 시간에 옮긴다. 실행은 `CombatComposition.ForAction`이고 `CombatPieceRunPlayer`가 진행·취소한다. 결과 끝 사실 반영도 기다린다.

### 3.5 던짐 연출(TN5)

`CombatCharacterEventHandler.TryBeginTornado`를 고친다.

- 사건의 `TornadoContact`로 대상을 찾는다. 중심은 `Contact.Center`의 월드 자리이고, 표시의 최종 자리나 유닛 위치를 쓰지 않는다.
- 설정은 인스턴스가 아니라 `TerrainVisual`의 Tornado 줄에서 읽는다. 그래서 숨겨진 폭풍도 같은 길로 던진다.
- `CombatTornadoLiftPiece`의 몸 이탈 판정과 숨기기 콜백을 걷는다. `CombatTornadoLiftTarget`의 `Top`도 함께 걷는다.
- 도착 자세는 그 사건의 `Relocated.Final`만 쓴다. 뒤따르는 밀림·사망 사실을 덮지 않는다.

같은 폭풍의 여러 유닛, 다른 폭풍의 연쇄 던짐, A → B → A는 사건마다 한 번씩 그대로 재생한다. 겹치는 방식은 TN10이 대신한다.

### 3.6 값(TN6)

`TornadoPresentationSettingsSO`에 칸을 더한다: 칸 이동 시간, 방향 화살표 프리팹, 몸 기준 칸 수, 경고 소리 프리셋. `TerrainVisual_Tornado`의 위치 보정(−0.5, −0.5)은 프리팹 축 보정이라 값을 두고 배율만 함께 적용한다.

임시 외형은 있는 에셋만 쓴다(Q-TN2). 몸은 지금 토네이도 프리팹이고 2×2로 만들어져 기준 칸 수는 2다. 화살표는 `Prefabs/Combat/Arrow.prefab`이다. TA 이펙트가 오면 프리팹과 기준 칸 수만 바꾼다.

### 3.7 호버·소리(TN7)

- `CombatEnvironmentText`: `TornadoSourceFact` 대신 `ReadTornadoAreas`의 칸으로 설명을 찾는다. `{n}` 값은 `StageGimmicks`의 `TornadoMemberSpec`에서 읽는다. 지형 사실을 꾸며 내지 않는다. `TerrainVisual_Tornado`의 옛 설명("무작위 위치에 다시 생성")도 고친다.
- 2026-11-02 그룹 호버 영역: `CombatEnvironmentHover`가 같은 칸 집기 결과로 공개 토네이도 영역의 전체 칸을 별도 강조 층에 전달한다. 범위·겹침·수명 정책은 [기믹 호버 범위 설계](2026-11-02-combat-gimmick-hover-range-design.md)가 원본이다.
- `CombatEventSounds`: 옛 갈래를 걷는다. 보이는 폭풍의 0번 중심에 `SfxEvent.Tornado`를 한 번 내고, 경고는 연결 대기 슬롯이다. 요약·중심·완료마다 겹쳐 내지 않는다.

### 3.8 SO 모드 도구(TN8)

도구는 호진 영역이고 `Tool/Editor/`에 있다.

- 3D 미리보기의 2×2 자리(`CombatStageSoVisuals.TornadoPosition`)를 중심 기준 3×3으로 바꾼다.
- 배치 칸은 이제 폭풍 중심이다. 3×3이 판 밖으로 나가면 저장 때 문제로 보인다.
- 안쪽 고정 중심은 가로 방향이 없으면 전투 준비에서 예외다. `tornadoInitialHorizontalSign` 칸을 패널에 더한다.

### 3.9 옛 길 걷기(TN9)

`CombatTornadoEvent` 줄과 사실 처리기 갈래, 필드 창구 `HideTornado`·`ShowTornado`·`TornadoLiftTarget`, `FieldTerrainPresenter`의 토네이도 갈래, `EnvironmentVisualKinds`·`CombatEnvironmentText`의 `TornadoSourceFact` 갈래를 걷는다(Q-TN4). Core 형식 자체는 건드리지 않는다.

## 4 경계 경우

| 경우 | 처리 | 원본 |
|---|---|---|
| 판이 좁아 이동 없음 | 0번 중심만 온다. 화살표 없음, 접촉은 그대로 재생 | 인계 §4.3 |
| 짧은 반복 간격 | 이동 끝 상태가 이미 경고다. 경고 사건을 기다리지 않고 화살표를 켠다 | 인계 §4.1 |
| 치명적 착지 | 도달한 중심까지만 온다. 나머지를 꾸며 내지 않는다 | 인계 §4.6 |
| 끌어오기·넉백 중 던짐 | Core가 잘린 경로만 준다. 남은 길을 재생하지 않는다 | 인계 §4.6 |
| 면역·착지 칸 없음 | 진입 시작 사건만 오고 던짐이 없다 | 인계 §4.4 |
| 배치 단계로 돌아감 | 중심·기한을 유지하고 그대로 보인다. 화살표는 `WarningDirection`대로 꺼진다 | 인계 §4.1 |
| 결과 안의 최종 `IsActive` | 재생 중에는 쓰지 않는다. 결과 끝에만 맞춘다 | 인계 §4.2 |
| 되돌리기 | Core 응답대로다. 던짐을 다시 재생하지 않는다 | 인계 §4.6 |

## 5 인계 문서와 다른 점

- 인계 §9.3은 배치 단계 사각형이 공개 칸과 맞는지 보라고 한다. 칸 표시를 두지 않으므로(Q-TN1) 이펙트 자리와 호버 설명으로 확인한다.
- 인계 §9.2는 사건이 건너뛰어지면 시험으로 잡으라고 한다. View 작업은 새 시험을 만들지 않는 규칙(2026-10-27 사용자)이라 재생 확인으로 대신한다. 인계 §9.3의 나머지 목록을 Unity 확인 항목으로 쓴다.

## 6 결정(2026-11-01 사용자)

| ID | 결정 |
|---|---|
| Q-TN1 | 상시 3×3 칸 강조는 두지 않는다. TA가 더 좋은 이펙트를 만들어 준다. 2026-11-02 호버 중 그룹 영역 강조는 별도 확장(§3.2) |
| Q-TN2 | 임시 외형은 지금 있는 프리팹을 쓴다(§3.6). 최종 외형도 프리팹으로 온다 |
| Q-TN3 | ~~한 중심에서 여러 유닛이 날아가면 사건 순서대로 차례로 재생한다~~ 2026-11-01 사용자가 바꿈: 시작 순서는 사건 순서이고 함께 날아가며 폭풍은 멈추지 않는다(TN10) |
| Q-TN4 | 옛 토네이도 표시 코드는 마지막 Phase에 걷는다. 그 코드를 검사하던 시험은 함께 정리하고, 다른 사람 시험이면 먼저 묻는다 |
| Q-TN5 | SO 모드 도구를 이번 범위에 넣는다 |

## 7 Phase(안)

| Phase | 내용 | 화면 변화 |
|---|---|---|
| TN-a | 값 칸, 있는 프리팹 연결 | 없음 |
| TN-b | 표시, 진입·결과 끝, 방향 화살표 | 폭풍 보임(던지면 아직 예외) |
| TN-c | 던짐 연출 수정 | 던짐 동작 |
| TN-d | 사건 줄·처리기, 이동 조각 | 경고·이동 |
| TN-e | 호버, 소리, SO 모드 도구 | 설명·소리 |
| TN-f | 옛 길 걷기, 문서·공유, 묶음 끝 검토 | 없음 |

`Test` 스테이지 확인은 TN-c 뒤부터 안전하다. TN-b까지는 폭풍에 닿으면 전투가 멈춘다.

## 8 원칙 점검(코드 전 예상, 약 8.7/10)

| 기준 | 판단 |
|---|---|
| SSOT | 중심·단계·방향은 Core 값. 허용 판단은 `ReadTornadoAreas` 하나. 칸 목록은 Core가 파생한 값을 쓴다 |
| SOLID | 표시·이동 조각·처리기를 나눈다. 전용 처리기 칸이라 사실 처리기가 늘지 않는다. 약점: 처리기 표 인자와 필드 창구 인자가 는다 |
| FW | 이펙트·카메라·조각 실행은 있는 창구. 값은 View DB. 새 서비스 등록 없음 |
| 독립성 | 이동 속도는 SO, 이동 곡선은 조각 하나, 표시 모양은 표시·프리팹만 고친다. 낙석·하붑 내부를 몰라도 된다 |

남는 위험:

- 공유 파일(형식 표, 처리기 표, 필드 창구, 표시 Factory)은 민형님 공유 문서에 적는다. 필드 창구는 용근님 특성 미리보기 Factory에서도 만들어 그 줄도 맞춘다. 창구를 만드는 시험은 없다.
- 컴파일과 재생 확인은 사용자가 Unity에서 한다.

## 9 구현 메모(2026-11-01, TN-a~TN-f, 컴파일 미확인 부분은 계획 §4)

이동하면서 날리기는 §10이다.

- 칸 표시·칸 이동을 만들지 않았다(사용자가 TP-4·TP-10 철회, TA 이펙트가 맡음). 폭풍은 몸과 방향 화살표만 보인다.
- 표시 계약은 `ICombatTornadoField` 하나다. 처리기·필드 창구는 표시 클래스를 모른다(TP-3).
- 임시 몸 프리팹(`Toneido.prefab`)의 층은 켜져도 저절로 재생하지 않아 `CombatTornadoAppearance`가 켤 때 입자를 비우고 재생한다. 민형님 `c6e5478ce`(10-30)의 프리팹 변경 때문이고 프리팹은 고치지 않았다.
- 소리는 보이는 폭풍에만 나므로 소리 표가 아니라 처리기가 낸다. 무엇을 내는지는 소리 표 함수(`TornadoMove`·`TornadoWarning`)가 답한다. 경고 슬롯은 연결 대기다.
- SO 모드: 좌우 뒤집기·대칭 복사가 가로 방향도 뒤집고, 작업본 형식을 3으로 올렸다(둘 다 계획에 없던 것, 계획 §4).
- 옛 길은 View에서 모두 걷었다. Core의 `CombatTornadoEvent`·`TornadoSourceFact`·옛 입력 칸은 Core 계약이라 그대로 있고, Core가 옛 사건을 내지 않아 View는 읽지 않는다.

## 10 이동하면서 날리기(TN10, 2026-11-01)

사용자 요청: 유닛이 판정에 들어와도 폭풍이 멈추지 않고, 이동하면서 날린다. 조건은 토네이도 밖에 영향이 없을 것.

- 던짐 사건은 유닛이 빨려 들어가기 시작하면 완료다. 착지 사실 반영은 그 실행이 끝날 때 한다.
- 토네이도 사건 넷은 진행 중인 유닛 연출을 기다리지 않는다(형식 표 칸 `overlapsUnitRuns`, 막기 규칙 `CombatSubjectBlocking`). 폭풍 이동은 행동 실행 그대로라, 도착하기 전에는 그 칸의 던짐이 시작하지 않는다. 다만 다른 행동 실행과 달리 진행 중인 걸음·반응의 Tick을 멈추지 않는다(필드 행동 `CombatComposition.ForFieldAction`, 2026-11-02 규약 검사 M1 수정).
- 빨려 들어가는 목표는 접촉 자리 + 폭풍 몸이 움직인 양이다. 몸 자리에는 프리팹 보정이 들어 있어 그대로 쓰지 않는다. 숨겨진 폭풍은 접촉 자리에 머문다.
- 그 유닛의 다음 사건은 주체 막기가, 결과 끝은 재생 끝 판정이 착지까지 기다린다. 그래서 착지 사건이 있거나 주체 없는 다른 사건이 끼면 폭풍이 그때 잠깐 기다린다(사건 순서 규칙, 고치지 않음).
- Core 사실은 그대로다. 규칙 판정은 이미 끝난 뒤라 결과는 같고 보이는 시점만 겹친다.

## 11 옛 지형 토네이도 (10-17~10-31, 대신함)

<a id="tornado-visual-only"></a>
<a id="94-재개-요구와-현재-코드-대조--2026-10-27"></a>
<a id="tornado-2x2-visual"></a>
<a id="tornado-presentation-settings"></a>
<a id="tornado-2x2-scope"></a>

11-01 Core 기믹으로 바뀌기 전의 규칙과 View다. 이 View 코드는 TN-f에서 걷었고, 아래는 결과 요약과 지금도 쓰는 자원 사실만 남긴다.

- **옛 규칙(Core, 천명님 영역을 사용자 허용으로 고침, `1ab12e55`):** 지형 토네이도 한 칸(`TornadoSourceFact.Cell`)이 판정이고 외형은 2×2(판정 칸에서 대각선 반 칸 보정). 진영 턴 3번마다 발동하고, 판정 칸에 들어오면 경로 중간이어도 바로 발동. 대상 유닛 하나를 Core 환경 난수로 고른 빈 칸에 옮기고(`Relocated`), 원본도 다른 칸으로 다시 생성. 최초 칸은 스테이지 `timelineSeed`로 한 번. 밤 종료·기차 기믹과 같은 진영 끝이면 토네이도 → 기차 → 밤 순서(Core 전달 문서 §45).
- **옛 View:** 사건 시점 사라짐·다시 생김(TR-1), wrapper 몸 조회(TR-2, `FieldTerrainPresenter.TryGetBody`), 판정 유닛의 자체 회전 → 중심 접근 → 상승 → 이탈 순간 원본 숨김 → 목적지 수직 낙하(`CombatTornadoLiftPiece`), 크기 생성·소멸(`CombatTornadoAppearance`). 처리기는 `CombatCharacterEventHandler`, Factory 창구 넷(민형님 공유 없음).
- **철회된 안:** 실제 네 칸 판정(10-20~10-27, Core 요청 §29 철회). 10-29 대각선 V·W 이동 요청([Core 요청 메모](../handoffs/2026-10-29-tornado-diagonal-move-core-request-Hojin.md), 미전달)은 11-01 Core 기믹이 대각선 이동으로 들어오며 대신했다.
- **지금도 쓰는 자원:** `TerrainVisual_Tornado`가 전투 wrapper `Prefabs/Default/Environment/Terrain_Tornado_Toneido.prefab`을 가리킨다. 비활성 identity 루트 아래 원본 `Toneido`(Render 원본, Apply 안 함)를 nested로 두고, 허용한 덮어쓰기는 nested 균일 배율 .26·`scalingMode` Hierarchy·재질 슬롯 셋·값 조회용 꺼진 BoxCollider(center (0,.62,0), size (1.64,2.04,1.64))뿐이다. Layer2/3/4는 전투 복사 재질(`Prefabs/Default/Environment/Toneido/Materials/`)이고 `_VertexPadding` = 원본 값 × 배율(.0507/.03536/.13). 배율만 줄이면 Layer4 정점 확장이 남아서 넣은 보정이다.
- **남은 사실:** 시험이 끝날 때 Layer2/3/4 재질의 `m_CustomRenderQueue`가 3001~3004 → 2001~2004로 저장되는 현상은 원인 미상이다(매번 되돌림). 크기 방식 등장·사라짐은 비용이 커서 민형님 셰이더 교체를 기다린다(그 전까지 다듬지 않음). 유닛 발끝 Y 정비(B-TR3)는 사용자 보류.
