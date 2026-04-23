using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace Quad64_new
{
    // テキストが空のときVisibleを返す（プレースホルダー表示用）
    public class EmptyStringToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
            => string.IsNullOrEmpty(value as string) ? Visibility.Visible : Visibility.Collapsed;

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }
}
