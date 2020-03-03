module Fescq.EventStore

open System
open Core
open EventRegistry


/// event name -> event version -> maybe type
type DtoTypeProvider = string -> int -> Type option

// event -> (event name, event version) -> dto type -> unit
type EventAppender = Event -> struct (string * int) -> Type -> unit

let create (registry:RegisteredEvents) (dtoTypeProvider:DtoTypeProvider) (getEvents:DtoTypeProvider -> Guid -> seq<Event>) (addEvent:EventAppender) (save:unit -> unit) =

   let getEvents aggregateId : Result<Event list, string> =
      try
         getEvents dtoTypeProvider aggregateId
         |> Seq.toList |> Ok
      with
         ex -> Error ex.Message

   
   let addEvent e : Result<unit, string> =
      try
         let dtoType = e.EventData.GetType()

         eventRevision registry dtoType
         |> function
            | Some revision ->
               addEvent e revision dtoType
               Ok ()
            | None ->
               sprintf "event not registered for aggregate (Id=%A, Version=%d)" e.AggregateKey.Id e.AggregateKey.Version
               |> Error

      with
         ex -> Error ex.Message


   let save () : Result<unit, string> =
      try
         save() |> Ok         
      with
         ex -> Error ex.Message


   { EventStore.Registry = registry
     GetEvents = getEvents
     AddEvent = addEvent
     Save = save }
