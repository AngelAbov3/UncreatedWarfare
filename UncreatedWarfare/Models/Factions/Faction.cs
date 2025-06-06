using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Uncreated.Warfare.Models.Kits;

namespace Uncreated.Warfare.Models.Factions;

[Table("factions")]
public class Faction
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    [Column("pk")]
    public uint Key { get; set; }

    [Required]
    [MaxLength(16)]
    [Column("Id")]
    public string InternalName { get; set; } = null!;

    [MaxLength(32)]
    [Required]
    public string Name { get; set; }

    [MaxLength(8)]
    [Required]
    public string KitPrefix { get; set; }

    [MaxLength(24)]
    public string? ShortName { get; set; }

    [MaxLength(8)]
    [Required]
    public string Abbreviation { get; set; }

    [Column(TypeName = "char(6)")]
    [Required]
    public string HexColor { get; set; }

    public KitModel? UnarmedKit { get; set; }

    [ForeignKey(nameof(UnarmedKit))]
    [Column("UnarmedKitId")]
    public uint? UnarmedKitId { get; set; }

    [MaxLength(128)]
    [Required]
    public string FlagImageUrl { get; set; }

    public int? SpriteIndex { get; set; }

    [MaxLength(64)]
    public string? Emoji { get; set; }

    public IList<FactionAsset>? Assets { get; set; }
    public IList<FactionLocalization>? Translations { get; set; }
}