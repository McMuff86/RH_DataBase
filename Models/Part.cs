using System;
using System.Collections.Generic;
using Postgrest.Attributes;
using Postgrest.Models;
using Newtonsoft.Json;

namespace RH_DataBase.Models
{
    [Table("parts")]
    public class Part : BaseModel
    {
        [PrimaryKey("id")]
        public int Id { get; set; }

        [Column("name")]
        public string Name { get; set; }

        [Column("description")]
        public string Description { get; set; }

        [Column("category")]
        public string Category { get; set; }

        [Column("material")]
        public string Material { get; set; }

        [Column("dimensions")]
        public string Dimensions { get; set; }

        [Column("weight")]
        public double? Weight { get; set; }

        [Column("model_path")]
        public string ModelPath { get; set; }

        [Column("thumbnail_url")]
        public string ThumbnailUrl { get; set; }

        [Column("created_at")]
        public DateTime CreatedAt { get; set; }

        [Column("updated_at")]
        public DateTime UpdatedAt { get; set; }

        [Column("drawing_ids")]
        [JsonProperty("drawing_ids")]
        public List<int> DrawingIds { get; set; } = new List<int>();

        // Zusätzliche Eigenschaften zum Handling in Rhino
        [JsonIgnore]
        public bool IsSelected { get; set; }

        [JsonIgnore]
        public bool IsVisible { get; set; } = true;
    }
} 