using System;
using System.Collections.Generic;
using System.Windows.Media.Imaging;
using System.Windows.Media;
using System.Windows;

namespace IPS.Core.Models
{
    /// <summary>
    /// Represents a menu item available from an unmanned kiosk system
    /// </summary>
    public class MenuItem
    {
        private BitmapImage? _imageCache;
        private static BitmapSource? _defaultPlaceholder;

        /// <summary>
        /// Unique identifier for this menu item (UUID)
        /// </summary>
        public string MenuId { get; set; } = string.Empty;

        /// <summary>
        /// Display name of the menu item (e.g., "Cafe Latte", "Hot Americano")
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Detailed description of the menu item
        /// </summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// Base price of the menu item
        /// </summary>
        public decimal Price { get; set; }

        /// <summary>
        /// Currency unit for the price (e.g., "USD", "KRW", "EUR")
        /// </summary>
        public string PriceUnit { get; set; } = string.Empty;

        /// <summary>
        /// Path to the menu item's image (can be local file path or pack URI)
        /// </summary>
        public string ImagePath { get; set; } = string.Empty;

        /// <summary>
        /// Cached BitmapImage for faster UI rendering
        /// Automatically loaded from ImagePath when first accessed
        /// Returns default placeholder if no image is available
        /// </summary>
        public BitmapSource? Image
        {
            get
            {
                if (_imageCache == null && !string.IsNullOrEmpty(ImagePath))
                {
                    try
                    {
                        _imageCache = new BitmapImage();
                        _imageCache.BeginInit();
                        _imageCache.UriSource = new Uri(ImagePath, UriKind.RelativeOrAbsolute);
                        _imageCache.CacheOption = BitmapCacheOption.OnLoad;
                        _imageCache.DecodePixelWidth = 300; // Optimize size for display
                        _imageCache.EndInit();
                        _imageCache.Freeze(); // Makes it thread-safe and improves performance
                    }
                    catch
                    {
                        _imageCache = null; // Fallback to placeholder
                    }
                }

                // Return image if loaded, otherwise return default placeholder
                return _imageCache ?? GetDefaultPlaceholder();
            }
        }

        /// <summary>
        /// Creates a default placeholder image with a coffee gradient
        /// </summary>
        private static BitmapSource GetDefaultPlaceholder()
        {
            if (_defaultPlaceholder == null)
            {
                int width = 245;
                int height = 175;
                int dpi = 96;

                var drawingVisual = new DrawingVisual();
                using (var context = drawingVisual.RenderOpen())
                {
                    // Coffee gradient background
                    var gradient = new LinearGradientBrush(
                        Color.FromRgb(0xD4, 0xA5, 0x74), // AccentLatte
                        Color.FromRgb(0xF5, 0xE6, 0xD3), // LightCream
                        new Point(0, 0),
                        new Point(0, 1)
                    );

                    context.DrawRectangle(gradient, null, new Rect(0, 0, width, height));

                    // Draw coffee cup icon (simple representation)
                    var cupBrush = new SolidColorBrush(Color.FromRgb(0x6F, 0x4E, 0x37)); // PrimaryBrown
                    var pen = new Pen(cupBrush, 3);

                    // Cup body (trapezoid)
                    var cupPath = new PathGeometry();
                    var figure = new PathFigure { StartPoint = new Point(width / 2 - 40, height / 2) };
                    figure.Segments.Add(new LineSegment(new Point(width / 2 - 30, height / 2 + 50), true));
                    figure.Segments.Add(new LineSegment(new Point(width / 2 + 30, height / 2 + 50), true));
                    figure.Segments.Add(new LineSegment(new Point(width / 2 + 40, height / 2), true));
                    figure.IsClosed = true;
                    cupPath.Figures.Add(figure);

                    context.DrawGeometry(null, pen, cupPath);

                    // Steam lines
                    for (int i = -1; i <= 1; i++)
                    {
                        var steamPen = new Pen(cupBrush, 2);
                        context.DrawLine(steamPen,
                            new Point(width / 2 + i * 15, height / 2 - 10),
                            new Point(width / 2 + i * 15 + 5, height / 2 - 30));
                    }
                }

                var bitmap = new RenderTargetBitmap(width, height, dpi, dpi, PixelFormats.Pbgra32);
                bitmap.Render(drawingVisual);
                bitmap.Freeze();

                _defaultPlaceholder = bitmap;
            }

            return _defaultPlaceholder;
        }

        /// <summary>
        /// Whether this menu item is currently available for ordering
        /// </summary>
        public bool IsAvailable { get; set; }

        /// <summary>
        /// Category identifier (UUID) - groups related items together
        /// </summary>
        public string CategoryId { get; set; } = string.Empty;

        /// <summary>
        /// Display name of the category (e.g., "Coffee (Hot)", "Non-Coffee (Iced)")
        /// </summary>
        public string CategoryName { get; set; } = string.Empty;

        /// <summary>
        /// Available options/customizations for this menu item (optional)
        /// Options with the same OptionCategoryId are mutually exclusive
        /// Can be null or empty if no options are available
        /// </summary>
        public List<MenuOption>? Options { get; set; }
    }
}
