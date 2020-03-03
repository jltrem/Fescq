namespace Fescq.CSharp

open System
open Fescq.Core
open Fescq.EventRegistry

type DtoTypeProvider = System.Func<string, int, Type>
type GetEvents = System.Func<DtoTypeProvider, Guid, seq<Event>>
type AddEvent = System.Action<Event, struct (string * int), Type>

[<AbstractClass; Sealed>]
type Storage private () =

   static member CreateEventStore (registry:RegisteredEvents, getEvents:GetEvents, addEvent:AddEvent, save:Action) =

      let wrappedGetEvent aggregateId : Result<Event list, string> =
         try
            let dtoTypeProvider = Func<string, int, Type>(fun name version ->
               eventType registry name version
               |> function
                  | Some x -> x
                  | None _ -> null)

            getEvents.Invoke(dtoTypeProvider, aggregateId)
            |> Seq.toList |> Ok
         with
            ex -> Error ex.Message

      
      let wrappedAddEvent e : Result<unit, string> =
         try
            let dtoType = e.EventData.GetType()

            Fescq.EventRegistry.eventRevision registry dtoType
            |> function
               | Some revision ->
                  addEvent.Invoke(e, revision, dtoType)
                  Ok ()
               | None ->
                  sprintf "event not registered for aggregate (Id=%A, Version=%d)" e.AggregateKey.Id e.AggregateKey.Version
                  |> Error

         with
            ex -> Error ex.Message

      let wrappedSave () : Result<unit, string> =
         try
            save.Invoke()
            Ok ()
         with
            ex -> Error ex.Message

      { EventStore.Registry = registry
        GetEvents = wrappedGetEvent
        AddEvent = wrappedAddEvent
        Save = wrappedSave }


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
