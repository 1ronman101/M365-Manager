using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;

namespace M365Manager.Views;

/// <summary>
/// Inverts a boolean value.
/// </summary>
public class InvertedBoolConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
        => value is bool b ? !b : value;

    public object ConvertBack(object value, Type targetType, object parameter, string language)
        => value is bool b ? !b : value;
}

/// <summary>
/// Returns Collapsed when true, Visible when false.
/// </summary>
public class InvertedBoolToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
        => value is bool b && b ? Visibility.Collapsed : Visibility.Visible;

    public object ConvertBack(object value, Type targetType, object parameter, string language)
        => throw new NotImplementedException();
}

/// <summary>
/// Returns Visible when bool is true, Collapsed when false.
/// </summary>
public class BoolToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
        => value is bool b && b ? Visibility.Visible : Visibility.Collapsed;

    public object ConvertBack(object value, Type targetType, object parameter, string language)
        => throw new NotImplementedException();
}

/// <summary>
/// Returns Visible when count is zero (for "no items" messages).
/// </summary>
public class ZeroCountToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
        => value is int count && count == 0 ? Visibility.Visible : Visibility.Collapsed;

    public object ConvertBack(object value, Type targetType, object parameter, string language)
        => throw new NotImplementedException();
}

/// <summary>
/// Returns Visible when count is greater than zero.
/// </summary>
public class NonZeroCountToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
        => value is int count && count > 0 ? Visibility.Visible : Visibility.Collapsed;

    public object ConvertBack(object value, Type targetType, object parameter, string language)
        => throw new NotImplementedException();
}

/// <summary>
/// Converts bool account status to a friendly colour resource key.
/// </summary>
public class AccountStatusToColorConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
        => value is bool enabled && enabled ? "SystemFillColorSuccessBrush" : "SystemFillColorCriticalBrush";

    public object ConvertBack(object value, Type targetType, object parameter, string language)
        => throw new NotImplementedException();
}

/// <summary>
/// Converts bool to "Active"/"Disabled" text.
/// </summary>
public class AccountStatusTextConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
        => value is bool enabled && enabled ? "Active" : "Disabled";

    public object ConvertBack(object value, Type targetType, object parameter, string language)
        => throw new NotImplementedException();
}

/// <summary>
/// Toggle button text: shows "Disable" for enabled accounts, "Enable" for disabled.
/// </summary>
public class ToggleAccountTextConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
        => value is bool enabled && enabled ? "Disable Account" : "Enable Account";

    public object ConvertBack(object value, Type targetType, object parameter, string language)
        => throw new NotImplementedException();
}
