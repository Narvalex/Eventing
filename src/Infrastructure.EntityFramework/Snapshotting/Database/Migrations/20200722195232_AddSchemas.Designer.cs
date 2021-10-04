﻿// <auto-generated />
using System;
using Infrastructure.EntityFramework.Snapshotting.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Infrastructure.EntityFramework.Migrations
{
    [DbContext(typeof(SnapshotStoreDbContext))]
    [Migration("20200722195232_AddSchemas")]
    partial class AddSchemas
    {
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "3.1.5")
                .HasAnnotation("Relational:MaxIdentifierLength", 128)
                .HasAnnotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn);

            modelBuilder.Entity("Infrastructure.EntityFramework.Snapshotting.Database.SchemaEntity", b =>
                {
                    b.Property<string>("Type")
                        .HasColumnType("nvarchar(450)");

                    b.Property<string>("Assembly")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<bool>("ThereAreStaleSnapshots")
                        .HasColumnType("bit");

                    b.Property<string>("Hash")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<int>("Version")
                        .HasColumnType("int");

                    b.HasKey("Type");

                    b.ToTable("Schemas");
                });

            modelBuilder.Entity("Infrastructure.EntityFramework.Snapshotting.Database.SnapshotEntity", b =>
                {
                    b.Property<string>("StreamName")
                        .HasColumnType("nvarchar(450)");

                    b.Property<string>("Assembly")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("Payload")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<int>("SchemaVersion")
                        .HasColumnType("int");

                    b.Property<DateTime>("Timestamp")
                        .HasColumnType("datetime2");

                    b.Property<string>("Type")
                        .IsRequired()
                        .HasColumnType("nvarchar(450)");

                    b.Property<long>("Version")
                        .HasColumnType("bigint");

                    b.HasKey("StreamName");

                    b.HasIndex("Type", "SchemaVersion");

                    b.ToTable("Snapshots");
                });
#pragma warning restore 612, 618
        }
    }
}
