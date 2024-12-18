﻿// <auto-generated />
using System;
using System.Text.Json;
using DatingClickerServerApp.Core.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace DatingClickerServerApp.Common.Migrations
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20241121000913_AddSuperLikeTextToDatingUserAction")]
    partial class AddSuperLikeTextToDatingUserAction
    {
        /// <inheritdoc />
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "8.0.10")
                .HasAnnotation("Relational:MaxIdentifierLength", 63);

            NpgsqlModelBuilderExtensions.UseIdentityByDefaultColumns(modelBuilder);

            modelBuilder.Entity("DatingClickerServerApp.Common.Model.BlacklistedDatingUser", b =>
                {
                    b.Property<Guid>("Id")
                        .HasColumnType("uuid");

                    b.Property<DateTime>("CreatedDate")
                        .HasColumnType("timestamp with time zone");

                    b.HasKey("Id");

                    b.ToTable("BlacklistedDatingUsers");
                });

            modelBuilder.Entity("DatingClickerServerApp.Common.Model.DatingAccount", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uuid")
                        .HasDefaultValueSql("gen_random_uuid()");

                    b.Property<string>("AppName")
                        .IsRequired()
                        .HasMaxLength(50)
                        .HasColumnType("character varying(50)");

                    b.Property<string>("AppUserId")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<DateTime>("CreatedDate")
                        .HasColumnType("timestamp with time zone");

                    b.Property<string>("JsonAuthData")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<JsonElement>("JsonProfileData")
                        .HasColumnType("jsonb");

                    b.Property<DateTime>("UpdatedDate")
                        .HasColumnType("timestamp with time zone");

                    b.HasKey("Id");

                    b.ToTable("DatingAccounts");
                });

            modelBuilder.Entity("DatingClickerServerApp.Common.Model.DatingUser", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uuid")
                        .HasDefaultValueSql("gen_random_uuid()");

                    b.Property<string>("About")
                        .HasMaxLength(500)
                        .HasColumnType("character varying(500)");

                    b.Property<int?>("Age")
                        .HasColumnType("integer");

                    b.Property<string>("CityName")
                        .HasMaxLength(100)
                        .HasColumnType("character varying(100)");

                    b.Property<DateTime>("CreatedDate")
                        .HasColumnType("timestamp with time zone");

                    b.Property<string>("ExternalId")
                        .IsRequired()
                        .HasMaxLength(50)
                        .HasColumnType("character varying(50)");

                    b.Property<bool?>("HasChildren")
                        .HasColumnType("boolean");

                    b.Property<int?>("Height")
                        .HasColumnType("integer");

                    b.Property<string[]>("Interests")
                        .HasColumnType("text[]");

                    b.Property<bool>("IsVerified")
                        .HasColumnType("boolean");

                    b.Property<JsonElement>("JsonData")
                        .HasColumnType("jsonb");

                    b.Property<string>("Name")
                        .HasMaxLength(100)
                        .HasColumnType("character varying(100)");

                    b.Property<string>("PreviewUrl")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<DateTime>("UpdatedDate")
                        .HasColumnType("timestamp with time zone");

                    b.HasKey("Id");

                    b.HasIndex("ExternalId")
                        .IsUnique();

                    b.ToTable("DatingUsers");
                });

            modelBuilder.Entity("DatingClickerServerApp.Common.Model.DatingUserAction", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uuid")
                        .HasDefaultValueSql("gen_random_uuid()");

                    b.Property<string>("ActionType")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<DateTime>("CreatedDate")
                        .HasColumnType("timestamp with time zone");

                    b.Property<Guid?>("DatingAccountId")
                        .HasColumnType("uuid");

                    b.Property<Guid>("DatingUserId")
                        .HasColumnType("uuid");

                    b.Property<string>("SuperLikeText")
                        .HasMaxLength(300)
                        .HasColumnType("character varying(300)");

                    b.HasKey("Id");

                    b.HasIndex("DatingAccountId");

                    b.HasIndex("DatingUserId");

                    b.ToTable("DatingUserActions");
                });

            modelBuilder.Entity("DatingClickerServerApp.Common.Model.BlacklistedDatingUser", b =>
                {
                    b.HasOne("DatingClickerServerApp.Common.Model.DatingUser", "DatingUser")
                        .WithOne("BlacklistedDatingUser")
                        .HasForeignKey("DatingClickerServerApp.Common.Model.BlacklistedDatingUser", "Id")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("DatingUser");
                });

            modelBuilder.Entity("DatingClickerServerApp.Common.Model.DatingUserAction", b =>
                {
                    b.HasOne("DatingClickerServerApp.Common.Model.DatingAccount", "DatingAccount")
                        .WithMany("Actions")
                        .HasForeignKey("DatingAccountId");

                    b.HasOne("DatingClickerServerApp.Common.Model.DatingUser", "DatingUser")
                        .WithMany("Actions")
                        .HasForeignKey("DatingUserId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("DatingAccount");

                    b.Navigation("DatingUser");
                });

            modelBuilder.Entity("DatingClickerServerApp.Common.Model.DatingAccount", b =>
                {
                    b.Navigation("Actions");
                });

            modelBuilder.Entity("DatingClickerServerApp.Common.Model.DatingUser", b =>
                {
                    b.Navigation("Actions");

                    b.Navigation("BlacklistedDatingUser");
                });
#pragma warning restore 612, 618
        }
    }
}
