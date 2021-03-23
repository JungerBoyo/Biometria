using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Text;
using System.Windows.Documents.Serialization;

namespace WpfProject
{
    static class BitmapsHandler
    {
        public static BitmapData LockBits(Bitmap bmp, ImageLockMode LockMode, PixelFormat format)
            => bmp.LockBits(new Rectangle(Point.Empty, bmp.Size), LockMode, format);

        public static void CreateOffsetMatrix(out int[] Matrix, int size, int width, int stride)
        {
            int sizeStride = 2 * size + 1;
            int[] matrix = new int[sizeStride * sizeStride];

            for(int i=(-size), k=0; i<=size; i++)
                for(int j=-(size*stride); j<=size * stride; j+=stride, k++)  
                    matrix[k] = i * width + j;
                          
            Matrix = matrix;
        }

        public unsafe static Bitmap FillWithColor(Bitmap bmp, Color color, PixelFormat format)
        {
            int stride = (format == PixelFormat.Format32bppPArgb || format == PixelFormat.Format32bppArgb) ? 4 : 3;
            BitmapData write = LockBits(bmp, ImageLockMode.WriteOnly, format);
            byte* ptr = (byte*)write.Scan0.ToPointer();

            for (int i = 0; i < write.Stride * write.Height; i += stride)
            {
                ptr[i] = color.R;
                ptr[i + 1] = color.G;
                ptr[i + 2] = color.B;
            }

            bmp.UnlockBits(write);
            return bmp;
        }
    }
}
