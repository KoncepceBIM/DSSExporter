using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xbim.Common;
using Xbim.Ifc4.MeasureResource;
using Xbim.Ifc4.PropertyResource;

namespace LOIN.Exporter
{
    internal class EnumsMap
    {
        private static readonly StringComparer comparer = StringComparer.OrdinalIgnoreCase;

        private readonly Dictionary<string, IfcPropertyEnumeration> _cache = 
            new Dictionary<string, IfcPropertyEnumeration>(comparer);
        private readonly IModel _model;

        public EnumsMap(LOIN.Model model)
        {
            _model = model.Internal;

            // cache any existing enums
            foreach (var item in _model.Instances.OfType<IfcPropertyEnumeration>())
            {
                if (item.Name != "" && !_cache.ContainsKey(item.Name))
                    _cache.Add(item.Name, item);
            }
        }

        public IfcPropertyEnumeration this[string key]
        {
            get
            {
                if (_cache.TryGetValue(key, out IfcPropertyEnumeration enumeration))
                    return enumeration;
                return null;
            }
        }

        public IfcPropertyEnumeration GetOrAdd(string name, string[] values)
        {
            return GetOrAdd(name, name, values);
        }

        public IfcPropertyEnumeration GetOrAdd(string key, string name, string[] values)
        {
            if (_cache.TryGetValue(key, out IfcPropertyEnumeration result))
            {
                var hash = new HashSet<string>(values);
                if (!comparer.Equals(result.Name,name))  
                {
                    throw new Exception($"IfcPropertyEnumeration with key {key} already exists but has different name {name}/{result.Name}");
                }

                // make sure enumeration values are merged if needed
                var enumerationValues = result.EnumerationValues
                    .Select(v => v.ToString())
                    .Union(values, comparer)
                    .Select(e => new IfcLabel(e))
                    .Cast<IfcValue>();
                result.EnumerationValues.Clear();
                result.EnumerationValues.AddRange(enumerationValues);
                return result;
            }

            var enumValues = values.Select(v => new IfcLabel(v)).Cast<IfcValue>();
            result = _model.Instances.New<IfcPropertyEnumeration>(e => {
                e.Name = name;
                e.EnumerationValues.AddRange(enumValues);
            });
            _cache.Add(key, result);
            return result;
        }
    }
}
