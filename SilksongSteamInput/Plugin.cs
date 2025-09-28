using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using GlobalEnums;
using HarmonyLib;
using JetBrains.Annotations;
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

            RegularlyFixGlyphs();
        }

        // The official way[0] to do this is to call GetDigitalActionOrigins every frame (it's fast)
        // but we actually need to read the controllers (which may not be as fast) regularly, so
        // this function calls itself every second.
        //
        // [0] See "Step 3.3 - On-screen Glyphs" at https://partner.steamgames.com/doc/features/steam_controller/getting_started_for_devs
        private void RegularlyFixGlyphs()
        {
            Invoke(nameof(RegularlyFixGlyphs), 1.0f);

            var handles = new InputHandle_t[Steamworks.Constants.STEAM_INPUT_MAX_COUNT];
            int count = SteamInput.GetConnectedControllers(handles);

            InputHandle_t handle;
            GamepadType? gamepadType = null;

            // Loop over all controllers until we find a controller that needs glyph modifications
            for (int i = 0; i < count; i++)
            {
                handle = handles[i];
                var inputType = SteamInput.GetInputTypeForHandle(handle);
                if (inputType == ESteamInputType.k_ESteamInputType_PS5Controller)
                {
                    gamepadType = GamepadType.PS5;
                    break;
                }
                else if (inputType == ESteamInputType.k_ESteamInputType_PS4Controller)
                {
                    gamepadType = GamepadType.PS4;
                    break;
                }
                else if (inputType == ESteamInputType.k_ESteamInputType_PS3Controller)
                {
                    gamepadType = GamepadType.PS3_WIN;
                    break;
                }
                else
                {
                    Logger.LogInfo($"RegularlyFixGlyphs: Skipping unknown controller type {inputType}");
                }
            }

            if (gamepadType == null)
            {
                // No known gamepad found, nothing to do
                return;
            }

            // Two options:
            // 1. either update the activeGamepadType, which is far reaching and could cause problems with input
            // 2. or copypaste and patch UIButtonSkins to use our detected gamepad type instead of activeGamepadType
            //
            // based on user reports we might need to switch from (1) to (2)

            var currActiveGamepadType = InputHandler.Instance.activeGamepadType;
            if (currActiveGamepadType != gamepadType)
            {
                Logger.LogInfo($"RegularlyFixGlyphs: Setting activeGamepadType from {currActiveGamepadType} to {gamepadType}");
                InputHandler.Instance.activeGamepadType = gamepadType.Value;
            }
        }

        private void Update()
        {
            if (Input.GetKeyDown("k"))
            {
                Logger.LogInfo("K pressed, killing self");
                DamageTag.DamageTagInstance dmg = new DamageTag.DamageTagInstance { amount = 100 };
                HeroController.instance.ApplyTagDamage(damageTagInstance: dmg);
            }
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

        // PostFix HeroController.Start to add a OnDeath handler
        [HarmonyPatch(typeof(HeroController), nameof(HeroController.Start))]
        class HeroController_Start_Patch
        {
            [HarmonyPostfix]
            static void Postfix(HeroController __instance)
            {
                Logger.LogInfo($"HeroController.Start postfix, hooking OnDeath {__instance}");
                __instance.OnDeath += OnDeath;
            }
        }

        private static void OnDeath()
        {
            Logger.LogInfo($"OnDeath fired");
            SteamTimeline.AddTimelineEvent("steam_death", "Killed", null, 0, 0, 0, ETimelineEventClipPriority.k_ETimelineEventClipPriority_Standard);
        }
    }
}
