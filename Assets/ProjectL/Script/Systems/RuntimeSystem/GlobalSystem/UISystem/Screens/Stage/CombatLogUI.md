# CombatLogUI 연결

`develop-4week`의 `CombatLog` 표시 형식을 현재 서비스/버퍼 구조로 옮긴 유지 UI입니다.
최상단 고정 영역에는 최신 공격 로그 한 줄을 표시합니다. 토글로 펼치는 별도 Scroll View에는 최신 로그를 제외한 이전 기록을 최신순으로 표시합니다.
최신 로그 영역은 처음에는 비활성화하며 첫 표시 대상 로그가 들어오면 활성화합니다. 기록을 초기화하면 다시 숨깁니다.

## Hierarchy

```text
Canvas
└─ CombatLog (RectTransform, CombatLogUI)
   ├─ Toggle (Button)
   ├─ Latest (RectTransform, 최신 행이 생성되는 고정 영역)
   └─ History (RectTransform, ScrollRect, 기본 비활성)
      ├─ Scrollbar (Scrollbar, 선택 사항)
      └─ Viewport (RectTransform, RectMask2D)
         └─ Content (RectTransform)
            └─ Log (RectTransform, 비활성 템플릿)
               ├─ Origin (Image)
               ├─ Action (Image)
               ├─ Target (Image)
               ├─ DMG/HEAL (TMP_Text)
               └─ Dead (Image)
```

- Latest Log Root에는 고정 영역 `Latest`, Scroll View에는 별도 자식 `History`의 ScrollRect를 연결합니다. Latest는 History 바깥에 배치합니다. 기존처럼 CombatLog 루트에 ScrollRect를 붙이는 구조에서 변경되었습니다.
- History의 Viewport와 Content를 연결합니다. Content 아래 `Log` 대신 외부 `Log Prefab`도 연결할 수 있습니다.
- `Log.prefab`의 `Origin / Action / Target / DMG/HEAL / Dead` 요소를 사용합니다. `DMG/HEAL`은 Log 바로 아래 자식의 이름입니다.
- Log는 고정 높이로 디자인하고 Latest 영역에도 동일한 높이를 지정합니다. 새 로그가 들어오면 기존 최신 행을 Content 맨 위로 이동합니다.
- History는 Latest 아래에 배치하고 크기는 Inspector에서 설정합니다. 코드에서는 History와 Viewport의 크기를 변경하지 않습니다. 접어도 Latest와 Toggle은 계속 표시됩니다.
- Toggle Button 하나를 연결합니다. 같은 버튼으로 펼치고 접습니다. 버튼의 클릭 영역은 Viewport 위에서 입력을 가로막지 않게 배치합니다. 입력은 UIInputBinding이 직접 전달하므로 Inspector OnClick에는 등록하지 않습니다.
- 토글은 History의 활성/비활성만 전환합니다. Vertical Scrollbar를 ScrollRect에 연결하면 접었을 때 함께 숨겨집니다. Horizontal Scrollbar는 연결하지 않습니다. Content는 기록 수에 맞춰 길이가 늘어납니다.
- Unit Icons에 EntityKey별 초상화를, Attack Icon과 Death Icon에 행동/사망 아이콘을 연결합니다. 아이콘이 없으면 이름·ID, `>`, `X`로 대체합니다.
- 현재 UISystem의 씬 UI 수집/서비스 바인딩 대상으로 등록합니다. `UIId.CombatLog`와 `GlobalUIBase`를 사용합니다.

## 기록 수명

CombatStateBuffer는 명령과 자동 진행의 결과를 읽기 전용 이력으로 보관합니다. UI가 숨겨져 있어도 결과가 누락되지 않습니다.
UI에는 수락된 공격의 대상별 실제 피해량을 표시하며, 미리보기·거절·공격 이외의 사건은 행으로 만들지 않습니다.
동일 공격이 Attack 필드와 CombatAttackEvent 양쪽에 있어도 한 번만 표시합니다.
전투 종료 후에는 기록을 유지하고, 다음 전투 준비 시 버퍼 이력과 UI 행을 초기화합니다.
`ClearLogs()`는 현재까지의 기록을 지우고 이후 결과부터 표시합니다.
