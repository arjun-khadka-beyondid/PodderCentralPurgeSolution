using Npgsql;
using NpgsqlTypes;
using System;
using System.Collections.Generic;
using System.Text;

namespace PodderCentral.PurgeData
{
    public class DBContext
    {
        public DBContext(DBSecretCredential secretCredential)
        {
            _connectionString = $"Server={secretCredential.Host};Port={secretCredential.Port};User Id={secretCredential.UserName};Password={secretCredential.Password};Database={secretCredential.DbName};Timeout=15";
        }
        public DBContext(string connString)
        {
            _connectionString = connString;
        }
        public string SignUpTableName = "SIGNUPDETAILSENC";
        public string _connectionString;
        public  int PurgeData(int daysToPreserveData)
        {
            int result = -1;
            using (NpgsqlConnection con = new NpgsqlConnection(_connectionString))
            {
                con.Open();
                //string executeQuery = $"DELETE FROM {SignUpTableName} WHERE activationkey=@activationkey";
                string executeQuery = $"DELETE FROM public.signupdetailsenc WHERE DATE_PART('day', AGE(now(), keygenerateddate))  > {daysToPreserveData}";
                using (NpgsqlCommand cmd = new NpgsqlCommand(executeQuery, con))
                {
                  
                    try
                    {
                        result = cmd.ExecuteNonQuery();
                    }
                    catch (Exception ex)
                    {
                        con.Close();
                        throw ex;
                    }
                    finally
                    {
                        con.Close();
                    }
                }
            }
            return result;
        }

        public SignUp GetSignUpUserDetails(Guid activationkey)
        {
            SignUp user;

            using (NpgsqlConnection con = new NpgsqlConnection(_connectionString))
            {
                con.Open();
                string selectQuery = $"SELECT * FROM  public.signupdetailsenc WHERE activationkey=@activationkey";
                using (NpgsqlCommand cmd = new NpgsqlCommand(selectQuery, con))
                {
                    cmd.Parameters.AddWithValue("activationkey", NpgsqlDbType.Uuid, activationkey);
                    try
                    {
                        NpgsqlDataReader dr = cmd.ExecuteReader();
                        dr.Read();
                        if (!dr.HasRows)
                        {
                            return null;
                        }
                        user = new SignUp()
                        {
                            UniqueKey = new Guid(dr["activationkey"].ToString()),
                            IsKeyUsed = dr["iskeyused"].ToString() == "y" ? true : false,
                            FirstName = dr["firstname"].ToString(),
                            LastName = dr["lastname"].ToString(),
                            BirthDate = dr["birthdate"].ToString(),
                            GuardianFirstName = dr["guardianfirstname"].ToString(),
                            GuardianLastName = dr["guardianlastname"].ToString(),
                            PrimaryPhoneNumber = dr["phone"].ToString(),
                            EmailAddress = dr["email"].ToString(),
                            CurrentOmniPodProduct = dr["omnipodproduct"].ToString(),
                            PDMSerialNumber = dr["pdmserialnumber"]?.ToString(),
                            ClaimStatus = dr["claimstatus"].ToString(),
                            PatientId = dr["PatientId"]?.ToString(),
                            SNMatch = dr["snmatch"]?.ToString(),
                            PhoneMatch = dr["phonematch"]?.ToString(),
                            LastnameMatch = dr["lastnamematch"]?.ToString(),
                            EmailMatch = dr["emailmatch"]?.ToString(),
                            DobMatch = dr["dobmatch"]?.ToString(),
                            HasSNInClaimedAccount = dr["hasSN"]?.ToString(),
                        };
                    }
                    catch
                    {
                        throw;
                    }
                    finally
                    {
                        con.Close();
                    }
                }
            }

            return user;

        }

    }

    public class SignUp
    {
        public Guid UniqueKey { get; set; }
        public bool IsKeyUsed { get; set; }
        public DateTime KeyGeneratedDate { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string BirthDate { get; set; }
        public string GuardianFirstName { get; set; }
        public string GuardianLastName { get; set; }
        public string PrimaryPhoneNumber { get; set; }
        public string EmailAddress { get; set; }
        public string CurrentOmniPodProduct { get; set; }
        public string PDMSerialNumber { get; set; }
        public string ClaimStatus { get; set; }

        public string PatientId { get; set; }
        public string CustomerId { get; set; }

        public string SNMatch { get; set; }
        public string PhoneMatch { get; set; }
        public string LastnameMatch { get; set; }
        public string EmailMatch { get; set; }
        public string DobMatch { get; set; }

        public string HasSNInClaimedAccount { get; set; }

    }
}
