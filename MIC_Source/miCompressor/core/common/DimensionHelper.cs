using System;
using System.Collections.Generic;
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
        /// The friendly name of the print size (e.g., "4R - Postcard Size").
        /// </summary>
        [AutoNotify] public string commonName;

        /// <summary>
        /// The long edge of the print in inches.   
        /// Reduce margin from this to get the actual image size and multiply by 300 to get the pixel value as 300 DPI is considered for FitInFrame and FixedInFrame.
        /// </summary>
        [AutoNotify] public decimal longEdgeInInch;

        /// <summary>
        /// The short edge of the print in inches. Like long edge, reduce margin from this to get the actual image size and multiply by 300 to get the pixel value as 300 DPI is considered for FitInFrame and FixedInFrame.
        /// </summary>
        [AutoNotify] public decimal shortEdgeInInch;

        /// <summary>
        /// Margin in inch to be left in the image when fitting/fixing in a frame. This is used when dimension strategy is set to FitInFrame and FixedInFrame.
        /// The margin is in inch and is applied to all four sides of the image. 
        /// Multiply by 300 to get the pixel value as 300 DPI is considered for FitInFrame and FixedInFrame.
        /// </summary>
        [AutoNotify] public decimal margin;


        /// <summary>
        /// Initializes a new instance of the <see cref="PrintDimension"/> class with a specified common name.
        /// </summary>
        /// <param name="commonName">The friendly name of the print size.</param>
        /// <param name="longEdge">The longer edge of the print in inches.</param>
        /// <param name="shortEdge">The shorter edge of the print in inches.</param>
        /// <param name="margin">Optional margin size in inches (default is 0.25").</param>
        public PrintDimension(string commonName, decimal longEdge, decimal shortEdge, decimal margin = 0.25m)
        {
            this.commonName = commonName;
            this.longEdgeInInch = longEdge;
            this.shortEdgeInInch = shortEdge;
            this.margin = margin;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="PrintDimension"/> class without specifying a name.
        /// The common name is automatically determined based on dimensions.
        /// </summary>
        /// <param name="longEdge">The longer edge of the print in inches.</param>
        /// <param name="shortEdge">The shorter edge of the print in inches.</param>
        /// <param name="margin">Optional margin size in inches (default is 0.25").</param>
        public PrintDimension(decimal longEdge, decimal shortEdge, decimal margin = 0.25m)
        {
            this.longEdgeInInch = longEdge;
            this.shortEdgeInInch = shortEdge;
            this.margin = margin;
            this.commonName = GetNameFromDimensions(longEdge, shortEdge);
        }

        /// <summary>
        /// Retrieves a list of common print dimensions with their respective sizes and margins.
        /// </summary>
        /// <returns></returns>
        public static PrintDimension[] GetCommonPrintDimensions() =>
        [
            // Small Prints
            new("Wallet - Mini Photo", 3.5m, 2.5m, 0.08m),
            new("3R - Small Print", 5m, 3.5m, 0.08m),

            // Standard Prints
            new("4R - Postcard Size", 6m, 4m, 0.12m),
            new("5R - Phone Size", 7m, 5m, 0.12m),
            new("6R - Desk Photo", 8m, 6m, 0.15m),
            new("8R - Medium Frame Print", 10m, 8m, 0.2m),
            new("10R - Large Frame Print", 12m, 10m, 0.25m),
            new("11R - Art Print", 14m, 11m, 0.25m),
            new("12R - Gallery Print", 16m, 12m, 0.25m),
            new("14R - Small Poster", 17m, 14m, 0.25m),
            new("16R - Medium Poster", 20m, 16m, 0.25m),
            new("20R - Large Poster", 24m, 20m, 0.25m),
            new("24R - Movie Poster", 30m, 24m, 0.25m),

            // A-Series (ISO 216)
            new("A4 - Standard Document", 11.69m, 8.27m),
            new("A3 - Large Document", 16.54m, 11.69m),
            new("A2 - Small Poster", 23.39m, 16.54m),
            new("A1 - Medium Poster", 33.11m, 23.39m),
            new("A0 - Large Poster", 46.81m, 33.11m),

            // US Standard Paper Sizes
            new("Letter - US Standard", 11m, 8.5m),
            new("Legal - US Legal Size", 14m, 8.5m),
            new("Tabloid - Large US Print", 17m, 11m),

            // Passport & Visa Photos
            new("Passport US (2x2 in)", 2m, 2m, 0.08m),
            new("Visa US (2x2 in)", 2m, 2m, 0.08m),
            new("Passport UK (35x45 mm)", 1.77m, 1.38m, 0.08m),
            new("Passport EU (35x45 mm)", 1.77m, 1.38m, 0.08m),
            new("Passport India (51x51 mm)", 2m, 2m, 0.08m),
            new("Passport China (33x48 mm)", 1.89m, 1.3m, 0.08m),
            new("Passport Canada (50x70 mm)", 2.76m, 1.97m, 0.08m)
        ];

        /// <summary>
        /// Retrieves the common print size name based on given dimensions.
        /// </summary>
        /// <param name="longEdge">The longer edge of the print.</param>
        /// <param name="shortEdge">The shorter edge of the print.</param>
        /// <returns>
        /// The corresponding common name if found, otherwise "Unknown Size".
        /// </returns>
        public static string GetNameFromDimensions(decimal longEdge, decimal shortEdge)
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
        public static (int height, int width) GetOutputDimensions(OutputSettings outputSettings, int originalWidth, int originalHeight)
        {
            // Ensure valid input dimensions
            if (originalWidth <= 0 || originalHeight <= 0)
                return (0, 0); // Invalid image size, return zero dimensions safely

            int newHeight = originalHeight;
            int newWidth = originalWidth;

            // Ensure valid primary edge length
            int primaryEdge = Math.Max(1, outputSettings.primaryEdgeLength);

            switch (outputSettings.dimensionStrategy)
            {
                case DimensionReductionStrategy.LongEdge:
                    if (originalWidth > originalHeight)
                    {
                        newWidth = Math.Min(primaryEdge, originalWidth);
                        newHeight = Math.Max(1, (int)Math.Round((double)originalHeight / originalWidth * newWidth));
                    }
                    else
                    {
                        newHeight = Math.Min(primaryEdge, originalHeight);
                        newWidth = Math.Max(1, (int)Math.Round((double)originalWidth / originalHeight * newHeight));
                    }
                    break;

                case DimensionReductionStrategy.MaxHeight:
                    if (originalHeight > primaryEdge)
                    {
                        newHeight = primaryEdge;
                        newWidth = Math.Max(1, (int)Math.Round((double)originalWidth / originalHeight * newHeight));
                    }
                    break;

                case DimensionReductionStrategy.MaxWidth:
                    if (originalWidth > primaryEdge)
                    {
                        newWidth = primaryEdge;
                        newHeight = Math.Max(1, (int)Math.Round((double)originalHeight / originalWidth * newWidth));
                    }
                    break;

                case DimensionReductionStrategy.FixedHeight:
                    newHeight = primaryEdge;
                    newWidth = Math.Max(1, (int)Math.Round((double)originalWidth / originalHeight * newHeight));
                    break;

                case DimensionReductionStrategy.FixedWidth:
                    newWidth = primaryEdge;
                    newHeight = Math.Max(1, (int)Math.Round((double)originalHeight / originalWidth * newWidth));
                    break;

                case DimensionReductionStrategy.FitInFrame:
                case DimensionReductionStrategy.FixedInFrame:
                    {
                        // Convert inches to pixels (300 DPI standard)
                        int marginPixels = Math.Max(0, (int)(outputSettings.PrintDimension.margin * 300));
                        int primaryEdgePixels = Math.Max(1, (int)((outputSettings.PrintDimension.longEdgeInInch - outputSettings.PrintDimension.margin) * 300));
                        int secondaryEdgePixels = Math.Max(1, (int)((outputSettings.PrintDimension.shortEdgeInInch - outputSettings.PrintDimension.margin) * 300));

                        if (originalWidth > originalHeight) // Landscape
                        {
                            newWidth = (outputSettings.dimensionStrategy == DimensionReductionStrategy.FitInFrame)
                                ? Math.Min(primaryEdgePixels, originalWidth)  // No upscaling for FitInFrame
                                : primaryEdgePixels; // Always scale for FixedInFrame

                            newHeight = Math.Max(1, (int)Math.Round((double)originalHeight / originalWidth * newWidth));

                            if (newHeight > secondaryEdgePixels)
                            {
                                newHeight = secondaryEdgePixels;
                                newWidth = Math.Max(1, (int)Math.Round((double)originalWidth / originalHeight * newHeight));
                            }
                        }
                        else // Portrait
                        {
                            newHeight = (outputSettings.dimensionStrategy == DimensionReductionStrategy.FitInFrame)
                                ? Math.Min(primaryEdgePixels, originalHeight)  // No upscaling for FitInFrame
                                : primaryEdgePixels; // Always scale for FixedInFrame

                            newWidth = Math.Max(1, (int)Math.Round((double)originalWidth / originalHeight * newHeight));

                            if (newWidth > secondaryEdgePixels)
                            {
                                newWidth = secondaryEdgePixels;
                                newHeight = Math.Max(1, (int)Math.Round((double)originalHeight / originalWidth * newWidth));
                            }
                        }
                    }
                    break;

                default:
                    // Keep original dimensions (KeepSame strategy)
                    break;
            }

            return (newHeight, newWidth);
        }
    }
}
