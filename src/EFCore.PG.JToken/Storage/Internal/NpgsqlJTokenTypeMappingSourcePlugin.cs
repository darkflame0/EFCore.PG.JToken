using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Storage;
using Newtonsoft.Json.Linq;

namespace Npgsql.EntityFrameworkCore.PostgreSQL.Storage.Internal
{
    public class NpgsqlJTokenTypeMappingSourcePlugin : IRelationalTypeMappingSourcePlugin
    {
        public ConcurrentDictionary<string, RelationalTypeMapping[]> StoreTypeMappings { get; }
        public ConcurrentDictionary<Type, RelationalTypeMapping> ClrTypeMappings { get; }

        private readonly JTokenTypeMapping _jsonb = new JTokenTypeMapping("jsonb", typeof(JToken));
        private readonly JTokenTypeMapping _json = new JTokenTypeMapping("json", typeof(JToken));

        public NpgsqlJTokenTypeMappingSourcePlugin()
        {
            var clrTypeMappings = new Dictionary<Type, RelationalTypeMapping>()
            {
                { typeof(JToken),_jsonb },
            };
            var storeTypeMappings = new Dictionary<string, RelationalTypeMapping[]>()
            {
                {
                    "jsonb",new RelationalTypeMapping[]
                    {
                        _jsonb
                    }
                },
                {
                    "json",new RelationalTypeMapping[]
                    {
                        _json
                    }
                }

            };
            StoreTypeMappings = new ConcurrentDictionary<string, RelationalTypeMapping[]>(storeTypeMappings, StringComparer.OrdinalIgnoreCase);
            ClrTypeMappings = new ConcurrentDictionary<Type, RelationalTypeMapping>(clrTypeMappings);
        }

        public RelationalTypeMapping? FindMapping(in RelationalTypeMappingInfo mappingInfo)
        {
            var clrType = mappingInfo.ClrType;
            var storeTypeName = mappingInfo.StoreTypeName;
            var storeTypeNameBase = mappingInfo.StoreTypeNameBase;
            if (storeTypeName != null)
            {
                if (StoreTypeMappings.TryGetValue(storeTypeName, out var mappings))
                {
                    if (clrType == null)
                        return mappings[0];

                    foreach (var m in mappings)
                        if (m.ClrType == clrType)
                            return m;

                    return null;
                }
                if (StoreTypeMappings.TryGetValue(storeTypeNameBase, out mappings))
                {
                    if (clrType == null)
                        return mappings[0].Clone(in mappingInfo);

                    foreach (var m in mappings)
                        if (m.ClrType == clrType)
                            return m.Clone(in mappingInfo);

                    return null;
                }
            }
            if (clrType == null || !ClrTypeMappings.TryGetValue(clrType, out var mapping))
                return null;
            return mapping;
        }
    }
}
