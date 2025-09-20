using Kitchen;
using System.Collections.Generic;
using System.Reflection;
using Unity.Entities;

namespace KitchenMysteryMeat.Systems
{
    internal static class CorpseStorageUtils
    {
        private static readonly FieldInfo StoredByFieldInfo = ResolveStoredByFieldInfo();
        private static readonly PropertyInfo StoredByPropertyInfo = ResolveStoredByPropertyInfo();

        internal static List<Entity> CollectHolderEntities(
            EntityManager entityManager,
            Entity storedEntity,
            List<Entity> results,
            Dictionary<Entity, Entity> heldItemLookup = null)
        {
            results ??= new List<Entity>(capacity: 2);
            results.Clear();

            if (heldItemLookup != null && heldItemLookup.TryGetValue(storedEntity, out Entity lookupHolder))
            {
                TryAddHolder(results, lookupHolder);
            }

            if (entityManager.HasComponent<CHeldBy>(storedEntity))
            {
                CHeldBy heldBy = entityManager.GetComponentData<CHeldBy>(storedEntity);
                TryAddHolder(results, heldBy.Holder);
            }

            if (entityManager.HasComponent<CStoredBy>(storedEntity))
            {
                CStoredBy storedBy = entityManager.GetComponentData<CStoredBy>(storedEntity);
                Entity storedByEntity = ExtractStoredByEntity(storedBy);
                TryAddHolder(results, storedByEntity);
            }

            return results;
        }

        internal static List<Entity> CollectHolderEntities(
            EntityContext ctx,
            Entity storedEntity,
            List<Entity> results,
            Dictionary<Entity, Entity> heldItemLookup = null)
        {
            results ??= new List<Entity>(capacity: 2);
            results.Clear();

            if (heldItemLookup != null && heldItemLookup.TryGetValue(storedEntity, out Entity lookupHolder))
            {
                TryAddHolder(results, lookupHolder);
            }

            if (ctx.Has<CHeldBy>(storedEntity))
            {
                CHeldBy heldBy = ctx.Get<CHeldBy>(storedEntity);
                TryAddHolder(results, heldBy.Holder);
            }

            if (ctx.Has<CStoredBy>(storedEntity))
            {
                CStoredBy storedBy = ctx.Get<CStoredBy>(storedEntity);
                Entity storedByEntity = ExtractStoredByEntity(storedBy);
                TryAddHolder(results, storedByEntity);
            }

            return results;
        }

        private static void TryAddHolder(List<Entity> results, Entity candidate)
        {
            if (candidate == Entity.Null)
            {
                return;
            }

            if (!results.Contains(candidate))
            {
                results.Add(candidate);
            }
        }

        private static Entity ExtractStoredByEntity(CStoredBy storedBy)
        {
            if (StoredByFieldInfo != null)
            {
                object value = StoredByFieldInfo.GetValue(storedBy);
                if (value is Entity fieldEntity)
                {
                    return fieldEntity;
                }
            }

            if (StoredByPropertyInfo != null)
            {
                object value = StoredByPropertyInfo.GetValue(storedBy);
                if (value is Entity propertyEntity)
                {
                    return propertyEntity;
                }
            }

            return Entity.Null;
        }

        private static FieldInfo ResolveStoredByFieldInfo()
        {
            BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

            FieldInfo explicitField = typeof(CStoredBy).GetField("StoredBy", flags);
            if (explicitField != null && explicitField.FieldType == typeof(Entity))
            {
                return explicitField;
            }

            foreach (FieldInfo candidate in typeof(CStoredBy).GetFields(flags))
            {
                if (candidate.FieldType == typeof(Entity))
                {
                    return candidate;
                }
            }

            return null;
        }

        private static PropertyInfo ResolveStoredByPropertyInfo()
        {
            BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

            PropertyInfo explicitProperty = typeof(CStoredBy).GetProperty("StoredBy", flags);
            if (explicitProperty != null && explicitProperty.PropertyType == typeof(Entity))
            {
                return explicitProperty;
            }

            foreach (PropertyInfo candidate in typeof(CStoredBy).GetProperties(flags))
            {
                if (candidate.PropertyType == typeof(Entity))
                {
                    return candidate;
                }
            }

            return null;
        }
    }
}
