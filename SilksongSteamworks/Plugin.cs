using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using GlobalEnums;
using HarmonyLib;
using Steamworks;

namespace SilksongSteamworks {
    static class Constants {
        internal static InputHandle_t AllHandles = new(Steamworks.Constants.STEAM_INPUT_HANDLE_ALL_CONTROLLERS);
    }


    [BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
    public class Plugin : BaseUnityPlugin {
        // For access from Harmony patches, which are static
        internal static Plugin Instance;
        internal static new ManualLogSource Logger;

        internal static ConfigEntry<bool> PluginEnabled; // always true, someday we can allow disabling via mod menu
        internal bool fixingGlyphs; // so we don't trigger multiple loops

        private void Awake() {
            Instance = this; // patches run statically, but the plugin is an instance

            PluginEnabled = Config.Bind("General", "Enabled", true);

            Harmony harmony = new(MyPluginInfo.PLUGIN_GUID);
            harmony.PatchAll();

            Logger = base.Logger;
        }

        private void Start() {
            Logger.LogInfo("Plugin started");

            // Not sure why this is needed when Unity initialises this for us...
            // maybe the mod has its own "instance" of Steamworks, isolated from
            // the one that Unity initialises?
            //
            // If we don't do this, ActivateActionSet doesn't work.
            SteamInput.Init(false);

            SetMenu();
        }

        //
        // Set up pause state hooks on GameManager Start
        //
        [HarmonyPatch(typeof(GameManager), nameof(GameManager.Start))]
        class GameManager_Start_Patch {
            [HarmonyPostfix]
            static void Postfix() {
                Logger.LogInfo($"GameManager.Start postfix, hooking GamePausedChange and GameStateChange");
                GameManager.instance.GamePausedChange += (bool paused) => {
                    Logger.LogInfo($"PausedEvent fired: {paused}");
                    if (paused) {
                        Instance.SetMenu();
                    } else {
                        Instance.SetInGame();
                    }
                };

                GameManager.instance.GameStateChange += (GameState state) => {
                    Logger.LogInfo($"GameStateChange fired: {state}");
                    if (state != GameState.PAUSED) {
                        Instance.SetInGame();
                    }
                };

                // Start the glyph fixing loop (but only once)
                if (!Instance.fixingGlyphs) Instance.RegularlyFixGlyphs();

            }
        }

        // RegularlyFixGlyphs checks every second what controllers are connected,
        // and if it finds a PlayStation controller, it sets InputHandler.activeGamepadType
        // accordingly, so that the correct glyphs are shown in-game.
        private void RegularlyFixGlyphs() {
            fixingGlyphs = true;

            // The official way[0] to do this is to call GetDigitalActionOrigins every frame (it's fast)
            // but we actually need to read the controllers from Steam (which may not be as fast), so
            // this function calls itself every second.
            //
            // [0] See "Step 3.3 - On-screen Glyphs" at https://partner.steamgames.com/doc/features/steam_controller/getting_started_for_devs
            Invoke(nameof(RegularlyFixGlyphs), 1.0f);

            var handles = new InputHandle_t[Steamworks.Constants.STEAM_INPUT_MAX_COUNT];
            int count = SteamInput.GetConnectedControllers(handles);

            GamepadType? gamepadType = null;
            foreach (var handle in handles) {
                // No more controllers
                if (handle.m_InputHandle == 0) break;

                // If there's any controller whose glyphs are PlayStation, force PlayStation glyphs.
                //
                // On Steam Deck, this ensures that if the user plugs in a PlayStation controller,
                // the PlayStation glyphs will be used instead of the Xbox ones.
                var inputType = SteamInput.GetInputTypeForHandle(handle);
                gamepadType = inputType switch {
                    ESteamInputType.k_ESteamInputType_PS5Controller => GamepadType.PS5,
                    ESteamInputType.k_ESteamInputType_PS4Controller => GamepadType.PS4,
                    ESteamInputType.k_ESteamInputType_PS3Controller => GamepadType.PS3_WIN,
                    _ => null
                };
                if (gamepadType != null) break;
                Logger.LogInfo($"RegularlyFixGlyphs: Skipping unknown controller type {inputType}");
            }

            // No known gamepad found, nothing to do
            if (gamepadType == null) return;

            // Don't change the gamepad type if it's already correct or if it's unset
            var currActiveGamepadType = InputHandler.Instance.activeGamepadType;
            if (currActiveGamepadType == gamepadType || currActiveGamepadType == GamepadType.NONE) {
                return;
            }

            // Two options:
            // 1. either update the activeGamepadType, which is far reaching and could cause problems with input
            // 2. or copypaste and patch UIButtonSkins to use our detected gamepad type instead of activeGamepadType
            //
            // based on user reports we might need to switch from (1) to (2)
            Logger.LogInfo($"RegularlyFixGlyphs: Setting activeGamepadType from {currActiveGamepadType} to {gamepadType}");
            InputHandler.Instance.activeGamepadType = gamepadType.Value;
            //
            // # Alternative implementation
            //
            // Instead of directly setting activeGamepadType, we could callSetActiveGamepadType, but:
            //
            // - I'd need to massage a modified InputDevice
            // - It overwrites some other things like the bindable keys (e.g. to support touch pad).
            //   But since Steam Input is an Xbox controller I am not 100% sure we want that
            //
            //InputHandler.Instance.SetActiveGamepadType(unk);
        }

        // Debug: Press K to kill the player
        //private void Update()
        //{
        //    if (Input.GetKeyDown("k"))
        //    {
        //        Logger.LogInfo("K pressed, killing self");
        //        DamageTag.DamageTagInstance dmg = new DamageTag.DamageTagInstance { amount = 100 };
        //        HeroController.instance.ApplyTagDamage(damageTagInstance: dmg);
        //    }
        //}

        // SetMenu switches the SteamInput action set to "MenuControls".
        private void SetMenu() {
            var actionSet = SteamInput.GetActionSetHandle("MenuControls");
            SteamInput.ActivateActionSet(Constants.AllHandles, actionSet);
            Logger.LogInfo($"Set to MenuControls, actionSet: {actionSet}");
        }

        // SetInGame switches the SteamInput action set to "InGameControls".
        private void SetInGame() {
            var actionSet = SteamInput.GetActionSetHandle("InGameControls");
            SteamInput.ActivateActionSet(Constants.AllHandles, actionSet);
            Logger.LogInfo($"Set to InGameControls, actionSet: {actionSet}");
        }

        //
        // Create a death Steam Timeline event when the player dies
        //

        [HarmonyPatch(typeof(HeroController), nameof(HeroController.Start))]
        class HeroController_Start_Patch {
            [HarmonyPostfix]
            static void Postfix(HeroController __instance) {
                Logger.LogInfo($"HeroController.Start postfix, hooking OnDeath {__instance}");
                __instance.OnDeath += OnDeath;
            }
        }

        private static void OnDeath() {
            Logger.LogInfo($"OnDeath fired");
            SteamTimeline.AddTimelineEvent("steam_death", "Killed", null, 0, 0, 0, ETimelineEventClipPriority.k_ETimelineEventClipPriority_Standard);
        }
    }
}
