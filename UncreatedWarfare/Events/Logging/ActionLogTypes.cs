namespace Uncreated.Warfare.Events.Logging;

public static class ActionLogTypes
{
    public static ActionLogType Punch                   { get; } = new ActionLogType("Player punched",                      "PUNCH",                        1);
    public static ActionLogType BuildableDestroyed      { get; } = new ActionLogType("Buildable destroyed",                 "BUILDABLE_DESTROYED",          2);
    public static ActionLogType BuildablePlaced         { get; } = new ActionLogType("Buildable placed",                    "BUILDABLE_PLACED",             3);
    public static ActionLogType BuildableTransformed    { get; } = new ActionLogType("Buildable transformed",               "BUILDABLE_TRANSFORMED",        4);
    public static ActionLogType BuildableSignChanged    { get; } = new ActionLogType("Sign text changed",                   "BUILDABLE_SIGN_TEXT_CHANGED",  5);
    public static ActionLogType TrapTriggered           { get; } = new ActionLogType("Trap triggered",                      "TRAP_TRIGGERED",               6);
    public static ActionLogType FlagCaptured            { get; } = new ActionLogType("Flag captured",                       "FLAG_CAPTURED",                7);
    public static ActionLogType FlagNeutralized         { get; } = new ActionLogType("Flag neutralized",                    "FLAG_NEUTRALIZED",             8);
    public static ActionLogType FlagStateChanged        { get; } = new ActionLogType("Flag state changed",                  "FLAG_STATE_CHANGED",           9);
    public static ActionLogType FlagsSetUp              { get; } = new ActionLogType("Flags set up",                        "FLAGS_LOADED",                 10);
    public static ActionLogType ObjectiveChanged        { get; } = new ActionLogType("Objective changed",                   "OBJECTIVE_CHANGED",            11);
    public static ActionLogType PlayerEnteredObjective  { get; } = new ActionLogType("Player entered objective",            "ENTERED_OBJECTIVE",            12);
    public static ActionLogType PlayerExitedObjective   { get; } = new ActionLogType("Player exited objective",             "EXITED_OBJECTIVE",             13);
    public static ActionLogType FobBuilt                { get; } = new ActionLogType("FOB shoveled",                        "FOB_BUILT",                    14);
    public static ActionLogType FobCreated              { get; } = new ActionLogType("FOB created",                         "FOB_CREATED",                  15);
    public static ActionLogType FobRemoved              { get; } = new ActionLogType("FOB removed",                         "FOB_REMOVED",                  16);
    public static ActionLogType FobDestroyed            { get; } = new ActionLogType("FOB destroyed",                       "FOB_DESTROYED",                17);
    public static ActionLogType FobUpdated              { get; } = new ActionLogType("FOB updated",                         "FOB_UPDATED",                  18);
    public static ActionLogType ShovelableBuilt         { get; } = new ActionLogType("Buildable shoveled",                  "SHOVELABLE_BUILT",             19);
    public static ActionLogType KitRearmed              { get; } = new ActionLogType("Kit rearmed",                         "KIT_REARMED",                  20);
    public static ActionLogType DroppedItem             { get; } = new ActionLogType("Dropped item",                        "DROPPED_ITEM",                 21);
    public static ActionLogType ChangedKit              { get; } = new ActionLogType("Changed kit",                         "KIT_CHANGED",                  22);
    public static ActionLogType AidedPlayer             { get; } = new ActionLogType("Aided player",                        "AIDED_PLAYER",                 23);
    public static ActionLogType Chat                    { get; } = new ActionLogType("Send chat message",                   "CHAT",                         24);
    public static ActionLogType PlayerDamaged           { get; } = new ActionLogType("Damaged player",                      "DAMAGED_PLAYER",               25);
    public static ActionLogType PlayerDeployed          { get; } = new ActionLogType("Player deployed",                     "PLAYER_DEPLOYED",              26);
    public static ActionLogType PlayerDied              { get; } = new ActionLogType("Player died",                         "DEATH",                        27);
    public static ActionLogType KilledPlayer            { get; } = new ActionLogType("Player killed",                       "KILL",                         28);
    public static ActionLogType Teamkilled              { get; } = new ActionLogType("Player teamkilled",                   "TEAMKILL",                     29);
    public static ActionLogType TryConnect              { get; } = new ActionLogType("Attempt to connect to the server",    "TRY_CONNECT",                  30);
    public static ActionLogType Connect                 { get; } = new ActionLogType("Fully connect to the server",         "CONNECT",                      31);
    public static ActionLogType Disconnect              { get; } = new ActionLogType("Disconnect from the server",          "DISCONNECT",                   32);
    public static ActionLogType ChangeTeam              { get; } = new ActionLogType("Change teams",                        "CHANGE_TEAM",                  33);
    public static ActionLogType EnterVehicle            { get; } = new ActionLogType("Enter vehicle",                       "ENTER_VEHICLE",                34);
    public static ActionLogType LeaveVehicle            { get; } = new ActionLogType("Exit vehicle",                        "EXIT_VEHICLE",                 35);
    public static ActionLogType SwapVehicleSeats        { get; } = new ActionLogType("Swap vehicle seats",                  "SWAP_VEHICLE_SEATS",           36);
    public static ActionLogType VehicleExploded         { get; } = new ActionLogType("Vehicle exploded",                    "VEHICLE_EXPLODED",             37);
    public static ActionLogType VehicleDespawned        { get; } = new ActionLogType("Vehicle despawned",                   "VEHICLE_DESPAWNED",            38);
    public static ActionLogType PlayerEnteredZone       { get; } = new ActionLogType("Player entered zone",                 "ENTERED_ZONE",                 39);
    public static ActionLogType PlayerExitedZone        { get; } = new ActionLogType("Player exited zone",                  "EXITED_ZONE",                  40);
    public static ActionLogType PlayerInjured           { get; } = new ActionLogType("Player injured",                      "INJURED",                      41);
    public static ActionLogType Melee                   { get; } = new ActionLogType("Player meleed",                       "MELEE",                        42);

    public static ActionLogType ChatGlobal              { get; } = new ActionLogType("Send global chat message",            "CHAT_GLOBAL",                  1);
    public static ActionLogType ChatAreaOrSquad         { get; } = new ActionLogType("Send area/squad chat message",        "CHAT_AREA_OR_SQUAD",           2);
    public static ActionLogType ChatGroup               { get; } = new ActionLogType("Send team chat message",              "CHAT_GROUP",                   3);
    public static ActionLogType RequestAmmo             { get; } = new ActionLogType("Request ammo",                        "REQUEST_AMMO",                 4);
    public static ActionLogType DutyChanged             { get; } = new ActionLogType("Update duty status",                  "DUTY_CHANGED",                 5);
    public static ActionLogType ModeratePlayer          { get; } = new ActionLogType("Apply moderation entry to player",    "MODERATE_PLAYER",              6);
    public static ActionLogType StartReport             { get; } = new ActionLogType("Start a player report",               "START_REPORT",                 7);
    public static ActionLogType ConfirmReport           { get; } = new ActionLogType("Complete a player report",            "CONFIRM_REPORT",               8);
    public static ActionLogType BuyKit                  { get; } = new ActionLogType("Purchase a kit",                      "BUY_KIT",                      9);
    public static ActionLogType ClearItems              { get; } = new ActionLogType("Clear ground items",                  "CLEAR_ITEMS",                  10);
    public static ActionLogType ClearInventory          { get; } = new ActionLogType("Clear player inventory",              "CLEAR_INVENTORY",              11);
    public static ActionLogType ClearVehicles           { get; } = new ActionLogType("Clear all vehicles",                  "CLEAR_VEHICLES",               12);
    public static ActionLogType ClearStructures         { get; } = new ActionLogType("Clear structures and barricades",     "CLEAR_STRUCTURES",             13);
    public static ActionLogType ChangeGroupWithCommand  { get; } = new ActionLogType("Change group via command",            "CHANGE_GROUP_WITH_COMMAND",    14);
    public static ActionLogType ChangeGroupInLobby      { get; } = new ActionLogType("Change group via lobby",              "CHANGE_GROUP_WITH_UI",         15);
    public static ActionLogType GiveItem                { get; } = new ActionLogType("Give the player an item",             "GIVE_ITEM",                    19);
}