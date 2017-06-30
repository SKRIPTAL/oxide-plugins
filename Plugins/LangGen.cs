// Requires: Babel

using System;
using System.Collections.Generic;
using Oxide.Core.Libraries.Covalence;
using Oxide.Core.Plugins;

namespace Oxide.Plugins
{
    [Info("LangGen", "Wulf/lukespragg", "0.2.0", ResourceId = 2168)]
    [Description("Generates language files using the default language strings")]
    public class LangGen : CovalencePlugin
    {
        #region Initialization

        [PluginReference]
        private Plugin Babel;

        /*List<object> defaultLanguages; 

        protected override void LoadDefaultConfig()
        {
            // Settings
            Config["Default Languages"] = defaultLanguages = GetConfig("Default Languages", new List<object> { "en", "de" });

            SaveConfig();
        }*/

        private void Init()
        {
            //LoadDefaultConfig();
        }
        
        /*private void OnServerInitialized()
        {
            foreach (var language in defaultLanguages)
            {
                // TODO: Generate default languages from config
            }
        }*/

        #endregion

        #region Localization

        private new void LoadDefaultMessages()
        {
            // English
            lang.RegisterMessages(new Dictionary<string, string>
            {
                ["CommandUsage"] = "Usage: {0} <plugin name> <language code>",
                ["PluginNotFound"] = "Plugin '{0}' could not be found or isn't loaded",
                ["StringsToTranslate"] = "{0} strings to translate from '{1}'",
                ["StringsTranslated"] = "{0} strings translated to '{1}'"
            }, this);
        }

        #endregion

        #region Lang Generation

        [Command("langgen"), Permission("langgen.use")]
        private void Generate(IPlayer player, string command, string[] args)
        {
            if (args.Length != 2)
            {
                player.Reply(Lang("CommandUsage", player.Id, command));
                return;
            }

            var plugin = plugins.PluginManager.GetPlugin(args[0]);
            if (plugin == null || !plugin.IsLoaded)
            {
                player.Reply(Lang("PluginNotFound", player.Id, args[0]));
                return;
            }

            var langTo = args[1];
            var langFrom = lang.GetServerLanguage();
            var origLang = lang.GetMessages(langFrom, plugin);
            var newLang = new Dictionary<string, string>();

            player.Reply(string.Join(", ", lang.GetLanguages(plugin)));
            player.Reply(Lang("StringsToTranslate", player.Id, origLang.Count, langFrom));

            var processed = 1;
            foreach (var pair in origLang)
            {
                var original = pair.Value.Replace("><", "> <").Replace("<", "< ").Replace(">", " >"); // TODO: Fix some languages not translating when > is by (

                Action<string> callback = translation =>
                {
                    translation = translation.Replace("><", "> <").Replace("< ", "<").Replace(" >", ">"); // TODO: Find a better way to do this
                    translation = translation.Replace("{ ", "{").Replace(" }", "}").Replace(" &gt;", ">"); // TODO: Find a better way to do this
#if DEBUG
                    player.Reply($"Original: {pair.Value}");
                    player.Reply($"Translated: {translation}");
#endif
                    newLang.Add(pair.Key, translation);

                    if (processed == origLang.Count)
                    {
                        lang.RegisterMessages(newLang, plugin, langTo);

                        player.Reply(Lang("StringsTranslated", player.Id, processed, langTo));
                        player.Reply(string.Join(", ", lang.GetLanguages(plugin)));
                    }

                    processed++;
                };

                Babel.Call("Translate", original, langTo, langFrom, callback);
            }
        }

        #endregion

        #region Helpers

        private T GetConfig<T>(string name, T value) => Config[name] == null ? value : (T)Convert.ChangeType(Config[name], typeof(T));

        private string Lang(string key, string id = null, params object[] args) => string.Format(lang.GetMessage(key, this, id), args);

        #endregion
    }
}
