using System;
using BepInEx;
using BepInEx.Logging;
using HarmonyLib;

namespace EarlyWarp
{
    [BepInPlugin(PluginGuid, PluginName, PluginVersion)]
    public sealed class EarlyWarpPlugin : BaseUnityPlugin
    {
        public const string PluginGuid = "sinthome358.dsp.earlywarp";
        public const string PluginName = "Early Warp";
        public const string PluginVersion = "1.0.0";

        private const int GravitonRefractionTechId = 1704;
        private const int LogisticsCarrierEngine4TechId = 3404;
        private const int LogisticsVesselWarpDriveFunctionId = 17;
        private const double LogisticsVesselWarpDriveFunctionValue = 1.0;
        private const int MaxWarperCount = 50;

        private static ManualLogSource _logger;
        private static bool _techDefinitionsMigrated;
        private Harmony _harmony;

        private void Awake()
        {
            _logger = Logger;
            _harmony = new Harmony(PluginGuid);

            try
            {
                _harmony.PatchAll();
                _logger.LogInfo(
                    PluginName + " " + PluginVersion +
                    " loaded; warp slot migration target: " +
                    GravitonRefractionTechId + " <- function " +
                    LogisticsVesselWarpDriveFunctionId + " from " +
                    LogisticsCarrierEngine4TechId + ".");
            }
            catch (Exception exception)
            {
                _logger.LogError("Failed to apply Harmony patches: " + exception);
            }
        }

        private void OnDestroy()
        {
            if (_harmony != null)
            {
                _harmony.UnpatchSelf();
                _harmony = null;
            }
        }

        internal static void ApplyTechDefinitionMigration()
        {
            if (_techDefinitionsMigrated)
            {
                return;
            }

            try
            {
                if (LDB.techs == null)
                {
                    _logger.LogError("TechProtoSet is unavailable; tech migration was not applied.");
                    return;
                }

                TechProto warpTech = LDB.techs.Select(GravitonRefractionTechId);
                TechProto engineTech = LDB.techs.Select(LogisticsCarrierEngine4TechId);
                if (warpTech == null || engineTech == null)
                {
                    _logger.LogError(
                        "Could not find required tech protos: " +
                        GravitonRefractionTechId + "=" + (warpTech != null) +
                        ", " + LogisticsCarrierEngine4TechId + "=" + (engineTech != null) + ".");
                    return;
                }

                string engineBefore = TechDefinitionMigration.DescribeFunctions(engineTech);
                string warpBefore = TechDefinitionMigration.DescribeFunctions(warpTech);
                bool changed = TechDefinitionMigration.MoveWarpUnlock(
                    warpTech,
                    engineTech,
                    LogisticsVesselWarpDriveFunctionId,
                    LogisticsVesselWarpDriveFunctionValue);

                if (changed)
                {
                    LDB.techs.Signature = ProtoSignature_0_10_30_3100.CalculateSignature(LDB.techs);
                }

                _techDefinitionsMigrated = true;
                _logger.LogInfo(
                    "Tech migration complete; changed=" + changed +
                    "; warp=" + warpBefore + " -> " +
                    TechDefinitionMigration.DescribeFunctions(warpTech) +
                    "; engine4=" + engineBefore + " -> " +
                    TechDefinitionMigration.DescribeFunctions(engineTech) + ".");
            }
            catch (Exception exception)
            {
                _logger.LogError("Failed to migrate tech definitions: " + exception);
            }
        }

        internal static void ApplyLoadedSaveCompatibility(GameData gameData)
        {
            try
            {
                if (gameData == null || gameData.history == null)
                {
                    return;
                }

                if (!gameData.history.TechUnlocked(GravitonRefractionTechId))
                {
                    return;
                }

                bool restoredWarpDrive = false;
                if (!gameData.history.logisticShipWarpDrive)
                {
                    gameData.history.logisticShipWarpDrive = true;
                    restoredWarpDrive = true;
                }

                int repairedStationCount = EnsureExistingStationWarperSlots(gameData.galacticTransport);
                if (restoredWarpDrive || repairedStationCount > 0)
                {
                    _logger.LogInfo(
                        "Loaded save compatibility applied for tech " + GravitonRefractionTechId +
                        "; warp drive restored=" + restoredWarpDrive +
                        "; station slots repaired=" + repairedStationCount + ".");
                }
            }
            catch (Exception exception)
            {
                _logger.LogError("Failed to restore warp state for a loaded save: " + exception);
            }
        }

        private static int EnsureExistingStationWarperSlots(GalacticTransport transport)
        {
            if (transport == null || transport.stationPool == null)
            {
                return 0;
            }

            int repairedCount = 0;
            int stationCount = Math.Min(transport.stationCursor, transport.stationPool.Length);
            for (int i = 1; i < stationCount; i++)
            {
                StationComponent station = transport.stationPool[i];
                if (station == null ||
                    station.gid != i ||
                    !station.isStellar ||
                    station.isCollector ||
                    station.isVeinCollector ||
                    station.warperMaxCount >= MaxWarperCount)
                {
                    continue;
                }

                station.warperMaxCount = MaxWarperCount;
                repairedCount++;
            }

            return repairedCount;
        }
    }

    [HarmonyPatch(typeof(VFPreload), "InvokeOnLoadWorkEnded")]
    internal static class VFPreloadPatch
    {
        [HarmonyPostfix]
        [HarmonyPriority(Priority.Last)]
        private static void Postfix()
        {
            EarlyWarpPlugin.ApplyTechDefinitionMigration();
        }
    }

    [HarmonyPatch(typeof(GameData), nameof(GameData.Import))]
    internal static class GameDataImportPatch
    {
        [HarmonyPostfix]
        [HarmonyPriority(Priority.Last)]
        private static void Postfix(GameData __instance)
        {
            EarlyWarpPlugin.ApplyLoadedSaveCompatibility(__instance);
        }
    }
}
