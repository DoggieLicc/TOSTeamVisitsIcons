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
                DictionaryExtensions.SetValue(Settings.SettingsCache, "Display Mode", ModSettings.GetString("Display Mode", "doggie.licc.factionvisits"));
                DictionaryExtensions.SetValue(Settings.SettingsCache, "Role Revival Icon", ModSettings.GetBool("Role Revival Icon", "doggie.licc.factionvisits"));
                DictionaryExtensions.SetValue(Settings.SettingsCache, "Book Icon", ModSettings.GetBool("Book Icon", "doggie.licc.factionvisits"));
                DictionaryExtensions.SetValue(Settings.SettingsCache, "Special Ability Icon", ModSettings.GetBool("Special Ability Icon", "doggie.licc.factionvisits"));
                DictionaryExtensions.SetValue(Settings.SettingsCache, "Show Own Actions", ModSettings.GetBool("Show Own Actions", "doggie.licc.factionvisits"));
                DictionaryExtensions.SetValue(Settings.SettingsCache, "Handle Overcharged", ModSettings.GetString("Handle Overcharged", "doggie.licc.factionvisits"));
            }
            catch (Exception ex)
            {
                Console.WriteLine("TOSTVI The rainbow faction crashed the mod. Contact pokegustavo. Error: " + ex.Message);
            }
            try 
            {
                Settings.fancyUI = Assembly.LoadFrom(Path.Combine(AppContext.BaseDirectory, "SalemModLoader\\Mods\\FancyUI.dll")); 
            }
            catch 
            {
                Console.WriteLine("TOSTVI Fancy UI was not found.");
            }
        }
    }

    [DynamicSettings]
    public class Settings
    {
        public static Assembly fancyUI = null;
        public static Dictionary<string, object> SettingsCache = new Dictionary<string, object>
        {
            {
                "Display Mode",
                "Role Icon"
            },
            {
                "Revival Icon",
                false
            },
            {
                "Book Icon",
                "Replace Icon"
            },
            {
                "Special Ability Icon",
                "Add Icon"
            },
            {
                "Show Own Actions",
                "Never"
            },
            {
                "Day Ability Icons",
                true
            },
            {
                "Handle Overcharged",
                "Only Myself"
            }
        };

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
                    Available = true,
                    OnChanged = delegate (string s)
                    {
                        DictionaryExtensions.SetValue(SettingsCache, "Display Mode", s);
                    }
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
                    Available = true,
                    OnChanged = delegate (string s)
                    {
                        DictionaryExtensions.SetValue(SettingsCache, "Book Icon", s);
                    }
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
                    Available = true,
                    OnChanged = delegate (string s)
                    {
                        DictionaryExtensions.SetValue(SettingsCache, "Special Ability Icon", s);
                    }
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
                    Available = true,
                    OnChanged = delegate (string s)
                    {
                        DictionaryExtensions.SetValue(SettingsCache, "Handle Overcharged", s);
                    }
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
                    Description = "Dictates whether icons will be added for your own actions",
                    Options = ShowOwnActionSettings,
                    AvailableInGame = false,
                    Available = true,
                    OnChanged = delegate (string s)
                    {
                        DictionaryExtensions.SetValue(SettingsCache, "Show Own Actions", s);
                    }
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
                    Available = true,
                    OnChanged = delegate (bool b)
                    {
                        DictionaryExtensions.SetValue(SettingsCache, "Role Revival Icon", b);
                    }
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
                    Description = "If enabled, icons will be added for your faction member's day abilities (Jailing, indoc, etc.)",
                    DefaultValue = true,
                    AvailableInGame = false,
                    Available = true,
                    OnChanged = delegate (bool b)
                    {
                        DictionaryExtensions.SetValue(SettingsCache, "Day Ability Icons", b);
                    }
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
