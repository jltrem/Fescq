module Fescq.Aggregate

open Core

type AggregateProjection<'entity> = Projection<EntityState<'entity>, Event>

// TODO: consider how to handle hydrating an aggregate when the event validation rules have changed
// so that an historical event is now considered invalid.  Is this a real concern?

/// Apply all events with the provided ApplyEvent function.
/// This function must validate domain rules so that construction is successful IFF the state is valid.
///
/// Note that historical events should always pass this validation, as they were subjected to it
/// when they were appended (assuming validation rules have not changed); but the future events must fail
/// (throw an exception) if they put the aggregate in an invalid state.
let create<'entity> (projection:AggregateProjection<'entity>) (history:Event list) (future:Event list) : Agg<'entity> =

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

   let key =
      events
      |> List.last
      |> fun x -> x.AggregateKey

   events
   |> List.tail
   |> List.fold projection.Update projection.Init
   |> fun state ->
      { Key = key
        Entity = state.Entity
        History = events }


let createWithFirstEvent<'entity> (projectionFactory: Event -> AggregateProjection<'entity>) first =
   let projection = projectionFactory first
   create projection [] [first]


let createWithNextEvent<'entity> (projectionFactory: Event -> AggregateProjection<'entity>) history next =
   let projection = projectionFactory (List.head history)
   create projection history [next]


let createFromHistory<'entity> (projectionFactory: Event -> AggregateProjection<'entity>) history =
   let projection = projectionFactory (List.head history)
   create projection history []


let projectionFactory<'entity> create update : Event -> AggregateProjection<'entity> =
   fun (initial:Event) ->
      { Init = initial |> create
        Update = fun state event -> update state event }

