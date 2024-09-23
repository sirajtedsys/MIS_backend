using Microsoft.EntityFrameworkCore;
using MIS_Backend.Class;
using MIS_Backend.Data.Class;
using MIS_Backend.Data.DbModel;
using Oracle.ManagedDataAccess.Client;
using System.Data;
using System.Dynamic;
using static JwtService;

namespace MIS_Backend.Repositry
{
    public class CommonRepositry
    {
        private readonly AppDbContext _DbContext;

        private readonly JwtHandler jwthand;

        public CommonRepositry(AppDbContext dbContext, JwtHandler _jwthand)
        {
            _DbContext = dbContext;
            jwthand = _jwthand;
        }

        public async Task<dynamic> LoginCheck(string username, string password)
        {
            try
            {
                var data = await _DbContext.EMR_ADMIN_USERS
                    .Where(x => x.AUSR_USERNAME == username
                    && x.AUSR_PWD == password
                    && x.AUSR_STATUS != "D")
                    .ToListAsync();

                //return userdata;

                //SELECT B.BRANCH_ID, B.BRCH_NAME FROM UCHMASTER.HRM_BRANCH B
                //                INNER JOIN UCHEMR.EMR_ADMIN_USERS_BRANCH_LINK BL ON BL.BRANCH_ID = B.BRANCH_ID
                //                INNER JOIN UCHEMR.EMR_ADMIN_USERS USR ON USR.AUSR_ID = BL.AUSR_ID
                //                WHERE NVL(B.ACTIVE_STATUS, 'A')= 'A' AND ausr_username = 'tedsys' AND ausr_pwd = 'ted@123' ORDER BY B.BRANCH_ID



                if (data != null)
                {
                    var userdat = new UserTocken
                    {
                        AUSR_ID = data[0].AUSR_ID,
                        USERNAME = data[0].AUSR_USERNAME,
                        PASSWORD = data[0].AUSR_PWD
                    };

                    var token = jwthand.GenerateToken(userdat);

                    //check exiting userdetail in loginsettings
                    var dat = await _DbContext.LOGIN_SETTINGS.Where(x => x.USERID == data[0].AUSR_ID).ToListAsync();
                    var existingDATA = new UCHMASTER_LoginSettings();

                    if (dat.Count > 0)
                    {

                        existingDATA = dat[0];
                    }
                    else
                    {
                        existingDATA = null;
                    }
                    //if data exist then edit the table

                    if (existingDATA != null)
                    {
                        existingDATA.TOKEN = token;
                        existingDATA.GENERATEDATE = DateTime.Now;
                        _DbContext.SaveChanges();

                    }
                    //if no existing data exist then add new data to teh tablw
                    else
                    {
                        var newlogin = new UCHMASTER_LoginSettings
                        {
                            USERID = userdat.AUSR_ID,
                            TOKEN = token,
                            GENERATEDATE = DateTime.Now,
                        };

                        _DbContext.LOGIN_SETTINGS.Add(newlogin);
                        _DbContext.SaveChanges();
                    }

                    var msgsuccsess = new DefaultMessage.Message1
                    {
                        Status = 200,
                        Message = token
                    };
                    return msgsuccsess;


                }
                else
                {
                    var msg = new DefaultMessage.Message1
                    {
                        Status = 600,
                        Message = "Invalid Username and Password"

                    };
                    return msg;
                }




            }
            catch (Exception ex)
            {
                var msg1 = new DefaultMessage.Message1
                {
                    Status = 500,
                    Message = ex.Message
                };
                return msg1;
            }

        }

        public async Task<dynamic> GetAllUserBranches(UserTocken ut)
        {
            try
            {

                var result =
                    from b in _DbContext.HRM_BRANCH
                    join bl in _DbContext.EMR_ADMIN_USERS_BRANCH_LINK
                        on b.BRANCH_ID equals bl.BRANCH_ID
                    join usr in _DbContext.EMR_ADMIN_USERS
                        on bl.AUSR_ID equals usr.AUSR_ID
                    where b.ACTIVE_STATUS == "A"
                          && usr.AUSR_USERNAME == ut.USERNAME
                          && usr.AUSR_PWD == ut.PASSWORD
                    orderby b.BRANCH_ID
                    select new
                    {
                        b.BRANCH_ID,
                        b.BRCH_NAME
                    };
                return result;

            }
            catch (Exception ex)
            {
                var msg1 = new DefaultMessage.Message1
                {
                    Status = 500,
                    Message = ex.Message
                };
                return msg1;

            }
        }


        //sp_ds_purchase_order   10
        public async Task<dynamic> CallPurchaseOrderProcedureAsync(string fromDate, string toDate)
        {
            try
            {
                // Define the SQL statement to call the stored procedure
                var sql = "BEGIN UCHTRANS.SP_DS_PURCHASE_ORDER(:P_FROM_DATE, :P_TO_DATE, :STROUT); END;";

                // Format the dates to match Oracle's expected format (e.g., DD/MM/YYYY)
                string formattedFromDate = DateTime.Parse(fromDate).ToString("dd/MM/yyyy");
                string formattedToDate = DateTime.Parse(toDate).ToString("dd/MM/yyyy");

                // Define the parameters
                var fromDateParam = new OracleParameter("P_FROM_DATE", OracleDbType.Varchar2) { Value = formattedFromDate };
                var toDateParam = new OracleParameter("P_TO_DATE", OracleDbType.Varchar2) { Value = formattedToDate };
                var strOutParam = new OracleParameter("STROUT", OracleDbType.RefCursor) { Direction = ParameterDirection.Output };

                // Execute the stored procedure
                using (var cmd = _DbContext.Database.GetDbConnection().CreateCommand())
                {
                    cmd.CommandText = sql;
                    cmd.CommandType = System.Data.CommandType.Text;

                    // Add parameters to the command
                    cmd.Parameters.Add(fromDateParam);
                    cmd.Parameters.Add(toDateParam);
                    cmd.Parameters.Add(strOutParam);

                    await _DbContext.Database.GetDbConnection().OpenAsync();

                    using (var reader = (OracleDataReader)await cmd.ExecuteReaderAsync())
                    {
                        var results = new List<dynamic>();

                        while (await reader.ReadAsync())
                        {
                            var result = new
                            {
                                GP = reader["GP"] != DBNull.Value ? reader["GP"].ToString() : string.Empty,
                                BillCount = reader["BILL_CNT"] != DBNull.Value ? Convert.ToInt32(reader["BILL_CNT"]) : 0,
                                Amount = reader["AMOUNT"] != DBNull.Value ? Convert.ToDecimal(reader["AMOUNT"]) : 0.0m
                            };

                            results.Add(result);
                        }

                        return results;
                    }
                }
            }
            catch (Exception ex)
            {
                var msg1 = new DefaultMessage.Message1
                {
                    Status = 500,
                    Message = ex.Message
                };
                return msg1;
            }
        }


        //sp_ds_appnmt_sts  9
        public async Task<dynamic> CallAppnmntStsProcedureAsync(string dateFrom, string dateTo)
        {
            try
            {
                // Define the SQL statement to call the stored procedure
                var sql = "BEGIN UCHTRANS.SP_DS_APPNMNT_STS(:DATE_FROM, :DATE_TO, :STROUT); END;";

                // Format the dates to match Oracle's expected format (e.g., DD/MM/YY)
                string formattedDateFrom = DateTime.Parse(dateFrom).ToString("dd/MM/yy");
                string formattedDateTo = DateTime.Parse(dateTo).ToString("dd/MM/yy");

                // Define the parameters
                var dateFromParam = new OracleParameter("DATE_FROM", OracleDbType.Varchar2) { Value = formattedDateFrom };
                var dateToParam = new OracleParameter("DATE_TO", OracleDbType.Varchar2) { Value = formattedDateTo };
                var stroUtrParam = new OracleParameter("STROUT", OracleDbType.RefCursor) { Direction = ParameterDirection.Output };

                // Execute the stored procedure
                using (var cmd = _DbContext.Database.GetDbConnection().CreateCommand())
                {
                    cmd.CommandText = sql;
                    cmd.CommandType = System.Data.CommandType.Text;

                    // Add parameters to the command
                    cmd.Parameters.Add(dateFromParam);
                    cmd.Parameters.Add(dateToParam);
                    cmd.Parameters.Add(stroUtrParam);

                    await _DbContext.Database.GetDbConnection().OpenAsync();

                    using (var reader = (OracleDataReader)await cmd.ExecuteReaderAsync())
                    {
                        var results = new List<dynamic>();

                        while (await reader.ReadAsync())
                        {
                            var result = new
                            {
                                TokenTotal = reader["TOKEN_TOTAL"],
                                NewBooked = reader["NEW_BOOKED"],
                                NewVisited = reader["NEW_VISITED"],
                                RevisitBooked = reader["REVISIT_BOOKED"],
                                RevisitVisited = reader["REVISIT_VISITED"],
                                ReportBooked = reader["REPORT_BOOKED"],
                                ReportVisited = reader["REPORT_VISITED"]
                            };

                            results.Add(result);
                        }

                        return results;
                    }
                }
            }
            catch (Exception ex)
            {
                var msg1 = new DefaultMessage.Message1
                {
                    Status = 500,
                    Message = ex.Message
                };
                return msg1;
            }
        }

        //sp_ds_op_sts
        public async Task<dynamic> CallOpStsProcedureAsync(string dateFrom, string dateTo)
        {
            try
            {
                // Define the SQL statement to call the stored procedure
                var sql = "BEGIN UCHTRANS.SP_DS_OP_STS(:STRFROMDATE, :STRTODATE, :strOut); END;";

                // Parse the input strings as DateTime and avoid formatting issues by using DateTime objects
                DateTime parsedDateFrom = DateTime.Parse(dateFrom);
                DateTime parsedDateTo = DateTime.Parse(dateTo);

                // Define the parameters using OracleDbType.Date
                var dateFromParam = new OracleParameter("STRFROMDATE", OracleDbType.Date) { Value = parsedDateFrom };
                var dateToParam = new OracleParameter("STRTODATE", OracleDbType.Date) { Value = parsedDateTo };
                var strOutParam = new OracleParameter("strOut", OracleDbType.RefCursor) { Direction = ParameterDirection.Output };

                // Execute the stored procedure
                using (var cmd = _DbContext.Database.GetDbConnection().CreateCommand())
                {
                    cmd.CommandText = sql;
                    cmd.CommandType = System.Data.CommandType.Text;

                    // Add parameters to the command
                    cmd.Parameters.Add(dateFromParam);
                    cmd.Parameters.Add(dateToParam);
                    cmd.Parameters.Add(strOutParam);

                    await _DbContext.Database.GetDbConnection().OpenAsync();

                    using (var reader = (OracleDataReader)await cmd.ExecuteReaderAsync())
                    {
                        var results = new List<dynamic>();

                        while (await reader.ReadAsync())
                        {
                            var result = new
                            {
                                FreeNo = reader["FREE_NO"],
                                RevisitNo = reader["REVISIT_NO"],
                                NewNo = reader["NEW_NO"],
                                PrcNo = reader["PRC_NO"],
                                PayType = reader["PAY_TYPE"],
                                Total = reader["TOTAL"]
                            };

                            results.Add(result);
                        }

                        return results;
                    }
                }
            }
            catch (Exception ex)
            {
                var msg1 = new DefaultMessage.Message1
                {
                    Status = 500,
                    Message = ex.Message
                };
                return msg1;
            }
        }

        //sp_ds_Collection  8
        public async Task<dynamic> CallCollectionProcedureAsync(string fromDate, string toDate, string branchId)
        {
            try
            {
                // Define the SQL statement to call the stored procedure
                var sql = "BEGIN UCHTRANS.SP_DS_COLLECTION(:P_FROM_DATE, :P_TO_DATE, :P_BRANCH_ID, :STROUT); END;";

                // Format the dates to match Oracle's expected format (e.g., DD/MM/YYYY)
                string formattedFromDate = DateTime.Parse(fromDate).ToString("dd/MM/yyyy");
                string formattedToDate = DateTime.Parse(toDate).ToString("dd/MM/yyyy");

                // Define the parameters
                var fromDateParam = new OracleParameter("P_FROM_DATE", OracleDbType.Varchar2) { Value = formattedFromDate };
                var toDateParam = new OracleParameter("P_TO_DATE", OracleDbType.Varchar2) { Value = formattedToDate };
                var branchIdParam = new OracleParameter("P_BRANCH_ID", OracleDbType.Varchar2) { Value = branchId };
                var strOutParam = new OracleParameter("STROUT", OracleDbType.RefCursor) { Direction = ParameterDirection.Output };

                // Execute the stored procedure
                using (var cmd = _DbContext.Database.GetDbConnection().CreateCommand())
                {
                    cmd.CommandText = sql;
                    cmd.CommandType = System.Data.CommandType.Text;

                    // Add parameters to the command
                    cmd.Parameters.Add(fromDateParam);
                    cmd.Parameters.Add(toDateParam);
                    cmd.Parameters.Add(branchIdParam);
                    cmd.Parameters.Add(strOutParam);

                    await _DbContext.Database.GetDbConnection().OpenAsync();

                    using (var reader = (OracleDataReader)await cmd.ExecuteReaderAsync())
                    {
                        var results = new List<dynamic>();

                        while (await reader.ReadAsync())
                        {
                            var result = new
                            {
                                Cash = reader["CASH"],
                                Credit = reader["CREDIT"],
                                CreditCard = reader["CREDIT_CARD"],
                                OT = reader["OT"],
                                Insurance = reader["INSURANCE"],
                                Cheque = reader["CHEQUE"],
                                BWallet = reader["B_WALLET"],
                                Total = reader["TOTAL"]
                            };

                            results.Add(result);
                        }

                        return results;
                    }
                }
            }
            catch (Exception ex)
            {
                var msg1 = new DefaultMessage.Message1
                {
                    Status = 500,
                    Message = ex.Message
                };
                return msg1;
            }
        }

        //sp_ds_colection_sct  7
        public async Task<dynamic> CallCollectionSctProcedureAsync(string fromDate, string toDate, string branchId)
        {
            try
            {
                // Define the SQL statement to call the stored procedure
                var sql = "BEGIN UCHTRANS.SP_DS_COLLECTION_SCT(:P_FROM_DATE, :P_TO_DATE, :P_BRANCH_ID, :STROUT); END;";

                // Format the dates to match Oracle's expected format (e.g., DD/MM/YYYY)
                string formattedFromDate = DateTime.Parse(fromDate).ToString("dd/MM/yyyy");
                string formattedToDate = DateTime.Parse(toDate).ToString("dd/MM/yyyy");

                // Define the parameters
                var fromDateParam = new OracleParameter("P_FROM_DATE", OracleDbType.Varchar2) { Value = formattedFromDate };
                var toDateParam = new OracleParameter("P_TO_DATE", OracleDbType.Varchar2) { Value = formattedToDate };
                var branchIdParam = new OracleParameter("P_BRANCH_ID", OracleDbType.Varchar2) { Value = branchId };
                var strOutParam = new OracleParameter("STROUT", OracleDbType.RefCursor) { Direction = ParameterDirection.Output };

                // Execute the stored procedure
                using (var cmd = _DbContext.Database.GetDbConnection().CreateCommand())
                {
                    cmd.CommandText = sql;
                    cmd.CommandType = System.Data.CommandType.Text;

                    // Add parameters to the command
                    cmd.Parameters.Add(fromDateParam);
                    cmd.Parameters.Add(toDateParam);
                    cmd.Parameters.Add(branchIdParam);
                    cmd.Parameters.Add(strOutParam);

                    await _DbContext.Database.GetDbConnection().OpenAsync();

                    using (var reader = (OracleDataReader)await cmd.ExecuteReaderAsync())
                    {
                        var results = new List<dynamic>();

                        while (await reader.ReadAsync())
                        {
                            var result = new
                            {
                                GP = reader["GP"],
                                Cash = reader["CASH"],
                                Credit = reader["CREDIT"],
                                CreditCard = reader["CREDIT_CARD"],
                                OT = reader["OT"],
                                Insurance = reader["INSURANCE"],
                                Cheque = reader["CHEQUE"],
                                BWallet = reader["B_WALLET"],
                                Total = reader["TOTAL"]
                            };

                            results.Add(result);
                        }

                        return results;
                    }
                }
            }
            catch (Exception ex)
            {
                var msg1 = new DefaultMessage.Message1
                {
                    Status = 500,
                    Message = ex.Message
                };
                return msg1;
            }
        }


        //sp_ds_dept_rev  13
        public async Task<dynamic> CallDeptRevProcedureAsync(string fromDate, string toDate)
        {
            try
            {
                // Define the SQL statement to call the stored procedure
                var sql = "BEGIN UCHTRANS.SP_DS_DEPT_REV(:DATE_FROM, :DATE_TO, :STROUT); END;";

                // Format the dates to match Oracle's expected format (e.g., DD/MM/YY)
                string formattedFromDate = DateTime.Parse(fromDate).ToString("dd/MM/yy");
                string formattedToDate = DateTime.Parse(toDate).ToString("dd/MM/yy");

                // Define the parameters
                var fromDateParam = new OracleParameter("DATE_FROM", OracleDbType.Varchar2) { Value = formattedFromDate };
                var toDateParam = new OracleParameter("DATE_TO", OracleDbType.Varchar2) { Value = formattedToDate };
                var strOutParam = new OracleParameter("STROUT", OracleDbType.RefCursor) { Direction = ParameterDirection.Output };

                // Create and execute the command
                using (var connection = _DbContext.Database.GetDbConnection())
                {
                    await connection.OpenAsync();

                    using (var cmd = connection.CreateCommand())
                    {
                        cmd.CommandText = sql;
                        cmd.CommandType = CommandType.Text;

                        // Add parameters to the command
                        cmd.Parameters.Add(fromDateParam);
                        cmd.Parameters.Add(toDateParam);
                        cmd.Parameters.Add(strOutParam);

                        using (var reader = (OracleDataReader)await cmd.ExecuteReaderAsync())
                        {
                            var results = new List<dynamic>();

                            while (await reader.ReadAsync())
                            {
                                var result = new
                                {
                                    SPETY_NAME = reader["SPETY__NAME"],
                                    TOTAL = reader["TOTAL"]
                                };

                                results.Add(result);
                            }

                            return results;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // Log detailed error information
                var msg1 = new DefaultMessage.Message1
                {
                    Status = 500,
                    Message = $"An error occurred while executing the procedure: {ex.Message}"
                };
                return msg1;
            }
        }

        //sp_doc_rev  3
        public async Task<dynamic> DsDoctRevProcedureAsync(string fromDate, string toDate)
        {
            try
            {
                // Define the SQL statement to call the new stored procedure
                var sql = "BEGIN UCHTRANS.SP_DS_DOCT_REV(:DATE_FROM, :DATE_TO, :STROUT); END;";

                // Format the dates to match Oracle's expected format (e.g., DD/MM/YYYY)
                string formattedFromDate = DateTime.Parse(fromDate).ToString("dd/MM/yyyy");
                string formattedToDate = DateTime.Parse(toDate).ToString("dd/MM/yyyy");

                // Define the parameters
                var fromDateParam = new OracleParameter("DATE_FROM", OracleDbType.Varchar2) { Value = formattedFromDate };
                var toDateParam = new OracleParameter("DATE_TO", OracleDbType.Varchar2) { Value = formattedToDate };
                var strOutParam = new OracleParameter("STROUT", OracleDbType.RefCursor) { Direction = ParameterDirection.Output };

                // Execute the stored procedure
                using (var cmd = _DbContext.Database.GetDbConnection().CreateCommand())
                {
                    cmd.CommandText = sql;
                    cmd.CommandType = System.Data.CommandType.Text;

                    // Add parameters to the command
                    cmd.Parameters.Add(fromDateParam);
                    cmd.Parameters.Add(toDateParam);
                    cmd.Parameters.Add(strOutParam);

                    await _DbContext.Database.GetDbConnection().OpenAsync();

                    using (var reader = (OracleDataReader)await cmd.ExecuteReaderAsync())
                    {
                        var results = new List<dynamic>();

                        while (await reader.ReadAsync())
                        {
                            var result = new
                            {
                                // Update these field names to match the columns returned by the new procedure
                                DOCTOR = reader["DOCTOR"],
                                TOTAL = reader["TOTAL"]
                            };

                            results.Add(result);
                        }

                        return results;
                    }
                }
            }
            catch (Exception ex)
            {
                var msg1 = new DefaultMessage.Message1
                {
                    Status = 500,
                    Message = ex.Message
                };
                return msg1;
            }
        }

        //sp_ds_ins_rev  12
        public async Task<dynamic> spInsRevProcedureAsync(string fromDate, string toDate)
        {
            try
            {
                // Define the SQL statement to call the stored procedure
                var sql = "BEGIN UCHTRANS.SP_DS_INS_RECV(:DATE_FROM, :DATE_TO, :STROUT); END;";

                // Format the dates to match Oracle's expected format (e.g., DD/MM/YYYY)
                string formattedFromDate = DateTime.Parse(fromDate).ToString("dd/MM/yyyy");
                string formattedToDate = DateTime.Parse(toDate).ToString("dd/MM/yyyy");

                // Define the parameters
                var fromDateParam = new OracleParameter("DATE_FROM", OracleDbType.Varchar2) { Value = formattedFromDate };
                var toDateParam = new OracleParameter("DATE_TO", OracleDbType.Varchar2) { Value = formattedToDate };
                var strOutParam = new OracleParameter("STROUT", OracleDbType.RefCursor) { Direction = ParameterDirection.Output };

                // Execute the stored procedure
                using (var cmd = _DbContext.Database.GetDbConnection().CreateCommand())
                {
                    cmd.CommandText = sql;
                    cmd.CommandType = System.Data.CommandType.Text;

                    // Add parameters to the command
                    cmd.Parameters.Add(fromDateParam);
                    cmd.Parameters.Add(toDateParam);
                    cmd.Parameters.Add(strOutParam);

                    await _DbContext.Database.GetDbConnection().OpenAsync();

                    using (var reader = (OracleDataReader)await cmd.ExecuteReaderAsync())
                    {
                        var results = new List<dynamic>();

                        while (await reader.ReadAsync())
                        {
                            var result = new
                            {
                                PRVD_NAME = reader["PRVD_NAME"],
                                INS_AMT = reader["INS_AMT"]
                            };

                            results.Add(result);
                        }

                        return results;
                    }
                }
            }
            catch (Exception ex)
            {
                var msg1 = new DefaultMessage.Message1
                {
                    Status = 500,
                    Message = ex.Message
                };
                return msg1;
            }
        }

        //SP_DS_IP_REV   1
        public async Task<dynamic> DsIpRevProcedureAsync(string fromDate, string toDate)
        {
            try
            {
                // Define the SQL statement to call the stored procedure
                var sql = "BEGIN UCHTRANS.SP_DS_IP_REV(:DT_FROM, :DT_TO, :STROUT); END;";

                // Format the dates to match Oracle's expected format (e.g., DD/MM/YYYY)
                string formattedFromDate = DateTime.Parse(fromDate).ToString("dd/MM/yyyy");
                string formattedToDate = DateTime.Parse(toDate).ToString("dd/MM/yyyy");

                // Define the parameters
                var fromDateParam = new OracleParameter("DT_FROM", OracleDbType.Varchar2) { Value = formattedFromDate };
                var toDateParam = new OracleParameter("DT_TO", OracleDbType.Varchar2) { Value = formattedToDate };
                var strOutParam = new OracleParameter("STROUT", OracleDbType.RefCursor) { Direction = ParameterDirection.Output };

                // Execute the stored procedure
                using (var cmd = _DbContext.Database.GetDbConnection().CreateCommand())
                {
                    cmd.CommandText = sql;
                    cmd.CommandType = System.Data.CommandType.Text;

                    // Add parameters to the command
                    cmd.Parameters.Add(fromDateParam);
                    cmd.Parameters.Add(toDateParam);
                    cmd.Parameters.Add(strOutParam);

                    await _DbContext.Database.GetDbConnection().OpenAsync();

                    using (var reader = (OracleDataReader)await cmd.ExecuteReaderAsync())
                    {
                        var results = new List<dynamic>();

                        while (await reader.ReadAsync())
                        {
                            var result = new
                            {
                                GP = reader["GP"],
                                AMOUNT = reader["AMOUNT"]
                            };

                            results.Add(result);
                        }

                        return results;
                    }
                }
            }
            catch (Exception ex)
            {
                var msg1 = new DefaultMessage.Message1
                {
                    Status = 500,
                    Message = ex.Message
                };
                return msg1;
            }
        }

        //SP_DS_PROC_CATEGORY  4
        public async Task<dynamic> DsProcCategoryProcedureAsync(string fromDate, string toDate)
        {
            try
            {
                // Define the SQL statement to call the stored procedure
                var sql = "BEGIN UCHTRANS.SP_DS_PROC_CATEGORY(:FROMDATE, :TODATE, :STROUT); END;";

                // Format the dates to match Oracle's expected format (e.g., DD-MM-YY)
                string formattedFromDate = DateTime.Parse(fromDate).ToString("dd-MM-yyyy");
                string formattedToDate = DateTime.Parse(toDate).ToString("dd-MM-yyyy");

                // Define the parameters
                var fromDateParam = new OracleParameter("FROMDATE", OracleDbType.Varchar2) { Value = formattedFromDate };
                var toDateParam = new OracleParameter("TODATE", OracleDbType.Varchar2) { Value = formattedToDate };
                var strOutParam = new OracleParameter("STROUT", OracleDbType.RefCursor) { Direction = ParameterDirection.Output };

                // Execute the stored procedure
                using (var cmd = _DbContext.Database.GetDbConnection().CreateCommand())
                {
                    cmd.CommandText = sql;
                    cmd.CommandType = System.Data.CommandType.Text;

                    // Add parameters to the command
                    cmd.Parameters.Add(fromDateParam);
                    cmd.Parameters.Add(toDateParam);
                    cmd.Parameters.Add(strOutParam);

                    await _DbContext.Database.GetDbConnection().OpenAsync();

                    using (var reader = (OracleDataReader)await cmd.ExecuteReaderAsync())
                    {
                        var results = new List<dynamic>();

                        while (await reader.ReadAsync())
                        {
                            var result = new
                            {
                                PRCCAT_ID = reader["PRCCAT_ID"],
                                PRCCAT_NAME = reader["PRCCAT_NAME"],
                                NETAMOUNT = reader["NETAMOUNT"]
                            };

                            results.Add(result);
                        }

                        return results;
                    }
                }
            }
            catch (Exception ex)
            {
                var msg1 = new DefaultMessage.Message1
                {
                    Status = 500,
                    Message = ex.Message
                };
                return msg1;
            }
        }

        //SP_DS_PROC_GROUP  5
        public async Task<dynamic> DsProcGroupProcedureAsync(string fromDate, string toDate)
        {
            try
            {
                // Define the SQL statement to call the stored procedure
                var sql = "BEGIN UCHTRANS.SP_DS_PROC_GROUP(:FROMDATE, :TODATE, :STROUT); END;";

                // Format the dates to match Oracle's expected format (e.g., DD-MM-YY)
                string formattedFromDate = DateTime.Parse(fromDate).ToString("dd-MM-yyyy");
                string formattedToDate = DateTime.Parse(toDate).ToString("dd-MM-yyyy");

                // Define the parameters
                var fromDateParam = new OracleParameter("FROMDATE", OracleDbType.Varchar2) { Value = formattedFromDate };
                var toDateParam = new OracleParameter("TODATE", OracleDbType.Varchar2) { Value = formattedToDate };
                var strOutParam = new OracleParameter("STROUT", OracleDbType.RefCursor) { Direction = ParameterDirection.Output };

                // Execute the stored procedure
                using (var cmd = _DbContext.Database.GetDbConnection().CreateCommand())
                {
                    cmd.CommandText = sql;
                    cmd.CommandType = System.Data.CommandType.Text;

                    // Add parameters to the command
                    cmd.Parameters.Add(fromDateParam);
                    cmd.Parameters.Add(toDateParam);
                    cmd.Parameters.Add(strOutParam);

                    await _DbContext.Database.GetDbConnection().OpenAsync();

                    using (var reader = (OracleDataReader)await cmd.ExecuteReaderAsync())
                    {
                        var results = new List<dynamic>();

                        while (await reader.ReadAsync())
                        {
                            var result = new
                            {
                                PRC_GRP_ID = reader["PRC_GRP_ID"],
                                PRC_GRP_NAME = reader["PRC_GRP_NAME"],
                                NETAMOUNT = reader["NETAMOUNT"]
                            };

                            results.Add(result);
                        }

                        return results;
                    }
                }
            }
            catch (Exception ex)
            {
                var msg1 = new DefaultMessage.Message1
                {
                    Status = 500,
                    Message = ex.Message
                };
                return msg1;
            }
        }

        //SP_DS_PROCEDURE  6
        public async Task<dynamic> DsProcedureProcedureAsync(string fromDate, string toDate)
        {
            try
            {
                // Define the SQL statement to call the stored procedure
                var sql = "BEGIN UCHTRANS.SP_DS_PROCEDURE(:DATE_FROM, :DATE_TO, :STROUT); END;";

                // Format the dates to match Oracle's expected format (e.g., DD-MM-YY)
                string formattedFromDate = DateTime.Parse(fromDate).ToString("dd-MM-yy");
                string formattedToDate = DateTime.Parse(toDate).ToString("dd-MM-yy");

                // Define the parameters
                var dateFromParam = new OracleParameter("DATE_FROM", OracleDbType.Varchar2) { Value = formattedFromDate };
                var dateToParam = new OracleParameter("DATE_TO", OracleDbType.Varchar2) { Value = formattedToDate };
                var strOutParam = new OracleParameter("STROUT", OracleDbType.RefCursor) { Direction = ParameterDirection.Output };

                // Execute the stored procedure
                using (var cmd = _DbContext.Database.GetDbConnection().CreateCommand())
                {
                    cmd.CommandText = sql;
                    cmd.CommandType = System.Data.CommandType.Text;

                    // Add parameters to the command
                    cmd.Parameters.Add(dateFromParam);
                    cmd.Parameters.Add(dateToParam);
                    cmd.Parameters.Add(strOutParam);

                    await _DbContext.Database.GetDbConnection().OpenAsync();

                    using (var reader = (OracleDataReader)await cmd.ExecuteReaderAsync())
                    {
                        var results = new List<dynamic>();

                        while (await reader.ReadAsync())
                        {
                            var result = new
                            {
                                PRC_ID = reader["PRC_ID"].ToString(),
                                PRC_NAME = reader["PRC_NAME"].ToString(),
                                NetAmount = Convert.ToDecimal(reader["NETAMOUNT"]),
                                ProcedureCount = Convert.ToInt32(reader["PROC_CNT"])
                            };

                            results.Add(result);
                        }

                        return results;
                    }
                }
            }
            catch (Exception ex)
            {
                var msg1 = new DefaultMessage.Message1
                {
                    Status = 500,
                    Message = ex.Message
                };
                return msg1;
            }
        }

        //SP_DS_PURCHASE  2
        public async Task<dynamic> DsPurchaseProcedureAsync(string dateFrom, string dateTo)
        {
            try
            {
                // Define the SQL statement to call the stored procedure
                var sql = "BEGIN UCHTRANS.SP_DS_PURCHASE(:DATE_FROM, :DATE_TO, :STROUT); END;";

                // Format the dates to match Oracle's expected format (e.g., DD/MM/YY)
                string formattedDateFrom = DateTime.Parse(dateFrom).ToString("dd/MM/yyyy");
                string formattedDateTo = DateTime.Parse(dateTo).ToString("dd/MM/yyyy");

                // Define the parameters
                var dateFromParam = new OracleParameter("DATE_FROM", OracleDbType.Varchar2) { Value = formattedDateFrom };
                var dateToParam = new OracleParameter("DATE_TO", OracleDbType.Varchar2) { Value = formattedDateTo };
                var strOutParam = new OracleParameter("STROUT", OracleDbType.RefCursor) { Direction = ParameterDirection.Output };

                // Execute the stored procedure
                using (var cmd = _DbContext.Database.GetDbConnection().CreateCommand())
                {
                    cmd.CommandText = sql;
                    cmd.CommandType = System.Data.CommandType.Text;

                    // Add parameters to the command
                    cmd.Parameters.Add(dateFromParam);
                    cmd.Parameters.Add(dateToParam);
                    cmd.Parameters.Add(strOutParam);

                    await _DbContext.Database.GetDbConnection().OpenAsync();

                    using (var reader = (OracleDataReader)await cmd.ExecuteReaderAsync())
                    {
                        var results = new List<dynamic>();

                        while (await reader.ReadAsync())
                        {
                            var result = new
                            {
                                GP = reader["GP"],
                                BILL_CNT = reader["BILL_CNT"],
                                AMOUNT = reader["AMOUNT"]
                            };

                            results.Add(result);
                        }

                        return results;
                    }
                }
            }
            catch (Exception ex)
            {
                var msg1 = new DefaultMessage.Message1
                {
                    Status = 500,
                    Message = ex.Message
                };
                return msg1;
            }
        }

        //SP_DS_PACKAGE  11
        public async Task<dynamic> CallPackageProcedureAsync(string fromDate, string toDate)
        {
            try
            {
                // Define the SQL statement to call the stored procedure
                var sql = "BEGIN UCHTRANS.SP_DS_PACKAGE(:P_FROM_DATE, :P_TO_DATE, :STROUT); END;";

                // Format the dates to match Oracle's expected format (e.g., DD/MM/YYYY)
                string formattedFromDate = DateTime.Parse(fromDate).ToString("dd/MM/yyyy");
                string formattedToDate = DateTime.Parse(toDate).ToString("dd/MM/yyyy");

                // Define the parameters
                var fromDateParam = new OracleParameter("P_FROM_DATE", OracleDbType.Varchar2) { Value = formattedFromDate };
                var toDateParam = new OracleParameter("P_TO_DATE", OracleDbType.Varchar2) { Value = formattedToDate };
                var strOutParam = new OracleParameter("STROUT", OracleDbType.RefCursor) { Direction = ParameterDirection.Output };

                // Execute the stored procedure
                using (var cmd = _DbContext.Database.GetDbConnection().CreateCommand())
                {
                    cmd.CommandText = sql;
                    cmd.CommandType = System.Data.CommandType.Text;

                    // Add parameters to the command
                    cmd.Parameters.Add(fromDateParam);
                    cmd.Parameters.Add(toDateParam);
                    cmd.Parameters.Add(strOutParam);

                    await _DbContext.Database.GetDbConnection().OpenAsync();

                    using (var reader = (OracleDataReader)await cmd.ExecuteReaderAsync())
                    {
                        var results = new List<dynamic>();

                        while (await reader.ReadAsync())
                        {
                            var result = new
                            {
                                Section = reader["SECTION"].ToString(),
                                PackageAmount = Convert.ToDecimal(reader["PKG_AMT"])
                            };

                            results.Add(result);
                        }

                        return results;
                    }
                }
            }
            catch (Exception ex)
            {
                var msg1 = new DefaultMessage.Message1
                {
                    Status = 500,
                    Message = ex.Message
                };
                return msg1;
            }
        }


        //SP_DS_REFERAL_REPORT  15
        public async Task<dynamic> CallReferalReportProcedureAsync(string fromDate, string toDate)
        {
            try
            {
                // Define the SQL statement to call the stored procedure
                var sql = "BEGIN UCHTRANS.SP_DS_REFERAL_REPORT(:DATE_FROM, :DATE_TO, :STROUT); END;";

                // Format the dates to match Oracle's expected format (e.g., DD/MM/YYYY)
                string formattedFromDate = DateTime.Parse(fromDate).ToString("dd/MM/yyyy");
                string formattedToDate = DateTime.Parse(toDate).ToString("dd/MM/yyyy");

                // Define the parameters
                var dateFromParam = new OracleParameter("DATE_FROM", OracleDbType.Varchar2) { Value = formattedFromDate };
                var dateToParam = new OracleParameter("DATE_TO", OracleDbType.Varchar2) { Value = formattedToDate };
                var strOutParam = new OracleParameter("STROUT", OracleDbType.RefCursor) { Direction = ParameterDirection.Output };

                // Execute the stored procedure
                using (var cmd = _DbContext.Database.GetDbConnection().CreateCommand())
                {
                    cmd.CommandText = sql;
                    cmd.CommandType = System.Data.CommandType.Text;

                    // Add parameters to the command
                    cmd.Parameters.Add(dateFromParam);
                    cmd.Parameters.Add(dateToParam);
                    cmd.Parameters.Add(strOutParam);

                    await _DbContext.Database.GetDbConnection().OpenAsync();

                    using (var reader = (OracleDataReader)await cmd.ExecuteReaderAsync())
                    {
                        var results = new List<dynamic>();

                        while (await reader.ReadAsync())
                        {
                            var result = new
                            {
                                ReferredBy = reader["REFERED_BY"].ToString(),
                                NetAmount = Convert.ToDecimal(reader["NET_AMT"])
                            };

                            results.Add(result);
                        }

                        return results;
                    }
                }
            }
            catch (Exception ex)
            {
                var msg1 = new DefaultMessage.Message1
                {
                    Status = 500,
                    Message = ex.Message
                };
                return msg1;
            }
        }




    }
}
