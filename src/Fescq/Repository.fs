module Fescq.Repository

open System
open Core

let create<'t> (eventStore:EventStore) =

   /// input: aggregate ID * factory
   /// output: aggregate
   let load (aggregateId:Guid) (factory:(Event list -> Result<Agg<'t>, string>)) =
      aggregateId
         |> eventStore.GetEvents
         |> Result.bind factory

   /// input: aggregate ID * factory * expectedVersion
   /// output: aggregate
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
                  match eventStore.AddEvent head with
                  | Ok _ -> Some (Ok (), tail)
                  | Error msg -> Some (Error msg, [])
               | [] -> None)

         |> List.last
         |> Result.bind eventStore.Save
         |> Result.bind (fun x -> x |> Ok)
      else
         Error "unpersisted events must be part of aggregate history"

   { Load = load
     LoadExpectedVersion = loadExpectedVersion
     Save = save }
