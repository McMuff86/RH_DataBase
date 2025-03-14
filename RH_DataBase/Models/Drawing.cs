using System;
using Postgrest.Attributes;
using Postgrest.Models;
using Newtonsoft.Json;

namespace RH_DataBase.Models
{
    [Table("drawings")]
    public class Drawing : BaseModel
    {
        [PrimaryKey("id")]
        public int Id { get; set; }

        [Column("title")]
        public string Title { get; set; }

        [Column("description")]
        public string Description { get; set; }

        [Column("drawing_number")]
        public string DrawingNumber { get; set; }

        [Column("revision")]
        public string Revision { get; set; }

        [Column("file_path")]
        public string FilePath { get; set; }

        [Column("file_type")]
        public string FileType { get; set; }

        [Column("created_by")]
        public string CreatedBy { get; set; }

        [Column("approved_by")]
        public string ApprovedBy { get; set; }

        [Column("created_at")]
        public DateTime CreatedAt { get; set; }

        [Column("updated_at")]
        public DateTime UpdatedAt { get; set; }

        [Column("part_id")]
        public int? PartId { get; set; }

        // Zusätzliche Eigenschaften für die UI
        [JsonIgnore]
        public bool IsSelected { get; set; }
    }
} 