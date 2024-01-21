using BepInEx;
using BepInEx.Bootstrap;
using BepInEx.Configuration;
using HarmonyLib;
using ServerSync;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace DeathTweaks
{

    [BepInPlugin(ModGUID, ModName, ModVersion)]
    public class DeathTweaksPlugin : BaseUnityPlugin
    {

        internal const string ModName = "DeathTweaks";
        internal const string ModVersion = "0.0.1";
        internal const string Author = "marlthon";
        private const string ModGUID = Author + "." + ModName;
        private static string ConfigFileName = ModGUID + ".cfg";
        
        public static ConfigEntry<bool> modEnabled;
        public static ConfigEntry<bool> isDebug;
        public static ConfigEntry<int> nexusID;

        public static ConfigEntry<bool> keepEquippedItems;
        public static ConfigEntry<bool> keepHotbarItems;
        public static ConfigEntry<bool> keepAllItems;
        public static ConfigEntry<bool> destroyAllItems;

        public static ConfigEntry<bool> useTombStone;
        public static ConfigEntry<bool> keepFoodLevels;
        public static ConfigEntry<bool> keepQuickSlotItems;
        public static ConfigEntry<bool> keepTeleportableItems;
        public static ConfigEntry<bool> createDeathEffects;
        public static ConfigEntry<bool> reduceSkills;
        public static ConfigEntry<bool> noSkillProtection;
        public static ConfigEntry<bool> useFixedSpawnCoordinates;
        public static ConfigEntry<bool> spawnAtStart;

        public static ConfigEntry<Vector3> fixedSpawnCoordinates;
        public static ConfigEntry<float> skillReduceFactor;

        public static ConfigEntry<string> keepItemTypes;
        public static ConfigEntry<string> dropItemTypes;
        public static ConfigEntry<string> destroyItemTypes;

        public static ConfigEntry<string> keepItemNames;
        public static ConfigEntry<string> dropItemNames;
        public static ConfigEntry<string> destroyItemNames;

        private static DeathTweaksPlugin context;
        private static List<string> typeEnums = new List<string>();

        private static Assembly quickSlotsAssembly;
        public static void Dbgl(string str = "", bool pref = true)
        {
            if (isDebug.Value)
                Debug.Log((pref ? typeof(DeathTweaksPlugin).Namespace + " " : "") + str);
        }

        private readonly Harmony _harmony = new(ModGUID);

        private static readonly ConfigSync ConfigSync = new(ModGUID)
        { DisplayName = ModName, CurrentVersion = ModVersion, MinimumRequiredVersion = ModVersion };

        // Deus Seja Louvado!
                

        public void Awake()
        {
            // Needed for ServerSync to add locking of config toggle
            _serverConfigLocked = config("General", "Force Server Config", true, "Force Server Config");
            _ = ConfigSync.AddLockingConfigEntry(_serverConfigLocked);

            modEnabled = config("General", "Keep Equiped Items", true, "Keep Equiped Items");
                        
            keepEquippedItems = config("General", "Keep Equiped Items", true, "Keep Equiped Items");
            keepHotbarItems = config("General", "Keep Hotbar Items", true, "Keep Hotbar Items");
            keepAllItems = config("General", "Keep All Items", true, "Keep All Items");
            destroyAllItems = config("General", "Destroy All Items", true, "Destroy All Items");

            useTombStone = config("General", "Use Tomb Stone", true, "Use Tomb Stone");
            keepFoodLevels = config("General", "Keep Food Levels", true, "Keep Food Levels");
            keepQuickSlotItems = config("General", "Keep Quick Slot Items", true, "Keep Quick Slot Items");
            keepTeleportableItems = config("General", "Keep Teleportable Items", true, "Keep Teleportable Items");
            createDeathEffects = config("General", "Keep Equiped Items", true, "Keep Equiped Items");
            reduceSkills = config("General", "Reduce Skills", true, "Reduce Skills");
            noSkillProtection = config("General", "No Skill Protection", true, "No Skill Protection");
            useFixedSpawnCoordinates = config("General", "Use Fixed Spawn Coordinates", true, "Use Fixed Spawn Coordinates");

            spawnAtStart = config("General", "Spawn At Start", true, "Spawn At Start");
            fixedSpawnCoordinates = config("General", "Fixed Spawn Coordinates", new Vector3(0, 0, 0), "Fixed Spawn Coordinates");
            skillReduceFactor = config("General", "Skill Reduce Factor", 0f, "Skill Reduce Factor");
            keepItemTypes = config("General", "Keep Item Types", "", "Keep Item Types");
            dropItemTypes = config("General", "Drop Item Types", "", "Drop Item Types");
            destroyItemTypes = config("General", "Destroy  Item Types", "", "Destroy  Item Types");
            keepItemNames = config("General", "Keep Item Names", "", "Keep Item Names");
            dropItemNames = config("General", "Drop Item Names", "", "Drop Item Names");
            destroyItemNames = config("General", "Destroy Item Names", "", "Destroy Item Names");
                        

        }
               

        private void Start()
        {
            if (Chainloader.PluginInfos.ContainsKey("randyknapp.mods.equipmentandquickslots"))
                quickSlotsAssembly = Chainloader.PluginInfos["randyknapp.mods.equipmentandquickslots"].Instance.GetType().Assembly;

        }

        class InventoryInfos { 
            public List<ItemDrop.ItemData> drop_list;
            public Inventory inventory;

            public InventoryInfos(Inventory i, List<ItemDrop.ItemData> d ) {
                drop_list = d;
                inventory = i;
            }
        }

        [HarmonyPatch(typeof(Player), "OnDeath")]
        [HarmonyPriority(Priority.First)]
        static class OnDeath_Patch
        {
            static bool Prefix(Player __instance, Inventory ___m_inventory, ref float ___m_timeSinceDeath, float ___m_hardDeathCooldown, ZNetView ___m_nview, List<Player.Food> ___m_foods, Skills ___m_skills, SEMan ___m_seman, HitData ___m_lastHit)
            {
                if (!modEnabled.Value)
                    return true;

                ___m_nview.GetZDO().Set("dead", true);
                ___m_nview.InvokeRPC(ZNetView.Everybody, "OnDeath", new object[] { });
                Game.instance.IncrementPlayerStat(PlayerStatType.Deaths, 1f);
                switch (___m_lastHit.m_hitType)
                {
                    case HitData.HitType.Undefined:
                        Game.instance.IncrementPlayerStat(PlayerStatType.DeathByUndefined, 1f);
                        break;
                    case HitData.HitType.EnemyHit:
                        Game.instance.IncrementPlayerStat(PlayerStatType.DeathByEnemyHit, 1f);
                        break;
                    case HitData.HitType.PlayerHit:
                        Game.instance.IncrementPlayerStat(PlayerStatType.DeathByPlayerHit, 1f);
                        break;
                    case HitData.HitType.Fall:
                        Game.instance.IncrementPlayerStat(PlayerStatType.DeathByFall, 1f);
                        break;
                    case HitData.HitType.Drowning:
                        Game.instance.IncrementPlayerStat(PlayerStatType.DeathByDrowning, 1f);
                        break;
                    case HitData.HitType.Burning:
                        Game.instance.IncrementPlayerStat(PlayerStatType.DeathByBurning, 1f);
                        break;
                    case HitData.HitType.Freezing:
                        Game.instance.IncrementPlayerStat(PlayerStatType.DeathByFreezing, 1f);
                        break;
                    case HitData.HitType.Poisoned:
                        Game.instance.IncrementPlayerStat(PlayerStatType.DeathByPoisoned, 1f);
                        break;
                    case HitData.HitType.Water:
                        Game.instance.IncrementPlayerStat(PlayerStatType.DeathByWater, 1f);
                        break;
                    case HitData.HitType.Smoke:
                        Game.instance.IncrementPlayerStat(PlayerStatType.DeathBySmoke, 1f);
                        break;
                    case HitData.HitType.EdgeOfWorld:
                        Game.instance.IncrementPlayerStat(PlayerStatType.DeathByEdgeOfWorld, 1f);
                        break;
                    case HitData.HitType.Impact:
                        Game.instance.IncrementPlayerStat(PlayerStatType.DeathByImpact, 1f);
                        break;
                    case HitData.HitType.Cart:
                        Game.instance.IncrementPlayerStat(PlayerStatType.DeathByCart, 1f);
                        break;
                    case HitData.HitType.Tree:
                        Game.instance.IncrementPlayerStat(PlayerStatType.DeathByTree, 1f);
                        break;
                    case HitData.HitType.Self:
                        Game.instance.IncrementPlayerStat(PlayerStatType.DeathBySelf, 1f);
                        break;
                    case HitData.HitType.Structural:
                        Game.instance.IncrementPlayerStat(PlayerStatType.DeathByStructural, 1f);
                        break;
                    case HitData.HitType.Turret:
                        Game.instance.IncrementPlayerStat(PlayerStatType.DeathByTurret, 1f);
                        break;
                    case HitData.HitType.Boat:
                        Game.instance.IncrementPlayerStat(PlayerStatType.DeathByBoat, 1f);
                        break;
                    case HitData.HitType.Stalagtite:
                        Game.instance.IncrementPlayerStat(PlayerStatType.DeathByStalagtite, 1f);
                        break;
                    default:
                        ZLog.LogWarning("Not implemented death type " + ___m_lastHit.m_hitType.ToString());
                        break;
                }
                Game.instance.GetPlayerProfile().SetDeathPoint(__instance.transform.position);

                if (createDeathEffects.Value)
                    Traverse.Create(__instance).Method("CreateDeathEffects").GetValue();

                List<InventoryInfos> drop_inventorys = new List<InventoryInfos>();
                

                if (!keepAllItems.Value)
                {

                    //List<Inventory> inventories = new List<Inventory>();

                    if (quickSlotsAssembly != null)
                    {
                        var extendedInventory = quickSlotsAssembly.GetType("EquipmentAndQuickSlots.InventoryExtensions").GetMethod("Extended", BindingFlags.Public | BindingFlags.Static).Invoke(null, new object[] { ___m_inventory });
                        foreach(var inventory in (List<Inventory>)quickSlotsAssembly.GetType("EquipmentAndQuickSlots.ExtendedInventory").GetField("_inventories", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(extendedInventory)) {
                            drop_inventorys.Add(new InventoryInfos(inventory , new List<ItemDrop.ItemData>()));
                        }
                    }
                    else
                    {
                        drop_inventorys.Add(new InventoryInfos( ___m_inventory, new List<ItemDrop.ItemData>() ));
                    }

                    var keepItemTypeArray = string.IsNullOrEmpty(keepItemTypes.Value) ? new string[0] : keepItemTypes.Value.Split(',');
                    var keepItemNameArray = string.IsNullOrEmpty(keepItemNames.Value) ? new string[0] : keepItemNames.Value.Split(',');
                    var destroyItemTypeArray = string.IsNullOrEmpty(destroyItemTypes.Value) ? new string[0] : destroyItemTypes.Value.Split(',');
                    var destroyItemNameArray = string.IsNullOrEmpty(destroyItemNames.Value) ? new string[0] : destroyItemNames.Value.Split(',');
                    var dropItemTypeArray = string.IsNullOrEmpty(dropItemTypes.Value) ? new string[0] : dropItemTypes.Value.Split(',');
                    var dropItemNameArray = string.IsNullOrEmpty(dropItemNames.Value) ? new string[0] : dropItemNames.Value.Split(',');

                    for (int inv_num = 0; inv_num < drop_inventorys.Count; inv_num++)
                    {
                        Inventory inv = drop_inventorys[inv_num].inventory;
                        List<ItemDrop.ItemData> dropItems = drop_inventorys[inv_num].drop_list;

                        Dbgl($"  inventory {inv_num}");

                        var items2 = inv.GetAllItems();
                        for (int i2 = items2.Count - 1; i2 >= 0; i2--)
                        {
                            var item = items2[i2];
                            Dbgl($"   Item  Name: {item.m_dropPrefab.name}   Cat: {item.m_shared.m_itemType}");
                        }

                        if (quickSlotsAssembly != null && keepQuickSlotItems.Value && inv == (Inventory)quickSlotsAssembly.GetType("EquipmentAndQuickSlots.PlayerExtensions").GetMethod("GetQuickSlotInventory", BindingFlags.Public | BindingFlags.Static).Invoke(null, new object[] { __instance }))
                        {
                            Dbgl("Skipping quick slot inventory");
                            continue;
                        }

                        List<ItemDrop.ItemData> keepItems = Traverse.Create(inv).Field("m_inventory").GetValue<List<ItemDrop.ItemData>>();

                        if (destroyAllItems.Value)
                            keepItems.Clear();
                        else
                        {

                            for(int j = keepItems.Count - 1; j >= 0; j--)
                            {
                                ItemDrop.ItemData item = keepItems[j];

                                if (keepEquippedItems.Value && item.m_equipped)
                                    continue;

                                if (keepHotbarItems.Value && inv.GetName() == ___m_inventory.GetName() && item.m_gridPos.y == 0)
                                    continue;

                                if (item.m_shared.m_questItem)
                                    continue;

                                if (destroyItemTypeArray.Contains(Enum.GetName(typeof(ItemDrop.ItemData.ItemType), item.m_shared.m_itemType)))
                                {
                                    keepItems.RemoveAt(j);
                                    continue;
                                }

                                if (destroyItemNameArray.Contains(item.m_dropPrefab.name))
                                {
                                    keepItems.RemoveAt(j);
                                    continue;
                                }

                                if (keepItemTypeArray.Contains(Enum.GetName(typeof(ItemDrop.ItemData.ItemType), item.m_shared.m_itemType)))
                                    continue;

                                if (keepItemNameArray.Contains(item.m_dropPrefab.name))
                                    continue;


                                if (dropItemTypeArray.Contains(Enum.GetName(typeof(ItemDrop.ItemData.ItemType), item.m_shared.m_itemType)))
                                {
                                    dropItems.Add(item);
                                    keepItems.RemoveAt(j);
                                    continue;
                                }

                                if (dropItemNameArray.Contains(item.m_dropPrefab.name))
                                {
                                    dropItems.Add(item);
                                    keepItems.RemoveAt(j);
                                    continue;
                                }
                                if(item.m_shared.m_teleportable && keepTeleportableItems.Value)
                                {
                                    continue;
                                }
                                dropItems.Add(item);
                                keepItems.RemoveAt(j);
                            }
                        }
                        Traverse.Create(inv).Method("Changed").GetValue();
                    }
                }

                /*
                 * with the EquipmentAndQuickSlots Mod we need a custom Tombstone for the Quick and Eqipment Slots
                 * otherwitse the items are lost if the Tombstone is collected after quiting the game
                 * 
                 * The Items in the special slots have to be marked with item.m_customData to detect in which slot they have to inserted 
                 * 
                 */

                for (int inv_num = 0; inv_num < drop_inventorys.Count; inv_num++)
                {
                    Inventory inv = drop_inventorys[inv_num].inventory;
                    List<ItemDrop.ItemData> dropItems = drop_inventorys[inv_num].drop_list;

                    if (useTombStone.Value && dropItems.Any())
                    {
                        Dbgl("    dropItems.Any");

                        
                        Vector3 position = __instance.GetCenterPoint() + Vector3.left * (inv_num * 2 );

                        GameObject gameObject = Instantiate(__instance.m_tombstone, position , __instance.transform.rotation);
                        gameObject.GetComponent<Container>().GetInventory().RemoveAll();


                        int width = Traverse.Create(inv).Field("m_width").GetValue<int>();
                        int height = Traverse.Create(inv).Field("m_height").GetValue<int>();
                        Traverse.Create(gameObject.GetComponent<Container>().GetInventory()).Field("m_width").SetValue(width);
                        Traverse.Create(gameObject.GetComponent<Container>().GetInventory()).Field("m_height").SetValue(height);


                        

                        TombStone component = gameObject.GetComponent<TombStone>();
                        PlayerProfile playerProfile = Game.instance.GetPlayerProfile();
                        switch( inv_num)
                        {
                            case 0:
                                component.Setup(playerProfile.GetName(), playerProfile.GetPlayerID());
                                break;
                            case 1:
                                component.Setup(playerProfile.GetName() + "- Quickslots", playerProfile.GetPlayerID());
                                foreach (var item in dropItems)
                                {
                                    var oldSlot = item.m_gridPos;
                                    item.m_customData["eaqs-qs"] = $"{oldSlot.x},{oldSlot.y}";
                                    Dbgl($"   Quickslot Item  Name: {item.m_dropPrefab.name}   Cat: {item.m_shared.m_itemType}");
                                }
                                break;
                            case 2:
                                component.Setup(playerProfile.GetName() + "- Eqipment", playerProfile.GetPlayerID());
                                foreach ( var item in dropItems)
                                {
                                    item.m_customData["eaqs-e"] = "1";
                                    Dbgl($"   Eqipment Item  Name: {item.m_dropPrefab.name}   Cat: {item.m_shared.m_itemType}");
                                }
                                

                                break;
                        }

                        Traverse.Create(gameObject.GetComponent<Container>().GetInventory()).Field("m_inventory").SetValue(dropItems);
                        Traverse.Create(gameObject.GetComponent<Container>().GetInventory()).Method("Changed").GetValue();


                    }
                    else
                    {

                        Dbgl("   !! dropItems.Any");


                        foreach (ItemDrop.ItemData item in dropItems)
                        {

                            Dbgl($"       Item : {item.m_dropPrefab.name}");

                            Vector3 position = __instance.transform.position + Vector3.up * 0.5f + UnityEngine.Random.insideUnitSphere * 0.3f;
                            Quaternion rotation = Quaternion.Euler(0f, (float)UnityEngine.Random.Range(0, 360), 0f);
                            ItemDrop.DropItem(item, 0, position, rotation);
                        }
                    }

                }

                if (!keepFoodLevels.Value)
                    ___m_foods.Clear();

                bool hardDeath = noSkillProtection.Value || ___m_timeSinceDeath > ___m_hardDeathCooldown;

                if (hardDeath && reduceSkills.Value)
                {
                    ___m_skills.LowerAllSkills(skillReduceFactor.Value);
                }

                ___m_seman.RemoveAllStatusEffects(false);
                Game.instance.RequestRespawn(10f);
                ___m_timeSinceDeath = 0;

                if (!hardDeath)
                {
                    __instance.Message(MessageHud.MessageType.TopLeft, "$msg_softdeath", 0, null);
                }
                __instance.Message(MessageHud.MessageType.Center, "$msg_youdied", 0, null);
                __instance.ShowTutorial("death", false);
                Minimap.instance.AddPin(__instance.transform.position, Minimap.PinType.Death, string.Format("$hud_mapday {0}", EnvMan.instance.GetDay(ZNet.instance.GetTimeSeconds())), true, false, 0L);

                if (__instance.m_onDeath != null)
                {
                    __instance.m_onDeath();
                }

                string eventLabel = "biome:" + __instance.GetCurrentBiome().ToString();
                Gogan.LogEvent("Game", "Death", eventLabel, 0L);

                return false;
            }

        }

        [HarmonyPatch(typeof(Player), "HardDeath")]
        static class HardDeath_Patch
        {
            static bool Prefix(ref bool __result)
            {
                if (!modEnabled.Value || !noSkillProtection.Value)
                    return true;
                __result = true;
                return false;
            }
        }

        [HarmonyPatch(typeof(Game), "FindSpawnPoint")]
        static class FindSpawnPoint_Patch
        {
            static bool Prefix(ref Vector3 point, ref bool usedLogoutPoint, bool ___m_firstSpawn, ref bool __result)
            {
                if (!modEnabled.Value || ___m_firstSpawn)
                    return true;

                if (spawnAtStart.Value)
                {
                    usedLogoutPoint = false;

                    Vector3 a;
                    if (ZoneSystem.instance.GetLocationIcon(Game.instance.m_StartLocation, out a))
                    {
                        point = a + Vector3.up * 2f;
                        ZNet.instance.SetReferencePosition(point);
                        __result = ZNetScene.instance.IsAreaReady(point);
                        if(__result)
                            Dbgl($"respawning at start: {point}");
                    }
                    else
                    {
                        Dbgl("start point not found");
                        ZNet.instance.SetReferencePosition(Vector3.zero);
                        point = Vector3.zero;
                        __result = false;
                    }
                    return false;
                }
                else if (useFixedSpawnCoordinates.Value)
                {
                    usedLogoutPoint = false;

                    point = fixedSpawnCoordinates.Value;
                    ZNet.instance.SetReferencePosition(point);
                    __result = ZNetScene.instance.IsAreaReady(point);
                    if(__result)
                        Dbgl($"respawning at custom point {point}");
                    return false;

                }
                return true;
            }
        }

        [HarmonyPatch(typeof(Skills), "OnDeath")]
        static class Skills_OnDeath_Patch
        {
            static bool Prefix()
            {
                if (!modEnabled.Value)
                    return true;
                return reduceSkills.Value;
            }
        }

        private static ConfigEntry<bool>? _serverConfigLocked;

        private ConfigEntry<T> config<T>(string group, string name, T value, ConfigDescription description,
            bool synchronizedSetting = true)
        {
            ConfigDescription extendedDescription =
                new(
                    description.Description +
                    (synchronizedSetting ? " [Synced with Server]" : " [Not Synced with Server]"),
                    description.AcceptableValues, description.Tags);
            ConfigEntry<T> configEntry = Config.Bind(group, name, value, extendedDescription);

            SyncedConfigEntry<T> syncedConfigEntry = ConfigSync.AddConfigEntry(configEntry);
            syncedConfigEntry.SynchronizedConfig = synchronizedSetting;

            return configEntry;
        }

        private ConfigEntry<T> config<T>(string group, string name, T value, string description,
            bool synchronizedSetting = true)
        {
            return config(group, name, value, new ConfigDescription(description), synchronizedSetting);
        }

        private class ConfigurationManagerAttributes
        {
            public bool? Browsable = false;
        }

        [HarmonyPatch(typeof(Terminal), "InputText")]
        static class InputText_Patch
        {
            static bool Prefix(Terminal __instance)
            {
                if (!modEnabled.Value)
                    return true;
                string text = __instance.m_input.text;
                if (text.ToLower().Equals($"{typeof(DeathTweaksPlugin).Namespace.ToLower()} reset"))
                {
                    context.Config.Reload();
                    context.Config.Save();

                    __instance.AddString(text);
                    __instance.AddString($"{context.Info.Metadata.Name} config reloaded");
                    return false;
                }
                return true;
            }
        }
    }
}
