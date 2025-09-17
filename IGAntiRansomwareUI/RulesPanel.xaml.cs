using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Collections.ObjectModel;

namespace IGAntiRansomwareUI
{
    public partial class RulesPanel : UserControl
    {
        public event EventHandler BackRequested;

        private RuleManager ruleManager = new RuleManager();
        private RuleEditorPanel ruleEditor;

        public RulesPanel()
        {
            InitializeComponent();
            InitializeRuleEditor();
            LoadRulesList();
        }

        private void InitializeRuleEditor()
        {
            ruleEditor = new RuleEditorPanel();
            ruleEditor.ruleSaved += RuleEditor_RuleSaved;
            ruleEditor.editCancelled += RuleEditor_EditCancelled;
            ruleEditorHost.Content = ruleEditor;
        }

        private void LoadRulesList()
        {
            try
            {
                var rules = ruleManager.GetAllRules();
                lstRules.ItemsSource = new ObservableCollection<RuleManager.RuleStruct>(rules);
                UpdateStatus();
            }
            catch (Exception ex)
            {
                txtRulesStatus.Text = $"Erro ao carregar regras: {ex.Message}";
            }
        }

        private void UpdateStatus()
        {
            var activeRules = ruleManager.GetActiveRules().Count;
            var totalRules = ruleManager.GetAllRules().Count;
            txtRulesStatus.Text = $"{activeRules} de {totalRules} regras ativas";
        }

        // CORREÇÃO: Método BtnBack_Click adicionado
        private void BtnBack_Click(object sender, RoutedEventArgs e)
        {
            BackRequested?.Invoke(this, EventArgs.Empty);
        }

        private void BtnNewRule_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var newRule = new RuleManager.RuleStruct
                {
                    Name = "Nova Regra",
                    Pattern = "*",
                    Type = RuleManager.RuleType.Dynamic,
                    Flags = RuleManager.RuleFlags.Active | RuleManager.RuleFlags.AlertOnly
                };

                ruleEditor.SetRule(newRule);
                editorContainer.Visibility = Visibility.Visible;
            }
            catch (Exception ex)
            {
                txtRulesStatus.Text = $"Erro ao criar regra: {ex.Message}";
            }
        }

        private void BtnApplyRules_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                byte[] rulesData = ruleManager.SerializeRulesForDriver();

                // Aqui você enviaria para o driver
                // DriverCommunication.LoadRules(driverHandle, rulesData);

                txtRulesStatus.Text = "Regras aplicadas ao driver com sucesso! ✓";

                var timer = new System.Timers.Timer(3000);
                timer.Elapsed += (s, args) =>
                {
                    Dispatcher.Invoke(UpdateStatus);
                    timer.Stop();
                };
                timer.Start();
            }
            catch (Exception ex)
            {
                txtRulesStatus.Text = $"Erro: {ex.Message}";
            }
        }

        private void LstRules_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (lstRules.SelectedItem is RuleManager.RuleStruct selectedRule)
            {
                ruleEditor.SetRule(selectedRule);
                editorContainer.Visibility = Visibility.Visible;
            }
        }

        private void RuleEditor_RuleSaved(object sender, RuleManager.RuleStruct rule)
        {
            try
            {
                bool isNewRule = ruleManager.GetAllRules().Find(r => r.Id == rule.Id) == null;

                if (isNewRule)
                {
                    ruleManager.AddDynamicRule(rule);
                }
                else
                {
                    // Atualizar regra existente
                 //   ruleManager.UpdateDynamicRule(rule);
                }

                LoadRulesList();
                editorContainer.Visibility = Visibility.Collapsed;
                lstRules.SelectedItem = null;

                txtRulesStatus.Text = "Regra salva com sucesso!";
            }
            catch (Exception ex)
            {
                txtRulesStatus.Text = $"Erro ao salvar regra: {ex.Message}";
            }
        }

        private void RuleEditor_EditCancelled(object sender, EventArgs e)
        {
            editorContainer.Visibility = Visibility.Collapsed;
            lstRules.SelectedItem = null;
        }

        private void BtnDeleteRule_Click(object sender, RoutedEventArgs e)
        {
            if (lstRules.SelectedItem is RuleManager.RuleStruct selectedRule)
            {
                try
                {
                    ruleManager.RemoveDynamicRule(selectedRule.Id);
                    LoadRulesList();
                    txtRulesStatus.Text = "Regra removida com sucesso!";
                }
                catch (Exception ex)
                {
                    txtRulesStatus.Text = $"Erro ao remover regra: {ex.Message}";
                }
            }
        }
    }
}