using Windows.Graphics.Imaging;
using Windows.Media.Capture;
using Windows.Media.Capture.Frames;
using Windows.Media.MediaProperties;
using Windows.Storage.Streams;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using BitmapEncoder = Windows.Graphics.Imaging.BitmapEncoder;
using RoutedEventArgs = System.Windows.RoutedEventArgs;
using Window = System.Windows.Window;
using Visibility = System.Windows.Visibility;

namespace MediaCapturePreviewByFrameDemo
{
    /// <summary>
    /// 摄像头显示Demo，通过UWP-WindowsXamlHost承载画面（置顶）
    /// 监听FrameArrived，使用Windows.UI.Xaml.Media.Imaging.BitmapImage渲染展示（仅用于展示，延迟很高）
    /// </summary>
    public partial class MainWindow : Window
    {
        private MediaFrameReader _frameReader;
        private Image _captureImage;

        public MainWindow()
        {
            InitializeComponent();
            AddUwpElement();
        }
        private void AddUwpElement()
        {
            Windows.UI.Xaml.Controls.Grid mainGrid = new Windows.UI.Xaml.Controls.Grid();
            var image = _captureImage = new Image()
            {
                Stretch = Stretch.UniformToFill
            };
            mainGrid.Children.Add(image);
            VideoViewHost.Child = mainGrid;
        }

        private async void StartButton_OnClick(object sender, RoutedEventArgs e)
        {
            VideoViewHost.Visibility = Visibility.Visible;

            // 1. 初始化 MediaCapture 对象
            var mediaCapture = new MediaCapture();
            var settings = new MediaCaptureInitializationSettings()
            {
                MemoryPreference = MediaCaptureMemoryPreference.Cpu,
                StreamingCaptureMode = StreamingCaptureMode.Video,
            };
            await mediaCapture.InitializeAsync(settings);

            // 配置视频帧读取器
            var frameSource = mediaCapture.FrameSources.Values.FirstOrDefault(source => source.Info.MediaStreamType == MediaStreamType.VideoRecord);
            _frameReader = await mediaCapture.CreateFrameReaderAsync(frameSource, MediaEncodingSubtypes.Argb32);
            _frameReader.FrameArrived += FrameReader_FrameArrived;
            await _frameReader.StartAsync();
        }
        private async void FrameReader_FrameArrived(MediaFrameReader sender, MediaFrameArrivedEventArgs args)
        {
            var frame = sender.TryAcquireLatestFrame();
            if (frame != null)
            {
                var bitmap = frame.VideoMediaFrame?.SoftwareBitmap;
                if (bitmap != null)
                {
                    // 在这里对每一帧进行处理
                    await Dispatcher.InvokeAsync(async () =>
                    {
                        var bitmapImage = await ConvertSoftwareBitmapToBitmapImageAsync(bitmap);
                        _captureImage.Source = bitmapImage;
                    });
                }
            }
        }

        private async Task<Windows.UI.Xaml.Media.Imaging.BitmapImage> ConvertSoftwareBitmapToBitmapImageAsync(SoftwareBitmap softwareBitmap)
        {
            var bitmapImage = new Windows.UI.Xaml.Media.Imaging.BitmapImage();
            using (var stream = new InMemoryRandomAccessStream())
            {
                BitmapEncoder encoder = await BitmapEncoder.CreateAsync(BitmapEncoder.PngEncoderId, stream);
                encoder.SetSoftwareBitmap(softwareBitmap);
                await encoder.FlushAsync();
                stream.Seek(0);
                await bitmapImage.SetSourceAsync(stream);
            }
            return bitmapImage;
        }
        private async void StopButton_OnClick(object sender, RoutedEventArgs e)
        {
            await _frameReader.StopAsync();
        }
    }
}