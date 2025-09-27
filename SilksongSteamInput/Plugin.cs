using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using GlobalEnums;
using HarmonyLib;
using Steamworks;
using System.Runtime.ExceptionServices;
using UnityEngine;
using static InputModuleBinder;


namespace SilksongSteamInput
{
    [BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
    public class Plugin : BaseUnityPlugin
    {
        internal static Plugin Instance;
        internal static new ManualLogSource Logger;
        internal static ConfigEntry<bool> PluginEnabled;

        internal InputHandle_t allHandles = new(Steamworks.Constants.STEAM_INPUT_HANDLE_ALL_CONTROLLERS);

        private void Awake()
        {
            Instance = this; // patches run statically, but the plugin is an instance

            PluginEnabled = Config.Bind("General", "Enabled", true);

            Harmony harmony = new(MyPluginInfo.PLUGIN_GUID);
            harmony.PatchAll();

            Logger = base.Logger;
        }

        // WaitForGameManager runs itself every 2s until GameManager.instance is valid.
        //
        // TODO(qaisjp): find a better way to do this.
        private void WaitForGameManager()
        {
            if (!GameManager.instance)
            {
                Logger.LogInfo("GameManager not found, waiting another 2s");
                Invoke(nameof(WaitForGameManager), 2f);
                return;
            }


            GameManager.instance.GamePausedChange += (bool paused) =>
            {
                Logger.LogInfo($"PausedEvent fired: {paused}");
                if (paused)
                {
                    SetMenu();
                }
                else
                {
                    SetInGame();
                }
            };

            GameManager.instance.GameStateChange += (GameState state) =>
            {
                Logger.LogInfo($"GameStateChange fired: {state}");
                if (state != GameState.PAUSED)
                {
                    SetInGame();
                }
            };
        }

        private void Start()
        {
            Logger.LogInfo("Plugin started");

            // Not sure why this is needed when Unity initialises this for us...
            // maybe the mod has its own "instance" of Steamworks, isolated from
            // the one that Unity initialises?
            //
            // If we don't do this, ActivateActionSet doesn't work.
            SteamInput.Init(false);

            SetMenu();

            Logger.LogInfo($"Waiting for GameManager to be ready");
            WaitForGameManager();
        }

        private void SetMenu()
        {
            var actionSet = SteamInput.GetActionSetHandle("MenuControls");
            SteamInput.ActivateActionSet(allHandles, actionSet);
            Logger.LogInfo($"Set to MenuControls, actionSet: {actionSet}");
        }

        private void SetInGame()
        {
            var actionSet = SteamInput.GetActionSetHandle("InGameControls");
            SteamInput.ActivateActionSet(allHandles, actionSet);
            Logger.LogInfo($"Set to InGameControls, actionSet: {actionSet}");
        }
    }
}
