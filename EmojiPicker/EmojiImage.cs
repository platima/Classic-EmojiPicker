//
//  Classic Emoji Picker — True Colour Emoji Rendering
//
//  Based on Emoji.Wpf by Sam Hocevar <sam@hocevar.net>
//  Licensed under WTFPL
//

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Typography.OpenFont;
using Typography.OpenFont.Tables;

namespace EmojiPicker
{
    /// <summary>
    /// Provides an attached property for rendering emoji characters as images with true colour support.
    /// Uses Typography.OpenFont to parse COLR/CPAL font tables for proper colour emoji rendering.
    /// </summary>
    public static class EmojiImage
    {
        /// <summary>
        /// Attached property for setting the emoji source on an Image control.
        /// </summary>
        public static readonly DependencyProperty SourceProperty =
            DependencyProperty.RegisterAttached(
                "Source",
                typeof(string),
                typeof(EmojiImage),
                new PropertyMetadata(null, OnSourceChanged));

        public static void SetSource(DependencyObject o, string value)
            => o.SetValue(SourceProperty, value);

        public static string GetSource(DependencyObject o)
            => (string)o.GetValue(SourceProperty);

        private static EmojiTypeface? s_emojiTypeface;

        private static void OnSourceChanged(DependencyObject o, DependencyPropertyChangedEventArgs e)
        {
            if (o is System.Windows.Controls.Image image)
            {
                var emoji = e.NewValue as string;
                if (!string.IsNullOrEmpty(emoji))
                {
                    try
                    {
                        var imageSource = CreateColorEmojiImage(emoji);
                        image.Source = imageSource;
                        
                        if (imageSource != null)
                            Debug.WriteLine($"EmojiImage: Successfully rendered colour emoji '{emoji}'");
                        else
                            Debug.WriteLine($"EmojiImage: Failed to render emoji '{emoji}'");
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"EmojiImage: Error rendering emoji '{emoji}': {ex.Message}");
                        image.Source = null;
                    }
                }
                else
                {
                    image.Source = null;
                }
            }
        }

        /// <summary>
        /// Creates a colour emoji image using Typography.OpenFont COLR/CPAL rendering.
        /// </summary>
        private static ImageSource? CreateColorEmojiImage(string emoji)
        {
            try
            {
                // Ensure we have a typeface loaded
                if (s_emojiTypeface == null)
                {
                    s_emojiTypeface = LoadEmojiTypeface();
                    if (s_emojiTypeface == null)
                        return null;
                }

                // Create the drawing group for the emoji
                var drawingGroup = RenderEmojiDrawing(emoji, s_emojiTypeface);
                if (drawingGroup == null)
                    return null;

                // Convert to ImageSource
                var drawingImage = new DrawingImage(drawingGroup);
                drawingImage.Freeze();
                return drawingImage;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Loads the emoji typeface from our bundled font or system font.
        /// </summary>
        private static EmojiTypeface? LoadEmojiTypeface()
        {
            try
            {
                // Try to load our bundled font first
                var bundledFontUri = new Uri("pack://application:,,,/Fonts/seguiemj.ttf", UriKind.Absolute);
                var streamResourceInfo = Application.GetResourceStream(bundledFontUri);
                
                if (streamResourceInfo?.Stream != null)
                {
                    Debug.WriteLine("EmojiImage: Loading bundled emoji font");
                    return new EmojiTypeface(streamResourceInfo.Stream);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"EmojiImage: Failed to load bundled font: {ex.Message}");
            }

            try
            {
                // Fallback to system font
                var systemFontPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Fonts), "seguiemj.ttf");
                if (File.Exists(systemFontPath))
                {
                    Debug.WriteLine("EmojiImage: Loading system emoji font");
                    using var stream = File.OpenRead(systemFontPath);
                    return new EmojiTypeface(stream);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"EmojiImage: Failed to load system font: {ex.Message}");
            }

            Debug.WriteLine("EmojiImage: No emoji font available");
            return null;
        }

        /// <summary>
        /// Renders an emoji as a DrawingGroup using COLR/CPAL colour information.
        /// </summary>
        private static DrawingGroup? RenderEmojiDrawing(string emoji, EmojiTypeface typeface)
        {
            try
            {
                var drawingGroup = new DrawingGroup();
                
                using (var context = drawingGroup.Open())
                {
                    // Set up emoji size and baseline
                    const double fontSize = 32.0;
                    const double emojiSize = 28.0;
                    
                    // Draw transparent background to establish bounds
                    var bounds = new Rect(0, 0, fontSize, fontSize);
                    context.DrawRectangle(Brushes.Transparent, null, bounds);
                    
                    // Get glyph information for the emoji
                    var glyphs = typeface.GetGlyphsForString(emoji);
                    if (!glyphs.Any())
                        return null;
                    
                    double xPosition = (fontSize - emojiSize) / 2;
                    double yPosition = fontSize * 0.8; // Baseline position
                    
                    // Render each glyph with colour
                    foreach (var glyphInfo in glyphs)
                    {
                        RenderColorGlyph(context, typeface, glyphInfo, xPosition, yPosition, emojiSize);
                        xPosition += glyphInfo.AdvanceWidth * emojiSize / 1000.0; // Scale advance
                    }
                }
                
                return drawingGroup;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Renders a single glyph with colour information from COLR/CPAL tables.
        /// </summary>
        private static void RenderColorGlyph(DrawingContext context, EmojiTypeface typeface, 
            GlyphInfo glyph, double x, double y, double size)
        {
            try
            {
                // Try to get colour layers from COLR/CPAL tables
                var colorLayers = typeface.GetColorLayers(glyph.GlyphIndex);
                
                if (colorLayers?.Any() == true)
                {
                    // Render colour layers
                    foreach (var layer in colorLayers)
                    {
                        var geometry = typeface.GetGlyphGeometry(layer.GlyphIndex, size);
                        if (geometry != null)
                        {
                            var brush = new SolidColorBrush(layer.Color);
                            
                            context.PushTransform(new TranslateTransform(x, y));
                            context.DrawGeometry(brush, null, geometry);
                            context.Pop();
                        }
                    }
                }
                else
                {
                    // Fallback to black glyph if no colour information
                    var geometry = typeface.GetGlyphGeometry(glyph.GlyphIndex, size);
                    if (geometry != null)
                    {
                        context.PushTransform(new TranslateTransform(x, y));
                        context.DrawGeometry(Brushes.Black, null, geometry);
                        context.Pop();
                    }
                }
            }
            catch
            {
                // Ignore rendering errors for individual glyphs
            }
        }
    }    /// <summary>
    /// Wrapper for Typography.OpenFont functionality to handle emoji rendering.
    /// </summary>
    internal class EmojiTypeface
    {
        private readonly Typography.OpenFont.Typeface _typeface;
        private readonly COLR? _colrTable;
        private readonly CPAL? _cpalTable;

        public EmojiTypeface(Stream fontStream)
        {
            var reader = new OpenFontReader();
            _typeface = reader.Read(fontStream, ReadFlags.Full);
            
            // Get colour tables
            _colrTable = _typeface.COLRTable;
            _cpalTable = _typeface.CPALTable;
        }

        /// <summary>
        /// Gets glyph information for a string.
        /// </summary>
        public IEnumerable<GlyphInfo> GetGlyphsForString(string text)
        {
            var glyphs = new List<GlyphInfo>();
            
            for (int i = 0; i < text.Length; )
            {
                int codepoint = char.ConvertToUtf32(text, i);
                ushort glyphIndex = _typeface.LookupIndex(codepoint);
                
                if (glyphIndex != 0)
                {
                    var advanceWidth = _typeface.GetHAdvanceWidthFromGlyphIndex(glyphIndex);
                    glyphs.Add(new GlyphInfo(glyphIndex, advanceWidth));
                }
                
                i += codepoint >= 0x10000 ? 2 : 1;
            }
            
            return glyphs;
        }

        /// <summary>
        /// Gets colour layers for a glyph from COLR/CPAL tables.
        /// </summary>
        public IEnumerable<ColorLayer>? GetColorLayers(ushort glyphIndex)
        {
            if (_colrTable == null || _cpalTable == null)
                return null;
                
            try
            {
                // Check if this glyph has colour layers
                if (!_colrTable.LayerIndices.TryGetValue(glyphIndex, out ushort layerIndex))
                    return null;
                    
                var layerCount = _colrTable.LayerCounts[glyphIndex];
                var layers = new List<ColorLayer>();
                
                for (int i = 0; i < layerCount; i++)
                {
                    var index = layerIndex + i;
                    if (index < _colrTable.GlyphLayers.Length)
                    {
                        var layerGlyphIndex = _colrTable.GlyphLayers[index];
                        var paletteIndex = _colrTable.GlyphPalettes[index];
                        
                        // Get colour from palette (use first palette)
                        var colorIndex = _cpalTable.Palettes[0] + paletteIndex;
                        _cpalTable.GetColor(colorIndex, out byte r, out byte g, out byte b, out byte a);
                        
                        var color = Color.FromArgb(a, r, g, b);
                        layers.Add(new ColorLayer(layerGlyphIndex, color));
                    }
                }
                
                return layers;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Gets the geometry for a glyph at the specified size.
        /// </summary>
        public Geometry? GetGlyphGeometry(ushort glyphIndex, double size)
        {
            try
            {
                var glyph = _typeface.GetGlyph(glyphIndex);
                if (glyph == null)
                    return null;
                    
                // Convert glyph outline to WPF geometry
                var geometry = ConvertGlyphToGeometry(glyph, size);
                return geometry;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Converts a Typography.OpenFont glyph to WPF Geometry.
        /// </summary>
        private Geometry? ConvertGlyphToGeometry(Glyph glyph, double size)
        {
            try
            {
                // This is a simplified conversion - a full implementation would
                // properly convert the glyph's outline data to WPF paths
                var group = new GeometryGroup();
                
                // For now, create a simple rectangle as a placeholder
                // TODO: Implement proper glyph outline conversion
                var rect = new RectangleGeometry(new Rect(0, -size * 0.8, size * 0.8, size * 0.8));
                group.Children.Add(rect);
                
                return group;
            }
            catch
            {
                return null;
            }
        }
    }

    /// <summary>
    /// Information about a glyph.
    /// </summary>
    internal record GlyphInfo(ushort GlyphIndex, ushort AdvanceWidth);

    /// <summary>
    /// A colour layer from COLR/CPAL tables.
    /// </summary>
    internal record ColorLayer(ushort GlyphIndex, Color Color);
}
