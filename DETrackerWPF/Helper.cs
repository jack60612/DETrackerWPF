using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Caliburn.Micro;

namespace DETrackerWPF
{
    public class Helper
    {

    // Faction Setup

    private static string _factionName = "Dark Echo";
    private static int _factionEDDBID = 11217;

    public static string FactionName
    {
      get { return _factionName; }
      set { _factionName = value; }
    }

    public static int FactionEDDBID
    {
      get { return _factionEDDBID; }
      set { _factionEDDBID = value; }
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="src"></param>
    /// <returns></returns>
    public BitmapImage Convert(Bitmap src)
        {
            MemoryStream ms = new MemoryStream();
            ((Bitmap) src).Save(ms, ImageFormat.Png);
            BitmapImage image = new BitmapImage();
            image.BeginInit();
            ms.Seek(0, SeekOrigin.Begin);
            image.StreamSource = ms;
            image.EndInit();
            return image;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="CurrentInfluence"></param>
        /// <param name="Population"></param>
        /// <returns></returns>
        public static double MaxInfluence(double CurrentInfluence, Int64 Population)
        {
          var p1 = (CurrentInfluence + (42.8 - Math.Log(Population, 2)));
          var p2 = (100 + (42.8 - Math.Log(Population, 2)));

          return (p1 / p2) * 100;
        }



    /// <summary>
    /// 
    /// </summary>
    /// <param name="str"></param>
    /// <returns></returns>
    public static string Clean(string str)
        {
            var start = str.IndexOf("_", StringComparison.Ordinal) + 1;
            var workStr = str.Substring(start, (str.Length - start)).TrimEnd(';');
            if (workStr.Contains("_"))
            {
                start = workStr.IndexOf("_", StringComparison.Ordinal) + 1;
                return workStr.Substring(start, (workStr.Length - start));
            }
            else
            {
                return workStr;
            }
        }

        public static void TransformToPixels(double unitX, double unitY, 
            out int pixelX,
            out int pixelY)
        {
            using (Graphics g = Graphics.FromHwnd(IntPtr.Zero))
            {
                pixelX = (int)((g.DpiX / 96) * unitX);
                pixelY = (int)((g.DpiY / 96) * unitY);
            }

        }


    }
}
