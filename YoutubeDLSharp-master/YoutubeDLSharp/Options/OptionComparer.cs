using System.Collections.Generic;

namespace YoutubeDLSharp.Options
{
    internal class OptionComparer : IEqualityComparer<IOption> 
    {
        public bool Equals(IOption x, IOption y)
        {
            if (x != null)
            {
                return y != null && x.ToString().Equals(y.ToString());
            }
            
            return y == null;
        }

        public int GetHashCode(IOption obj) => obj.ToString().GetHashCode();
    }
}