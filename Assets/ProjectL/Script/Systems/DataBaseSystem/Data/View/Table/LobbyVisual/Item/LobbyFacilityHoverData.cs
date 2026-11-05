using System;
using ProjectL.Framework.DataBase;
using TMPro;
using UnityEngine;

namespace ProjectL.Systems.DataBaseSystem.Data.View.Table.LobbyVisual.Item
{
    /// <summary>
    /// 로비 시설 호버 때 안내 글자의 빛남 값입니다. 호버하는 동안 이 값으로 밝게 빛나고,
    /// 벗어나면 원래 글자로 돌아갑니다(로비 설계 §16).
    /// </summary>
    internal sealed class LobbyFacilityHoverData : IData
    {
        /// <summary>호버하는 동안의 글자 색입니다.</summary>
        internal Color TextColor { get; }

        /// <summary>글자 뒤 빛 번짐 색입니다. 알파가 빛 세기입니다.</summary>
        internal Color GlowColor { get; }

        /// <summary>빛 번짐 크기(TMP Underlay Dilate)입니다. 0~1입니다.</summary>
        internal float GlowSize { get; }

        /// <summary>빛 번짐 부드러움(TMP Underlay Softness)입니다. 0~1입니다.</summary>
        internal float GlowSoftness { get; }

        /// <summary>
        /// Underlay를 켠 TMP 머티리얼입니다. 안내 글자와 같은 셰이더여야 합니다.
        /// 빌드에 빛 번짐 셰이더 변형을 남기는 데만 쓰고, 색·크기 값은 위 칸이 정합니다.
        /// </summary>
        internal Material GlowMaterial { get; }

        internal LobbyFacilityHoverData(Color textColor, Color glowColor, float glowSize, float glowSoftness,
            Material glowMaterial)
        {
            TextColor = textColor;
            GlowColor = glowColor;
            GlowSize = glowSize;
            GlowSoftness = glowSoftness;
            GlowMaterial = glowMaterial;
        }

        /// <summary>호버 빛남 값 조건의 유일한 원본입니다. DB 생성 때 한 번 검사합니다.</summary>
        internal void Validate()
        {
            if (!IsUnitColor(TextColor))
                throw Invalid(nameof(TextColor), "글자 색의 각 성분은 0~1이어야 합니다.");
            if (!IsUnitColor(GlowColor))
                throw Invalid(nameof(GlowColor), "빛 색의 각 성분은 0~1이어야 합니다.");
            if (!IsUnit(GlowSize))
                throw Invalid(nameof(GlowSize), "빛 크기는 0~1이어야 합니다.");
            if (!IsUnit(GlowSoftness))
                throw Invalid(nameof(GlowSoftness), "빛 부드러움은 0~1이어야 합니다.");
            if (GlowMaterial == null)
                throw Invalid(nameof(GlowMaterial), "Underlay를 켠 TMP 머티리얼이 필요합니다.");
            Material glow = GlowMaterial;
            if (!glow.HasProperty(ShaderUtilities.ID_UnderlayColor) || !glow.HasProperty(ShaderUtilities.ID_UnderlayDilate) ||
                !glow.HasProperty(ShaderUtilities.ID_UnderlaySoftness))
                throw Invalid(nameof(GlowMaterial), $"'{glow.name}'의 셰이더에 TMP Underlay 값이 없습니다.");
            if (!glow.IsKeywordEnabled(ShaderUtilities.Keyword_Underlay))
                throw Invalid(nameof(GlowMaterial), $"'{glow.name}'에서 Underlay를 켜야 합니다({ShaderUtilities.Keyword_Underlay}).");
        }

        /// <summary>
        /// 방의 안내 글자가 빛 번짐 머티리얼과 같은 셰이더인지 DB 생성 때 확인합니다.
        /// 방 프리팹 검사는 <see cref="LobbyVisualPrefabRules"/> 한 곳이 맡고, 여기서는 비교할 셰이더만 넘깁니다(로비 리팩토링 2차 설계 N-58).
        /// </summary>
        internal void ValidateLabels(GameObject room) =>
            LobbyVisualPrefabRules.ValidateRoomLabelShader(room, GlowMaterial.shader, nameof(LobbyModelData.RoomPrefab));

        private static InvalidOperationException Invalid(string field, string message) =>
            new($"[LobbyFacilityHoverData] {field}: {message}");

        private static bool IsUnitColor(Color color) =>
            IsUnit(color.r) && IsUnit(color.g) && IsUnit(color.b) && IsUnit(color.a);

        private static bool IsUnit(float value) => float.IsFinite(value) && value >= 0f && value <= 1f;
    }
}
