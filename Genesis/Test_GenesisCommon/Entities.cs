using System;

namespace LibGenesisCommon.Tests
{
    public class Address
    {
        public string AddressLine1 { get; set; }
        public string AddressLine2 { get; set; }
        public string AddressLine3 { get; set; }

        public string City { get; set; }
        public string State { get; set; }
        public string PinCode { get; set; }
    }

    public class User
    {
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public DateTime DateOfBirth { get; set; }
        public string EmailId { get; set; }
        public bool IsAdult { get; set; }
        public Address Address { get; set; }

        public User()
        {
            IsAdult = false;
            DateOfBirth = DateTime.Now;
        }
    }
}