using System;
using System.Collections;
using System.Drawing;
using System.Drawing.Imaging;
using System.Numerics;
using System.Xml;
using Underanalyzer.Decompiler;
using UndertaleModLib;
using UndertaleModLib.Compiler;
using UndertaleModLib.Decompiler;
using UndertaleModLib.Models;
using UndertaleModLib.Util;
using xdelta3.net;

namespace Watercooler
{
    public class Compiler
    {
        readonly string _rootPath, _gamePath, _outPath;
        readonly UndertaleData _gameData;

        public Compiler(string[] rootPath, string gamePath, string outPath)
        {
            _rootPath = rootPath.First();
            _gamePath = gamePath;
            _outPath = outPath;

            string outExt = Path.GetExtension(outPath);
            bool isXdelta = string.Equals(".xdelta", outExt, StringComparison.OrdinalIgnoreCase);

            if (!File.Exists(_gamePath)) throw new Exception($"Invalid game path: {_gamePath}");
            if (!string.Equals(".win", outExt, StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(".ios", outExt, StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(".unx", outExt, StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(".droid", outExt, StringComparison.OrdinalIgnoreCase) &&
                !isXdelta)
                Console.WriteLine($"WARN: Unsupported output file extension. Watercooler will save the output using the GameMaker data file format.");

            Console.WriteLine("Reading game data...");
            FileStream fs = File.OpenRead(_gamePath);
            _gameData = UndertaleIO.Read(fs);

            MemoryStream? fuckass = isXdelta ? new() : null;
            fs.Seek(0, SeekOrigin.Begin);
            if (fuckass != null) fs.CopyTo(fuckass);

            fs.Close();
            Console.WriteLine("Read successful!");
            Console.WriteLine("-----------------------------------------------");

            Console.WriteLine("Reading patch data...");

            foreach (string project in rootPath)
            {
                _rootPath = project;
                if (!Directory.Exists(_rootPath)) throw new Exception($"Invalid Watercooler project path: {_rootPath}");

                Console.WriteLine($"----------------------------- STARTING READ OF PROJECT '{project}'");
                LoadSpritePatches();
                LoadObjectPatches();
                LoadCodePatches();
                Console.WriteLine("----------------------------- PROJECT APPLIED SUCCESSFULLY");
            }

            if (isXdelta && fuckass != null)
            {
                MemoryStream ms = new();
                UndertaleIO.Write(ms, _gameData);

                //Console.WriteLine($"MS LENGTH: {ms.Length}, FUCKASS LENGTH: {fuckass.Length}");

                byte[] patchBytes = Xdelta3Lib.Encode(fuckass.ToArray(), ms.ToArray()).ToArray();
                File.WriteAllBytes(_outPath, patchBytes);
            }
            else
            {
                FileStream fstemp = File.OpenWrite(_outPath);
                UndertaleIO.Write(fstemp, _gameData);
                fstemp.Close();
            }

            Console.WriteLine("\nPatches applied.");
            _gameData.Dispose();
        }

        public void LoadSpritePatches()
        {
            Console.WriteLine("\nSPRITE PATCHES ---");

            List<Classes.Sprite> finishedSprites = AssetProcessor.ScanAssetsInDirectory<Classes.Sprite>(Path.Combine(_rootPath, "sprites"), _gameData);
            int atlasSize = 2048;

            if (finishedSprites.Count < 1)
            {
                Console.WriteLine("No sprites found.");
                return;
            }

            Console.WriteLine($"{Environment.NewLine}Building texture atlases for {finishedSprites.Count} processed sprite(s).");

            Classes.Packer spritePacker = new(finishedSprites);
            List<Classes.Packer.Atlas> atlases = spritePacker.Package(atlasSize);

            Console.WriteLine($"Built {atlases.Count} atlas(es). Starting import...");

            int lastTextureEntry = _gameData.EmbeddedTextures.Count - 1;
            int lastTextureItemEntry = _gameData.TexturePageItems.Count - 1;

            foreach (Classes.Packer.Atlas atlas in atlases)
            {
                UndertaleEmbeddedTexture uet = new()
                {
                    Name = _gameData.Strings.MakeString($"Texture {++lastTextureEntry}")
                };

                Bitmap atl = atlas.CreateImage();

                using MemoryStream atlasStream = new();
                atl.Save(atlasStream, ImageFormat.Png);

                uet.TextureData.Image = GMImage.FromPng(atlasStream.ToArray());
                foreach (Classes.Packer.Atlas.SpriteContainer spr in atlas.Sprites)
                {
                    UndertaleTexturePageItem tpi = new()
                    {
                        Name = _gameData.Strings.MakeString($"PageItem {++lastTextureItemEntry}"),
                        SourceX = (ushort)spr.Location.X,
                        SourceY = (ushort)spr.Location.Y,
                        SourceWidth = (ushort)spr.Size.X,
                        SourceHeight = (ushort)spr.Size.Y,
                        TargetX = 0,
                        TargetY = 0,
                        TargetWidth = (ushort)spr.Size.X,
                        TargetHeight = (ushort)spr.Size.Y,
                        BoundingWidth = (ushort)spr.Size.X,
                        BoundingHeight = (ushort)spr.Size.Y,
                        TexturePage = uet,
                    };

                    _gameData.TexturePageItems.Add(tpi);

                    switch (spr.Data.Type)
                    {
                        default:
                        case "sprite":
                            {
                                UndertaleSprite? spriteEnt = _gameData.Sprites.ByName(spr.Data.Name);
                                UndertaleSprite.TextureEntry texEnt = new()
                                {
                                    Texture = tpi
                                };

                                if (spriteEnt == null)
                                {
                                    spriteEnt = new()
                                    {
                                        Name = _gameData.Strings.MakeString(spr.Data.Name),
                                        Width = (ushort)spr.Size.X,
                                        Height = (ushort)spr.Size.Y,
                                        MarginLeft = (ushort)spr.Data.Margin.X,
                                        MarginRight = (ushort)spr.Data.Margin.Y,
                                        MarginTop = (ushort)spr.Data.Margin.Z,
                                        MarginBottom = (ushort)spr.Data.Margin.W,
                                        OriginX = (ushort)spr.Data.Origin.X,
                                        OriginY = (ushort)spr.Data.Origin.Y,
                                    };

                                    UndertaleSprite.MaskEntry mask = spriteEnt.NewMaskEntry();

                                    Bitmap cloneBitmap = atl.Clone(new Rectangle((int)spr.Location.X, (int)spr.Location.Y, (int)spr.Size.X, (int)spr.Size.Y), atl.PixelFormat);
                                    int w = ((int)spr.Size.X + 7) / 8 * 8; // int shenanigans?? idk, im copying the utmt script
                                    int h = (int)spr.Size.Y;

                                    BitArray maskingArr = new(w * h);
                                    for (int y = 0; y < h; y++)
                                        for (int x = 0; x < spr.Size.X; x++)
                                            maskingArr[y * w + x] = cloneBitmap.GetPixel(x, y).A > 0;

                                    BitArray tempBitArray = new(w * h);
                                    for (int i = 0; i < maskingArr.Length; i += 8)
                                        for (int j = 0; j < 8; j++)
                                            tempBitArray[j + i] = maskingArr[-(j - 7) + i];

                                    int numBytes;
                                    numBytes = maskingArr.Length / 8;

                                    byte[] bytes = new byte[numBytes];
                                    tempBitArray.CopyTo(bytes, 0);

                                    for (int i = 0; i < bytes.Length; i++)
                                        mask.Data[i] = bytes[i];

                                    spriteEnt.CollisionMasks.Add(mask);
                                    spriteEnt.Textures.Add(texEnt);
                                    _gameData.Sprites.Add(spriteEnt);
                                    break;
                                }

                                spriteEnt.Width = (ushort)spr.Size.X;
                                spriteEnt.Height = (ushort)spr.Size.Y;
                                spriteEnt.MarginLeft = (ushort)spr.Data.Margin.X;
                                spriteEnt.MarginRight = (ushort)spr.Data.Margin.Y;
                                spriteEnt.MarginTop = (ushort)spr.Data.Margin.Z;
                                spriteEnt.MarginBottom = (ushort)spr.Data.Margin.W;
                                spriteEnt.OriginX = (ushort)spr.Data.Origin.X;
                                spriteEnt.OriginY = (ushort)spr.Data.Origin.Y;

                                spriteEnt.Textures.Clear();
                                spriteEnt.Textures.Add(texEnt);
                                break;
                            }

                        case "background":
                            {
                                UndertaleBackground? bg = _gameData.Backgrounds.ByName(spr.Data.Name);
                                if (bg != null)
                                {
                                    bg.Texture = tpi;
                                    break;
                                }

                                bg = new()
                                {
                                    Name = _gameData.Strings.MakeString(spr.Data.Name),
                                    Transparent = false,
                                    Preload = false,
                                    Texture = tpi
                                };
                                _gameData.Backgrounds.Add(bg);
                                break;
                            }
                    }
                }

                _gameData.EmbeddedTextures.Add(uet);
            }

            Console.WriteLine("Sprite patches imported.");
        }

        public void LoadObjectPatches()
        {
            Console.WriteLine("\nOBJECT PATCHES ---");
            List<Classes.Object> finishedObjects = AssetProcessor.ScanAssetsInDirectory<Classes.Object>(Path.Combine(_rootPath, "objects"), _gameData);

            if (finishedObjects.Count < 1)
            {
                Console.WriteLine("No objects found.");
                return;
            }

            GlobalDecompileContext gdc = new(_gameData);
            IDecompileSettings ds = _gameData.ToolInfo.DecompilerSettings;
            CodeImportGroup importGroup = new(_gameData, gdc, ds);
            // I fucking       LOVE AUTOCREATEASSETS!!!!
            importGroup.AutoCreateAssets = true;

            foreach (Classes.Object obj in finishedObjects)
            {
                Console.WriteLine($"Importing object asset {obj.Name}");
                UndertaleGameObject importedObject = new();

                importedObject.Name = _gameData.Strings.MakeString(obj.Name);
                importedObject.Persistent = obj.Persistent;
                importedObject.Solid = obj.Solid;
                importedObject.ParentId = obj.Parent;
                importedObject.Sprite = obj.Sprite;
                importedObject.Visible = obj.Visible;
                importedObject.CollisionShape = obj.Collision;
                importedObject.TextureMaskId = obj.TextureMask;

                _gameData.GameObjects.Add(importedObject);

                // !! This can fully override existing object code -- hopefully that is not an issue...
                foreach (Classes.Object.EventAction action in obj.eventActions)
                    importGroup.QueueReplace(action.ImportName, File.ReadAllText(action.CodePath));
            }

            Console.WriteLine("\nObject asset importing completed, now importing object code...");
            importGroup.Import();
            Console.WriteLine("Object code imported.");
        }

        public void LoadCodePatches()
        {
            Console.WriteLine("\nCODE PATCHES ---");
            List<string> fi = Directory
                .GetFiles(Path.Combine(_rootPath, "scripts"), "*.gml", SearchOption.AllDirectories)
                .Select(x => Path.GetFileName(x))
                .ToList();

            GlobalDecompileContext gdc = new(_gameData);
            IDecompileSettings ds = _gameData.ToolInfo.DecompilerSettings;
            CodeImportGroup importGroup = new(_gameData, gdc, ds);

            foreach (string f in fi)
            {
                string[] codeLines = File.ReadAllLines(Path.Combine(_rootPath, "scripts", f));
                string code = string.Join(Environment.NewLine, codeLines);

                bool isReadingContinuously = false;
                string readingScript = "";
                List<string> readLines = new();

                Console.WriteLine($"Applying scripts/{f}");
                for (int i = 0; i < codeLines.Length; i++)
                {
                    string ln = codeLines[i];
                    string nextLn = i < codeLines.Length - 1 ? codeLines[i + 1] : "";

                    if (!ln.StartsWith("//WATERCOOLER// "))
                    {
                        if (isReadingContinuously) readLines.Add(ln);
                        continue;
                    }

                    bool continueAnalysis = true;
                    List<string> wcCommand = ln.Substring("//WATERCOOLER// ".Length).Split(" ").ToList();

                    string commandType = wcCommand[0].ToUpper();
                    Console.WriteLine($"-- WATERCOOLER Command: {commandType}");

                    switch (commandType)
                    {
                        case "REPLACE":
                            {
                                if (wcCommand[1].ToUpper() == "ALL")
                                {
                                    importGroup.QueueReplace(wcCommand[2], code);
                                    continueAnalysis = false;
                                    Console.WriteLine($"-- -- Replacing '{wcCommand[2]}' with ALL of '{f}'");
                                }

                                if (wcCommand[1].ToUpper() == "START")
                                {
                                    isReadingContinuously = true;
                                    readingScript = wcCommand[2];
                                }

                                if (wcCommand[1].ToUpper() == "END")
                                {
                                    isReadingContinuously = false;
                                    importGroup.QueueReplace(readingScript, string.Join(Environment.NewLine, readLines));
                                    Console.WriteLine($"-- -- Replacing '{readingScript}' with a {readLines.Count}-line SECTION of '{f}'");
                                    readLines.Clear();
                                }
                                break;
                            }

                        case "CREATE":
                            {
                                if (wcCommand[1].ToUpper() == "SCRIPT")
                                {
                                    if (_gameData.Scripts.ByName(wcCommand[2], true) != null)
                                    {
                                        importGroup.QueueReplace(wcCommand[2], code);
                                        Console.WriteLine($"-- -- Replacing '{wcCommand[2]}' with ALL of '{f}'");
                                        break;
                                    }

                                    Console.WriteLine($"-- -- Creating script '{wcCommand[2]}' using ALL of '{f}'");

                                    UndertaleScript scr = new();
                                    scr.Name = _gameData.Strings.MakeString(wcCommand[2]);
                                    scr.Code = UndertaleCode.CreateEmptyEntry(_gameData, $"gml_GlobalScript_{scr.Name.Content}");
                                    _gameData.GlobalInitScripts.Add(new UndertaleGlobalInit() { Code = scr.Code });
                                    importGroup.QueueReplace(scr.Code.Name.Content, code);
                                }

                                if (wcCommand[1].ToUpper() == "EVENT")
                                {
                                    string objName = wcCommand[3];
                                    string eventName = wcCommand[4];

                                    if (wcCommand[2].ToUpper() == "FROM-FUNCTION")
                                    {
                                        List<string> lines = ReadUntilFunctionEnds(codeLines, i + 1);
                                        (EventType evt, uint subtype) = GetEventTypeFromString(eventName);

                                        Console.WriteLine($"-- -- Replacing event handler for '{objName}' from {lines.Count}-line FUNCTION of '{f}'");
                                        importGroup.QueueReplace(_gameData.GameObjects.ByName(objName).EventHandlerFor(evt, subtype, _gameData), string.Join(Environment.NewLine, lines));
                                    }
                                }
                                break;
                            }
                    }

                    if (!continueAnalysis) break;
                }
            }

            Console.WriteLine("\nAnalysis of script files completed. Starting import...");
            importGroup.Import();
            Console.WriteLine("Code patches imported.");
        }

        public (EventType eventType, uint eventSubtype) GetEventTypeFromString(string eventName)
        {
            EventType t = Enum.Parse<EventType>(eventName);
            uint sub = 0u;
            if (!Enum.IsDefined(t))
            {
                string ev = eventName.ToLower();
                Console.WriteLine("-- TODO: " + ev);

                t = EventType.Create;
            }

            return (t, sub);
        }

        public List<string> ReadUntilFunctionEnds(string[] data, int lineOffset)
        {
            List<string> res = new();
            int startingTags = data[lineOffset].Where(x => x == '{').Count();
            int endingTags = data[lineOffset].Where(x => x == '}').Count();

            if (!data[lineOffset].StartsWith("function"))
            {
                Console.WriteLine("-- ERROR: Cannot use 'FROM-FUNCTION' read type if next line doesn't start with 'function'");
                return new();
            }

            if (startingTags == endingTags)
            {
                Console.WriteLine("-- WARN: 'FROM-FUNCTION' read type detected one-line function data.");
                return new();
            }

            for (int i = lineOffset + 1; i < data.Length; i++)
            {
                startingTags += data[i].Where(x => x == '{').Count();
                endingTags += data[i].Where(x => x == '}').Count();

                if (startingTags == endingTags) break; // function ended

                //Console.WriteLine(data[i]);
                res.Add(data[i]);
            }
            return res;
        }
    }
}
