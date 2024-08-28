using System.Windows;
using Windows.Devices.Enumeration;
using Windows.Media.Capture;
using Windows.UI.Xaml.Controls;
using Button = Windows.UI.Xaml.Controls.Button;

namespace MediaCapturePreviewDemo
{
    /// <summary>
    /// 摄像头显示Demo，通过UWP-WindowsXamlHost承载画面（置顶），直接用CaptureElement渲染速度很快。实现逻辑同windows系统相机
    /// </summary>
    public partial class MainWindow : Window
    {
        private CaptureElement _captureElement;
        private MediaCapture _mediaCapture;

        public MainWindow()
        {
            InitializeComponent();
            AddUwpElement();
        }
        private void AddUwpElement()
        {
            Windows.UI.Xaml.Controls.Grid mainGrid = new Windows.UI.Xaml.Controls.Grid();
            var captureElement = _captureElement = new CaptureElement()
            {
                Stretch = Windows.UI.Xaml.Media.Stretch.UniformToFill
            };
            mainGrid.Children.Add(captureElement);
            var button = new Button()
            {
                VerticalAlignment = Windows.UI.Xaml.VerticalAlignment.Bottom,
                HorizontalAlignment = Windows.UI.Xaml.HorizontalAlignment.Center,
                Content = "关闭",
                Width = 80
            };
            button.Click += CloseButton_Click;
            mainGrid.Children.Add(button);
            VideoViewHost.Child = mainGrid;
        }

        private void CloseButton_Click(object sender, Windows.UI.Xaml.RoutedEventArgs e)
        {
            VideoViewHost.Visibility = Visibility.Collapsed;
            _mediaCapture.StopPreviewAsync();
        }

        private async void StartButton_OnClick(object sender, RoutedEventArgs e)
        {
            _mediaCapture = new MediaCapture();
            var videos = await DeviceInformation.FindAllAsync(DeviceClass.VideoCapture);
            var settings = new MediaCaptureInitializationSettings()
            {
                VideoDeviceId = videos[0].Id,
                StreamingCaptureMode = StreamingCaptureMode.Video,
            };
            await _mediaCapture.InitializeAsync(settings);

            VideoViewHost.Visibility = Visibility.Visible;
            _captureElement.Source = _mediaCapture;
            await _mediaCapture.StartPreviewAsync();
        }
    }
}