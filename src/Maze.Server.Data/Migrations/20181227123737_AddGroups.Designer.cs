﻿// <auto-generated />
using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Orcus.Server.Data.EfCode;

namespace Orcus.Server.Data.Migrations
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20181227123737_AddGroups")]
    partial class AddGroups
    {
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "2.1.4-rtm-31024");

            modelBuilder.Entity("Orcus.Server.Data.EfClasses.Account", b =>
                {
                    b.Property<int>("AccountId")
                        .ValueGeneratedOnAdd();

                    b.Property<DateTime>("CreatedOn")
                        .ValueGeneratedOnAdd()
                        .HasDefaultValueSql("CURRENT_TIMESTAMP");

                    b.Property<bool>("IsEnabled");

                    b.Property<string>("Password")
                        .IsRequired()
                        .HasColumnType("char(60)");

                    b.Property<DateTime>("TokenValidityPeriod")
                        .ValueGeneratedOnAdd()
                        .HasDefaultValueSql("CURRENT_TIMESTAMP");

                    b.Property<string>("Username")
                        .IsRequired()
                        .HasMaxLength(64);

                    b.HasKey("AccountId");

                    b.HasIndex("Username")
                        .IsUnique();

                    b.ToTable("Accounts");
                });

            modelBuilder.Entity("Orcus.Server.Data.EfClasses.Client", b =>
                {
                    b.Property<int>("ClientId")
                        .ValueGeneratedOnAdd();

                    b.Property<DateTimeOffset>("CreatedOn")
                        .ValueGeneratedOnAdd()
                        .HasDefaultValueSql("CURRENT_TIMESTAMP");

                    b.Property<string>("HardwareId")
                        .IsRequired()
                        .HasColumnType("char(64)");

                    b.Property<string>("MacAddress");

                    b.Property<string>("OperatingSystem");

                    b.Property<string>("SystemLanguage");

                    b.Property<string>("Username")
                        .IsRequired();

                    b.HasKey("ClientId");

                    b.HasIndex("HardwareId")
                        .IsUnique();

                    b.ToTable("Clients");
                });

            modelBuilder.Entity("Orcus.Server.Data.EfClasses.ClientConfiguration", b =>
                {
                    b.Property<int>("ClientConfigurationId")
                        .ValueGeneratedOnAdd();

                    b.Property<int?>("ClientGroupId");

                    b.Property<string>("Content")
                        .IsRequired();

                    b.Property<long>("ContentHash");

                    b.Property<DateTimeOffset>("UpdatedOn");

                    b.HasKey("ClientConfigurationId");

                    b.HasIndex("ClientGroupId")
                        .IsUnique();

                    b.ToTable("ClientConfiguration");
                });

            modelBuilder.Entity("Orcus.Server.Data.EfClasses.ClientGroup", b =>
                {
                    b.Property<int>("ClientGroupId")
                        .ValueGeneratedOnAdd();

                    b.Property<string>("Name")
                        .IsRequired();

                    b.HasKey("ClientGroupId");

                    b.HasIndex("Name")
                        .IsUnique();

                    b.ToTable("Groups");
                });

            modelBuilder.Entity("Orcus.Server.Data.EfClasses.ClientGroupMembership", b =>
                {
                    b.Property<int>("ClientId");

                    b.Property<int>("ClientGroupId");

                    b.HasKey("ClientId", "ClientGroupId");

                    b.HasIndex("ClientGroupId");

                    b.ToTable("ClientGroupMembership");
                });

            modelBuilder.Entity("Orcus.Server.Data.EfClasses.ClientSession", b =>
                {
                    b.Property<int>("ClientSessionId")
                        .ValueGeneratedOnAdd();

                    b.Property<int>("ClientId");

                    b.Property<string>("ClientPath");

                    b.Property<string>("ClientVersion");

                    b.Property<DateTimeOffset>("CreatedOn")
                        .ValueGeneratedOnAdd()
                        .HasDefaultValueSql("CURRENT_TIMESTAMP");

                    b.Property<string>("IpAddress");

                    b.Property<bool>("IsAdministrator");

                    b.HasKey("ClientSessionId");

                    b.HasIndex("ClientId");

                    b.ToTable("ClientSession");
                });

            modelBuilder.Entity("Orcus.Server.Data.EfClasses.ClientConfiguration", b =>
                {
                    b.HasOne("Orcus.Server.Data.EfClasses.ClientGroup", "ClientGroup")
                        .WithMany()
                        .HasForeignKey("ClientGroupId");
                });

            modelBuilder.Entity("Orcus.Server.Data.EfClasses.ClientGroupMembership", b =>
                {
                    b.HasOne("Orcus.Server.Data.EfClasses.ClientGroup", "ClientGroup")
                        .WithMany("ClientGroupMemberships")
                        .HasForeignKey("ClientGroupId")
                        .OnDelete(DeleteBehavior.Cascade);

                    b.HasOne("Orcus.Server.Data.EfClasses.Client", "Client")
                        .WithMany("ClientGroupMemberships")
                        .HasForeignKey("ClientId")
                        .OnDelete(DeleteBehavior.Cascade);
                });

            modelBuilder.Entity("Orcus.Server.Data.EfClasses.ClientSession", b =>
                {
                    b.HasOne("Orcus.Server.Data.EfClasses.Client", "Client")
                        .WithMany("ClientSessions")
                        .HasForeignKey("ClientId")
                        .OnDelete(DeleteBehavior.Cascade);
                });
#pragma warning restore 612, 618
        }
    }
}