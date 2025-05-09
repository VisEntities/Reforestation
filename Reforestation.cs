﻿/*
 * Copyright (C) 2024 Game4Freak.io
 * This mod is provided under the Game4Freak EULA.
 * Full legal terms can be found at https://game4freak.io/eula/
 */

using Facepunch;
using Newtonsoft.Json;
using Rust;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("Reforestation", "VisEntities", "1.5.0")]
    [Description("Keeps forests dense by replanting trees after they are cut down.")]
    public class Reforestation : RustPlugin
    {
        #region Fields

        private static Reforestation _plugin;
        private static Configuration _config;

        private const int LAYER_TREES = Layers.Mask.Tree;
        private const int LAYER_GROUND = Layers.Mask.Terrain | Layers.Mask.World;
        private const int LAYER_ENTITIES = Layers.Mask.Construction | Layers.Mask.Deployed;

        private Dictionary<ulong, Timer> _treeRespawnTimers = new Dictionary<ulong, Timer>();
        private static readonly Dictionary<TreeType, string[]> _treePrefabs = new Dictionary<TreeType, string[]>
        {
            {
                TreeType.TundraBirchBig, new string[]
                {
                    "assets/bundled/prefabs/autospawn/resource/v3_tundra_field/birch_big_tundra.prefab",
                    "assets/bundled/prefabs/autospawn/resource/v3_tundra_forest/birch_big_tundra.prefab"
                }
            },
            {
                TreeType.TundraBirchLarge, new string[]
                {
                    "assets/bundled/prefabs/autospawn/resource/v3_tundra_field/birch_large_tundra.prefab",
                    "assets/bundled/prefabs/autospawn/resource/v3_tundra_forest/birch_large_tundra.prefab"
                }
            },
            {
                TreeType.TundraBirchMedium, new string[]
                {
                    "assets/bundled/prefabs/autospawn/resource/v3_tundra_field/birch_medium_tundra.prefab",
                    "assets/bundled/prefabs/autospawn/resource/v3_tundra_forestside/birch_medium_tundra.prefab"
                }
            },
            {
                TreeType.TundraBirchSmall, new string[]
                {
                    "assets/bundled/prefabs/autospawn/resource/v3_tundra_field/birch_small_tundra.prefab",
                    "assets/bundled/prefabs/autospawn/resource/v3_tundra_forestside/birch_small_tundra.prefab"
                }
            },
            {
                TreeType.TundraBirchTiny, new string[]
                {
                    "assets/bundled/prefabs/autospawn/resource/v3_tundra_field/birch_tiny_tundra.prefab",
                    "assets/bundled/prefabs/autospawn/resource/v3_tundra_forestside/birch_tiny_tundra.prefab"
                }
            },
            {
                TreeType.TundraPineDead, new string[]
                {
                    "assets/bundled/prefabs/autospawn/resource/v3_tundra_field/pine_dead_d.prefab",
                    "assets/bundled/prefabs/autospawn/resource/v3_tundra_field/pine_dead_e.prefab",
                    "assets/bundled/prefabs/autospawn/resource/v3_tundra_field/pine_dead_f.prefab",
                    "assets/bundled/prefabs/autospawn/resource/v3_tundra_forest/pine_dead_a.prefab",
                    "assets/bundled/prefabs/autospawn/resource/v3_tundra_forest/pine_dead_b.prefab",
                    "assets/bundled/prefabs/autospawn/resource/v3_tundra_forest/pine_dead_c.prefab",
                    "assets/bundled/prefabs/autospawn/resource/v3_tundra_forest/pine_dead_d.prefab",
                    "assets/bundled/prefabs/autospawn/resource/v3_tundra_forest/pine_dead_e.prefab",
                    "assets/bundled/prefabs/autospawn/resource/v3_tundra_forest/pine_dead_f.prefab"
                }
            },
            {
                TreeType.TundraPine, new string[]
                {
                    "assets/bundled/prefabs/autospawn/resource/v3_tundra_forest/pine_a.prefab",
                    "assets/bundled/prefabs/autospawn/resource/v3_tundra_forest/pine_c.prefab",
                    "assets/bundled/prefabs/autospawn/resource/v3_tundra_field_pines/pine_a.prefab",
                    "assets/bundled/prefabs/autospawn/resource/v3_tundra_field_pines/pine_b.prefab",
                    "assets/bundled/prefabs/autospawn/resource/v3_tundra_field_pines/pine_d.prefab",
                }
            },
            {
                TreeType.TundraPineSapling, new string[]
                {
                    "assets/bundled/prefabs/autospawn/resource/v3_tundra_field_pines/pine_sapling_a.prefab",
                    "assets/bundled/prefabs/autospawn/resource/v3_tundra_field_pines/pine_sapling_b.prefab",
                    "assets/bundled/prefabs/autospawn/resource/v3_tundra_field_pines/pine_sapling_c.prefab",
                    "assets/bundled/prefabs/autospawn/resource/v3_tundra_field_pines/pine_sapling_d.prefab",
                    "assets/bundled/prefabs/autospawn/resource/v3_tundra_field_pines/pine_sapling_e.prefab",
                }
            },
            {
                TreeType.TundraDouglasFir, new string[]
                {
                    "assets/bundled/prefabs/autospawn/resource/v3_tundra_forest/douglas_fir_a.prefab",
                    "assets/bundled/prefabs/autospawn/resource/v3_tundra_forest/douglas_fir_b.prefab",
                    "assets/bundled/prefabs/autospawn/resource/v3_tundra_forest/douglas_fir_c.prefab",
                    "assets/bundled/prefabs/autospawn/resource/v3_tundra_forest/douglas_fir_d.prefab"
                }
            },
            {
                TreeType.TempAmericanBeech, new string[]
                {
                    "assets/bundled/prefabs/autospawn/resource/v3_temp_forest/american_beech_a.prefab",
                    "assets/bundled/prefabs/autospawn/resource/v3_temp_field/american_beech_d.prefab",
                    "assets/bundled/prefabs/autospawn/resource/v3_temp_field/american_beech_e.prefab",
                    "assets/bundled/prefabs/autospawn/resource/v3_temp_forest/american_beech_b.prefab",
                    "assets/bundled/prefabs/autospawn/resource/v3_temp_forest/american_beech_c.prefab"
                }
            },
            {
                TreeType.TempBirch, new string[]
                {
                    "assets/bundled/prefabs/autospawn/resource/v3_temp_field/birch_small_temp.prefab",
                    "assets/bundled/prefabs/autospawn/resource/v3_temp_field/birch_tiny_temp.prefab",
                    "assets/bundled/prefabs/autospawn/resource/v3_temp_forest/birch_medium_temp.prefab",
                    "assets/bundled/prefabs/autospawn/resource/v3_temp_forest/birch_large_temp.prefab"
                }
            },
            {
                TreeType.TempOak, new string[]
                {
                    "assets/bundled/prefabs/autospawn/resource/v3_temp_field/oak_e.prefab",
                    "assets/bundled/prefabs/autospawn/resource/v3_temp_field/oak_f.prefab",
                    "assets/bundled/prefabs/autospawn/resource/v3_temp_field_large/oak_b.prefab",
                    "assets/bundled/prefabs/autospawn/resource/v3_temp_field_large/oak_c.prefab",
                    "assets/bundled/prefabs/autospawn/resource/v3_temp_field_large/oak_d.prefab"
                }
            },
            {
                TreeType.TempPine, new string[]
                {
                    "assets/bundled/prefabs/autospawn/resource/v3_temp_forest_pine/pine_a.prefab",
                    "assets/bundled/prefabs/autospawn/resource/v3_temp_forest_pine/pine_c.prefab",
                    "assets/bundled/prefabs/autospawn/resource/v3_temp_field/pine_b.prefab",
                    "assets/bundled/prefabs/autospawn/resource/v3_temp_field/pine_d.prefab"
                }
            },
            {
                TreeType.AridPalm, new string[]
                {
                    "assets/bundled/prefabs/autospawn/resource/v3_arid_forest/palm_tree_med_a_entity.prefab",
                    "assets/bundled/prefabs/autospawn/resource/v3_arid_forest/palm_tree_short_a_entity.prefab",
                    "assets/bundled/prefabs/autospawn/resource/v3_arid_forest/palm_tree_short_b_entity.prefab",
                    "assets/bundled/prefabs/autospawn/resource/v3_arid_forest/palm_tree_short_c_entity.prefab",
                    "assets/bundled/prefabs/autospawn/resource/v3_arid_forest/palm_tree_tall_a_entity.prefab",
                    "assets/bundled/prefabs/autospawn/resource/v3_arid_forest/palm_tree_tall_b_entity.prefab",
                    "assets/bundled/prefabs/autospawn/resource/v3_arid_forestside/palm_tree_short_a_entity.prefab",
                    "assets/bundled/prefabs/autospawn/resource/v3_arid_forestside/palm_tree_short_b_entity.prefab",
                    "assets/bundled/prefabs/autospawn/resource/v3_arid_forestside/palm_tree_short_c_entity.prefab"
                }
            },
            {
                TreeType.Swamp, new string[]
                {
                    "assets/bundled/prefabs/autospawn/resource/swamp-trees/swamp_tree_a.prefab",
                    "assets/bundled/prefabs/autospawn/resource/swamp-trees/swamp_tree_b.prefab",
                    "assets/bundled/prefabs/autospawn/resource/swamp-trees/swamp_tree_c.prefab",
                    "assets/bundled/prefabs/autospawn/resource/swamp-trees/swamp_tree_d.prefab",
                    "assets/bundled/prefabs/autospawn/resource/swamp-trees/swamp_tree_e.prefab",
                    "assets/bundled/prefabs/autospawn/resource/swamp-trees/swamp_tree_f.prefab"
                }
            },
            {
                TreeType.ArcticDouglasFir, new string[]
                {
                    "assets/bundled/prefabs/autospawn/resource/v3_arctic_forest/douglas_fir_a_snow.prefab",
                    "assets/bundled/prefabs/autospawn/resource/v3_arctic_forest/douglas_fir_b_snow.prefab",
                    "assets/bundled/prefabs/autospawn/resource/v3_arctic_forest/douglas_fir_c_snow.prefab"
                }
            },
            {
                TreeType.ArcticPine, new string[]
                {
                    "assets/bundled/prefabs/autospawn/resource/v3_arctic_forest/pine_a_snow.prefab",
                    "assets/bundled/prefabs/autospawn/resource/v3_arctic_forest/pine_c_snow.prefab",
                    "assets/bundled/prefabs/autospawn/resource/v3_arctic_forestside/pine_a_snow.prefab",
                    "assets/bundled/prefabs/autospawn/resource/v3_arctic_forestside/pine_d_snow.prefab"
                }
            },
            {
                TreeType.ArcticPineDead, new string[]
                {
                    "assets/bundled/prefabs/autospawn/resource/v3_arctic_forest/pine_dead_snow_a.prefab",
                    "assets/bundled/prefabs/autospawn/resource/v3_arctic_forest/pine_dead_snow_b.prefab",
                    "assets/bundled/prefabs/autospawn/resource/v3_arctic_forest/pine_dead_snow_c.prefab",
                    "assets/bundled/prefabs/autospawn/resource/v3_arctic_forest/pine_dead_snow_d.prefab",
                    "assets/bundled/prefabs/autospawn/resource/v3_arctic_forest/pine_dead_snow_e.prefab",
                    "assets/bundled/prefabs/autospawn/resource/v3_arctic_forest/pine_dead_snow_f.prefab",
                    "assets/bundled/prefabs/autospawn/resource/v3_arctic_forestside/pine_dead_snow_e.prefab",
                    "assets/bundled/prefabs/autospawn/resource/v3_arctic_forestside/pine_dead_snow_f.prefab"
                }
            },
            {
                TreeType.ArcticPineSapling, new string[]
                {
                    "assets/bundled/prefabs/autospawn/resource/v3_arctic_forest_saplings/pine_sapling_a_snow.prefab",
                    "assets/bundled/prefabs/autospawn/resource/v3_arctic_forest_saplings/pine_sapling_b_snow.prefab",
                    "assets/bundled/prefabs/autospawn/resource/v3_arctic_forest_saplings/pine_sapling_c_snow.prefab",
                    "assets/bundled/prefabs/autospawn/resource/v3_arctic_forest_saplings/pine_sapling_d_snow.prefab",
                    "assets/bundled/prefabs/autospawn/resource/v3_arctic_forest_saplings/pine_sapling_e_snow.prefab",
                    "assets/bundled/prefabs/autospawn/resource/v3_arctic_forestside/pine_sapling_d_snow.prefab",
                    "assets/bundled/prefabs/autospawn/resource/v3_arctic_forestside/pine_sapling_e_snow.prefab"
                }
            },
            {
                TreeType.JungleKapok, new []
                {
                    "assets/bundled/prefabs/autospawn/resource/vine_swinging/vineswingingtreeprefab.prefab"
                }
            },
            {
                TreeType.JungleTrumpet, new []
                {
                    "assets/bundled/prefabs/autospawn/resource/v3_jungle_forest/trumpet_tree_a.prefab",
                    "assets/bundled/prefabs/autospawn/resource/v3_jungle_forest/trumpet_tree_b.prefab",
                    "assets/bundled/prefabs/autospawn/resource/v3_jungle_forest/trumpet_tree_c.prefab",
                    "assets/bundled/prefabs/autospawn/resource/v3_jungle_forest/trumpet_tree_d.prefab",
                    "assets/bundled/prefabs/autospawn/resource/v3_jungle_forest_saplings/trumpet_tree_sapling_d.prefab"
                }
            },
            {
                TreeType.JungleHura, new []
                {
                    "assets/bundled/prefabs/autospawn/resource/v3_jungle_forest/hura_crepitans_a.prefab",
                    "assets/bundled/prefabs/autospawn/resource/v3_jungle_forest/hura_crepitans_b.prefab",
                    "assets/bundled/prefabs/autospawn/resource/v3_jungle_forest/hura_crepitans_d.prefab"
                }
            },
            {
                TreeType.JungleMauritia, new []
                {
                    "assets/bundled/prefabs/autospawn/resource/v3_jungle_forest/mauritia_flexuosa_xs.prefab",
                    "assets/bundled/prefabs/autospawn/resource/v3_jungle_forest/mauritia_flexuosa_s.prefab",
                    "assets/bundled/prefabs/autospawn/resource/v3_jungle_forest/mauritia_flexuosa_m.prefab",
                    "assets/bundled/prefabs/autospawn/resource/v3_jungle_forest/mauritia_flexuosa_l.prefab",
                    "assets/bundled/prefabs/autospawn/resource/v3_jungle_forest_saplings/mauritia_flexuosa_sapling.prefab"
                }
            },
        };
               
        #endregion Fields

        #region Configuration

        private class Configuration
        {
            [JsonProperty("Version")]
            public string Version { get; set; }

            [JsonProperty("Delay Before Planting Trees Seconds")]
            public float DelayBeforePlantingTreesSeconds { get; set; }

            [JsonProperty("Minimum Number Of Trees To Plant")]
            public int MinimumNumberOfTreesToPlant { get; set; }

            [JsonProperty("Maximum Number Of Trees To Plant")]
            public int MaximumNumberOfTreesToPlant { get; set; }

            [JsonProperty("Chance To Plant Each Tree")]
            public int ChanceToPlantEachTree { get; set; }

            [JsonProperty("Minimum Search Radius For Planting Site")]
            public float MinimumSearchRadiusForPlantingSite { get; set; }

            [JsonProperty("Maximum Search Radius For Planting Site")]
            public float MaximumSearchRadiusForPlantingSite { get; set; }

            [JsonProperty("Maximum Search Attempts For Planting Site")]
            public int MaximumSearchAttemptsForPlantingSite { get; set; }

            [JsonProperty("Allowable Distance From Nearby Trees")]
            public float AllowableDistanceFromNearbyTrees { get; set; }

            [JsonProperty("Building Check Radius")]
            public float BuildingCheckRadius { get; set; }

            [JsonProperty("Excluded Biomes")]
            public List<string> ExcludedBiomes { get; set; }

            [JsonProperty("Excluded Tree Prefabs")]
            public List<string> ExcludedTreePrefabs { get; set; }
        }

        protected override void LoadConfig()
        {
            base.LoadConfig();
            _config = Config.ReadObject<Configuration>();

            if (string.Compare(_config.Version, Version.ToString()) < 0)
                UpdateConfig();

            SaveConfig();
        }

        protected override void LoadDefaultConfig()
        {
            _config = GetDefaultConfig();
        }

        protected override void SaveConfig()
        {
            Config.WriteObject(_config, true);
        }

        private void UpdateConfig()
        {
            PrintWarning("Config changes detected! Updating...");

            Configuration defaultConfig = GetDefaultConfig();

            if (string.Compare(_config.Version, "1.0.0") < 0)
                _config = defaultConfig;

            if (string.Compare(_config.Version, "1.1.0") < 0)
            {
                _config.BuildingCheckRadius = defaultConfig.BuildingCheckRadius;
            }

            if (string.Compare(_config.Version, "1.2.0") < 0)
            {
                _config.ExcludedBiomes = defaultConfig.ExcludedBiomes;
            }

            if (string.Compare(_config.Version, "1.3.0") < 0)
            {
                _config.MinimumSearchRadiusForPlantingSite = defaultConfig.MinimumSearchRadiusForPlantingSite;
                _config.MaximumSearchRadiusForPlantingSite = defaultConfig.MaximumSearchRadiusForPlantingSite;
            }

            if (string.Compare(_config.Version, "1.4.0") < 0)
            {
                _config.ExcludedTreePrefabs = defaultConfig.ExcludedTreePrefabs;
            }

            PrintWarning("Config update complete! Updated from version " + _config.Version + " to " + Version.ToString());
            _config.Version = Version.ToString();
        }

        private Configuration GetDefaultConfig()
        {
            return new Configuration
            {
                Version = Version.ToString(),
                DelayBeforePlantingTreesSeconds = 60f,
                MinimumNumberOfTreesToPlant = 1,
                MaximumNumberOfTreesToPlant = 3,
                ChanceToPlantEachTree = 50,
                MinimumSearchRadiusForPlantingSite = 5f,
                MaximumSearchRadiusForPlantingSite = 30f,
                MaximumSearchAttemptsForPlantingSite = 10,
                AllowableDistanceFromNearbyTrees = 2f,
                BuildingCheckRadius = 5f,
                ExcludedBiomes = new List<string>
                {
                    "Arid"
                },
                ExcludedTreePrefabs = new List<string>
                {
                    "assets/bundled/prefabs/autospawn/resource/v3_arid_forest/palm_tree_tall_a_entity.prefab",
                    "assets/bundled/prefabs/autospawn/resource/v3_arid_forest/palm_tree_tall_b_entity.prefab"
                }
            };
        }

        #endregion Configuration

        #region Oxide Hooks

        private void Init()
        {
            _plugin = this;
        }

        private void Unload()
        {
            foreach (var timer in _treeRespawnTimers.Values)
            {
                if (timer != null)
                    timer.Destroy();
            }
            _treeRespawnTimers.Clear();

            _config = null;
            _plugin = null;
        }

        private void OnDispenserBonus(ResourceDispenser dispenser, BasePlayer player, Item item)
        {
            if (player == null)
                return;

            TreeEntity tree = dispenser.GetComponentInParent<TreeEntity>();
            if (tree != null && !_treeRespawnTimers.ContainsKey(tree.net.ID.Value) && !_config.ExcludedTreePrefabs.Contains(tree.PrefabName))
            {
                ScheduleTreeReplanting(tree);
            }
        }

        #endregion Oxide Hooks

        #region Tree Planting

        private void ScheduleTreeReplanting(TreeEntity tree)
        {
            ulong treeId = tree.net.ID.Value;
            Vector3 treePosition = tree.transform.position;
            TreeType treeType = GetTreeType(tree.PrefabName);

            if (treeType == TreeType.Unknown)
                return;

            string biome = GetBiome(treePosition);
            if (_config.ExcludedBiomes.Contains(biome))
                return;

            Timer spawnIn = timer.Once(_config.DelayBeforePlantingTreesSeconds, () =>
            {
                int treesToPlant = Random.Range(_config.MinimumNumberOfTreesToPlant, _config.MaximumNumberOfTreesToPlant + 1);

                for (int i = 0; i < treesToPlant; i++)
                {
                    if (!ChanceSucceeded(_config.ChanceToPlantEachTree))
                        continue;

                    if (TryFindSuitablePlantingSite(treePosition, _config.MinimumSearchRadiusForPlantingSite, _config.MaximumSearchRadiusForPlantingSite,
                        out Vector3 position, out Quaternion rotation, _config.MaximumSearchAttemptsForPlantingSite))
                    {
                        string treePrefab = GetRandomTreePrefabOfType(treeType);
                        if (treePrefab != null)
                        {
                            SpawnTree(treePrefab, position, rotation);
                        }
                    }
                }

                _treeRespawnTimers.Remove(treeId);
            });

            _treeRespawnTimers[treeId] = spawnIn;
        }

        public bool TryFindSuitablePlantingSite(Vector3 center, float minSearchRadius, float maxSearchRadius, out Vector3 position, out Quaternion rotation, int maximumAttempts)
        {
            position = Vector3.zero;
            rotation = Quaternion.identity;

            for (int attempt = 0; attempt < maximumAttempts; attempt++)
            {
                Vector3 candidatePosition = TerrainUtil.GetRandomPositionAround(center, minSearchRadius, maxSearchRadius);

                if (TerrainUtil.GetGroundInfo(candidatePosition, out RaycastHit raycastHit, 5f, LAYER_GROUND)
                    && !TerrainUtil.OnRoadOrRail(center)
                    && !TerrainUtil.InsideRock(center, 2.0f)
                    && !TerrainUtil.HasEntityNearby(raycastHit.point, _config.AllowableDistanceFromNearbyTrees, LAYER_TREES)
                    && !TerrainUtil.HasEntityNearby(raycastHit.point, _config.BuildingCheckRadius, LAYER_ENTITIES)
                    && !TerrainUtil.InWater(raycastHit.point))
                {
                    position = raycastHit.point;
                    return true;
                }
            }

            return false;
        }

        private TreeEntity SpawnTree(string prefabPath, Vector3 position, Quaternion rotation, bool wakeUpNow = true)
        {
            TreeEntity tree = GameManager.server.CreateEntity(prefabPath, position, rotation, wakeUpNow) as TreeEntity;
            if (tree == null)
                return null;

            tree.Spawn();
            return tree;
        }

        #endregion Tree Planting

        #region Tree Type Identification and Prefab Selection

        private TreeType GetTreeType(string prefabName)
        {
            foreach (TreeType treeType in _treePrefabs.Keys)
            {
                if (_treePrefabs[treeType].Contains(prefabName))
                    return treeType;
            }

            return TreeType.Unknown;
        }

        private string GetRandomTreePrefabOfType(TreeType treeType)
        {
            if (_treePrefabs.ContainsKey(treeType))
            {
                string[] prefabs = _treePrefabs[treeType];
                return prefabs[Random.Range(0, prefabs.Length)];
            }

            return null;
        }

        #endregion Tree Type Identification and Prefab Selection

        #region Biome Retrieval

        private string GetBiome(Vector3 position)
        {
            TerrainBiome.Enum biome = (TerrainBiome.Enum)TerrainMeta.BiomeMap.GetBiomeMaxType(position);
            switch (biome)
            {
                case TerrainBiome.Enum.Arctic:
                    return "Arctic";
                case TerrainBiome.Enum.Tundra:
                    return "Tundra";
                case TerrainBiome.Enum.Temperate:
                    return "Temperate";
                case TerrainBiome.Enum.Arid:
                    return "Arid";
                default:
                    return "Unknown";
            }
        }

        #endregion Biome Retrieval

        #region Enums

        public enum TreeType
        {
            Unknown,
            TundraBirchBig,
            TundraBirchLarge,
            TundraBirchMedium,
            TundraBirchSmall,
            TundraBirchTiny,
            TundraPineDead,
            TundraPine,
            TundraPineSapling,
            TundraDouglasFir,
            TempAmericanBeech,
            TempBirch,
            TempPine,
            TempOak,
            AridPalm,
            Swamp,
            ArcticDouglasFir,
            ArcticPine,
            ArcticPineDead,
            ArcticPineSapling,
            JungleKapok,
            JungleTrumpet,
            JungleHura,
            JungleMauritia,
        }

        #endregion Enums

        #region Helper Functions

        private static bool ChanceSucceeded(int percent)
        {
            if (percent <= 0)
                return false;

            if (percent >= 100)
                return true;

            float roll = Random.Range(0f, 100f);
            return roll < percent;
        }

        #endregion Helper Functions

        #region Helper Classes

        public static class TerrainUtil
        {
            public static bool OnTopology(Vector3 position, TerrainTopology.Enum topology)
            {
                return (TerrainMeta.TopologyMap.GetTopology(position) & (int)topology) != 0;
            }

            public static bool OnRoadOrRail(Vector3 position)
            {
                var combinedTopology = TerrainTopology.Enum.Road | TerrainTopology.Enum.Roadside |
                                   TerrainTopology.Enum.Rail | TerrainTopology.Enum.Railside;

                return OnTopology(position, combinedTopology);
            }

            public static bool InWater(Vector3 position)
            {
                return WaterLevel.Test(position, false, false);
            }

            public static bool InsideRock(Vector3 position, float radius)
            {
                List<Collider> colliders = Pool.Get<List<Collider>>();
                Vis.Colliders(position, radius, colliders, Layers.Mask.World, QueryTriggerInteraction.Ignore);

                bool result = false;

                foreach (Collider collider in colliders)
                {
                    if (collider.name.Contains("rock", CompareOptions.OrdinalIgnoreCase)
                        || collider.name.Contains("cliff", CompareOptions.OrdinalIgnoreCase)
                        || collider.name.Contains("formation", CompareOptions.OrdinalIgnoreCase))
                    {
                        result = true;
                        break;
                    }
                }

                Pool.FreeUnmanaged(ref colliders);
                return result;
            }

            public static bool HasEntityNearby(Vector3 position, float radius, LayerMask mask, string prefabName = null)
            {
                List<Collider> hitColliders = Pool.Get<List<Collider>>();
                GamePhysics.OverlapSphere(position, radius, hitColliders, mask, QueryTriggerInteraction.Ignore);

                bool hasEntityNearby = false;
                foreach (Collider collider in hitColliders)
                {
                    BaseEntity entity = collider.gameObject.ToBaseEntity();
                    if (entity != null)
                    {
                        if (prefabName == null || entity.PrefabName == prefabName)
                        {
                            hasEntityNearby = true;
                            break;
                        }
                    }
                }

                Pool.FreeUnmanaged(ref hitColliders);
                return hasEntityNearby;
            }

            public static Vector3 GetRandomPositionAround(Vector3 position, float minimumRadius, float maximumRadius, bool adjustToWaterHeight = false, bool adjustToTerrainHeight = false)
            {
                Vector3 randomDirection = Random.insideUnitSphere.normalized;

                float randomDistance = Random.Range(minimumRadius, maximumRadius);

                Vector3 randomPosition = position + randomDirection * randomDistance;

                if (adjustToWaterHeight)
                    randomPosition.y = TerrainMeta.WaterMap.GetHeight(randomPosition);
                else if (adjustToTerrainHeight)
                    randomPosition.y = TerrainMeta.HeightMap.GetHeight(randomPosition);

                return randomPosition;
            }

            public static bool GetGroundInfo(Vector3 startPosition, out RaycastHit raycastHit, float range, LayerMask mask)
            {
                return Physics.Linecast(startPosition + new Vector3(0.0f, range, 0.0f), startPosition - new Vector3(0.0f, range, 0.0f), out raycastHit, mask);
            }

            public static bool GetGroundInfo(Vector3 startPosition, out RaycastHit raycastHit, float range, LayerMask mask, Transform ignoreTransform = null)
            {
                startPosition.y += 0.25f;
                range += 0.25f;
                raycastHit = default;

                RaycastHit hit;
                if (!GamePhysics.Trace(new Ray(startPosition, Vector3.down), 0f, out hit, range, mask, QueryTriggerInteraction.UseGlobal, null))
                    return false;

                if (ignoreTransform != null && hit.collider != null
                    && (hit.collider.transform == ignoreTransform || hit.collider.transform.IsChildOf(ignoreTransform)))
                {
                    return GetGroundInfo(startPosition - new Vector3(0f, 0.01f, 0f), out raycastHit, range, mask, ignoreTransform);
                }

                raycastHit = hit;
                return true;
            }
        }

        #endregion Helper Classes
    }
}