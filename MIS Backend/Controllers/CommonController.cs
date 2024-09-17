using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using MIS_Backend.Data.Class;
using MIS_Backend.Repositry;
using static JwtService;
using System.Runtime.ConstrainedExecution;

namespace MIS_Backend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CommonController : ControllerBase
    {
        private readonly CommonRepositry comrep;
        private readonly JwtHandler jwtHandler;

        public CommonController(CommonRepositry _comrep, JwtHandler _jwthand)
        {
            comrep = _comrep;
            jwtHandler = _jwthand;
        }


        [HttpPost("CheckLogin")]
        public async Task<dynamic> LoginCheck(Login log)
        {
            if (log != null)
            {
                var resp = await comrep.LoginCheck(log.Username,log.Password);
                return resp;
            }
            else
            {
                return BadRequest();
            }
         }

        [HttpGet("GetAllUserBranches")]
        public async Task<dynamic> GetAllUserBranches()
        {
            try
            {
                // Retrieve token from Authorization header
                string authorizationHeader = Request.Headers["Authorization"];

                if (string.IsNullOrEmpty(authorizationHeader))
                {
                    return Unauthorized();
                }

                // Extract token from header (remove "Bearer " prefix)
                string token = authorizationHeader.Replace("Bearer ", "");

                // Decode token (not decrypt, assuming DecriptTocken is for decoding)
                UserTocken decodedToken = jwtHandler.DecriptTocken(authorizationHeader);

                if (decodedToken == null)
                {
                    return Unauthorized();
                }

                // Validate token
                var isValid = await jwtHandler.ValidateToken(token);

                if (isValid)
                {
                    // Return user details or appropriate response
                    //return Ok(new { Message = "User details retrieved successfully", UserDetails = decodedToken });
                    return await comrep.GetAllUserBranches(decodedToken);
                }
                else
                {
                    return Unauthorized();
                }
            }
            catch (Exception ex)
            {
                // Log the exception
                Console.WriteLine($"Error in UserDetails: {ex.Message}");
                return StatusCode(500, "Internal server error");
            }

        }


        [HttpGet("CallPurchaseOrderProcedureAsync")]
        public async Task<dynamic> CallPurchaseOrderProcedureAsync(string fromd, string tod)
        {
            try
            {
                // Retrieve token from Authorization header
                string authorizationHeader = Request.Headers["Authorization"];

                if (string.IsNullOrEmpty(authorizationHeader))
                {
                    return Unauthorized();
                }

                // Extract token from header (remove "Bearer " prefix)
                string token = authorizationHeader.Replace("Bearer ", "");

                // Decode token (not decrypt, assuming DecriptTocken is for decoding)
                UserTocken decodedToken = jwtHandler.DecriptTocken(authorizationHeader);

                if (decodedToken == null)
                {
                    return Unauthorized();
                }

                // Validate token
                var isValid = await jwtHandler.ValidateToken(token);

                if (isValid)
                {
                    // Return user details or appropriate response
                    //return Ok(new { Message = "User details retrieved successfully", UserDetails = decodedToken });
                    return await comrep.CallPurchaseOrderProcedureAsync(fromd ,tod);
                }
                else
                {
                    return Unauthorized();
                }
            }
            catch (Exception ex)
            {
                // Log the exception
                Console.WriteLine($"Error in UserDetails: {ex.Message}");
                return StatusCode(500, "Internal server error");
            }

        }




        [HttpGet("SP_DS_APPNMNT_STS")]
        public async Task<dynamic> SP_DS_APPNMNT_STS(string fromd,string tod)
        {
            try
            {
                // Retrieve token from Authorization header
                string authorizationHeader = Request.Headers["Authorization"];

                if (string.IsNullOrEmpty(authorizationHeader))
                {
                    return Unauthorized();
                }

                // Extract token from header (remove "Bearer " prefix)
                string token = authorizationHeader.Replace("Bearer ", "");

                // Decode token (not decrypt, assuming DecriptTocken is for decoding)
                UserTocken decodedToken = jwtHandler.DecriptTocken(authorizationHeader);

                if (decodedToken == null)
                {
                    return Unauthorized();
                }

                // Validate token
                var isValid = await jwtHandler.ValidateToken(token);

                if (isValid)
                {
                    // Return user details or appropriate response
                    //return Ok(new { Message = "User details retrieved successfully", UserDetails = decodedToken });
                    return await comrep.CallAppnmntStsProcedureAsync(fromd,tod);
                }
                else
                {
                    return Unauthorized();
                }
            }
            catch (Exception ex)
            {
                // Log the exception
                Console.WriteLine($"Error in SP_DS_APPNMNT_STS: {ex.Message}");
                return StatusCode(500, "Internal server error");
            }

        }

        [HttpGet("sp_ds_op_sts")]
        public async Task<dynamic> sp_ds_op_sts(string fromd, string tod)
        {
            try
            {
                // Retrieve token from Authorization header
                string authorizationHeader = Request.Headers["Authorization"];

                if (string.IsNullOrEmpty(authorizationHeader))
                {
                    return Unauthorized();
                }

                // Extract token from header (remove "Bearer " prefix)
                string token = authorizationHeader.Replace("Bearer ", "");

                // Decode token (not decrypt, assuming DecriptTocken is for decoding)
                UserTocken decodedToken = jwtHandler.DecriptTocken(authorizationHeader);

                if (decodedToken == null)
                {
                    return Unauthorized();
                }

                // Validate token
                var isValid = await jwtHandler.ValidateToken(token);

                if (isValid)
                {
                    // Return user details or appropriate response
                    //return Ok(new { Message = "User details retrieved successfully", UserDetails = decodedToken });
                    return await comrep.CallOpStsProcedureAsync(fromd, tod);
                }
                else
                {
                    return Unauthorized();
                }
            }
            catch (Exception ex)
            {
                // Log the exception
                Console.WriteLine($"Error in sp_ds_op_sts: {ex.Message}");
                return StatusCode(500, "Internal server error");
            }

        }


        [HttpGet("CallDeptRevProcedureAsync")]
        public async Task<dynamic> CallDeptRevProcedureAsync(string fromd, string tod)
        {
            try
            {
                // Retrieve token from Authorization header
                string authorizationHeader = Request.Headers["Authorization"];

                if (string.IsNullOrEmpty(authorizationHeader))
                {
                    return Unauthorized();
                }

                // Extract token from header (remove "Bearer " prefix)
                string token = authorizationHeader.Replace("Bearer ", "");

                // Decode token (not decrypt, assuming DecriptTocken is for decoding)
                UserTocken decodedToken = jwtHandler.DecriptTocken(authorizationHeader);

                if (decodedToken == null)
                {
                    return Unauthorized();
                }

                // Validate token
                var isValid = await jwtHandler.ValidateToken(token);

                if (isValid)
                {
                    // Return user details or appropriate response
                    //return Ok(new { Message = "User details retrieved successfully", UserDetails = decodedToken });
                    return await comrep.CallDeptRevProcedureAsync(fromd, tod);
                }
                else
                {
                    return Unauthorized();
                }
            }
            catch (Exception ex)
            {
                // Log the exception
                Console.WriteLine($"Error in CallDeptRevProcedureAsync: {ex.Message}");
                return StatusCode(500, "Internal server error");
            }

        }

        [HttpGet("DsDoctRevProcedureAsync")]
        public async Task<dynamic> DsDoctRevProcedureAsync(string fromd, string tod)
        {
            try
            {
                // Retrieve token from Authorization header
                string authorizationHeader = Request.Headers["Authorization"];

                if (string.IsNullOrEmpty(authorizationHeader))
                {
                    return Unauthorized();
                }

                // Extract token from header (remove "Bearer " prefix)
                string token = authorizationHeader.Replace("Bearer ", "");

                // Decode token (not decrypt, assuming DecriptTocken is for decoding)
                UserTocken decodedToken = jwtHandler.DecriptTocken(authorizationHeader);

                if (decodedToken == null)
                {
                    return Unauthorized();
                }

                // Validate token
                var isValid = await jwtHandler.ValidateToken(token);

                if (isValid)
                {
                    // Return user details or appropriate response
                    //return Ok(new { Message = "User details retrieved successfully", UserDetails = decodedToken });
                    return await comrep.DsDoctRevProcedureAsync(fromd, tod);
                }
                else
                {
                    return Unauthorized();
                }
            }
            catch (Exception ex)
            {
                // Log the exception
                Console.WriteLine($"Error in DsDoctRevProcedureAsync: {ex.Message}");
                return StatusCode(500, "Internal server error");
            }

        }

        [HttpGet("spInsRevProcedureAsync")]
        public async Task<dynamic> spInsRevProcedureAsync(string fromd, string tod)
        {
            try
            {
                // Retrieve token from Authorization header
                string authorizationHeader = Request.Headers["Authorization"];

                if (string.IsNullOrEmpty(authorizationHeader))
                {
                    return Unauthorized();
                }

                // Extract token from header (remove "Bearer " prefix)
                string token = authorizationHeader.Replace("Bearer ", "");

                // Decode token (not decrypt, assuming DecriptTocken is for decoding)
                UserTocken decodedToken = jwtHandler.DecriptTocken(authorizationHeader);

                if (decodedToken == null)
                {
                    return Unauthorized();
                }

                // Validate token
                var isValid = await jwtHandler.ValidateToken(token);

                if (isValid)
                {
                    // Return user details or appropriate response
                    //return Ok(new { Message = "User details retrieved successfully", UserDetails = decodedToken });
                    return await comrep.spInsRevProcedureAsync(fromd, tod);
                }
                else
                {
                    return Unauthorized();
                }
            }
            catch (Exception ex)
            {
                // Log the exception
                Console.WriteLine($"Error in spInsRevProcedureAsync: {ex.Message}");
                return StatusCode(500, "Internal server error");
            }

        }

        [HttpGet("DsIpRevProcedureAsync")]
        public async Task<dynamic> DsIpRevProcedureAsync(string fromd, string tod)
        {
            try
            {
                // Retrieve token from Authorization header
                string authorizationHeader = Request.Headers["Authorization"];

                if (string.IsNullOrEmpty(authorizationHeader))
                {
                    return Unauthorized();
                }

                // Extract token from header (remove "Bearer " prefix)
                string token = authorizationHeader.Replace("Bearer ", "");

                // Decode token (not decrypt, assuming DecriptTocken is for decoding)
                UserTocken decodedToken = jwtHandler.DecriptTocken(authorizationHeader);

                if (decodedToken == null)
                {
                    return Unauthorized();
                }

                // Validate token
                var isValid = await jwtHandler.ValidateToken(token);

                if (isValid)
                {
                    // Return user details or appropriate response
                    //return Ok(new { Message = "User details retrieved successfully", UserDetails = decodedToken });
                    return await comrep.DsIpRevProcedureAsync(fromd, tod);
                }
                else
                {
                    return Unauthorized();
                }
            }
            catch (Exception ex)
            {
                // Log the exception
                Console.WriteLine($"Error in DsIpRevProcedureAsync: {ex.Message}");
                return StatusCode(500, "Internal server error");
            }

        }

        [HttpGet("DsProcCategoryProcedureAsync")]
        public async Task<dynamic> DsProcCategoryProcedureAsync(string fromd, string tod)
        {
            try
            {
                // Retrieve token from Authorization header
                string authorizationHeader = Request.Headers["Authorization"];

                if (string.IsNullOrEmpty(authorizationHeader))
                {
                    return Unauthorized();
                }

                // Extract token from header (remove "Bearer " prefix)
                string token = authorizationHeader.Replace("Bearer ", "");

                // Decode token (not decrypt, assuming DecriptTocken is for decoding)
                UserTocken decodedToken = jwtHandler.DecriptTocken(authorizationHeader);

                if (decodedToken == null)
                {
                    return Unauthorized();
                }

                // Validate token
                var isValid = await jwtHandler.ValidateToken(token);

                if (isValid)
                {
                    // Return user details or appropriate response
                    //return Ok(new { Message = "User details retrieved successfully", UserDetails = decodedToken });
                    return await comrep.DsProcCategoryProcedureAsync(fromd, tod);
                }
                else
                {
                    return Unauthorized();
                }
            }
            catch (Exception ex)
            {
                // Log the exception
                Console.WriteLine($"Error in DsProcCategoryProcedureAsync: {ex.Message}");
                return StatusCode(500, "Internal server error");
            }

        }

        [HttpGet("DsProcGroupProcedureAsync")]
        public async Task<dynamic> DsProcGroupProcedureAsync(string fromd, string tod)
        {
            try
            {
                // Retrieve token from Authorization header
                string authorizationHeader = Request.Headers["Authorization"];

                if (string.IsNullOrEmpty(authorizationHeader))
                {
                    return Unauthorized();
                }

                // Extract token from header (remove "Bearer " prefix)
                string token = authorizationHeader.Replace("Bearer ", "");

                // Decode token (not decrypt, assuming DecriptTocken is for decoding)
                UserTocken decodedToken = jwtHandler.DecriptTocken(authorizationHeader);

                if (decodedToken == null)
                {
                    return Unauthorized();
                }

                // Validate token
                var isValid = await jwtHandler.ValidateToken(token);

                if (isValid)
                {
                    // Return user details or appropriate response
                    //return Ok(new { Message = "User details retrieved successfully", UserDetails = decodedToken });
                    return await comrep.DsProcGroupProcedureAsync(fromd, tod);
                }
                else
                {
                    return Unauthorized();
                }
            }
            catch (Exception ex)
            {
                // Log the exception
                Console.WriteLine($"Error in DsProcGroupProcedureAsync: {ex.Message}");
                return StatusCode(500, "Internal server error");
            }

        }

        [HttpGet("DsProcedureProcedureAsync")]
        public async Task<dynamic> DsProcedureProcedureAsync(string fromd, string tod)
        {
            try
            {
                // Retrieve token from Authorization header
                string authorizationHeader = Request.Headers["Authorization"];

                if (string.IsNullOrEmpty(authorizationHeader))
                {
                    return Unauthorized();
                }

                // Extract token from header (remove "Bearer " prefix)
                string token = authorizationHeader.Replace("Bearer ", "");

                // Decode token (not decrypt, assuming DecriptTocken is for decoding)
                UserTocken decodedToken = jwtHandler.DecriptTocken(authorizationHeader);

                if (decodedToken == null)
                {
                    return Unauthorized();
                }

                // Validate token
                var isValid = await jwtHandler.ValidateToken(token);

                if (isValid)
                {
                    // Return user details or appropriate response
                    //return Ok(new { Message = "User details retrieved successfully", UserDetails = decodedToken });
                    return await comrep.DsProcedureProcedureAsync(fromd, tod);
                }
                else
                {
                    return Unauthorized();
                }
            }
            catch (Exception ex)
            {
                // Log the exception
                Console.WriteLine($"Error in DsProcedureProcedureAsync: {ex.Message}");
                return StatusCode(500, "Internal server error");
            }

        }

        [HttpGet("DsPurchaseProcedureAsync")]
        public async Task<dynamic> DsPurchaseProcedureAsync(string fromd, string tod)
        {
            try
            {
                // Retrieve token from Authorization header
                string authorizationHeader = Request.Headers["Authorization"];

                if (string.IsNullOrEmpty(authorizationHeader))
                {
                    return Unauthorized();
                }

                // Extract token from header (remove "Bearer " prefix)
                string token = authorizationHeader.Replace("Bearer ", "");

                // Decode token (not decrypt, assuming DecriptTocken is for decoding)
                UserTocken decodedToken = jwtHandler.DecriptTocken(authorizationHeader);

                if (decodedToken == null)
                {
                    return Unauthorized();
                }

                // Validate token
                var isValid = await jwtHandler.ValidateToken(token);

                if (isValid)
                {
                    // Return user details or appropriate response
                    //return Ok(new { Message = "User details retrieved successfully", UserDetails = decodedToken });
                    return await comrep.DsPurchaseProcedureAsync(fromd, tod);
                }
                else
                {
                    return Unauthorized();
                }
            }
            catch (Exception ex)
            {
                // Log the exception
                Console.WriteLine($"Error in DsPurchaseProcedureAsync: {ex.Message}");
                return StatusCode(500, "Internal server error");
            }

        }

        [HttpGet("CallCollectionSctProcedureAsync")]
        public async Task<dynamic> CallCollectionSctProcedureAsync(string fromd, string tod,string branchid)
        {
            try
            {
                // Retrieve token from Authorization header
                string authorizationHeader = Request.Headers["Authorization"];

                if (string.IsNullOrEmpty(authorizationHeader))
                {
                    return Unauthorized();
                }

                // Extract token from header (remove "Bearer " prefix)
                string token = authorizationHeader.Replace("Bearer ", "");

                // Decode token (not decrypt, assuming DecriptTocken is for decoding)
                UserTocken decodedToken = jwtHandler.DecriptTocken(authorizationHeader);

                if (decodedToken == null)
                {
                    return Unauthorized();
                }

                // Validate token
                var isValid = await jwtHandler.ValidateToken(token);

                if (isValid)
                {
                    // Return user details or appropriate response
                    //return Ok(new { Message = "User details retrieved successfully", UserDetails = decodedToken });
                    return await comrep.CallCollectionSctProcedureAsync(fromd, tod, branchid);
                }
                else
                {
                    return Unauthorized();
                }
            }
            catch (Exception ex)
            {
                // Log the exception
                Console.WriteLine($"Error in CallCollectionSctProcedureAsync: {ex.Message}");
                return StatusCode(500, "Internal server error");
            }

        }

        [HttpGet("CallCollectionProcedureAsync")]
        public async Task<dynamic> CallCollectionProcedureAsync(string fromd, string tod,string branchid)
        {
            try
            {
                // Retrieve token from Authorization header
                string authorizationHeader = Request.Headers["Authorization"];

                if (string.IsNullOrEmpty(authorizationHeader))
                {
                    return Unauthorized();
                }

                // Extract token from header (remove "Bearer " prefix)
                string token = authorizationHeader.Replace("Bearer ", "");

                // Decode token (not decrypt, assuming DecriptTocken is for decoding)
                UserTocken decodedToken = jwtHandler.DecriptTocken(authorizationHeader);

                if (decodedToken == null)
                {
                    return Unauthorized();
                }

                // Validate token
                var isValid = await jwtHandler.ValidateToken(token);

                if (isValid)
                {
                    // Return user details or appropriate response
                    //return Ok(new { Message = "CallCollectionProcedureAsync details retrieved successfully", UserDetails = decodedToken });
                    return await comrep.CallCollectionProcedureAsync(fromd, tod, branchid);
                }
                else
                {
                    return Unauthorized();
                }
            }
            catch (Exception ex)
            {
                // Log the exception
                Console.WriteLine($"Error in CallCollectionProcedureAsync: {ex.Message}");
                return StatusCode(500, "Internal server error");
            }

        }

        [HttpGet("CallPackageProcedureAsync")]
        public async Task<dynamic> CallPackageProcedureAsync(string fromd, string tod)
        {
            try
            {
                // Retrieve token from Authorization header
                string authorizationHeader = Request.Headers["Authorization"];

                if (string.IsNullOrEmpty(authorizationHeader))
                {
                    return Unauthorized();
                }

                // Extract token from header (remove "Bearer " prefix)
                string token = authorizationHeader.Replace("Bearer ", "");

                // Decode token (not decrypt, assuming DecriptTocken is for decoding)
                UserTocken decodedToken = jwtHandler.DecriptTocken(authorizationHeader);

                if (decodedToken == null)
                {
                    return Unauthorized();
                }

                // Validate token
                var isValid = await jwtHandler.ValidateToken(token);

                if (isValid)
                {
                    // Return user details or appropriate response
                    //return Ok(new { Message = "CallCollectionProcedureAsync details retrieved successfully", UserDetails = decodedToken });
                    return await comrep.CallPackageProcedureAsync(fromd, tod);
                }
                else
                {
                    return Unauthorized();
                }
            }
            catch (Exception ex)
            {
                // Log the exception
                Console.WriteLine($"Error in CallPackageProcedureAsync: {ex.Message}");
                return StatusCode(500, "Internal server error");
            }

        }

    }
}
