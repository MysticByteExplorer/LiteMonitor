using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using LiteMonitor.src.Core;
using LiteMonitor.src.UI.Controls;

namespace LiteMonitor.src.UI.SettingsPage
{
    public class MainPanelPage : SettingsPageBase
    {
        private Panel _container;
        private bool _isLoaded = false;

        // 交互与状态
        private LiteCheck _chkHideMain;
        private LiteCheck _chkAutoHide;
        private LiteCheck _chkTopMost;
        private LiteCheck _chkClickThrough;
        private LiteCheck _chkClamp;

        // 外观
        private LiteComboBox _cmbTheme;
        private LiteComboBox _cmbOrientation;
        private LiteComboBox _cmbWidth;
        private LiteComboBox _cmbOpacity;
        private LiteComboBox _cmbScale;

        public MainPanelPage()
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

            CreateBehaviorCard();
            CreateAppearanceCard();

            _container.ResumeLayout();
            _isLoaded = true;
        }

        private void CreateBehaviorCard()
        {
            var group = new LiteSettingsGroup(LanguageManager.T("Menu.MainFormSettings")); // 或 "Interaction"

            // 显示/隐藏开关
            _chkHideMain = new LiteCheck(Config.HideMainForm, LanguageManager.T("Menu.Enable"));
            group.AddItem(new LiteSettingsItem(LanguageManager.T("Menu.HideMainForm"), _chkHideMain));

            // 窗口置顶
            _chkTopMost = new LiteCheck(Config.TopMost, LanguageManager.T("Menu.Enable"));
            group.AddItem(new LiteSettingsItem(LanguageManager.T("Menu.TopMost"), _chkTopMost));

           
            // 限制在屏幕内
            _chkClamp = new LiteCheck(Config.ClampToScreen, LanguageManager.T("Menu.Enable"));
            group.AddItem(new LiteSettingsItem(LanguageManager.T("Menu.ClampToScreen"), _chkClamp));
            
            // 自动隐藏
            _chkAutoHide = new LiteCheck(Config.AutoHide, LanguageManager.T("Menu.Enable"));
            group.AddItem(new LiteSettingsItem(LanguageManager.T("Menu.AutoHide"), _chkAutoHide));

             // 鼠标穿透
            _chkClickThrough = new LiteCheck(Config.ClickThrough, LanguageManager.T("Menu.Enable"));
            group.AddItem(new LiteSettingsItem(LanguageManager.T("Menu.ClickThrough"), _chkClickThrough));


            _chkHideMain.CheckedChanged += (s, e) => CheckVisibilitySafe();

            AddGroupToPage(group);
        }

        private void CreateAppearanceCard()
        {
            var group = new LiteSettingsGroup(LanguageManager.T("Menu.Appearance")); // 使用原 AppearancePage 的标题

            // 主题
            _cmbTheme = new LiteComboBox();
            foreach (var t in ThemeManager.GetAvailableThemes()) _cmbTheme.Items.Add(t);
            SetComboVal(_cmbTheme, Config.Skin);
            group.AddItem(new LiteSettingsItem(LanguageManager.T("Menu.Theme"), _cmbTheme));

            // 方向
            _cmbOrientation = new LiteComboBox();
            _cmbOrientation.Items.Add(LanguageManager.T("Menu.Vertical"));   
            _cmbOrientation.Items.Add(LanguageManager.T("Menu.Horizontal")); 
            _cmbOrientation.SelectedIndex = Config.HorizontalMode ? 1 : 0;
            group.AddItem(new LiteSettingsItem(LanguageManager.T("Menu.DisplayMode"), _cmbOrientation));

            // 宽度
            _cmbWidth = new LiteComboBox();
            int[] widths = { 180, 200, 220, 240, 260, 280, 300, 360, 420, 480, 540, 600, 660, 720, 780, 840, 900, 960, 1020, 1080, 1140, 1200 };
            foreach (var w in widths) _cmbWidth.Items.Add(w + " px");
            SetComboVal(_cmbWidth, Config.PanelWidth + " px");
            group.AddItem(new LiteSettingsItem(LanguageManager.T("Menu.Width"), _cmbWidth));

            // 缩放
            _cmbScale = new LiteComboBox();
            double[] scales = { 0.5, 0.75, 0.9, 1.0, 1.25, 1.5, 1.75, 2.0 };
            foreach (var s in scales) _cmbScale.Items.Add((s * 100) + "%");
            SetComboVal(_cmbScale, (Config.UIScale * 100) + "%");
            group.AddItem(new LiteSettingsItem(LanguageManager.T("Menu.Scale"), _cmbScale));

            // 透明度
            _cmbOpacity = new LiteComboBox();
            double[] presetOps = { 1.0, 0.95, 0.9, 0.85, 0.8, 0.75, 0.7, 0.6, 0.5, 0.4, 0.3 };
            foreach (var op in presetOps) _cmbOpacity.Items.Add((op * 100) + "%");
            SetComboVal(_cmbOpacity, Math.Round(Config.Opacity * 100) + "%");
            group.AddItem(new LiteSettingsItem(LanguageManager.T("Menu.Opacity"), _cmbOpacity));

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
        private int ParseInt(string s) { string clean = new string(s.Where(char.IsDigit).ToArray()); return int.TryParse(clean, out int v) ? v : 0; }
        private double ParsePercent(string s) { int v = ParseInt(s); return v > 0 ? v / 100.0 : 1.0; }

        private void CheckVisibilitySafe()
        {
            // 简单的防死锁：如果当前正在勾选隐藏主界面，且任务栏也没开，且托盘也是隐藏的...
            if (_chkHideMain.Checked && !Config.ShowTaskbar && Config.HideTrayIcon)
            {
                 MessageBox.Show("为了防止程序无法唤出，不能同时隐藏 [主界面]、[托盘图标] 和 [任务栏]。", 
                                "安全警告", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                
                // 强制恢复勾选
                _chkHideMain.Checked = false; 
            }
        }

        public override void Save()
        {
            if (!_isLoaded) return;

            Config.HideMainForm = _chkHideMain.Checked;
            Config.AutoHide = _chkAutoHide.Checked;
            Config.TopMost = _chkTopMost.Checked;
            Config.ClickThrough = _chkClickThrough.Checked;
            Config.ClampToScreen = _chkClamp.Checked;

            if (_cmbTheme.SelectedItem != null) Config.Skin = _cmbTheme.SelectedItem.ToString();
            Config.HorizontalMode = (_cmbOrientation.SelectedIndex == 1);
            Config.PanelWidth = ParseInt(_cmbWidth.Text);
            Config.UIScale = ParsePercent(_cmbScale.Text);
            Config.Opacity = ParsePercent(_cmbOpacity.Text);

            AppActions.ApplyVisibility(Config, MainForm);
            AppActions.ApplyWindowAttributes(Config, MainForm);
            AppActions.ApplyThemeAndLayout(Config, UI, MainForm);
        }
    }
}