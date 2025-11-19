namespace project.Services;

public interface INativeNavigationService
{
    event EventHandler? ScannerClosed;

    Task ShowNativeScannerAsync();

    void NotifyScannerClosed();
}


