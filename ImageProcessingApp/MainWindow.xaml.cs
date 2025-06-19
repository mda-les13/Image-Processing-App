using Microsoft.Win32;
using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Threading;

namespace ImageProcessingApp
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private BitmapSource _watermarkImage;
        private readonly DialogService _dialogService = new();
        private readonly ImageProcessor _imageProcessor = new();

        public MainWindow()
        {
            InitializeComponent();
            MinHeight = 450;
            MinWidth = 800;
        }

        #region Event Handlers

        private async void SelectImage_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string filePath = _dialogService.ShowOpenFileDialog();
                if (filePath != null)
                {
                    OriginalImage.Source = await _imageProcessor.LoadImageAsync(filePath);
                }
            }
            catch (Exception ex)
            {
                ShowError("Ошибка загрузки изображения", ex);
            }
        }

        private async void ConvertImageFormat_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (OriginalImage.Source is not BitmapSource bitmap)
                    throw new InvalidOperationException("Изображение не загружено");

                string filePath = _dialogService.ShowSaveFileDialog();
                if (filePath != null)
                {
                    await _imageProcessor.SaveImageAsync(bitmap, filePath);
                }
            }
            catch (Exception ex)
            {
                ShowError("Ошибка конвертации формата", ex);
            }
        }

        private void ConvertToGrayscale_Click(object sender, RoutedEventArgs e) => ProcessImage(_imageProcessor.ConvertToGrayscale);

        private void EnhanceSharpness_Click(object sender, RoutedEventArgs e) =>
            ProcessImage((bitmap) => _imageProcessor.EnhanceSharpness(bitmap, (float)SharpnessSlider.Value));

        private void EnhanceBrightness_Click(object sender, RoutedEventArgs e) =>
            ProcessImage((bitmap) => _imageProcessor.EnhanceBrightness(bitmap, (int)BrightnessSlider.Value));

        private void ConvertToNegative_Click(object sender, RoutedEventArgs e) => ProcessImage(_imageProcessor.ConvertToNegative);

        private void VignetteImage_Click(object sender, RoutedEventArgs e) =>
            ProcessImage((bitmap) => _imageProcessor.ApplyVignette(bitmap, (float)VignetteStrengthSlider.Value));

        private void ApplySepia_Click(object sender, RoutedEventArgs e) =>
            ProcessImage((bitmap) => _imageProcessor.ApplySepia(bitmap, (float)SepiaStrengthSlider.Value));

        private async void AddWatermark_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string filePath = _dialogService.ShowOpenFileDialog("Image files (*.png;*.jpg;*.jpeg)|*.png;*.jpg;*.jpeg");
                if (filePath != null)
                {
                    _watermarkImage = await _imageProcessor.LoadImageAsync(filePath);
                }

                if (_watermarkImage != null && GetCurrentImage() is BitmapSource original)
                {
                    ModifiedImage.Source = _imageProcessor.AddWatermark(original, _watermarkImage);
                }
            }
            catch (Exception ex)
            {
                ShowError("Ошибка добавления водяного знака", ex);
            }
        }

        private async void SaveImage_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (ModifiedImage.Source is not BitmapSource modified)
                    throw new InvalidOperationException("Нет изображения для сохранения");

                string filePath = _dialogService.ShowSaveFileDialog();
                if (filePath != null)
                {
                    await _imageProcessor.SaveImageAsync(modified, filePath);
                }
            }
            catch (Exception ex)
            {
                ShowError("Ошибка сохранения изображения", ex);
            }
        }

        private void RemoveImage_Click(object sender, RoutedEventArgs e)
        {
            OriginalImage.Source = null;
            ModifiedImage.Source = null;
            ModifyOriginalCheckbox.IsChecked = true;
        }

        private void Exit_Click(object sender, RoutedEventArgs e) => Application.Current.Shutdown();

        private void About_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Это приложение позволяет редактировать изображения различных форматов и сохранять их.",
                "О программе", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.O when Keyboard.Modifiers == ModifierKeys.Control:
                    SelectImage_Click(this, null);
                    break;
                case Key.S when Keyboard.Modifiers == ModifierKeys.Control:
                    SaveImage_Click(this, null);
                    break;
                case Key.D when Keyboard.Modifiers == ModifierKeys.Control:
                    RemoveImage_Click(this, null);
                    break;
                case Key.Q when Keyboard.Modifiers == ModifierKeys.Control:
                    ConvertImageFormat_Click(this, null);
                    break;
                case Key.W when Keyboard.Modifiers == ModifierKeys.Control:
                    ConvertToGrayscale_Click(this, null);
                    break;
                case Key.E when Keyboard.Modifiers == ModifierKeys.Control:
                    ConvertToNegative_Click(this, null);
                    break;
                case Key.R when Keyboard.Modifiers == ModifierKeys.Control:
                    EnhanceSharpness_Click(this, null);
                    break;
                case Key.T when Keyboard.Modifiers == ModifierKeys.Control:
                    EnhanceBrightness_Click(this, null);
                    break;
                case Key.Y when Keyboard.Modifiers == ModifierKeys.Control:
                    VignetteImage_Click(this, null);
                    break;
                case Key.U when Keyboard.Modifiers == ModifierKeys.Control:
                    ApplySepia_Click(this, null);
                    break;
                case Key.I when Keyboard.Modifiers == ModifierKeys.Control:
                    AddWatermark_Click(this, null);
                    break;
                case Key.F4:
                    Exit_Click(this, null);
                    break;
            }
        }

        #endregion

        #region Helper Methods

        private BitmapSource GetCurrentImage() =>
            ModifyOriginalCheckbox.IsChecked == true ?
                (BitmapSource)OriginalImage.Source :
                (BitmapSource)ModifiedImage.Source;

        private void ProcessImage(Func<BitmapSource, BitmapSource> processFunction)
        {
            try
            {
                if (GetCurrentImage() is BitmapSource bitmap)
                {
                    ModifiedImage.Source = processFunction(bitmap);
                }
            }
            catch (Exception ex)
            {
                ShowError("Ошибка обработки изображения", ex);
            }
        }

        private void ShowError(string message, Exception ex = null)
        {
            MessageBox.Show($"{message}: {ex?.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
        }

        #endregion

        #region Services

        public class DialogService
        {
            public string ShowOpenFileDialog(string filter = "Image files (*.png;*.jpg;*.jpeg;*.bmp;*.gif;*.tiff)|*.png;*.jpg;*.jpeg;*.bmp;*.gif;*.tiff")
            {
                var dialog = new OpenFileDialog { Filter = filter };
                return dialog.ShowDialog() == true ? dialog.FileName : null;
            }

            public string ShowSaveFileDialog()
            {
                var dialog = new SaveFileDialog
                {
                    Filter = "PNG Image|*.png|JPEG Image|*.jpg|Bitmap Image|*.bmp|TIFF Image|*.tiff",
                    Title = "Сохранить изображение как"
                };
                return dialog.ShowDialog() == true ? dialog.FileName : null;
            }
        }

        public class ImageProcessor
        {
            public async Task<BitmapSource> LoadImageAsync(string filePath)
            {
                return await Task.Run(() =>
                {
                    var bitmap = new BitmapImage();
                    bitmap.BeginInit();
                    bitmap.UriSource = new Uri(filePath);
                    bitmap.CacheOption = BitmapCacheOption.OnLoad;
                    bitmap.EndInit();
                    bitmap.Freeze();
                    return bitmap;
                });
            }

            public async Task SaveImageAsync(BitmapSource bitmap, string filePath)
            {
                await Task.Run(() =>
                {
                    BitmapEncoder encoder = GetEncoder(filePath);
                    encoder.Frames.Add(BitmapFrame.Create(bitmap));
                    using var stream = new FileStream(filePath, FileMode.Create);
                    encoder.Save(stream);
                });
            }

            public BitmapSource ConvertToGrayscale(BitmapSource original) => ApplyColorTransformation(original, (r, g, b) =>
            {
                int gray = (int)(0.3 * r + 0.59 * g + 0.11 * b);
                return (gray << 16) | (gray << 8) | gray;
            });

            public BitmapSource EnhanceSharpness(BitmapSource original, float sharpnessFactor)
            {
                float[] kernel =
                {
                    0, -1 * sharpnessFactor, 0,
                    -1 * sharpnessFactor, 5 * sharpnessFactor, -1 * sharpnessFactor,
                    0, -1 * sharpnessFactor, 0
                };
                return ApplyConvolutionFilter(original, kernel);
            }

            public BitmapSource EnhanceBrightness(BitmapSource original, int brightnessAdjustment) => ApplyPixelTransformation(original, (r, g, b) =>
            {
                r = Math.Clamp(r + brightnessAdjustment, 0, 255);
                g = Math.Clamp(g + brightnessAdjustment, 0, 255);
                b = Math.Clamp(b + brightnessAdjustment, 0, 255);
                return (r << 16) | (g << 8) | b;
            });

            public BitmapSource ConvertToNegative(BitmapSource original) => ApplyPixelTransformation(original, (r, g, b) =>
            {
                r = 255 - r;
                g = 255 - g;
                b = 255 - b;
                return (r << 16) | (g << 8) | b;
            });

            private BitmapSource ApplyColorTransformation(BitmapSource source, Func<int, int, int, int> transform)
            {
                int width = source.PixelWidth;
                int height = source.PixelHeight;
                int[] pixels = new int[width * height];
                source.CopyPixels(pixels, width * 4, 0);
                int[] resultPixels = new int[width * height];

                Parallel.For(0, height, y =>
                {
                    for (int x = 0; x < width; x++)
                    {
                        int offset = y * width + x;
                        int pixel = pixels[offset];
                        int a = (pixel >> 24) & 0xff;
                        int r = (pixel >> 16) & 0xff;
                        int g = (pixel >> 8) & 0xff;
                        int b = pixel & 0xff;

                        int transformed = transform(r, g, b);
                        resultPixels[offset] = (a << 24) | (transformed & 0xffffff);
                    }
                });

                var result = new WriteableBitmap(width, height, source.DpiX, source.DpiY, PixelFormats.Bgra32, null);
                result.WritePixels(new Int32Rect(0, 0, width, height), resultPixels, width * 4, 0);
                return result;
            }

            private BitmapSource ApplyPositionalTransformation(BitmapSource source, Func<int, int, int, int, int, int, int, int> transform)
            {
                int width = source.PixelWidth;
                int height = source.PixelHeight;
                int[] pixels = new int[width * height];
                source.CopyPixels(pixels, width * 4, 0);
                int[] resultPixels = new int[width * height];

                Parallel.For(0, height, y =>
                {
                    for (int x = 0; x < width; x++)
                    {
                        int offset = y * width + x;
                        int pixel = pixels[offset];
                        int a = (pixel >> 24) & 0xff;
                        int r = (pixel >> 16) & 0xff;
                        int g = (pixel >> 8) & 0xff;
                        int b = pixel & 0xff;

                        int transformed = transform(x, y, width, height, r, g, b);
                        resultPixels[offset] = (a << 24) | (transformed & 0xffffff);
                    }
                });

                var result = new WriteableBitmap(width, height, source.DpiX, source.DpiY, PixelFormats.Bgra32, null);
                result.WritePixels(new Int32Rect(0, 0, width, height), resultPixels, width * 4, 0);
                return result;
            }

            public BitmapSource ApplyVignette(BitmapSource original, float strength) => ApplyPositionalTransformation(original, (x, y, width, height, r, g, b) =>
            {
                float centerX = width / 2f;
                float centerY = height / 2f;
                float maxDistance = (float)Math.Sqrt(centerX * centerX + centerY * centerY);
                float distance = (float)Math.Sqrt((x - centerX) * (x - centerX) + (y - centerY) * (y - centerY));
                float factor = 1 - Math.Min(distance / maxDistance, 1) * strength;

                r = (int)(r * factor);
                g = (int)(g * factor);
                b = (int)(b * factor);

                return (r << 16) | (g << 8) | b;
            });

            public BitmapSource ApplySepia(BitmapSource original, float strength) => ApplyPixelTransformation(original, (r, g, b) =>
            {
                int newR = (int)(r * (1 - strength) + (0.393 * r + 0.769 * g + 0.189 * b) * strength);
                int newG = (int)(g * (1 - strength) + (0.349 * r + 0.686 * g + 0.168 * b) * strength);
                int newB = (int)(b * (1 - strength) + (0.272 * r + 0.534 * g + 0.131 * b) * strength);
                return (Math.Clamp(newR, 0, 255) << 16) |
                       (Math.Clamp(newG, 0, 255) << 8) |
                       Math.Clamp(newB, 0, 255);
            });

            public BitmapSource AddWatermark(BitmapSource original, BitmapSource watermark)
            {
                int width = original.PixelWidth;
                int height = original.PixelHeight;
                int[] pixels = new int[width * height];
                original.CopyPixels(pixels, width * 4, 0);

                // Масштабируем водяной знак
                double scale = 0.3;
                int wWidth = (int)(watermark.PixelWidth * scale);
                int wHeight = (int)(watermark.PixelHeight * scale);
                int[] wPixels = new int[wWidth * wHeight];
                int[] wOriginal = new int[watermark.PixelWidth * watermark.PixelHeight];
                watermark.CopyPixels(wOriginal, watermark.PixelWidth * 4, 0);

                for (int y = 0; y < wHeight; y++)
                {
                    for (int x = 0; x < wWidth; x++)
                    {
                        int ox = (int)(x / scale);
                        int oy = (int)(y / scale);
                        wPixels[y * wWidth + x] = wOriginal[oy * watermark.PixelWidth + ox];
                    }
                }

                // Позиционирование
                int posX = width - wWidth - 10;
                int posY = height - wHeight - 10;
                byte opacity = 128;

                for (int y = 0; y < wHeight; y++)
                {
                    for (int x = 0; x < wWidth; x++)
                    {
                        int wOffset = y * wWidth + x;
                        int oOffset = (posY + y) * width + (posX + x);
                        int wPixel = wPixels[wOffset];
                        int a = (wPixel >> 24) & 0xff;

                        if (a > 0)
                        {
                            int oPixel = pixels[oOffset];
                            int or = (oPixel >> 16) & 0xff;
                            int og = (oPixel >> 8) & 0xff;
                            int ob = oPixel & 0xff;
                            int wr = (wPixel >> 16) & 0xff;
                            int wg = (wPixel >> 8) & 0xff;
                            int wb = wPixel & 0xff;

                            int nr = (or * (255 - opacity) + wr * opacity) / 255;
                            int ng = (og * (255 - opacity) + wg * opacity) / 255;
                            int nb = (ob * (255 - opacity) + wb * opacity) / 255;

                            pixels[oOffset] = (255 << 24) | (nr << 16) | (ng << 8) | nb;
                        }
                    }
                }

                var result = new WriteableBitmap(width, height, original.DpiX, original.DpiY, PixelFormats.Bgra32, null);
                result.WritePixels(new Int32Rect(0, 0, width, height), pixels, width * 4, 0);
                return result;
            }

            private BitmapEncoder GetEncoder(string filePath)
            {
                string ext = Path.GetExtension(filePath).ToLower();
                return ext switch
                {
                    ".png" => new PngBitmapEncoder(),
                    ".jpg" or ".jpeg" => new JpegBitmapEncoder(),
                    ".bmp" => new BmpBitmapEncoder(),
                    ".tiff" => new TiffBitmapEncoder(),
                    _ => throw new NotSupportedException($"Формат {ext} не поддерживается")
                };
            }

            private BitmapSource ApplyPixelTransformation(BitmapSource source, Func<int, int, int, int> transform)
            {
                int width = source.PixelWidth;
                int height = source.PixelHeight;
                int[] pixels = new int[width * height];
                source.CopyPixels(pixels, width * 4, 0);

                Parallel.For(0, height, y =>
                {
                    for (int x = 0; x < width; x++)
                    {
                        int offset = y * width + x;
                        int pixel = pixels[offset];
                        int a = (pixel >> 24) & 0xff;
                        int r = (pixel >> 16) & 0xff;
                        int g = (pixel >> 8) & 0xff;
                        int b = pixel & 0xff;

                        int transformed = transform(r, g, b);
                        pixels[offset] = (a << 24) | (transformed & 0xffffff);
                    }
                });

                var result = new WriteableBitmap(width, height, source.DpiX, source.DpiY, PixelFormats.Bgra32, null);
                result.WritePixels(new Int32Rect(0, 0, width, height), pixels, width * 4, 0);
                return result;
            }

            private BitmapSource ApplyConvolutionFilter(BitmapSource source, float[] kernel)
            {
                int width = source.PixelWidth;
                int height = source.PixelHeight;
                int[] pixels = new int[width * height];
                source.CopyPixels(pixels, width * 4, 0);
                int[] resultPixels = new int[width * height];

                int size = (int)Math.Sqrt(kernel.Length);
                int radius = size / 2;

                Parallel.For(radius, height - radius, y =>
                {
                    for (int x = radius; x < width - radius; x++)
                    {
                        int offset = y * width + x;
                        float newR = 0, newG = 0, newB = 0;

                        for (int ky = -radius; ky <= radius; ky++)
                        {
                            for (int kx = -radius; kx <= radius; kx++)
                            {
                                int neighborOffset = (y + ky) * width + (x + kx);
                                float weight = kernel[(ky + radius) * size + (kx + radius)];

                                int pixel = pixels[neighborOffset];
                                newR += ((pixel >> 16) & 0xff) * weight;
                                newG += ((pixel >> 8) & 0xff) * weight;
                                newB += (pixel & 0xff) * weight;
                            }
                        }

                        newR = Math.Clamp(newR, 0, 255);
                        newG = Math.Clamp(newG, 0, 255);
                        newB = Math.Clamp(newB, 0, 255);
                        resultPixels[offset] = (0xff << 24) | ((int)newR << 16) | ((int)newG << 8) | (int)newB;
                    }
                });

                var result = new WriteableBitmap(width, height, source.DpiX, source.DpiY, PixelFormats.Bgra32, null);
                result.WritePixels(new Int32Rect(0, 0, width, height), resultPixels, width * 4, 0);
                return result;
            }

            #endregion
        }
    }
}