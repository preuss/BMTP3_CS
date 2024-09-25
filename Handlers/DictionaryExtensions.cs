using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BMTP3_CS.Handlers {
	public static class DictionaryExtensions {
		public static bool DictionaryEqual<TKey, TValue>(this IDictionary<TKey, TValue> dict1, IDictionary<TKey, TValue> dict2, IEqualityComparer<TValue>? equalityComparer = null) {
			if(dict1 == dict2) {
				return true;
			}

			if(dict1 == null || dict2 == null) {
				return false;
			}

			if(dict1.Count != dict2.Count) {
				return false;
			}

			if(equalityComparer == null) {
				equalityComparer = EqualityComparer<TValue>.Default;
			}

			return dict1.All(kvp => {
				TValue? value2;
				return dict2.TryGetValue(kvp.Key, out value2)
					&& equalityComparer.Equals(kvp.Value, value2);
			});
		}
	}
}
