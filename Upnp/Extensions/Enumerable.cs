using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Upnp.Extensions
{
    public static class Enumerable
    {
        public static IEnumerable<T> Add<T>(this IEnumerable<T> enumerable, params Func<T>[] items)
        {
            return enumerable.Concat(items.Select(action => action()));
        }

        public static IEnumerable<T> Map<T>(this IEnumerable<T> enumerable, Action<T> map)
        {
            return enumerable.Select(item =>
            {
                map(item);
                return item;
            });
        }
    }
}
