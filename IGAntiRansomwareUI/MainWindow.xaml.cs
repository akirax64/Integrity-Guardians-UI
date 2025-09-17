using Microsoft.Win32;
using Microsoft.Win32.SafeHandles;
using System;
using System.Collections.Generic;
using System.IO;
using System.Collections.ObjectModel;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;

namespace IGAntiRansomwareUI
{
    public partial class MainWindow : Window
    {
        private SafeFileHandle driverHandle = new SafeFileHandle(IntPtr.Zero, true);
        private CancellationTokenSource alertCancellationTokenSource;
        private RuleManager ruleManager;
        public ObservableCollection<Logs.LogEntry> LogEntries { get; set; } = new ObservableCollection<Logs.LogEntry>();

        // Painéis da interface
        private DashboardPanel dashboardPanel;
        private RulesPanel rulesPanel;
        private ProtectionPanel protectionPanel;
        //private BlacklistPanel blacklistPanel;
        private Logs logs;
      //  private SettingsPanel settingsPanel;

        public ObservableCollection<string> Logs { get; set; }
        public ObservableCollection<string> Notifications { get; set; }
        public ObservableCollection<string> Blacklist { get; set; }

        private bool protectionEnabled;
        private bool scanning = false;
        private int scanProgress = 0;
        private string currentPage = "dashboard";

        public MainWindow()
        {
            InitializeComponent();
            InitializeApplication();
        }

        private void InitializeApplication()
        {
            Logs = new ObservableCollection<string>();
            Notifications = new ObservableCollection<string>();
            Blacklist = new ObservableCollection<string> { "fabio.exe", "randark.exe" };

            ruleManager = new RuleManager();

            // Inicializar painéis
            InitializePanels();

            LoadBlacklist();

            AddLog("Aplicação iniciada");
            AddLog("Proteção anti-ransomware ativada");
            AddLog($"Blacklist carregada com {Blacklist.Count} itens");

            ShowDashboard();
        }

        private void InitializePanels()
        {
            dashboardPanel = new DashboardPanel();
            dashboardPanel.ProtectionToggled += (s, enabled) => ToggleProtection();
            dashboardPanel.ScanRequested += (s, e) => StartScan();

            rulesPanel = new RulesPanel();
            rulesPanel.BackRequested += (s, e) => ShowDashboard();

            protectionPanel = new ProtectionPanel();
           // blacklistPanel = new BlacklistPanel();
            logs = new Logs();
            //settingsPanel = new SettingsPanel();

            // Configurar eventos dos painéis se necessário
        }

        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            await ConnectDriver();
        }

        private void Window_Unloaded(object sender, RoutedEventArgs e)
        {
            if (driverHandle != null && !driverHandle.IsInvalid)
            {
                driverHandle.Close();
            }
        }

        #region Navegação entre Painéis

        private void ShowDashboard_Click(object sender, RoutedEventArgs e)
        {
            ShowDashboard();
        }

        private void ShowProtection_Click(object sender, RoutedEventArgs e)
        {
            ShowProtection();
        }

        private void ShowBlacklist_Click(object sender, RoutedEventArgs e)
        {
            ShowBlacklist();
        }

        private void ShowLogs_Click(object sender, RoutedEventArgs e)
        {
            ShowLogs();
        }

        private void ShowSettings_Click(object sender, RoutedEventArgs e)
        {
            ShowSettings();
        }

        private void ShowDashboard()
        {
            SetActivePanel(dashboardPanel);
            currentPage = "dashboard";
            dashboardPanel.UpdateStatus(protectionEnabled);
        }

        private void ShowProtection()
        {
            SetActivePanel(protectionPanel);
            currentPage = "protection";
        }

        private void ShowBlacklist()
        {
        //    SetActivePanel(blacklistPanel);
            currentPage = "blacklist";
        //    blacklistPanel.LoadBlacklist(Blacklist);
        }

        public void ShowLogs()
        {
            SetActivePanel(logs);
            currentPage = "logs";

            // Certifique-se de que logsPanel foi inicializado
            if (logs == null)
            {
                logs = new Logs();
                logs.BackRequested += (s, e) => ShowDashboard();
            }

            logs.LoadLogs(LogEntries); // ← Agora passa a coleção correta
        }


        private void ShowSettings()
        {
        //    SetActivePanel(settingsPanel);
            currentPage = "settings";
        }

        private void ShowRules()
        {
            SetActivePanel(rulesPanel);
            currentPage = "rules";
        }

        private void SetActivePanel(UserControl panel)
        {
            MainContent.Content = panel;
        }

        #endregion

        #region Comunicação com Driver

        private async Task ConnectDriver()
        {
            try
            {
                driverHandle = await Task.Run(() =>
                {
                    return DriverCommunication.CreateFile(
                        DriverCommunication.devicePath,
                        DriverCommunication.GENERIC_READ | DriverCommunication.GENERIC_WRITE,
                        0,
                        IntPtr.Zero,
                        DriverCommunication.OPEN_EXISTING,
                        0,
                        IntPtr.Zero);
                });

                if (driverHandle.IsInvalid)
                {
                    AddLog("Status: Desconectado do Driver");
                    UpdateStatusDisplay(false, "Desconectado");
                }
                else
                {
                    AddLog("Status: Conectado ao Driver");
                    await UpdateStatusDisplay();
                }
            }
            catch (Exception ex)
            {
                AddLog($"Erro ao conectar ao driver: {ex.Message}");
                UpdateStatusDisplay(false, "Erro de Conexão");
            }
        }

        private async Task UpdateStatusDisplay()
        {
            if (driverHandle.IsInvalid)
            {
                UpdateStatusDisplay(false, "Desconectado");
                return;
            }

            try
            {
                var statusInfo = await Task.Run(() => DriverCommunication.GetDriverStatus(driverHandle));
                protectionEnabled = statusInfo.EnableMonitoring;

                UpdateStatusDisplay(protectionEnabled,
                    protectionEnabled ? "Protegido" : "Desprotegido");
            }
            catch (Exception ex)
            {
                AddLog($"Erro ao obter status do driver: {ex.Message}");
                UpdateStatusDisplay(false, "Erro");
            }
        }

        private void UpdateStatusDisplay(bool isProtected, string statusText)
        {
            Dispatcher.Invoke(() =>
            {
                StatusText.Text = statusText;

                if (isProtected)
                {
                    IconText.Foreground = Brushes.Green;
                    StatusText.Foreground = Brushes.Green;
                }
                else
                {
                    IconText.Foreground = Brushes.OrangeRed;
                    StatusText.Foreground = Brushes.OrangeRed;
                }

                // Atualizar dashboard se estiver visível
                if (currentPage == "dashboard")
                {
                    dashboardPanel.UpdateStatus(isProtected);
                }
            });
        }

        #endregion

        #region Funcionalidades da Aplicação

        public void AddLog(string message, string logType = "Info")
        {
            Dispatcher.Invoke(() =>
            {
                // 1. Primeiro adiciona à coleção de strings original (Logs)
                string timestamp = DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss");
                string logMessage = $"[{timestamp}] [{logType}] {message}";
                Logs.Insert(0, logMessage);

                if (Logs.Count > 200)
                    Logs.RemoveAt(Logs.Count - 1);

                // 2. Agora cria e adiciona à nova coleção estruturada (LogEntries)
                var logEntry = new Logs.LogEntry
                {
                    Timestamp = DateTime.Now.ToString("HH:mm:ss"),
                    LogType = logType,
                    Message = message
                };

                LogEntries.Insert(0, logEntry);
                if (LogEntries.Count > 1000)
                {
                    LogEntries.RemoveAt(LogEntries.Count - 1);
                }

                // 3. Atualiza o LogsPanel se estiver visível
                if (currentPage == "logs" && logs != null)
                {
                    logs.AddLog(logEntry);
                }
            });
        }

        public void AddNotification(string message)
        {
            Dispatcher.Invoke(() =>
            {
                string timestamp = DateTime.Now.ToString("HH:mm");
                Notifications.Insert(0, $"[{timestamp}] {message}");

                if (Notifications.Count > 50)
                    Notifications.RemoveAt(Notifications.Count - 1);
            });
        }

        private void LoadBlacklist()
        {
            try
            {
                string configDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "config");
                string blacklistPath = Path.Combine(configDir, "blacklist.json");

                if (File.Exists(blacklistPath))
                {
                    string json = File.ReadAllText(blacklistPath);
                    var loadedBlacklist = JsonSerializer.Deserialize<List<string>>(json);

                    foreach (var item in new List<string> { "fabio.exe", "randark.exe" })
                    {
                        if (!loadedBlacklist.Contains(item))
                            loadedBlacklist.Add(item);
                    }

                    Blacklist = new ObservableCollection<string>(loadedBlacklist);
                }
            }
            catch (Exception ex)
            {
                AddLog($"Erro ao carregar blacklist: {ex.Message}");
            }
        }

        public void ToggleProtection()
        {
            protectionEnabled = !protectionEnabled;

            try
            {
                DriverCommunication.SetDriverMonitoring(driverHandle, protectionEnabled, 1, true);

                if (protectionEnabled)
                {
                    AddLog("Proteção ativada");
                    AddNotification("Proteção ativada");
                    UpdateStatusDisplay(true, "Protegido");
                }
                else
                {
                    AddLog("Proteção desativada");
                    AddNotification("Proteção desativada - sistema vulnerável");
                    UpdateStatusDisplay(false, "Desprotegido");
                }
            }
            catch (Exception ex)
            {
                AddLog($"Erro ao alternar proteção: {ex.Message}");
                protectionEnabled = !protectionEnabled;
            }
        }

        public void StartScan()
        {
            if (!scanning)
            {
                scanning = true;
                scanProgress = 0;
                AddLog("Iniciando verificação do sistema");
                AddNotification("Verificação iniciada");

                Thread scanThread = new Thread(ScanThread);
                scanThread.IsBackground = true;
                scanThread.Start();
            }
        }

        private void ScanThread()
        {
            try
            {
                AddLog("Coletando informações do sistema...");

                var dirsToScan = new List<string>
                {
                    Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                    Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
                    Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) + "\\Downloads",
                    Path.GetTempPath()
                };

                // Implementar lógica de verificação aqui
                for (int i = 0; i <= 100; i += 10)
                {
                    scanProgress = i;
                    Thread.Sleep(500);
                }

                scanProgress = 100;
                scanning = false;
                AddLog("Verificação concluída");
                AddNotification("Verificação concluída");
            }
            catch (Exception ex)
            {
                AddLog($"Erro durante verificação: {ex.Message}");
                scanning = false;
                scanProgress = 100;
            }
        }

        #endregion

        #region Propriedades Públicas

        public bool IsProtectionEnabled => protectionEnabled;
        public bool IsScanning => scanning;
        public int ScanProgress => scanProgress;
        public RuleManager RuleManager => ruleManager;
        public SafeFileHandle DriverHandle => driverHandle;

        #endregion

        private void ShowRules_Click(object sender, RoutedEventArgs e)
        {
            ShowRules();
        }
    }
}