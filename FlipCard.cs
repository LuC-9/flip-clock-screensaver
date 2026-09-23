using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Drawing.Text;

namespace FliqloClock
{
    public class FlipCard : IDisposable
    {
        private string currentValue = "";
        private string targetValue = "";
        private float progress = 1.0f; // 1.0f indicates resting (no active animation)

        private Bitmap currentBmp = null;
        private Bitmap targetBmp = null;

        private string cachedCurrentVal = null;
        private string cachedTargetVal = null;
        private int cachedWidth = 0;
        private int cachedHeight = 0;
        private string cachedAmPm = null;

        public void SetValues(string current, string target, float animationProgress)
        {
            currentValue = current ?? "";
            targetValue = target ?? "";
            progress = animationProgress;
        }

        public void Draw(Graphics g, int x, int y, int width, int height, string amPmText)
        {
            if (width <= 0 || height <= 0) return;

            UpdateCache(width, height, amPmText);

            float radius = width * 0.065f;
            if (radius < 2.0f) radius = 2.0f;

            int halfH = height / 2;
            Rectangle srcTop = new Rectangle(0, 0, width, halfH);
            Rectangle srcBottom = new Rectangle(0, halfH, width, height - halfH);

            Rectangle rectTop = new Rectangle(x, y, width, halfH);
            Rectangle rectBottom = new Rectangle(x, y + halfH, width, height - halfH);

            GraphicsState state = g.Save();
            try
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.InterpolationMode = InterpolationMode.HighQualityBilinear;
                g.PixelOffsetMode = PixelOffsetMode.HighQuality;

                // Clip all card rendering to rounded card contour so corners remain smoothly rounded throughout animation
                using (GraphicsPath cardPath = GetRoundedRectPath(new RectangleF(x, y, width, height), radius))
                {
                    g.SetClip(cardPath);

                    if (progress >= 1.0f || currentValue == targetValue)
                    {
                        // 1. Static resting card
                        if (targetBmp != null)
                        {
                            g.DrawImage(targetBmp, x, y);
                        }
                        else if (currentBmp != null)
                        {
                            g.DrawImage(currentBmp, x, y);
                        }
                    }
                    else
                    {
                        // 2. Active 3D Flip animation
                        // Static top half reveals the new (target) digit
                        if (targetBmp != null)
                        {
                            g.DrawImage(targetBmp, rectTop, srcTop, GraphicsUnit.Pixel);
                        }

                        // Static bottom half shows the old (current) digit waiting to be covered
                        if (currentBmp != null)
                        {
                            g.DrawImage(currentBmp, rectBottom, srcBottom, GraphicsUnit.Pixel);
                        }

                        double angleRad = progress * Math.PI;
                        float cosVal = (float)Math.Cos(angleRad);

                        if (progress < 0.5f)
                        {
                            // First half of flip: Old top flap folds downward to center horizontal line
                            float flapHeight = halfH * cosVal;
                            if (flapHeight > 0.5f)
                            {
                                RectangleF rectFlip = new RectangleF(x, y + halfH - flapHeight, width, flapHeight);
                                if (currentBmp != null)
                                {
                                    g.DrawImage(currentBmp, rectFlip, srcTop, GraphicsUnit.Pixel);
                                }

                                // Darkening shadow over folding flap as it rotates away from light
                                int flapAlpha = (int)Math.Min(180, Math.Max(0, progress * 2.0f * 180));
                                if (flapAlpha > 0)
                                {
                                    using (LinearGradientBrush flapShadow = new LinearGradientBrush(
                                        rectFlip,
                                        Color.FromArgb(flapAlpha, 0, 0, 0),
                                        Color.FromArgb((int)(flapAlpha * 0.2f), 0, 0, 0),
                                        LinearGradientMode.Vertical))
                                    {
                                        g.FillRectangle(flapShadow, rectFlip);
                                    }
                                }
                            }

                            // Cast drop shadow on the static bottom card
                            float shadowDepth = halfH * 0.6f;
                            int dropAlpha = (int)Math.Min(150, Math.Max(0, (1.0f - cosVal) * 120));
                            if (dropAlpha > 0)
                            {
                                RectangleF dropRect = new RectangleF(x, y + halfH, width, shadowDepth);
                                using (LinearGradientBrush dropBrush = new LinearGradientBrush(
                                    dropRect,
                                    Color.FromArgb(dropAlpha, 0, 0, 0),
                                    Color.Transparent,
                                    LinearGradientMode.Vertical))
                                {
                                    g.FillRectangle(dropBrush, dropRect);
                                }
                            }
                        }
                        else
                        {
                            // Second half of flip: New bottom flap unfolds downward from horizontal center line
                            float flapHeight = halfH * (-cosVal);
                            if (flapHeight > 0.5f)
                            {
                                RectangleF rectFlip = new RectangleF(x, y + halfH, width, flapHeight);
                                if (targetBmp != null)
                                {
                                    g.DrawImage(targetBmp, rectFlip, srcBottom, GraphicsUnit.Pixel);
                                }

                                // Flap unfolds from shadow to full light
                                int flapAlpha = (int)Math.Min(180, Math.Max(0, (1.0f - progress) * 2.0f * 180));
                                if (flapAlpha > 0)
                                {
                                    using (LinearGradientBrush flapShadow = new LinearGradientBrush(
                                        rectFlip,
                                        Color.FromArgb((int)(flapAlpha * 0.25f), 0, 0, 0),
                                        Color.FromArgb(flapAlpha, 0, 0, 0),
                                        LinearGradientMode.Vertical))
                                    {
                                        g.FillRectangle(flapShadow, rectFlip);
                                    }
                                }
                            }

                            // Top card subtle shadow near hinge
                            float shadowDepth = halfH * 0.4f;
                            int topAlpha = (int)Math.Min(120, Math.Max(0, (-cosVal) * 100));
                            if (topAlpha > 0)
                            {
                                RectangleF topShadowRect = new RectangleF(x, y + halfH - shadowDepth, width, shadowDepth);
                                using (LinearGradientBrush topBrush = new LinearGradientBrush(
                                    topShadowRect,
                                    Color.Transparent,
                                    Color.FromArgb(topAlpha, 0, 0, 0),
                                    LinearGradientMode.Vertical))
                                {
                                    g.FillRectangle(topBrush, topShadowRect);
                                }
                            }
                        }
                    }

                    // 3. Iconic Fliqlo mechanical details:
                    // A. Horizontal split groove line
                    int dividerY = y + halfH;
                    using (Pen groovePen = new Pen(Color.FromArgb(10, 11, 13), 2.0f))
                    {
                        g.DrawLine(groovePen, x, dividerY, x + width, dividerY);
                    }

                    // B. Bevel highlight just below the split line
                    using (Pen highlightPen = new Pen(Color.FromArgb(52, 55, 62), 1.0f))
                    {
                        g.DrawLine(highlightPen, x + 2, dividerY + 1, x + width - 2, dividerY + 1);
                    }

                    // C. Physical hinge notches on left and right borders where flaps rotate on axle
                    int notchW = Math.Max(3, (int)(width * 0.024f));
                    int notchH = Math.Max(5, (int)(height * 0.038f));
                    int notchY = dividerY - notchH / 2;

                    using (SolidBrush notchBrush = new SolidBrush(Color.FromArgb(12, 12, 14)))
                    {
                        // Left notch
                        g.FillRectangle(notchBrush, x, notchY, notchW, notchH);
                        // Right notch
                        g.FillRectangle(notchBrush, x + width - notchW, notchY, notchW, notchH);
                    }

                    // Reset clip
                    g.ResetClip();
                }
            }
            finally
            {
                g.Restore(state);
            }
        }

        private void UpdateCache(int width, int height, string amPmText)
        {
            bool sizeChanged = (width != cachedWidth || height != cachedHeight);
            bool amPmChanged = (amPmText != cachedAmPm);

            if (sizeChanged || amPmChanged)
            {
                DisposeBitmaps();
                cachedWidth = width;
                cachedHeight = height;
                cachedAmPm = amPmText;
                cachedCurrentVal = null;
                cachedTargetVal = null;
            }

            if (currentBmp == null || cachedCurrentVal != currentValue)
            {
                if (currentBmp != null) currentBmp.Dispose();
                currentBmp = !string.IsNullOrEmpty(currentValue) ? CreateCardBitmap(currentValue, width, height, amPmText) : null;
                cachedCurrentVal = currentValue;
            }

            if (targetBmp == null || cachedTargetVal != targetValue)
            {
                if (targetBmp != null) targetBmp.Dispose();
                targetBmp = !string.IsNullOrEmpty(targetValue) ? CreateCardBitmap(targetValue, width, height, amPmText) : null;
                cachedTargetVal = targetValue;
            }
        }

        private Bitmap CreateCardBitmap(string value, int width, int height, string amPmText)
        {
            Bitmap bmp = new Bitmap(width, height, PixelFormat.Format32bppPArgb);
            using (Graphics g = Graphics.FromImage(bmp))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;
                g.InterpolationMode = InterpolationMode.HighQualityBilinear;
                g.PixelOffsetMode = PixelOffsetMode.HighQuality;

                float radius = width * 0.065f;
                if (radius < 2.0f) radius = 2.0f;

                RectangleF cardRect = new RectangleF(0, 0, width, height);

                // 1. Draw card background with subtle gradient for depth
                using (GraphicsPath path = GetRoundedRectPath(cardRect, radius))
                {
                    using (LinearGradientBrush bgBrush = new LinearGradientBrush(
                        cardRect,
                        Color.FromArgb(34, 36, 40),
                        Color.FromArgb(24, 25, 28),
                        LinearGradientMode.Vertical))
                    {
                        g.FillPath(bgBrush, path);
                    }
                }

                // 2. Draw digits text with exact geometric centering
                if (!string.IsNullOrEmpty(value))
                {
                    using (FontFamily family = GetDigitFontFamily())
                    {
                        int fontStyle = (int)FontStyle.Bold;
                        float targetFontSize = height * 0.60f;

                        using (StringFormat sf = new StringFormat())
                        {
                            sf.FormatFlags = StringFormatFlags.NoWrap;

                            PointF origin = new PointF(0, 0);
                            RectangleF refBounds;
                            using (GraphicsPath refPath = new GraphicsPath())
                            {
                                refPath.AddString("8", family, fontStyle, targetFontSize, origin, sf);
                                refBounds = refPath.GetBounds();
                            }

                            using (GraphicsPath textPath = new GraphicsPath())
                            {
                                textPath.AddString(value, family, fontStyle, targetFontSize, origin, sf);
                                RectangleF textBounds = textPath.GetBounds();

                                // Ensure text comfortably fits within card width with margins
                                float maxAllowedWidth = width * 0.84f;
                                float scale = 1.0f;
                                if (textBounds.Width > maxAllowedWidth && textBounds.Width > 0)
                                {
                                    scale = maxAllowedWidth / textBounds.Width;
                                }

                                // Center vertically using the reference cap height so all digits align to identical center
                                float refCenterY = (refBounds.Top + refBounds.Bottom) / 2.0f;
                                float targetCenterY = height / 2.0f;
                                float dy = targetCenterY - (refCenterY * scale);

                                // Center horizontally using actual text width
                                float dx = (width - (textBounds.Width * scale)) / 2.0f - (textBounds.X * scale);

                                using (Matrix matrix = new Matrix())
                                {
                                    if (scale != 1.0f)
                                    {
                                        matrix.Scale(scale, scale);
                                    }
                                    matrix.Translate(dx / scale, dy / scale);
                                    textPath.Transform(matrix);
                                }

                                using (SolidBrush textBrush = new SolidBrush(Color.FromArgb(236, 239, 244)))
                                {
                                    g.FillPath(textBrush, textPath);
                                }
                            }
                        }
                    }
                }

                // 3. Draw AM/PM indicator in bottom-left corner of the card if present
                if (!string.IsNullOrEmpty(amPmText))
                {
                    float amPmSize = height * 0.088f;
                    using (Font amPmFont = new Font("Segoe UI", amPmSize, FontStyle.Bold, GraphicsUnit.Pixel))
                    {
                        using (SolidBrush amPmBrush = new SolidBrush(Color.FromArgb(145, 149, 158)))
                        {
                            float xPos = width * 0.085f;
                            float yPos = height * 0.81f;
                            g.DrawString(amPmText, amPmFont, amPmBrush, xPos, yPos);
                        }
                    }
                }
            }
            return bmp;
        }

        private static FontFamily GetDigitFontFamily()
        {
            string[] fontNames = new string[] { "Segoe UI", "Arial" };
            foreach (string name in fontNames)
            {
                try
                {
                    return new FontFamily(name);
                }
                catch
                {
                }
            }
            return new FontFamily(GenericFontFamilies.SansSerif);
        }

        private static GraphicsPath GetRoundedRectPath(RectangleF rect, float radius)
        {
            GraphicsPath path = new GraphicsPath();
            float diameter = radius * 2.0f;
            path.AddArc(rect.X, rect.Y, diameter, diameter, 180, 90);
            path.AddArc(rect.Right - diameter, rect.Y, diameter, diameter, 270, 90);
            path.AddArc(rect.Right - diameter, rect.Bottom - diameter, diameter, diameter, 0, 90);
            path.AddArc(rect.X, rect.Bottom - diameter, diameter, diameter, 90, 90);
            path.CloseFigure();
            return path;
        }

        private void DisposeBitmaps()
        {
            if (currentBmp != null)
            {
                currentBmp.Dispose();
                currentBmp = null;
            }
            if (targetBmp != null)
            {
                targetBmp.Dispose();
                targetBmp = null;
            }
        }

        public void Dispose()
        {
            DisposeBitmaps();
        }
    }
}
