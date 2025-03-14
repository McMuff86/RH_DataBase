using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Supabase;
using Supabase.Gotrue;
using Postgrest;
using RH_DataBase.Models;
using RH_DataBase.Config;

namespace RH_DataBase.Services
{
    public class SupabaseService
    {
        private static Supabase.Client _client;
        private static SupabaseService _instance;

        public static SupabaseService Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new SupabaseService();
                }
                return _instance;
            }
        }

        private SupabaseService()
        {
            Initialize();
        }

        private void Initialize()
        {
            try
            {
                var options = new SupabaseOptions
                {
                    AutoRefreshToken = true,
                    AutoConnectRealtime = true
                };

                _client = new Supabase.Client(SupabaseConfig.SupabaseUrl, SupabaseConfig.SupabaseAnonKey, options);
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to initialize Supabase client: {ex.Message}");
            }
        }

        #region Part Operations

        public async Task<List<Part>> GetAllPartsAsync()
        {
            try
            {
                var response = await _client.From<Part>()
                    .Get();
                return response.Models;
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to fetch parts: {ex.Message}");
            }
        }

        public async Task<Part> GetPartByIdAsync(int id)
        {
            try
            {
                var response = await _client.From<Part>()
                    .Where(p => p.Id == id)
                    .Single();
                return response;
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to fetch part with ID {id}: {ex.Message}");
            }
        }

        public async Task<List<Part>> SearchPartsByNameAsync(string searchTerm)
        {
            try
            {
                var response = await _client.From<Part>()
                    .Filter("name", Postgrest.Constants.Operator.ILike, $"%{searchTerm}%")
                    .Get();
                return response.Models;
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to search parts: {ex.Message}");
            }
        }

        public async Task<Part> CreatePartAsync(Part part)
        {
            try
            {
                var response = await _client.From<Part>()
                    .Insert(part);
                return response.Models[0];
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to create part: {ex.Message}");
            }
        }

        public async Task<Part> UpdatePartAsync(Part part)
        {
            try
            {
                var response = await _client.From<Part>()
                    .Where(p => p.Id == part.Id)
                    .Update(part);
                return response.Models[0];
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to update part: {ex.Message}");
            }
        }

        public async Task DeletePartAsync(int id)
        {
            try
            {
                await _client.From<Part>()
                    .Where(p => p.Id == id)
                    .Delete();
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to delete part with ID {id}: {ex.Message}");
            }
        }

        #endregion

        #region Drawing Operations

        public async Task<List<Drawing>> GetAllDrawingsAsync()
        {
            try
            {
                var response = await _client.From<Drawing>()
                    .Get();
                return response.Models;
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to fetch drawings: {ex.Message}");
            }
        }

        public async Task<Drawing> GetDrawingByIdAsync(int id)
        {
            try
            {
                var response = await _client.From<Drawing>()
                    .Where(d => d.Id == id)
                    .Single();
                return response;
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to fetch drawing with ID {id}: {ex.Message}");
            }
        }

        public async Task<List<Drawing>> GetDrawingsForPartAsync(int partId)
        {
            try
            {
                var response = await _client.From<Drawing>()
                    .Where(d => d.PartId == partId)
                    .Get();
                return response.Models;
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to fetch drawings for part with ID {partId}: {ex.Message}");
            }
        }

        public async Task<Drawing> CreateDrawingAsync(Drawing drawing)
        {
            try
            {
                var response = await _client.From<Drawing>()
                    .Insert(drawing);
                return response.Models[0];
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to create drawing: {ex.Message}");
            }
        }

        public async Task<Drawing> UpdateDrawingAsync(Drawing drawing)
        {
            try
            {
                var response = await _client.From<Drawing>()
                    .Where(d => d.Id == drawing.Id)
                    .Update(drawing);
                return response.Models[0];
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to update drawing: {ex.Message}");
            }
        }

        public async Task DeleteDrawingAsync(int id)
        {
            try
            {
                await _client.From<Drawing>()
                    .Where(d => d.Id == id)
                    .Delete();
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to delete drawing with ID {id}: {ex.Message}");
            }
        }

        #endregion
    }
} 