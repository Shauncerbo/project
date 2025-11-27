using System.Diagnostics;

namespace project.Services
{
    public class ToastService : IToastService
    {
        public event Action<string, string>? OnShow;
        
        // State properties
        public bool IsVisible { get; private set; }
        public string Message { get; private set; } = "";
        public string Type { get; private set; } = "success";

        public void ShowSuccess(string message)
        {
            System.Diagnostics.Debug.WriteLine($"[ToastService] ShowSuccess called: {message}");
            Show("success", message);
        }

        public void ShowError(string message)
        {
            System.Diagnostics.Debug.WriteLine($"[ToastService] ShowError called: {message}");
            Show("error", message);
        }

        public void ShowInfo(string message)
        {
            System.Diagnostics.Debug.WriteLine($"[ToastService] ShowInfo called: {message}");
            Show("info", message);
        }

        public void ShowWarning(string message)
        {
            System.Diagnostics.Debug.WriteLine($"[ToastService] ShowWarning called: {message}");
            Show("warning", message);
        }

        private void Show(string type, string message)
        {
            try
            {
                Type = type;
                Message = message;
                IsVisible = true;
                System.Diagnostics.Debug.WriteLine($"[ToastService] State updated - IsVisible={IsVisible}, Type={Type}, Message={Message}");
                
                // Invoke event
                OnShow?.Invoke(type, message);
                System.Diagnostics.Debug.WriteLine($"[ToastService] Event invoked successfully");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ToastService] Error: {ex.Message}");
            }
        }

        public void Hide()
        {
            IsVisible = false;
            System.Diagnostics.Debug.WriteLine("[ToastService] Hide called");
        }
    }
}
