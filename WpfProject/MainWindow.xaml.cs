using Biometria;
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
            this.mainImBitmap = new Bitmap("apple.png");
            this.MainIm.Source = CreateBitmapSource(mainImBitmap);
            this.mainImBitmapCopy = new Bitmap("apple.png");
        }

        private BitmapSource CreateBitmapSource(Bitmap bmp)
        {
            using var memoryStream = new MemoryStream();
            bmp.Save(memoryStream, ImageFormat.Png);
            memoryStream.Seek(0, SeekOrigin.Begin);

            var bmpDecoder = BitmapDecoder.Create(memoryStream, BitmapCreateOptions.PreservePixelFormat,
                                                  BitmapCacheOption.OnLoad);

            WriteableBitmap writeable = new WriteableBitmap(bmpDecoder.Frames.Single());
            writeable.Freeze();

            return writeable;
        }

        private void Otsu_button(object sender, RoutedEventArgs e) =>
            this.MainIm.Source = CreateBitmapSource(mainImBitmap = Effects.OtsuBinarizationBCV(mainImBitmap, PixelFormat.Format24bppRgb));
        
        private void Grayscale_button(object sender, RoutedEventArgs e) =>
            this.MainIm.Source = CreateBitmapSource(mainImBitmap = Effects.GrayScale(mainImBitmap, PixelFormat.Format24bppRgb));
        
        private void Blurr_button(object sender, RoutedEventArgs e) => 
           this.MainIm.Source = CreateBitmapSource(mainImBitmap = Effects.Blurr(mainImBitmap, PixelFormat.Format24bppRgb, 1));
        
        private void ClearFilters_button(object sender, RoutedEventArgs e)
        {
            this.MainIm.Source = CreateBitmapSource(mainImBitmapCopy);
            this.mainImBitmap = new Bitmap(mainImBitmapCopy);
        }
        private void Pixelize_button(object sender, RoutedEventArgs e) =>
           this.MainIm.Source = CreateBitmapSource(mainImBitmap = Effects.Pixelize(mainImBitmap, PixelFormat.Format24bppRgb));
        
        private void Median_button(object sender, RoutedEventArgs e) => 
           this.MainIm.Source = CreateBitmapSource(mainImBitmap = Effects.MedianFilter(mainImBitmap, PixelFormat.Format24bppRgb));

        private void Sobel_button(object sender, RoutedEventArgs e) =>
           this.MainIm.Source = CreateBitmapSource(mainImBitmap = Effects.EdgeDetection(mainImBitmap, PixelFormat.Format24bppRgb, Kernel.SOBEL));

        private void Perwitt_button(object sender, RoutedEventArgs e) =>
            this.MainIm.Source = CreateBitmapSource(mainImBitmap = Effects.EdgeDetection(mainImBitmap, PixelFormat.Format24bppRgb, Kernel.PERWITT));

        private void Niblack_button(object sender, RoutedEventArgs e) =>
            this.MainIm.Source = CreateBitmapSource(mainImBitmap = Effects.Niblack(mainImBitmap, PixelFormat.Format24bppRgb));

        private void Savoula_button(object sender, RoutedEventArgs e) =>
            this.MainIm.Source = CreateBitmapSource(mainImBitmap = Effects.Savoula(mainImBitmap, PixelFormat.Format24bppRgb));

        private void Phanscalar_button(object sender, RoutedEventArgs e) =>
            this.MainIm.Source = CreateBitmapSource(mainImBitmap = Effects.Phanscalar(mainImBitmap, PixelFormat.Format24bppRgb));

        private void K3M_button(object sender, RoutedEventArgs e) =>
            this.MainIm.Source = CreateBitmapSource(mainImBitmap = Skeletonization.K3M(mainImBitmap, PixelFormat.Format24bppRgb));

        private void crossing_button(object sender, RoutedEventArgs e)
        {
            Skeletonization histogram = new Skeletonization();
            this.MainIm.Source = CreateBitmapSource(mainImBitmap = histogram.CrossingNumber(mainImBitmap, PixelFormat.Format24bppRgb));

            File.WriteAllText("histogramTraits.txt", 
                $"CROSSES  ::  {histogram.GetTraitsCount(Skeletonization.trait.CROSS)}\n"+
                $"CUTCROSSES  ::  {histogram.GetTraitsCount(Skeletonization.trait.CUTCROSS)}\n"+
                $"TIPS  ::  {histogram.GetTraitsCount(Skeletonization.trait.TIP)}\n"+
                $"PASSAGES  ::  {histogram.GetTraitsCount(Skeletonization.trait.PASSAGE)}\n"
                );
        }

        private void mosaic_button(object sender, RoutedEventArgs e) =>  
            this.MainIm.Source = CreateBitmapSource(mainImBitmap = Effects.Mosaic(mainImBitmap, PixelFormat.Format24bppRgb));

        private void claster_button(object sender, RoutedEventArgs e) =>
             this.MainIm.Source = CreateBitmapSource(mainImBitmap = Effects.Clasterization(mainImBitmap, PixelFormat.Format24bppRgb));
    }
}
