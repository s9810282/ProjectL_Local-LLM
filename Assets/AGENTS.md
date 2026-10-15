# UI input conventions

- In Deck and future Hand unit lists, clicking a unit pins its info card. Hovering any other unit must not switch or unpin it. Clicking another unit may switch the pinned selection; clicking the pinned unit again closes it. Hover previews resume after the pin is cleared.

- For new or modified UI, prefer Unity pointer handler interfaces (`IPointerClickHandler`, `IPointerEnterHandler`, `IPointerExitHandler`) and `ISubmitHandler` with direct calls to an explicit receiver interface.
- Reuse `UIInputBinding` and `IUIInputReceiver` in `ProjectL/Script/Systems/RuntimeSystem/GlobalSystem/UISystem/Base` for Button inputs. Bind the receiver and slot once; do not connect screen behavior through `Button.onClick.AddListener`, delegate closures, or Inspector UnityEvent listeners.
- Keep Button visuals, interactability, left-click behavior, and keyboard/gamepad Submit support. Hover must be cleared once on exit, disable, rebind, or loss of interactability. Avoid per-control raycasts and allocations in pointer callbacks.
- InputSystem owns action maps, shortcuts, and input layers. UI views own presentation and receive explicit input calls. Do not introduce event subscriptions for UI input without a concrete need.
- When verifying input, use EventSystem handler dispatch rather than invoking Button.onClick directly.
