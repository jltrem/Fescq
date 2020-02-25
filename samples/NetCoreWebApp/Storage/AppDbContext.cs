using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using LanguageExt;
using static LanguageExt.Prelude;

namespace NetCoreWebApp.Storage
{
   public class AppDbContext : DbContext
   {
      public DbSet<AggregateEvent> AggregateEvents { get; set; }


      public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
      {
      }

      protected override void OnModelCreating(ModelBuilder builder)
      {
         var constaint = new ConstraintHelper();

         builder
            .Entity<AggregateEvent>()
            .HasKey(x => x.Id);

         builder
            .Entity<AggregateEvent>()
            .Property(p => p.Id)
            .HasColumnType("bigint");

         builder
            .Entity<AggregateEvent>()
            .HasIndex(x => x.RootId)
            .IncludeProperties(x => new
            {
               x.AggregateVersion,
               x.Timestamp,
               x.Id
            });

         builder.Entity<AggregateEvent>()
            .HasIndex(x => new { x.AggregateName });

         constaint.MaxLength(nameof(AggregateEvent.AggregateName)).IfSome(max =>
            builder.Entity<AggregateEvent>()
               .Property(x => x.AggregateName)
               .HasMaxLength(max));

         constaint.MaxLength(nameof(AggregateEvent.EventName)).IfSome(max =>
            builder.Entity<AggregateEvent>()
               .Property(x => x.EventName)
               .HasMaxLength(max));
      }
   }

   internal class ConstraintHelper
   {
      private readonly Map<string, int> _nameMaxLengthMap;

      public Option<int> MaxLength(string fieldName) =>
         _nameMaxLengthMap.ContainsKey(fieldName)
            ? Some(_nameMaxLengthMap[fieldName])
            : None;

      public ConstraintHelper()
      {
         _nameMaxLengthMap =
            new[]
            {
            (nameof(AggregateEvent.AggregateName), 40),
            (nameof(AggregateEvent.EventName), 40)
            }
            .Apply(toMap);
      }

   }
}
