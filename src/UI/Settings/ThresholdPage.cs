using System;
using System.Drawing;
using System.Windows.Forms;
using LiteMonitor.src.Core;
using LiteMonitor.src.UI.Controls;

namespace LiteMonitor.src.UI.SettingsPage
{
    public class ThresholdPage : SettingsPageBase
    {
        private Panel _container;
        private bool _isLoaded = false;

        private class ThresholdInputs { public LiteUnderlineInput Warn; public LiteUnderlineInput Crit; }

        private ThresholdInputs _inCpuLoad;
        private ThresholdInputs _inCpuTemp;
        private ThresholdInputs _inDisk;
        private ThresholdInputs _inNetUp;
        private ThresholdInputs _inNetDown;
        private ThresholdInputs _inDataUp;
        private ThresholdInputs _inDataDown;
        // ★★★ 新增：弹窗告警设置 ★★★
        private LiteCheck _chkAlertTemp;
        private LiteUnderlineInput _inAlertTemp;

        public ThresholdPage()
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

             // ★★★ 新增：高温报通知分组 (插入在这里比较合适) ★★★
            var grpAlert = new LiteSettingsGroup(LanguageManager.T("Menu.AlertTemp"));
            
            // 高温报警开关
            _chkAlertTemp = new LiteCheck(Config.AlertTempEnabled, LanguageManager.T("Menu.Enable"));
            grpAlert.AddItem(new LiteSettingsItem(LanguageManager.T("Menu.AlertTemp"), _chkAlertTemp));

            // 高温报警阈值
            _inAlertTemp = new LiteUnderlineInput(Config.AlertTempThreshold.ToString("F0"), "°C", "", 80, UIColors.TextCrit, HorizontalAlignment.Center);
            grpAlert.AddItem(new LiteSettingsItem(LanguageManager.T("Menu.AlertThreshold"), _inAlertTemp));

            AddGroupToPage(grpAlert);

            // 1. 硬件负载
            var grpHardware = new LiteSettingsGroup(LanguageManager.T("Menu.GeneralHardware"));
            
            // 文案放在前面了
            _inCpuLoad = AddThresholdRow(grpHardware, LanguageManager.T("Menu.HardwareLoad"), Config.Thresholds.Load, "%", LanguageManager.T("Menu.ValueWarnColor"), LanguageManager.T("Menu.ValueCritColor"));
            _inCpuTemp = AddThresholdRow(grpHardware, LanguageManager.T("Menu.HardwareTemp"), Config.Thresholds.Temp, "°C", LanguageManager.T("Menu.ValueWarnColor"), LanguageManager.T("Menu.ValueCritColor"));

            AddGroupToPage(grpHardware);

            // 2. 网络与磁盘
            var grpNet = new LiteSettingsGroup(LanguageManager.T("Menu.NetworkDiskSpeed"));
            
            _inDisk = AddThresholdRow(grpNet, LanguageManager.T("Menu.DiskIOSpeed"), Config.Thresholds.DiskIOMB, "MB/s", LanguageManager.T("Menu.ValueWarnColor"), LanguageManager.T("Menu.ValueCritColor"));
            _inNetUp = AddThresholdRow(grpNet, LanguageManager.T("Menu.UploadSpeed"), Config.Thresholds.NetUpMB, "MB/s", LanguageManager.T("Menu.ValueWarnColor"), LanguageManager.T("Menu.ValueCritColor"));
            _inNetDown = AddThresholdRow(grpNet, LanguageManager.T("Menu.DownloadSpeed"), Config.Thresholds.NetDownMB, "MB/s", LanguageManager.T("Menu.ValueWarnColor"), LanguageManager.T("Menu.ValueCritColor"));

            AddGroupToPage(grpNet);

            // 3. 流量限额
            var grpData = new LiteSettingsGroup(LanguageManager.T("Menu.DailyTraffic"));

            _inDataUp = AddThresholdRow(grpData, LanguageManager.T("Items.DATA.DayUp"), Config.Thresholds.DataUpMB, "MB", LanguageManager.T("Menu.ValueWarnColor"), LanguageManager.T("Menu.ValueCritColor"));
            _inDataDown = AddThresholdRow(grpData, LanguageManager.T("Items.DATA.DayDown"), Config.Thresholds.DataDownMB, "MB", LanguageManager.T("Menu.ValueWarnColor"), LanguageManager.T("Menu.ValueCritColor"));

            AddGroupToPage(grpData);

           

            _container.ResumeLayout();
            _isLoaded = true;
        }

        private ThresholdInputs AddThresholdRow(
            LiteSettingsGroup group, 
            string title, 
            ValueRange val, 
            string unit,
            string labelWarn, 
            string labelCrit)
        {
            var panel = new Panel { Height = 40, Margin = new Padding(0), Padding = new Padding(0) };

            var lblTitle = new Label {
                Text = title, AutoSize = true, 
                Font = new Font("Microsoft YaHei UI", 9F), ForeColor = UIColors.TextMain,
                TextAlign = ContentAlignment.MiddleLeft
            };
            panel.Controls.Add(lblTitle);

            // 右侧流式布局
            var rightBox = new FlowLayoutPanel {
                AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink,
                FlowDirection = FlowDirection.LeftToRight, WrapContents = false, 
                BackColor = Color.Transparent, Padding = new Padding(0)
            };

            // 参数顺序已更新：text, unit, labelPrefix, width, color
            // 宽度设为 140 比较紧凑，如果文案长（如中文“严重警告”）可以设大一点
            var inputWarn = new LiteUnderlineInput(val.Warn.ToString(), unit, labelWarn, 140, UIColors.TextWarn, HorizontalAlignment.Center);

            var arrow = new Label { 
                Text = "➜", AutoSize = true, ForeColor = Color.LightGray, 
                Font = new Font("Microsoft YaHei UI", 9F), Margin = new Padding(5, 4, 5, 0) 
            };

            var inputCrit = new LiteUnderlineInput(val.Crit.ToString(), unit, labelCrit, 140, UIColors.TextCrit, HorizontalAlignment.Center);

            rightBox.Controls.Add(inputWarn);
            rightBox.Controls.Add(arrow);
            rightBox.Controls.Add(inputCrit);

            panel.Controls.Add(rightBox);

            // 布局
            panel.Layout += (s, e) => {
                lblTitle.Location = new Point(0, (panel.Height - lblTitle.Height) / 2);
                rightBox.Location = new Point(panel.Width - rightBox.Width, (panel.Height - rightBox.Height) / 2);
            };

            // 底部分割线
            panel.Paint += (s, e) => {
                using(var p = new Pen(Color.FromArgb(240, 240, 240))) 
                    e.Graphics.DrawLine(p, 0, panel.Height-1, panel.Width, panel.Height-1);
            };

            group.AddFullItem(panel);
            return new ThresholdInputs { Warn = inputWarn, Crit = inputCrit };
        }

        private void AddGroupToPage(LiteSettingsGroup group)
        {
            var wrapper = new Panel { Dock = DockStyle.Top, AutoSize = true, Padding = new Padding(0, 0, 0, 20) };
            wrapper.Controls.Add(group);
            _container.Controls.Add(wrapper);
            _container.Controls.SetChildIndex(wrapper, 0);
        }

        // ★★★ 修复点：将解析方法提取为类成员，避免局部函数的作用域问题 ★★★
        private double Parse(LiteUnderlineInput input) 
        {
            // 简单防空保护
            if (input == null || input.Inner == null) return 0;
            return double.TryParse(input.Inner.Text, out double v) ? v : 0;
        }

        private int ParseInt(LiteUnderlineInput input)
        {
            if (input == null || input.Inner == null) return 0;
            return int.TryParse(input.Inner.Text, out int v) ? v : 0;
        }
        public override void Save()
        {
            if (!_isLoaded) return;

            Config.Thresholds.Load.Warn = Parse(_inCpuLoad.Warn);
            Config.Thresholds.Load.Crit = Parse(_inCpuLoad.Crit);
            Config.Thresholds.Temp.Warn = Parse(_inCpuTemp.Warn);
            Config.Thresholds.Temp.Crit = Parse(_inCpuTemp.Crit);
            Config.Thresholds.DiskIOMB.Warn = Parse(_inDisk.Warn);
            Config.Thresholds.DiskIOMB.Crit = Parse(_inDisk.Crit);
            Config.Thresholds.NetUpMB.Warn = Parse(_inNetUp.Warn);
            Config.Thresholds.NetUpMB.Crit = Parse(_inNetUp.Crit);
            Config.Thresholds.NetDownMB.Warn = Parse(_inNetDown.Warn);
            Config.Thresholds.NetDownMB.Crit = Parse(_inNetDown.Crit);
            Config.Thresholds.DataUpMB.Warn = Parse(_inDataUp.Warn);
            Config.Thresholds.DataUpMB.Crit = Parse(_inDataUp.Crit);
            Config.Thresholds.DataDownMB.Warn = Parse(_inDataDown.Warn);
            Config.Thresholds.DataDownMB.Crit = Parse(_inDataDown.Crit);
            // ★★★ 保存告警设置 ★★★
            Config.AlertTempEnabled = _chkAlertTemp.Checked;
            Config.AlertTempThreshold = ParseInt(_inAlertTemp);
        }
    }
}