using System;
using System.Drawing;

namespace Photo_Editor.DataModels
{
    public class BitmapSlice
    {
        public Bitmap Bitmap { get; set; }

        public int SliceXStartInOriginal { get; set; }

        public int SliceWidth { get; set; }

        public int OffsetLeft { get; set; }

        public int OffsetRight { get; set; }
    }
}
