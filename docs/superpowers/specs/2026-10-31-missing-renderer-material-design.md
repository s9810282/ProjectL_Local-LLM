# Renderer 누락 머티리얼 연결 설계

사용자 요청: 프리팹과 씬에서 Renderer의 None/Missing 슬롯을 기존 색상 머티리얼로 채운다. 텍스처 슬롯 채우기가 아니라 Renderer 재질 참조 복구다.

## 범위와 연결 정책

- `Assets`의 저장된 `.prefab`·`.unity` 전체를 조사한다. Renderer의 직접 배열과 PrefabInstance의 `m_Materials.Array.data[n]` 오버라이드를 모두 검사한다.
- GUID 유효성은 Assets와 설치된 패키지를 함께 확인한다. URP 기본 Lit·TerrainLit·ParticlesUnlit은 정상 연결이다.
- 정상 참조는 유지한다. 재질 속성·셰이더·메시·게임 로직은 변경하지 않는다. 새 재질을 복제하지 않는다.
- 삭제·이름 변경 이력에서 후속 재질을 확정할 수 있으면 그 에셋을 재사용한다. 옛 rockT5·rockT7은 같은 텍스처를 쓰는 `Cartoon_Rock_RockT4`, 마차·울타리·건물·선인장은 해당 기존 색상 재질이다. 옛 BoomEffect의 후속 에셋은 `Brick.mat`다.
- 삭제된 NightShader·LightColor는 원본 FBX의 Renderer ID·슬롯·텍스처와 색상 재질을 대조한다. 두 레일 재질은 슬롯별 텍스처로 구분한다.
- 원본 슬롯의 텍스처가 없으면 같은 모델의 정상 연결, 모델 안에 기록된 텍스처 이름 순으로 대조한다. Bar_ver1/2는 모델의 RGB_texture에 대응하는 건물 재질, rock2/3는 동일 모델의 정상 Rock 재질, 랜턴은 정상 인스턴스의 원본 텍스처에 대응하는 재질을 쓴다.
- 텍스처와 정상 연결이 모두 없는 `ShaderTests/.../test.fbx`의 누락은 기존 단색 `Cartoon_Solid_RockGray`를 사용한다. 이는 원래 야간 효과의 복원이 아니라 사용자 요청에 따른 색상 재질 연결이다.
- `enabled = false` 또는 ParticleSystemRenderer의 `renderMode = None`인 비표시 파티클 슬롯은 의도된 빈 값으로 유지한다. 활성 Trail의 별도 정상 재질도 유지한다.

## 구조 점검

- SSOT: 색·텍스처 설정은 기존 `.mat`, 오브젝트 연결은 해당 Renderer/Prefab 오버라이드만 소유한다. 원본 모델은 선택 근거이며 런타임 동기화 상태를 추가하지 않는다.
- SOLID: 표시 에셋 참조만 수정한다. 별도 서비스·인터페이스·Manager를 추가할 이유가 없다.
- JMH/FW: 서비스 등록·수명·Core/Buffer 경계 변경이 없으므로 FW-001~016의 새 등록은 없다. FieldCamera·Architecture·Core 파일을 수정하지 않는다.
- 독립성: 수정·검증은 재질 연결 및 변경 에셋의 import에 한정한다. 재질 표현 변경은 기존 재질에서, 연결 변경은 프리팹·씬에서 끝난다.

## 보존과 검증

- 변경 전 파일을 `output/material-assignment/backups/`에 보관하고 슬롯별 변경 근거를 기록한다. 실제 쓰기 직전에 조사한 내용과 현재 파일을 비교해 동시 변경을 덮어쓰지 않는다.
- 열린 씬의 dirty 상태를 확인한다. 외부 변경 대상 씬에 미저장 변경이 있으면 해당 씬 파일을 쓰기 전에 해결한다. 다른 씬을 저장하거나 전환하지 않는다.
- 변경은 재질 GUID 참조의 범위로 제한하고, 복구 후 전체 정적 재검사와 변경 에셋의 Unity 로드 검사를 한다. 임시 읽기 전용 Editor 조회 도구는 작업 후 제거한다.
- 초기 Assets 전용 조사 수치는 패키지 기본 재질과 따옴표가 있는 오버라이드를 반영하지 않은 중간값이다. 최종 집계는 전체 검증 보고서를 원본으로 한다.
