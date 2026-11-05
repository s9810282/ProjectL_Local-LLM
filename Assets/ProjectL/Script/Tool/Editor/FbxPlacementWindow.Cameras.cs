using Cinemachine;
using Cinemachine.PostFX;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

namespace ProjectL.Tool.Editor
{
    public sealed partial class FbxPlacementWindow
    {
        [SerializeField] private bool showCameraCatalog;
        [SerializeField] private Vector3 cameraPlacementPosition = new Vector3(0, 8, -8);
        [SerializeField] private Vector3 cameraPlacementEuler = new Vector3(40, 0, 0);
        [SerializeField] private Vector2 cameraPreviewSize = new Vector2(320, 230);
        private Vector2 _cameraCatalogScroll;
        private readonly UniversalRenderPipeline.SingleCameraRequest _virtualCameraPreviewRequest = new UniversalRenderPipeline.SingleCameraRequest();
        private Camera _virtualCameraPreviewCamera;
        private VolumeStack _virtualCameraPreviewStack;
        private RenderTexture _virtualCameraPreviewTexture;
        private int _virtualCameraPreviewWidth;
        private int _virtualCameraPreviewHeight;
        private VisualElement _virtualCameraPreviewPanel;
        private Label _virtualCameraPreviewTitle;
        private Label _virtualCameraPreviewMessage;
        private Image _virtualCameraPreviewImage;
        private double _nextVirtualCameraPreviewTime;
        private VisualElement _virtualCameraPreviewResizer;
        private int _cameraPreviewResizePointerId = -1;
        private Vector2 _cameraPreviewResizeStart;
        private Vector2 _cameraPreviewResizeStartSize;

        private const float MinCameraPreviewWidth = 200;
        private const float MinCameraPreviewHeight = 150;
        private const string CameraPreviewWidthPreference = "ProjectL.FbxPlacement.CameraPreview.Width";
        private const string CameraPreviewHeightPreference = "ProjectL.FbxPlacement.CameraPreview.Height";

        private void DrawCameraCatalog()
        {
            using (new GUILayout.AreaScope(new Rect(8, 6, CatalogWidth - 16, CatalogContentHeight - 12)))
            using (var scroll = new EditorGUILayout.ScrollViewScope(_cameraCatalogScroll))
            {
                _cameraCatalogScroll = scroll.scrollPosition;
                GUILayout.Label("Cinemachine 버추얼 카메라", EditorStyles.boldLabel);
                EditorGUILayout.HelpBox("맵 프리팹에 함께 저장됩니다. 실행 화면은 게임 씬의 Output Camera와 Cinemachine Brain이 표시합니다.", MessageType.Info);

                if (GUILayout.Button("현재 작업 화면으로 설치", GUILayout.Height(26)))
                    Run(AddCameraFromSceneView);
                EditorGUILayout.LabelField("또는 위치와 회전을 직접 입력", EditorStyles.miniLabel);
                cameraPlacementPosition = EditorGUILayout.Vector3Field("맵 기준 위치", cameraPlacementPosition);
                cameraPlacementEuler = EditorGUILayout.Vector3Field("월드 회전", cameraPlacementEuler);
                if (GUILayout.Button("설정한 위치로 설치", GUILayout.Height(24)))
                    Run(() => AddCamera(cameraPlacementPosition, Quaternion.Euler(cameraPlacementEuler)));

                GUILayout.Space(8);
                CinemachineVirtualCamera[] cameras = FbxPlacementCameras.GetCameras(workspaceRoot);
                GUILayout.Label("맵의 카메라 · " + cameras.Length + "개", EditorStyles.boldLabel);
                if (cameras.Length == 0)
                    EditorGUILayout.LabelField("현재 작업 화면으로 첫 카메라를 설치해 보세요.", EditorStyles.wordWrappedMiniLabel);
                for (int i = 0; i < cameras.Length; i++) DrawCameraCatalogRow(cameras[i]);
            }
        }

        private void DrawCameraCatalogRow(CinemachineVirtualCamera camera)
        {
            LensSettings lens = camera.m_Lens;
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Toggle(Selection.Contains(camera.gameObject), camera.name, "Button") != Selection.Contains(camera.gameObject))
                    Select(camera.transform);
                if (GUILayout.Button("보기", GUILayout.Width(34)))
                {
                    Select(camera.transform);
                    Frame(camera.transform);
                }
            }
            string lensDescription = lens.Orthographic ? "직교 크기 " + lens.OrthographicSize.ToString("0.##") :
                "원근 FOV " + lens.FieldOfView.ToString("0.##");
            EditorGUILayout.LabelField(lensDescription + " · 우선순위 " + camera.Priority, EditorStyles.miniLabel);
        }

        private void AddCameraFromSceneView()
        {
            Camera source = camera;
            if (source == null) throw new System.InvalidOperationException("작업 화면 카메라를 찾을 수 없습니다.");
            CinemachineVirtualCamera virtualCamera = FbxPlacementCameras.Add(workspaceRoot,
                workspaceRoot.transform.InverseTransformPoint(source.transform.position), source.transform.rotation);
            _expanded.Add(workspaceRoot.transform.GetInstanceID());
            Select(virtualCamera.transform);
            Changed();
            _notice = null;
            CopyLensFromSceneView(virtualCamera, source);
        }

        private void AddCamera(Vector3 position, Quaternion rotation)
        {
            CinemachineVirtualCamera camera = FbxPlacementCameras.Add(workspaceRoot, position, rotation);
            _expanded.Add(workspaceRoot.transform.GetInstanceID());
            Select(camera.transform);
            Changed();
            _notice = null;
        }

        private void DrawCameraSettings()
        {
            CinemachineVirtualCamera camera = selected.GetComponent<CinemachineVirtualCamera>();
            if (camera == null) return;

            GUILayout.Space(8);
            GUILayout.Label("버추얼 카메라 설정", EditorStyles.boldLabel);
            if (GUILayout.Button("현재 작업 화면으로 맞추기"))
            {
                Camera source = this.camera;
                if (source != null)
                {
                    Undo.RecordObject(camera.transform, "버추얼 카메라 화면 맞추기");
                    Undo.RecordObject(camera, "버추얼 카메라 화면 맞추기");
                    camera.transform.SetPositionAndRotation(source.transform.position, source.transform.rotation);
                    CopyLensFromSceneView(camera, source);
                    Changed();
                }
            }

            LensSettings lens = camera.m_Lens;
            EditorGUI.BeginChangeCheck();
            bool orthographic = EditorGUILayout.Toggle("직교 카메라", lens.Orthographic);
            float orthographicSize = lens.OrthographicSize;
            float fieldOfView = lens.FieldOfView;
            if (orthographic) orthographicSize = Mathf.Max(.01f, EditorGUILayout.FloatField("직교 크기", orthographicSize));
            else fieldOfView = EditorGUILayout.Slider("시야각", fieldOfView, 1, 179);
            int priority = EditorGUILayout.IntField("우선순위", camera.Priority);
            bool receivesImpulse = camera.TryGetComponent(out CinemachineImpulseListener listener);
            receivesImpulse = EditorGUILayout.Toggle("흔들림 수신", receivesImpulse);
            int channel = listener != null ? listener.m_ChannelMask : 1;
            if (receivesImpulse) channel = EditorGUILayout.IntField("흔들림 채널", channel);
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(camera, "버추얼 카메라 설정");
                lens.Orthographic = orthographic;
                lens.OrthographicSize = orthographicSize;
                lens.FieldOfView = fieldOfView;
                camera.m_Lens = lens;
                camera.Priority = priority;
                if (receivesImpulse && listener == null)
                {
                    listener = Undo.AddComponent<CinemachineImpulseListener>(camera.gameObject);
                    listener.m_ChannelMask = channel;
                }
                if (!receivesImpulse && listener != null)
                {
                    Undo.DestroyObjectImmediate(listener);
                }
                else if (listener != null)
                {
                    Undo.RecordObject(listener, "버추얼 카메라 흔들림 설정");
                    listener.m_ChannelMask = channel;
                }
                Changed();
            }

            DrawCameraVolumeSettings(camera);
            EditorGUILayout.HelpBox("이름은 실행 중 함께 로드되는 다른 버추얼 카메라와 겹치면 안 됩니다. 위의 '이름' 필드에서 고유하게 바꾸세요.", MessageType.Warning);
        }

        private void DrawCameraVolumeSettings(CinemachineVirtualCamera camera)
        {
            GUILayout.Space(8);
            GUILayout.Label("버추얼 Volume", EditorStyles.boldLabel);
            if (!camera.TryGetComponent(out CinemachineVolumeSettings settings))
            {
                EditorGUILayout.HelpBox("이 카메라에는 카메라별 후처리 설정이 없습니다.", MessageType.Info);
                if (GUILayout.Button("버추얼 Volume 컴포넌트 추가"))
                {
                    Undo.AddComponent<CinemachineVolumeSettings>(camera.gameObject);
                    Changed();
                }
                return;
            }

            EditorGUI.BeginChangeCheck();
            VolumeProfile profile = (VolumeProfile)EditorGUILayout.ObjectField("사용할 Volume Profile", settings.m_Profile,
                typeof(VolumeProfile), false);
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(settings, "버추얼 카메라 Volume 설정");
                settings.m_Profile = profile;
                settings.InvalidateCachedProfile();
                Changed();
            }
            EditorGUILayout.LabelField("Volume GameObject를 만들지 않습니다. 선택한 Profile만 이 Cinemachine 컴포넌트에 저장되며, 카메라가 활성일 때 Output Camera에 적용됩니다.", EditorStyles.wordWrappedMiniLabel);
        }

        private void BuildVirtualCameraPreviewPanel()
        {
            RemoveVirtualCameraPreviewPanel();
            cameraPreviewSize = new Vector2(
                EditorPrefs.GetFloat(CameraPreviewWidthPreference, cameraPreviewSize.x),
                EditorPrefs.GetFloat(CameraPreviewHeightPreference, cameraPreviewSize.y));
            _virtualCameraPreviewPanel = new VisualElement { name = "fbx-virtual-camera-preview" };
            _virtualCameraPreviewPanel.style.position = Position.Absolute;
            _virtualCameraPreviewPanel.style.top = PlacementToolbarHeight + 12;
            _virtualCameraPreviewPanel.style.backgroundColor = new Color(.12f, .12f, .12f, 1);
            _virtualCameraPreviewPanel.style.paddingLeft = 7;
            _virtualCameraPreviewPanel.style.paddingRight = 7;
            _virtualCameraPreviewPanel.style.paddingTop = 6;
            _virtualCameraPreviewPanel.style.paddingBottom = 7;
            _virtualCameraPreviewTitle = new Label("카메라 화면 미리보기");
            _virtualCameraPreviewTitle.style.unityFontStyleAndWeight = FontStyle.Bold;
            _virtualCameraPreviewTitle.style.height = 22;
            _virtualCameraPreviewTitle.style.flexShrink = 0;
            _virtualCameraPreviewImage = new Image { scaleMode = ScaleMode.ScaleToFit, pickingMode = PickingMode.Ignore };
            _virtualCameraPreviewImage.style.flexGrow = 1;
            _virtualCameraPreviewImage.style.minHeight = 0;
            _virtualCameraPreviewImage.style.backgroundColor = new Color(.08f, .08f, .08f, 1);
            _virtualCameraPreviewMessage = new Label();
            _virtualCameraPreviewMessage.style.whiteSpace = WhiteSpace.Normal;
            _virtualCameraPreviewPanel.Add(_virtualCameraPreviewTitle);
            _virtualCameraPreviewPanel.Add(_virtualCameraPreviewImage);
            _virtualCameraPreviewPanel.Add(_virtualCameraPreviewMessage);
            var resizeHint = new Label("크기 조절 ↘") { pickingMode = PickingMode.Ignore };
            resizeHint.style.height = 18;
            resizeHint.style.flexShrink = 0;
            resizeHint.style.unityTextAlign = TextAnchor.MiddleRight;
            _virtualCameraPreviewPanel.Add(resizeHint);
            _virtualCameraPreviewResizer = new VisualElement
            {
                name = "fbx-camera-preview-resizer",
                tooltip = "드래그하면 카메라 프리뷰의 너비와 높이를 조절합니다."
            };
            _virtualCameraPreviewResizer.style.position = Position.Absolute;
            _virtualCameraPreviewResizer.style.right = 0;
            _virtualCameraPreviewResizer.style.bottom = 0;
            _virtualCameraPreviewResizer.style.width = 24;
            _virtualCameraPreviewResizer.style.height = 24;
            _virtualCameraPreviewResizer.RegisterCallback<PointerDownEvent>(BeginCameraPreviewResize);
            _virtualCameraPreviewResizer.RegisterCallback<PointerMoveEvent>(ResizeCameraPreview);
            _virtualCameraPreviewResizer.RegisterCallback<PointerUpEvent>(EndCameraPreviewResize);
            _virtualCameraPreviewResizer.RegisterCallback<PointerCaptureOutEvent>(OnCameraPreviewResizeCaptureOut);
            _virtualCameraPreviewPanel.Add(_virtualCameraPreviewResizer);
            _virtualCameraPreviewPanel.style.display = DisplayStyle.None;
            rootVisualElement.Add(_virtualCameraPreviewPanel);
            ApplyVirtualCameraPreviewLayout();
        }

        private void RemoveVirtualCameraPreviewPanel()
        {
            if (_cameraPreviewResizePointerId != -1 && _virtualCameraPreviewResizer != null &&
                _virtualCameraPreviewResizer.HasPointerCapture(_cameraPreviewResizePointerId))
                _virtualCameraPreviewResizer.ReleasePointer(_cameraPreviewResizePointerId);
            _cameraPreviewResizePointerId = -1;
            _virtualCameraPreviewResizer = null;
            _virtualCameraPreviewPanel?.RemoveFromHierarchy();
            _virtualCameraPreviewPanel = null;
            _virtualCameraPreviewTitle = null;
            _virtualCameraPreviewImage = null;
            _virtualCameraPreviewMessage = null;
        }

        private Vector2 ApplyVirtualCameraPreviewLayout()
        {
            if (_virtualCameraPreviewPanel == null) return Vector2.zero;
            float left = catalogPanelWidth + sidebarPanelWidth + 12;
            float maxWidth = Mathf.Max(64, rootVisualElement.contentRect.width - left - 12);
            float maxHeight = Mathf.Max(64, rootVisualElement.contentRect.height - PlacementToolbarHeight - 24);
            var size = new Vector2(
                Mathf.Clamp(cameraPreviewSize.x, Mathf.Min(MinCameraPreviewWidth, maxWidth), maxWidth),
                Mathf.Clamp(cameraPreviewSize.y, Mathf.Min(MinCameraPreviewHeight, maxHeight), maxHeight));
            // 뷰포트 왼쪽 위를 기준으로 잡아 오른쪽 아래 손잡이가 드래그 방향대로 움직인다.
            _virtualCameraPreviewPanel.style.left = left;
            _virtualCameraPreviewPanel.style.width = size.x;
            _virtualCameraPreviewPanel.style.height = size.y;
            return size;
        }

        private void BeginCameraPreviewResize(PointerDownEvent evt)
        {
            if (evt.button != 0 || _cameraPreviewResizePointerId != -1) return;
            _cameraPreviewResizePointerId = evt.pointerId;
            _cameraPreviewResizeStart = rootVisualElement.WorldToLocal(evt.position);
            _cameraPreviewResizeStartSize = ApplyVirtualCameraPreviewLayout();
            _virtualCameraPreviewResizer.CapturePointer(evt.pointerId);
            evt.StopPropagation();
        }

        private void ResizeCameraPreview(PointerMoveEvent evt)
        {
            if (_cameraPreviewResizePointerId != evt.pointerId) return;
            Vector2 pointer = rootVisualElement.WorldToLocal(evt.position);
            cameraPreviewSize = _cameraPreviewResizeStartSize + pointer - _cameraPreviewResizeStart;
            cameraPreviewSize = ApplyVirtualCameraPreviewLayout();
            _nextVirtualCameraPreviewTime = 0;
            Repaint();
            evt.StopPropagation();
        }

        private void EndCameraPreviewResize(PointerUpEvent evt)
        {
            if (_cameraPreviewResizePointerId != evt.pointerId || evt.button != 0) return;
            SaveCameraPreviewSize();
            _virtualCameraPreviewResizer.ReleasePointer(evt.pointerId);
            _cameraPreviewResizePointerId = -1;
            evt.StopPropagation();
        }

        private void OnCameraPreviewResizeCaptureOut(PointerCaptureOutEvent evt)
        {
            if (_cameraPreviewResizePointerId != -1) SaveCameraPreviewSize();
            _cameraPreviewResizePointerId = -1;
        }

        private void SaveCameraPreviewSize()
        {
            EditorPrefs.SetFloat(CameraPreviewWidthPreference, cameraPreviewSize.x);
            EditorPrefs.SetFloat(CameraPreviewHeightPreference, cameraPreviewSize.y);
        }

        private void UpdateVirtualCameraPreview()
        {
            if (_virtualCameraPreviewPanel == null) return;
            CinemachineVirtualCamera source = selected != null ? selected.GetComponent<CinemachineVirtualCamera>() : null;
            bool visible = source != null && Owns(source.transform) && !showBakedResult &&
                !EditorApplication.isPlayingOrWillChangePlaymode && !FbxPlacementBaker.IsRunning;
            _virtualCameraPreviewPanel.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
            if (!visible || EditorApplication.isCompiling || EditorApplication.timeSinceStartup < _nextVirtualCameraPreviewTime) return;
            _nextVirtualCameraPreviewTime = EditorApplication.timeSinceStartup + 1.0 / 15;
            _virtualCameraPreviewPanel.BringToFront();
            _virtualCameraPreviewTitle.text = "카메라 화면 · " + source.name;
            Vector2 size = ApplyVirtualCameraPreviewLayout();
            Rect imageRect = _virtualCameraPreviewImage.contentRect;
            if (imageRect.width <= 0 || imageRect.height <= 0)
                imageRect = new Rect(0, 0, size.x - 14, size.y - 53);
            _virtualCameraPreviewImage.image = RenderVirtualCameraPreview(source, imageRect);
            _virtualCameraPreviewPanel.MarkDirtyRepaint();
        }

        private void DrawWorkspaceCameras()
        {
            foreach (CinemachineVirtualCamera source in FbxPlacementCameras.GetCameras(workspaceRoot))
            {
                if (!source.gameObject.activeInHierarchy) continue;
                bool isSelected = selected == source.transform;
                float size = HandleUtility.GetHandleSize(source.transform.position) * .12f;
                Color color = isSelected ? new Color(1, .8f, .2f) : new Color(.3f, .8f, 1);
                using (new Handles.DrawingScope(color))
                {
                    if (Handles.Button(source.transform.position, source.transform.rotation, size, size * 1.5f, Handles.CubeHandleCap))
                        Select(source.transform);
                    Handles.Label(source.transform.position + Vector3.up * size * 1.5f, source.name);
                    Handles.ArrowHandleCap(0, source.transform.position, source.transform.rotation, size * 4, EventType.Repaint);
                    if (isSelected) DrawVirtualCameraFrustum(source, size);
                }
            }
        }

        private void DrawVirtualCameraFrustum(CinemachineVirtualCamera source, float handleSize)
        {
            LensSettings lens = source.m_Lens;
            float aspect = _virtualCameraPreviewWidth > 0 && _virtualCameraPreviewHeight > 0 ?
                (float)_virtualCameraPreviewWidth / _virtualCameraPreviewHeight : 16f / 9f;
            float near = Mathf.Max(.01f, lens.NearClipPlane);
            float far = Mathf.Max(near + .01f, lens.FarClipPlane);
            // 먼 클립 면이 작업 화면 전체를 덮지 않도록 기즈모의 표시 길이만 제한한다.
            far = Mathf.Min(far, Mathf.Max(near + .01f, handleSize * 20));
            float nearHeight = lens.Orthographic ? lens.OrthographicSize : Mathf.Tan(lens.FieldOfView * Mathf.Deg2Rad * .5f) * near;
            float farHeight = lens.Orthographic ? lens.OrthographicSize : Mathf.Tan(lens.FieldOfView * Mathf.Deg2Rad * .5f) * far;
            using (new Handles.DrawingScope(Matrix4x4.TRS(source.transform.position, source.transform.rotation, Vector3.one)))
            {
                Vector3 a = new Vector3(-nearHeight * aspect, -nearHeight, near);
                Vector3 b = new Vector3(-nearHeight * aspect, nearHeight, near);
                Vector3 c = new Vector3(nearHeight * aspect, nearHeight, near);
                Vector3 d = new Vector3(nearHeight * aspect, -nearHeight, near);
                Vector3 e = new Vector3(-farHeight * aspect, -farHeight, far);
                Vector3 f = new Vector3(-farHeight * aspect, farHeight, far);
                Vector3 g = new Vector3(farHeight * aspect, farHeight, far);
                Vector3 h = new Vector3(farHeight * aspect, -farHeight, far);
                Handles.DrawLine(a, b);
                Handles.DrawLine(b, c);
                Handles.DrawLine(c, d);
                Handles.DrawLine(d, a);
                Handles.DrawLine(e, f);
                Handles.DrawLine(f, g);
                Handles.DrawLine(g, h);
                Handles.DrawLine(h, e);
                Handles.DrawLine(a, e);
                Handles.DrawLine(b, f);
                Handles.DrawLine(c, g);
                Handles.DrawLine(d, h);
            }
        }

        private RenderTexture RenderVirtualCameraPreview(CinemachineVirtualCamera source, Rect rect)
        {
            // URP 요청이 바꾼 타깃을 복원해야 이후 툴 UI가 미리보기 텍스처에 그려지지 않는다.
            RenderTexture previousTarget = RenderTexture.active;
            try
            {
                RenderTexture texture = RenderVirtualCameraPreviewContents(source, rect);
                _virtualCameraPreviewMessage.text = texture == null ? "카메라 화면을 준비하고 있습니다." : "";
                return texture;
            }
            catch (System.Exception exception)
            {
                _virtualCameraPreviewMessage.text = "미리보기 오류: " + exception.Message;
                return null;
            }
            finally
            {
                RenderTexture.active = previousTarget;
            }
        }

        private RenderTexture RenderVirtualCameraPreviewContents(CinemachineVirtualCamera source, Rect rect)
        {
            if (workspaceRoot == null || !Owns(source.transform)) return null;
            EnsureVirtualCameraPreview();
            if (_virtualCameraPreviewCamera == null) return null;

            float pixelScale = Mathf.Min(EditorGUIUtility.pixelsPerPoint, 2048f / Mathf.Max(rect.width, rect.height, 1));
            int width = Mathf.Max(64, Mathf.CeilToInt(rect.width * pixelScale));
            int height = Mathf.Max(64, Mathf.CeilToInt(rect.height * pixelScale));
            if (_virtualCameraPreviewTexture == null || _virtualCameraPreviewWidth != width || _virtualCameraPreviewHeight != height)
            {
                if (_virtualCameraPreviewTexture != null) DestroyImmediate(_virtualCameraPreviewTexture);
                var descriptor = new RenderTextureDescriptor(width, height, RenderTextureFormat.ARGB32, 24)
                {
                    msaaSamples = 1,
                    sRGB = QualitySettings.activeColorSpace == ColorSpace.Linear,
                    useMipMap = false,
                    autoGenerateMips = false
                };
                _virtualCameraPreviewTexture = new RenderTexture(descriptor)
                {
                    name = "버추얼 카메라 미리보기",
                    hideFlags = HideFlags.HideAndDontSave
                };
                if (!_virtualCameraPreviewTexture.Create())
                    throw new System.InvalidOperationException("카메라 미리보기용 색상 RenderTexture를 만들지 못했습니다.");
                _virtualCameraPreviewWidth = width;
                _virtualCameraPreviewHeight = height;
            }

            LensSettings lens = source.m_Lens;
            _virtualCameraPreviewCamera.transform.SetPositionAndRotation(source.transform.position, source.transform.rotation);
            _virtualCameraPreviewCamera.orthographic = lens.Orthographic;
            _virtualCameraPreviewCamera.orthographicSize = lens.OrthographicSize;
            _virtualCameraPreviewCamera.fieldOfView = lens.FieldOfView;
            _virtualCameraPreviewCamera.nearClipPlane = lens.NearClipPlane;
            _virtualCameraPreviewCamera.farClipPlane = lens.FarClipPlane;
            _virtualCameraPreviewCamera.aspect = (float)width / height;
            _virtualCameraPreviewCamera.targetTexture = _virtualCameraPreviewTexture;
            VolumeProfile profile = source.TryGetComponent(out CinemachineVolumeSettings settings) ? settings.m_Profile : null;
            if (!RenderVirtualCameraPreviewWithUrp(profile))
            {
                _virtualCameraPreviewCamera.Render();
            }
            return _virtualCameraPreviewTexture;
        }

        private void EnsureVirtualCameraPreview()
        {
            if (_virtualCameraPreviewCamera != null && _virtualCameraPreviewCamera.gameObject.scene == workspaceRoot.scene) return;
            ReleaseVirtualCameraPreview();
            GameObject preview = EditorUtility.CreateGameObjectWithHideFlags("버추얼 카메라 미리보기", HideFlags.HideAndDontSave,
                typeof(Camera), typeof(UniversalAdditionalCameraData));
            SceneManager.MoveGameObjectToScene(preview, workspaceRoot.scene);
            _virtualCameraPreviewCamera = preview.GetComponent<Camera>();
            _virtualCameraPreviewCamera.enabled = false;
            _virtualCameraPreviewCamera.cameraType = CameraType.Preview;
            _virtualCameraPreviewCamera.clearFlags = CameraClearFlags.SolidColor;
            _virtualCameraPreviewCamera.backgroundColor = new Color(.08f, .08f, .08f, 1);
            _virtualCameraPreviewCamera.cullingMask = ~0;
            _virtualCameraPreviewCamera.scene = workspaceRoot.scene;
            _virtualCameraPreviewCamera.overrideSceneCullingMask = EditorSceneManager.GetSceneCullingMask(workspaceRoot.scene);
            UniversalAdditionalCameraData cameraData = preview.GetComponent<UniversalAdditionalCameraData>();
            cameraData.renderPostProcessing = true;
            cameraData.volumeLayerMask = 0;
        }

        private void ReleaseVirtualCameraPreview()
        {
            if (_virtualCameraPreviewTexture != null) DestroyImmediate(_virtualCameraPreviewTexture);
            _virtualCameraPreviewTexture = null;
            _virtualCameraPreviewWidth = 0;
            _virtualCameraPreviewHeight = 0;
            if (_virtualCameraPreviewCamera != null) DestroyImmediate(_virtualCameraPreviewCamera.gameObject);
            _virtualCameraPreviewCamera = null;
            if (_virtualCameraPreviewStack != null && VolumeManager.instance.isInitialized)
                VolumeManager.instance.DestroyStack(_virtualCameraPreviewStack);
            _virtualCameraPreviewStack = null;
        }

        private bool RenderVirtualCameraPreviewWithUrp(VolumeProfile profile)
        {
            if (!(GraphicsSettings.currentRenderPipeline is UniversalRenderPipelineAsset)) return false;

            UniversalAdditionalCameraData cameraData = _virtualCameraPreviewCamera.GetComponent<UniversalAdditionalCameraData>();
            cameraData.renderPostProcessing = profile != null;
            _virtualCameraPreviewRequest.destination = _virtualCameraPreviewTexture;
            if (!VolumeManager.instance.isInitialized)
            {
                RenderPipeline.SubmitRenderRequest(_virtualCameraPreviewCamera, _virtualCameraPreviewRequest);
                return true;
            }

            if (_virtualCameraPreviewStack == null || !_virtualCameraPreviewStack.isValid)
            {
                if (_virtualCameraPreviewStack != null) VolumeManager.instance.DestroyStack(_virtualCameraPreviewStack);
                _virtualCameraPreviewStack = VolumeManager.instance.CreateStack();
            }

            VolumeStack savedStack = VolumeManager.instance.stack;
            try
            {
                // 이 미리보기는 선택한 Cinemachine Profile만 사용한다. 맵의 Volume GameObject는 읽지 않는다.
                VolumeManager.instance.Update(_virtualCameraPreviewStack, _virtualCameraPreviewCamera.transform, 0);
                if (profile != null)
                {
                    foreach (VolumeComponent component in profile.components)
                    {
                        if (component == null || !component.active) continue;
                        VolumeComponent state = _virtualCameraPreviewStack.GetComponent(component.GetType());
                        if (state != null) component.Override(state, 1);
                    }
                }
                VolumeManager.instance.stack = _virtualCameraPreviewStack;
                RenderPipeline.SubmitRenderRequest(_virtualCameraPreviewCamera, _virtualCameraPreviewRequest);
                return true;
            }
            finally
            {
                VolumeManager.instance.stack = savedStack;
            }
        }

        private static void CopyLensFromSceneView(CinemachineVirtualCamera target, Camera source)
        {
            LensSettings lens = target.m_Lens;
            lens.Orthographic = source.orthographic;
            lens.OrthographicSize = Mathf.Max(.01f, source.orthographicSize);
            lens.FieldOfView = source.fieldOfView;
            target.m_Lens = lens;
        }
    }
}
