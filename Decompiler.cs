using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UndertaleModLib;
using xdelta3.net;

namespace Watercooler
{
    public class Decompiler
    {
        readonly string _gamePath, _patchPath, _outPath;
        readonly UndertaleData _originalGameData, _modifiedGameData;

        public Decompiler(string gamePath, string patchPath, string outPath)
        {
            _gamePath = gamePath;
            _patchPath = patchPath;
            _outPath = outPath;

            Console.WriteLine("Reading game data...");
            FileStream fs = File.OpenRead(_gamePath);
            _originalGameData = UndertaleIO.Read(fs);

            byte[] patchBytes = File.ReadAllBytes(_patchPath);

            MemoryStream modStream = new();
            fs.Seek(0, SeekOrigin.Begin);
            fs.CopyTo(modStream);
            fs.Close();

            Console.WriteLine("Reading patched game data...");

            byte[] patchedDataBytes = Xdelta3Lib.Decode(modStream.ToArray(), patchBytes).ToArray();
            modStream.Close();

            MemoryStream patchedDataStream = new(patchedDataBytes);
            _modifiedGameData = UndertaleIO.Read(patchedDataStream);

            patchedDataStream.Close();

            Console.WriteLine("Read successful!");
            Console.WriteLine("-----------------------------------------------");
        }

        public void Analyze()
        {
            Console.WriteLine($"Comparing {Math.Max(_originalGameData.Code.Count, _modifiedGameData.Code.Count)} code entry hashes... (This may take a while)");
            foreach (var moddedCode in _modifiedGameData.Code)
            {
                Console.WriteLine(moddedCode.Name);
            }

        }
    }
}
