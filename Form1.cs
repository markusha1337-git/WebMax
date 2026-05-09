#nullable enable
using System;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using Microsoft.Web.WebView2.WinForms;
using Microsoft.Web.WebView2.Core;

namespace WebMax
{
    public partial class Form1 : Form
    {
        private WebView2? webView = null!;
        private Panel? titleBar = null!;
        private Label? titleLabel = null!;
        private Button? closeButton = null!;
        private Button? minimizeButton = null!;
        private Button? backButton = null!;
        private Button? forwardButton = null!;
        private Button? refreshButton = null!;
        private Button? homeButton = null!;
        private NotifyIcon? trayIcon = null!;
        private ContextMenuStrip? trayMenu = null!;

        [DllImport("user32.dll")]
        private static extern int SendMessage(IntPtr hWnd, int Msg, int wParam, int lParam);
        [DllImport("user32.dll")]
        private static extern bool ReleaseCapture();
        
        private const int WM_NCLBUTTONDOWN = 0xA1;
        private const int HT_CAPTION = 0x2;

        public Form1()
        {
            InitializeComponent();
            InitializeForm();
            CreateTrayIcon();
            CreateTitleBar();
            InitializeWebView();
        }

        private void InitializeForm()
        {
            this.Text = "WebMax";
            this.Size = new Size(1280, 800);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.None;
            this.BackColor = Color.FromArgb(32, 32, 32);
            this.MinimumSize = new Size(800, 600);
            
            // При закрытии - сворачиваем в трей
            this.FormClosing += Form1_FormClosing;
            
            try
            {
                using var stream = GetEmbeddedResource("ico.ico");
                if (stream != null)
                {
                    this.Icon = new Icon(stream);
                }
            }
            catch { }
        }

private void CreateTrayIcon()
{
    trayMenu = new ContextMenuStrip();
    
    var showItem = new ToolStripMenuItem("Открыть WebMax");
    showItem.Click += (s, e) => ShowFromTray();
    showItem.Font = new Font("Segoe UI", 9, FontStyle.Bold);
    
    var separator = new ToolStripSeparator();
    
    var exitItem = new ToolStripMenuItem("Закрыть");
    exitItem.Click += (s, e) => ExitApplication();
    
    trayMenu.Items.Add(showItem);
    trayMenu.Items.Add(separator);
    trayMenu.Items.Add(exitItem);

    trayIcon = new NotifyIcon
    {
        Text = "WebMax",
        ContextMenuStrip = trayMenu,
        Visible = false
    };

    try
    {
        using var stream = GetEmbeddedResource("ico.ico");
        if (stream != null)
        {
            trayIcon.Icon = new Icon(stream);
        }
    }
    catch { }

    trayIcon.DoubleClick += (s, e) => ShowFromTray();
}

        private void Form1_FormClosing(object? sender, FormClosingEventArgs e)
        {
            // При закрытии на крестик - сворачиваем в трей
            if (e.CloseReason == CloseReason.UserClosing)
            {
                e.Cancel = true;
                HideToTray();
            }
        }

        private void HideToTray()
        {
            this.Hide();
            if (trayIcon != null)
            {
                trayIcon.Visible = true;
                trayIcon.ShowBalloonTip(3000, "WebMax", "Приложение свернуто в трей", ToolTipIcon.Info);
            }
        }

        private void ShowFromTray()
        {
            this.Show();
            this.WindowState = FormWindowState.Normal;
            this.Activate();
            if (trayIcon != null)
            {
                trayIcon.Visible = false;
            }
        }

        private void ExitApplication()
        {
            if (trayIcon != null)
            {
                trayIcon.Visible = false;
                trayIcon.Dispose();
            }
            
            // Отключаем обработчик чтобы действительно закрыть
            this.FormClosing -= Form1_FormClosing;
            Application.Exit();
        }

        private void CreateTitleBar()
        {
            titleBar = new Panel
            {
                Height = 45,
                Dock = DockStyle.Top,
                BackColor = Color.FromArgb(45, 45, 48)
            };

            titleLabel = new Label
            {
                Text = "WebMax",
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 10, FontStyle.Regular),
                Location = new Point(140, 12),
                AutoSize = true
            };

            backButton = CreateImageButton("back.png", 10, 10, 25, 25);
            backButton.Click += (s, e) => 
            { 
                if (webView?.CoreWebView2?.CanGoBack == true) 
                    webView.CoreWebView2.GoBack(); 
            };

            forwardButton = CreateImageButton("forward.png", 40, 10, 25, 25);
            forwardButton.Click += (s, e) => 
            { 
                if (webView?.CoreWebView2?.CanGoForward == true) 
                    webView.CoreWebView2.GoForward(); 
            };

            refreshButton = CreateImageButton("refresh.png", 70, 10, 25, 25);
            refreshButton.Click += (s, e) => webView?.Reload();

            homeButton = CreateImageButton("home.png", 100, 10, 25, 25);
            homeButton.Click += (s, e) => webView?.CoreWebView2?.Navigate("https://web.max.ru");

            minimizeButton = CreateImageButton("minimize.png", this.Width - 80, 10, 25, 25);
            minimizeButton.Click += (s, e) => this.WindowState = FormWindowState.Minimized;

            closeButton = CreateImageButton("close.png", this.Width - 45, 10, 25, 25);
            closeButton.Click += (s, e) => this.Close();
            closeButton.MouseEnter += (s, e) => closeButton.BackColor = Color.FromArgb(232, 17, 35);
            closeButton.MouseLeave += (s, e) => closeButton.BackColor = Color.Transparent;

            titleBar.Controls.AddRange(new Control[] 
            { 
                backButton, forwardButton, refreshButton, homeButton,
                titleLabel, minimizeButton, closeButton 
            });

            titleBar.MouseDown += (s, e) => 
            {
                if (e.Button == MouseButtons.Left)
                {
                    ReleaseCapture();
                    SendMessage(this.Handle, WM_NCLBUTTONDOWN, HT_CAPTION, 0);
                }
            };
            
            titleLabel.MouseDown += (s, e) => 
            {
                if (e.Button == MouseButtons.Left)
                {
                    ReleaseCapture();
                    SendMessage(this.Handle, WM_NCLBUTTONDOWN, HT_CAPTION, 0);
                }
            };

            this.Controls.Add(titleBar);
        }

        private Button CreateImageButton(string resourceName, int x, int y, int width, int height)
        {
            Button button = new Button
            {
                FlatStyle = FlatStyle.Flat,
                FlatAppearance = { BorderSize = 0 },
                BackColor = Color.Transparent,
                Size = new Size(width, height),
                Location = new Point(x, y),
                Text = "",
                ImageAlign = ContentAlignment.MiddleCenter,
                Cursor = Cursors.Hand
            };

            try
            {
                using var stream = GetEmbeddedResource(resourceName);
                if (stream != null)
                {
                    button.Image = new Bitmap(stream);
                    button.Image = new Bitmap(button.Image, new Size(16, 16));
                }
                else
                {
                    button.Text = GetFallbackSymbol(resourceName);
                    button.ForeColor = Color.White;
                    button.Font = new Font("Segoe UI", 8);
                    button.TextAlign = ContentAlignment.MiddleCenter;
                }
            }
            catch
            {
                button.Text = GetFallbackSymbol(resourceName);
                button.ForeColor = Color.White;
                button.Font = new Font("Segoe UI", 8);
                button.TextAlign = ContentAlignment.MiddleCenter;
            }

            button.MouseEnter += (s, e) => 
            {
                if (button != closeButton)
                    button.BackColor = Color.FromArgb(70, 70, 70);
            };
            button.MouseLeave += (s, e) => 
            {
                if (button != closeButton)
                    button.BackColor = Color.Transparent;
            };

            return button;
        }

        private Stream? GetEmbeddedResource(string name)
        {
            var assembly = Assembly.GetExecutingAssembly();
            var resourcePath = $"WebMax.{name}";
            return assembly.GetManifestResourceStream(resourcePath);
        }

        private string GetFallbackSymbol(string resourceName)
        {
            string filename = Path.GetFileNameWithoutExtension(resourceName).ToLower();
            return filename switch
            {
                "back" => "◀",
                "forward" => "▶",
                "refresh" => "↻",
                "home" => "⌂",
                "minimize" => "─",
                "close" => "✕",
                _ => "●"
            };
        }

        private async void InitializeWebView()
        {
            var userDataFolder = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "WebMax",
                "WebView2"
            );

            var env = await CoreWebView2Environment.CreateAsync(
                browserExecutableFolder: null,
                userDataFolder: userDataFolder
            );

            webView = new WebView2
            {
                Dock = DockStyle.Fill
            };
            
            this.Controls.Add(webView);
            webView.BringToFront();
            
            try
            {
                await webView.EnsureCoreWebView2Async(env);
                webView.CoreWebView2.Navigate("https://web.max.ru");
                
                webView.CoreWebView2.DocumentTitleChanged += (s, e) =>
                {
                    this.Invoke(new Action(() =>
                    {
                        if (titleLabel != null)
                            titleLabel.Text = webView.CoreWebView2.DocumentTitle ?? "WebMax";
                    }));
                };
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки: {ex.Message}", "WebMax", 
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            if (minimizeButton != null && closeButton != null)
            {
                minimizeButton.Location = new Point(this.Width - 80, 10);
                closeButton.Location = new Point(this.Width - 45, 10);
            }
        }
    }
}