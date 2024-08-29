using System.Windows;
using Windows.Devices.Enumeration;
using Windows.Media.Capture;
using Windows.Media.MediaProperties;
using Windows.Storage.Streams;

namespace MediaCaptureFileDemo
{
    /// <summary>
    /// 摄像头显示Demo，保存录制文件
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }
        private MediaCapture _mediaCapture;
        private InMemoryRandomAccessStream _randomAccessStream;
        private async void StartButton_OnClick(object sender, RoutedEventArgs e)
        {
            // 1. 初始化 MediaCapture 对象
            var mediaCapture = _mediaCapture = new MediaCapture();
            var videos = await DeviceInformation.FindAllAsync(DeviceClass.VideoCapture);
            var settings = new MediaCaptureInitializationSettings()
            {
                VideoDeviceId = videos[0].Id,
                StreamingCaptureMode = StreamingCaptureMode.Video,
            };
            await mediaCapture.InitializeAsync(settings);

            // 2. 设置要录制的数据流
            var randomAccessStream = _randomAccessStream = new InMemoryRandomAccessStream();
            // 3. 配置录制的视频设置
            var mediaEncodingProfile = MediaEncodingProfile.CreateMp4(VideoEncodingQuality.Auto);
            // 4. 开始录制
            await mediaCapture.StartRecordToStreamAsync(mediaEncodingProfile, randomAccessStream);
        }

        private async void StopButton_OnClick(object sender, RoutedEventArgs e)
        {
            // 停止录制
            await _mediaCapture.StopRecordAsync();
            // 处理录制后的数据,保存至"C:\Users\XXX\Videos\RecordedVideo.mp4"
            var storageFolder = Windows.Storage.KnownFolders.VideosLibrary;
            var file = await storageFolder.CreateFileAsync("RecordedVideo.mp4", Windows.Storage.CreationCollisionOption.GenerateUniqueName);
            using var fileStream = await file.OpenAsync(Windows.Storage.FileAccessMode.ReadWrite);
            await RandomAccessStream.CopyAndCloseAsync(_randomAccessStream.GetInputStreamAt(0), fileStream.GetOutputStreamAt(0));
            _randomAccessStream.Dispose();
        }
    }
}