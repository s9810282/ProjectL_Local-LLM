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

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Display.Unit
{
    /// <summary>
    /// 필드에 놓인 유닛·구조물 하나의 표시입니다(옛 FieldUnitView 이식, 계획 D.2).
    /// 위치·방향·표시 여부·체력 숫자만 다루고, 실제 표시값과 다를 때만 씁니다(설계 §6.1).
    /// </summary>
    /// <remarks>
    /// 프리팹 인스턴스에 런타임으로 붙습니다. 공격·반응·능력 동작은 같은 자세 대여를 사용합니다.
    /// 재생 쪽은 창구(<see cref="CombatUnitPresentationWindow"/>)를 거쳐 표시 계약(<see cref="ICombatUnitPresentationTarget"/>)으로만 이 표시를 씁니다(연출 조립 설계 B13).
    /// </remarks>
    [UnityEngine.Scripting.APIUpdating.MovedFrom(true, sourceNamespace: "ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Display")]
    [DisallowMultipleComponent]
    public sealed class FieldUnitView : MonoBehaviour, ICombatUnitPresentationTarget, ICombatUnitHudTarget, IUnitHUDAnchor
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
        //  5. HUD 내용을 정하는 표가 바뀔 수 있을 때: 표시 집합 FieldUnitPresenter의 Configure·ConfigureHUDCategories가 ForgetHudFacts로.
        private CharacterCombatFacts _hudFacts;
        private bool _entryHudHidden;
        // 채널마다 대여 칸 하나입니다. 대여 수명 규칙은 칸이 갖고, 시작 조건·복원 내용은 아래 대여 클래스가 갖습니다(클립 계획 T10a).
        private readonly UnitPresentationLeaseSlot<PresentationUnitPose> _poseSlot = new();
        // 특성 발동 유닛 행동이 옮긴 서는 자리입니다(유닛 행동 설계 AA2). 자세 칸의 아래 층이라 자세 칸과 따로입니다.
        private readonly UnitPresentationLeaseSlot<PresentationUnitStand> _standSlot = new();
        private readonly UnitPresentationLeaseSlot<PresentationUnitScale> _scaleSlot = new();
        // 모델 외형 층(이동 반투명·피격 플래시)의 합성 소유자입니다(조각·조립 설계 BP9).
        private UnitModelAppearance _appearance;
        // 적 전용 디졸브 셰이더 복제본의 수명입니다. 원본 프리팹 재질은 바꾸지 않습니다.
        private EntityCartoonAppearance _entityCartoon;
        private IColorSystemService _movementColors;
        private readonly UnitPresentationLeaseSlot<PresentationUnitFlash> _flashSlot = new();
        // 사건 클립 채널입니다(클립 설계 AC9). 이벤트 컴포넌트가 붙은 표시만 가집니다. 컴포넌트 찾기는 인스턴스마다 한 번입니다.
        private UnitClipChannel _clipChannel;
        private CombatAnimationEvents _clipEvents;
        private bool _clipEventsSearched;
        private readonly UnitPresentationLeaseSlot<PresentationUnitClip> _clipSlot = new();
        internal UnitHUD Hud => _unitHud;

        /// <summary>지금 자세 대여입니다. 없으면 null입니다(시험·진단용 읽기).</summary>
        internal ICombatAttackUnitPose CurrentPresentationPose => _poseSlot.Current;

        /// <summary>지금 사건 클립 채널입니다. 없으면 null입니다(시험·진단용 읽기).</summary>
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

        private void OnEnable()
        {
            SyncHudActive();
            _clipChannel?.Resume();
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
            Transform self = transform;

            // 연출·풀 재사용이 바꾼 값도 되돌리도록 저장값이 아니라 실제 표시값과 비교한다.
            // 자세·서는 자리 대여가 있으면 위치·회전은 그 대여가 쓴다(서는 자리는 유닛 행동 설계 AA2).
            bool leased = _poseSlot.Current != null || _standSlot.Current != null;
            if (!leased && self.position != worldPosition)
            {
                self.position = worldPosition;
                changed = true;
            }

            if (!leased) changed |= ApplyRotation(worldRotation);

            if (gameObject.activeSelf != visible)
            {
                gameObject.SetActive(visible);
                changed = true;
            }

            changed |= RefreshHealthLabel();

            return changed;
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
            if (_unitHud != null) _unitHud.Clear();
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
        /// 표시 대여 다섯(자세·서는 자리·크기·플래시·클립)과 이동 투명도·예상 체력을 모두 정리합니다. 다시 묶기·끄기·풀 반환이 이 목록 하나를 씁니다.
        /// </summary>
        /// <remarks>
        /// 복원 대상은 칸마다 다릅니다(자세·서는 자리는 위치·회전, 크기는 localScale, 플래시·투명도는 외형의 두 층, 클립은 Animator 아래, 예상 체력은 HUD).
        /// 자세와 서는 자리는 같은 위치·회전을 쓰므로 위 층(자세)을 먼저 되돌리고 아래 층(서는 자리)을 뒤에 되돌려, 끝 자리가 서는 자리를 빌리기 전 자리가 되게 합니다.
        /// 대여 칸의 Reset은 취소 콜백을 바로 부르고, 그 콜백이 같은 실행이 빌린 다른 칸을 되돌립니다.
        /// 그래서 대여 다섯의 순서(자세 → 서는 자리 → 크기 → 플래시 → 클립)는 지킵니다. "자세 취소가 크기 취소보다 먼저"는 시험이 지키는 순서입니다
        /// (<c>CombatUnitPresentationWindowTests</c>의 <c>…CancelsPoseBeforeScale</c> 둘).
        /// 서는 자리만은 취소 콜백 없이 내려놓습니다(<see cref="DropStand"/>, 묶음 끝 검토 H1). 콜백이 없는 이동 투명도는 맨 앞에서 지웁니다.
        /// </remarks>
        private void ReleasePresentation()
        {
            ClearMovementOpacity();
            ClearDeathDissolve();
            ResetPresentationPose();
            DropStand();
            ResetPresentationScale();
            ResetHitFlash();
            ResetClip();
            SetAttackHealthPreview(null);
        }

        private void OnDisable()
        {
            SyncHudActive();
            ReleasePresentation();
            _clipChannel?.Pause();
        }

        /// <summary>모델 외형 층(이동 반투명·피격 플래시)을 준비합니다. 색 서비스가 없으면 준비하지 않습니다.</summary>
        internal void PrepareMovementAppearance(IColorSystemService colors)
        {
            _movementColors = colors;
            if (_appearance != null || colors == null) return;
            var models = new List<Renderer>();
            foreach (Renderer renderer in GetComponentsInChildren<Renderer>(true))
                if (renderer.GetComponentInParent<TMP_Text>(true) == null && renderer.GetComponentInParent<Canvas>(true) == null)
                    models.Add(renderer);
            _appearance = new UnitModelAppearance(models.ToArray(), colors);
        }

        /// <summary>체력이 있는 적 표시일 때만 모델 재질을 EntityCartoon으로 바꿉니다.</summary>
        internal void SetEntityCartoon(bool enabled)
        {
            if ((_entityCartoon?.IsEnabled ?? false) == enabled) return;

            // 외형 층은 캡처한 원래 재질로 복원하므로, 셰이더 교체 전에 반드시 끝냅니다.
            ReleaseMovementAppearance();
            try
            {
                _entityCartoon ??= new EntityCartoonAppearance(gameObject);
                _entityCartoon.SetEnabled(enabled);
            }
            finally
            {
                PrepareMovementAppearance(_movementColors);
            }
        }

        internal void SetMovementOpacity(float opacity)
        {
            if (_appearance == null)
                throw new InvalidOperationException("[FieldUnitView] 이동 모델 표시 준비가 필요합니다.");
            _appearance.SetOpacity(opacity);
        }

        /// <summary>이동 반투명 층만 끝냅니다. 피격 플래시 층이 남으면 그대로 둡니다.</summary>
        internal void ClearMovementOpacity() => _appearance?.ClearOpacity();

        /// <summary>적 사망 반응의 디졸브 진행값입니다. EntityCartoon 이외의 표시는 바꾸지 않습니다.</summary>
        internal void SetDeathDissolve(float amount)
        {
            if (_entityCartoon?.IsEnabled == true)
                _appearance?.SetDissolve(amount);
        }

        private void ClearDeathDissolve() => _appearance?.ClearDissolve();

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

        internal void ReleaseMovementAppearance()
        {
            UnitModelAppearance appearance = _appearance;
            _appearance = null;
            appearance?.Dispose();
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

        private void ResetClip() => _clipSlot.Reset(lease => lease.RestoreClip());

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
                    try { _entityCartoon?.Dispose(); }
                    finally { ReleaseUnitHud(); }
                }
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
        private sealed class PresentationUnitPose : ICombatAttackUnitPose
        {
            private readonly FieldUnitView _view;
            private readonly EntityId _entityId;
            private readonly Vector3 _healthPosition;

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
                _view.SetDeathDissolve(amount);
            }

            public void ClearDeathDissolve()
            {
                if (!IsCurrent) return;
                _view.ClearDeathDissolve();
            }

            public void RestorePose()
            {
                if (!IsCurrent || !_view._poseSlot.Release(this)) return;
                _view.transform.position = Position;
                _view.ApplyRotation(Rotation);
                if (_view._healthLabel != null)
                    _view._healthLabel.transform.position = _healthPosition;
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

            public void SetFlash(Color color, float strength)
            {
                if (!IsAvailable)
                    throw new InvalidOperationException("[FieldUnitView] 피격 플래시 대여가 취소·반환됐습니다.");
                _view._appearance.SetFlash(color, strength);
            }

            public void RestoreFlash()
            {
                if (!IsCurrent || !_view._flashSlot.Release(this)) return;
                _view._appearance?.ClearFlash();
            }
        }

        /// <summary>사건 클립 대여 하나입니다(클립 설계 AC3). 수명 규칙은 대여 칸이 갖습니다(클립 계획 T10a).</summary>
        private sealed class PresentationUnitClip : ICombatUnitClip
        {
            private readonly FieldUnitView _view;
            private readonly EntityId _entityId;

            internal PresentationUnitClip(FieldUnitView view)
            {
                _view = view;
                _entityId = view.EntityId;
            }

            private bool IsCurrent => _view != null && _view.EntityId == _entityId && _view._clipSlot.IsCurrent(this);

            public bool IsAvailable => IsCurrent && _view.gameObject.activeInHierarchy && _view._clipChannel != null;

            public void Play(CombatClipData clip)
            {
                RequireAvailable();
                _view._clipChannel.Begin(clip);
            }

            public void SetTime(float time)
            {
                RequireAvailable();
                _view._clipChannel.SetTime(time);
            }

            public void RestoreClip()
            {
                if (!IsCurrent || !_view._clipSlot.Release(this)) return;
                _view._clipChannel?.End();
            }

            public bool TryGetMuzzle(out Vector3 position) => TryGetMuzzle(0, out position);

            public bool TryGetMuzzle(int point, out Vector3 position)
            {
                RequireAvailable();
                return _view._clipChannel.TryGetMuzzle(point, out position);
            }

            public bool TryGetEffectPoint(out Vector3 position, out Quaternion rotation) => TryGetEffectPoint(0, out position, out rotation);

            public bool TryGetEffectPoint(int point, out Vector3 position, out Quaternion rotation)
            {
                RequireAvailable();
                return _view._clipChannel.TryGetEffectPoint(point, out position, out rotation);
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
