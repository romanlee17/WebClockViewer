using MirraGames.SDK.Common;
using MirraGames.SDK.Fallback;

namespace CustomTimeService
{
    [Configuration]
    public class AppConfiguration : FallbackConfiguration
    {
        public override string Name => nameof(AppConfiguration);
        public override string Description => "Custom WebClockViewer configuration";
        public override string IconName => string.Empty;
        public override bool ReadOnly => false;

        public override string DateTimeProviderName { get; } = nameof(WebDateTime);
    }
}
