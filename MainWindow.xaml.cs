using Microsoft.Kinect;
using System;
using System.IO.Ports;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace KinectAssist360
{
    public partial class MainWindow : Window
    {
        private KinectManager km;
        private Ellipse ballEllipse;
        private Ellipse footGuide;
        private DispatcherTimer timer;
        private SerialPort serialPort;
        private int lastTilt = 0;

        public MainWindow()
        {
            InitializeComponent();

            Loaded += MainWindow_Loaded;
            Closing += MainWindow_Closing;
            btnReset.Click += BtnReset_Click;

            ballEllipse = new Ellipse() { Width = 80, Height = 80, Fill = Brushes.Red }; // pelota roja
            footGuide = new Ellipse() { Width = 30, Height = 30, Fill = Brushes.LimeGreen }; // footguide
            GameCanvas.Children.Add(ballEllipse);
            GameCanvas.Children.Add(footGuide);
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            km = new KinectManager();
            km.StatusChanged += Km_StatusChanged;
            km.ColorUpdated += Km_ColorUpdated;
            km.ScoreUpdated += Km_ScoreUpdated;
            km.Initialize();

            // arduino serial (Recordar cambiar COM)
            try
            {
                serialPort = new SerialPort("COM9", 9600);
                serialPort.DataReceived += SerialPort_DataReceived;
                serialPort.Open();
                StatusLabel.Content = "Arduino conectado en COM9.";
            }
            catch
            {
                StatusLabel.Content = "Error al conectar con Arduino. Posible COM mal compilado?";
            }

            timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromMilliseconds(30);
            timer.Tick += Timer_Tick;
            timer.Start();
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            Canvas.SetLeft(ballEllipse, km.BallPosition.X - ballEllipse.Width / 2);
            Canvas.SetTop(ballEllipse, km.BallPosition.Y - ballEllipse.Height / 2);

            var foot = km.GetFootRightColorPoint();
            if (foot.HasValue)
            {
                var pt = foot.Value;
                Canvas.SetLeft(footGuide, pt.X - footGuide.Width / 2);
                Canvas.SetTop(footGuide, pt.Y - footGuide.Height / 2);
                footGuide.Visibility = Visibility.Visible;
            }
            else
                footGuide.Visibility = Visibility.Hidden;
        }

        private void SerialPort_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            try
            {
                string line = serialPort.ReadLine().Trim();
                if (line.StartsWith("ANGLE:"))
                {
                    int angle = int.Parse(line.Substring(6));
                    angle = Math.Max(-27, Math.Min(27, angle));

                    Dispatcher.Invoke(() =>
                    {
                        lblAngle.Content = $"Tilt: {angle}°";
                        km.SetTiltAngle(angle);
                    });
                }
            }
            catch { }
        }

        private void BtnReset_Click(object sender, RoutedEventArgs e)
        {
            km.ResetGame();
        }

        private void Km_ScoreUpdated(int score)
        {
            Dispatcher.Invoke(() => ScoreLabel.Content = "Score: " + score);
        }

        private void Km_ColorUpdated(ImageSource img)
        {
            Dispatcher.Invoke(() => GameImage.Source = img);
        }

        private void Km_StatusChanged(string msg)
        {
            Dispatcher.Invoke(() => StatusLabel.Content = "Estado: " + msg);
        }

        private void MainWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            timer?.Stop();
            km?.Shutdown();
            if (serialPort?.IsOpen == true)
                serialPort.Close();
        }
    }
}
