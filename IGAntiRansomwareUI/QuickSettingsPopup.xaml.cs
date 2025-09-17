using System;
using System.Windows;
using System.Windows.Controls;

namespace IGAntiRansomwareUI
{
    public partial class QuickSettingsPopup : Window
    {
        public QuickSettingsConfig Config { get; private set; }

        public QuickSettingsPopup()
        {
            InitializeComponent();
            Owner = Application.Current.MainWindow;
            WindowStartupLocation = WindowStartupLocation.CenterOwner;
            Config = new QuickSettingsConfig();

            Loaded += QuickSettingsPopup_Loaded;
        }

        private void QuickSettingsPopup_Loaded(object sender, RoutedEventArgs e)
        {
            // Configurar eventos
            sliderMaxFileSize.ValueChanged += SliderMaxFileSize_ValueChanged;
            sliderMaxBackups.ValueChanged += SliderMaxBackups_ValueChanged;

            // Carregar configurações atuais
            LoadCurrentSettings();
        }

        private void LoadCurrentSettings()
        {
            // Aqui você carregaria as configurações atuais do aplicativo
            // Por enquanto, vamos usar valores padrão

            cmbDetectionMode.SelectedIndex = 0;
            sliderMaxFileSize.Value = 100;
            chkAutoBackup.IsChecked = true;
            chkBackupEncrypted.IsChecked = false;
            chkLimitBackups.IsChecked = true;
            sliderMaxBackups.Value = 3;
            chkDetailedLogs.IsChecked = true;
            chkLogToFile.IsChecked = true;
            chkAutoClearLogs.IsChecked = false;
            chkDesktopNotifications.IsChecked = true;
            chkSoundAlert.IsChecked = true;
        }

        private void SliderMaxFileSize_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            txtMaxFileSize.Text = $"{(int)sliderMaxFileSize.Value} MB";
        }

        private void SliderMaxBackups_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            txtMaxBackups.Text = $"{(int)sliderMaxBackups.Value}";
        }

        private void SaveSettings_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Salvar configurações no objeto
                Config.DetectionMode = cmbDetectionMode.SelectedIndex;
                Config.MaxFileSizeMB = (int)sliderMaxFileSize.Value;
                Config.AutoBackup = chkAutoBackup.IsChecked ?? false;
                Config.BackupEncrypted = chkBackupEncrypted.IsChecked ?? false;
                Config.LimitBackups = chkLimitBackups.IsChecked ?? false;
                Config.MaxBackups = (int)sliderMaxBackups.Value;
                Config.DetailedLogs = chkDetailedLogs.IsChecked ?? false;
                Config.LogToFile = chkLogToFile.IsChecked ?? false;
                Config.AutoClearLogs = chkAutoClearLogs.IsChecked ?? false;
                Config.DesktopNotifications = chkDesktopNotifications.IsChecked ?? false;
                Config.SoundAlert = chkSoundAlert.IsChecked ?? false;

                // Aqui você aplicaria as configurações ao sistema
                ApplySettings(Config);

                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erro ao salvar configurações: {ex.Message}", "Erro",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ApplySettings(QuickSettingsConfig config)
        {
            try
            {
                // Aplicar ao driver (se conectado)
                if (Application.Current.MainWindow is MainWindow mainWindow)
                {
                    if (!mainWindow.DriverHandle.IsInvalid)
                    {
                        try
                        {
                            uint detectionMode = config.DetectionMode switch
                            {
                                0 => 1, // Ativo
                                1 => 0, // Passivo
                                2 => 2, // Monitoramento
                                _ => 1  // Padrão: Ativo
                            };

                            DriverCommunication.SetDriverMonitoring(
                                mainWindow.DriverHandle,
                                true,
                                detectionMode,
                                config.AutoBackup
                            );

                            // Disparar evento através do MainWindow
                            mainWindow.AddLog("Configurações do driver atualizadas");
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show($"Erro ao aplicar configurações no driver: {ex.Message}",
                                "Aviso", MessageBoxButton.OK, MessageBoxImage.Warning);

                            mainWindow.AddLog($"Erro no driver: {ex.Message}");
                        }
                    }
                    else
                    {
                        mainWindow.AddLog("Driver não conectado - configurações salvas localmente");
                    }
                }

                // Salvar configurações em arquivo
                SaveConfigToFile(config);

                MessageBox.Show("Configurações aplicadas com sucesso!", "Sucesso",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erro ao aplicar configurações: {ex.Message}", "Erro",
                    MessageBoxButton.OK, MessageBoxImage.Error);

                if (Application.Current.MainWindow is MainWindow mainWin)
                {
                    mainWin.AddLog($"Erro nas configurações: {ex.Message}");
                }
            }
        }

        private void SaveConfigToFile(QuickSettingsConfig config)
        {
            try
            {
                string configDir = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "config");
                System.IO.Directory.CreateDirectory(configDir);

                string configPath = System.IO.Path.Combine(configDir, "quick_settings.json");
                string json = System.Text.Json.JsonSerializer.Serialize(config,
                    new System.Text.Json.JsonSerializerOptions { WriteIndented = true });

                System.IO.File.WriteAllText(configPath, json);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro ao salvar configurações: {ex.Message}");
            }
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        // Classe para armazenar as configurações
        public class QuickSettingsConfig
        {
            public int DetectionMode { get; set; } // 0=Passivo, 1=Ativo, 2=Monitoramento
            public int MaxFileSizeMB { get; set; }
            public bool AutoBackup { get; set; }
            public bool BackupEncrypted { get; set; }
            public bool LimitBackups { get; set; }
            public int MaxBackups { get; set; }
            public bool DetailedLogs { get; set; }
            public bool LogToFile { get; set; }
            public bool AutoClearLogs { get; set; }
            public bool DesktopNotifications { get; set; }
            public bool SoundAlert { get; set; }

            public QuickSettingsConfig()
            {
                // Valores padrão
                DetectionMode = 1; // Ativo
                MaxFileSizeMB = 100;
                AutoBackup = true;
                BackupEncrypted = false;
                LimitBackups = true;
                MaxBackups = 3;
                DetailedLogs = true;
                LogToFile = true;
                AutoClearLogs = false;
                DesktopNotifications = true;
                SoundAlert = true;
            }
        }
    }
}   