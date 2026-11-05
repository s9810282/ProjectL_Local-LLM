using System.Linq;
using UnityEditor;
using UnityEngine;

namespace ProjectL.Tool.Editor
{
    /// <summary>Shader Composer의 조작 순서와 지원 범위를 설명하는 읽기 전용 창이다.</summary>
    public sealed class ShaderComposerHelpWindow : EditorWindow
    {
        private sealed class Section
        {
            internal readonly string Heading;
            internal readonly string Text;

            internal Section(string heading, string text)
            {
                Heading = heading;
                Text = text;
            }
        }

        private sealed class Page
        {
            internal readonly string Title;
            internal readonly Section[] Sections;

            internal Page(string title, params Section[] sections)
            {
                Title = title;
                Sections = sections;
            }
        }

        [SerializeField] private int selectedPage;
        private Vector2 _scroll;
        private Page[] _pages;
        private string[] _titles;
        private GUIStyle _bodyStyle;
        private GUIStyle _headingStyle;
        private GUIStyle _menuStyle;

        [MenuItem("ProjectL/Shader Composer Help")]
        public static void Open()
        {
            GetWindow<ShaderComposerHelpWindow>().Show();
        }

        private void OnEnable()
        {
            titleContent = new GUIContent("Shader Composer 사용 설명서");
            minSize = new Vector2(740, 520);
            _pages = CreatePages();
            _titles = _pages.Select(page => page.Title).ToArray();
            selectedPage = Mathf.Clamp(selectedPage, 0, _pages.Length - 1);
        }

        private void OnGUI()
        {
            if (_bodyStyle == null)
            {
                _bodyStyle = new GUIStyle(EditorStyles.wordWrappedLabel) { fontSize = 13 };
                _headingStyle = new GUIStyle(EditorStyles.boldLabel) { wordWrap = true, fontSize = 14 };
                _menuStyle = new GUIStyle(EditorStyles.miniButton) { alignment = TextAnchor.MiddleLeft, fixedHeight = 32 };
            }
            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                GUILayout.Label("Shader Composer · 사용 설명서", EditorStyles.miniLabel);
                GUILayout.FlexibleSpace();
            }
            using (new EditorGUILayout.HorizontalScope())
            {
                using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox, GUILayout.Width(150)))
                {
                    EditorGUILayout.LabelField("목차", EditorStyles.boldLabel);
                    int next = GUILayout.SelectionGrid(selectedPage, _titles, 1, _menuStyle);
                    if (next != selectedPage)
                    {
                        selectedPage = next;
                        _scroll = Vector2.zero;
                    }
                }
                using (new EditorGUILayout.VerticalScope())
                {
                    _scroll = EditorGUILayout.BeginScrollView(_scroll);
                    EditorGUILayout.Space(10);
                    EditorGUILayout.LabelField(_pages[selectedPage].Title, EditorStyles.largeLabel);
                    foreach (Section section in _pages[selectedPage].Sections)
                    {
                        EditorGUILayout.Space(14);
                        GUILayout.Label(section.Heading, _headingStyle);
                        EditorGUILayout.Space(4);
                        GUILayout.Label(section.Text, _bodyStyle);
                    }
                    EditorGUILayout.Space(20);
                    EditorGUILayout.EndScrollView();
                }
            }
        }

        private static string Labels(ShaderComposerModule[] modules)
        {
            return string.Join(" / ", modules.Select(module => module.Label));
        }

        private static Page[] CreatePages()
        {
            return new[]
            {
                new Page("시작하기",
                    new Section("1. 조립 창 열기", "ProjectL > Shader Composer를 엽니다. Shader 이름은 프로젝트 안에서 다른 Shader와 겹치지 않게 정하세요. 예: ProjectL/Generated/MyCharacter"),
                    new Section("2. 표면과 조명 정하기", "라이트 모델에서 Toon 등의 모델을 고르고, 기본 표면의 기본색·기본 텍스처·타일링·이동을 정합니다. 텍스처를 비우면 흰색 텍스처를 기준으로 표시합니다."),
                    new Section("3. 효과 넣기", "+ 효과 추가를 누르고 효과와 마스크를 고릅니다. 모델들을 혼합한 결과에 효과가 위에서 아래 순서로 적용됩니다. 마스크가 없음이면 전체 표면에 적용됩니다."),
                    new Section("4. 모델에 대조하기", "Project의 FBX를 FBX 모델 필드에 넣습니다. 비워 두면 기본 도형을 사용합니다. FBX 미리보기는 드래그로 회전하고 휠로 확대·축소합니다."),
                    new Section("5. 저장하고 사용하기", "Shader 파일로 내보내기로 Assets 아래에 저장합니다. 머티리얼도 함께 만들기를 켜면 현재 설정의 새 Material도 생성합니다. 생성한 Material을 실제 오브젝트의 Renderer에 할당하세요."),
                    new Section("빠른 예제: 빛을 받는 부분만 발광", "효과를 발광, 마스크를 빛 (실시간 조명)으로 정합니다. 발광 색과 강도를 고르고, 기준 밝기와 경계 부드러움을 조절합니다. 어두운 영역에 표시하려면 반전을 켭니다.")),
                new Page("라이트 모델",
                    new Section("사용 가능한 모델", Labels(ShaderComposerCatalog.Models)),
                    new Section("기존 BaseShader 열기", "조립한 Shader 열기로 View/NotUse/BaseShader/Base/BaseShader.shader를 선택하면 Base Lit (기존 베이스) 모델과 기존 프로퍼티 연결을 읽습니다. 평면 명암 노멀, 반사광, 빛을 받는 림을 모델에서 조절합니다. 입력에 사용자 프로퍼티가 연결되어 있으면 프로퍼티 창에서 기본값을 편집하세요. _Color·_MainTex 등 이름을 유지하면 기존 Material과 런타임 설정을 계속 사용할 수 있습니다. 파일 안의 Recipe는 Shader 기본값이며 개별 Material에서 바꾼 값은 별도입니다."),
                    new Section("모델별 용도", "Lit은 금속성과 매끄러움을 사용하는 PBR 조명입니다. Toon은 명암 단계·어두운 밝기·경계 부드러움·노멀 라이팅 배율을 조절합니다. Unlit은 조명 계산 없이 기본 표면을 표시합니다. 하프램버트는 밝고 어두운 면의 전환을 부드럽게 만드는 확산 조명입니다."),
                    new Section("하프램버트", "명암 곡선의 값이 클수록 밝은 영역이 좁아집니다. 기본 곡선은 2입니다. 환경광 강도는 간접광의 비중을 정합니다. 반사광·금속성 표현은 Lit이나 Toon과 혼합해서 만들 수 있습니다."),
                    new Section("Half Lambert Depth Palette", "노멀 스무스 0은 월드 위치 미분으로 구한 삼각형 면 노멀, 1은 메시의 보간된 노멀로 하프램버트를 계산하며 중간값으로 부드러움을 조절합니다. 기본값은 0입니다. 라이트맵이 없는 환경광도 같은 노멀로 평가합니다. 깊이 보정의 접평면은 항상 삼각형 면 노멀입니다. 하드 노멀 메시에서는 스무스를 올려도 경계가 남을 수 있습니다. Step 색 경계와 메시 실루엣은 유지됩니다. 주변 8방향의 화면 깊이에서 홈과 돌출부를 찾아 하프램버트 밝기를 보정한 뒤 가장 가까운 Step 위치의 색을 칠합니다. 경사진 평면 자체의 기울기는 제거하며 배경·화면 밖·최대 비교 거리 밖의 표면은 제외합니다. 기본 Step은 5이며 0·0.25·0.5·0.75·1에 각각 지정한 색을 선택합니다. Step을 늘리면 5개 기준색 사이에서 더 촘촘하게 선택합니다. 반경은 픽셀 단위, 최대 비교 거리는 월드 단위입니다. 깊이 보정 강도 0으로 보정 유무를 비교하세요. 입력 기본색·텍스처도 팔레트에 곱해지므로 처음에는 흰색으로 확인하세요. 불투명 표면을 그리기 전에 현재 카메라의 깊이가 필요합니다. 현재 PC 렌더러의 불투명 이전 SSAO는 깊이 prepass를 요구합니다. Preview도 사용하는 렌더러 설정에 따라 깊이 시점이 달라집니다. 다른 렌더러에서 Depth Texture 옵션만 켠 경우에는 깊이 복사가 불투명 뒤일 수 있습니다. 유효 깊이가 없으면 보정 없이 표시합니다. 화면 밖이나 가려진 면은 복구하지 않으며 카메라·해상도에 따라 결과가 달라질 수 있습니다."),
                    new Section("PBR Step (결과 밝기 단계)", "삼각형 면 노멀로 PBR을 계산한 뒤 결과 RGB의 선형 밝기만 가장 가까운 단계로 맞춥니다. RGB 비율을 유지하며, 반사광·환경광·그림자가 반영된 PBR 결과에 적용합니다. 기본 단계 수는 6개(2~20), 기준 밝기 범위는 1입니다. 간격은 기준 범위를 단계 수-1로 나눈 값입니다. 기본 설정에서 밝기 0.63은 0.6이 되며 HDR 밝기 2.63은 2.6으로 이어집니다. 기준 범위는 최대 밝기를 제한하지 않습니다. 범위를 늘리면 단계 간격이 커지고 줄이면 촘촘해집니다. 금속성과 매끄러움을 조절할 수 있습니다. 이 모델 결과를 만든 뒤 모델 혼합·효과·안개·카메라 톤매핑이 적용됩니다."),
                    new Section("Step Lit (인접 조명색)", "삼각형 면 노멀로 계산한 일반 라이팅의 내적값 N·L과 가장 가까운 조명 표본을 고른 뒤 그 값으로 확산광 색을 계산합니다. 정점 보간 노멀을 그대로 쓰지 않고 월드 위치 미분으로 면 노멀을 구합니다. 표본은 0~1에 균등 배치되며 기본 6개, 2~20개를 지정합니다. 6개라면 0, 0.2, 0.4, 0.6, 0.8, 1에서 고르므로 내적 0.63은 0.6의 조명색이 됩니다. 정중앙은 큰 표본을 고릅니다. 광원별 색·강도·거리 감쇠·그림자를 적용한 뒤 합산하고 환경광을 더합니다. 라이트맵이 없는 환경광도 면 노멀로 평가합니다. 혼합하는 다른 라이트 모델의 노멀은 변경하지 않습니다. 별도 팔레트를 입력하거나 최종 RGB를 각각 단계화하지 않습니다."),
                    new Section("여러 모델 혼합", "+ 라이트 모델 추가로 최대 " + ShaderComposerCatalog.MaximumLightingLayers + "개를 사용합니다. 같은 종류도 여러 번 추가할 수 있습니다. 각 카드의 값은 독립적이며 체크를 끈 모델은 생성에서 제외합니다. 모델은 하나 이상 활성화해야 합니다."),
                    new Section("혼합 가중치", "가중치는 비율입니다. Toon 1 + 하프램버트 1은 각각 50%, 0.25 + 0.75는 각각 25%와 75%입니다. 모든 가중치가 0이면 혼합 결과는 검정이며 그 뒤의 발광 등 효과는 계속 적용됩니다. 모델의 나열 순서는 혼합 비율에 영향을 주지 않습니다.")),
                new Page("효과·마스크",
                    new Section("효과 목록", Labels(ShaderComposerCatalog.Effects) + "\n효과는 최대 " + ShaderComposerCatalog.MaximumLayers + "개입니다. 위·아래 버튼으로 적용 순서를 바꾸고 체크를 끄면 제외합니다."),
                    new Section("효과의 의미", "색상 곱하기는 현재 결과에 색을 곱합니다. 발광은 색을 더합니다. 림 발광은 시선과 표면 방향으로 가장자리를 밝힙니다. 디졸브는 마스크 값과 디졸브 양을 비교해 표면을 지우고 경계에 색을 표시합니다. Bloom 후광은 씬의 후처리 설정이 별도로 필요합니다."),
                    new Section("마스크 목록", Labels(ShaderComposerCatalog.Masks)),
                    new Section("텍스처·노이즈", "모델의 UV를 사용합니다. 텍스처는 읽을 채널 R/G/B/A와 타일링·이동을 정합니다. 빈 텍스처는 흰색이므로 무늬가 필요하면 텍스처를 지정하세요. 노이즈는 크기·흐르는 속도·기준값·부드러움을 조절합니다."),
                    new Section("높이·밴드", "오브젝트 로컬 좌표의 X/Y/Z 중 하나를 선택합니다. 높이는 시작 높이에서 끝 높이로 갈수록 마스크 값이 커집니다. 밴드는 중심에 불투명한 구간을 두고 양쪽으로 흐려집니다. 중앙 두께는 전체 폭이며 아래·위 페이드는 각각 바깥 구간의 폭입니다."),
                    new Section("라인 (UV)", "모드에서 Repeat(반복 줄무늬)와 Single(직선 한 줄)을 고릅니다. 원본 UV의 중심 (0.5, 0.5)을 기준으로 0도는 수평, 90도는 수직이며 선에 수직인 방향으로 위치를 이동합니다. 선 폭은 전체 폭, 반복 중심 간격은 선 중심 사이 거리입니다. 간격은 한 줄 모드에서 사용하지 않습니다. 폭·이동·간격·경계 부드러움은 UV 단위이며, 모델의 UV 배치에 따라 이음새나 늘어남이 보일 수 있습니다. 폭 0은 효과를 숨기고, 반복 폭이 간격 이상이면 전체에 적용합니다. 경계 부드러움 0에도 약 1픽셀의 가장자리 완화가 적용됩니다. 반전·사용자 프로퍼티 연결·디졸브에도 사용할 수 있습니다."),
                    new Section("빛 마스크", "메인광과 추가 조명의 밝기·방향·거리·그림자를 반영합니다. 기준 밝기를 높이면 더 강한 빛을 받는 부분만 남고, 경계 부드러움은 표시 전환 폭입니다. 환경광·반사광·베이크 GI는 포함하지 않습니다. 추가 조명과 그림자는 실제 URP 설정에서 제공되는 범위만 반영합니다."),
                    new Section("반전과 디졸브 제한", "반전은 마스크의 밝고 어두운 영역을 뒤집습니다. 빛 마스크는 디졸브와 함께 쓸 수 없습니다. 디졸브에는 텍스처·노이즈·높이·밴드 등을 사용하세요. 빛 마스크를 사용하는 효과는 GI 베이크에 포함하지 않으며, 시선 의존 림 발광도 굽지 않습니다.")),
                new Page("FBX 미리보기",
                    new Section("FBX 넣기", "Project에 임포트된 .fbx 파일을 FBX 모델 필드에 끌어 넣습니다. FBX 파일 선택으로 Assets 안의 파일을 고르거나, Project에서 FBX를 선택한 뒤 선택 FBX 사용을 눌러도 됩니다. 외부 파일은 먼저 Project에 임포트하세요. 씬에 배치할 필요는 없습니다."),
                    new Section("파일 전체 읽기", "FBX 안의 하위 Mesh를 넣어도 해당 FBX의 모델 루트를 읽습니다. 하위 MeshRenderer와 SkinnedMeshRenderer를 모으며, LODGroup이 있으면 LOD0를 사용합니다. 씬 오브젝트와 일반 프리팹은 입력으로 받지 않습니다."),
                    new Section("표시되는 머티리얼", "조립 중인 Material을 모델의 모든 서브메시에 적용합니다. FBX의 원래 머티리얼 조합을 재현하는 미리보기는 아닙니다. 원본 FBX·임포터·머티리얼은 변경하지 않습니다."),
                    new Section("보기 조작", "미리보기 안에서 왼쪽 또는 오른쪽 버튼으로 드래그하면 회전하고 휠로 확대·축소합니다. 보기 초기화는 카메라와 메시 스냅샷을 다시 만듭니다. FBX를 수정한 뒤에도 이 버튼으로 다시 읽을 수 있습니다. 입력을 비우면 기본 도형으로 돌아갑니다."),
                    new Section("오른쪽 프리뷰 설정", "기본 구와 FBX 모두 오른쪽 설정의 조명을 사용합니다. 배경색·환경광을 바꾸고 + 라이트 추가로 최대 " + ShaderComposerPreviewSettings.MaximumLights + "개의 Directional/Point/Spot 라이트를 편집합니다. 각 카드에서 켜기/끄기·삭제·색·강도와 종류별 위치·회전·범위·스폿 각도를 조절합니다. 실제 동시 반영 수는 URP 설정을 따릅니다."),
                    new Section("위치와 범위", "위치의 원점은 모델 중심이며 위치·범위의 1은 모델 반경입니다. 예를 들어 위치 Z=-2는 모델 중심에서 뒤쪽으로 반경 두 배만큼 떨어진 곳입니다. Spot은 회전이 가리키는 방향으로 비춥니다. 기본 조명 복원은 배경색·환경광과 두 Directional 조명을 초기화합니다."),
                    new Section("라이트 오버레이", "오른쪽 라이트 오버레이를 켜면 번호·종류·위치와 Directional의 방향 화살표, Point의 범위 구, Spot의 안쪽/바깥쪽 각도·범위를 표시합니다. 영향 범위 표시를 끄면 범위 선만 숨깁니다. 꺼진 라이트는 회색이며 화면 밖이면 테두리에 안내합니다. 범위가 크면 휠로 축소해서 확인하세요. Directional의 화살표 위치는 방향을 보여 주기 위한 표시 위치입니다. 오버레이는 표시 전용이며 실제 조명이나 내보내기 결과에는 영향을 주지 않습니다."),
                    new Section("URP 후처리", "오른쪽 URP 후처리를 켜고 Volume Profile 에셋을 넣으면 Bloom·색 보정·Tonemapping 등의 효과를 프리뷰에 적용합니다. 씬에서 사용 중인 Volume의 Profile 에셋을 넣어 같은 설정을 확인할 수 있습니다. 비어 있으면 URP 기본 Volume 설정만 사용하며 실제 씬의 Volume을 자동으로 섞지 않습니다. 원본 프로필은 수정하지 않습니다. 효과별 Override 체크와 활성 상태, URP Renderer의 후처리 지원을 확인하세요. 라이트 오버레이는 후처리 뒤에 표시됩니다. 후처리 비용은 프리뷰 갱신 시에도 발생하며 Shader 내보내기에 포함되지 않습니다."),
                    new Section("씬과 비교할 때", "프리뷰 라이트는 실제 씬의 조명을 바꾸지 않으며 Shader/Material 내보내기에도 포함되지 않습니다. 현재 프리뷰 라이트는 그림자와 쿠키를 설정하지 않습니다. 씬의 Volume 혼합·커스텀 Renderer Feature·Motion Blur 같은 카메라 이동 이력은 완전히 재현하지 않습니다. 실제 씬의 최종 결과도 확인하세요. 애니메이션·게임 스크립트·ParticleSystem·VFX 시뮬레이션은 실행하지 않습니다. Skinned Mesh는 읽는 시점의 포즈를 고정해 표시합니다.")),
                new Page("사용자 프로퍼티",
                    new Section("프로퍼티 만들기", "조립 창의 프로퍼티 창 버튼 또는 ProjectL > Shader Composer Properties를 엽니다. + 프로퍼티 추가로 Shader 이름·표시 이름·타입·기본값을 정합니다. Shader 이름은 _EffectColor처럼 _로 시작하는 영문·숫자·밑줄 이름이며 중복할 수 없습니다."),
                    new Section("지원 타입", string.Join(" / ", ShaderComposerProperties.Labels) + "\nRange에는 최솟값·최댓값을, Color에는 HDR 여부를 설정할 수 있습니다."),
                    new Section("입력에 연결하기", "호환되는 프로퍼티를 만들면 모델·혼합 가중치·기본 표면·효과·마스크에 ○○ 입력 목록이 나타납니다. 직접 값 대신 프로퍼티를 선택하세요. 같은 프로퍼티를 여러 입력에 연결하면 하나의 값으로 함께 조절합니다."),
                    new Section("타입과 값의 의미", "숫자 입력은 Float/Range, 색은 Color, 텍스처는 Texture2D에 연결합니다. 속도 같은 Vector2 입력은 Vector2/Vector4, 타일링+이동은 Vector4에 연결합니다. 타일링은 XY, 이동은 ZW입니다. 축·채널은 정수, 반전은 0/1 값을 사용하세요. 공유 Range의 범위는 연결한 입력에 맞춰 직접 정해야 합니다."),
                    new Section("수정·삭제", "프로퍼티 이름을 바꿔도 연결은 유지합니다. 삭제하거나 호환되지 않는 타입으로 바꾸면 현재 기본값을 해당 입력의 직접 값으로 복사하고 연결을 풉니다. 직접 값의 슬라이더 범위는 각 입력의 범위를 따릅니다. 모델·효과·마스크 종류를 바꾸면 새 종류에 없는 입력은 제거됩니다.")),
                new Page("저장·재편집",
                    new Section("파일명과 Shader 내부 이름", "저장 대화상자의 파일명과 창 위쪽의 Shader 이름은 별개입니다. 새 파일로 저장할 때 내부 이름이 다른 Shader와 겹치면 기존 상위 경로에 저장 파일명을 붙인 이름으로 자동 조정합니다. 그 이름도 중복되면 숫자를 붙이며, 저장 뒤 창의 Shader 이름도 갱신됩니다. 고유한 내부 이름은 그대로 유지합니다. 기존 파일에 다시 저장할 때 다른 Shader와 이름이 겹치면 충돌 경로를 표시하므로 내부 이름을 바꿔 주세요."),
                    new Section("Shader와 Material 저장", "Shader 파일로 내보내기로 Assets 아래의 .shader 파일을 저장합니다. 같은 경로를 선택하면 해당 Shader 파일을 다시 씁니다. 기존 파일을 바꾸기 전에 경로를 확인하세요. 머티리얼도 함께 만들기는 현재 값을 가진 새 Material을 만들며, 같은 파일명이 있으면 새 이름을 사용합니다."),
                    new Section("기존 조립 다시 열기", "조립한 Shader 열기에서 이 도구로 내보낸 Shader를 선택합니다. 파일에 저장된 모델·효과·마스크·프로퍼티·기본값을 복원합니다. 예전 단일 모델 레시피도 읽습니다. 외부 Shader나 직접 수정한 HLSL을 GUI로 역변환하는 기능은 없습니다."),
                    new Section("머티리얼 값과 레시피", "Inspector에서 바꾼 Material의 현재 값은 조립 레시피에 자동 반영되지 않습니다. 조립 창은 Shader에 저장된 기본값을 읽습니다. 다시 내보내도 기존 Material의 값을 자동으로 덮어쓰지 않습니다. 새 Material을 적용하거나 기존 Material의 값을 직접 조절하세요."),
                    new Section("지원 출력", "현재는 불투명 표면과 디졸브의 픽셀 폐기를 지원합니다. 투명 Blend·양면 렌더링·버텍스 변형은 지원하지 않습니다. 추가 출력에서 그림자·깊이·노멀·GI 베이크 Pass를 조절할 수 있지만, 필요한 출력을 끄면 해당 렌더 기능이 동작하지 않을 수 있습니다."),
                    new Section("다른 프로젝트로 옮길 때", "결과 Shader는 현재 프로젝트의 Composer 공용 HLSL과 URP를 참조합니다. Shader 파일 하나만 다른 프로젝트로 복사해서 사용할 수 있는 독립 패키지는 아닙니다.")),
                new Page("성능",
                    new Section("미리보기의 비용", "FBX 미리보기는 Editor 전용입니다. 메시 수집과 Skinned Mesh Bake는 모델 변경/보기 초기화 때 수행합니다. 현재 창은 가만히 있어도 주기적으로 미리보기를 다시 그리므로 큰 FBX와 많은 서브메시는 Editor CPU/GPU 사용량을 높일 수 있습니다. 작업하지 않을 때는 조립 창을 닫으세요."),
                    new Section("게임에서의 비용", "내보낸 Shader의 비용은 게임에서도 발생합니다. 모델을 추가하면 픽셀당 조명 계산이 늘어납니다. 현재 모델들이 조명을 각각 순회하고 빛 마스크도 별도 계산하므로 모델 혼합·빛 마스크·여러 그림자 조명의 조합은 비용이 커질 수 있습니다. 모델마다 별도의 Forward Pass가 추가되는 방식은 아닙니다."),
                    new Section("사용 기준", "우선 모델 하나로 원하는 표현을 만들고, 혼합이 필요한 경우에만 모델을 추가하세요. 사용하지 않는 모델/효과는 체크를 끄면 생성에서 제외합니다. 가중치 0은 결과에서 제외하지만 셰이더 구성에는 모델이 남습니다. 큰 화면 영역·많은 오브젝트에 적용할 때는 실제 씬에서 비교 측정하세요."),
                    new Section("편집 중 멈춤", "모델·효과·마스크 종류나 입력 연결을 바꾸면 셰이더를 다시 만들고 컴파일하므로 일시적으로 Editor가 멈출 수 있습니다. 같은 구성에서 일반 숫자·색 기본값만 바꾸는 경우에는 주로 Material 값만 갱신합니다."),
                    new Section("검증의 범위", "컴파일 성공·표시 확인·SRP Batcher 호환은 프레임 성능 보장이 아닙니다. 목표 기기의 실제 씬에서 Unity Profiler의 CPU/GPU 시간과 Frame Debugger의 렌더 구성을 확인하세요. 현재 도구에는 자동 성능 측정이나 안전한 모델 수 판정 기능이 없습니다.")),
                new Page("문제 해결",
                    new Section("모델이 검게 보일 때", "활성 모델과 혼합 가중치를 확인하세요. 모든 가중치가 0이면 검정입니다. 기본색·텍스처, 색상 곱하기 효과, 조명·환경광도 확인합니다. Unlit으로 잠시 바꾸면 표면 입력과 조명 문제를 구분하기 쉽습니다."),
                    new Section("FBX가 보이지 않을 때", "Unity에 임포트된 .fbx 파일인지 확인하세요. 씬 오브젝트·일반 프리팹은 사용할 수 없습니다. 메시가 없거나 렌더러가 비활성화된 파일은 표시되지 않을 수 있습니다. 보기 초기화로 다시 읽고, 입력을 비워 기본 도형도 확인하세요."),
                    new Section("효과가 안 보일 때", "효과 체크·강도·순서와 마스크 반전/기준값을 확인하세요. 빛 마스크는 충분한 직접광이 필요합니다. 텍스처 마스크는 채널과 UV를 확인합니다. 같은 위치라도 FBX 로컬 좌표와 씬 배치 좌표는 다릅니다."),
                    new Section("연결 오류가 뜰 때", "○○ 입력에서 프로퍼티가 존재하는지, 타입이 맞는지 확인하거나 직접 값으로 바꾸세요. 사용자 프로퍼티 이름이 자동 생성 이름이나 Unity 예약 이름과 겹치면 다른 이름을 사용합니다. 빛 마스크+디졸브 조합은 다른 마스크로 바꿔야 합니다."),
                    new Section("내보내기 버튼이 비활성일 때", "미리보기 갱신/컴파일이 끝날 때까지 기다리고 창에 표시된 오류를 해결하세요. 필요하면 미리보기 새로고침을 누릅니다. Shader 이름이 다른 파일과 겹치면 이름을 바꾸고 다시 저장하세요."),
                    new Section("미리보기와 게임 화면이 다를 때", "실제 Renderer의 Material 할당과 값을 먼저 확인합니다. 그다음 씬의 조명·그림자·URP 설정·후처리를 확인하세요. 미리보기의 전용 조명은 씬 조명과 연동되지 않습니다."))
            };
        }
    }
}
