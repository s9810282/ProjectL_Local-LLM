# 캐릭터 FBX 클립 복사·Animator 연결 — 2026-11-15

## 설계

- 사용자 요청: `Resources/Characters/New`의 각 FBX 클립을 대응하는 `Prefabs/Characters/New/진영/캐릭터/Animation`으로 복사하고 프리팹 Animator를 연결한다.
- 원본 목록은 FBX 폴더이며, 캐릭터별 기본 Idle 이름만 명시한다. Gravedigger·Sniper도 포함한다.
- 기본 Idle과 말·무기 등의 부속 Idle은 Controller의 별도 레이어에서 함께 재생한다. 공격·피격 상태 전이나 전투 SO 교체는 이번 요청에 추가하지 않는다.
- 기존 Controller는 같은 에셋을 갱신한다. 없으면 캐릭터 폴더에 생성한다. 프리팹에 Animator가 없으면 루트에 추가한다.
- `.anim`은 같은 경로가 있으면 GUID와 조정한 이벤트를 보존한다. 새 동작 클립에는 기존 복사 도구의 시작·중간·끝 이벤트를 적용하고, 새 Idle에는 전투 이벤트를 추가하지 않는다.
- 기존 구형 클립을 삭제하지 않는다. FBX에 클립이 없는 EnemyRecover와 대응 FBX가 없는 EnemyNexus는 새 애니메이션을 만들지 않는다.
- 프리팹의 뼈대 경로와 클립 바인딩이 다르면 해당 캐릭터 연결을 실패로 보고한다. 모델 교체·뼈대 재구성으로 작업을 넓히지 않는다.

## 원칙 점검

- SSOT: 곡선 원본은 각 FBX, 기본 Idle 선택은 에디터 조립 도구의 명시 목록 한 곳이다.
- SOLID·독립성: 기존 클립 복사 기능을 재사용하고 캐릭터별 조립은 별도 에디터 도구로 분리한다. 런타임·Core·DB 규칙을 수정하지 않는다.
- FW 계약: 에디터 에셋 저작 작업이며 서비스 등록·Builder·수명 계약을 추가하지 않는다.

## Phase 1 — 복사·연결

1. 기존 복사 함수에 새 Idle의 기본 이벤트 생성을 생략하는 선택 인자를 추가한다.
2. 전체 FBX의 클립 복사와 프리팹·Controller 연결 도구를 만든다.
3. Unity 배치 에디터를 에셋 저작 용도로 실행하고 캐릭터별 결과를 기록한다.
4. 파일·GUID·Controller·프리팹 참조를 정적으로 대조하고 결과를 보고한 뒤 멈춘다.

Test 코드·MSBuild·재생 검증은 실행하지 않는다. 컴파일·재생 확인은 사용자 Unity 확인 항목으로 남긴다.

## 실행 기록

- 준비: FBX 14개, 이 중 EnemyRecover를 제외한 13개에서 총 66개 테이크를 확인했다.
- Phase 1 결과: Unity 메뉴에서 클립 66개 복사 완료, 캐릭터 11개 연결 완료. EnemyRecover는 원본 클립 없음. EnemyShabow는 `Horse_Bone`, Boomber는 `아마츄어` 경로가 기존 프리팹에 없어 연결을 중단했다.
- 2026-11-15 사용자 승인: “두 캐릭터 모델 교체 후 연결”.

## Phase 2 — 승인된 두 캐릭터의 모델 교체

1. EnemyShabow·Boomber만 대상으로 한다. 기존 프리팹 파일과 meta를 Temp에 보관한다.
2. 기존 루트·Animator·Collider·전투 스크립트와 프리팹 GUID를 유지하고, Animator 아래 모델 자식을 새 FBX의 모델 자식으로 교체한다.
3. 기존 모델 자식에 스크립트나 전투 스크립트의 참조가 있으면 중단한다. 무작정 삭제·유실하지 않는다.
4. 새 FBX 클립 경로를 확인하고 캐릭터별 Controller를 연결한다.
5. 저장된 파일의 루트·컴포넌트 식별자와 GUID·클립 참조를 대조하고 보고한다. 컴파일·재생 검증은 사용자 확인으로 남긴다.

## Phase 3 — 모델 변경에 따른 공격 SO 연결

- 2026-11-15 사용자 승인: “두 캐릭터의 공격 SO도 새 클립으로 연결”. 모델 교체 뒤 Boomber의 기존 공격 경로 `BaseHumanBone`이 새 모델의 `아마츄어/BaseHumanBone`과 달라 이 추가 결정을 확인했다.
- `CombatUnitAnimation_Sabow`: 몸 = `Enemy_Horse_Shovel_Attack`, 부속 = `Enemy_Horse_Attack`.
- `CombatUnitAnimation_Bommber`: 몸 = `Horse_Player_Boomber_Attack`, 부속 = `Horse_Player_Attack`.
- 부속의 이벤트는 비운다. 시점의 원본은 몸 클립 하나이며 기존 `RequireCompanionClips` 계약을 따른다. 길이·루프·곡선 겹침을 정적으로 확인한다.

## 최종 결과 — 2026-11-15

- Phase 1~3 에셋 저작 완료. 열린 Unity의 메뉴에서 실행했다(동일 프로젝트의 배치 실행은 열린 에디터 잠금 때문에 거부됨).
- FBX 14개 중 애니메이션이 있는 13캐릭터의 66클립을 복사하고 Controller 13개를 연결했다. Gravedigger 4클립·Sniper 3클립도 포함한다. EnemyRecover는 원본에 클립이 없다.
- 몸·부속 Idle 20클립을 루프로 설정하고 Controller에 연결했다. 공격 상태 전이는 추가하지 않았다.
- FBX 테이크에 다른 부분의 정지 곡선이 함께 들어 있어 `CharacterAnimationParts`가 곡선 소유를 분리한다. 움직이는 부분을 소유자로 두고, 정지 곡선 중복을 제거하며 서로 다른 움직임이 충돌하면 실패한다. 쿼터니언·벡터 성분은 반드시 같은 소유자에게 함께 둔다.
- EnemyShabow는 같은 아마추어에 말·기수가 합쳐져 있어, 실제 계층의 `Horse_Bone/뼈대.005/CTRL_Body/BaseHumanBone` 하위를 기수 소유로 명시했다.
- EnemyShabow·Boomber 모델 교체 전후 프리팹 GUID·Animator·Collider·전투 스크립트 식별자 보존을 대조했다. 원본 파일은 `Temp/character-model-backups`에 보관했다.
- 승인된 공격 SO 2개는 새 몸·말 공격 클립을 사용한다. 기존 전체 클립 묶음의 부속 6개 관계에서 이벤트 없음·루프 꺼짐·곡선 겹침 없음을 파일로 대조했다.
- 66클립의 파일·GUID·이벤트 범위, 13프리팹의 Controller·Idle 참조를 대조했다. 결과는 `Temp/character-animation-setup-report.json`에 있다.
- Test 코드 작성·MSBuild·재생 검증은 하지 않았다. **컴파일 미확인.** 사용자 확인: Idle·공격·말/무기 동시 재생, 두 교체 모델의 외형·피격/래그돌 연결, `DataBaseAssetTests`.

## Phase 4 — 말 접지 높이·BaseShader 머티리얼 (2026-11-15 사용자 요청)

- EnemyShabow·Boomber의 남은 모델 높이 보정을 새 모델의 Idle 0초 메시 최저점으로 확인하고 지면에 맞춘다. 클립 곡선은 바꾸지 않는다.
- New 캐릭터의 MeshRenderer·SkinnedMeshRenderer 슬롯을 BaseShader 안의 머티리얼과 텍스처 기준으로 대조해 연결한다. ParticleSystemRenderer 등의 특수 VFX는 대상으로 삼지 않는다.
- 셰이더·공유 머티리얼 원본을 임의 수정하지 않는다. 대응하는 머티리얼이 없으면 대상과 텍스처를 확인해 보고한다.
- 높이 측정·머티리얼 조사 → 기존 머티리얼 매핑 → 프리팹 저장 → GUID·참조·저장값 대조 순서다. Test·MSBuild·Play 검증은 실행하지 않는다.

### Phase 4 결과

- Unity 메뉴 적용 완료. 7프리팹의 14머티리얼 슬롯을 교체했고, 기존 19슬롯을 포함해 33슬롯 모두 BaseShader 하위 머티리얼을 참조한다. 기본 텍스처가 정확히 같은 기존 머티리얼을 선택했다. 셰이더·공유 머티리얼 에셋은 수정하지 않았다.
- EnemyShabow: 모델 Local Y `0.73 → -0.005826473`, Idle 0초 메시 최저점 `0.73582649 → 약 0`.
- Boomber: 모델 Local Y `0.463 → 0.005741599`, Idle 0초 메시 최저점 `0.45725837 → 약 0`.
- 측정은 임시 프리팹 내용에서 Idle 0초를 샘플링하고 스키닝된 메시의 정점을 루트 좌표로 변환해 수행했다. 측정 뒤 모든 Transform을 복원하고 모델 부모의 Y 보정만 저장했다. 클립 곡선은 수정하지 않았다.
- 7프리팹의 GUID·Animator·Collider·전투 스크립트 식별자 보존을 대조했다. 이전 파일은 `Temp/character-appearance-backups`에 있다. 결과는 `Temp/character-appearance-applied.json`·`Temp/character-appearance-report.json`.
- **컴파일 미확인.** 사용자 Unity 확인: 재생 중 접지·외형·선택 Collider 위치. Test 코드·MSBuild·Play 검증은 실행하지 않았다.
