using System;
using System.Collections.Generic;
using System.Text;

namespace Common.HelperModels
{
    public class FileUpload
    {
        public byte[] Data { get; set; }
        public long Size { get; set; }
        public FileUpload()
        {

        }
    }
}
