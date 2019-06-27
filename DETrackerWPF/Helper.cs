using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace DETrackerWPF
{
  public class Helper
  {

    public BitmapImage Convert(Bitmap src)
    {
      MemoryStream ms = new MemoryStream();
      ((Bitmap)src).Save(ms, ImageFormat.Png);
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
  }
}
