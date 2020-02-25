using System;

namespace NetCoreWebApp.Storage
{
   public class AggregateEvent
   {
      public long Id { get; set; }
      public Guid RootId { get; set; }
      public int AggregateVersion { get; set; }
      public string AggregateName { get; set; }
      public string EventName { get; set; }
      public int EventVersion { get; set; }
      public string EventData { get; set; }
      public DateTimeOffset Timestamp { get; set; }
      public string MetaData { get; set; }
   }
}
