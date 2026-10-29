using System;

namespace EarlyWarp
{
    internal static class TechDefinitionMigration
    {
        internal static bool MoveWarpUnlock(
            TechProto warpTech,
            TechProto engineTech,
            int functionId,
            double functionValue)
        {
            if (warpTech == null)
            {
                throw new ArgumentNullException(nameof(warpTech));
            }

            if (engineTech == null)
            {
                throw new ArgumentNullException(nameof(engineTech));
            }

            bool changed = RemoveUnlockFunction(engineTech, functionId);
            changed |= AddOrUpdateUnlockFunction(warpTech, functionId, functionValue);
            return changed;
        }

        internal static bool ContainsUnlockFunction(TechProto tech, int functionId)
        {
            return tech != null &&
                   tech.UnlockFunctions != null &&
                   Array.IndexOf(tech.UnlockFunctions, functionId) >= 0;
        }

        internal static string DescribeFunctions(TechProto tech)
        {
            if (tech == null || tech.UnlockFunctions == null || tech.UnlockFunctions.Length == 0)
            {
                return "<none>";
            }

            return string.Join(",", Array.ConvertAll(tech.UnlockFunctions, value => value.ToString()));
        }

        private static bool RemoveUnlockFunction(TechProto tech, int functionId)
        {
            int[] functions = tech.UnlockFunctions ?? Array.Empty<int>();
            double[] values = tech.UnlockValues ?? Array.Empty<double>();

            int keptCount = 0;
            for (int i = 0; i < functions.Length; i++)
            {
                if (functions[i] != functionId)
                {
                    keptCount++;
                }
            }

            if (keptCount == functions.Length)
            {
                return false;
            }

            int[] newFunctions = new int[keptCount];
            double[] newValues = new double[keptCount];
            int targetIndex = 0;
            for (int i = 0; i < functions.Length; i++)
            {
                if (functions[i] == functionId)
                {
                    continue;
                }

                newFunctions[targetIndex] = functions[i];
                newValues[targetIndex] = i < values.Length ? values[i] : 0.0;
                targetIndex++;
            }

            tech.UnlockFunctions = newFunctions;
            tech.UnlockValues = newValues;
            return true;
        }

        private static bool AddOrUpdateUnlockFunction(TechProto tech, int functionId, double value)
        {
            int[] functions = tech.UnlockFunctions ?? Array.Empty<int>();
            double[] values = tech.UnlockValues ?? Array.Empty<double>();
            int existingIndex = Array.IndexOf(functions, functionId);

            if (existingIndex >= 0)
            {
                if (existingIndex < values.Length && values[existingIndex].Equals(value))
                {
                    return false;
                }

                double[] updatedValues = new double[functions.Length];
                Array.Copy(values, updatedValues, Math.Min(values.Length, updatedValues.Length));
                updatedValues[existingIndex] = value;
                tech.UnlockValues = updatedValues;
                return true;
            }

            int[] newFunctions = new int[functions.Length + 1];
            double[] newValues = new double[functions.Length + 1];
            Array.Copy(functions, newFunctions, functions.Length);
            Array.Copy(values, newValues, Math.Min(values.Length, newValues.Length));
            newFunctions[functions.Length] = functionId;
            newValues[functions.Length] = value;
            tech.UnlockFunctions = newFunctions;
            tech.UnlockValues = newValues;
            return true;
        }
    }
}
