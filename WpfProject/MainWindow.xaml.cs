using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Net.Cache;
using System.Windows;
using System.Windows.Media.Imaging;

namespace WpfProject
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    /// 

    public partial class MainWindow : Window
    {
        private Bitmap mainImBitmap;
        private Bitmap mainImBitmapCopy;
        public MainWindow()
        {
            InitializeComponent();
            this.mainImBitmap = new Bitmap("UDO.jpg");
            this.MainIm.Source = CreateBitmapSource(mainImBitmap);
            this.mainImBitmapCopy = new Bitmap("UDO.jpg");
        }

        private BitmapSource CreateBitmapSource(Bitmap bmp)
        {
            using var memoryStream = new MemoryStream();
            bmp.Save(memoryStream, ImageFormat.Jpeg);
            memoryStream.Seek(0, SeekOrigin.Begin);

            var bmpDecoder = BitmapDecoder.Create(memoryStream, BitmapCreateOptions.PreservePixelFormat,
                                                  BitmapCacheOption.OnLoad);

            WriteableBitmap writeable = new WriteableBitmap(bmpDecoder.Frames.Single());
            writeable.Freeze();

            return writeable;
        }

        private void Otsu_button(object sender, RoutedEventArgs e)
        {
            this.MainIm.Source = CreateBitmapSource(Effects.OtsuBinarizationBCV(mainImBitmap, PixelFormat.Format32bppPArgb));
        }

        private void Grayscale_button(object sender, RoutedEventArgs e)
        {
            this.MainIm.Source = CreateBitmapSource(Effects.GrayScale(mainImBitmap, PixelFormat.Format32bppPArgb));
        }

        private void Blurr_button(object sender, RoutedEventArgs e)
        {
            this.MainIm.Source = CreateBitmapSource(Effects.Blurr(mainImBitmap, PixelFormat.Format32bppPArgb));
        }

        private void ClearFilters_button(object sender, RoutedEventArgs e)
        {
            this.MainIm.Source = CreateBitmapSource(mainImBitmapCopy);
            this.mainImBitmap = new Bitmap(mainImBitmapCopy);

        }

        private void Pixelize_button(object sender, RoutedEventArgs e)
        {
            this.MainIm.Source = CreateBitmapSource(Effects.Pixelize(mainImBitmap, PixelFormat.Format32bppPArgb));
        }
    }
}
