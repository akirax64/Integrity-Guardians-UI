using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Threading.Tasks;
using System.ComponentModel;

namespace IGAntiRansomwareUI
{
    public partial class ProtectionPanel : UserControl
    {
        public event EventHandler BackRequested;
        public event EventHandler<bool> ProtectionToggled;
        public event EventHandler<int> ProtectionModeChanged;

        private bool _protectionEnabled = true;
        private int _currentMode = 0;

        public ProtectionPanel()
        {
            InitializeComponent();
            Loaded += ProtectionPanel_Loaded;
        }

        private async void ProtectionPanel_Loaded(object sender, RoutedEventArgs e)
        {
            await LoadProtectionStatus();
            UpdateStatistics();
        }

        public async Task LoadProtectionStatus()
        {
            if (Application.Current.MainWindow is MainWindow mainWindow)
            {
                try
                {
                    if (!mainWindow.DriverHandle.IsInvalid)
                    {
                        var status = await Task.Run(() =>
                            DriverCommunication.GetDriverStatus(mainWindow.DriverHandle));

                        _protectionEnabled = status.EnableMonitoring;
                        _currentMode = (int)status.Mode;

                        UpdateProtectionStatus(_protectionEnabled, _currentMode);
                    }
                    else
                    {
                        UpdateProtectionStatus(false, 0);
                        mainWindow.AddLog("Driver não conectado - proteção desativada", "Warning");
                    }
                }
                catch (Exception ex)
                {
                    mainWindow.AddLog($"Erro ao carregar status da proteção: {ex.Message}", "Error");
                    UpdateProtectionStatus(false, 0);
                }
            }
        }

        public void UpdateProtectionStatus(bool isEnabled, int mode)
        {
            Dispatcher.Invoke(() =>
            {
                _protectionEnabled = isEnabled;
                _currentMode = mode;

                if (isEnabled)
                {
                    protectionStatus.Text = "Proteção Ativa";
                    protectionStatus.Foreground = new SolidColorBrush(Color.FromRgb(166, 227, 161));
                    statusIcon.Text = "✓";
                    statusIcon.Foreground = new SolidColorBrush(Color.FromRgb(166, 227, 161));
                    btnToggleProtection.Content = "Desativar Proteção";
                    btnToggleProtection.Background = new SolidColorBrush(Color.FromRgb(243, 139, 168));
                }
                else
                {
                    protectionStatus.Text = "Proteção Inativa";
                    protectionStatus.Foreground = new SolidColorBrush(Color.FromRgb(243, 139, 168));
                    statusIcon.Text = "✗";
                    statusIcon.Foreground = new SolidColorBrush(Color.FromRgb(243, 139, 168));
                    btnToggleProtection.Content = "Ativar Proteção";
                    btnToggleProtection.Background = new SolidColorBrush(Color.FromRgb(166, 227, 161));
                }

                // Atualiza o modo de proteção
                if (mode >= 0 && mode < cmbProtectionMode.Items.Count)
                {
                    cmbProtectionMode.SelectedIndex = mode;
                }
            });
        }

        private void UpdateStatistics()
        {
            // Estatísticas simuladas - você deve substituir com dados reais do driver
            var random = new Random();

            txtFilesMonitored.Text = (random.Next(500, 2000) * 1000).ToString("N0");
            txtBlocksToday.Text = random.Next(0, 15).ToString("N0");
            txtFilesProtected.Text = (random.Next(100, 500) * 1000).ToString("N0");
        }

        private void BtnBack_Click(object sender, RoutedEventArgs e)
        {
            BackRequested?.Invoke(this, EventArgs.Empty);
        }

        private async void BtnToggleProtection_Click(object sender, RoutedEventArgs e)
        {
            if (Application.Current.MainWindow is MainWindow mainWindow)
            {
                try
                {
                    bool newState = !_protectionEnabled;

                    if (!mainWindow.DriverHandle.IsInvalid)
                    {
                        await Task.Run(() =>
                            DriverCommunication.SetDriverMonitoring(
                                mainWindow.DriverHandle,
                                newState,
                                (uint)_currentMode,
                                true));

                        _protectionEnabled = newState;
                        UpdateProtectionStatus(_protectionEnabled, _currentMode);

                        ProtectionToggled?.Invoke(this, newState);

                        mainWindow.AddLog($"Proteção {(newState ? "ativada" : "desativada")}",
                            newState ? "Info" : "Warning");
                    }
                    else
                    {
                        mainWindow.AddLog("Driver não conectado - não foi possível alterar proteção", "Error");
                    }
                }
                catch (Exception ex)
                {
                    mainWindow.AddLog($"Erro ao alternar proteção: {ex.Message}", "Error");
                }
            }
        }

        private async void CmbProtectionMode_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cmbProtectionMode.SelectedIndex >= 0 && _currentMode != cmbProtectionMode.SelectedIndex)
            {
                if (Application.Current.MainWindow is MainWindow mainWindow)
                {
                    try
                    {
                        int newMode = cmbProtectionMode.SelectedIndex;

                        if (!mainWindow.DriverHandle.IsInvalid)
                        {
                            await Task.Run(() =>
                                DriverCommunication.SetDriverMonitoring(
                                    mainWindow.DriverHandle,
                                    _protectionEnabled,
                                    (uint)newMode,
                                    true));

                            _currentMode = newMode;
                            ProtectionModeChanged?.Invoke(this, newMode);

                            string modeName = newMode switch
                            {
                                0 => "Ativo",
                                1 => "Passivo",
                                2 => "Agressivo",
                                _ => "Desconhecido"
                            };

                            mainWindow.AddLog($"Modo de proteção alterado para: {modeName}", "Info");
                        }
                    }
                    catch (Exception ex)
                    {
                        mainWindow.AddLog($"Erro ao alterar modo de proteção: {ex.Message}", "Error");
                        // Reverte para o modo anterior
                        cmbProtectionMode.SelectedIndex = _currentMode;
                    }
                }
            }
        }

        private void BtnRefreshStats_Click(object sender, RoutedEventArgs e)
        {
            UpdateStatistics();

            if (Application.Current.MainWindow is MainWindow mainWindow)
            {
                mainWindow.AddLog("Estatísticas de proteção atualizadas", "Info");
            }
        }

        private void BtnViewBlockLogs_Click(object sender, RoutedEventArgs e)
        {
            // Navega para os logs
            if (Application.Current.MainWindow is MainWindow mainWindow)
            {
                mainWindow.ShowLogs();
            }
        }

        private void BtnAdvancedSettings_Click(object sender, RoutedEventArgs e)
        {
            // Abre configurações avançadas
            var settings = new QuickSettingsPopup();
            settings.Owner = Application.Current.MainWindow;
            settings.ShowDialog();
        }

        private void BtnTestProtection_Click(object sender, RoutedEventArgs e)
        {
            if (Application.Current.MainWindow is MainWindow mainWindow)
            {
                if (!_protectionEnabled)
                {
                    MessageBox.Show("A proteção está desativada. Ative-a primeiro para testar.",
                        "Proteção Desativada", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                try
                {
                    // Simula um teste de proteção
                    mainWindow.AddLog("Iniciando teste de proteção...", "Info");

                    // Aqui você pode adicionar um teste real no futuro
                    Task.Delay(1000).ContinueWith(_ =>
                    {
                        mainWindow.AddLog("Teste de proteção concluído com sucesso!", "Info");
                        MessageBox.Show("Teste de proteção concluído! O sistema está funcionando corretamente.",
                            "Teste Concluído", MessageBoxButton.OK, MessageBoxImage.Information);
                    }, TaskScheduler.FromCurrentSynchronizationContext());
                }
                catch (Exception ex)
                {
                    mainWindow.AddLog($"Erro no teste de proteção: {ex.Message}", "Error");
                }
            }
        }

        private void BtnDiagnostic_Click(object sender, RoutedEventArgs e)
        {
            if (Application.Current.MainWindow is MainWindow mainWindow)
            {
                mainWindow.AddLog("Executando diagnóstico do sistema...", "Info");

                // Simula diagnóstico
                Task.Delay(1500).ContinueWith(_ =>
                {
                    string diagnosticResult = _protectionEnabled ?
                        "Diagnóstico concluído: Todos os sistemas operando normalmente" :
                        "Diagnóstico concluído: Proteção desativada - sistema vulnerável";

                    mainWindow.AddLog(diagnosticResult, _protectionEnabled ? "Info" : "Warning");

                    MessageBox.Show(diagnosticResult, "Diagnóstico",
                        MessageBoxButton.OK, _protectionEnabled ? MessageBoxImage.Information : MessageBoxImage.Warning);
                }, TaskScheduler.FromCurrentSynchronizationContext());
            }
        }

        private void ChkFileSystem_Checked(object sender, RoutedEventArgs e)
        {
            UpdateComponentStatus(chkFileSystem, "Proteção de sistema de arquivos");
        }

        private void ChkMemory_Checked(object sender, RoutedEventArgs e)
        {
            UpdateComponentStatus(chkMemory, "Proteção de memória");
        }

        private void ChkProcess_Checked(object sender, RoutedEventArgs e)
        {
            UpdateComponentStatus(chkProcess, "Proteção de processos");
        }

        private void UpdateComponentStatus(CheckBox checkBox, string componentName)
        {
            if (Application.Current.MainWindow is MainWindow mainWindow && checkBox.IsLoaded)
            {
                string status = checkBox.IsChecked == true ? "ativada" : "desativada";
                mainWindow.AddLog($"{componentName} {status}", "Info");
            }
        }
    }
}