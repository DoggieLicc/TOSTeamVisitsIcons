using SalemModLoaderUI;
using Server.Shared.Extensions;
using Services;
using SML;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

namespace FactionVisits
{
    [Mod.SalemMod]
    public class Main
    {
        public void Start()
        {
            Console.WriteLine("Modding time!");
            try 
            {
                Settings.fancyUI = Assembly.LoadFrom(Path.Combine(AppContext.BaseDirectory, "SalemModLoader\\Mods\\FancyUI.dll")); 
            }
            catch 
            {
                Console.WriteLine("FactionVisits Fancy UI was not found.");
            }
            try 
            {
                Settings.betterTos = Assembly.LoadFrom(Path.Combine(AppContext.BaseDirectory, "SalemModLoader\\Mods\\BetterTOS2.dll")); 
            }
            catch 
            {
                Console.WriteLine("FactionVisits BetterTOS2 was not found.");
            }
        }
    }

    [DynamicSettings]
    public class Settings
    {
        public static Assembly fancyUI = null;
        public static Assembly betterTos = null;

        public ModSettings.DropdownSetting DisplayMode 
        {
            get
            {
                ModSettings.DropdownSetting dropdownSetting = new ModSettings.DropdownSetting
                {
                    Name = "Display Mode",
                    Description = "When showing the icons of your teammates you can choose to show the icon of their role (with some exceptions for roles that have 2 targets), or you can choose to show the ability icon",
                    Options = DisplaySettings,
                    AvailableInGame = false,
                    Available = true
                };
                return dropdownSetting;
            }
        }

        public ModSettings.DropdownSetting BookIcon 
        {
            get
            {
                ModSettings.DropdownSetting dropdownSetting = new ModSettings.DropdownSetting
                {
                    Name = "Book Icon",
                    Description = "Dictates whether the Necronomicon icon should be disabled, replace the role/ability icon, or be added next to role/ability icon",
                    Options = BookIconSettings,
                    AvailableInGame = false,
                    Available = true
                };
                return dropdownSetting;
            }
        }

        public ModSettings.DropdownSetting SpecialAbilityIcon 
        {
            get
            {
                ModSettings.DropdownSetting dropdownSetting = new ModSettings.DropdownSetting
                {
                    Name = "Special Ability Icon",
                    Description = "Dictates whether the special ability icon should be disabled, replace the role/ability icon, or be added next to role/ability icon",
                    Options = SpecialAbilitySettings,
                    AvailableInGame = false,
                    Available = true
                };
                return dropdownSetting;
            }
        }

        public ModSettings.DropdownSetting HandleOvercharged 
        {
            get
            {
                ModSettings.DropdownSetting dropdownSetting = new ModSettings.DropdownSetting
                {
                    Name = "Handle Overcharged",
                    Description = "Dictates whether the mod should handle overcharged abilities for you, all teammates, or be disabled.\n\nNOTE: Handling all teammate's overcharges is experimental, as there's no 100% way to tell if a teammate's overcharged.",
                    Options = HandleOverchargedSettings,
                    AvailableInGame = false,
                    Available = true
                };
                return dropdownSetting;
            }
        }

        public ModSettings.DropdownSetting ShowOwnAction 
        {
            get
            {
                ModSettings.DropdownSetting dropdownSetting = new ModSettings.DropdownSetting
                {
                    Name = "Show Own Actions",
                    Description = "Dictates whether icons will be added for your own actions\n\nSetting to \"Only as Factional Evil \" means it will only add icons for your own abilities when you are playing as an evil with a night-chat.",
                    Options = ShowOwnActionSettings,
                    AvailableInGame = false,
                    Available = true
                };
                return dropdownSetting;
            }
        }

        public ModSettings.CheckboxSetting RevivalIcon 
        {
            get 
            {
                ModSettings.CheckboxSetting checkboxSetting = new ModSettings.CheckboxSetting 
                {
                    Name = "Role Revival Icon",
                    Description = "If enabled whenever a necromancer or retri in your team revives someone the role icon of the revived person will be shown visiting the target" +
                    "instead of the secondary ability icon.",
                    DefaultValue = false,
                    AvailableInGame = false,
                    Available = true
                };
                return checkboxSetting;

            }
        }

        public ModSettings.CheckboxSetting DayAbilityIcons 
        {
            get 
            {
                ModSettings.CheckboxSetting checkboxSetting = new ModSettings.CheckboxSetting 
                {
                    Name = "Day Ability Icons",
                    Description = "If enabled, icons will be added for your faction member's day abilities (Jailing, indoc, etc.)\n\nDoes not add icons for instant day abilities (Revealing, Meteoring, etc.)",
                    DefaultValue = true,
                    AvailableInGame = false,
                    Available = true
                };
                return checkboxSetting;

            }
        }

        private readonly List<string> DisplaySettings = new List<string>(3)
        {
            "Role Icon",
            "Ability Icon",
            "No Icon"
        };

        private readonly List<string> BookIconSettings = new List<string>(3)
        {
            "Replace Icon",
            "Add Icon",
            "No Icon"
        };

        private readonly List<string> SpecialAbilitySettings = new List<string>(3)
        {
            "Add Icon",
            "Replace Icon",
            "No Icon"
        };

        private readonly List<string> ShowOwnActionSettings = new List<string>(3)
        {
            "Never",
            "Only as Factional Evil",
            "Always"
        };

        private readonly List<string> HandleOverchargedSettings = new List<string>(3)
        {
            "Only Myself",
            "All Teammates (EXPERIMENTAL)",
            "Never"
        };
    }
}
