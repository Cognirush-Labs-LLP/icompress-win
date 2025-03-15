using miCompressor.core;
using miCompressor.ui.viewmodel;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace miCompressor.ui
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class ErrorAndWarnings : UserControl
    {
        WarningViewModel vm = new WarningViewModel();

        /// <summary>
        /// Gets or sets the selected path.
        /// </summary>
        public string Kind
        {
            get => (string)GetValue(KindProperty);
            set => SetValue(KindProperty, value);
        }

        private static void OnKindChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var control = (ErrorAndWarnings)d;
            if (e.NewValue is string newKind)
            {
                if (newKind == null)
                    return;
                control.vm.CurrentWarningType = newKind;
            }
        }

        /// <summary>
        /// Identifies the <see cref="Kind"/> dependency property.
        /// Possible Values: 
        ///  - Pre Compression Warnings
        ///  - Post Compression Warnings
        ///  - Compression Errors
        /// </summary>
        public static readonly DependencyProperty KindProperty =
            DependencyProperty.Register("Kind", typeof(string), typeof(ErrorAndWarnings), new PropertyMetadata(null, OnKindChanged));

        public ErrorAndWarnings()
        {
            this.InitializeComponent();
        }

        private void CopyToClipboardButton_Click(object sender, RoutedEventArgs e)
        {
            Task.Delay(100).ContinueWith(_ => UIThreadHelper.RunOnUIThread(() =>
            {
                CopyToClipboardIcon.Glyph = "\uE930";
            }));

            Task.Delay(2000).ContinueWith(_ => UIThreadHelper.RunOnUIThread(() =>
            {
                CopyToClipboardIcon.Glyph = "\uE8C8";
            }));

            ClipboardHelper.CopyToClipboard(vm.GetText());
        }
    }
}
