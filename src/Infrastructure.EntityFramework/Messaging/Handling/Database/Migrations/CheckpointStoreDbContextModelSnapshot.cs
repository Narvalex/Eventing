﻿// <auto-generated />
using System;
using Infrastructure.EntityFramework.Messaging.Handling.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Infrastructure.EntityFramework.Migrations.CheckpointStoreDb
{
    [DbContext(typeof(CheckpointStoreDbContext))]
    partial class CheckpointStoreDbContextModelSnapshot : ModelSnapshot
    {
        protected override void BuildModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "3.1.0")
                .HasAnnotation("Relational:MaxIdentifierLength", 128)
                .HasAnnotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn);

            modelBuilder.Entity("Infrastructure.EntityFramework.Messaging.Handling.Database.CheckpointEntity", b =>
                {
                    b.Property<string>("SubscriptionId")
                        .HasColumnType("nvarchar(450)");

                    b.Property<long>("CommitPosition")
                        .HasColumnType("bigint");

                    b.Property<long>("EventNumber")
                        .HasColumnType("bigint");

                    b.Property<long>("PreparePosition")
                        .HasColumnType("bigint");

                    b.Property<DateTime>("TimeStamp")
                        .HasColumnType("datetime2");

                    b.Property<string>("Type")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.HasKey("SubscriptionId");

                    b.ToTable("Checkpoints");
                });
#pragma warning restore 612, 618
        }
    }
}
