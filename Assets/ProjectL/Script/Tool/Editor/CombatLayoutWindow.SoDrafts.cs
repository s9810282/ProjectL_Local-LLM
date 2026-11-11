using System;
using UnityEditor;
using UnityEngine;

namespace ProjectL.Tool.Editor
{
    // 초안의 해석·보호 판단과 사용자 명령입니다. 원문 입출력은 DraftStore만 수행합니다.
    public sealed partial class CombatLayoutWindow
    {
        private string SoDraftKey => DraftKey + ".So";
        private string SoRecoveryKey => CombatStageSoDraftStore.RecoveryKey(SoDraftKey);
        [NonSerialized] private string _soDraftExpectedRaw, _soDraftError, _soInvalidDraftReason;
        [NonSerialized] private bool _soDraftKnown, _soDraftInvalidDocument;
        [SerializeField] private string soProtectedMemoryHash;
        [NonSerialized] private string _soValidatedActiveRaw;
        // 복원 경계를 Undo해 같은 Root의 다른 작업본으로 돌아간 경우도 구분합니다.
        [SerializeField] private string soDraftRestoreBoundary = "";
        [NonSerialized] private string _soObservedRestoreBoundary = "";
        // 쓰기 실패 뒤 이전 원문 복구까지 실패한 경우 도메인 리로드에도 원문을 남깁니다.
        [SerializeField] private bool soDraftRollbackPending, soDraftRollbackMissing;
        [SerializeField] private string soDraftRollbackRaw, soDraftAttemptedRaw;
        // 에셋 저장 성공 뒤 초안 정리 실패는 미저장 작업과 구분합니다.
        [SerializeField] private bool soDraftClearPending;
        [SerializeField] private string soDraftClearRaw;
        [NonSerialized] private (bool Rollback, bool Missing, string Raw, string Attempted, bool Clear, string ClearRaw) _soStorageFailure;
        // 도메인 리로드 직전에 현재 기준을 보관합니다. 창 Undo의 과거 값은 OnDisable에서 다시 덮습니다.
        [SerializeField] private bool soDraftBaselineKnown, soDraftBaselineMissing;
        [SerializeField] private string soDraftBaselineRaw, soDraftBaselineError, soDraftBaselineRestoreOwner;
        [NonSerialized] private string _soDraftRestoreOwner;

        private bool SoDraftBlocked => !string.IsNullOrEmpty(_soDraftError) || soDraftRollbackPending || soDraftClearPending;
        private bool SoDraftEditable => !SoDraftBlocked && !_soDraftInvalidDocument;

        private void InitializeSoDraftProtection()
        {
            soDraftChecked = false; _soDraftKnown = soDraftBaselineKnown;
            _soDraftExpectedRaw = soDraftBaselineMissing ? null : soDraftBaselineRaw;
            _soDraftError = soDraftBaselineError; _soValidatedActiveRaw = null;
            _soDraftRestoreOwner = soDraftBaselineRestoreOwner;
            _soObservedRestoreBoundary = soDraftRestoreBoundary;
            RememberSoDraftStorageFailure();
        }

        private void CaptureSoDraftProtection()
        {
            soDraftBaselineKnown = _soDraftKnown;
            soDraftBaselineMissing = _soDraftExpectedRaw == null;
            soDraftBaselineRaw = _soDraftExpectedRaw;
            soDraftBaselineError = _soDraftError;
            soDraftBaselineRestoreOwner = _soDraftRestoreOwner;
        }

        private void RememberSoDraftStorageFailure() => _soStorageFailure =
            (soDraftRollbackPending, soDraftRollbackMissing, soDraftRollbackRaw, soDraftAttemptedRaw, soDraftClearPending, soDraftClearRaw);

        // 창 Undo가 입출력 실패 원문까지 과거 값으로 되돌리지 않도록 현재 실패 기록을 유지합니다.
        private void PreserveSoDraftStorageFailure()
        {
            soDraftRollbackPending = _soStorageFailure.Rollback; soDraftRollbackMissing = _soStorageFailure.Missing;
            soDraftRollbackRaw = _soStorageFailure.Raw; soDraftAttemptedRaw = _soStorageFailure.Attempted;
            soDraftClearPending = _soStorageFailure.Clear; soDraftClearRaw = _soStorageFailure.ClearRaw;
        }

        private static bool TryReadSoDraft(string raw, out CombatStageSoDocument candidate, out string reason)
        {
            candidate = null;
            try
            {
                if (string.IsNullOrWhiteSpace(raw)) { reason = "빈 문자열 초안입니다."; return false; }
                candidate = JsonUtility.FromJson<CombatStageSoDocument>(raw);
                return IsSoDraftEditable(candidate, out reason);
            }
            catch (Exception exception) { reason = "JSON을 읽지 못했습니다: " + exception.Message; return false; }
        }

        private static bool IsSoDraftEditable(CombatStageSoDocument candidate, out string reason)
        {
            if (candidate == null || !candidate.IsOpen) reason = "열린 작업본으로 해석할 수 없습니다(Root 정보 없음).";
            else if (candidate.format != CombatStageSoDocument.CurrentFormat)
                reason = $"초안 형식 {candidate.format}은 현재 형식 {CombatStageSoDocument.CurrentFormat}과 다릅니다. 자동 변환하지 않습니다.";
            else if (CombatStageSoAssets.Load(candidate.rootGuid) == null) reason = "초안의 Stage Root 에셋을 찾을 수 없습니다: " + candidate.rootGuid;
            else if (candidate.view == null || candidate.rockfall == null || candidate.haboob == null
                || candidate.sharedEdits == null || candidate.enemyUnits == null || candidate.structures == null
                || candidate.terrains == null || candidate.obstacles == null || candidate.objects == null
                || candidate.railCells == null || candidate.playerDeploymentCells == null || candidate.structureCatalog == null
                || candidate.mainCharacter == null || candidate.enemyHeadquarters == null || candidate.legacyTerrainSourceIds == null)
                reason = "작업본의 필수 목록/설정이 없어 편집할 수 없습니다.";
            else { reason = null; return true; }
            return false;
        }

        // 머리줄·키 처리보다 먼저 호출합니다. DT에서는 읽지 않은 SO 초안을 건드리지 않습니다.
        private void PrepareSoDraftProtection(bool restoreActive = true)
        {
            if (soDocument == null) soDocument = new CombatStageSoDocument();
            _soDraftInvalidDocument = soDocument.IsOpen && !IsSoDraftEditable(soDocument, out _soInvalidDraftReason);
            if (!soDraftChecked)
            {
                soDraftChecked = true;
                _soObservedRestoreBoundary = soDraftRestoreBoundary;
                // 저장 뒤 정리하지 못한 원문을 재컴파일에서 미저장 작업으로 복원하지 않습니다.
                if (soDraftRollbackPending || soDraftClearPending)
                {
                    _soDraftExpectedRaw = soDraftRollbackPending
                        ? (soDraftRollbackMissing ? null : soDraftRollbackRaw) : soDraftClearRaw;
                    _soDraftKnown = true;
                }
                else if (!_soDraftKnown && !SoDraftBlocked && restoreActive) RestoreSoDraft();
            }
            if (SoDraftBlocked) return;
            var active = CombatStageSoDraftStore.ReadRaw(SoDraftKey);
            if (!active.Succeeded) { _soDraftError = active.Message; return; }
            if (_soDraftKnown && active.Raw != _soDraftExpectedRaw)
            {
                _soDraftError = "활성 초안이 마지막 확인 뒤 바뀌었습니다. 현재 작업과 원문을 내보내고 대상별로 정리하세요.";
                return;
            }
            _soDraftKnown = true; _soDraftExpectedRaw = active.Raw;
            if (active.Status != CombatStageSoDraftStatus.Empty && _soValidatedActiveRaw != active.Raw)
            {
                if (!TryReadSoDraft(active.Raw, out _, out string reason))
                {
                    if (!ProtectSoDraft(active.Raw, reason, "EditorPrefs 활성 원문")) return;
                    // 재컴파일로 남은 작업본보다 업데이트 전 EditorPrefs 원문을 우선합니다.
                    if (soDocument.IsOpen && !IsSoDraftEditable(soDocument, out _))
                        soProtectedMemoryHash = Hash128.Compute(JsonUtility.ToJson(soDocument)).ToString();
                }
                else _soValidatedActiveRaw = active.Raw;
            }
            _soDraftInvalidDocument = soDocument.IsOpen && !IsSoDraftEditable(soDocument, out _soInvalidDraftReason);
            if (_soDraftInvalidDocument) ReopenSoOldFormat();
        }

        private bool ProtectSoDraft(string raw, string reason, string source)
        {
            var result = CombatStageSoDraftStore.ProtectRaw(SoDraftKey, raw, reason, source);
            if (!result.Succeeded) { _soDraftError = result.Message; return false; }
            _soDraftExpectedRaw = null; _soDraftKnown = true;
            return true;
        }

        private void RestoreSoDraft()
        {
            var result = CombatStageSoDraftStore.ReadRaw(SoDraftKey);
            if (!result.Succeeded) { _soDraftError = result.Message; return; }
            _soDraftExpectedRaw = result.Raw; _soDraftKnown = true;
            if (result.Status == CombatStageSoDraftStatus.Empty) return;
            if (!TryReadSoDraft(result.Raw, out var restored, out string reason))
            {
                if (ProtectSoDraft(result.Raw, reason, "EditorPrefs 활성 원문") && soDocument.IsOpen
                    && !IsSoDraftEditable(soDocument, out _)) soProtectedMemoryHash = Hash128.Compute(JsonUtility.ToJson(soDocument)).ToString();
                return;
            }
            _soValidatedActiveRaw = result.Raw;
            if (soDocument.IsOpen) return;
            soDocument = restored;
            ResetSoDraftDocumentView();
            InvalidateSoCheck(changed: true); soModified = true;
            SetSoNotice("저장하지 않은 마지막 SO 편집 초안을 복원했습니다.");
        }

        // 이름은 기존 호출부와 호환합니다. 구형 작업본을 자동 폐기하거나 에셋에서 다시 읽지 않습니다.
        private void ReopenSoOldFormat()
        {
            if (!_soDraftInvalidDocument || SoDraftBlocked) return;
            string raw = JsonUtility.ToJson(soDocument);
            if (soProtectedMemoryHash == Hash128.Compute(raw).ToString()) return;
            var active = CombatStageSoDraftStore.ReadRaw(SoDraftKey);
            if (!active.Succeeded) { _soDraftError = active.Message; return; }
            if (ProtectSoDraft(active.Raw ?? raw, _soInvalidDraftReason,
                active.Raw == null ? "창 메모리 직렬화 사본(이전 형식의 알 수 없는 필드는 포함되지 않을 수 있음)" : "EditorPrefs 활성 원문"))
                soProtectedMemoryHash = Hash128.Compute(raw).ToString();
        }

        private void SaveSoDraft()
        {
            if (editMode != CombatLayoutEditMode.So || soDocument == null || !soDocument.IsOpen) return;
            PrepareSoDraftProtection();
            if (!soModified || !SoDraftEditable) return;
            string raw;
            try { raw = JsonUtility.ToJson(soDocument); }
            catch (Exception exception) { _soDraftError = "작업본 직렬화 실패: " + exception.Message; return; }
            ReplaceSoActiveDraft(raw);
        }

        private bool ReplaceSoActiveDraft(string raw)
        {
            string before = _soDraftExpectedRaw;
            var result = CombatStageSoDraftStore.ReplaceVerified(SoDraftKey, before, raw);
            if (!result.Succeeded)
            {
                _soDraftError = result.Message;
                if (result.RollbackFailed)
                {
                    soDraftRollbackPending = true; soDraftRollbackMissing = before == null;
                    soDraftRollbackRaw = before; soDraftAttemptedRaw = raw;
                    RememberSoDraftStorageFailure();
                }
                return false;
            }
            _soDraftExpectedRaw = raw; _soDraftKnown = true;
            return true;
        }

        private bool ClearSoActiveDraft()
        {
            var result = CombatStageSoDraftStore.ClearIfMatches(SoDraftKey, _soDraftExpectedRaw);
            if (!result.Succeeded)
            {
                soDraftClearPending = true; soDraftClearRaw = _soDraftExpectedRaw;
                RememberSoDraftStorageFailure();
                _soDraftError = "에셋 저장/작업본 교체와 별개로 활성 초안 정리를 완료하지 못했습니다: " + result.Message;
                return false;
            }
            _soDraftExpectedRaw = null; _soDraftKnown = true;
            return true;
        }

        private void ResetSoDraftDocumentView()
        {
            CancelSoWorkspaceInput();
            _soInspectedPlacement = null;
            ClearSharedCopies(); soFieldColumns = 0; soField = null; soZone = null; soOccupants = null;
            soHasBox = false; soSelectedCell = new Vector2Int(-1, -1);
            soTileSelection.Clear(); soTileSelectionKey = null; soTileBand = null;
            ResetSoGimmickShapeViews();
            soPreview?.Invalidate(); soPreviewNeedsReset = true;
            _soDraftInvalidDocument = false;
            ClearSoIssueFocus();
        }

        // 복원 경계의 Undo는 현재 모드가 DT여도 SO 작업본 정리/보호를 먼저 수행합니다.
        private bool HandleSoDraftRestoreUndo()
        {
            if (_soObservedRestoreBoundary == soDraftRestoreBoundary) return false;
            string previousBoundary = _soObservedRestoreBoundary;
            _soObservedRestoreBoundary = soDraftRestoreBoundary;
            ResetSoDraftDocumentView();
            soModified = soDocument != null && soDocument.IsOpen;
            // 빈 창 Undo를 자동 복원으로 즉시 취소하지 않습니다. 읽지 않은 다른 초안도 지우지 않습니다.
            PrepareSoDraftProtection(restoreActive: false);
            if (!soModified)
            {
                if (_soDraftExpectedRaw != null)
                {
                    if (!string.IsNullOrEmpty(previousBoundary) && _soDraftRestoreOwner == previousBoundary)
                    {
                        if (SoDraftBlocked)
                        {
                            soDraftClearPending = true; soDraftClearRaw = _soDraftExpectedRaw;
                            RememberSoDraftStorageFailure();
                        }
                        else ClearSoActiveDraft();
                    }
                    else _soDraftError = "빈 창으로 되돌렸지만 다른 작업의 활성 초안은 유지했습니다. 원문 내보내기·대상별 폐기로 확인하세요.";
                }
                _soDraftRestoreOwner = null;
            }
            else _soDraftRestoreOwner = soDraftRestoreBoundary;
            InvalidateSoCheck(changed: true);
            if (soModified) SaveSoDraft();
            Repaint();
            return true;
        }

        private void DrawSoDraftProtection()
        {
            var protectedRead = CombatStageSoDraftStore.ReadRaw(SoRecoveryKey);
            if (!protectedRead.Succeeded) _soDraftError = "보호 초안 읽기 실패: " + protectedRead.Message;
            bool hasProtected = protectedRead.Status == CombatStageSoDraftStatus.Success;
            string reopenRoot = soDocument.rootGuid;
            if (!hasProtected && !SoDraftBlocked && !_soDraftInvalidDocument) return;
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField("초안 보호 · 이 PC에만 보관", EditorStyles.boldLabel);
                if (SoDraftBlocked)
                {
                    EditorGUILayout.HelpBox("초안 보관 실패/충돌: " + (_soDraftError ?? "이전 원문 복구 또는 활성 초안 정리가 필요합니다.")
                        + "\n원문을 덮는 편집·열기·저장·등록·복제는 해결 전까지 막습니다. 내보내기만으로 원문은 삭제되지 않습니다.", MessageType.Error);
                    if (GUILayout.Button("보관 상태 재확인 / 실패 작업 재시도")) { RetrySoDraftStorage(); GUIUtility.ExitGUI(); }
                }
                if (_soDraftInvalidDocument)
                    EditorGUILayout.HelpBox("열린 작업본은 복원 불가: " + _soInvalidDraftReason + "\n보호 원문을 남기고 에셋을 다시 열거나 다른 스테이지를 고르세요.", MessageType.Warning);
                if (hasProtected)
                {
                    bool readable = CombatStageSoDraftStore.TryReadProtected(protectedRead.Raw, out var envelope);
                    if (!soDocument.IsOpen && readable) reopenRoot = envelope.rootGuid;
                    EditorGUILayout.HelpBox(readable
                        ? $"보호된 초안 있음 · 형식 {envelope.originalFormat} · Root {envelope.rootGuid}\n보관 시각(UTC): {envelope.storedUtc}\n이유: {envelope.reason}\n출처: {envelope.source}"
                        : "보호 봉투를 읽을 수 없습니다. 원문을 그대로 내보내거나 명시적으로 폐기할 수 있습니다.", MessageType.Warning);
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        if (GUILayout.Button("보호 원문 내보내기")) ExportSoDraftRaw(readable ? envelope.raw : protectedRead.Raw, "보호 초안");
                        bool canRestore = readable && TryReadSoDraft(envelope.raw, out _, out _);
                        using (new EditorGUI.DisabledScope(!canRestore || SoDraftBlocked))
                            if (GUILayout.Button("보호 초안 복원")) { RestoreSoProtectedDraft(protectedRead.Raw, envelope.raw); GUIUtility.ExitGUI(); }
                        if (GUILayout.Button("보호본만 폐기")) { DiscardSoDraftRaw(SoRecoveryKey, protectedRead.Raw, readable ? "보호본 · " + envelope.storedUtc : "손상된 보호 봉투"); GUIUtility.ExitGUI(); }
                    }
                }
                if (SoDraftBlocked)
                {
                    var active = CombatStageSoDraftStore.ReadRaw(SoDraftKey);
                    if (active.Status == CombatStageSoDraftStatus.Success)
                    {
                        using (new EditorGUILayout.HorizontalScope())
                        {
                            if (GUILayout.Button("활성 원문 내보내기")) ExportSoDraftRaw(active.Raw, "활성 초안");
                            if (GUILayout.Button("활성 원문만 폐기")) { DiscardSoDraftRaw(SoDraftKey, active.Raw, "충돌 중인 활성 원문(현재 메모리 작업본은 유지)"); GUIUtility.ExitGUI(); }
                        }
                    }
                    if (soDraftRollbackPending && !soDraftRollbackMissing && GUILayout.Button("복구 실패 전 원문 내보내기"))
                        ExportSoDraftRaw(soDraftRollbackRaw, "복구 실패 전 원문");
                    if (soDocument.IsOpen && GUILayout.Button("현재 메모리 작업본 사본 내보내기"))
                        ExportSoDraftRaw(JsonUtility.ToJson(soDocument), "현재 메모리 사본");
                }
                using (new EditorGUI.DisabledScope(SoDraftBlocked || string.IsNullOrEmpty(reopenRoot)))
                    if (GUILayout.Button("보호본을 유지하고 에셋에서 다시 열기")) { ReloadSoFromAssets(reopenRoot); GUIUtility.ExitGUI(); }
            }
        }

        private void ExportSoDraftRaw(string raw, string label)
        {
            string path = EditorUtility.SaveFilePanel(label + " 내보내기", "", "combat-stage-draft.json", "json");
            if (string.IsNullOrEmpty(path)) return;
            var result = CombatStageSoDraftStore.ExportRaw(path, raw);
            SetSoNotice(result.Succeeded ? label + "을 내보냈습니다. 보관 원문은 그대로 남아 있습니다."
                : "원문 내보내기 실패: " + result.Message, result.Succeeded ? MessageType.Info : MessageType.Error);
        }

        private void DiscardSoDraftRaw(string key, string expectedRaw, string label)
        {
            if (!EditorUtility.DisplayDialog("초안 원문 폐기", label + "만 삭제합니다. 내보내지 않은 원문은 되돌릴 수 없습니다. 계속할까요?", "이 원문 폐기", "취소")) return;
            var result = CombatStageSoDraftStore.ClearIfMatches(key, expectedRaw);
            if (!result.Succeeded) { _soDraftError = result.Message; return; }
            if (key == SoDraftKey) { _soDraftExpectedRaw = null; soDraftClearPending = false; }
            RememberSoDraftStorageFailure();
            // 폐기 승인은 보류 중인 이전 원문을 덮거나 버리는 승인이 아닙니다.
            _soDraftError = null;
            PrepareSoDraftProtection();
            SetSoNotice(label + "을 폐기했습니다. 다른 원문과 메모리 작업본은 유지했습니다.");
        }

        private void RetrySoDraftStorage()
        {
            var active = CombatStageSoDraftStore.ReadRaw(SoDraftKey);
            if (!active.Succeeded) { _soDraftError = active.Message; return; }
            if (soDraftRollbackPending)
            {
                string previous = soDraftRollbackMissing ? null : soDraftRollbackRaw;
                if (active.Raw != previous && active.Raw != soDraftAttemptedRaw && active.Raw != null)
                { _soDraftError = "다른 활성 원문이 있어 이전 원문을 덮어 복구하지 않았습니다. 대상별 내보내기·폐기를 먼저 해 주세요."; return; }
                var result = CombatStageSoDraftStore.ReplaceVerified(SoDraftKey, active.Raw, previous);
                if (!result.Succeeded) { _soDraftError = result.Message; return; }
                _soDraftExpectedRaw = previous; soDraftRollbackPending = false;
                soDraftRollbackRaw = null; soDraftAttemptedRaw = null;
                RememberSoDraftStorageFailure();
            }
            if (soDraftClearPending)
            {
                var result = CombatStageSoDraftStore.ClearIfMatches(SoDraftKey, soDraftClearRaw);
                if (!result.Succeeded) { _soDraftError = result.Message; return; }
                _soDraftExpectedRaw = null; soDraftClearPending = false; soDraftClearRaw = null;
                RememberSoDraftStorageFailure();
            }
            if (!ReconcileSoProtectedDraft()) return;
            _soDraftError = null;
            if (!_soDraftKnown) RestoreSoDraft();
            PrepareSoDraftProtection();
            SaveSoDraft();
        }

        // 보호 원문 이동 중 삭제 후 확인만 실패한 경우를 명시적 재시도에서 수습합니다.
        // 활성 키가 없다는 이유만으로 기준을 버리지 않고 같은 원문이 보호돼 있는지 확인합니다.
        private bool ReconcileSoProtectedDraft()
        {
            if (!_soDraftKnown || _soDraftExpectedRaw == null) return true;
            var active = CombatStageSoDraftStore.ReadRaw(SoDraftKey);
            if (!active.Succeeded) { _soDraftError = active.Message; return false; }
            if (active.Status != CombatStageSoDraftStatus.Empty) return true;
            var protection = CombatStageSoDraftStore.ReadRaw(SoRecoveryKey);
            if (!protection.Succeeded) { _soDraftError = protection.Message; return false; }
            if (!CombatStageSoDraftStore.TryReadProtected(protection.Raw, out var envelope)
                || envelope.raw != _soDraftExpectedRaw)
            {
                _soDraftError = "활성 초안은 없지만 마지막 원문과 같은 보호본을 확인하지 못했습니다. 원문을 보호하기 위해 차단을 유지합니다.";
                return false;
            }
            // 구형 메모리는 재직렬화 사본보다 이미 보관한 활성 원문을 우선합니다.
            if (soDocument.IsOpen && !IsSoDraftEditable(soDocument, out _))
                soProtectedMemoryHash = Hash128.Compute(JsonUtility.ToJson(soDocument)).ToString();
            _soDraftExpectedRaw = null;
            _soValidatedActiveRaw = null;
            return true;
        }

        private void RestoreSoProtectedDraft(string expectedEnvelope, string raw)
        {
            if (!TryReadSoDraft(raw, out var candidate, out string reason)) { SetSoNotice(reason, MessageType.Warning); return; }
            string expectedActive = _soDraftExpectedRaw;
            if (!AllowSoReplace()) return;
            var protection = CombatStageSoDraftStore.ReadRaw(SoRecoveryKey);
            if (!protection.Succeeded || protection.Raw != expectedEnvelope)
            { _soDraftError = "확인 중 보호본이 바뀌었거나 읽기에 실패했습니다. 다시 확인하세요."; return; }
            if (expectedActive != _soDraftExpectedRaw) { _soDraftError = "확인 중 활성 원문이 바뀌었습니다."; return; }
            CancelSoWorkspaceInput();
            if (!ReplaceSoActiveDraft(raw)) return;
            Undo.FlushUndoRecordObjects();
            Undo.IncrementCurrentGroup();
            int group = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName("보호 SO 초안 복원");
            // 창 전체 스냅샷은 이후 복사한 두 클립보드까지 되돌립니다. 작업본 변경분만 즉시 확정합니다.
            Undo.RecordObject(this, "보호 SO 초안 복원");
            soDocument = candidate; soModified = true;
            soDraftRestoreBoundary = Guid.NewGuid().ToString("N");
            _soObservedRestoreBoundary = soDraftRestoreBoundary;
            _soDraftRestoreOwner = soDraftRestoreBoundary;
            Undo.FlushUndoRecordObjects();
            Undo.CollapseUndoOperations(group);
            Undo.IncrementCurrentGroup();
            EditorUtility.SetDirty(this);
            try
            {
                ResetSoDraftDocumentView();
                InvalidateSoCheck(changed: true);
                SetSoNotice("보호 초안을 작업본으로 복원했습니다. 재검사·저장이 필요합니다. 보호 원문은 남겨 두었습니다.");
            }
            catch (Exception exception)
            {
                InvalidateSoCheck(changed: true);
                SetSoNotice("초안은 적용했지만 표시 갱신에 실패했습니다. 2D/설정에서 확인하세요: " + exception.Message, MessageType.Error);
            }
        }
    }
}
