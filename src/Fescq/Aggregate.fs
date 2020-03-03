module Fescq.Aggregate

open Core

// TODO: consider how to handle hydrating an aggregate when the event validation rules have changed
// so that an historical event is now considered invalid.  Is this a real concern?

/// Apply all events with the provided ApplyEvent function.
/// This function must validate domain rules so that construction is successful IFF the state is valid.
///
/// Note that historical events should always pass this validation, as they were subjected to it
/// when they were appended (assuming validation rules have not changed); but the future events must fail
/// (throw an exception) if they put the aggregate in an invalid state.
let create<'entity> (apply:ApplyEvent<'entity>) (history:Event list) (future:Event list) : Agg<'entity> =

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

   let folder (state:EntityState<'entity> option) (event:Event) =

      let newState = 
         match state with
         | None -> event |> Create |> apply 
         | Some prev -> (prev, event) |> Update |> apply

      newState |> Some      
      

   events
   |> List.fold folder None
   |> function
      | None -> failwith "unexpected failure during create (None)"
      | Some state ->
         { Key = key
           Entity = state.Entity
           History = events }


let createWithFirstEvent<'entity> (apply:ApplyEvent<'entity>) (first:Event) =
   create apply [] [first]


let createWithNextEvent<'entity> (apply:ApplyEvent<'entity>) (history:Event list) (next:Event) =
   create apply history [next]


let createFromHistory<'entity> (apply:ApplyEvent<'entity>) (history:Event list) =
   create apply history []


let makeApplyFunc<'entity> create update : ApplyEvent<'entity> =

   fun (action:ApplyAction<'entity>) ->
      match action with
      | Create e -> create e
      | Update (s, e) -> update s e
