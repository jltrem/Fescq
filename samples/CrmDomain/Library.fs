namespace CrmDomain

open System

type CompanyName = string

type PersonalName = {
   Given: string
   Middle: string
   Family: string
}

type PhoneType = 
   | Unknown = 0
   | Mobile = 1
   | Work = 2
   | Home = 3

type PhoneNumber = {
   PhoneType: PhoneType
   Number: string
   Ext: string
}

type AddressType =
   | Unknown = 0
   | Primary = 1
   | Alternate = 2
   | Shipping = 3
   | Billing = 4

type Address = {
   AddressType: AddressType
   Line1: string
   Line2: string
   City: string
   State: string
   Country: string
   Zip: string
}

module Validate =

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

      
