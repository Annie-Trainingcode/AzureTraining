using EmployeecosmosApi.Models;
namespace EmployeecosmosApi.Services;

public interface ICosmosDbService
{
    Task<IEnumerable<Employee>> GetEmployeesAsync(string queryString);
    Task<Employee?> GetEmployeeAsync(string id);
    Task<Employee> AddEmployeeAsync(Employee employee);
    Task UpdateEmployeeAsync(string id, Employee employee);
    Task DeleteEmployeeAsync(string id);
    Task<IEnumerable<Employee>> GetEmployeesByDepartmentAsync(string department);
}
