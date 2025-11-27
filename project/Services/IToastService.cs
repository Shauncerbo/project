namespace project.Services
{
    public interface IToastService
    {
        event Action<string, string>? OnShow;
        void ShowSuccess(string message);
        void ShowError(string message);
        void ShowInfo(string message);
        void ShowWarning(string message);
        void Hide();
        
        // State-based properties for direct access
        bool IsVisible { get; }
        string Message { get; }
        string Type { get; }
    }
}
