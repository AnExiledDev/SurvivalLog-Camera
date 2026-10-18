Name: Camera
Summary: A free camera for Survival Log: orbit all the way round, tilt, raise and lower, zoom further, and no more snapping back to your character unless you press the center camera key. 让镜头绕着角色转一整圈，倾斜、升高、降低，缩放范围更大；走楼梯、关闭菜单或退出规划模式时，镜头不再跳回角色身上。

Description (BBCode):
Survival Log's camera looks at your character from one fixed angle and snaps back to them whenever they take the stairs, a menu closes or build mode ends. Camera lets you move it the way most games do, and keeps it where you put it. It only moves the camera; it never changes your character, the world or a save.

[size=5]Main features[/size]
[list]
[*][b]Orbit[/b]: hold the middle mouse button and drag to turn the camera all the way around your character and tilt it. The game's rotate keys, Period (.) and Comma (,), turn it smoothly too.[/*]
[*][b]Tilt[/b]: Insert tilts the view steeper, Delete flatter.[/*]
[*][b]Height[/b]: PageUp lifts the camera, PageDown lowers it.[/*]
[*][b]Reset[/b]: Home puts the angle, height and zoom back to the game's.[/*]
[*][b]Wider zoom[/b]: the scroll wheel zooms further in and out than the game allows, with an adjustable scroll speed.[/*]
[*][b]Zoom stays put[/b]: the plain game puts your zoom back to an older setting whenever the game re-centres the camera while it follows your character, for example when they take stairs or a ladder. Camera keeps the zoom you chose.[/*]
[*][b]No more snapping[/b]: once you have moved the camera off your character, only the center camera key and the camera button on the screen bring it back. Stairs and ladders, closing an object menu, leaving build mode after moving or placing something, and the game's rotate keys all leave the camera where it is, on any floor. Cutscenes, photo mode, the tutorial and travelling to another map still move it the game's way.[/*]
[*][b]Steps aside when needed[/b]: expeditions, photo mode, cutscenes and the death camera use the game's own view, and yours comes back afterwards. Build mode keeps your view.[/*]
[/list]
Every key can be changed or turned off in the config. Keys do nothing while you type, while a menu is open or while you rebind a key, and a key the game or another Survival Log mod already uses is turned off rather than doubled up. Works alongside Time Control.

Camera adds no data to your save and does not edit game files. You can remove it at any time.

Works in English and Chinese.

[size=5]Known issues[/size]
[list]
[*]With OnlyCenterKeyFollows off, leaving build mode only keeps the view while you were looking at another floor.[/*]
[*]Once you have moved the camera off your character, the game's rotate keys orbit the view instead of sending the camera back to your character.[/*]
[*]A camera move the mod does not know about still goes through when it heads for another floor or map, so a game event may still take the camera there.[/*]
[*]With the default middle mouse drag, that drag turns the camera and no longer pans it. Set MouseButton to Right or None to change that.[/*]
[*]Photo mode, expeditions, cutscenes and the death camera use the game's camera.[/*]
[*]Zoom changes the field of view, like the game's own zoom, so the screen edges stretch when zoomed far out.[/*]
[*]The keys hold back for the whole time the crafting table or the codex is open, not only while you type in their search box.[/*]
[*]The game caps one scroll step, so high ScrollSpeed values can zoom no further than lower ones on a fast wheel.[/*]
[/list]

[size=5]Requirements[/size]
[list]
[*]Survival Log 1.1.18902. A game update can break it until the mod is updated.[/*]
[*][url=https://www.nexusmods.com/survivallog/mods/12]BepInEx for Survival Log[/url] (BepInEx 6.0.0-be.788, IL2CPP). Install it first.[/*]
[/list]

[size=5]Installation[/size]
[list=1]
[*]Install [url=https://www.nexusmods.com/survivallog/mods/12]BepInEx[/url] [i](if no other mods were installed before, start the game once so BepInEx finishes setup, then quit)[/i].[/*]
[*]Download the zip from the Files tab.[/*]
[*]Extract it into the game folder (the folder with [font=Courier New]SurvivalLog.exe[/font]). The zip holds a BepInEx folder, so the DLL lands in the right place. When asked, merge the folders. The result should be:
[code]C:\Program Files (x86)\Steam\steamapps\common\Survival Log\BepInEx\plugins\Camera.dll[/code][/*]
[/list]

[size=5]Uninstall[/size]
Delete [font=Courier New]Camera.dll[/font] from the [font=Courier New]BepInEx\plugins[/font] folder.

[size=5]Configuration[/size]
To change keys, limits or speeds, or turn off any piece, edit [font=Courier New]BepInEx\config\com.dev.survivallog.camera.cfg[/font] while the game is closed (see [url=https://github.com/AnExiledDev/SurvivalLog-Camera/blob/main/CONFIG.md]CONFIG.md[/url] for every setting; the cfg file explains each setting in English and Chinese). Changes apply at the next game start.

[size=5]Feedback[/size]
Found a bug or have an idea? Come say hi on the [url=https://discord.gg/trTsyxUFqg]Core Directive Discord[/url], or leave a comment here. For bugs, please include [font=Courier New]BepInEx\LogOutput.log[/font] and [font=Courier New]BepInEx\camera.log[/font].

[size=5]Source and license[/size]
Open source under MIT: [url=https://github.com/AnExiledDev/SurvivalLog-Camera]github.com/AnExiledDev/SurvivalLog-Camera[/url]

[line]
[size=5]简体中文[/size]

Survival Log 的镜头从一个固定的角度看着你的角色，而且每当角色走楼梯、菜单关闭或规划模式结束时，都会跳回角色身上。Camera 让你像大多数游戏那样移动镜头，并把镜头留在你放的位置。它只移动镜头，从不改动你的角色、世界或存档。

[size=5]主要功能[/size]
[list]
[*][b]环绕[/b]：按住鼠标中键拖动，让镜头绕着角色转一整圈并倾斜。游戏的镜头转向键 Period（.）和 Comma（,）也能平滑转动。[/*]
[*][b]倾斜[/b]：Insert 让视角变陡，Delete 让视角变平。[/*]
[*][b]高度[/b]：PageUp 升高镜头，PageDown 降低镜头。[/*]
[*][b]重置[/b]：Home 把角度、高度和缩放恢复成游戏原本的状态。[/*]
[*][b]更大的缩放范围[/b]：滚轮能比游戏放得更大、缩得更小，滚动速度可以调整。[/*]
[*][b]缩放保持不变[/b]：原版游戏在镜头跟随角色时，每次重新居中（比如角色走楼梯或爬梯子）都会把缩放恢复成旧的设置。Camera 会保留你选的缩放。[/*]
[*][b]不再跳回[/b]：一旦你把镜头从角色身上移开，只有镜头跟随键和屏幕上的镜头按钮能把它带回来。走楼梯或爬梯子、关闭物件菜单、移动或摆放物件后退出规划模式，以及游戏的镜头转向键，都会让镜头留在原处，在任何楼层都一样。过场动画、拍照模式、新手引导和前往其他地图时，镜头仍按游戏原本的方式移动。[/*]
[*][b]需要时自动让开[/b]：外出探索、拍照模式、过场动画和死亡镜头使用游戏自己的视角，结束后你的视角会回来。规划模式保留你的视角。[/*]
[/list]
每个按键都可以在配置中修改或关闭。打字时、菜单打开时或重新绑定按键时，按键不起作用；游戏或其他 Survival Log 模组已经在用的键会被关闭，不会重复触发。可以和 Time Control 一起使用。

Camera 不会往存档里写入数据，也不会修改游戏文件。你可以随时移除它。

支持英文和中文。

[size=5]已知问题[/size]
[list]
[*]关闭 OnlyCenterKeyFollows 时，退出规划模式只在你查看其他楼层时才保留视角。[/*]
[*]一旦你把镜头从角色身上移开，游戏的镜头转向键会环绕视角，而不是把镜头送回角色身上。[/*]
[*]模组不认识的镜头移动，如果是前往其他楼层或地图，仍会生效，所以游戏事件仍可能把镜头带过去。[/*]
[*]使用默认的鼠标中键拖动时，拖动会转动镜头，不再平移镜头。把 MouseButton 设为 Right 或 None 可以改变这一点。[/*]
[*]拍照模式、外出探索、过场动画和死亡镜头使用游戏的镜头。[/*]
[*]缩放改变的是视野角度，和游戏自己的缩放一样，所以缩得很远时屏幕边缘会被拉伸。[/*]
[*]制作台或图鉴打开期间，按键会一直不起作用，而不只是在搜索框里打字时。[/*]
[*]游戏限制了每一格滚动的幅度，所以在一次会滚好几格的滚轮上，ScrollSpeed 调高也可能不会缩放得更多。[/*]
[/list]

[size=5]前置要求[/size]
[list]
[*]Survival Log 1.1.18902。游戏更新后模组可能暂时失效，直到模组更新为止。[/*]
[*][url=https://www.nexusmods.com/survivallog/mods/12]BepInEx for Survival Log[/url]，请先安装。[/*]
[/list]

[size=5]安装[/size]
[list=1]
[*]安装 [url=https://www.nexusmods.com/survivallog/mods/12]BepInEx[/url] [i]（如果之前没有装过其他模组，先启动一次游戏，让 BepInEx 完成初始化，然后退出）[/i]。[/*]
[*]在「文件」页下载压缩包。[/*]
[*]把压缩包解压到游戏目录（[font=Courier New]SurvivalLog.exe[/font] 所在的文件夹）。压缩包里带有 BepInEx 文件夹，DLL 会自动放到正确位置。如果提示合并文件夹，选择合并。解压后应为：
[code]C:\Program Files (x86)\Steam\steamapps\common\Survival Log\BepInEx\plugins\Camera.dll[/code][/*]
[/list]

[size=5]卸载[/size]
从 [font=Courier New]BepInEx\plugins[/font] 文件夹中删除 [font=Courier New]Camera.dll[/font]。

[size=5]配置[/size]
如需修改按键、限制或速度，或关闭任何一项功能，请在游戏关闭时编辑 [font=Courier New]BepInEx\config\com.dev.survivallog.camera.cfg[/font]。配置文件中每项设置都有中文说明，完整说明见 [url=https://github.com/AnExiledDev/SurvivalLog-Camera/blob/main/CONFIG.md]CONFIG.md[/url]（英文）。修改会在下次启动游戏时生效。

[size=5]反馈[/size]
发现 bug 或有好点子？欢迎来 [url=https://discord.gg/trTsyxUFqg]Core Directive Discord[/url] 打个招呼，或在这里留言。报告 bug 时，请附上 [font=Courier New]BepInEx\LogOutput.log[/font] 和 [font=Courier New]BepInEx\camera.log[/font]。

[size=5]源代码与许可[/size]
基于 MIT 许可开源：[url=https://github.com/AnExiledDev/SurvivalLog-Camera]github.com/AnExiledDev/SurvivalLog-Camera[/url]
