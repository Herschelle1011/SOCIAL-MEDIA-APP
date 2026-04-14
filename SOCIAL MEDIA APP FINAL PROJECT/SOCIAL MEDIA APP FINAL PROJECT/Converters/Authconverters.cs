using System.Globalization;

namespace SOCIAL_MEDIA_APP_FINAL_PROJECT.Converters
{
    // Background color for the LOGIN tab
    public class BoolToLoginBgConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
            => (value is bool b && b) ? Color.FromArgb("#06C167") : Colors.Transparent;

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }

    // Text color for the LOGIN tab
    public class BoolToLoginTextConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
            => (value is bool b && b) ? Colors.Black : Color.FromArgb("#A0A0A0");

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }

    // Background color for the REGISTER tab
    public class BoolToRegisterBgConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
            => (value is bool b && !b) ? Color.FromArgb("#06C167") : Colors.Transparent;

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }

    // Text color for the REGISTER tab
    public class BoolToRegisterTextConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
            => (value is bool b && !b) ? Colors.Black : Color.FromArgb("#A0A0A0");

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }
}