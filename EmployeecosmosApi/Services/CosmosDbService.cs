using EmployeecosmosApi.Models;

using Microsoft.Azure.Cosmos;
namespace EmployeecosmosApi.Services
{


 
        public class CosmosDbService : ICosmosDbService
        {
            private Container _container;

            public CosmosDbService(
                CosmosClient dbClient,
                string databaseName,
                string containerName)
            {
            this._container = dbClient.GetContainer(databaseName, containerName);
            Console.WriteLine(databaseName + " " + containerName);
            }

            public async Task<Employee> AddEmployeeAsync(Employee employee)
            {
                try
            {
               
                    employee.Id = Guid.NewGuid().ToString();
                    ItemResponse<Employee> response = await this._container.CreateItemAsync<Employee>(employee, new PartitionKey(employee.Department));
                    return response.Resource;
                }
                catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.Conflict)
                {
                    throw new InvalidOperationException($"Employee with ID {employee.Id} already exists.");
                }
            }

            public async Task DeleteEmployeeAsync(string id)
            {
                try
                {
                    // First get the employee to retrieve the partition key
                    var employee = await GetEmployeeAsync(id);
                    if (employee != null)
                    {
                        await this._container.DeleteItemAsync<Employee>(id, new PartitionKey(employee.Department));
                    }
                }
                catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    throw new KeyNotFoundException($"Employee with ID {id} not found.");
                }
            }

            public async Task<Employee?> GetEmployeeAsync(string id)
            {
                try
                {
                    // Query to find the employee regardless of partition key
                    var queryDefinition = new QueryDefinition("SELECT * FROM c WHERE c.id = @id")
                        .WithParameter("@id", id);

                    using FeedIterator<Employee> feed = this._container.GetItemQueryIterator<Employee>(queryDefinition);

                    while (feed.HasMoreResults)
                    {
                        FeedResponse<Employee> response = await feed.ReadNextAsync();
                        return response.FirstOrDefault();
                    }

                    return null;
                }
                catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    return null;
                }
            }

            public async Task<IEnumerable<Employee>> GetEmployeesAsync(string queryString)
            {
                var query = this._container.GetItemQueryIterator<Employee>(new QueryDefinition(queryString));
                List<Employee> results = new List<Employee>();
                while (query.HasMoreResults)
                {
                    var response = await query.ReadNextAsync();
                    results.AddRange(response.ToList());
                }

                return results;
            }

            public async Task<IEnumerable<Employee>> GetEmployeesByDepartmentAsync(string department)
            {
                var queryDefinition = new QueryDefinition("SELECT * FROM c WHERE c.department = @department")
                    .WithParameter("@department", department);

                var query = this._container.GetItemQueryIterator<Employee>(queryDefinition);
                List<Employee> results = new List<Employee>();
                while (query.HasMoreResults)
                {
                    var response = await query.ReadNextAsync();
                    results.AddRange(response.ToList());
                }

                return results;
            }

            public async Task UpdateEmployeeAsync(string id, Employee employee)
            {
                try
                {
                    // First get the existing employee to maintain the partition key
                    var existingEmployee = await GetEmployeeAsync(id);
                    if (existingEmployee == null)
                    {
                        throw new KeyNotFoundException($"Employee with ID {id} not found.");
                    }

                    employee.Id = id;
                    await this._container.UpsertItemAsync<Employee>(employee, new PartitionKey(employee.Department));
                }
                catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    throw new KeyNotFoundException($"Employee with ID {id} not found.");
                }
            }
        }
    }
