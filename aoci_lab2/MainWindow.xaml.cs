using System;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Microsoft.Win32;
using Emgu.CV.Structure;
using Emgu.CV;

namespace aoci_lab2
{
    public partial class MainWindow : Window
    {
        private Image<Bgr, byte> sourceImage;
        public MainWindow()
        {
            InitializeComponent();
        }

        public BitmapSource ToBitmapSource(Image<Bgr, byte> image)
        {
            var mat = image.Mat;

            return BitmapSource.Create(
                mat.Width,
                mat.Height,
                96d,
                96d,
                PixelFormats.Bgr24,
                null,
                mat.DataPointer,
                mat.Step * mat.Height,
                mat.Step);
        }
        public Image<Bgr, byte> ToEmguImage(BitmapSource source)
        {
            if (source == null) return null;

            FormatConvertedBitmap safeSource = new FormatConvertedBitmap();
            safeSource.BeginInit();
            safeSource.Source = source;
            safeSource.DestinationFormat = PixelFormats.Bgr24;
            safeSource.EndInit();

            Image<Bgr, byte> resultImage = new Image<Bgr, byte>(safeSource.PixelWidth, safeSource.PixelHeight);
            var mat = resultImage.Mat;

            safeSource.CopyPixels(
                new System.Windows.Int32Rect(0, 0, safeSource.PixelWidth, safeSource.PixelHeight), 
                mat.DataPointer, 
                mat.Step * mat.Height,
                mat.Step); 

            return resultImage;
        }

        private void LoadImage_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "Файлы изображений (*.jpg, *.jpeg, *.png)|*.jpg;*.jpeg;*.png";
            if (openFileDialog.ShowDialog() == true)
            {
                sourceImage = new Image<Bgr, byte>(openFileDialog.FileName);

                MainImage.Source = ToBitmapSource(sourceImage);
            }
        }

        private void SaveImage_Click(object sender, RoutedEventArgs e)
        {
            BitmapSource currentWpfImage = MainImage.Source as BitmapSource;
            if (currentWpfImage == null)
            {
                MessageBox.Show("Отсутсвует изображение");
                return;
            }

            SaveFileDialog saveFileDialog = new SaveFileDialog();
            saveFileDialog.Filter = "PNG Image (*.png)|*.png|JPEG Image (*.jpg)|*.jpg|Bitmap Image (*.bmp)|*.bmp";

            if (saveFileDialog.ShowDialog() == true)
            {
                try
                {
                    Image<Bgr, byte> imageToSave = ToEmguImage(currentWpfImage);
                    imageToSave.Save(saveFileDialog.FileName);

                    MessageBox.Show($"Изображение успешно сохранено в {saveFileDialog.FileName}");
                }
                catch (Exception ex)
                {

                    MessageBox.Show($"Ошибка! Не могу сохранить файл. Подробности: {ex.Message}");
                }
            }
        }

        private void UpdateImage_Click(object sender, RoutedEventArgs e)
        {
            BitmapSource currentWpfImage = MainImage.Source as BitmapSource;

            if (currentWpfImage == null)
            {
                MessageBox.Show("Изображение отсутсвует");
                return;
            }

            sourceImage = ToEmguImage(currentWpfImage);
            MessageBox.Show("Изменения применены. Теперь это новый оригинал.");
        }
        private void Clear_Click(object sender, RoutedEventArgs e)
        {
            if (sourceImage == null) return;
            MainImage.Source = ToBitmapSource(sourceImage);
        }

        private void Sepia_Click(object sender, RoutedEventArgs e)
        {
            if (sourceImage == null) return;

            //Снова создаем клон для безопасной работы.
            Image<Bgr, byte> sepiaImage = sourceImage.Clone();

            for (int y = 0; y < sepiaImage.Rows; y++)
            {
                for (int x = 0; x < sepiaImage.Cols; x++)
                {
                    Bgr pixel = sepiaImage[y, x];
                    double r = pixel.Red;
                    double g = pixel.Green;
                    double b = pixel.Blue;

                    double newR = 0.393 * r + 0.769 * g + 0.189 * b;
                    double newG = 0.349 * r + 0.686 * g + 0.168 * b;
                    double newB = 0.272 * r + 0.534 * g + 0.131 * b;

                    pixel.Red = (byte)Math.Min(255, newR);
                    pixel.Green = (byte)Math.Min(255, newG);
                    pixel.Blue = (byte)Math.Min(255, newB);

                    sepiaImage[y, x] = pixel;
                }
            }

            MainImage.Source = ToBitmapSource(sepiaImage);
        }

        private void OnRGBFilterChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (sourceImage == null) return;

            Image<Bgr, byte> filteredRGBImage = sourceImage.Clone();

            double contrast = ContrastRGBSlider.Value;
            double saturation = SaturationRGBSlider.Value;

            if (Math.Abs(contrast - 1.0) > 0.01)
            {
                for (int y = 0; y < filteredRGBImage.Rows; y++)
                {
                    for (int x = 0; x < filteredRGBImage.Cols; x++)
                    {
                        Bgr pixel = filteredRGBImage[y, x];
                        pixel.Red = (byte)Math.Max(0, Math.Min(255, contrast * (pixel.Red - 128) + 128));
                        pixel.Green = (byte)Math.Max(0, Math.Min(255, contrast * (pixel.Green - 128) + 128));
                        pixel.Blue = (byte)Math.Max(0, Math.Min(255, contrast * (pixel.Blue - 128) + 128));
                        filteredRGBImage[y, x] = pixel;
                    }
                }
            }

            if (Math.Abs(saturation - 1.0) < 0.01)
            {
                MainImage.Source = ToBitmapSource(filteredRGBImage);
            }

            for (int y = 0; y < filteredRGBImage.Rows; y++)
            {
                for (int x = 0; x < filteredRGBImage.Cols; x++)
                {
                    Bgr pixel = filteredRGBImage[y, x];
                    double r = pixel.Red;
                    double g = pixel.Green;
                    double b = pixel.Blue;

                    double gray = r * 0.299 + g * 0.587 + b * 0.114;

                    double newR = gray + (r - gray) * saturation;
                    double newG = gray + (g - gray) * saturation;
                    double newB = gray + (b - gray) * saturation;

                    pixel.Red = (byte)Math.Max(0, Math.Min(255, newR));
                    pixel.Green = (byte)Math.Max(0, Math.Min(255, newG));
                    pixel.Blue = (byte)Math.Max(0, Math.Min(255, newB));

                    filteredRGBImage[y, x] = pixel;
                }
            }

            MainImage.Source = ToBitmapSource(filteredRGBImage);
        }

        private void OnHSVFilterChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (sourceImage == null) return;

            Image<Hsv, byte> hsvImage = sourceImage.Convert<Hsv, byte>();

            double hsvContrast = ContrastHSVSlider.Value;
            double saturation = SaturationHSVSlider.Value;
            double hueShift = HueHSVSlider.Value;

            for (int y = 0; y < hsvImage.Rows; y++)
            {
                for (int x = 0; x < hsvImage.Cols; x++)
                {
                    Hsv pixel = hsvImage[y, x];

                    double v = pixel.Value; 
                    double newV = hsvContrast * (v - 128) + 128;
                    pixel.Value = (byte)Math.Max(0, Math.Min(255, newV));

                    double newHue = (pixel.Hue + hueShift / 2.0);
                    if (newHue < 0) newHue += 180;
                    pixel.Hue = (byte)(newHue % 180);

                    double newSat = pixel.Satuation * saturation;
                    pixel.Satuation = (byte)Math.Min(255, newSat);

                    hsvImage[y, x] = pixel;
                }
            }

            MainImage.Source = ToBitmapSource(hsvImage.Convert<Bgr, byte>());
        }

        private void ColorShiftChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (sourceImage == null) return;

            Image<Bgr, byte> colorshiftImage = sourceImage.Clone();

            int red = (int)RedSlider.Value;
            int green = (int)GreenSlider.Value;
            int blue = (int)BlueSlider.Value;

            for (int y = 0; y < colorshiftImage.Rows; y++)
            {
                for (int x = 0; x < colorshiftImage.Cols; x++)
                {
                    Bgr pixel = colorshiftImage[y, x];
                    double newR = pixel.Red + red;
                    double newG = pixel.Green + green;
                    double newB = pixel.Blue + blue;

                    pixel.Red = (byte)Math.Max(0, Math.Min(255, newR));
                    pixel.Green = (byte)Math.Max(0, Math.Min(255, newG));
                    pixel.Blue = (byte)Math.Max(0, Math.Min(255, newB));

                    colorshiftImage[y, x] = pixel;
                }
            }

            MainImage.Source = ToBitmapSource(colorshiftImage);
        }

        private void GammaChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (sourceImage == null) return;

            Image<Bgr, byte> gammaImage = sourceImage.Clone();

            double gamma = GammaSlider.Value;

            for (int y = 0; y < gammaImage.Rows; y++)
            {
                for (int x = 0; x < gammaImage.Cols; x++)
                {
                    Bgr pixel = gammaImage[y, x];
                    double newR = 255 * Math.Pow(pixel.Red / 255.0, 1.0 / gamma);
                    double newG = 255 * Math.Pow(pixel.Green / 255.0, 1.0 / gamma);
                    double newB = 255 * Math.Pow(pixel.Blue / 255.0, 1.0 / gamma);

                    pixel.Red = (byte)Math.Max(0, Math.Min(255, newR));
                    pixel.Green = (byte)Math.Max(0, Math.Min(255, newG));
                    pixel.Blue = (byte)Math.Max(0, Math.Min(255, newB));

                    gammaImage[y, x] = pixel;
                }
            }

            MainImage.Source = ToBitmapSource(gammaImage);
        }

        private void LevelsChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (sourceImage == null) return;

            Image<Bgr, byte> gammaImage = sourceImage.Clone();

            int levels = (int)LevelsSlider.Value;

            for (int y = 0; y < gammaImage.Rows; y++)
            {
                for (int x = 0; x < gammaImage.Cols; x++)
                {
                    Bgr pixel = gammaImage[y, x];
                    double newR = Math.Round(pixel.Red / 255.0 * levels) / levels * 255.0;
                    double newG = Math.Round(pixel.Green / 255.0 * levels) / levels * 255.0;
                    double newB = Math.Round(pixel.Blue / 255.0 * levels) / levels * 255.0;

                    pixel.Red = (byte)Math.Max(0, Math.Min(255, newR));
                    pixel.Green = (byte)Math.Max(0, Math.Min(255, newG));
                    pixel.Blue = (byte)Math.Max(0, Math.Min(255, newB));

                    gammaImage[y, x] = pixel;
                }
            }

            MainImage.Source = ToBitmapSource(gammaImage);
        }
    }
}
