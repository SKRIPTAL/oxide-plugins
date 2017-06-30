﻿using System;
using System.Collections.Generic;
using Oxide.Core.Libraries.Covalence;

namespace Oxide.Plugins
{
    [Info("StartMachine", "Wulf/lukespragg", "2.1.0", ResourceId = 1586)]
    [Description("Starts machines automatically on server startup and by manual control")]

    class StartMachine : CovalencePlugin
    {
        #region Initialization

        const string permControl = "startmachine.control";

        bool campfireControl;
        bool drillControl;
        bool fridgeControl;
        bool furnaceControl;
        bool state;

        protected override void LoadDefaultConfig()
        {
            // Options
            Config["CampfireControl"] = campfireControl = GetConfig("CampfireControl", true);
            Config["DrillControl"] = drillControl = GetConfig("DrillControl", true);
            Config["FridgeControl"] = fridgeControl = GetConfig("FridgeControl", true);
            Config["FurnaceControl"] = furnaceControl = GetConfig("FurnaceControl", true);

            SaveConfig();
        }

        void OnServerInitialized()
        {
            LoadDefaultConfig();
            LoadDefaultMessages();
            permission.RegisterPermission(permControl, this);

            ToggleMachines(true);
        }

        #endregion

        #region Localization

        void LoadDefaultMessages()
        {
            // English
            lang.RegisterMessages(new Dictionary<string, string>
            {
                ["NotAllowed"] = "You are not allowed to use the '{0}' command",
                ["Started"] = "{0} {1} machines have been started",
                ["Stopped"] = "{0} {1} machines have been stopped"
            }, this);
        }

        #endregion

        [Command("machines")]
        void MachinesCommand(IPlayer player, string command, string[] args)
        {
            if (!player.HasPermission(permControl))
            {
                player.Reply(Lang("NotAllowed", player.Id, command));
                return;
            }

            if (args.Length > 0) state = args[0] != "off";

            ToggleMachines(!state, player);
        }

#if HURTWORLD
        void ToggleMachines(bool toggle, IPlayer player = null)
        {
            if (campfireControl)
            {
                var count = 0;
                var enumerator = CampfireMachine.GetEnumerator();
                while (enumerator.MoveNext())
                {
                    var camp = enumerator.Current.Value;

                    if (!camp.isActiveAndEnabled) continue;
                    if (camp.GetPowered() == toggle) continue;
                    if (!camp.HasFuel) continue;

                    camp.SetPoweredServer(toggle);
                    count++;
                }

                var message = toggle ? Lang("Started", player?.Id, count, "campfire") : Lang("Stopped", player?.Id, count, "campfire");
                player?.Reply(message);
                Puts(message);
            }

            if (drillControl)
            {
                var count = 0;
                var drills = DrillMachine.GetEnumerator();
                while (drills.MoveNext())
                {
                    var drill = drills.Current.Value;

                    if (!drill.isActiveAndEnabled) continue;
                    if (drill.GetPowered() == toggle) continue;
                    if (!drill.HasFuel) continue;

                    drill.SetPoweredServer(toggle);
                    count++;
                }

                var message = toggle ? Lang("Started", player?.Id, count, "drill") : Lang("Stopped", player?.Id, count, "drill");
                player?.Reply(message);
                Puts(message);
            }

            if (fridgeControl)
            {
                var count = 0;
                var enumerator = FridgeMachine.GetEnumerator();
                while (enumerator.MoveNext())
                {
                    var fridge = enumerator.Current.Value;

                    if (!fridge.isActiveAndEnabled) continue;
                    if (fridge.GetPowered() == toggle) continue;
                    if (!fridge.HasFuel) continue;

                    fridge.SetPoweredServer(toggle);
                    count++;
                }

                var message = toggle ? Lang("Started", player?.Id, count, "fridge") : Lang("Stopped", player?.Id, count, "fridge");
                player?.Reply(message);
                Puts(message);
            }

            state = toggle;
        }
#endif

        #region Helpers

        T GetConfig<T>(string name, T value) => Config[name] == null ? value : (T)Convert.ChangeType(Config[name], typeof(T));

        string Lang(string key, string id = null, params object[] args) => string.Format(lang.GetMessage(key, this, id), args);

        #endregion
    }
}
