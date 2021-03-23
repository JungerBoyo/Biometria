using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Text;
using System.Windows.Controls;
using System.Windows.Shell;
using WpfProject;

namespace Biometria
{
    unsafe static class K3M
    { 
        public static Bitmap alg(Bitmap bmp, PixelFormat format)
        {
            int stride = (format == PixelFormat.Format32bppPArgb || format == PixelFormat.Format32bppArgb) ? 4 : 3;

            BitmapData readwrite = BitmapsHandler.LockBits(bmp, ImageLockMode.ReadWrite, format);

            byte* ptr = (byte*)readwrite.Scan0.ToPointer();

            int width = readwrite.Stride;
            int length = width * readwrite.Height;

            int[] offsetMat;
            BitmapsHandler.CreateOffsetMatrix(out offsetMat, 1, width, stride);

            List<int[]> phases = new List<int[]>(5);
            phases.Add(P1);
            phases.Add(P2);
            phases.Add(P3);
            phases.Add(P4);
            phases.Add(P5);

            bool haveChanged = true;

            while (haveChanged)
            {
                haveChanged = false;

                //phase 0
                List<int> borderPixels = new List<int>();
                for (int i = width + stride; i < length - width - stride; i+=stride)
                {
                    if (ptr[i] == fGround)
                    {
                        int sum = 0;
                        for (int k = 0; k < 9; k++)
                            sum += (ptr[i + offsetMat[k]] == fGround) ? edgeWeights[k] : 0;

                        if (Array.Exists(P0, x => (x == sum)))
                            borderPixels.Add(i);
                    }
                }

                //phase 1 2 3 4 5
                foreach (var phase in phases)
                {
                    foreach (var bPixel in borderPixels)
                    {
                        int sum = 0;
                        for (int k = 0; k < 9; k++)
                            sum += (ptr[bPixel + offsetMat[k]] == fGround) ? edgeWeights[k] : 0;

                        if (Array.Exists(phase, x => (x == sum)))
                        {
                            ptr[bPixel] = ptr[bPixel+1] = ptr[bPixel+2] = bGround;

                            if (!haveChanged)
                                haveChanged = true;
                        }
                    }
                }
            }

            // 1-pixel width phase          
            for (int i = width + stride; i < length - width - stride; i += stride)
            {
                if (ptr[i] == fGround)
                {
                    int sum = 0;
                    for (int k = 0; k < 9; k++)
                        sum += (ptr[i + offsetMat[k]] == fGround) ? edgeWeights[k] : 0;

                    if (Array.Exists(P0, x => (x == sum)))                
                        ptr[i] = ptr[i + 1] = ptr[i + 2] = bGround;              
                }
            }

            bmp.UnlockBits(readwrite);
            return bmp;
        }

        static private readonly byte fGround = byte.MinValue;
        static private readonly byte bGround = byte.MaxValue;

        static private readonly int[] edgeWeights =
        {
            128,  1, 2, 
             64,  0, 4,
             32, 16, 8
        };

        static private readonly int[] P0 = 
        {    
             3, 6, 7, 12, 14, 15, 24, 28, 
             30, 31, 48, 56, 60, 62, 63, 96, 
             112, 120, 124, 126, 127, 129, 131, 
             135, 143, 159, 191, 192, 193, 195, 
             199, 207, 223, 224, 225, 227, 231,
             239, 240, 241, 243, 247, 248, 249, 
             251, 252, 253, 254
        };

        static private readonly int[] P1 = { 7, 14, 28, 56, 112, 131, 193, 224 };
        static private readonly int[] P2 = { 7, 14, 15, 28, 30, 56, 60, 112, 120, 131, 135, 193, 195, 224, 225, 240 };
        static private readonly int[] P3 = { 7, 14, 15, 28, 30, 31, 56, 60, 62, 112, 120, 124, 131, 135, 143, 193, 195, 199, 224, 225, 227, 240, 241, 248 };
        static private readonly int[] P4 = { 7, 14, 15, 28, 30, 31, 56, 60, 62, 63, 112, 120, 124, 126, 131, 135, 143, 159, 193, 195, 199, 207, 224, 225, 227, 231, 240, 241, 243, 248, 249, 252 };
        static private readonly int[] P5 = { 7, 14, 15, 28, 30, 31, 56, 60, 62, 63, 112, 120, 124, 126, 131, 135, 143, 159, 191, 193, 195, 199, 207, 224, 225, 227, 231, 239, 240, 241, 243, 248, 249, 251, 252, 254 };
    }
}
