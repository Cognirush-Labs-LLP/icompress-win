using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace miCompressor.ui
{
    public class NullToBooleanConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            return value == null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Converts a string to Visibility. 
    /// If the string is null or empty, it returns Collapsed; otherwise, returns Visible.
    /// </summary>
    public class StringEmptyToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            string text = value as string;
            return string.IsNullOrWhiteSpace(text) ? Visibility.Collapsed : Visibility.Visible;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Converts a boolean value to a Visibility value, where true results in Collapsed and false results in Visible.
    /// </summary>
    public class ReverseBoolToVisibilityConverter : IValueConverter
    {
        /// <summary>
        /// Converts a boolean value to a Visibility value.
        /// </summary>
        /// <param name="value">The boolean value to convert.</param>
        /// <param name="targetType">The target type (ignored).</param>
        /// <param name="parameter">Optional parameter (ignored).</param>
        /// <param name="language">The culture info (ignored).</param>
        /// <returns>Visibility.Collapsed if true, Visibility.Visible if false.</returns>
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is bool boolValue)
            {
                return boolValue ? Visibility.Collapsed : Visibility.Visible;
            }
            return Visibility.Visible; // Default fallback
        }

        /// <summary>
        /// Converts a Visibility value back to a boolean.
        /// </summary>
        /// <param name="value">The Visibility value.</param>
        /// <param name="targetType">The target type (ignored).</param>
        /// <param name="parameter">Optional parameter (ignored).</param>
        /// <param name="language">The culture info (ignored).</param>
        /// <returns>True if Collapsed, False if Visible.</returns>
        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            if (value is Visibility visibility)
            {
                return visibility == Visibility.Collapsed;
            }
            return false; // Default fallback
        }
    }
}
