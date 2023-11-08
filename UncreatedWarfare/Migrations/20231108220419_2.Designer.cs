﻿// <auto-generated />
using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Uncreated.Warfare.Database;

namespace Uncreated.Warfare.Migrations
{
    [DbContext(typeof(WarfareDbContext))]
    [Migration("20231108220419_2")]
    partial class _2
    {
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "3.1.32")
                .HasAnnotation("Relational:MaxIdentifierLength", 64);

            modelBuilder.Entity("Uncreated.Warfare.Models.Localization.LanguageAlias", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnName("pk")
                        .HasColumnType("int");

                    b.Property<string>("Alias")
                        .IsRequired()
                        .HasColumnType("varchar(64) CHARACTER SET utf8mb4")
                        .HasMaxLength(64);

                    b.Property<int>("LanguageKey")
                        .HasColumnType("int");

                    b.HasKey("Id");

                    b.HasIndex("LanguageKey");

                    b.ToTable("lang_aliases");
                });

            modelBuilder.Entity("Uncreated.Warfare.Models.Localization.LanguageContributor", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnName("pk")
                        .HasColumnType("int");

                    b.Property<ulong>("Contributor")
                        .HasColumnType("bigint unsigned");

                    b.Property<int>("LanguageKey")
                        .HasColumnType("int");

                    b.HasKey("Id");

                    b.HasIndex("LanguageKey");

                    b.ToTable("lang_credits");
                });

            modelBuilder.Entity("Uncreated.Warfare.Models.Localization.LanguageCulture", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnName("pk")
                        .HasColumnType("int");

                    b.Property<string>("CultureCode")
                        .IsRequired()
                        .HasColumnType("varchar(16) CHARACTER SET utf8mb4")
                        .HasMaxLength(16);

                    b.Property<int>("LanguageKey")
                        .HasColumnType("int");

                    b.HasKey("Id");

                    b.HasIndex("LanguageKey");

                    b.ToTable("lang_cultures");
                });

            modelBuilder.Entity("Uncreated.Warfare.Models.Localization.LanguageInfo", b =>
                {
                    b.Property<int>("Key")
                        .ValueGeneratedOnAdd()
                        .HasColumnName("pk")
                        .HasColumnType("int");

                    b.Property<string>("Code")
                        .IsRequired()
                        .HasColumnType("char(5)");

                    b.Property<string>("DefaultCultureCode")
                        .HasColumnType("varchar(16) CHARACTER SET utf8mb4")
                        .HasMaxLength(16);

                    b.Property<string>("DisplayName")
                        .IsRequired()
                        .HasColumnType("varchar(64) CHARACTER SET utf8mb4")
                        .HasMaxLength(64);

                    b.Property<string>("FallbackTranslationLanguageCode")
                        .HasColumnType("char(5)");

                    b.Property<bool>("HasTranslationSupport")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("tinyint(1)")
                        .HasDefaultValue(false);

                    b.Property<string>("NativeName")
                        .HasColumnType("varchar(64) CHARACTER SET utf8mb4")
                        .HasMaxLength(64);

                    b.Property<bool>("RequiresIMGUI")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("tinyint(1)")
                        .HasDefaultValue(false);

                    b.Property<string>("SteamLanguageName")
                        .HasColumnType("varchar(32) CHARACTER SET utf8mb4")
                        .HasMaxLength(32);

                    b.HasKey("Key");

                    b.ToTable("lang_info");
                });

            modelBuilder.Entity("Uncreated.Warfare.Models.Localization.LanguagePreferences", b =>
                {
                    b.Property<ulong>("Steam64")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("bigint unsigned");

                    b.Property<string>("Culture")
                        .HasColumnType("varchar(16) CHARACTER SET utf8mb4")
                        .HasMaxLength(16);

                    b.Property<int?>("LanguageKey")
                        .HasColumnType("int");

                    b.Property<DateTime>("LastUpdated")
                        .HasColumnType("datetime(6)");

                    b.Property<bool>("UseCultureForCommandInput")
                        .ValueGeneratedOnAdd()
                        .HasColumnName("UseCultureForCmdInput")
                        .HasColumnType("tinyint(1)")
                        .HasDefaultValue(false);

                    b.HasKey("Steam64");

                    b.HasIndex("LanguageKey");

                    b.ToTable("lang_preferences");
                });

            modelBuilder.Entity("Uncreated.Warfare.Models.Teams.Faction", b =>
                {
                    b.Property<int>("Key")
                        .ValueGeneratedOnAdd()
                        .HasColumnName("pk")
                        .HasColumnType("int");

                    b.Property<string>("Abbreviation")
                        .HasColumnType("varchar(8) CHARACTER SET utf8mb4")
                        .HasMaxLength(8);

                    b.Property<string>("Emoji")
                        .HasColumnType("varchar(64) CHARACTER SET utf8mb4")
                        .HasMaxLength(64);

                    b.Property<string>("FlagImageUrl")
                        .HasColumnType("varchar(128) CHARACTER SET utf8mb4")
                        .HasMaxLength(128);

                    b.Property<string>("HexColor")
                        .HasColumnType("char(6)");

                    b.Property<string>("Id")
                        .IsRequired()
                        .HasColumnType("varchar(16) CHARACTER SET utf8mb4")
                        .HasMaxLength(16);

                    b.Property<string>("Name")
                        .HasColumnType("varchar(32) CHARACTER SET utf8mb4")
                        .HasMaxLength(32);

                    b.Property<string>("ShortName")
                        .HasColumnType("varchar(24) CHARACTER SET utf8mb4")
                        .HasMaxLength(24);

                    b.Property<int?>("SpriteIndex")
                        .HasColumnType("int");

                    b.Property<string>("UnarmedKit")
                        .HasColumnType("varchar(25) CHARACTER SET utf8mb4")
                        .HasMaxLength(25);

                    b.HasKey("Key");

                    b.ToTable("factions");
                });

            modelBuilder.Entity("Uncreated.Warfare.Models.Teams.FactionAsset", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnName("pk")
                        .HasColumnType("int");

                    b.Property<string>("Asset")
                        .IsRequired()
                        .HasColumnType("char(32)");

                    b.Property<int>("FactionKey")
                        .HasColumnType("int");

                    b.Property<string>("Redirect")
                        .IsRequired()
                        .HasColumnType("enum('Shirt','Pants','Vest','Hat','Mask','Backpack','Glasses','AmmoSupply','BuildSupply','RallyPoint','Radio','AmmoBag','AmmoCrate','RepairStation','Bunker','VehicleBay','EntrenchingTool','UAV','RepairStationBuilt','AmmoCrateBuilt','BunkerBuilt','Cache','RadioDamaged','LaserDesignator','StandardAmmoIcon','StandardMeleeIcon','StandardGrenadeIcon','StandardSmokeGrenadeIcon')");

                    b.Property<string>("VariantKey")
                        .HasColumnType("varchar(32) CHARACTER SET utf8mb4")
                        .HasMaxLength(32);

                    b.HasKey("Id");

                    b.HasIndex("FactionKey");

                    b.ToTable("faction_assets");
                });

            modelBuilder.Entity("Uncreated.Warfare.Models.Teams.FactionLocalization", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnName("pk")
                        .HasColumnType("int");

                    b.Property<string>("Abbreviation")
                        .HasColumnType("varchar(8) CHARACTER SET utf8mb4")
                        .HasMaxLength(8);

                    b.Property<int>("FactionKey")
                        .HasColumnType("int");

                    b.Property<int>("LanguageKey")
                        .HasColumnType("int");

                    b.Property<string>("Name")
                        .HasColumnType("varchar(32) CHARACTER SET utf8mb4")
                        .HasMaxLength(32);

                    b.Property<string>("ShortName")
                        .HasColumnType("varchar(24) CHARACTER SET utf8mb4")
                        .HasMaxLength(24);

                    b.HasKey("Id");

                    b.HasIndex("FactionKey");

                    b.HasIndex("LanguageKey");

                    b.ToTable("faction_translations");
                });

            modelBuilder.Entity("Uncreated.Warfare.Models.Users.WarfareUserData", b =>
                {
                    b.Property<ulong>("Steam64")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("bigint unsigned");

                    b.Property<string>("CharacterName")
                        .IsRequired()
                        .HasColumnType("varchar(30) CHARACTER SET utf8mb4")
                        .HasMaxLength(30);

                    b.Property<DateTime?>("FirstJoined")
                        .HasColumnType("datetime(6)");

                    b.Property<DateTime?>("LastJoined")
                        .HasColumnType("datetime(6)");

                    b.Property<string>("NickName")
                        .IsRequired()
                        .HasColumnType("varchar(30) CHARACTER SET utf8mb4")
                        .HasMaxLength(30);

                    b.Property<string>("PermissionLevel")
                        .IsRequired()
                        .ValueGeneratedOnAdd()
                        .HasColumnType("longtext CHARACTER SET utf8mb4")
                        .HasDefaultValue("Member");

                    b.Property<string>("PlayerName")
                        .IsRequired()
                        .HasColumnType("varchar(48) CHARACTER SET utf8mb4")
                        .HasMaxLength(48);

                    b.HasKey("Steam64");

                    b.ToTable("users");
                });

            modelBuilder.Entity("Uncreated.Warfare.Models.Localization.LanguageAlias", b =>
                {
                    b.HasOne("Uncreated.Warfare.Models.Localization.LanguageInfo", "Language")
                        .WithMany("Aliases")
                        .HasForeignKey("LanguageKey")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();
                });

            modelBuilder.Entity("Uncreated.Warfare.Models.Localization.LanguageContributor", b =>
                {
                    b.HasOne("Uncreated.Warfare.Models.Localization.LanguageInfo", "Language")
                        .WithMany("Contributors")
                        .HasForeignKey("LanguageKey")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();
                });

            modelBuilder.Entity("Uncreated.Warfare.Models.Localization.LanguageCulture", b =>
                {
                    b.HasOne("Uncreated.Warfare.Models.Localization.LanguageInfo", "Language")
                        .WithMany("SupportedCultures")
                        .HasForeignKey("LanguageKey")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();
                });

            modelBuilder.Entity("Uncreated.Warfare.Models.Localization.LanguagePreferences", b =>
                {
                    b.HasOne("Uncreated.Warfare.Models.Localization.LanguageInfo", "Language")
                        .WithMany()
                        .HasForeignKey("LanguageKey");
                });

            modelBuilder.Entity("Uncreated.Warfare.Models.Teams.FactionAsset", b =>
                {
                    b.HasOne("Uncreated.Warfare.Models.Teams.Faction", "Faction")
                        .WithMany("Assets")
                        .HasForeignKey("FactionKey")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();
                });

            modelBuilder.Entity("Uncreated.Warfare.Models.Teams.FactionLocalization", b =>
                {
                    b.HasOne("Uncreated.Warfare.Models.Teams.Faction", "Faction")
                        .WithMany("Translations")
                        .HasForeignKey("FactionKey")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("Uncreated.Warfare.Models.Localization.LanguageInfo", "Language")
                        .WithMany()
                        .HasForeignKey("LanguageKey")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();
                });
#pragma warning restore 612, 618
        }
    }
}
