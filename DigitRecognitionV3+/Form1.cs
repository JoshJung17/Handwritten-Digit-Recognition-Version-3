using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using NeuralNetworks;

namespace DigitRecognitionV3_
{
    public partial class Form1 : Form
    {
        const float BRUSH_RAD = 10;
        Graphics g;

        float[,] drawBoard = new float[80, 120];
        int minX, maxX, minY, maxY;
        Rectangle boundingBox;
        NeuralNetwork2D network;

        public Form1()
        {
            InitializeComponent();
            for (int i = 0; i < 80; i++)
                for (int j = 0; j < 120; j++)
                    drawBoard[i, j] = -1;
            minX = 80; maxX = 0; minY = 120; maxY = 0;
            boundingBox = new Rectangle();
            network = new NeuralNetwork2D("network.dat");
        }

        private void Form1_Shown(object sender, EventArgs e)
        {
            g = this.CreateGraphics();
            g.FillRectangle(Brushes.Black, 50, 50, 3 * 80, 3 * 120);
            for (int c = 0; c <= 80; c++)
                g.DrawLine(Pens.Gray, 50 + 3 * c, 50, 50 + 3 * c, 50 + 3 * 120);
            for (int r = 0; r <= 120; r++)
                g.DrawLine(Pens.Gray, 50, 50 + 3 * r, 50 + 3 * 80, 50 + 3 * r);
        }

        bool mouseDown = false;
        private void Form1_MouseDown(object sender, MouseEventArgs e)
        {
            mouseDown = true;
            int ptime = Environment.TickCount;
            while(mouseDown)
            {
                Point p = PointToClient(Cursor.Position);
                p.X--; p.Y--;

                for (int i = 0; i < 80; i++)
                {
                    for (int j = 0; j < 120; j++)
                    {
                        if (Math.Abs(50 + 3 * i - p.X) > BRUSH_RAD || Math.Abs(50 + 3 * j - p.Y) > BRUSH_RAD) continue;

                        float dx = 50 + 3 * i - p.X, dy = 50 + 3 * j - p.Y;
                        if (dx * dx + dy * dy > BRUSH_RAD * BRUSH_RAD) continue;
                        drawBoard[i, j] = 1;
                        g.FillRectangle(Brushes.White, 51 + 3 * i, 51 + 3 * j, 2,2);
                        minX = Math.Min(minX, i);
                        minY = Math.Min(minY, j);
                        maxX = Math.Max(maxX, i+1);
                        maxY = Math.Max(maxY, j+1);
                    }
                }
                if (Environment.TickCount-ptime>30)
                {
                    ptime = Environment.TickCount;
                    g.DrawRectangle(Pens.Gray, boundingBox);
                    boundingBox.X = 50 + 3 * minX;
                    boundingBox.Y = 50 + 3 * minY;
                    boundingBox.Width = 3 * Math.Max(0, maxX - minX);
                    boundingBox.Height = 3 * Math.Max(0, maxY - minY);
                    g.DrawRectangle(Pens.Red, boundingBox);
                }
                Application.DoEvents();
            }
            ptime = Environment.TickCount;
            g.DrawRectangle(Pens.Gray, boundingBox);
            boundingBox.X = 50 + 3 * minX;
            boundingBox.Y = 50 + 3 * minY;
            boundingBox.Width = 3 * Math.Max(0, maxX - minX);
            boundingBox.Height = 3 * Math.Max(0, maxY - minY);
            g.DrawRectangle(Pens.Red, boundingBox);
        }

        private void Form1_MouseUp(object sender, MouseEventArgs e)
        {
            mouseDown = false;
        }

        private void btnClear_Click(object sender, EventArgs e)
        {
            for (int i = 0; i < 80; i++)
                for (int j = 0; j < 120; j++)
                    drawBoard[i, j] = -1;
            g.FillRectangle(Brushes.Black, 50, 50, 3 * 80, 3 * 120);
            for (int c = 0; c <= 80; c++)
                g.DrawLine(Pens.Gray, 50 + 3 * c, 50, 50 + 3 * c, 50 + 3 * 120);
            for (int r = 0; r <= 120; r++)
                g.DrawLine(Pens.Gray, 50, 50 + 3 * r, 50 + 3 * 80, 50 + 3 * r);
            minX = 80; maxX = 0; minY = 120; maxY = 0;
        }

        private void btnRun_Click(object sender, EventArgs e)
        {
            float[,] arr = Crop();
            arr = Scale(arr, 20.2f / arr.GetLength(0));
            float[,] input = GetInput(arr);
            for (int r = 0; r < 28; r++)
            {
                for (int c = 0; c < 28; c++)
                {
                    g.FillRectangle(new SolidBrush(Color.FromArgb((int)(127 + 127 * input[r, c]), (int)(127 + 127 * input[r, c]), (int)(127 + 127 * input[r, c]))),
                        300 + c * 5, 100 + r * 5, 5, 5);
                }
            }

            float[] output = network.RunNetwork(new float[][,] { input }, true);
            float mini=1, maxi=-1;
            for (int i = 0; i < 10; i++)
            {
                maxi = Math.Max(maxi, output[i]);
                mini = Math.Min(mini, output[i]);
            }
            g.FillRectangle(Brushes.White, 600, 300, 200, 200);
            mini = Math.Min(mini, -0.7f);
            maxi = 1;
            for (int i = 0; i < 10; i++)
            {
                int height = (int)(200f * (output[i] - mini) / (maxi - mini));
                g.FillRectangle(Brushes.Blue, 600+20*i, 500 - height, 20, height);
                g.DrawString(i.ToString(), new Font("Courier", 10), Brushes.Black, 604 + 20 * i, 500);
            }
        }

        float[,] Crop()
        {
            int dim = Math.Max(maxX - minX, maxY - minY);
            float[,] ret = new float[dim,dim];
            for (int i = 0; i < dim; i++)
                for (int j = 0; j < dim; j++)
                    ret[i, j] = -1;
            for (int i = minX; i < maxX; i++)
            {
                for (int j = minY; j < maxY; j++)
                {
                    ret[i - minX, j - minY] = drawBoard[i, j];
                }
            }
            return ret;
        }

        // resizes the image
        float[,] Scale(float[,] img, float factor)
        {
            float[,] resized = new float[(int)(factor * img.GetLength(0) + 2), (int)(factor * img.GetLength(1) + 2)];
            float[,] weightSum = new float[resized.GetLength(0), resized.GetLength(1)];

            for (int i = 0; i < img.GetLength(0); ++i)
            {
                for (int j = 0; j < img.GetLength(1); ++j)
                {
                    int x1 = (int)(factor * i), y1 = (int)(factor * j);
                    int x2 = (int)(factor * i + 0.999F), y2 = (int)(factor * j + 0.999F);
                    double dx, dy, d;

                    dx = 1.0 - Math.Abs(factor * i - x1);
                    dy = 1.0 - Math.Abs(factor * j - y1);
                    d = (float)Math.Sqrt(dx * dx + dy * dy);
                    resized[x1, y1] += (float)(img[i, j] * d);
                    weightSum[x1, y1] += (float)d;

                    dx = 1.0 - Math.Abs(factor * i - x2);
                    dy = 1.0 - Math.Abs(factor * j - y1);
                    d = (float)Math.Sqrt(dx * dx + dy * dy);
                    resized[x2, y1] += (float)(img[i, j] * d);
                    weightSum[x2, y1] += (float)d;

                    dx = 1.0 - Math.Abs(factor * i - x1);
                    dy = 1.0 - Math.Abs(factor * j - y2);
                    d = (float)Math.Sqrt(dx * dx + dy * dy);
                    resized[x1, y2] += (float)(img[i, j] * d);
                    weightSum[x1, y2] += (float)d;

                    dx = 1.0 - Math.Abs(factor * i - x2);
                    dy = 1.0 - Math.Abs(factor * j - y2);
                    d = (float)Math.Sqrt(dx * dx + dy * dy);
                    resized[x2, y2] += (float)(img[i, j] * d);
                    weightSum[x2, y2] += (float)d;
                }
            }
            float[,] ret = new float[20, 20];
            for (int i = 0; i < 20; ++i)
            {
                for (int j = 0; j < 20; ++j)
                {
                    if (weightSum[i, j] > 0 && Math.Min(resized.GetLength(0) - i, resized.GetLength(1) - j) > 2)
                        ret[i, j] = resized[i, j] / weightSum[i, j];
                    else
                        ret[i, j] = 0;
                }
            }
            return ret;
        }

        float[,] GetInput(float[,] arr)
        {
            float[,] ret = new float[28, 28];
            int x = 0, y = 0;
            int cnt = 0;
            for (int i = 0; i < 20; i++)
            {
                for (int j = 0; j < 20; j++)
                {
                    if (arr[i, j] < -0.8) continue;
                    cnt++;
                    x += i; y += j;
                }
            }
            x /= cnt; y /= cnt;
            x = 14 - x; y = 14 - y;
            for (int i = 0; i < 28; i++)
                for (int j = 0; j < 28; j++)
                    ret[i, j] = -1;
            for (int r = 0; r < 20; r++)
            {
                for (int c = 0; c < 20; c++)
                {
                    if (0<=y+r&&y+r<28&&0<=x+c&&x+c<28)
                        ret[y+r,x+c] = arr[c,r];
                }
            }
            return ret;
        }
    }
}
