#if UNITY_EDITOR
using System;
using System.Linq;
using System.Collections.Generic;
using ProjectL.Framework.Composition;
using ProjectL.Framework.DataBase;
using ProjectL.Framework.SystemBase;
using ProjectL.Systems.DataBaseSystem;
using ProjectL.Systems.Factory.DBFactory;
using ProjectL.Systems.Factory.Runtime;
using ProjectL.Systems.Factory.Runtime.InGame.View;
using ProjectL.Systems.RuntimeSystem.ErrorHandling;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.RenderSystem.Core.EffectSystem;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Display.Field;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Display.Map;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Display.Unit;
using ProjectL.Systems.ServiceKeys.Runtime;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Dev.TraitPreview.Scenario;
using UnityEngine;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Dev.TraitPreview
{
    /// <summary>전용 씬에서 서비스 런타임과 확인 세션을 한 번씩 진행합니다. 게임 구간에는 진입하지 않습니다.</summary>
    public sealed class TraitActivationPreviewHost : MonoBehaviour
    {
        [SerializeField]
        private DatabaseCatalogSO catalog;
        private readonly HashSet<string> _bodyTextDependencies = new(StringComparer.Ordinal);
        public bool IsBodyTextDependency(string path) => _bodyTextDependencies.Contains(path);
        [SerializeField]
        private FieldView field;
        [SerializeField]
        private FieldUnitPresenter units;
        [SerializeField]
        private Camera output;
        private FrameworkRuntime _runtime;
        private StageViewPropPresenter _backdrop;
        private EffectScope _scope;
        private GameObject _pointerInput;
        private TraitPreviewSession _session;
        private RuntimeFaultBoundary _faults;
        private int _stageViewKey;
        private bool _closed;
        public int ScopeAcquisitionCount { get; private set; }
        public int CompletedCount => _session?.CompletedCount ?? 0;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetBridge() => TraitActivationPreviewBridge.Host = null;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void ConnectSceneHost()
        {
            // Scene Reload가 꺼져 OnEnable이 다시 오지 않아도 진입 경계에서 한 번 연결합니다.
            foreach (TraitActivationPreviewHost host in FindObjectsByType<TraitActivationPreviewHost>(FindObjectsSortMode.None))
            {
                if (!host.isActiveAndEnabled)
                    continue;
                host.OnEnable();
            }
        }

        private void OnEnable()
        {
            if (!Application.isPlaying)
                return;
            if (TraitActivationPreviewBridge.Host != null && TraitActivationPreviewBridge.Host != this)
                throw new InvalidOperationException("특성 확인 호스트가 둘 이상입니다.");
            TraitActivationPreviewBridge.Host = this;
            _closed = false;
            if (_runtime == null)
                ScopeAcquisitionCount = 0;
        }

        internal DatabaseCatalogSO Catalog => catalog;

        public void Play(TraitActivationPreviewInput input) => Play(TraitPreviewScenarioImport.FromSimple(input));
        public void Play(TraitPreviewScenarioInput original)
        {
            if (field == null || units == null || output == null || catalog == null)
                throw new InvalidOperationException("특성 확인 씬의 Catalog·Field·Units·Output 참조가 필요합니다.");
            // 합성 계약과 최신 DB를 먼저 검사해 입력 오류가 이전 장면을 지우지 않게 합니다.
            TraitPreviewScenarioInput input = original?.Copy() ?? throw new ArgumentNullException(nameof(original));
            CompiledTraitPreviewScenario result = CombatTraitPreviewScenarioFactory.Create().Compile(input);
            var database = DataBaseFactory.Create(catalog);
            CombatDisplayTables tables = CombatDisplayTables.From(((IHasViewQuery)database).ViewQuery);
            foreach (var entry in result.Entries)
                if (entry.Result != null)
                    foreach (var item in entry.Result.Events)
                        if (item is CoreSystem.CoreCaculator.Contracts.Combat.CombatEffectsEvent effects)
                            foreach (var trait in effects.Traits)
                                if (!tables.Traits.TryGetTraitVisual(trait.TraitKey, out _))
                                    throw new ArgumentException($"재생 Catalog에 등록되지 않은 특성 외형입니다: {trait.TraitKey}");
            foreach (var character in result.Initial)
                tables.Units.GetUnitVisual(character.CharacterKey);
            var feedbackRows = new Combat.Playback.Support.CombatActivationRows(tables.CueSources);
            var feedbackEvidence = CombatTraitFeedbackFactory.CreateEvidence();
            foreach (var entry in result.Entries)
            {
                if (entry.Result == null)
                    continue;
                var plan = Combat.Playback.Support.CombatTraitFeedbackPlan.Build(entry.Result, feedbackRows, feedbackEvidence);
                if (plan.Issues.Count > 0)
                    throw new ArgumentException(string.Join("\n", plan.Issues));
            }

            CombatBodyTextFactory.ValidatePrefabs(tables.CueSources, tables.PresentationSettings.BodyTextPrefab);
            var bodyDependencies = new HashSet<string>(StringComparer.Ordinal);
            foreach (var prefab in tables.CueSources.BodyTextPrefabs(tables.PresentationSettings.BodyTextPrefab))
                foreach (string dependency in UnityEditor.AssetDatabase.GetDependencies(UnityEditor.AssetDatabase.GetAssetPath(prefab)))
                    bodyDependencies.Add(dependency);
            if (_closed)
                throw new InvalidOperationException("확인 환경이 해제됐습니다. 환경을 다시 여세요.");
            if (_runtime != null && _stageViewKey != input.stageViewKey)
                throw new InvalidOperationException("맵 변경은 환경을 종료한 뒤 다시 열어 적용하세요.");
            try
            {
                PlayPrepared(input, result, database, tables);
                _bodyTextDependencies.Clear();
                foreach (string dependency in bodyDependencies)
                    _bodyTextDependencies.Add(dependency);
            }
            catch
            {
                Shutdown();
                throw;
            }
        }

        private void PlayPrepared(TraitPreviewScenarioInput input, CompiledTraitPreviewScenario result, IServiceModule database, CombatDisplayTables tables)
        {
            if (_runtime == null)
            {
                _stageViewKey = input.stageViewKey;
                _faults = new RuntimeFaultBoundary(Fail);
                _backdrop = new StageViewPropPresenter(tables.StageProps, field.CellsRoot, tables.Field);
                _backdrop.Show(_stageViewKey, new Vector2Int(8, 8));
                _runtime = FrameworkRuntimeFactory.Create(faultBoundary: _faults);
                _runtime.BindScene();
                ThrowIfFaulted();
                _runtime.InitData(database);
                ThrowIfFaulted();
                _runtime.PostBindService();
                ThrowIfFaulted();
                _runtime.Initialize();
                ThrowIfFaulted();
                CombatDisplayServices services = CombatDisplayServices.From(_runtime.GetService(SystemGroupsService.Global));
                _scope = services.Effects.GetEffectHandle();
                ++ScopeAcquisitionCount;
                _pointerInput = CombatTraitPreviewFactory.CreatePointerInput(transform);
                _session = new TraitPreviewSession(() => CombatTraitPreviewFactory.Create(field, units, output, transform, CombatDisplayTables.From(((IHasViewQuery)DataBaseFactory.Create(catalog)).ViewQuery), services, _backdrop, _scope), value => CombatTraitPreviewScenarioFactory.Create().Compile(value));
            }

            RequireInactiveGame();
            _session.Play(input.Copy(), result);
        }

        public void Stop() => _session?.Stop();
        public void Advance() => _session?.Advance();
        public void SetBoundaryPause(bool value) => _session?.SetBoundaryPause(value);
        public bool CanAdvance => _session?.CanAdvance ?? false;
        public bool BoundaryPause => _session?.BoundaryPause ?? false;
        public string Diagnostic => _session?.Diagnostic ?? "";
        public string Status => _closed ? $"환경 종료 — {_faults?.FirstError?.Message ?? "환경을 다시 열어 주세요."}" : _session?.Status ?? "준비됨 — 연출 확인 / 합성 결과";
        public bool HasMapCamera => _backdrop != null && _backdrop.MapCameras.Count > 0;

        private void Update()
        {
            if (_runtime == null || _closed)
                return;
            if (_faults.IsFaulted)
            {
                Shutdown();
                return;
            }

            try
            {
                RequireInactiveGame();
                _runtime.Tick(Time.deltaTime);
                if (_closed || _faults.IsFaulted)
                {
                    Shutdown();
                    return;
                }

                _session.Tick(Time.deltaTime);
            }
            catch (Exception error)
            {
                _faults.Report(error);
                Shutdown();
            }
        }

        private void RequireInactiveGame()
        {
            if (_runtime.GetService(SystemGroupsService.LifeCycle).LifeCycleManager.HasActiveLifeCycle || _runtime.GetService(BufferSystemKey.Buffer).CombatStateReadSink.HasBattle)
                throw new InvalidOperationException("연출 확인 환경에서 운영 구간/전투가 시작됐습니다.");
        }

        private void Fail(Exception error)
        {
            // 경계 내부 콜백에서 초기화·Tick 도중 재진입 해제하지 않습니다. 호출 단계가 끝난 뒤 호스트가 정리합니다.
            Debug.LogException(error);
        }

        private void ThrowIfFaulted()
        {
            if (_faults.IsFaulted)
                throw new InvalidOperationException("특성 확인 서비스 초기화가 실패했습니다.", _faults.FirstError);
        }

        public void Shutdown()
        {
            if (_closed)
                return;
            _closed = true;
            List<Exception> errors = null;
            CleanupErrors.Try(() => _session?.Dispose(), ref errors);
            CleanupErrors.Try(() =>
            {
                if (_pointerInput != null)
                {
                    _pointerInput.SetActive(false);
                    Destroy(_pointerInput);
                }

                _pointerInput = null;
            }, ref errors);
            CleanupErrors.Try(() => _runtime?.Shutdown(), ref errors);
            CleanupErrors.Try(() => _backdrop?.Release(), ref errors);
            _session = null;
            _runtime = null;
            _scope = null;
            _backdrop = null;
            if (errors != null)
                Debug.LogException(new AggregateException("특성 확인 환경 종료 실패", errors));
        }

        private void OnDisable()
        {
            if (TraitActivationPreviewBridge.Host == this)
                TraitActivationPreviewBridge.Host = null;
            Shutdown();
        }
    }

    /// <summary>기본 Editor 어셈블리에 공개하는 개발용 창구입니다. 운영 내부 타입은 노출하지 않습니다.</summary>
    public static class TraitActivationPreviewBridge
    {
        public static TraitActivationPreviewHost Host { get; internal set; }

        public static string[] RegisteredTraitPaths() => Scenario.PreviewTraitRegistration.Assets()
            .Select(asset => UnityEditor.AssetDatabase.GetAssetPath(asset)).ToArray();

        public static string ValidateScenario(TraitPreviewScenarioInput input)
        {
            var program = CombatTraitPreviewScenarioFactory.Create().Compile(input.Copy());
            return $"구성 유효: 유닛 {program.Initial.Count}명 / 순서 {program.Entries.Count}개. 합성 결과이며 실제 조건·피해 공식은 계산하지 않습니다.";
        }

        public static string FeedbackInfo(UnityEngine.Object asset, int kind)
        {
            if (asset is not ProjectL.Systems.DataBaseSystem.Data.View.Table.Trait.Item.TraitVisualDataSO so)
                return "";
            var data = (ProjectL.Systems.DataBaseSystem.Data.View.Table.Trait.Item.TraitVisualData)so.GetData();
            if (!data.TryGetFirstActivation((CoreSystem.CoreCaculator.Contracts.Common.TraitActivationKind)kind, out var row))
                return "텍스트/큐 줄 없음";
            return $"큐 시작: {row.CueTiming} / 몸체 텍스트: {(row.BodyText == null ? "없음" : row.BodyText.Count.ToString())}" + (row.BodyText == null ? "" : $"\n외형: {(row.BodyText.PrefabOverride != null ? row.BodyText.PrefabOverride.name : "공통 Body Text Prefab")} (프리팹에서 편집)") + (row.CueTiming == DataBaseSystem.Data.View.CombatPresentation.CombatActivationCueTiming.StateApplied ? "\n같은 묶음에 Activation → State가 필요합니다. 가속 X N은 앞선 Movement도 같은 묶음에 두세요." : "");
        }

        public static int[] Kinds(UnityEngine.Object asset)
        {
            if (asset is not ProjectL.Systems.DataBaseSystem.Data.View.Table.Trait.Item.TraitVisualDataSO so)
                return Array.Empty<int>();
            var data = (ProjectL.Systems.DataBaseSystem.Data.View.Table.Trait.Item.TraitVisualData)so.GetData();
            var kinds = new List<int>();
            foreach (CoreSystem.CoreCaculator.Contracts.Common.TraitActivationKind kind in Enum.GetValues(typeof(CoreSystem.CoreCaculator.Contracts.Common.TraitActivationKind)))
                if (data.TryGetFirstActivation(kind, out _))
                    kinds.Add((int)kind);
            return kinds.ToArray();
        }

        public static string Describe(TraitActivationPreviewInput input)
        {
            if (input.traitAsset is not ProjectL.Systems.DataBaseSystem.Data.View.Table.Trait.Item.TraitVisualDataSO so)
                return "특성 외형을 선택하세요.";
            try
            {
                var data = (ProjectL.Systems.DataBaseSystem.Data.View.Table.Trait.Item.TraitVisualData)so.GetData();
                if (!data.TryGetFirstActivation(input.kind, out var row))
                    return $"{data.Key} / {input.kind}: 줄 없음 — 큐·유닛 행동 없음. 추가 공격 자체는 결과에 따라 재생됩니다.";
                return FeedbackInfo(input.traitAsset, (int)input.kind) + "\n" + $"{data.Key} / {input.kind} / {input.context}\n큐: {(row.Cues.IsEmpty ? "없음" : "있음")}, " + $"유닛 행동: {row.UnitActions.Count}개, " + $"추가 공격 표시: {(row.AttackPresentation == null ? "유닛 기본" : "덮어쓰기 요청 (fallback 경고는 Console)")}";
            }
            catch (Exception error)
            {
                return $"설정 오류: {error.Message}";
            }
        }
    }
}
#endif
