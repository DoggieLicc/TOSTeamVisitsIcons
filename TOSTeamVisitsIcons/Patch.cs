using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Reflection;
using Game.Interface;
using Game.Simulation;
using HarmonyLib;
using Server.Shared.Info;
using Server.Shared.Messages;
using Server.Shared.State;
using Server.Shared.State.Chat;
using Services;
using Shared.Chat;
using SML;
using UnityEngine;
using UnityEngine.UI;

namespace TOSTeamVisitsIcons
{
    internal class Interpreter
    {
        static RoleCardPanel panel = null;
        public static Dictionary<int, int> summonTargets = new Dictionary<int, int>();
        internal static void HandleMessages(ChatLogMessage chatLogMessage)
        {
            if (Service.Game.Sim.info.gameInfo.Data.gamePhase == GamePhase.PLAY && (chatLogMessage.chatLogEntry is ChatLogFactionTargetSelectionFeedbackEntry || chatLogMessage.chatLogEntry is ChatLogTargetSelectionFeedbackEntry))
            {
                try
                {
                    int teammatePosition;
                    Role teammateRole;
                    int teammateTarget1;
                    int teammateTarget2;
                    Role teammateTargetRole;
                    bool hasNecronomicon;
                    bool isChangingTarget;
                    bool isCancel;
                    MenuChoiceType menuChoiceType;

                    if (chatLogMessage.chatLogEntry is ChatLogFactionTargetSelectionFeedbackEntry)
                    {
                        ChatLogFactionTargetSelectionFeedbackEntry data = chatLogMessage.chatLogEntry as ChatLogFactionTargetSelectionFeedbackEntry;
                        teammatePosition = data.teammatePosition;
                        teammateRole = data.teammateRole;
                        teammateTarget1 = data.teammateTargetingPosition1;
                        teammateTarget2 = data.teammateTargetingPosition2;
                        teammateTargetRole = data.teammatesTargetRole;
                        hasNecronomicon = data.bHasNecronomicon;
                        isChangingTarget = data.bIsChangingTarget;
                        isCancel = data.bIsCancel;
                        menuChoiceType = data.menuChoiceType;
                    }
                    else
                    {
                        GameSimulation gameSimulation = Service.Game.Sim.simulation;
                        RoleCardObservation roleCardObservation = Service.Game.Sim.info.roleCardObservation;
                        ChatLogTargetSelectionFeedbackEntry data = chatLogMessage.chatLogEntry as ChatLogTargetSelectionFeedbackEntry;
                        switch (ModSettings.GetString("Show Own Actions"))
                        {
                            default:
                            case "Never":
                                return;
                            case "Only as Factional Evil":
                                PlayerIdentityData myIdentity = (PlayerIdentityData)gameSimulation.myIdentity;
                                FactionType faction = myIdentity.faction;
                                if (!(faction == FactionType.COVEN || faction == FactionType.APOCALYPSE || faction == FactionType.VAMPIRE || faction == FactionType.CURSED_SOUL))
                                {
                                    return;
                                }
                                break;
                            case "Always":
                                break;
                        }
                        teammatePosition = gameSimulation.myPosition;
                        teammateRole = data.currentRole;
                        teammateTarget1 = data.playerNumber1;
                        teammateTarget2 = data.playerNumber2;
                        teammateTargetRole = data.targetRoleId;
                        hasNecronomicon = roleCardObservation.Data.hasNecronomicon;
                        isChangingTarget = data.bIsChangingTarget;
                        isCancel = data.bIsCancel;
                        menuChoiceType = data.menuChoiceType;
                    }

                    //Only care about non-instant day abilites
                    if (Service.Game.Sim.info.gameInfo.Data.playPhase != PlayPhase.NIGHT)
                    {
                        if (!ModSettings.GetBool("Day Ability Icons") || !(teammateRole == Role.JAILOR || teammateRole == Role.ADMIRER || teammateRole == Role.CORONER || teammateRole == Role.SOCIALITE || teammateRole == Role.CULTIST))
                        {
                            return;
                        }
                    }

                    UIRoleData.UIRoleDataInstance roleData = null;
                    Console.Write($"TOSTVI recieved message: player {teammatePosition + 1} (role {teammateRole}) has decided to ");
                    if (isCancel)
                    {
                        Console.WriteLine($"Cancel their night ability");
                    }
                    else if (isChangingTarget)
                    {
                        Console.WriteLine($"Change their target to {teammateTarget1}");
                    }
                    else
                    {
                        Console.WriteLine($"Target {teammateTarget1}");
                    }
                    Console.WriteLine($"They {(hasNecronomicon ? "have" : "don't have")} the necronomicon!");
                    if (panel == null)
                    {
                        panel = UnityEngine.Object.FindObjectOfType<RoleCardPanel>();
                        Console.WriteLine("TOSTVI panel was null, a new one was grabbed");
                    }
                    if (panel != null)
                    {
                        roleData = panel.roleData.roleDataList.Find((UIRoleData.UIRoleDataInstance d) => d.role == teammateRole);
                        if (roleData != null)
                        {
                            if (roleData.roleIcon != null)
                            {
                                Console.WriteLine("TOSTVI all roledata grabed with success");
                                if (isCancel)
                                {
                                    Manager.Instance.CancelTarget(menuChoiceType, teammateRole, teammatePosition);
                                }
                                else
                                {
                                    Console.WriteLine("TOSTVI grabbing sprite");
                                    //By default use role icon
                                    Sprite sprite = Manager.GetSprite(roleData, panel, 0);
                                    Sprite sprite2 = null;
                                    //Apply ability icon in case option is enabled
                                    if (ModSettings.GetString("Display Mode") == "No Icon")
                                    {
                                        sprite = null;
                                    }
                                    if (ModSettings.GetString("Display Mode") == "Ability Icon")
                                    {
                                        if (menuChoiceType == MenuChoiceType.NightAbility || ((teammateRole == Role.ILLUSIONIST || teammateRole == Role.JAILOR) && menuChoiceType == MenuChoiceType.NightAbility2))
                                        {
                                            sprite = Manager.GetSprite(roleData, panel, 1);
                                        }
                                        else if (menuChoiceType == MenuChoiceType.NightAbility2)
                                        {
                                            sprite = Manager.GetSprite(roleData, panel, 2);
                                            //Failsafe
                                            if (!sprite)
                                            {
                                                sprite = Manager.GetSprite(roleData, panel, 1);
                                                Console.WriteLine("TOSTVI DM ability 1 case scenario");
                                            }
                                            //Fail-Failsafe
                                            if (!sprite)
                                            {
                                                sprite = Manager.GetSprite(roleData, panel, 3);
                                            }
                                            //Fail-Fail-Failsafe
                                            if (!sprite)
                                            {
                                                Console.WriteLine("TOSTVIRI - No sprites found, using role");
                                                sprite = Manager.GetSprite(roleData, panel, 0);
                                            }
                                        }
                                    }
                                    if (hasNecronomicon)
                                    {
                                        switch (ModSettings.GetString("Book Icon"))
                                        {
                                            case "No Icon":
                                                break;
                                            default:
                                            case "Replace Icon":
                                                sprite = Service.Game.PlayerEffects.GetEffect(EffectType.NECRONOMICON).sprite;
                                                break;
                                            case "Add Icon":
                                                sprite2 = Service.Game.PlayerEffects.GetEffect(EffectType.NECRONOMICON).sprite;
                                                break;
                                        }
                                    }
                                    if (ModSettings.GetBool("Role Revival Icon") && (teammateRole == Role.NECROMANCER || teammateRole == Role.RETRIBUTIONIST))
                                    {
                                        if (menuChoiceType == MenuChoiceType.SpecialAbility)
                                        {
                                            //Add summon info to cache
                                            Console.WriteLine("TOSTVI adding summon target to cache");
                                            if(summonTargets.ContainsKey(teammatePosition))
                                            {
                                                summonTargets.Remove(teammatePosition);
                                            }
                                            summonTargets.Add(teammatePosition, teammateTarget1);
                                        }
                                        else {
                                            try
                                            {
                                                int summonTarget = summonTargets[teammatePosition];
                                                List<KillRecord> killRecords = (List<KillRecord>)Service.Game.Sim.simulation.killRecords;
                                                KillRecord killRecord = killRecords.Find((KillRecord k) => k.playerId == summonTarget);
                                                Role revivalRole = killRecord.playerRole;
                                                //Check if theres hidden info
                                                if (killRecord.hiddenPlayerRole != Role.NONE && killRecord.hiddenPlayerRole != Role.HIDDEN)
                                                {
                                                    revivalRole = killRecord.hiddenPlayerRole;
                                                }
                                                Console.WriteLine("TOSTVIRI revived player role: " + revivalRole);
                                                //Check if is valid know role
                                                if (revivalRole != Role.NONE && revivalRole != Role.STONED && revivalRole != Role.HIDDEN)
                                                {
                                                    UIRoleData.UIRoleDataInstance revivalRoleData = panel.roleData.roleDataList.Find((UIRoleData.UIRoleDataInstance d) => d.role == revivalRole);
                                                    FactionType revivedFaction = killRecord.playerFaction;
                                                    if (killRecord.hiddenPlayerFaction != FactionType.NONE && killRecord.hiddenPlayerFaction != FactionType.UNKNOWN)
                                                    {
                                                        revivedFaction = killRecord.hiddenPlayerFaction;
                                                    }
                                                    if (revivedFaction == FactionType.UNKNOWN)
                                                    {
                                                        Console.WriteLine("TOSTVIRI revived player faction is stoned or hidden, setting to none");
                                                        revivedFaction = FactionType.NONE;
                                                    }
                                                    Console.WriteLine("TOSTVIRI revived player faction: " + revivedFaction);
                                                    sprite = Manager.GetSprite(revivalRoleData, revivedFaction, 0);
                                                }
                                                else
                                                {
                                                    //If unable to get icon of the role been revived, put ability 2 icon
                                                    Console.WriteLine("TOSTVIRI invalid revival role");
                                                    sprite = Manager.GetSprite(roleData, panel, 2);
                                                }
                                            }
                                            catch (KeyNotFoundException)
                                            {
                                                Console.WriteLine("TOSTVIRI summon info not found");
                                                sprite = Manager.GetSprite(roleData, panel, 2);
                                            }
                                        }
                                    }
                                    //Add 2nd ability icon no matter the option selected to avoid duplicated icons
                                    else if ((teammateRole == Role.WITCH || teammateRole == Role.NECROMANCER || teammateRole == Role.RETRIBUTIONIST || teammateRole == Role.POISONER) && menuChoiceType == MenuChoiceType.NightAbility2)
                                    {
                                        Console.WriteLine("TOSTVI ability 2 case scenario");
                                        sprite = Manager.GetSprite(roleData, panel, 2);
                                    }
                                    //Always apply ability icon when it comes to special abilities
                                    if (menuChoiceType == MenuChoiceType.SpecialAbility)
                                    {
                                        Console.WriteLine("TOSTVI special ability case scenario");
                                        sprite = Manager.GetSprite(roleData, panel, 3);
                                    }
                                    Console.WriteLine("TOSTVI starting the request");
                                    switch (menuChoiceType)
                                    {
                                        case MenuChoiceType.NightAbility:
                                            if (sprite)
                                            {
                                                Manager.Instance.ChangeTarget(MenuChoiceType.NightAbility, teammateTarget1, sprite, teammateRole, teammatePosition);
                                            }
                                            if (!sprite && sprite2)
                                            {
                                                Manager.Instance.ChangeTarget(MenuChoiceType.NightAbility, teammateTarget1, sprite2, teammateRole, teammatePosition);
                                            }
                                            else if (sprite2)
                                            {
                                                Manager.Instance.AddTarget(MenuChoiceType.NightAbility, teammateTarget1, sprite2, teammateRole, teammatePosition);
                                            }
                                            break;
                                        case MenuChoiceType.NightAbility2:
                                            if (!sprite) break;
                                            Manager.Instance.ChangeTarget(MenuChoiceType.NightAbility2, teammateTarget2, sprite, teammateRole, teammatePosition);
                                            break;
                                        case MenuChoiceType.SpecialAbility:
                                            if (!sprite) break;
                                            //If special ability with no targets, just put it on themselves
                                            int teammateTargetingPosition = teammatePosition;
                                            if (teammateTarget1 != -1)
                                            {
                                                teammateTargetingPosition = teammateTarget1;
                                            }
                                            if (teammateTarget2 != -1)
                                            {
                                                teammateTargetingPosition = teammateTarget2;
                                            }
                                            //Will add oracle icon to all known aegis targets
                                            if (teammateTargetingPosition == teammatePosition && (teammateTargetRole != Role.NONE) && ModSettings.GetString("Special Ability Icon") != "No Icon")
                                            {
                                                Manager.Instance.CancelTarget(MenuChoiceType.SpecialAbility, teammateRole, teammatePosition);
                                                foreach (TosAbilityPanelListItem player in Manager.Instance.Panel.playerListPlayers)
                                                {
                                                    if (player.playerRole == teammateTargetRole)
                                                    {
                                                        Manager.Instance.AddTarget(MenuChoiceType.SpecialAbility, player.characterPosition, sprite, teammateRole, teammatePosition);
                                                    }
                                                }
                                                return;
                                            }
                                            switch (ModSettings.GetString("Special Ability Icon"))
                                            {
                                                case "No Icon":
                                                    break;
                                                case "Replace Icon":
                                                    Manager.Instance.CancelTarget(MenuChoiceType.NightAbility2, teammateRole, teammatePosition);
                                                    Manager.Instance.ChangeTarget(MenuChoiceType.NightAbility, teammateTargetingPosition, sprite, teammateRole, teammatePosition);
                                                    break;
                                                default:
                                                case "Add Icon":
                                                    Manager.Instance.ChangeTarget(MenuChoiceType.SpecialAbility, teammateTargetingPosition, sprite, teammateRole, teammatePosition);
                                                    break;
                                            }
                                            break;
                                    }
                                }
                            }
                        }
                    }
                    else
                    {
                        Console.WriteLine("TOSTVI There was no panel");
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine("TOSTVI Error! " + e.Message);
                }
            }
        }
    }
    [HarmonyPatch(typeof(GameObservations))]
    internal class Cleaner
    {
        static bool hooked = false;
        static bool clearedDayIcons = false;
        static bool clearedNightIcons = false;

        [HarmonyPatch("HandleGameInfo")]
        [HarmonyPostfix]
        static void ClearIcons(GameInfoObservation gameInfoObservation)
        {
            if (gameInfoObservation.Data.gamePhase == GamePhase.PLAY && !hooked)
            {
                hooked = true;
                Console.WriteLine("TOSTVI adding hook");
                Service.Game.Sim.simulation.incomingChatLogMessage.OnChanged += Interpreter.HandleMessages;
            }
            else if (gameInfoObservation.Data.gamePhase != GamePhase.PLAY && hooked)
            {
                hooked = false;
                Console.WriteLine("TOSTVI removing hook");
                Service.Game.Sim.simulation.incomingChatLogMessage.OnChanged -= Interpreter.HandleMessages;
            }
            // Clear Day Icons as we enter Night Phase
            if (gameInfoObservation.Data.gamePhase == GamePhase.PLAY && gameInfoObservation.Data.playPhase == PlayPhase.NIGHT)
            {
                // tos2 retriggers this if someone dcs at night, make sure to not clear our night icons
                if (!clearedDayIcons)
                {
                    Console.WriteLine($"TOSTVI Requesting icons clear because of playphase: " + gameInfoObservation.Data.playPhase);
                    Manager.Instance.Clear();
                    clearedDayIcons = true;
                }
                clearedNightIcons = false;
            }
            // Clear Night Icons as we enter Day Phase
            if (gameInfoObservation.Data.gamePhase == GamePhase.PLAY && gameInfoObservation.Data.playPhase == PlayPhase.NIGHT_WRAP_UP)
            {
                //just in case
                if (!clearedNightIcons)
                {
                    Console.WriteLine($"TOSTVI Requesting icons clear because of playphase: " + gameInfoObservation.Data.playPhase);
                    Manager.Instance.Clear();
                    clearedNightIcons = true;
                }
                clearedDayIcons = false;
            }
        }
    }

    internal class Manager
    {
        internal Dictionary<int, List<Image>> visits = new Dictionary<int, List<Image>>();
        TosAbilityPanel _panel = null;
        static Manager _instance = null;
        internal static Manager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new Manager();
                }
                return _instance;
            }
        }
        internal TosAbilityPanel Panel
        {
            get
            {
                if (_panel == null)
                {
                    foreach (TosAbilityPanel pan in UnityEngine.Object.FindObjectsOfType<TosAbilityPanel>())
                    {
                        if (pan.playerListPlayers.Count > 0)
                        {
                            _panel = pan;
                            visits = new Dictionary<int, List<Image>>();
                            for (int i = 0; i < pan.playerListPlayers.Count; i++)
                            {
                                visits.Add(i, new List<Image>());
                            }
                            break;
                        }
                    }
                }
                return _panel;
            }
        }
        internal void AddTarget(MenuChoiceType abilityId, int targetPlayer, Sprite sprite, Role role, int actorPlayer)
        {
            //Adds the sprite to a list with a special name to mark it aparta by player, role and ability
            TosAbilityPanelListItem tagetPlayerPanel = Panel.playerListPlayers[targetPlayer];
            string targetName = $"{role}({actorPlayer})";
            if (abilityId == MenuChoiceType.NightAbility2)
            {
                targetName += "2";
            }
            else if (abilityId == MenuChoiceType.SpecialAbility)
            {
                targetName += "S";
            }
            Image image = UnityEngine.Object.Instantiate(Panel.playerListPlayers[targetPlayer].effectImage2);
            image.gameObject.name = targetName;
            image.name = targetName;
            if (Panel.playerListPlayers[targetPlayer].roleIconButton.isActiveAndEnabled)
            {
                image.transform.SetParent(tagetPlayerPanel.roleIconButton.transform);
            }
            else
            {
                image.transform.SetParent(tagetPlayerPanel.playerNameButton.transform);
            }
            Console.WriteLine("TOSTVI adding icon " + image.name);
            image.transform.localScale = Vector3.one;
            image.sprite = sprite;
            visits[targetPlayer].Add(image);
            image.transform.localPosition = new Vector3(80 + 32 * (visits[targetPlayer].Count - 1), 0, 0);
            image.gameObject.SetActive(true);
        }
        internal int TargetsCount(MenuChoiceType abilityId, Role role, int actorPlayer)
        {
            int counter = 0;
            string roleName = $"{role}({actorPlayer})";
            if (abilityId == MenuChoiceType.NightAbility2)
            {
                roleName += "2";
            }
            else if (abilityId == MenuChoiceType.SpecialAbility)
            {
                roleName += "S";
            }
            Console.WriteLine("TOSTVID count target: " + roleName);
            foreach (List<Image> imgs in visits.Values)
            {
                for (int i = 0; i < imgs.Count; i++)
                {
                    if (imgs[i].gameObject.name == roleName)
                    {
                        counter++;
                    }
                }
            }
            return counter;
        }
        internal void CancelTarget(MenuChoiceType abilityId, Role role, int actorPlayer)
        {
            //Removes the requested sprite from the list of sprites
            bool removed = false;
            string roleName = $"{role}({actorPlayer})";
            if (abilityId == MenuChoiceType.NightAbility2)
            {
                roleName += "2";
            }
            else if (abilityId == MenuChoiceType.SpecialAbility)
            {
                roleName += "S";
            }
            Console.WriteLine("TOSTVID removal target: " + roleName);
            foreach (List<Image> imgs in visits.Values)
            {
                for (int i = 0; i < imgs.Count; i++)
                {
                    if (imgs[i].gameObject.name == roleName)
                    {
                        Image temp = imgs[i];
                        Console.WriteLine("TOSTVI removing " + temp.gameObject.name + " because of target change or cancel");
                        imgs.RemoveAt(i);
                        UnityEngine.Object.DestroyImmediate(temp);
                        removed = true;
                    }
                    if (removed && i < imgs.Count)
                    {
                        for (int j = i; j < imgs.Count; j++)
                        {
                            imgs[j].transform.localPosition -= new Vector3(32, 0, 0);
                        }
                        i--;
                    }
                    removed = false;
                }
            }
        }
        internal void ChangeTarget(MenuChoiceType abilityId, int targetPlayer, Sprite sprite, Role role, int actorPlayer)
        {
            //First removes all relevant sprites, then adds any relevant sprites to the list
            Console.WriteLine("TOSTVI requesting cancels for the change of target");
            switch (role)
            {
                case Role.POTIONMASTER:
                case Role.RITUALIST:
                case Role.VOODOOMASTER:
                    CancelTarget(MenuChoiceType.NightAbility, role, actorPlayer);
                    CancelTarget(MenuChoiceType.NightAbility2, role, actorPlayer);
                    CancelTarget(MenuChoiceType.SpecialAbility, role, actorPlayer);
                    break;
                case Role.NECROMANCER:
                    if (abilityId == MenuChoiceType.NightAbility)
                    {
                        CancelTarget(MenuChoiceType.NightAbility, role, actorPlayer);
                        CancelTarget(MenuChoiceType.NightAbility2, role, actorPlayer);
                        CancelTarget(MenuChoiceType.SpecialAbility, role, actorPlayer);
                    }
                    else
                    {
                        CancelTarget(MenuChoiceType.NightAbility, role, actorPlayer);
                        CancelTarget(abilityId, role, actorPlayer);
                    }
                    break;
                case Role.ILLUSIONIST:
                case Role.POISONER:
                case Role.MEDUSA:
                case Role.VAMPIRE:
                    CancelTarget(MenuChoiceType.NightAbility, role, actorPlayer);
                    CancelTarget(MenuChoiceType.NightAbility2, role, actorPlayer);
                    break;
                case Role.COVENLEADER:
                    CancelTarget(MenuChoiceType.SpecialAbility, role, actorPlayer);
                    CancelTarget(MenuChoiceType.NightAbility, role, actorPlayer);
                    break;
                case Role.DOOMSAYER:
                    if (TargetsCount(MenuChoiceType.SpecialAbility, role, actorPlayer) >= 3)
                    {
                        CancelTarget(MenuChoiceType.SpecialAbility, role, actorPlayer);
                    }
                    break;
                default:
                    CancelTarget(abilityId, role, actorPlayer);
                    break;
            }
            Console.WriteLine("TOSTVI adding icon to new target");
            AddTarget(abilityId, targetPlayer, sprite, role, actorPlayer);
        }
        internal void Clear()
        {
            //End of night full sprite clear
            foreach (List<Image> imgs in visits.Values)
            {
                for (int i = imgs.Count - 1; i >= 0; i--)
                {
                    Image temp = imgs[i];
                    imgs.RemoveAt(i);
                    if (temp != null && temp.gameObject != null)
                    {
                        Console.WriteLine("TOSTIV deleting icon " + temp.gameObject.name);
                        temp.gameObject.SetActive(true);
                        UnityEngine.Object.DestroyImmediate(temp.gameObject);

                    }
                }
            }
            Interpreter.summonTargets.Clear();
        }


        internal static Sprite GetSprite(UIRoleData.UIRoleDataInstance instance, RoleCardPanel panel, int ability = 0) 
        {
            return GetSprite(instance, panel.CurrentFaction, ability);
        }
        internal static Sprite GetSprite(UIRoleData.UIRoleDataInstance instance, FactionType faction, int ability = 0)
        {
            Sprite sprite;
            if (ModStates.IsEnabled("alchlcsystm.fancy.ui") && Settings.fancyUI != null)
            {
                //Get sprite from Fancy UI if found
                sprite = GetFancyUISprite(instance.role, faction, ability);
                if (sprite != ((Sprite)Settings.fancyUI.GetType("FancyUI.Assets.FancyAssetManager").GetProperty("Blank", BindingFlags.Static | BindingFlags.Public).GetValue(null))) return sprite;
            }
            //Get vannila icons
            switch (ability)
            {
                default:
                case 0:
                    sprite = instance.roleIcon;
                    break;
                case 1:
                    sprite = instance.abilityIcon;
                    break;
                case 2:
                    sprite = instance.abilityIcon2;
                    break;
                case 3:
                    sprite = instance.specialAbilityIcon;
                    break;
            }
            return sprite;
        }

        private static Sprite GetFancyUISprite(Role role, FactionType faction, int ability)
        {
            //Load types
            Type fancyuiassman = Settings.fancyUI.GetType("FancyUI.Assets.FancyAssetManager");
            Type facnyuiutils = Settings.fancyUI.GetType("FancyUI.Utils");
            //Get role name
            string RoleName = (string)facnyuiutils.GetMethod("RoleName", BindingFlags.Static | BindingFlags.Public).Invoke(null, new object[] { role, null });
            //Add ability to name if needed
            switch (ability)
            {
                default:
                    break;
                case 1:
                    RoleName += "_Ability_1";
                    break;
                case 2:
                    RoleName += "_Ability_2";
                    break;
                case 3:
                    RoleName += "_Special";
                    break;
            }
            //Get faction name
            string FactionName = (string)facnyuiutils.GetMethod("FactionName", BindingFlags.Static | BindingFlags.Public, null, new Type[] { typeof(FactionType), Settings.fancyUI.GetType("FancyUI.GameModType"), typeof(bool), typeof(bool) }, null).Invoke(null, new object[] { faction, null, true, false });
            //Get sprite
            Sprite sprite = (Sprite)fancyuiassman.GetMethod("GetSprite", BindingFlags.Static | BindingFlags.Public, null, new Type[] { typeof(string), typeof(bool), typeof(string), typeof(string), typeof(bool) }, null).Invoke(null, new object[] { RoleName, true, FactionName, null, false });
            //Make a 2nd check for ability 1 due to 2 options of file name
            if (ability == 1 && sprite == ((Sprite)Settings.fancyUI.GetType("FancyUI.Assets.FancyAssetManager").GetProperty("Blank", BindingFlags.Static | BindingFlags.Public).GetValue(null))) 
            {
                RoleName = RoleName.Remove(RoleName.IndexOf("_1"));
                sprite = (Sprite)fancyuiassman.GetMethod("GetSprite", BindingFlags.Static | BindingFlags.Public, null, new Type[] { typeof(string), typeof(bool), typeof(string), typeof(string), typeof(bool) }, null).Invoke(null, new object[] { RoleName, true, FactionName, null, false });
            }
            return sprite;
        }
    }
}
