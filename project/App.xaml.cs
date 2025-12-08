namespace project;

public partial class App : Application
{
    public App()
    {
        InitializeComponent();
    }

    protected override Window CreateWindow(IActivationState? activationState)
    {
        var window = new Window(new NavigationPage(new MainPage())) 
        { 
            Title = "project" 
        };

#if WINDOWS
        // Set window size (width x height)
        window.Width = 1520.8;
        window.Height = 2825.9;
        
        // Optional: Center the window on screen
        window.X = -1; // -1 means center horizontally
        window.Y = -1; // -1 means center vertically
        
        // Optional: Set minimum window size
        window.MinimumWidth = 800;
        window.MinimumHeight = 600;
#endif

        return window;
    }
}
