module Fescq.Repository

open System
open Core
open EventStore

type IRepository<'t> =

   abstract member Save : Agg<'t> * Event list -> Result<Agg<'t>, string>

   /// input: aggregate ID * factory
   /// output: aggregate
   abstract member Load : Guid * (Event list -> Result<Agg<'t>, string>) -> Result<Agg<'t>, string>

   /// input: aggregate ID * factory * expectedVersion
   /// output: aggregate
   abstract member LoadExpectedVersion : Guid * (Event list -> Result<Agg<'t>, string>) * int -> Result<Agg<'t>, string>


type Repository<'t> (storage:EventStore) =

   let load (aggregateId:Guid) (factory:(Event list -> Result<Agg<'t>, string>)) =
      aggregateId
         |> storage.GetEvents
         |> Result.bind factory

   let loadExpectedVersion (aggregateId:Guid) (factory:(Event list -> Result<Agg<'t>, string>)) (expectedVersion:int) =
      load aggregateId factory
      |> Result.bind (fun x ->
            match x.Key.Version = expectedVersion with
            | true -> Ok x
            | false -> Error "aggregate version is different")

   let save (aggregate:Agg<'t>) (unpersisted:Event list) =
      if aggregate.History.Length > 0
         && unpersisted.Length > 0
         && (List.last unpersisted) = (List.last aggregate.History) then

         // bail at the first error
         // https://stackoverflow.com/a/26890974/571637
         unpersisted
         |> List.unfold (fun events ->
               match events with
               | head :: tail ->
                  match storage.AddEvent head with
                  | Ok _ -> Some (Ok (), tail)
                  | Error msg -> Some (Error msg, [])
               | [] -> None)

         |> List.last
         |> Result.bind storage.Save
         |> Result.bind (fun _ -> aggregate |> Ok)
      else
         Error "unpersisted events must be part of aggregate history"

   interface IRepository<'t> with

      member x.Load (aggregateId:Guid, factory:(Event list -> Result<Agg<'t>, string>)) =
         load aggregateId factory

      member x.LoadExpectedVersion (aggregateId:Guid, factory:(Event list -> Result<Agg<'t>, string>), expectedVersion:int) =
         loadExpectedVersion aggregateId factory expectedVersion

      member x.Save (aggregate:Agg<'t>, unpersisted:Event list) =
         save aggregate unpersisted
