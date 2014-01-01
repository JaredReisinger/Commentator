using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Text;

namespace Spudnoggin.Commentator
{
    public static class Extensions
    {
        public static T GetService<T>(this IServiceProvider serviceProvider)
        {
            return serviceProvider.GetService<T,T>();
        }

        public static TResult GetService<T, TResult>(this IServiceProvider serviceProvider)
            where T : TResult   // is this, in fact, always true?
        {
            return (TResult)serviceProvider.GetService(typeof(T));
        }
    }
}
