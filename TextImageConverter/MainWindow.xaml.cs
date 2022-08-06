using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using Newtonsoft.Json;

namespace TextImageConverter
{
    public partial class MainWindow : Window
    {
        private static Dictionary<string, int> Wordbook = new Dictionary<string, int>();
        private static Dictionary<int, string> reverseWordbook = new Dictionary<int, string>();
        private static string SelectedImage;
        private static string ImageFolder = $"{AppDomain.CurrentDomain.BaseDirectory}Output\\";

        public MainWindow()
        {
            InitializeComponent();
            Directory.CreateDirectory(ImageFolder);
        }

        private void inputText_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                e.Handled = true;
            }
        }

        private void selectWordbookButton_Click(object sender, RoutedEventArgs e)
        {
            Wordbook.Clear();
            Microsoft.Win32.OpenFileDialog openDialog = new Microsoft.Win32.OpenFileDialog();
            bool? result = openDialog.ShowDialog();
            if (result == true)
            {
                try
                {
                    wordbookPathLabel.Content = openDialog.FileName;
                    using (StreamReader sr = new StreamReader(openDialog.FileName))
                    using (JsonTextReader reader = new JsonTextReader(sr))
                    {
                        while (reader.Read())
                        {
                            if (reader.TokenType == JsonToken.PropertyName)
                            {
                                string word = reader.Value.ToString();
                                int num = (int)reader.ReadAsInt32();
                                Wordbook.Add(word, num);
                                reverseWordbook.Add(num, word);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Read file error: {ex.Message}");
                }
                textToImageGroup.IsEnabled = true;
                imageToTextGroup.IsEnabled = true;
            }
        }

        private void convertToImageButton_Click(object sender, RoutedEventArgs e)
        {
            var inputWords = inputText.Text.Split(' ', StringSplitOptions.RemoveEmptyEntries).Where(w => Wordbook.ContainsKey(w));
            Bitmap image = new Bitmap(inputWords.Count(), 1, PixelFormat.Format24bppRgb);
            LockBitmap lockImage = new LockBitmap(image);
            lockImage.LockBits();
            int x = 0;
            foreach (string word in inputWords)
            {
                Color clr = NumToColor(Wordbook[word]);
                lockImage.SetPixel(x, 0, clr);
                x++;
            }
            lockImage.UnlockBits();
            try
            {
                image.Save($"{ImageFolder}output.png", ImageFormat.Png);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Save image error: {ex.Message}");
            }
            image.Dispose();
        }

        private void selectImageButton_Click(object sender, RoutedEventArgs e)
        {
            Microsoft.Win32.OpenFileDialog openDialog = new Microsoft.Win32.OpenFileDialog();
            bool? result = openDialog.ShowDialog();
            if (result == true)
            {
                imagePathLabel.Content = openDialog.FileName;
                SelectedImage = openDialog.FileName;
            }
        }

        private void convertToTextButton_Click(object sender, RoutedEventArgs e)
        {
            Bitmap image = new Bitmap(SelectedImage);
            try
            {
                LockBitmap lockImage = new LockBitmap(image);
                lockImage.LockBits();
                List<string> words = new List<string>();
                for (int x = 0; x < lockImage.Width; x++)
                {
                    int num = ColorToNum(lockImage.GetPixel(x, 0));
                    if (reverseWordbook.TryGetValue(num, out string word))
                    {
                        words.Add(word);
                    }
                }
                outputText.Text = string.Join(" ", words);
                image.Dispose();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Read image error: {ex.Message}");
            }
        }

        private static Color NumToColor(int num)
        {
            return Color.FromArgb((byte)(num & 255), (byte)((num >> 8) & 255), (byte)((num >> 16) & 255));
        }

        private static int ColorToNum(Color clr)
        {
            return clr.R + clr.G * 256 + clr.B * 65536;
        }

        private void openImageFolder_Click(object sender, RoutedEventArgs e)
        {
            Process.Start("explorer.exe", ImageFolder);
        }
    }
}
