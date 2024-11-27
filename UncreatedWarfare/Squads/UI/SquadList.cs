﻿using Uncreated.Framework.UI;
using Uncreated.Framework.UI.Patterns;
using Uncreated.Framework.UI.Reflection;
using Uncreated.Warfare.Configuration;

namespace Uncreated.Warfare.Squads.UI;
[UnturnedUI(BasePath = "Canvas")]
public class SquadList : UnturnedUI
{
    public readonly UnturnedLabel Header = new UnturnedLabel("Header");

    public readonly SquadMenuItem[] Squads = ElementPatterns.CreateArray<SquadMenuItem>("{0}", 0, to: 7);
    public SquadList(AssetConfiguration assetConfig, ILoggerFactory loggerFactory) : base(loggerFactory, assetConfig.GetAssetLink<EffectAsset>("UI:SquadList"), staticKey: true) { }
    public class SquadMenuItem
    {
        [Pattern(Root = true)]
        public UnturnedUIElement Root { get; set; }

        [Pattern("N{0}")]
        public UnturnedLabel Name { get; set; }

        [Pattern("M{0}")]
        public UnturnedLabel MemberCount { get; set; }
    }
}
