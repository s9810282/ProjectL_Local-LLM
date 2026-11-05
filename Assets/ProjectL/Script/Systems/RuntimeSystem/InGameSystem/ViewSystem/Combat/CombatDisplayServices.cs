using System;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.UISystem;
using ProjectL.Systems.RuntimeSystem.GlobalSystem;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.AudioSystem;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.RenderSystem.Core.CameraGroup.CameraEffectSystem;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.RenderSystem.Core.ColorSystem;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.RenderSystem.Core.EffectSystem;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.RenderSystem.Core.MeshSystem;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.RenderSystem.View.AreaOutlineSystem;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.RenderSystem.View.ScreenTransitionSystem;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Display;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat
{
    /// <summary>
    /// 전투 씬 표시(<see cref="CombatSceneDisplay"/>)가 쓰는 Global 서비스(Render·Audio·UI, 카메라 효과는 Render 쪽) 묶음입니다(구조 리팩토링 설계 D4).
    /// </summary>
    /// <remarks>
    /// <see cref="CombatViewSystem"/>은 서비스를 받을 때 <see cref="From"/>으로 이 묶음을 한 번 만들고, 씬 연결 때 표시 생성 계약(<c>ICombatSceneFactory</c>)에 넘깁니다.
    /// Factory는 개별 서비스를 필요한 협력 객체에 연결하며 서비스 수명은 기존 Global 소유자가 관리합니다.
    /// 오디오는 표시(국면·완료 뒤 배경음·사건 소리)와 연결 입력 묶음(입력 효과음)이 이 묶음에서 받습니다(View 2라운드 W6-5).
    /// 모든 서비스가 필수입니다(View 2라운드 W6-7). 시험 조립은 아무것도 하지 않는 가짜를 넘깁니다.
    /// </remarks>
    internal sealed class CombatDisplayServices
    {
        /// <param name="audio">효과음·배경음 서비스입니다.</param>
        /// <param name="screenTransitions">단계 화면 전환(밤·아침) 서비스입니다.</param>
        /// <param name="ui">UI 서비스입니다(정보창).</param>
        /// <param name="cameraEffects">카메라 효과(줌·흔들림)입니다(카메라 연출 조각 설계 K1).</param>
        internal CombatDisplayServices(
            IColorSystemService colors,
            IAreaOutlineSystemService areaOutlines,
            IMeshSystemService meshes,
            IEffectSystemServices effects,
            IAudioSystemService audio,
            IScreenTransitionSystemService screenTransitions,
            IUISystemService ui,
            ICameraEffectService cameraEffects)
        {
            Colors = colors ?? throw new ArgumentNullException(nameof(colors));
            // GridOutline 표시 모드가 칸 테두리를 그릴 때 쓴다. CellPrefab 모드에서도 같은 조립 경로라 필수로 받는다.
            AreaOutlines = areaOutlines ?? throw new ArgumentNullException(nameof(areaOutlines));
            // GridOutline 표시 모드가 범위 칸을 채울 때 쓴다. 테두리와 같은 이유로 필수로 받는다.
            Meshes = meshes ?? throw new ArgumentNullException(nameof(meshes));
            Effects = effects ?? throw new ArgumentNullException(nameof(effects));
            Audio = audio ?? throw new ArgumentNullException(nameof(audio));
            ScreenTransitions = screenTransitions ?? throw new ArgumentNullException(nameof(screenTransitions));
            UI = ui ?? throw new ArgumentNullException(nameof(ui));
            CameraEffects = cameraEffects ?? throw new ArgumentNullException(nameof(cameraEffects));
        }

        /// <summary>
        /// Global 그룹에서 전투 표시가 쓰는 Render·Audio·UI 서비스를 고릅니다. 어떤 서비스가 필요한지는 이 묶음 한 곳이 정합니다
        /// (<see cref="CombatDisplayTables.From"/>와 같은 방식, 구조 개선 SV5·E16).
        /// </summary>
        internal static CombatDisplayServices From(IGlobalSystemGroupService global)
        {
            if (global == null)
                throw new ArgumentNullException(nameof(global));

            var render = global.RenderServiceCommand;
            return new CombatDisplayServices(
                render.CoreGroup.ColorSystemServices,
                render.ViewGroup.AreaOutlineSystemServices,
                render.CoreGroup.MeshSystemServices,
                render.CoreGroup.EffectSystemServices,
                global.AudioServiceCommand,
                render.ViewGroup.ScreenTransitionSystemServices,
                global.UIServiceCommand,
                render.CoreGroup.CameraGroup.CameraEffectSystem);
        }

        internal IColorSystemService Colors { get; }

        internal IAreaOutlineSystemService AreaOutlines { get; }

        internal IMeshSystemService Meshes { get; }

        internal IEffectSystemServices Effects { get; }

        /// <summary>효과음·배경음 서비스입니다(필수).</summary>
        internal IAudioSystemService Audio { get; }

        /// <summary>단계 화면 전환 서비스입니다(필수). 시험 조립은 바로 끝나는 가짜를 넘깁니다.</summary>
        internal IScreenTransitionSystemService ScreenTransitions { get; }

        internal IUISystemService UI { get; }

        /// <summary>카메라 효과 서비스입니다(필수). 맵 카메라가 없으면 전투 카메라 창구가 줌·흔들림을 내지 않습니다.</summary>
        internal ICameraEffectService CameraEffects { get; }
    }
}
