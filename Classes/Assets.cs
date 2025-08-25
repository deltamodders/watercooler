using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using UndertaleModLib;
using UndertaleModLib.Models;

namespace Watercooler.Classes
{
    public class Asset
    {
        public string Name = "spr_watercooler_default";
    }

    public class Sprite : Asset
    {
        public Vector2 Origin = Vector2.Zero;
        public Vector2 Size = Vector2.Zero;
        public Vector4 Margin = Vector4.Zero; // Left, Right, Top, Bottom (X, Y, Z, W)
        public string Type = "sprite";
        public List<string> Frames = new();
    }

    public class Object : Asset
    {
        UndertaleResourceById<UndertaleSprite, UndertaleChunkSPRT> _sprite = new(), _textureMask = new();
        UndertaleResourceById<UndertaleGameObject, UndertaleChunkOBJT> _parent = new();

        public UndertaleSprite? Sprite { get => _sprite.Resource; set { _sprite.Resource = value; } }
        public bool Visible = true;
        public bool Solid = false;
        public bool Persistent = false;
        public UndertaleGameObject? Parent { get => _parent.Resource; set { _parent.Resource = value; } }
        public UndertaleSprite? TextureMask { get => _textureMask.Resource; set { _textureMask.Resource = value; } }
        public CollisionShapeFlags Collision = CollisionShapeFlags.Box;
    }
}
