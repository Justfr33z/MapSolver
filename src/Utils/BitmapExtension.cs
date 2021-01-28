using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MapSolver.Utils {
    public static class BitmapExtension {
        public static string ToBase64(this Bitmap bitmap) {
            using (var memoryStream = new MemoryStream()) {
                bitmap.Save(memoryStream, ImageFormat.Png);
                return Convert.ToBase64String(memoryStream.ToArray());
            }
        }
    }
}
