using ProjectL.Framework.DataBase;
using UnityEngine;

namespace ProjectL.Systems.DataBaseSystem.Data.View.Table.LobbyVisual.Item
{
    /// <summary>로비 시설 호버 때 안내 글자 빛남(글자 색·빛 번짐)을 입력합니다(로비 설계 §16).</summary>
    [CreateAssetMenu(fileName = "NewLobbyFacilityHoverData", menuName = "ProjectL/Database/View/LobbyFacilityHoverData")]
    internal sealed class LobbyFacilityHoverDataSO : DatabaseItem
    {
        [Header("호버하는 동안")]
        [Tooltip("호버하는 동안 안내 글자 색입니다. 흰색에 가까울수록 밝습니다.")]
        [SerializeField] private Color textColor = new(1f, 1f, 0.92f, 1f);
        [Tooltip("글자 뒤 빛 번짐 색입니다. 알파가 빛 세기입니다(1이 가장 셈).")]
        [SerializeField] private Color glowColor = new(1f, 0.85f, 0.45f, 1f);
        [Tooltip("빛 번짐 크기입니다(TMP Underlay Dilate). 클수록 빛이 넓게 퍼집니다.")]
        [SerializeField, Range(0f, 1f)] private float glowSize = 0.45f;
        [Tooltip("빛 번짐 부드러움입니다(TMP Underlay Softness). 클수록 가장자리가 흐립니다.")]
        [SerializeField, Range(0f, 1f)] private float glowSoftness = 0.5f;

        [Header("셰이더")]
        [Tooltip("Underlay를 켠 TMP 머티리얼입니다(안내 글자와 같은 셰이더). 빌드에 빛 번짐 셰이더 변형을 남기는 데만 쓰고, 색·크기는 위 칸이 정합니다.")]
        [SerializeField] private Material glowMaterial;

        public override IData GetData() => new LobbyFacilityHoverData(textColor, glowColor, glowSize, glowSoftness,
            glowMaterial);
    }
}
