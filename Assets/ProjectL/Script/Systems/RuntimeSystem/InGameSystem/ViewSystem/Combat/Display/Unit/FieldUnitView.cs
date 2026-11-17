using ProjectL.Systems.RuntimeSystem.GlobalSystem.UISystem.Screens.Stage;
using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using TMPro;
using Unity.Profiling;
using UnityEngine;
using System.Collections.Generic;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.RenderSystem.Core.ColorSystem;
using ProjectL.Systems.DataBaseSystem.Data.View.CombatPresentation;
using ProjectL.Systems.RuntimeSystem.ErrorHandling;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Display.Unit
{
    /// <summary>
    /// 필드에 놓인 유닛·구조물 하나의 표시입니다(옛 FieldUnitView 이식, 계획 D.2).
    /// 위치·방향·표시 여부·체력 숫자와 유닛 HUD를 실제 표시값과 다를 때만 쓰고(설계 §6.1), 재생 쪽 표시 대여 다섯(자세·서는 자리·크기·플래시·클립)과
    /// 모델 외형 층·사건 클립 채널을 소유합니다(연출 조립 설계 B13).
    /// </summary>
    /// <remarks>
    /// 프리팹 인스턴스에 런타임으로 붙습니다. 공격·반응·능력 동작은 같은 자세 대여를 사용합니다.
    /// 재생 쪽은 창구(<see cref="CombatUnitPresentationWindow"/>)를 거쳐 표시 계약(<see cref="ICombatUnitPresentationTarget"/>)으로만 이 표시를 씁니다(연출 조립 설계 B13).
    /// </remarks>
    [UnityEngine.Scripting.APIUpdating.MovedFrom(true, sourceNamespace: "ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Display")]
    [DisallowMultipleComponent]
    public sealed class FieldUnitView : MonoBehaviour, ICombatUnitPresentationTarget, ICombatUnitHudTarget, IUnitHUDAnchor, ICombatUnitHighlightTarget
    {
        private TMP_Text _healthLabel;
        private static readonly ProfilerMarker HealthLabelMarker = new("Combat.Units.CreateHealthLabel");
        private static readonly ProfilerMarker ClipMarker = new("Combat.Units.CreateClipChannel");
        private Quaternion _healthLabelRotation;
        private bool _hasShownHealth;
        private int _shownHealth;
        private int _shownMaxHealth;
        private int _actualHealth;
        private int _actualMaxHealth;
        private int? _previewHealth;
        private UnitHUD _unitHud;
        private UnitHUDLayer _hudLayer;
        private Vector3 _hudOffset;
        private bool _showUnitHud;
        // HUD에 마지막으로 쓴 캐릭터 사실입니다(GC 끊김 설계 GH3). 사실 객체는 불변이라 같은 객체면 HUD 내용도 같아 다시 쓰지 않습니다.
        // 비우는 때는 이 목록 한 곳에 적습니다(계획 §3.7 L8). 비우는 길을 더하거나 빼면 이 목록도 고칩니다.
        //  1. HUD를 붙이거나 뗄 때: AttachUnitHud·ReleaseUnitHud.
        //  2. 다른 개체로 묶일 때: Bind.
        //  3. HUD를 숨길 때: SyncHudActive(유닛 꺼짐 OnDisable·HUD 끔·등장 연출 숨김 SetEntryHudHidden·풀 반환이 이 길을 지남). 숨겨진 HUD는 체력 표시 상태를 지워 다시 보일 때 모두 다시 써야 함.
        //  4. HUD 층이 숨겨질 때: IUnitHUDAnchor.OnHudLayerHidden(층과 함께 HUD가 꺼져 3과 같은 까닭, 계획 §3.7 L1).
        //  5. HUD 내용을 정하는 표가 바뀔 수 있을 때: 표시 집합 FieldUnitPresenter의 Configure가 ForgetHudFacts로.
        private CharacterCombatFacts _hudFacts;
        private bool _entryHudHidden;
        // 채널마다 대여 칸 하나입니다. 대여 수명 규칙은 칸이 갖고, 시작 조건·복원 내용은 아래 대여 클래스가 갖습니다(클립 계획 T10a).
        private readonly UnitPresentationLeaseSlot<PresentationUnitPose> _poseSlot = new();
        // 특성 발동 유닛 행동이 옮긴 서는 자리입니다(유닛 행동 설계 AA2). 자세 칸의 아래 층이라 자세 칸과 따로입니다.
        private readonly UnitPresentationLeaseSlot<PresentationUnitStand> _standSlot = new();
        private readonly UnitPresentationLeaseSlot<PresentationUnitScale> _scaleSlot = new();
        // 모델 외형 층(이동 반투명·피격 플래시·디졸브·아웃라인)의 합성 소유자입니다.
        private UnitModelAppearance _appearance;
        // 적 표시와 아군 랙돌 디졸브에 쓰는 셰이더 복제본입니다. 원본 프리팹 재질은 바꾸지 않습니다.
        private CartoonModelMaterialFallback _cartoonFallback;
        private bool _temporaryDeathCartoon;
        // 걸린 상태가 정하는 모습(재구성 중 잔해 등)입니다. 처음 그릴 때 만듭니다(재구성 연출 설계 RE2).
        private UnitStatusAppearance _statusAppearance;
        private IColorSystemService _movementColors;
        private readonly UnitPresentationLeaseSlot<PresentationUnitFlash> _flashSlot = new();
        private readonly UnitPresentationLeaseSlot<PresentationUnitOutline> _outlineSlot = new();
        private readonly List<Renderer> _modelBoundsRenderers = new();
        // 사건 클립 채널입니다(클립 설계 AC9). 이벤트 컴포넌트가 붙은 표시만 가집니다. 컴포넌트 찾기는 인스턴스마다 한 번입니다.
        private UnitClipChannel _clipChannel;
        private CombatAnimationEvents _clipEvents;
        private bool _clipEventsSearched;
        private readonly UnitPresentationLeaseSlot<PresentationUnitClip> _clipSlot = new();
        internal UnitHUD Hud => _unitHud;

        /// <summary>지금 자세 대여입니다. 없으면 null입니다(진단용 읽기, 지금 부르는 곳은 없음).</summary>
        internal ICombatAttackUnitPose CurrentPresentationPose => _poseSlot.Current;

        /// <summary>지금 사건 클립 채널입니다. 없으면 null입니다(진단용 읽기, 지금 부르는 곳은 없음).</summary>
        internal UnitClipChannel ClipChannel => _clipChannel;

        public Vector3 HudWorldPosition => (_poseSlot.Current?.Position ?? transform.position) + _hudOffset;
        public bool IsHudVisible => _showUnitHud && !_entryHudHidden && isActiveAndEnabled;

        /// <summary>진입 때만 HUD를 억제합니다. 원래 HUD 사용 여부는 바꾸지 않습니다.</summary>
        internal void SetEntryHudHidden(bool hidden)
        {
            _entryHudHidden = hidden;
            SyncHudActive();
            if (_healthLabel != null) _healthLabel.gameObject.SetActive(!_showUnitHud && !hidden);
        }

        internal EntityId EntityId { get; private set; }
        internal int BodyTrackingVersion { get; private set; }

        internal EntityKey EntityKey { get; private set; }

        internal TMP_Text HealthLabel => _healthLabel;

        /// <summary>공용 화면 Layer에 생성된 HUD를 이 표시의 수명에 연결합니다.</summary>
        internal void AttachUnitHud(UnitHUD hud, Vector3 offset, UnitHUDLayer layer)
        {
            if (hud == null) throw new ArgumentNullException(nameof(hud));
            if (layer == null) throw new ArgumentNullException(nameof(layer));
            if (_unitHud != null) throw new InvalidOperationException("[FieldUnitView] 기존 HUD를 먼저 해제해야 합니다.");
            layer.Register(hud, this);
            _hudLayer = layer;
            _hudOffset = offset;
            _unitHud = hud;
            _hudFacts = null;
            SyncHudActive();
        }

        /// <summary>HUD에 마지막으로 쓴 사실이 이 객체인지입니다(GH3). 같으면 HUD 내용을 다시 쓰지 않아도 됩니다.</summary>
        internal bool ShowsHudFacts(CharacterCombatFacts facts) => facts != null && ReferenceEquals(_hudFacts, facts);

        /// <summary>HUD에 이 사실을 다 썼음을 기억합니다. 쓰기가 끝난 뒤에만 부릅니다.</summary>
        internal void RememberHudFacts(CharacterCombatFacts facts) => _hudFacts = facts;

        /// <summary>HUD에 쓴 사실 기억을 비웁니다. 다음 그리기가 HUD 내용을 모두 다시 씁니다(표가 바뀔 수 있는 Configure 등).</summary>
        internal void ForgetHudFacts() => _hudFacts = null;

        void IUnitHUDAnchor.OnHudLayerHidden() => _hudFacts = null;

        internal void ShowUnitHud(bool show)
        {
            _showUnitHud = show;
            SyncHudActive();
            if (_healthLabel != null) _healthLabel.gameObject.SetActive(!show && !_entryHudHidden);
        }

        /// <summary>HUD 보임을 지금 표시 상태로 다시 맞춥니다. 준비 대여가 끝날 때 옛 스냅숏 대신 이것을 씁니다(평가 G2-02).</summary>
        internal void ResyncHudVisible() => SyncHudActive();

        private void OnEnable()
        {
            SyncHudActive();
            _clipChannel?.Resume();
        }

        // 채널에 넘긴 꼬리(뒤 단계·Out 섞기·남은 섞기)만 프레임마다 진행합니다(클립 칸 다시 짜기 계획 TP-10, 설계 P10·CS7). 꼬리가 없으면 확인 하나뿐입니다.
        private void Update()
        {
            if (_clipChannel != null && _clipChannel.HasTail)
                _clipChannel.TickTail(Time.deltaTime);
        }

        private void SyncHudActive()
        {
            if (_unitHud == null) return;
            bool active = IsHudVisible;
            if (!active) _hudFacts = null;
            _unitHud.SetOwnerVisible(active);
        }

        /// <summary>다른 부모에 있는 HUD도 유닛 수명 종료 시 함께 해제합니다. 풀 반환 때는 호출하지 않습니다.</summary>
        internal void ReleaseUnitHud()
        {
            UnitHUD hud = _unitHud;
            _unitHud = null;
            _hudFacts = null;
            if (_hudLayer != null) _hudLayer.Unregister(hud);
            _hudLayer = null;
            if (hud == null) return;
            hud.Hide();
            if (Application.isPlaying) Destroy(hud.gameObject);
            else DestroyImmediate(hud.gameObject);
        }

        /// <summary>풀에서 처음 만들 때 한 번 체력 표시를 붙입니다.</summary>
        internal void AttachHealthLabel(TMP_Text template, Vector3 localOffset)
        {
            if (_healthLabel != null || template == null)
                return;

            using var sample = HealthLabelMarker.Auto();
            _healthLabel = Instantiate(template, transform, false);
            _healthLabel.name = "HealthLabel";
            _healthLabel.transform.localPosition = localOffset;
            // 유닛이 돌아도 숫자가 뒤집히지 않도록 템플릿의 월드 방향을 유지한다.
            _healthLabelRotation = template.transform.rotation;
            _healthLabel.transform.rotation = _healthLabelRotation;
            _healthLabel.gameObject.SetActive(true);
        }

        internal void Bind(EntityId entityId, EntityKey entityKey)
        {
            if (EntityId == entityId && EntityKey == entityKey)
                return;

            ReleasePresentation();
            ResetStatusAppearance();
            _hasShownHealth = false;
            _hudFacts = null;
            BodyTrackingVersion++;
            EntityId = entityId;
            EntityKey = entityKey;
            gameObject.name = $"FieldUnit_{entityKey}_{entityId.Value}";
        }

        /// <summary>현재 값을 적용합니다. 실제로 바뀐 속성이 있으면 true입니다.</summary>
        internal bool Apply(Vector3 worldPosition, Quaternion worldRotation, int currentHealth, int maxHealth, bool visible)
        {
            bool changed = _previewHealth.HasValue;
            _actualHealth = currentHealth;
            _actualMaxHealth = maxHealth;
            changed |= SetAttackHealthPreview(null);
            // 연출·풀 재사용이 바꾼 값도 되돌리도록 저장값이 아니라 실제 표시값과 비교한다.
            // 자세·서는 자리 대여가 있으면 위치·회전은 그 대여가 쓴다(서는 자리는 유닛 행동 설계 AA2).
            changed |= ApplyStandingPosition(worldPosition);
            if (!HasLeasedPoseOrStand) changed |= ApplyRotation(worldRotation);

            if (gameObject.activeSelf != visible)
            {
                gameObject.SetActive(visible);
                changed = true;
            }

            changed |= RefreshHealthLabel();

            return changed;
        }

        private bool HasLeasedPoseOrStand => _poseSlot.Current != null || _standSlot.Current != null;

        internal bool ApplyStandingPosition(Vector3 position)
        {
            if (HasLeasedPoseOrStand || transform.position == position) return false;
            transform.position = position;
            return true;
        }

        /// <summary>표시용 예상 체력만 적용합니다. 실제 체력과 AP는 변경하지 않습니다.</summary>
        internal bool SetAttackHealthPreview(int? projectedHealth)
        {
            _previewHealth = projectedHealth;
            if (_unitHud != null)
                _unitHud.SetHealthPreview(_unitHud.gameObject.activeSelf ? projectedHealth : null);
            return RefreshHealthLabel();
        }

        private bool RefreshHealthLabel()
        {
            int health = _previewHealth ?? _actualHealth;
            if (_healthLabel != null && (!_hasShownHealth || _shownHealth != health || _shownMaxHealth != _actualMaxHealth))
            {
                _healthLabel.SetText("{0}/{1}", health, _actualMaxHealth);
                _hasShownHealth = true;
                _shownHealth = health;
                _shownMaxHealth = _actualMaxHealth;
                return true;
            }
            return false;
        }

        /// <summary>위치·체력·활성 상태를 건드리지 않고 모델과 체력 숫자 방향만 갱신합니다.</summary>
        internal bool ApplyRotation(Quaternion worldRotation)
        {
            bool changed = false;
            if (transform.rotation != worldRotation)
            {
                transform.rotation = worldRotation;
                changed = true;
            }
            if (_healthLabel != null && _healthLabel.transform.rotation != _healthLabelRotation)
            {
                _healthLabel.transform.rotation = _healthLabelRotation;
                changed = true;
            }
            return changed;
        }

        internal void ResetForPool()
        {
            // 대여의 복원은 같은 EntityId일 때만 돌므로, EntityId를 비우기 전에 정리한다.
            ReleasePresentation();
            ResetStatusAppearance();
            if (_unitHud != null) _unitHud.Clear();
            // 진입 연출의 HUD 숨김은 진입 소유자만 풀지만, 풀로 돌아가는 표시에는 남기지 않습니다(평가 G2-01).
            _entryHudHidden = false;
            ShowUnitHud(false);
            EntityId = EntityId.Empty;
            _hasShownHealth = false;
            _actualHealth = _actualMaxHealth = 0;
            if (gameObject.activeSelf)
                gameObject.SetActive(false);
        }

        /// <summary>이미 빌린 표시의 공격·반응 자세를 배타적으로 소유합니다. 다음 대여를 이전 공격이 복원할 수 없습니다.</summary>
        internal ICombatAttackUnitPose BeginAttackPose(Action cancelled = null)
            => BeginPose(cancelled);

        internal ICombatUnitPose BeginReactionPose(Action cancelled = null)
            => BeginPose(cancelled);

        /// <summary>
        /// 크기 배율을 배타적으로 소유합니다(포효 커지기, 설계 D6b). 자세 대여와 따로라 커진 채로 공격 자세를 재생할 수 있습니다.
        /// 다음 대여를 이전 RestoreScale이 수정할 수 없습니다.
        /// </summary>
        internal ICombatUnitScale BeginPresentationScale(Action cancelled = null)
        {
            if (_scaleSlot.Current != null)
                throw new InvalidOperationException("[FieldUnitView] 이전 표시 크기가 아직 남아 있습니다.");
            if (EntityId.IsEmpty || !gameObject.activeInHierarchy)
                throw new InvalidOperationException("[FieldUnitView] 활성 대여에서만 표시 크기를 시작할 수 있습니다.");
            var lease = new PresentationUnitScale(this);
            _scaleSlot.Hold(lease, cancelled);
            return lease;
        }

        private void ResetPresentationScale() => _scaleSlot.Reset(lease => lease.RestoreScale());

        private PresentationUnitPose BeginPose(Action cancelled)
        {
            if (_poseSlot.Current != null)
                throw new InvalidOperationException("[FieldUnitView] 이전 표시 자세가 아직 남아 있습니다.");
            if (EntityId.IsEmpty || !gameObject.activeInHierarchy)
                throw new InvalidOperationException("[FieldUnitView] 활성 대여에서만 표시 자세를 시작할 수 있습니다.");
            var lease = new PresentationUnitPose(this);
            _poseSlot.Hold(lease, cancelled);
            return lease;
        }

        private void ResetPresentationPose() => _poseSlot.Reset(lease => lease.RestorePose());

        /// <summary>
        /// 서는 자리 대여를 시작합니다(유닛 행동 설계 AA2). 빌릴 때 위치·방향이 되돌릴 자리입니다.
        /// <paramref name="keepsWhereItIs"/>가 true인 동안(사망 모델 유지)은 되돌리기가 자리를 쓰지 않습니다(설계 P8).
        /// </summary>
        private PresentationUnitStand BeginStand(Action cancelled, Func<bool> keepsWhereItIs)
        {
            if (keepsWhereItIs == null) throw new ArgumentNullException(nameof(keepsWhereItIs));
            if (_standSlot.Current != null)
                throw new InvalidOperationException("[FieldUnitView] 이전 서는 자리가 아직 남아 있습니다.");
            if (EntityId.IsEmpty || !gameObject.activeInHierarchy)
                throw new InvalidOperationException("[FieldUnitView] 활성 대여에서만 서는 자리를 시작할 수 있습니다.");
            var lease = new PresentationUnitStand(this, keepsWhereItIs);
            _standSlot.Hold(lease, cancelled);
            return lease;
        }

        // 서는 자리는 없어져도 되는 칸입니다(묶음 끝 검토 H1). 다른 칸처럼 취소 콜백을 부르면, 대신 맞은 수호자가 죽어 표시가 풀로 돌아갈 때
        // 그 서는 자리를 빌린 공격 실행까지 "표시가 사라졌다"로 멈춥니다. 그래서 취소 없이 빌릴 때 자리로 되돌리기만 하고(사망 유지면 그 자리),
        // 빌린 실행은 이어 갑니다. 그 실행의 서는 자리 조각은 대여를 쓸 수 없으면(IsAvailable 거짓) 바로 끝나고, 실행 끝의 되돌리기는 지난 대여라 아무 일도 하지 않습니다.
        private void DropStand() => _standSlot.Current?.RestoreStand();

        /// <summary>
        /// 재생 표시 대여 다섯과 아웃라인 대여·이동 투명도·사망 디졸브·예상 체력을 모두 정리합니다. 다시 묶기·끄기·풀 반환이 이 목록 하나를 씁니다.
        /// </summary>
        /// <remarks>
        /// 복원 대상은 칸마다 다릅니다(자세·서는 자리는 위치·회전, 크기는 localScale, 플래시·투명도는 외형의 두 층, 클립은 Animator 아래, 예상 체력은 HUD).
        /// 자세와 서는 자리는 같은 위치·회전을 쓰므로 위 층(자세)을 먼저 되돌리고 아래 층(서는 자리)을 뒤에 되돌려, 끝 자리가 서는 자리를 빌리기 전 자리가 되게 합니다.
        /// 대여 칸의 Reset은 취소 콜백을 바로 부르고, 그 콜백이 같은 실행이 빌린 다른 칸을 되돌립니다.
        /// 그래서 대여 다섯의 순서(자세 → 서는 자리 → 크기 → 플래시 → 클립)는 지킵니다. 특히 자세 취소가 크기 취소보다 먼저여야 합니다. 이 순서를 잡아 주는 시험은 없으니 바꿀 때 직접 확인합니다.
        /// 서는 자리만은 취소 콜백 없이 내려놓습니다(<see cref="DropStand"/>, 묶음 끝 검토 H1). 콜백이 없는 아웃라인·이동 투명도·사망 디졸브는 맨 앞에서 지웁니다.
        /// </remarks>
        private void ReleasePresentation() =>
            // 한 칸(취소 콜백)이 던져도 나머지 칸을 끝까지 정리한 뒤 모아 던집니다(OnDestroy와 같은 보장, 평가 C9-07).
            // 순서는 위 그대로입니다. 대리자 배열은 표시마다 한 번만 만듭니다(GC 끊김 설계 GH4).
            CleanupErrors.RunAll("[FieldUnitView] 표시 대여 정리에 실패했습니다.",
                _releaseSteps ??= new Action[]
                {
                    ResetOutline, ClearMovementOpacity, ClearDeathDissolve, ResetPresentationPose, DropStand,
                    ResetPresentationScale, ResetHitFlash, ResetClip, ClearHealthPreview
                });

        private Action[] _releaseSteps;

        private void ClearHealthPreview() => SetAttackHealthPreview(null);

        private void OnDisable()
        {
            SyncHudActive();
            try
            {
                ReleasePresentation();
            }
            finally
            {
                _clipChannel?.Pause();
            }
        }

        /// <summary>모델 외형 층(이동 반투명·피격 플래시)을 준비합니다. 색 서비스가 없으면 준비하지 않습니다.</summary>
        internal void PrepareMovementAppearance(IColorSystemService colors)
        {
            _movementColors = colors;
            if (_appearance != null || colors == null) return;
            var models = new List<Renderer>();
            foreach (Renderer renderer in GetComponentsInChildren<Renderer>(true))
                if (UnitModelParts.Is(renderer))
                    models.Add(renderer);
            _appearance = new UnitModelAppearance(models.ToArray(), colors);
        }

        /// <summary>이전 모델 재질만 전투용 Cartoon으로 바꿉니다. 임시 사망 재질은 반환까지 유지합니다.</summary>
        internal bool SetCartoonMaterialFallback(bool enabled)
        {
            if (!enabled && _temporaryDeathCartoon) return false;
            if (enabled)
            {
                _cartoonFallback ??= new CartoonModelMaterialFallback(gameObject);
                if (!_cartoonFallback.NeedsConversion) return false;
            }
            if ((_cartoonFallback?.IsEnabled ?? false) == enabled) return false;

            // 외형 층은 캡처한 원래 재질로 복원하므로, 셰이더 교체 전에 반드시 끝냅니다.
            ReleaseMovementAppearance();
            try
            {
                _cartoonFallback.SetEnabled(enabled);
            }
            finally
            {
                PrepareMovementAppearance(_movementColors);
            }
            return true;
        }

        internal void SetMovementOpacity(float opacity)
        {
            if (_appearance == null)
                throw new InvalidOperationException("[FieldUnitView] 이동 모델 표시 준비가 필요합니다.");
            _appearance.SetOpacity(opacity);
        }

        /// <summary>이동 반투명 층만 끝냅니다. 피격 플래시 층이 남으면 그대로 둡니다.</summary>
        internal void ClearMovementOpacity() => _appearance?.ClearOpacity();

        /// <summary>랙돌 사망은 이전 모델 재질에도 디졸브가 적용되도록 필요할 때만 재질을 교체합니다.</summary>
        private void SetDeathDissolve(float amount, bool usesRagdoll)
        {
            if (usesRagdoll && amount > 0f && SetCartoonMaterialFallback(true))
                _temporaryDeathCartoon = true;
            _appearance?.SetDissolve(amount);
        }

        private void ClearDeathDissolve()
        {
            if (_temporaryDeathCartoon)
            {
                _temporaryDeathCartoon = false;
                SetCartoonMaterialFallback(false);
            }
            else
            {
                _appearance?.ClearDissolve();
            }
        }

        /// <summary>모델 외형 층을 쓸 수 있는지입니다. 피격 플래시 대여의 조건입니다.</summary>
        internal bool SupportsHitFlash => _appearance != null;

        /// <summary>피격 플래시 층 대여를 시작합니다(조각·조립 설계 BP9). 표시가 꺼지거나 다시 대여되면 취소되고 <paramref name="cancelled"/>를 부릅니다.</summary>
        internal ICombatUnitFlash BeginHitFlash(Action cancelled = null)
        {
            if (_flashSlot.Current != null)
                throw new InvalidOperationException("[FieldUnitView] 이전 피격 플래시가 아직 남아 있습니다.");
            if (EntityId.IsEmpty || !gameObject.activeInHierarchy)
                throw new InvalidOperationException("[FieldUnitView] 활성 대여에서만 피격 플래시를 시작할 수 있습니다.");
            if (_appearance == null)
                throw new InvalidOperationException("[FieldUnitView] 모델 외형 준비가 필요합니다.");
            var lease = new PresentationUnitFlash(this);
            _flashSlot.Hold(lease, cancelled);
            return lease;
        }

        private void ResetHitFlash() => _flashSlot.Reset(lease => lease.RestoreFlash());

        // 튜토리얼 강조 계약입니다(ICombatUnitHighlightTarget). 파괴된 표시에서도 부를 수 있게 Unity null 판정(this == null)을 먼저 합니다.
        EntityId ICombatUnitHighlightTarget.EntityId => EntityId;

        int ICombatUnitHighlightTarget.BodyTrackingVersion => BodyTrackingVersion;

        bool ICombatUnitHighlightTarget.IsActive => this != null && isActiveAndEnabled;

        Matrix4x4 ICombatUnitHighlightTarget.LocalToWorld => transform.localToWorldMatrix;

        Transform ICombatUnitHighlightTarget.HudTransform => _unitHud != null ? _unitHud.transform : null;

        /// <summary>선택 상태를 바꾸지 않고 현재 모델 외곽선만 빌립니다.</summary>
        ICombatUnitOutline ICombatUnitHighlightTarget.BeginOutline(Color color, float width)
        {
            if (_outlineSlot.Current != null)
                throw new InvalidOperationException("[FieldUnitView] 이전 아웃라인 대여가 아직 남아 있습니다.");
            if (this == null || EntityId.IsEmpty || !isActiveAndEnabled)
                throw new InvalidOperationException("[FieldUnitView] 활성 개체에서만 아웃라인을 시작할 수 있습니다.");
            if (_appearance == null)
                throw new InvalidOperationException("[FieldUnitView] 아웃라인에 필요한 모델 외형 준비가 없습니다.");
            _appearance.SetOutline(color, width);
            var lease = new PresentationUnitOutline(this);
            _outlineSlot.Hold(lease, null);
            return lease;
        }

        private void ResetOutline() => _outlineSlot.Reset(lease => lease.Dispose());

        /// <summary>현재 보이는 실제 모델의 월드 경계입니다. HUD·문자·파티클·경계 제외 표시는 포함하지 않습니다.</summary>
        bool ICombatUnitHighlightTarget.TryGetModelBounds(out Bounds bounds)
        {
            bounds = default;
            if (this == null || !isActiveAndEnabled) return false;
            GetComponentsInChildren(false, _modelBoundsRenderers);
            bool found = false;
            foreach (Renderer renderer in _modelBoundsRenderers)
            {
                if (renderer == null || !renderer.enabled
                    || !(renderer is MeshRenderer || renderer is SkinnedMeshRenderer)
                    || !UnitModelParts.Is(renderer))
                    continue;
                if (found) bounds.Encapsulate(renderer.bounds);
                else bounds = renderer.bounds;
                found = true;
            }
            return found;
        }

        internal void ReleaseMovementAppearance()
        {
            List<Exception> errors = null;
            CleanupErrors.Try(ResetOutline, ref errors);
            UnitModelAppearance appearance = _appearance;
            _appearance = null;
            if (appearance != null)
                CleanupErrors.Try(appearance.Dispose, ref errors);
            CleanupErrors.ThrowIfAny(errors, "[FieldUnitView] 모델 외형/아웃라인 정리에 실패했습니다.");
        }

        /// <summary>
        /// 사건 클립 채널을 준비합니다(클립 설계 AC9). 이벤트 컴포넌트를 찾아 채널을 만들고, 없으면 만들지 않으며 Animator도 건드리지 않습니다
        /// (Unity가 컨트롤러를 평소처럼 재생, 설계 AC4). 규칙 위반의 예외는 <see cref="CombatAnimationEvents.FindIn"/>이 냅니다(평가 G2).
        /// </summary>
        internal void PrepareClipChannel()
        {
            if (_clipChannel != null) return;
            if (!_clipEventsSearched)
            {
                _clipEvents = CombatAnimationEvents.FindIn(gameObject);
                _clipEventsSearched = true;
            }
            if (_clipEvents == null) return;
            using var sample = ClipMarker.Auto();
            _clipChannel = new UnitClipChannel(_clipEvents);
            if (!isActiveAndEnabled) _clipChannel.Pause();
        }

        /// <summary>사건 클립 채널을 쓸 수 있는지입니다. 사건 클립 대여의 조건입니다.</summary>
        internal bool SupportsClip => _clipChannel != null;
        bool ICombatUnitPresentationTarget.HasClip => _clipSlot.Current != null;

        /// <summary>사건 클립 대여를 시작합니다(클립 설계 AC3). 표시가 꺼지거나 다시 대여되면 취소되고 <paramref name="cancelled"/>를 부릅니다.</summary>
        internal ICombatUnitClip BeginClip(Action cancelled = null)
        {
            if (_clipSlot.Current != null)
                throw new InvalidOperationException("[FieldUnitView] 이전 사건 클립이 아직 남아 있습니다.");
            if (EntityId.IsEmpty || !gameObject.activeInHierarchy)
                throw new InvalidOperationException("[FieldUnitView] 활성 대여에서만 사건 클립을 시작할 수 있습니다.");
            if (_clipChannel == null)
                throw new InvalidOperationException("[FieldUnitView] 클립 채널 준비가 필요합니다.");
            var lease = new PresentationUnitClip(this);
            _clipSlot.Hold(lease, cancelled);
            return lease;
        }

        // 표시 대여 정리(취소·풀 반환)는 뒤 단계 꼬리도 끊습니다(클립 칸 다시 짜기 설계 P8). 대여가 넘긴 꼬리는 대여 반환만으로는 끊기지 않습니다.
        private void ResetClip()
        {
            try { _clipSlot.Reset(lease => lease.RestoreClip()); }
            finally { _clipChannel?.CutTail(); }
        }

        /// <summary>뒤 단계 꼬리가 있으면 끊습니다(연출 취소, 설계 P8). 표시 대여는 건드리지 않습니다.</summary>
        internal void CutClipTail() => _clipChannel?.CutTail();

        /// <summary>남은 사건 클립 대여를 끝내고 채널을 해제합니다(그래프 파괴, 넘겨받은 컨트롤러 반환). 표시 집합 해제와 파괴 때 부릅니다.</summary>
        internal void ReleaseClipChannel()
        {
            try { ResetClip(); }
            finally
            {
                UnitClipChannel channel = _clipChannel;
                _clipChannel = null;
                channel?.Dispose();
            }
        }

        private void OnDestroy()
        {
            try { ReleaseClipChannel(); }
            finally
            {
                try { ReleaseMovementAppearance(); }
                finally
                {
                    try { _cartoonFallback?.Dispose(); }
                    finally { ReleaseUnitHud(); }
                }
            }
        }

        /// <summary>
        /// 이 사실에서 걸린 상태가 정하는 모습을 맞춥니다(재구성 연출 설계 RE2·계획 TP-4·TP-5). 같은 사실 객체면 아무것도 하지 않습니다.
        /// 바뀌면 외형 층(투명도·플래시·윤곽)을 놓고 모델을 바꾼 뒤 다시 잡습니다(<see cref="SetCartoonMaterialFallback"/>과 같은 순서) — 층이 잡은 렌더러의
        /// 원래 재질로 되돌리므로 바꾸기 전에 놓아야 하고, 다시 잡아야 잔해도 피격 플래시를 받습니다.
        /// </summary>
        /// <param name="resolve">사실 → 보일 프리팹(원래 모습이면 null)입니다. 표시 집합이 한 번 만든 대리자를 넘깁니다.</param>
        internal void SyncStatusAppearance(CharacterCombatFacts facts, Func<CharacterCombatFacts, GameObject> resolve)
        {
            _statusAppearance ??= new UnitStatusAppearance(transform);
            if (_statusAppearance.TryResolveChange(facts, resolve, out GameObject next))
                ChangeStatusAppearance(next);
        }

        /// <summary>모습 판단 기억을 비웁니다. 다음 그리기가 상태를 다시 읽습니다(표가 바뀔 수 있는 Configure).</summary>
        internal void ForgetStatusAppearance() => _statusAppearance?.Forget();

        // 원래 모습으로 되돌리고 기억을 비웁니다(다른 개체로 묶일 때·풀 반환).
        private void ResetStatusAppearance()
        {
            if (_statusAppearance == null) return;
            if (_statusAppearance.IsReplaced) ChangeStatusAppearance(null);
            _statusAppearance.Forget();
        }

        private void ChangeStatusAppearance(GameObject prefab)
        {
            bool layered = _appearance != null;
            if (layered) ReleaseMovementAppearance();
            try
            {
                _statusAppearance.Show(prefab);
            }
            finally
            {
                if (layered) PrepareMovementAppearance(_movementColors);
            }
        }

        EntityId ICombatUnitPresentationTarget.EntityId => EntityId;

        EntityKey ICombatUnitPresentationTarget.EntityKey => EntityKey;

        bool ICombatUnitPresentationTarget.IsActive => isActiveAndEnabled;

        bool ICombatUnitPresentationTarget.HasPose => _poseSlot.Current != null;

        Quaternion ICombatUnitPresentationTarget.Rotation => transform.rotation;

        ICombatAttackUnitPose ICombatUnitPresentationTarget.BeginPose(Action cancelled) => BeginPose(cancelled);

        ICombatUnitScale ICombatUnitPresentationTarget.BeginScale(Action cancelled) => BeginPresentationScale(cancelled);

        void ICombatUnitPresentationTarget.SetPlacementPosition(Vector3 position) => transform.position = position;

        void ICombatUnitPresentationTarget.ApplyFacing(Quaternion rotation) => ApplyRotation(rotation);

        void ICombatUnitPresentationTarget.SetMovementOpacity(float opacity) => SetMovementOpacity(opacity);

        void ICombatUnitPresentationTarget.ClearMovementOpacity() => ClearMovementOpacity();

        bool ICombatUnitPresentationTarget.SupportsHitFlash => SupportsHitFlash;

        ICombatUnitFlash ICombatUnitPresentationTarget.BeginHitFlash(Action cancelled) => BeginHitFlash(cancelled);

        bool ICombatUnitPresentationTarget.SupportsClip => SupportsClip;

        ICombatUnitClip ICombatUnitPresentationTarget.BeginClip(Action cancelled) => BeginClip(cancelled);
        void ICombatUnitPresentationTarget.CutClipTail() => CutClipTail();

        bool ICombatUnitPresentationTarget.HasStand => _standSlot.Current != null;

        ICombatUnitStand ICombatUnitPresentationTarget.BeginStand(Action cancelled, Func<bool> keepsWhereItIs) =>
            BeginStand(cancelled, keepsWhereItIs);

        bool ICombatUnitPresentationTarget.FoldStand() => _standSlot.Current?.Fold() ?? false;

        bool ICombatUnitPresentationTarget.TryGetStand(out Vector3 position, out Quaternion rotation)
        {
            PresentationUnitStand stand = _standSlot.Current;
            position = stand?.Position ?? default;
            rotation = stand?.Rotation ?? Quaternion.identity;
            return stand != null;
        }

        CatusCannonWeaponView ICombatUnitPresentationTarget.MortarWeapon => GetComponent<CatusCannonWeaponView>();

        bool ICombatUnitHudTarget.IsActive => isActiveAndEnabled;

        void ICombatUnitHudTarget.ShowDamage(int damage)
        {
            if (_unitHud != null) _unitHud.ShowDamage(damage);
        }

        void ICombatUnitHudTarget.SetHealthPreview(int? health) => SetAttackHealthPreview(health);

        /// <summary>자세 대여 하나입니다. 수명 규칙(늦은 복원 무시·취소 콜백)은 대여 칸이 갖습니다(클립 계획 T10a).</summary>
        private sealed class PresentationUnitPose : ICombatAttackUnitPose, ICombatUnitRagdollPose
        {
            private readonly FieldUnitView _view;
            private readonly EntityId _entityId;
            private readonly Vector3 _healthPosition;
            private CombatUnitRagdoll _ragdoll;

            internal PresentationUnitPose(FieldUnitView view)
            {
                _view = view;
                _entityId = view.EntityId;
                Position = view.transform.position;
                Rotation = view.transform.rotation;
                _healthPosition = view._healthLabel != null ? view._healthLabel.transform.position : Vector3.zero;
            }

            private bool IsCurrent => _view != null && _view.EntityId == _entityId && _view._poseSlot.IsCurrent(this);

            public bool IsAvailable => IsCurrent && _view.gameObject.activeInHierarchy;
            public Vector3 Position { get; }
            public Quaternion Rotation { get; }

            public void ApplyPose(Vector3 position, Quaternion rotation)
            {
                if (!IsAvailable)
                    throw new InvalidOperationException("[FieldUnitView] 표시 자세 대여가 취소·반환됐습니다.");
                _view.transform.position = position;
                _view.ApplyRotation(rotation);
                if (_view._healthLabel != null)
                    _view._healthLabel.transform.position = _healthPosition;
            }

            public void SetDeathDissolve(float amount)
            {
                if (!IsAvailable)
                    throw new InvalidOperationException("[FieldUnitView] 표시 자세 대여가 취소·반환됐습니다.");
                _view.SetDeathDissolve(amount, _ragdoll != null);
            }

            public void ClearDeathDissolve()
            {
                if (!IsCurrent) return;
                _view.ClearDeathDissolve();
            }

            public bool TryBeginDeathRagdoll(Vector3 push, Vector3 groundUp, float groundWidth)
            {
                if (!IsAvailable || _ragdoll != null)
                    throw new InvalidOperationException("[FieldUnitView] 활성 자세 대여에서 랙돌을 한 번만 시작할 수 있습니다.");
                // 걸린 상태가 정한 다른 모델(재구성 중 잔해 등)을 보이는 동안은 래그돌을 쓰지 않습니다. 원래 모델이 꺼져 있어 래그돌이 보이지 않고,
                // false면 사망 반응이 몸 전체를 기울여 잔해가 쓰러집니다(재구성 연출 설계 Q-RE2 A, 묶음 끝 검토 M1).
                if (_view._statusAppearance?.IsReplaced == true || !_view.TryGetComponent(out CombatUnitRagdoll ragdoll))
                    return false;

                // 래그돌이 몸을 맡기 전에 뒤 단계 꼬리를 끊습니다(클립 칸 다시 짜기 설계 P8).
                _view._clipChannel?.CutTail();
                _view._clipChannel?.Pause();
                try
                {
                    ragdoll.Begin(push, groundUp, groundWidth);
                    _ragdoll = ragdoll;
                    return true;
                }
                catch
                {
                    if (_view.isActiveAndEnabled) _view._clipChannel?.Resume();
                    throw;
                }
            }

            public void RestorePose()
            {
                if (!IsCurrent || !_view._poseSlot.Release(this)) return;
                CombatUnitRagdoll ragdoll = _ragdoll;
                _ragdoll = null;
                try
                {
                    if (ragdoll != null) ragdoll.End();
                }
                finally
                {
                    _view.transform.position = Position;
                    _view.ApplyRotation(Rotation);
                    if (_view._healthLabel != null)
                        _view._healthLabel.transform.position = _healthPosition;
                    if (ragdoll != null && _view.isActiveAndEnabled) _view._clipChannel?.Resume();
                }
            }
        }

        /// <summary>
        /// 서는 자리 대여 하나입니다(유닛 행동 설계 AA2). 수명 규칙은 대여 칸이 갖고, 여기서는 자리 쓰기·되돌리기·접기만 합니다.
        /// 체력 숫자는 자식이라 유닛을 따라가고, 방향은 숫자가 뒤집히지 않게 <see cref="ApplyRotation"/>으로 씁니다.
        /// 표시가 꺼지거나 풀로 돌아가면 취소 없이 내려놓습니다(<see cref="DropStand"/>, 묶음 끝 검토 H1).
        /// </summary>
        private sealed class PresentationUnitStand : ICombatUnitStand
        {
            private readonly FieldUnitView _view;
            private readonly EntityId _entityId;
            private readonly Func<bool> _keepsWhereItIs;

            internal PresentationUnitStand(FieldUnitView view, Func<bool> keepsWhereItIs)
            {
                _view = view;
                _entityId = view.EntityId;
                _keepsWhereItIs = keepsWhereItIs;
                HomePosition = Position = view.transform.position;
                HomeRotation = Rotation = view.transform.rotation;
            }

            private bool IsCurrent => _view != null && _view.EntityId == _entityId && _view._standSlot.IsCurrent(this);

            public bool IsAvailable => IsCurrent && _view.gameObject.activeInHierarchy;
            public bool CanMove => IsAvailable && _view._poseSlot.Current == null;
            public Vector3 HomePosition { get; }
            public Quaternion HomeRotation { get; }
            public Vector3 Position { get; private set; }
            public Quaternion Rotation { get; private set; }

            public void ApplyStand(Vector3 position, Quaternion rotation)
            {
                if (!IsAvailable)
                    throw new InvalidOperationException("[FieldUnitView] 서는 자리 대여가 취소·반환·접혔습니다.");
                if (_view._poseSlot.Current != null)
                    throw new InvalidOperationException("[FieldUnitView] 자세 대여가 있는 동안에는 서는 자리를 옮길 수 없습니다(자세가 되돌릴 자리와 어긋남).");
                Position = position;
                Rotation = rotation;
                _view.transform.position = position;
                _view.ApplyRotation(rotation);
            }

            public void RestoreStand()
            {
                if (!IsCurrent || !_view._standSlot.Release(this)) return;
                // 사망 모델 유지 중이면 그 자리에 둡니다(죽은 유닛은 돌아오지 않음, 설계 P8). 결과 끝 그리기가 정리합니다.
                if (_keepsWhereItIs()) return;
                _view.transform.position = HomePosition;
                _view.ApplyRotation(HomeRotation);
            }

            // 창구의 걸음 배치 시작이 부릅니다(설계 AA8 안전장치). 자리를 쓰지 않고, 취소 콜백도 부르지 않습니다(빌린 실행은 이어 갑니다).
            internal bool Fold() => IsCurrent && _view._standSlot.Release(this);
        }

        /// <summary>늦은 반환이 새 개체·새 모델·새 대여를 지우지 않는 강조 대여입니다.</summary>
        private sealed class PresentationUnitOutline : ICombatUnitOutline
        {
            private readonly FieldUnitView _view;
            private readonly EntityId _entityId;
            private readonly int _bodyTrackingVersion;
            private readonly UnitModelAppearance _appearance;

            internal PresentationUnitOutline(FieldUnitView view)
            {
                _view = view;
                _entityId = view.EntityId;
                _bodyTrackingVersion = view.BodyTrackingVersion;
                _appearance = view._appearance;
            }

            private bool IsCurrent => _view != null && _view.EntityId == _entityId
                && _view.BodyTrackingVersion == _bodyTrackingVersion
                && ReferenceEquals(_view._appearance, _appearance) && _view._outlineSlot.IsCurrent(this);

            public bool IsAvailable => IsCurrent && _view.isActiveAndEnabled && _view._appearance != null;

            public void Dispose()
            {
                if (!IsCurrent || !_view._outlineSlot.Release(this)) return;
                _view._appearance?.ClearOutline();
            }
        }

        /// <summary>피격 플래시 층 대여 하나입니다(조각·조립 설계 BP9). 수명 규칙은 대여 칸이 갖습니다(클립 계획 T10a).</summary>
        private sealed class PresentationUnitFlash : ICombatUnitFlash
        {
            private readonly FieldUnitView _view;
            private readonly EntityId _entityId;

            internal PresentationUnitFlash(FieldUnitView view)
            {
                _view = view;
                _entityId = view.EntityId;
            }

            private bool IsCurrent => _view != null && _view.EntityId == _entityId && _view._flashSlot.IsCurrent(this);

            public bool IsAvailable => IsCurrent && _view.gameObject.activeInHierarchy && _view._appearance != null;

            public void SetFlash(CombatFlashLook look, float strength)
            {
                if (!IsAvailable)
                    throw new InvalidOperationException("[FieldUnitView] 피격 플래시 대여가 취소·반환됐습니다.");
                _view._appearance.SetFlash(look, strength);
            }

            public void RestoreFlash()
            {
                if (!IsCurrent || !_view._flashSlot.Release(this)) return;
                _view._appearance?.ClearFlash();
            }
        }

        /// <summary>사건 클립 대여 하나입니다(클립 설계 AC3). 수명 규칙은 대여 칸이 갖습니다(클립 계획 T10a).</summary>
        private sealed class PresentationUnitClip : ICombatUnitClip, ICombatWeaponPitchClip
        {
            private readonly FieldUnitView _view;
            private readonly EntityId _entityId;
            // 이 대여가 첫 단계를 꽂았는지입니다. 다시 꽂으면 단계 바뀜(Between)입니다(클립 칸 다시 짜기 설계 CS7).
            private bool _played;
            // 본 단계 끝에 나머지를 채널에 넘겼는지입니다(설계 CS6). 넘긴 뒤 대여 반환은 돌고 있는 꼬리를 끊지 않습니다.
            private bool _tailed;

            internal PresentationUnitClip(FieldUnitView view)
            {
                _view = view;
                _entityId = view.EntityId;
            }

            private bool IsCurrent => _view != null && _view.EntityId == _entityId && _view._clipSlot.IsCurrent(this);

            public bool IsAvailable => IsCurrent && _view.gameObject.activeInHierarchy && _view._clipChannel != null;

            public void Play(CombatClipData clip, CombatClipBlend blend)
            {
                RequireAvailable();
                if (_tailed)
                    throw new InvalidOperationException("[FieldUnitView] 나머지를 채널에 넘긴 대여는 단계를 꽂지 않습니다.");
                if (_played)
                    _view._clipChannel.CrossFade(clip, blend);
                else
                    _view._clipChannel.Begin(clip, blend);
                _played = true;
            }

            public bool IsBlending => IsAvailable && _view._clipChannel.IsBlending;

            public void AdvanceBlend(float deltaTime)
            {
                RequireAvailable();
                if (!_tailed)
                    _view._clipChannel.AdvanceBlend(deltaTime);
            }

            public void AimWeapon(CombatWeaponAim aim)
            {
                RequireAvailable();
                _view._clipChannel.AimWeapon(aim);
            }

            public void SetTime(float time)
            {
                RequireAvailable();
                if (_tailed)
                    throw new InvalidOperationException("[FieldUnitView] 뒤 단계를 채널에 넘긴 대여는 시각을 정하지 않습니다.");
                _view._clipChannel.SetTime(time);
            }

            public void PlayTail(CombatClipData tail, float speed, CombatClipBlend between, CombatClipBlend fadeOut)
            {
                RequireAvailable();
                if (_tailed)
                    throw new InvalidOperationException("[FieldUnitView] 이 대여는 이미 나머지를 채널에 넘겼습니다.");
                _view._clipChannel.HandOff(tail, speed, between, fadeOut);
                _tailed = true;
            }

            public void RestoreClip()
            {
                if (!IsCurrent || !_view._clipSlot.Release(this)) return;
                // 넘긴 뒤 단계·Out 섞기는 채널이 혼자 끝까지 돌립니다(설계 CS6·CS7). 넘길 때 남은 In·Between 섞기만 있거나 아무것도 안 넘겼으면 지금처럼 바로 돌아갑니다.
                // 대여 정리(취소·풀 반환)는 FieldUnitView.ResetClip이 따로 끊습니다.
                UnitClipChannel channel = _view._clipChannel;
                if (channel != null && !(_tailed && channel.HoldsTail))
                    channel.End();
            }

            public bool TryGetEventPoint(int point, out Vector3 position, out Quaternion rotation)
            {
                RequireAvailable();
                return _view._clipChannel.TryGetEventPoint(point, out position, out rotation);
            }

            private void RequireAvailable()
            {
                if (!IsAvailable)
                    throw new InvalidOperationException("[FieldUnitView] 사건 클립 대여가 취소·반환됐습니다.");
            }
        }

        /// <summary>크기 배율 대여 하나입니다(설계 D6b). 수명 규칙은 대여 칸이 갖습니다(클립 계획 T10a).</summary>
        private sealed class PresentationUnitScale : ICombatUnitScale
        {
            private readonly FieldUnitView _view;
            private readonly EntityId _entityId;
            private readonly Vector3 _localScale;

            internal PresentationUnitScale(FieldUnitView view)
            {
                _view = view;
                _entityId = view.EntityId;
                _localScale = view.transform.localScale;
            }

            private bool IsCurrent => _view != null && _view.EntityId == _entityId && _view._scaleSlot.IsCurrent(this);

            public bool IsAvailable => IsCurrent && _view.gameObject.activeInHierarchy;

            public void ApplyScale(float multiplier)
            {
                if (!IsAvailable)
                    throw new InvalidOperationException("[FieldUnitView] 표시 크기 대여가 취소·반환됐습니다.");
                if (!(multiplier > 0f) || float.IsInfinity(multiplier))
                    throw new ArgumentOutOfRangeException(nameof(multiplier), multiplier, "크기 배율은 유한한 양수여야 합니다.");
                _view.transform.localScale = _localScale * multiplier;
            }

            public void RestoreScale()
            {
                if (!IsCurrent || !_view._scaleSlot.Release(this)) return;
                _view.transform.localScale = _localScale;
            }
        }
    }
}
