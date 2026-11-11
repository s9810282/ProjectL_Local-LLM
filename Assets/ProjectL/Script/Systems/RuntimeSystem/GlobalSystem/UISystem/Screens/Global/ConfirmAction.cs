namespace ProjectL.Systems.RuntimeSystem.GlobalSystem.UISystem.Screens.Global
{
    public enum ConfirmAction
    {
        SaveSettings,
        SaveProgress,
        AbandonRun,
        QuitGame,
        AbandonStageRun
    }

    public interface IConfirmReceiver
    {
        void Respond(ConfirmAction action, bool accepted);
    }
}
