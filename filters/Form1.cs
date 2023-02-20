using System;
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

        private void медианныйToolStripMenuItem_Click(object sender, EventArgs e)
        {
/*            MedianFilter filter = new MedianFilter();
            *//*            Bitmap resultImg = filter.process(image);
                        pictureBox1.Image = resultImg;
                        pictureBox1.Refresh();*//*
            image = filter.process(image);
            pictureBox1.Image = image;
            pictureBox1.Refresh();*/
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

   /* public class MedianFilter : filters
    {
        protected override Color MakeNewColor(Bitmap Img, int x, int y)
        {
            if (x == 0 || x + 1 == Img.Width || y == 0 || y + 1 == Img.Height)
                return Img.GetPixel(x, y);
            Color [] arrColor = new Color[9];
            for (int i = -1; i < 2; i++)
                for (int j = -1; j < 2; j++)
                {
                        arrColor[(i + 1) * 3 + j + 1] = Img.GetPixel(x + i, y + j);
                }
            int len = arrColor.Length;
            Color t;
            for (int i = 1; i < len; i++)
            {
                for (int j = 0; j < len - i; j++)
                {
                    if (arrColor[j].R + arrColor[j].G + arrColor[j].B > arrColor[j + 1].R + arrColor[j + 1].G + arrColor[j + 1].B)
                    {
                        t = arrColor[j];
                        arrColor[j] = arrColor[j + 1];
                        arrColor[j + 1] = t;
                    }
                }
            }

            return arrColor[4];
        }
    }

    public class AverFilter : filters
    {
        protected override Color MakeNewColor(Bitmap Img, int x, int y)
        {
            if (x == 0 || x + 1 == Img.Width || y == 0 || y + 1 == Img.Height)
                return Img.GetPixel(x, y);
            Color[] arrColor = new Color[9];
            for (int i = -1; i < 2; i++)
                for (int j = -1; j < 2; j++)
                {
                    arrColor[(i + 1) * 3 + j + 1] = Img.GetPixel(x + i, y + j);
                }
            int len = arrColor.Length;
            int r = 0, b = 0, g = 0;
            for (int i = 1; i < len; i++)
            {
                r = r + arrColor[i].R;
                g = g + arrColor[i].G;
                b = b + arrColor[i].B;
            }
            Color resultColor = Color.FromArgb(r / 9, g / 9, b / 9);
            return resultColor;
        }
    }*/
}
