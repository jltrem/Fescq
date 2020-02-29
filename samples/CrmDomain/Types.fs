namespace CrmDomain

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
