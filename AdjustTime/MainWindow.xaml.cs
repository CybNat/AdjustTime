using System;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Media.Imaging;
using System.Globalization;
using Microsoft.Win32;
using System.Collections.Generic;

namespace AdjustTime
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private string[] selectedFiles = new string[]{};

        private void LoadFiles()
        {
            _listBoxFiles.Items.Clear();
            var sels = new List<string>();
            foreach (string fileName in selectedFiles)
            {
                try
                {
                    var exifData = new ExifData();
                    exifData.Caption = fileName;
                    Stream imageStreamSource = new FileStream(fileName, FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite);
                    BitmapDecoder decoder = BitmapDecoder.Create(imageStreamSource, BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.Default);
                    InPlaceBitmapMetadataWriter pngInplace = decoder.Frames[0].CreateInPlaceBitmapMetadataWriter();

                    if (pngInplace != null)
                    {
                        if (pngInplace.DateTaken != null)
                            exifData.DateTaken = DateTime.Parse(pngInplace.DateTaken);
                    }
                    imageStreamSource.Close();
                    _listBoxFiles.Items.Add(exifData);
                    sels.Add(fileName);
                }
                catch { }
            }
            selectedFiles = sels.ToArray();
        }

        private void _buttonBrowse_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog{Multiselect = true};
            if (dialog.ShowDialog() == false) return;
            selectedFiles = dialog.FileNames;
            LoadFiles();
        }

        private void buttonRefresh_Click(object sender, RoutedEventArgs e)
        {
            LoadFiles();
        }

        private ulong rational(double a)
        {
            uint denom = 1000;
            uint num = (uint)(a * denom);
            ulong tmp;
            tmp = (ulong)denom << 32;
            tmp |= (ulong)num;
            return tmp;
        }

        private void buttonAdjust_Click(object sender, RoutedEventArgs e)
        {
            int Year = int.Parse(_textBoxYear.Text);
            int Month = int.Parse(_textBoxMonth.Text);
            int Day = int.Parse(_textBoxDay.Text);
            int Hour = int.Parse(_textBoxHour.Text);
            int Minute = int.Parse(_textBoxMinute.Text);
            int Second = int.Parse(_textBoxSecond.Text);
            foreach (string fileName in selectedFiles)
            {
                FileStream Foto = File.Open(fileName, FileMode.Open, FileAccess.Read);
                // открыли файл по адресу s для чтения

                BitmapDecoder decoder = JpegBitmapDecoder.Create(Foto, BitmapCreateOptions.IgnoreColorProfile, BitmapCacheOption.Default);
                //"распаковали" снимок и создали объект decoder

                BitmapMetadata TmpImgEXIF = (BitmapMetadata)decoder.Frames[0].Metadata.Clone();
                //считали и сохранили метаданные

                DateTime taken = TmpImgEXIF.DateTaken == null ? DateTime.Now : Convert.ToDateTime(TmpImgEXIF.DateTaken);
                DateTime DateOfShot = taken.AddYears(Year).AddMonths(Month).AddDays(Day).AddHours(Hour).AddMinutes(Minute).AddSeconds(Second);
                if (DateOfShot<= new DateTime(2011,12,31,23,59,59))
                {
                    DateOfShot = DateOfShot.AddHours(-1);
                }
                var news = DateOfShot.ToString("yyyy:MM:dd HH:mm:ss");
                TmpImgEXIF.SetQuery("/app1/ifd/exif/{ushort=36867}", news);
                TmpImgEXIF.SetQuery("/app13/irb/8bimiptc/iptc/date created", news);
                TmpImgEXIF.SetQuery("/xmp/xmp:CreateDate", news);
                TmpImgEXIF.SetQuery("/app1/ifd/exif/{ushort=36868}", news);
                TmpImgEXIF.SetQuery("/xmp/exif:DateTimeOriginal", news);

                JpegBitmapEncoder Encoder = new JpegBitmapEncoder();//создали новый энкодер для Jpeg
                Encoder.Frames.Add(BitmapFrame.Create(decoder.Frames[0], decoder.Frames[0].Thumbnail, TmpImgEXIF, decoder.Frames[0].ColorContexts)); //добавили в энкодер новый кадр(он там всего один) с указанными параметрами
                string NewFileName = fileName + "+.jpg";//имя исходного файла +GeoTag.jpg
                using (Stream jpegStreamOut = File.Open(NewFileName, FileMode.Create, FileAccess.ReadWrite))//создали новый файл
                {
                    Encoder.Save(jpegStreamOut);//сохранили новый файл
                }
                Foto.Close();//и закрыли исходный файл

                if (File.Exists(fileName + "+.jpg"))
                {
                    if (!File.Exists(fileName + ".backup"))
                    {
                        File.Move(fileName, fileName + ".backup");
                    }
                    else
                    {
                        File.Delete(fileName);
                    }
                    File.Move(fileName + "+.jpg", fileName);
                    File.Delete(fileName + "+.jpg");
                }
                GC.Collect();
            }
            LoadFiles();
        }

        private void _buttonRestore_Click(object sender, RoutedEventArgs e)
        {
            foreach (string fileName in selectedFiles)
            {
                if (File.Exists(fileName + ".backup"))
                {
                    File.Replace(fileName + ".backup", fileName, null);
                    File.Delete(fileName + ".backup");
                }
            }
            LoadFiles();
        }
        private void _buttonClearRestore_Click(object sender, RoutedEventArgs e)
        {
            foreach (string fileName in selectedFiles)
            {
                if (File.Exists(fileName + ".backup"))
                {
                     File.Delete(fileName + ".backup");
                }
            }
        }
    }

    class ExifData
    {
        public string Caption;
        public DateTime DateTaken;
        public override string ToString()
        {
            return string.Format("{0} ({1})",Caption, DateTaken);
        }
    }
}
