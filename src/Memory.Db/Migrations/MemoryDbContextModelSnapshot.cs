﻿// <auto-generated />
using System;
using Memory.Db;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

#nullable disable

namespace Memory.Db.Migrations
{
    [DbContext(typeof(MemoryDbContext))]
    partial class MemoryDbContextModelSnapshot : ModelSnapshot
    {
        protected override void BuildModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder.HasAnnotation("ProductVersion", "8.0.0");

            modelBuilder.Entity("Memory.Db.Memory", b =>
                {
                    b.Property<long>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<DateTime>("CreationTime")
                        .HasColumnType("TEXT");

                    b.Property<int>("Day")
                        .ValueGeneratedOnAddOrUpdate()
                        .HasColumnType("INTEGER")
                        .HasComputedColumnSql("CAST(strftime('%d', CreationTime) AS INTEGER)");

                    b.Property<string>("FileContentHash")
                        .HasColumnType("TEXT");

                    b.Property<string>("FileExtension")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<string>("FilePath")
                        .IsRequired()
                        .HasMaxLength(300)
                        .HasColumnType("TEXT");

                    b.Property<string>("FilePathHash")
                        .IsRequired()
                        .HasMaxLength(32)
                        .HasColumnType("TEXT");

                    b.Property<long>("FileSize")
                        .HasColumnType("INTEGER");

                    b.Property<bool>("IsTaggedByFace")
                        .HasColumnType("INTEGER");

                    b.Property<int>("Likes")
                        .HasColumnType("INTEGER");

                    b.Property<int>("Month")
                        .ValueGeneratedOnAddOrUpdate()
                        .HasColumnType("INTEGER")
                        .HasComputedColumnSql("CAST(strftime('%m', CreationTime) AS INTEGER)");

                    b.Property<int>("Views")
                        .HasColumnType("INTEGER");

                    b.Property<int>("Year")
                        .ValueGeneratedOnAddOrUpdate()
                        .HasColumnType("INTEGER")
                        .HasComputedColumnSql("CAST(strftime('%Y', CreationTime) AS INTEGER)");

                    b.HasKey("Id");

                    b.HasIndex("CreationTime");

                    b.HasIndex("Day");

                    b.HasIndex("FileContentHash");

                    b.HasIndex("FilePathHash");

                    b.HasIndex("Month");

                    b.HasIndex("Year");

                    b.ToTable("Memories");
                });

            modelBuilder.Entity("Memory.Db.MemoryMeta", b =>
                {
                    b.Property<long>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<DateTime?>("DateTimeOriginal")
                        .HasColumnType("TEXT");

                    b.Property<double?>("Latitude")
                        .HasColumnType("REAL");

                    b.Property<string>("LensModal")
                        .HasColumnType("TEXT");

                    b.Property<double?>("Longitude")
                        .HasColumnType("REAL");

                    b.Property<string>("Make")
                        .HasColumnType("TEXT");

                    b.Property<long>("MemoryId")
                        .HasColumnType("INTEGER");

                    b.Property<string>("Modal")
                        .HasColumnType("TEXT");

                    b.Property<string>("OffsetTimeOriginal")
                        .HasColumnType("TEXT");

                    b.HasKey("Id");

                    b.HasIndex("MemoryId")
                        .IsUnique();

                    b.ToTable("MemoryMetas");
                });

            modelBuilder.Entity("Memory.Db.MemoryTag", b =>
                {
                    b.Property<int>("TagId")
                        .HasColumnType("INTEGER");

                    b.Property<long>("MemoryId")
                        .HasColumnType("INTEGER");

                    b.HasKey("TagId", "MemoryId");

                    b.HasIndex("MemoryId");

                    b.ToTable("MemoryTags");
                });

            modelBuilder.Entity("Memory.Db.Tag", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasMaxLength(20)
                        .HasColumnType("TEXT");

                    b.HasKey("Id");

                    b.ToTable("Tags");
                });

            modelBuilder.Entity("Memory.Db.User", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<int>("LockoutRetryCount")
                        .HasColumnType("INTEGER");

                    b.Property<DateTime?>("LockoutTime")
                        .HasColumnType("TEXT");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<string>("Password")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.HasKey("Id");

                    b.ToTable("Users");
                });

            modelBuilder.Entity("Memory.Db.MemoryMeta", b =>
                {
                    b.HasOne("Memory.Db.Memory", "Memory")
                        .WithOne("MemoryMeta")
                        .HasForeignKey("Memory.Db.MemoryMeta", "MemoryId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Memory");
                });

            modelBuilder.Entity("Memory.Db.MemoryTag", b =>
                {
                    b.HasOne("Memory.Db.Memory", "Memory")
                        .WithMany("MemoryTags")
                        .HasForeignKey("MemoryId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("Memory.Db.Tag", "Tag")
                        .WithMany("MemoryTags")
                        .HasForeignKey("TagId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Memory");

                    b.Navigation("Tag");
                });

            modelBuilder.Entity("Memory.Db.Memory", b =>
                {
                    b.Navigation("MemoryMeta");

                    b.Navigation("MemoryTags");
                });

            modelBuilder.Entity("Memory.Db.Tag", b =>
                {
                    b.Navigation("MemoryTags");
                });
#pragma warning restore 612, 618
        }
    }
}
