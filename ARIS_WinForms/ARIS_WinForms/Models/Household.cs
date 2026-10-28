using System;

namespace ARIS_WinForms.Models
{
    public class Household
    {
        public int Id { get; set; }
        public string HouseholdNumber { get; set; } = string.Empty;
        public string HeadName { get; set; } = string.Empty;
        public string AddressText { get; set; } = string.Empty;
        public int MemberCount { get; set; }
    }
}