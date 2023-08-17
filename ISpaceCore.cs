using System.Reflection;

namespace KediNPCUtilities
{
    public interface ISpaceCore
    {
        /// Must take (Event, GameLocation, GameTime, string[])
        void AddEventCommand(string command, MethodInfo info);
    }
}
