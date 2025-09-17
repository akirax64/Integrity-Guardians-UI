using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace IGAntiRansomwareUI
{
    public partial class RuleEditorPanel : UserControl
    {
        public event EventHandler<RuleManager.RuleStruct> ruleSaved;
        public event EventHandler editCancelled;

        private RuleManager.RuleStruct currentRule;

        public RuleEditorPanel()
        {
            InitializeComponent();
            LoadRuleTypes();
        }

        public void SetRule(RuleManager.RuleStruct rule)
        {
            currentRule = rule;
            DataContext = currentRule;
            UpdateUI();
        }

        private void LoadRuleTypes()
        {
            cmbRuleType.ItemsSource = Enum.GetValues(typeof(RuleManager.RuleType));
        }

        private void UpdateUI()
        {
            if (currentRule == null) return;

            // Atualizar checkboxes baseado nas flags
            chkActive.IsChecked = currentRule.Flags.HasFlag(RuleManager.RuleFlags.Active);
            chkBlock.IsChecked = currentRule.Flags.HasFlag(RuleManager.RuleFlags.Block);
            chkAlertOnly.IsChecked = currentRule.Flags.HasFlag(RuleManager.RuleFlags.AlertOnly);
            chkBackup.IsChecked = currentRule.Flags.HasFlag(RuleManager.RuleFlags.Backup);
        }

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            if (ValidateRule())
            {
                UpdateRuleFromUI();
                ruleSaved?.Invoke(this, currentRule);
            }
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            editCancelled?.Invoke(this, EventArgs.Empty);
        }

        private bool ValidateRule()
        {
            if (string.IsNullOrWhiteSpace(txtRuleName.Text))
            {
                ShowStatus("Nome da regra é obrigatório", false);
                return false;
            }

            if (string.IsNullOrWhiteSpace(txtPattern.Text))
            {
                ShowStatus("Pattern é obrigatório", false);
                return false;
            }

            // Validar tamanhos de arquivo
            if (!uint.TryParse(txtMinFileSize.Text, out uint minSize) ||
                !uint.TryParse(txtMaxFileSize.Text, out uint maxSize))
            {
                ShowStatus("Tamanhos de arquivo devem ser números válidos", false);
                return false;
            }

            if (minSize > maxSize)
            {
                ShowStatus("Tamanho mínimo não pode ser maior que o máximo", false);
                return false;
            }

            return true;
        }

        private void UpdateRuleFromUI()
        {
            if (currentRule == null) return;

            // Atualizar propriedades básicas
            currentRule.Name = txtRuleName.Text;
            currentRule.Pattern = txtPattern.Text;
            currentRule.TargetPath = txtTargetPath.Text;
            currentRule.Description = txtDescription.Text;
            currentRule.Type = (RuleManager.RuleType)cmbRuleType.SelectedItem;

            // Atualizar tamanhos de arquivo
            if (uint.TryParse(txtMinFileSize.Text, out uint minSize))
                currentRule.MinFileSize = minSize;

            if (uint.TryParse(txtMaxFileSize.Text, out uint maxSize))
                currentRule.MaxFileSize = maxSize;

            // Atualizar flags
            UpdateFlagsFromCheckboxes();

            currentRule.Modified = DateTime.Now;

            ShowStatus("Regra salva com sucesso", true);
        }

        private void UpdateFlagsFromCheckboxes()
        {
            currentRule.Flags = RuleManager.RuleFlags.None;

            if (chkActive.IsChecked == true)
                currentRule.Flags |= RuleManager.RuleFlags.Active;

            if (chkBlock.IsChecked == true)
                currentRule.Flags |= RuleManager.RuleFlags.Block;

            if (chkAlertOnly.IsChecked == true)
                currentRule.Flags |= RuleManager.RuleFlags.AlertOnly;

            if (chkBackup.IsChecked == true)
                currentRule.Flags |= RuleManager.RuleFlags.Backup;
        }

        private void ShowStatus(string message, bool isSuccess)
        {
            txtStatus.Text = message;
            txtStatus.Foreground = isSuccess ?
                new SolidColorBrush(Color.FromRgb(166, 227, 161)) : // Verde
                new SolidColorBrush(Color.FromRgb(243, 139, 168));  // Vermelho

            txtStatus.Visibility = Visibility.Visible;

            // Auto-hide após 3 segundos
            var timer = new System.Timers.Timer(3000);
            timer.Elapsed += (s, args) =>
            {
                Dispatcher.Invoke(() => txtStatus.Visibility = Visibility.Collapsed);
                timer.Stop();
            };
            timer.Start();
        }

        // Handlers para validação em tempo real
        private void TxtRuleName_TextChanged(object sender, TextChangedEventArgs e)
        {
            ValidateForm();
        }

        private void TxtPattern_TextChanged(object sender, TextChangedEventArgs e)
        {
            ValidateForm();
        }

        private void TxtFileSize_TextChanged(object sender, TextChangedEventArgs e)
        {
            ValidateForm();
        }

        private void ValidateForm()
        {
            bool hasName = !string.IsNullOrWhiteSpace(txtRuleName.Text);
            bool hasPattern = !string.IsNullOrWhiteSpace(txtPattern.Text);

            bool validSizes = uint.TryParse(txtMinFileSize.Text, out uint minSize) &&
                             uint.TryParse(txtMaxFileSize.Text, out uint maxSize) &&
                             minSize <= maxSize;

            btnSave.IsEnabled = hasName && hasPattern && validSizes;
        }

        // Propriedades para binding mais fácil
        public bool IsActive
        {
            get => currentRule?.Flags.HasFlag(RuleManager.RuleFlags.Active) ?? false;
            set => SetFlag(RuleManager.RuleFlags.Active, value);
        }

        public bool ShouldBlock
        {
            get => currentRule?.Flags.HasFlag(RuleManager.RuleFlags.Block) ?? false;
            set => SetFlag(RuleManager.RuleFlags.Block, value);
        }

        public bool AlertOnly
        {
            get => currentRule?.Flags.HasFlag(RuleManager.RuleFlags.AlertOnly) ?? false;
            set => SetFlag(RuleManager.RuleFlags.AlertOnly, value);
        }

        public bool ShouldBackup
        {
            get => currentRule?.Flags.HasFlag(RuleManager.RuleFlags.Backup) ?? false;
            set => SetFlag(RuleManager.RuleFlags.Backup, value);
        }

        private void SetFlag(RuleManager.RuleFlags flag, bool value)
        {
            if (currentRule == null) return;

            if (value)
                currentRule.Flags |= flag;
            else
                currentRule.Flags &= ~flag;
        }
    }
}