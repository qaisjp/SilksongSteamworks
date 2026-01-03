# Silkworks: Steam Input and more for Silksong ([view demo on YouTube](https://youtu.be/gNmEASa1j6Y))

This mod improves Hollow Knight Silksong's support for Steam Input and other Steamworks features

Current features:

* New action sets "In-game" and "Menu"
* On death, the Steam recordings timeline shows death indicators
* When using Steam Input with a DualShock/DualSense controller, the game will now show PlayStation glyphs instead of Xbox glyphs.

Future ideas:

* Add support for named actions, e.g. "Needolin"

## Screenshots

Video demo: https://youtu.be/gNmEASa1j6Y

### Action sets (requires advanced installation)

<img width="1485" height="715" alt="image" src="https://github.com/user-attachments/assets/4c06699c-1307-4244-98de-0ad3c8b1d4f7" />

### Steam Timeline integration

<img width="915" height="286" alt="image" src="https://github.com/user-attachments/assets/3da2cbb8-2cc8-41c4-9037-1b3afad8a9ee" />

### PlayStation glyphs

<img width="105" height="35" alt="{3EA0FFAC-DFE3-4A3F-8085-649DB59D0588}" src="https://github.com/user-attachments/assets/3d722355-b6b3-40ac-9a29-9941aa7a8da6" />

<img width="92" height="32" alt="{C103017A-3216-4C58-8951-47D65400A2D3}" src="https://github.com/user-attachments/assets/2e37ab7e-1165-477e-a2db-fe28bcb61156" />


## Installation instructions

### Basic installation (excludes action sets)

1. Open the game directory, easiest way to do this is to right click the game in Steam -> Manage -> Browse local files
1. Install BepInEx if you haven't already by [following these instructions](https://docs.bepinex.dev/articles/user_guide/installation/index.html), copied below for simplicity:
    1. Download [BepInEx_win_x64_5.4.23.4.zip](https://github.com/BepInEx/BepInEx/releases/download/v5.4.23.4/BepInEx_win_x64_5.4.23.4.zip) -- this is the version I've tested with, you can try a different version though
    1. Extract the contents of the zip alongside `Hollow Knight Silksong.exe` (i.e. `winhttp.dll` should end up in the same folder as the exe)
    1. Run the game once, and exit the game
    1. The `BepInEx` folder alongside `Hollow Knight Silksong.exe` should now contain a `plugins` folder
1. Download [SilksongSteamworks.dll](https://github.com/qaisjp/SilksongSteamworks/releases/latest/download/SilksongSteamworks.dll) and place it in `BepInEx/plugins` (alongisde the `Hollow Knight Silksong.exe` file)

The mod is now installed and should work.

### Advanced installation (includes action sets)

This lets you set different controller keybinds based on whether you're in-game vs. in-menu.

Helpful if you have shoulder buttons pointing to something else, but want to restore them back to normal when in menus.

1. Navigate to your Steam folder. This folder should contain `controller_base`.
1. Alongside (NOT INSIDE) the `controller_base` folder, create a `controller_config` folder.
1. Inside `controller_config`, save [`game_actions_1030300.vdf`](https://raw.githubusercontent.com/qaisjp/SilksongSteamworks/master/game_actions_1030300.vdf).
1. Start the game. You shouldn't be able to navigate any menus with your controller because there are no keybinds associated with each button.
1. Open Steam overlay and manually rebind all the controller buttons. Make sure to set bindings for both the "In-game" and "Menu" action sets
1. Now you can play the game normally. And set custom binds based on whether you're in-game vs. in menus.
