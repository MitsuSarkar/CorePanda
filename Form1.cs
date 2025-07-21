using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using LibreHardwareMonitor.Hardware;

namespace CorePanda
{
    public class Form1 : Form
    {
        private readonly NotifyIcon trayIcon;
        private readonly ContextMenuStrip menu;
        private readonly System.Windows.Forms.Timer timer;
        private readonly PerformanceCounter cpuCounter;

        private Image[] gifFrames = Array.Empty<Image>();
        private int[] frameDelays = Array.Empty<int>();
        private int currentFrame = 0;

        private bool showCpuUsage = true;
        private bool showCpuTemp = true;

        private readonly Queue<float> cpuSamples = new Queue<float>();
        private const int maxSamples = 10;

        public bool ShowCpuUsage
        {
            get => showCpuUsage;
            set => showCpuUsage = value;
        }

        public bool ShowCpuTemp
        {
            get => showCpuTemp;
            set => showCpuTemp = value;
        }

        public Form1()
        {
            this.ShowInTaskbar = false;
            this.WindowState = FormWindowState.Minimized;
            this.Opacity = 0;
            this.FormBorderStyle = FormBorderStyle.FixedToolWindow;

            menu = new ContextMenuStrip();
            menu.Items.Add("Settings", null, OpenSettings);
            menu.Items.Add("Choose Animation", null, ChooseAnimation);
            menu.Items.Add("Exit", null, (s, e) => Application.Exit());

            trayIcon = new NotifyIcon
            {
                Icon = new Icon("logo.ico"),
                Text = "CorePanda",
                ContextMenuStrip = menu,
                Visible = true
            };

            cpuCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total");

            timer = new System.Windows.Forms.Timer();
            timer.Tick += Timer_Tick;
            timer.Interval = 100;
            timer.Start();

            Application.ApplicationExit += (s, e) =>
            {
                foreach (var frame in gifFrames) frame.Dispose();
                trayIcon.Dispose();
            };

            this.Load += (s, e) => { this.Hide(); };
        }

        private void Timer_Tick(object? sender, EventArgs e)
        {
            try
            {
                float cpuSample = cpuCounter.NextValue();

                cpuSamples.Enqueue(cpuSample);
                if (cpuSamples.Count > maxSamples)
                    cpuSamples.Dequeue();

                float cpu = cpuSamples.Average();

                string temp = GetCpuTemperature();

                string tooltip = "CorePanda";

                if (showCpuUsage)
                    tooltip += $"\nCPU: {cpu:F1}%";
                if (showCpuTemp)
                    tooltip += $" | Temp: {temp}";

                trayIcon.Text = tooltip;

                if (gifFrames.Length > 0)
                {
                    using var bmp = new Bitmap(16, 16, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
                    using (Graphics g = Graphics.FromImage(bmp))
                    {
                        g.Clear(Color.Transparent);
                        g.DrawImage(gifFrames[currentFrame], 0, 0, 16, 16);
                    }

                    Icon icon = Icon.FromHandle(bmp.GetHicon());
                    trayIcon.Icon = icon;

                    int baseDelay = (frameDelays.Length > currentFrame) ? frameDelays[currentFrame] : 100;

                    double factor = 1.5 - Math.Min(cpu, 100) / 100.0;
                    if (factor < 0.5) factor = 0.5;
                    if (factor > 2.0) factor = 2.0;

                    int adjustedDelay = (int)(baseDelay * factor);
                    if (adjustedDelay < 20) adjustedDelay = 20;

                    timer.Interval = adjustedDelay;

                    currentFrame = (currentFrame + 1) % gifFrames.Length;
                }
                else
                {
                    trayIcon.Icon = new Icon("logo.ico");
                    timer.Interval = 100;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Timer error: {ex.Message}");
            }
        }

        private void ChooseAnimation(object? sender, EventArgs e)
        {
            using var dialog = new OpenFileDialog();
            dialog.Filter = "GIF files (*.gif)|*.gif";
            if (dialog.ShowDialog() == DialogResult.OK)
            {
                LoadGifFrames(dialog.FileName);
                currentFrame = 0;
            }
        }

        private void LoadGifFrames(string gifPath)
        {
            try
            {
                using var gifImg = Image.FromFile(gifPath);
                int frameCount = gifImg.GetFrameCount(System.Drawing.Imaging.FrameDimension.Time);

                var framesList = new List<Image>();
                var delaysList = new List<int>();

                var item = gifImg.GetPropertyItem(0x5100); // FrameDelay
                byte[] values = item.Value;

                for (int i = 0; i < frameCount; i++)
                {
                    gifImg.SelectActiveFrame(System.Drawing.Imaging.FrameDimension.Time, i);

                    Bitmap bmp = new Bitmap(gifImg.Width, gifImg.Height);
                    using (Graphics g = Graphics.FromImage(bmp))
                    {
                        g.DrawImage(gifImg, Point.Empty);
                    }
                    framesList.Add(new Bitmap(bmp));

                    int delay = (values[i * 4] + (values[i * 4 + 1] << 8)) * 10; // 1/100s → ms
                    if (delay <= 0) delay = 100;
                    delaysList.Add(delay);
                }

                foreach (var img in gifFrames)
                    img.Dispose();

                gifFrames = framesList.ToArray();
                frameDelays = delaysList.ToArray();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to load GIF: {ex.Message}");
            }
        }

        private string GetCpuTemperature()
        {
            try
            {
                Computer computer = new Computer { IsCpuEnabled = true };
                computer.Open();

                foreach (IHardware hardware in computer.Hardware)
                {
                    if (hardware.HardwareType == HardwareType.Cpu)
                    {
                        hardware.Update();
                        foreach (ISensor sensor in hardware.Sensors)
                        {
                            if (sensor.SensorType == SensorType.Temperature &&
                                (sensor.Name.Contains("Core") || sensor.Name.Contains("Package")))
                            {
                                if (sensor.Value.HasValue)
                                    return $"{sensor.Value.Value:F1}°C";
                            }
                        }
                    }
                }

                computer.Close();
            }
            catch
            {
                return "N/A";
            }

            return "N/A";
        }

        private void OpenSettings(object? sender, EventArgs e)
        {
            var settingsForm = new SettingsForm(this);
            settingsForm.ShowDialog();
        }
    }
}
