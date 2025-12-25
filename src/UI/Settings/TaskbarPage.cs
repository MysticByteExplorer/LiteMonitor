using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using LiteMonitor.src.Core;
using LiteMonitor.src.UI.Controls;

namespace LiteMonitor.src.UI.SettingsPage
{
    public class TaskbarPage : SettingsPageBase
    {
        private Panel _container;
        private bool _isLoaded = false;

        // === 模块 1: 常规设置 ===
        private LiteCheck _chkShowTaskbar;
        private LiteComboBox _cmbTaskbarStyle;
        private LiteComboBox _cmbTaskbarAlign;
        private LiteCheck _chkTaskbarClickThrough;

        // === 模块 2: 自定义颜色 ===
        private LiteCheck _chkTaskbarCustom;
        private LiteColorInput _inColorLabel;
        private LiteColorInput _inColorSafe;
        private LiteColorInput _inColorWarn;
        private LiteColorInput _inColorCrit;
        private LiteColorInput _inColorBg;

        public TaskbarPage()
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

            CreateGeneralGroup(); // 创建常规分组
            CreateColorGroup();   // 创建颜色分组

            _container.ResumeLayout();
            _isLoaded = true;
        }

        // 模块 1: 基础功能
        private void CreateGeneralGroup()
        {
            var group = new LiteSettingsGroup(LanguageManager.T("Menu.TaskbarSettings"));

            // 1. 总开关
            _chkShowTaskbar = new LiteCheck(Config.ShowTaskbar, LanguageManager.T("Menu.Enable"));
            group.AddItem(new LiteSettingsItem(LanguageManager.T("Menu.TaskbarShow"), _chkShowTaskbar));
            _chkShowTaskbar.CheckedChanged += (s, e) => CheckVisibilitySafe();

             // 4. 鼠标穿透
            _chkTaskbarClickThrough = new LiteCheck(Config.TaskbarClickThrough, LanguageManager.T("Menu.Enable"));
            group.AddItem(new LiteSettingsItem(LanguageManager.T("Menu.ClickThrough"), _chkTaskbarClickThrough));   

            // 2. 样式 (大字/小字)
            _cmbTaskbarStyle = new LiteComboBox();
            _cmbTaskbarStyle.Items.Add(LanguageManager.T("Menu.TaskbarStyleBold")); 
            _cmbTaskbarStyle.Items.Add(LanguageManager.T("Menu.TaskbarStyleRegular"));
            bool isCompact = (Math.Abs(Config.TaskbarFontSize - 9f) < 0.1f) && !Config.TaskbarFontBold;
            _cmbTaskbarStyle.SelectedIndex = isCompact ? 1 : 0;
            group.AddItem(new LiteSettingsItem(LanguageManager.T("Menu.TaskbarStyle"), _cmbTaskbarStyle));

            // 3. 对齐 (左/右)
            _cmbTaskbarAlign = new LiteComboBox();
            _cmbTaskbarAlign.Items.Add(LanguageManager.T("Menu.TaskbarAlignRight"));
            _cmbTaskbarAlign.Items.Add(LanguageManager.T("Menu.TaskbarAlignLeft"));
            _cmbTaskbarAlign.SelectedIndex = Config.TaskbarAlignLeft ? 1 : 0;
            group.AddItem(new LiteSettingsItem(LanguageManager.T("Menu.TaskbarAlign"), _cmbTaskbarAlign));

           
            
            // 提示：对齐功能仅Win11居中时有效 (移到这里更合适)
            var tips = new LiteNote(LanguageManager.T("Menu.TaskbarAlignTip"), 0);
            group.AddFullItem(tips);

            AddGroupToPage(group);
        }

        // 模块 2: 自定义外观 (独立分组)
        private void CreateColorGroup()
        {
            // 使用 "Custom Colors" 作为标题 (如果没有对应的Key，可以直接写字符串或复用 Menu.Appearance)
            // 这里建议在语言文件加一个 Key: "Menu.ColorSettings" 或复用 "Menu.CustomColors"
            var group = new LiteSettingsGroup(LanguageManager.T("Menu.TaskbarCustomColors"));

            // 1. 自定义开关
            _chkTaskbarCustom = new LiteCheck(Config.TaskbarCustomStyle, LanguageManager.T("Menu.Enable"));
            // 这里 Item 的 label 可以叫 "Enable Custom Colors" 或者简单点 "Enabled"
            group.AddItem(new LiteSettingsItem(LanguageManager.T("Menu.TaskbarCustomColors"), _chkTaskbarCustom));
            
            // 简单的交互：开关切换时刷新颜色控件的可用状态 (可选，视觉上更好)
            _chkTaskbarCustom.CheckedChanged += (s, e) => ToggleColorInputs(_chkTaskbarCustom.Checked);

            // 提示：自定义颜色仅在启用后生效
            var tips = new LiteNote(LanguageManager.T("Menu.TaskbarCustomTip"), 0);
            group.AddFullItem(tips);

            // 2. 颜色选择器
            _inColorLabel = new LiteColorInput(Config.TaskbarColorLabel);
            group.AddItem(new LiteSettingsItem(LanguageManager.T("Menu.LabelColor"), _inColorLabel));

            _inColorSafe = new LiteColorInput(Config.TaskbarColorSafe);
            group.AddItem(new LiteSettingsItem(LanguageManager.T("Menu.ValueSafeColor"), _inColorSafe));

            _inColorWarn = new LiteColorInput(Config.TaskbarColorWarn);
            group.AddItem(new LiteSettingsItem(LanguageManager.T("Menu.ValueWarnColor"), _inColorWarn));

            _inColorCrit = new LiteColorInput(Config.TaskbarColorCrit);
            group.AddItem(new LiteSettingsItem(LanguageManager.T("Menu.ValueCritColor"), _inColorCrit));

            _inColorBg = new LiteColorInput(Config.TaskbarColorBg);
            group.AddItem(new LiteSettingsItem(LanguageManager.T("Menu.BackgroundColor"), _inColorBg));

            AddGroupToPage(group);
            
            // 初始化状态
            ToggleColorInputs(Config.TaskbarCustomStyle);
        }

        private void ToggleColorInputs(bool enabled)
        {
            // 简单的视觉反馈，让用户知道颜色设置是否生效
            _inColorLabel.Enabled = enabled;
            _inColorSafe.Enabled = enabled;
            _inColorWarn.Enabled = enabled;
            _inColorCrit.Enabled = enabled;
            _inColorBg.Enabled = enabled;
            
            // 如果不想隐藏而是禁用，也可以用 Enabled = enabled;
            // 但 LiteColorInput 可能没完全实现 Enabled 样式的传递，Visible 效果最直接。
        }

        private void AddGroupToPage(LiteSettingsGroup group)
        {
            var wrapper = new Panel { Dock = DockStyle.Top, AutoSize = true, Padding = new Padding(0, 0, 0, 20) };
            wrapper.Controls.Add(group);
            _container.Controls.Add(wrapper);
            _container.Controls.SetChildIndex(wrapper, 0);
        }

        private void CheckVisibilitySafe() 
        {
            // 简单防呆：检查 Config 中另外两个入口是否早已隐藏
            // 逻辑：如果 (主界面已隐藏) 且 (托盘已隐藏) -> 此时关掉任务栏会导致程序“消失”
            if (!_chkShowTaskbar.Checked && Config.HideMainForm && Config.HideTrayIcon) 
            {
                MessageBox.Show("为了防止程序无法唤出，不能同时隐藏 [主界面]、[托盘图标] 和 [任务栏]。", 
                                "安全警告", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                
                // 强制恢复勾选
                _chkShowTaskbar.Checked = true; 
            }
        }

        public override void Save()
        {
            if (!_isLoaded) return;

            // === 保存常规设置 ===
            Config.ShowTaskbar = _chkShowTaskbar.Checked;
            if (_cmbTaskbarStyle.SelectedIndex == 1) { 
                Config.TaskbarFontSize = 9f;
                Config.TaskbarFontBold = false;
            } else { 
                Config.TaskbarFontSize = 10f;
                Config.TaskbarFontBold = true;
            }
            Config.TaskbarAlignLeft = (_cmbTaskbarAlign.SelectedIndex == 1);
            Config.TaskbarClickThrough = _chkTaskbarClickThrough.Checked;

            // === 保存颜色设置 ===
            Config.TaskbarCustomStyle = _chkTaskbarCustom.Checked;
            Config.TaskbarColorLabel = _inColorLabel.HexValue;
            Config.TaskbarColorSafe = _inColorSafe.HexValue;
            Config.TaskbarColorWarn = _inColorWarn.HexValue;
            Config.TaskbarColorCrit = _inColorCrit.HexValue;
            Config.TaskbarColorBg = _inColorBg.HexValue;

            // === 应用 ===
            AppActions.ApplyVisibility(Config, MainForm);
            AppActions.ApplyTaskbarStyle(Config, UI);
        }
    }
}