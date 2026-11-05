# 1b155e0 오디오·철도 View 병합 기록

- 날짜: 2026-10-13
- 현재 브랜치: `feature/Yonggeun/Stage`, 병합 전 HEAD `d16e95d0`
- 대상: `1b155e02bab820cd3b60a8bb5e59033515ec5bdc` (`[현강] 머지 및 오류수정`)
- 사용자 승인: 철도·이펙트·오디오 인자를 합쳐 병합. 커밋·push는 사용자가 수행한다.
- `git merge --no-commit --no-ff 1b155e0`으로 적용했다. 이 기록 시점에는 병합 커밋을 만들지 않았다.

## 해결 내용

- 충돌 2개: `CombatSceneDisplay.cs`, `CombatViewSystem.cs`.
- 표시 생성자의 마지막 인자를 `RailwayVisualData railwayConfig, IEffectSystemServices effects, IAudioSystemService audio = null`로 합쳤다. 운영 호출부는 `_railwayConfig, _effects, _audio`를 모두 전달한다.
- 기존 철도 Install/Begin/Tick/Cancel/Clear/Release와 이펙트 전달을 유지한다. 대상 커밋의 AudioSystem 서비스 전달, 입력 효과음, 사건 효과음·BGM 및 이동 시작 알림도 반영한다.
- 오디오 선택 인자는 대상 커밋의 계약을 유지한다. 기존 테스트 호출은 철도·이펙트 인자를 그대로 전달할 수 있다.
- SSOT/SOLID: 철도는 기존 Presenter, 오디오는 서비스 인터페이스를 통한 기존 사건·입력 경로에 남긴다. 규칙 계산·상태 원본을 추가하지 않는다. FW 계약: 기존 서비스 조립과 철도 자원 반환 경로 유지.
- 게임 Core·LifeCycle 파일에는 병합 변경이 없다.

## 검증

- `ProjectL.Tests.Framework.csproj` 및 참조 시스템 어셈블리 MSBuild 성공, 오류 0. 경고는 변경하지 않은 Core/UI/테스트 파일에서 출력됐다.
- Unity 생성 csproj를 수정하지 않고 `Temp/AudioRailwayMerge/CompileOverrides.targets`로 새 소스 5개를 누락 없이 포함했다.
- 관리 코드 회귀 109건 통과: 필드 강조 상태, 전투 Plan, 포기/개발 패널, 철도 레이아웃, 전투 입력 및 오디오 프리셋. NUnit assertion을 직접 실행했으며 Unity Test Runner 결과는 아니다.
- 신규 소스·테스트 `.meta` 5개의 GUID 고유성과 병합 C# 공백 검사를 확인했다.
- 증거: `Temp/AudioRailwayMerge/build.log`, `Temp/AudioRailwayMerge/managed-regression.log`.
- Unity EditMode 및 실제 철도·UI/이동/공격 효과음·BGM 재생은 확인 대기다. 열려 있는 Unity를 종료하거나 잠금 파일을 삭제하지 않았다.
