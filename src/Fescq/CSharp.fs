namespace Fescq.CSharp

open System
open Fescq.Core


[<AbstractClass; Sealed>]
type Storage private () =

   static member GetEvents (eventStore:IEventStore, aggId:Guid) =
      eventStore.GetEvents aggId
      |> function
         | Ok events -> struct (events |> List.toSeq, "")
         | Error msg -> struct ([] |> List.toSeq, msg)

   static member AddEvent (eventStore:IEventStore, event:Event) =
      eventStore.AddEvent event
      |> function
         | Ok _ -> struct (true, "")
         | Error msg -> struct (false, msg)

   static member Save (eventStore:IEventStore) =
      eventStore.Save ()
      |> function
         | Ok ok -> struct (true, "")
         | Error msg -> struct (false, msg)


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
