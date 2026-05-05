using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Reflection;
using Game.Interface;
using Game.Simulation;
using Game.Services;
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

namespace FactionVisits
{
    internal class Interpreter
    {
        public static RoleCardPanel panel = null;
        public static bool isRapidMode = false;
        public static Dictionary<int, int> summonTargets = new Dictionary<int, int>();
        public static Dictionary<int, bool> tSpecialAbiilityData = new Dictionary<int, bool>();
        public static int potionChoiceData = 1;
        //Roles whose abiility1 gets replaced when holding Necronomicon (Kinda guessing here)
        //As opposed to roles whose regular ability is the same but with an attack (Like ench+attack)
        public static List<Role> BookReplacesAbility = new List<Role>
        {
            Role.ILLUSIONIST,
            Role.BODYGUARD,
            //Role.CATALYST, cata with book overcharges too (???)
            Role.CLERIC,
            Role.CRUSADER,
            Role.PILGRIM, //no ability, just put here incase
            Role.SOCIALITE,
            Role.TRAPPER,
            Role.TRICKSTER, //technically trick with book just unleashes a basic attack, but basically the same thing
            Role.COVENITE,
            Role.SERIALKILLER, // These roles need book to attack on cov team, so just use book icon for them if they wield book
            Role.SHROUD,
            Role.BERSERKER,
            Role.VIGILANTE,
            Role.WEREWOLF, //Attack is abil1, scent is abil2, should work
            Role.ARSONIST, //These roles ability1 move to ability2
            Role.BAKER,
            Role.MONARCH,
            Role.SOCIALITE,
            Role.JAILOR,
            Role.POTIONMASTER,
            Role.AMNESIAC,
            Role.COVENLEADER,
            (Role)65 // BTOS2 Socialite
        };
        // Factional evil factions
        public static List<FactionType> factionsWithChat = new List<FactionType>
        {
            FactionType.COVEN,
            FactionType.APOCALYPSE,
            FactionType.VAMPIRE,
            FactionType.CURSED_SOUL,
            (FactionType)33, //Jackal, needs incase recruited evil
            (FactionType)34, //Frogs
            (FactionType)35, //Lions
            (FactionType)36, //Hawks
            (FactionType)43, //Pandora
            (FactionType)44 //Compliance
        };
        //Roles with non-instant day abilities
        public static List<Role> dayAbilityRoles = new List<Role>
        {
            Role.JAILOR,
            Role.ADMIRER,
            Role.CORONER,
            Role.SOCIALITE,
            Role.CULTIST,
            Role.DOOMSAYER, //BTOS2 Doom moment
            Role.PIRATE, //BTOS2 Pirate moment
            (Role)64, //BTOS2 Cultist
            (Role)65  //BTOS2 Socialite
        };
        public static Dictionary<Role, Role> acolyteToHorsemen = new Dictionary<Role, Role>
        {
            {Role.PLAGUEBEARER, Role.PESTILENCE},
            {Role.BERSERKER, Role.WAR},
            {Role.BAKER, Role.FAMINE},
            {Role.SOULCOLLECTOR, Role.DEATH},
            {(Role)62, Role.DEATH} //Warlock
        };
        public static void Reset()
        {
            Interpreter.panel = null;
            Interpreter.isRapidMode = false;
            Interpreter.summonTargets = new Dictionary<int, int>();
            Interpreter.tSpecialAbiilityData = new Dictionary<int, bool>();
            Interpreter.potionChoiceData = 1;
        }
        internal static void HandleMessages(ChatLogMessage chatLogMessage)
        {
            if (chatLogMessage.chatLogEntry is ChatLogWhoDiedEntry && ModSettings.GetBool("Day Ability Icons"))
            {
                ChatLogWhoDiedEntry data = chatLogMessage.chatLogEntry as ChatLogWhoDiedEntry;
                KillRecord killRecord = data.killRecord;
                Console.WriteLine($"FactionVisits clearing {killRecord.playerId}'s day icons, if any");
                Manager.Instance.CancelTarget(MenuChoiceType.SpecialAbility, null, killRecord.playerId);
            }
            if (chatLogMessage.chatLogEntry is ChatLogGameMessageEntry)
            {
                ChatLogGameMessageEntry data = chatLogMessage.chatLogEntry as ChatLogGameMessageEntry;
                bool clearOurIcons = false;
                bool replaceOurIcons = false;
                Role ourRole = Role.NONE;
                switch (data.messageId)
                {
                    case GameFeedbackMessage.RAPID_MODE_STARTING:
                        Console.WriteLine("FactionVisits setting rapid mode to true");
                        Interpreter.isRapidMode = true;
                        break;
                    case GameFeedbackMessage.ROLE_RETRAIN_ACCEPTED:
                        if (ModSettings.GetBool("Day Ability Icons"))
                        {
                            Console.WriteLine("FactionVisits clearing any day icons due to retrain");
                            Manager.Instance.CancelTarget(MenuChoiceType.SpecialAbility, null, data.playerNumber1);
                        }
                        break;
                    case GameFeedbackMessage.TARGET_IS_CURSED_SOUL: //BTOS2 Vamp promotion message
                        if (Manager.isModded())
                        {
                            Console.WriteLine("FactionVisits clearing any day icons due to vamp promotion");
                            Manager.Instance.CancelTarget(MenuChoiceType.SpecialAbility, null, data.playerNumber1);
                        }
                        break;
                    case GameFeedbackMessage.PLAYER_IS_NOW_THE_MAIN_VAMPIRE: //Vanilla Vamp promotion message
                        if (!Manager.isModded())
                        {
                            Console.WriteLine("FactionVisits clearing any day icons due to vamp promotion");
                            Manager.Instance.CancelTarget(MenuChoiceType.SpecialAbility, null, data.playerNumber1);
                        }
                        break;
	        	    case GameFeedbackMessage.POTION_MASTER_POTION_CHOICE_ONE:
                    case GameFeedbackMessage.POTION_MASTER_POTION_CHOICE_ONE_INSTEAD: //Barrier
                        potionChoiceData = 1;
                        clearOurIcons = true;
                        ourRole = Role.POTIONMASTER;
                        break;
	            	case GameFeedbackMessage.POTION_MASTER_POTION_CHOICE_TWO:
                    case GameFeedbackMessage.POTION_MASTER_POTION_CHOICE_TWO_INSTEAD: //Reveal
                        potionChoiceData = 2;
                        clearOurIcons = true;
                        ourRole = Role.POTIONMASTER;
                        break;
	        	    case GameFeedbackMessage.POTION_MASTER_POTION_CHOICE_THREE:
                    case GameFeedbackMessage.POTION_MASTER_POTION_CHOICE_THREE_INSTEAD: //Attack
                        potionChoiceData = 3;
                        clearOurIcons = true;
                        ourRole = Role.POTIONMASTER;
                        break;
                    case GameFeedbackMessage.BLINDED: //Btos2 moment
                        if (!Manager.isModded()) return;
                        potionChoiceData = 1;
                        ourRole = Role.VOODOOMASTER;
                        replaceOurIcons = true;
                        break;
                    case GameFeedbackMessage.BLINDED_BUT_SATED: //Btos2 moment
                        if (!Manager.isModded()) return;
                        potionChoiceData = 2;
                        ourRole = Role.VOODOOMASTER;
                        replaceOurIcons = true;
                        break;
                    case GameFeedbackMessage.VOODOO_MASTER_SILENCE_TARGET:
                        potionChoiceData = 1;
                        ourRole = Role.VOODOOMASTER;
                        replaceOurIcons = true;
                        if (Manager.isModded())
                        {
                            potionChoiceData = 3;
                        }
                        break;
                    case GameFeedbackMessage.VOODOO_MASTER_DEAFEN_TARGET:
                        potionChoiceData = 2;
                        ourRole = Role.VOODOOMASTER;
                        replaceOurIcons = true;
                        break;
                    case GameFeedbackMessage.VOODOO_MASTER_BLIND_TARGET:
                        potionChoiceData = 3;
                        ourRole = Role.VOODOOMASTER;
                        replaceOurIcons = true;
                        break;
                    case (GameFeedbackMessage)1066:
                    case (GameFeedbackMessage)1069: //BTOS2 Baker Wheat Loaf (Reveal)
                        if (!Manager.isModded()) return;
                        potionChoiceData = 1;
                        clearOurIcons = true;
                        ourRole = Role.BAKER;
                        break;
                    case (GameFeedbackMessage)1067:
                    case (GameFeedbackMessage)1070: //BTOS2 Baker Rye Boule (Roleblock)
                        if (!Manager.isModded()) return;
                        potionChoiceData = 2;
                        clearOurIcons = true;
                        ourRole = Role.BAKER;
                        break;
                    case (GameFeedbackMessage)1068:
                    case (GameFeedbackMessage)1071: //BTOS2 Baker Soul Cake (Barrier)
                        if (!Manager.isModded()) return;
                        potionChoiceData = 3;
                        clearOurIcons = true;
                        ourRole = Role.BAKER;
                        break;
                }
                if (clearOurIcons && Manager.showOwnActions())
                {
                    GameSimulation gameSimulation = Service.Game.Sim.simulation;
                    Manager.Instance.ChangeTarget(MenuChoiceType.NightAbility, -1, null, ourRole, Service.Game.Sim.simulation.myPosition);
                    Console.WriteLine("FactionVisits clearing own icons due to potion/bread switch");
                }
                if (replaceOurIcons && ModSettings.GetString("Display Mode") == "Ability Icon" && Manager.showOwnActions())
                {
                    RoleCardData rcData = Service.Game.Sim.info.roleCardObservation.Data;
                    GameSimulation gameSimulation = Service.Game.Sim.simulation;
                    int ourTarget = Manager.Instance.GetTarget(MenuChoiceType.NightAbility, ourRole, Service.Game.Sim.simulation.myPosition);
                    if (ourTarget == -1) return;
                    if (rcData.hasNecronomicon && ModSettings.GetString("Book Icon") == "Replace Icon") return;
                    if (panel == null) panel = UnityEngine.Object.FindObjectOfType<RoleCardPanel>();
                    if (panel == null) return;
                    UIRoleData.UIRoleDataInstance  roleData = panel.roleData.roleDataList.Find((UIRoleData.UIRoleDataInstance d) => d.role == ourRole);
                    if (roleData == null || roleData.roleIcon == null) return;
                    FactionType myFaction = Service.Game.Sim.simulation.myIdentity.Data.faction;
                    Sprite sprite = Manager.GetSprite(roleData, myFaction, potionChoiceData);
                    Manager.Instance.ChangeTarget(MenuChoiceType.NightAbility, ourTarget, sprite, ourRole, Service.Game.Sim.simulation.myPosition);
                    Console.WriteLine("FactionVisits replacing own icons due to doll switch");
                    if (rcData.hasNecronomicon && ModSettings.GetString("Book Icon") == "Add Icon")
                    {
                        Sprite bookSprite = Service.Game.PlayerEffects.GetEffect(EffectType.NECRONOMICON).sprite;
                        Manager.Instance.AddTarget(MenuChoiceType.NightAbility, ourTarget, bookSprite, ourRole, Service.Game.Sim.simulation.myPosition);
                    }
                }
            }
            if (!(Service.Game.Sim.info.gameInfo.Data.gamePhase == GamePhase.PLAY && (chatLogMessage.chatLogEntry is ChatLogFactionTargetSelectionFeedbackEntry || chatLogMessage.chatLogEntry is ChatLogTargetSelectionFeedbackEntry))) return;
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
                FactionType teammateFaction = Service.Game.Sim.simulation.myIdentity.Data.faction;
                bool isMe = false;
                int additData = 1;

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
                    additData = data.specialData;
                }
                else
                {
                    GameSimulation gameSimulation = Service.Game.Sim.simulation;
                    RoleCardObservation roleCardObservation = Service.Game.Sim.info.roleCardObservation;
                    ChatLogTargetSelectionFeedbackEntry data = chatLogMessage.chatLogEntry as ChatLogTargetSelectionFeedbackEntry;
                    if (!Manager.showOwnActions()) return;
                    teammatePosition = gameSimulation.myPosition;
                    teammateRole = data.currentRole;
                    teammateTarget1 = data.playerNumber1;
                    teammateTarget2 = data.playerNumber2;
                    teammateTargetRole = data.targetRoleId;
                    hasNecronomicon = roleCardObservation.Data.hasNecronomicon;
                    isChangingTarget = data.bIsChangingTarget;
                    isCancel = data.bIsCancel;
                    menuChoiceType = data.menuChoiceType;
                    isMe = true;
                }

                // Don't handle unsupported choice types
                if (!(menuChoiceType == MenuChoiceType.NightAbility || menuChoiceType == MenuChoiceType.NightAbility2 || menuChoiceType == MenuChoiceType.SpecialAbility)) return;

                // Teammate role might be inaccurate for transformed horsemen, so we check here
                if (acolyteToHorsemen.ContainsKey(teammateRole))
                {
                    Tuple<Role, FactionType> rfTuple;
                    Service.Game.Sim.simulation.knownRolesAndFactions.Data.TryGetValue(teammatePosition, out rfTuple);
                    if (rfTuple != null && rfTuple.Item1 == acolyteToHorsemen[teammateRole])
                    {
                        teammateRole = acolyteToHorsemen[teammateRole];
                        Console.WriteLine($"Fixed {teammatePosition} Role to {teammateRole}");
                    }
                }

                // Set correct extra data for self when PMer, VMer or BTOS2 Baker
                if (isMe && (teammateRole == Role.POTIONMASTER || teammateRole == Role.VOODOOMASTER || (teammateRole == Role.BAKER && Manager.isModded())))
                {
                    additData = potionChoiceData;
                }

                // Set current amount of stone charges when we're medusa
                if (isMe && teammateRole == Role.MEDUSA)
                {
                    additData = Service.Game.Sim.info.roleCardObservation.Data.normalAbilityRemaining;
                }

                if (menuChoiceType == MenuChoiceType.SpecialAbility && teammateRole == Role.SHROUD)
                {
                    Console.WriteLine("FactionVisits toggleable special ability data to cache");
                    if (tSpecialAbiilityData.ContainsKey(teammatePosition))
                    {
                        tSpecialAbiilityData[teammatePosition] = !isCancel;
                    }
                    else
                    {
                        tSpecialAbiilityData.Add(teammatePosition, !isCancel);
                    }
                }

                bool isFullMoon = Service.Game.Sim.info.gameInfo.Data.playPhase == PlayPhase.NIGHT && (Service.Game.Sim.info.daytime.Data.daynightNumber > 4 || Service.Game.Sim.info.daytime.Data.daynightNumber % 2 == 0);
                float remainingTime = (float)(Service.Game.Sim.simulation.playPhaseState.Data.playPhaseTime - DateTime.UtcNow).TotalSeconds;
                int dayNightNumber = Service.Game.Sim.info.daytime.Data.daynightNumber;
                float secondHalfTime = Interpreter.isRapidMode ? 10f : 19f;
                Console.WriteLine("FactionVisits - Remaining time: " + remainingTime);
                Console.WriteLine("FactionVisits - Current Day/Night number: " + dayNightNumber);
                bool potentiallyOvercharged = (!isCancel && !isChangingTarget) || isMe;

                //Overcharge handler
                if (dayNightNumber > 1 && teammateRole != Role.CURSED_SOUL && Service.Game.Sim.info.gameInfo.Data.playPhase == PlayPhase.NIGHT && potentiallyOvercharged && remainingTime <= secondHalfTime && Manager.Instance.handleOvercharged && Manager.Instance.overchargedTeammate == -1)
                {
                    int tgc1 = Manager.Instance.TargetsCount(MenuChoiceType.NightAbility, teammateRole, teammatePosition);
                    int tgc2 = Manager.Instance.TargetsCount(MenuChoiceType.NightAbility2, teammateRole, teammatePosition);
                    int tgcS = Manager.Instance.TargetsCount(MenuChoiceType.SpecialAbility, teammateRole, teammatePosition);
                    bool isToggleableSpecialAbiility = (menuChoiceType == MenuChoiceType.SpecialAbility && (teammateRole == Role.SHROUD || teammateRole == Role.SERIALKILLER || teammateRole == Role.ENCHANTER || teammateRole == Role.VOODOOMASTER));
                    bool isSecondChoiceAbility = (menuChoiceType == MenuChoiceType.NightAbility2 && (teammateRole == Role.RETRIBUTIONIST || teammateRole == Role.NECROMANCER || teammateRole == Role.SEER || teammateRole == Role.WITCH || teammateRole == Role.WAR));
                    bool onlyMe = ModSettings.GetString("Handle Overcharged") == "Only Myself";
                    bool amIOvercharged = Service.Game.Sim.info.roleAlteringEffectsObservation.Data.bIsOvercharged;
                    bool dontHandleOthers = onlyMe && !isMe;
                    bool fakeOvercharged = isMe && !amIOvercharged;
                    if ((tgc1 + tgc2 + tgcS) != 0 && !isToggleableSpecialAbiility && !isSecondChoiceAbility && !dontHandleOthers && !fakeOvercharged)
                    {
                        Console.WriteLine("FactionVisits - Setting " + teammatePosition + " as overcharged teammate");
                        Manager.Instance.overchargedTeammate = teammatePosition;
                    }
                }

                //Only care about non-instant day abilites
                if (Service.Game.Sim.info.gameInfo.Data.playPhase != PlayPhase.NIGHT)
                {
                    if (!ModSettings.GetBool("Day Ability Icons") || !dayAbilityRoles.Contains(teammateRole))
                    {
                        return;
                    }

                    Manager.Instance.CancelTarget(MenuChoiceType.SpecialAbility, teammateRole, teammatePosition);

                    //Ignore if day ability has no targets, like starspawn's daybreak
                    if (teammateTarget1 == -1 && teammateTarget2 == -1) return;
                }

                UIRoleData.UIRoleDataInstance roleData = null;
                Console.Write($"FactionVisits recieved message: player {teammatePosition + 1} (role {teammateRole}) has decided to ");
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
                    Console.WriteLine("FactionVisits panel was null, a new one was grabbed");
                }
                if (panel == null)
                {
                    Console.WriteLine("FactionVisits There was no panel");
                    return;
                }
                roleData = panel.roleData.roleDataList.Find((UIRoleData.UIRoleDataInstance d) => d.role == teammateRole);

                if (roleData == null || roleData.roleIcon == null) return;

                Console.WriteLine("FactionVisits all roledata grabed with success");

                //Get correct teammate faction if we're a recruit
                if (!isMe && Manager.Instance.amIRecruited)
                {
                    Tuple<Role, FactionType> rfTuple;
                    Service.Game.Sim.simulation.knownRolesAndFactions.Data.TryGetValue(teammatePosition, out rfTuple);
                    if (rfTuple == null)
                    {
                        teammateFaction = FactionType.NONE;
                        Console.WriteLine($"FactionVisits as a recruit, unable to get faction of {teammatePosition}");
                    }
                    else
                    {
                        teammateFaction = rfTuple.Item2;
                    }
                    Console.WriteLine($"FactionVisits as a recruit, setting faction of {teammatePosition} to {teammateFaction}");
                }

                //Handle BTOS2 Pacifist Rallies
                if (Manager.isModded() && teammateRole == (Role)66 && !(hasNecronomicon && menuChoiceType == MenuChoiceType.NightAbility))
                {
                    Console.WriteLine("FactionVisits handling Pacifist rally");
                    if (ModSettings.GetString("Display Mode") == "No Icon") return;
                    Sprite pacSprite = Manager.GetSprite(roleData, teammateFaction, 0);
                    if (ModSettings.GetString("Display Mode") == "Ability Icon") pacSprite = Manager.GetSprite(roleData, teammateFaction, 2);
                    string roleName = "PACIFIST-" + teammateTarget2;
                    Manager.Instance.CancelTarget(MenuChoiceType.NightAbility, teammateRole, teammatePosition);
                    if (isCancel)
                    {
                        Manager.Instance.CancelTarget(MenuChoiceType.NightAbility2, roleName, teammatePosition);
                        return;
                    }
                    Console.WriteLine("FactionVisits adding Pacifist sprite with name: " + roleName);

                    Manager.Instance.AddTarget(MenuChoiceType.NightAbility2, teammateTarget2, pacSprite, roleName, teammatePosition);
                    return;
                }

                if (isCancel && !(teammateRole == Role.SHROUD && menuChoiceType == MenuChoiceType.SpecialAbility))
                {
                    Manager.Instance.ChangeTarget(menuChoiceType, -1, null, teammateRole, teammatePosition);
                    return;
                }

                //Manually set ability to scent for ww
                if (!isFullMoon && teammateRole == Role.WEREWOLF)
                {
                    menuChoiceType = MenuChoiceType.NightAbility2;
                    teammateTarget2 = teammateTarget1;
                    teammateTarget1 = -1;
                    Console.WriteLine("FactionVisits Setting werewolf to 2");
                }

                Console.WriteLine("FactionVisits grabbing sprite");
                //By default use role icon
                Sprite sprite = Manager.GetSprite(roleData, teammateFaction, 0);
                Sprite sprite2 = null;
                //Apply ability icon in case option is enabled
                if (ModSettings.GetString("Display Mode") == "No Icon")
                {
                    sprite = null;
                }
                if (ModSettings.GetString("Display Mode") == "Ability Icon")
                {
                    if (teammateRole == Role.SHROUD)
                    {
                        Console.WriteLine("FactionVisits handling shroud");
                        bool useShroudingIcon = false;
                        if (tSpecialAbiilityData.ContainsKey(teammatePosition)) useShroudingIcon = tSpecialAbiilityData[teammatePosition];
                        Console.WriteLine("FactionVisits is shrouding?: " + useShroudingIcon);
                        if (menuChoiceType == MenuChoiceType.SpecialAbility)
                        {
                            int oldTarget = Manager.Instance.GetTarget(MenuChoiceType.NightAbility, teammateRole, teammatePosition);
                            if (oldTarget == -1) oldTarget = Manager.Instance.GetTarget(MenuChoiceType.NightAbility2, teammateRole, teammatePosition);
                            if (oldTarget == -1) return;
                            Console.WriteLine("FactionVisits found shrouded target: " + oldTarget);
                            if (useShroudingIcon)
                            {
                                menuChoiceType = MenuChoiceType.NightAbility2;
                                teammateTarget2 = oldTarget;
                            }
                            else
                            {
                                menuChoiceType = MenuChoiceType.NightAbility;
                                teammateTarget1 = oldTarget;
                            }
                        } else if (useShroudingIcon)
                        {
                            menuChoiceType = MenuChoiceType.NightAbility2;
                            teammateTarget2 = teammateTarget1;
                        }
                    }
                    if (teammateRole == Role.MEDUSA)
                    {
                        sprite = Manager.GetSprite(roleData, teammateFaction, 1);
                        if (additData == 0) sprite = null;
                    }
                    else if (teammateRole == Role.POTIONMASTER || teammateRole == Role.VOODOOMASTER || (Manager.isModded() && teammateRole == Role.BAKER))
                    {
                        if (teammateRole == Role.BAKER && !isMe) //Game doesn't tell us our teammates bread choice :( use Feed icon instead
                        {
                            sprite = Manager.GetSprite(roleData, teammateFaction, 3);
                        }
                        else if (!(teammateRole == Role.POTIONMASTER && additData == 3))
                        {
                            sprite = Manager.GetSprite(roleData, teammateFaction, additData);
                        }
                    }
                    else if (menuChoiceType == MenuChoiceType.NightAbility || ((teammateRole == Role.ILLUSIONIST || teammateRole == Role.JAILOR) && menuChoiceType == MenuChoiceType.NightAbility2))
                    {
                        sprite = Manager.GetSprite(roleData, teammateFaction, 1);
                    }
                    else if (menuChoiceType == MenuChoiceType.NightAbility2)
                    {
                        sprite = Manager.GetSprite(roleData, teammateFaction, 2);
                        //Failsafe
                        if (!sprite)
                        {
                            sprite = Manager.GetSprite(roleData, teammateFaction, 1);
                            Console.WriteLine("FactionVisits DM ability 1 case scenario");
                        }
                        //Fail-Failsafe
                        if (!sprite)
                        {
                            sprite = Manager.GetSprite(roleData, teammateFaction, 3);
                        }
                        //Fail-Fail-Failsafe
                        if (!sprite)
                        {
                            Console.WriteLine("FactionVisits - No sprites found, using role");
                            sprite = Manager.GetSprite(roleData, teammateFaction, 0);
                        }
                    }
                    if (menuChoiceType == MenuChoiceType.NightAbility2 && teammateRole == Role.SHROUD)
                    {
                        sprite = Manager.GetSprite(roleData, teammateFaction, 3);
                    }
                }
                bool pmerNotUsingKillPot = teammateRole == Role.POTIONMASTER && additData != 3;
                //Second part is a check for BTOS2 Coven-SK using Posses
                if (hasNecronomicon && !pmerNotUsingKillPot && ((menuChoiceType == MenuChoiceType.NightAbility) || (teammateRole == Role.SHROUD || teammateRole == Role.WEREWOLF || teammateRole == Role.TRICKSTER) || (teammateRole == Role.SERIALKILLER && menuChoiceType == MenuChoiceType.NightAbility2 && Manager.isModded())))
                {
                    bool isShrouding = false;
                    if (teammateRole == Role.SHROUD && tSpecialAbiilityData.ContainsKey(teammatePosition)) isShrouding = tSpecialAbiilityData[teammatePosition];
                    bool replaceAbility = (((BookReplacesAbility.Contains(teammateRole) && !isShrouding) && menuChoiceType == MenuChoiceType.NightAbility) || ((teammateRole == Role.DREAMWEAVER && Manager.isModded()) || teammateRole == Role.TRICKSTER)) && ModSettings.GetString("Display Mode") == "Ability Icon";
                    Console.WriteLine("FactionVisits book handler - replaceAbility?: " + replaceAbility);
                    switch (ModSettings.GetString("Book Icon"))
                    {
                        case "No Icon":
                            //If their role's normal ability gets deleted with book, remove original sprite
                            if (replaceAbility)
                            {
                                sprite = null;
                            }
                            break;
                        default:
                        case "Replace Icon":
                            sprite = Service.Game.PlayerEffects.GetEffect(EffectType.NECRONOMICON).sprite;
                            break;
                        case "Add Icon":
                            //If their role's normal ability gets deleted with book, replace first sprite anyway
                            if (replaceAbility)
                            {
                                sprite = Service.Game.PlayerEffects.GetEffect(EffectType.NECRONOMICON).sprite;
                            }
                            else
                            {
                                sprite2 = Service.Game.PlayerEffects.GetEffect(EffectType.NECRONOMICON).sprite;
                            }
                            break;
                    }
                }
                if (ModSettings.GetBool("Role Revival Icon") && (teammateRole == Role.NECROMANCER || teammateRole == Role.RETRIBUTIONIST))
                {
                    if (menuChoiceType == MenuChoiceType.SpecialAbility)
                    {
                        //Add summon info to cache
                        Console.WriteLine("FactionVisits adding summon target to cache");
                        if(summonTargets.ContainsKey(teammatePosition))
                        {
                            summonTargets.Remove(teammatePosition);
                        }
                        summonTargets.Add(teammatePosition, teammateTarget1);
                    }
                    else if (menuChoiceType == MenuChoiceType.NightAbility2)
                    {
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
                            Console.WriteLine("FactionVisits revived player role: " + revivalRole);
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
                                    Console.WriteLine("FactionVisits revived player faction is stoned or hidden, setting to none");
                                    revivedFaction = FactionType.NONE;
                                }
                                Console.WriteLine("FactionVisits revived player faction: " + revivedFaction);
                                sprite = Manager.GetSprite(revivalRoleData, revivedFaction, 0);
                            }
                            else
                            {
                                //If unable to get icon of the role been revived, put ability 2 icon
                                Console.WriteLine("FactionVisits invalid revival role");
                                sprite = Manager.GetSprite(roleData, teammateFaction, 2);
                            }
                        }
                        catch (KeyNotFoundException)
                        {
                            Console.WriteLine("FactionVisits summon info not found");
                            sprite = Manager.GetSprite(roleData, teammateFaction, 2);
                        }
                    }
                }
                //Add 2nd ability icon no matter the option selected to avoid duplicated icons
                else if ((teammateRole == Role.WITCH || teammateRole == Role.NECROMANCER || teammateRole == Role.RETRIBUTIONIST || teammateRole == Role.POISONER || teammateRole == Role.SEER || teammateRole == Role.WAR || (Manager.isModded() && teammateRole == Role.CORONER)) && menuChoiceType == MenuChoiceType.NightAbility2)
                {
                    Console.WriteLine("FactionVisits ability 2 case scenario");
                    sprite = Manager.GetSprite(roleData, teammateFaction, 2);
                }
                //Always apply ability icon when it comes to special abilities
                if (menuChoiceType == MenuChoiceType.SpecialAbility)
                {
                    Console.WriteLine("FactionVisits special ability case scenario");
                    sprite = Manager.GetSprite(roleData, teammateFaction, 3);
                }
                // Always use matchmake icon for btos2 seer ability2
                if (teammateRole == Role.SEER && menuChoiceType == MenuChoiceType.NightAbility2 && Manager.isModded())
                {
                    sprite = Manager.GetSprite(roleData, teammateFaction, 3);
                }
                Console.WriteLine("FactionVisits starting the request");
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
                        if (teammateRole == Role.SEER && Manager.isModded() && ModSettings.GetString("Special Ability Icon") == "No Icon") return;
                        if (sprite)
                        {
                            Manager.Instance.ChangeTarget(MenuChoiceType.NightAbility2, teammateTarget2, sprite, teammateRole, teammatePosition);
                        }
                        if (!sprite && sprite2)
                        {
                            Manager.Instance.ChangeTarget(MenuChoiceType.NightAbility2, teammateTarget2, sprite2, teammateRole, teammatePosition);
                        }
                        else if (sprite2)
                        {
                            Manager.Instance.AddTarget(MenuChoiceType.NightAbility2, teammateTarget2, sprite2, teammateRole, teammatePosition);
                        }
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
                            Manager.Instance.CancelTarget(MenuChoiceType.NightAbility, teammateRole, teammatePosition);
                            foreach (TosAbilityPanelListItem player in Manager.Instance.Panel.playerListPlayers)
                            {
                                if (player.playerRole == teammateTargetRole)
                                {
                                    Manager.Instance.AddTarget(MenuChoiceType.SpecialAbility, player.characterPosition, sprite, teammateRole, teammatePosition);
                                }
                            }
                            return;
                        }
                        //Add Arsonist icon to all known doused targets
                        if (teammateRole == Role.ARSONIST && ModSettings.GetString("Special Ability Icon") != "No Icon")
                        {
                            Manager.Instance.CancelTarget(MenuChoiceType.NightAbility, teammateRole, teammatePosition);
                            List<PlayerEffectsObservation> playerEffectsObs = Service.Game.Sim.info.playerEffects;
                            foreach (PlayerEffectsObservation playerEffectOb in playerEffectsObs)
                            {
                                int playerEffectPos = playerEffectOb.Data.playerPosition;
                                List<EffectType> effects = playerEffectOb.Data.effects;
                                if (effects.Contains(EffectType.DOUSED))
                                {
                                    Manager.Instance.AddTarget(MenuChoiceType.SpecialAbility, playerEffectPos, sprite, teammateRole, teammatePosition);
                                }
                            }
                            //If they're using book to ignite, put book icon on themselves
                            if (hasNecronomicon && ModSettings.GetString("Book Icon") != "No Icon")
                            {
                                Sprite bookSprite = Service.Game.PlayerEffects.GetEffect(EffectType.NECRONOMICON).sprite;
                                Manager.Instance.AddTarget(MenuChoiceType.SpecialAbility, teammatePosition, bookSprite, teammateRole, teammatePosition);
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
            catch (Exception e)
            {
                Console.WriteLine("FactionVisits Error! " + e.Message);
                Console.WriteLine("FactionVisits ErrorSource: " + e.Source);
                Console.WriteLine("FactionVisits ErrorTrace: --\n" + e.StackTrace);
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
                Console.WriteLine("FactionVisits adding hook");
                Service.Game.Sim.simulation.incomingChatLogMessage.OnChanged += Interpreter.HandleMessages;
            }
            else if (gameInfoObservation.Data.gamePhase != GamePhase.PLAY && hooked)
            {
                hooked = false;
                Console.WriteLine("FactionVisits removing hook");
                Service.Game.Sim.simulation.incomingChatLogMessage.OnChanged -= Interpreter.HandleMessages;
            }
            // Clear Day Icons as we enter Night Phase
            if (gameInfoObservation.Data.gamePhase == GamePhase.PLAY && gameInfoObservation.Data.playPhase == PlayPhase.NIGHT)
            {
                // tos2 retriggers this if someone dcs at night, make sure to not clear our night icons
                if (!clearedDayIcons)
                {
                    Console.WriteLine($"FactionVisits Requesting icons clear because of playphase: " + gameInfoObservation.Data.playPhase);
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
                    Console.WriteLine($"FactionVisits Requesting icons clear because of playphase: " + gameInfoObservation.Data.playPhase);
                    Manager.Instance.Clear();
                    clearedNightIcons = true;
                }
                clearedDayIcons = false;
                Manager.Instance.overchargedTeammate = -1;
            }
            if (gameInfoObservation.Data.gamePhase == GamePhase.PLAY && gameInfoObservation.Data.playPhase == PlayPhase.FIRST_DISCUSSION)
            {
                Manager.Instance.Reset();
                Manager.Instance.setHandleOvercharged();
                Interpreter.Reset();
                Console.WriteLine("FactionVisits do we handle overcharges?: " + Manager.Instance.handleOvercharged);
                Console.WriteLine("FactionVisits are we modded?: " + Manager.isModded());
                if (Manager.isModded())
                {
                    FactionType myFaction = Service.Game.Sim.simulation.myIdentity.Data.faction;
                    Role myRole = Service.Game.Sim.simulation.myIdentity.Data.role;
                    if (myFaction == (FactionType)33 && myRole != (Role)55) Manager.Instance.amIRecruited = true;
                    Console.WriteLine("FactionVisits are we recruited?: " + Manager.Instance.amIRecruited);
                }
            }
        }
    }

    internal class Manager
    {
        internal Dictionary<int, List<Image>> visits = new Dictionary<int, List<Image>>();
        public bool handleOvercharged = false;
        public int overchargedTeammate = -1;  //Can only handle 1 overcharged teammate at a time due to game limits
        public bool amIRecruited = false;
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
        internal void Reset()
        {
            handleOvercharged = false;
            overchargedTeammate = -1;
            amIRecruited = false;
            _panel = null;
            visits = new Dictionary<int, List<Image>>();
        }
        internal bool canOverchargeHappen()
        {
            if (ModSettings.GetString("Handle Overcharged") == "Never") return false;
            List<Role> modifiers = Service.Game.Sim.simulation.roleDeckBuilder.Data.modifierCards;
            if ((!Manager.isModded() && modifiers.Contains(Role.ALL_OUTLIERS)) || (Manager.isModded() && modifiers.Contains((Role)231))) return true;  //Role 231 is All Outliers in BTOS2
            List<RoleDeckSlot> roleDeckSlots = Service.Game.Sim.simulation.roleDeckBuilder.Data.roleDeckSlots;
            if (roleDeckSlots.Count == 0) return true; //Replays dont show any slots sometimes, assume true if so
            foreach(RoleDeckSlot roleDeckSlot in roleDeckSlots)
            {
                if (!Manager.isModded() && (roleDeckSlot.Role1 == Role.CATALYST || roleDeckSlot.Role2 == Role.CATALYST)) return true;
                if (Manager.isModded() && (roleDeckSlot.Role1 == (Role)63 || roleDeckSlot.Role2 == (Role)63)) return true; //Role 63 is BTOS2 Catalyst
            }
            return false;
        }
        internal void setHandleOvercharged()
        {
            handleOvercharged = canOverchargeHappen();
        }
        public static bool isModded()
        {
            if (Settings.betterTos == null || !ModStates.IsEnabled("curtis.tuba.better.tos2")) return false;
            Type btosInfo = Settings.betterTos.GetType("BetterTOS2.BTOSInfo");
            return (bool)btosInfo.GetField("IS_MODDED", BindingFlags.Static | BindingFlags.Public).GetValue(null);
        }
        public static bool showOwnActions()
        {
            switch (ModSettings.GetString("Show Own Actions"))
            {
                default:
                case "Never":
                    return false;
                case "Only as Factional Evil":
                    GameSimulation gameSimulation = Service.Game.Sim.simulation;
                    PlayerIdentityData myIdentity = (PlayerIdentityData)gameSimulation.myIdentity;
                    FactionType faction = myIdentity.faction;
                    if (!Interpreter.factionsWithChat.Contains(faction))
                    {
                        return false;
                    }
                    if (Manager.isModded() && myIdentity.role == (Role)55) return false; //Role 55 is BTOS2 Jackal
                    return true;
                case "Always":
                    return true;
            }
        }
        internal void AddTarget(MenuChoiceType abilityId, int targetPlayer, Sprite sprite, object role, int actorPlayer)
        {
            //Adds the sprite to a list with a special name to mark it aparta by player, role and ability
            TosAbilityPanelListItem tagetPlayerPanel = Panel.playerListPlayers[targetPlayer];
            string targetName = GetRoleName(abilityId, role, actorPlayer);
            try
            {
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
                Console.WriteLine("FactionVisits adding icon " + image.name);
                image.transform.localScale = Vector3.one;
                image.sprite = sprite;
                visits[targetPlayer].Add(image);
                image.transform.localPosition = new Vector3(80 + 32 * (visits[targetPlayer].Count - 1), 0, 0);
                image.gameObject.SetActive(true);
            } catch (Exception e)
            {
                Console.WriteLine("FactionVisits Error adding icon " + targetName);
                Console.WriteLine("FactionVisits Error! " + e.Message);
                Console.WriteLine("FactionVisits ErrorSource: " + e.Source);
                Console.WriteLine("FactionVisits ErrorTrace: --\n" + e.StackTrace);
            }
        }
        internal int GetTarget(MenuChoiceType abilityId, object role, int actorPlayer)
        {
            int counter = 0;
            string roleName = GetRoleName(abilityId, role, actorPlayer);
            Console.WriteLine("FactionVisits get target: " + roleName);
            foreach (List<Image> imgs in visits.Values)
            {
                for (int i = 0; i < imgs.Count; i++)
                {
                    try
                    {
                        if (imgs[i].gameObject.name == roleName) return counter;
                    } catch 
                    {
                        Console.WriteLine($"FactionVisits ignoring error when getting {roleName}");
                    }
                }
                counter++;
            }
            return -1;
        }
        internal int TargetsCount(MenuChoiceType abilityId, object role, int actorPlayer)
        {
            int counter = 0;
            string roleName = GetRoleName(abilityId, role, actorPlayer);
            Console.WriteLine("FactionVisits count target: " + roleName);
            foreach (List<Image> imgs in visits.Values)
            {
                for (int i = 0; i < imgs.Count; i++)
                {
                    try {
                        if (imgs[i].gameObject.name == roleName) counter++;
                    } catch 
                    {
                        Console.WriteLine($"FactionVisits ignoring error when counting {roleName}");
                    }
                }
            }
            return counter;
        }
        internal void CancelTarget(MenuChoiceType abilityId, object role, int actorPlayer)
        {
            //Removes the requested sprite from the list of sprites
            bool removed = false;
            string roleName = GetRoleName(abilityId, role, actorPlayer);
            Console.WriteLine("FactionVisits removal target: " + roleName);
            foreach (List<Image> imgs in visits.Values)
            {
                for (int i = 0; i < imgs.Count; i++)
                {
                    try {
                        if (imgs[i].gameObject.name.Contains(roleName))
                        {
                            Image temp = imgs[i];
                            Console.WriteLine("FactionVisits removing " + temp.gameObject.name + " because of target change or cancel");
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
                    } catch 
                    {
                        Console.WriteLine($"FactionVisits ignoring error when removing {roleName}");
                    }
                }
            }
        }
        internal void ChangeTarget(MenuChoiceType abilityId, int targetPlayer, Sprite sprite, Role role, int actorPlayer)
        {
            //First removes all relevant sprites, then adds any relevant sprites to the list
            Console.WriteLine("FactionVisits requesting cancels for the change of target");
            switch (role)
            {
                case Role.BODYGUARD:
                case Role.CLERIC:
                case Role.TRICKSTER:
                case Role.BAKER:
                case Role.ARSONIST:
                case Role.POTIONMASTER:
                case Role.RITUALIST:
                case Role.VOODOOMASTER:
                    CancelTarget(MenuChoiceType.NightAbility, role, actorPlayer);
                    CancelTarget(MenuChoiceType.NightAbility2, role, actorPlayer);
                    CancelTarget(MenuChoiceType.SpecialAbility, role, actorPlayer);
                    break;
                case Role.RETRIBUTIONIST:
                case Role.NECROMANCER:
                    if (abilityId == MenuChoiceType.NightAbility || (abilityId == MenuChoiceType.SpecialAbility && targetPlayer == -1))
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
                case (Role)58: //Auditor in BTOS2, Covenite in Vanilla
                case (Role)59: //Inquisitor in BTOS2, Catalyst in Vanilla
                case (Role)62: //Warlock in BTOS2
                case (Role)65: //Socialite in BTOS2
                case Role.DREAMWEAVER:
                case Role.JAILOR:
                case Role.SOCIALITE:
                case Role.MONARCH:
                case Role.PIRATE:
                case Role.POISONER:
                case Role.MEDUSA:
                case Role.VAMPIRE:
                case Role.WEREWOLF:
                    CancelTarget(MenuChoiceType.NightAbility, role, actorPlayer);
                    CancelTarget(MenuChoiceType.NightAbility2, role, actorPlayer);
                    break;
                case Role.ORACLE:
                case (Role)61: //Oracle in BTOS2
                case Role.VETERAN:
                case Role.ILLUSIONIST:
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
                case (Role)66: //BTOS2 Pacifist
                    if (!Manager.isModded()) return;
                    CancelTarget(MenuChoiceType.NightAbility, role, actorPlayer);
                    for (int i = 0; i < _panel.playerListPlayers.Count; i++)
                    {
                        CancelTarget(MenuChoiceType.NightAbility2, "PACIFIST-"+i, actorPlayer);
                    }
                    break;
                case Role.SEER:
                    if (Manager.isModded())
                    {
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
                    }
                    else
                    {
                        CancelTarget(abilityId, role, actorPlayer);
                    }
                    break;
                case Role.SERIALKILLER:
                case Role.SHROUD:
                    if (abilityId == MenuChoiceType.SpecialAbility) return;
                    CancelTarget(MenuChoiceType.NightAbility, role, actorPlayer);
                    CancelTarget(MenuChoiceType.NightAbility2, role, actorPlayer);
                    break;
                default:
                    CancelTarget(abilityId, role, actorPlayer);
                    break;
            }
            if (targetPlayer == -1 || sprite == null) return;
            Console.WriteLine("FactionVisits adding icon to new target");
            AddTarget(abilityId, targetPlayer, sprite, role, actorPlayer);
        }
        internal string GetRoleName(MenuChoiceType abilityId, object role, int actorPlayer)
        {
            string roleName = $"{role}({actorPlayer})";
            if (role == null) roleName = $"({actorPlayer})";
            if (abilityId == MenuChoiceType.NightAbility)
            {
                roleName += "1";
            }
            if (abilityId == MenuChoiceType.NightAbility2)
            {
                roleName += "2";
            }
            else if (abilityId == MenuChoiceType.SpecialAbility)
            {
                roleName += "S";
            }
            if (actorPlayer == overchargedTeammate)
            {
                roleName += "C";
            }
            return roleName;
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

        internal static Sprite GetSprite(UIRoleData.UIRoleDataInstance instance, FactionType faction, int ability = 0)
        {
            Sprite sprite;
            if (ModStates.IsEnabled("alchlcsystm.fancy.ui") && Settings.fancyUI != null)
            {
                //Get sprite from Fancy UI if found
                sprite = GetFancyUISprite(instance.role, faction, ability);
                if (sprite != ((Sprite)Settings.fancyUI.GetType("FancyUI.Assets.FancyAssetManager").GetProperty("Blank", BindingFlags.Static | BindingFlags.Public).GetValue(null))) return sprite;
                Console.WriteLine($"FactionVisits no FancyUI sprite found for {instance.role}({ability})[{faction}], gonna retry with base faction");
                sprite = GetFancyUISprite(instance.role, null, ability);
                if (sprite != ((Sprite)Settings.fancyUI.GetType("FancyUI.Assets.FancyAssetManager").GetProperty("Blank", BindingFlags.Static | BindingFlags.Public).GetValue(null))) return sprite;
                Console.WriteLine($"FactionVisits no FancyUI sprite found for {instance.role}({ability}), using vanilla sprite.");
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

        private static Sprite GetFancyUISprite(Role role, FactionType? faction, int ability)
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
            if (faction == null)
            {
                faction = (FactionType)facnyuiutils.GetMethod("GetFactionType", BindingFlags.Static | BindingFlags.Public).Invoke(null, new object[] { role, null });
            }
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
