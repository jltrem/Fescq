module Fescq.EventStore

open System
open Fescq.EventRegistry
open Core

type DtoTypeProvider = System.Func<string, int, Type option>
type GetEvents = System.Func<DtoTypeProvider, Guid, seq<Event>>
type AddEvent = System.Action<Event, struct (string * int), Type>

type EventStore (registry:RegisteredEvents, getEvents:GetEvents, addEvent:AddEvent, save:Action) =
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

