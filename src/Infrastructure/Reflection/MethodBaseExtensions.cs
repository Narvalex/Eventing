using System.Collections.Generic;
using System.Reflection;

namespace Infrastructure.Reflection
{
    public static class MethodBaseExtensions
    {
        public static IList<Instruction> GetInstructions(this MethodBase self) =>
            MethodBodyReader.GetInstructions(self).AsReadOnly();
    }
}
