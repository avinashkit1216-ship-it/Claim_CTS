using System.Text.Json;
using ClaimSubmission.API.Models;

namespace ClaimSubmission.API.Data.LocalStorage
{
    /// <summary>
    /// Service for managing local JSON-based storage operations
    /// </summary>
    public class LocalStorageService
    {
        private readonly string _storagePath;
        private readonly ILogger<LocalStorageService> _logger;
        private static readonly object _lockObj = new();

        public LocalStorageService(ILogger<LocalStorageService> logger)
        {
            _logger = logger;
            // Use app data directory in the project root
            _storagePath = Path.Combine(AppContext.BaseDirectory, "..", "..", "Data", "LocalStorage", "data");
            EnsureStorageDirectoryExists();
        }

        private void EnsureStorageDirectoryExists()
        {
            try
            {
                if (!Directory.Exists(_storagePath))
                {
                    Directory.CreateDirectory(_storagePath);
                    _logger.LogInformation($"Created storage directory at: {_storagePath}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error creating storage directory: {_storagePath}");
                throw;
            }
        }

        /// <summary>
        /// Read all items from a JSON file
        /// </summary>
        public async Task<List<T>> ReadAllAsync<T>(string fileName) where T : class
        {
            try
            {
                lock (_lockObj)
                {
                    var filePath = Path.Combine(_storagePath, fileName);
                    
                    if (!File.Exists(filePath))
                    {
                        _logger.LogDebug($"File not found: {filePath}, returning empty list");
                        return new List<T>();
                    }

                    var json = File.ReadAllText(filePath);
                    if (string.IsNullOrWhiteSpace(json))
                    {
                        return new List<T>();
                    }

                    var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                    return JsonSerializer.Deserialize<List<T>>(json, options) ?? new List<T>();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error reading from {fileName}");
                throw;
            }
        }

        /// <summary>
        /// Read a single item by ID
        /// </summary>
        public async Task<T?> ReadByIdAsync<T>(string fileName, int id) where T : class
        {
            try
            {
                var items = await ReadAllAsync<T>(fileName);
                var idProperty = typeof(T).GetProperty("Id") ?? typeof(T).GetProperty($"{typeof(T).Name}Id");
                
                if (idProperty == null)
                {
                    _logger.LogWarning($"No ID property found on type {typeof(T).Name}");
                    return null;
                }

                return items.FirstOrDefault(item => 
                    idProperty.GetValue(item) is int itemId && itemId == id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error reading item with ID {id} from {fileName}");
                throw;
            }
        }

        /// <summary>
        /// Write all items to a JSON file
        /// </summary>
        public async Task WriteAllAsync<T>(string fileName, List<T> items) where T : class
        {
            try
            {
                lock (_lockObj)
                {
                    var filePath = Path.Combine(_storagePath, fileName);
                    var options = new JsonSerializerOptions 
                    { 
                        PropertyNameCaseInsensitive = true,
                        WriteIndented = true 
                    };
                    
                    var json = JsonSerializer.Serialize(items, options);
                    File.WriteAllText(filePath, json);
                    _logger.LogDebug($"Wrote {items.Count} items to {fileName}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error writing to {fileName}");
                throw;
            }
        }

        /// <summary>
        /// Add a new item and return its generated ID
        /// </summary>
        public async Task<int> AddAsync<T>(string fileName, T item) where T : class
        {
            try
            {
                var items = await ReadAllAsync<T>(fileName);
                var idProperty = typeof(T).GetProperty("Id") ?? typeof(T).GetProperty($"{typeof(T).Name}Id");
                
                if (idProperty == null)
                {
                    throw new InvalidOperationException($"No ID property found on type {typeof(T).Name}");
                }

                // Generate new ID (max existing ID + 1)
                int newId = items.Any() ? items.Max(item => (int)(idProperty.GetValue(item) ?? 0)) + 1 : 1;
                
                // Set the ID on the new item
                idProperty.SetValue(item, newId);
                
                items.Add(item);
                await WriteAllAsync(fileName, items);
                
                _logger.LogInformation($"Added new item with ID {newId} to {fileName}");
                return newId;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error adding item to {fileName}");
                throw;
            }
        }

        /// <summary>
        /// Update an existing item
        /// </summary>
        public async Task UpdateAsync<T>(string fileName, int id, T updatedItem) where T : class
        {
            try
            {
                var items = await ReadAllAsync<T>(fileName);
                var idProperty = typeof(T).GetProperty("Id") ?? typeof(T).GetProperty($"{typeof(T).Name}Id");
                
                if (idProperty == null)
                {
                    throw new InvalidOperationException($"No ID property found on type {typeof(T).Name}");
                }

                var existingItem = items.FirstOrDefault(item => 
                    idProperty.GetValue(item) is int itemId && itemId == id);

                if (existingItem == null)
                {
                    throw new KeyNotFoundException($"Item with ID {id} not found in {fileName}");
                }

                // Copy properties from updatedItem to existingItem
                var properties = typeof(T).GetProperties();
                foreach (var prop in properties)
                {
                    if (prop.CanWrite && prop.CanRead)
                    {
                        var value = prop.GetValue(updatedItem);
                        prop.SetValue(existingItem, value);
                    }
                }

                await WriteAllAsync(fileName, items);
                _logger.LogInformation($"Updated item with ID {id} in {fileName}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error updating item {id} in {fileName}");
                throw;
            }
        }

        /// <summary>
        /// Delete an item by ID
        /// </summary>
        public async Task DeleteAsync<T>(string fileName, int id) where T : class
        {
            try
            {
                var items = await ReadAllAsync<T>(fileName);
                var idProperty = typeof(T).GetProperty("Id") ?? typeof(T).GetProperty($"{typeof(T).Name}Id");
                
                if (idProperty == null)
                {
                    throw new InvalidOperationException($"No ID property found on type {typeof(T).Name}");
                }

                var itemToRemove = items.FirstOrDefault(item => 
                    idProperty.GetValue(item) is int itemId && itemId == id);

                if (itemToRemove != null)
                {
                    items.Remove(itemToRemove);
                    await WriteAllAsync(fileName, items);
                    _logger.LogInformation($"Deleted item with ID {id} from {fileName}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error deleting item {id} from {fileName}");
                throw;
            }
        }

        /// <summary>
        /// Query items with filtering and sorting
        /// </summary>
        public async Task<List<T>> QueryAsync<T>(string fileName, Func<T, bool> predicate) where T : class
        {
            try
            {
                var items = await ReadAllAsync<T>(fileName);
                return items.Where(predicate).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error querying items from {fileName}");
                throw;
            }
        }

        /// <summary>
        /// Get the storage path for reference
        /// </summary>
        public string GetStoragePath() => _storagePath;
    }
}
