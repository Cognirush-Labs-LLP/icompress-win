using Microsoft.UI.Xaml.Media;
using Microsoft.UI;
using Windows.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace miCompressor.ui
{
    public class BlendedBrushes
    {
        public SolidColorBrush ThemedRed { get; } = CreateBlendedBrush(Colors.Red, GetSystemPrimaryColor(), 0.5);

        private static SolidColorBrush CreateBlendedBrush(Color baseColor, Color overlayColor, double overlayOpacity)
        {
            byte r = (byte)((baseColor.R * (1 - overlayOpacity)) + (overlayColor.R * overlayOpacity));
            byte g = (byte)((baseColor.G * (1 - overlayOpacity)) + (overlayColor.G * overlayOpacity));
            byte b = (byte)((baseColor.B * (1 - overlayOpacity)) + (overlayColor.B * overlayOpacity));
            Color blendedColor = Color.FromArgb(255, r, g, b);
            return new SolidColorBrush(blendedColor);
        }

        private static Color GetSystemAccentColor()
        {
            return (Color)App.Current.Resources["SystemAccentColor"];
        }

        private static Color GetSystemPrimaryColor()
        {
            return ((SolidColorBrush)App.Current.Resources["Primary_100"]).Color;
        }
    }
}
