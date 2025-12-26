using System;
using System.Drawing;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using LiteMonitor.src.Core;
using LiteMonitor.src.SystemServices;

namespace LiteMonitor
{
    // FPS屏幕显示位置枚举
    public enum FpsScreenPosition
    {
        TopLeft,
        TopCenter,
        TopRight
    }

    // FPS屏幕显示配置
    public class FpsScreenConfig
    {
        public bool Enabled { get; set; } = false;
        public FpsScreenPosition Position { get; set; } = FpsScreenPosition.TopRight;
        public float FontSize { get; set; } = 24f;
        public string Color { get; set; } = "255,0,0";
    }

    // FPS屏幕显示窗体
    public class FpsScreenForm : Form
    {
        private Label _fpsLabel;
        private readonly Settings _settings;
        private readonly HardwareMonitor _hardwareMonitor;
        private System.Windows.Forms.Timer _updateTimer;
        private FpsScreenPosition _currentPosition;

        // Win32 API定义
        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

        private const uint SWP_NOSIZE = 0x0001;
        private const uint SWP_NOACTIVATE = 0x0010;
        private static readonly IntPtr HWND_TOPMOST = new IntPtr(-1);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern int GetWindowLong(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

        private const int GWL_EXSTYLE = -20;
        private const int WS_EX_TRANSPARENT = 0x20;
        private const int WS_EX_LAYERED = 0x80000;
        private const int WS_EX_NOACTIVATE = 0x8000000;

        public FpsScreenForm(Settings settings, HardwareMonitor hardwareMonitor)
        {
            _settings = settings;
            _hardwareMonitor = hardwareMonitor;
            _currentPosition = (FpsScreenPosition)_settings.FpsScreenPosition;

            InitializeComponent();
            InitializeTimer();
        }

        private void InitializeComponent()
        {
            // 设置表单属性
            this.BackColor = Color.Black;
            this.TransparencyKey = Color.Black;
            this.FormBorderStyle = FormBorderStyle.None;
            this.Size = new Size(150, 50);
            this.StartPosition = FormStartPosition.Manual;
            this.TopMost = true;
            this.DoubleBuffered = true;
            this.ShowInTaskbar = false;

            // 设置窗口层级（始终在最前，不获取焦点）
            int exStyle = GetWindowLong(this.Handle, GWL_EXSTYLE);
            SetWindowLong(this.Handle, GWL_EXSTYLE, exStyle | WS_EX_LAYERED | WS_EX_NOACTIVATE | WS_EX_TRANSPARENT);

            // 创建FPS标签
            _fpsLabel = new Label
            {
                AutoSize = true,
                Font = new Font("Arial", _settings.FpsScreenFontSize, FontStyle.Bold),
                ForeColor = ParseHexColor(_settings.FpsScreenColor),
                BackColor = Color.Transparent,
                Location = new Point(0, 0),
                Text = "FPS: 0.0"
            };

            this.Controls.Add(_fpsLabel);

            // 设置初始位置
            SetWindowPosition(_currentPosition);
        }

        private void InitializeTimer()
        {
            _updateTimer = new System.Windows.Forms.Timer
            {
                Interval = 1000
            };
            _updateTimer.Tick += UpdateTimer_Tick;
            _updateTimer.Start();
        }

        private void UpdateTimer_Tick(object sender, EventArgs e)
        {
            // 获取当前FPS值
            float fps = _hardwareMonitor.Get("FPS.Current") ?? 0;
            
            // 更新FPS显示，支持$[fps]格式
            _fpsLabel.Text = FormatFpsText(_settings.FpsScreenFormat, fps);
            
            // 使用配置的字体大小
            _fpsLabel.Font = new Font("Arial", _settings.FpsScreenFontSize, FontStyle.Bold);
            
            // 使用配置的颜色
            _fpsLabel.ForeColor = ParseHexColor(_settings.FpsScreenColor);
            
            // 重新调整窗口大小以适应文本
            using (var graphics = this.CreateGraphics())
            {
                var textSize = graphics.MeasureString(_fpsLabel.Text, _fpsLabel.Font);
                int newWidth = (int)Math.Ceiling(textSize.Width);
                int newHeight = (int)Math.Ceiling(textSize.Height);
                
                // 只有当窗口大小改变时才更新，避免不必要的重绘
                if (this.Width != newWidth || this.Height != newHeight)
                {
                    // 先计算新的位置，避免闪烁
                    Screen primaryScreen = Screen.PrimaryScreen;
                    int x = this.Location.X;
                    int y = this.Location.Y;
                    
                    // 根据当前位置计算新的X坐标
                    switch (_currentPosition)
                    {
                        case FpsScreenPosition.TopLeft:
                            x = 0;
                            y = 0;
                            break;
                        case FpsScreenPosition.TopCenter:
                            x = (primaryScreen.WorkingArea.Width - newWidth) / 2;
                            y = 0;
                            break;
                        case FpsScreenPosition.TopRight:
                            x = primaryScreen.WorkingArea.Width - newWidth;
                            y = 0;
                            break;
                    }
                    
                    // 使用SetWindowPos一次性更新位置和大小，减少闪烁
                    SetWindowPos(this.Handle, HWND_TOPMOST, x, y, newWidth, newHeight, 0);
                }
            }
        }
        
        // 格式化FPS文本，支持$[fps]格式
        private string FormatFpsText(string format, float fps)
        {
            if (string.IsNullOrEmpty(format))
                format = "FPS: $[fps]";
            
            // 替换$[fps]为格式化的FPS值
            return format.Replace("$[fps]", $"{fps:0.0}", StringComparison.OrdinalIgnoreCase);
        }

        private void SetWindowPosition(FpsScreenPosition position)
        {
            _currentPosition = position;
            Screen primaryScreen = Screen.PrimaryScreen;
            int x;
            int y = 0;

            // 使用SetWindowPos一次性更新位置，减少闪烁
            switch (position)
            {
                case FpsScreenPosition.TopLeft:
                    x = 0;
                    break;
                case FpsScreenPosition.TopCenter:
                    x = (primaryScreen.WorkingArea.Width - this.Width) / 2;
                    break;
                case FpsScreenPosition.TopRight:
                    x = primaryScreen.WorkingArea.Width - this.Width;
                    break;
                default:
                    x = 0;
                    break;
            }

            // 一次性更新位置和确保置顶，减少闪烁
            SetWindowPos(this.Handle, HWND_TOPMOST, x, y, 0, 0, SWP_NOSIZE | SWP_NOACTIVATE);
        }

        public void SetPosition(FpsScreenPosition position)
        {
            SetWindowPosition(position);
        }

        public void UpdateFps(float fps)
        {
            _fpsLabel.Text = $"{fps:0.0} FPS";
        }

        // 解析十六进制颜色字符串为Color对象
        private Color ParseHexColor(string hexColor)
        {
            try
            {
                if (string.IsNullOrEmpty(hexColor))
                    return Color.Red;
                
                // 处理#前缀
                if (hexColor.StartsWith("#"))
                    hexColor = hexColor.Substring(1);
                
                // 解析RGB值
                int r, g, b;
                if (hexColor.Length == 6)
                {
                    r = int.Parse(hexColor.Substring(0, 2), System.Globalization.NumberStyles.HexNumber);
                    g = int.Parse(hexColor.Substring(2, 2), System.Globalization.NumberStyles.HexNumber);
                    b = int.Parse(hexColor.Substring(4, 2), System.Globalization.NumberStyles.HexNumber);
                }
                else if (hexColor.Length == 3) // 简写形式，如#FFF
                {
                    r = int.Parse(hexColor[0].ToString() + hexColor[0], System.Globalization.NumberStyles.HexNumber);
                    g = int.Parse(hexColor[1].ToString() + hexColor[1], System.Globalization.NumberStyles.HexNumber);
                    b = int.Parse(hexColor[2].ToString() + hexColor[2], System.Globalization.NumberStyles.HexNumber);
                }
                else
                {
                    return Color.Red;
                }
                
                return Color.FromArgb(r, g, b);
            }
            catch
            {
                return Color.Red;
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _updateTimer?.Stop();
                _updateTimer?.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}