using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;

namespace Infrastructure.EntityFramework.Files
{
    public class FileEntity
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public long Size { get; set; }
        public DateTime Timestamp { get; set; }
        public byte[] Blob { get; set; }
    }

    public class FileEntityConfig : IEntityTypeConfiguration<FileEntity>
    {
        public void Configure(EntityTypeBuilder<FileEntity> builder)
        {
            builder.HasKey(x => x.Id);
            builder.ToTable("Files");
        }
    }
}
