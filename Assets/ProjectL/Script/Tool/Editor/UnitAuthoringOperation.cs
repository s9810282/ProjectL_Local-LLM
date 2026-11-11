using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Compilation;
using UnityEngine;
using Object = UnityEngine.Object;

namespace ProjectL.Tool.Editor
{
    /// <summary>창과 독립적으로 재컴파일·생성·등록·실패 복구의 순서를 소유합니다.</summary>
    [InitializeOnLoad]
    internal static class UnitAuthoringOperation
    {
        internal const string Waiting = "컴파일 대기";
        internal const string Working = "생성·등록 중";
        internal const string Failed = "오류·재시도 대기";
        internal const string Complete = "완료";
        internal const string Cancelled = "취소";
        internal const string Cancelling = "취소 정리 중";
        private const string SessionKey = "ProjectL.UnitAuthoring.AutoResume";
        private static readonly string DomainId = Guid.NewGuid().ToString("N");
        /// <summary>요청 뒤 컴파일 시작·재로드를 기다리는 상한입니다. 진행 중인 컴파일은 시간으로 끊지 않습니다(설계 UA7).</summary>
        private const double CompletionWaitSeconds = 120;
        private static UnitAuthoringRequest _current;
        private static double _nextCheck;
        private static string _storageError;
        private static bool _executing;
        private static bool _saveRetry;

        internal static UnitAuthoringRequest Current => _current;
        internal static string StorageError => _storageError;
        internal static bool HasPending => _current != null && _current.phase != Complete && _current.phase != Cancelled;
        internal static bool IsAutomatic => _current != null && SessionState.GetString(SessionKey, "") == _current.id;
        /// <summary>Unity를 다시 켜서 자동 진행이 멈춘 대기 요청입니다.</summary>
        internal static bool IsStalled => _current != null && _current.phase == Waiting && !IsAutomatic;

        /// <summary>완료 결과를 창이 한 번 열었다고 기록합니다(실패해도 다시 열지 않음).</summary>
        internal static void MarkResultOpened()
        {
            if (_current == null || _current.resultOpened) return;
            _current.resultOpened = true;
            try { Save(); }
            catch (Exception exception) { _storageError = "작업 기록 저장 실패: " + exception.Message; }
        }

        /// <summary>끝난 요청이나 읽지 못한 기록 파일을 지웁니다. 생성물·enum 백업은 남깁니다.</summary>
        internal static void DiscardRecord()
        {
            RequireIdle();
            if (HasPending && string.IsNullOrEmpty(_storageError))
                throw new InvalidOperationException("진행 중인 요청은 먼저 재개하거나 취소해 주세요.");
            UnitAuthoringPendingStore.Discard();
            _current = null;
            _storageError = null;
            SessionState.EraseString(SessionKey);
        }

        /// <summary>요청 뒤 바뀐 원본을 그대로 쓰기로 하고 지문을 다시 기록한 뒤 재개합니다.</summary>
        internal static void AcceptSourceChange()
        {
            RequireIdle();
            if (!HasPending) throw new InvalidOperationException("재개할 유닛 생성 요청이 없습니다.");
            _current.unitFingerprint = UnitAuthoringAssets.Fingerprint(UnitAuthoringAssets.Load(_current.unitSource, UnitAuthoringAssets.UnitType));
            _current.visualFingerprint = UnitAuthoringAssets.Fingerprint(UnitAuthoringAssets.Load(_current.visualSource, UnitAuthoringAssets.VisualType));
            _current.sourceChanged = false;
            Save();
            Begin();
        }

        static UnitAuthoringOperation()
        {
            try { _current = UnitAuthoringPendingStore.Load(); }
            catch (Exception exception) { _storageError = exception.Message; }
            EditorApplication.update += Update;
            CompilationPipeline.compilationStarted += CompilationStarted;
            CompilationPipeline.assemblyCompilationFinished += AssemblyCompilationFinished;
            CompilationPipeline.compilationFinished += CompilationFinished;
            AssemblyReloadEvents.beforeAssemblyReload += Detach;
            EditorApplication.quitting += Detach;
        }

        private static void Detach()
        {
            // 미뤄 둔 기록은 재로드 전에 한 번 더 씁니다.
            if (_saveRetry && _current != null)
            {
                try { Save(); _saveRetry = false; }
                catch (Exception exception) { Debug.LogWarning("[유닛 통합 에디터] 재로드 전 작업 기록 저장 실패: " + exception.Message); }
            }
            EditorApplication.update -= Update;
            CompilationPipeline.compilationStarted -= CompilationStarted;
            CompilationPipeline.assemblyCompilationFinished -= AssemblyCompilationFinished;
            CompilationPipeline.compilationFinished -= CompilationFinished;
            AssemblyReloadEvents.beforeAssemblyReload -= Detach;
            EditorApplication.quitting -= Detach;
        }

        internal static void Start(UnitAuthoringRequest request)
        {
            if (request == null) throw new ArgumentNullException(nameof(request));
            RequireIdle();
            if (!string.IsNullOrEmpty(_storageError)) throw new InvalidOperationException(_storageError);
            if (HasPending) throw new InvalidOperationException("진행 중인 유닛 생성 요청을 먼저 재개하거나 취소해 주세요.");
            if (EditorUtility.scriptCompilationFailed) throw new InvalidOperationException("Unity의 컴파일 오류를 먼저 해결해 주세요.");
            UnitAuthoringAssets.ValidateRequest(request);
            new UnitEntityKeySource().Validate(request.keyName, request.keyValue, request.addKey);
            request.id = Guid.NewGuid().ToString("N");
            request.rosters ??= Array.Empty<string>();
            request.rosterRegistered = new bool[request.rosters.Length];
            _current = request;
            Begin();
        }

        internal static void Retry()
        {
            RequireIdle();
            if (!HasPending) throw new InvalidOperationException("재개할 유닛 생성 요청이 없습니다.");
            if (_current.cancelRequested)
            {
                Cancel();
                return;
            }
            Begin();
        }

        private static void Begin()
        {
            try
            {
                // 이전에 완료한 생성·등록은 보존하고, 기록과 실제 에셋을 대조합니다.
                UnitAuthoringAssets.ValidateRequest(_current);
                var source = new UnitEntityKeySource();
                bool exists = source.Entries.TryGetValue(_current.keyName, out int value) && value == _current.keyValue;
                byte[] addition = null;
                if (!exists)
                {
                    if (!string.IsNullOrEmpty(_current.sourceAfter) && source.Hash != _current.sourceBefore)
                        throw new InvalidOperationException("요청 이후 enum 소스가 바뀌었고 요청한 키가 없습니다. 변경 내용을 확인해 주세요. 삭제된 키를 자동으로 다시 추가하지 않습니다.");
                    if (!_current.addKey) source.Validate(_current.keyName, _current.keyValue, false);
                    addition = source.AddedBytes(_current.keyName, _current.keyValue);
                    _current.sourceBefore = source.Hash;
                    _current.sourceAfter = UnitEntityKeySource.HashBytes(addition);
                    _current.sourceBackup = UnitEntityKeySource.BackupPath(_current.id);
                }
                else source.Validate(_current.keyName, _current.keyValue, false);
                _current.phase = Waiting;
                _current.error = "";
                _current.compilationRequested = addition != null || !UnitEntityKeySource.IsLoaded(_current.keyName, _current.keyValue) || EditorUtility.scriptCompilationFailed;
                _current.compilationStarted = false;
                _current.compilationFinished = false;
                _current.compilationHadErrors = false;
                _current.compilationDomain = DomainId;
                _current.compilationRequestedAt = EditorApplication.timeSinceStartup;
                _current.compilationFinishedAt = 0;
                Save();
                SessionState.SetString(SessionKey, _current.id);
                if (addition != null) source.Write(addition, _current.id);
                if (_current.compilationRequested)
                {
                    AssetDatabase.ImportAsset(UnitEntityKeySource.AssetPath, ImportAssetOptions.ForceUpdate);
                    CompilationPipeline.RequestScriptCompilation();
                }
                else Finish();
            }
            catch (Exception exception) { Fail(exception); }
        }

        private static bool TracksCompilation => _current != null && _current.phase == Waiting && IsAutomatic && _current.compilationRequested;

        private static void CompilationStarted(object context)
        {
            if (!TracksCompilation) return;
            _current.compilationStarted = true;
            _current.compilationFinished = false;
            _current.compilationHadErrors = false;
            SaveCompilationState();
        }

        private static void AssemblyCompilationFinished(string assemblyPath, CompilerMessage[] messages)
        {
            if (!TracksCompilation || !_current.compilationStarted || _current.compilationHadErrors) return;
            // 어셈블리마다 불리므로(수십 번) 오류가 처음 생길 때만 기록합니다.
            foreach (CompilerMessage message in messages)
                if (message.type == CompilerMessageType.Error)
                {
                    _current.compilationHadErrors = true;
                    SaveCompilationState();
                    return;
                }
        }

        private static void CompilationFinished(object context)
        {
            if (!TracksCompilation || !_current.compilationStarted) return;
            _current.compilationFinished = true;
            _current.compilationFinishedAt = EditorApplication.timeSinceStartup;
            SaveCompilationState();
        }

        private static void SaveCompilationState()
        {
            // 컴파일 콜백에서는 에셋 작업을 하지 않고 기록만 남깁니다.
            // 컴파일 중에는 파일이 잠깐 잠길 수 있으므로, 기록 실패로 요청을 실패시키지 않고 Update에서 다시 씁니다.
            try
            {
                Save();
                _saveRetry = false;
            }
            catch (Exception exception)
            {
                _saveRetry = true;
                Debug.LogWarning("[유닛 통합 에디터] 컴파일 중 작업 기록 저장을 미룹니다: " + exception.Message);
            }
        }

        private static void Update()
        {
            if (_executing || _current == null || _current.phase != Waiting || !IsAutomatic ||
                EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.isPlayingOrWillChangePlaymode ||
                EditorApplication.timeSinceStartup < _nextCheck) return;
            _nextCheck = EditorApplication.timeSinceStartup + 0.5;
            if (_saveRetry)
            {
                SaveCompilationState();
                if (_saveRetry) return;
            }
            if (!_current.compilationStarted)
            {
                if (EditorApplication.timeSinceStartup - _current.compilationRequestedAt >= CompletionWaitSeconds)
                    Fail(new InvalidOperationException("요청 뒤 컴파일이 시작되지 않았습니다. Console을 확인한 뒤 재개해 주세요. 보관된 생성물은 유지됩니다."));
                return;
            }
            // 시작한 컴파일은 끝날 때까지 기다립니다. 시간으로 끊지 않습니다.
            // 다만 도메인이 이미 바뀌었으면 컴파일은 끝난 것입니다(끝 기록을 저장하지 못한 채 재로드된 경우).
            if (!_current.compilationFinished && _current.compilationDomain != DomainId)
            {
                _current.compilationFinished = true;
                _current.compilationFinishedAt = EditorApplication.timeSinceStartup;
                SaveCompilationState();
            }
            if (!_current.compilationFinished) return;
            if (_current.compilationHadErrors || EditorUtility.scriptCompilationFailed)
            {
                Fail(new InvalidOperationException("Unity 컴파일에 실패했습니다. Console 오류를 해결한 뒤 재개해 주세요. 보관된 생성물은 유지됩니다."));
                return;
            }
            if (!UnitEntityKeySource.IsLoaded(_current.keyName, _current.keyValue))
            {
                if (_current.compilationDomain != DomainId)
                    Fail(new InvalidOperationException($"컴파일·재로드가 끝났지만 요청한 키가 없습니다: {_current.keyName} = {_current.keyValue}. enum 변경 내용을 확인해 주세요."));
                else if (EditorApplication.timeSinceStartup - _current.compilationFinishedAt >= CompletionWaitSeconds)
                    Fail(new InvalidOperationException("컴파일은 끝났지만 새 키를 로드한 도메인 재로드를 확인하지 못했습니다. 재로드 잠금·Console을 확인한 뒤 재개해 주세요."));
                return;
            }
            Finish();
        }

        private static void Finish()
        {
            _executing = true;
            try
            {
                if (!UnitEntityKeySource.IsLoaded(_current.keyName, _current.keyValue))
                    throw new InvalidOperationException("새 EntityKey가 아직 로드되지 않았습니다.");
                new UnitEntityKeySource().Validate(_current.keyName, _current.keyValue, false);
                UnitAuthoringAssets.ValidateRequest(_current);
                _current.phase = Working;
                Save();
                Object unit = !string.IsNullOrEmpty(_current.createdUnit)
                    ? UnitAuthoringAssets.LoadOwned(_current.createdUnit, _current.unitPath, UnitAuthoringAssets.UnitType, _current.keyValue)
                    : UnitAuthoringAssets.Create(UnitAuthoringAssets.Load(_current.unitSource, UnitAuthoringAssets.UnitType),
                        _current.unitPath, _current.keyValue, _current.displayName, _current.description, _current.eligibilityIndex, guid =>
                        {
                            _current.createdUnit = guid;
                            Save();
                        });
                Object visual = !string.IsNullOrEmpty(_current.createdVisual)
                    ? UnitAuthoringAssets.LoadOwned(_current.createdVisual, _current.visualPath, UnitAuthoringAssets.VisualType, _current.keyValue)
                    : UnitAuthoringAssets.Create(UnitAuthoringAssets.Load(_current.visualSource, UnitAuthoringAssets.VisualType),
                        _current.visualPath, _current.keyValue, _current.displayName, _current.description, _current.eligibilityIndex, guid =>
                        {
                            _current.createdVisual = guid;
                            Save();
                        });
                // 실제 등록을 재확인하여 저장 후·기록 전 중단에도 같은 참조를 중복 등록하지 않습니다.
                UnitAuthoringAssets.Register(UnitAuthoringAssets.Load(_current.unitTable, UnitAuthoringAssets.UnitTableType), unit);
                _current.unitRegistered = true;
                Save();
                UnitAuthoringAssets.Register(UnitAuthoringAssets.Load(_current.visualTable, UnitAuthoringAssets.VisualTableType), visual);
                _current.visualRegistered = true;
                Save();
                // 로스터는 마지막에 하나씩 넣고 그때마다 기록합니다(설계 UA4). 이미 들어 있으면 건너뜁니다.
                for (int i = 0; i < _current.rosters.Length; i++)
                {
                    Object roster = UnitAuthoringAssets.Load(_current.rosters[i], UnitAuthoringRosters.RosterType);
                    UnitAuthoringRosters.Add(roster, unit, false);
                    UnitAuthoringAssets.ValidateData(roster);
                    UnitAuthoringAssets.SaveChecked(roster);
                    _current.rosterRegistered[i] = true;
                    Save();
                }
                UnitAuthoringAssets.SaveChecked(unit);
                UnitAuthoringAssets.SaveChecked(visual);
                _current.phase = Complete;
                _current.error = "";
                Save();
                SessionState.EraseString(SessionKey);
            }
            catch (Exception exception) { Fail(exception); }
            finally { _executing = false; }
            // 완료 기록 이후의 UI 오류는 생성 작업을 실패로 되돌리지 않습니다.
            if (_current.phase == Complete)
            {
                try
                {
                    Object unit = UnitAuthoringAssets.Load(_current.createdUnit, UnitAuthoringAssets.UnitType);
                    Selection.activeObject = unit;
                    EditorGUIUtility.PingObject(unit);
                }
                catch (Exception exception) { Debug.LogWarning("[유닛 통합 에디터] 생성은 완료했지만 결과 선택에 실패했습니다: " + exception.Message); }
            }
        }

        private static void Fail(Exception exception)
        {
            SessionState.EraseString(SessionKey);
            string error = exception.Message;
            _current.phase = Failed;
            _current.error = error;
            _current.sourceChanged = exception is UnitAuthoringAssets.SourceChangedException;
            try { Save(); }
            catch (Exception storage) { _storageError = "작업 기록 저장 실패: " + storage.Message; }
            Debug.LogError("[유닛 통합 에디터] " + error);
        }

        private static void Cleanup()
        {
            var errors = new List<string>();
            // 에셋을 먼저 지우면 로스터에 Missing 참조가 남으므로 로스터부터 뺍니다(설계 UA4).
            Object created = string.IsNullOrEmpty(_current.createdUnit) ? null
                : AssetDatabase.LoadMainAssetAtPath(AssetDatabase.GUIDToAssetPath(_current.createdUnit));
            for (int i = 0; i < _current.rosters.Length; i++)
            {
                try
                {
                    if (created != null)
                    {
                        Object roster = UnitAuthoringAssets.Load(_current.rosters[i], UnitAuthoringRosters.RosterType);
                        UnitAuthoringRosters.Remove(roster, created, false);
                        UnitAuthoringAssets.SaveChecked(roster);
                    }
                    _current.rosterRegistered[i] = false;
                    Save();
                }
                catch (Exception exception) { errors.Add(exception.Message); }
            }
            if (errors.Count > 0) throw new InvalidOperationException(string.Join("\n", errors));
            try
            {
                UnitAuthoringAssets.RemoveOwned(_current.unitTable, _current.createdUnit, _current.unitPath);
                _current.createdUnit = "";
                _current.unitRegistered = false;
                Save();
            }
            catch (Exception exception) { errors.Add(exception.Message); }
            try
            {
                UnitAuthoringAssets.RemoveOwned(_current.visualTable, _current.createdVisual, _current.visualPath);
                _current.createdVisual = "";
                _current.visualRegistered = false;
                Save();
            }
            catch (Exception exception) { errors.Add(exception.Message); }
            if (errors.Count > 0) throw new InvalidOperationException(string.Join("\n", errors));
        }

        internal static void Cancel()
        {
            RequireIdle();
            if (!HasPending) return;
            _executing = true;
            try
            {
                SessionState.EraseString(SessionKey);
                _current.cancelRequested = true;
                _current.phase = Cancelling;
                Save();
                Cleanup();
                _current.phase = Cancelled;
                _current.error = "생성을 취소했습니다. 이미 추가한 EntityKey는 유지됩니다. 기존 키 모드로 다시 사용할 수 있습니다.";
                Save();
                SessionState.EraseString(SessionKey);
            }
            catch (Exception exception) { Fail(exception); }
            finally { _executing = false; }
        }

        private static void Save()
        {
            UnitAuthoringPendingStore.Save(_current);
            _storageError = null;
        }

        private static void RequireIdle()
        {
            if (_executing || EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("편집 모드에서 컴파일·에셋 갱신이 끝난 뒤 실행해 주세요.");
        }
    }
}
