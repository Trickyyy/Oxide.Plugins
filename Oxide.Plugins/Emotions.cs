using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Oxide.Core.Plugins;
using Oxide.Game.Rust.Cui;
using System.Globalization;
using Oxide.Core;
using UnityEngine;
using Color = UnityEngine.Color;

namespace Oxide.Plugins
{
    [Info("Emotions", "Tricky & Mevent", "0.1.0")]
    [Description("Convenient UI for emotions")]

    public class Emotions : RustPlugin
    {
        #region Plugin References
        [PluginReference]
        private Plugin ImageLibrary;
        #endregion

        #region Config
        Configuration config;

        public class Configuration
        {
            [JsonProperty(PropertyName = "Command")]
            public string Command = "emotions";

            [JsonProperty(PropertyName = "Button Radius")]
            public int ButtonRadius = 100;

            [JsonProperty(PropertyName = "Close Button Color")]
            public string CloseButtonColor = "#FFB6B3DE";

            [JsonProperty(PropertyName = "Emotion Button Color")]
            public string EmotionButtonColor = "#FF6666DE";

            [JsonProperty(PropertyName = "Emotion Button Size")]
            public int EmotionButtonSize = 50;

            [JsonProperty(PropertyName = "Emotions", ObjectCreationHandling = ObjectCreationHandling.Replace)]
            public List<Emotion> Emotions = new List<Emotion>
            {
                new Emotion
                {
                    Name = "wave",
                    Icon = "https://i.imgur.com/pB3iZer.png"
                },

                new Emotion
                {
                    Name = "victory",
                    Icon = "https://i.imgur.com/PLbSgED.png"
                },

                new Emotion
                {
                    Name = "shrug",
                    Icon = "https://i.imgur.com/A3hHcgV.png"
                },

                new Emotion
                {
                    Name = "thumbsup",
                    Icon = "https://i.imgur.com/yWuhCMu.png"
                },

                new Emotion
                {
                    Name = "chicken",
                    Icon = "https://i.imgur.com/Qxhjf6N.png"
                },

                new Emotion
                {
                    Name = "hurry",
                    Icon = "https://i.imgur.com/vVKVeha.png"
                },

                new Emotion
                {
                    Name = "whoa",
                    Icon = "https://i.imgur.com/AFeGOrK.png"
                }
            };

            public class Emotion
            {
                [JsonProperty(PropertyName = "Gesture Name")]
                public string Name;

                [JsonProperty(PropertyName = "Image")]
                public string Icon;
            }
        }

        protected override void LoadConfig()
        {
            base.LoadConfig();
            try
            {
                config = Config.ReadObject<Configuration>();
                if (config == null) throw new Exception();
            }
            catch
            {
                Config.WriteObject(config, false, $"{Interface.Oxide.ConfigDirectory}/{Name}.jsonError");
                PrintError("The configuration file contains an error and has been replaced with a default config.\n" +
                           "The error configuration file was saved in the .jsonError extension");
                LoadDefaultConfig();
            }

            SaveConfig();
        }

        protected override void LoadDefaultConfig() => config = new Configuration();

        protected override void SaveConfig() => Config.WriteObject(config);
        #endregion

        #region Stored Data
        private const string Layer = "UI_Emotions";
        List<ulong> Players = new List<ulong>();
        #endregion

        #region Oxide Hooks
        private void Init()
        {
            cmd.AddChatCommand(config.Command, this, nameof(EmotionsCommand));
            cmd.AddConsoleCommand("emotions.close", this, nameof(EmotionsCloseCommand));
        }

        private void Loaded()
        {
            if (ImageLibrary == null)
            {
                PrintError("Image Library is not loaded, get it at https://umod.org/plugins/image-library");
                Interface.Oxide.UnloadPlugin(Title);
                return;
            }
        }

        private void OnServerInitialized()
        {
            ImageLibrary.Call("AddImage", "https://i.imgur.com/D40FoBT.png", "CloseImage");
            ImageLibrary.Call("AddImage", "https://i.imgur.com/2fjUdcJ.png", "EmotionImage");

            for (int i = 0; i < config.Emotions.Count; i++)
                ImageLibrary.Call("AddImage", config.Emotions[i].Name, config.Emotions[i].Icon);
        }
        #endregion

        #region Commands
        private void EmotionsCommand(BasePlayer player, string command, string[] args)
        {
            if (!Players.Contains(player.userID))
            {
                UI_DrawInterface(player);
                Players.Add(player.userID);
            }
            else
            {
                CuiHelper.DestroyUi(player, Layer);
                Players.Remove(player.userID);
            }
        }

        private void EmotionsCloseCommand(ConsoleSystem.Arg arg)
        {
            var player = arg.Player();
            if (player == null)
                return;

            Players.Remove(player.userID);
        }
        #endregion

        #region User Interface
        private void UI_DrawInterface(BasePlayer player)
        {
            var container = new CuiElementContainer();

            container.Add(new CuiPanel
            {
                CursorEnabled = true,
                Image = { Color = "0 0 0 0" },
                RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5" }
            }, "Overlay", Layer);

            LoadImage(ref container, Layer, Layer + ".Img", "CloseImage", oMin: "-30 -30", oMax: "30 30", color: HexToRustFormat(config.CloseButtonColor));

            container.Add(new CuiButton
            {
                Button = { Command = "emotions.close", Color = "0 0 0 0", Close = Layer },
                Text = { Text = "" },
                RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1" }
            }, Layer + ".Img");

            for (int i = 0; i < config.Emotions.Count; i++)
            {
                var emotion = config.Emotions[i];
                int r = config.Emotions.Count * 10 + config.ButtonRadius;
                var c = (double) config.Emotions.Count / 2;
                var pos = i / c * Math.PI;
                var x = r * Math.Sin(pos);
                var y = r * Math.Cos(pos);

                LoadImage(ref container, Layer, $"EmoButton.{i}", "EmotionImage", aMin: $"{x - config.EmotionButtonSize} {y - config.EmotionButtonSize}", aMax: $"{x + config.EmotionButtonSize} {y + config.EmotionButtonSize}", color: HexToRustFormat(config.EmotionButtonColor));
                LoadImage(ref container, $"EmoButton.{i}", $"EmoButton.{i}.Img", emotion.Icon, aMin: "0.5 0.5", aMax: "0.5 0.5", oMin: "-30 -30", oMax: "30 30");

                container.Add(new CuiButton
                {
                    RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1" },
                    Button = { Color = "0 0 0 0", Command = $"gesture {emotion.Name}" },
                    Text = { Text = "" }
                }, $"EmoButton.{i}");
            }

            CuiHelper.DestroyUi(player, Layer);
            CuiHelper.AddUi(player, container);
        }

        public void LoadImage(ref CuiElementContainer container, string parent, string name, string image, string aMin = "0 0", string aMax = "1 1", string oMin = "0 0", string oMax = "0 0", string color = "1 1 1 1")
        {
            container.Add(new CuiElement
            {
                Name = name,
                Parent = parent,
                Components =
                {
                    new CuiRawImageComponent { Png = (string) ImageLibrary.Call("GetImage", image), Color = color },
                    new CuiRectTransformComponent { AnchorMin = aMin, AnchorMax = aMax, OffsetMin = oMin, OffsetMax = oMax },
                }
            });
        }
        #endregion

        #region Helpers
        private static string HexToRustFormat(string hex)
        {
            if (string.IsNullOrEmpty(hex))
            {
                hex = "#FFFFFFFF";
            }

            var str = hex.Trim('#');
            if (str.Length == 6)
                str += "FF";

            if (str.Length != 8)
            {
                throw new Exception(hex);
                throw new InvalidOperationException("Cannot convert a wrong format.");
            }

            var r = byte.Parse(str.Substring(0, 2), NumberStyles.HexNumber);

            var g = byte.Parse(str.Substring(2, 2), NumberStyles.HexNumber);
            var b = byte.Parse(str.Substring(4, 2), NumberStyles.HexNumber);
            var a = byte.Parse(str.Substring(6, 2), NumberStyles.HexNumber);
            Color color = new Color32(r, g, b, a);
            return string.Format("{0:F2} {1:F2} {2:F2} {3:F2}", color.r, color.g, color.b, color.a);
        }
        #endregion
    }
}
