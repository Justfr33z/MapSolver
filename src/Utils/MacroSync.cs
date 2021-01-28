using OQ.MineBot.PluginBase;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MapSolver.Utils {
    public class MacroSync {
        private Task macroTask;
        private string name;

        public MacroSync() { }
        public MacroSync(string name) {
            this.name = name;
        }

        public bool IsMacroRunning() {
            if (macroTask is null)
                return false;

            return !macroTask.IsCompleted && !macroTask.IsCanceled && !macroTask.IsFaulted;
        }

        public void Run(IBotContext botContext) {
            macroTask = botContext.Functions.StartMacro(name);
        }

        public void Run(IBotContext botContext, string name) {
            macroTask = botContext.Functions.StartMacro(name);
        }
    }
}
