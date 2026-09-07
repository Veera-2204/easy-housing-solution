using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace Functional
{
    public static class Function1
    {
        // Connection string to your SQL database
        private static readonly string ConnectionString = Environment.GetEnvironmentVariable("SqlConnectionString");

        [FunctionName("UpdatePropertyStatus")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            log.LogInformation("Processing request to update property status.");

            // Read request body
            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            PropertyUpdateRequest data;

            try
            {
                data = JsonConvert.DeserializeObject<PropertyUpdateRequest>(requestBody);
            }
            catch (JsonException ex)
            {
                log.LogError(ex, "Invalid JSON format.");
                return new BadRequestObjectResult("Invalid JSON format.");
            }

            if (data == null || data.PropertyId <= 0 || !data.IsActive.GetType().IsAssignableFrom(typeof(bool)))
            {
                return new BadRequestObjectResult("Invalid request body. Ensure 'propertyId' is a positive integer and 'isActive' is a boolean.");
            }

            try
            {
                using (SqlConnection sqlConnection = new SqlConnection(ConnectionString))
                {
                    string query = "UPDATE Properties SET IsActive = @isActive WHERE PropertyId = @propertyId";
                    using (SqlCommand cmd = new SqlCommand(query, sqlConnection))
                    {
                        cmd.Parameters.AddWithValue("@isActive", data.IsActive);
                        cmd.Parameters.AddWithValue("@propertyId", data.PropertyId);

                        await sqlConnection.OpenAsync();
                        int rowsAffected = await cmd.ExecuteNonQueryAsync();

                        return new OkObjectResult($"{rowsAffected} rows affected.");
                    }
                }
            }
            catch (Exception ex)
            {
                log.LogError(ex, "An error occurred while updating property status."+ex.Message);
                return new StatusCodeResult(StatusCodes.Status500InternalServerError);
            }
        }
    }

    public class PropertyUpdateRequest
    {
        public int PropertyId { get; set; }
        public bool IsActive { get; set; }
    }
}
