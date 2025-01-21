using Microsoft.Win32;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace ImageProcessingApp
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private BitmapSource watermarkImage;
        public MainWindow()
        {
            InitializeComponent();
            MinHeight = 450;
            MinWidth = 800;
        }

        private void SelectImage_Click(object sender, RoutedEventArgs? e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "Image files (*.png;*.jpg;*.jpeg;*.bmp;*.gif;*.tiff)|*.png;*.jpg;*.jpeg;*.bmp;*.gif;*.tiff";

            if (openFileDialog.ShowDialog() == true)
            {
                BitmapImage bitmap = new BitmapImage(new Uri(openFileDialog.FileName));
                OriginalImage.Source = bitmap;
            }
        }

        private void ConvertImageFormat_Click(object sender, RoutedEventArgs? e)
        {
            if (OriginalImage.Source is BitmapSource originalBitmap)
            {
                SaveFileDialog saveFileDialog = new SaveFileDialog();
                saveFileDialog.Filter = "PNG Image|*.png|JPEG Image|*.jpg|Bitmap Image|*.bmp|TIFF Image|*.tiff";
                saveFileDialog.Title = "Сохранить изображение как";

                if (saveFileDialog.ShowDialog() == true)
                {
                    using (FileStream stream = new FileStream(saveFileDialog.FileName, FileMode.Create))
                    {
                        BitmapEncoder encoder;

                        // Определяем нужный формат
                        switch (System.IO.Path.GetExtension(saveFileDialog.FileName).ToLower())
                        {
                            case ".png":
                                encoder = new PngBitmapEncoder();
                                break;
                            case ".jpg":
                            case ".jpeg":
                                encoder = new JpegBitmapEncoder();
                                break;
                            case ".bmp":
                                encoder = new BmpBitmapEncoder();
                                break;
                            case ".tiff":
                                encoder = new TiffBitmapEncoder();
                                break;
                            default:
                                return;
                        }

                        encoder.Frames.Add(BitmapFrame.Create(originalBitmap));
                        encoder.Save(stream);
                    }
                }
            }
            else
            {
                MessageBox.Show("Сначала выберите изображение.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private BitmapSource GetCurrentImage()
        {
            return ModifyOriginalCheckbox.IsChecked == true ?
                   (BitmapSource)OriginalImage.Source :
                   (BitmapSource)ModifiedImage.Source;
        }

        private void ConvertToGrayscale_Click(object sender, RoutedEventArgs? e)
        {
            if (GetCurrentImage() is BitmapSource originalBitmap)
            {
                int width = originalBitmap.PixelWidth;
                int height = originalBitmap.PixelHeight;
                int[] pixels = new int[width * height];

                originalBitmap.CopyPixels(pixels, width * 4, 0);

                for (int i = 0; i < pixels.Length; i++)
                {
                    int a = (pixels[i] >> 24) & 0xff;
                    int r = (pixels[i] >> 16) & 0xff;
                    int g = (pixels[i] >> 8) & 0xff;
                    int b = pixels[i] & 0xff;

                    int gray = (int)(0.3 * r + 0.59 * g + 0.11 * b);
                    pixels[i] = (a << 24) | (gray << 16) | (gray << 8) | gray; // RGBA
                }

                WriteableBitmap grayBitmap = new WriteableBitmap(width, height, originalBitmap.DpiX, originalBitmap.DpiY, PixelFormats.Bgra32, null);
                grayBitmap.WritePixels(new Int32Rect(0, 0, width, height), pixels, width * 4, 0);
                ModifiedImage.Source = grayBitmap;
            }
            else
            {
                MessageBox.Show("Сначала выберите изображение.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void EnhanceSharpness_Click(object sender, RoutedEventArgs? e)
        {
            if (GetCurrentImage() is BitmapSource originalBitmap)
            {
                int width = originalBitmap.PixelWidth;
                int height = originalBitmap.PixelHeight;
                int[] pixels = new int[width * height];
                originalBitmap.CopyPixels(pixels, width * 4, 0);

                int[] resultPixels = new int[width * height];

                // Ядро для повышения четкости
                float sharpnessFactor = (float)SharpnessSlider.Value; // Получаем степень четкости
                int[] kernel = new int[]
                {
                    0, (int)(-1 * sharpnessFactor), 0,
                    (int)(-1 * sharpnessFactor), (int)(5 * sharpnessFactor), (int)(-1 * sharpnessFactor),
                    0, (int)(-1 * sharpnessFactor), 0
                };

                // Применение свертки
                for (int y = 1; y < height - 1; y++)
                {
                    for (int x = 1; x < width - 1; x++)
                    {
                        int offset = y * width + x;
                        int a = (pixels[offset] >> 24) & 0xff;
                        int r = (pixels[offset] >> 16) & 0xff;
                        int g = (pixels[offset] >> 8) & 0xff;
                        int b = pixels[offset] & 0xff;

                        int newR = 0, newG = 0, newB = 0;

                        // Применение ядра
                        for (int ky = -1; ky <= 1; ky++)
                        {
                            for (int kx = -1; kx <= 1; kx++)
                            {
                                int neighborOffset = (y + ky) * width + (x + kx);
                                int neighborR = (pixels[neighborOffset] >> 16) & 0xff;
                                int neighborG = (pixels[neighborOffset] >> 8) & 0xff;
                                int neighborB = pixels[neighborOffset] & 0xff;

                                newR += neighborR * kernel[(ky + 1) * 3 + (kx + 1)];
                                newG += neighborG * kernel[(ky + 1) * 3 + (kx + 1)];
                                newB += neighborB * kernel[(ky + 1) * 3 + (kx + 1)];
                            }
                        }

                        // Ограничиваем значения
                        newR = Math.Min(Math.Max(newR, 0), 255);
                        newG = Math.Min(Math.Max(newG, 0), 255);
                        newB = Math.Min(Math.Max(newB, 0), 255);

                        resultPixels[offset] = (a << 24) | (newR << 16) | (newG << 8) | newB; // RGBA
                    }
                }

                WriteableBitmap sharpenedBitmap = new WriteableBitmap(width, height, originalBitmap.DpiX, originalBitmap.DpiY, PixelFormats.Bgra32, null);
                sharpenedBitmap.WritePixels(new Int32Rect(0, 0, width, height), resultPixels, width * 4, 0);
                ModifiedImage.Source = sharpenedBitmap;
            }
            else
            {
                MessageBox.Show("Сначала выберите изображение.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void EnhanceBrightness_Click(object sender, RoutedEventArgs? e)
        {
            if (GetCurrentImage() is BitmapSource originalBitmap)
            {
                int width = originalBitmap.PixelWidth;
                int height = originalBitmap.PixelHeight;
                int[] pixels = new int[width * height];
                originalBitmap.CopyPixels(pixels, width * 4, 0);

                int brightnessAdjustment = (int)BrightnessSlider.Value;

                int[] resultPixels = new int[width * height];

                // Применение изменения яркости
                for (int i = 0; i < pixels.Length; i++)
                {
                    int a = (pixels[i] >> 24) & 0xff;
                    int r = (pixels[i] >> 16) & 0xff;
                    int g = (pixels[i] >> 8) & 0xff;
                    int b = pixels[i] & 0xff;

                    r = Math.Min(Math.Max(r + brightnessAdjustment, 0), 255);
                    g = Math.Min(Math.Max(g + brightnessAdjustment, 0), 255);
                    b = Math.Min(Math.Max(b + brightnessAdjustment, 0), 255);

                    resultPixels[i] = (a << 24) | (r << 16) | (g << 8) | b; // RGBA
                }

                WriteableBitmap brightnessAdjustedBitmap = new WriteableBitmap(width, height, originalBitmap.DpiX, originalBitmap.DpiY, PixelFormats.Bgra32, null);
                brightnessAdjustedBitmap.WritePixels(new Int32Rect(0, 0, width, height), resultPixels, width * 4, 0);
                ModifiedImage.Source = brightnessAdjustedBitmap;
            }
            else
            {
                MessageBox.Show("Сначала выберите изображение.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void ConvertToNegative_Click(object sender, RoutedEventArgs? e)
        {
            if (GetCurrentImage() is BitmapSource originalBitmap)
            {
                int width = originalBitmap.PixelWidth;
                int height = originalBitmap.PixelHeight;
                int[] pixels = new int[width * height];
                originalBitmap.CopyPixels(pixels, width * 4, 0);

                int[] resultPixels = new int[width * height];

                // Инвертируем цвета
                for (int i = 0; i < pixels.Length; i++)
                {
                    int a = (pixels[i] >> 24) & 0xff;
                    int r = (pixels[i] >> 16) & 0xff;
                    int g = (pixels[i] >> 8) & 0xff;
                    int b = pixels[i] & 0xff;

                    r = 255 - r;
                    g = 255 - g;
                    b = 255 - b;

                    resultPixels[i] = (a << 24) | (r << 16) | (g << 8) | b; // RGBA
                }

                WriteableBitmap negativeBitmap = new WriteableBitmap(width, height, originalBitmap.DpiX, originalBitmap.DpiY, PixelFormats.Bgra32, null);
                negativeBitmap.WritePixels(new Int32Rect(0, 0, width, height), resultPixels, width * 4, 0);
                ModifiedImage.Source = negativeBitmap;
            }
            else
            {
                MessageBox.Show("Сначала выберите изображение.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void VignetteImage_Click(object sender, RoutedEventArgs? e)
        {
            if (GetCurrentImage() is BitmapSource originalBitmap)
            {
                int width = originalBitmap.PixelWidth;
                int height = originalBitmap.PixelHeight;
                int[] pixels = new int[width * height];
                originalBitmap.CopyPixels(pixels, width * 4, 0);

                int[] resultPixels = new int[width * height];

                // Центр изображения
                float centerX = width / 2f;
                float centerY = height / 2f;
                float maxDistance = (float)Math.Sqrt(centerX * centerX + centerY * centerY);
                float vignetteStrength = (float)VignetteStrengthSlider.Value; // Получаем степень виньетирования

                // Применение виньетирования
                for (int y = 0; y < height; y++)
                {
                    for (int x = 0; x < width; x++)
                    {
                        int offset = y * width + x;
                        int a = (pixels[offset] >> 24) & 0xff;
                        int r = (pixels[offset] >> 16) & 0xff;
                        int g = (pixels[offset] >> 8) & 0xff;
                        int b = pixels[offset] & 0xff;

                        // Рассчёт расстояния до центра
                        float distance = (float)Math.Sqrt((x - centerX) * (x - centerX) + (y - centerY) * (y - centerY));
                        float factor = 1 - Math.Min(distance / maxDistance, 1) * vignetteStrength;

                        // Применение виньетирования
                        r = (int)(r * factor);
                        g = (int)(g * factor);
                        b = (int)(b * factor);

                        resultPixels[offset] = (a << 24) | (r << 16) | (g << 8) | b; // RGBA
                    }
                }

                WriteableBitmap vignettedBitmap = new WriteableBitmap(width, height, originalBitmap.DpiX, originalBitmap.DpiY, PixelFormats.Bgra32, null);
                vignettedBitmap.WritePixels(new Int32Rect(0, 0, width, height), resultPixels, width * 4, 0);
                ModifiedImage.Source = vignettedBitmap;
            }
            else
            {
                MessageBox.Show("Сначала выберите изображение.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void ApplySepia_Click(object sender, RoutedEventArgs? e)
        {
            if (GetCurrentImage() is BitmapSource originalBitmap)
            {
                int width = originalBitmap.PixelWidth;
                int height = originalBitmap.PixelHeight;
                int[] pixels = new int[width * height];
                originalBitmap.CopyPixels(pixels, width * 4, 0);

                int[] resultPixels = new int[width * height];

                // Получение степени сепии
                float sepiaStrength = (float)SepiaStrengthSlider.Value;

                // Применение эффекта сепии
                for (int i = 0; i < pixels.Length; i++)
                {
                    int a = (pixels[i] >> 24) & 0xff;
                    int r = (pixels[i] >> 16) & 0xff;
                    int g = (pixels[i] >> 8) & 0xff;
                    int b = pixels[i] & 0xff;

                    // Применение сепии
                    int newR = (int)(r * (1 - sepiaStrength) + (0.393 * r + 0.769 * g + 0.189 * b) * sepiaStrength);
                    int newG = (int)(g * (1 - sepiaStrength) + (0.349 * r + 0.686 * g + 0.168 * b) * sepiaStrength);
                    int newB = (int)(b * (1 - sepiaStrength) + (0.272 * r + 0.534 * g + 0.131 * b) * sepiaStrength);

                    // Ограничиваем значения
                    newR = Math.Min(Math.Max(newR, 0), 255);
                    newG = Math.Min(Math.Max(newG, 0), 255);
                    newB = Math.Min(Math.Max(newB, 0), 255);

                    resultPixels[i] = (a << 24) | (newR << 16) | (newG << 8) | newB; // RGBA
                }

                WriteableBitmap sepiaBitmap = new WriteableBitmap(width, height, originalBitmap.DpiX, originalBitmap.DpiY, PixelFormats.Bgra32, null);
                sepiaBitmap.WritePixels(new Int32Rect(0, 0, width, height), resultPixels, width * 4, 0);
                ModifiedImage.Source = sepiaBitmap;
            }
            else
            {
                MessageBox.Show("Сначала выберите изображение.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void AddWatermark_Click(object sender, RoutedEventArgs? e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "Image files (*.png;*.jpg;*.jpeg)|*.png;*.jpg;*.jpeg";

            if (openFileDialog.ShowDialog() == true)
            {
                watermarkImage = new BitmapImage(new Uri(openFileDialog.FileName));
            }

            if (GetCurrentImage() is BitmapSource originalBitmap && watermarkImage != null)
            {
                int width = originalBitmap.PixelWidth;
                int height = originalBitmap.PixelHeight;

                WriteableBitmap resultBitmap = new WriteableBitmap(width, height, originalBitmap.DpiX, originalBitmap.DpiY, PixelFormats.Bgra32, null);
                int[] pixels = new int[width * height];
                originalBitmap.CopyPixels(pixels, width * 4, 0);

                int originalWatermarkWidth = watermarkImage.PixelWidth;
                int originalWatermarkHeight = watermarkImage.PixelHeight;

                // Коэффициент уменьшения
                double scaleFactor = 0.3;
                int watermarkWidth = (int)(originalWatermarkWidth * scaleFactor);
                int watermarkHeight = (int)(originalWatermarkHeight * scaleFactor);

                // Создаем новое изображение для водяного знака с уменьшенными размерами
                WriteableBitmap scaledWatermark = new WriteableBitmap(watermarkWidth, watermarkHeight, watermarkImage.DpiX, watermarkImage.DpiY, PixelFormats.Bgra32, null);
                int[] watermarkPixels = new int[watermarkWidth * watermarkHeight];
                int[] originalWatermarkPixels = new int[originalWatermarkWidth * originalWatermarkHeight];
                var watermarkBitmap = new WriteableBitmap(watermarkImage);
                watermarkBitmap.CopyPixels(originalWatermarkPixels, originalWatermarkWidth * 4, 0);

                // Масштабируем водяной знак с помощью простого усреднения пикселей
                for (int y = 0; y < watermarkHeight; y++)
                {
                    for (int x = 0; x < watermarkWidth; x++)
                    {
                        int originalX = (int)(x / scaleFactor);
                        int originalY = (int)(y / scaleFactor);
                        int originalOffset = originalY * originalWatermarkWidth + originalX;

                        watermarkPixels[y * watermarkWidth + x] = originalWatermarkPixels[originalOffset];
                    }
                }

                // Позиционирование водяного знака в правом нижнем углу
                int watermarkX = width - watermarkWidth - 10; // Отступ 10 пикселей
                int watermarkY = height - watermarkHeight - 10;

                // Уровень прозрачности (0-255, где 255 - полная непрозрачность)
                byte transparencyLevel = 128; // Полупрозрачный (50%)

                // Наложение водяного знака
                for (int y = 0; y < watermarkHeight; y++)
                {
                    for (int x = 0; x < watermarkWidth; x++)
                    {
                        int watermarkOffset = y * watermarkWidth + x;
                        int originalOffset = (watermarkY + y) * width + (watermarkX + x);

                        int watermarkA = (watermarkPixels[watermarkOffset] >> 24) & 0xff;

                        // Проверяем, если пиксель водяного знака не прозрачный
                        if (watermarkA > 0)
                        {
                            // Смешиваем цвета с учетом прозрачности
                            int originalR = (pixels[originalOffset] >> 16) & 0xff;
                            int originalG = (pixels[originalOffset] >> 8) & 0xff;
                            int originalB = pixels[originalOffset] & 0xff;

                            int watermarkR = (watermarkPixels[watermarkOffset] >> 16) & 0xff;
                            int watermarkG = (watermarkPixels[watermarkOffset] >> 8) & 0xff;
                            int watermarkB = watermarkPixels[watermarkOffset] & 0xff;

                            // Смешивание цветов
                            int newR = (originalR * (255 - transparencyLevel) + watermarkR * transparencyLevel) / 255;
                            int newG = (originalG * (255 - transparencyLevel) + watermarkG * transparencyLevel) / 255;
                            int newB = (originalB * (255 - transparencyLevel) + watermarkB * transparencyLevel) / 255;

                            // Обновляем пиксель основного изображения
                            pixels[originalOffset] = (255 << 24) | (newR << 16) | (newG << 8) | newB; // RGBA
                        }
                    }
                }

                resultBitmap.WritePixels(new Int32Rect(0, 0, width, height), pixels, width * 4, 0);
                ModifiedImage.Source = resultBitmap;
            }
            else
            {
                MessageBox.Show("Сначала выберите изображение.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void SaveImage_Click(object sender, RoutedEventArgs? e)
        {
            if (ModifiedImage.Source is BitmapSource modifiedBitmap)
            {
                SaveFileDialog saveFileDialog = new SaveFileDialog();
                saveFileDialog.Filter = "Image files (*.png;*.jpg;*.jpeg;*.bmp;*.gif;*.tiff)|*.png;*.jpg;*.jpeg;*.bmp;*.gif;*.tiff";
                saveFileDialog.DefaultExt = ".png";

                if (saveFileDialog.ShowDialog() == true)
                {
                    BitmapEncoder encoder = null;
                    string extension = System.IO.Path.GetExtension(saveFileDialog.FileName).ToLower();

                    switch (extension)
                    {
                        case ".jpg":
                        case ".jpeg":
                            encoder = new JpegBitmapEncoder();
                            break;
                        case ".png":
                            encoder = new PngBitmapEncoder();
                            break;
                        case ".bmp":
                            encoder = new BmpBitmapEncoder();
                            break;
                        case ".gif":
                            encoder = new GifBitmapEncoder();
                            break;
                        case ".tiff":
                            encoder = new TiffBitmapEncoder();
                            break;
                    }

                    if (encoder != null)
                    {
                        encoder.Frames.Add(BitmapFrame.Create(modifiedBitmap));
                        using (var fileStream = new FileStream(saveFileDialog.FileName, FileMode.Create))
                        {
                            encoder.Save(fileStream);
                        }
                    }
                }
            }
            else
            {
                MessageBox.Show("Сначала преобразуйте изображение.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void RemoveImage_Click(object sender, RoutedEventArgs? e)
        {
            OriginalImage.Source = null;
            ModifiedImage.Source = null;
            ModifyOriginalCheckbox.IsChecked = true;
        }

        private void Exit_Click(object sender, RoutedEventArgs? e)
        {
            Application.Current.Shutdown();
        }

        private void About_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Это приложение позволяет редактировать изображения различных форматов и сохранять их.", "О программе", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.O && Keyboard.Modifiers == ModifierKeys.Control)
            {
                SelectImage_Click(this, null);
            }

            else if (e.Key == Key.S && Keyboard.Modifiers == ModifierKeys.Control)
            {
                SaveImage_Click(this, null);
            }

            else if (e.Key == Key.D && Keyboard.Modifiers == ModifierKeys.Control)
            {
                RemoveImage_Click(this, null);
            }

            else if (e.Key == Key.Q && Keyboard.Modifiers == ModifierKeys.Control)
            {
                ConvertImageFormat_Click(this, null);
            }

            else if (e.Key == Key.W && Keyboard.Modifiers == ModifierKeys.Control)
            {
                ConvertToGrayscale_Click(this, null);
            }

            else if (e.Key == Key.E && Keyboard.Modifiers == ModifierKeys.Control)
            {
                ConvertToNegative_Click(this, null);
            }

            else if (e.Key == Key.R && Keyboard.Modifiers == ModifierKeys.Control)
            {
                EnhanceSharpness_Click(this, null);
            }

            else if (e.Key == Key.T && Keyboard.Modifiers == ModifierKeys.Control)
            {
                EnhanceBrightness_Click(this, null);
            }

            else if (e.Key == Key.Y && Keyboard.Modifiers == ModifierKeys.Control)
            {
                VignetteImage_Click(this, null);
            }

            else if (e.Key == Key.U && Keyboard.Modifiers == ModifierKeys.Control)
            {
                ApplySepia_Click(this, null);
            }

            else if (e.Key == Key.I && Keyboard.Modifiers == ModifierKeys.Control)
            {
                AddWatermark_Click(this, null);
            }

            else if (e.Key == Key.F4)
            {
                Exit_Click(this, null);
            }
        }
    }
}