using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace miCompressor.core
{
    /// <summary>
    /// Represents a standard print dimension with a common name, long edge, short edge, and optional margin.
    /// Provides methods to retrieve common print dimensions and lookup names based on dimensions.
    /// </summary>
    public partial class PrintDimension
    {
        /// <summary>
        /// The friendly name of the print size (e.g., "4R - Postcard Size"). This is never used in OutputSettings. Only user facing for combobox option string.
        /// </summary>
        [AutoNotify] public string commonName;

        /// <summary>
        /// The long edge of the print in inches.   
        /// Reduce margin from this to get the actual image size and multiply by 300 to get the pixel value as 300 DPI is considered for FitInFrame and FixedInFrame.
        /// </summary>
        [AutoNotify] public double longEdgeInInch;

        /// <summary>
        /// The short edge of the print in inches. Like long edge, reduce margin from this to get the actual image size and multiply by 300 to get the pixel value as 300 DPI is considered for FitInFrame and FixedInFrame.
        /// </summary>
        [AutoNotify] public double shortEdgeInInch;

        /// <summary>
        /// Margin in inch to be left in the image when fitting/fixing in a frame. This is used when dimension strategy is set to FitInFrame and FixedInFrame.
        /// The margin is in inch and is applied to all four sides of the image. 
        /// Multiply by 300 to get the pixel value as 300 DPI is considered for FitInFrame and FixedInFrame.
        /// </summary>
        [AutoNotify] public double margin;


        /// <summary>
        /// Initializes a new instance of the <see cref="PrintDimension"/> class with a specified common name.
        /// </summary>
        /// <param name="commonName">The friendly name of the print size.</param>
        /// <param name="longEdge">The longer edge of the print in inches.</param>
        /// <param name="shortEdge">The shorter edge of the print in inches.</param>
        /// <param name="margin">Optional margin size in inches (default is 0.25").</param>
        public PrintDimension(string commonName, double longEdge, double shortEdge, double margin = 0.25)
        {
            this.commonName = commonName;
            this.longEdgeInInch = longEdge;
            this.shortEdgeInInch = shortEdge;
            this.margin = margin;
        }

        /// <summary>
        /// Only for Deserialization purpose. 
        /// </summary>
        public PrintDimension() : this("Wallet - Mini Photo", 3.5, 2.5, 0.08)
        {           
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="PrintDimension"/> class without specifying a name.
        /// The common name is automatically determined based on dimensions.
        /// </summary>
        /// <param name="longEdge">The longer edge of the print in inches.</param>
        /// <param name="shortEdge">The shorter edge of the print in inches.</param>
        /// <param name="margin">Optional margin size in inches (default is 0.25").</param>
        public PrintDimension(double longEdge, double shortEdge, double margin = 0.25)
        {
            this.longEdgeInInch = longEdge;
            this.shortEdgeInInch = shortEdge;
            this.margin = margin;
            this.commonName = GetNameFromDimensions(longEdge, shortEdge);
        }

        /// <summary>
        /// Sets values such that no property changed call back is invoked for each property. Helpful when dealing with combobox + textbox for allowing custom values. property change even for ALL properties are invoked once all properties are set.
        /// </summary>
        /// <param name="printDimension">PrintDimension to copy value from </param>
        public void CopyValues(PrintDimension printDimension)
        {
            bool longEdgeInInchChanged = false;
            bool shortEdgeInInchChanged = false;
            bool marginChanged = false;

            if (this.longEdgeInInch != printDimension.longEdgeInInch)
            {
                this.longEdgeInInch = printDimension.longEdgeInInch;
                longEdgeInInchChanged = true;
            }
            if (this.shortEdgeInInch != printDimension.shortEdgeInInch)
            {
                this.shortEdgeInInch = printDimension.shortEdgeInInch;
                shortEdgeInInchChanged = true;
            }
            if (this.margin != printDimension.margin)
            {
                this.margin = printDimension.margin;
                marginChanged = true;
            }

            if (longEdgeInInchChanged)
            {
                OnPropertyChanged(nameof(LongEdgeInInch));
            }
            if (shortEdgeInInchChanged)
            {
                OnPropertyChanged(nameof(ShortEdgeInInch));
            }
            if (marginChanged)
            {
                OnPropertyChanged(nameof(Margin));
            }
        }
        /// <summary>
        /// Retrieves a list of common print dimensions with their respective sizes and margins.
        /// </summary>
        /// <returns></returns>
        public static PrintDimension[] GetCommonPrintDimensions() =>
        [

            new("Custom Size",0,0,0),
            
            // Small Prints
            new("Wallet - Mini Photo", 3.5, 2.5, 0.08),
            new("3R - Small Print", 5, 3.5, 0.08),

            // Standard Prints
            new("4R - Postcard Size", 6, 4, 0.12),
            new("5R - Phone Size", 7, 5, 0.12),
            new("6R - Desk Photo", 8, 6, 0.15),
            new("8R - Medium Frame Print", 10, 8, 0.2),
            new("10R - Large Frame Print", 12, 10, 0.25),
            new("11R - Art Print", 14, 11, 0.25),
            new("12R - Gallery Print", 16, 12, 0.25),
            new("14R - Small Poster", 17, 14, 0.25),
            new("16R - Medium Poster", 20, 16, 0.25),
            new("20R - Large Poster", 24, 20, 0.25),
            new("24R - Movie Poster", 30, 24, 0.25),

            // A-Series (ISO 216)
            new("A4 - Standard Document", 11.69, 8.27),
            new("A3 - Large Document", 16.54, 11.69),
            new("A2 - Small Poster", 23.39, 16.54),
            new("A1 - Medium Poster", 33.11, 23.39),
            new("A0 - Large Poster", 46.81, 33.11),

            // US Standard Paper Sizes
            new("Letter - US Standard", 11, 8.5),
            new("Legal - US Legal Size", 14, 8.5),
            new("Tabloid - Large US Print", 17, 11),

            // Passport & Visa Photos
            new("Passport US (2x2 in)", 2, 2, 0.08),
            new("Visa US (2x2 in)", 2, 2, 0.08),
            new("Passport UK (35x45 mm)", 1.77, 1.38, 0.08),
            new("Passport EU (35x45 mm)", 1.77, 1.38, 0.08),
            new("Passport India (51x51 mm)", 2, 2, 0.08),
            new("Passport China (33x48 mm)", 1.89, 1.3, 0.08),
            new("Passport Canada (50x70 mm)", 2.76, 1.97, 0.08)
        ];

        /// <summary>
        /// Retrieves the common print size name based on given dimensions.
        /// </summary>
        /// <param name="longEdge">The longer edge of the print.</param>
        /// <param name="shortEdge">The shorter edge of the print.</param>
        /// <returns>
        /// The corresponding common name if found, otherwise "Unknown Size".
        /// </returns>
        public static string GetNameFromDimensions(double longEdge, double shortEdge)
        {
            var match = GetCommonPrintDimensions()
                .FirstOrDefault(p => (p.longEdgeInInch == longEdge && p.shortEdgeInInch == shortEdge) ||
                                     (p.longEdgeInInch == shortEdge && p.shortEdgeInInch == longEdge));

            return match?.commonName ?? "Custom Size";
        }

    }

    /// <summary>
    /// Helper class to calculate output dimensions based on the provided settings.
    /// </summary>
    public static class DimensionHelper
    {
        /// <summary>
        /// Calculates the output dimensions based on the provided output settings.
        /// This method ensures correct aspect ratio preservation and handles various dimension strategies.
        /// </summary>
        /// <param name="outputSettings">Output settings containing dimension reduction strategies.</param>
        /// <param name="originalWidth">Original image width in pixels.</param>
        /// <param name="originalHeight">Original image height in pixels.</param>
        /// <returns>A tuple representing the new height and width.</returns>
        public static (uint height, uint width) GetOutputDimensions(OutputSettings outputSettings, uint originalHeight, uint originalWidth)
        {
            // Ensure valid input dimensions
            if (originalWidth <= 0 || originalHeight <= 0)
                return (0, 0); // Invalid image size, return zero dimensions safely

            uint newHeight = originalHeight;
            uint newWidth = originalWidth;

            // Ensure valid primary edge length
            uint primaryEdge = Math.Max(1, outputSettings.primaryEdgeLength);

            switch (outputSettings.dimensionStrategy)
            {
                case DimensionReductionStrategy.Percentage:
                    // Reduce both dimensions by the given percentage
                    double scale = outputSettings.percentageOfLongEdge / 100.0;
                    newWidth = (uint)Math.Round(originalWidth * scale);
                    newHeight = (uint)Math.Round(originalHeight * scale);
                    break;

                case DimensionReductionStrategy.LongEdge:
                    uint maxLongEdge = primaryEdge;
                    uint longEdge = Math.Max(originalWidth, originalHeight);
                    if (longEdge > maxLongEdge)
                    {
                        double ratio = (double)maxLongEdge / longEdge;
                        newWidth = (uint)Math.Round(originalWidth * ratio);
                        newHeight = (uint)Math.Round(originalHeight * ratio);
                    }
                    break;

                case DimensionReductionStrategy.MaxHeight:
                    if (originalHeight > primaryEdge)
                    {
                        newHeight = primaryEdge;
                        newWidth = Math.Max(1, (uint)Math.Round((double)originalWidth / originalHeight * newHeight));
                    }
                    break;

                case DimensionReductionStrategy.MaxWidth:
                    if (originalWidth > primaryEdge)
                    {
                        newWidth = primaryEdge;
                        newHeight = Math.Max(1, (uint)Math.Round((double)originalHeight / originalWidth * newWidth));
                    }
                    break;

                case DimensionReductionStrategy.FixedHeight:
                    newHeight = primaryEdge;
                    newWidth = Math.Max(1, (uint)Math.Round((double)originalWidth / originalHeight * newHeight));
                    break;

                case DimensionReductionStrategy.FixedWidth:
                    newWidth = primaryEdge;
                    newHeight = Math.Max(1, (uint)Math.Round((double)originalHeight / originalWidth * newWidth));
                    break;

                case DimensionReductionStrategy.FitInFrame:
                case DimensionReductionStrategy.FixedInFrame:
                    {
                        // Convert inches to pixels (300 DPI standard)
                        uint marginPixels = Math.Max(0, (uint)(outputSettings.PrintDimension.margin * 300));
                        uint primaryEdgePixels = Math.Max(1, (uint)((outputSettings.PrintDimension.longEdgeInInch - outputSettings.PrintDimension.margin) * 300));
                        uint secondaryEdgePixels = Math.Max(1, (uint)((outputSettings.PrintDimension.shortEdgeInInch - outputSettings.PrintDimension.margin) * 300));

                        if (originalWidth > originalHeight) // Landscape
                        {
                            newWidth = (outputSettings.dimensionStrategy == DimensionReductionStrategy.FitInFrame)
                                ? Math.Min(primaryEdgePixels, originalWidth)  // No upscaling for FitInFrame
                                : primaryEdgePixels; // Always scale for FixedInFrame

                            newHeight = Math.Max(1, (uint)Math.Round((double)originalHeight / originalWidth * newWidth));

                            if (newHeight > secondaryEdgePixels)
                            {
                                newHeight = secondaryEdgePixels;
                                newWidth = Math.Max(1, (uint)Math.Round((double)originalWidth / originalHeight * newHeight));
                            }
                        }
                        else // Portrait
                        {
                            newHeight = (outputSettings.dimensionStrategy == DimensionReductionStrategy.FitInFrame)
                                ? Math.Min(primaryEdgePixels, originalHeight)  // No upscaling for FitInFrame
                                : primaryEdgePixels; // Always scale for FixedInFrame

                            newWidth = Math.Max(1, (uint)Math.Round((double)originalWidth / originalHeight * newHeight));

                            if (newWidth > secondaryEdgePixels)
                            {
                                newWidth = secondaryEdgePixels;
                                newHeight = Math.Max(1, (uint)Math.Round((double)originalHeight / originalWidth * newWidth));
                            }
                        }
                    }
                    break;

                default:
                    // Keep original dimensions (KeepSame strategy)
                    break;
            }

            // Ensure at least 1 pixel in each dimension
            if (newWidth < 1) newWidth = 1;
            if (newHeight < 1) newHeight = 1;

            return (newHeight, newWidth);
        }
    }
}
