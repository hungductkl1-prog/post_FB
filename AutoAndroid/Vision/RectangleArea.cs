using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace AutoAndroid
{
    public class RectangleArea
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
        public Random _random;

        public RectangleArea(string boundsString)
        {
            string[] array = boundsString.Split('[', ',', ']');
            Left = Convert.ToInt32(array[1]);
            Top = Convert.ToInt32(array[2]);
            Right = Convert.ToInt32(array[4]);
           
            Bottom = Convert.ToInt32(array[5]);
            _random = new Random();
        }

        public RectangleArea(int left, int top, int right, int bottom)
        {
            Left = left;
            Top = top;
            Right = right;
            Bottom = bottom;
            _random = new Random();
        }

        public string ToBoundsString()
        {
            return $"[{Left},{Top}][{Right},{Bottom}]";
        }

        public Point GetCenterPoint()
        {
            try
            {
                int x = (Left + Right) / 2;
                int y = (Top + Bottom) / 2;
                return new Point(x, y);
            }
            catch (Exception)
            {
            }
            return default(Point);
        }

        internal int Height()
        {
            return Bottom - Top;
        }

        internal int Width()
        {
            return Right - Left;
        }

        public Point RandomPoint()
        {
            try
            {
                int x = _random.Next(Left, Right + 1);
                int y = _random.Next(Top, Bottom + 1);
                return new Point(x, y);
            }
            catch (Exception)
            {
            }
            return default(Point);
        }

        public bool Intersects(string boundsString)
        {
            return Intersection(ToBoundsString(), boundsString) != null;
        }

        public RectangleArea GetBelow(string boundsString)
        {
            return GetBelowRectangle(ToBoundsString(), boundsString);
        }

        public static RectangleArea FindOverlap(string boundsString, List<string> boundsList)
        {
            RectangleArea result = null;
            try
            {
                RectangleArea target = new RectangleArea(boundsString);
                for (int i = 0; i < boundsList.Count; i++)
                {
                    RectangleArea rect = new RectangleArea(boundsList[i]);
                    int top = Math.Max(target.Top, rect.Top);
                    int bottom = Math.Min(target.Bottom, rect.Bottom);
                    if (top < bottom)
                    {
                        result = new RectangleArea(rect.Left, top, rect.Right, bottom);
                        break;
                    }
                }
            }
            catch
            {
            }
            return result;
        }

        public static RectangleArea Intersection(string boundsA, string boundsB)
        {
            RectangleArea result = null;
            try
            {
                RectangleArea a = new RectangleArea(boundsA);
                RectangleArea b = new RectangleArea(boundsB);
                int left = Math.Max(a.Left, b.Left);
                int top = Math.Max(a.Top, b.Top);
                int right = Math.Min(a.Right, b.Right);
                int bottom = Math.Min(a.Bottom, b.Bottom);
                if (top < bottom)
                {
                    result = new RectangleArea(left, top, right, bottom);
                }
            }
            catch
            {
            }
            return result;
        }

        public static RectangleArea GetBelowRectangle(string boundsA, string boundsB)
        {
            RectangleArea result = null;
            try
            {
                RectangleArea a = new RectangleArea(boundsA);
                RectangleArea b = new RectangleArea(boundsB);
                if (a.Bottom < b.Bottom)
                {
                    result = new RectangleArea(b.Left, a.Bottom + 1, b.Right, b.Bottom);
                }
            }
            catch
            {
            }
            return result;
        }

        public static List<RectangleArea> GroupByTop(string xmlSource, string xpath)
        {//smethod_2
            Dictionary<int, List<RectangleArea>> dict = new Dictionary<int, List<RectangleArea>>();
            List<string> boundsList = GetBoundsFromXml(xmlSource, xpath);
            foreach (string item in boundsList)
            {
                RectangleArea rect = new RectangleArea(item);
                if (!dict.ContainsKey(rect.Top))
                {
                    dict.Add(rect.Top, new List<RectangleArea>());
                }
                dict[rect.Top].Add(rect);
            }
            if (dict.Count > 0)
            {
                return dict.OrderByDescending(kvp => kvp.Value.Count).First().Value;
            }
            return new List<RectangleArea>();
        }

        public static List<string> GetBoundsFromXml(string xmlSource, string xpath, string attribute = "bounds")
        {
            List<string> list = new List<string>();
            try
            {
                xmlSource = xmlSource.ToLower();
                xpath = xpath.ToLower();
                XmlDocument doc = new XmlDocument();
                doc.LoadXml(xmlSource);
                XmlNodeList nodes = doc.SelectNodes(xpath);
                for (int i = 0; i < nodes.Count; i++)
                {
                    list.Add(nodes[i].Attributes[attribute].Value);
                }
            }
            catch (Exception)
            {
            }
            return list;
        }
    }
}
