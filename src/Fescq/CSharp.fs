namespace Fescq.CSharp

open System
open Fescq.Core

type DtoTypeProvider = System.Func<string, int, Type>
type GetEvents = System.Func<DtoTypeProvider, Guid, seq<Event>>
type AddEvent = System.Action<Event, struct (string * int), Type>

[<AbstractClass; Sealed>]
type Storage private () =

   static member CreateEventStore (registry:RegisteredEvents, getEvents:GetEvents, addEvent:AddEvent, save:Action) =

      let wrappedGetEvent dtoTypeProvider aggregateId =

         let csharpDtoTypeProvider = Func<string, int, Type>(fun name version ->
            dtoTypeProvider name version
            |> function
               | Some x -> x
               | None _ -> null)

         getEvents.Invoke(csharpDtoTypeProvider, aggregateId)

      let wrappedAddEvent e revision dtoType =
         addEvent.Invoke(e, revision, dtoType)
         ()
      
      let wrappedSave () =
         save.Invoke()
         ()

      Fescq.EventStore.create registry wrappedGetEvent wrappedAddEvent wrappedSave


[<AbstractClass; Sealed>]
type EventRegistry private () =

   static member Create (mappings:seq<EventTypeInfo>) =
      Fescq.EventRegistry.create mappings

   static member CreateForAssemblies (assemblies:seq<System.Reflection.Assembly>) =
      Fescq.EventRegistry.createForAssemblies assemblies

   static member CreateEventType (registry:RegisteredEvents, name:string, version:int) =
      Fescq.EventRegistry.eventType registry name version
      |> function
         | Some x -> x
         | None _ -> failwith "no event found for specified name and version"

   static member CreateEventRevision (registry:RegisteredEvents, dataType:Type) =
      Fescq.EventRegistry.eventRevision registry dataType
      |> function
         | Some x -> x
         | None _ -> failwith "no event found for specified type"
