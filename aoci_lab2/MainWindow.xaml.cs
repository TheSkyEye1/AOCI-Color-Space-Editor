using System;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Microsoft.Win32;
using Emgu.CV.Structure;
using Emgu.CV;
using System.Security.Claims;

namespace aoci_lab2
{
    public partial class MainWindow : Window
    {

        // --- Код повторяется из лабораторной работы #1 ---

        private Image<Bgr, byte> sourceImage;
        public MainWindow()
        {
            InitializeComponent();

            //Создание изображения в формате HSV
            Image<Hsv, byte> hsvImage = new Image<Hsv, byte>(640, 480);

            //Обращение к каналу Тона (Hue)
            hsvImage.Data[0, 0, 0] = (byte)(128);

            //Данная запись вызовет у нас ошибку так как тон не должен быть >=180
            hsvImage.Data[0, 0, 0] = (byte)(255);

            //Обращение к каналу Насыщенности (Saturation)
            hsvImage.Data[0, 0, 1] = (byte)(200);

            //Обращение к каналу Яркости (Value)
            hsvImage.Data[0, 0, 2] = (byte)(128);


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



        // --- Фильтры и эффекты ---
        private void Sepia_Click(object sender, RoutedEventArgs e)
        {
            if (sourceImage == null) return;

            Image<Bgr, byte> sepiaImage = sourceImage.Clone();

            for (int y = 0; y < sepiaImage.Rows; y++)
            {
                for (int x = 0; x < sepiaImage.Cols; x++)
                {
                    Bgr pixel = sepiaImage[y, x];
                    double r = pixel.Red;
                    double g = pixel.Green;
                    double b = pixel.Blue;

                    //Применяем стандартную матрицу трансформации для получения эффекта сепии.
                    //Эти коэффициенты — результат линейной комбинации, которая "окрашивает" оттенки серого в коричневые тона.
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

        //Обрабатываем изменение значений слайдеров контрастности и насыщенности, выполняя вычисления в цветовом пространстве RGB.
        private void OnRGBFilterChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (sourceImage == null) return;

            Image<Bgr, byte> filteredRGBImage = sourceImage.Clone();

            int brightness = (int)BrightnessRGBSlider.Value;
            double contrast = ContrastRGBSlider.Value;
            double saturation = SaturationRGBSlider.Value;

            if (Math.Abs(brightness - 1.0) >= 1)
            { 
                for (int y = 0; y < filteredRGBImage.Rows; y++)
                {
                    for (int x = 0; x < filteredRGBImage.Cols; x++)
                    {
                        Bgr pixel = filteredRGBImage[y, x];

                        pixel.Red = (byte)Math.Max(0, Math.Min(255, pixel.Red + brightness));
                        pixel.Green = (byte)Math.Max(0, Math.Min(255, pixel.Green + brightness));
                        pixel.Blue = (byte)Math.Max(0, Math.Min(255, pixel.Blue + brightness));

                        filteredRGBImage[y, x] = pixel;
                    }
                }
            }

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

            if (Math.Abs(saturation - 1.0) > 0.01)
            {
                for (int y = 0; y < filteredRGBImage.Rows; y++)
                {
                    for (int x = 0; x < filteredRGBImage.Cols; x++)
                    {
                        Bgr pixel = filteredRGBImage[y, x];
                        double r = pixel.Red;
                        double g = pixel.Green;
                        double b = pixel.Blue;

                        //Находим "яркость" (оттенок серого) пикселя.
                        double gray = r * 0.299 + g * 0.587 + b * 0.114;

                        //Смешиваем исходный цвет с его серым эквивалентом.
                        double newR = gray + (r - gray) * saturation;
                        double newG = gray + (g - gray) * saturation;
                        double newB = gray + (b - gray) * saturation;

                        pixel.Red = (byte)Math.Max(0, Math.Min(255, newR));
                        pixel.Green = (byte)Math.Max(0, Math.Min(255, newG));
                        pixel.Blue = (byte)Math.Max(0, Math.Min(255, newB));

                        filteredRGBImage[y, x] = pixel;
                    }
                }
            }

            MainImage.Source = ToBitmapSource(filteredRGBImage);
        }

        //Обрабатывает изменение значений слайдеров, работая в цветовом пространстве HSV.
        //Это более корректный способ для раздельной коррекции насыщенности и яркости (но не контраста)
        private void OnHSVFilterChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (sourceImage == null) return;

            //Конвертация из BGR в HSV

            // HSV (Hue, Saturation, Value) разделяет цвет на:
            // H - Цветовой тон (какой это цвет: красный, зеленый, синий?). Диапазон 0-179 в EmguCV.
            // S - Насыщенность (насколько цвет "чистый" или "бледный"?). 0-255.
            // V - Значение/Яркость (насколько цвет темный или светлый?). 0-255.

            Image<Hsv, byte> hsvImage = sourceImage.Convert<Hsv, byte>();

            double hsvBrightness = BrightnessHSVSlider.Value;
            double hsvContrast = ContrastHSVSlider.Value;
            double saturation = SaturationHSVSlider.Value;
            double hueShift = HueHSVSlider.Value;

            for (int y = 0; y < hsvImage.Rows; y++)
            {
                for (int x = 0; x < hsvImage.Cols; x++)
                {
                    Hsv pixel = hsvImage[y, x];

                    double v = pixel.Value;
                    pixel.Value = (byte)Math.Max(0, Math.Min(255, v + hsvBrightness));

                    double newV = hsvContrast * (pixel.Value - 128) + 128;
                    pixel.Value = (byte)Math.Max(0, Math.Min(255, newV));

                    //Сдвиг цветового тона(Hue) — простое сложение.
                    //Hue - циклическое значение (0-179). Например, 179(красный)+5 -> 4(красно-оранжевый).
                    double newHue = (pixel.Hue + hueShift / 2.0);
                    if (newHue < 0) newHue += 180;
                    pixel.Hue = (byte)(newHue % 180);

                    //Насыщенность в HSV — простое умножение канала S (Saturation).
                    double newSat = pixel.Satuation * saturation;
                    pixel.Satuation = (byte)Math.Min(255, newSat);

                    hsvImage[y, x] = pixel;
                }
            }

            //ВАЖНО: Конвертируем обратно в BGR, чтобы WPF мог отобразить изображение.
            MainImage.Source = ToBitmapSource(hsvImage.Convert<Bgr, byte>());
        }

        // Применяем гамма-коррекцию к изображению. Это нелинейный способ изменения яркости, который сильнее влияет на средние тона.
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

                    //Формула гамма-коррекции: 255 * (OldValue/255) ^ (1/gamma)
                    //1. pixel.Red / 255.0: Нормализуем значение канала к диапазону [0.0, 1.0].
                    //2. Math.Pow(..., 1.0 / gamma): Возводим в степень (1/gamma).
                    //- Если gamma > 1, изображение становится темнее.
                    //- Если gamma < 1, изображение становится светлее.
                    //3. 255 * (значение предыдущего шага): Возвращаем значение к диапазону [0, 255].
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

        ///Применям эффект постеризации (квантования цвета), уменьшая количество уникальных цветовых оттенков в изображении.
        private void LevelsChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (sourceImage == null) return;

            Image<Bgr, byte> gammaImage = sourceImage.Clone();

            //Количество "уровней" или "шагов" для каждого цветового канала.
            //Если levels = 2, каждый канал может быть только черным или белым. Если levels = 8, у каждого канала будет 8 возможных значений.
            int levels = (int)LevelsSlider.Value;

            for (int y = 0; y < gammaImage.Rows; y++)
            {
                for (int x = 0; x < gammaImage.Cols; x++)
                {
                    Bgr pixel = gammaImage[y, x];

                    //Алгоритм постеризации:
                    //1. (pixel.Red / 255.0): Нормализуем значение [0, 255] -> [0.0, 1.0].
                    //2. ((Значение пред. шага) * levels): Масштабируем до диапазона [0.0, levels].
                    //3. Math.Round(...): Округляем до ближайшего целого уровня.
                    //4. (... / levels): Нормализуем обратно в диапазон [0.0, 1.0], но уже ступенчато.
                    //5. (... * 255.0): Масштабируем обратно до [0, 255].
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
