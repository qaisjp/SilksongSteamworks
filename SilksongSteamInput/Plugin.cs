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
        internal bool ready = false;

        private void Awake()
        {
            Instance = this; // patches run statically, but the plugin is an instance

            PluginEnabled = Config.Bind("General", "Enabled", true);

            Harmony harmony = new(MyPluginInfo.PLUGIN_GUID);
            harmony.PatchAll();

            Logger = base.Logger;
            Logger.LogInfo($"Waiting for GameManager to be ready");
            WaitForGameManager();
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
            ready = true;
            Logger.LogInfo("Plugin started");

            // Not sure why this is needed when Unity initialises this for us...
            SteamInput.Init(false);

            SetMenu();
            Logger.LogInfo("GameManager is ready. Assuming SteamInput is initialized, getting connected controllers");

            //InvokeRepeating(nameof(PrintControllerInfo), 1f, 1f);
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

        //[HarmonyPatch(typeof(GameManager), nameof(GameManager.SetPausedState))]
        //private static class GameManager_SetPausedState_Patch
        //{
        //    [HarmonyPostfix]
        //    private static void Postfix(bool value)
        //    {
        //        if (!PluginEnabled.Value) return;
        //        Logger.LogInfo($"Game paused state changed: {value}");

        //        if (value)
        //        {
        //            Instance.SetMenu();
        //        } else
        //        {
        //            Instance.SetInGame();
        //        }

        //        // Update SteamInput action set


        //    }
        //}

        //[HarmonyPatch(typeof(HeroController), nameof(HeroController.Attack))]
        //private static class HeroController_Attack_Patch
        //{
        //    [HarmonyPrefix]
        //    private static void Prefix(HeroController __instance, ref AttackDirection attackDir)
        //    {
        //        Logger.LogInfo($"Attack called with direction (orig): {attackDir}, paused: {GameManager.instance.IsGamePaused()}");
        //    }
        //}

        //private void PrintControllerInfo()
        //{
        //    // https://github.com/rlabrecque/Steamworks.NET/issues/73 means that this always returns 0
        //    // (not just returning zero but the handles array is always empty too)
        //    // InputHandle_t[] handles = new InputHandle_t[Steamworks.Constants.STEAM_INPUT_MAX_COUNT];
        //    // int numControllers = SteamInput.GetConnectedControllers(handles);            

        //    // Note: this won't work with there are multiple gamepads for some reason.
        //    //
        //    // Not sure when this happens but in the future we could
        //    // just keep getting the next handle until we get a null handle.
        //    var handle = SteamInput.GetControllerForGamepadIndex(0);
        //}


    }
}
