# Structure 피격·선택 View 공유 메모

- 2026-11-01 작성. **미전달**. 코드/에셋 연결 완료, Unity Test Runner·Play 인수 미확인.
- [설계](../specs/2026-10-30-combat-structure-hit-design.md), [Task와 검증 증거](../plans/2026-10-30-combat-structure-hit-plan.md), [코드 리뷰](../specs/2026-11-01-combat-structure-hit-implementation-review.md).

- 일반 적 Structure도 기존 공격 해제 순간 HP·숫자·StructureHit 큐를 표시한다. 네이티브 장애물의 기존 피격/파괴 큐는 유지한다.
- UnitVisual에 `structureHitCues` 추가: 이펙트/Start/SubjectBody/지연 0. Headquarters에 기존 Hit를 연결했다. 파괴 뒤에도 남도록 해당 에셋은 stayInPlace=true다.
- HUD.prefab의 기존 UnitDamageText 컴포넌트를 DamageNumbers 자식으로 옮겼다. 기존 컴포넌트/TMP fileID와 연출 설정은 유지했다. 캐릭터 숫자의 기존 2슬롯 동작을 반드시 회귀 확인한다.
- DeckUI.prefab에 선택 이름과 Structure 설명/진영 텍스트를 연결했다. 일반 적 Structure 점유 칸 클릭은 정보 선택이고 캐릭터 명령 행위자가 되지 않는다. 실제 화면의 겹침/잘림·패널 전환은 미확인이다.
- 공용 UI/Render 서비스를 추가하거나 루트에 재노출하지 않았다. Core 요청/수정 없음.
- 남은 확인: Unity EditMode의 `StructureHitPresentationTests`, 기존 HUD/카메라 투영 회귀, 근접/원거리 생존·치명타·반복 피격·선택 교체·전투 취소/씬 재진입.
