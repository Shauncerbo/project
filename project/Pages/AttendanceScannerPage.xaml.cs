using ZXing.Net.Maui;
using project.Services;
using Microsoft.Maui.Controls;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Extensions.DependencyInjection;
using project.Models;

namespace project.Pages;

public partial class AttendanceScannerPage : ContentPage
{
    private readonly IAttendanceService? _attendanceService;
    private readonly IMemberService? _memberService;
    private readonly INativeNavigationService? _nativeNavigationService;
    private bool _isProcessing = false;
    private DateTime _lastScanTime = DateTime.MinValue;
    private const int MIN_SCAN_INTERVAL_MS = 2000; // Prevent rapid scans

    public AttendanceScannerPage(
        IAttendanceService? attendanceService = null,
        IMemberService? memberService = null,
        INativeNavigationService? nativeNavigationService = null)
    {
        InitializeComponent();
        _attendanceService = attendanceService ?? Application.Current?.Handler?.MauiContext?.Services?.GetService<IAttendanceService>();
        _memberService = memberService ?? Application.Current?.Handler?.MauiContext?.Services?.GetService<IMemberService>();
        _nativeNavigationService = nativeNavigationService ?? Application.Current?.Handler?.MauiContext?.Services?.GetService<INativeNavigationService>();
    }

    private async void OnBarcodeDetected(object? sender, BarcodeDetectionEventArgs e)
    {
        // Prevent rapid scans
        var timeSinceLastScan = (DateTime.Now - _lastScanTime).TotalMilliseconds;
        if (timeSinceLastScan < MIN_SCAN_INTERVAL_MS)
        {
            return;
        }

        if (_isProcessing || e.Results == null || e.Results.Length == 0)
            return;

        var qrCode = e.Results[0].Value;
        if (string.IsNullOrWhiteSpace(qrCode))
            return;

        _isProcessing = true;
        _lastScanTime = DateTime.Now;

        // Stop camera immediately to prevent lag and resource issues
        try
        {
            BarcodeReader.IsDetecting = false;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error stopping camera: {ex.Message}");
        }

        try
        {
            System.Diagnostics.Debug.WriteLine($"QR Code detected: {qrCode}");

            if (qrCode.StartsWith("GYM:MEMBER:"))
            {
                var parts = qrCode.Split(':');
                if (parts.Length >= 3 && int.TryParse(parts[2], out int memberId))
                {
                    if (_attendanceService != null)
                    {
                        Member? member = null;
                        if (_memberService != null)
                        {
                            member = await _memberService.GetMemberByIdAsync(memberId);
                            if (member == null)
                            {
                                await ShowAlertAsync("Member Not Found", $"Member ID {memberId} was not found in the system.");
                                await Task.Delay(500);
                                RestartCamera();
                                return;
                            }

                            if (!IsMemberEligibleForAttendance(member, out var reason))
                            {
                                await ShowAlertAsync("Attendance Blocked", reason);
                                await Task.Delay(500);
                                RestartCamera();
                                return;
                            }
                        }

                        // Process attendance asynchronously
                        var success = await _attendanceService.ProcessQrAttendanceAsync(qrCode);
                        
                        if (success)
                        {
                            if (member == null && _memberService != null)
                            {
                                member = await _memberService.GetMemberByIdAsync(memberId);
                            }

                            var memberName = member != null 
                                ? $"{member.FirstName} {member.LastName}" 
                                : $"Member {memberId}";

                            // Show success message on UI thread
                            await ShowAlertAsync(
                                "Success",
                                $"{memberName} attendance processed successfully!");
                            
                            // Add small delay to ensure all processing completes
                            await Task.Delay(300);
                            
                            // Close scanner after successful scan
                            await CloseScannerSafely();
                        }
                        else
                        {
                            await ShowAlertAsync("Error", "Failed to process attendance. Please try again.");
                            // Restart camera for retry
                            await Task.Delay(500);
                            RestartCamera();
                        }
                    }
                }
            }
            else
            {
                await ShowAlertAsync("Invalid QR Code", "This is not a valid gym member QR code.");
                // Restart camera after invalid code
                await Task.Delay(500);
                RestartCamera();
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error processing QR code: {ex.Message}");
            await ShowAlertAsync("Error", $"An error occurred: {ex.Message}");
            // Restart camera after error
            await Task.Delay(500);
            RestartCamera();
        }
        finally
        {
            _isProcessing = false;
        }
    }

    private async Task CloseScannerSafely()
    {
        await MainThread.InvokeOnMainThreadAsync(async () =>
        {
            try
            {
                if (BarcodeReader != null)
                {
                    BarcodeReader.IsDetecting = false;
                }

                await Task.Delay(200);

                if (Navigation != null && Navigation.NavigationStack.Count > 1)
                {
                    await Navigation.PopAsync();
                }

                _nativeNavigationService?.NotifyScannerClosed();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error closing scanner: {ex.Message}");
                try
                {
                    if (Navigation != null && Navigation.NavigationStack.Count > 1)
                    {
                        await Navigation.PopAsync();
                    }
                }
                catch (Exception innerEx)
                {
                    System.Diagnostics.Debug.WriteLine($"Navigation fallback error: {innerEx.Message}");
                }
            }
        });
    }

    private void RestartCamera()
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            try
            {
                if (BarcodeReader != null && !_isProcessing)
                {
                    BarcodeReader.IsDetecting = true;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error restarting camera: {ex.Message}");
            }
        });
    }

    private async void OnCloseClicked(object? sender, EventArgs e)
    {
        await CloseScannerSafely();
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        MainThread.BeginInvokeOnMainThread(() =>
        {
            try
            {
                if (BarcodeReader != null)
                {
                    BarcodeReader.IsDetecting = true;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error starting camera: {ex.Message}");
            }
        });
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        MainThread.BeginInvokeOnMainThread(() =>
        {
            try
            {
                if (BarcodeReader != null)
                {
                    BarcodeReader.IsDetecting = false;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error stopping camera in OnDisappearing: {ex.Message}");
            }
        });
    }

    protected override void OnNavigatedFrom(NavigatedFromEventArgs args)
    {
        base.OnNavigatedFrom(args);
        MainThread.BeginInvokeOnMainThread(() =>
        {
            try
            {
                if (BarcodeReader != null)
                {
                    BarcodeReader.IsDetecting = false;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in OnNavigatedFrom: {ex.Message}");
            }
        });
    }

    private Task ShowAlertAsync(string title, string message)
    {
        return MainThread.InvokeOnMainThreadAsync(async () =>
        {
            try
            {
                await DisplayAlert(title, message, "OK");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error showing alert: {ex.Message}");
            }
        });
    }

    private static bool IsMemberEligibleForAttendance(Member member, out string reason)
    {
        if (member.IsArchived)
        {
            reason = $"{member.FullName} is archived and cannot log attendance.";
            return false;
        }

        if (!string.Equals(member.Status, "Active", StringComparison.OrdinalIgnoreCase))
        {
            var statusText = string.IsNullOrWhiteSpace(member.Status) ? "inactive" : member.Status.ToLower();
            reason = $"{member.FullName} is currently {statusText} and cannot log attendance.";
            return false;
        }

        if (member.ExpirationDate.HasValue && member.ExpirationDate.Value.Date < DateTime.Today)
        {
            reason = $"{member.FullName}'s membership expired on {member.ExpirationDate.Value:MM/dd/yyyy}.";
            return false;
        }

        reason = string.Empty;
        return true;
    }
}


