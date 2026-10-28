using System;
using System.Collections.Generic;
using ARIS_WinForms.Models;

namespace ARIS_WinForms.DAL
{
    public class HouseholdDAL
    {
        // Dữ liệu giả lập để test Form chạy mượt mà ngay lập tức
        public List<Household> GetAll()
        {
            return new List<Household>
            {
                new Household { Id = 1, HouseholdNumber = "HK001", HeadName = "Nguyễn Văn A", AddressText = "123 Quận 1, TP.HCM", MemberCount = 4 },
                new Household { Id = 2, HouseholdNumber = "HK002", HeadName = "Trần Thị B", AddressText = "456 Quận 3, TP.HCM", MemberCount = 3 }
            };
        }
    }
}