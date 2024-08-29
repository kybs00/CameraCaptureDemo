using System.IO;
using System.Windows.Media.Imaging;
using Windows.Graphics.Imaging;
using Windows.Media.Capture;
using Windows.Media.Capture.Frames;
using Windows.Media.MediaProperties;
using Windows.Storage.Streams;
using BitmapEncoder = Windows.Graphics.Imaging.BitmapEncoder;
using RoutedEventArgs = System.Windows.RoutedEventArgs;
using Window = System.Windows.Window;

namespace MediaCapturePreviewByBytesDemo
{
    /// <summary>
    /// 摄像头显示Demo
    /// 监听FrameArrived，获取到字节帧数据（流媒体，可用于摄像头数据采集）。使用WPF-Image渲染展示（仅用于展示，延迟很高）
    /// </summary>
    public partial class MainWindow : Window
    {
        private MediaFrameReader _frameReader;

        public MainWindow()
        {
            InitializeComponent();
        }

        private async void StartButton_OnClick(object sender, RoutedEventArgs e)
        {
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
                    var bytes = await SoftwareBitmapToByteArrayAsync(bitmap);
                    //转化为System.Windows.Media.ImageSource，用于WPF控件显示
                    var bitmapImage = ByteArrayToBitmapImage(bytes);
                    await Dispatcher.InvokeAsync(() =>
                    {
                        CaptureImage.Source = bitmapImage;
                    });
                    bitmap.Dispose();
                }
            }
        }
        public System.Windows.Media.Imaging.BitmapImage ByteArrayToBitmapImage(byte[] imageData)
        {
            var bitmapImage = new System.Windows.Media.Imaging.BitmapImage();
            using (MemoryStream stream = new MemoryStream(imageData))
            {
                stream.Position = 0;
                bitmapImage.BeginInit();
                bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                bitmapImage.StreamSource = stream;
                bitmapImage.EndInit();
                bitmapImage.Freeze(); // To make it cross-thread accessible
            }
            return bitmapImage;
        }
    public async Task<byte[]> SoftwareBitmapToByteArrayAsync(SoftwareBitmap softwareBitmap)
    {
        // 使用InMemoryRandomAccessStream来存储图像数据
        using var stream = new InMemoryRandomAccessStream();
        // 创建位图编码器
        var encoder = await BitmapEncoder.CreateAsync(BitmapEncoder.PngEncoderId, stream);
        // 转换为BGRA8格式，如果当前格式不同
        var bitmap = SoftwareBitmap.Convert(softwareBitmap, BitmapPixelFormat.Bgra8, BitmapAlphaMode.Premultiplied);
        encoder.SetSoftwareBitmap(bitmap);
        await encoder.FlushAsync();
        bitmap.Dispose();

        // 读取字节数据
        using var reader = new DataReader(stream.GetInputStreamAt(0));
        byte[] byteArray = new byte[stream.Size];
        await reader.LoadAsync((uint)stream.Size);
        reader.ReadBytes(byteArray);

        return byteArray;
    }

        private async void StopButton_OnClick(object sender, RoutedEventArgs e)
        {
            await _frameReader.StopAsync();
        }
    }
}