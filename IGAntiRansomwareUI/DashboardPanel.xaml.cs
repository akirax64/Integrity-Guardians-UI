using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;

namespace IGAntiRansomwareUI
{
    public partial class DashboardPanel : UserControl
    {
        public event EventHandler<bool> ProtectionToggled;
        public event EventHandler ScanRequested;
        public event EventHandler<string> LogMessage;
        public event EventHandler<string> ShowNotification;

        public DashboardPanel()
        {
            InitializeComponent();
        }

        public void UpdateStatus(bool isProtected)
        {
            Dispatcher.Invoke(() =>
            {
                if (protectionStatus == null || toggleBtn == null || statusIcon == null)
                    return;

                if (isProtected)
                {
                    protectionStatus.Text = "Proteção Ativa";
                    protectionStatus.Foreground = new SolidColorBrush(Color.FromRgb(166, 227, 161));
                    toggleBtn.Content = "Desativar";
                    toggleBtn.Background = new SolidColorBrush(Color.FromRgb(243, 139, 168));
                    statusIcon.Text = "✓";
                    statusIcon.Foreground = new SolidColorBrush(Color.FromRgb(166, 227, 161));
                }
                else
                {
                    protectionStatus.Text = "Proteção Desativada";
                    protectionStatus.Foreground = new SolidColorBrush(Color.FromRgb(243, 139, 168));
                    toggleBtn.Content = "Ativar";
                    toggleBtn.Background = new SolidColorBrush(Color.FromRgb(166, 227, 161));
                    statusIcon.Text = "⚠";
                    statusIcon.Foreground = new SolidColorBrush(Color.FromRgb(243, 139, 168));
                }
            });
        }

        public void UpdateScanProgress(int progress)
        {
            Dispatcher.Invoke(() =>
            {
                if (scanProgressBar == null || scanProgressText == null || scanStatus == null || startScanBtn == null)
                    return;

                scanProgressBar.Value = progress;
                scanProgressText.Text = $"{progress}%";

                if (progress >= 100)
                {
                    scanStatus.Text = "Verificação concluída";
                    scanStatus.Foreground = new SolidColorBrush(Color.FromRgb(166, 227, 161));
                    startScanBtn.Content = "Nova Verificação";
                    startScanBtn.IsEnabled = true;
                }
                else if (progress > 0)
                {
                    scanStatus.Text = "Verificando sistema...";
                    scanStatus.Foreground = new SolidColorBrush(Color.FromRgb(137, 180, 250));
                    startScanBtn.Content = "Verificando...";
                    startScanBtn.IsEnabled = false;
                }
            });
        }

        public void UpdateStats(int filesScanned, int threatsDetected, int filesProtected)
        {
            Dispatcher.Invoke(() =>
            {
                if (filesScannedText == null || threatsDetectedText == null || filesProtectedText == null)
                    return;

                filesScannedText.Text = filesScanned.ToString("N0");
                threatsDetectedText.Text = threatsDetected.ToString("N0");
                filesProtectedText.Text = filesProtected.ToString("N0");
            });
        }

        public void AddRecentAlert(string alertMessage)
        {
            Dispatcher.Invoke(() =>
            {
                if (alertsStackPanel == null || alertsBadge == null || alertsBadgeText == null)
                    return;

                var alertText = new TextBlock
                {
                    Text = $"• {DateTime.Now:HH:mm} - {alertMessage}",
                    Foreground = new SolidColorBrush(Color.FromRgb(243, 139, 168)),
                    Margin = new Thickness(0, 2, 0, 2),
                    TextWrapping = TextWrapping.Wrap
                };

                alertsStackPanel.Children.Insert(0, alertText);

                if (alertsStackPanel.Children.Count > 5)
                {
                    alertsStackPanel.Children.RemoveAt(alertsStackPanel.Children.Count - 1);
                }

                if (alertsStackPanel.Children.Count > 0)
                {
                    alertsBadge.Visibility = Visibility.Visible;
                    alertsBadgeText.Text = alertsStackPanel.Children.Count.ToString();
                }
            });
        }

        private void ToggleBtn_Click(object sender, RoutedEventArgs e)
        {
            if (toggleBtn == null) return;

            var currentText = toggleBtn.Content.ToString();
            bool newState = currentText == "Ativar";

            ProtectionToggled?.Invoke(this, newState);
        }

        private void StartScanBtn_Click(object sender, RoutedEventArgs e)
        {
            ScanRequested?.Invoke(this, EventArgs.Empty);

            if (scanProgressBar == null || scanProgressText == null || scanStatus == null || startScanBtn == null)
                return;

            scanProgressBar.Value = 0;
            scanProgressText.Text = "0%";
            scanStatus.Text = "Iniciando verificação...";
            scanStatus.Foreground = new SolidColorBrush(Color.FromRgb(137, 180, 250));
            startScanBtn.Content = "Verificando...";
            startScanBtn.IsEnabled = false;
        }

        private void ViewAllAlertsBtn_Click(object sender, RoutedEventArgs e)
        {
            if (alertsBadge == null || alertsBadgeText == null) return;

            alertsBadge.Visibility = Visibility.Collapsed;
            alertsBadgeText.Text = "0";
        }

        private void QuickSettingsBtn_Click(object sender, RoutedEventArgs e)
        {
            var quickSettings = new QuickSettingsPopup();
            quickSettings.Owner = Window.GetWindow(this);

            if (quickSettings.ShowDialog() == true)
            {
                LogMessage?.Invoke(this, "Configurações rápidas aplicadas");

                if (quickSettings.Config.DesktopNotifications)
                {
                    ShowNotification?.Invoke(this, "Configurações atualizadas com sucesso");
                }
            }
        }

        private void UpdateSystemStats()
        {
            var random = new Random();
            UpdateStats(
                random.Next(5000, 15000),
                random.Next(0, 5),
                random.Next(100, 500)
            );
        }

        public void StartLiveUpdates()
        {
            var updateTimer = new DispatcherTimer();
            updateTimer.Interval = TimeSpan.FromSeconds(10);
            updateTimer.Tick += (s, e) => UpdateSystemStats();
            updateTimer.Start();
        }
    }
}