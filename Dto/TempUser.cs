using System;

namespace postgres_database_restore_tool.Dto
{
    public class TempUser
    {
        public int id { get; set; }
        public string user_name { get; set; }
        public string normalized_user_name { get; set; }

        public string email { get; set; }
        public string normalized_email { get; set; }
        public bool email_confirmed { get; set; }
        public string password_hash { get; set; }
        public string security_stamp { get; set; }
        public string concurrency_stamp { get; set; }
        public string phone_number { get; set; }
        public bool phone_number_confirmed { get; set; }
        public bool two_factor_enabled { get; set; }
        public DateTime? lockout_end { get; set; }
        public bool lockout_enabled { get; set; }
        public int access_failed_count { get; set; }
        public int branch_id { get; set; }
        public int rec_by { get; set; }
        public DateTime? rec_date { get; set; }
        public string rec_status { get; set; }
        public string user_language { get; set; }
        public DateTime txn_date { get; set; }
        public string gmt { get; set; }
        public string running_date { get; set; }
        public string user_status { get; set; }
        public bool is_parent_user { get; set; }
        public string parent_user_id { get; set; }
        public DateTime? created_at { get; set; }
        public DateTime? updated_at { get; set; }
        public string created_by_user { get; set; }
        public string updated_by_user { get; set; }
        public bool is_in_trial { get; set; }
        public string business_name { get; set; }
        public string primary_contact_number { get; set; }
        public string mobile_number { get; set; }
        public string country { get; set; }
        public string state { get; set; }
        public string city { get; set; }
        public string first_name { get; set; }
        public string middle_name { get; set; }
        public string last_name { get; set; }
        public DateTime? license_valid_upto_date { get; set; }

        public string user_level { get; set; }
    }
}