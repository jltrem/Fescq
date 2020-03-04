module CrmDomain.Validate

open System

let private trim (value:string) = 
   if String.IsNullOrEmpty(value) then "" else value.Trim()
   
let private lengthWithin min max value =
   trim value      
   |> fun x -> 
      if x.Length >= min && x.Length <= max then
         Some value
      else 
         None

let companyName (value:CompanyName) = 
   lengthWithin 1 80 value

let personalName (value:PersonalName) =
   let g = trim value.Given
   let m = trim value.Middle
   let f = trim value.Family

   String.Concat (g, m, f)
   |> lengthWithin 1 256 
   |> Option.map (fun _ -> 
      {
         Given = g
         Middle = m
         Family = f
      })

let phoneNumber (value:PhoneNumber) =
   lengthWithin 1 80 value.Number
   |> Option.map (fun n -> 
      {
         PhoneType = value.PhoneType
         Number = n
         Ext = trim value.Ext
      })

let address (value:Address) =
   [lengthWithin 1 80 value.Line1; lengthWithin 1 80 value.City]
   |> function
      | [Some line1; Some city] -> 
         {
            AddressType = value.AddressType
            Line1 = line1
            Line2 = trim value.Line2
            City = city
            State = trim value.State
            Country = trim value.Country
            Zip = trim value.Zip
         } |> Some
      | _ -> None
