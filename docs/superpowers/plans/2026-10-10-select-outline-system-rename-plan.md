# 선택 아웃라인 시스템 이름 정리

## 설계와 범위

- 기존 Render `OutlineSystem`은 `_SelectOutline` 머티리얼을 사용하는 오브젝트 선택 외곽선 시스템이다.
- 시스템 이름을 `SelectOutlineSystem`으로 통일한다. 폴더·네임스페이스·클래스·서비스 계약·키·ViewGroup 접근자·Factory·테스트 참조를 함께 변경한다.
- `AreaOutlineSystem`(칸/이동 경로 강조), 머티리얼 키, 런타임 계층 이름, 풀·Scop의 동작은 변경하지 않는다.
- Core, FieldCamera, Architecture와 기존 사용자 변경은 건드리지 않는다. 파일과 폴더의 Unity GUID는 유지한다.
- SSOT: 선택 아웃라인의 등록/조회 이름을 일치시킨다. SOLID: 기존 책임과 계약 형태는 유지한다. FW-001/002: 서비스·모듈 등록과 생명주기를 그대로 유지한다.

## 단일 Phase

1. 시스템 파일 3개와 `.meta`, 폴더 `.meta`를 새 이름/경로로 옮긴다.
2. Factory·서비스 키·ViewGroup·구성 테스트와 개발 가이드 API 표의 참조를 변경한다.
3. 이전 식별자 잔존, GUID 유지, 컴파일을 확인하고 멈춰 보고한다.

## 검증 기록

- 구현 전: 이동 대상과 참조 파일에 기존 사용자 수정이 없음을 확인했다.
- 단일 Phase 코드 완료. 클래스/서비스 이름, 폴더/네임스페이스, 등록/조회, 테스트, 개발 가이드 API 표를 갱신했다.
- 이동한 소스 3개를 HEAD 원본에 이름 치환한 결과와 비교해 이름 외 변경이 없음을 확인했다.
- 파일 3개와 폴더의 `.meta` GUID 4/4 유지 확인. 옛 빈 폴더만 제거했다.
- Assets의 C#·Markdown에서 옛 시스템/서비스/접근자 식별자 참조 0건. 변경 대상 `git diff --check` 통과. 기존 사용자 `FieldOutlineRenderer.cs`의 공백 변경은 그대로 유지했다.
- VS 2022 MSBuild로 `ProjectL.Tests.Framework.csproj` 및 참조 어셈블리 빌드 성공(오류 0). 변경하지 않은 파일에서 경고 10건(Core nullable 1, UI 필드 7, 테스트 필드 2)이 출력됐으며 변경 파일 경고는 없다.
- Unity가 csproj를 다시 생성하기 전 검증을 위해 `Temp/SelectOutlineRename/CompileOverrides.targets`에서 옛 Compile 경로를 새 경로로 교체했다. 생성 csproj는 수정하지 않았고, 빌드 산출물도 같은 임시 폴더에 분리했다.
- Unity Editor 컴파일·EditMode 테스트 실행·선택 아웃라인 재생 확인은 아직 하지 않았다. 여기서 구현 Phase를 멈춘다.
