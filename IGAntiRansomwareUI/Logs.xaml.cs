using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using System.Windows.Media;

namespace IGAntiRansomwareUI
{
    public partial class Logs : UserControl
    {
        public event EventHandler BackRequested;

        private ObservableCollection<LogEntry> _currentLogs;
        private bool _autoScroll = true;
        private bool _autoRefresh = true;

        public Logs()
        {
            InitializeComponent();
            InitializeLogs();
        }

        private void InitializeLogs()
        {
            _currentLogs = new ObservableCollection<LogEntry>();
            lstLogs.ItemsSource = _currentLogs;
            UpdateLogCount();
        }

        public void LoadLogs(ObservableCollection<LogEntry> logs)
        {
            if (logs == null) return;

            Dispatcher.Invoke(() =>
            {
                _currentLogs.Clear();
                foreach (var log in logs)
                {
                    _currentLogs.Add(log);
                }

                if (_autoScroll)
                {
                    ScrollToBottom();
                }

                UpdateLogCount();
            });
        }

        public void AddLog(LogEntry logEntry)
        {
            Dispatcher.Invoke(() =>
            {
                _currentLogs.Insert(0, logEntry);

                // Mantém um limite máximo de logs para performance
                if (_currentLogs.Count > 1000)
                {
                    _currentLogs.RemoveAt(_currentLogs.Count - 1);
                }

                if (_autoScroll)
                {
                    ScrollToBottom();
                }

                UpdateLogCount();
            });
        }

        private void ScrollToBottom()
        {
            if (lstLogs.Items.Count > 0)
            {
                lstLogs.ScrollIntoView(lstLogs.Items[0]);
            }
        }

        private void UpdateLogCount()
        {
            txtLogCount.Text = $"{_currentLogs.Count} logs";

            // Atualiza status do auto-scroll
            txtAutoScrollStatus.Text = _autoScroll ? " | Auto-scroll: Ativo" : " | Auto-scroll: Inativo";
            txtAutoScrollStatus.Foreground = _autoScroll ?
                new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(166, 227, 161)) : // Verde
                new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(243, 139, 168));  // Vermelho
        }

        private void BtnBack_Click(object sender, RoutedEventArgs e)
        {
            BackRequested?.Invoke(this, EventArgs.Empty);
        }

        private void BtnClearLogs_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show("Tem certeza que deseja limpar todos os logs?",
                "Confirmar Limpeza", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                _currentLogs.Clear();
                UpdateLogCount();

                // Limpa também os logs do MainWindow se disponível
                if (Application.Current.MainWindow is MainWindow mainWindow)
                {
                    mainWindow.LogEntries.Clear();
                    mainWindow.Logs.Clear();
                }
            }
        }

        private void BtnExportLogs_Click(object sender, RoutedEventArgs e)
        {
            var saveFileDialog = new SaveFileDialog
            {
                Filter = "Arquivo de Texto (*.txt)|*.txt|Arquivo CSV (*.csv)|*.csv",
                FileName = $"logs_antiransomware_{DateTime.Now:yyyyMMdd_HHmmss}",
                Title = "Exportar Logs"
            };

            if (saveFileDialog.ShowDialog() == true)
            {
                try
                {
                    using (var writer = new StreamWriter(saveFileDialog.FileName))
                    {
                        writer.WriteLine("Timestamp;Tipo;Mensagem");

                        // Exporta em ordem cronológica (do mais antigo para o mais recente)
                        for (int i = _currentLogs.Count - 1; i >= 0; i--)
                        {
                            var log = _currentLogs[i];
                            writer.WriteLine($"{log.Timestamp};{log.LogType};{EscapeCsvField(log.Message)}");
                        }
                    }

                    MessageBox.Show("Logs exportados com sucesso!", "Exportação Concluída",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Erro ao exportar logs: {ex.Message}", "Erro",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private string EscapeCsvField(string field)
        {
            if (string.IsNullOrEmpty(field))
                return field;

            if (field.Contains(";") || field.Contains("\"") || field.Contains("\n") || field.Contains("\r"))
            {
                return $"\"{field.Replace("\"", "\"\"")}\"";
            }

            return field;
        }

        private void BtnRefresh_Click(object sender, RoutedEventArgs e)
        {
            // Atualiza a partir do MainWindow se disponível
            if (Application.Current.MainWindow is MainWindow mainWindow)
            {
                LoadLogs(mainWindow.LogEntries);
            }
        }

        private void CmbLogType_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ApplyFilters();
        }

        private void ApplyFilters()
        {
            if (Application.Current.MainWindow is not MainWindow mainWindow)
                return;

            var filteredLogs = new ObservableCollection<LogEntry>();
            var selectedFilter = cmbLogType.SelectedIndex;

            foreach (var log in mainWindow.LogEntries)
            {
                if (selectedFilter == 0) // Todos
                {
                    filteredLogs.Add(log);
                }
                else if (selectedFilter == 1 && log.LogType == "Info") // Info
                {
                    filteredLogs.Add(log);
                }
                else if (selectedFilter == 2 && log.LogType == "Warning") // Avisos
                {
                    filteredLogs.Add(log);
                }
                else if (selectedFilter == 3 && log.LogType == "Error") // Erros
                {
                    filteredLogs.Add(log);
                }
                else if (selectedFilter == 4 && log.LogType == "Alert") // Alertas
                {
                    filteredLogs.Add(log);
                }
            }

            LoadLogs(filteredLogs);
        }

        private void ChkAutoScroll_Checked(object sender, RoutedEventArgs e)
        {
            _autoScroll = chkAutoScroll.IsChecked == true;
            UpdateLogCount();

            if (_autoScroll)
            {
                ScrollToBottom();
            }
        }

        private void ChkAutoRefresh_Checked(object sender, RoutedEventArgs e)
        {
            _autoRefresh = chkAutoRefresh.IsChecked == true;

            // Aqui você pode implementar um timer para atualização automática
            // se necessário no futuro
        }

        private void LstLogs_ScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            // Atualiza o auto-scroll baseado na posição do scroll
            if (e.VerticalChange != 0)
            {
                var scrollViewer = e.OriginalSource as ScrollViewer;
                if (scrollViewer != null)
                {
                    // Se o usuário scrollou manualmente para cima, desativa auto-scroll
                    if (scrollViewer.VerticalOffset != scrollViewer.ScrollableHeight)
                    {
                        _autoScroll = false;
                        chkAutoScroll.IsChecked = false;
                        UpdateLogCount();
                    }
                    // Se chegou no final, reativa auto-scroll
                    else if (scrollViewer.VerticalOffset == scrollViewer.ScrollableHeight)
                    {
                        _autoScroll = true;
                        chkAutoScroll.IsChecked = true;
                        UpdateLogCount();
                    }
                }
            }
        }

        // Classe LogEntry para representar cada entrada de log
        public class LogEntry
        {
            public string Timestamp { get; set; }
            public string LogType { get; set; }
            public string Message { get; set; }

            public System.Windows.Media.Brush LogTypeColor
            {
                get
                {
                    return LogType switch
                    {
                        "Error" => new SolidColorBrush(
                            Color.FromRgb(243, 139, 168)),
                        "Warning" => new SolidColorBrush(
                            Color.FromRgb(250, 179, 135)),
                        "Alert" => new SolidColorBrush(
                            Color.FromRgb(166, 227, 161)),
                        "Info" => new SolidColorBrush(
                            Color.FromRgb(137, 180, 250)),
                        _ => new SolidColorBrush(
                            Color.FromRgb(180, 190, 254))
                    };
                }
            }
        }
    }
}