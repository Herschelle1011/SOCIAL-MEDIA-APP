using System.Globalization;

namespace SOCIAL_MEDIA_APP_FINAL_PROJECT.Converters
{
    // Edit button background: green when editing (Cancel), transparent when not
    public class BoolToEditBgConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
            => (value is bool b && b) ? Color.FromArgb("#1A1A1A") : Color.FromArgb("#0D2B1A");

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }

    // Edit button text color
    public class BoolToEditTextConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
            => (value is bool b && b) ? Color.FromArgb("#FF4444") : Color.FromArgb("#06C167");

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }

    // Field border stroke: green when editing, dark when read-only
    public class BoolToFieldStrokeConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
            => (value is bool b && b) ? Color.FromArgb("#06C167") : Color.FromArgb("#2A2A2A");

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }
}