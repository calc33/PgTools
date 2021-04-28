using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Db2Source
{
    public struct RGB: IComparable
    {
        public byte R { get; set; }
        public byte G { get; set; }
        public byte B { get; set; }

        public float ScR
        {
            get
            {
                return R / 255f;
            }
            set
            {
                if (value < 0 || 1 < value)
                {
                    throw new ArgumentOutOfRangeException("ScR");
                }
                R = (byte)(value * 255f + 0.5f);
            }
        }
        public float ScG
        {
            get
            {
                return G / 255f;
            }
            set
            {
                if (value < 0 || 1 < value)
                {
                    throw new ArgumentOutOfRangeException("ScG");
                }
                G = (byte)(value * 255f + 0.5f);
            }
        }
        public float ScB
        {
            get
            {
                return B / 255f;
            }
            set
            {
                if (value < 0 || 1 < value)
                {
                    throw new ArgumentOutOfRangeException("ScB");
                }
                B = (byte)(value * 255f + 0.5f);
            }
        }
        public uint ColorCode
        {
            get
            {
                return ((uint)R) * 65536 + ((uint)G) * 256 + ((uint)B);
            }
            set
            {
                R = (byte)((value / 65536) & 0xff);
                G = (byte)((value / 256) & 0xff);
                B = (byte)(value & 0xff);
            }
        }
        public RGB(byte red, byte green, byte blue)
        {
            R = red;
            G = green;
            B = blue;
        }
        public RGB(float red, float green, float blue)
        {
            if (red < 0 || 1 < red)
            {
                throw new ArgumentOutOfRangeException("red");
            }
            if (green < 0 || 1 < green)
            {
                throw new ArgumentOutOfRangeException("green");
            }
            if (blue < 0 || 1 < blue)
            {
                throw new ArgumentOutOfRangeException("blue");
            }
            R = (byte)(red * 255f + 0.5f);
            G = (byte)(green * 255f + 0.5f);
            B = (byte)(blue * 255f + 0.5f);
        }
        public RGB(uint code)
        {
            R = (byte)((code / 65536) & 0xff);
            G = (byte)((code / 256) & 0xff);
            B = (byte)(code & 0xff);
        }

        public override bool Equals(object obj)
        {
            if (!(obj is RGB))
            {
                return false;
            }
            RGB o = (RGB)obj;
            return R == o.R && G == o.G && B == o.B;
        }
        public override int GetHashCode()
        {
            return ColorCode.GetHashCode();
        }
        public override string ToString()
        {
            return string.Format("#{0:X6}", ColorCode);
        }

        public int CompareTo(object obj)
        {
            if (!(obj is RGB))
            {
                return -1;
            }
            RGB o = (RGB)obj;
            return ColorCode.CompareTo(o.ColorCode);
        }
    }

    public struct HSV
    {
        public float H { get; set; }
        public float S { get; set; }
        public float V { get; set; }
        public HSV(RGB color)
        {
            float r = color.ScR;
            float g = color.ScG;
            float b = color.ScB;

            float max = Math.Max(r, Math.Max(g, b));
            float min = Math.Min(r, Math.Min(g, b));

            V = max;
            if (max == min)
            {
                //undefined
                H = 0f;
                S = 0f;
            }
            else
            {
                float c = max - min;

                if (max == r)
                {
                    H = (g - b) / c;
                }
                else if (max == g)
                {
                    H = (b - r) / c + 2f;
                }
                else
                {
                    H = (r - g) / c + 4f;
                }
                H *= 60f;
                if (H < 0f)
                {
                    H += 360f;
                }
                S = c / max;
            }
        }
        public HSV(float h, float s, float v)
        {
            H = h;
            while (H < 0)
            {
                H += 360f;
            }
            while (360 <= H)
            {
                H -= 360f;
            }
            S = s;
            V = v;
        }
        public RGB ToRGB()
        {
            float r, g, b;
            if (S == 0)
            {
                return new RGB(V, V, V);
            }
            float h = H / 60f;
            int i = (int)Math.Floor(h);
            float f = h - i;
            float p = V * (1f - S);
            float q;
            if (i % 2 == 0)
            {
                //t
                q = V * (1f - (1f - f) * S);
            }
            else
            {
                q = V * (1f - f * S);
            }

            switch (i)
            {
                case 0:
                    r = V;
                    g = q;
                    b = p;
                    break;
                case 1:
                    r = q;
                    g = V;
                    b = p;
                    break;
                case 2:
                    r = p;
                    g = V;
                    b = q;
                    break;
                case 3:
                    r = p;
                    g = q;
                    b = V;
                    break;
                case 4:
                    r = q;
                    g = p;
                    b = V;
                    break;
                case 5:
                    r = V;
                    g = p;
                    b = q;
                    break;
                default:
                    throw new ArgumentException(
                        "色相の値が不正です。", "hsv");
            }
            return new RGB(r, g, b);
        }
    }
    public static class ColorConverter
    {
        public static RGB FromHSV(float h, float s, float v)
        {
            return new HSV(h, s, v).ToRGB();
        }
    }
}
