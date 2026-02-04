using Newtonsoft.Json;

namespace EmployeecosmosApi.Models
{

    public class Employee
    {
        [JsonProperty("id")]
        public string Id { get; set; } = Guid.NewGuid().ToString();

        [JsonProperty("employeeId")]
        public string EmployeeId { get; set; } = string.Empty;

        [JsonProperty("firstName")]
        public string FirstName { get; set; } = string.Empty;

        [JsonProperty("lastName")]
        public string LastName { get; set; } = string.Empty;

        [JsonProperty("email")]
        public string Email { get; set; } = string.Empty;

        [JsonProperty("department")]
        public string Department { get; set; } = string.Empty;

        [JsonProperty("position")]
        public string Position { get; set; } = string.Empty;

        [JsonProperty("salary")]
        public decimal Salary { get; set; }

        [JsonProperty("hireDate")]
        public DateTime HireDate { get; set; } = DateTime.UtcNow;


    }
}