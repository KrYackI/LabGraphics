﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace filters
{
    public partial class Form1 : Form
    {
        Bitmap image;

        public Form1()
        {
            InitializeComponent();
        }

        private void открытьToolStripMenuItem_Click(object sender, EventArgs e)
        {
            OpenFileDialog dialog = new OpenFileDialog();
            dialog.Filter = "Image files | *.png; *.jpg; *.bmp | All files (*.*) | *.*";

            if (dialog.ShowDialog() == DialogResult.OK)
            {
                image = new Bitmap(dialog.FileName);

                pictureBox1.Image = image;
                pictureBox1.Refresh();
            }

        }

        private void инверсияToolStripMenuItem_Click(object sender, EventArgs e)
        {
            filters filter = new InversionFilter();
            backgroundWorker1.RunWorkerAsync(filter);
        }

        private void сепияToolStripMenuItem_Click(object sender, EventArgs e)
        {
            filters filter = new SepyFilter();
            backgroundWorker1.RunWorkerAsync(filter);
        }

        private void медианныйToolStripMenuItem_Click(object sender, EventArgs e)
        {
            filters filter = new MedianFilter();
            backgroundWorker1.RunWorkerAsync(filter);
        }

        private void размытиеToolStripMenuItem_Click(object sender, EventArgs e)
        {
            filters filter = new BlurFilter();
            backgroundWorker1.RunWorkerAsync(filter);
        }

        private void нормальныйToolStripMenuItem_Click(object sender, EventArgs e)
        {
            filters filter = new GaussFilter();
            backgroundWorker1.RunWorkerAsync(filter);
        }

        private void резкостьToolStripMenuItem_Click(object sender, EventArgs e)
        {
            filters filter = new SharpnessFilter();
            backgroundWorker1.RunWorkerAsync(filter);
        }

        private void волныToolStripMenuItem_Click(object sender, EventArgs e)
        {
            filters filter = new WaveFilter();
            backgroundWorker1.RunWorkerAsync(filter);
        }

        private void backgroundWorker1_DoWork(object sender, DoWorkEventArgs e)
        {
            Bitmap newImg = ((filters)e.Argument).process(image, backgroundWorker1);
            if (backgroundWorker1.CancellationPending != true)
                image = newImg;
        }

        private void backgroundWorker1_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            progressBar1.Value = e.ProgressPercentage;
        }

        private void backgroundWorker1_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (!e.Cancelled)
            {
                pictureBox1.Image = image;
                pictureBox1.Refresh();
            }
            progressBar1.Value = 0;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            backgroundWorker1.CancelAsync();
        }

    }

    public abstract class filters
    {

        protected abstract Color MakeNewColor(Bitmap Img, int x, int y);

        public int clamp(int val, int min, int max)
        {
            if (val < min)
                return min;
            if (val > max)
                return max;
            return val;
        }

        public Bitmap process(Bitmap sourceImg, BackgroundWorker worker)
        {
            Bitmap resultImg = new Bitmap(sourceImg.Width, sourceImg.Height);

            for (int i = 0; i < sourceImg.Width; i++)
            {
                worker.ReportProgress((int)((float)i / resultImg.Width * 100));
                if (worker.CancellationPending)
                    return null;
                for (int j = 0; j < sourceImg.Height; j++)
                {
                    resultImg.SetPixel(i, j, MakeNewColor(sourceImg, i, j));
                }
            }
            return resultImg;
        }
    }

    public class InversionFilter : filters
    {
        protected override Color MakeNewColor(Bitmap Img, int x, int y)
        {
            Color sourceColor = Img.GetPixel(x, y);
            Color resultColor = Color.FromArgb(255 - sourceColor.R, 255 - sourceColor.G, 255 - sourceColor.B);
            return resultColor;
        }
    }

    public class SepyFilter : filters
    {
        protected override Color MakeNewColor(Bitmap Img, int x, int y)
        {
            Color sourceColor = Img.GetPixel(x, y);
            int k = 16;
            char Intensity = (char)(0.36 * sourceColor.R + 0.53 * sourceColor.G + 0.11 * sourceColor.B);
            Color resultColor = Color.FromArgb(clamp(Intensity + 2 * k, 0, 255), clamp(Intensity + (int)(0.5 * k), 0, 255), clamp(Intensity - 1 * k, 0, 255));
            return resultColor;
        }
    }

    public class MedianFilter : filters
    {
        protected override Color MakeNewColor(Bitmap Img, int x, int y)
        {
            int[] arrR = new int[9];
            int[] arrG = new int[9];
            int[] arrB = new int[9];
            for (int i = -1; i < 2; i++)
                for (int j = -1; j < 2; j++)
                {
                    arrR[(i + 1) * 3 + j + 1] = Img.GetPixel(clamp(x + i, 0, Img.Width - 1), clamp(y + j, 0, Img.Height - 1)).R;
                    arrG[(i + 1) * 3 + j + 1] = Img.GetPixel(clamp(x + i, 0, Img.Width - 1), clamp(y + j, 0, Img.Height - 1)).G;
                    arrB[(i + 1) * 3 + j + 1] = Img.GetPixel(clamp(x + i, 0, Img.Width - 1), clamp(y + j, 0, Img.Height - 1)).B;
                }
            int len = arrR.Length;
            int t;
            for (int i = 1; i < len; i++)
            {
                for (int j = 0; j < len - i; j++)
                {
                    if (arrR[j] > arrR[j + 1])
                    {
                        t = arrR[j];
                        arrR[j] = arrR[j + 1];
                        arrR[j + 1] = t;
                    }
                    if (arrG[j] > arrG[j + 1])
                    {
                        t = arrR[j];
                        arrG[j] = arrG[j + 1];
                        arrG[j + 1] = t;
                    }
                    if (arrB[j] > arrB[j + 1])
                    {
                        t = arrB[j];
                        arrB[j] = arrB[j + 1];
                        arrB[j + 1] = t;
                    }
                }
            }

            return Color.FromArgb(arrR[len / 2], arrG[len / 2], arrB[len / 2]);
        }
    }

    public class MatrixFilter : filters
    {
        protected float[,] kernel = null;
        protected MatrixFilter() { }
        public MatrixFilter(float [,] kernel)
        {
            this.kernel = kernel;
        }

        protected override Color MakeNewColor(Bitmap Img, int x, int y)
        {
            int radiusX = kernel.GetLength(0) / 2;
            int radiusY = kernel.GetLength(1) / 2;

            float R = 0;
            float G = 0;
            float B = 0;
            for (int i = -radiusY; i <= radiusY; i++)
                for (int j = -radiusX; j <= radiusX; j++)
                {
                    int idX = clamp(x + i, 0, Img.Width - 1);
                    int idY = clamp(y + j, 0, Img.Height - 1);
                    Color neighborColor = Img.GetPixel(idX, idY);
                    R += neighborColor.R * kernel[i + radiusX, j + radiusY];
                    G += neighborColor.G * kernel[i + radiusX, j + radiusY];
                    B += neighborColor.B * kernel[i + radiusX, j + radiusY];
                }
            return Color.FromArgb(clamp((int)R, 0, 255), clamp((int)G, 0, 255), clamp((int)B, 0, 255));
        }
    }

    public class BlurFilter: MatrixFilter
    {
        public BlurFilter()
        {
            int sizeX = 3;
            int sizeY = 3;
            kernel = new float[sizeX, sizeY];
            for (int i = 0; i < sizeX; i++)
                for (int j = 0; j < sizeY; j++)
                    kernel[i, j] = 1.0f / (float)(sizeX * sizeY);
        }
    }

    public class GaussFilter : MatrixFilter
    {
        public void CreateGaussKernel(int rad, float sigma)
        {
            int size = 2 * rad + 1;
            kernel = new float[size, size];
            float norm = 0.0F;
            for (int i = -rad; i <= rad; i++)
                for (int j = -rad; j <=rad; j++)
                {
                    kernel[i + rad, j + rad] = (float)(Math.Exp(-(i * i + j * j) / (sigma * sigma)));
                    norm += kernel[i + rad, j + rad];
                }
            for (int i = 0; i < size; i++)
                for (int j = 0; j < size; j++)
                    kernel[i, j] /= norm;
        }

        public GaussFilter()
        {
            CreateGaussKernel(3, 2);
        }
    }

    public class SharpnessFilter : MatrixFilter
    {
        public SharpnessFilter()
        {
            kernel = new float[3, 3];
            for (int i = 0; i < 3; i++)
                for (int j = 0; j < 3; j++)
                    kernel[i, j] = -1;
            kernel[1, 1] = 9;
        }
    }

    public class WaveFilter: filters
    {
        protected override Color MakeNewColor(Bitmap Img, int x, int y)
        {
            int k = x + (int)(20 * Math.Sin(2 * Math.PI * y / 60));
            int l = y;
            return Img.GetPixel(clamp(k, 0, Img.Width - 1), clamp(l, 0, Img.Height - 1));
        }
    }

}
