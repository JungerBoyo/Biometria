using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Text;

namespace WpfProject
{
    static class BitmapsHandler
    {
        public static BitmapData LockBits(Bitmap bmp, ImageLockMode LockMode = ImageLockMode.ReadWrite, PixelFormat format = PixelFormat.Format32bppArgb)
            => bmp.LockBits(new Rectangle(Point.Empty, bmp.Size), LockMode, format);

        public static void CreateOffsetMatrix(out int[] Matrix, int size, int stride, int pixelFrmTrait)
        {
            int sizeStride = 2 * size + 1;
            int[] matrix = new int[sizeStride * sizeStride];

            for(int i=(-size), k=0; i<=size; i++)
                for(int j=-(size*pixelFrmTrait); j<=size * pixelFrmTrait; j+=pixelFrmTrait, k++)  
                    matrix[k] = i * stride + j;
                          
            Matrix = matrix;
        }
    }
}
