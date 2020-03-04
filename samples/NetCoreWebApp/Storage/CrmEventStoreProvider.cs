using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using LanguageExt;
using static LanguageExt.Prelude;
using static Fescq.Core;
using static Fescq.CSharp.Storage;


namespace NetCoreWebApp.Storage
{
   public class CrmEventStoreProvider
   {
      private readonly AppDbContext _db;

      public EventStore EventStore { get; }

      public CrmEventStoreProvider(AppDbContext db, CrmEventRegistry registry)
      {
         _db = db;
         EventStore = CreateEventStore(registry.Registry, GetEvents, AddEvent, Save);
      }

      private static string SerializeEventDto(IEventData eventData, Type dtoType) =>
         JsonConvert.SerializeObject(eventData, dtoType, new JsonSerializerSettings
         {
            Formatting = Formatting.None,
            TypeNameHandling = TypeNameHandling.None
         });

      private static Event DeserializeEventDto(AggregateEvent aggEvent, Type dtoType)
      {
         var dto = JsonConvert.DeserializeObject(aggEvent.EventData, dtoType) as IEventData;
         if (dto == null) throw new Exception($"event could not be deserialized: (rootId={aggEvent.RootId}, version={aggEvent.AggregateVersion})");

         var info = new AggregateKey(aggEvent.AggregateName, aggEvent.RootId, aggEvent.AggregateVersion);
         return new Event(info, aggEvent.Timestamp, aggEvent.MetaData, dto);
      }

      private IEnumerable<Event> GetEvents(Func<string, int, Type> dtoTypeProvider, Guid aggregateId) =>
         _db.AggregateEvents
            .Where(x => x.RootId == aggregateId)
            .OrderBy(x => x.AggregateVersion)
            .ToList()
            .Bind(x =>

               Optional(dtoTypeProvider(x.EventName, x.EventVersion))
                  .Map(dtoType => DeserializeEventDto(x, dtoType))
            );

      private void AddEvent(Event e, (string name, int version) revision, Type dtoType)
      {
         var data = new AggregateEvent
         {
            RootId = e.AggregateKey.Id,
            AggregateVersion = e.AggregateKey.Version,
            AggregateName = e.AggregateKey.Name,
            EventName = revision.name,
            EventVersion = revision.version,
            EventData = SerializeEventDto(e.EventData, dtoType),
            Timestamp = e.Timestamp,
            MetaData = e.MetaData
         };

         _db.AggregateEvents.Add(data);
      }

      private void Save() =>
         _db.SaveChanges();
   }
}
