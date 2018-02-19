#region Licence
/**
* Copyright (C) 2015 Open Tibia Tools <https://github.com/ottools/open-tibia>
*
* Permission is hereby granted, free of charge, to any person obtaining a copy
* of this software and associated documentation files (the "Software"), to deal
* in the Software without restriction, including without limitation the rights
* to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
* copies of the Software, and to permit persons to whom the Software is
* furnished to do so, subject to the following conditions:
*
* The above copyright notice and this permission notice shall be included in all
* copies or substantial portions of the Software.
*
* THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
* IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
* FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
* AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
* LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
* OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
* SOFTWARE.
*/
#endregion

#region Using Statements
using System;
using System.Drawing;
#endregion

namespace OpenTibia.Utils
{
    public static class ColorUtils
    {
        public static int RgbToEightBit(Color color)
        {
            int c = 0;
            c += (color.R / 51) * 36;
            c += (color.G / 51) * 6;
            c += (color.B / 51);
            return c;
        }

        public static Color EightBitToRgb(int color)
        {
            if (color <= 0 || color >= 216)
            {
                return Color.FromArgb(0, 0, 0);
            }

            int r = (int)(color / 36) % 6 * 51;
            int g = (int)(color / 6) % 6 * 51;
            int b = color % 6 * 51;
            return Color.FromArgb(r, g, b);
        }

        public static Color HsiToRgb(int hsi)
        {
            const int values = 7;
            const int steps = 19;
            double hue = 0;
            double saturation = 0;
            double intensity = 0;
            double red = 0;
            double green = 0;
            double blue = 0;

            if (hsi >= steps * values)
            {
                hsi = 0;
            }
            
            if (hsi % steps == 0)
            {
                hue = 0;
                saturation = 0;
                intensity = 1 - (double)hsi / steps / values;
            }
            else
            {
                hue = hsi % steps * (1.0d / 18.0d);
                saturation = 1.0d;
                intensity = 1.0d;
                
                switch ((int)(hsi / steps))
                {
                    case 0:
                        saturation = 0.25d;
                        intensity = 1.0d;
                        break;

                    case 1:
                        saturation = 0.25d;
                        intensity = 0.75d;
                        break;

                    case 2:
                        saturation = 0.5d;
                        intensity = 0.75d;
                        break;

                    case 3:
                        saturation = 0.667d;
                        intensity = 0.75d;
                        break;

                    case 4:
                        saturation = 1.0d;
                        intensity = 1.0d;
                        break;

                    case 5:
                        saturation = 1.0d;
                        intensity = 0.75d;
                        break;

                    case 6:
                        saturation = 1.0d;
                        intensity = 0.5d;
                        break;
                }
            }
            
            if (intensity == 0)
            {
                return Color.Black;
            }
            
            if (saturation == 0)
            {
                byte value = (byte)(intensity * 255);
                return Color.FromArgb(value, value, value);
            }

            if (hue < (1.0d / 6.0d))
            {
                red = intensity;
                blue = intensity * (1 - saturation);
                green = blue + (intensity - blue) * 6 * hue;
            }
            else if (hue < (2.0d / 6.0d))
            {
                green = intensity;
                blue = intensity * (1 - saturation);
                red = green - (intensity - blue) * (6 * hue - 1);
            }
            else if (hue < (3.0d / 6.0d))
            {
                green = intensity;
                red = intensity * (1 - saturation);
                blue = red + (intensity - red) * (6 * hue - 2);
            }
            else if (hue < (4.0d / 6.0d))
            {
                blue = intensity;
                red = intensity * (1 - saturation);
                green = blue - (intensity - red) * (6 * hue - 3);
            }
            else if (hue < (5.0d / 6.0d))
            {
                blue = intensity;
                green = intensity * (1 - saturation);
                red = green + (intensity - green) * (6 * hue - 4);
            }
            else
            {
                red = intensity;
                green = intensity * (1 - saturation);
                blue = red - (intensity - green) * (6 * hue - 5);
            }

            return Color.FromArgb((byte)(red * 0xFF), (byte)(green * 0xFF), (byte)(blue * 0xFF));
        }
    }
}
