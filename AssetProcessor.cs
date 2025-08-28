using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using UndertaleModLib;
using UndertaleModLib.Models;
using Watercooler.Classes;
using Object = Watercooler.Classes.Object;

namespace Watercooler
{
    public static class AssetProcessor
    {
        public static List<T> ScanAssetsInDirectory<T>(string _rootPath, UndertaleData _gameData) where T : Asset
        {
            List<string> fi = Directory
                .GetDirectories(_rootPath, "*", SearchOption.AllDirectories)
                .Where(x => File.Exists(Path.Combine(x, "asset.xml")))
                .ToList();

            List<T> assets = new();

            foreach (string asset in fi)
            {
                string rel = $"{Path.GetRelativePath(_rootPath, asset)}\\asset.xml";
                try
                {
                    XmlDocument config = new();
                    config.Load(Path.Combine(asset, "asset.xml"));

                    XmlNodeList? roots = config.DocumentElement?.ChildNodes;
                    if (roots == null || roots.Count < 1)
                    {
                        Console.WriteLine($"-- WARN: Couldn't find valid 'assets' node in '{rel}'. Please make sure it is correctly formatted.");
                        continue;
                    }

                    int i = 0;
                    Console.WriteLine($"Processing {roots.Count} asset(s) from '{rel}'.");
                    foreach (XmlNode root in roots)
                    {
                        i++;

                        Asset? entryAsset;
                        switch (root.Name.ToLower())
                        {
                            case "sprite" or "background":
                                {
                                    entryAsset = new Sprite()
                                    {
                                        Type = root.Name.ToLower()
                                    };
                                    break;
                                }

                            case "object":
                                {
                                    entryAsset = new Object();
                                    break;
                                }

                            default:
                                {
                                    Console.WriteLine($"-- ERROR: Invalid asset type provided. Failed to process asset #{i} in {rel}.");
                                    continue;
                                }
                        }

                        if (entryAsset == null) continue; // shouldn't be able to happen

                        foreach (XmlNode node in root.ChildNodes)
                        {
                            //Console.WriteLine($"DEBUG Node: {node.Name}");
                            switch (node.Name.ToLower())
                            {
                                case "name":
                                    {
                                        entryAsset.Name = node.InnerText.Trim();
                                        break;
                                    }

                                // Sprite assets
                                case "size" when entryAsset is Sprite spriteAsset:
                                    {
                                        XmlAttributeCollection? attrs = node.Attributes;

                                        XmlNode? width = attrs?.GetNamedItem("width");
                                        XmlNode? height = attrs?.GetNamedItem("height");

                                        if (attrs == null || width == null || height == null)
                                        {
                                            Console.WriteLine($"-- WARN: Node '{node.Name}' of asset #{i} in '{rel}' does not have required attributes ([width, height]).");
                                            break;
                                        }

                                        if (int.TryParse(width.Value, out int widthval) && int.TryParse(height.Value, out int heightval))
                                            spriteAsset.Size = new Vector2(widthval, heightval);
                                        else Console.WriteLine($"-- ERROR: Node '{node.Name}' of asset #{i} in '{rel}' failed to parse Width and Height attributes.");
                                        break;
                                    }

                                case "origin" when entryAsset is Sprite spriteAsset:
                                    {
                                        XmlAttributeCollection? attrs = node.Attributes;

                                        XmlNode? mode = attrs?.GetNamedItem("mode");
                                        XmlNode? x    = attrs?.GetNamedItem("x");
                                        XmlNode? y    = attrs?.GetNamedItem("y");

                                        if (attrs == null || mode == null ||
                                            (string.Equals(mode.Value, "manual", StringComparison.OrdinalIgnoreCase) && (x == null || y == null)))
                                        {
                                            // invalid node
                                            Console.WriteLine($"-- WARN: Node '{node.Name}' of asset #{i} in '{rel}' does not have required attributes ([mode] || [mode == manual, x, y]).");
                                            break;
                                        }

                                        switch (mode.Value?.ToLower())
                                        {
                                            case "default" or "top-left": spriteAsset.Origin = Vector2.Zero; break;
                                            case "top" or "top-center": spriteAsset.Origin = new Vector2((int)Math.Floor(spriteAsset.Size.X / 2), 0); break;
                                            case "top-right": spriteAsset.Origin = new Vector2((int)Math.Floor(spriteAsset.Size.X), 0); break;

                                            case "left": spriteAsset.Origin = new Vector2(0, (int)Math.Floor(spriteAsset.Size.Y / 2)); break;
                                            case "middle" or "center": spriteAsset.Origin = new Vector2((int)Math.Floor(spriteAsset.Size.X / 2), (int)Math.Floor(spriteAsset.Size.Y / 2)); break;
                                            case "right": spriteAsset.Origin = new Vector2((int)Math.Floor(spriteAsset.Size.X), (int)Math.Floor(spriteAsset.Size.Y / 2)); break;

                                            case "bottom-left": spriteAsset.Origin = new Vector2(0, (int)Math.Floor(spriteAsset.Size.Y)); break;
                                            case "bottom" or "bottom-center": spriteAsset.Origin = new Vector2((int)Math.Floor(spriteAsset.Size.X / 2), (int)Math.Floor(spriteAsset.Size.Y)); break;
                                            case "bottom-right": spriteAsset.Origin = new Vector2((int)Math.Floor(spriteAsset.Size.X), (int)Math.Floor(spriteAsset.Size.Y)); break;

                                            case "manual":
                                                {
                                                    if (int.TryParse(x?.Value, out int vx) && int.TryParse(y?.Value, out int vy))
                                                        spriteAsset.Origin = new Vector2(vx, vy);
                                                    else Console.WriteLine($"-- ERROR: Node '{node.Name}' of asset #{i} in '{rel}' failed to parse X and Y attributes.");
                                                    break;
                                                }

                                            default:
                                                {
                                                    Console.WriteLine($"-- ERROR: Node '{node.Name}' of asset #{i} in '{rel}' has an unsupported 'mode' attribute.");
                                                    break;
                                                }
                                        }

                                        break;
                                    }

                                case "margin" when entryAsset is Sprite spriteAsset:
                                    {
                                        XmlAttributeCollection? attrs = node.Attributes;

                                        XmlNode? mode   = attrs?.GetNamedItem("mode");
                                        XmlNode? left   = attrs?.GetNamedItem("left");
                                        XmlNode? right  = attrs?.GetNamedItem("right");
                                        XmlNode? top    = attrs?.GetNamedItem("top");
                                        XmlNode? bottom = attrs?.GetNamedItem("bottom");

                                        bool isManual = mode != null && string.Equals(mode.Value, "manual", StringComparison.OrdinalIgnoreCase);

                                        if (attrs == null || mode == null ||
                                            (isManual && (left == null || right == null || top == null || bottom == null)))
                                        {
                                            Console.WriteLine($"-- WARN: Node '{node.Name}' of asset #{i} in '{rel}' does not have required attributes ([mode] || [mode == manual, left, right, top, bottom]).");
                                            break;
                                        }

                                        if (isManual)
                                        {
                                            if (int.TryParse(left?.Value, out int leftv) &&
                                                int.TryParse(right?.Value, out int rightv) &&
                                                int.TryParse(top?.Value, out int topv) &&
                                                int.TryParse(bottom?.Value, out int bottomv))
                                                spriteAsset.Margin = new Vector4(leftv, rightv, topv, bottomv);
                                            else
                                            {
                                                Console.WriteLine($"-- ERROR: Node '{node.Name}' of asset #{i} in '{rel}' failed to parse Left, Right, Top and Bottom attributes.");
                                                spriteAsset.Margin = new Vector4(0, spriteAsset.Size.X - 1, 0, spriteAsset.Size.Y - 1);
                                                break;
                                            }
                                        }
                                        else spriteAsset.Margin = new Vector4(0, spriteAsset.Size.X - 1, 0, spriteAsset.Size.Y - 1);
                                        break;
                                    }

                                case "frames" when entryAsset is Sprite spriteAsset:
                                    {
                                        XmlNodeList children = node.ChildNodes;

                                        // we dont care about filtering out for duplicates, since frames can repeat
                                        // and we dont want to force the mod developer to have to copy the same
                                        // exact image

                                        foreach (XmlNode child in children)
                                            if (string.Equals(child.Name, "image", StringComparison.OrdinalIgnoreCase)) spriteAsset.Frames.Add($"{asset}\\{child.InnerText}");

                                        // for now that should be good
                                        break;
                                    }

                                // Object assets
                                case "sprite" when entryAsset is Object obj:
                                    {
                                        UndertaleSprite? foundSprite = _gameData.Sprites.ByName(node.InnerText.Trim(), true);
                                        if (foundSprite != null) obj.Sprite = foundSprite;
                                        else Console.WriteLine($"-- WARN: Sprite '{node.InnerText.Trim()}' was not found for asset #{i} in '{rel}'.");
                                        break;
                                    }

                                case "visible" when entryAsset is Object obj:
                                    {
                                        obj.Visible = StringToBool(node.InnerText, true);
                                        break;
                                    }

                                case "solid" when entryAsset is Object obj:
                                    {
                                        obj.Solid = StringToBool(node.InnerText, true);
                                        break;
                                    }

                                case "persistent" when entryAsset is Object obj:
                                    {
                                        obj.Persistent = StringToBool(node.InnerText, true);
                                        break;
                                    }

                                case "parent" when entryAsset is Object obj:
                                    {
                                        UndertaleGameObject? possibleParent = _gameData.GameObjects.ByName(node.InnerText, true);
                                        if (possibleParent != null) obj.Parent = possibleParent;
                                        else Console.WriteLine($"-- WARN: Parent object '{node.InnerText.Trim()}' was not found for asset #{i} in '{rel}'.");
                                        break;
                                    }

                                case "texturemask" when entryAsset is Object obj:
                                    {
                                        UndertaleSprite? foundSprite = _gameData.Sprites.ByName(node.InnerText.Trim(), true);
                                        if (foundSprite != null) obj.TextureMask = foundSprite;
                                        else Console.WriteLine($"-- WARN: Texture mask sprite '{node.InnerText.Trim()}' was not found for asset #{i} in '{rel}'.");
                                        break;
                                    }

                                case "collision" when entryAsset is Object obj:
                                    {
                                        break;
                                    }

                                // Other
                                case "#comment": break; // ignore comments
                                default:
                                    {
                                        Console.WriteLine($"-- WARN: Unrecognized node '{node.Name}' of asset #{i} in '{rel}'.");
                                        break;
                                    }
                            }
                        }

                        if (entryAsset is T validAsset)
                        {
                            Console.WriteLine($"-- Asset #{i} from '{rel}' successfully processed as '{entryAsset.Name}'");
                            assets.Add(validAsset);
                        }
                        else Console.WriteLine($"-- WARN: Found supported asset type, but in wrong project folder. Entry '{entryAsset.Name}' (#{i} in '{rel}') will NOT be imported.");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"-- ERROR ({ex.Message}): Failed to process assets in '{rel}'");
                }
            }

            return assets;
        }

        public static bool StringToBool(string? nodeValue, bool defaultTo = false)
        {
            return nodeValue != null ?
                string.Equals(nodeValue, "yes", StringComparison.OrdinalIgnoreCase) || string.Equals(nodeValue, "true", StringComparison.OrdinalIgnoreCase) :
                defaultTo;
        }
    }
}
