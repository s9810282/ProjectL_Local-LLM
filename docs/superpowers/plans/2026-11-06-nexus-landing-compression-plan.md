# 적 Nexus 착지 때 건물·바닥 눌림 — 설계·Task 계획 (2026-11-06)

사용자 요청: 하늘에서 떨어진 건물이 지면에 닿으면 건물과 그 아래 타일이 잠깐 내려갔다가 올라온다. 기존 대상인 전투 진입 적 Nexus에 적용한다.

## 설계

- 기존 낙하가 지면에 닿는 순간 FallingRocks를 한 번 재생하고, Nexus와 점유 칸 아래의 실제 바닥 타일을 같은 월드 높이만큼 내렸다가 원래 위치로 복구한다. 이펙트 받침도 접촉면 높이를 따른다. 복구 전까지 Nexus 자세 대여와 HUD 숨김을 유지한다.
- 같은 적 Nexus 수집에서 공개된 점유 칸을 함께 얻는다(StructureCombatFacts 계약상 필수). FieldTerrainPresenter가 이미 가진 칸↔타일 대응으로 바닥과 그 타일 위 지형/대체 지형을 수집한다. 여러 Nexus/여러 칸이 같은 타일을 가리키면 한 번만 움직이며, 부모가 움직이는 자식은 중복 변위를 주지 않는다. 바닥 타일 없는 칸도 정상이며 있는 지형과 건물만 움직인다.
- 완성 바닥 자세는 보드·선로·일반 구조물 단계가 끝난 Nexus Begin에서 저장한다. 로딩 중 Board.Prepare가 내린 위치를 저장하지 않는다. 눌림은 필드 위쪽의 음의 방향이며 높이 표·Buffer·실제 점유/판정을 바꾸지 않는다.
- 기존 Nexus Drop 설정에 Impact Depth=0.35(월드 단위), Impact Down Seconds=0.08, Impact Return Seconds=0.22를 추가한다. 모두 유한한 0 이상이다. 깊이 0 또는 두 시간 모두 0은 눌림을 건너뛴다. 내려갈 때 빠르게 감속하고 복귀는 부드럽게 원래 높이에 닿는다. 새 진행 타이머 없이 기존 Nexus 경과 시간에서 착지 후 시간을 계산한다.
- 착지 이펙트 대기와 눌림은 동시에 진행한다. 다음 적 등장은 눌림 복구와 기존 이펙트 완료/3초 대기 상한을 모두 만족한 뒤 시작한다. 기존 전체 진입 순서(보드→선로→일반 구조물→Object 공개→Nexus→적), 낙하 비활성의 일반 구조물 상승, 입력/자동 진행 대기를 유지한다.
- 정상 완료·중간 취소·오류·재진입에서 건물 자세와 바닥 위치를 복구한다. 새 GroundCompression 부품은 수집된 Transform의 자세·공통 눌림 곡선만 관리하고, Nexus 부품이 건물·이펙트와 동일 변위를 적용한다. Factory가 부품을 조립한다.

SSOT: 대상은 기존 Nexus 수집/칸↔타일 표, 수치는 Nexus Drop SO, 시간은 기존 Nexus 부품, 복구 자세는 Begin의 스냅숏이다. S/O: 바닥 수집·눌림 실행·단계 조정·Factory 조립을 분리한다. L: 기존 낙하/효과 상한/취소 및 진입 완료 계약 유지. I: 좁은 Transform 목록과 설정만 전달한다. D: Factory가 실행 부품을 주입하며 새 전역 조회가 없다. FW/JMH: 기존 Unit 자세·Effect 창구를 사용하고 새 서비스/등록/Export/MonoBehaviour/코루틴은 만들지 않는다. 기능 독립성: 눌림 변경은 전용 부품/설정에, 대상 변경은 기존 수집 경계에 머문다. Core·FieldCamera·Architecture·다른 진입 연출 내부는 수정하지 않는다.

## Phase NI1

- [x] NI1.1 설정·검증·운영 에셋에 눌림 깊이/내려가기/복귀 시간 추가.
- [x] NI1.2 선택한 Nexus 점유 칸과 바닥/지형 수집 연결.
- [x] NI1.3 눌림 실행/.meta와 낙하·이펙트·HUD·완료/취소 연결.
- [x] NI1.4 호출·완료/취소·공유 타일·0값·설계 규약 정적 검토 및 인수 기록.

검증: Test 코드 작성/실행·MSBuild·Unity 실행 없음. **컴파일 미확인**으로 보고한다. Unity 인수는 착지 순간 건물/바닥/지형 동반 눌림, 접촉면 FallingRocks, 원위치 복귀 뒤 적 등장, 다칸/여러 Nexus/공유 타일/교체 지형, 깊이 및 시간 0, Nexus Drop/Board Reveal 끄기, 중간 취소·재진입이다. 설정 에셋 변경으로 DataBaseAssetTests도 사용자 확인 항목에 포함한다.

## NI1 결과

기존 Nexus 수집 함수가 같은 선택에서 점유 칸을 함께 돌려주고, BoardEntry 준비가 해당 칸의 타일/지형 목록을 Nexus에 전달한다. 수집은 기존 FieldTileReplacement의 칸↔타일 대응을 재사용한다. 중복 Transform과 부모·자식 중복 변위를 제거하고, 새 CombatEntryGroundCompression이 Nexus Begin에서만 완성 localPosition과 월드 위쪽의 부모 기준 축을 저장한다.

Nexus의 기존 경과 시간에서 낙하 시간을 뺀 값을 눌림 부품에 전달한다. 반환한 단일 높이 변위를 타일·지형, 빌린 건물 자세, FallingRocks 받침에 같이 적용한다. 복구가 끝나면 건물 자세를 반환하고 HUD를 공개한다. 다음 적 등장은 눌림 복구와 기존 이펙트 완료/상한을 모두 기다리므로 두 연출의 시간이 순차 누적되지 않는다. 이미 복구된 바닥 스냅숏은 비워 재사용 중인 외형에 늦은 복구가 쓰이지 않게 했다.

정적 검토: Factory 한 곳의 생성과 Prepare/수집 호출을 대조했다. 지면 스냅숏이 Board.Prepare 이후의 내려간 상태를 저장하지 않는지, 낙하 비활성·대상 없음·타일 없음·공유 타일·교체 지형·깊이 0·한쪽/양쪽 시간 0에서 완료/나눗셈 조건을 검토했다. 새 부품 Cancel이 건물 자세/효과/표시 정리와 함께 실행되고, 기존 SceneDisplay의 진입 취소가 지형/유닛 해제보다 앞서는 것을 확인했다. 값 검증은 기존 RequireNonNegative를 사용하며 합산 시간의 넘침도 거절한다. 새 .meta GUID, UTF-8/LF와 diff 공백을 확인했다.

SSOT/SOLID/FW·JMH/기능 독립성은 위 설계를 유지한다. Core·높이 표·서비스 등록/Export·다른 진입 연출 구현 변경 없음. 같은 FieldUnitPresenter에 병행 중인 Unit Action Cursor 변경은 이번 작업과 별개이며 보존했다. 새 시험·기존 시험 수정·MSBuild·Unity 실행 없음. **컴파일 미확인**, Unity 재생 및 DataBaseAssetTests는 사용자 인수 대기다.

조정 위치: `CombatPresentationSettings_Default` → Combat Presentation Settings Data → 전투 진입 적 Nexus 낙하 → Nexus Drop → 착지 때 건물·바닥 눌림. Impact Depth=0.35, Impact Down Seconds=0.08, Impact Return Seconds=0.22. 깊이 0이면 기존 착지로 돌아간다. 기존 Height=30과 Seconds=0.8은 유지했다.
