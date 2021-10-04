﻿// <auto-generated />
using System;
using Infrastructure.EntityFramework.EventStorage.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Infrastructure.EntityFramework.Migrations.EventStoreDb
{
    [DbContext(typeof(EventStoreDbContext))]
    partial class EventStoreDbContextModelSnapshot : ModelSnapshot
    {
        protected override void BuildModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "2.1.3-rtm-32065")
                .HasAnnotation("Relational:MaxIdentifierLength", 128)
                .HasAnnotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn);

            modelBuilder.Entity("Infrastructure.EntityFramework.EventStorage.Database.EventDescriptor", b =>
                {
                    b.Property<string>("Category");

                    b.Property<string>("SourceId");

                    b.Property<long>("Version");

                    b.Property<string>("EventType");

                    b.Property<string>("Metadata");

                    b.Property<string>("Payload");

                    b.Property<long>("Position");

                    b.Property<DateTime>("TimeStamp");

                    b.HasKey("Category", "SourceId", "Version");

                    b.HasIndex("Position");

                    b.ToTable("Events");
                });
#pragma warning restore 612, 618
        }
    }
}
