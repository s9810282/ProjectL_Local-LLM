using System;
using ProjectL.Systems.DataBaseSystem.Data.View.Table.StageViewProp.Item;
using ProjectL.Systems.DataBaseSystem.Queries.ViewDataBase.ViewTable;
using UnityEngine;
using Object = UnityEngine.Object;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Display
{
    /// <summary>
    /// 스테이지 배경 프랍(View DB StageViewProp)을 필드 기준으로 하나 세웁니다.
    /// 전투 설치 때 스테이지 키로 한 번 고르고, 정리·해제 때 없앱니다. 게임 상태와 무관한 장식입니다.
    /// </summary>
    /// <remarks>
    /// 같은 스테이지로 다시 설치하면 기존 인스턴스를 그대로 둡니다. 키에 맞는 프랍이 없으면 아무것도 두지 않습니다.
    /// 필드 칸 집기는 평면 계산이라 프랍의 콜라이더가 입력을 가리지 않습니다.
    /// </remarks>
    internal sealed class StageViewPropPresenter
    {
        private readonly IStageViewPropTableQuery _props;
        private readonly Transform _parent;
        private GameObject _instance;
        private int _stageKey;

        internal StageViewPropPresenter(IStageViewPropTableQuery props, Transform parent)
        {
            _props = props ?? throw new ArgumentNullException(nameof(props));
            if (parent == null)
                throw new ArgumentNullException(nameof(parent));
            _parent = parent;
        }

        /// <summary>스테이지 키의 프랍을 필드 아래에 둡니다. 키가 0이거나 등록된 프랍이 없으면 기존 것만 치웁니다.</summary>
        internal void Show(int stageKey)
        {
            if (_instance != null && _stageKey == stageKey)
                return;

            Release();
            if (stageKey < 1 || !_props.TryGetStageViewProp(stageKey, out StageViewPropData data))
                return;

            _instance = Object.Instantiate(data.GameObject, _parent, false);
            _instance.transform.localPosition = data.PositionOffset;
            _instance.transform.localRotation = Quaternion.Euler(data.RotationOffset);
            _instance.transform.localScale = Vector3.Scale(data.GameObject.transform.localScale, data.ScaleOffset);
            _stageKey = stageKey;
        }

        /// <summary>세운 프랍을 없앱니다. 여러 번 불려도 안전합니다.</summary>
        internal void Release()
        {
            GameObject instance = _instance;
            _instance = null;
            _stageKey = 0;
            if (instance == null)
                return;

            // EditMode 테스트에서는 Destroy를 쓸 수 없다.
            if (Application.isPlaying)
                Object.Destroy(instance);
            else
                Object.DestroyImmediate(instance);
        }
    }
}
