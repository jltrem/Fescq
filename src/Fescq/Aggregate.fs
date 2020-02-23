module Fescq.Aggregate


// State is a (version, entity) pair
type ApplyEvent<'entity> = (int*'entity) option -> Event -> (int*'entity) option

/// Apply all events with the provided folder function.
/// This function must validate domain rules so that construction
/// is successful IFF the state is valid
/// Returns a tuple (Agg<'entity * Event list)
/// where the first element is the new aggreate
/// and the second element is the future events (part of the aggregate but not unpersisted)
let create<'entity> (apply:ApplyEvent<'entity>) (history:Event list) (future:Event list)  =

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
   |> List.fold apply None
   |> function
      | None -> failwith "unexpected failure during create"
      | Some result ->
      { Key = key
        Entity = snd result
        History = events }
      |> fun agg -> (agg, future)


let createWithFirstEvent<'entity> (apply:ApplyEvent<'entity>) (first:Event) =
   create apply [] [first]


let createWithNextEvent<'entity> (apply:ApplyEvent<'entity>) (history:Event list) (next:Event) =
   create apply history [next]


let createFromHistory<'entity> (apply:ApplyEvent<'entity>) (history:Event list) =
   create apply history []

