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
        /// <summary>
        /// A standalone event action for usage with code importing.
        /// </summary>
        public class EventAction
        {
            /// <summary>
            /// Type of Event that the action is.
            /// </summary>
            public EventType Type;

            /// <summary>
            /// SubType of an event (ex: DrawGUI, PostDraw) but as an uint because this sucks.
            /// </summary>
            public uint SubType;

            /// <summary>
            /// Name of the code entry that will be imported into the data file.
            /// </summary>
            public string ImportName;

            /// <summary>
            /// Path to the code file for this action.
            /// </summary>
            public string CodePath;

            public EventAction(string type, string codePath, string objectName)
            {
                CodePath = codePath;
                SubType = 0;

                // Get event type
                // !!! TODO: This list needs to be completed!!!
                switch (type.ToLower())
                {
                    case "create":
                        Type = EventType.Create;
                        SubType = 0;
                        break;

                    case "step":
                        Type = EventType.Step;
                        break;

                    case "draw":
                        Type = EventType.Draw;
                        break;

                    case "drawgui":
                        Type = EventType.Draw;
                        SubType = (uint)EventSubtypeDraw.DrawGUI;
                        break;

                    case "destroy":
                        Type = EventType.Destroy;
                        break;
                }

                ImportName = $"gml_Object_{objectName}_{Type}_{SubType}";
            }
        }
        UndertaleResourceById<UndertaleSprite, UndertaleChunkSPRT> _sprite = new(), _textureMask = new();
        UndertaleResourceById<UndertaleGameObject, UndertaleChunkOBJT> _parent = new();

        public List<EventAction> eventActions = new();

        public UndertaleSprite? Sprite { get => _sprite.Resource; set { _sprite.Resource = value; } }
        public bool Visible = true;
        public bool Solid = false;
        public bool Persistent = false;
        public UndertaleGameObject? Parent { get => _parent.Resource; set { _parent.Resource = value; } }
        public UndertaleSprite? TextureMask { get => _textureMask.Resource; set { _textureMask.Resource = value; } }
        public CollisionShapeFlags Collision = CollisionShapeFlags.Box;
    }
}
