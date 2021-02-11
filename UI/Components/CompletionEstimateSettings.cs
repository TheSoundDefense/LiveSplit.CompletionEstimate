using System;
using System.Drawing;
using System.Windows.Forms;
using System.Xml;

namespace LiveSplit.UI.Components
{
    public partial class CompletionEstimateSettings : UserControl
    {
        public Color TextColor { get; set; }
        public bool OverrideTextColor { get; set; }
        public Color CompletionColor { get; set; }
        public bool OverrideCompletionColor { get; set; }

        public enum CompletionComparison
        {
            CurrentComparison = 0,
            PersonalBest = 1,
            BestSegments = 2,
            AverageSegments = 3
        }
        public CompletionComparison Comparison { get; set; }
        public enum CompletionTimingMethod
        {
            CurrentTimingMethod = 0,
            RealTime = 1,
            GameTime = 2
        }
        public CompletionTimingMethod TimingMethod { get; set; }
        public enum CompletionAccuracy
        {
            ZeroDecimal,
            OneDecimal,
            TwoDecimal
        }
        public CompletionAccuracy Accuracy { get; set; }
        public bool ShowTrailingZeroes { get; set; }

        public Color BackgroundColor { get; set; }
        public Color BackgroundColor2 { get; set; }
        public GradientType BackgroundGradient { get; set; }
        public string GradientString
        {
            get { return BackgroundGradient.ToString(); }
            set { BackgroundGradient = (GradientType)Enum.Parse(typeof(GradientType), value); }
        }

        public bool Display2Rows { get; set; }

        public LayoutMode Mode { get; set; }

        public CompletionEstimateSettings()
        {
            InitializeComponent();

            TextColor = Color.FromArgb(255, 255, 255);
            OverrideTextColor = false;
            CompletionColor = Color.FromArgb(255, 255, 255);
            OverrideCompletionColor = false;
            Comparison = CompletionComparison.CurrentComparison;
            TimingMethod = CompletionTimingMethod.CurrentTimingMethod;
            Accuracy = CompletionAccuracy.ZeroDecimal;
            BackgroundColor = Color.Transparent;
            BackgroundColor2 = Color.Transparent;
            BackgroundGradient = GradientType.Plain;
            Display2Rows = false;

            chkOverrideTextColor.DataBindings.Add("Checked", this, "OverrideTextColor", false, DataSourceUpdateMode.OnPropertyChanged);
            btnTextColor.DataBindings.Add("BackColor", this, "TextColor", false, DataSourceUpdateMode.OnPropertyChanged);
            chkOverrideCmpColor.DataBindings.Add("Checked", this, "OverrideCompletionColor", false, DataSourceUpdateMode.OnPropertyChanged);
            btnCmpColor.DataBindings.Add("BackColor", this, "CompletionColor", false, DataSourceUpdateMode.OnPropertyChanged);
            btnColor1.DataBindings.Add("BackColor", this, "BackgroundColor", false, DataSourceUpdateMode.OnPropertyChanged);
            btnColor2.DataBindings.Add("BackColor", this, "BackgroundColor2", false, DataSourceUpdateMode.OnPropertyChanged);
            cmbGradientType.DataBindings.Add("SelectedItem", this, "GradientString", false, DataSourceUpdateMode.OnPropertyChanged);
            chkTrailingZeroes.DataBindings.Add("Checked", this, "ShowTrailingZeroes", false, DataSourceUpdateMode.OnPropertyChanged);
        }

        private void CompletionEstimateSettings_Load(object sender, EventArgs e)
        {
            chkOverrideTextColor_CheckedChanged(null, null);
            chkOverrideCmpColor_CheckedChanged(null, null);
            cmpComboBox.SelectedIndex = (int)Comparison;
            timingComboBox.SelectedIndex = (int)TimingMethod;
            rdoDecimalZero.Checked = Accuracy == CompletionAccuracy.ZeroDecimal;
            rdoDecimalOne.Checked = Accuracy == CompletionAccuracy.OneDecimal;
            rdoDecimalTwo.Checked = Accuracy == CompletionAccuracy.TwoDecimal;
            if (Mode == LayoutMode.Horizontal)
            {
                chkTwoRows.Enabled = false;
                chkTwoRows.DataBindings.Clear();
                chkTwoRows.Checked = true;
            }
            else
            {
                chkTwoRows.Enabled = true;
                chkTwoRows.DataBindings.Clear();
                chkTwoRows.DataBindings.Add("Checked", this, "Display2Rows", false, DataSourceUpdateMode.OnPropertyChanged);
            }
        }

        public void SetSettings(XmlNode node)
        {
            var element = (XmlElement)node;
            TextColor = SettingsHelper.ParseColor(element["TextColor"]);
            OverrideTextColor = SettingsHelper.ParseBool(element["OverrideTextColor"]);
            CompletionColor = SettingsHelper.ParseColor(element["CompletionColor"]);
            OverrideCompletionColor = SettingsHelper.ParseBool(element["OverrideCompletionColor"]);
            Comparison = SettingsHelper.ParseEnum<CompletionComparison>(element["Comparison"]);
            TimingMethod = SettingsHelper.ParseEnum<CompletionTimingMethod>(element["TimingMethod"]);
            Accuracy = SettingsHelper.ParseEnum<CompletionAccuracy>(element["Accuracy"]);
            ShowTrailingZeroes = SettingsHelper.ParseBool(element["ShowTrailingZeroes"]);
            BackgroundColor = SettingsHelper.ParseColor(element["BackgroundColor"]);
            BackgroundColor2 = SettingsHelper.ParseColor(element["BackgroundColor2"]);
            GradientString = SettingsHelper.ParseString(element["BackgroundGradient"]);
            Display2Rows = SettingsHelper.ParseBool(element["Display2Rows"], false);
        }

        public XmlNode GetSettings(XmlDocument document)
        {
            var parent = document.CreateElement("Settings");
            CreateSettingsNode(document, parent);
            return parent;
        }

        public int GetSettingsHashCode()
        {
            return CreateSettingsNode(null, null);
        }

        private int CreateSettingsNode(XmlDocument document, XmlElement parent)
        {
            return SettingsHelper.CreateSetting(document, parent, "Version", "1.0") ^
            SettingsHelper.CreateSetting(document, parent, "TextColor", TextColor) ^
            SettingsHelper.CreateSetting(document, parent, "OverrideTextColor", OverrideTextColor) ^
            SettingsHelper.CreateSetting(document, parent, "CompletionColor", CompletionColor) ^
            SettingsHelper.CreateSetting(document, parent, "OverrideCompletionColor", OverrideCompletionColor) ^
            SettingsHelper.CreateSetting(document, parent, "Comparison", Comparison) ^
            SettingsHelper.CreateSetting(document, parent, "TimingMethod", TimingMethod) ^
            SettingsHelper.CreateSetting(document, parent, "Accuracy", Accuracy) ^
            SettingsHelper.CreateSetting(document, parent, "ShowTrailingZeroes", ShowTrailingZeroes) ^
            SettingsHelper.CreateSetting(document, parent, "BackgroundColor", BackgroundColor) ^
            SettingsHelper.CreateSetting(document, parent, "BackgroundColor2", BackgroundColor2) ^
            SettingsHelper.CreateSetting(document, parent, "BackgroundGradient", BackgroundGradient) ^
            SettingsHelper.CreateSetting(document, parent, "Display2Rows", Display2Rows);
        }

        private void chkOverrideTextColor_CheckedChanged(object sender, EventArgs e)
        {
            textColorLabel.Enabled = btnTextColor.Enabled = chkOverrideTextColor.Checked;
        }

        private void chkOverrideCmpColor_CheckedChanged(object sender, EventArgs e)
        {
            cmpColorLabel.Enabled = btnCmpColor.Enabled = chkOverrideCmpColor.Checked;
        }

        private void ColorButtonClick(object sender, EventArgs e)
        {
            SettingsHelper.ColorButtonClick((Button)sender, this);
        }

        private void cmbGradientType_SelectedIndexChanged(object sender, EventArgs e)
        {
            btnColor1.Visible = cmbGradientType.SelectedItem.ToString() != "Plain";
            btnColor2.DataBindings.Clear();
            btnColor2.DataBindings.Add("BackColor", this, btnColor1.Visible ? "BackgroundColor2" : "BackgroundColor", false, DataSourceUpdateMode.OnPropertyChanged);
            GradientString = cmbGradientType.SelectedItem.ToString();
        }

        private void cmpComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            Comparison = (CompletionComparison)cmpComboBox.SelectedIndex;
        }

        private void timingComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            TimingMethod = (CompletionTimingMethod)timingComboBox.SelectedIndex;
        }

        private void rdoDecimalZero_CheckedChanged(object sender, EventArgs e)
        {
            UpdateAccuracy();
        }

        private void rdoDecimalTwo_CheckedChanged(object sender, EventArgs e)
        {
            UpdateAccuracy();
        }

        private void UpdateAccuracy()
        {
            if (rdoDecimalZero.Checked)
                Accuracy = CompletionAccuracy.ZeroDecimal;
            else if (rdoDecimalOne.Checked)
                Accuracy = CompletionAccuracy.OneDecimal;
            else
                Accuracy = CompletionAccuracy.TwoDecimal;
        }
    }
}
