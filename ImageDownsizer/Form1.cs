using System;
using System.Drawing;
using System.Threading;
using System.Windows.Forms;
using System.Diagnostics;
using System.Drawing.Imaging;

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

                    Image downscaledImage = ResizeImageParallel(originalImage, newWidth, newHeight);
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
        private Image ResizeImageParallel(Image originalImage, int newWidth, int newHeight)
        {
            byte[,,] originalImageData = ConvertToColor(originalImage);
            byte[,,] newImageData = new byte[newWidth, newHeight, 3];

           
            int originalWidth = originalImage.Width;
            int originalHeight = originalImage.Height;

            Parallel.For(0, newHeight, y =>
            {
                Debug.WriteLine($"Processing line {y} on thread {Thread.CurrentThread.ManagedThreadId}");

                for (int x = 0; x < newWidth; x++)
                {
                    
                    float originalX = x * originalWidth / (float)newWidth;
                    float originalY = y * originalHeight / (float)newHeight;

                    int x1 = (int)Math.Floor(originalX);
                    int x2 = Math.Min(x1 + 1, originalWidth - 1);
                    int y1 = (int)Math.Floor(originalY);
                    int y2 = Math.Min(y1 + 1, originalHeight - 1);

                    float xFrac = originalX - x1;
                    float yFrac = originalY - y1;

                    for (int channel = 0; channel < 3; channel++)
                    {
                        float interpolatedValue = (1 - xFrac) * (1 - yFrac) * originalImageData[x1, y1, channel] +
                                                  xFrac * (1 - yFrac) * originalImageData[x2, y1, channel] +
                                                  (1 - xFrac) * yFrac * originalImageData[x1, y2, channel] +
                                                  xFrac * yFrac * originalImageData[x2, y2, channel];

                        newImageData[x, y, channel] = (byte)interpolatedValue;
                    }
                }
            });

            return CreateBitmapFrom3DArray(newImageData, newWidth, newHeight);
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

        private Bitmap CreateBitmapFrom3DArray(byte[,,] array, int width, int height)
        {
            
            Bitmap bitmap = new Bitmap(width, height, PixelFormat.Format24bppRgb);

        
            BitmapData bitmapData = bitmap.LockBits(new Rectangle(0, 0, width, height), ImageLockMode.WriteOnly, bitmap.PixelFormat);

            
            int bytes = bitmapData.Stride * bitmapData.Height;
            byte[] rgbValues = new byte[bytes];

            
            int counter = 0;
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    rgbValues[counter++] = array[x, y, 2]; // Blue
                    rgbValues[counter++] = array[x, y, 1]; // Green
                    rgbValues[counter++] = array[x, y, 0]; // Red
                }
                
                counter += bitmapData.Stride - (width * 3);
            }

            
            System.Runtime.InteropServices.Marshal.Copy(rgbValues, 0, bitmapData.Scan0, bytes);

            
            bitmap.UnlockBits(bitmapData);

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