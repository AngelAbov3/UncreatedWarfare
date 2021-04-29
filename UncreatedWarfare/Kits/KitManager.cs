﻿using Rocket.Unturned.Player;
using SDG.Unturned;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UncreatedWarfare.Kits
{
    public class KitManager : JSONSaver<Kit>
    {
        public Dictionary<UnturnedPlayer, Kit> ActiveKits;

        public KitManager()
            : base(UCWarfare.KitsStorage + "kits.json")
        {
            ActiveKits = new Dictionary<UnturnedPlayer, Kit>();
        }

        public void CreateKit(string kitName, List<KitItem> items, List<KitClothing> clothes) => AddObjectToSave(new Kit(kitName, items, clothes));
        public void DeleteKit(string kitName) => RemoveFromSaveWhere(k => k.Name.ToLower() == kitName.ToLower());
        public void DeleteAllKits() => RemoveAllObjectsFromSave();
        public void GetKitsWhere(Func<Kit, bool> predicate) => GetObjectsWhere(predicate);
        public bool KitExists(string kitName, out Kit kit)
        {
            bool result = ObjectExists(i => i.Name.ToLower() == kitName.ToLower(), out var k);
            kit = k;
            return result;
        }
        public bool OverwriteKitItems(string kitName, List<KitItem> newItems, List<KitClothing> newClothes)
        {
            var kits = GetExistingObjects();
            foreach (var kit in kits)
            {
                if (kit.Name.ToLower() == kitName.ToLower())
                {
                    kit.Items = newItems;
                    kit.Clothes = newClothes;
                    OverwriteSavedList(kits);
                    return true;
                }
            }
            return false;
        }
        public bool SetProperty(string kitName, object property, object newValue, out bool propertyIsValid, out bool kitExists, out bool argIsValid)
        {
            propertyIsValid = false;
            kitExists = false;
            argIsValid = false;

            if (!IsPropertyValid<Kit.EKitProperty>(property, out var p))
            {
                return false;
            }
            propertyIsValid = true;

            var kits = GetExistingObjects();
            foreach (var kit in kits)
            {
                if (kit.Name.ToLower() == kitName.ToLower())
                {
                    kitExists = true;

                    switch (property)
                    {
                        case Kit.EKitProperty.CLASS:
                            if (Enum.TryParse<Kit.EClass>(newValue.ToString().ToUpper(), out var kitclass))
                            {
                                kit.Class = kitclass;
                                argIsValid = true;
                            }
                            break;
                        case Kit.EKitProperty.BRANCH:
                            if (Enum.TryParse<Kit.EBranch>(newValue.ToString().ToUpper(), out var branch))
                            {
                                kit.Branch = branch;
                                argIsValid = true;
                            }
                            break;
                        case Kit.EKitProperty.TEAM:

                            if (UInt64.TryParse(newValue.ToString(), out var team))
                            {
                                kit.Team = team;
                                argIsValid = true;
                            }
                            break;
                        case Kit.EKitProperty.COST:
                            if (UInt16.TryParse(newValue.ToString(), out var cost))
                            {
                                kit.Cost = cost;
                                argIsValid = true;
                            }
                            break;
                        case Kit.EKitProperty.LEVEL:
                            if (UInt16.TryParse(newValue.ToString(), out var level))
                            {
                                kit.RequiredLevel = level;
                                argIsValid = true;
                            }
                            break;
                        case Kit.EKitProperty.TICKETS:
                            if (UInt16.TryParse(newValue.ToString(), out var tickets))
                            {
                                kit.TicketCost = tickets;
                                argIsValid = true;
                            }
                            break;
                        case Kit.EKitProperty.PREMIUM:
                            if (Boolean.TryParse(newValue.ToString(), out var ispremium))
                            {
                                kit.IsPremium = ispremium;
                                argIsValid = true;
                            }
                            break;
                        case Kit.EKitProperty.CLEARINV:
                            if (Boolean.TryParse(newValue.ToString(), out var clearinv))
                            {
                                kit.ShouldClearInventory = clearinv;
                                argIsValid = true;
                            }
                            break;
                    }
                    if (argIsValid)
                    {
                        OverwriteSavedList(kits);
                        return true;
                    }
                }
            }
            return false;
        }
        public List<KitItem> ItemsFromInventory(UnturnedPlayer player)
        {
            var items = new List<KitItem>();

            for (byte page = 0; page < PlayerInventory.PAGES - 1; page++)
            {
                for (byte i = 0; i < player.Inventory.getItemCount(page); i++)
                {
                    ItemJar jar = player.Inventory.getItem(page, i);

                    items.Add(new KitItem(
                        jar.item.id,
                        jar.x,
                        jar.y,
                        jar.rot,
                        jar.item.quality,
                        System.Convert.ToBase64String(jar.item.metadata),
                        jar.item.amount,
                        page
                    ));
                }
            }

            return items;
        }
        public List<KitClothing> ClothesFromInventory(UnturnedPlayer player)
        {
            PlayerClothing playerClothes = player.Player.clothing;

            var clothes = new List<KitClothing>();

            clothes.Add(new KitClothing(playerClothes.shirt, playerClothes.shirtQuality, Convert.ToBase64String(playerClothes.shirtState), KitClothing.EClothingType.SHIRT));
            clothes.Add(new KitClothing(playerClothes.pants, playerClothes.pantsQuality, Convert.ToBase64String(playerClothes.pantsState), KitClothing.EClothingType.PANTS));
            clothes.Add(new KitClothing(playerClothes.vest, playerClothes.vestQuality, Convert.ToBase64String(playerClothes.vestState), KitClothing.EClothingType.VEST));
            clothes.Add(new KitClothing(playerClothes.hat, playerClothes.hatQuality, Convert.ToBase64String(playerClothes.hatState), KitClothing.EClothingType.HAT));
            clothes.Add(new KitClothing(playerClothes.mask, playerClothes.maskQuality, Convert.ToBase64String(playerClothes.maskState), KitClothing.EClothingType.MASK));
            clothes.Add(new KitClothing(playerClothes.backpack, playerClothes.backpackQuality, Convert.ToBase64String(playerClothes.backpackState), KitClothing.EClothingType.BACKPACK));
            clothes.Add(new KitClothing(playerClothes.glasses, playerClothes.glassesQuality, Convert.ToBase64String(playerClothes.glassesState), KitClothing.EClothingType.GLASSES));

            return clothes;
        }

        public bool HasAccess(ulong playerID, string kitName)
        {
            var kits = GetExistingObjects();
            foreach (var kit in kits)
            {
                if (kit.Name.ToLower() == kitName.ToLower())
                    return kit.AllowedUsers.Contains(playerID);
            }
            return false;
        }
        public List<Kit> GetAccessibleKits(ulong playerID)
        {
            return GetExistingObjects().Where(kit => kit.AllowedUsers.Contains(playerID)).ToList();
        }
        public void GiveAccess(ulong playerID, string kitName)
        {
            var kits = GetExistingObjects();
            foreach (var kit in kits)
            {
                if (kit.Name.ToLower() == kitName.ToLower())
                {
                    if (!kit.AllowedUsers.Contains(playerID))
                    {
                        kit.AllowedUsers.Add(playerID);
                        OverwriteSavedList(kits);
                        return;
                    }
                }
            }
        }
        public void RemoveAccess(ulong playerID, string kitName)
        {
            var kits = GetExistingObjects();
            foreach (var kit in kits)
            {
                if (kit.Name == kitName.ToLower())
                {
                    kit.AllowedUsers.RemoveAll(id => id == playerID);
                    OverwriteSavedList(kits);
                    return;
                }
            }
        }
    }

    public class Kit
    {
        public string Name;
        public string DisplayName
        {
            get
            {
                switch (this.Class)
                {
                    case EClass.UNARMED:
                        return "Unarmed";
                    case EClass.SQUADLEADER:
                        return "Squad Leader";
                    case EClass.RIFLEMAN:
                        return "Rifleman";
                    case EClass.MEDIC:
                        return "Medic";
                    case EClass.BREACHER:
                        return "Breacher";
                    case EClass.AUTOMATIC_RIFLEMAN:
                        return "Automatic Rifleman";
                    case EClass.MACHINE_GUNNER:
                        return "Machine Gunner";
                    case EClass.LAT:
                        return "Light Anti-Tank";
                    case EClass.HAT:
                        return "Heavy Anti-Tank";
                    case EClass.MARKSMAN:
                        return "Designated Marksman";
                    case EClass.SNIPER:
                        return "Sniper";
                    case EClass.AP_RIFLEMAN:
                        return "Anti-Personel Rifleman";
                    case EClass.COMBAT_ENGINEER:
                        return "Combat Engineer";
                    case EClass.CREWMAN:
                        return "Crewman";
                    case EClass.PILOT:
                        return "Pilot";

                }
                return Name;
            }
        }
        public EClass Class;
        public EBranch Branch;
        public ulong Team;
        public ushort Cost;
        public ushort RequiredLevel;
        public ushort TicketCost;
        public bool IsPremium;
        public bool ShouldClearInventory;
        public List<KitItem> Items;
        public List<KitClothing> Clothes;
        public List<ulong> AllowedUsers { get; protected set; }

        public Kit(string name, List<KitItem> items, List<KitClothing> clothes)
        {
            Name = name;
            Items = items;
            Clothes = clothes;
            Class = EClass.NONE;
            Branch = EBranch.DEFAULT;
            Team = 0;
            Cost = 0;
            RequiredLevel = 0;
            TicketCost = 0;
            IsPremium = false;
            ShouldClearInventory = true;
        }

        public enum EClothingType
        {
            SHIRT,
            PANTS,
            VEST,
            HAT,
            MASK,
            BACKPACK,
            GLASSES
        }
        public enum EBranch
        {
            DEFAULT,
            INFANTRY,
            ARMOR,
            AIRFORCE,
            SPECOPS
        }
        public enum EClass
        {
            NONE,
            UNARMED,
            SQUADLEADER,
            RIFLEMAN,
            MEDIC,
            BREACHER,
            AUTOMATIC_RIFLEMAN,
            MACHINE_GUNNER,
            LAT,
            HAT,
            MARKSMAN,
            SNIPER,
            AP_RIFLEMAN,
            COMBAT_ENGINEER,
            CREWMAN,
            PILOT
        }

         public enum EKitProperty
        {
            CLASS,
            BRANCH,
            TEAM,
            COST,
            LEVEL,
            TICKETS,
            PREMIUM,
            CLEARINV
        }
    }
    public class KitItem
    {
        public ushort ID;
        public byte x;
        public byte y;
        public byte rotation;
        public byte quality;
        public string metadata;
        public byte amount;
        public byte page;

        public KitItem(ushort ID, byte x, byte y, byte rotation, byte quality, string metadata, byte amount, byte page)
        {
            this.ID = ID;
            this.x = x;
            this.y = y;
            this.rotation = rotation;
            this.quality = quality;
            this.metadata = metadata;
            this.amount = amount;
            this.page = page;
        }
    }
    public class KitClothing
    {
        public ushort ID;
        public byte quality;
        public string state;
        public EClothingType type;

        public KitClothing(ushort ID, byte quality, string state, EClothingType type)
        {
            this.ID = ID;
            this.quality = quality;
            this.state = state;
            this.type = type;
        }
        public enum EClothingType
        {
            SHIRT,
            PANTS,
            VEST,
            HAT,
            MASK,
            BACKPACK,
            GLASSES
        }
    }
}
