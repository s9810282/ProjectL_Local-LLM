using System;
using ProjectL.Framework.DataBase;
using UnityEngine;
using UnityEngine.InputSystem;

namespace ProjectL.Systems.DataBaseSystem.Data.View.Table.InputSettings.Item
{
    /// <summary>
    /// 입력 연결 계층이 쓰는 액션 에셋입니다. 런타임은 이 에셋을 복제해 쓰고 원본은 바꾸지 않습니다.
    /// ActionMap·Action 이름과 선택 버튼 설정은 입력 연결 계층이 시작할 때 검증합니다.
    /// </summary>
    [Serializable]
    internal sealed class InputSettingsData : IData
    {
        [Tooltip("스테이지 입력이 쓰는 액션 에셋입니다(Assets/ProjectL/Input/PlayerInputActions).")]
        [SerializeField] private InputActionAsset actions;

        public InputActionAsset Actions => actions;

        internal void Validate()
        {
            if (actions == null)
                throw new InvalidOperationException("[InputSettingsData] Input action asset is required.");
        }
    }

    [CreateAssetMenu(fileName = "NewInputSettingsData", menuName = "ProjectL/Database/View/InputSettingsData")]
    internal sealed class InputSettingsDataSO : DatabaseItem
    {
        [SerializeField] private InputSettingsData inputSettingsData = new();

        public override IData GetData() => inputSettingsData;
    }
}
