module Fescq.Aggregate

open Core

type AggregateProjection<'entity> = Projection<EntityState<'entity>, Event>

type private CreateCfg<'entity> = {
   Key: AggregateKey
   Events: Event list
   Projection: AggregateProjection<'entity>
}

// TODO: consider how to handle hydrating an aggregate when the event validation rules have changed
// so that an historical event is now considered invalid.  Is this a real concern?

/// Create the aggregate using the provided projection.
/// This projection function must validate domain rules so that construction is successful IFF the state is valid.
///
/// Note that historical events should always pass this validation, as they were subjected to it when they
/// were appended (assuming validation rules have not changed); but the future events must fail (throw an exception)
/// if they put the aggregate in an invalid state.
let private create<'entity> (cfg:CreateCfg<'entity>) : Agg<'entity> =

   cfg.Events
   |> List.tail
   |> List.fold cfg.Projection.Update cfg.Projection.Init
   |> fun state ->
      { Key = cfg.Key
        Entity = state.Entity
        History = cfg.Events }


let private makeCreateCfg (projectionFactory: Event -> AggregateProjection<'entity>) (history:Event list) (future:Event list) =

   let events = 
      history @ future
      |> function
         | [] -> failwith "events cannot be empty"
         | all ->
            all
            |> List.map(fun x -> x.AggregateKey.Id)
            |> List.distinct
            |> function
               | [_] -> all
               | _ -> failwith "events must refer to the same aggregate id"

   let key = events |> List.last |> fun x -> x.AggregateKey

   { Key = key
     Events = events
     Projection = projectionFactory (List.head events) }


let createWithFirstEvent<'entity> (projectionFactory: Event -> AggregateProjection<'entity>) first =
   makeCreateCfg projectionFactory [] [first]
   |> create


let createWithNextEvent<'entity> (projectionFactory: Event -> AggregateProjection<'entity>) history next =
   makeCreateCfg projectionFactory history [next]
   |> create


let createFromHistory<'entity> (projectionFactory: Event -> AggregateProjection<'entity>) history =
   makeCreateCfg projectionFactory history []
   |> create


let projectionFactory<'entity> create update : Event -> AggregateProjection<'entity> =
   fun (initial:Event) ->
      { Init = initial |> create
        Update = fun state event -> update state event }

