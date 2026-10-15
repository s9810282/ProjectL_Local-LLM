using System;
using ProjectL.Systems.ServiceKeys;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Current;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.UISystem;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.UISystem.Base;
using ProjectL.Framework.SystemBase;
using UnityEngine;

namespace ProjectL.Systems.RuntimeSystem.GlobalSystem.UISystem.Screens.Stage
{
    public sealed class StageUI : UIBase
    {
        [SerializeField] private TMPro.TMP_Text stageNameText;
        private ICurrentProgressBufferReadSink buffer;
        public override UIId ID => UIId.Stage;

        public override void ExportUIService(IUIRegistry registry) => registry.AddUI(ID, this);
        public override void BindUIService(ISystemServiceStoreReader resolver)
        {
            buffer = resolver.GetService(BufferSystemKey.Buffer).CurrentProgressReadSink;
        }

        public override void GetUIData(IServiceModule query)
        {
            if (query == null)
                throw new ArgumentNullException(nameof(query));
        }

        public override void Open()
        {
            if (buffer == null) throw new InvalidOperationException("StageUI requires DB initialization.");
            if (stageNameText == null) throw new InvalidOperationException("Assign StageUI's stage name Text.");
            stageNameText.text = buffer.HasSelection ? buffer.CurrentStage.StageName : string.Empty;
            gameObject.SetActive(true);
        }

        public override void Hide() => gameObject.SetActive(false);
    }
}
