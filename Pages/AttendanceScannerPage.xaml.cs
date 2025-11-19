using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Dispatching;
using Microsoft.Maui.Controls;
using project.Services;
using ZXing.Net.Maui;

namespace project.Pages
{
    public partial class AttendanceScannerPage : ContentPage
    {
        private readonly IAttendanceService _attendanceService;
        private bool _isProcessing;

        public AttendanceScannerPage(IAttendanceService attendanceService)
        {
            InitializeComponent();
            _attendanceService = attendanceService;
        }

        private void StartButton_Clicked(object sender, EventArgs e)
        {
            cameraView.IsDetecting = true;
            statusLabel.Text = "Scanner running... point camera at QR code.";
        }

        private void StopButton_Clicked(object sender, EventArgs e)
        {
            cameraView.IsDetecting = false;
            statusLabel.Text = "Scanner stopped.";
        }

        private void TorchButton_Clicked(object sender, EventArgs e)
        {
            cameraView.IsTorchOn = !cameraView.IsTorchOn;
            statusLabel.Text = cameraView.IsTorchOn ? "Torch enabled." : "Torch disabled.";
        }

        private async void CloseButton_Clicked(object sender, EventArgs e)
        {
            cameraView.IsDetecting = false;
            if (Navigation.NavigationStack.Count > 1)
            {
                await Navigation.PopAsync();
            }
        }

        private async void CameraView_BarcodesDetected(object sender, BarcodeDetectionEventArgs e)
        {
            if (_isProcessing)
                return;

            var result = e.Results?.FirstOrDefault();
            if (result == null)
                return;

            _isProcessing = true;

            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                statusLabel.Text = $"Processing QR: {result.Value}";
            });

            var success = await _attendanceService.ProcessQrAttendanceAsync(result.Value);

            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                lastResultLabel.Text = $"Last QR: {result.Value}";
                statusLabel.Text = success
                    ? "Attendance updated successfully!"
                    : "QR not recognized or attendance update failed.";
            });

            await Task.Delay(1500);
            _isProcessing = false;
        }
    }
}

