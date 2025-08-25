using System.Numerics;
using System.Drawing;

namespace Watercooler.Classes
{
    public class Packer
    {
        public class Atlas
        {
            public struct SpriteContainer
            {
                public Vector2 Location;
                public Vector2 Size;
                public Sprite Data;
            }

            public int Width = 2048;
            public int Height = 2048;
            public List<SpriteContainer> Sprites = new();

            public int CurrentX = 0;
            public int CurrentY = 0;

            public bool CanFit(Sprite spr)
            {
                var t = CurrentX;
                if (CurrentY + spr.Size.Y > Height)
                {
                    t += (int)Sprites
                        .Where(x => x.Location.X == CurrentX)
                        .OrderByDescending(x => x.Size.X)
                        .FirstOrDefault(new SpriteContainer())
                        .Size.X;
                }

                return t + spr.Size.X <= Width/* && CurrentY + spr.Size.Y <= Height*/;
            }

            public void AddSprite(Sprite spr) {
                if (CurrentY + spr.Size.Y > Height)
                {
                    CurrentX += (int)Sprites
                        .Where(x => x.Location.X == CurrentX)
                        .OrderByDescending(x => x.Size.X)
                        .FirstOrDefault(new SpriteContainer())
                        .Size.X;
                    CurrentY = 0;
                }

                Sprites.Add(new SpriteContainer
                {
                    Location = new Vector2(CurrentX, CurrentY),
                    Size = new Vector2(spr.Size.X, spr.Size.Y),
                    Data = spr
                });

                CurrentY += (int)spr.Size.Y;
            }

            public Bitmap CreateImage()
            {
                // TODO: frames
                Dictionary<string, Image> loadedImages = new();
                Image bitmap = new Bitmap(Width, Height, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
                Graphics g = Graphics.FromImage(bitmap);

                foreach (SpriteContainer cont in Sprites)
                {
                    //foreach (string frame in cont.Data.Frames)
                    //    if (!loadedImages.ContainsKey(frame)) loadedImages.Add(frame, Image.FromFile(frame));
                    g.DrawImage(Image.FromFile(cont.Data.Frames.First()), cont.Location.X, cont.Location.Y, cont.Size.X, cont.Size.Y);
                }

                Bitmap resFix = new Bitmap(bitmap);
                resFix.SetResolution(96, 96);

                return resFix;
            }
        }

        List<Sprite> _sprites = new();

        public Packer(List<Sprite> sprites)
        {
            _sprites = sprites;
        }

        public List<Atlas> Package(int atlasSize)
        {
            List<Atlas> results = new();

            while (_sprites.Count > 0)
            {
                //Console.WriteLine($"Created new atlas: {_sprites.Count} sprite(s) remain.");
                Atlas newAtlas = new()
                {
                    Width = atlasSize,
                    Height = atlasSize,
                };

                LayoutAtlas(newAtlas);
                results.Add(newAtlas);
            }

            //Console.WriteLine($"Finished importing sprites with {results.Count} atlas(es).");
            return results;
        }

        public void LayoutAtlas(Atlas atlas)
        {
            List<Sprite> sprites = _sprites.ToList();
            foreach (Sprite sprite in sprites)
            {
                if (!atlas.CanFit(sprite)) return; // next sprite cant fit anymore. return to make new atlas in Package()

                atlas.AddSprite(sprite);
                _sprites.Remove(sprite);
                //Console.WriteLine($"Fitting sprite {sprite.Name} ({sprite.Size.X}x{sprite.Size.Y}) in atlas. CurrentX is now {atlas.CurrentX} and CurrentY is now {atlas.CurrentY}");
            }
        }
    }
}
