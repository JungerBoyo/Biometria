using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Text;
using System.Transactions;
using System.Windows.Automation.Peers;
using System.Windows.Input;
using System.Windows.Navigation;
using System.Xml.Schema;

namespace WpfProject
{
    static public unsafe class Effects
    {

        static public Bitmap GrayScale(Bitmap bmp, PixelFormat format)
        {
            int stride = (format == PixelFormat.Format32bppPArgb || format == PixelFormat.Format32bppArgb) ? 4 : 3;

            BitmapData readwrite = BitmapsHandler.LockBits(bmp, ImageLockMode.ReadWrite);

            byte* ptr01 = (byte*)readwrite.Scan0.ToPointer();

            int width = readwrite.Stride;
            int bitmapLength = bmp.Height * width;

            for (int i = 0; i < bitmapLength; i+=stride)                 
                ptr01[i] = ptr01[i + 1] = ptr01[i + 2] = (byte)((ptr01[i] + ptr01[i + 1] + ptr01[i + 2]) / 3);
            

            bmp.UnlockBits(readwrite);
            return bmp;
        }

        static public Bitmap OtsuBinarizationWCV(Bitmap bmp, PixelFormat format)
        {
            int stride = (format == PixelFormat.Format32bppPArgb || format == PixelFormat.Format32bppArgb) ? 4 : 3;

            bmp = GrayScale(bmp, format);

            BitmapData readwrite = BitmapsHandler.LockBits(bmp, ImageLockMode.ReadWrite, format);

            byte* ptr = (byte*)readwrite.Scan0.ToPointer();

            int width = readwrite.Stride;
            int bitmapLength = readwrite.Height * stride * readwrite.Width;

            // Step 1 Creating and filling pixel values histogram
            int[] pixelValuesHistogram = new int[256];

            for (int i = 0; i < bitmapLength; i += stride)
                pixelValuesHistogram[(int)ptr[i]]++;

            // Step 2 Computing within class variance for every possible threshold

            int minThreshold = 256;
            double minWithinClassVariance = double.MaxValue;
            int pixelAmount = pixelValuesHistogram.Sum();

            for(int threshold = 1; threshold < 256; threshold++)
            {
                //FOREGROUND
                double weightF = 0.0;
                double meanF = 0.0;
                double VarianceF = 0.0;

                for (int foregroundIt = 0; foregroundIt < threshold; foregroundIt++)
                {
                    weightF += pixelValuesHistogram[foregroundIt];
                    meanF += (foregroundIt * pixelValuesHistogram[foregroundIt]);
                }
                meanF /= weightF;
                weightF /= pixelAmount;

                for (int foregroundIt = 0; foregroundIt <= threshold; foregroundIt++)
                    VarianceF += (foregroundIt - meanF)*(foregroundIt - meanF)*pixelValuesHistogram[foregroundIt];
                VarianceF /= weightF * pixelAmount;

                //BACKGROUND
                double weightB = 0.0;
                double meanB = 0.0;
                double VarianceB = 0.0;

                for (int backgroundIt = threshold; backgroundIt <= 255; backgroundIt++)
                {
                    weightB += pixelValuesHistogram[backgroundIt];
                    meanB += (backgroundIt * pixelValuesHistogram[backgroundIt]);
                }             
                meanB /= weightB;
                weightB /= pixelAmount;

                for (int backgroundIt = threshold; backgroundIt <= 255; backgroundIt++)
                    VarianceB += (backgroundIt - meanB) * (backgroundIt - meanB) * pixelValuesHistogram[backgroundIt];
                VarianceB /= weightB * pixelAmount;

                double withinClassVariance = weightF * VarianceF + weightB * VarianceB;
                if (withinClassVariance < minWithinClassVariance)
                {
                    minWithinClassVariance = withinClassVariance;
                    minThreshold = threshold;
                }
            }

            // Step 3 Binarization with computed thresold

            for (int i = 0; i < bitmapLength; i ++)
                ptr[i] = ptr[i] > minThreshold ? byte.MinValue : byte.MaxValue;

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

            double weightF = 0.0, weightB = 0.0;
            double weightSumF = 0.0;
            int pixelValuesHistogramSum = pixelValuesHistogram.Sum();

            double MaxBetweenClassVariance = double.MinValue;
            int bestThreshold = 0;

            for(int i = 0; i < 256; i++)
            {
                weightF += pixelValuesHistogram[i];
                weightB = pixelValuesHistogramSum - weightF;

                weightSumF += i * pixelValuesHistogram[i];

                double meanF = (weightSumF / weightF);
                double meanB = (weightedHistogramSum - weightSumF) / weightB;

                double BetweenClassVariance = weightB * weightF * (meanB - meanF) * (meanB - meanF);

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
    
        static public Bitmap Blurr(Bitmap bmp, PixelFormat format)
        {
            int stride = (format == PixelFormat.Format32bppPArgb || format == PixelFormat.Format32bppArgb) ? 4 : 3;

            BitmapData readBmp = BitmapsHandler.LockBits(bmp, ImageLockMode.ReadOnly, format);
            Bitmap blankBmp = new Bitmap(bmp.Width, bmp.Height, format);
            BitmapData writeBmp = BitmapsHandler.LockBits(blankBmp, ImageLockMode.WriteOnly, format);

            byte* ptrR = (byte*)readBmp.Scan0.ToPointer();
            byte* ptrW = (byte*)writeBmp.Scan0.ToPointer();

            int width = readBmp.Stride;
            int length = readBmp.Height * width;

            int blurrSize = 4;
            int offsetMatSize = (2 * blurrSize + 1) * (2 * blurrSize + 1);
            int[] offsetMatrix = new int[offsetMatSize];
            BitmapsHandler.CreateOffsetMatrix(out offsetMatrix, blurrSize, width, stride);

            for(int i = (blurrSize*width + stride*blurrSize); 
                    i < length - (blurrSize * width + stride);
                    i ++)
            {
                int mean = 0;
                for (int k = 0; k < offsetMatSize; k++)
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
    
    }
}
