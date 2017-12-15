﻿using System;
using System.Collections.Generic;
using System.Linq;

using RimWorld;
using Verse;

namespace Quarry {

  public static class OreDictionary {

    private static System.Random rand = new System.Random();
		private static SimpleCurve commonalityCurve = new SimpleCurve {
			{ new CurvePoint(0.0f, 10f) },
			{ new CurvePoint(0.02f, 9f) },
			{ new CurvePoint(0.04f, 8f) },
			{ new CurvePoint(0.06f, 6f) },
			{ new CurvePoint(0.08f, 3f) },
			{ new CurvePoint(float.MaxValue, 1f) }
		};
		private static Predicate<ThingDef> validOre = ((ThingDef def) => def.mineable && def != QuarryDefOf.MineableComponents && def.building != null && def.building.isResourceRock && def.building.mineableThing != null);


		public static void Build() {
			List<ThingCountExposable> oreDictionary = new List<ThingCountExposable>();

			// Get all ThingDefs that have mineable resources
			IEnumerable<ThingDef> ores = DefDatabase<ThingDef>.AllDefs.Where((ThingDef def) => validOre(def));

			// Assign commonality values for ores
			foreach (ThingDef ore in ores) {
				oreDictionary.Add(new ThingCountExposable(ore.building.mineableThing, ValueForMineableOre(ore)));
			}

			// Manually add components
			oreDictionary.Add(new ThingCountExposable(ThingDefOf.Component, 7));

			// Assign this dictionary for the mod to use
			QuarrySettings.oreDictionary = oreDictionary;
		}


		public static int ValueForMineableOre(ThingDef def) {
			if (!validOre(def)) {
				Log.Error($"{Static.Quarry}:: Unable to process def {def.LabelCap} as a mineable resource rock.");
				return 0;
			}
			return (int)(((((def.building.mineableThing.deepCommonality < 1.5f) ? def.building.mineableThing.deepCommonality : 1.5f) 
				* (def.building.mineableScatterCommonality * commonalityCurve.Evaluate(def.building.mineableScatterCommonality))) * 50) 
				/ ((def.building.mineableThing.BaseMarketValue < 2f) ? 2f : (def.building.mineableThing.BaseMarketValue / 5f)));
		}


		public static Dictionary<ThingDef, int> PercentageDictionary(Dictionary<ThingDef, int> dictionary) {
			Dictionary<ThingDef, int> dict = new Dictionary<ThingDef, int>();

			foreach (KeyValuePair<ThingDef, int> pair in dictionary) {
				dict.Add(pair.Key, WeightAsPercentage(dictionary.AsThingCountExposableList(), pair.Value));
			}
			return dict;
		}


		public static int WeightAsPercentage(List<ThingCountExposable> dictionary, int weight) {
			float sum = 0;

			foreach (ThingCountExposable tc in dictionary) {
				sum += tc.count;
			}
			return (int)((weight / sum) * 100f);
		}


    public static ThingDef TakeOne() {
			// Make sure there is a dictionary to work from
			if (QuarrySettings.oreDictionary == null) {
				Build();
			}

			// Sorts the weight list
			List<ThingCountExposable> sortedWeights = Sort(QuarrySettings.oreDictionary);

      // Sums all weights
      int sum = 0;
      foreach (ThingCountExposable ore in QuarrySettings.oreDictionary) {
        sum += ore.count;
      }

      // Randomizes a number from Zero to Sum
      int roll = rand.Next(0, sum);

      // Finds chosen item based on weight
      ThingDef selected = sortedWeights[sortedWeights.Count - 1].thingDef;
      foreach (ThingCountExposable ore in sortedWeights) {
        if (roll < ore.count) {
          selected = ore.thingDef;
          break;
        }
        roll -= ore.count;
      }

      // Returns the selected item
      return selected;
    }


    private static List<ThingCountExposable> Sort(List<ThingCountExposable> weights) {
			List<ThingCountExposable> list = new List<ThingCountExposable>(weights);

      // Sorts the Weights List for randomization later
      list.Sort(
          delegate (ThingCountExposable firstPair,
										ThingCountExposable nextPair) {
                    return firstPair.count.CompareTo(nextPair.count);
                   }
       );

      return list;
    }
  }
}
