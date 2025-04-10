using System;

namespace EntityConfigurationGenerator
{
    internal static class Guids
    {
        public const string PackageGuidString = "08cd4165-b5ac-4b54-b5cb-9baebef59972";
        public static readonly Guid PackageGuid = new Guid(PackageGuidString);

        public const string CommandSetString = "7fb0260a-1b80-4015-9025-7c9571af8c59";
        public static readonly Guid CommandSet = new Guid(CommandSetString);
        internal static class CommandIds
        {
            public const int GenerateSingleConfig = 0x0100;
            public const int GenerateAllConfigs = 0x0101;
            public const int ToggleUsePartials = 0x0102;

            public const int MyMenuGroup = 0x1020;
            public const int MyFolderMenuGroup = 0x1021;
            public const int MyExtensionsMenuGroup = 0x1022;
            public const int MyTopLevelExtensionMenu = 0x3000;
        }
    }
}
