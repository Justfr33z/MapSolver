using OQ.MineBot.PluginBase.Classes.Entity.Player;
using OQ.MineBot.PluginBase.Base.Plugin.Tasks;
using OQ.MineBot.PluginBase.Classes.Items;
using OQ.MineBot.PluginBase.Classes.Base;
using OQ.MineBot.PluginBase.Classes;
using OQ.MineBot.PluginBase;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Drawing.Imaging;
using MapSolver.Utils;
using System.Net.Http;

namespace MapSolver.Tasks {
    public class Map : ITask, ITickListener {
        private readonly MacroSync successfulMacroSync;
        private readonly MacroSync incorrectMacroSync;
        private readonly AntiCaptcha antiCaptcha;
        private readonly string response;
        private readonly (bool, string) onMessage;
        private readonly (bool, string) onSuccess;
        private readonly (bool, string) onIncorrect;
        private readonly bool debug;

        private ushort MAP;
        private byte[,] lastMap;
        private bool solvingMap;
        private bool run = true;

        public Map(MacroSync successfulMacroSync, MacroSync incorrectMacroSync, (bool, string) onMessage, (bool, string) onSuccess, (bool, string) onIncorrect, AntiCaptcha antiCaptcha, string response, bool debug) {
            this.successfulMacroSync = successfulMacroSync;
            this.incorrectMacroSync = incorrectMacroSync;
            this.onMessage = onMessage;
            this.onSuccess = onSuccess;
            this.onIncorrect = onIncorrect;
            this.antiCaptcha = antiCaptcha;
            this.response = response;
            this.debug = debug;
        }

        public override async Task Start() {
            MAP = Items.Instance.GetId("minecraft:filled_map").Value;

            if (onMessage.Item1 || onSuccess.Item1 || onIncorrect.Item1) {
                if (onMessage.Item1)
                    run = false;

                Context.Events.onChat += OnChat;
            }
        }

        public override async Task Stop() {
            if (onMessage.Item1 || onSuccess.Item1 || onIncorrect.Item1)
                Context.Events.onChat -= OnChat;
        }

        private void OnChat(IBotContext context, IChat message, byte position) {
            var rawMessage = message.GetText();

            if (onMessage.Item1) {
                if (rawMessage.Contains(onMessage.Item2)) {
                    run = true;

                    if (debug)
                        Console.WriteLine($"[{context.Player.GetUsername()}] Got an indecator that he has a map to solve");

                    return;
                }
            }

            if (onSuccess.Item1) {
                if (rawMessage.Contains(onSuccess.Item2)) {
                    successfulMacroSync.Run(context);

                    if (onMessage.Item1)
                        run = false;

                    if (debug)
                        Console.WriteLine($"[{context.Player.GetUsername()}] Successfully solved the map");

                    return;
                }
            }

            if (onIncorrect.Item1) {
                if (rawMessage.Contains(onIncorrect.Item2)) {
                    incorrectMacroSync.Run(context);
                    run = true;

                    if (debug)
                        Console.WriteLine($"[{context.Player.GetUsername()}] Didn't solve the map correctly");
                }
            }
        }

        public override bool Exec() {
            return !Context.Player.IsDead() && !solvingMap && !successfulMacroSync.IsMacroRunning() && !incorrectMacroSync.IsMacroRunning() && run;
        }

        public async Task OnTick() {
            var slot = Context.Containers.GetInventory().hotbar.GetAt(Context.Player.GetHeldIndex());
            if (slot is null || slot.Id != MAP) {
                lastMap = null;
                return;
            }

            var map = slot.GetItemData() as IMapItem;
            if (map is null || map.Raw is null)
                return;

            if (lastMap == map.Raw)
                return;

            lastMap = map.Raw;
            solvingMap = true;

            if (debug)
                Console.WriteLine($"[{Context.Player.GetUsername()}] Got a map to solve");

            string solution = String.Empty;
            try {
                var image = map.GetImage();
                var encodedImage = image.ToBase64();

                var taskId = await antiCaptcha.CreateTaskAsync(encodedImage);
                if (debug)
                    Console.WriteLine($"[{Context.Player.GetUsername()}] TaskId: {taskId}");

                solution = await antiCaptcha.GetTaskResultAsync(taskId);
                if (debug)
                    Console.WriteLine($"[{Context.Player.GetUsername()}] Solution: {solution}");

            } catch (Exception exception) {
                Console.WriteLine($"[Error] {exception.Message}");
                solvingMap = false;
                lastMap = null;
                return;
            }

            solvingMap = false;
            if (onMessage.Item1)
                if (!onSuccess.Item1)
                    run = false;

            Context.Player.Chat(response.Replace("%solution%", solution));
        }
    }
}
