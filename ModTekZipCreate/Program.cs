using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO.Compression;
using System.IO;

namespace ModTekZipCreate
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length != 2) { return; }
            if(Directory.Exists(args[0]) == false) { return; }
            if (File.Exists(args[1])) { File.Delete(args[1]); };
            ZipFile.CreateFromDirectory(args[0], args[1]);
        }
    }
}
