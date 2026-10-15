using System;
using ProjectL.Framework.DataBase;
using UnityEngine;

namespace ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Method.Core.Table.Item
{
    /// <summary>A serialized row from the MethodMaster worksheet. Column values are preserved without enum conversion.</summary>
    [Serializable]
    public sealed class MethodMasterData : IExcelData
    {
        [SerializeField] private int Method_Key;
        [SerializeField] private int Method_Type;
        [SerializeField] private int Method_ChoiceRange;
        [SerializeField] private int Method_Splash;
        [SerializeField] private int Method_SplashValue;

        public int Key => Method_Key;
        public int Type => Method_Type;
        public int ChoiceRange => Method_ChoiceRange;
        public int Splash => Method_Splash;
        public int SplashValue => Method_SplashValue;
    }
}
