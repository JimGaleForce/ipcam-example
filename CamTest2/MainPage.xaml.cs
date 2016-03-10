namespace CamTest2
{
    using System;
    using System.IO;
    using System.Runtime.InteropServices.WindowsRuntime;

    using MjpegProcessor;

    using Windows.Foundation;
    using Windows.Storage.Streams;
    using Windows.UI.Core;
    using Windows.UI.Xaml;
    using Windows.UI.Xaml.Controls;
    using Windows.UI.Xaml.Media.Imaging;

    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        // Source: https://channel9.msdn.com/coding4fun/articles/MJPEG-Decoder
        // Source: http://mjpeg.codeplex.com/
        private readonly MjpegDecoder mjpeg;

        public MainPage()
        {
            this.InitializeComponent();
            this.mjpeg = new MjpegDecoder();
            this.mjpeg.FrameReady += this._mjpeg_FrameReady;
            this.mjpeg.Error += this._mjpeg_Error;
        }

        private async void _mjpeg_Error(object sender, ErrorEventArgs e)
        {
            await this.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => { this.errors.Text = e.Message; });
        }

        private async void _mjpeg_FrameReady(object sender, FrameReadyEventArgs e)
        {
            // get it on the UI thread
            await this.Dispatcher.RunAsync(
                CoreDispatcherPriority.Normal, 
                async () =>
                    {
                        // create a new BitmapImage from the JPEG bytes
                        var img = new BitmapImage();

                        var ras = new MemoryRandomAccessStream(e.FrameBuffer.ToArray());

                        await img.SetSourceAsync(ras);
                        this.image.Source = img;
                    });
        }

        private void Btn_Stop(object sender, RoutedEventArgs e)
        {
            this.mjpeg?.StopStream();
        }

        private void Btn_Go(object sender, RoutedEventArgs e)
        {
            this.errors.Text = string.Empty;

            var path = this.camtype1.IsChecked.Value
                           ? "/videostream.cgi?user={0}&pwd={1}"
                           : "/cgi-bin/CGIStream.cgi?cmd=GetMJStream&usr={0}&pwd={1}";

            var url = "http://" + this.ip.Text.Trim() + string.Format(path, this.user.Text.Trim(), this.pwd.Text.Trim());

            this.mjpeg.ParseStream(new Uri(url));

            //source: http://www.ipcam-shop.nl/media/Foscam%20H264%20FW/How%20to%20fetch%20MJPEG%20stream%20on%20the%20FI9821W.pdf
            //do this first before cam2, to enable MJPEG: http://<URL>/cgibin/CGIProxy.fcgi?usr=USR&pwd=PWD&cmd=setSubStreamFormat&format=1
        }
    }

    // Source: https://canbilgin.wordpress.com/2012/06/06/how-to-convert-byte-array-to-irandomaccessstream/
    internal class MemoryRandomAccessStream : IRandomAccessStream
    {
        private readonly Stream m_InternalStream;

        public MemoryRandomAccessStream(Stream stream)
        {
            this.m_InternalStream = stream;
        }

        public MemoryRandomAccessStream(byte[] bytes)
        {
            this.m_InternalStream = new MemoryStream(bytes);
        }

        public IInputStream GetInputStreamAt(ulong position)
        {
            this.m_InternalStream.Seek((long)position, SeekOrigin.Begin);

            return this.m_InternalStream.AsInputStream();
        }

        public IOutputStream GetOutputStreamAt(ulong position)
        {
            this.m_InternalStream.Seek((long)position, SeekOrigin.Begin);

            return this.m_InternalStream.AsOutputStream();
        }

        public ulong Size
        {
            get
            {
                return (ulong)this.m_InternalStream.Length;
            }

            set
            {
                this.m_InternalStream.SetLength((long)value);
            }
        }

        public bool CanRead
        {
            get
            {
                return true;
            }
        }

        public bool CanWrite
        {
            get
            {
                return true;
            }
        }

        public IRandomAccessStream CloneStream()
        {
            throw new NotSupportedException();
        }

        public ulong Position
        {
            get
            {
                return (ulong)this.m_InternalStream.Position;
            }
        }

        public void Seek(ulong position)
        {
            this.m_InternalStream.Seek((long)position, 0);
        }

        public void Dispose()
        {
            this.m_InternalStream.Dispose();
        }

        public IAsyncOperationWithProgress<IBuffer, uint> ReadAsync(
            IBuffer buffer, 
            uint count, 
            InputStreamOptions options)
        {
            var inputStream = this.GetInputStreamAt(0);
            return inputStream.ReadAsync(buffer, count, options);
        }

        public IAsyncOperation<bool> FlushAsync()
        {
            var outputStream = this.GetOutputStreamAt(0);
            return outputStream.FlushAsync();
        }

        public IAsyncOperationWithProgress<uint, uint> WriteAsync(IBuffer buffer)
        {
            var outputStream = this.GetOutputStreamAt(0);
            return outputStream.WriteAsync(buffer);
        }
    }
}