namespace Fescq

open System
open Fescq.Aggregate

type IRepository<'t> =

   abstract member Save : Agg<'t> * Event list -> Result<Agg<'t>, string>

   /// input: aggregate ID * factory
   /// output: aggregate
   abstract member Load : Guid * (Event list -> Result<Agg<'t>, string>) -> Result<Agg<'t>, string>


type Repository<'t> (storage:IEventStore) =

   interface IRepository<'t> with

      member x.Load (aggregateId:Guid, factory:(Event list -> Result<Agg<'t>, string>)) =
         aggregateId
         |> storage.GetEvents
         |> Result.bind (fun events ->
               events
               |> factory
            )

      member x.Save (aggregate:Agg<'t>, unpersisted:Event list) =
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

