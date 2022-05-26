using System;
using System.IO;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media.Imaging;

namespace MolkZip
{
    [ValueConversion(typeof(string), typeof(BitmapImage))]
    public class HeaderToImageConverter : IValueConverter
    {

        public static HeaderToImageConverter Instance = new HeaderToImageConverter();

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var path = (string)value;

            if (path == null)
                return null;

            /*if (new FileInfo(path).Attributes.HasFlag(FileAttributes.Hidden))
            {
                return null;
            }*/

            var name = MainWindow.GetFileFolderName(path);

            var image = "Assets/file.png";

            if (string.IsNullOrEmpty(name))
                image = "Assets/drive.png";
            else if(new FileInfo(path).Attributes.HasFlag(FileAttributes.Directory))
                image = "Assets/folder-closed.png";

            return new BitmapImage(new Uri($"pack://application:,,,/{image}"));
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
