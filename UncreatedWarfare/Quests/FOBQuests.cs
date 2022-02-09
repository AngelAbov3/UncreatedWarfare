﻿using SDG.Unturned;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Uncreated.Warfare.FOBs;
using Uncreated.Warfare.Teams;

namespace Uncreated.Warfare.Quests.Types;

[QuestData(EQuestType.BUILD_FOBS)]
public class BuildFOBsQuest : BaseQuestData<BuildFOBsQuest.Tracker, BuildFOBsQuest.State, BuildFOBsQuest>
{
    public DynamicIntegerValue BuildCount;
    public override int TickFrequencySeconds => 0;
    public override Tracker CreateQuestTracker(UCPlayer player, ref State state) => new Tracker(player, ref state);
    public override void OnPropertyRead(string propertyname, ref Utf8JsonReader reader)
    {
        if (propertyname.Equals("fobs_required", StringComparison.Ordinal))
        {
            if (!reader.TryReadIntegralValue(out BuildCount))
                BuildCount = new DynamicIntegerValue(10);
        }
    }
    public struct State : IQuestState<Tracker, BuildFOBsQuest>
    {
        public IDynamicValue<int>.IChoice BuildCount;
        public void Init(BuildFOBsQuest data)
        {
            this.BuildCount = data.BuildCount.GetValue();
        }
        public void OnPropertyRead(ref Utf8JsonReader reader, string prop)
        {
            if (prop.Equals("fobs_required", StringComparison.Ordinal))
                BuildCount = DynamicIntegerValue.ReadChoice(ref reader);
        }
        public void WriteQuestState(Utf8JsonWriter writer)
        {
            writer.WriteProperty("fobs_required", BuildCount);
        }
    }
    public class Tracker : BaseQuestTracker, INotifyFOBBuilt
    {
        private readonly int BuildCount = 0;
        private int _fobsBuilt;
        public override void ResetToDefaults() => _fobsBuilt = 0;
        public Tracker(UCPlayer target, ref State questState) : base(target)
        {
            BuildCount = questState.BuildCount.InsistValue();
        }
        public override void OnReadProgressSaveProperty(string prop, ref Utf8JsonReader reader)
        {
            if (reader.TokenType == JsonTokenType.Number && prop.Equals("fobs_built", StringComparison.Ordinal))
                _fobsBuilt = reader.GetInt32();
        }
        public override void WriteQuestProgress(Utf8JsonWriter writer)
        {
            writer.WriteProperty("fobs_built", _fobsBuilt);
        }
        public void OnFOBBuilt(UCPlayer constructor, Components.FOB fob)
        {
            if (constructor.Steam64 == _player.Steam64)
            {
                _fobsBuilt++;
                if (_fobsBuilt >= BuildCount)
                    TellCompleted();
                else
                    TellUpdated();
            }
        }
        public override string Translate() => QuestData.Translate(_player, _fobsBuilt, BuildCount);
    }
}
[QuestData(EQuestType.BUILD_FOBS_NEAR_OBJECTIVES)]
public class BuildFOBsNearObjQuest : BaseQuestData<BuildFOBsNearObjQuest.Tracker, BuildFOBsNearObjQuest.State, BuildFOBsNearObjQuest>
{
    public DynamicIntegerValue BuildCount;
    public DynamicFloatValue BuildRange;
    public override int TickFrequencySeconds => 0;
    public override Tracker CreateQuestTracker(UCPlayer player, ref State state) => new Tracker(player, ref state);
    public override void OnPropertyRead(string propertyname, ref Utf8JsonReader reader)
    {
        if (propertyname.Equals("fobs_required", StringComparison.Ordinal))
        {
            if (!reader.TryReadIntegralValue(out BuildCount))
                BuildCount = new DynamicIntegerValue(10);
        }
        else if (propertyname.Equals("buildables_required", StringComparison.Ordinal))
        {
            if (!reader.TryReadFloatValue(out BuildRange))
                BuildRange = new DynamicFloatValue(200f);
        }
    }
    public struct State : IQuestState<Tracker, BuildFOBsNearObjQuest>
    {
        public IDynamicValue<int>.IChoice BuildCount;
        public IDynamicValue<float>.IChoice BuildRange;
        public void Init(BuildFOBsNearObjQuest data)
        {
            this.BuildCount = data.BuildCount.GetValue();
            this.BuildRange = data.BuildRange.GetValue();
        }
        public void OnPropertyRead(ref Utf8JsonReader reader, string prop)
        {
            if (prop.Equals("fobs_required", StringComparison.Ordinal))
                BuildCount = DynamicIntegerValue.ReadChoice(ref reader);
            else if (prop.Equals("buildables_required", StringComparison.Ordinal))
                BuildRange = DynamicFloatValue.ReadChoice(ref reader);
        }
        public void WriteQuestState(Utf8JsonWriter writer)
        {
            writer.WriteProperty("fobs_required", BuildCount);
            writer.WriteProperty("buildables_required", BuildRange);
        }
    }
    public class Tracker : BaseQuestTracker, INotifyFOBBuilt
    {
        private readonly int BuildCount = 0;
        private readonly float SqrBuildRange = 0f;
        private int _fobsBuilt;
        public override void ResetToDefaults() => _fobsBuilt = 0;
        public Tracker(UCPlayer target, ref State questState) : base(target)
        {
            BuildCount = questState.BuildCount.InsistValue();
            SqrBuildRange = questState.BuildRange.InsistValue();
            SqrBuildRange *= SqrBuildRange;
        }
        public override void OnReadProgressSaveProperty(string prop, ref Utf8JsonReader reader)
        {
            if (reader.TokenType == JsonTokenType.Number && prop.Equals("fobs_built", StringComparison.Ordinal))
                _fobsBuilt = reader.GetInt32();
        }
        public override void WriteQuestProgress(Utf8JsonWriter writer)
        {
            writer.WriteProperty("fobs_built", _fobsBuilt);
        }
        public void OnFOBBuilt(UCPlayer constructor, Components.FOB fob)
        {
            if (constructor.Steam64 == _player.Steam64)
            {
                ulong team = _player.GetTeam();
                if (Data.Is(out Gamemodes.Flags.TeamCTF.TeamCTF ctf))
                {
                    if ((team == 1 && ctf.ObjectiveTeam1 != null && F.SqrDistance2D(fob.Position, ctf.ObjectiveTeam1.Position) <= SqrBuildRange) ||
                        (team == 2 && ctf.ObjectiveTeam2 != null && F.SqrDistance2D(fob.Position, ctf.ObjectiveTeam2.Position) <= SqrBuildRange))
                    {
                        goto add;
                    }
                }
                else if (Data.Is(out Gamemodes.Flags.Invasion.Invasion inv))
                {
                    if ((inv.AttackingTeam == 1 && ctf.ObjectiveTeam1 != null && F.SqrDistance2D(fob.Position, ctf.ObjectiveTeam1.Position) <= SqrBuildRange) ||
                        (inv.AttackingTeam == 2 && ctf.ObjectiveTeam2 != null && F.SqrDistance2D(fob.Position, ctf.ObjectiveTeam2.Position) <= SqrBuildRange))
                    {
                        goto add;
                    }
                }
                else if (Data.Is(out Gamemodes.Insurgency.Insurgency ins))
                {
                    for (int i = 0; i < ins.Caches.Count; i++)
                    {
                        Gamemodes.Insurgency.Insurgency.CacheData cache = ins.Caches[i];
                        if (cache != null && cache.IsActive && F.SqrDistance2D(fob.Position, ctf.ObjectiveTeam1.Position) <= SqrBuildRange)
                            goto add;
                    }
                }
            }
            return;
        add:
            _fobsBuilt++;
            if (_fobsBuilt >= BuildCount)
                TellCompleted();
            else
                TellUpdated();
        }
        public override string Translate() => QuestData.Translate(_player, _fobsBuilt, BuildCount);
    }
}
[QuestData(EQuestType.BUILD_FOB_ON_ACTIVE_OBJECTIVE)]
public class BuildFOBsOnObjQuest : BaseQuestData<BuildFOBsOnObjQuest.Tracker, BuildFOBsOnObjQuest.State, BuildFOBsOnObjQuest>
{
    public DynamicIntegerValue BuildCount;
    public override int TickFrequencySeconds => 0;
    public override Tracker CreateQuestTracker(UCPlayer player, ref State state) => new Tracker(player, ref state);
    public override void OnPropertyRead(string propertyname, ref Utf8JsonReader reader)
    {
        if (propertyname.Equals("fobs_required", StringComparison.Ordinal))
        {
            if (!reader.TryReadIntegralValue(out BuildCount))
                BuildCount = new DynamicIntegerValue(10);
        }
    }
    public struct State : IQuestState<Tracker, BuildFOBsOnObjQuest>
    {
        public IDynamicValue<int>.IChoice BuildCount;
        public void Init(BuildFOBsOnObjQuest data)
        {
            this.BuildCount = data.BuildCount.GetValue();
        }
        public void OnPropertyRead(ref Utf8JsonReader reader, string prop)
        {
            if (prop.Equals("fobs_required", StringComparison.Ordinal))
                BuildCount = DynamicIntegerValue.ReadChoice(ref reader);
        }
        public void WriteQuestState(Utf8JsonWriter writer)
        {
            writer.WriteProperty("fobs_required", BuildCount);
        }
    }
    public class Tracker : BaseQuestTracker, INotifyFOBBuilt
    {
        private readonly int BuildCount = 0;
        private int _fobsBuilt;
        public override void ResetToDefaults() => _fobsBuilt = 0;
        public Tracker(UCPlayer target, ref State questState) : base(target)
        {
            BuildCount = questState.BuildCount.InsistValue();
        }
        public override void OnReadProgressSaveProperty(string prop, ref Utf8JsonReader reader)
        {
            if (reader.TokenType == JsonTokenType.Number && prop.Equals("fobs_built", StringComparison.Ordinal))
                _fobsBuilt = reader.GetInt32();
        }
        public override void WriteQuestProgress(Utf8JsonWriter writer)
        {
            writer.WriteProperty("fobs_built", _fobsBuilt);
        }
        public void OnFOBBuilt(UCPlayer constructor, Components.FOB fob)
        {
            if (constructor.Steam64 == _player.Steam64)
            {
                ulong team = _player.GetTeam();
                if (Data.Is(out Gamemodes.Flags.TeamCTF.TeamCTF ctf))
                {
                    if ((team == 1 && ctf.ObjectiveTeam1 != null && ctf.ObjectiveTeam1.PlayerInRange(fob.Position)) ||
                        (team == 2 && ctf.ObjectiveTeam2 != null && ctf.ObjectiveTeam2.PlayerInRange(fob.Position)))
                    {
                        goto add;
                    }
                }
                else if (Data.Is(out Gamemodes.Flags.Invasion.Invasion inv))
                {
                    if ((inv.AttackingTeam == 1 && ctf.ObjectiveTeam1 != null && ctf.ObjectiveTeam1.PlayerInRange(fob.Position)) ||
                        (inv.AttackingTeam == 2 && ctf.ObjectiveTeam2 != null && ctf.ObjectiveTeam2.PlayerInRange(fob.Position)))
                    {
                        goto add;
                    }
                }
                else if (Data.Is(out Gamemodes.Insurgency.Insurgency ins))
                {
                    for (int i = 0; i < ins.Caches.Count; i++)
                    {
                        Gamemodes.Insurgency.Insurgency.CacheData cache = ins.Caches[i];
                        if (cache != null && cache.IsActive && F.SqrDistance2D(fob.Position, ctf.ObjectiveTeam1.Position) <= 100f)
                            goto add;
                    }
                }
            }
            return;
        add:
            _fobsBuilt++;
            if (_fobsBuilt >= BuildCount)
                TellCompleted();
            else
                TellUpdated();
        }
        public override string Translate() => QuestData.Translate(_player, _fobsBuilt, BuildCount);
    }
}
[QuestData(EQuestType.DELIVER_SUPPLIES)]
public class DeliverSuppliesQuest : BaseQuestData<DeliverSuppliesQuest.Tracker, DeliverSuppliesQuest.State, DeliverSuppliesQuest>
{
    public DynamicIntegerValue SupplyCount;
    public override int TickFrequencySeconds => 0;
    public override Tracker CreateQuestTracker(UCPlayer player, ref State state) => new Tracker(player, ref state);
    public override void OnPropertyRead(string propertyname, ref Utf8JsonReader reader)
    {
        if (propertyname.Equals("supply_count", StringComparison.Ordinal))
        {
            if (!reader.TryReadIntegralValue(out SupplyCount))
                SupplyCount = new DynamicIntegerValue(10);
        }
    }
    public struct State : IQuestState<Tracker, DeliverSuppliesQuest>
    {
        public IDynamicValue<int>.IChoice SupplyCount;
        public void Init(DeliverSuppliesQuest data)
        {
            this.SupplyCount = data.SupplyCount.GetValue();
        }
        public void OnPropertyRead(ref Utf8JsonReader reader, string prop)
        {
            if (prop.Equals("supply_count", StringComparison.Ordinal))
                SupplyCount = DynamicIntegerValue.ReadChoice(ref reader);
        }
        public void WriteQuestState(Utf8JsonWriter writer)
        {
            writer.WriteProperty("supply_count", SupplyCount);
        }
    }
    public class Tracker : BaseQuestTracker, INotifySuppliesConsumed
    {
        private readonly int SupplyCount = 0;
        private int _suppliesDelivered;
        public override void ResetToDefaults() => _suppliesDelivered = 0;
        public Tracker(UCPlayer target, ref State questState) : base(target)
        {
            SupplyCount = questState.SupplyCount.InsistValue();
        }
        public override void OnReadProgressSaveProperty(string prop, ref Utf8JsonReader reader)
        {
            if (reader.TokenType == JsonTokenType.Number && prop.Equals("supplies_delivered", StringComparison.Ordinal))
                _suppliesDelivered = reader.GetInt32();
        }
        public override void WriteQuestProgress(Utf8JsonWriter writer)
        {
            writer.WriteProperty("supplies_delivered", _suppliesDelivered);
        }
        public void OnSuppliesConsumed(Components.FOB fob, ulong player, int amount)
        {
            if (player == _player.Steam64)
            {
                _suppliesDelivered += amount;
                if (_suppliesDelivered >= SupplyCount)
                    TellCompleted();
                else
                    TellUpdated();
            }
        }
        public override string Translate() => QuestData.Translate(_player, _suppliesDelivered, SupplyCount);
    }
    public enum ESupplyType : byte { AMMO, BUILD }
}
[QuestData(EQuestType.SHOVEL_BUILDABLES)]
public class HelpBuildQuest : BaseQuestData<HelpBuildQuest.Tracker, HelpBuildQuest.State, HelpBuildQuest>
{
    public DynamicIntegerValue Amount;
    public DynamicAssetValue<ItemBarricadeAsset> BaseIDs = new DynamicAssetValue<ItemBarricadeAsset>(EDynamicValueType.ANY);
    public DynamicEnumValue<EBuildableType> BuildableType = new DynamicEnumValue<EBuildableType>(EDynamicValueType.ANY, EChoiceBehavior.ALLOW_ALL);
    public override int TickFrequencySeconds => 0;
    public override Tracker CreateQuestTracker(UCPlayer player, ref State state) => new Tracker(player, ref state);
    public override void OnPropertyRead(string propertyname, ref Utf8JsonReader reader)
    {
        if (propertyname.Equals("buildables_required", StringComparison.Ordinal))
        {
            if (!reader.TryReadIntegralValue(out Amount))
                Amount = new DynamicIntegerValue(250);
        }
        else if (propertyname.Equals("buildable_type", StringComparison.Ordinal))
        {
            if (!reader.TryReadEnumValue(out BuildableType))
                BuildableType = new DynamicEnumValue<EBuildableType>(EDynamicValueType.ANY, EChoiceBehavior.ALLOW_ONE);
        }
        else if (propertyname.Equals("base_ids", StringComparison.Ordinal))
        {
            if (!reader.TryReadAssetValue(out BaseIDs))
                BaseIDs = new DynamicAssetValue<ItemBarricadeAsset>(EDynamicValueType.ANY, EChoiceBehavior.ALLOW_ALL);
        }
    }
    public struct State : IQuestState<Tracker, HelpBuildQuest>
    {
        public IDynamicValue<int>.IChoice Amount;
        public DynamicAssetValue<ItemBarricadeAsset>.Choice BaseIDs;
        public IDynamicValue<EBuildableType>.IChoice BuildableType;
        public void Init(HelpBuildQuest data)
        {
            this.Amount = data.Amount.GetValue();
            this.BaseIDs = data.BaseIDs.GetValue();
            this.BuildableType = data.BuildableType.GetValue();
        }
        public void OnPropertyRead(ref Utf8JsonReader reader, string prop)
        {
            if (prop.Equals("successful_hits", StringComparison.Ordinal))
                Amount = DynamicIntegerValue.ReadChoice(ref reader);
        }
        public void WriteQuestState(Utf8JsonWriter writer)
        {
            writer.WriteProperty("successful_hits", Amount);
        }
    }
    public class Tracker : BaseQuestTracker, INotifyBuildableBuilt
    {
        private readonly int Amount = 0;
        private readonly DynamicAssetValue<ItemBarricadeAsset>.Choice BaseIDs;
        private readonly IDynamicValue<EBuildableType>.IChoice BuildableType;
        private int _built;
        public override void ResetToDefaults() => _built = 0;
        public Tracker(UCPlayer target, ref State questState) : base(target)
        {
            Amount = questState.Amount.InsistValue();
            BaseIDs = questState.BaseIDs;
            BuildableType = questState.BuildableType;
        }
        public override void OnReadProgressSaveProperty(string prop, ref Utf8JsonReader reader)
        {
            if (reader.TokenType == JsonTokenType.Number && prop.Equals("hits", StringComparison.Ordinal))
                _built = reader.GetInt32();
        }
        public override void WriteQuestProgress(Utf8JsonWriter writer)
        {
            writer.WriteProperty("buildables_built", _built);
        }
        public void OnBuildableBuilt(UCPlayer player, BuildableData buildable)
        {
            if (player.Steam64 == _player.Steam64 && BuildableType.IsMatch(buildable.type) && BaseIDs.IsMatch(buildable.foundationID))
            {
                _built ++;
                if (_built >= Amount)
                    TellCompleted();
                else
                    TellUpdated();
            }
        }
        public override string Translate() => QuestData.Translate(_player, _built, Amount);
    }
}