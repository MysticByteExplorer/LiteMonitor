using System;
using System.Drawing;
using System.IO;
using System.Linq; 
using System.Windows.Forms;
using LiteMonitor.src.Core;
using LiteMonitor.src.SystemServices;
using LiteMonitor.src.UI.Controls;

namespace LiteMonitor.src.UI.SettingsPage
{
    public class SystemHardwarPage : SettingsPageBase
    {
        private Panel _container;
        private bool _isLoaded = false;

        private LiteComboBox _cmbLang;
        private LiteCheck _chkAutoStart;
        private LiteCheck _chkHideTray;
        
        private LiteComboBox _cmbRefresh;
        private LiteComboBox _cmbNet;
        private LiteComboBox _cmbDisk;

        // 最大限制校准
        private LiteUnderlineInput _txtMaxCpuPower;
        private LiteUnderlineInput _txtMaxCpuClock;
        private LiteUnderlineInput _txtMaxGpuPower;
        private LiteUnderlineInput _txtMaxGpuClock;

        private string _originalLanguage;

        public SystemHardwarPage()
        {
            this.BackColor = UIColors.MainBg;
            this.Dock = DockStyle.Fill;
            this.Padding = new Padding(0);
            _container = new Panel { Dock = DockStyle.Fill, AutoScroll = true, Padding = new Padding(20) };
            this.Controls.Add(_container);
        }
    
        public override void OnShow()
        {
            if (Config == null || _isLoaded) return;
            _container.SuspendLayout();
            _container.Controls.Clear();
           
            CreateSystemCard();   
            CreateCalibrationCard();
            CreateSourceCard();   

            _originalLanguage = Config.Language;
            _container.ResumeLayout();
            _isLoaded = true;
        }

        private void CreateSystemCard()
        {
            var group = new LiteSettingsGroup(LanguageManager.T("Menu.SystemSettings"));
            
            // 语言
            _cmbLang = new LiteComboBox();
            string langDir = Path.Combine(AppContext.BaseDirectory, "resources/lang");
            if (Directory.Exists(langDir)) {
                foreach (var file in Directory.EnumerateFiles(langDir, "*.json")) {
                    string code = Path.GetFileNameWithoutExtension(file);
                    _cmbLang.Items.Add(code.ToUpper());
                }
            }
            string curLang = string.IsNullOrEmpty(Config.Language) ? "en" : Config.Language;
            foreach (var item in _cmbLang.Items) {
                if (item.ToString().Contains(curLang.ToUpper())) _cmbLang.SelectedItem = item;
            }
            group.AddItem(new LiteSettingsItem(LanguageManager.T("Menu.Language"), _cmbLang));

            // 开机自启
            _chkAutoStart = new LiteCheck(Config.AutoStart, LanguageManager.T("Menu.Enable"));
            group.AddItem(new LiteSettingsItem(LanguageManager.T("Menu.AutoStart"), _chkAutoStart));

            // 隐藏托盘图标
            _chkHideTray = new LiteCheck(Config.HideTrayIcon, LanguageManager.T("Menu.Enable"));
            group.AddItem(new LiteSettingsItem(LanguageManager.T("Menu.HideTrayIcon"), _chkHideTray));
            
            // 安全检查事件
            _chkHideTray.CheckedChanged += (s, e) => CheckVisibilitySafe();

            AddGroupToPage(group);
        }

        
        private void CreateCalibrationCard()
        {
            // 将原来的 "Max Limits" 独立显示，逻辑更清晰
            var group = new LiteSettingsGroup(LanguageManager.T("Menu.Calibration"));

            _txtMaxCpuPower = new LiteUnderlineInput(Config.RecordedMaxCpuPower.ToString("F0"), "W", "", 50);
            group.AddItem(new LiteSettingsItem(LanguageManager.T("Items.CPU.Power") + " (" + LanguageManager.T("Menu.MaxLimits") + ")", _txtMaxCpuPower));

            _txtMaxCpuClock = new LiteUnderlineInput(Config.RecordedMaxCpuClock.ToString("F0"), "MHz", "", 70);
            group.AddItem(new LiteSettingsItem(LanguageManager.T("Items.CPU.Clock") + " (" + LanguageManager.T("Menu.MaxLimits") + ")", _txtMaxCpuClock));

            _txtMaxGpuPower = new LiteUnderlineInput(Config.RecordedMaxGpuPower.ToString("F0"), "W", "", 50);
            group.AddItem(new LiteSettingsItem(LanguageManager.T("Items.GPU.Power") + " (" + LanguageManager.T("Menu.MaxLimits") + ")", _txtMaxGpuPower));

            _txtMaxGpuClock = new LiteUnderlineInput(Config.RecordedMaxGpuClock.ToString("F0"), "MHz", "", 70);
            group.AddItem(new LiteSettingsItem(LanguageManager.T("Items.GPU.Clock") + " (" + LanguageManager.T("Menu.MaxLimits") + ")", _txtMaxGpuClock));

            group.AddFullItem(new LiteNote(LanguageManager.T("Menu.CalibrationTip"), 0));

            AddGroupToPage(group);
        }


        private void CreateSourceCard()
        {
            var group = new LiteSettingsGroup(LanguageManager.T("Menu.HardwareSettings"));

            // 磁盘源
            _cmbDisk = new LiteComboBox();
            foreach (var d in HardwareMonitor.ListAllDisks()) _cmbDisk.Items.Add(d);
            SetComboVal(_cmbDisk, string.IsNullOrEmpty(Config.PreferredDisk) ? LanguageManager.T("Menu.Auto") : Config.PreferredDisk);
            group.AddItem(new LiteSettingsItem(LanguageManager.T("Menu.DiskSource"), _cmbDisk));

            // 网络源
            _cmbNet = new LiteComboBox();
            foreach (var n in HardwareMonitor.ListAllNetworks()) _cmbNet.Items.Add(n);
            SetComboVal(_cmbNet, string.IsNullOrEmpty(Config.PreferredNetwork) ? LanguageManager.T("Menu.Auto") : Config.PreferredNetwork);
            group.AddItem(new LiteSettingsItem(LanguageManager.T("Menu.NetworkSource"), _cmbNet));

            // 刷新率
            _cmbRefresh = new LiteComboBox();
            int[] rates = { 100, 200, 300, 500, 600, 700, 800, 1000, 1500, 2000, 3000 };
            foreach (var r in rates) _cmbRefresh.Items.Add(r + " ms");
            SetComboVal(_cmbRefresh, Config.RefreshMs + " ms");
            group.AddItem(new LiteSettingsItem(LanguageManager.T("Menu.Refresh"), _cmbRefresh));

            AddGroupToPage(group);
        }

        private void AddGroupToPage(LiteSettingsGroup group)
        {
            var wrapper = new Panel { Dock = DockStyle.Top, AutoSize = true, Padding = new Padding(0, 0, 0, 20) };
            wrapper.Controls.Add(group);
            _container.Controls.Add(wrapper);
            _container.Controls.SetChildIndex(wrapper, 0);
        }
        
        private void SetComboVal(LiteComboBox cmb, string val) { if (!cmb.Items.Contains(val)) cmb.Items.Insert(0, val); cmb.SelectedItem = val; }
        
        // 安全检查：防止所有入口都被隐藏
        private void CheckVisibilitySafe() {
            if (!Config.ShowTaskbar && Config.HideMainForm && _chkHideTray.Checked) {
                MessageBox.Show("为了防止程序无法唤出，不能同时隐藏 [主界面]、[托盘图标] 和 [任务栏]。", 
                                "安全警告", MessageBoxButtons.OK, MessageBoxIcon.Warning);

                _chkHideTray.Checked = false;
            }
        }
        
        private int ParseInt(string s) { string clean = new string(s.Where(char.IsDigit).ToArray()); return int.TryParse(clean, out int v) ? v : 0; }
        private float ParseFloat(string s) { string clean = new string(s.Where(c => char.IsDigit(c) || c == '.').ToArray()); return float.TryParse(clean, out float v) ? v : 0f; }

        public override void Save()
        {
            if (!_isLoaded) return;
            
            Config.AutoStart = _chkAutoStart.Checked;
            
            if (_cmbLang.SelectedItem != null) {
                string s = _cmbLang.SelectedItem.ToString();
                Config.Language = (s == "Auto") ? "" : s.Split('(')[0].Trim().ToLower();
            }
            
            Config.HideTrayIcon = _chkHideTray.Checked;
            
            Config.RefreshMs = ParseInt(_cmbRefresh.Text);
            if (Config.RefreshMs < 50) Config.RefreshMs = 1000;
            
            if (_cmbDisk.SelectedItem != null) { string d = _cmbDisk.SelectedItem.ToString(); Config.PreferredDisk = (d == "Auto") ? "" : d; }
            if (_cmbNet.SelectedItem != null) { string n = _cmbNet.SelectedItem.ToString(); Config.PreferredNetwork = (n == "Auto") ? "" : n; }

            Config.RecordedMaxCpuPower = ParseFloat(_txtMaxCpuPower.Inner.Text);
            Config.RecordedMaxCpuClock = ParseFloat(_txtMaxCpuClock.Inner.Text);
            Config.RecordedMaxGpuPower = ParseFloat(_txtMaxGpuPower.Inner.Text);
            Config.RecordedMaxGpuClock = ParseFloat(_txtMaxGpuClock.Inner.Text);

            // 应用更改
            AppActions.ApplyAutoStart(Config);
            AppActions.ApplyVisibility(Config, this.MainForm);
            
            if (_originalLanguage != Config.Language) {
                AppActions.ApplyLanguage(Config, this.UI, this.MainForm);
                _originalLanguage = Config.Language; 
            }
        }
    }
}