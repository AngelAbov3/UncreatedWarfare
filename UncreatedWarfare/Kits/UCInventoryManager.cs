﻿using Rocket.Unturned.Player;
using SDG.Unturned;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Uncreated.Warfare.Kits
{
    public static class UCInventoryManager
    {
        public static void GiveKitToPlayer(UnturnedPlayer player, Kit kit)
        {
            if (kit != null)
            {
                if (kit.ShouldClearInventory)
                {
                    ClearInventory(player);
                }
                foreach (KitClothing clothing in kit.Clothes)
                {
                    if (clothing.type == KitClothing.EClothingType.SHIRT)
                        player.Player.clothing.askWearShirt(clothing.ID, clothing.quality, Convert.FromBase64String(clothing.state), true);
                    if (clothing.type == KitClothing.EClothingType.PANTS)
                        player.Player.clothing.askWearPants(clothing.ID, clothing.quality, Convert.FromBase64String(clothing.state), true);
                    if (clothing.type == KitClothing.EClothingType.VEST)
                        player.Player.clothing.askWearVest(clothing.ID, clothing.quality, Convert.FromBase64String(clothing.state), true);
                    if (clothing.type == KitClothing.EClothingType.HAT)
                        player.Player.clothing.askWearHat(clothing.ID, clothing.quality, Convert.FromBase64String(clothing.state), true);
                    if (clothing.type == KitClothing.EClothingType.MASK)
                        player.Player.clothing.askWearMask(clothing.ID, clothing.quality, Convert.FromBase64String(clothing.state), true);
                    if (clothing.type == KitClothing.EClothingType.BACKPACK)
                        player.Player.clothing.askWearBackpack(clothing.ID, clothing.quality, Convert.FromBase64String(clothing.state), true);
                    if (clothing.type == KitClothing.EClothingType.GLASSES)
                        player.Player.clothing.askWearGlasses(clothing.ID, clothing.quality, Convert.FromBase64String(clothing.state), true);
                }

                foreach (KitItem k in kit.Items)
                {
                    Item item = new Item(k.ID, k.amount, k.quality);
                    item.metadata = Convert.FromBase64String(k.metadata);

                    if (!player.Inventory.tryAddItem(item, k.x, k.y, k.page, k.rotation))
                        player.Inventory.tryAddItem(item, true);
                }
            }
        }

        public static void ClearInventory(UnturnedPlayer player) => ClearInventory(player.Player.channel.owner);
        public static void ClearInventory(SteamPlayer player)
        {
            player.player.equipment.dequip();

            for (byte page = 0; page < PlayerInventory.PAGES - 1; page++)
            {
                if (page == PlayerInventory.AREA)
                    continue;

                var count = player.player.inventory.getItemCount(page);

                for (byte index = 0; index < count; index++)
                {
                    player.player.inventory.removeItem(page, 0);
                }
            }

            System.Action removeUnequipped = () => {
                for (byte i = 0; i < player.player.inventory.getItemCount(2); i++)
                {
                    player.player.inventory.removeItem(2, 0);
                }
            };

            player.player.clothing.askWearBackpack(0, 0, new byte[0], true);
            removeUnequipped();

            player.player.clothing.askWearGlasses(0, 0, new byte[0], true);
            removeUnequipped();

            player.player.clothing.askWearHat(0, 0, new byte[0], true);
            removeUnequipped();

            player.player.clothing.askWearPants(0, 0, new byte[0], true);
            removeUnequipped();

            player.player.clothing.askWearMask(0, 0, new byte[0], true);
            removeUnequipped();

            player.player.clothing.askWearShirt(0, 0, new byte[0], true);
            removeUnequipped();

            player.player.clothing.askWearVest(0, 0, new byte[0], true);
            removeUnequipped();

            player.player.equipment.ReceiveSlot(0, 0, new byte[0]);
            player.player.equipment.ReceiveSlot(1, 0, new byte[0]);
        }

        public static void RemoveNumberOfItemsFromStorage(InteractableStorage storage, ushort itemID, int amount)
        {
            int counter = 0;

            for (byte i = (byte)(storage.items.getItemCount() - 1); i >= 0; i--)
            {
                if (storage.items.getItem(i).item.id == itemID)
                {
                    counter++;
                    storage.items.removeItem(i);

                    if (counter == amount)
                        return;
                }
            }
        }

        public static int CountItems(UnturnedPlayer player, ushort itemID)
        {
            int count = 0;

            for (byte page = 0; page < PlayerInventory.PAGES - 1; page++)
            {
                var pageCount = player.Player.inventory.getItemCount(page);

                for (byte index = 0; index < pageCount; index++)
                {
                    if (player.Player.inventory.getItem(page, index).item.id == itemID)
                    {
                        count++;
                    }
                }
            }

            return count;
        }
    }
}
