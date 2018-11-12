using System;
using System.Drawing;

namespace PictureHelper
{
    public class FileInfo
    {
        public string Filename { get; set; }

        public DateTime DateTaken { get; set; }

        public Image Image { get; set; }

        public bool DateTakenValid { get; set; }

        public string ImageKey { get; set; }
    }
}