using System;
using System.IO;
using System.Threading.Tasks;
using Rhino;
using Rhino.Commands;
using RH_DataBase.Services;

namespace RH_DataBase.Tests
{
    public class BucketTest : Command
    {
        public override string EnglishName => "TestSupabaseBuckets";

        protected override Result RunCommand(RhinoDoc doc, RunMode mode)
        {
            // Teste asynchron die Supabase-Buckets
            Task.Run(async () => await TestBuckets()).Wait();
            return Result.Success;
        }

        private async Task TestBuckets()
        {
            try
            {
                RhinoApp.WriteLine("Teste Supabase Buckets...");
                
                // Service-Instanz abrufen
                var supabaseService = SupabaseService.Instance;
                
                // Überprüfe uploadrhino Bucket
                RhinoApp.WriteLine("Überprüfe 'uploadrhino' Bucket...");
                bool uploadRhinoExists = await supabaseService.CreateBucketIfNotExistsAsync("uploadrhino", true);
                RhinoApp.WriteLine($"uploadrhino Bucket existiert oder wurde erstellt: {uploadRhinoExists}");
                
                // Überprüfe blocks Bucket
                RhinoApp.WriteLine("Überprüfe 'blocks' Bucket...");
                bool blocksExists = await supabaseService.CreateBucketIfNotExistsAsync("blocks", true);
                RhinoApp.WriteLine($"blocks Bucket existiert oder wurde erstellt: {blocksExists}");
                
                // Test-Upload in uploadrhino Bucket
                RhinoApp.WriteLine("Teste Upload in 'uploadrhino' Bucket...");
                
                // Temporäre Testdatei erstellen
                string testFilePath = Path.GetTempFileName();
                File.WriteAllText(testFilePath, "Dies ist ein Testinhalt für Supabase Bucket-Test");
                
                try
                {
                    string fileName = $"test-{DateTime.Now.Ticks}.txt";
                    RhinoApp.WriteLine($"Uploading {fileName} zu 'uploadrhino'...");
                    
                    string fileUrl = await supabaseService.UploadFileAsync("uploadrhino", testFilePath, fileName);
                    RhinoApp.WriteLine($"Upload erfolgreich! URL: {fileUrl}");
                    
                    // Versuchen, die Datei zu löschen
                    RhinoApp.WriteLine($"Versuche, die Datei wieder zu löschen...");
                    await supabaseService.DeleteFileAsync("uploadrhino", fileName);
                    RhinoApp.WriteLine("Datei erfolgreich gelöscht!");
                }
                catch (Exception ex)
                {
                    RhinoApp.WriteLine($"Fehler bei Upload/Delete Test: {ex.Message}");
                }
                
                // Test-Upload in blocks Bucket
                RhinoApp.WriteLine("Teste Upload in 'blocks' Bucket...");
                
                try
                {
                    string fileName = $"test-{DateTime.Now.Ticks}.txt";
                    RhinoApp.WriteLine($"Uploading {fileName} zu 'blocks'...");
                    
                    string fileUrl = await supabaseService.UploadFileAsync("blocks", testFilePath, fileName);
                    RhinoApp.WriteLine($"Upload erfolgreich! URL: {fileUrl}");
                    
                    // Versuchen, die Datei zu löschen
                    RhinoApp.WriteLine($"Versuche, die Datei wieder zu löschen...");
                    await supabaseService.DeleteFileAsync("blocks", fileName);
                    RhinoApp.WriteLine("Datei erfolgreich gelöscht!");
                }
                catch (Exception ex)
                {
                    RhinoApp.WriteLine($"Fehler bei Upload/Delete Test: {ex.Message}");
                }
                
                // Temporäre Datei löschen
                try { File.Delete(testFilePath); } catch {}
                
                RhinoApp.WriteLine("Bucket-Tests abgeschlossen.");
            }
            catch (Exception ex)
            {
                RhinoApp.WriteLine($"Fehler: {ex.Message}");
                if (ex.InnerException != null)
                {
                    RhinoApp.WriteLine($"Inner Exception: {ex.InnerException.Message}");
                }
            }
        }
    }
} 