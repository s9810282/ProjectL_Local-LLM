using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Input.Contracts;
using UnityEngine;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Input.Cameras
{
    /// <summary>
    /// 전투 카메라 조작 입력입니다(카메라 조작 설계 CC1·CC2). 키 방향·휠 칸·마지막 포인터 위치를 받아 두고,
    /// 조정자 Tick마다 한 번 이번 프레임의 이동 방향·줌 칸 수를 만들어 시점(<see cref="ICombatCameraControl"/>)에 넘깁니다.
    /// 카메라 위치·렌즈·속도·제한은 모릅니다.
    /// </summary>
    /// <remarks>
    /// <para>
    /// 입력은 상태로만 저장하고, 실제로 움직일지는 <see cref="Tick"/>이 받은 정책 답(<c>CombatInputPolicy.CanMoveCamera</c>)으로 정합니다(계획 P5).
    /// 막힌 동안 들어온 휠은 버리고, 누르고 있는 키 방향은 남겨 막힘이 풀리면 바로 이어 움직입니다.
    /// </para>
    /// <para>
    /// 화면 가장자리 이동(결정 Q-CC5 A)은 포인터가 창 안이고, 가장자리 띠 안이며, UI 위가 아닐 때만 합니다. 창 포커스를 잃으면(<see cref="Clear"/>)
    /// 마지막 포인터를 잊어 포인터가 다시 움직일 때까지 가장자리 이동이 없습니다.
    /// </para>
    /// <para>
    /// 시점이 바뀐 프레임에는 <see cref="Tick"/>이 참을 돌려주고, 묶음이 마지막 포인터(<see cref="TryGetPointer"/>)로 포인터 이동을 다시 보냅니다
    /// (평가 C10-01). 마우스가 그대로여도 그 아래 칸이 바뀌기 때문입니다. 줌 렌즈가 옮겨 가는 동안에도 참이고, 실제 카메라 자리가 같은 프레임
    /// 뒤쪽(LateUpdate)에 옮겨지므로 마지막으로 바뀐 뒤 한 Tick 더 참입니다. 줌 렌즈 보간은 보낸 다음 프레임부터 진행하므로, 보낸 Tick에는
    /// 남은 시간을 줄이지 않습니다. 그래야 마지막 다시 집기가 끝난 렌즈를 봅니다.
    /// </para>
    /// <para>연결 입력 묶음(<c>CombatSceneInput</c>)이 연결마다 하나 갖고, 정지 때 놓습니다.</para>
    /// </remarks>
    internal sealed class CombatCameraInput
    {
        private readonly ICombatCameraControl _camera;
        private readonly ICombatPointerOcclusion _occlusion;
        private readonly Func<Vector2> _screenSize;
        private Vector2 _keyDirection;
        private int _zoomSteps;
        private bool _hasPointer;
        private Vector2 _pointer;
        // 줌 렌즈가 새 크기로 옮겨 가는 남은 시간(초)입니다. 그동안 화면 아래 칸이 계속 바뀝니다.
        private float _zoomSettleSeconds;
        // 포인터를 다시 집을 남은 Tick 수입니다. 시점이 바뀐 Tick과 그다음 Tick에 집습니다.
        private int _repickTicks;

        /// <param name="camera">이 연결의 카메라 조작 시점입니다.</param>
        /// <param name="occlusion">UI 가림 판정입니다. 가장자리 이동이 UI 위에서 일어나지 않게 합니다.</param>
        /// <param name="screenSize">지금 화면 크기(픽셀)입니다. 운영은 Unity 화면 크기를 넘깁니다.</param>
        internal CombatCameraInput(ICombatCameraControl camera, ICombatPointerOcclusion occlusion, Func<Vector2> screenSize)
        {
            _camera = camera ?? throw new ArgumentNullException(nameof(camera));
            _occlusion = occlusion ?? throw new ArgumentNullException(nameof(occlusion));
            _screenSize = screenSize ?? throw new ArgumentNullException(nameof(screenSize));
        }

        /// <summary>이동 키 방향이 바뀌었습니다(x = 오른쪽, y = 위). 모두 떼면 (0, 0)입니다.</summary>
        internal void PanChanged(float x, float y) => _keyDirection = new Vector2(x, y);

        /// <summary>휠을 굴렸습니다. 값의 크기는 보지 않고 한 번 = 한 칸, 위 = 줌인으로 셉니다(계획 P1).</summary>
        internal void Scrolled(float delta)
        {
            if (delta > 0f)
                ++_zoomSteps;
            else if (delta < 0f)
                --_zoomSteps;
        }

        /// <summary>포인터가 움직였습니다(화면 좌표). 가장자리 이동은 이 마지막 위치를 봅니다.</summary>
        internal void PointerMoved(float screenX, float screenY)
        {
            _pointer = new Vector2(screenX, screenY);
            _hasPointer = true;
        }

        /// <summary>마지막 포인터 위치입니다. 받은 적이 없거나 비운 뒤면 false입니다.</summary>
        internal bool TryGetPointer(out float screenX, out float screenY)
        {
            screenX = _pointer.x;
            screenY = _pointer.y;
            return _hasPointer;
        }

        /// <summary>창 포커스를 잃었거나 입력을 정지합니다. 누른 키·쌓인 휠·마지막 포인터를 모두 잊고, 포인터 다시 집기도 멈춥니다.</summary>
        internal void Clear()
        {
            _keyDirection = Vector2.zero;
            _zoomSteps = 0;
            _hasPointer = false;
            _zoomSettleSeconds = 0f;
            _repickTicks = 0;
        }

        /// <summary>
        /// 이번 프레임을 한 번 진행합니다. 쌓인 휠 칸은 늘 비웁니다(막힌 동안의 휠을 나중에 쓰지 않음).
        /// <paramref name="canMove"/>가 참일 때만 줌 → 이동 순서로 시점에 넘깁니다.
        /// </summary>
        /// <param name="canMove">지금 카메라를 움직여도 되는지입니다(<c>CombatInputPolicy.CanMoveCamera</c>).</param>
        /// <returns>
        /// 이번에 포인터를 다시 집어야 하면 true입니다. 움직여도 되고 포인터를 받았으며, 시점이 바뀌었거나 줌 렌즈가 옮겨 가는 중이거나
        /// 바로 앞 Tick에 바뀌었을 때입니다(클래스 설명).
        /// </returns>
        internal bool Tick(bool canMove, float deltaTime)
        {
            int steps = _zoomSteps;
            _zoomSteps = 0;
            bool changed = false;
            // 이미 옮겨 가는 줌의 남은 시간을 먼저 줄인다. 이번 Tick에 보낸 줌은 다음 프레임부터 보간되므로 여기서 줄이지 않는다.
            if (_zoomSettleSeconds > 0f)
            {
                _zoomSettleSeconds -= Mathf.Max(0f, deltaTime);
                changed = true;
            }
            if (canMove)
            {
                if (steps != 0 && _camera.Zoom(steps))
                {
                    _zoomSettleSeconds = Mathf.Max(_zoomSettleSeconds, _camera.ZoomSeconds);
                    changed = true;
                }
                changed |= _camera.Pan(_keyDirection, EdgeDirection(), deltaTime);
            }
            if (changed)
                _repickTicks = 2;
            if (_repickTicks == 0)
                return false;
            --_repickTicks;
            return canMove && _hasPointer;
        }

        /// <summary>마지막 포인터가 가장자리 띠 안이면 그쪽 방향(각 축 −1·0·1)입니다. 창 밖·UI 위·띠 없음이면 (0, 0)입니다.</summary>
        private Vector2 EdgeDirection()
        {
            float thickness = _camera.EdgeThickness;
            if (!_hasPointer || !(thickness > 0f))
                return Vector2.zero;
            Vector2 size = _screenSize();
            if (_pointer.x < 0f || _pointer.y < 0f || _pointer.x >= size.x || _pointer.y >= size.y)
                return Vector2.zero;
            float band = thickness * Mathf.Min(size.x, size.y);
            float x = _pointer.x < band ? -1f : _pointer.x >= size.x - band ? 1f : 0f;
            float y = _pointer.y < band ? -1f : _pointer.y >= size.y - band ? 1f : 0f;
            if (x == 0f && y == 0f)
                return Vector2.zero;
            // UI 판정은 Raycast라 띠 안일 때만 합니다.
            return _occlusion.IsPointerOverUi(_pointer.x, _pointer.y) ? Vector2.zero : new Vector2(x, y);
        }
    }
}
