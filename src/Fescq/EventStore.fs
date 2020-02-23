namespace Fescq

open System
open EventRegistryFuncs

type DtoTypeProvider = System.Func<string, int, Type option>
type GetEvents = System.Func<DtoTypeProvider, Guid, seq<Event>>
type AddEvent = System.Action<Event, struct (string * int), Type>

type EventStore (registry:EventRegistry, getEvents:GetEvents, addEvent:AddEvent, save:Action) =
   interface IEventStore with

      member x.GetEvents aggregateId : Result<Event list, string> =
         try
            let dtoTypeProvider = Func<string, int, Type option>(fun name version -> eventType registry name version)
            getEvents.Invoke(dtoTypeProvider, aggregateId)
            |> Seq.toList |> Ok
         with
            ex -> Error ex.Message

      member x.AddEvent e : Result<unit, string> =
         try
            let dtoType = e.EventData.GetType()

            eventRevision registry dtoType
            |> function
               | Some revision ->
                  addEvent.Invoke(e, revision, dtoType)
                  Ok ()
               | None ->
                  sprintf "event not registered for aggregate (Id=%A, Version=%d)" e.AggregateKey.Id e.AggregateKey.Version
                  |> Error

         with
            ex -> Error ex.Message

      member x.Save () : Result<unit, string> =
         try
            save.Invoke()
            Ok ()
         with
            ex -> Error ex.Message


module EventStoreCSharp =

   let GetEvents (eventStore:IEventStore, aggId:Guid) =
      eventStore.GetEvents aggId
      |> function
         | Ok events -> struct (Some events, None)
         | Error msg -> struct (None, Some msg)

   let AddEvent (eventStore:IEventStore, event:Event) =
      eventStore.AddEvent event
      |> function
         | Ok _ -> struct (Some 1, None)
         | Error msg -> struct (None, Some msg)

   let Save (eventStore:IEventStore) =
      eventStore.Save ()
      |> function
         | Ok ok -> struct (Some ok, None)
         | Error msg -> struct (None, Some msg)

