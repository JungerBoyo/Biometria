using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Security.Permissions;
using System.Text;
using System.Transactions;
using System.Windows.Automation.Peers;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Navigation;
using System.Xml.Schema;

namespace WpfProject
{
    public enum Kernel { PERWITT = 1, SOBEL = 2 };
    static public unsafe class Effects
    {
        public delegate float FilterType(float mean, float std);
        static public Bitmap GrayScale(Bitmap bmp, PixelFormat format)
        {
            int stride = (format == PixelFormat.Format32bppPArgb || format == PixelFormat.Format32bppArgb) ? 4 : 3;

            BitmapData readwrite = BitmapsHandler.LockBits(bmp, ImageLockMode.ReadWrite, format);

            byte* ptr01 = (byte*)readwrite.Scan0.ToPointer();

            int width = readwrite.Stride;
            int bitmapLength = bmp.Height * width;

            for (int i = 0; i < bitmapLength; i+=stride)                 
                ptr01[i] = ptr01[i + 1] = ptr01[i + 2] = (byte)((ptr01[i] + ptr01[i + 1] + ptr01[i + 2]) / 3);
           
            bmp.UnlockBits(readwrite);
            return bmp;
        }

        static public Bitmap OtsuBinarizationBCV(Bitmap bmp, PixelFormat format)
        {
            int stride = (format == PixelFormat.Format32bppPArgb || format == PixelFormat.Format32bppArgb) ? 4 : 3;

            bmp = GrayScale(bmp, format);

            BitmapData readwrite = BitmapsHandler.LockBits(bmp, ImageLockMode.ReadWrite, format);

            byte* ptr = (byte*)readwrite.Scan0.ToPointer();

            int width = readwrite.Stride;
            int bitmapLength = readwrite.Height * stride* readwrite.Width;

            // Step 1 Creating and filling pixel values histogram
            int[] pixelValuesHistogram = new int[256];

            for (int i = 0; i < bitmapLength; i += stride)
                pixelValuesHistogram[(int)ptr[i]]++;

            int weightedHistogramSum = 0;
            for (int i = 0; i < 256; i++)
                weightedHistogramSum += pixelValuesHistogram[i] * i;

            float weightF = 0.0f, weightB = 0.0f;
            float weightSumF = 0.0f;
            int pixelValuesHistogramSum = pixelValuesHistogram.Sum();

            float MaxBetweenClassVariance = float.MinValue;
            int bestThreshold = 0;

            for(int i = 0; i < 256; i++)
            {
                weightF += pixelValuesHistogram[i];
                weightB = pixelValuesHistogramSum - weightF;

                weightSumF += i * pixelValuesHistogram[i];

                float meanF = (weightSumF / weightF);
                float meanB = (weightedHistogramSum - weightSumF) / weightB;

                float BetweenClassVariance = weightB * weightF * (meanB - meanF) * (meanB - meanF);

                if(MaxBetweenClassVariance < BetweenClassVariance)
                {
                    MaxBetweenClassVariance = BetweenClassVariance;
                    bestThreshold = i;
                }
            }
   
            for (int i = 0; i < bitmapLength; i++)
                ptr[i] = ( ptr[i] > (byte)bestThreshold ) ? byte.MinValue : byte.MaxValue;

            bmp.UnlockBits(readwrite);
            return bmp;
        }
    
        static public Bitmap Blurr(Bitmap bmp, PixelFormat format, int force)
        {
            int stride = (format == PixelFormat.Format32bppPArgb || format == PixelFormat.Format32bppArgb) ? 4 : 3;

            BitmapData readBmp = BitmapsHandler.LockBits(bmp, ImageLockMode.ReadOnly, format);
            Bitmap blankBmp = new Bitmap(bmp.Width, bmp.Height, format);
            BitmapData writeBmp = BitmapsHandler.LockBits(blankBmp, ImageLockMode.WriteOnly, format);

            byte* ptrR = (byte*)readBmp.Scan0.ToPointer();
            byte* ptrW = (byte*)writeBmp.Scan0.ToPointer();

            int width = readBmp.Stride;
            int length = readBmp.Height * width;

            int blurrSize = force;
            int offsetMatSize = (2 * blurrSize + 1) * (2 * blurrSize + 1);
            int[] offsetMatrix = new int[offsetMatSize];
            BitmapsHandler.CreateOffsetMatrix(out offsetMatrix, blurrSize, width, stride);

            for(int i = (blurrSize*width + stride*blurrSize); 
                    i < length - (blurrSize * width + stride);
                    i ++)
            {
                int mean = 0;
                for (int k = 0; k < offsetMatSize; k++)
                    if(i + offsetMatrix[k] < length)
                      mean += ptrR[i + offsetMatrix[k]];

                mean /= offsetMatSize;
                
                if (mean > 255) mean = 255;
                else if (mean < 0) mean = 0;
                    
                ptrW[i] = (byte)mean;
            }

            bmp.UnlockBits(readBmp);
            blankBmp.UnlockBits(writeBmp);

            return blankBmp; 
        }

        static public Bitmap Pixelize(Bitmap bmp, PixelFormat format)
        {
            int stride = (format == PixelFormat.Format32bppPArgb || format == PixelFormat.Format32bppArgb) ? 4 : 3;

            BitmapData readBmp = BitmapsHandler.LockBits(bmp, ImageLockMode.ReadOnly, format);
            Bitmap blankBmp = new Bitmap(bmp.Width, bmp.Height, format);
            BitmapData writeBmp = BitmapsHandler.LockBits(blankBmp, ImageLockMode.WriteOnly, format);

            byte* ptrR = (byte*)readBmp.Scan0.ToPointer();
            byte* ptrW = (byte*)writeBmp.Scan0.ToPointer();

            int width = readBmp.Stride;
            int length = readBmp.Height * width;

            int blurrSize = 36;

            int offsetMatSize = (2 * blurrSize + 1) * (2 * blurrSize + 1);
            int[] offsetMatrix = new int[offsetMatSize];
            BitmapsHandler.CreateOffsetMatrix(out offsetMatrix, blurrSize, width, stride);

            for (int i = (blurrSize * width + stride * blurrSize);
                    i < length - (blurrSize * width + stride);
                    i+=stride*(2*blurrSize))
            {
                for (int rgba = 0; rgba < stride; rgba++)
                {
                    int mean = 0;
                    for (int k = 0; k < offsetMatSize; k++)
                        mean += ptrR[i + offsetMatrix[k] + rgba];

                    mean /= offsetMatSize;

                    if (mean > 255) mean = 255;
                    else if (mean < 0) mean = 0;

                    for (int fillIt = 0; fillIt < offsetMatSize; fillIt++)
                        ptrW[offsetMatrix[fillIt] + i + rgba] = (byte)mean;
                }
                

                if((i + stride * (2 * blurrSize))/width > i/width)
                {
                    i += ((i / width ) * width - i) + 2 * blurrSize * width + blurrSize * stride;
                    i -= stride * (2 * blurrSize);
                }
            }

            bmp.UnlockBits(readBmp);
            blankBmp.UnlockBits(writeBmp);

            return blankBmp;
        }
    
        static public Bitmap MedianFilter(Bitmap bmp, PixelFormat format)
        {
            int stride = (format == PixelFormat.Format32bppPArgb || format == PixelFormat.Format32bppArgb) ? 4 : 3;

            BitmapData read = BitmapsHandler.LockBits(bmp, ImageLockMode.ReadOnly, format);
            Bitmap result = new Bitmap(bmp.Width, bmp.Height, format);
            BitmapData write = BitmapsHandler.LockBits(result, ImageLockMode.WriteOnly, format);

            byte* ptrR = (byte*)read.Scan0.ToPointer();
            byte* ptrW = (byte*)write.Scan0.ToPointer();

            int width = read.Stride;
            int length = width * read.Height;

            int[] offsetMat;
            BitmapsHandler.CreateOffsetMatrix(out offsetMat, 1, width, stride);
            
            for (int i=width+stride; i<length-width-stride; i++)
            {
                int[] values = new int[9];
                for (int k = 0; k < 9; k++)
                    values[k] = (ptrR[i + offsetMat[k]]);

                Array.Sort(values);

                ptrW[i] = (byte)values[4];
            }

            bmp.UnlockBits(read);
            result.UnlockBits(write);

            return result;
        }
    
        static public Bitmap EdgeDetection(Bitmap bmp, PixelFormat format, Kernel kernel)
        {
            int stride = (format == PixelFormat.Format32bppPArgb || format == PixelFormat.Format32bppArgb) ? 4 : 3;

            bmp = Blurr(bmp, format, 1);

            BitmapData read = BitmapsHandler.LockBits(bmp, ImageLockMode.ReadOnly, format);
            Bitmap result = new Bitmap(bmp.Width, bmp.Height, format);
            BitmapData write = BitmapsHandler.LockBits(result, ImageLockMode.WriteOnly, format);

            byte* ptrR = (byte*)read.Scan0.ToPointer();
            byte* ptrW = (byte*)write.Scan0.ToPointer();

            int width = read.Stride;
            int length = width * read.Height;

            int[] offsetMat;
            BitmapsHandler.CreateOffsetMatrix(out offsetMat, 1, width, stride);

            int[] kernelX =  
            {
                 -1,      0,      1,
                 -(int)kernel, 0, (int)kernel,
                 -1,      0,      1
            };

            int[] kernelY =
            {
                -1, -2, -1,
                 0,  0,  0,
                 1,  2,  1
            };
            
            for(int i = width+stride; i < length-width-stride; i ++)
            {
                int dX = 0, dY = 0;
                int grayScaledPixel;
                for(int k = 0; k < 9; k++)
                {
                    grayScaledPixel = (ptrR[i + offsetMat[k]] + ptrR[i + offsetMat[k] + 1] + ptrR[i + offsetMat[k] + 2]);
                    grayScaledPixel /= 3;

                    dX += grayScaledPixel * kernelX[k];
                    dY += grayScaledPixel * kernelY[k];
                }

                int incline = ((dX * dX) + (dY * dY));
                incline >>= 7;

                byte value = (incline > 255) ? byte.MaxValue : (byte)incline;
  
                for (int k = 0; k < 3; k++)
                    ptrW[i + k] = value;
            }

            bmp.UnlockBits(read);
            result.UnlockBits(write);

            return result;
        }

        public static Bitmap Phanscalar(Bitmap bmp, PixelFormat format, float pow = 2.0f, float q = 10.0f, float ratio = 0.5f, float div = 0.25f)
            => Niblack(bmp, format,  (mean, std) => mean * (1 + pow * (float)Math.Exp((-q * mean)) + ratio * (std / div - 1)));
        public static Bitmap Savoula(Bitmap bmp, PixelFormat format, float ratio = 0.5f, float div = 2.0f)
            => Niblack(bmp, format, (mean, std) => mean * (1 + ratio * (std / div - 1)));
        static public Bitmap Niblack(Bitmap bmp, PixelFormat format, FilterType equation = null)
        {
            equation ??= (mean, stdDev) => 0.2f * stdDev + mean;

            int stride = (format == PixelFormat.Format32bppPArgb || format == PixelFormat.Format32bppArgb) ? 4 : 3;

            BitmapData readBmp = BitmapsHandler.LockBits(bmp, ImageLockMode.ReadOnly, format);
            Bitmap blankBmp = new Bitmap(bmp.Width, bmp.Height, format);
            BitmapData writeBmp = BitmapsHandler.LockBits(blankBmp, ImageLockMode.WriteOnly, format);

            byte* ptrR = (byte*)readBmp.Scan0.ToPointer();
            byte* ptrW = (byte*)writeBmp.Scan0.ToPointer();

            int width = readBmp.Stride;
            int length = readBmp.Height * width;

            int[] offsetMat;
            BitmapsHandler.CreateOffsetMatrix(out offsetMat, 1, width, stride);

            for(int i=width+stride; i<length-width-stride; i++)
            {
                float mean = 0.0f;
                float stdDev = 0.0f;

                for (int k = 0; k < 9; k++)
                    mean += ptrR[i + offsetMat[k]];
                mean /= 9;

                for (int k = 0; k < 9; k++)
                    stdDev += (ptrR[i + offsetMat[k]] - mean) * (ptrR[i + offsetMat[k]] - mean);
                stdDev /= 9;

                ptrW[i] = (equation(mean, stdDev) > ptrR[i]) ? byte.MinValue : byte.MaxValue;
            }


            bmp.UnlockBits(readBmp);
            blankBmp.UnlockBits(writeBmp);

            return blankBmp;
        }
    }
}
