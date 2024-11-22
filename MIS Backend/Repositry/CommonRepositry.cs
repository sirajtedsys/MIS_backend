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
                //var data = await _DbContext.EMR_ADMIN_USERS
                //    .Where(x => x.AUSR_USERNAME == username
                //    && x.AUSR_PWD == password
                //    && x.AUSR_STATUS != "D")
                //    .ToListAsync();
                var data = await _DbContext.HRM_EMPLOYEE
                   .Where(e => e.EMP_ACTIVE_STATUS == 'A' &&
                               e.EMP_LOGIN_NAME == username &&
                               e.EMP_PASSWORD == password)
                   .Select(e => new
                   {
                       e.EMP_ID,
                       e.EMP_OFFL_NAME,
                       e.EMP_LOGIN_NAME,
                       e.EMP_PASSWORD
                   })
                   .ToListAsync();

                //return data/*;*/

                //return userdata;

                //SELECT B.BRANCH_ID, B.BRCH_NAME FROM UCHMASTER.HRM_BRANCH B
                //                INNER JOIN UCHEMR.EMR_ADMIN_USERS_BRANCH_LINK BL ON BL.BRANCH_ID = B.BRANCH_ID
                //                INNER JOIN UCHEMR.EMR_ADMIN_USERS USR ON USR.AUSR_ID = BL.AUSR_ID
                //                WHERE NVL(B.ACTIVE_STATUS, 'A')= 'A' AND ausr_username = 'tedsys' AND ausr_pwd = 'ted@123' ORDER BY B.BRANCH_ID



                if (data != null && data.Count>0)
                {
                    var userdat = new UserTocken
                    {
                        AUSR_ID = data[0].EMP_ID,
                        USERNAME = data[0].EMP_LOGIN_NAME,
                        PASSWORD = data[0].EMP_PASSWORD
                    };

                    var token = jwthand.GenerateToken(userdat);

                    //check exiting userdetail in loginsettings
                    var dat = await _DbContext.LOGIN_SETTINGS.Where(x => x.USERID == data[0].EMP_ID).ToListAsync();
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

                //var result =
                //    from b in _DbContext.HRM_BRANCH
                //    join bl in _DbContext.EMR_ADMIN_USERS_BRANCH_LINK
                //        on b.BRANCH_ID equals bl.BRANCH_ID
                //    join usr in _DbContext.EMR_ADMIN_USERS
                //        on bl.AUSR_ID equals usr.AUSR_ID
                //    where b.ACTIVE_STATUS == "A"
                //          && usr.AUSR_USERNAME == ut.USERNAME
                //          && usr.AUSR_PWD == ut.PASSWORD
                //    orderby b.BRANCH_ID
                //    select new
                //    {
                //        b.BRANCH_ID,
                //        b.BRCH_NAME
                //    };
                var result = (from b in _DbContext.HRM_BRANCH
                              join bl in _DbContext.HRM_EMPLOYEE_BRANCH_LINK on b.BRANCH_ID equals bl.BRANCH_ID
                              join hr in _DbContext.HRM_EMPLOYEE_HR on bl.EMP_ID equals hr.EMP_ID
                              join emp in _DbContext.HRM_EMPLOYEE on hr.EMP_ID equals emp.EMP_ID_HR
                              where b.ACTIVE_STATUS  == "A"
                                    && emp.EMP_LOGIN_NAME == ut.USERNAME
                                    && emp.EMP_PASSWORD == ut.PASSWORD
                              orderby b.BRANCH_ID
                              select new
                              {
                                  b.BRANCH_ID,
                                  b.BRCH_NAME
                              }).ToList();
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


        //userwntedreports
        public async Task<dynamic> GetAppMenuAsync(string userId)
        {
            try
            {
                // Define the SQL statement to call the stored procedure
                var sql = "BEGIN UCHTRANS.SP_MIS_APP_MENU(:USER_ID, :STROUT); END;";

                // Define the parameters
                var userIdParam = new OracleParameter("USER_ID", OracleDbType.Varchar2) { Value = userId };
                var strOutParam = new OracleParameter("STROUT", OracleDbType.RefCursor) { Direction = ParameterDirection.Output };

                // Execute the stored procedure
                using (var cmd = _DbContext.Database.GetDbConnection().CreateCommand())
                {
                    cmd.CommandText = sql;
                    cmd.CommandType = System.Data.CommandType.Text;

                    // Add parameters to the command
                    cmd.Parameters.Add(userIdParam);
                    cmd.Parameters.Add(strOutParam);

                    await _DbContext.Database.GetDbConnection().OpenAsync();

                    using (var reader = (OracleDataReader)await cmd.ExecuteReaderAsync())
                    {
                        var results = new List<dynamic>();

                        while (await reader.ReadAsync())
                        {
                            var result = new
                            {
                                TabName = reader["TAB_NAME"] != DBNull.Value ? reader["TAB_NAME"].ToString() : null,
                                Link = reader["LINK"] != DBNull.Value ? reader["LINK"].ToString() : null,
                                Priority = reader["PRIORITY"] != DBNull.Value ? Convert.ToInt32(reader["PRIORITY"]) : (int?)null
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

                // Format the dates to match Oracle's expected format (e.g., DD/MM/YYYY)
                string formattedFromDate = DateTime.Parse(fromDate).ToString("dd/MM/yyyy");
                string formattedToDate = DateTime.Parse(toDate).ToString("dd/MM/yyyy");

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
                                // Ensure proper handling for PRCCAT_ID
                                PRCCAT_ID = reader["PRCCAT_ID"] != DBNull.Value ? reader["PRCCAT_ID"].ToString() : "N/A",
                                PRCCAT_NAME = reader["PRCCAT_NAME"] != DBNull.Value ? reader["PRCCAT_NAME"].ToString() : "N/A",

                                // Use Convert.ToDecimal safely
                                GROSS = reader["GROSS"] != DBNull.Value ? Convert.ToDecimal(reader["GROSS"]) : 0.0m,
                                DISCOUNT = reader["DISCOUNT"] != DBNull.Value ? Convert.ToDecimal(reader["DISCOUNT"]) : 0.0m,
                                NETAMOUNT = reader["NETAMOUNT"] != DBNull.Value ? Convert.ToDecimal(reader["NETAMOUNT"]) : 0.0m
                            };

                            results.Add(result);
                        }

                        return results;
                    }
                }
            }
            catch (FormatException ex)
            {
                // Handle format exceptions specifically
                var msg1 = new DefaultMessage.Message1
                {
                    Status = 400, // Bad Request for format issues
                    Message = $"Format Error: {ex.Message}"
                };
                return msg1;
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

                // Format the dates to match Oracle's expected format (e.g., DD/MM/YY)
                string formattedFromDate = DateTime.Parse(fromDate).ToString("dd/MM/yy");
                string formattedToDate = DateTime.Parse(toDate).ToString("dd/MM/yy");

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
                            // Safely read values and handle potential nulls or format issues
                            var result = new
                            {
                                ProcedureGroupId = reader["PRC_GRP_ID"] != DBNull.Value ?
                                    (int.TryParse(reader["PRC_GRP_ID"].ToString(), out int id) ? id : 0) : 0, // Handle format issues
                                ProcedureGroupName = reader["PRC_GRP_NAME"] != DBNull.Value ?
                                    reader["PRC_GRP_NAME"].ToString() : "N/A",
                                GrossAmount = reader["GROSS"] != DBNull.Value ?
                                    Convert.ToDecimal(reader["GROSS"]) : 0.0m,
                                Discount = reader["DISCOUNT"] != DBNull.Value ?
                                    Convert.ToDecimal(reader["DISCOUNT"]) : 0.0m,
                                NetAmount = reader["NETAMOUNT"] != DBNull.Value ?
                                    Convert.ToDecimal(reader["NETAMOUNT"]) : 0.0m
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
                var sql = "BEGIN UCHTRANS.SP_DS_PROCEDURE(:P_FROM_DATE, :P_TO_DATE, :STROUT); END;";

                // Format the dates to match Oracle's expected format (e.g., DD/MM/YYYY)
                string formattedFromDate = DateTime.Parse(fromDate).ToString("dd-MM-yy");
                string formattedToDate = DateTime.Parse(toDate).ToString("dd-MM-yy");

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
                                PrcId = reader["PRC_ID"].ToString(),
                                PrcName = reader["PRC_NAME"].ToString(),
                                Gross = Convert.ToDecimal(reader["GROSS"]),
                                Discount = Convert.ToDecimal(reader["DISCOUNT"]),
                                NetAmount = Convert.ToDecimal(reader["NETAMOUNT"]),
                                ProcCount = Convert.ToInt32(reader["PROC_CNT"]),
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

                // Format the dates to match Oracle's expected format (e.g., DD/MM/YY)
                string formattedFromDate = DateTime.Parse(fromDate).ToString("dd/MM/yy");
                string formattedToDate = DateTime.Parse(toDate).ToString("dd/MM/yy");

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
                                NetAmount = Convert.ToDecimal(reader["NET_AMT"]),
                                GrossAmount = Convert.ToDecimal(reader["GROSS"]),
                                Discount = Convert.ToDecimal(reader["DISCOUNT"])
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



        //discount request op
        public async Task<dynamic> GetBillDiscountsAsync(string fromDate, string toDate, string strWhere = "")
        {
            try
            {
                // Ensure the dates are formatted as DD/MM/YY
                var formattedFromDate = string.IsNullOrEmpty(fromDate) ? (object)DBNull.Value : DateTime.Parse(fromDate).ToString("dd/MM/yy");
                var formattedToDate = string.IsNullOrEmpty(toDate) ? (object)DBNull.Value : DateTime.Parse(toDate).ToString("dd/MM/yy");

                // Define the SQL to call the stored procedure
                var sql = "BEGIN UCHTRANS.SP_OPN_RQST_BILL_DISCOUNT(:P_FROM_DATE, :P_TO_DATE, :P_STRWHERE, :STROUT); END;";

                // Define Oracle parameters
                var parameters = new[]
                {
                    new OracleParameter("P_FROM_DATE", OracleDbType.Varchar2) { Value = formattedFromDate },
                    new OracleParameter("P_TO_DATE", OracleDbType.Varchar2) { Value = formattedToDate },
                    new OracleParameter("P_STRWHERE", OracleDbType.Varchar2) { Value = string.IsNullOrEmpty(strWhere) ? (object)DBNull.Value : strWhere },
                    new OracleParameter("STROUT", OracleDbType.RefCursor) { Direction = ParameterDirection.Output }
                };

                // Execute the stored procedure
                using (var cmd = _DbContext.Database.GetDbConnection().CreateCommand())
                {
                    cmd.CommandText = sql;
                    cmd.CommandType = CommandType.Text;
                    cmd.Parameters.AddRange(parameters);

                    await _DbContext.Database.GetDbConnection().OpenAsync();

                    using (var reader = (OracleDataReader)await cmd.ExecuteReaderAsync())
                    {
                        var results = new List<dynamic>();

                        while (await reader.ReadAsync())
                        {
                            var result = new
                            {
                                RequestId = reader["RQST_ID"] != DBNull.Value ? Convert.ToInt32(reader["RQST_ID"]) : (int?)null,
                                OpVisitId = reader["OPVISIT_ID"] != DBNull.Value ? reader["OPVISIT_ID"].ToString() : null,
                                PatientId = reader["PATI_ID"] != DBNull.Value ? reader["PATI_ID"].ToString() : null,
                                DoctorId = reader["DOCT_ID"] != DBNull.Value ? reader["DOCT_ID"].ToString() : null,
                                CustomerId = reader["CUST_ID"] != DBNull.Value ? reader["CUST_ID"].ToString() : null,
                                CustomerName = reader["CUST_NAME"] != DBNull.Value ? reader["CUST_NAME"].ToString() : null,
                                DiscountPercentage = reader["DISC_PER"] != DBNull.Value ? Convert.ToDecimal(reader["DISC_PER"]) : (decimal?)null,
                                DiscountAmount = reader["DISC_AMT"] != DBNull.Value ? Convert.ToDecimal(reader["DISC_AMT"]) : (decimal?)null,
                                Remarks = reader["REMARKS"] != DBNull.Value ? reader["REMARKS"].ToString() : null,
                                RequestedBy = reader["RQSTD_BY"] != DBNull.Value ? reader["RQSTD_BY"].ToString() : null,
                                RequestedOn = reader["RQSTD_ON"] != DBNull.Value ? Convert.ToDateTime(reader["RQSTD_ON"]) : (DateTime?)null,
                                ApprovedBy = reader["APPRVD_BY"] != DBNull.Value ? reader["APPRVD_BY"].ToString() : null,
                                ApprovedOn = reader["APPRVD_ON"] != DBNull.Value ? Convert.ToDateTime(reader["APPRVD_ON"]) : (DateTime?)null,
                                RequestStatus = reader["REQUEST_STATUS"] != DBNull.Value ? reader["REQUEST_STATUS"].ToString() : null,
                                ApprovalRemarks = reader["APRVL_REMARKS"] != DBNull.Value ? reader["APRVL_REMARKS"].ToString() : null,
                                Doctor = reader["DOCTOR"] != DBNull.Value ? reader["DOCTOR"].ToString() : null,
                                RequestedUser = reader["RQSTD_USER"] != DBNull.Value ? reader["RQSTD_USER"].ToString() : null,
                                ApprovedUser = reader["APPRVD_USER"] != DBNull.Value ? reader["APPRVD_USER"].ToString() : null,
                                Status = reader["STATUS"] != DBNull.Value ? reader["STATUS"].ToString() : null,
                                PatientOpNo = reader["PATI_OPNO"] != DBNull.Value ? reader["PATI_OPNO"].ToString() : null,
                                PatientName = reader["PATI_NAME"] != DBNull.Value ? reader["PATI_NAME"].ToString() : null,
                                PatientGender = reader["PATI_GENDER"] != DBNull.Value ? reader["PATI_GENDER"].ToString() : null,
                                PatientMobile = reader["PATI_MOBILE"] != DBNull.Value ? reader["PATI_MOBILE"].ToString() : null
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


        //discount request lab
        public async Task<List<dynamic>> GetLabDiscountApprovalsAsync(string fromDate, string toDate, string type="D")
        {
            try
            {
                // Ensure the dates are formatted as DD/MM/YY
                var formattedFromDate = string.IsNullOrEmpty(fromDate) ? (object)DBNull.Value : DateTime.Parse(fromDate).ToString("dd/MM/yy");
                var formattedToDate = string.IsNullOrEmpty(toDate) ? (object)DBNull.Value : DateTime.Parse(toDate).ToString("dd/MM/yy");

                // Define the SQL statement to call the stored procedure
                var sql = "BEGIN UCHTRANS.SP_LBM_RQST_DISC_APRVL_new(:P_FROM_DATE, :P_TO_DATE, :P_TYPE, :STROUT); END;";

                // Define Oracle parameters
                var parameters = new[]
                {
                    new OracleParameter("P_FROM_DATE", OracleDbType.Varchar2) { Value = formattedFromDate },
                    new OracleParameter("P_TO_DATE", OracleDbType.Varchar2) { Value = formattedToDate },
                    new OracleParameter("P_TYPE", OracleDbType.Varchar2) { Value = string.IsNullOrEmpty(type) ? (object)DBNull.Value : type },
                    new OracleParameter("STROUT", OracleDbType.RefCursor) { Direction = ParameterDirection.Output }
                };

                // Execute the stored procedure
                using (var cmd = _DbContext.Database.GetDbConnection().CreateCommand())
                {
                    cmd.CommandText = sql;
                    cmd.CommandType = CommandType.Text;
                    cmd.Parameters.AddRange(parameters);

                    await _DbContext.Database.GetDbConnection().OpenAsync();

                    using (var reader = (OracleDataReader)await cmd.ExecuteReaderAsync())
                    {
                        var results = new List<dynamic>();

                        while (await reader.ReadAsync())
                        {
                            var result = new
                            {
                                RequestId = reader["RQST_ID"] != DBNull.Value ? Convert.ToInt32(reader["RQST_ID"]) : (int?)null,
                                PatientOpNo = reader["PATI_OPNO"]?.ToString(),
                                PatientName = reader["PATIENT_NAME"]?.ToString(),
                                ServiceName = reader["SERVICE_NAME"]?.ToString(),
                                BillDate = reader["BILL_DATE"] != DBNull.Value ? Convert.ToDateTime(reader["BILL_DATE"]) : (DateTime?)null,
                                BillNo = reader["BILL_NO"]?.ToString(),
                                RequestedFor = reader["REQUESTED_FOR"]?.ToString(),
                                LbmPlanSlNo = reader["LBM_PLAN_SLNO"] != DBNull.Value ? Convert.ToInt32(reader["LBM_PLAN_SLNO"]) : (int?)null,
                                Quantity = reader["QUANTITY"] != DBNull.Value ? Convert.ToInt32(reader["QUANTITY"]) : (int?)null,
                                ProcedureId = reader["PRC_ID"]?.ToString(),
                                DiscountPercentage = reader["DISC_PER"] != DBNull.Value ? Convert.ToDecimal(reader["DISC_PER"]) : (decimal?)null,
                                Remarks = reader["REMARKS"]?.ToString(),
                                RequestedBy = reader["RQSTED_BY"]?.ToString(),
                                RequestedOn = reader["RQSTD_ON"] != DBNull.Value ? Convert.ToDateTime(reader["RQSTD_ON"]) : (DateTime?)null,
                                EmrDocId = reader["EMR_DOC_ID"]?.ToString(),
                                TestName = reader["TEST_NAME"]?.ToString(),
                                RequestStatus = reader["RQST_STATUS"]?.ToString(),
                                Doctor = reader["DOCTOR"]?.ToString(),
                                IdCardNo = reader["IDCARD_NO"]?.ToString(),
                                ProcedureRate = reader["PRC_RATE"] != DBNull.Value ? Convert.ToDecimal(reader["PRC_RATE"]) : (decimal?)null,
                                ApprovalRemarks = reader["APRVL_REMARKS"]?.ToString(),
                                DiscountAmount = reader["DISC_AMT"] != DBNull.Value ? Convert.ToDecimal(reader["DISC_AMT"]) : (decimal?)null
                            };

                            results.Add(result);
                        }

                        return results;
                    }
                }
            }
            catch (Exception ex)
            {
                // Log the exception and return error information
                return new List<dynamic>
        {
            new
            {
                Status = 500,
                Message = ex.Message
            }
        };
            }
        }


        //op discount approval
        public async Task<dynamic> UpdateOpDiscountAppAsync(List<DiscountUpdateModel> requests, UserTocken ut)
        {
            try
            {
                if (requests == null || !requests.Any())
                {
                    return new { Status = 400, Message = "No requests provided" };
                }

                var query = @"
        UPDATE UCHTRANS.OPN_RQST_BILL_DISCOUNT
        SET DISC_AMT = :DiscountAmount, 
            APRVL_REMARKS = :ApprovalRemarks, 
            REQUEST_STATUS = 'A', 
            APPRVD_ON = SYSDATE, 
            DISC_PER = :DiscountPercentage, 
            APPRVD_BY = :ApprovedBy
        WHERE RQST_ID = :RequestId";

                using (var connection = _DbContext.Database.GetDbConnection())
                {
                    await connection.OpenAsync();

                    using (var transaction = connection.BeginTransaction())
                    {
                        try
                        {
                            foreach (var request in requests)
                            {
                                var parameters = new List<OracleParameter>
                            {
                                new OracleParameter("DiscountAmount", OracleDbType.Decimal) { Value = request.DiscountAmount },
                                new OracleParameter("ApprovalRemarks", OracleDbType.Varchar2) { Value = request.Remarks },
                                new OracleParameter("DiscountPercentage", OracleDbType.Decimal) { Value = request.DiscountPercentage },
                                new OracleParameter("ApprovedBy", OracleDbType.Varchar2) { Value = ut.AUSR_ID },
                                new OracleParameter("RequestId", OracleDbType.Int32) { Value = request.RequestId }
                            };

                                using (var command = connection.CreateCommand())
                                {
                                    command.CommandText = query;
                                    command.CommandType = CommandType.Text;
                                    command.Parameters.AddRange(parameters.ToArray());

                                    await command.ExecuteNonQueryAsync();
                                }
                            }

                            transaction.Commit();

                            return new { Status = 200, Message = $"{requests.Count} requests successfully approved" };
                        }
                        catch (Exception ex)
                        {
                            transaction.Rollback();
                            return new { Status = 500, Message = $"Error approving discounts: {ex.Message}" };
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                return new { Status = 500, Message = $"Error: {ex.Message}" };
            }
        }

        //op doiscount rejection
        public async Task<dynamic> RejectOPDiscountAsync(List<DiscountUpdateModel> requests, UserTocken ut)
        {
            try
            {
                if (requests == null || !requests.Any())
                {
                    return new { Status = 400, Message = "No requests provided" };
                }

                var query = @"
        UPDATE UCHTRANS.OPN_RQST_BILL_DISCOUNT
        SET APRVL_REMARKS = :ApprovalRemarks,
            REQUEST_STATUS = 'R',
            APPRVD_ON = SYSDATE,
            APPRVD_BY = :ApprovedBy
        WHERE RQST_ID = :RequestId";

                using (var connection = _DbContext.Database.GetDbConnection())
                {
                    await connection.OpenAsync();

                    using (var transaction = connection.BeginTransaction())
                    {
                        try
                        {
                            foreach (var request in requests)
                            {
                                var parameters = new List<OracleParameter>
                            {
                                new OracleParameter("ApprovalRemarks", OracleDbType.Varchar2) { Value = request.Remarks },
                                new OracleParameter("ApprovedBy", OracleDbType.Varchar2) { Value = ut.AUSR_ID },
                                new OracleParameter("RequestId", OracleDbType.Int32) { Value = request.RequestId }
                            };

                                using (var command = connection.CreateCommand())
                                {
                                    command.CommandText = query;
                                    command.CommandType = CommandType.Text;
                                    command.Parameters.AddRange(parameters.ToArray());

                                    await command.ExecuteNonQueryAsync();
                                }
                            }

                            transaction.Commit();

                            return new { Status = 200, Message = $"{requests.Count} requests successfully rejected" };
                        }
                        catch (Exception ex)
                        {
                            transaction.Rollback();
                            return new { Status = 500, Message = $"Error rejecting discounts: {ex.Message}" };
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                return new { Status = 500, Message = $"Error: {ex.Message}" };
            }
        }


        //lab request approval
        public async Task<dynamic> UpdateLbmRequestsAsync(List<DiscountUpdateModel> requests, UserTocken ut)
        {
            try
            {
                if (requests == null || !requests.Any())
                {
                    return new { Status = 400, Message = "No requests provided" };
                }

                var query = @"
        UPDATE UCHTRANS.LBM_RQST_DISC_OR_CANCEL 
        SET APPRVD_BY = :ApprovedBy,
            APPRVD_ON = SYSDATE,
            RQST_STATUS = :RequestStatus,
            DISC_PER = :DiscountPercentage,
            REMARKS = :Remarks,
            DISC_AMT = :DiscountAmount
        WHERE RQST_ID = :RequestId";

                using (var connection = _DbContext.Database.GetDbConnection())
                {
                    await connection.OpenAsync();

                    using (var transaction = connection.BeginTransaction())
                    {
                        try
                        {
                            foreach (var request in requests)
                            {
                                var parameters = new List<OracleParameter>
                            {
                                new OracleParameter("ApprovedBy", OracleDbType.Varchar2) { Value = ut.AUSR_ID },
                                new OracleParameter("RequestStatus", OracleDbType.Varchar2) { Value = request.Status },
                                new OracleParameter("DiscountPercentage", OracleDbType.Decimal) { Value = request.DiscountPercentage },
                                new OracleParameter("Remarks", OracleDbType.Varchar2) { Value = request.Remarks },
                                new OracleParameter("DiscountAmount", OracleDbType.Decimal) { Value = request.DiscountAmount },
                                new OracleParameter("RequestId", OracleDbType.Int32) { Value = request.RequestId }
                            };

                                using (var command = connection.CreateCommand())
                                {
                                    command.CommandText = query;
                                    command.CommandType = CommandType.Text;
                                    command.Parameters.AddRange(parameters.ToArray());

                                    await command.ExecuteNonQueryAsync();
                                }
                            }

                            transaction.Commit();

                            var st = requests[0].Status == "A" ? "Approved" : requests[0].Status == "R" ? "Rejected" : "";

                            return new { Status = 200, Message = $"{requests.Count} requests successfully " +st };
                        }
                        catch (Exception ex)
                        {
                            transaction.Rollback();
                            return new { Status = 500, Message = $"Error processing requests: {ex.Message}" };
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                return new { Status = 500, Message = $"Error: {ex.Message}" };
            }
        }
    }


    ////op discount approval
    //public async Task<dynamic> UpdateOpDiscountAppAsync(int requestId, decimal discountAmount, string remarks, decimal discountPercentage, UserTocken ut)
    //{
    //    try
    //    {
    //        // SQL query with placeholders for parameters
    //        var query = @"
    //UPDATE UCHTRANS.OPN_RQST_BILL_DISCOUNT
    //SET DISC_AMT = :DiscountAmount, 
    //    APRVL_REMARKS = :ApprovalRemarks, 
    //    REQUEST_STATUS = 'A', 
    //    APPRVD_ON = SYSDATE, 
    //    DISC_PER = :DiscountPercentage, 
    //    APPRVD_BY = :ApprovedBy
    //WHERE RQST_ID = :RequestId";

    //        // Define the parameters
    //        var parameters = new List<OracleParameter>
    //{
    //    new OracleParameter("DiscountAmount", OracleDbType.Decimal) { Value = discountAmount },
    //    new OracleParameter("ApprovalRemarks", OracleDbType.Varchar2) { Value = remarks },
    //    new OracleParameter("DiscountPercentage", OracleDbType.Decimal) { Value = discountPercentage },
    //    new OracleParameter("ApprovedBy", OracleDbType.Varchar2) { Value = ut.AUSR_ID },
    //    new OracleParameter("RequestId", OracleDbType.Int32) { Value = requestId }
    //};

    //        // Execute the query
    //        using (var connection = _DbContext.Database.GetDbConnection())
    //        {
    //            await connection.OpenAsync();

    //            using (var command = connection.CreateCommand())
    //            {
    //                command.CommandText = query;
    //                command.CommandType = CommandType.Text;
    //                command.Parameters.AddRange(parameters.ToArray());

    //                var rowsAffected = await command.ExecuteNonQueryAsync();
    //                if (rowsAffected > 0)
    //                {
    //                    return new
    //                    {
    //                        Status = 200,
    //                        Message = "Successfully approved"
    //                    };
    //                }
    //            }
    //        }

    //        // If no rows were updated, return an error
    //        return new
    //        {
    //            Status = 500,
    //            Message = "Failed to approve discount"
    //        };
    //    }
    //    catch (Exception ex)
    //    {
    //        return new
    //        {
    //            Status = 500,
    //            Message = ex.Message
    //        };
    //    }
    //}


    ////op discoutn rejection
    //public async Task<dynamic> RejectOPDiscountAsync(int requestId, string approvalRemarks, UserTocken ut)
    //{
    //    try
    //    {
    //        // SQL query with placeholders for parameters
    //        var query = @"
    //UPDATE UCHTRANS.OPN_RQST_BILL_DISCOUNT
    //SET APRVL_REMARKS = :ApprovalRemarks,
    //    REQUEST_STATUS = 'R',
    //    APPRVD_ON = SYSDATE,
    //    APPRVD_BY = :ApprovedBy
    //WHERE RQST_ID = :RequestId";

    //        // Define the parameters
    //        var parameters = new List<OracleParameter>
    //{
    //    new OracleParameter("ApprovalRemarks", OracleDbType.Varchar2) { Value = approvalRemarks },
    //    new OracleParameter("ApprovedBy", OracleDbType.Varchar2) { Value = ut.AUSR_ID },
    //    new OracleParameter("RequestId", OracleDbType.Int32) { Value = requestId }
    //};

    //        // Execute the query
    //        using (var connection = _DbContext.Database.GetDbConnection())
    //        {
    //            await connection.OpenAsync();

    //            using (var command = connection.CreateCommand())
    //            {
    //                command.CommandText = query;
    //                command.CommandType = CommandType.Text;
    //                command.Parameters.AddRange(parameters.ToArray());

    //                var rowsAffected = await command.ExecuteNonQueryAsync();
    //                if (rowsAffected > 0)
    //                {
    //                    return new
    //                    {
    //                        Status = 200,
    //                        Message = "Successfully rejected"
    //                    };
    //                }
    //            }
    //        }

    //        // If no rows were updated, return an error
    //        return new
    //        {
    //            Status = 500,
    //            Message = "Failed to reject discount"
    //        };
    //    }
    //    catch (Exception ex)
    //    {
    //        return new
    //        {
    //            Status = 500,
    //            Message = ex.Message
    //        };
    //    }
    //}

    ////procedure bill update

    //public async Task<dynamic> UpdateLbmRequestAsync(int requestId, string status, decimal discountPercentage, string remarks, decimal discountAmount, UserTocken ut)
    //{
    //    try
    //    {
    //        // SQL query with placeholders for parameters
    //        var query = @"
    //    UPDATE UCHTRANS.LBM_RQST_DISC_OR_CANCEL 
    //    SET APPRVD_BY = :ApprovedBy,
    //        APPRVD_ON = SYSDATE,
    //        RQST_STATUS = :RequestStatus,
    //        DISC_PER = :DiscountPercentage,
    //        REMARKS = :Remarks,
    //        DISC_AMT = :DiscountAmount
    //    WHERE RQST_ID = :RequestId";

    //        // Define the parameters
    //        var parameters = new List<OracleParameter>
    //{
    //    new OracleParameter("ApprovedBy", OracleDbType.Varchar2) { Value = ut.AUSR_ID },
    //    new OracleParameter("RequestStatus", OracleDbType.Varchar2) { Value = status },
    //    new OracleParameter("DiscountPercentage", OracleDbType.Decimal) { Value = discountPercentage },
    //    new OracleParameter("Remarks", OracleDbType.Varchar2) { Value = remarks },
    //    new OracleParameter("DiscountAmount", OracleDbType.Decimal) { Value = discountAmount },
    //    new OracleParameter("RequestId", OracleDbType.Int32) { Value = requestId }
    //};

    //        // Execute the query
    //        using (var connection = _DbContext.Database.GetDbConnection())
    //        {
    //            await connection.OpenAsync();

    //            using (var command = connection.CreateCommand())
    //            {
    //                command.CommandText = query;
    //                command.CommandType = CommandType.Text;
    //                command.Parameters.AddRange(parameters.ToArray());

    //                var rowsAffected = await command.ExecuteNonQueryAsync();

    //                // Return success response if rows were updated
    //                if (rowsAffected > 0)
    //                {
    //                    if(status=="A")
    //                    {
    //                        return new
    //                        {
    //                            Status = 200,
    //                            Message = "successfully Approved"
    //                        };
    //                    }
    //                    else
    //                    {
    //                        return new
    //                        {
    //                            Status = 200,
    //                            Message = "Rejected successfully"
    //                        };
    //                    }

    //                }
    //            }
    //        }

    //        // Return failure response if no rows were updated
    //        return new
    //        {
    //            Status = 500,
    //            Message = "No rows updated"
    //        };
    //    }
    //    catch (Exception ex)
    //    {
    //        // Return error response
    //        return new
    //        {
    //            Status = 500,
    //            Message = $"An error occurred: {ex.Message}"
    //        };
    //    }
    //}





}
