using Microsoft.Kinect;
using System;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace KinectAssist360
{
    public class KinectManager
    {
        private KinectSensor sensor;
        private byte[] colorPixels;
        private WriteableBitmap colorBitmap;
        private Skeleton lastSkeleton;

        public Point BallPosition { get; private set; } = new Point(320, 100);
        private Vector ballVelocity = new Vector(0, 0);
        private const double Gravity = 0.5;
        public int Score { get; private set; } = 0;
        public bool GameOver { get; private set; } = false;

        public event Action<string> StatusChanged;
        public event Action<ImageSource> ColorUpdated;
        public event Action<int> ScoreUpdated;

        public void Initialize()
        {
            sensor = KinectSensor.KinectSensors.FirstOrDefault(s => s.Status == KinectStatus.Connected);
            if (sensor == null)
            {
                StatusChanged?.Invoke("Kinect no detectado");
                return;
            }

            sensor.ColorStream.Enable(ColorImageFormat.RgbResolution640x480Fps30);
            sensor.SkeletonStream.Enable();

            sensor.ColorFrameReady += Sensor_ColorFrameReady;
            sensor.SkeletonFrameReady += Sensor_SkeletonFrameReady;

            try
            {
                sensor.Start();
                StatusChanged?.Invoke("Kinect iniciado");
            }
            catch (IOException)
            {
                StatusChanged?.Invoke("No se pudo iniciar el sensor");
            }

            colorBitmap = new WriteableBitmap(sensor.ColorStream.FrameWidth,
                                              sensor.ColorStream.FrameHeight,
                                              96.0, 96.0, PixelFormats.Bgr32, null);
        }

        public void Shutdown()
        {
            if (sensor != null)
            {
                sensor.Stop();
                sensor.ColorFrameReady -= Sensor_ColorFrameReady;
                sensor.SkeletonFrameReady -= Sensor_SkeletonFrameReady;
                sensor = null;
            }
        }

        private void Sensor_ColorFrameReady(object sender, ColorImageFrameReadyEventArgs e)
        {
            using (var frame = e.OpenColorImageFrame())
            {
                if (frame == null) return;

                if (colorPixels == null || colorPixels.Length != frame.PixelDataLength)
                    colorPixels = new byte[frame.PixelDataLength];

                frame.CopyPixelDataTo(colorPixels);

                colorBitmap.Lock();
                colorBitmap.WritePixels(new Int32Rect(0, 0, colorBitmap.PixelWidth, colorBitmap.PixelHeight),
                                        colorPixels, colorBitmap.BackBufferStride, 0);
                colorBitmap.Unlock();

                ColorUpdated?.Invoke(colorBitmap);
            }
        }

        private void Sensor_SkeletonFrameReady(object sender, SkeletonFrameReadyEventArgs e)
        {
            using (var frame = e.OpenSkeletonFrame())
            {
                if (frame == null) return;

                Skeleton[] skeletons = new Skeleton[frame.SkeletonArrayLength];
                frame.CopySkeletonDataTo(skeletons);

                var skel = skeletons.FirstOrDefault(s => s.TrackingState == SkeletonTrackingState.Tracked);
                if (skel == null) return;

                lastSkeleton = skel;
                UpdateGame(skel);
            }
        }

        private void UpdateGame(Skeleton skel)
        {
            if (GameOver) return;

            ballVelocity.Y += Gravity;
            BallPosition = new Point(BallPosition.X + ballVelocity.X, BallPosition.Y + ballVelocity.Y);

            if (BallPosition.X < 0) { BallPosition = new Point(0, BallPosition.Y); ballVelocity.X *= -0.7; }
            if (BallPosition.X > 640) { BallPosition = new Point(640, BallPosition.Y); ballVelocity.X *= -0.7; }

            var foot = skel.Joints[JointType.FootRight].Position;
            var footPoint = sensor.CoordinateMapper.MapSkeletonPointToColorPoint(foot, sensor.ColorStream.Format);

            if (!float.IsInfinity(footPoint.X) && !float.IsInfinity(footPoint.Y))
            {
                double dx = BallPosition.X - footPoint.X;
                double dy = BallPosition.Y - footPoint.Y;
                double dist = Math.Sqrt(dx * dx + dy * dy);

                const double footRadius = 35;
                if (dist <= footRadius && ballVelocity.Y > 0)
                {
                    ballVelocity = new Vector(ballVelocity.X + dx * 0.05, -Math.Abs(ballVelocity.Y) * 0.9);
                    Score++;
                    ScoreUpdated?.Invoke(Score);
                }
            }

            if (BallPosition.Y >= 470)
            {
                BallPosition = new Point(BallPosition.X, 470);
                ballVelocity = new Vector(0, 0);
                GameOver = true;
                StatusChanged?.Invoke("Game Over!");
            }
        }

        public Point? GetFootRightColorPoint()
        {
            if (sensor == null || lastSkeleton == null) return null;
            var foot = lastSkeleton.Joints[JointType.FootRight].Position;
            var colorPt = sensor.CoordinateMapper.MapSkeletonPointToColorPoint(foot, sensor.ColorStream.Format);
            if (float.IsInfinity(colorPt.X) || float.IsInfinity(colorPt.Y)) return null;
            return new Point(colorPt.X, colorPt.Y);
        }

        public void ResetGame()
        {
            BallPosition = new Point(320, 100);
            ballVelocity = new Vector(0, 0);
            Score = 0;
            GameOver = false;
            ScoreUpdated?.Invoke(Score);
            StatusChanged?.Invoke("Patea!");
        }

        public void SetTiltAngle(int angle)
        {
            try
            {
                if (sensor != null && sensor.IsRunning)
                    sensor.ElevationAngle = angle;
            }
            catch { }
        }
    }
}
