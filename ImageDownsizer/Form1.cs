using System;
using System.Drawing;
using System.Threading;
using System.Windows.Forms;
using System.Diagnostics;


namespace ImageDownsizer_Homework
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                Filter = "Image Files|*.jpg;*.jpeg;*.png;*.bmp;*.gif|All Files|*.*"
            };

            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                string selectedImagePath = openFileDialog.FileName;
                Image originalImage = Image.FromFile(selectedImagePath);
                pictureBox1.Image = originalImage;
            }
        }

        private void pictureBox1_Click(object sender, EventArgs e)
        {

        }

        private void button2_Click(object sender, EventArgs e)
        {
            if (pictureBox1.Image != null)
            {
                if (!float.TryParse(textBox1.Text, out float percentage) || percentage <= float.MinValue || percentage > float.MaxValue)
                {
                    MessageBox.Show("Please enter a valid downscaling percentage.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                Stopwatch stopwatch = new Stopwatch();
                stopwatch.Start();

                Image originalImage = pictureBox1.Image;
                int newWidth = (int)(originalImage.Width * percentage / 100);
                int newHeight = (int)(originalImage.Height * percentage / 100);

                Image downscaledImage = ResizeImage(originalImage, newWidth, newHeight);
                pictureBox2.Image = downscaledImage;
                label1.Text = originalImage.Width.ToString() + "x" + originalImage.Height.ToString();
                label2.Text = newWidth.ToString() + "x" + newHeight.ToString();

                stopwatch.Stop();
                MessageBox.Show($"Non-threaded resizing took {stopwatch.ElapsedMilliseconds} ms.", "Time Measurement");
            }
        }

        private void button3_Click(object sender, EventArgs e)
        {
            if (pictureBox1.Image != null)
            {
                if (!float.TryParse(textBox1.Text, out float percentage) || percentage <= float.MinValue || percentage > float.MaxValue)
                {
                    MessageBox.Show("Please enter a valid downscaling percentage.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                Stopwatch stopwatch = new Stopwatch();
                stopwatch.Start();

                Thread resizingThread = new Thread(() =>
                {
                    Image originalImage = pictureBox1.Image;
                    int newWidth = (int)(originalImage.Width * percentage / 100);
                    int newHeight = (int)(originalImage.Height * percentage / 100);

                    Image downscaledImage = ResizeImage(originalImage, newWidth, newHeight);
                    SetImageOnMainThread(pictureBox2, downscaledImage);
                    stopwatch.Stop();
                    this.Invoke(new Action(() =>
                    {
                        MessageBox.Show($"Threaded resizing took {stopwatch.ElapsedMilliseconds} ms.", "Time Measurement");
                        label1.Text = originalImage.Width.ToString() + "x" + originalImage.Height.ToString();
                        label2.Text = newWidth.ToString() + "x" + newHeight.ToString();
                    }));
                });
                resizingThread.Start();
            }
        }

        private Image ResizeImage(Image originalImage, int newWidth, int newHeight)
        {
            byte[,,] originalImageData = ConvertToColor(originalImage);
            byte[,,] newImageData = BilinearInterpolation.ResizeImage(originalImageData, originalImage.Width, originalImage.Height, newWidth, newHeight);
            return CreateBitmapFrom3DArray(newImageData, newWidth, newHeight);
        }

        private byte[,,] ConvertToColor(Image image)
        {
            Bitmap bitmap = new Bitmap(image);
            int width = bitmap.Width;
            int height = bitmap.Height;
            byte[,,] colorImage = new byte[width, height, 3];

            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    Color pixel = bitmap.GetPixel(x, y);
                    colorImage[x, y, 0] = pixel.R;
                    colorImage[x, y, 1] = pixel.G;
                    colorImage[x, y, 2] = pixel.B;
                }
            }

            return colorImage;
        }

        private Image CreateBitmapFrom3DArray(byte[,,] array, int width, int height)
        {
            Bitmap bitmap = new Bitmap(width, height);

            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    Color color = Color.FromArgb(array[x, y, 0], array[x, y, 1], array[x, y, 2]);
                    bitmap.SetPixel(x, y, color);
                }
            }
            return bitmap;
        }

        private void SetImageOnMainThread(PictureBox pictureBox, Image image)
        {
            if (pictureBox.InvokeRequired)
            {
                pictureBox.Invoke(new Action(() => pictureBox.Image = image));
            }
            else
            {
                pictureBox.Image = image;
            }
        }
        private void Form1_Load(object sender, EventArgs e)
        {

        }

        private void label1_Click(object sender, EventArgs e)
        {

        }

        private void label1_Click_1(object sender, EventArgs e)
        {

        }

        private void openFileDialog1_FileOk(object sender, System.ComponentModel.CancelEventArgs e)
        {

        }

        private void pictureBox2_Click(object sender, EventArgs e)
        {

        }


    }
}