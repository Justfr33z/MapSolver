using MapSolver.Tasks;
using MapSolver.Utils;
using OQ.MineBot.PluginBase.Base;
using OQ.MineBot.PluginBase.Base.Plugin;
using OQ.MineBot.PluginBase.Bot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace MapSolver {
    [Plugin(2, "MapSolver", "Solves MapCaptchas using AntiCaptcha")]
    public class PluginCore : IStartPlugin {
        private AntiCaptcha antiCaptcha;

        public override void OnLoad(int version, int subversion, int buildversion) {
            Setting.Add(new StringSetting("ClientKey", "Your anti-captcha.com clientKey.", ""));
            Setting.Add(new StringSetting("Response", "The message containing the solution.\n%solution% gets replaced with the solution.", "%solution%"));
            Setting.Add(new BoolSetting("Debug", "Print everything to the console for debugging.", false));

            var onMessage = new GroupSetting("OnMessage", "Only solve the map when a certain message appears in chat.");
            onMessage.Add(new BoolSetting("Enable", null, false));
            onMessage.Add(new StringSetting("Message", "The message that will appear in chat.\nDoesn't have to be the full message!", ""));
            Setting.Add(onMessage);

            var onSuccess = new GroupSetting("OnSuccess", "The message that appears in chat if the map was solution was successful.");
            onSuccess.Add(new BoolSetting("Enable", null, false));
            onSuccess.Add(new StringSetting("Message", "The message that will appear in chat.\nDoesn't have to be the full message!", ""));
            onSuccess.Add(new StringSetting("Macro", "The name of the macro that will be run when the solution was successful.", ""));
            Setting.Add(onSuccess);

            var onIncorrect = new GroupSetting("OnIncorrect", "The message that appears in chat if the map solution was incorrect.");
            onIncorrect.Add(new BoolSetting("Enable", null, false));
            onIncorrect.Add(new StringSetting("Message", "The message that will appear in chat.\nDoesn't have to be the full message!", ""));
            onIncorrect.Add(new StringSetting("Macro", "The name of the macro that will be run when the solution was incorrect.", ""));
            Setting.Add(onIncorrect);

            ServicePointManager.ServerCertificateValidationCallback = (sender, certificate, chain, errors) => true;
        }

        public override PluginResponse OnEnable(IBotSettings botSettings) {
            if (!botSettings.loadInventory) {
                Console.WriteLine("'Load inventory' must be enabled.");
                return new PluginResponse(false, "'Load inventory' must be enabled.");
            }

            antiCaptcha = new AntiCaptcha(Setting.GetValue<String>("ClientKey"));

            float balance = float.NaN;
            try {
                balance = antiCaptcha.GetBalanceAsync().GetAwaiter().GetResult();
            } catch (Exception exception) {
                Console.WriteLine(exception.Message);
                return new PluginResponse(false, exception.Message);
            }

            Console.WriteLine($"Balance: {balance}");
            return new PluginResponse(true, $"Balance: {balance}");
        }

        public override void OnStart() {
            var onMessage = (IParentSetting)Setting.Get("OnMessage");
            var onSuccess = (IParentSetting)Setting.Get("OnSuccess");
            var onIncorrect = (IParentSetting)Setting.Get("OnIncorrect");

            var successfulMacroSync = new MacroSync(onSuccess.GetValue<string>("Macro"));
            var incorrectMacroSync = new MacroSync(onIncorrect.GetValue<string>("Macro"));

            RegisterTask(
                new Map(
                    successfulMacroSync: successfulMacroSync,
                    incorrectMacroSync: incorrectMacroSync,
                    onMessage: (onMessage.GetValue<bool>("Enable"), onMessage.GetValue<string>("Message")),
                    onSuccess: (onSuccess.GetValue<bool>("Enable"), onSuccess.GetValue<string>("Message")),
                    onIncorrect: (onIncorrect.GetValue<bool>("Enable"), onIncorrect.GetValue<string>("Message")),
                    antiCaptcha: antiCaptcha,
                    response: Setting.GetValue<string>("Response"),
                    debug: Setting.GetValue<bool>("Debug")));
        }
    }
}
