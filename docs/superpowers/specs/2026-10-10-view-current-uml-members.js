/* 현재 코드에서 추출한 표시용 스냅샷. HTML 상세 멤버 패널의 원본. */
const VIEW_UML_MEMBERS = {
  "ArtifactInputController": {
    "kind": "class",
    "inherits": "InputController",
    "source": "C:/Users/User/Project_L/Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/ViewSystem/ArtifactInput/ArtifactInputController.cs",
    "fields": [
      {
        "line": 10,
        "text": "private readonly IArtifactInputReceiver _receiver"
      },
      {
        "line": 11,
        "text": "private float _pointerX"
      },
      {
        "line": 12,
        "text": "private float _pointerY"
      },
      {
        "line": 13,
        "text": "private bool _isPressed"
      },
      {
        "line": 15,
        "text": "public override string ActionMap"
      }
    ],
    "methods": [
      {
        "line": 17,
        "text": "internal ArtifactInputController(IArtifactInputReceiver receiver)"
      },
      {
        "line": 22,
        "text": "protected override void Configure(InputBindings input)"
      },
      {
        "line": 31,
        "text": "private void OnPointer(InputSnapshot snapshot)"
      },
      {
        "line": 42,
        "text": "private void OnSelect(InputSnapshot snapshot)"
      }
    ]
  },
  "ArtifactInputLayers": {
    "kind": "class",
    "inherits": "",
    "source": "C:/Users/User/Project_L/Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/ViewSystem/ArtifactInput/ArtifactInputLayers.cs",
    "fields": [],
    "methods": []
  },
  "ArtifactInputNames": {
    "kind": "class",
    "inherits": "",
    "source": "C:/Users/User/Project_L/Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/ViewSystem/ArtifactInput/ArtifactInputNames.cs",
    "fields": [
      {
        "line": 6,
        "text": "internal const string Map"
      },
      {
        "line": 7,
        "text": "internal const string CommonMap"
      },
      {
        "line": 8,
        "text": "internal const string PointerPosition"
      },
      {
        "line": 9,
        "text": "internal const string Select"
      },
      {
        "line": 10,
        "text": "internal const string RotateLeft"
      },
      {
        "line": 11,
        "text": "internal const string RotateRight"
      },
      {
        "line": 12,
        "text": "internal const string Cancel"
      }
    ],
    "methods": []
  },
  "IArtifactInputSlot": {
    "kind": "interface",
    "inherits": "IServiceModule",
    "source": "C:/Users/User/Project_L/Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/ViewSystem/ArtifactInput/ArtifactInputSlot.cs",
    "fields": [],
    "methods": [
      {
        "line": 13,
        "text": "void Bind(IArtifactInputReceiver receiver)"
      },
      {
        "line": 19,
        "text": "void Unbind(IArtifactInputReceiver receiver)"
      }
    ]
  },
  "ArtifactInputSlot": {
    "kind": "class",
    "inherits": "IArtifactInputSlot, IArtifactInputReceiver, IExportSystemServices",
    "source": "C:/Users/User/Project_L/Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/ViewSystem/ArtifactInput/ArtifactInputSlot.cs",
    "fields": [
      {
        "line": 36,
        "text": "private IArtifactInputReceiver _receiver"
      },
      {
        "line": 37,
        "text": "private IArtifactInputReceiver _selectOwner"
      }
    ],
    "methods": [
      {
        "line": 39,
        "text": "public void Bind(IArtifactInputReceiver receiver)"
      },
      {
        "line": 49,
        "text": "public void Unbind(IArtifactInputReceiver receiver)"
      },
      {
        "line": 66,
        "text": "public void PointerMoved(float screenX, float screenY)"
      },
      {
        "line": 68,
        "text": "public void SelectPressed(float screenX, float screenY)"
      },
      {
        "line": 79,
        "text": "public void SelectReleased(float screenX, float screenY)"
      },
      {
        "line": 85,
        "text": "public void SelectCanceled()"
      },
      {
        "line": 91,
        "text": "public void RotateLeft()"
      },
      {
        "line": 93,
        "text": "public void RotateRight()"
      },
      {
        "line": 95,
        "text": "public void Cancel()"
      },
      {
        "line": 98,
        "text": "public void ExportServices(ISystemWriteSink sink)"
      },
      {
        "line": 107,
        "text": "private IArtifactInputReceiver TakeSelectOwner()"
      }
    ]
  },
  "IArtifactInputReceiver": {
    "kind": "interface",
    "inherits": "",
    "source": "C:/Users/User/Project_L/Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/ViewSystem/ArtifactInput/IArtifactInputReceiver.cs",
    "fields": [],
    "methods": [
      {
        "line": 6,
        "text": "void PointerMoved(float screenX, float screenY)"
      },
      {
        "line": 7,
        "text": "void SelectPressed(float screenX, float screenY)"
      },
      {
        "line": 8,
        "text": "void SelectReleased(float screenX, float screenY)"
      },
      {
        "line": 11,
        "text": "void SelectCanceled()"
      },
      {
        "line": 14,
        "text": "void RotateLeft()"
      },
      {
        "line": 17,
        "text": "void RotateRight()"
      },
      {
        "line": 20,
        "text": "void Cancel()"
      }
    ]
  },
  "ArtifactBackgroundLayout": {
    "kind": "class",
    "inherits": "",
    "source": "C:/Users/User/Project_L/Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/ViewSystem/ArtifactPopup/ArtifactBackgroundLayout.cs",
    "fields": [
      {
        "line": 9,
        "text": "private readonly Camera _camera"
      },
      {
        "line": 10,
        "text": "private readonly RectTransform _background"
      },
      {
        "line": 11,
        "text": "private readonly ArtifactPopupConfigData _config"
      },
      {
        "line": 12,
        "text": "private Matrix4x4 _view, _projection"
      },
      {
        "line": 13,
        "text": "private bool _valid"
      }
    ],
    "methods": [
      {
        "line": 15,
        "text": "internal ArtifactBackgroundLayout(Camera camera, RectTransform background, ArtifactPopupConfigData config)"
      },
      {
        "line": 22,
        "text": "internal void Update(bool force = false)"
      },
      {
        "line": 39,
        "text": "private Vector3 Corner(float x, float y, Plane plane)"
      }
    ]
  },
  "ArtifactBoardMapper": {
    "kind": "class",
    "inherits": "",
    "source": "C:/Users/User/Project_L/Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/ViewSystem/ArtifactPopup/ArtifactBoardMapper.cs",
    "fields": [
      {
        "line": 11,
        "text": "private readonly Transform _root"
      },
      {
        "line": 12,
        "text": "private readonly ArtifactPopupConfigData _config"
      }
    ],
    "methods": [
      {
        "line": 14,
        "text": "internal ArtifactBoardMapper(Transform root, ArtifactPopupConfigData config)"
      },
      {
        "line": 20,
        "text": "internal bool TryMap(Ray ray, int width, int height, out CellCoord cell)"
      },
      {
        "line": 32,
        "text": "internal Vector3 CellPosition(float x, float y, int width, int height, float heightOffset = 0)"
      },
      {
        "line": 38,
        "text": "internal static bool Contains(CellCoord cell, int width, int height)"
      }
    ]
  },
  "ArtifactDragDisplay": {
    "kind": "class",
    "inherits": "",
    "source": "C:/Users/User/Project_L/Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/ViewSystem/ArtifactPopup/ArtifactDragDisplay.cs",
    "fields": [
      {
        "line": 15,
        "text": "private readonly Transform _root"
      },
      {
        "line": 16,
        "text": "private readonly Transform _storage"
      },
      {
        "line": 17,
        "text": "private readonly ArtifactPopupConfigData _config"
      },
      {
        "line": 18,
        "text": "private readonly ArtifactBoardMapper _mapper"
      },
      {
        "line": 19,
        "text": "private readonly Dictionary<GameObject, CustomPool<GameObject>> _pools"
      },
      {
        "line": 20,
        "text": "private readonly List<UnityEngine.UI.Image> _cells"
      },
      {
        "line": 21,
        "text": "private readonly CustomPool<UnityEngine.UI.Image> _cellPool"
      },
      {
        "line": 22,
        "text": "private readonly RectTransform _canvas"
      },
      {
        "line": 23,
        "text": "private CustomPool<GameObject> _activePool"
      },
      {
        "line": 24,
        "text": "private GameObject _model"
      },
      {
        "line": 25,
        "text": "private GameObject _prefab"
      }
    ],
    "methods": [
      {
        "line": 27,
        "text": "internal ArtifactDragDisplay(Transform root, Transform storage, ArtifactPopupConfigData config)"
      },
      {
        "line": 50,
        "text": "internal void Prepare(GameObject prefab, int cellCount)"
      },
      {
        "line": 64,
        "text": "internal void Begin(GameObject prefab)"
      },
      {
        "line": 76,
        "text": "internal void Show(ArtifactPlacementPreview preview, Rotation4 rotation, int width, int height)"
      },
      {
        "line": 108,
        "text": "internal void ClearPreview()"
      },
      {
        "line": 114,
        "text": "internal void Hide()"
      },
      {
        "line": 128,
        "text": "private void ClearCells()"
      }
    ]
  },
  "ArtifactInteraction": {
    "kind": "class",
    "inherits": "",
    "source": "C:/Users/User/Project_L/Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/ViewSystem/ArtifactPopup/ArtifactInteraction.cs",
    "fields": [
      {
        "line": 9,
        "text": "internal bool IsDragging"
      },
      {
        "line": 10,
        "text": "internal bool IsPlaced"
      },
      {
        "line": 11,
        "text": "internal int ArtifactId"
      },
      {
        "line": 12,
        "text": "internal int InstanceId"
      },
      {
        "line": 13,
        "text": "internal CellCoord Anchor"
      },
      {
        "line": 14,
        "text": "internal Rotation4 Rotation"
      },
      {
        "line": 15,
        "text": "internal uint Revision"
      },
      {
        "line": 16,
        "text": "internal bool HasCandidate"
      },
      {
        "line": 17,
        "text": "private CellCoord _grabOffset"
      },
      {
        "line": 18,
        "text": "private bool _previewDirty"
      }
    ],
    "methods": [
      {
        "line": 20,
        "text": "internal void BeginPool(int artifactId)"
      },
      {
        "line": 27,
        "text": "internal void BeginPlaced(int artifactId, int instanceId, CellCoord anchor, Rotation4 rotation, CellCoord grabbedCell)"
      },
      {
        "line": 36,
        "text": "internal void SetCandidate(CellCoord pointerCell, bool usable)"
      },
      {
        "line": 46,
        "text": "internal void Rotate(int steps)"
      },
      {
        "line": 54,
        "text": "internal void InvalidateCandidate()"
      },
      {
        "line": 60,
        "text": "internal bool TryTakePreview(out ArtifactPreviewRequest request)"
      },
      {
        "line": 71,
        "text": "internal ArtifactEditRequest Release(bool overBoard, bool overReturnArea)"
      },
      {
        "line": 83,
        "text": "internal void Cancel()"
      }
    ]
  },
  "ArtifactPoolItemView": {
    "kind": "class",
    "inherits": "MonoBehaviour",
    "source": "C:/Users/User/Project_L/Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/ViewSystem/ArtifactPopup/ArtifactPoolItemView.cs",
    "fields": [
      {
        "line": 16,
        "text": "private readonly List<Image> _shapeCells"
      },
      {
        "line": 17,
        "text": "private CustomPool<Image> _shapePool"
      }
    ],
    "methods": [
      {
        "line": 20,
        "text": "internal void DrawShape(IReadOnlyList<CellCoord> cells)"
      },
      {
        "line": 56,
        "text": "internal void ClearShape()"
      }
    ]
  },
  "ArtifactPopupItem": {
    "kind": "struct",
    "inherits": "",
    "source": "C:/Users/User/Project_L/Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/ViewSystem/ArtifactPopup/ArtifactPopupDisplay.cs",
    "fields": [
      {
        "line": 11,
        "text": "internal int ArtifactId"
      },
      {
        "line": 12,
        "text": "internal int InstanceId"
      },
      {
        "line": 14,
        "text": "internal IReadOnlyList<CellCoord> Cells"
      },
      {
        "line": 16,
        "text": "internal Sprite Icon"
      },
      {
        "line": 17,
        "text": "internal GameObject ModelPrefab"
      },
      {
        "line": 18,
        "text": "internal Rotation4 Rotation"
      },
      {
        "line": 19,
        "text": "internal string DisplayName"
      },
      {
        "line": 20,
        "text": "internal string Description"
      }
    ],
    "methods": [
      {
        "line": 22,
        "text": "internal ArtifactPopupItem(int artifactId, int instanceId, IReadOnlyList<CellCoord> cells, Sprite icon, GameObject modelPrefab = null, Rotation4 rotation = Rotation4.R0, string displayName = \"\", string description = \"\")"
      }
    ]
  },
  "ArtifactPopupBoard": {
    "kind": "class",
    "inherits": "",
    "source": "C:/Users/User/Project_L/Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/ViewSystem/ArtifactPopup/ArtifactPopupDisplay.cs",
    "fields": [
      {
        "line": 39,
        "text": "internal int Width"
      },
      {
        "line": 40,
        "text": "internal int Height"
      },
      {
        "line": 41,
        "text": "internal IReadOnlyList<ArtifactPopupItem> PlacedItems"
      },
      {
        "line": 42,
        "text": "internal IReadOnlyList<ArtifactPopupItem> PoolItems"
      }
    ],
    "methods": [
      {
        "line": 44,
        "text": "internal ArtifactPopupBoard(int width, int height, IReadOnlyList<ArtifactPopupItem> placedItems, IReadOnlyList<ArtifactPopupItem> poolItems)"
      }
    ]
  },
  "ArtifactPopupText": {
    "kind": "class",
    "inherits": "",
    "source": "C:/Users/User/Project_L/Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/ViewSystem/ArtifactPopup/ArtifactPopupText.cs",
    "fields": [
      {
        "line": 9,
        "text": "internal const string Waiting"
      }
    ],
    "methods": [
      {
        "line": 12,
        "text": "internal static string ForEdit(ArtifactEditRequest request)"
      },
      {
        "line": 26,
        "text": "internal static string ForFailure(ArtifactActionFailureReason reason)"
      }
    ]
  },
  "ArtifactPopupView": {
    "kind": "class",
    "inherits": "MonoBehaviour, IArtifactPopupPresenter",
    "source": "C:/Users/User/Project_L/Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/ViewSystem/ArtifactPopup/ArtifactPopupView.cs",
    "fields": [
      {
        "line": 17,
        "text": "private Transform worldRoot"
      },
      {
        "line": 18,
        "text": "private GameObject backgroundRoot"
      },
      {
        "line": 19,
        "text": "private GameObject stageMapRoot"
      },
      {
        "line": 20,
        "text": "private GameObject cellPrefab"
      },
      {
        "line": 21,
        "text": "private readonly Dictionary<GameObject, CustomPool<GameObject>> _modelPools"
      },
      {
        "line": 23,
        "text": "private Transform _storage"
      },
      {
        "line": 24,
        "text": "private ArtifactPopupConfigData _config"
      },
      {
        "line": 25,
        "text": "private bool _suppressedMap"
      },
      {
        "line": 26,
        "text": "private ArtifactBoardMapper _mapper"
      },
      {
        "line": 27,
        "text": "private ArtifactDragDisplay _drag"
      },
      {
        "line": 28,
        "text": "private ArtifactBackgroundLayout _backgroundLayout"
      },
      {
        "line": 29,
        "text": "private readonly Dictionary<int, GameObject> _placedModels"
      },
      {
        "line": 30,
        "text": "internal Transform WorldRoot"
      },
      {
        "line": 31,
        "text": "internal int BorrowedCount"
      },
      {
        "line": 39,
        "text": "internal ArtifactPopupConfigData Configuration"
      }
    ],
    "methods": [
      {
        "line": 22,
        "text": "private readonly List<(CustomPool<GameObject> pool, GameObject view)"
      },
      {
        "line": 33,
        "text": "internal void Configure(ArtifactPopupConfigData config)"
      },
      {
        "line": 40,
        "text": "internal void ConnectCamera(Camera camera)"
      },
      {
        "line": 45,
        "text": "internal void UpdateBackground(bool force = false)"
      },
      {
        "line": 46,
        "text": "internal void BeginDrag(GameObject prefab, int instanceId, bool placed)"
      },
      {
        "line": 51,
        "text": "internal void ClearPreview()"
      },
      {
        "line": 52,
        "text": "internal void ShowPreview(ArtifactPlacementPreview preview, ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common.Rotation4 rotation, int width, int height)"
      },
      {
        "line": 54,
        "text": "internal void EndDrag()"
      },
      {
        "line": 59,
        "text": "internal void Validate()"
      },
      {
        "line": 66,
        "text": "public void Show()"
      },
      {
        "line": 77,
        "text": "public void Hide()"
      },
      {
        "line": 89,
        "text": "internal void Render(ArtifactPopupBoard board)"
      },
      {
        "line": 116,
        "text": "private GameObject PlaceModel(GameObject prefab, ArtifactPopupBoard board, float x, float y, float height, Quaternion rotation, float scale)"
      },
      {
        "line": 133,
        "text": "public void ClearBoard()"
      },
      {
        "line": 160,
        "text": "private void OnDestroy()"
      }
    ]
  },
  "ArtifactSceneRoot": {
    "kind": "class",
    "inherits": "MonoBehaviour",
    "source": "C:/Users/User/Project_L/Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/ViewSystem/ArtifactPopup/ArtifactSceneRoot.cs",
    "fields": [
      {
        "line": 18,
        "text": "private ArtifactPopupView popupView"
      },
      {
        "line": 20,
        "text": "private ArtifactPopupUI popupUI"
      },
      {
        "line": 22,
        "text": "private Camera outputCamera"
      },
      {
        "line": 24,
        "text": "internal ArtifactPopupView PopupView"
      },
      {
        "line": 25,
        "text": "internal ArtifactPopupUI PopupUI"
      },
      {
        "line": 26,
        "text": "internal Camera OutputCamera"
      }
    ],
    "methods": [
      {
        "line": 29,
        "text": "internal void Validate()"
      }
    ]
  },
  "ArtifactViewSystem": {
    "kind": "class",
    "inherits": "ISystemInitData<IHasViewQuery>, ISystemPostBindService, ISystemInitialize, ISystemTick, ISystemShutdown, IExportSystemServices, IArtifactInputReceiver, IArtifactPopupHost",
    "source": "C:/Users/User/Project_L/Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/ViewSystem/ArtifactPopup/ArtifactViewSystem.cs",
    "fields": [
      {
        "line": 47,
        "text": "internal const int AttachPriority"
      },
      {
        "line": 48,
        "text": "internal const int DetachPriority"
      },
      {
        "line": 50,
        "text": "private readonly RuntimeFaultBoundary _faults"
      },
      {
        "line": 51,
        "text": "private readonly Action<ScriptableRenderContext, Camera> _cameraRenderingAction"
      },
      {
        "line": 52,
        "text": "private readonly Camera.CameraCallback _cameraPreCullAction"
      },
      {
        "line": 53,
        "text": "private ILodingRegisterService _loadingRegister"
      },
      {
        "line": 54,
        "text": "private IInputLayerService _layers"
      },
      {
        "line": 55,
        "text": "private IUISystemService _ui"
      },
      {
        "line": 56,
        "text": "private IArtifactBufferReadSink _board"
      },
      {
        "line": 57,
        "text": "private ArtifactEditScheduler _scheduler"
      },
      {
        "line": 58,
        "text": "private Func<int, Sprite> _findIcon"
      },
      {
        "line": 59,
        "text": "private Func<int, ArtifactViewData> _findVisual"
      },
      {
        "line": 60,
        "text": "private ArtifactPopupConfigData _layout"
      },
      {
        "line": 61,
        "text": "private IArtifactInputSlot _inputSlot"
      },
      {
        "line": 62,
        "text": "private ArtifactBoardMapper _mapper"
      },
      {
        "line": 63,
        "text": "private readonly ArtifactInteraction _interaction"
      },
      {
        "line": 64,
        "text": "private Vector2 _pointer"
      },
      {
        "line": 65,
        "text": "private ArtifactPreviewRequest _previewRequest"
      },
      {
        "line": 66,
        "text": "private ArtifactPreviewRequest _shownPreview"
      },
      {
        "line": 67,
        "text": "private bool _inputBound"
      },
      {
        "line": 68,
        "text": "private bool _previewStatusShown"
      },
      {
        "line": 70,
        "text": "private ArtifactSceneRoot _root"
      },
      {
        "line": 71,
        "text": "private IArtifactPopupPresenter _presenter"
      },
      {
        "line": 72,
        "text": "private IArtifactPopupScreen _screen"
      },
      {
        "line": 73,
        "text": "private bool _isOpen"
      },
      {
        "line": 74,
        "text": "private ArtifactBufferSnapshot _drawnSnapshot"
      },
      {
        "line": 75,
        "text": "private bool _isShowingWaiting"
      },
      {
        "line": 76,
        "text": "private ArtifactEditRequest _reportedEdit"
      },
      {
        "line": 86,
        "text": "internal ArtifactSceneRoot Root"
      },
      {
        "line": 88,
        "text": "internal bool IsOpen"
      },
      {
        "line": 91,
        "text": "internal ArtifactEditScheduler Scheduler"
      },
      {
        "line": 218,
        "text": "public bool CanOpen"
      }
    ],
    "methods": [
      {
        "line": 78,
        "text": "internal ArtifactViewSystem(RuntimeFaultBoundary faults)"
      },
      {
        "line": 93,
        "text": "public void InitData(IHasViewQuery data)"
      },
      {
        "line": 107,
        "text": "public void PostBindService(ISystemServiceStoreReader resolver)"
      },
      {
        "line": 126,
        "text": "public void Initialize()"
      },
      {
        "line": 136,
        "text": "public void Tick(float deltaTime)"
      },
      {
        "line": 154,
        "text": "public void Shutdown()"
      },
      {
        "line": 159,
        "text": "public void ExportServices(ISystemWriteSink sink)"
      },
      {
        "line": 168,
        "text": "internal void BindIcons(Func<int, Sprite> findIcon)"
      },
      {
        "line": 174,
        "text": "internal void BindServices(ILodingRegisterService loadingRegister, IInputLayerService layers, IUISystemService ui, IArtifactBufferReadSink board, ArtifactEditScheduler scheduler)"
      },
      {
        "line": 185,
        "text": "internal void AttachScene(ArtifactSceneRoot root)"
      },
      {
        "line": 201,
        "text": "internal void AttachPresenter(IArtifactPopupPresenter presenter, IArtifactPopupScreen screen)"
      },
      {
        "line": 221,
        "text": "public void PopupOpened()"
      },
      {
        "line": 224,
        "text": "public void PopupHidden()"
      },
      {
        "line": 230,
        "text": "internal void Open()"
      },
      {
        "line": 253,
        "text": "internal void Close()"
      },
      {
        "line": 278,
        "text": "internal void DetachFromScene()"
      },
      {
        "line": 293,
        "text": "internal void Detach()"
      },
      {
        "line": 327,
        "text": "private void Refresh()"
      },
      {
        "line": 359,
        "text": "private ArtifactPopupBoard BuildBoard(ArtifactBufferSnapshot snapshot)"
      },
      {
        "line": 365,
        "text": "private ArtifactPopupItem[] ToItems(IReadOnlyList<ArtifactData> source)"
      },
      {
        "line": 379,
        "text": "private static bool IsFinished(ArtifactEditRequest request)"
      },
      {
        "line": 384,
        "text": "private void ShowDetail(int artifactId)"
      },
      {
        "line": 390,
        "text": "public void PointerMoved(float screenX, float screenY)"
      },
      {
        "line": 397,
        "text": "public void SelectPressed(float screenX, float screenY)"
      },
      {
        "line": 424,
        "text": "public void SelectReleased(float screenX, float screenY)"
      },
      {
        "line": 438,
        "text": "public void RotateLeft()"
      },
      {
        "line": 439,
        "text": "public void RotateRight()"
      },
      {
        "line": 440,
        "text": "public void SelectCanceled()"
      },
      {
        "line": 443,
        "text": "public void Cancel()"
      },
      {
        "line": 473,
        "text": "private void Rotate(int steps)"
      },
      {
        "line": 482,
        "text": "private void UpdateCandidate()"
      },
      {
        "line": 493,
        "text": "private bool TryPointerCell(out CellCoord cell)"
      },
      {
        "line": 502,
        "text": "private bool IsButtonOrDetail(Vector2 point)"
      },
      {
        "line": 504,
        "text": "private bool AllowsInteraction()"
      },
      {
        "line": 510,
        "text": "private void ShowCompletedPreview()"
      },
      {
        "line": 521,
        "text": "private void InvalidateDisplayedPreview()"
      },
      {
        "line": 529,
        "text": "private void CancelDrag()"
      },
      {
        "line": 538,
        "text": "private void ClearPreviewStatus()"
      },
      {
        "line": 545,
        "text": "private void ReleaseInput()"
      },
      {
        "line": 553,
        "text": "internal void StopInput()"
      },
      {
        "line": 561,
        "text": "private void OnCameraRendering(ScriptableRenderContext context, Camera camera)"
      },
      {
        "line": 563,
        "text": "private void OnCameraPreCull(Camera camera)"
      },
      {
        "line": 571,
        "text": "private void AttachFromScene()"
      }
    ]
  },
  "IArtifactPopupHost": {
    "kind": "interface",
    "inherits": "",
    "source": "C:/Users/User/Project_L/Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/ViewSystem/ArtifactPopup/IArtifactPopupHost.cs",
    "fields": [
      {
        "line": 10,
        "text": "bool CanOpen"
      }
    ],
    "methods": [
      {
        "line": 13,
        "text": "void PopupOpened()"
      },
      {
        "line": 16,
        "text": "void PopupHidden()"
      }
    ]
  },
  "IArtifactPopupPresenter": {
    "kind": "interface",
    "inherits": "",
    "source": "C:/Users/User/Project_L/Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/ViewSystem/ArtifactPopup/IArtifactPopupPresenter.cs",
    "fields": [],
    "methods": [
      {
        "line": 10,
        "text": "void Show()"
      },
      {
        "line": 13,
        "text": "void Hide()"
      },
      {
        "line": 16,
        "text": "void Render(ArtifactPopupBoard board)"
      },
      {
        "line": 19,
        "text": "void ClearBoard()"
      }
    ]
  },
  "IArtifactPopupScreen": {
    "kind": "interface",
    "inherits": "",
    "source": "C:/Users/User/Project_L/Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/ViewSystem/ArtifactPopup/IArtifactPopupScreen.cs",
    "fields": [],
    "methods": [
      {
        "line": 12,
        "text": "void Connect(IArtifactPopupHost host)"
      },
      {
        "line": 15,
        "text": "void Disconnect()"
      },
      {
        "line": 18,
        "text": "void ShowStatus(string message)"
      },
      {
        "line": 21,
        "text": "void ShowDetail(int artifactId)"
      },
      {
        "line": 24,
        "text": "bool TryPickPool(Vector2 screen, out int artifactId)"
      },
      {
        "line": 27,
        "text": "bool IsReturnArea(Vector2 screen)"
      },
      {
        "line": 30,
        "text": "bool IsBlockingArea(Vector2 screen)"
      }
    ]
  },
  "GameBootstrap": {
    "kind": "class",
    "inherits": "MonoBehaviour",
    "source": "C:/Users/User/Project_L/Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/ViewSystem/Boot/GameBootstrap.cs",
    "fields": [
      {
        "line": 57,
        "text": "private static GameBootstrap _activeInstance"
      },
      {
        "line": 59,
        "text": "private DatabaseCatalogSO databaseCatalog"
      },
      {
        "line": 61,
        "text": "private GameObject globalUICanvasPrefab"
      },
      {
        "line": 62,
        "text": "private StartLifeCycle startLifeCycle"
      },
      {
        "line": 63,
        "text": "private CombatExecutionMode executionMode"
      },
      {
        "line": 70,
        "text": "private bool useDevProgress"
      },
      {
        "line": 72,
        "text": "private DevBattleDefinition devBattleDefinition"
      },
      {
        "line": 74,
        "text": "private int devLoadoutKey"
      },
      {
        "line": 76,
        "text": "private int devChapterKey"
      },
      {
        "line": 78,
        "text": "private int devNodeId"
      },
      {
        "line": 81,
        "text": "private int devMapWidth"
      },
      {
        "line": 82,
        "text": "private int devMapHeight"
      },
      {
        "line": 83,
        "text": "private int devPlayerDeploymentDepth"
      },
      {
        "line": 84,
        "text": "private int devEnemyDeploymentDepth"
      },
      {
        "line": 88,
        "text": "private RuntimeFaultBoundary _faults"
      },
      {
        "line": 89,
        "text": "private FrameworkRuntime _runtime"
      },
      {
        "line": 90,
        "text": "private bool _isRunning"
      },
      {
        "line": 91,
        "text": "private bool _shutdownStarted"
      },
      {
        "line": 92,
        "text": "private CombatEntryRequest _bootEntry"
      },
      {
        "line": 93,
        "text": "private GameObject _globalUICanvas"
      },
      {
        "line": 94,
        "text": "private IUISystemService _ui"
      }
    ],
    "methods": [
      {
        "line": 98,
        "text": "private static void ResetActiveInstance()"
      },
      {
        "line": 103,
        "text": "private void Awake()"
      },
      {
        "line": 116,
        "text": "private void Start()"
      },
      {
        "line": 153,
        "text": "private void Update()"
      },
      {
        "line": 181,
        "text": "private void CreateGlobalUI()"
      },
      {
        "line": 190,
        "text": "private void BindUILifeCycle()"
      },
      {
        "line": 201,
        "text": "private void BindLoadedUI()"
      },
      {
        "line": 203,
        "text": "private void OpenCombatUI()"
      },
      {
        "line": 205,
        "text": "private void OnApplicationQuit()"
      },
      {
        "line": 210,
        "text": "private void OnDestroy()"
      },
      {
        "line": 216,
        "text": "private void ShutdownOnce()"
      },
      {
        "line": 253,
        "text": "private IBattleInitializationSource CreateDevBattleSource()"
      },
      {
        "line": 269,
        "text": "private void PrepareDevProgress()"
      },
      {
        "line": 296,
        "text": "private void RequestInitialLifeCycle()"
      },
      {
        "line": 321,
        "text": "private void ApplyEditorOverrides()"
      },
      {
        "line": 354,
        "text": "private static void ValidateScenes()"
      },
      {
        "line": 366,
        "text": "private static void RequireBuildScene(int buildIndex, string expectedPath)"
      },
      {
        "line": 374,
        "text": "private static LifeCycleKey ToLifeCycleKey(StartLifeCycle start)"
      }
    ]
  },
  "GameBootstrapEditorOverrides": {
    "kind": "class",
    "inherits": "",
    "source": "C:/Users/User/Project_L/Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/ViewSystem/Boot/GameBootstrapEditorOverrides.cs",
    "fields": [
      {
        "line": 18,
        "text": "private const string Prefix"
      }
    ],
    "methods": []
  },
  "AttackTargetingInteraction": {
    "kind": "class",
    "inherits": "",
    "source": "C:/Users/User/Project_L/Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/ViewSystem/Combat/AttackTargetingInteraction.cs",
    "fields": [
      {
        "line": 25,
        "text": "private readonly ICombatViewBufferWriteSink _input"
      },
      {
        "line": 26,
        "text": "private readonly ICombatViewBufferReadSink _requests"
      },
      {
        "line": 27,
        "text": "private readonly ICombatPointerPicker _picker"
      },
      {
        "line": 28,
        "text": "private readonly ICombatInteractionFeedback _feedback"
      },
      {
        "line": 29,
        "text": "private readonly CombatCommandGate _gate"
      },
      {
        "line": 30,
        "text": "private readonly CombatHoverTracker _hover"
      },
      {
        "line": 32,
        "text": "private bool _active"
      },
      {
        "line": 33,
        "text": "private EntityId _actor"
      },
      {
        "line": 34,
        "text": "private EntityId _recordActor"
      },
      {
        "line": 37,
        "text": "private int _rangeRequestNumber"
      },
      {
        "line": 38,
        "text": "private bool _rangeAnswered"
      },
      {
        "line": 39,
        "text": "private AttackAim[] _attackAims"
      },
      {
        "line": 40,
        "text": "private readonly Dictionary<CellCoord, List<AttackBlockedContact>> _rangeBlockers"
      },
      {
        "line": 41,
        "text": "private bool _canAffordAttack"
      },
      {
        "line": 44,
        "text": "private int _hoverRequestNumber"
      },
      {
        "line": 62,
        "text": "internal bool IsActive"
      },
      {
        "line": 65,
        "text": "internal bool HasRangeAnswer"
      }
    ],
    "methods": [
      {
        "line": 46,
        "text": "internal AttackTargetingInteraction( ICombatViewBufferWriteSink input, ICombatViewBufferReadSink requests, ICombatPointerPicker picker, ICombatInteractionFeedback feedback, CombatCommandGate gate)"
      },
      {
        "line": 71,
        "text": "internal bool CanEnter(in CombatInputFacts facts, out EntityId actor)"
      },
      {
        "line": 78,
        "text": "internal void Enter(in CombatInputFacts facts, EntityId actor)"
      },
      {
        "line": 87,
        "text": "internal void PointerMoved(in CombatInputFacts facts, IReadOnlyList<CharacterData> characters, float screenX, float screenY)"
      },
      {
        "line": 109,
        "text": "internal void SelectPressed(in CombatInputFacts facts, IReadOnlyList<CharacterData> characters, float screenX, float screenY)"
      },
      {
        "line": 121,
        "text": "internal void SelectReleased(in CombatInputFacts facts, IReadOnlyList<CharacterData> characters, float screenX, float screenY)"
      },
      {
        "line": 131,
        "text": "internal bool PreviewAnswered(in CombatInputFacts facts, IReadOnlyList<CharacterData> characters)"
      },
      {
        "line": 158,
        "text": "internal void AfterResult(in CombatInputFacts facts)"
      },
      {
        "line": 177,
        "text": "internal void Exit()"
      },
      {
        "line": 192,
        "text": "internal void ClearRecordActor()"
      },
      {
        "line": 198,
        "text": "private void RequestRange(in CombatInputFacts facts)"
      },
      {
        "line": 207,
        "text": "private void ApplyRangeAnswer(CombatPreviewResult answer)"
      },
      {
        "line": 233,
        "text": "private void ApplyAttackAnswer(AttackPreview attack)"
      },
      {
        "line": 250,
        "text": "private void RequestHoverForCell(in CombatInputFacts facts, IReadOnlyList<CharacterData> characters, CellCoord cell)"
      },
      {
        "line": 274,
        "text": "private void SubmitTarget(in CombatInputFacts facts, IReadOnlyList<CharacterData> characters, CellCoord cell)"
      },
      {
        "line": 290,
        "text": "private bool CanChooseTarget(in CombatInputFacts facts)"
      },
      {
        "line": 299,
        "text": "private bool CanAffordAttack(in CombatInputFacts facts)"
      },
      {
        "line": 311,
        "text": "private AttackAim? AimFor(in CombatInputFacts facts, IReadOnlyList<CharacterData> characters, CellCoord cell)"
      },
      {
        "line": 338,
        "text": "private AttackAim? NearestDirectionAim(CellCoord origin, CellCoord cell)"
      },
      {
        "line": 368,
        "text": "private static bool TryGetDirectionVector(AttackDirection direction, out int x, out int y)"
      },
      {
        "line": 384,
        "text": "private void ResetRangeAnswer()"
      },
      {
        "line": 396,
        "text": "private void InvalidateHoverPreview()"
      },
      {
        "line": 404,
        "text": "private void ClearOwnedPreview()"
      },
      {
        "line": 414,
        "text": "private bool IsModeStillValid(in CombatInputFacts facts)"
      },
      {
        "line": 422,
        "text": "private EntityId ResolveActor(in CombatInputFacts facts)"
      }
    ]
  },
  "CombatAbandonController": {
    "kind": "class",
    "inherits": "",
    "source": "C:/Users/User/Project_L/Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/ViewSystem/Combat/CombatAbandonController.cs",
    "fields": [
      {
        "line": 18,
        "text": "private readonly CombatExecutionMode _executionMode"
      },
      {
        "line": 19,
        "text": "private readonly RuntimeFaultBoundary _faults"
      },
      {
        "line": 20,
        "text": "private readonly Func<CombatInputFacts> _readFacts"
      },
      {
        "line": 22,
        "text": "private readonly Func<bool> _canAct"
      },
      {
        "line": 24,
        "text": "private readonly Action _cancelInputPresentation"
      },
      {
        "line": 26,
        "text": "private readonly Action _onOpen"
      },
      {
        "line": 27,
        "text": "private readonly Action _onConfirm"
      },
      {
        "line": 28,
        "text": "private readonly Action _onCancel"
      },
      {
        "line": 29,
        "text": "private ICombatAbandonDisplay _display"
      },
      {
        "line": 30,
        "text": "private ICombatAbandonRequest _request"
      },
      {
        "line": 50,
        "text": "internal bool IsConfirmOpen"
      },
      {
        "line": 52,
        "text": "private bool IsLive"
      }
    ],
    "methods": [
      {
        "line": 32,
        "text": "internal CombatAbandonController( CombatExecutionMode executionMode, RuntimeFaultBoundary faults, Func<CombatInputFacts> readFacts, Func<bool> canAct, Action cancelInputPresentation)"
      },
      {
        "line": 58,
        "text": "internal void Attach(ICombatAbandonDisplay display, ICombatAbandonRequest request)"
      },
      {
        "line": 72,
        "text": "internal void Sync()"
      },
      {
        "line": 84,
        "text": "internal void Close()"
      },
      {
        "line": 94,
        "text": "internal void Stop()"
      },
      {
        "line": 105,
        "text": "internal void Detach()"
      },
      {
        "line": 112,
        "text": "private bool CanRequestNow()"
      },
      {
        "line": 118,
        "text": "private void Open()"
      },
      {
        "line": 139,
        "text": "private void Confirm()"
      },
      {
        "line": 158,
        "text": "private void Cancel()"
      }
    ]
  },
  "CombatCharacterQueries": {
    "kind": "class",
    "inherits": "",
    "source": "C:/Users/User/Project_L/Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/ViewSystem/Combat/CombatCharacterQueries.cs",
    "fields": [],
    "methods": [
      {
        "line": 15,
        "text": "internal static CharacterData Find(IReadOnlyList<CharacterData> characters, EntityId id)"
      },
      {
        "line": 26,
        "text": "internal static CharacterData FindPlayerInArea(IReadOnlyList<CharacterData> characters, EntityId id, CharacterRuntimeArea area)"
      },
      {
        "line": 34,
        "text": "internal static CharacterData FindPlayerAt(IReadOnlyList<CharacterData> characters, CellCoord cell)"
      },
      {
        "line": 41,
        "text": "internal static CharacterData FindAnyAt(IReadOnlyList<CharacterData> characters, CellCoord cell)"
      }
    ]
  },
  "CombatCommandGate": {
    "kind": "class",
    "inherits": "",
    "source": "C:/Users/User/Project_L/Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/ViewSystem/Combat/CombatCommandGate.cs",
    "fields": [
      {
        "line": 19,
        "text": "private readonly ICombatViewBufferWriteSink _input"
      },
      {
        "line": 20,
        "text": "private PendingInputCommand? _pending"
      },
      {
        "line": 28,
        "text": "internal bool HasPending"
      },
      {
        "line": 31,
        "text": "internal PendingInputCommand? Pending"
      }
    ],
    "methods": [
      {
        "line": 22,
        "text": "internal CombatCommandGate(ICombatViewBufferWriteSink input)"
      },
      {
        "line": 37,
        "text": "internal int TrySubmit(in CombatInputFacts facts, CombatViewCommandData command)"
      },
      {
        "line": 49,
        "text": "internal bool SubmitPending(in CombatInputFacts facts, CombatViewCommandData command)"
      },
      {
        "line": 63,
        "text": "internal void Resolve(CombatResult result)"
      },
      {
        "line": 75,
        "text": "internal void Clear()"
      }
    ]
  },
  "CombatDevLayerController": {
    "kind": "class",
    "inherits": "",
    "source": "C:/Users/User/Project_L/Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/ViewSystem/Combat/CombatDevLayerController.cs",
    "fields": [
      {
        "line": 20,
        "text": "private readonly CombatExecutionMode _executionMode"
      },
      {
        "line": 21,
        "text": "private readonly RuntimeFaultBoundary _faults"
      },
      {
        "line": 22,
        "text": "private readonly Func<CombatInputFacts> _readFacts"
      },
      {
        "line": 24,
        "text": "private readonly Func<bool> _canReceive"
      },
      {
        "line": 26,
        "text": "private readonly Action _cancelPresentation"
      },
      {
        "line": 28,
        "text": "private readonly Action _onDeployment"
      },
      {
        "line": 29,
        "text": "private readonly Action _onCombat"
      },
      {
        "line": 30,
        "text": "private ICombatDevLayerDisplay _display"
      },
      {
        "line": 31,
        "text": "private IInputLayerService _layers"
      }
    ],
    "methods": [
      {
        "line": 33,
        "text": "internal CombatDevLayerController( CombatExecutionMode executionMode, RuntimeFaultBoundary faults, Func<CombatInputFacts> readFacts, Func<bool> canReceive, Action cancelPresentation)"
      },
      {
        "line": 50,
        "text": "internal void Attach(ICombatDevLayerDisplay display, IInputLayerService layers)"
      },
      {
        "line": 61,
        "text": "internal void Hide()"
      },
      {
        "line": 67,
        "text": "internal void Detach()"
      },
      {
        "line": 77,
        "text": "private void Receive(StagePhase stage)"
      }
    ]
  },
  "CombatDisplayValues": {
    "kind": "struct",
    "inherits": "",
    "source": "C:/Users/User/Project_L/Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/ViewSystem/Combat/CombatDisplayValues.cs",
    "fields": [
      {
        "line": 25,
        "text": "internal int StageKey"
      },
      {
        "line": 27,
        "text": "internal CombatStateData State"
      },
      {
        "line": 29,
        "text": "internal IReadOnlyList<CharacterData> Characters"
      },
      {
        "line": 35,
        "text": "internal int StageGold"
      },
      {
        "line": 37,
        "text": "internal IReadOnlyList<CombatTimelineEntrySnapshot> Timeline"
      },
      {
        "line": 40,
        "text": "internal int RemainingDraws"
      }
    ],
    "methods": [
      {
        "line": 15,
        "text": "internal CombatDisplayValues(CombatStateData state, IReadOnlyList<CharacterData> characters, IReadOnlyList<CombatTimelineEntrySnapshot> timeline = null, int stageKey = 0)"
      }
    ]
  },
  "CombatHoverTracker": {
    "kind": "class",
    "inherits": "",
    "source": "C:/Users/User/Project_L/Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/ViewSystem/Combat/CombatHoverTracker.cs",
    "fields": [
      {
        "line": 12,
        "text": "private readonly ICombatInteractionFeedback _feedback"
      },
      {
        "line": 13,
        "text": "private bool _hasHover"
      },
      {
        "line": 14,
        "text": "private CellCoord _cell"
      },
      {
        "line": 15,
        "text": "private CombatHoverState _state"
      },
      {
        "line": 22,
        "text": "internal bool HasHover"
      },
      {
        "line": 24,
        "text": "internal CellCoord Cell"
      }
    ],
    "methods": [
      {
        "line": 17,
        "text": "internal CombatHoverTracker(ICombatInteractionFeedback feedback)"
      },
      {
        "line": 26,
        "text": "internal void Set(CellCoord cell, CombatHoverState state)"
      },
      {
        "line": 37,
        "text": "internal void SetStateIfHovered(CombatHoverState state)"
      },
      {
        "line": 44,
        "text": "internal void Clear()"
      }
    ]
  },
  "CombatInputFacts": {
    "kind": "struct",
    "inherits": "",
    "source": "C:/Users/User/Project_L/Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/ViewSystem/Combat/CombatInputFacts.cs",
    "fields": [
      {
        "line": 59,
        "text": "internal bool IsBattleDisplayed"
      },
      {
        "line": 61,
        "text": "internal bool HasBattle"
      },
      {
        "line": 64,
        "text": "internal CombatFlowPhase Phase"
      },
      {
        "line": 67,
        "text": "internal StagePhase StagePhase"
      },
      {
        "line": 70,
        "text": "internal bool HasCompletion"
      },
      {
        "line": 73,
        "text": "internal CombatExecutionMode ExecutionMode"
      },
      {
        "line": 76,
        "text": "internal InputLayer CurrentLayer"
      },
      {
        "line": 79,
        "text": "internal int LatestResultSequence"
      },
      {
        "line": 82,
        "text": "internal int PresentedResultSequence"
      },
      {
        "line": 85,
        "text": "internal bool IsFinalizing"
      },
      {
        "line": 88,
        "text": "internal EntityId ActiveActorId"
      },
      {
        "line": 91,
        "text": "internal bool HasUnhandledCommand"
      },
      {
        "line": 94,
        "text": "internal bool HasPendingCommand"
      },
      {
        "line": 97,
        "text": "internal bool IsPresenting"
      },
      {
        "line": 100,
        "text": "internal bool IsInputAvailable"
      },
      {
        "line": 106,
        "text": "internal bool IsPathTarget"
      },
      {
        "line": 111,
        "text": "internal CombatPreviewResult PreviewAnswer"
      }
    ],
    "methods": [
      {
        "line": 20,
        "text": "internal CombatInputFacts( bool isBattleDisplayed, bool hasBattle, CombatFlowPhase phase, StagePhase stagePhase, bool hasCompletion, CombatExecutionMode executionMode = CombatExecutionMode.Live, InputLayer currentLayer = default, int latestResultSequence = 0, int presentedResultSequence = 0, bool isFinalizing = false, EntityId activeActorId = default, bool hasUnhandledCommand = false, CombatPreviewResult previewAnswer = null, bool hasPendingCommand = false, bool isPresenting = false, bool isInputAvailable = false, bool isPathTarget = false)"
      }
    ]
  },
  "CombatInputPolicy": {
    "kind": "class",
    "inherits": "",
    "source": "C:/Users/User/Project_L/Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/ViewSystem/Combat/CombatInputPolicy.cs",
    "fields": [],
    "methods": [
      {
        "line": 25,
        "text": "internal static bool IsTerminal(in CombatInputFacts facts)"
      },
      {
        "line": 34,
        "text": "internal static InputLayer GetDesiredLayer(in CombatInputFacts facts)"
      },
      {
        "line": 49,
        "text": "internal static bool CanSubmit(in CombatInputFacts facts, bool hasPendingCommand, CombatViewCommandKind kind)"
      },
      {
        "line": 70,
        "text": "internal static bool CanPreview(in CombatInputFacts facts, bool hasPendingCommand, CombatViewPreviewKind kind)"
      },
      {
        "line": 89,
        "text": "internal static bool CanEditPath(in CombatInputFacts facts)"
      },
      {
        "line": 101,
        "text": "internal static bool CanRequestAbandon(in CombatInputFacts facts, bool exitIdle)"
      },
      {
        "line": 114,
        "text": "internal static bool CanChangeDevLayer(in CombatInputFacts facts, bool hasPendingCommand)"
      },
      {
        "line": 120,
        "text": "internal static StagePhase StageOf(CombatViewCommandKind kind)"
      },
      {
        "line": 141,
        "text": "internal static StagePhase StageOf(CombatViewPreviewKind kind)"
      },
      {
        "line": 160,
        "text": "internal static InputLayer LayerOf(StagePhase stage)"
      },
      {
        "line": 173,
        "text": "private static bool PassesCommon(in CombatInputFacts facts, bool hasPendingCommand)"
      }
    ]
  },
  "CombatInputReceiver": {
    "kind": "class",
    "inherits": "IStageInputReceiver",
    "source": "C:/Users/User/Project_L/Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/ViewSystem/Combat/CombatInputReceiver.cs",
    "fields": [
      {
        "line": 18,
        "text": "private readonly IStageInputReceiverSlot _slot"
      },
      {
        "line": 19,
        "text": "private readonly RuntimeFaultBoundary _faults"
      },
      {
        "line": 21,
        "text": "private readonly Func<bool> _canReceive"
      },
      {
        "line": 23,
        "text": "private readonly Func<bool> _isConfirmOpen"
      },
      {
        "line": 24,
        "text": "private readonly Action<CombatInputRouter.Request, float, float> _route"
      },
      {
        "line": 41,
        "text": "internal bool IsBound"
      }
    ],
    "methods": [
      {
        "line": 26,
        "text": "internal CombatInputReceiver( IStageInputReceiverSlot slot, RuntimeFaultBoundary faults, Func<bool> canReceive, Func<bool> isConfirmOpen, Action<CombatInputRouter.Request, float, float> route)"
      },
      {
        "line": 44,
        "text": "internal void Bind()"
      },
      {
        "line": 58,
        "text": "internal void Unbind()"
      },
      {
        "line": 74,
        "text": "internal void FocusLost()"
      },
      {
        "line": 76,
        "text": "public void PointerMoved(float screenX, float screenY)"
      },
      {
        "line": 78,
        "text": "public void SelectPressed(float screenX, float screenY)"
      },
      {
        "line": 80,
        "text": "public void SelectReleased(float screenX, float screenY)"
      },
      {
        "line": 82,
        "text": "public void SelectCanceled()"
      },
      {
        "line": 84,
        "text": "public void Cancel()"
      },
      {
        "line": 86,
        "text": "public void EnterCombatRequested()"
      },
      {
        "line": 88,
        "text": "public void DrawRequested()"
      },
      {
        "line": 90,
        "text": "public void EndTurnRequested()"
      },
      {
        "line": 92,
        "text": "public void MoveModeRequested()"
      },
      {
        "line": 94,
        "text": "public void AttackModeRequested()"
      },
      {
        "line": 96,
        "text": "public void RequestReturn()"
      },
      {
        "line": 102,
        "text": "private void Receive(CombatInputRouter.Request request, float screenX, float screenY)"
      }
    ]
  },
  "CombatInputRouter": {
    "kind": "class",
    "inherits": "",
    "source": "C:/Users/User/Project_L/Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/ViewSystem/Combat/CombatInputRouter.cs",
    "fields": [
      {
        "line": 48,
        "text": "private readonly CombatInteraction _interaction"
      },
      {
        "line": 49,
        "text": "private readonly CombatCommandGate _gate"
      },
      {
        "line": 51,
        "text": "private readonly Func<bool, CombatInputFacts> _readFacts"
      },
      {
        "line": 52,
        "text": "private CombatPathInput _path"
      }
    ],
    "methods": [
      {
        "line": 54,
        "text": "internal CombatInputRouter(CombatInteraction interaction, CombatCommandGate gate, Func<bool, CombatInputFacts> readFacts)"
      },
      {
        "line": 69,
        "text": "internal static PointerTarget ResolvePointerTarget(InputLayer layer, bool attackActive, CombatExecutionMode executionMode)"
      },
      {
        "line": 84,
        "text": "internal void SetPathInput(CombatPathInput path)"
      },
      {
        "line": 89,
        "text": "internal void Route(Request request, float screenX, float screenY, IReadOnlyList<CharacterData> characters)"
      },
      {
        "line": 144,
        "text": "private void RoutePointer(Request request, float screenX, float screenY, IReadOnlyList<CharacterData> characters)"
      }
    ]
  },
  "CombatInteraction": {
    "kind": "class",
    "inherits": "",
    "source": "C:/Users/User/Project_L/Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/ViewSystem/Combat/CombatInteraction.cs",
    "fields": [
      {
        "line": 33,
        "text": "private readonly PlacementDragInteraction _placement"
      },
      {
        "line": 34,
        "text": "private readonly AttackTargetingInteraction _attack"
      },
      {
        "line": 51,
        "text": "internal bool IsAttackActive"
      },
      {
        "line": 54,
        "text": "internal DragKind Drag"
      },
      {
        "line": 57,
        "text": "internal TargetMode Mode"
      },
      {
        "line": 60,
        "text": "internal bool HasRangeAnswer"
      }
    ],
    "methods": [
      {
        "line": 36,
        "text": "internal CombatInteraction( ICombatViewBufferWriteSink input, ICombatViewBufferReadSink requests, ICombatPointerPicker picker, ICombatInteractionFeedback feedback, CombatCommandGate gate)"
      },
      {
        "line": 64,
        "text": "internal void PointerMoved(CombatInputRouter.PointerTarget target, in CombatInputFacts facts, IReadOnlyList<CharacterData> characters, float screenX, float screenY)"
      },
      {
        "line": 73,
        "text": "internal void SelectPressed(CombatInputRouter.PointerTarget target, in CombatInputFacts facts, IReadOnlyList<CharacterData> characters, float screenX, float screenY)"
      },
      {
        "line": 82,
        "text": "internal void SelectReleased(CombatInputRouter.PointerTarget target, in CombatInputFacts facts, IReadOnlyList<CharacterData> characters, float screenX, float screenY)"
      },
      {
        "line": 96,
        "text": "internal void SelectCanceled()"
      },
      {
        "line": 102,
        "text": "internal void Cancel()"
      },
      {
        "line": 114,
        "text": "internal void AttackModeRequested(in CombatInputFacts facts)"
      },
      {
        "line": 127,
        "text": "internal void PreviewAnswered(in CombatInputFacts facts, IReadOnlyList<CharacterData> characters)"
      },
      {
        "line": 142,
        "text": "internal void AfterResult(in CombatInputFacts facts, IReadOnlyList<CharacterData> characters)"
      },
      {
        "line": 152,
        "text": "internal void CancelPresentation()"
      },
      {
        "line": 162,
        "text": "internal void Stop()"
      }
    ]
  },
  "CombatMovementInteraction": {
    "kind": "class",
    "inherits": "",
    "source": "C:/Users/User/Project_L/Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/ViewSystem/Combat/CombatMovementInteraction.cs",
    "fields": [
      {
        "line": 21,
        "text": "private readonly ICombatViewBufferWriteSink _input"
      },
      {
        "line": 22,
        "text": "private readonly ICombatViewBufferReadSink _requests"
      },
      {
        "line": 23,
        "text": "private readonly ICombatStateBufferReadSink _combat"
      },
      {
        "line": 24,
        "text": "private readonly float _innerCellInset"
      },
      {
        "line": 25,
        "text": "private readonly List<CellCoord> _crossed"
      },
      {
        "line": 26,
        "text": "private readonly List<CellCoord> _unaccepted"
      },
      {
        "line": 27,
        "text": "private Vector2? _lastPointer"
      },
      {
        "line": 28,
        "text": "private bool _attached"
      },
      {
        "line": 29,
        "text": "private bool _frozen"
      },
      {
        "line": 30,
        "text": "private long _lifetime"
      },
      {
        "line": 31,
        "text": "private long _battle"
      },
      {
        "line": 32,
        "text": "private long _requestLifetime"
      },
      {
        "line": 33,
        "text": "private int _requestNumber"
      },
      {
        "line": 34,
        "text": "private int _submittedNumber"
      },
      {
        "line": 35,
        "text": "private long _requestRevision"
      },
      {
        "line": 36,
        "text": "private MovementPath _requestPath"
      },
      {
        "line": 37,
        "text": "private MovementPreviewContext _context"
      },
      {
        "line": 38,
        "text": "private MovementPath _retainedPath"
      },
      {
        "line": 39,
        "text": "private MovementPath _retainedAgainst"
      },
      {
        "line": 40,
        "text": "private readonly List<CellCoord> _prefixCells"
      },
      {
        "line": 41,
        "text": "private bool _resumeAdjacent"
      },
      {
        "line": 42,
        "text": "private bool _requiresEndpointReentry"
      },
      {
        "line": 43,
        "text": "private CellCoord? _rejectedCell"
      },
      {
        "line": 46,
        "text": "private MovementDraft Draft"
      },
      {
        "line": 51,
        "text": "internal MovementPath RetainedPath"
      },
      {
        "line": 53,
        "text": "internal CellCoord? CandidateCell"
      },
      {
        "line": 55,
        "text": "internal long DraftRevision"
      },
      {
        "line": 57,
        "text": "internal bool PointerValid"
      },
      {
        "line": 59,
        "text": "internal bool IsActive"
      },
      {
        "line": 61,
        "text": "internal long ValidatedRevision"
      },
      {
        "line": 63,
        "text": "internal CombatPreviewResult Preview"
      },
      {
        "line": 65,
        "text": "internal bool IsAwaitingPreview"
      },
      {
        "line": 67,
        "text": "internal bool IsSubmitted"
      }
    ],
    "methods": [
      {
        "line": 48,
        "text": "internal MovementPath Snapshot()"
      },
      {
        "line": 69,
        "text": "internal CombatMovementInteraction(ICombatViewBufferWriteSink input, ICombatViewBufferReadSink requests, ICombatStateBufferReadSink combat, float innerCellInset)"
      },
      {
        "line": 80,
        "text": "internal void Attach()"
      },
      {
        "line": 87,
        "text": "internal void Detach()"
      },
      {
        "line": 94,
        "text": "internal void FocusLost()"
      },
      {
        "line": 96,
        "text": "internal void CaptureLost()"
      },
      {
        "line": 102,
        "text": "internal bool Begin(EntityId actor, CellCoord origin, Vector2 pointerGrid, bool allowed)"
      },
      {
        "line": 128,
        "text": "internal void PointerMoved(Vector2? grid, bool overCancelArea, bool allowed)"
      },
      {
        "line": 195,
        "text": "internal void OnPreview(CombatPreviewResult result, bool allowed)"
      },
      {
        "line": 244,
        "text": "internal void Confirm(CellCoord? cell, bool overCancelArea, bool allowed)"
      },
      {
        "line": 259,
        "text": "internal void Cancel()"
      },
      {
        "line": 282,
        "text": "private void SuspendSampling()"
      },
      {
        "line": 292,
        "text": "private bool EnsureLive(bool allowed)"
      },
      {
        "line": 306,
        "text": "private void RequestPreview()"
      },
      {
        "line": 332,
        "text": "private void TrimRetainedPath()"
      },
      {
        "line": 355,
        "text": "private bool OwnsLatestRequest()"
      },
      {
        "line": 363,
        "text": "private void ClearOwnedPreview()"
      },
      {
        "line": 369,
        "text": "private void SubmitFrozen()"
      },
      {
        "line": 376,
        "text": "private static bool SamePath(MovementPath a, MovementPath b)"
      }
    ]
  },
  "CombatMovementPlayback": {
    "kind": "class",
    "inherits": "",
    "source": "C:/Users/User/Project_L/Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/ViewSystem/Combat/CombatMovementPlayback.cs",
    "fields": [
      {
        "line": 12,
        "text": "private readonly ICombatMovementPlaybackPresenter _presenter"
      },
      {
        "line": 13,
        "text": "private CombatCharacterEvent _step"
      },
      {
        "line": 14,
        "text": "private bool _detached"
      },
      {
        "line": 21,
        "text": "internal bool IsComplete"
      }
    ],
    "methods": [
      {
        "line": 16,
        "text": "internal CombatMovementPlayback(ICombatMovementPlaybackPresenter presenter)"
      },
      {
        "line": 23,
        "text": "internal void BeginStep(CombatCharacterEvent step)"
      },
      {
        "line": 31,
        "text": "internal void ShowReturn(CombatCharacterEvent returned)"
      },
      {
        "line": 39,
        "text": "internal void Tick(float deltaTime)"
      },
      {
        "line": 48,
        "text": "internal void Detach()"
      },
      {
        "line": 59,
        "text": "private void RequireAvailable(CombatCharacterEvent step, CombatEventKind kind)"
      }
    ]
  },
  "CombatPathInput": {
    "kind": "class",
    "inherits": "",
    "source": "C:/Users/User/Project_L/Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/ViewSystem/Combat/CombatPathInput.cs",
    "fields": [
      {
        "line": 18,
        "text": "private readonly ICombatStateBufferReadSink _state"
      },
      {
        "line": 19,
        "text": "private readonly ICharacterBufferReadSink _characters"
      },
      {
        "line": 20,
        "text": "private readonly ICombatViewBufferReadSink _input"
      },
      {
        "line": 21,
        "text": "private readonly ICombatViewBufferWriteSink _write"
      },
      {
        "line": 22,
        "text": "private readonly ICombatPointerMapping _mapping"
      },
      {
        "line": 23,
        "text": "private readonly ICombatMovementFeedback _presenter"
      },
      {
        "line": 25,
        "text": "private readonly Func<CombatInputFacts> _readFacts"
      },
      {
        "line": 26,
        "text": "private readonly CombatMovementInteraction _interaction"
      },
      {
        "line": 27,
        "text": "private bool _attached"
      },
      {
        "line": 28,
        "text": "private bool _returnWanted"
      },
      {
        "line": 29,
        "text": "private bool _draftShown"
      },
      {
        "line": 30,
        "text": "private bool _planningShown"
      },
      {
        "line": 31,
        "text": "private readonly long _battle"
      },
      {
        "line": 32,
        "text": "private int _returnRequest"
      },
      {
        "line": 33,
        "text": "private EntityId _returnActor"
      },
      {
        "line": 34,
        "text": "private MovementPath _shownPath"
      },
      {
        "line": 35,
        "text": "private CombatPreviewResult _shownPreview"
      },
      {
        "line": 36,
        "text": "private bool _shownPointerValid"
      },
      {
        "line": 37,
        "text": "private CellCoord? _shownCandidate"
      },
      {
        "line": 38,
        "text": "private int _draftDisplayVersion"
      },
      {
        "line": 39,
        "text": "private bool _waitingForOrigin"
      },
      {
        "line": 40,
        "text": "private EntityId _pathActor"
      },
      {
        "line": 41,
        "text": "private CellCoord _pathOrigin"
      }
    ],
    "methods": [
      {
        "line": 43,
        "text": "internal CombatPathInput(ICombatStateBufferReadSink state, ICharacterBufferReadSink characters, ICombatViewBufferReadSink input, ICombatViewBufferWriteSink write, ICombatPointerMapping mapping, ICombatMovementFeedback presenter, Func<CombatInputFacts> readFacts)"
      },
      {
        "line": 61,
        "text": "private bool AllowsInput()"
      },
      {
        "line": 70,
        "text": "internal void Detach()"
      },
      {
        "line": 120,
        "text": "internal void OnPreview(CombatPreviewResult preview)"
      },
      {
        "line": 135,
        "text": "public void FocusLost()"
      },
      {
        "line": 137,
        "text": "public void CaptureLost()"
      },
      {
        "line": 139,
        "text": "private CharacterData ActiveActor()"
      },
      {
        "line": 149,
        "text": "internal void BeginPlanning()"
      },
      {
        "line": 169,
        "text": "private bool ValidatePlanning(bool allowed)"
      },
      {
        "line": 179,
        "text": "public void PointerMoved(float x, float y)"
      },
      {
        "line": 208,
        "text": "public void SelectPressed(float x, float y)"
      },
      {
        "line": 230,
        "text": "public void SelectReleased(float x, float y)"
      },
      {
        "line": 234,
        "text": "public void SelectCanceled()"
      },
      {
        "line": 236,
        "text": "public void Cancel()"
      },
      {
        "line": 245,
        "text": "internal void CancelInputs()"
      },
      {
        "line": 257,
        "text": "private void SyncPlanning()"
      },
      {
        "line": 267,
        "text": "private bool Map(float x, float y, out Vector2 grid, out bool cancel)"
      },
      {
        "line": 276,
        "text": "private void ShowDraft()"
      },
      {
        "line": 311,
        "text": "private void ClearDraft()"
      },
      {
        "line": 321,
        "text": "private void InvalidateDraftDisplay()"
      },
      {
        "line": 329,
        "text": "internal void RefreshReturnPreview()"
      },
      {
        "line": 357,
        "text": "public void RequestReturn()"
      },
      {
        "line": 372,
        "text": "private void HandleReturnPreview(CombatPreviewResult preview)"
      },
      {
        "line": 393,
        "text": "private void ClearReturnRequest()"
      }
    ]
  },
  "CombatPlaybackToken": {
    "kind": "struct",
    "inherits": "",
    "source": "C:/Users/User/Project_L/Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/ViewSystem/Combat/CombatPlaybackToken.cs",
    "fields": [
      {
        "line": 16,
        "text": "internal int Generation"
      },
      {
        "line": 19,
        "text": "internal int Sequence"
      }
    ],
    "methods": [
      {
        "line": 9,
        "text": "internal CombatPlaybackToken(int generation, int sequence)"
      }
    ]
  },
  "CombatPointerMapper": {
    "kind": "class",
    "inherits": "ICombatPointerPicker, ICombatPointerMapping",
    "source": "C:/Users/User/Project_L/Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/ViewSystem/Combat/CombatPointerMapper.cs",
    "fields": [
      {
        "line": 22,
        "text": "private readonly float _innerCellInset"
      },
      {
        "line": 23,
        "text": "private readonly List<RaycastResult> _uiHits"
      },
      {
        "line": 24,
        "text": "private FieldView _field"
      },
      {
        "line": 25,
        "text": "private HandPresenter _hand"
      },
      {
        "line": 26,
        "text": "private Camera _assignedCamera"
      },
      {
        "line": 27,
        "text": "private FieldGridMapper _mapper"
      },
      {
        "line": 28,
        "text": "private EventSystem _pointerEventSystem"
      },
      {
        "line": 29,
        "text": "private PointerEventData _pointerEventData"
      },
      {
        "line": 30,
        "text": "private bool _isReleased"
      },
      {
        "line": 40,
        "text": "public float InnerCellInset"
      },
      {
        "line": 43,
        "text": "internal bool IsBound"
      }
    ],
    "methods": [
      {
        "line": 32,
        "text": "internal CombatPointerMapper(FieldView field, HandPresenter hand, Camera assignedCamera, float innerCellInset)"
      },
      {
        "line": 46,
        "text": "internal void Bind(FieldGridMapper mapper)"
      },
      {
        "line": 54,
        "text": "internal void Unbind()"
      },
      {
        "line": 61,
        "text": "internal void Release()"
      },
      {
        "line": 73,
        "text": "public bool TryPickCell(float screenX, float screenY, out CellCoord cell)"
      },
      {
        "line": 80,
        "text": "public bool TryPickHandUnit(float screenX, float screenY, out EntityId entityId)"
      },
      {
        "line": 86,
        "text": "public bool IsOverHandDropZone(float screenX, float screenY)"
      },
      {
        "line": 92,
        "text": "internal bool TryRayToFieldPlane(float screenX, float screenY, out Vector3 point)"
      },
      {
        "line": 98,
        "text": "public bool TryMapPointer(Vector2 screen, out Vector2 grid, out bool overCancelArea)"
      },
      {
        "line": 151,
        "text": "private bool TryGetRay(float screenX, float screenY, out Ray ray)"
      },
      {
        "line": 164,
        "text": "private void ClearPointerRaycast()"
      }
    ]
  },
  "CombatPresentationOutcome": {
    "kind": "struct",
    "inherits": "",
    "source": "C:/Users/User/Project_L/Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/ViewSystem/Combat/CombatPresentationOutcome.cs",
    "fields": [
      {
        "line": 20,
        "text": "private readonly OutcomeKind _kind"
      },
      {
        "line": 29,
        "text": "internal static CombatPresentationOutcome Completed"
      },
      {
        "line": 31,
        "text": "internal bool IsCompleted"
      },
      {
        "line": 33,
        "text": "internal bool IsFailed"
      },
      {
        "line": 36,
        "text": "internal bool IsValid"
      },
      {
        "line": 39,
        "text": "internal Exception Error"
      }
    ],
    "methods": [
      {
        "line": 22,
        "text": "private CombatPresentationOutcome(OutcomeKind kind, Exception error)"
      },
      {
        "line": 42,
        "text": "internal static CombatPresentationOutcome Failure(Exception error)"
      }
    ]
  },
  "CombatRefreshSignal": {
    "kind": "struct",
    "inherits": "",
    "source": "C:/Users/User/Project_L/Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/ViewSystem/Combat/CombatRefreshSignal.cs",
    "fields": [
      {
        "line": 23,
        "text": "internal CombatResult UnpresentedResult"
      },
      {
        "line": 26,
        "text": "internal bool PreviewAnswered"
      },
      {
        "line": 29,
        "text": "internal string Violation"
      }
    ],
    "methods": [
      {
        "line": 15,
        "text": "private CombatRefreshSignal(CombatResult unpresentedResult, bool previewAnswered, string violation)"
      },
      {
        "line": 31,
        "text": "internal static CombatRefreshSignal Observed(CombatResult unpresentedResult, bool previewAnswered)"
      },
      {
        "line": 36,
        "text": "internal static CombatRefreshSignal Violated(string violation)"
      }
    ]
  },
  "CombatResultPlayback": {
    "kind": "class",
    "inherits": "",
    "source": "C:/Users/User/Project_L/Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/ViewSystem/Combat/CombatResultPlayback.cs",
    "fields": [
      {
        "line": 16,
        "text": "private readonly ICombatPresentation _presentation"
      },
      {
        "line": 18,
        "text": "private int _generation"
      },
      {
        "line": 19,
        "text": "private bool _isAttached"
      },
      {
        "line": 20,
        "text": "private int _presentingSequence"
      },
      {
        "line": 21,
        "text": "private CombatPresentationOutcome? _outcome"
      },
      {
        "line": 22,
        "text": "private Exception _notificationViolation"
      },
      {
        "line": 23,
        "text": "private bool _isFinalizing"
      },
      {
        "line": 31,
        "text": "internal bool IsAttached"
      },
      {
        "line": 34,
        "text": "internal bool IsBusy"
      },
      {
        "line": 37,
        "text": "internal int PresentingSequence"
      },
      {
        "line": 40,
        "text": "internal bool IsFinalizing"
      }
    ],
    "methods": [
      {
        "line": 25,
        "text": "internal CombatResultPlayback(ICombatPresentation presentation)"
      },
      {
        "line": 43,
        "text": "internal void Attach()"
      },
      {
        "line": 53,
        "text": "internal CombatPlaybackToken Begin(CombatResult result)"
      },
      {
        "line": 74,
        "text": "internal bool TryGetFailure(out Exception error)"
      },
      {
        "line": 95,
        "text": "internal bool TryBeginFinalization(out CombatPlaybackToken token)"
      },
      {
        "line": 109,
        "text": "internal bool IsCurrent(CombatPlaybackToken token)"
      },
      {
        "line": 118,
        "text": "internal void CompleteFinalization(CombatPlaybackToken token)"
      },
      {
        "line": 130,
        "text": "internal void Abort()"
      },
      {
        "line": 142,
        "text": "private void OnFinished(CombatPlaybackToken token, CombatPresentationOutcome outcome)"
      },
      {
        "line": 165,
        "text": "private void ClearCurrent()"
      }
    ]
  },
  "CombatSceneDisplay": {
    "kind": "class",
    "inherits": "ICombatBattleDisplay, IUIInputReceiver",
    "source": "C:/Users/User/Project_L/Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/ViewSystem/Combat/CombatSceneDisplay.cs",
    "fields": [
      {
        "line": 33,
        "text": "private const int DevDeploymentSlot"
      },
      {
        "line": 34,
        "text": "private const int DevCombatSlot"
      },
      {
        "line": 36,
        "text": "private readonly FieldView _field"
      },
      {
        "line": 37,
        "text": "private readonly FieldUnitPresenter _units"
      },
      {
        "line": 38,
        "text": "private readonly HandPresenter _hand"
      },
      {
        "line": 39,
        "text": "private readonly CombatHud _hud"
      },
      {
        "line": 40,
        "text": "private readonly PlacementGhost _ghost"
      },
      {
        "line": 41,
        "text": "private readonly CombatAbandonPanel _abandon"
      },
      {
        "line": 43,
        "text": "private readonly CombatPointerMapper _pointer"
      },
      {
        "line": 44,
        "text": "private readonly GameObject _devPanel"
      },
      {
        "line": 45,
        "text": "private readonly Button _devDeploymentButton"
      },
      {
        "line": 46,
        "text": "private readonly Button _devCombatButton"
      },
      {
        "line": 47,
        "text": "private readonly StageFieldViewData _fieldConfig"
      },
      {
        "line": 48,
        "text": "private readonly IColorSystemService _colors"
      },
      {
        "line": 49,
        "text": "private readonly IAreaOutlineSystemService _areaOutlines"
      },
      {
        "line": 50,
        "text": "private readonly CombatSceneEventPresenter _events"
      },
      {
        "line": 51,
        "text": "private readonly StageViewPropPresenter _props"
      },
      {
        "line": 53,
        "text": "private Action _onDevDeployment"
      },
      {
        "line": 54,
        "text": "private Action _onDevCombat"
      },
      {
        "line": 55,
        "text": "private bool _isDevInputAttached"
      },
      {
        "line": 56,
        "text": "private bool _isReleased"
      },
      {
        "line": 57,
        "text": "private EntityId _hudPreviewActor"
      },
      {
        "line": 58,
        "text": "private readonly List<CellCoord> _blockedWallCells"
      },
      {
        "line": 59,
        "text": "private readonly HashSet<CellCoord> _blockedWallSet"
      },
      {
        "line": 60,
        "text": "public float InnerCellInset"
      },
      {
        "line": 61,
        "text": "public bool IsMovementComplete"
      }
    ],
    "methods": [
      {
        "line": 63,
        "text": "internal CombatSceneDisplay( CombatSceneRoot root, StageFieldViewData fieldConfig, IUnitVisualTableQuery unitVisuals, IStageViewPropTableQuery stageProps, IColorSystemService colors, ITraitVisualTableQuery traitVisuals, IAreaOutlineSystemService areaOutlines)"
      },
      {
        "line": 105,
        "text": "public void Install(in CombatDisplayValues values)"
      },
      {
        "line": 131,
        "text": "public void Refresh(in CombatDisplayValues values)"
      },
      {
        "line": 145,
        "text": "private void RenderActiveActor(in CombatDisplayValues values)"
      },
      {
        "line": 165,
        "text": "public void Clear()"
      },
      {
        "line": 174,
        "text": "public void Release()"
      },
      {
        "line": 189,
        "text": "public void ShowCompletion(BattleCompletionChanges completion, Action confirmed)"
      },
      {
        "line": 199,
        "text": "public void ShowDevLayerPanel(Action onDeployment, Action onCombat)"
      },
      {
        "line": 222,
        "text": "public void HideDevLayerPanel()"
      },
      {
        "line": 246,
        "text": "private void HideDevLayerPanelCore()"
      },
      {
        "line": 259,
        "text": "public void BindAbandon(Action open, Action confirm, Action cancel)"
      },
      {
        "line": 268,
        "text": "public void SetAbandonAvailable(bool visible, bool interactable)"
      },
      {
        "line": 274,
        "text": "public void ShowAbandonConfirm()"
      },
      {
        "line": 280,
        "text": "public void HideAbandonConfirm()"
      },
      {
        "line": 290,
        "text": "public bool TryPickCell(float screenX, float screenY, out CellCoord cell)"
      },
      {
        "line": 292,
        "text": "public bool TryPickHandUnit(float screenX, float screenY, out EntityId entityId)"
      },
      {
        "line": 295,
        "text": "public bool IsOverHandDropZone(float screenX, float screenY)"
      },
      {
        "line": 297,
        "text": "public bool TryMapPointer(Vector2 screen, out Vector2 grid, out bool overCancelArea)"
      },
      {
        "line": 304,
        "text": "public void ShowGhost(EntityKey entityKey)"
      },
      {
        "line": 314,
        "text": "public void MoveGhostToCell(CellCoord cell)"
      },
      {
        "line": 321,
        "text": "public void MoveGhostToPointer(float screenX, float screenY)"
      },
      {
        "line": 327,
        "text": "public void HideGhost()"
      },
      {
        "line": 336,
        "text": "public void HideOriginal(EntityId entityId)"
      },
      {
        "line": 346,
        "text": "public void ShowOriginal()"
      },
      {
        "line": 355,
        "text": "public void SetGhostPlaceable(bool placeable)"
      },
      {
        "line": 361,
        "text": "public void SetHoverCell(CellCoord cell, CombatHoverState state)"
      },
      {
        "line": 367,
        "text": "public void ClearHoverCell()"
      },
      {
        "line": 373,
        "text": "public void ShowRangeCells(IReadOnlyList<CellCoord> cells)"
      },
      {
        "line": 379,
        "text": "public void ClearRangeCells()"
      },
      {
        "line": 385,
        "text": "public void ShowAffectedCells(IReadOnlyList<CellCoord> cells)"
      },
      {
        "line": 391,
        "text": "public void ClearAffectedCells()"
      },
      {
        "line": 400,
        "text": "public void ShowAttackCostPreview(EntityId actorId, int cost)"
      },
      {
        "line": 408,
        "text": "public void ShowAttackBlockers(IReadOnlyList<AttackBlockedContact> contacts)"
      },
      {
        "line": 420,
        "text": "public void ClearAttackBlockers()"
      },
      {
        "line": 428,
        "text": "private void ClearHudCostPreview()"
      },
      {
        "line": 439,
        "text": "public void BindReturn(Action requested)"
      },
      {
        "line": 446,
        "text": "public void ShowDraft(MovementPath path, MovePreview? preview, bool pointerValid, CellCoord? candidate = null)"
      },
      {
        "line": 466,
        "text": "public void ClearDraft()"
      },
      {
        "line": 477,
        "text": "public void SetPathPlanning(bool planning)"
      },
      {
        "line": 484,
        "text": "public void SetReturnState(CheckpointReturnPreview preview)"
      },
      {
        "line": 490,
        "text": "public void BeginStep(CombatCharacterEvent step)"
      },
      {
        "line": 491,
        "text": "public void ShowReturn(CombatCharacterEvent returned)"
      },
      {
        "line": 492,
        "text": "public void CancelMovement()"
      },
      {
        "line": 493,
        "text": "public void TickPresentation(float deltaTime)"
      },
      {
        "line": 494,
        "text": "public void CancelPresentation()"
      },
      {
        "line": 495,
        "text": "public bool CanPresent(CombatEvent e)"
      },
      {
        "line": 496,
        "text": "public void BeginEvent(CombatEvent e, Action completed)"
      },
      {
        "line": 498,
        "text": "private void ThrowIfReleased()"
      },
      {
        "line": 505,
        "text": "private static void RunAll(params Action[] actions)"
      }
    ]
  },
  "CombatSceneEventPresenter": {
    "kind": "class",
    "inherits": "ICombatMovementPlaybackPresenter, ICombatResultPresenter, ICombatPresentationControl",
    "source": "C:/Users/User/Project_L/Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/ViewSystem/Combat/CombatSceneEventPresenter.cs",
    "fields": [
      {
        "line": 14,
        "text": "private readonly FieldUnitPresenter _units"
      },
      {
        "line": 15,
        "text": "private readonly float _stepSeconds"
      },
      {
        "line": 16,
        "text": "private readonly List<CharacterCombatFacts> _shownCharacters"
      },
      {
        "line": 17,
        "text": "private readonly List<StructureCombatFacts> _shownStructures"
      },
      {
        "line": 18,
        "text": "private FieldGridMapper _mapper"
      },
      {
        "line": 19,
        "text": "private CombatCharacterEvent _moving"
      },
      {
        "line": 20,
        "text": "private float _elapsed"
      },
      {
        "line": 22,
        "text": "private int _presentationVersion"
      },
      {
        "line": 30,
        "text": "public bool IsMovementComplete"
      }
    ],
    "methods": [
      {
        "line": 24,
        "text": "internal CombatSceneEventPresenter(FieldUnitPresenter units, float stepSeconds)"
      },
      {
        "line": 33,
        "text": "internal void Bind(FieldGridMapper mapper)"
      },
      {
        "line": 40,
        "text": "internal void Refresh(IReadOnlyList<CharacterData> characters, IReadOnlyList<StructureCombatFacts> structures)"
      },
      {
        "line": 53,
        "text": "internal void Clear()"
      },
      {
        "line": 61,
        "text": "public void BeginStep(CombatCharacterEvent step)"
      },
      {
        "line": 71,
        "text": "public void ShowReturn(CombatCharacterEvent returned)"
      },
      {
        "line": 79,
        "text": "public void TickPresentation(float deltaTime)"
      },
      {
        "line": 91,
        "text": "public void CancelMovement()"
      },
      {
        "line": 98,
        "text": "public void CancelPresentation()"
      },
      {
        "line": 100,
        "text": "public bool CanPresent(CombatEvent e)"
      },
      {
        "line": 107,
        "text": "public void BeginEvent(CombatEvent e, Action completed)"
      },
      {
        "line": 126,
        "text": "private void ApplyCharacter(CharacterCombatFacts facts)"
      },
      {
        "line": 132,
        "text": "private void RenderEvents()"
      },
      {
        "line": 134,
        "text": "private void RequireBound()"
      }
    ]
  },
  "CombatSceneRoot": {
    "kind": "class",
    "inherits": "MonoBehaviour",
    "source": "C:/Users/User/Project_L/Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/ViewSystem/Combat/CombatSceneRoot.cs",
    "fields": [
      {
        "line": 19,
        "text": "public event Action FocusLost"
      },
      {
        "line": 20,
        "text": "public event Action Disabled"
      },
      {
        "line": 23,
        "text": "private float innerCellInset"
      },
      {
        "line": 24,
        "text": "private float movementStepSeconds"
      },
      {
        "line": 25,
        "text": "internal float InnerCellInset"
      },
      {
        "line": 26,
        "text": "internal float MovementStepSeconds"
      },
      {
        "line": 27,
        "text": "private FieldView field"
      },
      {
        "line": 28,
        "text": "private FieldUnitPresenter fieldUnits"
      },
      {
        "line": 29,
        "text": "private HandPresenter hand"
      },
      {
        "line": 30,
        "text": "private CombatHud hud"
      },
      {
        "line": 31,
        "text": "private PlacementGhost ghost"
      },
      {
        "line": 33,
        "text": "private CombatAbandonPanel abandonPanel"
      },
      {
        "line": 36,
        "text": "private Camera worldCamera"
      },
      {
        "line": 39,
        "text": "private GameObject devLayerPanel"
      },
      {
        "line": 40,
        "text": "private Button devDeploymentButton"
      },
      {
        "line": 41,
        "text": "private Button devCombatButton"
      },
      {
        "line": 43,
        "text": "internal FieldView Field"
      },
      {
        "line": 45,
        "text": "internal FieldUnitPresenter FieldUnits"
      },
      {
        "line": 47,
        "text": "internal HandPresenter Hand"
      },
      {
        "line": 49,
        "text": "internal CombatHud Hud"
      },
      {
        "line": 51,
        "text": "internal PlacementGhost Ghost"
      },
      {
        "line": 53,
        "text": "internal CombatAbandonPanel AbandonPanel"
      },
      {
        "line": 56,
        "text": "internal Camera WorldCamera"
      },
      {
        "line": 58,
        "text": "internal GameObject DevLayerPanel"
      },
      {
        "line": 60,
        "text": "internal Button DevDeploymentButton"
      },
      {
        "line": 62,
        "text": "internal Button DevCombatButton"
      }
    ],
    "methods": [
      {
        "line": 21,
        "text": "private void OnApplicationFocus(bool focused)"
      },
      {
        "line": 22,
        "text": "private void OnDisable()"
      },
      {
        "line": 65,
        "text": "internal void ValidateReferences()"
      },
      {
        "line": 72,
        "text": "private string FindMissingReference()"
      },
      {
        "line": 102,
        "text": "private void OnValidate()"
      }
    ]
  },
  "CombatViewRefresh": {
    "kind": "class",
    "inherits": "",
    "source": "C:/Users/User/Project_L/Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/ViewSystem/Combat/CombatViewRefresh.cs",
    "fields": [
      {
        "line": 15,
        "text": "private int _observedPreviewNumber"
      }
    ],
    "methods": [
      {
        "line": 18,
        "text": "internal void Reset()"
      },
      {
        "line": 28,
        "text": "internal CombatRefreshSignal Observe( int latestSequence, int presentedSequence, CombatResult latestResult, int answeredPreviewNumber)"
      },
      {
        "line": 46,
        "text": "private static string FindViolation(int latestSequence, int presentedSequence, CombatResult latestResult)"
      }
    ]
  },
  "CombatViewSystem": {
    "kind": "class",
    "inherits": "ICombatViewService, ISystemInitData<IHasViewQuery>, ISystemPostBindService, ISystemInitialize, ISystemTick, ISystemShutdown, IExportSystemServices",
    "source": "C:/Users/User/Project_L/Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/ViewSystem/Combat/CombatViewSystem.cs",
    "fields": [
      {
        "line": 61,
        "text": "internal const int SceneEnterPriority"
      },
      {
        "line": 62,
        "text": "internal const int SceneExitPriority"
      },
      {
        "line": 65,
        "text": "private const int MinTimelineEntries"
      },
      {
        "line": 66,
        "text": "private const int TimelineEntriesPerCharacter"
      },
      {
        "line": 68,
        "text": "private readonly RuntimeFaultBoundary _faults"
      },
      {
        "line": 69,
        "text": "private readonly CombatResultPlayback _playback"
      },
      {
        "line": 70,
        "text": "private readonly ICombatPresentation _presentation"
      },
      {
        "line": 71,
        "text": "private CombatPathInput _pathInput"
      },
      {
        "line": 72,
        "text": "private CombatSceneRoot _sceneRoot"
      },
      {
        "line": 73,
        "text": "private long _battleInstance"
      },
      {
        "line": 74,
        "text": "private bool _submissionsSuspended"
      },
      {
        "line": 76,
        "text": "private bool _battleShown"
      },
      {
        "line": 77,
        "text": "private BattleCompletionChanges _shownCompletion"
      },
      {
        "line": 79,
        "text": "private bool IsMoveMode"
      },
      {
        "line": 80,
        "text": "private readonly CombatViewRefresh _refresh"
      },
      {
        "line": 82,
        "text": "private ICombatStateBufferReadSink _state"
      },
      {
        "line": 83,
        "text": "private ICombatResultReadSink _results"
      },
      {
        "line": 84,
        "text": "private ICharacterBufferReadSink _characters"
      },
      {
        "line": 85,
        "text": "private ICurrentProgressBufferReadSink _current"
      },
      {
        "line": 86,
        "text": "private ICombatViewBufferReadSink _inputRead"
      },
      {
        "line": 87,
        "text": "private ICombatViewBufferWriteSink _inputWrite"
      },
      {
        "line": 88,
        "text": "private IInputLayerService _layers"
      },
      {
        "line": 89,
        "text": "private ILodingRegisterService _loadingRegister"
      },
      {
        "line": 90,
        "text": "private IColorSystemService _colors"
      },
      {
        "line": 91,
        "text": "private IAreaOutlineSystemService _areaOutlines"
      },
      {
        "line": 92,
        "text": "private IUISystemService _ui"
      },
      {
        "line": 94,
        "text": "private StageFieldViewData _fieldConfig"
      },
      {
        "line": 95,
        "text": "private IUnitVisualTableQuery _unitVisuals"
      },
      {
        "line": 96,
        "text": "private IStageViewPropTableQuery _stageProps"
      },
      {
        "line": 97,
        "text": "private ITraitVisualTableQuery _traitVisuals"
      },
      {
        "line": 98,
        "text": "private ICombatProcessingService _combatQueries"
      },
      {
        "line": 101,
        "text": "private ICombatAbandonRequest _abandonRequest"
      },
      {
        "line": 102,
        "text": "private readonly CombatAbandonController _abandon"
      },
      {
        "line": 104,
        "text": "private readonly CombatDevLayerController _devLayer"
      },
      {
        "line": 106,
        "text": "private ICombatBattleDisplay _display"
      },
      {
        "line": 107,
        "text": "private CombatInteraction _interaction"
      },
      {
        "line": 109,
        "text": "private CombatCommandGate _gate"
      },
      {
        "line": 111,
        "text": "private CombatInputRouter _router"
      },
      {
        "line": 112,
        "text": "private readonly Func<CombatInputFacts> _readPathFacts"
      },
      {
        "line": 113,
        "text": "private readonly Func<bool, CombatInputFacts> _readInputFacts"
      },
      {
        "line": 115,
        "text": "private CombatInputReceiver _receiver"
      },
      {
        "line": 142,
        "text": "internal CombatExecutionMode ExecutionMode"
      },
      {
        "line": 145,
        "text": "public bool IsAttached"
      },
      {
        "line": 146,
        "text": "private ICombatEntryRead _entry"
      },
      {
        "line": 867,
        "text": "public bool HasPendingPresentation"
      },
      {
        "line": 870,
        "text": "internal bool HasPendingCommand"
      },
      {
        "line": 873,
        "text": "internal CombatInputReceiver Input"
      },
      {
        "line": 876,
        "text": "private bool IsInputBound"
      }
    ],
    "methods": [
      {
        "line": 117,
        "text": "internal CombatViewSystem( CombatExecutionMode executionMode, RuntimeFaultBoundary faultBoundary, ICombatPresentation presentation)"
      },
      {
        "line": 151,
        "text": "public void InitData(IHasViewQuery data)"
      },
      {
        "line": 166,
        "text": "public void PostBindService(ISystemServiceStoreReader resolver)"
      },
      {
        "line": 194,
        "text": "internal void BindServices( ICombatStateBufferReadSink state, ICombatResultReadSink results, ICharacterBufferReadSink characters, ICurrentProgressBufferReadSink current, ICombatViewBufferReadSink inputRead, ICombatViewBufferWriteSink inputWrite, IStageInputReceiverSlot slot, IInputLayerService layers, ILodingRegisterService loadingRegister, IColorSystemService colors, IAreaOutlineSystemService areaOutlines, ICombatEntryRead entry, ICombatAbandonRequest abandon)"
      },
      {
        "line": 230,
        "text": "public void Initialize()"
      },
      {
        "line": 244,
        "text": "public void Tick(float deltaTime)"
      },
      {
        "line": 305,
        "text": "public void Shutdown()"
      },
      {
        "line": 317,
        "text": "public void ExportServices(ISystemWriteSink sink)"
      },
      {
        "line": 333,
        "text": "public void Attach(CombatSceneRoot root)"
      },
      {
        "line": 357,
        "text": "internal void AttachDisplay(ICombatBattleDisplay display)"
      },
      {
        "line": 395,
        "text": "private void ShowBattle()"
      },
      {
        "line": 415,
        "text": "private void RequireActiveBattle()"
      },
      {
        "line": 426,
        "text": "public void Detach()"
      },
      {
        "line": 478,
        "text": "internal void StopInput()"
      },
      {
        "line": 507,
        "text": "private void StopInteraction(ref List<Exception> errors)"
      },
      {
        "line": 536,
        "text": "private void OnCombatSceneEntered()"
      },
      {
        "line": 553,
        "text": "private void OnCombatSceneDisabled()"
      },
      {
        "line": 569,
        "text": "private void ConsumeResult(CombatResult result)"
      },
      {
        "line": 603,
        "text": "private void FinalizeResult(CombatResult result, CombatPlaybackToken token)"
      },
      {
        "line": 638,
        "text": "private void SyncTimelineUI(CombatResult result)"
      },
      {
        "line": 658,
        "text": "private CombatDisplayValues ReadDisplayValues()"
      },
      {
        "line": 671,
        "text": "private void SyncInputToCore()"
      },
      {
        "line": 700,
        "text": "public void SuspendSubmissions()"
      },
      {
        "line": 711,
        "text": "public void PresentCompletion(BattleCompletionChanges completion, Action completed)"
      },
      {
        "line": 738,
        "text": "private void ReleaseInputOrReport()"
      },
      {
        "line": 750,
        "text": "private void ReleaseInput(ref List<Exception> errors)"
      },
      {
        "line": 775,
        "text": "private void AbortPlayback(ref List<Exception> errors)"
      },
      {
        "line": 792,
        "text": "private CombatInputFacts ReadFacts(bool includeUnhandledCommand = false, bool followUp = false)"
      },
      {
        "line": 817,
        "text": "private bool IsPresenting(bool hasBattle)"
      },
      {
        "line": 823,
        "text": "private CombatPreviewResult ReadCurrentPreviewAnswer()"
      },
      {
        "line": 836,
        "text": "private bool CanContinue()"
      },
      {
        "line": 845,
        "text": "private void SyncSubmissionState()"
      },
      {
        "line": 854,
        "text": "private bool IsCurrent(CombatPlaybackToken token)"
      },
      {
        "line": 859,
        "text": "private static void ThrowIfAny(List<Exception> errors, string message)"
      },
      {
        "line": 879,
        "text": "public void FocusLost()"
      },
      {
        "line": 880,
        "text": "public void CaptureLost()"
      },
      {
        "line": 881,
        "text": "public void RequestReturn()"
      },
      {
        "line": 887,
        "text": "private void CancelInputPresentation()"
      }
    ]
  },
  "CombatAbandonPanel": {
    "kind": "class",
    "inherits": "MonoBehaviour, IUIInputReceiver",
    "source": "C:/Users/User/Project_L/Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/ViewSystem/Combat/Display/CombatAbandonPanel.cs",
    "fields": [
      {
        "line": 21,
        "text": "private const int OpenSlot"
      },
      {
        "line": 22,
        "text": "private const int ConfirmSlot"
      },
      {
        "line": 23,
        "text": "private const int CancelSlot"
      },
      {
        "line": 26,
        "text": "private Button abandonButton"
      },
      {
        "line": 28,
        "text": "private GameObject confirmDialog"
      },
      {
        "line": 29,
        "text": "private Button confirmButton"
      },
      {
        "line": 30,
        "text": "private Button cancelButton"
      },
      {
        "line": 32,
        "text": "private Action _open"
      },
      {
        "line": 33,
        "text": "private Action _confirm"
      },
      {
        "line": 34,
        "text": "private Action _cancel"
      },
      {
        "line": 35,
        "text": "private bool _isInputAttached"
      }
    ],
    "methods": [
      {
        "line": 41,
        "text": "internal void Bind(Action open, Action confirm, Action cancel)"
      },
      {
        "line": 80,
        "text": "internal void SetAvailable(bool visible, bool interactable)"
      },
      {
        "line": 88,
        "text": "internal void ShowConfirm()"
      },
      {
        "line": 94,
        "text": "internal void HideConfirm()"
      },
      {
        "line": 100,
        "text": "internal string FindMissingReference()"
      },
      {
        "line": 111,
        "text": "private void Unbind()"
      },
      {
        "line": 118,
        "text": "private void AttachInputOnce()"
      }
    ]
  },
  "CombatHud": {
    "kind": "class",
    "inherits": "MonoBehaviour, IUIInputReceiver",
    "source": "C:/Users/User/Project_L/Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/ViewSystem/Combat/Display/CombatHud.cs",
    "fields": [
      {
        "line": 25,
        "text": "private const int ReturnSlot"
      },
      {
        "line": 26,
        "text": "private const int CompletionSlot"
      },
      {
        "line": 29,
        "text": "private TMP_Text stageGoldText"
      },
      {
        "line": 30,
        "text": "private TMP_Text remainingDrawText"
      },
      {
        "line": 32,
        "text": "private TMP_Text movementText"
      },
      {
        "line": 33,
        "text": "private Button returnButton"
      },
      {
        "line": 34,
        "text": "private TMP_Text returnText"
      },
      {
        "line": 35,
        "text": "private Action _returnRequested"
      },
      {
        "line": 36,
        "text": "private Button completionButton"
      },
      {
        "line": 37,
        "text": "private TMP_Text completionText"
      },
      {
        "line": 39,
        "text": "private Action _completionConfirmed"
      },
      {
        "line": 40,
        "text": "private bool _isInputAttached"
      },
      {
        "line": 41,
        "text": "private bool _hasMovement, _hasReturn"
      },
      {
        "line": 150,
        "text": "private bool _hasShown"
      },
      {
        "line": 151,
        "text": "private int _shownStageGold"
      },
      {
        "line": 152,
        "text": "private int _shownRemainingDraws"
      }
    ],
    "methods": [
      {
        "line": 42,
        "text": "private (bool HasPreview, bool PointerValid, bool CanMove, int Distance, int Cost, MovementFailureReason Reason)"
      },
      {
        "line": 43,
        "text": "private (bool HasPreview, bool CanReturn, int Distance, int Cost, MovementFailureReason Reason)"
      },
      {
        "line": 45,
        "text": "private void OnEnable()"
      },
      {
        "line": 46,
        "text": "private void OnDisable()"
      },
      {
        "line": 48,
        "text": "private void InvalidateShownValues()"
      },
      {
        "line": 55,
        "text": "internal void ShowCompletion(BattleCompletionChanges completion, Action confirmed)"
      },
      {
        "line": 70,
        "text": "internal void HideCompletion()"
      },
      {
        "line": 77,
        "text": "internal void BindReturn(Action requested)"
      },
      {
        "line": 103,
        "text": "private void ConfirmCompletion()"
      },
      {
        "line": 114,
        "text": "private void AttachInputOnce()"
      },
      {
        "line": 123,
        "text": "internal void ShowMovement(MovePreview? preview, bool pointerValid)"
      },
      {
        "line": 137,
        "text": "internal void ShowReturn(CheckpointReturnPreview preview)"
      },
      {
        "line": 154,
        "text": "internal void Render(int stageGold, int remainingDraws)"
      },
      {
        "line": 166,
        "text": "internal void Clear()"
      },
      {
        "line": 174,
        "text": "internal string FindMissingReference()"
      }
    ]
  },
  "EntityViewSet": {
    "kind": "class",
    "inherits": "<TView> where TView : class",
    "source": "C:/Users/User/Project_L/Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/ViewSystem/Combat/Display/EntityViewSet.cs",
    "fields": [
      {
        "line": 24,
        "text": "private readonly Func<EntityKey, TView> _rent"
      },
      {
        "line": 25,
        "text": "private readonly Action<EntityKey, TView> _return"
      },
      {
        "line": 26,
        "text": "private readonly Dictionary<EntityId, Entry> _entries"
      },
      {
        "line": 27,
        "text": "private readonly List<EntityId> _removal"
      },
      {
        "line": 28,
        "text": "private int _pass"
      },
      {
        "line": 29,
        "text": "private bool _isRendering"
      },
      {
        "line": 37,
        "text": "internal int Count"
      }
    ],
    "methods": [
      {
        "line": 31,
        "text": "internal EntityViewSet(Func<EntityKey, TView> rent, Action<EntityKey, TView> @return)"
      },
      {
        "line": 39,
        "text": "internal void Begin()"
      },
      {
        "line": 49,
        "text": "internal TView Acquire(EntityId id, EntityKey key)"
      },
      {
        "line": 75,
        "text": "internal void End()"
      },
      {
        "line": 93,
        "text": "internal bool TryGet(EntityId id, out TView view)"
      },
      {
        "line": 106,
        "text": "internal void Clear()"
      },
      {
        "line": 127,
        "text": "private void Remove(EntityId id)"
      }
    ]
  },
  "FieldCellColorRenderer": {
    "kind": "class",
    "inherits": "IFieldHighlightRenderer",
    "source": "C:/Users/User/Project_L/Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/ViewSystem/Combat/Display/FieldCellColorRenderer.cs",
    "fields": [
      {
        "line": 19,
        "text": "private readonly FieldCellPool _primaryPool"
      },
      {
        "line": 20,
        "text": "private readonly FieldCellPool _alternatePool"
      },
      {
        "line": 21,
        "text": "private readonly Vector3 _cellOffsetSize"
      },
      {
        "line": 22,
        "text": "private readonly float _paddingX"
      },
      {
        "line": 23,
        "text": "private readonly float _paddingY"
      },
      {
        "line": 24,
        "text": "private readonly FieldHighlightPalette _palette"
      },
      {
        "line": 25,
        "text": "private FieldCellHandle[] _cells"
      },
      {
        "line": 26,
        "text": "private IFieldHighlightStateReader _state"
      }
    ],
    "methods": [
      {
        "line": 28,
        "text": "internal FieldCellColorRenderer(Transform cellsRoot, StageFieldViewData config, IColorSystemService colors, FieldHighlightPalette palette)"
      },
      {
        "line": 42,
        "text": "public bool CanReuse(StageFieldViewData config)"
      },
      {
        "line": 49,
        "text": "public FieldCellSpacing MeasureSpacing()"
      },
      {
        "line": 66,
        "text": "public void Build(FieldGridMapper mapper, IFieldHighlightStateReader state)"
      },
      {
        "line": 97,
        "text": "public void Repaint(IReadOnlyList<int> changedCells)"
      },
      {
        "line": 107,
        "text": "public void Refresh()"
      },
      {
        "line": 114,
        "text": "public void Release()"
      },
      {
        "line": 141,
        "text": "public void Dispose()"
      },
      {
        "line": 147,
        "text": "internal FieldCellHandle CellAt(int index)"
      },
      {
        "line": 153,
        "text": "private void Paint(int index)"
      }
    ]
  },
  "FieldCellHandle": {
    "kind": "class",
    "inherits": "",
    "source": "C:/Users/User/Project_L/Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/ViewSystem/Combat/Display/FieldCellHandle.cs",
    "fields": [
      {
        "line": 17,
        "text": "private static readonly int BaseColorId"
      },
      {
        "line": 19,
        "text": "private readonly GameObject _anchor"
      },
      {
        "line": 20,
        "text": "private readonly IColorHandle[] _colorScopes"
      },
      {
        "line": 21,
        "text": "private readonly Color[][] _defaultColors"
      },
      {
        "line": 22,
        "text": "private bool _isHighlighted"
      },
      {
        "line": 23,
        "text": "private Color _highlight"
      }
    ],
    "methods": [
      {
        "line": 25,
        "text": "internal FieldCellHandle(GameObject anchor, Renderer[] renderers, IColorSystemService colors)"
      },
      {
        "line": 49,
        "text": "internal void ApplyLayout(Vector3 localPosition, Vector3 cellOffsetSize)"
      },
      {
        "line": 57,
        "text": "internal void SetVisible(bool visible)"
      },
      {
        "line": 63,
        "text": "internal void SetHighlight(Color color)"
      },
      {
        "line": 78,
        "text": "internal void ClearHighlight()"
      },
      {
        "line": 93,
        "text": "internal void ResetForPool()"
      },
      {
        "line": 99,
        "text": "private static Color[] ReadDefaultColors(Renderer renderer)"
      }
    ]
  },
  "FieldCellPool": {
    "kind": "class",
    "inherits": "",
    "source": "C:/Users/User/Project_L/Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/ViewSystem/Combat/Display/FieldCellPool.cs",
    "fields": [
      {
        "line": 15,
        "text": "private readonly Transform _cellsRoot"
      },
      {
        "line": 16,
        "text": "private readonly GameObject _cellPrefab"
      },
      {
        "line": 17,
        "text": "private readonly IColorSystemService _colors"
      },
      {
        "line": 18,
        "text": "private readonly CustomPool<FieldCellHandle> _pool"
      },
      {
        "line": 19,
        "text": "private readonly List<FieldCellHandle> _active"
      },
      {
        "line": 21,
        "text": "private bool _hasFootprint"
      },
      {
        "line": 22,
        "text": "private float _footprintWidth"
      },
      {
        "line": 23,
        "text": "private float _footprintLength"
      },
      {
        "line": 34,
        "text": "internal GameObject Prefab"
      },
      {
        "line": 36,
        "text": "internal float FootprintWidth"
      },
      {
        "line": 38,
        "text": "internal float FootprintLength"
      }
    ],
    "methods": [
      {
        "line": 25,
        "text": "internal FieldCellPool(Transform cellsRoot, GameObject cellPrefab, IColorSystemService colors)"
      },
      {
        "line": 41,
        "text": "internal void Rent(int count)"
      },
      {
        "line": 53,
        "text": "internal FieldCellHandle GetActive(int index)"
      },
      {
        "line": 58,
        "text": "internal void ReturnAll()"
      },
      {
        "line": 66,
        "text": "internal void EnsureMeasured()"
      },
      {
        "line": 74,
        "text": "private FieldCellPool RequireFootprint()"
      },
      {
        "line": 81,
        "text": "private FieldCellHandle CreateHandle()"
      },
      {
        "line": 99,
        "text": "private void Measure(Transform anchor, Renderer[] renderers)"
      }
    ]
  },
  "FieldCellSpacing": {
    "kind": "struct",
    "inherits": "",
    "source": "C:/Users/User/Project_L/Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/ViewSystem/Combat/Display/FieldCellSpacing.cs",
    "fields": [
      {
        "line": 12,
        "text": "private const float GapRatio"
      },
      {
        "line": 20,
        "text": "internal float Width"
      },
      {
        "line": 22,
        "text": "internal float Length"
      }
    ],
    "methods": [
      {
        "line": 14,
        "text": "private FieldCellSpacing(float width, float length)"
      },
      {
        "line": 26,
        "text": "internal static FieldCellSpacing Calculate(float footprintWidth, float footprintLength, float paddingX, float paddingY)"
      },
      {
        "line": 40,
        "text": "internal static FieldCellSpacing FromCenterSpacing(float width, float length)"
      },
      {
        "line": 47,
        "text": "private static void RequirePositive(float value, string name)"
      }
    ]
  },
  "FieldGridDecal": {
    "kind": "class",
    "inherits": "",
    "source": "C:/Users/User/Project_L/Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/ViewSystem/Combat/Display/FieldGridDecal.cs",
    "fields": [
      {
        "line": 18,
        "text": "private static readonly int CellSizeId"
      },
      {
        "line": 19,
        "text": "private static readonly int GridSizeId"
      },
      {
        "line": 20,
        "text": "private static readonly int GridEdgeSizeId"
      },
      {
        "line": 21,
        "text": "private static readonly int GridOriginId"
      },
      {
        "line": 22,
        "text": "private static readonly int MaskCenterId"
      },
      {
        "line": 24,
        "text": "private readonly Transform _fieldRoot"
      },
      {
        "line": 25,
        "text": "private GameObject _gameObject"
      },
      {
        "line": 26,
        "text": "private DecalProjector _projector"
      },
      {
        "line": 27,
        "text": "private Material _source"
      },
      {
        "line": 28,
        "text": "private Material _instance"
      },
      {
        "line": 29,
        "text": "private bool _warnedRotation"
      }
    ],
    "methods": [
      {
        "line": 31,
        "text": "internal FieldGridDecal(Transform fieldRoot)"
      },
      {
        "line": 40,
        "text": "internal void Show(FieldGridMapper mapper, FieldCellSpacing spacing, float lineWidth, Material source, float depth)"
      },
      {
        "line": 90,
        "text": "internal void Hide()"
      },
      {
        "line": 97,
        "text": "internal void Dispose()"
      },
      {
        "line": 107,
        "text": "private void EnsureProjector(Material source)"
      },
      {
        "line": 125,
        "text": "private static void DestroySafe(Object target)"
      }
    ]
  },
  "FieldGridMapper": {
    "kind": "class",
    "inherits": "",
    "source": "C:/Users/User/Project_L/Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/ViewSystem/Combat/Display/FieldGridMapper.cs",
    "fields": [
      {
        "line": 13,
        "text": "private readonly Transform _fieldRoot"
      },
      {
        "line": 14,
        "text": "private readonly float _cellWidth"
      },
      {
        "line": 15,
        "text": "private readonly float _cellLength"
      },
      {
        "line": 39,
        "text": "internal int ColumnCount"
      },
      {
        "line": 41,
        "text": "internal int RowCount"
      },
      {
        "line": 43,
        "text": "internal float TotalWidth"
      },
      {
        "line": 45,
        "text": "internal float TotalLength"
      }
    ],
    "methods": [
      {
        "line": 17,
        "text": "internal FieldGridMapper(Transform fieldRoot, int columnCount, int rowCount, float cellWidth, float cellLength)"
      },
      {
        "line": 47,
        "text": "internal bool IsInside(CellCoord cell)"
      },
      {
        "line": 52,
        "text": "internal Vector3 CellCenterToLocal(CellCoord cell)"
      },
      {
        "line": 62,
        "text": "internal Vector3 CellCenterToWorld(CellCoord cell)"
      },
      {
        "line": 68,
        "text": "internal Vector3 LocalToWorld(Vector3 localPosition)"
      },
      {
        "line": 74,
        "text": "internal Quaternion RotationToWorld(Rotation4 rotation)"
      },
      {
        "line": 79,
        "text": "internal Vector2 WorldToGrid(Vector3 worldPosition)"
      },
      {
        "line": 86,
        "text": "internal bool TryWorldToCell(Vector3 worldPosition, out CellCoord cell)"
      }
    ]
  },
  "FieldHighlightLayers": {
    "kind": "class",
    "inherits": "",
    "source": "C:/Users/User/Project_L/Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/ViewSystem/Combat/Display/FieldHighlightLayers.cs",
    "fields": [
      {
        "line": 47,
        "text": "internal static IReadOnlyList<FieldHighlightLayer> DrawOrder"
      }
    ],
    "methods": [
      {
        "line": 56,
        "text": "internal static FieldHighlightColor Resolve( bool isHovered, CombatHoverState hoverState, FieldHighlightLayer layers, bool hasStaticColor)"
      },
      {
        "line": 74,
        "text": "internal static FieldHighlightColor HoverColor(CombatHoverState hoverState)"
      },
      {
        "line": 88,
        "text": "internal static FieldHighlightColor ColorOf(FieldHighlightLayer layer)"
      }
    ]
  },
  "FieldHighlightPalette": {
    "kind": "class",
    "inherits": "",
    "source": "C:/Users/User/Project_L/Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/ViewSystem/Combat/Display/FieldHighlightPalette.cs",
    "fields": [
      {
        "line": 34,
        "text": "internal Color PlayerZone"
      },
      {
        "line": 35,
        "text": "internal Color EnemyZone"
      },
      {
        "line": 36,
        "text": "internal Color Rail"
      },
      {
        "line": 37,
        "text": "internal Color Hover"
      },
      {
        "line": 38,
        "text": "internal Color HoverValid"
      },
      {
        "line": 39,
        "text": "internal Color HoverInvalid"
      },
      {
        "line": 40,
        "text": "internal Color Affected"
      },
      {
        "line": 41,
        "text": "internal Color MovementDraft"
      },
      {
        "line": 42,
        "text": "internal Color Range"
      },
      {
        "line": 43,
        "text": "internal Color ActiveActor"
      }
    ],
    "methods": [
      {
        "line": 10,
        "text": "internal FieldHighlightPalette( Color playerZone, Color enemyZone, Color rail, Color hover, Color hoverValid, Color hoverInvalid, Color affected, Color movementDraft, Color range, Color activeActor)"
      },
      {
        "line": 46,
        "text": "internal bool TryGet(FieldHighlightColor slot, FieldStaticZone zone, out Color color)"
      },
      {
        "line": 62,
        "text": "internal bool TryGetZone(FieldStaticZone zone, out Color color)"
      }
    ]
  },
  "FieldHighlightState": {
    "kind": "class",
    "inherits": "IFieldHighlightStateReader",
    "source": "C:/Users/User/Project_L/Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/ViewSystem/Combat/Display/FieldHighlightState.cs",
    "fields": [
      {
        "line": 26,
        "text": "private static readonly int[] NoCells"
      },
      {
        "line": 28,
        "text": "private FieldHighlightLayer[] _layers"
      },
      {
        "line": 29,
        "text": "private FieldStaticZone[] _zones"
      },
      {
        "line": 30,
        "text": "private bool[] _marks"
      },
      {
        "line": 31,
        "text": "private readonly List<int> _changed"
      },
      {
        "line": 33,
        "text": "public int ColumnCount"
      },
      {
        "line": 35,
        "text": "public int RowCount"
      },
      {
        "line": 37,
        "text": "public int CellCount"
      },
      {
        "line": 40,
        "text": "public int HoverIndex"
      },
      {
        "line": 42,
        "text": "public CombatHoverState HoverState"
      },
      {
        "line": 45,
        "text": "public bool IsPlacementActive"
      },
      {
        "line": 48,
        "text": "public bool IsPathPlanning"
      },
      {
        "line": 54,
        "text": "public bool IsActing"
      }
    ],
    "methods": [
      {
        "line": 60,
        "text": "internal void Reset(int columnCount, int rowCount)"
      },
      {
        "line": 77,
        "text": "internal void Clear()"
      },
      {
        "line": 87,
        "text": "public FieldHighlightLayer LayersAt(int index)"
      },
      {
        "line": 89,
        "text": "public FieldStaticZone ZoneAt(int index)"
      },
      {
        "line": 92,
        "text": "public bool HasAnyCell(FieldHighlightLayer mask)"
      },
      {
        "line": 104,
        "text": "public bool TryGetIndex(CellCoord cell, out int index)"
      },
      {
        "line": 119,
        "text": "internal IReadOnlyList<int> SetStaticZones(int playerDeploymentDepth, int enemyDeploymentDepth, IReadOnlyList<CellCoord> railCells)"
      },
      {
        "line": 153,
        "text": "internal IReadOnlyList<int> SetHover(CellCoord cell, CombatHoverState state)"
      },
      {
        "line": 169,
        "text": "internal IReadOnlyList<int> ClearHover()"
      },
      {
        "line": 182,
        "text": "internal IReadOnlyList<int> SetLayerCells(FieldHighlightLayer layer, IReadOnlyList<CellCoord> cells)"
      },
      {
        "line": 208,
        "text": "internal IReadOnlyList<int> ClearLayer(FieldHighlightLayer layer)"
      },
      {
        "line": 224,
        "text": "internal bool SetPlacementActive(bool active)"
      },
      {
        "line": 234,
        "text": "internal bool SetPathPlanning(bool planning)"
      },
      {
        "line": 243,
        "text": "private void ResetTransient()"
      }
    ]
  },
  "FieldOutlineRenderer": {
    "kind": "class",
    "inherits": "IFieldHighlightRenderer",
    "source": "C:/Users/User/Project_L/Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/ViewSystem/Combat/Display/FieldOutlineRenderer.cs",
    "fields": [
      {
        "line": 24,
        "text": "private const float LayerHeightStep"
      },
      {
        "line": 26,
        "text": "private readonly Transform _cellsRoot"
      },
      {
        "line": 27,
        "text": "private readonly IAreaOutlineSystemService _outlines"
      },
      {
        "line": 28,
        "text": "private readonly FieldHighlightPalette _palette"
      },
      {
        "line": 29,
        "text": "private readonly FieldGridDecal _gridDecal"
      },
      {
        "line": 30,
        "text": "private readonly Material _gridDecalMaterial"
      },
      {
        "line": 31,
        "text": "private readonly Vector2 _cellSpacing"
      },
      {
        "line": 32,
        "text": "private readonly float _gridLineWidth"
      },
      {
        "line": 33,
        "text": "private readonly float _gridDecalDepth"
      },
      {
        "line": 34,
        "text": "private readonly float _thickness"
      },
      {
        "line": 35,
        "text": "private readonly float _yOffset"
      },
      {
        "line": 36,
        "text": "private readonly float _dashLength"
      },
      {
        "line": 37,
        "text": "private readonly float _dashFill"
      },
      {
        "line": 38,
        "text": "private readonly Slot[] _slots"
      },
      {
        "line": 39,
        "text": "private readonly List<Vector2Int> _scratch"
      },
      {
        "line": 41,
        "text": "private IFieldHighlightStateReader _state"
      },
      {
        "line": 42,
        "text": "private FieldGridMapper _mapper"
      },
      {
        "line": 43,
        "text": "private FieldCellSpacing _spacing"
      },
      {
        "line": 44,
        "text": "private Vector3 _gridOrigin"
      },
      {
        "line": 45,
        "text": "private bool _isBuilt"
      },
      {
        "line": 46,
        "text": "private bool _isRotated"
      },
      {
        "line": 47,
        "text": "private bool _warnedRotation"
      },
      {
        "line": 48,
        "text": "private bool _isDecalShown"
      }
    ],
    "methods": [
      {
        "line": 50,
        "text": "internal FieldOutlineRenderer(Transform cellsRoot, StageFieldViewData config, IAreaOutlineSystemService outlines, FieldHighlightPalette palette)"
      },
      {
        "line": 86,
        "text": "public bool CanReuse(StageFieldViewData config)"
      },
      {
        "line": 93,
        "text": "public FieldCellSpacing MeasureSpacing()"
      },
      {
        "line": 98,
        "text": "public void Build(FieldGridMapper mapper, IFieldHighlightStateReader state)"
      },
      {
        "line": 133,
        "text": "public void Repaint(IReadOnlyList<int> changedCells)"
      },
      {
        "line": 141,
        "text": "public void Refresh()"
      },
      {
        "line": 162,
        "text": "public void Release()"
      },
      {
        "line": 183,
        "text": "public void Dispose()"
      },
      {
        "line": 196,
        "text": "private Color Collect(Slot slot)"
      },
      {
        "line": 230,
        "text": "private void AddCell(int index)"
      },
      {
        "line": 237,
        "text": "private void Apply(Slot slot, int heightIndex, Color color)"
      },
      {
        "line": 258,
        "text": "private void SetDecalVisible(bool visible)"
      },
      {
        "line": 274,
        "text": "private void ReleaseHandles(ref List<Exception> errors)"
      }
    ]
  },
  "FieldUnitPresenter": {
    "kind": "class",
    "inherits": "MonoBehaviour",
    "source": "C:/Users/User/Project_L/Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/ViewSystem/Combat/Display/FieldUnitPresenter.cs",
    "fields": [
      {
        "line": 29,
        "text": "private Transform unitsRoot"
      },
      {
        "line": 31,
        "text": "private TMP_Text healthLabelTemplate"
      },
      {
        "line": 32,
        "text": "private Vector3 healthLabelOffset"
      },
      {
        "line": 34,
        "text": "private UnitHUD unitHudPrefab"
      },
      {
        "line": 35,
        "text": "private Vector3 unitHudOffset"
      },
      {
        "line": 36,
        "text": "private float unitHudScale"
      },
      {
        "line": 38,
        "text": "private readonly Dictionary<EntityKey, CustomPool<FieldUnitView>> _pools"
      },
      {
        "line": 39,
        "text": "private EntityViewSet<FieldUnitView> _views"
      },
      {
        "line": 40,
        "text": "private IUnitVisualTableQuery _visuals"
      },
      {
        "line": 41,
        "text": "private float _unitScale"
      },
      {
        "line": 42,
        "text": "private EntityId _hiddenEntityId"
      },
      {
        "line": 43,
        "text": "private ITraitVisualTableQuery _traitVisuals"
      },
      {
        "line": 44,
        "text": "private Camera _hudCamera"
      },
      {
        "line": 45,
        "text": "private EntityId _activeActorId"
      },
      {
        "line": 46,
        "text": "private readonly Dictionary<EntityId, int> _turnOrders"
      },
      {
        "line": 47,
        "text": "private readonly List<UnitHUDStatus> _hudStatuses"
      },
      {
        "line": 226,
        "text": "private static readonly Quaternion EnemyFacing"
      }
    ],
    "methods": [
      {
        "line": 50,
        "text": "internal void Configure(IUnitVisualTableQuery visuals, float unitScale, ITraitVisualTableQuery traitVisuals = null, Camera hudCamera = null)"
      },
      {
        "line": 64,
        "text": "internal void Render(IReadOnlyList<CharacterData> characters, IReadOnlyList<StructureCombatFacts> structures, FieldGridMapper mapper)"
      },
      {
        "line": 67,
        "text": "internal void RenderFacts(IReadOnlyList<CharacterCombatFacts> characters, IReadOnlyList<StructureCombatFacts> structures, FieldGridMapper mapper)"
      },
      {
        "line": 123,
        "text": "private void RenderUnitHud(FieldUnitView view, CharacterCombatFacts character)"
      },
      {
        "line": 149,
        "text": "internal void SetHudTimeline(EntityId activeActor, IReadOnlyList<CombatTimelineEntrySnapshot> timeline)"
      },
      {
        "line": 164,
        "text": "internal void ShowHudCostPreview(EntityId actorId, int cost)"
      },
      {
        "line": 170,
        "text": "internal void ClearHudPreview(EntityId actorId)"
      },
      {
        "line": 177,
        "text": "internal void SetHiddenEntity(EntityId entityId)"
      },
      {
        "line": 187,
        "text": "internal void ClearHiddenEntity()"
      },
      {
        "line": 193,
        "text": "internal void Release()"
      },
      {
        "line": 202,
        "text": "internal string FindMissingReference()"
      },
      {
        "line": 211,
        "text": "internal void Interpolate(CharacterCombatFacts from, CharacterCombatFacts to, FieldGridMapper mapper, float progress)"
      },
      {
        "line": 218,
        "text": "private void SetActive(EntityId entityId, bool active)"
      },
      {
        "line": 232,
        "text": "internal static Quaternion ModelRotation(Owner owner, Rotation4? rotation, FieldGridMapper mapper)"
      },
      {
        "line": 238,
        "text": "private static Vector3 CenterOf(IReadOnlyList<CellCoord> cells, CellCoord anchor, FieldGridMapper mapper)"
      },
      {
        "line": 249,
        "text": "private FieldUnitView Rent(EntityKey key)"
      },
      {
        "line": 261,
        "text": "private void Return(EntityKey key, FieldUnitView view)"
      },
      {
        "line": 267,
        "text": "private FieldUnitView CreateView(EntityKey key)"
      }
    ]
  },
  "FieldUnitView": {
    "kind": "class",
    "inherits": "MonoBehaviour",
    "source": "C:/Users/User/Project_L/Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/ViewSystem/Combat/Display/FieldUnitView.cs",
    "fields": [
      {
        "line": 18,
        "text": "private TMP_Text _healthLabel"
      },
      {
        "line": 19,
        "text": "private Quaternion _healthLabelRotation"
      },
      {
        "line": 20,
        "text": "private bool _hasShownHealth"
      },
      {
        "line": 21,
        "text": "private int _shownHealth"
      },
      {
        "line": 22,
        "text": "private int _shownMaxHealth"
      },
      {
        "line": 23,
        "text": "private UnitHUD _unitHud"
      },
      {
        "line": 24,
        "text": "private Canvas _hudCanvas"
      },
      {
        "line": 25,
        "text": "private Camera _hudCamera"
      },
      {
        "line": 26,
        "text": "private Vector3 _hudOffset"
      },
      {
        "line": 27,
        "text": "private float _hudScale"
      },
      {
        "line": 28,
        "text": "internal UnitHUD Hud"
      },
      {
        "line": 30,
        "text": "internal EntityId EntityId"
      },
      {
        "line": 32,
        "text": "internal EntityKey EntityKey"
      },
      {
        "line": 34,
        "text": "internal TMP_Text HealthLabel"
      }
    ],
    "methods": [
      {
        "line": 36,
        "text": "internal void AttachUnitHud(UnitHUD prefab, Vector3 offset, float scale, Camera camera)"
      },
      {
        "line": 54,
        "text": "internal void ShowUnitHud(bool show)"
      },
      {
        "line": 60,
        "text": "private void LateUpdate()"
      },
      {
        "line": 76,
        "text": "internal void AttachHealthLabel(TMP_Text template, Vector3 localOffset)"
      },
      {
        "line": 90,
        "text": "internal void Bind(EntityId entityId, EntityKey entityKey)"
      },
      {
        "line": 101,
        "text": "internal bool Apply(Vector3 worldPosition, Quaternion worldRotation, int currentHealth, int maxHealth, bool visible)"
      },
      {
        "line": 143,
        "text": "internal void ResetForPool()"
      }
    ]
  },
  "FieldView": {
    "kind": "class",
    "inherits": "MonoBehaviour",
    "source": "C:/Users/User/Project_L/Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/ViewSystem/Combat/Display/FieldView.cs",
    "fields": [
      {
        "line": 24,
        "text": "private Transform cellsRoot"
      },
      {
        "line": 27,
        "text": "private Color playerZoneColor"
      },
      {
        "line": 28,
        "text": "private Color enemyZoneColor"
      },
      {
        "line": 29,
        "text": "private Color railColor"
      },
      {
        "line": 32,
        "text": "private Color hoverColor"
      },
      {
        "line": 33,
        "text": "private Color hoverValidColor"
      },
      {
        "line": 34,
        "text": "private Color hoverInvalidColor"
      },
      {
        "line": 36,
        "text": "private Color affectedColor"
      },
      {
        "line": 38,
        "text": "private Color movementDraftColor"
      },
      {
        "line": 40,
        "text": "private Color rangeColor"
      },
      {
        "line": 42,
        "text": "private Color activeActorColor"
      },
      {
        "line": 44,
        "text": "private readonly FieldHighlightState _state"
      },
      {
        "line": 45,
        "text": "private IFieldHighlightRenderer _renderer"
      },
      {
        "line": 46,
        "text": "private FieldGridMapper _mapper"
      },
      {
        "line": 49,
        "text": "internal FieldGridMapper Mapper"
      },
      {
        "line": 52,
        "text": "internal Transform CellsRoot"
      },
      {
        "line": 55,
        "text": "internal IFieldHighlightStateReader HighlightState"
      }
    ],
    "methods": [
      {
        "line": 61,
        "text": "internal void Build(int width, int height, StageFieldViewData config, IColorSystemService colors, IAreaOutlineSystemService outlines)"
      },
      {
        "line": 99,
        "text": "internal void ApplyStaticHighlights(int playerDeploymentDepth, int enemyDeploymentDepth, IReadOnlyList<CellCoord> railCells)"
      },
      {
        "line": 110,
        "text": "internal void SetHoverCell(CellCoord cell, CombatHoverState state)"
      },
      {
        "line": 118,
        "text": "internal void ClearHoverCell()"
      },
      {
        "line": 127,
        "text": "internal void SetLayerCells(FieldHighlightLayer layer, IReadOnlyList<CellCoord> cells)"
      },
      {
        "line": 138,
        "text": "internal void ClearLayer(FieldHighlightLayer layer)"
      },
      {
        "line": 147,
        "text": "internal void SetPlacementActive(bool active)"
      },
      {
        "line": 157,
        "text": "internal void SetPathPlanning(bool planning)"
      },
      {
        "line": 167,
        "text": "internal bool TryRayToCell(Ray ray, out CellCoord cell)"
      },
      {
        "line": 179,
        "text": "internal bool TryRayToFieldPlane(Ray ray, out Vector3 point)"
      },
      {
        "line": 202,
        "text": "internal void Release()"
      },
      {
        "line": 209,
        "text": "private void OnDestroy()"
      },
      {
        "line": 216,
        "text": "internal string FindMissingReference()"
      },
      {
        "line": 221,
        "text": "private void EnsureRenderer(StageFieldViewData config, IColorSystemService colors, IAreaOutlineSystemService outlines)"
      },
      {
        "line": 243,
        "text": "private FieldHighlightPalette CreatePalette()"
      },
      {
        "line": 249,
        "text": "private void ValidateCellsRoot()"
      },
      {
        "line": 259,
        "text": "private bool HasUnitWorldScale()"
      },
      {
        "line": 265,
        "text": "private bool IsHorizontal()"
      },
      {
        "line": 271,
        "text": "private void OnValidate()"
      }
    ]
  },
  "HandPresenter": {
    "kind": "class",
    "inherits": "MonoBehaviour",
    "source": "C:/Users/User/Project_L/Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/ViewSystem/Combat/Display/HandPresenter.cs",
    "fields": [
      {
        "line": 27,
        "text": "private RectTransform cardRoot"
      },
      {
        "line": 29,
        "text": "private UnitCardView cardPrefab"
      },
      {
        "line": 31,
        "text": "private float cardScale"
      },
      {
        "line": 33,
        "text": "private float cardGap"
      },
      {
        "line": 35,
        "text": "private float bottomMargin"
      },
      {
        "line": 37,
        "text": "private float sideMargin"
      },
      {
        "line": 39,
        "text": "private CustomPool<UnitCardView> _pool"
      },
      {
        "line": 40,
        "text": "private readonly List<CharacterData> _ordered"
      },
      {
        "line": 41,
        "text": "private readonly Comparison<CharacterData> _bySlot"
      },
      {
        "line": 43,
        "text": "private EntityViewSet<UnitCardView> _views"
      },
      {
        "line": 44,
        "text": "private IUnitVisualTableQuery _visuals"
      },
      {
        "line": 45,
        "text": "private Camera _eventCamera"
      },
      {
        "line": 46,
        "text": "private EntityId _hiddenEntityId"
      }
    ],
    "methods": [
      {
        "line": 49,
        "text": "internal void Configure(IUnitVisualTableQuery visuals)"
      },
      {
        "line": 63,
        "text": "internal void Render(IReadOnlyList<CharacterData> characters, bool visible)"
      },
      {
        "line": 105,
        "text": "internal void SetHiddenEntity(EntityId entityId)"
      },
      {
        "line": 115,
        "text": "internal void ClearHiddenEntity()"
      },
      {
        "line": 120,
        "text": "internal void Release()"
      },
      {
        "line": 131,
        "text": "internal bool TryPickUnit(Vector2 screen, out EntityId entityId)"
      },
      {
        "line": 155,
        "text": "internal bool ContainsDropPoint(Vector2 screen)"
      },
      {
        "line": 160,
        "text": "internal string FindMissingReference()"
      },
      {
        "line": 167,
        "text": "private bool IsShown()"
      },
      {
        "line": 169,
        "text": "private void ApplyContent(UnitCardView card, CharacterData character)"
      },
      {
        "line": 185,
        "text": "private void ApplyLayout(UnitCardView card, int index, int count)"
      },
      {
        "line": 203,
        "text": "private static void SetCardActive(UnitCardView card, bool active)"
      },
      {
        "line": 209,
        "text": "private void SetActive(EntityId entityId, bool active)"
      },
      {
        "line": 217,
        "text": "private UnitCardView CreateCard()"
      }
    ]
  },
  "IFieldHighlightRenderer": {
    "kind": "interface",
    "inherits": "IDisposable",
    "source": "C:/Users/User/Project_L/Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/ViewSystem/Combat/Display/IFieldHighlightRenderer.cs",
    "fields": [],
    "methods": [
      {
        "line": 14,
        "text": "bool CanReuse(StageFieldViewData config)"
      },
      {
        "line": 17,
        "text": "FieldCellSpacing MeasureSpacing()"
      },
      {
        "line": 20,
        "text": "void Build(FieldGridMapper mapper, IFieldHighlightStateReader state)"
      },
      {
        "line": 23,
        "text": "void Repaint(IReadOnlyList<int> changedCells)"
      },
      {
        "line": 26,
        "text": "void Refresh()"
      },
      {
        "line": 32,
        "text": "void Release()"
      }
    ]
  },
  "IFieldHighlightStateReader": {
    "kind": "interface",
    "inherits": "",
    "source": "C:/Users/User/Project_L/Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/ViewSystem/Combat/Display/IFieldHighlightStateReader.cs",
    "fields": [
      {
        "line": 13,
        "text": "int ColumnCount"
      },
      {
        "line": 15,
        "text": "int RowCount"
      },
      {
        "line": 17,
        "text": "int CellCount"
      },
      {
        "line": 20,
        "text": "int HoverIndex"
      },
      {
        "line": 22,
        "text": "CombatHoverState HoverState"
      },
      {
        "line": 25,
        "text": "bool IsPlacementActive"
      },
      {
        "line": 28,
        "text": "bool IsPathPlanning"
      },
      {
        "line": 31,
        "text": "bool IsActing"
      }
    ],
    "methods": [
      {
        "line": 33,
        "text": "FieldHighlightLayer LayersAt(int index)"
      },
      {
        "line": 35,
        "text": "FieldStaticZone ZoneAt(int index)"
      },
      {
        "line": 38,
        "text": "bool HasAnyCell(FieldHighlightLayer mask)"
      },
      {
        "line": 41,
        "text": "bool TryGetIndex(CellCoord cell, out int index)"
      }
    ]
  },
  "PlacementGhost": {
    "kind": "class",
    "inherits": "MonoBehaviour",
    "source": "C:/Users/User/Project_L/Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/ViewSystem/Combat/Display/PlacementGhost.cs",
    "fields": [
      {
        "line": 25,
        "text": "private Transform ghostRoot"
      },
      {
        "line": 27,
        "text": "private readonly Dictionary<GameObject, CustomPool<GhostInstance>> _pools"
      },
      {
        "line": 28,
        "text": "private IUnitVisualTableQuery _visuals"
      },
      {
        "line": 29,
        "text": "private StageFieldViewData _config"
      },
      {
        "line": 30,
        "text": "private IColorSystemService _colors"
      },
      {
        "line": 31,
        "text": "private GhostInstance _active"
      }
    ],
    "methods": [
      {
        "line": 33,
        "text": "internal void Configure(IUnitVisualTableQuery visuals, StageFieldViewData config, IColorSystemService colors)"
      },
      {
        "line": 41,
        "text": "internal void Show(EntityKey entityKey)"
      },
      {
        "line": 55,
        "text": "internal void MoveTo(Vector3 worldPosition)"
      },
      {
        "line": 61,
        "text": "internal void SetPlaceable(bool placeable)"
      },
      {
        "line": 67,
        "text": "internal void Hide()"
      },
      {
        "line": 78,
        "text": "internal string FindMissingReference()"
      },
      {
        "line": 83,
        "text": "private GhostInstance Rent(GameObject prefab)"
      },
      {
        "line": 95,
        "text": "private GhostInstance Create(GameObject prefab)"
      }
    ]
  },
  "StageViewPropPresenter": {
    "kind": "class",
    "inherits": "",
    "source": "C:/Users/User/Project_L/Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/ViewSystem/Combat/Display/StageViewPropPresenter.cs",
    "fields": [
      {
        "line": 19,
        "text": "private readonly IStageViewPropTableQuery _props"
      },
      {
        "line": 20,
        "text": "private readonly Transform _parent"
      },
      {
        "line": 21,
        "text": "private GameObject _instance"
      },
      {
        "line": 22,
        "text": "private int _stageKey"
      }
    ],
    "methods": [
      {
        "line": 24,
        "text": "internal StageViewPropPresenter(IStageViewPropTableQuery props, Transform parent)"
      },
      {
        "line": 33,
        "text": "internal void Show(int stageKey)"
      },
      {
        "line": 50,
        "text": "internal void Release()"
      }
    ]
  },
  "ICombatAbandonDisplay": {
    "kind": "interface",
    "inherits": "",
    "source": "C:/Users/User/Project_L/Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/ViewSystem/Combat/ICombatAbandonDisplay.cs",
    "fields": [],
    "methods": [
      {
        "line": 16,
        "text": "void BindAbandon(Action open, Action confirm, Action cancel)"
      },
      {
        "line": 19,
        "text": "void SetAbandonAvailable(bool visible, bool interactable)"
      },
      {
        "line": 21,
        "text": "void ShowAbandonConfirm()"
      },
      {
        "line": 23,
        "text": "void HideAbandonConfirm()"
      }
    ]
  },
  "ICombatBattleDisplay": {
    "kind": "interface",
    "inherits": "ICombatPointerPicker, ICombatInteractionFeedback, ICombatMovementPresenter, ICombatPointerMapping, ICombatResultPresenter, ICombatPresentationControl, ICombatAbandonDisplay, ICombatDevLayerDisplay",
    "source": "C:/Users/User/Project_L/Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/ViewSystem/Combat/ICombatBattleDisplay.cs",
    "fields": [],
    "methods": [
      {
        "line": 20,
        "text": "void BindReturn(Action requested)"
      },
      {
        "line": 21,
        "text": "void ShowCompletion(BattleCompletionChanges completion, Action confirmed)"
      },
      {
        "line": 23,
        "text": "void Install(in CombatDisplayValues values)"
      },
      {
        "line": 26,
        "text": "void Refresh(in CombatDisplayValues values)"
      },
      {
        "line": 29,
        "text": "void Clear()"
      },
      {
        "line": 32,
        "text": "void Release()"
      }
    ]
  },
  "ICombatDevLayerDisplay": {
    "kind": "interface",
    "inherits": "",
    "source": "C:/Users/User/Project_L/Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/ViewSystem/Combat/ICombatDevLayerDisplay.cs",
    "fields": [],
    "methods": [
      {
        "line": 13,
        "text": "void ShowDevLayerPanel(Action onDeployment, Action onCombat)"
      },
      {
        "line": 16,
        "text": "void HideDevLayerPanel()"
      }
    ]
  },
  "ICombatInteractionFeedback": {
    "kind": "interface",
    "inherits": "",
    "source": "C:/Users/User/Project_L/Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/ViewSystem/Combat/ICombatInteractionFeedback.cs",
    "fields": [],
    "methods": [
      {
        "line": 17,
        "text": "void ShowGhost(EntityKey entityKey)"
      },
      {
        "line": 19,
        "text": "void MoveGhostToCell(CellCoord cell)"
      },
      {
        "line": 22,
        "text": "void MoveGhostToPointer(float screenX, float screenY)"
      },
      {
        "line": 25,
        "text": "void SetGhostPlaceable(bool placeable)"
      },
      {
        "line": 27,
        "text": "void HideGhost()"
      },
      {
        "line": 30,
        "text": "void HideOriginal(EntityId entityId)"
      },
      {
        "line": 32,
        "text": "void ShowOriginal()"
      },
      {
        "line": 35,
        "text": "void SetHoverCell(CellCoord cell, CombatHoverState state)"
      },
      {
        "line": 37,
        "text": "void ClearHoverCell()"
      },
      {
        "line": 40,
        "text": "void ShowRangeCells(IReadOnlyList<CellCoord> cells)"
      },
      {
        "line": 42,
        "text": "void ClearRangeCells()"
      },
      {
        "line": 45,
        "text": "void ShowAffectedCells(IReadOnlyList<CellCoord> cells)"
      },
      {
        "line": 47,
        "text": "void ClearAffectedCells()"
      },
      {
        "line": 49,
        "text": "void ShowAttackBlockers(IReadOnlyList<AttackBlockedContact> contacts)"
      },
      {
        "line": 50,
        "text": "void ClearAttackBlockers()"
      },
      {
        "line": 51,
        "text": "void ShowAttackCostPreview(EntityId actorId, int cost)"
      }
    ]
  },
  "ICombatMovementFeedback": {
    "kind": "interface",
    "inherits": "",
    "source": "C:/Users/User/Project_L/Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/ViewSystem/Combat/ICombatMovementFeedback.cs",
    "fields": [],
    "methods": [
      {
        "line": 12,
        "text": "void ShowDraft(MovementPath path, MovePreview? preview, bool pointerValid, CellCoord? candidate = null)"
      },
      {
        "line": 13,
        "text": "void ClearDraft()"
      },
      {
        "line": 14,
        "text": "void SetReturnState(CheckpointReturnPreview preview)"
      },
      {
        "line": 17,
        "text": "void SetPathPlanning(bool planning)"
      }
    ]
  },
  "ICombatMovementPlaybackPresenter": {
    "kind": "interface",
    "inherits": "",
    "source": "C:/Users/User/Project_L/Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/ViewSystem/Combat/ICombatMovementPlaybackPresenter.cs",
    "fields": [
      {
        "line": 11,
        "text": "bool IsMovementComplete"
      }
    ],
    "methods": [
      {
        "line": 8,
        "text": "void BeginStep(CombatCharacterEvent step)"
      },
      {
        "line": 9,
        "text": "void ShowReturn(CombatCharacterEvent returned)"
      },
      {
        "line": 12,
        "text": "void CancelMovement()"
      }
    ]
  },
  "ICombatMovementPresenter": {
    "kind": "interface",
    "inherits": "ICombatMovementFeedback, ICombatMovementPlaybackPresenter",
    "source": "C:/Users/User/Project_L/Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/ViewSystem/Combat/ICombatMovementPresenter.cs",
    "fields": [],
    "methods": []
  },
  "ICombatPointerMapping": {
    "kind": "interface",
    "inherits": "",
    "source": "C:/Users/User/Project_L/Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/ViewSystem/Combat/ICombatMovementPresenter.cs",
    "fields": [
      {
        "line": 14,
        "text": "float InnerCellInset"
      }
    ],
    "methods": [
      {
        "line": 15,
        "text": "bool TryMapPointer(Vector2 screen, out Vector2 grid, out bool overCancelArea)"
      }
    ]
  },
  "ICombatResultPresenter": {
    "kind": "interface",
    "inherits": "",
    "source": "C:/Users/User/Project_L/Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/ViewSystem/Combat/ICombatMovementPresenter.cs",
    "fields": [],
    "methods": [
      {
        "line": 21,
        "text": "bool CanPresent(CombatEvent combatEvent)"
      },
      {
        "line": 23,
        "text": "void BeginEvent(CombatEvent combatEvent, Action completed)"
      }
    ]
  },
  "ICombatPointerPicker": {
    "kind": "interface",
    "inherits": "",
    "source": "C:/Users/User/Project_L/Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/ViewSystem/Combat/ICombatPointerPicker.cs",
    "fields": [],
    "methods": [
      {
        "line": 14,
        "text": "bool TryPickCell(float screenX, float screenY, out CellCoord cell)"
      },
      {
        "line": 17,
        "text": "bool TryPickHandUnit(float screenX, float screenY, out EntityId entityId)"
      },
      {
        "line": 20,
        "text": "bool IsOverHandDropZone(float screenX, float screenY)"
      }
    ]
  },
  "ICombatPresentation": {
    "kind": "interface",
    "inherits": "",
    "source": "C:/Users/User/Project_L/Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/ViewSystem/Combat/ICombatPresentation.cs",
    "fields": [],
    "methods": [
      {
        "line": 16,
        "text": "void Attach(ICombatBattleDisplay display)"
      },
      {
        "line": 19,
        "text": "void Tick(float deltaTime)"
      },
      {
        "line": 22,
        "text": "void Detach()"
      },
      {
        "line": 25,
        "text": "void Play(CombatResult result, Action<CombatPresentationOutcome> onFinished)"
      },
      {
        "line": 28,
        "text": "void Cancel()"
      }
    ]
  },
  "ICombatPresentationControl": {
    "kind": "interface",
    "inherits": "",
    "source": "C:/Users/User/Project_L/Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/ViewSystem/Combat/ICombatPresentationControl.cs",
    "fields": [],
    "methods": [
      {
        "line": 6,
        "text": "void TickPresentation(float deltaTime)"
      },
      {
        "line": 7,
        "text": "void CancelPresentation()"
      }
    ]
  },
  "ICombatViewService": {
    "kind": "interface",
    "inherits": "IServiceModule, ICombatExitPresentation",
    "source": "C:/Users/User/Project_L/Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/ViewSystem/Combat/ICombatViewService.cs",
    "fields": [],
    "methods": [
      {
        "line": 7,
        "text": "void Attach(CombatSceneRoot scene)"
      },
      {
        "line": 8,
        "text": "void Detach()"
      },
      {
        "line": 9,
        "text": "void FocusLost()"
      },
      {
        "line": 10,
        "text": "void CaptureLost()"
      },
      {
        "line": 11,
        "text": "void RequestReturn()"
      }
    ]
  },
  "OrderedCombatPresentation": {
    "kind": "class",
    "inherits": "ICombatPresentation",
    "source": "C:/Users/User/Project_L/Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/ViewSystem/Combat/OrderedCombatPresentation.cs",
    "fields": [
      {
        "line": 17,
        "text": "private ICombatMovementPlaybackPresenter _movementPresenter"
      },
      {
        "line": 18,
        "text": "private ICombatResultPresenter _resultPresenter"
      },
      {
        "line": 19,
        "text": "private ICombatPresentationControl _control"
      },
      {
        "line": 20,
        "text": "private CombatMovementPlayback _movement"
      },
      {
        "line": 21,
        "text": "private CombatResult _result"
      },
      {
        "line": 22,
        "text": "private Action<CombatPresentationOutcome> _finished"
      },
      {
        "line": 23,
        "text": "private int _index"
      },
      {
        "line": 24,
        "text": "private int _generation"
      },
      {
        "line": 25,
        "text": "private bool _started"
      },
      {
        "line": 26,
        "text": "private bool _completed"
      },
      {
        "line": 27,
        "text": "private bool _isCancelling"
      }
    ],
    "methods": [
      {
        "line": 29,
        "text": "public void Attach(ICombatBattleDisplay display)"
      },
      {
        "line": 41,
        "text": "public void Play(CombatResult result, Action<CombatPresentationOutcome> onFinished)"
      },
      {
        "line": 60,
        "text": "public void Tick(float deltaTime)"
      },
      {
        "line": 119,
        "text": "public void Detach()"
      },
      {
        "line": 134,
        "text": "public void Cancel()"
      },
      {
        "line": 178,
        "text": "private static bool IsMovement(CombatEvent e)"
      }
    ]
  },
  "PendingInputCommand": {
    "kind": "struct",
    "inherits": "",
    "source": "C:/Users/User/Project_L/Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/ViewSystem/Combat/PendingInputCommand.cs",
    "fields": [
      {
        "line": 23,
        "text": "internal CombatViewCommandKind Kind"
      },
      {
        "line": 25,
        "text": "internal int Number"
      }
    ],
    "methods": [
      {
        "line": 12,
        "text": "internal PendingInputCommand(CombatViewCommandKind kind, int number)"
      }
    ]
  },
  "PlacementDragInteraction": {
    "kind": "class",
    "inherits": "",
    "source": "C:/Users/User/Project_L/Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/ViewSystem/Combat/PlacementDragInteraction.cs",
    "fields": [
      {
        "line": 23,
        "text": "private const float ClickMovementThreshold"
      },
      {
        "line": 25,
        "text": "private readonly ICombatViewBufferWriteSink _input"
      },
      {
        "line": 26,
        "text": "private readonly ICombatViewBufferReadSink _requests"
      },
      {
        "line": 27,
        "text": "private readonly ICombatPointerPicker _picker"
      },
      {
        "line": 28,
        "text": "private readonly ICombatInteractionFeedback _feedback"
      },
      {
        "line": 29,
        "text": "private readonly CombatCommandGate _gate"
      },
      {
        "line": 30,
        "text": "private readonly CombatHoverTracker _hover"
      },
      {
        "line": 32,
        "text": "private bool _dragging"
      },
      {
        "line": 33,
        "text": "private EntityId _dragEntity"
      },
      {
        "line": 34,
        "text": "private bool _dragIsMain"
      },
      {
        "line": 35,
        "text": "private bool _dragFromField"
      },
      {
        "line": 36,
        "text": "private Rotation4 _dragRotation"
      },
      {
        "line": 37,
        "text": "private bool _pressStartedDrag"
      },
      {
        "line": 38,
        "text": "private float _pressX"
      },
      {
        "line": 39,
        "text": "private float _pressY"
      },
      {
        "line": 40,
        "text": "private int _previewRequestNumber"
      },
      {
        "line": 58,
        "text": "internal bool IsDragging"
      }
    ],
    "methods": [
      {
        "line": 42,
        "text": "internal PlacementDragInteraction( ICombatViewBufferWriteSink input, ICombatViewBufferReadSink requests, ICombatPointerPicker picker, ICombatInteractionFeedback feedback, CombatCommandGate gate)"
      },
      {
        "line": 60,
        "text": "internal void PointerMoved(in CombatInputFacts facts, float screenX, float screenY)"
      },
      {
        "line": 75,
        "text": "internal void SelectPressed(in CombatInputFacts facts, IReadOnlyList<CharacterData> characters, float screenX, float screenY)"
      },
      {
        "line": 88,
        "text": "internal void SelectReleased(in CombatInputFacts facts, float screenX, float screenY)"
      },
      {
        "line": 106,
        "text": "internal bool PreviewAnswered(CombatPreviewResult answer)"
      },
      {
        "line": 123,
        "text": "internal void AfterResult(in CombatInputFacts facts, IReadOnlyList<CharacterData> characters)"
      },
      {
        "line": 140,
        "text": "internal void Cancel()"
      },
      {
        "line": 146,
        "text": "internal void CancelPresentation()"
      },
      {
        "line": 152,
        "text": "private void TryBeginPlacement(in CombatInputFacts facts, IReadOnlyList<CharacterData> characters, float screenX, float screenY)"
      },
      {
        "line": 178,
        "text": "private void BeginDrag(CharacterData character, bool isMain, bool fromField)"
      },
      {
        "line": 193,
        "text": "private void UpdateDragHover(in CombatInputFacts facts, float screenX, float screenY)"
      },
      {
        "line": 211,
        "text": "private void RequestDragPreview(in CombatInputFacts facts, CellCoord cell)"
      },
      {
        "line": 222,
        "text": "private void CommitDrag(in CombatInputFacts facts, float screenX, float screenY)"
      },
      {
        "line": 239,
        "text": "private void EndDrag()"
      },
      {
        "line": 257,
        "text": "private void ClearOwnedPreview()"
      },
      {
        "line": 264,
        "text": "private bool IsDragStillValid(in CombatInputFacts facts, IReadOnlyList<CharacterData> characters)"
      },
      {
        "line": 274,
        "text": "private bool IsClick(float screenX, float screenY)"
      }
    ]
  },
  "DevBattleInitializationSource": {
    "kind": "class",
    "inherits": "IBattleInitializationSource",
    "source": "C:/Users/User/Project_L/Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/ViewSystem/Dev/DevBattleInitializationSource.cs",
    "fields": [
      {
        "line": 30,
        "text": "internal const int DefaultMapWidth"
      },
      {
        "line": 31,
        "text": "internal const int DefaultMapHeight"
      },
      {
        "line": 32,
        "text": "internal const int DefaultPlayerDeploymentDepth"
      },
      {
        "line": 33,
        "text": "internal const int DefaultEnemyDeploymentDepth"
      },
      {
        "line": 34,
        "text": "private const int ReserveCapacity"
      },
      {
        "line": 35,
        "text": "private const int TimelineSeed"
      },
      {
        "line": 36,
        "text": "private const long NightLength"
      },
      {
        "line": 38,
        "text": "private readonly int _mapWidth"
      },
      {
        "line": 39,
        "text": "private readonly int _mapHeight"
      },
      {
        "line": 40,
        "text": "private readonly int _playerDeploymentDepth"
      },
      {
        "line": 41,
        "text": "private readonly int _enemyDeploymentDepth"
      }
    ],
    "methods": [
      {
        "line": 44,
        "text": "internal DevBattleInitializationSource(int mapWidth = DefaultMapWidth, int mapHeight = DefaultMapHeight, int playerDeploymentDepth = DefaultPlayerDeploymentDepth, int enemyDeploymentDepth = DefaultEnemyDeploymentDepth)"
      },
      {
        "line": 60,
        "text": "public BattleInitialization Read(IDataBaseQueryContext database, int chapterKey, StageKey stageKey)"
      },
      {
        "line": 70,
        "text": "private static StageEconomySpec ReadStageEconomy(IDataBaseQueryContext database, StageKey stageKey)"
      },
      {
        "line": 83,
        "text": "private BattleInitialization CreateDefinition(StageEconomySpec economy)"
      },
      {
        "line": 151,
        "text": "private static UnitSpec Unit(EntityKey key, UnitRole role, UnitOwnerEligibility eligibility, int maxHealth, int moveRange, int moveCost, int attackRange, int attackPower, int maxActionCost, int attackCost, int priority, int delay, params TraitSpec[] traits)"
      }
    ]
  },
  "IInGameViewSystemGroupService": {
    "kind": "interface",
    "inherits": "IServiceModule",
    "source": "C:/Users/User/Project_L/Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/ViewSystem/InGameViewSystemGroup.cs",
    "fields": [
      {
        "line": 11,
        "text": "ICombatViewService Combat"
      }
    ],
    "methods": []
  },
  "InGameViewSystemGroup": {
    "kind": "class",
    "inherits": "SystemGroup, IInGameViewSystemGroupService, ISystemBindScene, ISystemInitData, ISystemPostBindService, ISystemInitialize, ISystemTick, ISystemShutdown",
    "source": "C:/Users/User/Project_L/Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/ViewSystem/InGameViewSystemGroup.cs",
    "fields": [
      {
        "line": 28,
        "text": "public ICombatViewService Combat"
      }
    ],
    "methods": [
      {
        "line": 29,
        "text": "public InGameViewSystemGroup(ISystemReader systemReader, ICombatViewService combat)"
      },
      {
        "line": 34,
        "text": "public void BindScene()"
      },
      {
        "line": 40,
        "text": "public void InitData(IServiceModule query)"
      },
      {
        "line": 46,
        "text": "public void PostBindService(ISystemServiceStoreReader resolver)"
      },
      {
        "line": 52,
        "text": "public void Initialize()"
      },
      {
        "line": 58,
        "text": "public void Tick(float deltaTime)"
      },
      {
        "line": 64,
        "text": "public void Shutdown()"
      },
      {
        "line": 71,
        "text": "public override void ExportServices(ISystemWriteSink sink)"
      }
    ]
  },
  "CombatInputController": {
    "kind": "class",
    "inherits": "InputController",
    "source": "C:/Users/User/Project_L/Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/ViewSystem/StageInput/CombatInputController.cs",
    "fields": [
      {
        "line": 11,
        "text": "private readonly IStageInputReceiver _receiver"
      },
      {
        "line": 12,
        "text": "private readonly StageSelectInput _select"
      },
      {
        "line": 14,
        "text": "public override string ActionMap"
      }
    ],
    "methods": [
      {
        "line": 16,
        "text": "internal CombatInputController(IStageInputReceiver receiver)"
      },
      {
        "line": 22,
        "text": "protected override void Configure(InputBindings input)"
      }
    ]
  },
  "DeploymentInputController": {
    "kind": "class",
    "inherits": "InputController",
    "source": "C:/Users/User/Project_L/Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/ViewSystem/StageInput/DeploymentInputController.cs",
    "fields": [
      {
        "line": 11,
        "text": "private readonly IStageInputReceiver _receiver"
      },
      {
        "line": 12,
        "text": "private readonly StageSelectInput _select"
      },
      {
        "line": 14,
        "text": "public override string ActionMap"
      }
    ],
    "methods": [
      {
        "line": 16,
        "text": "internal DeploymentInputController(IStageInputReceiver receiver)"
      },
      {
        "line": 22,
        "text": "protected override void Configure(InputBindings input)"
      }
    ]
  },
  "IStageInputReceiver": {
    "kind": "interface",
    "inherits": "",
    "source": "C:/Users/User/Project_L/Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/ViewSystem/StageInput/IStageInputReceiver.cs",
    "fields": [],
    "methods": [
      {
        "line": 9,
        "text": "void PointerMoved(float screenX, float screenY)"
      },
      {
        "line": 12,
        "text": "void SelectPressed(float screenX, float screenY)"
      },
      {
        "line": 18,
        "text": "void SelectReleased(float screenX, float screenY)"
      },
      {
        "line": 24,
        "text": "void SelectCanceled()"
      },
      {
        "line": 27,
        "text": "void Cancel()"
      },
      {
        "line": 30,
        "text": "void EnterCombatRequested()"
      },
      {
        "line": 33,
        "text": "void DrawRequested()"
      },
      {
        "line": 36,
        "text": "void EndTurnRequested()"
      },
      {
        "line": 39,
        "text": "void MoveModeRequested()"
      },
      {
        "line": 42,
        "text": "void AttackModeRequested()"
      },
      {
        "line": 45,
        "text": "void RequestReturn()"
      }
    ]
  },
  "MovementDraft": {
    "kind": "class",
    "inherits": "",
    "source": "C:/Users/User/Project_L/Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/ViewSystem/StageInput/MovementDraft.cs",
    "fields": [
      {
        "line": 12,
        "text": "private readonly List<CellCoord> _cells"
      },
      {
        "line": 13,
        "text": "private MovementPath _snapshot"
      },
      {
        "line": 14,
        "text": "internal EntityId Actor"
      },
      {
        "line": 15,
        "text": "internal long Revision"
      },
      {
        "line": 16,
        "text": "internal bool PointerValid"
      },
      {
        "line": 17,
        "text": "internal bool IsActive"
      },
      {
        "line": 18,
        "text": "internal int StepCount"
      },
      {
        "line": 19,
        "text": "internal bool IsSuspended"
      },
      {
        "line": 20,
        "text": "internal CellCoord Endpoint"
      }
    ],
    "methods": [
      {
        "line": 23,
        "text": "internal static bool IsStep(CellCoord from, CellCoord to)"
      },
      {
        "line": 27,
        "text": "internal static bool CanStartAt(CellCoord origin, CellCoord cell)"
      },
      {
        "line": 29,
        "text": "internal void Begin(EntityId actor, CellCoord origin)"
      },
      {
        "line": 36,
        "text": "internal DraftEditResult Accept(CellCoord cell)"
      },
      {
        "line": 56,
        "text": "internal bool CanResumeAt(CellCoord cell, bool allowAdjacent = false)"
      },
      {
        "line": 59,
        "text": "internal void ResumeAt(CellCoord cell, bool allowAdjacent)"
      },
      {
        "line": 69,
        "text": "private int LatestNeighborIndex(CellCoord cell)"
      },
      {
        "line": 80,
        "text": "internal void Suspend()"
      },
      {
        "line": 81,
        "text": "internal void MarkPointerInvalid()"
      },
      {
        "line": 84,
        "text": "internal void RetainPrefix(int validStepCount)"
      },
      {
        "line": 95,
        "text": "internal void Cancel()"
      },
      {
        "line": 102,
        "text": "internal MovementPath Snapshot()"
      }
    ]
  },
  "MovementPointerSampler": {
    "kind": "class",
    "inherits": "",
    "source": "C:/Users/User/Project_L/Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/ViewSystem/StageInput/MovementPointerSampler.cs",
    "fields": [],
    "methods": [
      {
        "line": 11,
        "text": "internal static void AppendCrossedCells(Vector2 fromGrid, Vector2 toGrid, List<CellCoord> output)"
      },
      {
        "line": 33,
        "text": "internal static CellCoord CellAt(Vector2 grid)"
      },
      {
        "line": 41,
        "text": "internal static bool IsInside(Vector2 grid, float inset)"
      }
    ]
  },
  "StageInputLayers": {
    "kind": "class",
    "inherits": "",
    "source": "C:/Users/User/Project_L/Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/ViewSystem/StageInput/StageInputLayers.cs",
    "fields": [
      {
        "line": 25,
        "text": "internal static IReadOnlyList<InputLayer> All"
      }
    ],
    "methods": []
  },
  "StageInputNames": {
    "kind": "class",
    "inherits": "",
    "source": "C:/Users/User/Project_L/Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/ViewSystem/StageInput/StageInputNames.cs",
    "fields": [],
    "methods": []
  },
  "IStageInputReceiverSlot": {
    "kind": "interface",
    "inherits": "IServiceModule",
    "source": "C:/Users/User/Project_L/Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/ViewSystem/StageInput/StageInputReceiverSlot.cs",
    "fields": [],
    "methods": [
      {
        "line": 13,
        "text": "void Bind(IStageInputReceiver receiver)"
      },
      {
        "line": 19,
        "text": "void Unbind(IStageInputReceiver receiver)"
      }
    ]
  },
  "StageInputReceiverSlot": {
    "kind": "class",
    "inherits": "IStageInputReceiverSlot, IStageInputReceiver, IExportSystemServices",
    "source": "C:/Users/User/Project_L/Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/ViewSystem/StageInput/StageInputReceiverSlot.cs",
    "fields": [
      {
        "line": 36,
        "text": "private IStageInputReceiver _receiver"
      },
      {
        "line": 37,
        "text": "private IStageInputReceiver _selectOwner"
      }
    ],
    "methods": [
      {
        "line": 39,
        "text": "public void Bind(IStageInputReceiver receiver)"
      },
      {
        "line": 49,
        "text": "public void Unbind(IStageInputReceiver receiver)"
      },
      {
        "line": 66,
        "text": "public void PointerMoved(float screenX, float screenY)"
      },
      {
        "line": 68,
        "text": "public void SelectPressed(float screenX, float screenY)"
      },
      {
        "line": 79,
        "text": "public void SelectReleased(float screenX, float screenY)"
      },
      {
        "line": 85,
        "text": "public void SelectCanceled()"
      },
      {
        "line": 91,
        "text": "public void Cancel()"
      },
      {
        "line": 93,
        "text": "public void EnterCombatRequested()"
      },
      {
        "line": 95,
        "text": "public void DrawRequested()"
      },
      {
        "line": 97,
        "text": "public void EndTurnRequested()"
      },
      {
        "line": 99,
        "text": "public void MoveModeRequested()"
      },
      {
        "line": 101,
        "text": "public void AttackModeRequested()"
      },
      {
        "line": 103,
        "text": "public void RequestReturn()"
      },
      {
        "line": 105,
        "text": "public void ExportServices(ISystemWriteSink sink)"
      },
      {
        "line": 114,
        "text": "private IStageInputReceiver TakeSelectOwner()"
      }
    ]
  },
  "StageSelectInput": {
    "kind": "class",
    "inherits": "",
    "source": "C:/Users/User/Project_L/Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/ViewSystem/StageInput/StageSelectInput.cs",
    "fields": [
      {
        "line": 14,
        "text": "private readonly IStageInputReceiver _receiver"
      },
      {
        "line": 16,
        "text": "private float _pointerX"
      },
      {
        "line": 17,
        "text": "private float _pointerY"
      },
      {
        "line": 20,
        "text": "private bool _isPressed"
      }
    ],
    "methods": [
      {
        "line": 22,
        "text": "internal StageSelectInput(IStageInputReceiver receiver)"
      },
      {
        "line": 28,
        "text": "internal void Configure(InputBindings input, string pointerAction, string selectAction)"
      },
      {
        "line": 38,
        "text": "private void OnPointer(InputSnapshot snapshot)"
      },
      {
        "line": 48,
        "text": "private void OnSelect(InputSnapshot snapshot)"
      }
    ]
  },
  "IStageMapInteractionFeedback": {
    "kind": "interface",
    "inherits": "",
    "source": "C:/Users/User/Project_L/Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/ViewSystem/StageMap/IStageMapInteractionFeedback.cs",
    "fields": [],
    "methods": [
      {
        "line": 11,
        "text": "void HoverChanged(int previousNodeId, int currentNodeId)"
      },
      {
        "line": 14,
        "text": "void EntryRequested(int nodeId, int requestNumber)"
      }
    ]
  },
  "IStageNodePicker": {
    "kind": "interface",
    "inherits": "",
    "source": "C:/Users/User/Project_L/Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/ViewSystem/StageMap/IStageNodePicker.cs",
    "fields": [],
    "methods": [
      {
        "line": 11,
        "text": "bool TryPickNode(float screenX, float screenY, out int nodeId)"
      }
    ]
  },
  "StageLinkView": {
    "kind": "class",
    "inherits": "",
    "source": "C:/Users/User/Project_L/Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/ViewSystem/StageMap/StageLinkView.cs",
    "fields": [
      {
        "line": 13,
        "text": "private readonly List<GameObject> _instances"
      }
    ],
    "methods": [
      {
        "line": 15,
        "text": "internal void Show(IReadOnlyList<StageProgressData> nodes, StageMapViewData mapData, StageNodeView nodeView, Transform parent)"
      },
      {
        "line": 51,
        "text": "internal void Clear()"
      }
    ]
  },
  "StageMapEntryFlow": {
    "kind": "class",
    "inherits": "",
    "source": "C:/Users/User/Project_L/Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/ViewSystem/StageMap/StageMapEntryFlow.cs",
    "fields": [
      {
        "line": 11,
        "text": "private readonly StageMapViewReader _reader"
      },
      {
        "line": 12,
        "text": "private readonly IStageViewBufferReadSink _requests"
      },
      {
        "line": 13,
        "text": "private readonly IStageViewBufferWriteSink _writer"
      },
      {
        "line": 14,
        "text": "private readonly ICombatEntryService _combat"
      },
      {
        "line": 15,
        "text": "private readonly Action<string> _report"
      },
      {
        "line": 16,
        "text": "private CombatEntryRequest _pending"
      },
      {
        "line": 17,
        "text": "private int _commandNumber"
      }
    ],
    "methods": [
      {
        "line": 19,
        "text": "internal StageMapEntryFlow(StageMapViewReader reader, IStageViewBufferReadSink requests, IStageViewBufferWriteSink writer, ICombatEntryService combat, Action<string> report)"
      },
      {
        "line": 29,
        "text": "internal void Tick(bool canSubmit)"
      },
      {
        "line": 74,
        "text": "internal void Detach()"
      },
      {
        "line": 82,
        "text": "private void Finish()"
      }
    ]
  },
  "StageMapInteraction": {
    "kind": "class",
    "inherits": "IStageSelectionInputReceiver",
    "source": "C:/Users/User/Project_L/Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/ViewSystem/StageMap/StageMapInteraction.cs",
    "fields": [
      {
        "line": 18,
        "text": "private readonly IStageNodePicker _picker"
      },
      {
        "line": 19,
        "text": "private readonly IStageViewBufferReadSink _requests"
      },
      {
        "line": 20,
        "text": "private readonly IStageViewBufferWriteSink _requestWriter"
      },
      {
        "line": 21,
        "text": "private readonly IStageMapInteractionFeedback _feedback"
      },
      {
        "line": 22,
        "text": "private int _hoveredNodeId"
      },
      {
        "line": 23,
        "text": "private int _pressedNodeId"
      },
      {
        "line": 35,
        "text": "internal int HoveredNodeId"
      }
    ],
    "methods": [
      {
        "line": 25,
        "text": "internal StageMapInteraction(IStageNodePicker picker, IStageViewBufferReadSink requests, IStageViewBufferWriteSink requestWriter, IStageMapInteractionFeedback feedback)"
      },
      {
        "line": 37,
        "text": "public void PointerMoved(float screenX, float screenY)"
      },
      {
        "line": 48,
        "text": "public void SelectPressed(float screenX, float screenY)"
      },
      {
        "line": 53,
        "text": "public void SelectReleased(float screenX, float screenY)"
      },
      {
        "line": 63,
        "text": "public void SelectCanceled()"
      },
      {
        "line": 68,
        "text": "public void ConfirmRequested()"
      },
      {
        "line": 73,
        "text": "public void CancelRequested()"
      },
      {
        "line": 78,
        "text": "private void RequestEntry(int nodeId)"
      },
      {
        "line": 88,
        "text": "private int Pick(float screenX, float screenY)"
      }
    ]
  },
  "StageMapInteractionLog": {
    "kind": "class",
    "inherits": "IStageMapInteractionFeedback",
    "source": "C:/Users/User/Project_L/Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/ViewSystem/StageMap/StageMapInteractionLog.cs",
    "fields": [],
    "methods": [
      {
        "line": 11,
        "text": "public void HoverChanged(int previousNodeId, int currentNodeId)"
      },
      {
        "line": 16,
        "text": "public void EntryRequested(int nodeId, int requestNumber)"
      }
    ]
  },
  "StageMapSceneRoot": {
    "kind": "class",
    "inherits": "MonoBehaviour",
    "source": "C:/Users/User/Project_L/Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/ViewSystem/StageMap/StageMapSceneRoot.cs",
    "fields": [
      {
        "line": 12,
        "text": "private Transform mapRoot"
      },
      {
        "line": 14,
        "text": "internal Transform MapRoot"
      }
    ],
    "methods": []
  },
  "StageMapViewDisplay": {
    "kind": "class",
    "inherits": "",
    "source": "C:/Users/User/Project_L/Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/ViewSystem/StageMap/StageMapViewDisplay.cs",
    "fields": [
      {
        "line": 12,
        "text": "private readonly StageNodeView _nodes"
      },
      {
        "line": 13,
        "text": "private readonly StageLinkView _links"
      }
    ],
    "methods": [
      {
        "line": 16,
        "text": "internal void Show(IReadOnlyList<StageProgressData> nodes, StageMapViewData mapData, Func<StageProgressData, GameObject> findVisualPrefab, Transform parent)"
      },
      {
        "line": 38,
        "text": "internal bool TryGetNode(int nodeId, out Transform nodeRoot)"
      },
      {
        "line": 44,
        "text": "internal bool TryGetNodeId(Transform hit, out int nodeId)"
      },
      {
        "line": 50,
        "text": "internal void Clear()"
      }
    ]
  },
  "StageMapViewReader": {
    "kind": "class",
    "inherits": "",
    "source": "C:/Users/User/Project_L/Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/ViewSystem/StageMap/StageMapViewReader.cs",
    "fields": [
      {
        "line": 11,
        "text": "private readonly ICurrentProgressBufferReadSink _current"
      },
      {
        "line": 12,
        "text": "private readonly IStageProgressBufferReadSink _stages"
      }
    ],
    "methods": [
      {
        "line": 14,
        "text": "internal StageMapViewReader(ICurrentProgressBufferReadSink current, IStageProgressBufferReadSink stages)"
      },
      {
        "line": 26,
        "text": "internal bool TryRead(out int mapKey, out IReadOnlyList<StageProgressData> nodes)"
      }
    ]
  },
  "StageMapViewSystem": {
    "kind": "class",
    "inherits": "ISystemInitData<IDataBaseQueryContext>, ISystemPostBindService, ISystemInitialize, ISystemTick, ISystemShutdown, IExportSystemServices",
    "source": "C:/Users/User/Project_L/Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/ViewSystem/StageMap/StageMapViewSystem.cs",
    "fields": [
      {
        "line": 42,
        "text": "internal const int AttachPriority"
      },
      {
        "line": 43,
        "text": "internal const int DetachPriority"
      },
      {
        "line": 45,
        "text": "private readonly StageMapViewDisplay _display"
      },
      {
        "line": 46,
        "text": "private StageMapViewData _mapData"
      },
      {
        "line": 47,
        "text": "private Func<StageProgressData, GameObject> _findVisualPrefab"
      },
      {
        "line": 48,
        "text": "private ILodingRegisterService _loadingRegister"
      },
      {
        "line": 49,
        "text": "private StageMapViewReader _reader"
      },
      {
        "line": 50,
        "text": "private IStageSelectionReceiverSlot _slot"
      },
      {
        "line": 51,
        "text": "private IInputLayerService _layers"
      },
      {
        "line": 52,
        "text": "private IStageViewBufferReadSink _requests"
      },
      {
        "line": 53,
        "text": "private IStageViewBufferWriteSink _requestWriter"
      },
      {
        "line": 54,
        "text": "private IStageMapInteractionFeedback _feedback"
      },
      {
        "line": 55,
        "text": "private StageMapSceneRoot _root"
      },
      {
        "line": 56,
        "text": "private StageMapInteraction _interaction"
      },
      {
        "line": 57,
        "text": "private bool _isShown"
      },
      {
        "line": 58,
        "text": "private StageMapEntryFlow _entryFlow"
      },
      {
        "line": 59,
        "text": "private Func<bool> _canSubmitEntry"
      },
      {
        "line": 60,
        "text": "private readonly RuntimeFaultBoundary _faults"
      }
    ],
    "methods": [
      {
        "line": 62,
        "text": "internal StageMapViewSystem(RuntimeFaultBoundary faults = null)"
      },
      {
        "line": 67,
        "text": "public void InitData(IDataBaseQueryContext data)"
      },
      {
        "line": 77,
        "text": "public void PostBindService(ISystemServiceStoreReader resolver)"
      },
      {
        "line": 97,
        "text": "public void Initialize()"
      },
      {
        "line": 107,
        "text": "public void Tick(float deltaTime)"
      },
      {
        "line": 123,
        "text": "public void Shutdown()"
      },
      {
        "line": 128,
        "text": "public void ExportServices(ISystemWriteSink sink)"
      },
      {
        "line": 137,
        "text": "internal void BindData(StageMapViewData mapData, IStageNodeVisualTableQuery visuals)"
      },
      {
        "line": 148,
        "text": "internal void BindServices(ILodingRegisterService loadingRegister, ICurrentProgressBufferReadSink current, IStageProgressBufferReadSink stages)"
      },
      {
        "line": 156,
        "text": "internal void BindInput(IStageSelectionReceiverSlot slot, IInputLayerService layers, IStageViewBufferReadSink requests, IStageViewBufferWriteSink requestWriter, IStageMapInteractionFeedback feedback)"
      },
      {
        "line": 171,
        "text": "internal void Attach(StageMapSceneRoot root, IStageNodePicker picker)"
      },
      {
        "line": 190,
        "text": "internal void Detach()"
      },
      {
        "line": 210,
        "text": "private void AttachFromScene()"
      }
    ]
  },
  "StageNodePicker": {
    "kind": "class",
    "inherits": "IStageNodePicker",
    "source": "C:/Users/User/Project_L/Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/ViewSystem/StageMap/StageNodePicker.cs",
    "fields": [
      {
        "line": 13,
        "text": "private readonly Camera _camera"
      },
      {
        "line": 14,
        "text": "private readonly StageMapViewDisplay _display"
      },
      {
        "line": 15,
        "text": "private readonly LayerMask _nodeMask"
      },
      {
        "line": 16,
        "text": "private readonly float _pickDistance"
      }
    ],
    "methods": [
      {
        "line": 18,
        "text": "internal StageNodePicker(Camera camera, StageMapViewDisplay display, LayerMask nodeMask, float pickDistance)"
      },
      {
        "line": 29,
        "text": "public bool TryPickNode(float screenX, float screenY, out int nodeId)"
      }
    ]
  },
  "StageNodeView": {
    "kind": "class",
    "inherits": "",
    "source": "C:/Users/User/Project_L/Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/ViewSystem/StageMap/StageNodeView.cs",
    "fields": [
      {
        "line": 13,
        "text": "private readonly Dictionary<int, Transform> _roots"
      },
      {
        "line": 14,
        "text": "private readonly Dictionary<Transform, int> _nodeIdsByRoot"
      }
    ],
    "methods": [
      {
        "line": 16,
        "text": "internal void Show(IReadOnlyList<StageProgressData> nodes, StageMapViewData mapData, Func<StageProgressData, GameObject> findVisualPrefab, Transform parent)"
      },
      {
        "line": 57,
        "text": "internal bool TryGetNode(int nodeId, out Transform nodeRoot)"
      },
      {
        "line": 69,
        "text": "internal bool TryGetNodeId(Transform hit, out int nodeId)"
      },
      {
        "line": 81,
        "text": "internal void Clear()"
      }
    ]
  },
  "IStageSelectionInputReceiver": {
    "kind": "interface",
    "inherits": "",
    "source": "C:/Users/User/Project_L/Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/ViewSystem/StageSelectionInput/IStageSelectionInputReceiver.cs",
    "fields": [],
    "methods": [
      {
        "line": 6,
        "text": "void PointerMoved(float screenX, float screenY)"
      },
      {
        "line": 7,
        "text": "void SelectPressed(float screenX, float screenY)"
      },
      {
        "line": 8,
        "text": "void SelectReleased(float screenX, float screenY)"
      },
      {
        "line": 11,
        "text": "void SelectCanceled()"
      },
      {
        "line": 13,
        "text": "void ConfirmRequested()"
      },
      {
        "line": 16,
        "text": "void CancelRequested()"
      }
    ]
  },
  "StageSelectionInputController": {
    "kind": "class",
    "inherits": "InputController",
    "source": "C:/Users/User/Project_L/Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/ViewSystem/StageSelectionInput/StageSelectionInputController.cs",
    "fields": [
      {
        "line": 10,
        "text": "private readonly IStageSelectionInputReceiver _receiver"
      },
      {
        "line": 11,
        "text": "private float _pointerX"
      },
      {
        "line": 12,
        "text": "private float _pointerY"
      },
      {
        "line": 13,
        "text": "private bool _isPressed"
      },
      {
        "line": 15,
        "text": "public override string ActionMap"
      }
    ],
    "methods": [
      {
        "line": 17,
        "text": "internal StageSelectionInputController(IStageSelectionInputReceiver receiver)"
      },
      {
        "line": 22,
        "text": "protected override void Configure(InputBindings input)"
      },
      {
        "line": 30,
        "text": "private void OnPointer(InputSnapshot snapshot)"
      },
      {
        "line": 41,
        "text": "private void OnSelect(InputSnapshot snapshot)"
      }
    ]
  },
  "StageSelectionInputLayers": {
    "kind": "class",
    "inherits": "",
    "source": "C:/Users/User/Project_L/Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/ViewSystem/StageSelectionInput/StageSelectionInputLayers.cs",
    "fields": [],
    "methods": []
  },
  "StageSelectionInputNames": {
    "kind": "class",
    "inherits": "",
    "source": "C:/Users/User/Project_L/Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/ViewSystem/StageSelectionInput/StageSelectionInputNames.cs",
    "fields": [
      {
        "line": 6,
        "text": "internal const string Map"
      },
      {
        "line": 7,
        "text": "internal const string CommonMap"
      },
      {
        "line": 8,
        "text": "internal const string PointerPosition"
      },
      {
        "line": 9,
        "text": "internal const string Select"
      },
      {
        "line": 10,
        "text": "internal const string Confirm"
      },
      {
        "line": 11,
        "text": "internal const string Cancel"
      }
    ],
    "methods": []
  },
  "IStageSelectionReceiverSlot": {
    "kind": "interface",
    "inherits": "IServiceModule",
    "source": "C:/Users/User/Project_L/Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/ViewSystem/StageSelectionInput/StageSelectionReceiverSlot.cs",
    "fields": [],
    "methods": [
      {
        "line": 13,
        "text": "void Bind(IStageSelectionInputReceiver receiver)"
      },
      {
        "line": 19,
        "text": "void Unbind(IStageSelectionInputReceiver receiver)"
      }
    ]
  },
  "StageSelectionReceiverSlot": {
    "kind": "class",
    "inherits": "IStageSelectionReceiverSlot, IStageSelectionInputReceiver, IExportSystemServices",
    "source": "C:/Users/User/Project_L/Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/ViewSystem/StageSelectionInput/StageSelectionReceiverSlot.cs",
    "fields": [
      {
        "line": 36,
        "text": "private IStageSelectionInputReceiver _receiver"
      },
      {
        "line": 37,
        "text": "private IStageSelectionInputReceiver _selectOwner"
      }
    ],
    "methods": [
      {
        "line": 39,
        "text": "public void Bind(IStageSelectionInputReceiver receiver)"
      },
      {
        "line": 49,
        "text": "public void Unbind(IStageSelectionInputReceiver receiver)"
      },
      {
        "line": 66,
        "text": "public void PointerMoved(float screenX, float screenY)"
      },
      {
        "line": 68,
        "text": "public void SelectPressed(float screenX, float screenY)"
      },
      {
        "line": 79,
        "text": "public void SelectReleased(float screenX, float screenY)"
      },
      {
        "line": 85,
        "text": "public void SelectCanceled()"
      },
      {
        "line": 91,
        "text": "public void ConfirmRequested()"
      },
      {
        "line": 93,
        "text": "public void CancelRequested()"
      },
      {
        "line": 95,
        "text": "public void ExportServices(ISystemWriteSink sink)"
      },
      {
        "line": 104,
        "text": "private IStageSelectionInputReceiver TakeSelectOwner()"
      }
    ]
  },
  "AreaOutlineData": {
    "kind": "struct",
    "inherits": "",
    "source": "C:/Users/User/Project_L/Assets/ProjectL/Script/Systems/RuntimeSystem/GlobalSystem/RenderSystem/View/AreaOutlineSystem/AreaOutlineData.cs",
    "fields": [
      {
        "line": 18,
        "text": "public string Name"
      },
      {
        "line": 19,
        "text": "public Vector2Int[] Cells"
      },
      {
        "line": 20,
        "text": "public Vector3 GridOrigin"
      },
      {
        "line": 21,
        "text": "public Vector2 CellSize"
      },
      {
        "line": 22,
        "text": "public float YOffset"
      },
      {
        "line": 23,
        "text": "public float Thickness"
      },
      {
        "line": 24,
        "text": "public Color Color"
      },
      {
        "line": 25,
        "text": "public bool IsDashed"
      },
      {
        "line": 27,
        "text": "public float DashCount"
      },
      {
        "line": 32,
        "text": "public float DashLength"
      },
      {
        "line": 33,
        "text": "public float DashFill"
      },
      {
        "line": 34,
        "text": "public AreaOutlineFlowDirection FlowDirection"
      },
      {
        "line": 35,
        "text": "public float FlowSpeed"
      }
    ],
    "methods": [
      {
        "line": 37,
        "text": "public AreaOutlineData( string name, Vector2Int[] cells, Vector3 gridOrigin, Vector2 cellSize, float yOffset, float thickness, Color color, bool isDashed = true, float dashCount = 4f, float dashFill = 0.55f, AreaOutlineFlowDirection flowDirection = AreaOutlineFlowDirection.Clockwise, float flowSpeed = 1f, float dashLength = 0f)"
      },
      {
        "line": 67,
        "text": "internal GridOutlineCreateData ToMeshData()"
      }
    ]
  },
  "IAreaOutlineHandlePoolService": {
    "kind": "interface",
    "inherits": "",
    "source": "C:/Users/User/Project_L/Assets/ProjectL/Script/Systems/RuntimeSystem/GlobalSystem/RenderSystem/View/AreaOutlineSystem/AreaOutlineHandle.cs",
    "fields": [],
    "methods": [
      {
        "line": 10,
        "text": "void Return(AreaOutlineHandle handle)"
      }
    ]
  },
  "AreaOutlineHandle": {
    "kind": "class",
    "inherits": "IAreaOutlineHandle",
    "source": "C:/Users/User/Project_L/Assets/ProjectL/Script/Systems/RuntimeSystem/GlobalSystem/RenderSystem/View/AreaOutlineSystem/AreaOutlineHandle.cs",
    "fields": [
      {
        "line": 15,
        "text": "private readonly IAreaOutlineHandlePoolService poolService"
      },
      {
        "line": 16,
        "text": "private readonly GameObject gameObject"
      },
      {
        "line": 17,
        "text": "private IMeshRenderHandle meshHandle"
      },
      {
        "line": 18,
        "text": "private IColorHandle colorHandle"
      },
      {
        "line": 20,
        "text": "internal MeshFilter MeshFilter"
      },
      {
        "line": 21,
        "text": "internal MeshRenderer MeshRenderer"
      },
      {
        "line": 22,
        "text": "internal bool IsValid"
      }
    ],
    "methods": [
      {
        "line": 24,
        "text": "internal AreaOutlineHandle( IAreaOutlineHandlePoolService poolService, GameObject gameObject, MeshFilter meshFilter, MeshRenderer meshRenderer)"
      },
      {
        "line": 36,
        "text": "internal void Validate()"
      },
      {
        "line": 38,
        "text": "internal void Bind(IMeshRenderHandle mesh, IColorHandle color)"
      },
      {
        "line": 48,
        "text": "public void Update(AreaOutlineData data)"
      },
      {
        "line": 64,
        "text": "public void Dispose()"
      },
      {
        "line": 70,
        "text": "internal void Reset()"
      },
      {
        "line": 81,
        "text": "internal void DestroyGameObject()"
      }
    ]
  },
  "AreaOutlineHandlePool": {
    "kind": "class",
    "inherits": "IDisposable",
    "source": "C:/Users/User/Project_L/Assets/ProjectL/Script/Systems/RuntimeSystem/GlobalSystem/RenderSystem/View/AreaOutlineSystem/AreaOutlineHandlePool.cs",
    "fields": [
      {
        "line": 17,
        "text": "private readonly CustomPool<AreaOutlineHandle> pool"
      },
      {
        "line": 18,
        "text": "private readonly HashSet<AreaOutlineHandle> handles"
      },
      {
        "line": 19,
        "text": "private readonly IAreaOutlineHandlePoolService poolService"
      },
      {
        "line": 20,
        "text": "private readonly Material material"
      },
      {
        "line": 21,
        "text": "private readonly Transform root"
      }
    ],
    "methods": [
      {
        "line": 23,
        "text": "public AreaOutlineHandlePool( IAreaOutlineHandlePoolService poolService, Material material, Transform root, int initialSize = 1)"
      },
      {
        "line": 39,
        "text": "public AreaOutlineHandle GetHandle()"
      },
      {
        "line": 40,
        "text": "public void ReturnHandle(AreaOutlineHandle handle)"
      },
      {
        "line": 42,
        "text": "public void Dispose()"
      },
      {
        "line": 49,
        "text": "private AreaOutlineHandle CreateHandle()"
      }
    ]
  },
  "IAreaOutlineHandle": {
    "kind": "interface",
    "inherits": "IDisposable",
    "source": "C:/Users/User/Project_L/Assets/ProjectL/Script/Systems/RuntimeSystem/GlobalSystem/RenderSystem/View/AreaOutlineSystem/AreaOutlineSystem.cs",
    "fields": [],
    "methods": [
      {
        "line": 16,
        "text": "void Update(AreaOutlineData data)"
      }
    ]
  },
  "IAreaOutlineSystemService": {
    "kind": "interface",
    "inherits": "IServiceModule",
    "source": "C:/Users/User/Project_L/Assets/ProjectL/Script/Systems/RuntimeSystem/GlobalSystem/RenderSystem/View/AreaOutlineSystem/AreaOutlineSystem.cs",
    "fields": [],
    "methods": [
      {
        "line": 21,
        "text": "IAreaOutlineHandle Create()"
      }
    ]
  },
  "AreaOutlineSystem": {
    "kind": "class",
    "inherits": "ISystemBindScene, ISystemShutdown, IExportSystemServices, ISystemInitData<IHasMaterialQuery>, ISystemInitialize, ISystemPostBindService, IAreaOutlineSystemService, IAreaOutlineHandlePoolService",
    "source": "C:/Users/User/Project_L/Assets/ProjectL/Script/Systems/RuntimeSystem/GlobalSystem/RenderSystem/View/AreaOutlineSystem/AreaOutlineSystem.cs",
    "fields": [
      {
        "line": 34,
        "text": "private readonly RenderRuntimeRoot runtimeRoot"
      },
      {
        "line": 35,
        "text": "private readonly HashSet<AreaOutlineHandle> activeHandles"
      },
      {
        "line": 36,
        "text": "private AreaOutlineHandlePool handlePool"
      },
      {
        "line": 37,
        "text": "private Transform areaOutlineRoot"
      },
      {
        "line": 38,
        "text": "private IHasMaterialQuery materialQuery"
      },
      {
        "line": 39,
        "text": "private IMeshSystemService meshSystem"
      },
      {
        "line": 40,
        "text": "private IColorSystemService colorSystem"
      }
    ],
    "methods": [
      {
        "line": 42,
        "text": "internal AreaOutlineSystem(RenderRuntimeRoot runtimeRoot)"
      },
      {
        "line": 47,
        "text": "public void BindScene()"
      },
      {
        "line": 52,
        "text": "public void InitData(IHasMaterialQuery data)"
      },
      {
        "line": 57,
        "text": "public void PostBindService(ISystemServiceStoreReader resolver)"
      },
      {
        "line": 64,
        "text": "public void ExportServices(ISystemWriteSink sink)"
      },
      {
        "line": 70,
        "text": "public void Initialize()"
      },
      {
        "line": 82,
        "text": "public IAreaOutlineHandle Create()"
      },
      {
        "line": 111,
        "text": "public void Shutdown()"
      }
    ]
  },
  "InGameViewSystemFactory": {
    "kind": "class",
    "inherits": "",
    "source": "C:/Users/User/Project_L/Assets/ProjectL/Script/Systems/Factory/Runtime/InGame/View/InGameViewSystemFactory.cs",
    "fields": [],
    "methods": [
      {
        "line": 23,
        "text": "public static IExportSystemServices Create( CombatExecutionMode executionMode = CombatExecutionMode.Live, RuntimeFaultBoundary faultBoundary = null)"
      }
    ]
  },
  "StageFieldViewData": {
    "kind": "class",
    "inherits": "IData",
    "source": "C:/Users/User/Project_L/Assets/ProjectL/Script/Systems/DataBaseSystem/Data/View/Table/StageField/Item/StageFieldViewDataSO.cs",
    "fields": [
      {
        "line": 17,
        "text": "private FieldDisplayMode fieldDisplayMode"
      },
      {
        "line": 20,
        "text": "private GameObject cellPrefab"
      },
      {
        "line": 22,
        "text": "private GameObject alternateCellPrefab"
      },
      {
        "line": 24,
        "text": "private Vector3 cellOffsetSize"
      },
      {
        "line": 26,
        "text": "private float cellPaddingX"
      },
      {
        "line": 28,
        "text": "private float cellPaddingY"
      },
      {
        "line": 32,
        "text": "private Vector2 outlineCellSpacing"
      },
      {
        "line": 34,
        "text": "private float gridLineWidth"
      },
      {
        "line": 36,
        "text": "private Material gridDecalMaterial"
      },
      {
        "line": 38,
        "text": "private float gridDecalDepth"
      },
      {
        "line": 40,
        "text": "private float outlineThickness"
      },
      {
        "line": 42,
        "text": "private float outlineYOffset"
      },
      {
        "line": 44,
        "text": "private float outlineDashLength"
      },
      {
        "line": 46,
        "text": "private float outlineDashFill"
      },
      {
        "line": 49,
        "text": "private LayerMask handUnitLayerMask"
      },
      {
        "line": 52,
        "text": "private Material ghostMaterial"
      },
      {
        "line": 53,
        "text": "private Color validGhostTint"
      },
      {
        "line": 54,
        "text": "private Color invalidGhostTint"
      },
      {
        "line": 55,
        "text": "private Vector3 ghostWorldOffset"
      },
      {
        "line": 56,
        "text": "private float ghostScale"
      },
      {
        "line": 57,
        "text": "private float fieldUnitScale"
      },
      {
        "line": 59,
        "text": "public FieldDisplayMode DisplayMode"
      },
      {
        "line": 61,
        "text": "public GameObject CellPrefab"
      },
      {
        "line": 63,
        "text": "public GameObject AlternateCellPrefab"
      },
      {
        "line": 64,
        "text": "public Vector3 CellOffsetSize"
      },
      {
        "line": 65,
        "text": "public float CellPaddingX"
      },
      {
        "line": 66,
        "text": "public float CellPaddingY"
      },
      {
        "line": 67,
        "text": "public Vector2 OutlineCellSpacing"
      },
      {
        "line": 68,
        "text": "public float GridLineWidth"
      },
      {
        "line": 70,
        "text": "public Material GridDecalMaterial"
      },
      {
        "line": 71,
        "text": "public float GridDecalDepth"
      },
      {
        "line": 72,
        "text": "public float OutlineThickness"
      },
      {
        "line": 73,
        "text": "public float OutlineYOffset"
      },
      {
        "line": 74,
        "text": "public float OutlineDashLength"
      },
      {
        "line": 75,
        "text": "public float OutlineDashFill"
      },
      {
        "line": 76,
        "text": "public LayerMask HandUnitLayerMask"
      },
      {
        "line": 77,
        "text": "public Material GhostMaterial"
      },
      {
        "line": 78,
        "text": "public Color ValidGhostTint"
      },
      {
        "line": 79,
        "text": "public Color InvalidGhostTint"
      },
      {
        "line": 80,
        "text": "public Vector3 GhostWorldOffset"
      },
      {
        "line": 81,
        "text": "public float GhostScale"
      },
      {
        "line": 82,
        "text": "public float FieldUnitScale"
      }
    ],
    "methods": [
      {
        "line": 84,
        "text": "internal void Validate()"
      },
      {
        "line": 103,
        "text": "private void ValidateCellPrefab()"
      },
      {
        "line": 118,
        "text": "private void ValidateGridOutline()"
      }
    ]
  },
  "StageFieldViewDataSO": {
    "kind": "class",
    "inherits": "DatabaseItem",
    "source": "C:/Users/User/Project_L/Assets/ProjectL/Script/Systems/DataBaseSystem/Data/View/Table/StageField/Item/StageFieldViewDataSO.cs",
    "fields": [
      {
        "line": 144,
        "text": "private StageFieldViewData stageFieldViewData"
      }
    ],
    "methods": [
      {
        "line": 146,
        "text": "public override IData GetData()"
      }
    ]
  },
  "ICombatViewBufferReadSink": {
    "kind": "interface",
    "inherits": "IServiceModule",
    "source": "C:/Users/User/Project_L/Assets/ProjectL/Script/Systems/RuntimeSystem/BufferSystems/CombatView/CombatViewBuffer.cs",
    "fields": [
      {
        "line": 18,
        "text": "IReadOnlyList<CombatViewCommand> Commands"
      },
      {
        "line": 23,
        "text": "bool HasPreview"
      },
      {
        "line": 26,
        "text": "CombatViewPreviewRequest Preview"
      },
      {
        "line": 29,
        "text": "int PresentedResultSequence"
      }
    ],
    "methods": [
      {
        "line": 21,
        "text": "bool TryGetNextCommand(int handledThrough, out CombatViewCommand command)"
      }
    ]
  },
  "ICombatViewBufferWriteSink": {
    "kind": "interface",
    "inherits": "IServiceModule",
    "source": "C:/Users/User/Project_L/Assets/ProjectL/Script/Systems/RuntimeSystem/BufferSystems/CombatView/CombatViewBuffer.cs",
    "fields": [],
    "methods": [
      {
        "line": 36,
        "text": "int Submit(CombatViewCommandData command)"
      },
      {
        "line": 39,
        "text": "int SetPreview(CombatViewPreviewData preview)"
      },
      {
        "line": 41,
        "text": "void ClearPreview()"
      },
      {
        "line": 44,
        "text": "void RemoveCommandsThrough(int handledNumber)"
      },
      {
        "line": 47,
        "text": "void AcknowledgePresented(int resultSequence)"
      },
      {
        "line": 50,
        "text": "void Clear()"
      }
    ]
  },
  "CombatViewBuffer": {
    "kind": "class",
    "inherits": "ICombatViewBufferReadSink, ICombatViewBufferWriteSink, IExportSystemServices",
    "source": "C:/Users/User/Project_L/Assets/ProjectL/Script/Systems/RuntimeSystem/BufferSystems/CombatView/CombatViewBuffer.cs",
    "fields": [
      {
        "line": 62,
        "text": "private readonly List<CombatViewCommand> _commands"
      },
      {
        "line": 63,
        "text": "private readonly ReadOnlyCollection<CombatViewCommand> _readOnlyCommands"
      },
      {
        "line": 64,
        "text": "private int _lastIssuedNumber"
      },
      {
        "line": 65,
        "text": "private CombatViewPreviewRequest _preview"
      },
      {
        "line": 66,
        "text": "private bool _hasPreview"
      },
      {
        "line": 67,
        "text": "private int _presentedResultSequence"
      },
      {
        "line": 69,
        "text": "public IReadOnlyList<CombatViewCommand> Commands"
      },
      {
        "line": 70,
        "text": "public bool HasPreview"
      },
      {
        "line": 71,
        "text": "public CombatViewPreviewRequest Preview"
      },
      {
        "line": 72,
        "text": "public int PresentedResultSequence"
      }
    ],
    "methods": [
      {
        "line": 74,
        "text": "public CombatViewBuffer()"
      },
      {
        "line": 79,
        "text": "public bool TryGetNextCommand(int handledThrough, out CombatViewCommand command)"
      },
      {
        "line": 95,
        "text": "public int Submit(CombatViewCommandData command)"
      },
      {
        "line": 105,
        "text": "public int SetPreview(CombatViewPreviewData preview)"
      },
      {
        "line": 114,
        "text": "public void ClearPreview()"
      },
      {
        "line": 120,
        "text": "public void RemoveCommandsThrough(int handledNumber)"
      },
      {
        "line": 132,
        "text": "public void AcknowledgePresented(int resultSequence)"
      },
      {
        "line": 141,
        "text": "public void Clear()"
      },
      {
        "line": 147,
        "text": "public void ExportServices(ISystemWriteSink sink)"
      },
      {
        "line": 153,
        "text": "private int PeekNextNumber()"
      }
    ]
  },
  "CombatViewCommandData": {
    "kind": "struct",
    "inherits": "",
    "source": "C:/Users/User/Project_L/Assets/ProjectL/Script/Systems/RuntimeSystem/BufferSystems/CombatView/CombatViewCommand.cs",
    "fields": [
      {
        "line": 15,
        "text": "public CombatViewCommandKind Kind"
      },
      {
        "line": 17,
        "text": "public EntityId EntityId"
      },
      {
        "line": 19,
        "text": "public CellCoord Cell"
      },
      {
        "line": 21,
        "text": "public Rotation4 Rotation"
      },
      {
        "line": 23,
        "text": "public AttackAim Aim"
      },
      {
        "line": 24,
        "text": "public MovementPath Path"
      },
      {
        "line": 25,
        "text": "public MovementPreviewContext MovementContext"
      }
    ],
    "methods": [
      {
        "line": 27,
        "text": "private CombatViewCommandData(CombatViewCommandKind kind, EntityId entityId, CellCoord cell, Rotation4 rotation, AttackAim aim, MovementPath path = null, MovementPreviewContext movementContext = null)"
      },
      {
        "line": 38,
        "text": "public static CombatViewCommandData PlaceMainCharacter(CellCoord cell, Rotation4 rotation)"
      },
      {
        "line": 45,
        "text": "public static CombatViewCommandData PlaceUnit(EntityId unit, CellCoord cell, Rotation4 rotation)"
      },
      {
        "line": 53,
        "text": "public static CombatViewCommandData ReturnUnit(EntityId unit)"
      },
      {
        "line": 60,
        "text": "public static CombatViewCommandData EnterCombat()"
      },
      {
        "line": 66,
        "text": "public static CombatViewCommandData Move(EntityId unit, MovementPath path, MovementPreviewContext context)"
      },
      {
        "line": 75,
        "text": "public static CombatViewCommandData ReturnToCheckpoint(EntityId unit, MovementPreviewContext context)"
      },
      {
        "line": 83,
        "text": "public static CombatViewCommandData Attack(EntityId attacker, AttackAim aim)"
      },
      {
        "line": 91,
        "text": "public static CombatViewCommandData EndUnitTurn()"
      },
      {
        "line": 97,
        "text": "public static CombatViewCommandData DrawCharacter()"
      }
    ]
  },
  "CombatViewCommand": {
    "kind": "struct",
    "inherits": "",
    "source": "C:/Users/User/Project_L/Assets/ProjectL/Script/Systems/RuntimeSystem/BufferSystems/CombatView/CombatViewCommand.cs",
    "fields": [
      {
        "line": 108,
        "text": "public int Number"
      },
      {
        "line": 109,
        "text": "public CombatViewCommandData Data"
      }
    ],
    "methods": [
      {
        "line": 111,
        "text": "internal CombatViewCommand(int number, CombatViewCommandData data)"
      }
    ]
  },
  "CombatViewInputGuard": {
    "kind": "class",
    "inherits": "",
    "source": "C:/Users/User/Project_L/Assets/ProjectL/Script/Systems/RuntimeSystem/BufferSystems/CombatView/CombatViewCommand.cs",
    "fields": [],
    "methods": [
      {
        "line": 126,
        "text": "internal static void RequireEntity(EntityId entityId, string paramName)"
      },
      {
        "line": 132,
        "text": "internal static void RequireRotation(Rotation4 rotation)"
      },
      {
        "line": 138,
        "text": "internal static void RequireAim(AttackAim aim)"
      }
    ]
  },
  "CombatViewPreviewData": {
    "kind": "struct",
    "inherits": "",
    "source": "C:/Users/User/Project_L/Assets/ProjectL/Script/Systems/RuntimeSystem/BufferSystems/CombatView/CombatViewPreview.cs",
    "fields": [
      {
        "line": 15,
        "text": "public CombatViewPreviewKind Kind"
      },
      {
        "line": 17,
        "text": "public EntityId EntityId"
      },
      {
        "line": 19,
        "text": "public CellCoord Cell"
      },
      {
        "line": 21,
        "text": "public Rotation4 Rotation"
      },
      {
        "line": 23,
        "text": "public AttackAim Aim"
      },
      {
        "line": 24,
        "text": "public MovementPath Path"
      }
    ],
    "methods": [
      {
        "line": 26,
        "text": "private CombatViewPreviewData(CombatViewPreviewKind kind, EntityId entityId, CellCoord cell, Rotation4 rotation, AttackAim aim, MovementPath path = null)"
      },
      {
        "line": 37,
        "text": "public static CombatViewPreviewData MainCharacterPlacement(CellCoord cell, Rotation4 rotation)"
      },
      {
        "line": 44,
        "text": "public static CombatViewPreviewData UnitPlacement(EntityId unit, CellCoord cell, Rotation4 rotation)"
      },
      {
        "line": 52,
        "text": "public static CombatViewPreviewData MoveRange(EntityId unit)"
      },
      {
        "line": 59,
        "text": "public static CombatViewPreviewData Move(EntityId unit, MovementPath path)"
      },
      {
        "line": 67,
        "text": "public static CombatViewPreviewData ReturnToCheckpoint(EntityId unit)"
      },
      {
        "line": 73,
        "text": "public static CombatViewPreviewData AttackRange(EntityId attacker)"
      },
      {
        "line": 80,
        "text": "public static CombatViewPreviewData Attack(EntityId attacker, AttackAim aim)"
      }
    ]
  },
  "CombatViewPreviewRequest": {
    "kind": "struct",
    "inherits": "",
    "source": "C:/Users/User/Project_L/Assets/ProjectL/Script/Systems/RuntimeSystem/BufferSystems/CombatView/CombatViewPreview.cs",
    "fields": [
      {
        "line": 93,
        "text": "public int Number"
      },
      {
        "line": 94,
        "text": "public CombatViewPreviewData Data"
      }
    ],
    "methods": [
      {
        "line": 96,
        "text": "internal CombatViewPreviewRequest(int number, CombatViewPreviewData data)"
      }
    ]
  },
  "IJobRegisterSystem": {
    "kind": "interface",
    "inherits": "IServiceModule",
    "source": "C:/Users/User/Project_L/Assets/ProjectL/Script/Systems/RuntimeSystem/GlobalSystem/JobSystem/JobRegister/JobRegisterSystem.cs",
    "fields": [],
    "methods": [
      {
        "line": 21,
        "text": "public void Register(JobRootType rootType, JobType jobType)"
      },
      {
        "line": 22,
        "text": "public void RegisterCombatEntry(CombatEntryRequest request)"
      }
    ]
  },
  "JobRegisterSystem": {
    "kind": "class",
    "inherits": "IJobRegisterSystem, ISystemPostBindService, ISystemInitialize, ISystemShutdown, IExportSystemServices",
    "source": "C:/Users/User/Project_L/Assets/ProjectL/Script/Systems/RuntimeSystem/GlobalSystem/JobSystem/JobRegister/JobRegisterSystem.cs",
    "fields": [
      {
        "line": 37,
        "text": "private readonly IJobRunner _runner"
      },
      {
        "line": 38,
        "text": "private readonly Dictionary<JobRootType, Func<IInGameSystemGroupService, JobType, List<IJobService>>> _plans"
      },
      {
        "line": 39,
        "text": "private IInGameSystemGroupService _inGameService"
      },
      {
        "line": 40,
        "text": "private IBufferQuery _buffers"
      }
    ],
    "methods": [
      {
        "line": 42,
        "text": "internal JobRegisterSystem(IJobRunner runner)"
      },
      {
        "line": 47,
        "text": "public void PostBindService(ISystemServiceStoreReader resolver)"
      },
      {
        "line": 56,
        "text": "public void Initialize()"
      },
      {
        "line": 64,
        "text": "public void Register(JobRootType rootType, JobType jobType)"
      },
      {
        "line": 77,
        "text": "public void RegisterCombatEntry(CombatEntryRequest request)"
      },
      {
        "line": 97,
        "text": "public void Shutdown()"
      },
      {
        "line": 104,
        "text": "public void ExportServices(ISystemWriteSink sink)"
      }
    ]
  },
  "IJobCompletion": {
    "kind": "interface",
    "inherits": "",
    "source": "C:/Users/User/Project_L/Assets/ProjectL/Script/Systems/RuntimeSystem/GlobalSystem/JobSystem/JobRunner/IJobCompletion.cs",
    "fields": [
      {
        "line": 6,
        "text": "public bool IsCompleted"
      }
    ],
    "methods": []
  },
  "IJobService": {
    "kind": "interface",
    "inherits": "IJobCompletion",
    "source": "C:/Users/User/Project_L/Assets/ProjectL/Script/Systems/RuntimeSystem/GlobalSystem/JobSystem/JobRunner/IJobService.cs",
    "fields": [
      {
        "line": 9,
        "text": "public int Priority"
      }
    ],
    "methods": [
      {
        "line": 10,
        "text": "public void Execute()"
      }
    ]
  },
  "IJobRunner": {
    "kind": "interface",
    "inherits": "IServiceModule",
    "source": "C:/Users/User/Project_L/Assets/ProjectL/Script/Systems/RuntimeSystem/GlobalSystem/JobSystem/JobRunner/JobRunnerSystem.cs",
    "fields": [
      {
        "line": 14,
        "text": "public bool IsIdle"
      }
    ],
    "methods": [
      {
        "line": 16,
        "text": "public void Register(IJobService job)"
      },
      {
        "line": 17,
        "text": "public void RegisterRange(IReadOnlyList<IJobService> jobs)"
      },
      {
        "line": 18,
        "text": "public void Clear()"
      }
    ]
  },
  "JobRunnerSystem": {
    "kind": "class",
    "inherits": "IJobRunner, ISystemTick, ISystemShutdown, IExportSystemServices",
    "source": "C:/Users/User/Project_L/Assets/ProjectL/Script/Systems/RuntimeSystem/GlobalSystem/JobSystem/JobRunner/JobRunnerSystem.cs",
    "fields": [
      {
        "line": 35,
        "text": "private readonly JobQueue<IJobService> _queue"
      },
      {
        "line": 39,
        "text": "private IJobService _currentJob"
      },
      {
        "line": 41,
        "text": "public bool IsIdle"
      }
    ],
    "methods": [
      {
        "line": 43,
        "text": "public void Register(IJobService job)"
      },
      {
        "line": 51,
        "text": "public void RegisterRange(IReadOnlyList<IJobService> jobs)"
      },
      {
        "line": 65,
        "text": "public void Clear()"
      },
      {
        "line": 71,
        "text": "public void Tick(float deltaTime)"
      },
      {
        "line": 91,
        "text": "public void Shutdown()"
      },
      {
        "line": 96,
        "text": "public void ExportServices(ISystemWriteSink sink)"
      }
    ]
  },
  "IJobSystemServiceContext": {
    "kind": "interface",
    "inherits": "IServiceModule",
    "source": "C:/Users/User/Project_L/Assets/ProjectL/Script/Systems/RuntimeSystem/GlobalSystem/JobSystem/JobSystemGroup.cs",
    "fields": [
      {
        "line": 13,
        "text": "public IJobRunner JobRunner"
      },
      {
        "line": 14,
        "text": "public IJobRegisterSystem JobRegister"
      }
    ],
    "methods": []
  },
  "JobSystemGroup": {
    "kind": "class",
    "inherits": "SystemGroup, IJobSystemServiceContext, ISystemPostBindService, ISystemInitialize, ISystemTick, ISystemShutdown",
    "source": "C:/Users/User/Project_L/Assets/ProjectL/Script/Systems/RuntimeSystem/GlobalSystem/JobSystem/JobSystemGroup.cs",
    "fields": [
      {
        "line": 25,
        "text": "public IJobRunner JobRunner"
      },
      {
        "line": 26,
        "text": "public IJobRegisterSystem JobRegister"
      }
    ],
    "methods": [
      {
        "line": 28,
        "text": "internal JobSystemGroup(ISystemReader systemReader) : base(systemReader)"
      },
      {
        "line": 34,
        "text": "public void PostBindService(ISystemServiceStoreReader resolver)"
      },
      {
        "line": 41,
        "text": "public void Initialize()"
      },
      {
        "line": 48,
        "text": "public void Tick(float deltaTime)"
      },
      {
        "line": 55,
        "text": "public void Shutdown()"
      },
      {
        "line": 62,
        "text": "public override void ExportServices(ISystemWriteSink sink)"
      }
    ]
  },
  "ArtifactEditRequest": {
    "kind": "class",
    "inherits": "",
    "source": "C:/Users/User/Project_L/Assets/ProjectL/Script/Systems/RuntimeSystem/LifeCycleSystem/LifeCycles/Artifact/ArtifactEditRequest.cs",
    "fields": [
      {
        "line": 40,
        "text": "public ArtifactEditKind Kind"
      },
      {
        "line": 42,
        "text": "public int ArtifactId"
      },
      {
        "line": 44,
        "text": "public int InstanceId"
      },
      {
        "line": 45,
        "text": "public CellCoord Anchor"
      },
      {
        "line": 46,
        "text": "public Rotation4 Rotation"
      },
      {
        "line": 48,
        "text": "public ArtifactRequestStatus Status"
      },
      {
        "line": 51,
        "text": "public ArtifactActionResult? Result"
      },
      {
        "line": 77,
        "text": "internal bool IsPending"
      }
    ],
    "methods": [
      {
        "line": 53,
        "text": "private ArtifactEditRequest(ArtifactEditKind kind, int artifactId, int instanceId, CellCoord anchor, Rotation4 rotation)"
      },
      {
        "line": 80,
        "text": "internal bool Cancel()"
      },
      {
        "line": 90,
        "text": "internal bool TryStart()"
      },
      {
        "line": 99,
        "text": "internal void Complete(ArtifactActionResult result)"
      },
      {
        "line": 108,
        "text": "internal void Fail()"
      }
    ]
  },
  "ArtifactPreviewRequest": {
    "kind": "class",
    "inherits": "",
    "source": "C:/Users/User/Project_L/Assets/ProjectL/Script/Systems/RuntimeSystem/LifeCycleSystem/LifeCycles/Artifact/ArtifactEditRequest.cs",
    "fields": [
      {
        "line": 120,
        "text": "public ArtifactPreviewKind Kind"
      },
      {
        "line": 121,
        "text": "public int ArtifactId"
      },
      {
        "line": 122,
        "text": "public int InstanceId"
      },
      {
        "line": 123,
        "text": "public CellCoord Anchor"
      },
      {
        "line": 124,
        "text": "public Rotation4 Rotation"
      },
      {
        "line": 126,
        "text": "public ArtifactRequestStatus Status"
      },
      {
        "line": 129,
        "text": "public ArtifactPlacementPreview Result"
      }
    ],
    "methods": [
      {
        "line": 131,
        "text": "private ArtifactPreviewRequest(ArtifactPreviewKind kind, int artifactId, int instanceId, CellCoord anchor, Rotation4 rotation)"
      },
      {
        "line": 152,
        "text": "internal bool Cancel()"
      },
      {
        "line": 161,
        "text": "internal bool TryStart()"
      },
      {
        "line": 170,
        "text": "internal void Complete(ArtifactPlacementPreview result)"
      },
      {
        "line": 179,
        "text": "internal void Fail()"
      }
    ]
  },
  "ArtifactEditScheduler": {
    "kind": "class",
    "inherits": "",
    "source": "C:/Users/User/Project_L/Assets/ProjectL/Script/Systems/RuntimeSystem/LifeCycleSystem/LifeCycles/Artifact/ArtifactEditScheduler.cs",
    "fields": [
      {
        "line": 21,
        "text": "private readonly IJobRunner _runner"
      },
      {
        "line": 22,
        "text": "private readonly IArtifactEditingService _artifacts"
      },
      {
        "line": 23,
        "text": "private readonly IArtifactBufferReadSink _board"
      },
      {
        "line": 24,
        "text": "private readonly IArtifactEditResultReadSink _results"
      },
      {
        "line": 25,
        "text": "private readonly IArtifactPreviewReadSink _previews"
      },
      {
        "line": 26,
        "text": "private readonly RuntimeFaultBoundary _faults"
      },
      {
        "line": 27,
        "text": "private readonly ArtifactPreviewSlot _previewSlot"
      },
      {
        "line": 42,
        "text": "internal ArtifactEditRequest CurrentEdit"
      },
      {
        "line": 45,
        "text": "internal ArtifactPreviewRequest LatestPreview"
      },
      {
        "line": 48,
        "text": "internal bool IsEditPending"
      },
      {
        "line": 51,
        "text": "internal bool CanAcceptRequests"
      }
    ],
    "methods": [
      {
        "line": 29,
        "text": "internal ArtifactEditScheduler(IJobRunner runner, IArtifactEditingService artifacts, IArtifactBufferReadSink board, IArtifactEditResultReadSink results, IArtifactPreviewReadSink previews, RuntimeFaultBoundary faults)"
      },
      {
        "line": 54,
        "text": "internal bool TryRequestEdit(ArtifactEditRequest request)"
      },
      {
        "line": 72,
        "text": "internal bool TryRequestPreview(ArtifactPreviewRequest request)"
      },
      {
        "line": 85,
        "text": "internal void InvalidatePreview()"
      },
      {
        "line": 93,
        "text": "internal void CancelPending()"
      }
    ]
  },
  "ArtifactLifeCycle": {
    "kind": "class",
    "inherits": "LifeCycleBase",
    "source": "C:/Users/User/Project_L/Assets/ProjectL/Script/Systems/RuntimeSystem/LifeCycleSystem/LifeCycles/Artifact/ArtifactLifeCycle.cs",
    "fields": [
      {
        "line": 8,
        "text": "public override LifeCycleKey Key"
      },
      {
        "line": 10,
        "text": "protected override int SceneIndex"
      }
    ],
    "methods": [
      {
        "line": 12,
        "text": "protected override void OnEnter()"
      },
      {
        "line": 17,
        "text": "protected override void OnExit()"
      }
    ]
  },
  "ArtifactPreviewSlot": {
    "kind": "class",
    "inherits": "",
    "source": "C:/Users/User/Project_L/Assets/ProjectL/Script/Systems/RuntimeSystem/LifeCycleSystem/LifeCycles/Artifact/ArtifactPreviewSlot.cs",
    "fields": [
      {
        "line": 11,
        "text": "private ArtifactPreviewRequest _pending"
      },
      {
        "line": 12,
        "text": "private bool _isJobQueued"
      },
      {
        "line": 15,
        "text": "internal ArtifactPreviewRequest Latest"
      }
    ],
    "methods": [
      {
        "line": 18,
        "text": "internal bool Offer(ArtifactPreviewRequest request)"
      },
      {
        "line": 30,
        "text": "internal bool TryTakePending(out ArtifactPreviewRequest request)"
      },
      {
        "line": 38,
        "text": "internal void Publish(ArtifactPreviewRequest request)"
      },
      {
        "line": 44,
        "text": "internal void Invalidate()"
      }
    ]
  },
  "ArtifactBoardInitializeJob": {
    "kind": "struct",
    "inherits": "IJobService",
    "source": "C:/Users/User/Project_L/Assets/ProjectL/Script/Systems/RuntimeSystem/LifeCycleSystem/LifeCycles/Artifact/Jobs/ArtifactBoardInitializeJob.cs",
    "fields": [
      {
        "line": 17,
        "text": "private readonly IArtifactEditingService _artifacts"
      },
      {
        "line": 18,
        "text": "private readonly IArtifactBufferReadSink _board"
      },
      {
        "line": 19,
        "text": "private readonly int _width"
      },
      {
        "line": 20,
        "text": "private readonly int _height"
      },
      {
        "line": 22,
        "text": "public int Priority"
      },
      {
        "line": 23,
        "text": "public bool IsCompleted"
      }
    ],
    "methods": [
      {
        "line": 25,
        "text": "internal ArtifactBoardInitializeJob(IArtifactEditingService artifacts, IArtifactBufferReadSink board, int width, int height, int priority)"
      },
      {
        "line": 38,
        "text": "public void Execute()"
      }
    ]
  },
  "ArtifactEditJob": {
    "kind": "struct",
    "inherits": "IJobService",
    "source": "C:/Users/User/Project_L/Assets/ProjectL/Script/Systems/RuntimeSystem/LifeCycleSystem/LifeCycles/Artifact/Jobs/ArtifactEditJob.cs",
    "fields": [
      {
        "line": 15,
        "text": "private readonly ArtifactEditRequest _request"
      },
      {
        "line": 16,
        "text": "private readonly IArtifactEditingService _artifacts"
      },
      {
        "line": 17,
        "text": "private readonly IArtifactEditResultReadSink _results"
      },
      {
        "line": 19,
        "text": "public int Priority"
      },
      {
        "line": 20,
        "text": "public bool IsCompleted"
      }
    ],
    "methods": [
      {
        "line": 22,
        "text": "internal ArtifactEditJob(ArtifactEditRequest request, IArtifactEditingService artifacts, IArtifactEditResultReadSink results, int priority)"
      },
      {
        "line": 32,
        "text": "public void Execute()"
      }
    ]
  },
  "ArtifactJob": {
    "kind": "struct",
    "inherits": "IJobService",
    "source": "C:/Users/User/Project_L/Assets/ProjectL/Script/Systems/RuntimeSystem/LifeCycleSystem/LifeCycles/Artifact/Jobs/ArtifactJob.cs",
    "fields": [
      {
        "line": 10,
        "text": "private readonly IInGameSystemGroupService _service"
      },
      {
        "line": 12,
        "text": "public int Priority"
      },
      {
        "line": 13,
        "text": "public bool IsCompleted"
      }
    ],
    "methods": [
      {
        "line": 15,
        "text": "internal ArtifactJob(IInGameSystemGroupService service, int priority)"
      },
      {
        "line": 22,
        "text": "public void Execute()"
      }
    ]
  },
  "ArtifactPreviewJob": {
    "kind": "struct",
    "inherits": "IJobService",
    "source": "C:/Users/User/Project_L/Assets/ProjectL/Script/Systems/RuntimeSystem/LifeCycleSystem/LifeCycles/Artifact/Jobs/ArtifactPreviewJob.cs",
    "fields": [
      {
        "line": 15,
        "text": "private readonly ArtifactPreviewSlot _slot"
      },
      {
        "line": 16,
        "text": "private readonly IArtifactEditingService _artifacts"
      },
      {
        "line": 17,
        "text": "private readonly IArtifactPreviewReadSink _previews"
      },
      {
        "line": 18,
        "text": "private readonly IArtifactBufferReadSink _board"
      },
      {
        "line": 20,
        "text": "public int Priority"
      },
      {
        "line": 21,
        "text": "public bool IsCompleted"
      }
    ],
    "methods": [
      {
        "line": 23,
        "text": "internal ArtifactPreviewJob(ArtifactPreviewSlot slot, IArtifactEditingService artifacts, IArtifactPreviewReadSink previews, IArtifactBufferReadSink board, int priority)"
      },
      {
        "line": 34,
        "text": "public void Execute()"
      },
      {
        "line": 80,
        "text": "private bool IsPlaced(int instanceId)"
      }
    ]
  },
  "ArtifactPlan": {
    "kind": "class",
    "inherits": "",
    "source": "C:/Users/User/Project_L/Assets/ProjectL/Script/Systems/RuntimeSystem/LifeCycleSystem/LifeCycles/Artifact/Plans/ArtifactPlan.cs",
    "fields": [
      {
        "line": 20,
        "text": "internal const int BoardInitializationPriority"
      },
      {
        "line": 31,
        "text": "internal const int RequestPriority"
      }
    ],
    "methods": [
      {
        "line": 23,
        "text": "internal static IJobService CreateBoardInitialization(IArtifactEditingService artifacts, IArtifactBufferReadSink board, int initialWidth, int initialHeight)"
      },
      {
        "line": 34,
        "text": "internal static IJobService CreateEditJob(ArtifactEditRequest request, IArtifactEditingService artifacts, IArtifactEditResultReadSink results)"
      },
      {
        "line": 41,
        "text": "internal static IJobService CreatePreviewJob(ArtifactPreviewSlot slot, IArtifactEditingService artifacts, IArtifactPreviewReadSink previews, IArtifactBufferReadSink board)"
      },
      {
        "line": 47,
        "text": "internal static List<IJobService> Register(IInGameSystemGroupService service, JobType jobType)"
      }
    ]
  },
  "ICombatEntryRead": {
    "kind": "interface",
    "inherits": "",
    "source": "C:/Users/User/Project_L/Assets/ProjectL/Script/Systems/RuntimeSystem/LifeCycleSystem/LifeCycles/Combat/CombatEntryRequest.cs",
    "fields": [
      {
        "line": 11,
        "text": "public bool IsActive"
      }
    ],
    "methods": []
  },
  "ICombatEntryService": {
    "kind": "interface",
    "inherits": "IServiceModule, ICombatEntryRead",
    "source": "C:/Users/User/Project_L/Assets/ProjectL/Script/Systems/RuntimeSystem/LifeCycleSystem/LifeCycles/Combat/CombatEntryRequest.cs",
    "fields": [],
    "methods": [
      {
        "line": 17,
        "text": "public CombatEntryRequest RequestEntry(int chapterKey, StageNodeId nodeId)"
      },
      {
        "line": 18,
        "text": "public bool CancelEntry(CombatEntryRequest request)"
      }
    ]
  },
  "CombatEntryRequest": {
    "kind": "class",
    "inherits": "",
    "source": "C:/Users/User/Project_L/Assets/ProjectL/Script/Systems/RuntimeSystem/LifeCycleSystem/LifeCycles/Combat/CombatEntryRequest.cs",
    "fields": [
      {
        "line": 41,
        "text": "public int ChapterKey"
      },
      {
        "line": 42,
        "text": "public StageNodeId NodeId"
      },
      {
        "line": 43,
        "text": "public CombatEntryStatus Status"
      },
      {
        "line": 44,
        "text": "public ChapterProgressionResult Result"
      },
      {
        "line": 45,
        "text": "public bool IsJobCompleted"
      }
    ],
    "methods": [
      {
        "line": 47,
        "text": "internal CombatEntryRequest(int chapterKey, StageNodeId nodeId)"
      },
      {
        "line": 57,
        "text": "internal void MarkQueued()"
      },
      {
        "line": 63,
        "text": "internal bool TryCancel()"
      },
      {
        "line": 71,
        "text": "internal bool TryStart()"
      },
      {
        "line": 80,
        "text": "internal void PublishResult(ChapterProgressionResult result)"
      },
      {
        "line": 90,
        "text": "internal void CompleteJob()"
      },
      {
        "line": 97,
        "text": "internal void BeginActivation()"
      },
      {
        "line": 105,
        "text": "internal void Activate()"
      },
      {
        "line": 114,
        "text": "private void Require(CombatEntryStatus expected)"
      }
    ]
  },
  "ICombatAbandonRequest": {
    "kind": "interface",
    "inherits": "",
    "source": "C:/Users/User/Project_L/Assets/ProjectL/Script/Systems/RuntimeSystem/LifeCycleSystem/LifeCycles/Combat/CombatExitSequence.cs",
    "fields": [
      {
        "line": 24,
        "text": "public CombatExitStatus ExitStatus"
      }
    ],
    "methods": [
      {
        "line": 30,
        "text": "public bool RequestAbandon()"
      }
    ]
  },
  "ICombatExitService": {
    "kind": "interface",
    "inherits": "IServiceModule, ICombatAbandonRequest",
    "source": "C:/Users/User/Project_L/Assets/ProjectL/Script/Systems/RuntimeSystem/LifeCycleSystem/LifeCycles/Combat/CombatExitSequence.cs",
    "fields": [],
    "methods": [
      {
        "line": 36,
        "text": "public bool RequestNormalExit(LifeCycleKey destination)"
      }
    ]
  },
  "ICombatExitPresentation": {
    "kind": "interface",
    "inherits": "",
    "source": "C:/Users/User/Project_L/Assets/ProjectL/Script/Systems/RuntimeSystem/LifeCycleSystem/LifeCycles/Combat/CombatExitSequence.cs",
    "fields": [
      {
        "line": 42,
        "text": "public bool IsAttached"
      },
      {
        "line": 43,
        "text": "public bool HasPendingPresentation"
      }
    ],
    "methods": [
      {
        "line": 44,
        "text": "public void SuspendSubmissions()"
      },
      {
        "line": 45,
        "text": "public void PresentCompletion(BattleCompletionChanges completion, Action completed)"
      }
    ]
  },
  "CombatExitSequence": {
    "kind": "class",
    "inherits": "",
    "source": "C:/Users/User/Project_L/Assets/ProjectL/Script/Systems/RuntimeSystem/LifeCycleSystem/LifeCycles/Combat/CombatExitSequence.cs",
    "fields": [
      {
        "line": 55,
        "text": "private readonly ICombatStateBufferReadSink _state"
      },
      {
        "line": 56,
        "text": "private readonly ICombatResultReadSink _results"
      },
      {
        "line": 57,
        "text": "private readonly ICharacterBufferReadSink _characters"
      },
      {
        "line": 58,
        "text": "private readonly ICombatViewBufferReadSink _input"
      },
      {
        "line": 59,
        "text": "private readonly IJobRunner _runner"
      },
      {
        "line": 60,
        "text": "private readonly ICombatProcessingService _core"
      },
      {
        "line": 61,
        "text": "private readonly IChapterProgressionCoordinator _progression"
      },
      {
        "line": 62,
        "text": "private readonly RuntimeFaultBoundary _faults"
      },
      {
        "line": 63,
        "text": "private readonly Func<CombatEntryRequest> _entry"
      },
      {
        "line": 64,
        "text": "private readonly Func<ICombatExitPresentation> _getView"
      },
      {
        "line": 65,
        "text": "private readonly Action _stopFlow"
      },
      {
        "line": 66,
        "text": "private readonly Action _registerNewRun"
      },
      {
        "line": 67,
        "text": "private readonly Action<LifeCycleKey> _change"
      },
      {
        "line": 68,
        "text": "private ICombatExitPresentation _view"
      },
      {
        "line": 69,
        "text": "private BattleCompletionChanges _completion"
      },
      {
        "line": 70,
        "text": "private CombatResult _finalResult"
      },
      {
        "line": 71,
        "text": "private int _finalSequence"
      },
      {
        "line": 72,
        "text": "private LifeCycleKey _destination"
      },
      {
        "line": 73,
        "text": "private long _battleInstance"
      },
      {
        "line": 74,
        "text": "private int _generation"
      },
      {
        "line": 75,
        "text": "private bool _abandon"
      },
      {
        "line": 76,
        "text": "private bool _confirmed"
      },
      {
        "line": 77,
        "text": "private bool _authorizingTransition"
      },
      {
        "line": 79,
        "text": "internal CombatExitStatus Status"
      }
    ],
    "methods": [
      {
        "line": 81,
        "text": "internal CombatExitSequence(ICombatStateBufferReadSink state, ICombatResultReadSink results, ICharacterBufferReadSink characters, ICombatViewBufferReadSink input, IJobRunner runner, ICombatProcessingService core, IChapterProgressionCoordinator progression, RuntimeFaultBoundary faults, Func<CombatEntryRequest> entry, Func<ICombatExitPresentation> getView, Action stopFlow, Action registerNewRun, Action<LifeCycleKey> change)"
      },
      {
        "line": 102,
        "text": "internal bool Request(LifeCycleKey destination)"
      },
      {
        "line": 121,
        "text": "internal bool RequestAbandon(LifeCycleKey destination)"
      },
      {
        "line": 138,
        "text": "private bool Begin(ICombatExitPresentation view, BattleCompletionChanges completion, LifeCycleKey destination, bool abandon)"
      },
      {
        "line": 160,
        "text": "internal void Tick()"
      },
      {
        "line": 232,
        "text": "private void ConfirmFinalResult()"
      },
      {
        "line": 245,
        "text": "private void Abandon()"
      },
      {
        "line": 284,
        "text": "private bool EndsRun()"
      },
      {
        "line": 300,
        "text": "private static void ValidateDestination(LifeCycleKey destination)"
      },
      {
        "line": 306,
        "text": "private static void ValidateTarget(BattleCompletionChanges completion, CombatEntryRequest entry)"
      },
      {
        "line": 314,
        "text": "private void Transition()"
      },
      {
        "line": 322,
        "text": "internal void ValidateTransition(LifeCycleKey? destination)"
      },
      {
        "line": 328,
        "text": "internal void Reset()"
      },
      {
        "line": 341,
        "text": "private void Fail(Exception error)"
      }
    ]
  },
  "CombatFlow": {
    "kind": "class",
    "inherits": "",
    "source": "C:/Users/User/Project_L/Assets/ProjectL/Script/Systems/RuntimeSystem/LifeCycleSystem/LifeCycles/Combat/CombatFlow.cs",
    "fields": [
      {
        "line": 34,
        "text": "private readonly ICombatStateBufferReadSink _state"
      },
      {
        "line": 35,
        "text": "private readonly ICombatResultReadSink _results"
      },
      {
        "line": 36,
        "text": "private readonly ICombatViewBufferReadSink _input"
      },
      {
        "line": 37,
        "text": "private readonly IJobRunner _runner"
      },
      {
        "line": 38,
        "text": "private readonly IJobRegisterSystem _register"
      },
      {
        "line": 39,
        "text": "private readonly CombatExecutionMode _executionMode"
      },
      {
        "line": 40,
        "text": "private readonly RuntimeFaultBoundary _faults"
      },
      {
        "line": 42,
        "text": "private PendingCheck _pending"
      },
      {
        "line": 45,
        "text": "private int _expectedNumber"
      },
      {
        "line": 46,
        "text": "private bool _isStopped"
      },
      {
        "line": 75,
        "text": "internal bool IsStopped"
      },
      {
        "line": 76,
        "text": "internal bool HasPendingCheck"
      }
    ],
    "methods": [
      {
        "line": 48,
        "text": "internal CombatFlow( ICombatStateBufferReadSink state, ICombatResultReadSink results, ICombatViewBufferReadSink input, IJobRunner runner, IJobRegisterSystem register, CombatExecutionMode executionMode, RuntimeFaultBoundary faultBoundary)"
      },
      {
        "line": 79,
        "text": "internal void ActivatePreparedBattle(CombatEntryRequest entry)"
      },
      {
        "line": 91,
        "text": "internal void Reset()"
      },
      {
        "line": 101,
        "text": "internal void Stop()"
      },
      {
        "line": 113,
        "text": "internal void BeginPreparation()"
      },
      {
        "line": 123,
        "text": "internal void ValidateEntryRegistration()"
      },
      {
        "line": 138,
        "text": "internal void Tick()"
      },
      {
        "line": 191,
        "text": "private bool IsPresentationDone()"
      },
      {
        "line": 196,
        "text": "private void RegisterAndExpect(JobType jobType, PendingCheck check, int expectedNumber)"
      },
      {
        "line": 208,
        "text": "private bool TryConfirmPending()"
      },
      {
        "line": 239,
        "text": "private string FindAdvanceFailure()"
      },
      {
        "line": 260,
        "text": "private bool IsPreviewSettled()"
      },
      {
        "line": 269,
        "text": "private void Fail(string reason)"
      },
      {
        "line": 276,
        "text": "private void ClearPending()"
      }
    ]
  },
  "CombatLifeCycle": {
    "kind": "class",
    "inherits": "LifeCycleBase, ICombatEntryService, ICombatExitService, ISystemTick, ILifeCycleTransitionGuard",
    "source": "C:/Users/User/Project_L/Assets/ProjectL/Script/Systems/RuntimeSystem/LifeCycleSystem/LifeCycles/Combat/CombatLifeCycle.cs",
    "fields": [
      {
        "line": 30,
        "text": "private ICombatViewBufferWriteSink _combatViewWrite"
      },
      {
        "line": 31,
        "text": "private ICombatStateBufferReadSink _combatStateRead"
      },
      {
        "line": 32,
        "text": "private CombatFlow _flow"
      },
      {
        "line": 33,
        "text": "private ICurrentProgressBufferReadSink _current"
      },
      {
        "line": 34,
        "text": "private IJobRunner _runner"
      },
      {
        "line": 35,
        "text": "private IJobRegisterSystem _register"
      },
      {
        "line": 36,
        "text": "private ILifeCycleManager _manager"
      },
      {
        "line": 37,
        "text": "private ILodingSystemService _loading"
      },
      {
        "line": 38,
        "text": "private CombatEntryRequest _entry"
      },
      {
        "line": 39,
        "text": "private CombatExitSequence _exit"
      },
      {
        "line": 40,
        "text": "private bool _ready"
      },
      {
        "line": 42,
        "text": "public bool IsActive"
      },
      {
        "line": 54,
        "text": "internal CombatExecutionMode ExecutionMode"
      },
      {
        "line": 57,
        "text": "internal RuntimeFaultBoundary FaultBoundary"
      },
      {
        "line": 59,
        "text": "public override LifeCycleKey Key"
      },
      {
        "line": 61,
        "text": "protected override int SceneIndex"
      },
      {
        "line": 100,
        "text": "public CombatExitStatus ExitStatus"
      },
      {
        "line": 112,
        "text": "private static LifeCycleKey RunEndDestination"
      }
    ],
    "methods": [
      {
        "line": 44,
        "text": "internal CombatLifeCycle(CombatExecutionMode executionMode, RuntimeFaultBoundary faultBoundary)"
      },
      {
        "line": 63,
        "text": "protected override void OnBindServices(ISystemServiceStoreReader resolver)"
      },
      {
        "line": 94,
        "text": "protected override void ExportLifeCycleServices(ISystemWriteSink sink)"
      },
      {
        "line": 121,
        "text": "public CombatEntryRequest RequestEntry(int chapterKey, StageNodeId nodeId)"
      },
      {
        "line": 156,
        "text": "public bool CancelEntry(CombatEntryRequest request)"
      },
      {
        "line": 193,
        "text": "protected override void OnEnter()"
      },
      {
        "line": 214,
        "text": "protected override void OnTick(float deltaTime)"
      },
      {
        "line": 225,
        "text": "private void RequestStageReturnAfterCompletion()"
      },
      {
        "line": 238,
        "text": "protected override void OnExit()"
      },
      {
        "line": 248,
        "text": "protected override void OnShutdown()"
      }
    ]
  },
  "CombatAdvanceJob": {
    "kind": "struct",
    "inherits": "IJobService",
    "source": "C:/Users/User/Project_L/Assets/ProjectL/Script/Systems/RuntimeSystem/LifeCycleSystem/LifeCycles/Combat/Jobs/CombatAdvanceJob.cs",
    "fields": [
      {
        "line": 13,
        "text": "private readonly IInGameSystemGroupService _service"
      },
      {
        "line": 15,
        "text": "public int Priority"
      },
      {
        "line": 16,
        "text": "public bool IsCompleted"
      }
    ],
    "methods": [
      {
        "line": 18,
        "text": "internal CombatAdvanceJob(IInGameSystemGroupService service, int priority)"
      },
      {
        "line": 25,
        "text": "public void Execute()"
      }
    ]
  },
  "CombatCommandJob": {
    "kind": "struct",
    "inherits": "IJobService",
    "source": "C:/Users/User/Project_L/Assets/ProjectL/Script/Systems/RuntimeSystem/LifeCycleSystem/LifeCycles/Combat/Jobs/CombatCommandJob.cs",
    "fields": [
      {
        "line": 13,
        "text": "private readonly IInGameSystemGroupService _service"
      },
      {
        "line": 15,
        "text": "public int Priority"
      },
      {
        "line": 16,
        "text": "public bool IsCompleted"
      }
    ],
    "methods": [
      {
        "line": 18,
        "text": "internal CombatCommandJob(IInGameSystemGroupService service, int priority)"
      },
      {
        "line": 25,
        "text": "public void Execute()"
      }
    ]
  },
  "CombatPrepareJob": {
    "kind": "struct",
    "inherits": "IJobService",
    "source": "C:/Users/User/Project_L/Assets/ProjectL/Script/Systems/RuntimeSystem/LifeCycleSystem/LifeCycles/Combat/Jobs/CombatPrepareJob.cs",
    "fields": [
      {
        "line": 13,
        "text": "private readonly IInGameSystemGroupService _service"
      },
      {
        "line": 14,
        "text": "private readonly CombatEntryRequest _entry"
      },
      {
        "line": 16,
        "text": "public int Priority"
      },
      {
        "line": 17,
        "text": "public bool IsCompleted"
      }
    ],
    "methods": [
      {
        "line": 19,
        "text": "internal CombatPrepareJob(IInGameSystemGroupService service, int priority)"
      },
      {
        "line": 28,
        "text": "internal CombatPrepareJob(IInGameSystemGroupService service, int priority, CombatEntryRequest entry)"
      },
      {
        "line": 34,
        "text": "public void Execute()"
      }
    ]
  },
  "CombatPreviewJob": {
    "kind": "struct",
    "inherits": "IJobService",
    "source": "C:/Users/User/Project_L/Assets/ProjectL/Script/Systems/RuntimeSystem/LifeCycleSystem/LifeCycles/Combat/Jobs/CombatPreviewJob.cs",
    "fields": [
      {
        "line": 13,
        "text": "private readonly IInGameSystemGroupService _service"
      },
      {
        "line": 15,
        "text": "public int Priority"
      },
      {
        "line": 16,
        "text": "public bool IsCompleted"
      }
    ],
    "methods": [
      {
        "line": 18,
        "text": "internal CombatPreviewJob(IInGameSystemGroupService service, int priority)"
      },
      {
        "line": 25,
        "text": "public void Execute()"
      }
    ]
  },
  "CombatPlan": {
    "kind": "class",
    "inherits": "",
    "source": "C:/Users/User/Project_L/Assets/ProjectL/Script/Systems/RuntimeSystem/LifeCycleSystem/LifeCycles/Combat/Plans/CombatPlan.cs",
    "fields": [
      {
        "line": 19,
        "text": "internal const int NewRunLoadoutKey"
      }
    ],
    "methods": [
      {
        "line": 22,
        "text": "internal static List<IJobService> RegisterEntry(IInGameSystemGroupService service, CombatEntryRequest request)"
      },
      {
        "line": 27,
        "text": "internal static List<IJobService> Register(IInGameSystemGroupService service, JobType jobType)"
      }
    ]
  },
  "LobbyJob": {
    "kind": "struct",
    "inherits": "IJobService",
    "source": "C:/Users/User/Project_L/Assets/ProjectL/Script/Systems/RuntimeSystem/LifeCycleSystem/LifeCycles/Lobby/Jobs/LobbyJob.cs",
    "fields": [
      {
        "line": 10,
        "text": "private readonly IInGameSystemGroupService _service"
      },
      {
        "line": 12,
        "text": "public int Priority"
      },
      {
        "line": 13,
        "text": "public bool IsCompleted"
      }
    ],
    "methods": [
      {
        "line": 15,
        "text": "internal LobbyJob(IInGameSystemGroupService service, int priority)"
      },
      {
        "line": 22,
        "text": "public void Execute()"
      }
    ]
  },
  "LobbyLifeCycle": {
    "kind": "class",
    "inherits": "LifeCycleBase",
    "source": "C:/Users/User/Project_L/Assets/ProjectL/Script/Systems/RuntimeSystem/LifeCycleSystem/LifeCycles/Lobby/LobbyLifeCycle.cs",
    "fields": [
      {
        "line": 8,
        "text": "public override LifeCycleKey Key"
      },
      {
        "line": 10,
        "text": "protected override int SceneIndex"
      }
    ],
    "methods": [
      {
        "line": 12,
        "text": "protected override void OnEnter()"
      },
      {
        "line": 17,
        "text": "protected override void OnExit()"
      }
    ]
  },
  "LobbyPlan": {
    "kind": "class",
    "inherits": "",
    "source": "C:/Users/User/Project_L/Assets/ProjectL/Script/Systems/RuntimeSystem/LifeCycleSystem/LifeCycles/Lobby/Plans/LobbyPlan.cs",
    "fields": [],
    "methods": [
      {
        "line": 13,
        "text": "internal static List<IJobService> Register(IInGameSystemGroupService service, JobType jobType)"
      }
    ]
  },
  "StageJob": {
    "kind": "struct",
    "inherits": "IJobService",
    "source": "C:/Users/User/Project_L/Assets/ProjectL/Script/Systems/RuntimeSystem/LifeCycleSystem/LifeCycles/Stage/Jobs/StageJob.cs",
    "fields": [
      {
        "line": 12,
        "text": "private readonly IInGameSystemGroupService _service"
      },
      {
        "line": 13,
        "text": "private readonly ICurrentProgressBufferReadSink _current"
      },
      {
        "line": 15,
        "text": "public int Priority"
      },
      {
        "line": 16,
        "text": "public bool IsCompleted"
      }
    ],
    "methods": [
      {
        "line": 18,
        "text": "internal StageJob(IInGameSystemGroupService service, int priority, ICurrentProgressBufferReadSink current)"
      },
      {
        "line": 26,
        "text": "public void Execute()"
      }
    ]
  },
  "NewRunJob": {
    "kind": "struct",
    "inherits": "IJobService",
    "source": "C:/Users/User/Project_L/Assets/ProjectL/Script/Systems/RuntimeSystem/LifeCycleSystem/LifeCycles/Stage/Jobs/StageJob.cs",
    "fields": [
      {
        "line": 47,
        "text": "private readonly IInGameSystemGroupService _service"
      },
      {
        "line": 48,
        "text": "private readonly int _loadoutKey"
      },
      {
        "line": 49,
        "text": "public int Priority"
      },
      {
        "line": 50,
        "text": "public bool IsCompleted"
      }
    ],
    "methods": [
      {
        "line": 52,
        "text": "internal NewRunJob(IInGameSystemGroupService service, int loadoutKey, int priority)"
      },
      {
        "line": 61,
        "text": "public void Execute()"
      }
    ]
  },
  "StagePlan": {
    "kind": "class",
    "inherits": "",
    "source": "C:/Users/User/Project_L/Assets/ProjectL/Script/Systems/RuntimeSystem/LifeCycleSystem/LifeCycles/Stage/Plans/StagePlan.cs",
    "fields": [
      {
        "line": 15,
        "text": "internal const int DefaultLoadoutKey"
      }
    ],
    "methods": [
      {
        "line": 17,
        "text": "internal static List<IJobService> Register(IInGameSystemGroupService service, JobType jobType, IBufferQuery buffers)"
      }
    ]
  },
  "StageLifeCycle": {
    "kind": "class",
    "inherits": "LifeCycleBase, ISystemInitData",
    "source": "C:/Users/User/Project_L/Assets/ProjectL/Script/Systems/RuntimeSystem/LifeCycleSystem/LifeCycles/Stage/StageLifeCycle.cs",
    "fields": [
      {
        "line": 28,
        "text": "private IJobRegisterSystem _jobRegister"
      },
      {
        "line": 29,
        "text": "private IJobRunner _jobRunner"
      },
      {
        "line": 30,
        "text": "private IArtifactEditingService _artifacts"
      },
      {
        "line": 31,
        "text": "private IArtifactBufferReadSink _artifactBoard"
      },
      {
        "line": 32,
        "text": "private IArtifactBoardConfigTableQuery _artifactBoardConfig"
      },
      {
        "line": 33,
        "text": "private ICharacterBufferReadSink _roster"
      },
      {
        "line": 35,
        "text": "public override LifeCycleKey Key"
      },
      {
        "line": 37,
        "text": "protected override int SceneIndex"
      },
      {
        "line": 40,
        "text": "internal IArtifactBoardConfigTableQuery ArtifactBoardConfig"
      }
    ],
    "methods": [
      {
        "line": 42,
        "text": "public void InitData(IServiceModule data)"
      },
      {
        "line": 51,
        "text": "protected override void OnBindServices(ISystemServiceStoreReader resolver)"
      },
      {
        "line": 60,
        "text": "protected override void OnEnter()"
      },
      {
        "line": 65,
        "text": "protected override void OnExit()"
      },
      {
        "line": 71,
        "text": "internal void BindServices(IJobRegisterSystem jobRegister, IJobRunner jobRunner, IArtifactEditingService artifacts, IArtifactBufferReadSink artifactBoard, ICharacterBufferReadSink roster)"
      },
      {
        "line": 84,
        "text": "internal void RegisterEntryJobs()"
      }
    ]
  },
  "JobSystemFactory": {
    "kind": "class",
    "inherits": "",
    "source": "C:/Users/User/Project_L/Assets/ProjectL/Script/Systems/Factory/Runtime/Global/JobSystemFactory.cs",
    "fields": [],
    "methods": [
      {
        "line": 13,
        "text": "internal static IExportSystemServices Create()"
      }
    ]
  },
  "JobQueue": {
    "kind": "class",
    "inherits": "",
    "source": "C:/Users/User/Project_L/Assets/ProjectL/Script/Framework/Utility/JobQueue.cs",
    "fields": [
      {
        "line": 33,
        "text": "private readonly List<Entry> _heap"
      },
      {
        "line": 34,
        "text": "private IComparer<TJob> _sortRule"
      },
      {
        "line": 35,
        "text": "private long _nextSequence"
      },
      {
        "line": 37,
        "text": "public int Count"
      },
      {
        "line": 42,
        "text": "public bool IsEmpty"
      }
    ],
    "methods": [
      {
        "line": 47,
        "text": "public JobQueue(IComparer<TJob> sortRule)"
      },
      {
        "line": 52,
        "text": "public JobQueue(IComparer<TJob> sortRule, int capacity)"
      },
      {
        "line": 65,
        "text": "public void Enqueue(TJob job)"
      },
      {
        "line": 74,
        "text": "public void EnqueueRange(IEnumerable<TJob> jobs)"
      },
      {
        "line": 89,
        "text": "public TJob Dequeue()"
      },
      {
        "line": 97,
        "text": "public bool TryDequeue(out TJob job)"
      },
      {
        "line": 109,
        "text": "public TJob Peek()"
      },
      {
        "line": 117,
        "text": "public bool TryPeek(out TJob job)"
      },
      {
        "line": 129,
        "text": "public void Clear()"
      },
      {
        "line": 135,
        "text": "public bool Contains(TJob job)"
      },
      {
        "line": 140,
        "text": "public bool Contains(TJob job, IEqualityComparer<TJob> equalityComparer)"
      },
      {
        "line": 154,
        "text": "public bool Remove(TJob job)"
      },
      {
        "line": 159,
        "text": "public bool Remove(TJob job, IEqualityComparer<TJob> equalityComparer)"
      },
      {
        "line": 180,
        "text": "public bool Refresh(TJob job)"
      },
      {
        "line": 185,
        "text": "public bool Refresh(TJob job, IEqualityComparer<TJob> equalityComparer)"
      },
      {
        "line": 207,
        "text": "public void Rebuild()"
      },
      {
        "line": 219,
        "text": "public void SetSortRule(IComparer<TJob> sortRule)"
      },
      {
        "line": 233,
        "text": "public List<TJob> GetSortedSnapshot()"
      },
      {
        "line": 255,
        "text": "private Entry DequeueEntry()"
      },
      {
        "line": 273,
        "text": "private void RemoveAt(int index)"
      },
      {
        "line": 292,
        "text": "private int SiftUp(int index)"
      },
      {
        "line": 308,
        "text": "private int SiftDown(int index)"
      },
      {
        "line": 338,
        "text": "private bool ComesBefore(Entry left, Entry right)"
      },
      {
        "line": 351,
        "text": "private bool ComesBeforeOrSame(Entry left, Entry right)"
      },
      {
        "line": 364,
        "text": "private void Swap(int a, int b)"
      }
    ]
  }
};
